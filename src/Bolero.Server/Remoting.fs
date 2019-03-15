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

namespace Bolero.Remoting.Server

open System
open System.IO
open System.Reflection
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Bolero
open Bolero.Remoting

type IRemoteHandler =
    abstract Handler : IRemoteService

[<AbstractClass>]
type RemoteHandler<'T when 'T :> IRemoteService>() =
    abstract Handler : 'T
    interface IRemoteHandler with
        member this.Handler = this.Handler :> IRemoteService

module Remote =

    let internal context = new ThreadLocal<HttpContext>()

    type internal IWithAuthorization =
        abstract AuthorizeData : seq<IAuthorizeData>

    type internal WithAuthorization<'req, 'resp>(authData: seq<IAuthorizeData>, f) =
        inherit FSharp.Core.FSharpFunc<'req, Async<'resp>>()

        interface IWithAuthorization with
            member this.AuthorizeData = authData

        override this.Invoke(req) = f context.Value req

    /// Give a remote function access to its HttpContext.
    /// Must be called outside any async {} block.
    let withContext (f: HttpContext -> 'req -> Async<'resp>) =
        id (fun req -> f context.Value req)

    /// Mark a remote function as requiring authentication with the given policy.
    /// Must be called outside any async {} block.
    let authorizeWith (authData: seq<IAuthorizeData>) (f: HttpContext -> 'req -> Async<'resp>) =
        WithAuthorization(authData, f) :> obj :?> ('req -> Async<'resp>)

    /// Mark a remote function as requiring authentication.
    /// Must be called outside any async {} block.
    let authorize (f: HttpContext -> 'req -> Async<'resp>) =
        WithAuthorization([], f) :> obj :?> ('req -> Async<'resp>)

type internal RemotingService(basePath: PathString, ty: System.Type, handler: obj, authPolicyProvider: IAuthorizationPolicyProvider) =

    let flags = BindingFlags.Public ||| BindingFlags.NonPublic
    let staticFlags = flags ||| BindingFlags.Static
    let instanceFlags = flags ||| BindingFlags.Instance

    let makeHandler (method: RemoteMethodDefinition) =
        let decoder = Json.GetDecoder method.ArgumentType
        let encoder = Json.GetEncoder method.ReturnType
        let meth = ty.GetProperty(method.Name).GetGetMethod().Invoke(handler, [||])
        let tAuthPolicy =
            let authData =
                match meth with
                | :? Remote.IWithAuthorization as m -> m.AuthorizeData
                | _ -> Seq.empty
            match authPolicyProvider, Seq.isEmpty authData with
            | _, true -> None
            | null, false ->
                failwithf "Remote method %s.%s is configured for authorization, but the application has no authorization policy."
                    ty.FullName method.Name
            | authPolicyProvider, false ->
                AuthorizationPolicy.CombineAsync(authPolicyProvider, authData)
                |> Some
        let callMeth = method.FunctionType.GetMethod("Invoke", instanceFlags)
        let output = typeof<RemotingService>.GetMethod("Output", staticFlags)
        let output = output.MakeGenericMethod(method.ReturnType)
        fun (auth: IAuthorizationService) (ctx: HttpContext) ->
            let run() =
                let arg =
                    using (new StreamReader(ctx.Request.Body)) Json.Raw.Read
                    |> decoder
                Remote.context.Value <- ctx
                let res = callMeth.Invoke(meth, [|arg|])
                output.Invoke(null, [|ctx; encoder; res|]) :?> Task
            match tAuthPolicy with
            | None -> run()
            | Some tAuthPolicy ->
                tAuthPolicy
                    .ContinueWith(fun (tAuthPolicy: Task<AuthorizationPolicy>) ->
                        auth.AuthorizeAsync(ctx.User, tAuthPolicy.Result)
                    )
                    .Unwrap()
                    .ContinueWith(fun (tAuthResult: Task<AuthorizationResult>) ->
                        if tAuthResult.Result.Succeeded then
                            run()
                        else
                            ctx.Response.StatusCode <- StatusCodes.Status401Unauthorized
                            // TODO: allow customizing based on what failed?
                            Task.CompletedTask
                    )
                    .Unwrap()

    let methodData =
        match RemotingExtensions.ExtractRemoteMethods ty with
        | Error errors ->
            raise <| AggregateException(
                "Cannot create remoting handler for type " + ty.FullName,
                [| for e in errors -> exn e |])
        | Ok methods ->
            methods

    let methods = dict [for m in methodData -> m.Name, makeHandler m]

    static member Output<'Out>(ctx: HttpContext, encoder: Json.Encoder<obj>, a: Async<'Out>) : Task =
        async {
            let! x = a
            let v = encoder x
            use writer = new StreamWriter(ctx.Response.Body)
            Json.Raw.Write writer v
        }
        |> Async.StartImmediateAsTask
        :> _

    member this.ServiceType = ty

    member this.Service = handler

    member this.TryHandle(ctx: HttpContext, auth: IAuthorizationService) : option<Task> =
        let mutable restPath = PathString.Empty
        if ctx.Request.Method = "POST" && ctx.Request.Path.StartsWithSegments(basePath, &restPath) then
            let methodName = restPath.Value.TrimStart('/')
            match methods.TryGetValue(methodName) with
            | true, handle -> Some(handle auth ctx)
            | false, _ -> None
        else
            None

