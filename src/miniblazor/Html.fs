module rec MiniBlazor.Html

open System.Collections.Generic

type Node<'Message> =
    | Empty
    | Concat of list<Node<'Message>>
    | Elt of name: string * attrs: IDictionary<string, string> * events: IDictionary<string, obj -> 'Message> * children: list<Node<'Message>>
    | Text of text: string
    | KeyedFragment of list<string * Node<'Message>>

    static member Collect (nodes: list<Node<'Message>>) =
        nodes |> List.collect (function
            | Empty -> []
            | Concat l -> l
            | x -> [x]
        )

    static member Collect (node: Node<'Message>) =
        match node with
        | Empty -> []
        | Concat l -> Node.Collect l
        | x -> [x]

[<Struct>]
type Attr<'Message> =
    | PlainAttr of aname: string * avalue: string
    | Handler of ename: string * ehandler: (obj -> 'Message)

let Element name attrsAndEvents (children: list<_>) =
    let attrs = Dictionary<string, string>()
    let events = Dictionary<string, obj -> 'Message>()
    for a in attrsAndEvents do
        match a with
        | PlainAttr(name, value) -> attrs.[name] <- value
        | Handler(name, handler) -> events.[name] <- handler
    Elt(name, attrs, events, children)

let text str = Text str
let [<GeneralizableValue>] empty<'Message> = Empty : Node<'Message>
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

type onLazy =
    static member event<'Message> event (message: obj -> 'Message) =
        Handler(event, message)

    static member click<'Message> (message: obj -> 'Message) =
        onLazy.event "click" (fun _ -> message())

type on =
    static member event<'Message> event (message: 'Message) =
        onLazy.event event (fun _ -> message)

    static member change<'T, 'Message> (message: 'T -> 'Message) =
        onLazy.event<'Message> "change" (fun args -> message (args :?> 'T))

    static member input<'T, 'Message> (message: 'T -> 'Message) =
        onLazy.event<'Message> "input" (fun args -> message (args :?> 'T))

    static member click<'Message> (message: 'Message) =
        on.event "click" message
