namespace Bolero.Remoting.Server

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Text.Json
open System.Threading.Tasks
open Bolero.Remoting
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.DependencyInjection

[<AutoOpen>]
module private ServerRemotingExtensionsHelpers =
    let convertOptionalAction (f: option<_ -> unit>) =
        match f with
        | None -> null
        | Some f -> Action<_> f

/// Extension methods to enable support for remoting in the ASP.NET Core server side.
[<Extension>]
type ServerRemotingExtensions =

    static member private AddBoleroRemotingImpl(this: IServiceCollection, getService: IServiceProvider -> RemotingService) =
        this.Add(ServiceDescriptor(typedefof<IRemoteProvider<_>>, typedefof<ServerRemoteProvider<_>>, ServiceLifetime.Transient))
        this.AddSingleton<RemotingService>(getService)
            .AddSingleton<IRemoteContext, RemoteContext>()
            .AddHttpContextAccessor()

    static member private AddBoleroRemotingImpl<'T when 'T : not struct>(
            this: IServiceCollection,
            basePath: Func<'T, PathString>,
            handler: Func<IRemoteContext, 'T>,
            configureSerialization: Action<JsonSerializerOptions>
        ) =
        ServerRemotingExtensions.AddBoleroRemotingImpl(this, fun services ->
            let ctx = services.GetRequiredService<IRemoteContext>()
            let handler = handler.Invoke(ctx)
            let basePath = basePath.Invoke(handler)
            RemotingService(basePath, typeof<'T>, handler, configureSerialization))

    /// <summary>Add a remote service at the given path.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="this">The DI service collection.</param>
    /// <param name="basePath">The base path under which the remote service is served.</param>
    /// <param name="handler">The function that builds the remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddBoleroRemoting<'T when 'T : not struct>(
            this: IServiceCollection,
            basePath: PathString,
            handler: Func<IRemoteContext, 'T>,
            [<Optional>] configureSerialization: Action<JsonSerializerOptions>
        ) =
        ServerRemotingExtensions.AddBoleroRemotingImpl<'T>(this, (fun _ -> basePath), handler, configureSerialization)

    /// <exclude />
    [<Extension; Obsolete "Use AddBoleroRemoting">]
    static member AddRemoting<'T when 'T : not struct>(
            this: IServiceCollection,
            basePath: PathString,
            handler: IRemoteContext -> 'T,
            ?configureSerialization: JsonSerializerOptions -> unit
        ) =
        this.AddBoleroRemoting<'T>(basePath, handler, convertOptionalAction configureSerialization)

    /// <summary>Add a remote service at the given path.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="this">The DI service collection.</param>
    /// <param name="basePath">The base path under which the remote service is served.</param>
    /// <param name="handler">The remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddBoleroRemoting<'T when 'T : not struct>(
            this: IServiceCollection,
            basePath: PathString,
            handler: 'T,
            [<Optional>] configureSerialization: Action<JsonSerializerOptions>
        ) =
        ServerRemotingExtensions.AddBoleroRemotingImpl<'T>(this, (fun _ -> basePath), (fun _ -> handler), configureSerialization)

    /// <exclude />
    [<Extension; Obsolete "Use AddBoleroRemoting">]
    static member AddRemoting<'T when 'T : not struct>(
            this: IServiceCollection,
            basePath: PathString,
            handler: 'T,
            ?configureSerialization: JsonSerializerOptions -> unit
        ) =
        this.AddBoleroRemoting<'T>(basePath, handler, convertOptionalAction configureSerialization)

    /// <summary>Add a remote service at the given path.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="this">The DI service collection.</param>
    /// <param name="basePath">The base path under which the remote service is served.</param>
    /// <param name="handler">The function that builds the remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddBoleroRemoting<'T when 'T : not struct>(
            this: IServiceCollection,
            basePath: string,
            handler: Func<IRemoteContext, 'T>,
            [<Optional>] configureSerialization: Action<JsonSerializerOptions>
        ) =
        ServerRemotingExtensions.AddBoleroRemotingImpl<'T>(this, (fun _ -> PathString basePath), handler, configureSerialization)

    /// <exclude />
    [<Extension; Obsolete "Use AddBoleroRemoting">]
    static member AddRemoting<'T when 'T : not struct>(
            this: IServiceCollection,
            basePath: string,
            handler: IRemoteContext -> 'T,
            ?configureSerialization: JsonSerializerOptions -> unit
        ) =
        this.AddBoleroRemoting<'T>(basePath, handler, convertOptionalAction configureSerialization)

    /// <summary>Add a remote service at the given path.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="this">The DI service collection.</param>
    /// <param name="basePath">The base path under which the remote service is served.</param>
    /// <param name="handler">The remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddBoleroRemoting<'T when 'T : not struct>(
            this: IServiceCollection,
            basePath: string,
            handler: 'T,
            configureSerialization: Action<JsonSerializerOptions>
        ) =
        ServerRemotingExtensions.AddBoleroRemotingImpl<'T>(this, (fun _ -> PathString basePath), (fun _ -> handler), configureSerialization)

    /// <exclude />
    [<Extension; Obsolete "Use AddBoleroRemoting">]
    static member AddRemoting<'T when 'T : not struct>(
            this: IServiceCollection,
            basePath: string,
            handler: 'T,
            ?configureSerialization: JsonSerializerOptions -> unit
        ) =
        this.AddBoleroRemoting<'T>(basePath, handler, convertOptionalAction configureSerialization)

    /// <summary>Add a remote service.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="this">The DI service collection.</param>
    /// <param name="handler">The function that builds the remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddBoleroRemoting<'T when 'T : not struct and 'T :> IRemoteService>(
            this: IServiceCollection,
            handler: Func<IRemoteContext, 'T>,
            [<Optional>] configureSerialization: Action<JsonSerializerOptions>
        ) =
        ServerRemotingExtensions.AddBoleroRemotingImpl<'T>(this, (fun h -> PathString h.BasePath), handler, configureSerialization)

    /// <exclude />
    [<Extension; Obsolete "Use AddBoleroRemoting">]
    static member AddRemoting<'T when 'T : not struct and 'T :> IRemoteService>(
            this: IServiceCollection,
            handler: IRemoteContext -> 'T,
            ?configureSerialization: JsonSerializerOptions -> unit
        ) =
        this.AddBoleroRemoting<'T>(handler, convertOptionalAction configureSerialization)

    /// <summary>Add a remote service.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="this">The DI service collection.</param>
    /// <param name="handler">The function that builds the remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddBoleroRemoting<'T when 'T : not struct and 'T :> IRemoteService>(
            this: IServiceCollection,
            handler: 'T,
            [<Optional>] configureSerialization: Action<JsonSerializerOptions>
        ) =
        ServerRemotingExtensions.AddBoleroRemotingImpl<'T>(this, (fun h -> PathString h.BasePath), (fun _ -> handler), configureSerialization)

    /// <exclude />
    [<Extension; Obsolete "Use AddBoleroRemoting">]
    static member AddRemoting<'T when 'T : not struct and 'T :> IRemoteService>(
            this: IServiceCollection,
            handler: 'T,
            ?configureSerialization: JsonSerializerOptions -> unit
        ) =
        this.AddBoleroRemoting<'T>(handler, convertOptionalAction configureSerialization)

    /// <summary>Add a remote service using dependency injection.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="this">The DI service collection.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddBoleroRemoting<'T when 'T : not struct and 'T :> IRemoteHandler>(
            this: IServiceCollection,
            [<Optional>] configureSerialization: Action<JsonSerializerOptions>
        ) =
        ServerRemotingExtensions.AddBoleroRemotingImpl(this.AddSingleton<'T>(), fun services ->
            let handler = services.GetRequiredService<'T>().Handler
            RemotingService(PathString handler.BasePath, handler.GetType(), handler, configureSerialization))

    /// <exclude />
    [<Extension; Obsolete "Use AddBoleroRemoting">]
    static member AddRemoting<'T when 'T : not struct and 'T :> IRemoteHandler>(
            this: IServiceCollection,
            ?configureSerialization: JsonSerializerOptions -> unit
        ) =
        this.AddBoleroRemoting<'T>(convertOptionalAction configureSerialization)

    /// <summary>Serve Bolero remote services.</summary>
    [<Extension>]
    static member MapBoleroRemoting<'T>(
            endpoints: IEndpointRouteBuilder,
            [<Optional>] buildEndpoint: Action<IRemoteMethodMetadata, IEndpointConventionBuilder>
        ) =
        match
            endpoints.ServiceProvider.GetServices<RemotingService>()
            |> Seq.tryFind (fun service -> service.ServiceType = typeof<'T>)
        with
        | None ->
            failwith $"\
                Remote service not registered: {typeof<'T>.FullName}. \
                Use services.AddBoleroRemoting<{typeof<'T>.Name}>() to register it."
        | Some service ->
            RemotingEndpointDataSource.Get(endpoints)
                .AddService(service, buildEndpoint)

    /// <summary>Serve Bolero remote services.</summary>
    [<Extension>]
    static member MapBoleroRemoting(
            endpoints: IEndpointRouteBuilder,
            [<Optional>] buildEndpoint: Action<IRemoteMethodMetadata, IEndpointConventionBuilder>
        ) =
        let services = endpoints.ServiceProvider.GetServices<RemotingService>()
        RemotingEndpointDataSource.Get(endpoints)
            .AddServicesIfNotAlreadyAdded(services, buildEndpoint)

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
