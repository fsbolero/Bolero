namespace MiniBlazor

open System
#if !IS_DESIGNTIME
open Microsoft.AspNetCore.Blazor.Components
#endif

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
    /// A raw HTML fragment.
    | RawHtml of html: string
#if !IS_DESIGNTIME
    /// A single Blazor component.
    | Component of Type * info: ComponentInfo * attrs: list<Attr> * children: list<Node>

    static member BlazorComponent(ty, attrs, children) =
        let rec nodeLength = function
            | Empty -> 0
            | Text _ -> 1
            | RawHtml _ -> 1
            | Concat nodes -> List.sumBy nodeLength nodes
            | Elt (_, attrs, children) ->
                1 + List.length attrs + List.sumBy nodeLength children
            | Component (_, i, _, _) -> i.length
        let length = 1 + List.length attrs + List.sumBy nodeLength children
        Node.Component(ty, { length = length }, attrs, children)

    static member BlazorComponent<'T when 'T :> IComponent>(attrs, children) =
        Node.BlazorComponent(typeof<'T>, attrs, children)
#endif

and [<Struct>] ComponentInfo = { length: int }

type TemplateNode() =
    /// For internal use only.
    member val Holes : obj[] = null with get, set
    