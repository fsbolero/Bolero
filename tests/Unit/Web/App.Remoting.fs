// $begin{copyright}
//
// This file is part of Bolero
//
// Copyright (c) 2018 IntelliFactory and contributors
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License.  You may
// obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied.  See the License for the specific language governing
// permissions and limitations under the License.
//
// $end{copyright}

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
