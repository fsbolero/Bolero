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
open System.Reflection
open System.Runtime.CompilerServices
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks.V2
open Bolero.Remoting

/// [omit]
type IRemoteHandler =
    abstract Handler : IRemoteService

/// [omit]
[<AbstractClass>]
type RemoteHandler<'T when 'T :> IRemoteService>() =
    abstract Handler : 'T
    interface IRemoteHandler with
        member this.Handler = this.Handler :> IRemoteService

/// The context to inject in a remote service to authorize remote functions.
type IRemoteContext =
    inherit IHttpContextAccessor

    /// Indicate that a remote function is only available to authenticated users.
    abstract Authorize<'req, 'resp> : ('req -> Async<'resp>) -> ('req -> Async<'resp>)

    /// Indicate that a remote function is available to users that match the given requirements.
    abstract AuthorizeWith<'req, 'resp> : seq<IAuthorizeData> -> ('req -> Async<'resp>) -> ('req -> Async<'resp>)

/// [omit]
type RemoteContext(http: IHttpContextAccessor, authService: IAuthorizationService, authPolicyProvider: IAuthorizationPolicyProvider) =

    let authorizeWith authData f =
        if Seq.isEmpty authData then f else
        let tAuthPolicy = AuthorizationPolicy.CombineAsync(authPolicyProvider, authData)
        fun req -> async {
            let! authPolicy = tAuthPolicy |> Async.AwaitTask
            let! authResult = authService.AuthorizeAsync(http.HttpContext.User, authPolicy) |> Async.AwaitTask
            if authResult.Succeeded then
                return! f req
            else
                return raise RemoteUnauthorizedException
        }

    interface IHttpContextAccessor with
        member __.HttpContext with get () = http.HttpContext and set v = http.HttpContext <- v

    interface IRemoteContext with
        member __.Authorize f = authorizeWith [AuthorizeAttribute()] f
        member __.AuthorizeWith authData f = authorizeWith authData f

type internal RemotingService(basePath: PathString, ty: Type, handler: obj, configureSerialization: option<JsonSerializerOptions -> unit>) as this =

    let flags = BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance
    let makeHandler (method: RemoteMethodDefinition) =
        let meth = ty.GetProperty(method.Name).GetValue(handler)
        let output =
            typeof<RemotingService>.GetMethod("InvokeForClientSide", flags)
                .MakeGenericMethod(method.ArgumentType, method.ReturnType)
        fun (ctx: HttpContext) ->
            output.Invoke(this, [|meth; ctx|]) :?> Task

    let methodData =
        match RemotingExtensions.ExtractRemoteMethods ty with
        | Error errors ->
            raise <| AggregateException(
                "Cannot create remoting handler for type " + ty.FullName,
                [| for e in errors -> exn e |])
        | Ok methods ->
            methods

    let methods = dict [for m in methodData -> m.Name, makeHandler m]

    let serOptions = JsonSerializerOptions()
    do match configureSerialization with
        | None -> serOptions.Converters.Add(JsonFSharpConverter())
        | Some f -> f serOptions

    member val ServerSideService = handler

    member private _.InvokeForClientSide<'req, 'resp>(func: 'req -> Async<'resp>, ctx: HttpContext) : Task =
        task {
            let! arg = JsonSerializer.DeserializeAsync<'req>(ctx.Request.Body, serOptions).AsTask()
            try
                let! x = func arg
                return! JsonSerializer.SerializeAsync<'resp>(ctx.Response.Body, x, serOptions)
            with exn when (exn.GetBaseException() :? RemoteUnauthorizedException) ->
                ctx.Response.StatusCode <- StatusCodes.Status401Unauthorized
                // TODO: allow customizing based on what failed?
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
            failwith $"Remote service not registered: {typeof<'T>.FullName}")

    interface IRemoteProvider with

        member this.GetService<'T>(_basePath: string) =
            this.GetService<'T>()

        member this.GetService<'T when 'T :> IRemoteService>() =
            this.GetService<'T>()

/// Extension methods to enable support for remoting in the ASP.NET Core server side.
[<Extension>]
type ServerRemotingExtensions =

    static member private AddRemotingImpl(this: IServiceCollection, getService: IServiceProvider -> RemotingService) =
        this.AddSingleton<RemotingService>(getService)
            .AddSingleton<IRemoteContext, RemoteContext>()
            .AddTransient<IRemoteProvider, ServerRemoteProvider>()
            .AddHttpContextAccessor()

    static member private AddRemotingImpl<'T when 'T : not struct>(this: IServiceCollection, basePath: 'T -> PathString, handler: IRemoteContext -> 'T, configureSerialization: option<JsonSerializerOptions -> unit>) =
        ServerRemotingExtensions.AddRemotingImpl(this, fun services ->
            let ctx = services.GetRequiredService<IRemoteContext>()
            let handler = handler ctx
            let basePath = basePath handler
            RemotingService(basePath, typeof<'T>, handler, configureSerialization))

    /// Add a remote service at the given path.
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct>(this: IServiceCollection, basePath: PathString, handler: IRemoteContext -> 'T, ?configureSerialization) =
        ServerRemotingExtensions.AddRemotingImpl<'T>(this, (fun _ -> basePath), handler, configureSerialization)

    /// Add a remote service at the given path.
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct>(this: IServiceCollection, basePath: PathString, handler: 'T, ?configureSerialization) =
        ServerRemotingExtensions.AddRemotingImpl<'T>(this, (fun _ -> basePath), (fun _ -> handler), configureSerialization)

    /// Add a remote service at the given path.
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct>(this: IServiceCollection, basePath: string, handler: IRemoteContext -> 'T, ?configureSerialization) =
        ServerRemotingExtensions.AddRemotingImpl<'T>(this, (fun _ -> PathString basePath), handler, configureSerialization)

    /// Add a remote service at the given path.
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct>(this: IServiceCollection, basePath: string, handler: 'T, ?configureSerialization) =
        ServerRemotingExtensions.AddRemotingImpl<'T>(this, (fun _ -> PathString basePath), (fun _ -> handler), configureSerialization)

    /// Add a remote service.
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct and 'T :> IRemoteService>(this: IServiceCollection, handler: IRemoteContext -> 'T, ?configureSerialization) =
        ServerRemotingExtensions.AddRemotingImpl<'T>(this, (fun h -> PathString h.BasePath), handler, configureSerialization)

    /// Add a remote service.
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct and 'T :> IRemoteService>(this: IServiceCollection, handler: 'T, ?configureSerialization) =
        ServerRemotingExtensions.AddRemotingImpl<'T>(this, (fun h -> PathString h.BasePath), (fun _ -> handler), configureSerialization)

    /// Add a remote service using dependency injection.
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct and 'T :> IRemoteHandler>(this: IServiceCollection, ?configureSerialization) =
        ServerRemotingExtensions.AddRemotingImpl(this.AddSingleton<'T>(), fun services ->
            let handler = services.GetRequiredService<'T>().Handler
            RemotingService(PathString handler.BasePath, handler.GetType(), handler, configureSerialization))

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
