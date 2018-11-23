module Bolero.Tests.Web.App.Remoting

open Bolero
open Bolero.Html
open Bolero.Remoting
open Elmish

type RemoteApi =
    {
        getValue : string -> Async<option<string>>
        setValue : string * string -> Async<unit>
        removeValue : string -> Async<unit>
    }

    interface IRemoteService with
        member this.BasePath = "/remote-api"

type Model =
    {
        key : string
        value : string
        received : option<string>
    }

let initModel =
    {
        key = ""
        value = ""
        received = None
    }

type Message =
    | SetKey of string
    | SetValue of string
    | Received of option<string>
    | Add
    | Remove
    | Get of string
    | Error of exn

let update api msg model =
    match msg with
    | SetKey x -> { model with key = x }, []
    | SetValue x -> { model with value = x }, []
    | Received x -> { model with received = x }, []
    | Add -> model, Cmd.ofAsync api.setValue (model.key, model.value) (fun () -> Get model.key) Error
    | Remove -> model, Cmd.ofAsync api.removeValue model.key (fun () -> Get model.key) Error
    | Get k -> model, Cmd.ofAsync api.getValue k Received Error
    | Error exn -> model, []

let view model dispatch =
    div [] [
        input [
            attr.classes ["key-input"]
            attr.value model.key
            on.input (fun e -> dispatch (SetKey (e.Value :?> string)))
        ]
        input [
            attr.classes ["value-input"]
            attr.value model.value
            on.input (fun e -> dispatch (SetValue (e.Value :?> string)))
        ]
        button [attr.classes ["add-btn"]; on.click (fun _ -> dispatch Add)] [text "Add"]
        button [attr.classes ["rem-btn"]; on.click (fun _ -> dispatch Remove)] [text "Remove"]
        cond model.received <| function
            | None -> div [attr.classes ["output-empty"]] []
            | Some v -> div [attr.classes ["output"]] [text v]
    ]

type Test() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let api = this.Remote<RemoteApi>()
        Program.mkProgram (fun _ -> initModel, []) (update api) view

let Tests() =
    div [attr.id "test-fixture-remoting"] [
        comp<Test> [] []
    ]
