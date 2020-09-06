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

#nowarn "44" // Ignore obsolescence of RemoteResponse

open System
open System.Threading.Tasks
open Microsoft.JSInterop
open Elmish
open Bolero.Remoting

/// Elmish commands for remote calls and JavaScript interop.
module Cmd =

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

    module OfAuthorized =

        /// Command that will call a remote Bolero function with authorization and map the result
        /// into successful Some if authorized, successful None if not, or error (of exception)
        let either (f: 'req -> Async<'resp>) (arg: 'req) (ofSuccess: option<'resp> -> 'msg) (ofError: exn -> 'msg) : Cmd<'msg> =
            Cmd.OfAsync.either (wrapAuthorized f) arg ofSuccess ofError

        /// Command that will call a remote Bolero function with authorization and map the result
        /// into Some if authorized, None if not, discarding any possible error
        let perform (f: 'req -> Async<'resp>) (arg: 'req) (ofSuccess: option<'resp> -> 'msg) : Cmd<'msg> =
            Cmd.OfAsync.perform (wrapAuthorized f) arg ofSuccess

        /// Command that will call a remote Bolero function with authorization and map the error (of exception)
        let attempt (f: 'req -> Async<'resp>) (arg: 'req) (ofError: exn -> 'msg) : Cmd<'msg> =
            Cmd.OfAsync.attempt (wrapAuthorized f) arg ofError

    module OfJS =

        let private invoke (js: IJSRuntime) (jsFunctionName: string) : obj[] -> Task<'res> =
            () // <-- Forces compiling this into a function-returning function rather than a 3-arg function
            fun args -> js.InvokeAsync(jsFunctionName, args).AsTask()

        /// Command that will perform a JavaScript interop call and map the result to a message
        /// or error (of exception)
        let either (js: IJSRuntime) (jsFunctionName: string) (args: obj[]) (ofSuccess: 'res -> 'msg) (ofError: exn -> 'msg) : Cmd<'msg> =
            Cmd.OfTask.either (invoke js jsFunctionName) args ofSuccess ofError

        /// Command that will perform a JavaScript interop call and map the result to a message
        /// discarding any possible error
        let perform (js: IJSRuntime) (jsFunctionName: string) (args: obj[]) (ofSuccess: 'res -> 'msg) : Cmd<'msg> =
            Cmd.OfTask.perform (invoke js jsFunctionName) args ofSuccess

        /// Command that will perform a JavaScript interop call and map the error (of exception)
        let attempt (js: IJSRuntime) (jsFunctionName: string) (args: obj[]) (ofError: exn -> 'msg) : Cmd<'msg> =
            Cmd.OfTask.attempt (invoke js jsFunctionName) args ofError

    /// Command that will call a remote Bolero function with authorization and map the result
    /// into response or error (of exception)
    [<Obsolete "Use Cmd.OfAsync.either or Cmd.OfAuthorized.either">]
    let ofRemote (f: 'req -> Async<'resp>) (arg: 'req) (ofSuccess: RemoteResponse<'resp> -> 'msg) (ofError: exn -> 'msg) : Cmd<'msg> =
        Cmd.OfAsync.either (wrapRemote f) arg ofSuccess ofError

    /// Command that will call a remote Bolero function with authorization and map the success
    /// to a message discarding any possible error
    [<Obsolete "Use Cmd.OfAsync.perform or Cmd.OfAuthorized.perform">]
    let performRemote (f: 'req -> Async<'resp>) (arg: 'req) (ofSuccess: RemoteResponse<'resp> -> 'msg) : Cmd<'msg> =
        Cmd.OfAsync.perform (wrapRemote f) arg ofSuccess

    /// Command that will call a remote Bolero function with authorization and map the result
    /// into successful Some if authorized, successful None if not, or error (of exception)
    [<Obsolete "Use Cmd.OfAuthorized.either">]
    let ofAuthorized f arg ofSuccess ofError = OfAuthorized.either f arg ofSuccess ofError

    /// Command that will call a remote Bolero function with authorization and map the result
    /// into Some if authorized, None if not, discarding any possible error
    [<Obsolete "Use Cmd.OfAuthorized.perform">]
    let performAuthorized f arg ofSuccess = OfAuthorized.perform f arg ofSuccess

    /// Command that will perform a JavaScript interop call and map the result to a message
    /// or error (of exception)
    [<Obsolete "Use Cmd.OfJS.either">]
    let either js jsFunctionName args ofSuccess ofError = OfJS.either js jsFunctionName args ofSuccess ofError

    /// Command that will perform a JavaScript interop call and map the result to a message
    /// discarding any possible error
    [<Obsolete "Use Cmd.OfJS.perform">]
    let performJS js jsFunctionName args ofSuccess = OfJS.perform js jsFunctionName args ofSuccess
