namespace MiniBlazor

open System

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

and [<Struct>] ComponentInfo = { length: int }
