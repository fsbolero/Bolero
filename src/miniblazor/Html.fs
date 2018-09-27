module rec MiniBlazor.Html

open System.Collections.Generic
open System.Text
open System.Net
open Microsoft.JSInterop.Internal

type Node<'Message> =
    | Empty
    | Concat of list<Node<'Message>>
    | Elt of name: string * attrs: IDictionary<string, string> * events: IDictionary<string, obj -> 'Message> * children: list<Node<'Message>>
    | Text of text: string

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

let div attrs children = Element "div" attrs children
let input attrs = Element "input" attrs []
let b attrs children = Element "b" attrs children
let i attrs children = Element "i" attrs children

let (=>) name value = PlainAttr(name, value)
let value x = "value" => x
let type_ x = "type" => x

let onLazy event handler = Handler(event, handler)
let on event message = Handler(event, fun _ -> message)
let onChange message = onLazy "change" (fun args -> message (args :?> string))
let onInput message = onLazy "input" (fun args -> message (args :?> string))
let onClick message = on "click" message
