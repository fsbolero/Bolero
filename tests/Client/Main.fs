module MiniBlazor.Test.Client.Main

open MiniBlazor
open MiniBlazor.Html

type Item =
    {
        K: int
        V: string
    }

type Model =
    { 
        input: string
        submitted: option<string>
        addKey: int
        revOrder: bool
        items: Map<int, string>
    }

type Message =
    | SetInput of text: string
    | Submit
    | RemoveItem of key: int
    | SetAddKey of key: int
    | SetKeyOf of key: int
    | AddKey
    | ToggleRevOrder

let InitModel =
    {
        input = ""
        submitted = None
        addKey = 4
        revOrder = false
        items = Map [
            0, "it's 0"
            1, "it's 1"
            2, "it's 2"
            3, "it's 3"
        ]
    }

let Update message model =
    match message with
    | SetInput text -> { model with input = text }
    | Submit -> { model with submitted = Some model.input }
    | RemoveItem k -> { model with items = Map.filter (fun k' _ -> k' <> k) model.items }
    | SetAddKey i -> { model with addKey = i }
    | AddKey -> { model with items = Map.add model.addKey (sprintf "it's %i" model.addKey) model.items }
    | SetKeyOf k ->
        match Map.tryFind k model.items with
        | None -> model
        | Some item ->
            let items = model.items |> Map.remove k |> Map.add model.addKey item
            { model with items = items }
    | ToggleRevOrder -> { model with revOrder = not model.revOrder }

let ViewInput model dispatch =
    div [] [
        input [value model.input; on.input (SetInput >> dispatch)]
        input [type_ "submit"; on.click (fun () -> dispatch Submit)]
        div [] [text (defaultArg model.submitted "")]
        (match model.submitted with
        | Some s ->
            concat [
                if s.Contains "secret" then
                    yield div [] [text "You typed the secret password!"]
                if s.Contains "super" then
                    yield div [] [text "You typed the super secret password!"]
            ]
        | None -> empty)
    ]

let ViewItem k v dispatch =
    concat [
        li [] [text v]
        li [] [
            input []
            button [on.click (fun () -> dispatch (SetKeyOf k))] [text "Set key from Add field"]
            button [on.click (fun () -> dispatch (RemoveItem k))] [text "Remove"]
        ]
    ]

let ViewList model dispatch =
    let items =
        if model.revOrder then
            Seq.rev model.items
        else
            model.items :> _
    div [] [
        input [value (string model.addKey); on.input (int >> SetAddKey >> dispatch)]
        button [on.click (fun () -> dispatch AddKey)] [text "Add"]
        br []
        button [on.click (fun () -> dispatch ToggleRevOrder)] [text "Toggle order"]
        ul [] [
            keyed [for KeyValue(k, v) in items -> string k, ViewItem k v dispatch]
        ]
    ]

let View model dispatch =
    concat [
        ViewInput model dispatch
        ViewList model dispatch
    ]

let MyApp = App.Create InitModel Update View

[<EntryPoint>]
let Main args =
    App.Run "#main" MyApp
    0
