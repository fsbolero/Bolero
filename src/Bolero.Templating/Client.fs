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
open System.Collections.Generic
open Microsoft.Extensions.DependencyInjection
open Microsoft.JSInterop
open Microsoft.AspNetCore.Blazor.Services
open Bolero
open Bolero.Templating
open Bolero.TemplatingInternals

type WebSocketClient internal (url: string, runtime: IJSInProcessRuntime, rerender: unit -> unit) as this =

    let thisRef = new DotNetObjectRef(this)
    let cache = Dictionary<string, Parsing.Expr>()

    do runtime.Invoke("Bolero.Templating.setup", url, thisRef)

    [<JSInvokable>]
    member this.FileChanged(filename: string, content: string) =
        cache.[filename] <- Parsing.Concat (Parsing.ParseFileOrContent content "").Main.Expr
        rerender()

    interface IClient with

        member this.FileChanged(filename, content) =
            this.FileChanged(filename, content)

        member this.RequestFile(filename) =
            match cache.TryGetValue(filename) with
            | false, _ ->
                runtime.Invoke("Bolero.Templating.requestFile", filename)
                None
            | true, e ->
                Some (fun vars -> ConvertExpr.ConvertNode vars e)

    interface IDisposable with

        member this.Dispose() =
            thisRef.Dispose()

module Program =

    let private registerClient (comp: ProgramComponent<_, _>) =
        match JSRuntime.Current with
        | :? IJSInProcessRuntime as rt ->
            let service = comp.Services.GetService<IHotReloadService>()
            let path = match service with null -> "/bolero-reload" | s -> s.UrlPath
            let baseUri = comp.Services.GetService<IUriHelper>().GetBaseUri()
            let uriBuilder = UriBuilder(baseUri, Scheme = "ws", Path = path)
            new WebSocketClient(uriBuilder.ToString(), rt, comp.Rerender) :> IClient
        | _ ->
            raise (NotImplementedException "Server-side template reloading not implemented yet")

    let withHotReloading (program: Elmish.Program<ProgramComponent<'model, 'msg>, 'model, 'msg, Node>) =
        { program with
            init = fun comp ->
                TemplateCache.client <- registerClient comp
                program.init comp }
