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

module Bolero.Tests.Remoting.Client

open System.Collections.Generic
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Bolero.Templating.Client
open Elmish

type MyApi =
    {
        getItems : unit -> Async<Map<int, string>>
        setItem : (int * string) -> Async<unit>
        removeItem : int -> Async<unit>
        login : string -> Async<unit>
        logout : unit -> Async<unit>
        getLogin : unit -> Async<string>
    }

    interface IRemoteService with
        member this.BasePath = "/myapi"

type Model =
    {
        currentKey: int
        currentValue: string
        items : Map<int, string>
        lastError : option<exn>
        loginInput : string
        currentLogin : option<string>
    }

let InitModel =
    {
        currentKey = 0
        currentValue = ""
        items = Map.empty
        lastError = None
        loginInput = ""
        currentLogin = None
    }

type Message =
    | SetCurrentKey of int
    | SetCurrentValue of string
    | RefreshItems
    | AddItem
    | RemoveItem of int
    | ItemsRefreshed of Map<int, string>
    | Error of exn
    | GetLogin
    | SetLoginInput of string
    | Login
    | Logout
    | GotLogin of option<string>

let Update (myApi: MyApi) msg model =
    match msg with
    | SetCurrentKey i ->
        { model with currentKey = i }, []
    | SetCurrentValue v ->
        { model with currentValue = v }, []
    | RefreshItems ->
        model,
        Cmd.ofAsync myApi.getItems () ItemsRefreshed Error
    | AddItem ->
        model,
        Cmd.ofAsync myApi.setItem (model.currentKey, model.currentValue)
            (fun () -> RefreshItems) Error
    | RemoveItem k ->
        model,
        Cmd.ofAsync myApi.removeItem k
            (fun () -> RefreshItems) Error
    | ItemsRefreshed items ->
        { model with items = items; lastError = None }, []
    | Error exn ->
        { model with lastError = Some exn }, []
    | GetLogin ->
        model, Cmd.ofAsync myApi.getLogin () (Some >> GotLogin) Error
    | SetLoginInput s ->
        { model with loginInput = s }, []
    | Login ->
        model, Cmd.ofAsync myApi.login model.loginInput (fun _ -> GetLogin) Error
    | Logout ->
        model, Cmd.ofAsync myApi.logout () (fun () -> GotLogin None) Error
    | GotLogin s ->
        { model with currentLogin = s }, []

type Tpl = Template<"main.html">
type Form = Template<"subdir/form.html">

type Item() =
    inherit ElmishComponent<KeyValuePair<int, string>, Message>()

    override __.View (KeyValue (k, v)) dispatch =
        Tpl.item().key(string k).value(v)
            .remove(fun _ -> dispatch (RemoveItem k))
            .Elt()

let Display model dispatch =
    let form =
        Form()
            .key(string model.currentKey)
            .setKey(fun e -> dispatch (SetCurrentKey (int (e.Value :?> string))))
            .value(string model.currentValue)
            .setValue(fun e -> dispatch (SetCurrentValue (e.Value :?> string)))
            .add(fun _ -> dispatch AddItem)
            .Elt()
    concat [
        Tpl()
            .form(form)
            .refresh(fun _ -> dispatch RefreshItems)
            .items(forEach model.items <| fun item -> ecomp<Item, _, _> item dispatch)
            .error(
                cond model.lastError <| function
                | None -> empty
                | Some exn -> text (string exn)
            )
            .Elt()
        hr []
        cond model.currentLogin <| function
        | None ->
            concat [
                input [bind.change model.loginInput (dispatch << SetLoginInput)]
                button [on.click (fun _ -> dispatch Login)] [text "Log in"]
            ]
        | Some login ->
            concat [
                textf "Logged in as %s" login
                button [on.click (fun _ -> dispatch Logout)] [text "Log out"]
            ]
    ]

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let myApi = this.Remote<MyApi>()
        Program.mkProgram (fun _ -> InitModel, Cmd.batch [
            Cmd.ofMsg RefreshItems
            Cmd.ofMsg GetLogin
        ]) (Update myApi) Display
        |> Program.withConsoleTrace
        |> Program.withHotReloading



open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Blazor.Builder
open Microsoft.AspNetCore.Blazor.Hosting

type Startup() =

    member __.ConfigureServices(services: IServiceCollection) =
        services.AddRemoting()
        |> ignore

    member __.Configure(app: IBlazorApplicationBuilder) =
        app.AddComponent<MyApp>("#main")

module Program =
    [<EntryPoint>]
    let Main args =
        BlazorWebAssemblyHost.CreateDefaultBuilder()
            .UseBlazorStartup<Startup>()
            .Build()
            .Run()
        0
