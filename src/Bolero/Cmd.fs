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

namespace Bolero.Remoting.Client

#nowarn "44" // Ignore obsoleteness of RemoteResponse

open System
open System.Threading.Tasks
open Microsoft.JSInterop
open Elmish
open Bolero.Remoting

/// Elmish commands for Async and Task jobs, remote calls and JavaScript interop.
module Cmd =

    // This should be in Elmish really.
    /// Command that will evaluate an async block and map the success to a message
    /// discarding any possible error
    let performAsync (f: 'req -> Async<'resp>) (arg: 'req) (ofSuccess: 'resp -> 'msg) =
        Cmd.ofSub <| fun dispatch ->
            async.Bind(f arg, ofSuccess >> dispatch >> async.Return)
            |> Async.StartImmediate

    // This should be in Elmish really.
    /// Command that will evaluate a task and map the success to a message
    /// discarding any possible error
    let performTask (f: 'req -> Task<'resp>) (arg: 'req) (ofSuccess: 'resp -> 'msg) =
        Cmd.ofSub <| fun dispatch ->
            (f arg).ContinueWith(
                (fun (t: Task<'resp>) ->
                    if t.IsCompletedSuccessfully then
                        dispatch (ofSuccess t.Result)),
                TaskContinuationOptions.OnlyOnRanToCompletion)
            |> ignore

    let private wrapAuthorized (f: 'req -> Async<'resp>) : 'req -> Async<option<'resp>> =
        () // <-- Forces compiling this into a function-returning function rather than a 2-arg function
        fun (arg: 'req) -> async {
            try
                let! r = f arg
                return Some r
            with RemoteUnauthorizedException ->
                return None
        }

    let private wrapRemote (f: 'req -> Async<'resp>) : 'req -> Async<RemoteResponse<'resp>> =
        wrapAuthorized f >> fun res -> async {
            match! res with
            | Some x -> return Success x
            | None -> return Unauthorized
        }

    /// Command that will call a remote Bolero function with authorization and map the result
    /// into successful Some if authorized, successful None if not, or error (of exception)
    let ofAuthorized (f: 'req -> Async<'resp>) (arg: 'req) (ofSuccess: option<'resp> -> 'msg) (ofError: exn -> 'msg) : Cmd<'msg> =
        Cmd.ofAsync (wrapAuthorized f) arg ofSuccess ofError

    /// Command that will call a remote Bolero function with authorization and map the result
    /// into Some if authorized, None if not, discarding any possible error
    let performAuthorized (f: 'req -> Async<'resp>) (arg: 'req) (ofSuccess: option<'resp> -> 'msg) : Cmd<'msg> =
        performAsync (wrapAuthorized f) arg ofSuccess

    /// Command that will call a remote Bolero function with authorization and map the result
    /// into response or error (of exception)
    [<Obsolete "Use ofAsync or ofAuthorized">]
    let ofRemote (f: 'req -> Async<'resp>) (arg: 'req) (ofSuccess: RemoteResponse<'resp> -> 'msg) (ofError: exn -> 'msg) : Cmd<'msg> =
        Cmd.ofAsync (wrapRemote f) arg ofSuccess ofError

    /// Command that will call a remote Bolero function with authorization and map the success
    /// to a message discarding any possible error
    [<Obsolete "Use performAsync or performAuthorized">]
    let performRemote (f: 'req -> Async<'resp>) (arg: 'req) (ofSuccess: RemoteResponse<'resp> -> 'msg) : Cmd<'msg> =
        performAsync (wrapRemote f) arg ofSuccess

    /// Command that will perform a JavaScript interop call and map the result to a message
    /// or error (of exception)
    let ofJS (js: IJSRuntime) (jsFunctionName: string) (args: obj[]) (ofSuccess: 'res -> 'msg) (ofError: exn -> 'msg) : Cmd<'msg> =
        Cmd.ofTask (fun args -> js.InvokeAsync(jsFunctionName, args).AsTask()) args ofSuccess ofError

    /// Command that will perform a JavaScript interop call and map the result to a message
    /// discarding any possible error
    let performJS (js: IJSRuntime) (jsFunctionName: string) (args: obj[]) (ofSuccess: 'res -> 'msg) : Cmd<'msg> =
        performTask (fun args -> js.InvokeAsync(jsFunctionName, args).AsTask()) args ofSuccess
