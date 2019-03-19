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

open System
open System.IO
open System.Net.Http
open System.Runtime.CompilerServices
open System.Text
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection.Extensions
open FSharp.Reflection
open Bolero
open Bolero.Remoting
open System.Net

type RemoteResponse<'resp> =
    | Success of 'resp
    | Unauthorized

    member this.TryGetResponse() =
        match this with
        | Success x -> Some x
        | Unauthorized -> None

module Cmd =

    // This should be in Elmish really.
    /// Command that will evaluate an async block and map the success to a message
    /// discarding any possible error
    let performAsync (f: 'req -> Async<'resp>) (arg: 'req) (ofSuccess: 'resp -> 'msg) =
        let bind dispatch = async {
            let! r = f arg
            dispatch (ofSuccess r)
        }
        [bind >> Async.StartImmediate]

    let private wrapRemote (f: 'req -> Async<'resp>) : 'req -> Async<RemoteResponse<'resp>> =
        () // <-- Forces compiling this into a function-returning function rather than a 2-arg function
        fun (arg: 'req) -> async {
            try
                let! r = f arg
                return Success r
            with RemoteUnauthorizedException ->
                return Unauthorized
        }

    /// Command that will call a remote Bolero function with authorization and map the result
    /// into response or error (of exception)
    let ofRemote (f: 'req -> Async<'resp>) (arg: 'req) (ofSuccess: RemoteResponse<'resp> -> 'msg) (ofError: exn -> 'msg) =
        Elmish.Cmd.ofAsync (wrapRemote f) arg ofSuccess ofError

    /// Command that will call a remote Bolero function with authorization and map the success
    /// to a message discarding any possible error
    let performRemote (f: 'req -> Async<'resp>) (arg: 'req) (ofSuccess: RemoteResponse<'resp> -> 'msg) =
        performAsync (wrapRemote f) arg ofSuccess

/// Provides remote service implementations when running in WebAssembly.
type ClientRemoteProvider(http: HttpClient) =

    let normalizeBasePath(basePath: string) =
        basePath + (if basePath.EndsWith "/" then "" else "/")

    let send (method: HttpMethod) (requestUri: string) (content: obj) =
        let content =
            match content with
            | null ->
                Json.Raw.Stringify Json.Null
            | content ->
                Json.GetEncoder (content.GetType()) content
                |> Json.Raw.Stringify
        new HttpRequestMessage(method, requestUri,
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        )
        |> http.SendAsync
        |> Async.AwaitTask

    member this.SendAndParse<'T>(method, requestUri, content) = async {
        let! resp = send method requestUri content
        match resp.StatusCode with
        | HttpStatusCode.OK ->
            let! respBody = resp.Content.ReadAsStreamAsync() |> Async.AwaitTask
            use reader = new StreamReader(respBody)
            return Json.Read<'T> reader
        | HttpStatusCode.Unauthorized ->
            return raise RemoteUnauthorizedException
        | code ->
            return raise (HttpRequestException("Unexpected response status: " + string code))
    }

    member this.MakeRemoteProxy(ty: Type, baseUri: string ref) =
        match RemotingExtensions.ExtractRemoteMethods(ty) with
        | Error errors ->
            raise <| AggregateException(
                "Cannot create remoting handler for type " + ty.FullName,
                [| for e in errors -> exn e |])
        | Ok methods ->
            let ctor = FSharpValue.PreComputeRecordConstructor(ty, true)
            methods
            |> Array.map (fun method ->
                let post =
                    typeof<ClientRemoteProvider>.GetMethod("SendAndParse")
                        .MakeGenericMethod([|method.ReturnType|])
                FSharpValue.MakeFunction(method.FunctionType, fun arg ->
                    let uri = !baseUri + method.Name
                    post.Invoke(this, [|HttpMethod.Post; uri; arg|])
                )
            )
            |> ctor

    interface IRemoteProvider with

        member this.GetService<'T>(basePath: string) =
            let basePath = normalizeBasePath(basePath)
            this.MakeRemoteProxy(typeof<'T>, ref basePath) :?> 'T

        member this.GetService<'T when 'T :> IRemoteService>() =
            let basePath = ref ""
            let proxy = this.MakeRemoteProxy(typeof<'T>, basePath) :?> 'T
            basePath := normalizeBasePath proxy.BasePath
            proxy

[<Extension>]
type ClientRemotingExtensions =

    /// Enable support for remoting in ProgramComponent.
    [<Extension>]
    static member AddRemoting(services: IServiceCollection) =
        services.TryAddSingleton<IRemoteProvider, ClientRemoteProvider>()
        services
