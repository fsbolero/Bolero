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
namespace Bolero.Templating.Server

open System
open System.IO
open System.Threading.Tasks
open System.Runtime.CompilerServices
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Bolero.Templating
open Bolero.TemplatingInternals

[<AutoOpen>]
module Impl =

    let rec asyncRetry (times: int) (job: Async<'T>) : Async<option<'T>> = async {
        try
            let! x = job
            return Some x
        with _ ->
            if times <= 1 then
                return None
            else
                do! Async.Sleep 1000
                return! asyncRetry (times - 1) job
    }

    type WatcherConfig =
        {
            dir: option<string>
        }

    type HotReloadHub(watcher: Watcher) =
        inherit Hub()

        member this.RequestFile(filename: string) : Task<string> =
            async {
                let fullPath = watcher.FullPathOf(filename)
                do! this.Groups.AddToGroupAsync(this.Context.ConnectionId, fullPath) |> Async.AwaitTask
                let! fileContent = watcher.GetFileContent fullPath
                return Option.toObj fileContent
            }
            |> Async.StartAsTask

    and Watcher(config: WatcherConfig, env: IHostingEnvironment, log: ILogger<Watcher>, hub: IHubContext<HotReloadHub>) =
        let dir =
            match config.dir with
            | Some dir -> Path.Combine(env.ContentRootPath, dir)
            | None -> env.ContentRootPath

        let fullPathOf filename =
            Path.Combine(dir, filename)

        let getFileContent fullPath =
            asyncRetry 3 <| async {
                use f = File.OpenText(fullPath)
                return! f.ReadToEndAsync() |> Async.AwaitTask
            }

        let changed = Event<string * string>()

        let onchange (fullPath: string) =
            async {
                let filename = Path.GetFileName(fullPath) // TODO: what if it's in a subdirectory?
                match! getFileContent fullPath with
                | None ->
                    log.LogWarning("Bolero HotReload: failed to reload {0}", fullPath)
                | Some content ->
                    changed.Trigger((filename, content))
                    return! hub.Clients.Group(fullPath)
                        .SendAsync("FileChanged", filename, content)
                        |> Async.AwaitTask
            }

        let callback (args: FileSystemEventArgs) =
            Async.StartImmediate <| onchange args.FullPath

        member this.Changed = changed.Publish

        member this.FullPathOf(filename) =
            fullPathOf filename

        member this.GetFileContent(fullPath) =
            getFileContent fullPath

        member this.Start() =
            let fsw = new FileSystemWatcher(dir, "*.html", EnableRaisingEvents = true)
            fsw.Created.Add(callback)
            fsw.Changed.Add(callback)
            fsw.Renamed.Add(callback)
            TemplateCache.client <-
                { new Client.ClientBase() with
                    member __.SetOnChange(_) = ()
                    member __.RequestFile(filename) =
                        onchange (fullPathOf filename)
                }

    /// Client used when running in Blazor server-side mode.
    and Client(watcher: Watcher) =

        let handlers = ResizeArray()

        interface IClient with

            member this.RequestTemplate(filename, subtemplate) =
                TemplateCache.client.RequestTemplate(filename, subtemplate)

            member this.SetOnChange(callback) =
                watcher.Changed.Subscribe(fun (filename, content) ->
                    TemplateCache.client.FileChanged(filename, content)
                    callback())
                |> handlers.Add

            member this.FileChanged(filename, content) =
                TemplateCache.client.FileChanged(filename, content)

        interface IDisposable with

            member this.Dispose() =
                for handler in handlers do
                    handler.Dispose()

[<Extension>]
type ServerTemplatingExtensions =

    [<Extension>]
    static member AddHotReload(this: IServiceCollection, ?templateDir: string) : IServiceCollection =
        this.AddSignalRCore().AddJsonProtocol() |> ignore
        this.AddSingleton({ dir = templateDir })
            .AddSingleton<Watcher>()
            .AddTransient<IClient, Client>()

    [<Extension>]
    static member UseHotReload(this: IApplicationBuilder, ?urlPath: string) : IApplicationBuilder =
        this.ApplicationServices.GetService<Watcher>().Start()
        let urlPath = defaultArg urlPath HotReloadSettings.Default.Url
        this.UseSignalR(fun route ->
            route.MapHub<HotReloadHub>(PathString urlPath)
        )
