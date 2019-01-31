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
namespace Bolero.Templating

open System
open System.IO
open System.Net.WebSockets
open System.Text
open System.Threading.Tasks
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

[<AutoOpen>]
module private Impl =

    type WebSocket with
        member this.AsyncSendText(s: string, [<Optional; DefaultParameterValue true>] endOfMessage: bool) = async {
            let s = Encoding.UTF8.GetBytes(s)
            let! token = Async.CancellationToken
            return! this.SendAsync(ArraySegment(s, 0, s.Length), WebSocketMessageType.Text, endOfMessage, token)
                |> Async.AwaitTask
        }

        member this.TryAsyncReceiveFrame(buffer) = async {
            let! token = Async.CancellationToken
            try
                let! response = this.ReceiveAsync(buffer, token) |> Async.AwaitTask
                match response.MessageType with
                | WebSocketMessageType.Close -> return None
                | _ -> return Some response
            with :? WebSocketException as exn when exn.WebSocketErrorCode = WebSocketError.ConnectionClosedPrematurely ->
                return None
        }

        member this.AsyncReceiveMessage() =
            let message = ResizeArray()
            let buffer = Array.zeroCreate 4096
            let rec go() = async {
                let! response = this.TryAsyncReceiveFrame(ArraySegment(buffer))
                match response with
                | None -> return None
                | Some response ->
                    message.AddRange(ArraySegment(buffer, 0, response.Count))
                    if response.EndOfMessage then
                        return Some (message.ToArray())
                    else
                        return! go()
            }
            go()

    type WatcherMessage =
        | FileChanged of fullPath: string * content: string
        | AddWatcher of filename: string * connId: string * notify: (string -> Async<unit>)
        | RemoveWatcher of connId: string

    type Watcher = MailboxProcessor<WatcherMessage>

    type WatcherState =
        {
            byConnId: Map<string, Set<string> * (string -> Async<unit>)>
            byPath: Map<string, Set<string>>
        }

    let logState (log: ILogger) (state: WatcherState) =
        log.LogInformation("Connections:")
        for KeyValue(connId, (paths, _)) in state.byConnId do
            log.LogInformation("  {0}: {1}", connId, String.concat ", " paths)
        log.LogInformation("Files:")
        for KeyValue(path, conns) in state.byPath do
            log.LogInformation("  {0}: {1}", path, String.concat ", " conns)

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

    let Watcher (dir: string) (log: ILogger) : Watcher = MailboxProcessor.Start(fun mb -> async {

        let callback (args: FileSystemEventArgs) =
            Async.StartImmediate <| async {
                let! content = asyncRetry 3 <| async {
                    use f = File.OpenText(args.FullPath)
                    return! f.ReadToEndAsync() |> Async.AwaitTask
                }
                match content with
                | None -> log.LogWarning("Bolero HotReload: ")
                | Some content -> mb.Post(FileChanged(args.FullPath, content))
            }

        use fsw = new FileSystemWatcher(dir, "*.html", EnableRaisingEvents = true)
        fsw.Created.Add(callback)
        fsw.Changed.Add(callback)
        fsw.Renamed.Add(callback)

        let rec loop state = async {
            match! mb.Receive() with
            | FileChanged (fullPath, content) ->
                let conns =
                    state.byPath
                    |> Map.tryFind fullPath
                    |> Option.defaultValue Set.empty
                for connId in conns do
                    do! (snd state.byConnId.[connId]) content
                return! loop state
            | AddWatcher (filename, connId, ws) ->
                let fullPath = Path.Combine(dir, filename)
                let connEntry =
                    match Map.tryFind connId state.byConnId with
                    | None -> Set.singleton fullPath, ws
                    | Some (paths, ws) -> Set.add fullPath paths, ws
                let pathEntry =
                    Map.tryFind fullPath state.byPath
                    |> Option.defaultValue Set.empty
                    |> Set.add connId
                return! loop
                    { state with
                        byConnId = Map.add connId connEntry state.byConnId
                        byPath = Map.add fullPath pathEntry state.byPath }
            | RemoveWatcher connId ->
                match Map.tryFind connId state.byConnId with
                | None -> return! loop state
                | Some (paths, _) ->
                    return! loop
                        { state with
                            byConnId = Map.remove connId state.byConnId
                            byPath = Set.foldBack Map.remove paths state.byPath }
        }

        return! loop
            { byConnId = Map.empty
              byPath = Map.empty }
    })

    let rec HandleConnection (connId: string) (ws: WebSocket) (watcher: Watcher) = async {
        match! ws.AsyncReceiveMessage() with
        | None -> ()
        | Some msg ->
            let filename = Encoding.UTF8.GetString(msg)
            watcher.Post(AddWatcher(filename, connId, fun text -> async {
                do! ws.AsyncSendText(filename + "\n", false)
                return! ws.AsyncSendText(text)
            }))
            return! HandleConnection connId ws watcher
    }

[<Extension>]
type ServerTemplatingExtensions =

    [<Extension>]
    static member UseHotReload
        (
            this: IApplicationBuilder,
            ?dir: string,
            ?urlPath: string,
            ?webSocketOptions: WebSocketOptions
        ) : IApplicationBuilder =
        let urlPath = defaultArg urlPath "/bolero-reload"
        let this =
            match webSocketOptions with
            | None -> this.UseWebSockets()
            | Some options -> this.UseWebSockets(options)
        let dir =
            match dir with
            | Some dir -> dir
            | None -> this.ApplicationServices.GetRequiredService<IHostingEnvironment>().ContentRootPath
        let log = this.ApplicationServices.GetService<ILogger<ServerTemplatingExtensions>>()
        let watcher = Watcher dir log
        this.Use(fun context next ->
            async {
                if context.Request.Path.Value = urlPath then
                    let! websocket = context.WebSockets.AcceptWebSocketAsync() |> Async.AwaitTask
                    return! HandleConnection context.Connection.Id websocket watcher
                else
                    return! next.Invoke() |> Async.AwaitTask
            }
            |> Async.StartAsTask
            :> Task)
