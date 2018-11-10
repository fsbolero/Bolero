module internal Bolero.Templating.Parsing

open System.Text.RegularExpressions
open FSharp.Quotations
open HtmlAgilityPack
open Bolero

type HoleType =
    | String
    | Html
    | Event

module HoleType =

    let Merge name t1 t2 =
        if t1 = t2 then t1 else
        match t1, t2 with
        | String, Html | Html, String -> String
        | _ -> failwithf "Hole name used multiple times with incompatible types: %s" name

    let TypeOf = function
        | String -> typeof<string>
        | Html -> typeof<Node>
        | Event -> typeof<unit -> unit> // TODO arg type

    let Wrap (innerType: HoleType) (outerType: HoleType) (expr: Expr) =
        if innerType = outerType then expr else
        match innerType, outerType with
        | Html, String -> <@@ Node.Text %%expr @@>
        | a, b -> failwithf "Hole name used multiple times with incompatible types (%A, %A)" a b

type Hole =
    {
        Var: Var
        Type: HoleType
    }

type Holes = Map<string, Hole>

type Parsed<'T> =
    {
        Holes: Holes
        Expr: Expr<'T>
    }

module Parsed =

    let MergeHoles (holes1: Holes) (holes2: Holes) =
        (holes1, holes2) ||> Map.fold (fun map key hole ->
            let hole =
                match Map.tryFind key map with
                | None -> hole
                | Some hole2 ->
                    let ty = HoleType.Merge key hole.Type hole2.Type
                    let var = if ty = hole.Type then hole.Var else hole2.Var
                    { Var = var; Type = ty }
            Map.add key hole map
        )

    let MergeManyHoles (holes: seq<Holes>) =
        Seq.fold MergeHoles Map.empty holes
    
    let Concat (p: seq<Parsed<'T>>) : Parsed<'T[]> =
        let finalHoles = MergeManyHoles [ for p in p -> p.Holes ]
        let exprs = p |> Seq.map (fun p ->
            (p.Expr, p.Holes) ||> Map.fold (fun e k v ->
                // Map var names for holes used multiple times
                match Map.tryFind k finalHoles with
                | Some v' when v'.Var <> v.Var ->
                    let value = Expr.Var v'.Var |> HoleType.Wrap v.Type v'.Type
                    Expr.Let(v.Var, value, e) |> Expr.Cast
                | _ -> e
            )
        )
        {
            Holes = finalHoles
            Expr = Expr.TypedArray<'T> exprs
        }

    let Map (f: Expr<'T> -> Expr<'U>) (p: Parsed<'T>) : Parsed<'U> =
        {
            Holes = p.Holes
            Expr = f p.Expr
        }

    let Map2 (f: Expr<'T> -> Expr<'U> -> Expr<'V>) (p1: Parsed<'T>) (p2: Parsed<'U>) : Parsed<'V> =
        {
            Holes = MergeHoles p1.Holes p2.Holes
            Expr = f p1.Expr p2.Expr
        }

let HoleNameRE = Regex(@"\${(\w+)}", RegexOptions.Compiled)

type TextPart =
    | Plain of string
    | Hole of Hole

let ParseText (t: string) (holeType: HoleType) : Holes * TextPart[] =
    let parse = HoleNameRE.Matches(t) |> Seq.cast<Match> |> Array.ofSeq
    if Array.isEmpty parse then Map.empty, [|Plain t|] else
    let parts = ResizeArray()
    let mutable lastHoleEnd = 0
    let mutable holes = Map.empty
    let getHole holeName =
        match Map.tryFind holeName holes with
        | Some hole -> hole
        | None ->
            let var = Var(holeName, HoleType.TypeOf holeType)
            let hole = { Type = holeType; Var = var }
            holes <- Map.add holeName hole holes
            hole
    for p in parse do
        if p.Index > lastHoleEnd then
            parts.Add(Plain t.[lastHoleEnd..p.Index - 1])
        let hole = getHole p.Groups.[1].Value
        parts.Add(Hole hole)
        lastHoleEnd <- p.Index + p.Length
    if lastHoleEnd < t.Length then
        parts.Add(Plain t.[lastHoleEnd..t.Length - 1])
    holes, parts.ToArray()

let ParseAttribute (attr: HtmlAttribute) : Parsed<Attr> =
    let holes, parts = ParseText attr.Value HoleType.String
    let name = attr.Name
    let value = Expr.TypedArray<string> [
        for part in parts do
            match part with
            | Plain t -> yield <@ t @>
            | Hole h -> yield Expr.Var h.Var |> Expr.Cast
    ]
    { Holes = holes; Expr = <@ Attr(name, String.concat "" %value) @> }

let EmptyNode : Parsed<Node> =
    { Holes = Map.empty; Expr = <@ Node.Empty @> }

let rec ParseNode (node: HtmlNode) : Parsed<Node> =
    match node.NodeType with
    | HtmlNodeType.Element ->
        let name = node.Name
        let attrs =
            node.Attributes
            |> Seq.map ParseAttribute
            |> Parsed.Concat
        let children =
            node.ChildNodes
            |> Seq.map ParseNode
            |> Parsed.Concat
        (attrs, children)
        ||> Parsed.Map2 (fun attrs children ->
            <@ Node.Elt(name, List.ofArray %attrs, List.ofArray %children) @>
        ) 
    | HtmlNodeType.Text ->
        let holes, parts = ParseText (node :?> HtmlTextNode).Text HoleType.Html
        let text = [
            for part in parts do
                match part with
                | Plain t -> yield <@ Node.Text t @>
                | Hole h -> yield Expr.Var h.Var |> Expr.Cast
        ]
        let expr =
            match text with
            | [] -> <@ Node.Empty @>
            | [x] -> x
            | xs -> <@ Node.Concat(List.ofArray %(Expr.TypedArray<Node> xs)) @>
        { Holes = holes; Expr = expr }
    | _ ->
        EmptyNode

let ParseDoc (doc: HtmlDocument) =
    doc.DocumentNode.ChildNodes
    |> Seq.map ParseNode
    |> Parsed.Concat
    |> Parsed.Map (fun e -> <@ Node.Concat (List.ofArray %e) @>)

let GetDoc (fileOrContent: string) =
    let doc = HtmlDocument()
    if fileOrContent.StartsWith("<") then
        doc.LoadHtml(fileOrContent)
    else
        doc.Load(fileOrContent)
    doc

let ParseFileOrContent (fileOrContent: string) =
    fileOrContent
    |> GetDoc
    |> ParseDoc
