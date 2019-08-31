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
open FSharp.Control.Tasks.V2
open FSharp.Reflection
open Bolero
open Bolero.Remoting
open System.Text

type IRemoteHandler =
    abstract Handler : IRemoteService

[<AbstractClass>]
type RemoteHandler<'T when 'T :> IRemoteService>() =
    abstract Handler : 'T
    interface IRemoteHandler with
        member this.Handler = this.Handler :> IRemoteService

module Remote =

    let internal localData = AsyncLocal<IAuthorizationService * IAuthorizationPolicyProvider * HttpContext>()

    /// Mark a remote function as requiring authentication with the given policy.
    /// Must be used directly as the value of a remote function.
    let authorizeWith (authData: seq<IAuthorizeData>) (f: 'req -> Async<'resp>) =
        fun req ->
            let authService, authPolicyProvider, http = localData.Value
            async {
                let! authPolicy = AuthorizationPolicy.CombineAsync(authPolicyProvider, authData) |> Async.AwaitTask
                let! authResult = authService.AuthorizeAsync(http.User, authPolicy) |> Async.AwaitTask
                if authResult.Succeeded then
                    return! f req
                else
                    return raise RemoteUnauthorizedException
            }

    /// Mark a remote function as requiring authentication.
    /// Must be used directly as the value of a remote function.
    let authorize (f: 'req -> Async<'resp>) =
        authorizeWith [AuthorizeAttribute()] f

type internal RemotingService(basePath: PathString, ty: System.Type, handler: obj, services: IServiceProvider) =

    let flags = BindingFlags.Public ||| BindingFlags.NonPublic
    let staticFlags = flags ||| BindingFlags.Static
    let instanceFlags = flags ||| BindingFlags.Instance

    let authPolicyProvider = services.GetService<IAuthorizationPolicyProvider>()
    let http = services.GetService<IHttpContextAccessor>()
    let authService = services.GetService<IAuthorizationService>()

    static let fail (ctx: HttpContext) =
        ctx.Response.StatusCode <- StatusCodes.Status401Unauthorized
        // TODO: allow customizing based on what failed?

    let makeHandler (method: RemoteMethodDefinition) =
        let meth = ty.GetProperty(method.Name).GetValue(handler)
        let output =
            typeof<RemotingService>.GetMethod("InvokeForClientSide", staticFlags)
                .MakeGenericMethod(method.ArgumentType, method.ReturnType)
        let decoder = Json.GetDecoder method.ArgumentType
        let encoder = Json.GetEncoder method.ReturnType
        fun (ctx: HttpContext) ->
            output.Invoke(null, [|decoder; encoder; meth; authService; authPolicyProvider; ctx|]) :?> Task

    let methodData =
        match RemotingExtensions.ExtractRemoteMethods ty with
        | Error errors ->
            raise <| AggregateException(
                "Cannot create remoting handler for type " + ty.FullName,
                [| for e in errors -> exn e |])
        | Ok methods ->
            methods

    let methods = dict [for m in methodData -> m.Name, makeHandler m]

    member val ServerSideService =
        let fields =
            methodData
            |> Array.map (fun method ->
                let meth = ty.GetProperty(method.Name).GetValue(handler)
                typeof<RemotingService>.GetMethod("WrapForServerSide", staticFlags)
                    .MakeGenericMethod(method.ArgumentType, method.ReturnType)
                    .Invoke(null, [|meth; authService; authPolicyProvider; http|])
            )
        FSharpValue.MakeRecord(ty, fields)

    static member private WrapForServerSide<'req, 'resp> (f: 'req -> Async<'resp>, authService: IAuthorizationService, authPolicyProvider: IAuthorizationPolicyProvider, http: IHttpContextAccessor) =
        fun req ->
            Remote.localData.Value <- (authService, authPolicyProvider, http.HttpContext)
            f req

    static member private InvokeForClientSide<'req, 'resp>(decoder: Json.Decoder<obj>, encoder: Json.Encoder<obj>, func: 'req -> Async<'resp>, authService: IAuthorizationService, authPolicyProvider: IAuthorizationPolicyProvider, ctx: HttpContext) : Task =
        Remote.localData.Value <- (authService, authPolicyProvider, ctx)
        task {
            use reader = new StreamReader(ctx.Request.Body)
            let! body = reader.ReadToEndAsync()
            let arg = Json.Raw.Parse body |> decoder :?> 'req
            try
                let! x = func arg
                let v = encoder x
                let json = Json.Raw.Stringify v
                return! ctx.Response.WriteAsync(json, Encoding.UTF8)
            with RemoteUnauthorizedException ->
                fail ctx
        } :> Task

    member this.ServiceType = ty

    member this.TryHandle(ctx: HttpContext) : option<Task> =
        let mutable restPath = PathString.Empty
        if ctx.Request.Method = "POST" && ctx.Request.Path.StartsWithSegments(basePath, &restPath) then
            let methodName = restPath.Value.TrimStart('/')
            match methods.TryGetValue(methodName) with
            | true, handle -> Some (handle ctx)
            | false, _ -> None
        else
            None

/// Provides remote service implementations when running in Server-side Blazor.
type internal ServerRemoteProvider(services: seq<RemotingService>) =

    member this.GetService<'T>() =
        services
        |> Seq.tryPick (fun s ->
            if s.ServiceType = typeof<'T> then
                Some (s.ServerSideService :?> 'T)
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
                RemotingService(basePath, typeof<'T>, handler, services))
            .AddTransient<IRemoteProvider, ServerRemoteProvider>()
            .AddHttpContextAccessor()

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
                RemotingService(PathString handler.BasePath, handler.GetType(), handler, services))
            .AddSingleton<IRemoteProvider, ServerRemoteProvider>()
            .AddHttpContextAccessor()

    [<Extension>]
    static member UseRemoting(this: IApplicationBuilder) =
        let handlers =
            this.ApplicationServices.GetServices<RemotingService>()
            |> Array.ofSeq
        this.Use(fun ctx next ->
            handlers
            |> Array.tryPick (fun h -> h.TryHandle(ctx))
            |> Option.defaultWith next.Invoke
        )
