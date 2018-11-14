module Bolero.Render

open System
open System.Collections.Generic
open FSharp.Reflection
open Microsoft.AspNetCore.Blazor
open Microsoft.AspNetCore.Blazor.RenderTree

/// Render `node` into `builder` at `sequence` number.
let rec renderNode (builder: RenderTreeBuilder) (matchCache: Type -> int * (obj -> int)) sequence node =
    match node with
    | Empty -> sequence
    | Text text ->
        builder.AddContent(sequence, text)
        sequence + 1
    | RawHtml html ->
        builder.AddMarkupContent(sequence, html)
        sequence + 1
    | Concat nodes ->
        List.fold (renderNode builder matchCache) sequence nodes
    | Cond (cond, node) ->
        builder.AddContent(sequence + (if cond then 1 else 0),
            RenderFragment(fun tb -> renderNode tb matchCache 0 node |> ignore))
        sequence + 2
    | Match (unionType, value, node) ->
        let caseCount, getMatchedCase = matchCache unionType
        let matchedCase = getMatchedCase value
        builder.AddContent(sequence + matchedCase,
            RenderFragment(fun tb -> renderNode tb matchCache 0 node |> ignore))
        sequence + caseCount
    | Elt (name, attrs, children) ->
        builder.OpenElement(sequence, name)
        let sequence = sequence + 1
        let sequence = Seq.fold (renderAttr builder) sequence attrs
        let sequence = List.fold (renderNode builder matchCache) sequence children
        builder.CloseElement()
        sequence
    | Component (comp, attrs, children) ->
        builder.OpenComponent(sequence, comp)
        let sequence = sequence + 1
        let sequence = Seq.fold (renderAttr builder) sequence attrs
        let hasChildren = not (List.isEmpty children)
        if hasChildren then
            let frag = RenderFragment(fun builder ->
                builder.AddContent(sequence + 1, RenderFragment(fun builder ->
                    Seq.fold (renderNode builder matchCache) 0 children
                    |> ignore)))
            builder.AddAttribute(sequence, "ChildContent", frag)
        builder.CloseComponent()
        sequence + (if hasChildren then 2 else 0)

/// Render an attribute with `name` and `value` into `builder` at `sequence` number.
and renderAttr builder sequence (name, value) =
    builder.AddAttribute(sequence, name, value)
    sequence + 1

let RenderNode builder (matchCache: Dictionary<Type, _>) node =
    let getMatchParams (ty: Type) =
        match matchCache.TryGetValue(ty) with
        | true, x -> x
        | false, _ ->
            let caseCount = FSharpType.GetUnionCases(ty, true).Length
            let r = FSharpValue.PreComputeUnionTagReader(ty)
            let v = (caseCount, r)
            matchCache.[ty] <- v
            v
    renderNode builder getMatchParams 0 node
    |> ignore
