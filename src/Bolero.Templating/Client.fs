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

namespace Bolero.Templating.Client

open System
open System.Collections.Concurrent
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open Microsoft.JSInterop
open Microsoft.AspNetCore.Blazor.Services
open Blazor.Extensions
open Bolero
open Bolero.Templating
open Bolero.TemplatingInternals

[<AbstractClass>]
type ClientBase() =

    let cache = ConcurrentDictionary<string, Parsing.ParsedTemplates>()

    member this.StoreFileContent(filename, content) =
        cache.[filename] <- Parsing.ParseFileOrContent content ""

    member this.RefreshAllFiles() =
        cache.Keys
        |> Seq.map this.RequestFile
        |> Async.Parallel
        |> Async.Ignore

    abstract RequestFile : string -> Async<unit>

    abstract SetOnChange : (unit -> unit) -> unit

    interface IClient with

        member this.RequestTemplate(filename, subtemplate) =
            match cache.TryGetValue(filename) with
            | false, _ ->
                this.RequestFile filename |> Async.Start
                None
            | true, tpl ->
                Some (fun vars ->
                    let tpl =
                        match subtemplate with
                        | null -> tpl.Main
                        | sub -> tpl.Nested.[sub]
                    let expr = Parsing.Concat tpl.Expr
                    ConvertExpr.ConvertNode vars expr)

        member this.SetOnChange(callback) =
            this.SetOnChange(callback)

        member this.FileChanged(filename, content) =
            this.StoreFileContent(filename, content)

type SignalRClient(settings: HotReloadSettings) as this =
    inherit ClientBase()

    let hub =
        HubConnectionBuilder()
            .WithUrl(settings.Url, fun opt ->
                opt.Transport <- HttpTransportType.WebSockets
                opt.SkipNegotiation <- true
                opt.LogLevel <- settings.LogLevel)
            .Build()

    let mutable rerender = ignore

    let setupHandlers() =
        hub.On("FileChanged", fun filename content ->
            this.StoreFileContent(filename, content)
            rerender()
            Task.CompletedTask)
        |> ignore

    let connect = async {
        let mutable connected = false
        while not connected do
            try
                do! hub.StartAsync() |> Async.AwaitTask
                connected <- true
            with _ ->
                do! Async.Sleep settings.ReconnectDelayInMs
                printfn "Hot reload reconnecting..."
        printfn "Connected!"
        do! this.RefreshAllFiles()
        rerender()
    }

    do  hub.OnClose(fun _ ->
            printfn "Hot reload disconnected!"
            connect |> Async.StartAsTask :> Task)
        setupHandlers()
        connect |> Async.Start

    override this.RequestFile(filename) =
        hub.InvokeAsync<string>("RequestFile", filename).ContinueWith(fun (t: Task<_>) ->
            if t.IsCompleted then this.StoreFileContent(filename, t.Result)
            elif t.IsFaulted then printfn "Hot reload failed to request file: %A" t.Exception
        )
        |> Async.AwaitTask

    override this.SetOnChange(callback) =
        rerender <- callback

module Program =

    let private registerClient (comp: ProgramComponent<_, _>) =
        match JSRuntime.Current with
        | :? IJSInProcessRuntime ->
            let settings =
                let s = comp.Services.GetService<HotReloadSettings>()
                if obj.ReferenceEquals(s, null) then HotReloadSettings.Default else s
            let baseUri = comp.Services.GetService<IUriHelper>().GetBaseUri()
            let url = UriBuilder(baseUri, Scheme = "ws", Path = settings.Url).ToString()
            let settings = { settings with Url = url }
            let client = new SignalRClient(settings)
            TemplateCache.client <- client
            client :> IClient
        | _ ->
            failwith "To use hot reload on the server side, call AddHotReload() in the ASP.NET Core services"

    let withHotReloading (program: Elmish.Program<ProgramComponent<'model, 'msg>, 'model, 'msg, Node>) =
        { program with
            init = fun comp ->
                let client =
                    match comp.Services.GetService<IClient>() with
                    | null -> registerClient comp
                    | client -> client
                client.SetOnChange(comp.Rerender)
                program.init comp }
