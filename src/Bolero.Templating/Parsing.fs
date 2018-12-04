module Bolero.Templating.Parsing

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open FSharp.Quotations
open FSharp.Reflection
open Microsoft.AspNetCore.Blazor
open Microsoft.AspNetCore.Blazor.Components
open HtmlAgilityPack
open Bolero
open Bolero.TemplatingInternals
open ProviderImplementation.ProvidedTypes

type BindingType =
    | String
    | Number
    | Bool

type HoleType =
    | String
    | Html
    | Event of argType: Type
    | DataBinding of BindingType
    | Attribute

module Binding =

    let ToString valType expr : Expr<string> =
        if valType = typeof<string> then
            Expr.Cast expr
        elif valType = typeof<bool> then
            <@ string (%%expr: bool) @>
        elif valType = typeof<int> then
            <@ string (%%expr: int) @>
        elif valType = typeof<float> then
            <@ string (%%expr: float) @>
        else
            failwithf "Unknown binding value type %s" valType.FullName

module HoleType =

    let Merge name t1 t2 =
        if t1 = t2 then t1 else
        match t1, t2 with
        | String, Html | Html, String -> String
        | Event _, Event _ -> Event typeof<UIEventArgs>
        | DataBinding valType, String | String, DataBinding valType
        | DataBinding valType, Html | Html, DataBinding valType -> DataBinding valType
        | _ -> failwithf "Hole name used multiple times with incompatible types: %s" name

    let EventHandlerOf argType =
        ProvidedTypeBuilder.MakeGenericType(typedefof<Action<_>>, [argType])

    let TypeOf = function
        | String -> typeof<string>
        | Html -> typeof<Node>
        | Event argType -> EventHandlerOf argType
        | DataBinding _ -> typeof<obj * Action<UIChangeEventArgs>>
        | Attribute -> typeof<Attr>

    let Wrap (innerType: HoleType) (outerType: HoleType) (expr: Expr) =
        if innerType = outerType then None else
        match innerType, outerType with
        | Html, String -> Some <@@ Node.Text %%expr @@>
        | Event argTy, Event _ -> Some <| Expr.Coerce(expr, EventHandlerOf argTy)
        | String, DataBinding _ -> Some <@@ fst (%%expr: obj * Action<UIChangeEventArgs>) @@>
        | Html, DataBinding _ -> Some <@@ Node.Text (string (fst (%%expr: obj * Action<UIChangeEventArgs>))) @@>
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
        | "oninput" -> typeof<UIChangeEventArgs>
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
            Expr = TExpr.Array<'T> exprs
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

module TextPart =

    let ToStringExpr = function
        | Plain s -> <@ s @>
        | Hole h -> Expr.Var h.Var |> Binding.ToString h.Var.Type

    let ManyToStringExpr = function
        | [||] -> <@ "" @>
        | [|p|] -> ToStringExpr p
        | [|p1;p2|] -> <@ %(ToStringExpr p1) + %(ToStringExpr p2) @>
        | parts ->
            let arr = TExpr.Array<string> (Seq.map ToStringExpr parts)
            <@ String.concat "" %arr @>

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

/// None if this is not a data binding.
/// Some None if this is a data binding without specified event.
/// Some (Some "onxyz") if this is a data binding with a specified event.
let GetDataBindingEvent = function
    | "bind-oninput" -> Some (Some "oninput")
    | "bind-onchange" -> Some (Some "onchange")
    | "bind" -> Some None
    | _ -> None


let GetDataBindingType (ownerNode: HtmlNode) (attrName: string) =
    match GetDataBindingEvent attrName with
    | None -> None
    | Some ev ->
    let nodeName = ownerNode.Name
    if nodeName = "textarea" then
        Some (BindingType.String, defaultArg ev "oninput")
    elif nodeName = "select" then
        Some (BindingType.String, defaultArg ev "onchange")
    elif nodeName = "input" then
        match ownerNode.GetAttributeValue("type", "text") with
        | "number" -> Some (BindingType.Number, defaultArg ev "oninput")
        | "checkbox" -> Some (BindingType.Bool, defaultArg ev "onchange")
        | _ -> Some (BindingType.String, defaultArg ev "oninput")
    else None

let MakeEventHandler (attrName: string) (holeName: string) : list<Parsed<Attr>> =
    let argType = HoleType.EventArg attrName
    let hole = MakeHole holeName (HoleType.Event argType)
    let holes = Map [holeName, hole]
    let value = TExpr.Coerce<obj>(Expr.Var hole.Var)
    [{ Holes = holes; Expr = <@ Attr(attrName, %value) @> }]

let MakeAttrAttribute (holeName: string) : list<Parsed<Attr>> =
    let hole = MakeHole holeName HoleType.Attribute
    let holes = Map [holeName, hole]
    let value = TExpr.Coerce<Attr>(Expr.Var hole.Var)
    [{ Holes = holes; Expr = value }]

let MakeDataBinding holeName valType eventName : list<Parsed<Attr>> =
    let hole = MakeHole holeName (HoleType.DataBinding valType)
    let holeVar() : Expr<obj * Action<UIChangeEventArgs>> = Expr.Var hole.Var |> Expr.Cast
    let holes = Map [holeName, hole]
    let valueAttrName =
        match valType with
        | BindingType.Number | BindingType.String -> "value"
        | BindingType.Bool -> "checked"
    [
        { Holes = holes; Expr = <@ Attr(valueAttrName, fst (%holeVar())) @> }
        { Holes = holes; Expr = <@ Attr(eventName, snd (%holeVar())) @> }
    ]

