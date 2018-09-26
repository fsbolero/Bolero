module MiniBlazor.Main

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
    div [] [
        yield input [value model.input; onInput SetInput]
        yield input [type_ "submit"; onClick Submit]
        yield div [] [text (defaultArg model.submitted "")]
        match model.submitted with
        | Some s ->
            if s.Contains "secret" then
                yield div [] [text "You typed the secret password!"]
            if s.Contains "super" then
                yield div [] [text "You typed the super secret password!"]
        | _ -> ()
    ]

[<EntryPoint>]
let Main args =
    App.Create InitModel Update Render
    |> App.Run "#main"
    0
