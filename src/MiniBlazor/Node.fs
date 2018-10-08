namespace MiniBlazor

open System

type Node =
    | Empty
    | Concat of list<Node>
    | Elt of name: string * attrs: list<string * obj> * children: list<Node>
    | Text of text: string
    | Component of Type * info: ComponentInfo * attrs: list<string * obj> * children: list<Node>

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

and [<Struct>] ComponentInfo = { length: int }
