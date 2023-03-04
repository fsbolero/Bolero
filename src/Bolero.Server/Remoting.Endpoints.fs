namespace Bolero.Remoting.Server

open System
open System.Collections
open System.Collections.Generic
open System.Reflection
open System.Runtime.CompilerServices
open System.Text.Json
open System.Threading
open Bolero.Remoting
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Metadata
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Routing.Patterns
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Primitives

type internal IAuthorizedMethodHandler =
    abstract AuthorizeData: array<IAuthorizeData>

type internal AuthorizedMethodHandler<'req, 'resp>(http: IHttpContextAccessor, authService: IAuthorizationService, authPolicyProvider: IAuthorizationPolicyProvider, authData: seq<IAuthorizeData>, f: 'req -> Async<'resp>) =
    inherit FSharp.Core.FSharpFunc<'req, Async<'resp>>()

    let tAuthPolicy = AuthorizationPolicy.CombineAsync(authPolicyProvider, authData)
    let handle = RemoteContext.Handle<'req, 'resp>(tAuthPolicy, authService, http, f)

    override this.Invoke(req) =
        handle req

    interface IAuthorizedMethodHandler with
        member this.AuthorizeData =
            Array.ofSeq authData

type EndpointsRemoteContext(http: IHttpContextAccessor, authService: IAuthorizationService, authPolicyProvider: IAuthorizationPolicyProvider) =

    member this.AuthorizeWith<'req, 'resp>(authData, f) =
        box (AuthorizedMethodHandler<'req, 'resp>(http, authService, authPolicyProvider, authData, f))

    interface IHttpContextAccessor with
        member _.HttpContext with get () = http.HttpContext and set v = http.HttpContext <- v

    interface IRemoteContext with
        member this.Authorize<'req,'resp> f = unbox (this.AuthorizeWith<'req, 'resp>([AuthorizeAttribute()], f))
        member this.AuthorizeWith<'req,'resp> authData f = unbox (this.AuthorizeWith<'req,'resp>(authData, f))

type RemoteParameterInfo(memberInfo, method: IRemoteMethodMetadata) =
    inherit ParameterInfo()
    override _.Name = "body"
    override _.Member = memberInfo
    override _.ParameterType = method.ArgumentType
    override _.HasDefaultValue = false
    override _.DefaultValue = null
    override _.GetCustomAttributesData() =
        [| { new CustomAttributeData() with
               override _.Constructor = typeof<FromBodyAttribute>.GetConstructor([||]) } |]

type RemoteMethodInfo(method: IRemoteMethodMetadata) =
    inherit MethodInfo()
    override this.GetBaseDefinition() = null
    override this.ReturnTypeCustomAttributes = null
    override this.ReturnType = method.ReturnType
    override this.GetMethodImplementationFlags() = MethodImplAttributes.Managed
    override this.GetParameters() = [| RemoteParameterInfo(this, method) |]
    override this.Invoke(_, _, _, _, _) = raise (NotImplementedException())
    override this.Attributes = MethodAttributes.Static ||| MethodAttributes.Public
    override this.MethodHandle = raise (NotImplementedException())
    override this.GetCustomAttributes(_) = [||]
    override this.GetCustomAttributes(_, _) = [||]
    override this.GetCustomAttributesData() = [||]
    override this.IsDefined(_, _) = raise (NotImplementedException())
    override this.DeclaringType = method.Service.Type
    override this.Name = method.Name
    override this.ReflectedType = null

