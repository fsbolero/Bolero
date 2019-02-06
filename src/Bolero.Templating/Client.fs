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

type SignalRClient internal (settings: HotReloadSettings, rerender: unit -> unit) =

    let cache = ConcurrentDictionary<string, Parsing.ParsedTemplates>()

    let hub =
        HubConnectionBuilder()
            .WithUrl(settings.Url, fun opt ->
                opt.Transport <- HttpTransportType.WebSockets
                opt.SkipNegotiation <- true
                opt.LogLevel <- settings.LogLevel)
            .Build()

    let storeFileContent filename content =
        cache.[filename] <- Parsing.ParseFileOrContent content ""

    let setupHandlers() =
        hub.On("FileChanged", fun filename content ->
            storeFileContent filename content
            rerender()
            Task.CompletedTask)
        |> ignore

    let requestFile (filename: string) =
        hub.InvokeAsync<string>("RequestFile", filename).ContinueWith(fun (t: Task<_>) ->
            if t.IsCompleted then storeFileContent filename t.Result
            elif t.IsFaulted then printfn "Hot reload failed to request file: %A" t.Exception
        )

    let connect = async {
        let mutable connected = false
        while not connected do
            try
                do! hub.StartAsync() |> Async.AwaitTask
                connected <- true
            with _ ->
                do! Async.Sleep settings.ReconnectDelay
                printfn "Hot reload reconnecting..."
        printfn "Connected!"
        for KeyValue(filename, _) in cache do
            do! requestFile filename |> Async.AwaitTask
        rerender()
    }

    do  hub.OnClose(fun _ ->
            printfn "Hot reload disconnected!"
            connect |> Async.StartAsTask :> Task)
        setupHandlers()
        connect |> Async.Start

    interface IClient with

        member this.RequestTemplate(filename, subtemplate) =
            match cache.TryGetValue(filename) with
            | false, _ ->
                requestFile filename |> ignore
                None
            | true, tpl ->
                Some (fun vars ->
                    let tpl =
                        match subtemplate with
                        | null -> tpl.Main
                        | sub -> tpl.Nested.[sub]
                    let expr = Parsing.Concat tpl.Expr
                    ConvertExpr.ConvertNode vars expr)

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
            new SignalRClient(settings, comp.Rerender) :> IClient
        | _ ->
            raise (NotImplementedException "Server-side template reloading not implemented yet")

    let withHotReloading (program: Elmish.Program<ProgramComponent<'model, 'msg>, 'model, 'msg, Node>) =
        { program with
            init = fun comp ->
                TemplateCache.client <- registerClient comp
                program.init comp }
