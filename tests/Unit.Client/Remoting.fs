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

module Bolero.Tests.Client.Remoting

open Microsoft.AspNetCore.Components.Authorization
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Elmish
open Microsoft.AspNetCore.Components

type RemoteApi =
    {
        getValue : string -> Async<option<string>>
        setValue : string * string -> Async<unit>
        removeValue : string -> Async<unit>
        signIn : string -> Async<unit>
        signOut : unit -> Async<unit>
        getUsername : unit -> Async<string>
        getAdmin : unit -> Async<string>
    }

    interface IRemoteService with
        member this.BasePath = "/remote-api"

type Model =
    {
        key : string
        value : string
        received : option<string>
        username : string
        signedInAs : option<string>
        signInError : option<string>
        getAdmin : option<string>
        error: option<exn>
    }

let initModel =
    {
        key = ""
        value = ""
        received = None
        username = ""
        signedInAs = None
        signInError = None
        getAdmin = None
        error = None
    }

type Message =
    | SetKey of string
    | SetValue of string
    | Received of option<string>
    | Add
    | Remove
    | Get of string
    | Error of exn

    | SetUsername of string
    | SendSignIn
    | RecvSignIn
    | SendSignOut
    | RecvSignOut
    | SendGetUsername
    | RecvGetUsername of option<string>
    | SendGetAdmin
    | RecvGetAdmin of option<string>

let update api msg model =
    match msg with
    | SetKey x -> { model with key = x }, []
    | SetValue x -> { model with value = x }, []
    | Received x -> { model with received = x }, []
    | Add -> model, Cmd.ofAsync api.setValue (model.key, model.value) (fun () -> Get model.key) Error
    | Remove -> model, Cmd.ofAsync api.removeValue model.key (fun () -> Get model.key) Error
    | Get k -> model, Cmd.ofAsync api.getValue k Received Error
    | Error exn -> { model with error = Some exn }, []

    | SetUsername x -> { model with username = x }, []
    | SendSignIn -> model, Cmd.ofAsync api.signIn model.username (fun () -> RecvSignIn) Error
    | RecvSignIn -> model, Cmd.ofMsg SendGetUsername
    | SendSignOut -> model, Cmd.ofAsync api.signOut () (fun () -> RecvSignOut) Error
    | RecvSignOut -> { model with signedInAs = None }, []
    | SendGetUsername -> model, Cmd.ofAuthorized api.getUsername () RecvGetUsername Error
    | RecvGetUsername resp -> { model with signedInAs = resp }, []
    | SendGetAdmin -> model, Cmd.ofAuthorized api.getAdmin () RecvGetAdmin Error
    | RecvGetAdmin resp -> { model with getAdmin = resp }, []

let remote model dispatch =
    concat [
        input [
            attr.classes ["signin-input"]
            bind.input.string model.username (dispatch << SetUsername)
        ]
        button [
            attr.classes ["signin-button"]
            on.click (fun _ -> dispatch SendSignIn)
        ] [text "Sign in"]
        button [
            attr.classes ["signout-button"]
            on.click (fun _ -> dispatch SendSignOut)
        ] [text "Sign out"]
        div [attr.classes ["is-signedin"]] [
            text (defaultArg model.signedInAs "<not logged in>")
        ]
        button [
            attr.classes ["get-admin"]
            on.click (fun _ -> dispatch SendGetAdmin)
        ] [text "Get whether I'm admin"]
        div [attr.classes ["is-admin"]] [
            text (defaultArg model.getAdmin "<not admin>")
        ]
    ]

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
        remote model dispatch
        cond model.error <| function
        | None -> empty
        | Some e -> p [] [textf "%A" e]
        comp<AuthorizeView> [
            attr.fragmentWith "Authorized" <| fun (context: AuthenticationState) ->
                printfn "Rendering Authorized"
                div [] [textf "You're authorized! Welcome %s" context.User.Identity.Name]
            attr.fragmentWith "NotAuthorized" <| fun (_: AuthenticationState) ->
                printfn "Rendering NotAuthorized"
                div [] [text "You're not authorized :("]
        ] []
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