type internal RemotingServiceEndpointBuilder(service: RemotingService, buildEndpoint: (IRemoteMethodMetadata ->  IEndpointConventionBuilder -> unit) option) =

    let endpoints =
        service.Methods
        |> Seq.map (fun (KeyValue(methodName, method)) ->
            let path = RoutePatternFactory.Parse $"{service.BasePath}/{methodName}"
            let endpoint = RouteEndpointBuilder(method.Handler, path, 0)
            endpoint.DisplayName <- $"Remote method {method.Name} on service {method.Service.Type.Name}"
            ([ { new IAcceptsMetadata with
                   member _.ContentTypes = ["application/json"]
                   member _.RequestType = method.ArgumentType
                   member _.IsOptional = false }
               { new IProducesResponseTypeMetadata with
                   member _.ContentTypes = ["application/json"]
                   member _.StatusCode = 200
                   member _.Type = method.ReturnType }
               RemoteMethodInfo(method)
               method
               { new IHttpMethodMetadata with
                   member _.AcceptCorsPreflight = true
                   member _.HttpMethods = ["POST"] } ] : obj list)
            |> List.iter endpoint.Metadata.Add

            match method.Function with
            | :? IAuthorizedMethodHandler as handler ->
                handler.AuthorizeData
                |> Seq.iter endpoint.Metadata.Add
            | _ -> ()

            buildEndpoint |> Option.iter (fun buildEndpoint ->
                { new IEndpointConventionBuilder with
                    member _.Add(f) = f.Invoke(endpoint) }
                |> buildEndpoint method)

            endpoint
        )
        |> Array.ofSeq

    let finally' = ResizeArray()

    member _.ServiceType = service.ServiceType

    member _.EndpointBuilders = endpoints

    member _.Finally(f: Action<EndpointBuilder>) =
        finally'.Add(f)

    member _.ApplyFinally() =
        for f in finally' do
            for endpoint in endpoints do
                f.Invoke(endpoint)
        finally'.Clear()
        endpoints

type internal RemotingEndpointDataSource() =
    inherit EndpointDataSource()

    let lock_ = obj()

    let endpointBuilders = ResizeArray<RemotingServiceEndpointBuilder>()

    let mutable cancellationTokenSource = new CancellationTokenSource()

    let mutable changeToken = CancellationChangeToken(cancellationTokenSource.Token)

    let withLock f =
        lock lock_ <| fun () ->
            let oldCancellationTokenSource = cancellationTokenSource
            let res = f()
            cancellationTokenSource <- new CancellationTokenSource()
            changeToken <- CancellationChangeToken(cancellationTokenSource.Token)
            oldCancellationTokenSource.Cancel()
            res

    member _.AddService(service: RemotingService, buildEndpoint: (IRemoteMethodMetadata ->  IEndpointConventionBuilder -> unit) option) =
        withLock <| fun () ->
            endpointBuilders.RemoveAll(fun b -> b.ServiceType = service.ServiceType) |> ignore
            let endpointBuilder = RemotingServiceEndpointBuilder(service, buildEndpoint)
            endpointBuilders.Add(endpointBuilder)
            { new IEndpointConventionBuilder with
                member _.Add(f) = Seq.iter f.Invoke endpointBuilder.EndpointBuilders
#if NET7_0_OR_GREATER
                member _.Finally(f) = endpointBuilder.Finally(f)
#endif
            }

    member _.AddServicesIfNotAlreadyAdded(services: seq<RemotingService>, buildEndpoint: (IRemoteMethodMetadata ->  IEndpointConventionBuilder -> unit) option) =
        withLock <| fun () ->
            let builders =
                services
                |> Seq.filter (fun service -> not (endpointBuilders.Exists(fun b -> b.ServiceType = service.ServiceType)))
                |> Seq.map (fun service -> RemotingServiceEndpointBuilder(service, buildEndpoint))
                |> Array.ofSeq
            Seq.iter endpointBuilders.Add builders
            { new IEndpointConventionBuilder with
                member _.Add(f) = builders |> Seq.iter (fun b -> Seq.iter f.Invoke b.EndpointBuilders)
#if NET7_0_OR_GREATER
                member _.Finally(f) = builders |> Seq.iter (fun b -> b.Finally(f))
#endif
            }

    static member Get(endpoints: IEndpointRouteBuilder) =
        endpoints.DataSources
        |> Seq.tryPick tryUnbox<RemotingEndpointDataSource>
        |> Option.defaultWith (fun () ->
            let dataSource = RemotingEndpointDataSource()
            endpoints.DataSources.Add(dataSource)
            dataSource)

    override _.GetChangeToken() = changeToken

    override _.Endpoints =
        endpointBuilders
        |> Seq.collect (fun b -> b.ApplyFinally())
        |> Seq.map (fun b -> b.Build())
        |> Array.ofSeq
        :> IReadOnlyList<Endpoint>


