module MiniBlazor.Test.Client.Main

open MiniBlazor
open MiniBlazor.Html

type Item =
    {
        K: int
        V: string
    }

type Model =
    { input: string
      submitted: option<string>
      addKey: int
      items: Map<int, string> }

type Message =
    | SetInput of text: string
    | Submit
    | RemoveItem of key: int
    | SetAddKey of key: int
    | AddKey

let InitModel =
    { input = ""
      submitted = None
      addKey = 4
      items = Map [
        0, "it's 0"
        1, "it's 1"
        2, "it's 2"
        3, "it's 3"
      ] }

let Update message model =
    match message with
    | SetInput text -> { model with input = text }
    | Submit -> { model with submitted = Some model.input }
    | RemoveItem k -> { model with items = Map.filter (fun k' _ -> k' <> k) model.items }
    | SetAddKey i -> { model with addKey = i }
    | AddKey -> { model with items = Map.add model.addKey (sprintf "it's %i" model.addKey) model.items }

let View model : Node<Message> =
    concat [
        p [] [
            input [value model.input; onInput SetInput]
            input [type_ "submit"; onClick Submit]
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
        p [] [
            input [value (string model.addKey); onInput (int >> SetAddKey)]
            button [onClick AddKey] [text "Add"]
            ul [] [
                keyed [
                    for KeyValue(k, v) in model.items ->
                        string k,
                        concat [
                            li [] [text v]
                            li [] [
                                input []
                                button [onClick (RemoveItem k)] [text "Remove"]
                            ]
                        ]
                ]
            ]
        ]
    ]

[<EntryPoint>]
let Main args =
    App.Create InitModel Update View
    |> App.Run "#main"
    0
