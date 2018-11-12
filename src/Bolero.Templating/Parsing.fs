module Bolero.Templating.Parsing

open System
open System.Text.RegularExpressions
open FSharp.Quotations
open Microsoft.AspNetCore.Blazor
open Microsoft.AspNetCore.Blazor.Components
open HtmlAgilityPack
open Bolero
open Bolero.TemplatingInternals

type HoleType =
    | String
    | Html
    | Event of argType: Type
    | DataBinding

module HoleType =
    open ProviderImplementation.ProvidedTypes

    let Merge name t1 t2 =
        if t1 = t2 then t1 else
        match t1, t2 with
        | String, Html | Html, String -> String
        | Event _, Event _ -> Event typeof<UIEventArgs>
        | DataBinding, String | String, DataBinding
        | DataBinding, Html | Html, DataBinding -> DataBinding
        | _ -> failwithf "Hole name used multiple times with incompatible types: %s" name

    let EventHandlerOf argType =
        ProvidedTypeBuilder.MakeGenericType(typedefof<Action<_>>, [argType])

    let TypeOf = function
        | String -> typeof<string>
        | Html -> typeof<Node>
        | Event argType -> EventHandlerOf argType
        | DataBinding -> typeof<string * Action<string>>

    let Wrap (innerType: HoleType) (outerType: HoleType) (expr: Expr) =
        if innerType = outerType then None else
        match innerType, outerType with
        | Html, String -> Some <@@ Node.Text %%expr @@>
        | Event argTy, Event _ -> Some <| Expr.Coerce(expr, EventHandlerOf argTy)
        | String, DataBinding -> Some <@@ fst (%%expr: string * Action<string>) @@>
        | Html, DataBinding -> Some <@@ Node.Text (fst (%%expr: string * Action<string>)) @@>
        | a, b -> failwithf "Hole name used multiple times with incompatible types (%A, %A)" a b

    let EventArg name =
        match name with
// BEGIN EVENTS
        | "onfocus" -> typeof<UIFocusEventArgs>
        | "onblur" -> typeof<UIFocusEventArgs>
        | "onfocusin" -> typeof<UIFocusEventArgs>
        | "onfocusout" -> typeof<UIFocusEventArgs>
        | "onmouseover" -> typeof<UIMouseEventArgs>
        | "onmouseout" -> typeof<UIMouseEventArgs>
        | "onmousemove" -> typeof<UIMouseEventArgs>
        | "onmousedown" -> typeof<UIMouseEventArgs>
        | "onmouseup" -> typeof<UIMouseEventArgs>
        | "onclick" -> typeof<UIMouseEventArgs>
        | "ondblclick" -> typeof<UIMouseEventArgs>
        | "onwheel" -> typeof<UIMouseEventArgs>
        | "onmousewheel" -> typeof<UIMouseEventArgs>
        | "oncontextmenu" -> typeof<UIMouseEventArgs>
        | "ondrag" -> typeof<UIDragEventArgs>
        | "ondragend" -> typeof<UIDragEventArgs>
        | "ondragenter" -> typeof<UIDragEventArgs>
        | "ondragleave" -> typeof<UIDragEventArgs>
        | "ondragover" -> typeof<UIDragEventArgs>
        | "ondragstart" -> typeof<UIDragEventArgs>
        | "ondrop" -> typeof<UIDragEventArgs>
        | "onkeydown" -> typeof<UIKeyboardEventArgs>
        | "onkeyup" -> typeof<UIKeyboardEventArgs>
        | "onkeypress" -> typeof<UIKeyboardEventArgs>
        | "onchange" -> typeof<UIChangeEventArgs>
        | "oncopy" -> typeof<UIClipboardEventArgs>
        | "oncut" -> typeof<UIClipboardEventArgs>
        | "onpaste" -> typeof<UIClipboardEventArgs>
        | "ontouchcancel" -> typeof<UITouchEventArgs>
        | "ontouchend" -> typeof<UITouchEventArgs>
        | "ontouchmove" -> typeof<UITouchEventArgs>
        | "ontouchstart" -> typeof<UITouchEventArgs>
        | "ontouchenter" -> typeof<UITouchEventArgs>
        | "ontouchleave" -> typeof<UITouchEventArgs>
        | "onpointercapture" -> typeof<UIPointerEventArgs>
        | "onlostpointercapture" -> typeof<UIPointerEventArgs>
        | "onpointercancel" -> typeof<UIPointerEventArgs>
        | "onpointerdown" -> typeof<UIPointerEventArgs>
        | "onpointerenter" -> typeof<UIPointerEventArgs>
        | "onpointerleave" -> typeof<UIPointerEventArgs>
        | "onpointermove" -> typeof<UIPointerEventArgs>
        | "onpointerout" -> typeof<UIPointerEventArgs>
        | "onpointerover" -> typeof<UIPointerEventArgs>
        | "onpointerup" -> typeof<UIPointerEventArgs>
        | "onloadstart" -> typeof<UIProgressEventArgs>
        | "ontimeout" -> typeof<UIProgressEventArgs>
        | "onabort" -> typeof<UIProgressEventArgs>
        | "onload" -> typeof<UIProgressEventArgs>
        | "onloadend" -> typeof<UIProgressEventArgs>
        | "onprogress" -> typeof<UIProgressEventArgs>
        | "onerror" -> typeof<UIProgressEventArgs>