let MakeStringAttribute (attrName: string) holes parts : list<Parsed<Attr>> =
    let value = TextPart.ManyToStringExpr parts
    [{ Holes = holes; Expr = <@ Attr(attrName, %value) @> }]

let ParseAttribute (ownerNode: HtmlNode) (attr: HtmlAttribute) : list<Parsed<Attr>> =
    let name = attr.Name
    match ParseText attr.Value HoleType.String with
    | holes, [|Hole _|] when name.StartsWith "on" ->
        let (KeyValue(holeName, _)) = Seq.head holes
        MakeEventHandler name holeName
    | holes, [|Hole _|] when name = "attr" ->
        let (KeyValue(holeName, _)) = Seq.head holes
        MakeAttrAttribute holeName
    | holes, ([|Hole _|] as parts) ->
        match GetDataBindingType ownerNode name with
        | Some (valType, eventName) ->
            let (KeyValue(holeName, _)) = Seq.head holes
            MakeDataBinding holeName valType eventName
        | None ->
            MakeStringAttribute name holes parts
    | holes, parts ->
        MakeStringAttribute name holes parts

type ParsedNode =
    | PlainHtml of string
    | WithHoles of Parsed<Node>

module ParsedNode =

    /// Convert to seq<Parsed<Node>> while merging consecutive plain HTML nodes
    let ManyToParsed (s: seq<ParsedNode>) : seq<Parsed<Node>> =
        let currentHtml = StringBuilder()
        let res = ResizeArray()
        let pushHtml() =
            let s = currentHtml.ToString()
            if s <> "" then
                res.Add({ Holes = Map.empty; Expr = <@ Node.RawHtml s @> })
            currentHtml.Clear() |> ignore
        for n in s do
            match n with
            | PlainHtml s -> currentHtml.Append(s) |> ignore
            | WithHoles h ->
                pushHtml()
                res.Add(h)
        pushHtml()
        res :> _

    let HasHoles = function
        | PlainHtml _ -> false
        | WithHoles h -> not (Map.isEmpty h.Holes)

let rec ParseNode (node: HtmlNode) : list<ParsedNode> =
    match node.NodeType with
    | HtmlNodeType.Element ->
        let name = node.Name
        let attrs =
            node.Attributes
            |> Seq.collect (ParseAttribute node)
            |> Parsed.Concat
        let children =
            node.ChildNodes
            |> Seq.collect ParseNode
        if Map.isEmpty attrs.Holes && not (Seq.exists ParsedNode.HasHoles children) then
            let rec removeComments (n: HtmlNode) =
                if isNull n then () else
                let nxt = n.NextSibling
                match n.NodeType with
                | HtmlNodeType.Text | HtmlNodeType.Element -> ()
                | _ -> n.Remove()
                removeComments nxt
            [PlainHtml node.OuterHtml]
        else
            let children =
                children
                |> ParsedNode.ManyToParsed
                |> Parsed.Concat
            (attrs, children)
            ||> Parsed.Map2 (fun attrs children ->
                <@ Node.Elt(name, List.ofArray %attrs, List.ofArray %children) @>
            )
            |> WithHoles
            |> List.singleton
    | HtmlNodeType.Text ->
        // Using .InnerHtml and RawHtml to properly interpret HTML entities.
        match ParseText (node :?> HtmlTextNode).InnerHtml HoleType.Html with
        | _, [|Plain t|] -> [PlainHtml t]
        | holes, parts ->
        [
            for part in parts do
                match part with
                | Plain t -> yield PlainHtml t
                | Hole h -> yield WithHoles { Holes = holes; Expr = Expr.Var h.Var |> Expr.Cast }
        ]
    | _ ->
        []

let ParseOneTemplate (nodes: HtmlNodeCollection) =
    nodes
    |> Seq.collect ParseNode
    |> ParsedNode.ManyToParsed
    |> Parsed.Concat
    |> Parsed.Map (fun e -> <@ Node.Concat (List.ofArray %e) @>)

type ParsedTemplates =
    {
        Main: Parsed<Node>
        Nested: Map<string, Parsed<Node>>
    }

let ParseDoc (doc: HtmlDocument) =
    let nested =
        let templateNodes =
            match doc.DocumentNode.SelectNodes("//template") with
            | null -> [||]
            | nodes -> Array.ofSeq nodes
        // Remove before processing so that 2-level nested templates don't appear in their parent
        templateNodes
        |> Seq.iter (fun n -> n.Remove())
        templateNodes
        |> Seq.map (fun n ->
            match n.GetAttributeValue("id", null) with
            | null ->
                failwithf "Nested template must have an id" // at %i:%i" n.Line n.LinePosition
            | id ->
                let parsed = ParseOneTemplate n.ChildNodes
                n.Remove()
                (id, parsed)
        )
        |> Map.ofSeq
    let main = ParseOneTemplate doc.DocumentNode.ChildNodes
    { Main = main; Nested = nested }

let GetDoc (fileOrContent: string) (rootFolder: string) =
    let doc = HtmlDocument()
    if fileOrContent.Contains("<") then
        doc.LoadHtml(fileOrContent)
    else
        doc.Load(Path.Combine(rootFolder, fileOrContent))
    doc

let ParseFileOrContent (fileOrContent: string) (rootFolder: string) =
    GetDoc fileOrContent rootFolder
    |> ParseDoc
