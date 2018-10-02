module rec MiniBlazor.Html

open System.Collections.Generic

type Node =
    | Empty
    | Concat of list<Node>
    | Elt of name: string * attrs: IDictionary<string, string> * events: IDictionary<string, obj -> unit> * children: list<Node>
    | Text of text: string
    | KeyedFragment of list<string * Node>

    static member Collect (nodes: list<Node>) =
        nodes |> List.collect (function
            | Empty -> []
            | Concat l -> l
            | x -> [x]
        )

    static member Collect (node: Node) =
        match node with
        | Empty -> []
        | Concat l -> Node.Collect l
        | x -> [x]

[<Struct>]
type Attr =
    | PlainAttr of aname: string * avalue: string
    | Handler of ename: string * ehandler: (obj -> unit)

let Element name attrsAndEvents (children: list<_>) =
    let attrs = Dictionary<string, string>()
    let events = Dictionary<string, obj -> unit>()
    for a in attrsAndEvents do
        match a with
        | PlainAttr(name, value) -> attrs.[name] <- value
        | Handler(name, handler) -> events.[name] <- handler
    Elt(name, attrs, events, children)

let text str = Text str
let empty = Empty
let concat nodes = Concat nodes
let keyed items = KeyedFragment items

let div attrs children = Element "div" attrs children
let input attrs = Element "input" attrs []
let button attrs children = Element "button" attrs children
let b attrs children = Element "b" attrs children
let br attrs = Element "br" attrs []
let i attrs children = Element "i" attrs children
let ul attrs children = Element "ul" attrs children
let li attrs children = Element "li" attrs children
let p attrs children = Element "p" attrs children

let (=>) name value = PlainAttr(name, value)
let value x = "value" => x
let type_ x = "type" => x

type on =
    static member event<'T> event (callback: 'T -> unit) =
        Handler(event, unbox<'T> >> callback)

    static member change<'T> (callback: 'T -> unit) =
        on.event "change" callback

    static member input (message: string -> unit) =
        on.event "input" message

    static member click (message: unit -> unit) =
        on.event "click" (fun _ -> message ())
