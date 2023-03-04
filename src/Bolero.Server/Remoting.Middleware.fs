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
open Bolero.Remoting

/// <exclude />
type IRemoteHandler =
    abstract Handler : IRemoteService

/// <exclude />
[<AbstractClass>]
type RemoteHandler<'T when 'T :> IRemoteService>() =
    abstract Handler : 'T
    interface IRemoteHandler with
        member this.Handler = this.Handler :> IRemoteService

/// <summary>The context to inject in a remote service to authorize remote functions.</summary>
type IRemoteContext =
    inherit IHttpContextAccessor

    /// <summary>Indicate that a remote function is only available to authenticated users.</summary>
    /// <typeparam name="req">The input request type.</typeparam>
    /// <typeparam name="resp">The output response type.</typeparam>
    /// <param name="request">The request handler to authorize.</param>
    /// <returns>The authorized request handler.</returns>
    abstract Authorize<'req, 'resp> : request: ('req -> Async<'resp>) -> ('req -> Async<'resp>)

    /// <summary>Indicate that a remote function is available to users that match the given requirements.</summary>
    /// <typeparam name="req">The input request type.</typeparam>
    /// <typeparam name="resp">The output response type.</typeparam>
    /// <param name="authorizeData">The authorization requirements.</param>
    /// <param name="request">The request handler to authorize.</param>
    /// <returns>The authorized request handler.</returns>
    abstract AuthorizeWith<'req, 'resp> : authorizeData: seq<IAuthorizeData> -> request: ('req -> Async<'resp>) -> ('req -> Async<'resp>)

/// <exclude />
type RemoteContext(http: IHttpContextAccessor, authService: IAuthorizationService, authPolicyProvider: IAuthorizationPolicyProvider) =

    static member Handle<'req, 'resp>(tAuthPolicy: Task<AuthorizationPolicy>, authService: IAuthorizationService, http: IHttpContextAccessor, f: 'req -> Async<'resp>) : 'req -> Async<'resp> =
        fun req -> async {
            let! authPolicy = tAuthPolicy |> Async.AwaitTask
            let! authResult = authService.AuthorizeAsync(http.HttpContext.User, authPolicy) |> Async.AwaitTask
            if authResult.Succeeded then
                return! f req
            else
                return raise RemoteUnauthorizedException
        }

    member this.AuthorizeWith<'req, 'resp>(authData, f) =
        let tAuthPolicy = AuthorizationPolicy.CombineAsync(authPolicyProvider, authData)
        RemoteContext.Handle<'req, 'resp>(tAuthPolicy, authService, http, f)

    interface IHttpContextAccessor with
        member _.HttpContext with get () = http.HttpContext and set v = http.HttpContext <- v

    interface IRemoteContext with
        member this.Authorize<'req,'resp> f = this.AuthorizeWith<'req, 'resp>([AuthorizeAttribute()], f)
        member this.AuthorizeWith<'req,'resp> authData f = this.AuthorizeWith<'req,'resp>(authData, f)

/// <summary>
/// Information about a remote service.
/// </summary>
type IRemoteServiceMetadata =
    abstract Type: Type
    abstract BasePath: PathString

/// <summary>
/// Information about a remote method.
/// </summary>
type IRemoteMethodMetadata =
    abstract Service: IRemoteServiceMetadata
    abstract Name: string
    abstract ArgumentType: Type
    abstract ReturnType: Type
    abstract Handler: RequestDelegate
    abstract Function: obj

type internal RemotingService(basePath: PathString, ty: Type, handler: obj, configureSerialization: option<JsonSerializerOptions -> unit>) as this =

    let flags = BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance
    let makeHandler (method: RemoteMethodDefinition) =
        let func = ty.GetProperty(method.Name).GetValue(handler)
        let output =
            typeof<RemotingService>.GetMethod("InvokeForClientSide", flags)
                .MakeGenericMethod(method.ArgumentType, method.ReturnType)
        func, RequestDelegate(fun (ctx: HttpContext) ->
            output.Invoke(this, [|func; ctx|]) :?> Task)

    let methodData =
        match RemotingExtensions.ExtractRemoteMethods ty with
        | Error errors ->
            raise <| AggregateException(
                "Cannot create remoting handler for type " + ty.FullName,
                [| for e in errors -> exn e |])
        | Ok methods ->
            methods

    let service =
        { new IRemoteServiceMetadata with
            member _.Type = ty
            member _.BasePath = basePath }

    let methods = dict [
        for m in methodData do
            let func, handler = makeHandler m
            m.Name,
            { new IRemoteMethodMetadata with
                member _.Service = service
                member _.Name = m.Name
                member _.ArgumentType = m.ArgumentType
                member _.ReturnType = m.ReturnType
                member _.Handler = handler
                member _.Function = func }
    ]

    let serOptions = JsonSerializerOptions()
    do match configureSerialization with
        | None -> serOptions.Converters.Add(JsonFSharpConverter())
        | Some f -> f serOptions

    member _.ServerSideService = handler

    member _.ServiceType = ty

    member _.Methods = methods

    member _.BasePath = basePath

    member private _.InvokeForClientSide<'req, 'resp>(func: 'req -> Async<'resp>, ctx: HttpContext) : Task =
        task {
            let! arg = JsonSerializer.DeserializeAsync<'req>(ctx.Request.Body, serOptions).AsTask()
            try
                let! x = func arg
                return! JsonSerializer.SerializeAsync<'resp>(ctx.Response.Body, x, serOptions)
            with exn when (exn.GetBaseException() :? RemoteUnauthorizedException) ->
                ctx.Response.StatusCode <- StatusCodes.Status401Unauthorized
        } :> Task

    member this.TryHandle(ctx: HttpContext) : option<Task> =
        let mutable restPath = PathString.Empty
        if ctx.Request.Method = "POST" && ctx.Request.Path.StartsWithSegments(basePath, &restPath) then
            let methodName = restPath.Value.TrimStart('/')
            match methods.TryGetValue(methodName) with
            | true, method -> Some (method.Handler.Invoke(ctx))
            | false, _ -> None
        else
            None

