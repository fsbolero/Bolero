namespace MiniBlazor

open System
open Microsoft.AspNetCore.Blazor.Components

/// HTML attribute or Blazor component parameter.
type Attr = string * obj

/// HTML fragment.
type Node =
    /// An empty HTML fragment.
    | Empty
    /// A concatenation of several HTML fragments.
    | Concat of list<Node>
    /// A single HTML element.
    | Elt of name: string * attrs: list<Attr> * children: list<Node>
    /// A single HTML text node.
    | Text of text: string
    /// A single Blazor component.
    | Component of Type * info: ComponentInfo * attrs: list<Attr> * children: list<Node>

    static member BlazorComponent<'T when 'T :> IComponent>(attrs, children) =
        let rec nodeLength = function
            | Empty -> 0
            | Text _ -> 1
            | Concat nodes -> List.sumBy nodeLength nodes
            | Elt (_, attrs, children) ->
                1 + List.length attrs + List.sumBy nodeLength children
            | Component (_, i, _, _) -> i.length
        let length = 1 + List.length attrs + List.sumBy nodeLength children
        Node.Component(typeof<'T>, { length = length }, attrs, children)

and [<Struct>] ComponentInfo = { length: int }
