module MiniBlazor.Render

open MiniBlazor.Html
open Microsoft.AspNetCore.Blazor
open Microsoft.AspNetCore.Blazor.RenderTree

let rec renderNode (builder: RenderTreeBuilder) sequence node =
    match node with
    | Empty -> sequence
    | Text text ->
        builder.AddContent(sequence, text)
        sequence + 1
    | Concat nodes ->
        List.fold (renderNode builder) sequence nodes
    | Elt (name, attrs, children) ->
        builder.OpenElement(sequence, name)
        let sequence = sequence + 1
        let sequence = Seq.fold (renderAttr builder) sequence attrs
        let sequence = List.fold (renderNode builder) sequence children
        builder.CloseElement()
        sequence
    | Component (comp, attrs, children) ->
        builder.OpenComponent(sequence, comp)
        let sequence = sequence + 1
        let sequence = Seq.fold (renderAttr builder) sequence attrs
        let frag = RenderFragment(fun builder ->
            Seq.fold (renderNode builder) sequence children |> ignore)
        builder.AddAttribute(sequence, "ChildContent", frag)
        builder.CloseComponent()
        sequence + List.sumBy nodeLength children

and renderAttr (builder: RenderTreeBuilder) sequence (name, value) =
    builder.AddAttribute(sequence, name, value)
    sequence + 1

and nodeLength = function
    | Empty -> 0
    | Text _ -> 1
    | Concat nodes -> List.sumBy nodeLength nodes
    | Elt (_, attrs, children)
    | Component (_, attrs, children) ->
        1 + Seq.length attrs + List.sumBy nodeLength children