/// Provides remote service implementations when running in Server-side Blazor.
type internal ServerRemoteProvider<'T>(services: seq<RemotingService>) =

    member this.GetService() =
        services
        |> Seq.tryPick (fun s ->
            if s.ServiceType = typeof<'T> then
                Some (s.ServerSideService :?> 'T)
            else
                None
        )
        |> Option.defaultWith (fun () ->
            failwith $"Remote service not registered: {typeof<'T>.FullName}")

    interface IRemoteProvider<'T> with

        member this.GetService(_basePath: 'T -> string) =
            this.GetService()

/// Extension methods to enable support for remoting in the ASP.NET Core server side.
[<Extension>]
type ServerRemotingExtensions =

    static member private AddRemotingImpl(this: IServiceCollection, getService: IServiceProvider -> RemotingService) =
        this.Add(ServiceDescriptor(typedefof<IRemoteProvider<_>>, typedefof<ServerRemoteProvider<_>>, ServiceLifetime.Transient))
        this.AddSingleton<RemotingService>(getService)
            .AddSingleton<IRemoteContext, RemoteContext>()
            .AddHttpContextAccessor()

    static member private AddRemotingImpl<'T when 'T : not struct>(this: IServiceCollection, basePath: 'T -> PathString, handler: IRemoteContext -> 'T, configureSerialization: option<JsonSerializerOptions -> unit>) =
        ServerRemotingExtensions.AddRemotingImpl(this, fun services ->
            let ctx = services.GetRequiredService<IRemoteContext>()
            let handler = handler ctx
            let basePath = basePath handler
            RemotingService(basePath, typeof<'T>, handler, configureSerialization))

    /// <summary>Add a remote service at the given path.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="basePath">The base path under which the remote service is served.</param>
    /// <param name="handler">The function that builds the remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct>(this: IServiceCollection, basePath: PathString, handler: IRemoteContext -> 'T, ?configureSerialization) =
        ServerRemotingExtensions.AddRemotingImpl<'T>(this, (fun _ -> basePath), handler, configureSerialization)

    /// <summary>Add a remote service at the given path.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="basePath">The base path under which the remote service is served.</param>
    /// <param name="handler">The remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct>(this: IServiceCollection, basePath: PathString, handler: 'T, ?configureSerialization) =
        ServerRemotingExtensions.AddRemotingImpl<'T>(this, (fun _ -> basePath), (fun _ -> handler), configureSerialization)

    /// <summary>Add a remote service at the given path.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="basePath">The base path under which the remote service is served.</param>
    /// <param name="handler">The function that builds the remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct>(this: IServiceCollection, basePath: string, handler: IRemoteContext -> 'T, ?configureSerialization) =
        ServerRemotingExtensions.AddRemotingImpl<'T>(this, (fun _ -> PathString basePath), handler, configureSerialization)

    /// <summary>Add a remote service at the given path.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="basePath">The base path under which the remote service is served.</param>
    /// <param name="handler">The remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct>(this: IServiceCollection, basePath: string, handler: 'T, ?configureSerialization) =
        ServerRemotingExtensions.AddRemotingImpl<'T>(this, (fun _ -> PathString basePath), (fun _ -> handler), configureSerialization)

    /// <summary>Add a remote service.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="handler">The function that builds the remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct and 'T :> IRemoteService>(this: IServiceCollection, handler: IRemoteContext -> 'T, ?configureSerialization) =
        ServerRemotingExtensions.AddRemotingImpl<'T>(this, (fun h -> PathString h.BasePath), handler, configureSerialization)

    /// <summary>Add a remote service.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="handler">The function that builds the remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct and 'T :> IRemoteService>(this: IServiceCollection, handler: 'T, ?configureSerialization) =
        ServerRemotingExtensions.AddRemotingImpl<'T>(this, (fun h -> PathString h.BasePath), (fun _ -> handler), configureSerialization)

    /// <summary>Add a remote service using dependency injection.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct and 'T :> IRemoteHandler>(this: IServiceCollection, ?configureSerialization) =
        ServerRemotingExtensions.AddRemotingImpl(this.AddSingleton<'T>(), fun services ->
            let handler = services.GetRequiredService<'T>().Handler
            RemotingService(PathString handler.BasePath, handler.GetType(), handler, configureSerialization))

    /// <summary>Add the middleware that serves Bolero remote services.</summary>
    [<Extension>]
    static member UseRemoting(this: IApplicationBuilder) =
        let handlers =
            this.ApplicationServices.GetServices<RemotingService>()
            |> Array.ofSeq
        this.Use(fun ctx (next: Func<Task>) ->
            handlers
            |> Array.tryPick (fun h -> h.TryHandle(ctx))
            |> Option.defaultWith next.Invoke
        )
