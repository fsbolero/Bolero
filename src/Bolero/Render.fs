module Bolero.Render

open Microsoft.AspNetCore.Blazor
open Microsoft.AspNetCore.Blazor.RenderTree

/// Render `node` into `builder` at `sequence` number.
let rec renderNode (builder: RenderTreeBuilder) sequence node =
    match node with
    | Empty -> sequence
    | Text text ->
        builder.AddContent(sequence, text)
        sequence + 1
    | RawHtml html ->
        builder.AddMarkupContent(sequence, html)
        sequence + 1
    | Concat nodes ->
        List.fold (renderNode builder) sequence nodes
    | Cond (cond, node) ->
        builder.AddContent(sequence + (if cond then 1 else 0),
            RenderFragment(fun tb -> renderNode tb 0 node |> ignore))
        sequence + 2
    | Elt (name, attrs, children) ->
        builder.OpenElement(sequence, name)
        let sequence = sequence + 1
        let sequence = Seq.fold (renderAttr builder) sequence attrs
        let sequence = List.fold (renderNode builder) sequence children
        builder.CloseElement()
        sequence
    | Component (comp, i, attrs, children) ->
        let initSequence = sequence
        builder.OpenComponent(sequence, comp)
        let sequence = sequence + 1
        let sequence = Seq.fold (renderAttr builder) sequence attrs
        if not (List.isEmpty children) then
            let frag = RenderFragment(fun builder ->
                let sequence = sequence + 1
                let sequence = Seq.fold (renderNode builder) sequence children
                assert (sequence = initSequence + i.length))
            builder.AddAttribute(sequence, "ChildContent", frag)
        builder.CloseComponent()
        initSequence + i.length

/// Render an attribute with `name` and `value` into `builder` at `sequence` number.
and renderAttr builder sequence (name, value) =
    builder.AddAttribute(sequence, name, value)
    sequence + 1
