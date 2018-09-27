module MiniBlazor.Test.Client.Main

open MiniBlazor
open MiniBlazor.Html

type Model =
    { input: string
      submitted: option<string> }

type Message =
    | SetInput of text: string
    | Submit

let InitModel = { input = ""; submitted = None }

let Update message model =
    match message with
    | SetInput text -> { model with input = text }
    | Submit -> { model with submitted = Some model.input }

let Render model : Node<Message> =
    concat [
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

[<EntryPoint>]
let Main args =
    App.Create InitModel Update Render
    |> App.Run "#main"
    0