/// Extension methods to enable support for remoting in the ASP.NET Core server side.
[<Extension>]
type EndpointsRemotingExtensions =

    static member private AddBoleroRemotingImpl(this: IServiceCollection, getService: IServiceProvider -> RemotingService) =
        this.Add(ServiceDescriptor(typedefof<IRemoteProvider<_>>, typedefof<ServerRemoteProvider<_>>, ServiceLifetime.Transient))
        this.AddSingleton<RemotingService>(getService)
            .AddSingleton<IRemoteContext, EndpointsRemoteContext>()
            .AddHttpContextAccessor()

    static member private AddBoleroRemotingImpl<'T when 'T : not struct>(this: IServiceCollection, basePath: 'T -> PathString, handler: IRemoteContext -> 'T, configureSerialization: option<JsonSerializerOptions -> unit>) =
        EndpointsRemotingExtensions.AddBoleroRemotingImpl(this, fun services ->
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
    static member AddBoleroRemoting<'T when 'T : not struct>(this: IServiceCollection, basePath: PathString, handler: IRemoteContext -> 'T, ?configureSerialization) =
        EndpointsRemotingExtensions.AddBoleroRemotingImpl<'T>(this, (fun _ -> basePath), handler, configureSerialization)

    /// <summary>Add a remote service at the given path.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="basePath">The base path under which the remote service is served.</param>
    /// <param name="handler">The remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddBoleroRemoting<'T when 'T : not struct>(this: IServiceCollection, basePath: PathString, handler: 'T, ?configureSerialization) =
        EndpointsRemotingExtensions.AddBoleroRemotingImpl<'T>(this, (fun _ -> basePath), (fun _ -> handler), configureSerialization)

    /// <summary>Add a remote service at the given path.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="basePath">The base path under which the remote service is served.</param>
    /// <param name="handler">The function that builds the remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddBoleroRemoting<'T when 'T : not struct>(this: IServiceCollection, basePath: string, handler: IRemoteContext -> 'T, ?configureSerialization) =
        EndpointsRemotingExtensions.AddBoleroRemotingImpl<'T>(this, (fun _ -> PathString basePath), handler, configureSerialization)

    /// <summary>Add a remote service at the given path.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="basePath">The base path under which the remote service is served.</param>
    /// <param name="handler">The remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddBoleroRemoting<'T when 'T : not struct>(this: IServiceCollection, basePath: string, handler: 'T, ?configureSerialization) =
        EndpointsRemotingExtensions.AddBoleroRemotingImpl<'T>(this, (fun _ -> PathString basePath), (fun _ -> handler), configureSerialization)

    /// <summary>Add a remote service.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="handler">The function that builds the remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddBoleroRemoting<'T when 'T : not struct and 'T :> IRemoteService>(this: IServiceCollection, handler: IRemoteContext -> 'T, ?configureSerialization) =
        EndpointsRemotingExtensions.AddBoleroRemotingImpl<'T>(this, (fun h -> PathString h.BasePath), handler, configureSerialization)

    /// <summary>Add a remote service.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="handler">The function that builds the remote service.</param>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddBoleroRemoting<'T when 'T : not struct and 'T :> IRemoteService>(this: IServiceCollection, handler: 'T, ?configureSerialization) =
        EndpointsRemotingExtensions.AddBoleroRemotingImpl<'T>(this, (fun h -> PathString h.BasePath), (fun _ -> handler), configureSerialization)

    /// <summary>Add a remote service using dependency injection.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="configureSerialization">Configure the JSON serialization of request and response values.</param>
    [<Extension>]
    static member AddBoleroRemoting<'T when 'T : not struct and 'T :> IRemoteHandler>(this: IServiceCollection, ?configureSerialization) =
        EndpointsRemotingExtensions.AddBoleroRemotingImpl(this.AddSingleton<'T>(), fun services ->
            let handler = services.GetRequiredService<'T>().Handler
            RemotingService(PathString handler.BasePath, handler.GetType(), handler, configureSerialization))

    /// <summary>Serve Bolero remote services.</summary>
    [<Extension>]
    static member MapBoleroRemoting<'T>(endpoints: IEndpointRouteBuilder, ?buildEndpoint: IRemoteMethodMetadata ->  IEndpointConventionBuilder -> unit) =
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
    static member MapBoleroRemoting(endpoints: IEndpointRouteBuilder, ?buildEndpoint: IRemoteMethodMetadata ->  IEndpointConventionBuilder -> unit) =
        let services = endpoints.ServiceProvider.GetServices<RemotingService>()
        RemotingEndpointDataSource.Get(endpoints)
            .AddServicesIfNotAlreadyAdded(services, buildEndpoint)
