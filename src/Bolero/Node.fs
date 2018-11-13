namespace Bolero

open System
#if !IS_DESIGNTIME
open Microsoft.AspNetCore.Blazor.Components
#endif

/// HTML attribute or Blazor component parameter.
type Attr = string * obj

type BlazorEventHandler<'T> = delegate of 'T -> unit

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
    /// A single Blazor component.
    | Component of Type * info: ComponentInfo * attrs: list<Attr> * children: list<Node>
    /// A conditional component.
    | Cond of bool * Node

    static member BlazorComponent(ty, attrs, children) =
        let rec nodeLength = function
            | Empty -> 0
            | Text _ -> 1
            | RawHtml _ -> 1
            | Concat nodes -> List.sumBy nodeLength nodes
            | Elt (_, attrs, children) ->
                1 + List.length attrs + List.sumBy nodeLength children
            | Component (_, i, _, _) -> i.length
            | Cond _ -> 2
        let childrenLength =
            match children with
            | [] -> 0
            | l -> 1 + List.sumBy nodeLength l
        let length = 1 + List.length attrs + childrenLength
        Node.Component(ty, { length = length }, attrs, children)

// The type provider includes this file.
// TPs fail if the TPDTC references an external type in a signature,
// so the following needs to be excluded from the TP.
// See https://github.com/fsprojects/FSharp.TypeProviders.SDK/issues/274
#if !IS_DESIGNTIME
    static member BlazorComponent<'T when 'T :> IComponent>(attrs, children) =
        Node.BlazorComponent(typeof<'T>, attrs, children)
#endif

and [<Struct>] ComponentInfo = { length: int }