/// Provides remote service implementations when running in Server-side Blazor.
type internal ServerRemoteProvider(services: seq<RemotingService>) =

    member this.GetService<'T>() =
        services
        |> Seq.tryPick (fun s ->
            if s.ServiceType = typeof<'T> then
                Some (s.Service :?> 'T)
            else
                None
        )
        |> Option.defaultWith (fun () ->
            failwithf "Remote service not registered: %s" typeof<'T>.FullName)

    interface IRemoteProvider with

        member this.GetService<'T>(_basePath: string) =
            this.GetService<'T>()

        member this.GetService<'T when 'T :> IRemoteService>() =
            this.GetService<'T>()

[<Extension>]
type ServerRemotingExtensions =

    [<Extension>]
    static member AddRemoting<'T when 'T : not struct>(this: IServiceCollection, basePath: PathString, handler: 'T) =
        this.AddSingleton<RemotingService>(fun services ->
                let authorizationPolicyProvider = services.GetService<IAuthorizationPolicyProvider>()
                RemotingService(basePath, typeof<'T>, handler, authorizationPolicyProvider))
            .AddSingleton<IRemoteProvider, ServerRemoteProvider>()

    [<Extension>]
    static member AddRemoting<'T when 'T : not struct>(this: IServiceCollection, basePath: string, handler: 'T) =
        this.AddRemoting(PathString.op_Implicit basePath, handler)

    [<Extension>]
    static member AddRemoting<'T when 'T : not struct and 'T :> IRemoteService>(this: IServiceCollection, handler: 'T) =
        this.AddRemoting(handler.BasePath, handler)

    [<Extension>]
    static member AddRemoting<'T when 'T : not struct and 'T :> IRemoteHandler>(this: IServiceCollection) =
        this.AddSingleton<'T>()
            .AddSingleton<RemotingService>(fun services ->
                let handler = services.GetRequiredService<'T>().Handler
                let authorizationPolicyProvider = services.GetService<IAuthorizationPolicyProvider>()
                RemotingService(PathString handler.BasePath, handler.GetType(), handler, authorizationPolicyProvider))
            .AddSingleton<IRemoteProvider, ServerRemoteProvider>()

    [<Extension>]
    static member UseRemoting(this: IApplicationBuilder) =
        let handlers =
            this.ApplicationServices.GetServices<RemotingService>()
            |> Array.ofSeq
        let auth = this.ApplicationServices.GetService<IAuthorizationService>()
        this.Use(fun ctx next ->
            handlers
            |> Array.tryPick (fun h -> h.TryHandle(ctx, auth))
            |> Option.defaultWith next.Invoke
        )
