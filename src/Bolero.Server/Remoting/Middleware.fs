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
open System.Runtime.CompilerServices
open System.Text.Json
open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Bolero.Remoting
open Microsoft.Extensions.DependencyInjection

type internal MiddlewareRemoteContext(http: IHttpContextAccessor, authService: IAuthorizationService, authPolicyProvider: IAuthorizationPolicyProvider) =
    inherit RemoteContext(http)

    override this.AuthorizeWith<'req, 'resp>(authData: seq<IAuthorizeData>, f: 'req -> Async<'resp>) =
        let tAuthPolicy = AuthorizationPolicy.CombineAsync(authPolicyProvider, authData)
        box (fun req -> async {
            let! authPolicy = tAuthPolicy |> Async.AwaitTask
            let! authResult = authService.AuthorizeAsync(http.HttpContext.User, authPolicy) |> Async.AwaitTask
            if authResult.Succeeded then
                return! f req
            else
                return raise RemoteUnauthorizedException
        })

[<AutoOpen>]
module private MiddlewareRemotingExtensionsHelpers =

    let convertOptionalAction (f: option<_ -> unit>) =
        match f with
        | None -> null
        | Some f -> Action<_> f

    let addRemoting (services: IServiceCollection) (getService: IServiceProvider -> RemotingService) =
        services.Add(ServiceDescriptor(typedefof<IRemoteProvider<_>>, typedefof<ServerRemoteProvider<_>>, ServiceLifetime.Transient))
        services.AddSingleton<RemotingService>(getService)
                .AddSingleton<IRemoteContext, MiddlewareRemoteContext>()
                .AddHttpContextAccessor()

    let addTypedRemoting<'T>
            (services: IServiceCollection)
            (basePath: 'T -> PathString)
            (handler: IRemoteContext -> 'T)
            (configureSerialization: (JsonSerializerOptions -> unit) option) =
        addRemoting services (fun services ->
            let ctx = services.GetRequiredService<IRemoteContext>()
            let handler = handler ctx
            let basePath = basePath handler
            let configureSerialization = convertOptionalAction configureSerialization
            RemotingService(basePath, typeof<'T>, handler, configureSerialization))

/// Extension methods to enable support for remoting in the ASP.NET Core server side using the remoting middleware.
[<Extension>]
type MiddlewareRemotingExtensions =

    /// <summary>Add a remote service at the given path.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="this">The DI service collection.</param>
    /// <param name="basePath">The base path under which the remote service is served.</param>
    /// <param name="handler">The function that builds the remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    /// <remarks>
    /// Use this method to enable <c>app.UseRemoting()</c>.
    /// Use <c>AddBoleroRemoting()</c> instead to enable <c>endpoints.MapBoleroRemoting()</c>.
    /// </remarks>
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct>(
            this: IServiceCollection,
            basePath: PathString,
            handler: IRemoteContext -> 'T,
            ?configureSerialization: JsonSerializerOptions -> unit
        ) =
        addTypedRemoting<'T> this (fun _ -> basePath) handler configureSerialization

    /// <summary>Add a remote service at the given path.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="this">The DI service collection.</param>
    /// <param name="basePath">The base path under which the remote service is served.</param>
    /// <param name="handler">The remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    /// <remarks>
    /// Use this method to enable <c>app.UseRemoting()</c>.
    /// Use <c>AddBoleroRemoting()</c> instead to enable <c>endpoints.MapBoleroRemoting()</c>.
    /// </remarks>
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct>(
            this: IServiceCollection,
            basePath: PathString,
            handler: 'T,
            ?configureSerialization: JsonSerializerOptions -> unit
        ) =
        addTypedRemoting<'T> this (fun _ -> basePath) (fun _ -> handler) configureSerialization

    /// <summary>Add a remote service at the given path.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="this">The DI service collection.</param>
    /// <param name="basePath">The base path under which the remote service is served.</param>
    /// <param name="handler">The function that builds the remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    /// <remarks>
    /// Use this method to enable <c>app.UseRemoting()</c>.
    /// Use <c>AddBoleroRemoting()</c> instead to enable <c>endpoints.MapBoleroRemoting()</c>.
    /// </remarks>
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct>(
            this: IServiceCollection,
            basePath: string,
            handler: IRemoteContext -> 'T,
            ?configureSerialization: JsonSerializerOptions -> unit
        ) =
        addTypedRemoting<'T> this (fun _ -> PathString basePath) handler configureSerialization

    /// <summary>Add a remote service at the given path.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="this">The DI service collection.</param>
    /// <param name="basePath">The base path under which the remote service is served.</param>
    /// <param name="handler">The remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    /// <remarks>
    /// Use this method to enable <c>app.UseRemoting()</c>.
    /// Use <c>AddBoleroRemoting()</c> instead to enable <c>endpoints.MapBoleroRemoting()</c>.
    /// </remarks>
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct>(
            this: IServiceCollection,
            basePath: string,
            handler: 'T,
            ?configureSerialization: JsonSerializerOptions -> unit
        ) =
        addTypedRemoting<'T> this (fun _ -> PathString basePath) (fun _ -> handler) configureSerialization

    /// <summary>Add a remote service.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="this">The DI service collection.</param>
    /// <param name="handler">The function that builds the remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    /// <remarks>
    /// Use this method to enable <c>app.UseRemoting()</c>.
    /// Use <c>AddBoleroRemoting()</c> instead to enable <c>endpoints.MapBoleroRemoting()</c>.
    /// </remarks>
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct and 'T :> IRemoteService>(
            this: IServiceCollection,
            handler: IRemoteContext -> 'T,
            ?configureSerialization: JsonSerializerOptions -> unit
        ) =
        addTypedRemoting<'T> this (fun h -> PathString h.BasePath) handler configureSerialization

    /// <summary>Add a remote service.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="this">The DI service collection.</param>
    /// <param name="handler">The function that builds the remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    /// <remarks>
    /// Use this method to enable <c>app.UseRemoting()</c>.
    /// Use <c>AddBoleroRemoting()</c> instead to enable <c>endpoints.MapBoleroRemoting()</c>.
    /// </remarks>
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct and 'T :> IRemoteService>(
            this: IServiceCollection,
            handler: 'T,
            ?configureSerialization: JsonSerializerOptions -> unit
        ) =
        addTypedRemoting<'T> this (fun h -> PathString h.BasePath) (fun _ -> handler) configureSerialization

    /// <summary>Add a remote service using dependency injection.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="this">The DI service collection.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    /// <remarks>
    /// Use this method to enable <c>app.UseRemoting()</c>.
    /// Use <c>AddBoleroRemoting()</c> instead to enable <c>endpoints.MapBoleroRemoting()</c>.
    /// </remarks>
    [<Extension>]
    static member AddRemoting<'T when 'T : not struct and 'T :> IRemoteHandler>(
            this: IServiceCollection,
            ?configureSerialization: JsonSerializerOptions -> unit
        ) =
        addRemoting (this.AddSingleton<'T>()) (fun services ->
            let handler = services.GetRequiredService<'T>().Handler
            RemotingService(PathString handler.BasePath, handler.GetType(), handler, convertOptionalAction configureSerialization))

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
