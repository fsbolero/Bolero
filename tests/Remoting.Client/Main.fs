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
open Microsoft.AspNetCore.Blazor.Components
open Bolero
open Bolero.Html
open Bolero.Remoting
open Elmish
open Bolero.Templating.Client

type MyApi =
    {
        getItems : unit -> Async<Map<int, string>>
        setItem : (int * string) -> Async<unit>
        removeItem : int -> Async<unit>
    }

    interface IRemoteService with
        member this.BasePath = "/myapi"

type Model =
    {
        currentKey: int
        currentValue: string
        items : Map<int, string>
        lastError : option<exn>
    }

let InitModel =
    {
        currentKey = 0
        currentValue = ""
        items = Map.empty
        lastError = None
    }

type Message =
    | SetCurrentKey of int
    | SetCurrentValue of string
    | RefreshItems
    | AddItem
    | RemoveItem of int
    | ItemsRefreshed of Map<int, string>
    | Error of exn

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

type Item() =
    inherit ElmishComponent<KeyValuePair<int, string>, Message>()

    override __.View (KeyValue (k, v)) dispatch =
        li [] [
            textf "%i => %s" k v
            button [on.click (fun _ -> dispatch (RemoveItem k))] [text "Remove"]
        ]

type Tpl = Template<"main.html">

let Display model dispatch =
    Tpl()
        .key(string model.currentKey)
        .setKey(fun e -> dispatch (SetCurrentKey (int (e.Value :?> string))))
        .value(string model.currentValue)
        .setValue(fun e -> dispatch (SetCurrentValue (e.Value :?> string)))
        .add(fun _ -> dispatch AddItem)
        .refresh(fun _ -> dispatch RefreshItems)
        .items(concat [for item in model.items -> ecomp<Item, _, _> item dispatch])
        .error(
            match model.lastError with
            | None -> empty
            | Some exn -> text (string exn)
        )
        .Elt()

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let myApi = this.Remote<MyApi>()
        Program.mkProgram (fun _ -> InitModel, Cmd.ofMsg RefreshItems) (Update myApi) Display
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