// END EVENTS
        | _ -> typeof<UIEventArgs>

type Hole =
    {
        Var: Var
        Type: HoleType
    }

module Hole =

    let Merge (key: string) (hole1: Hole) (hole2: Hole) =
        let ty = HoleType.Merge key hole1.Type hole2.Type
        let var =
            if ty = hole1.Type then hole1.Var
            elif ty = hole2.Type then hole2.Var
            else Var(key, HoleType.TypeOf ty)
        { Var = var; Type = ty }

type Holes = Map<string, Hole>

module Holes =

    let Merge (holes1: Holes) (holes2: Holes) =
        (holes1, holes2) ||> Map.fold (fun map key hole ->
            let hole =
                match Map.tryFind key map with
                | None -> hole
                | Some hole2 -> Hole.Merge key hole hole2
            Map.add key hole map
        )

    let MergeMany (holes: seq<Holes>) =
        Seq.fold Merge Map.empty holes

type Parsed<'T> =
    {
        Holes: Holes
        Expr: Expr<'T>
    }

module Parsed =

    let Concat (p: seq<Parsed<'T>>) : Parsed<'T[]> =
        let finalHoles = Holes.MergeMany [ for p in p -> p.Holes ]
        let exprs = p |> Seq.map (fun p ->
            (p.Expr, p.Holes) ||> Map.fold (fun e k v ->
                // Map var names for holes used multiple times
                match Map.tryFind k finalHoles with
                | Some v' when v'.Var <> v.Var ->
                    match Expr.Var v'.Var |> HoleType.Wrap v.Type v'.Type with
                    | None ->
                        e.Substitute(fun var ->
                            if var = v.Var
                            then Some (Expr.Var v'.Var)
                            else None)
                    | Some value ->
                        Expr.Let(v.Var, value, e)
                    |> Expr.Cast
                | Some _ -> e
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
            Holes = Holes.Merge p1.Holes p2.Holes
            Expr = f p1.Expr p2.Expr
        }

let HoleNameRE = Regex(@"\${(\w+)}", RegexOptions.Compiled)

type TextPart =
    | Plain of string
    | Hole of Hole

let MakeHole holeName holeType =
    let var = Var(holeName, HoleType.TypeOf holeType)
    { Type = holeType; Var = var }

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
            let hole = MakeHole holeName holeType
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

let IsDataBinding (ownerNode: HtmlNode) (attrName: string) =
    attrName = "bind" &&
    List.contains ownerNode.Name [
        "input"
        "textarea"
        "select"
    ]

let MakeEventHandler (attrName: string) (holeName: string) : list<Parsed<Attr>> =
    let argType = HoleType.EventArg attrName
    let hole = MakeHole holeName (HoleType.Event argType)
    let holes = Map [holeName, hole]
    let value = Expr.Coerce(Expr.Var hole.Var, typeof<obj>)
    [{ Holes = holes; Expr = <@ Attr(attrName, %%value) @> }]

let MakeDataBinding holeName : list<Parsed<Attr>> =
    let hole = MakeHole holeName (HoleType.DataBinding)
    let expr1 : Expr<string * Action<string>> = Expr.Var hole.Var |> Expr.Cast
    let expr2 : Expr<string * Action<string>> = Expr.Var hole.Var |> Expr.Cast
    let holes = Map [holeName, hole]
    [
        { Holes = holes; Expr = <@ Attr("onchange", Events.OnChange(snd %expr1)) @> }
        { Holes = holes; Expr = <@ Attr("value", fst %expr2) @> }
    ]

let MakeStringAttribute (attrName: string) holes parts : list<Parsed<Attr>> =
    let value = Expr.TypedArray<string> [
        for part in parts do
            match part with
            | Plain t -> yield <@ t @>
            | Hole h -> yield Expr.Var h.Var |> Expr.Cast
    ]
    [{ Holes = holes; Expr = <@ Attr(attrName, String.concat "" %value) @> }]

let ParseAttribute (ownerNode: HtmlNode) (attr: HtmlAttribute) : list<Parsed<Attr>> =
    let name = attr.Name
    match ParseText attr.Value HoleType.String with
    | holes, [|Hole _|] when name.StartsWith "on" ->
        let (KeyValue(holeName, _)) = Seq.head holes
        MakeEventHandler name holeName
    | holes, [|Hole _|] when IsDataBinding ownerNode name ->
        let (KeyValue(holeName, _)) = Seq.head holes
        MakeDataBinding holeName
    | holes, parts ->
        MakeStringAttribute name holes parts

let EmptyNode : Parsed<Node> =
    { Holes = Map.empty; Expr = <@ Node.Empty @> }

let rec ParseNode (node: HtmlNode) : Parsed<Node> =
    match node.NodeType with
    | HtmlNodeType.Element ->
        let name = node.Name
        let attrs =
            node.Attributes
            |> Seq.collect (ParseAttribute node)
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
