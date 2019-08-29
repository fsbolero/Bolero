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

/// Types inheriting from FSharpFunc used to sneak retrievable information into a value of type `a -> b`.
module internal RemoteInternals =

    type IWithContext =
        abstract Invoke : HttpContext -> obj

    type WithContext<'req, 'resp>(f: HttpContext -> 'req -> Async<'resp>) =
        inherit FSharp.Core.FSharpFunc<'req, Async<'resp>>()

        interface IWithContext with
            member this.Invoke ctx = box (f ctx)

        override this.Invoke(req) = f null req

    type IWithAuthorization =
        inherit IWithContext
        abstract AuthorizeData : seq<IAuthorizeData>

    type WithAuthorization<'req, 'resp>(authData: seq<IAuthorizeData>, f) =
        inherit WithContext<'req, 'resp>(f)

        interface IWithAuthorization with
            member this.AuthorizeData = authData

open RemoteInternals
open Microsoft.FSharp.Reflection

module Remote =

    /// Give a remote function access to its HttpContext.
    /// Must be used directly as the value of a remote function.
    let withContext (f: HttpContext -> 'req -> Async<'resp>) =
        WithContext(f) :> obj :?> ('req -> Async<'resp>)

    /// Mark a remote function as requiring authentication with the given policy.
    /// Must be used directly as the value of a remote function.
    let authorizeWith (authData: seq<IAuthorizeData>) (f: HttpContext -> 'req -> Async<'resp>) =
        WithAuthorization(authData, f) :> obj :?> ('req -> Async<'resp>)

    /// Mark a remote function as requiring authentication.
    /// Must be used directly as the value of a remote function.
    let authorize (f: HttpContext -> 'req -> Async<'resp>) =
        WithAuthorization([AuthorizeAttribute()], f) :> obj :?> ('req -> Async<'resp>)

type internal RemotingService(basePath: PathString, ty: System.Type, handler: obj, authPolicyProvider: IAuthorizationPolicyProvider) =

    let flags = BindingFlags.Public ||| BindingFlags.NonPublic
    let staticFlags = flags ||| BindingFlags.Static
    let instanceFlags = flags ||| BindingFlags.Instance

    static let fail (ctx: HttpContext) =
        ctx.Response.StatusCode <- StatusCodes.Status401Unauthorized
        // TODO: allow customizing based on what failed?

    let wrapHandler (method: RemoteMethodDefinition) =
        let meth = ty.GetProperty(method.Name).GetGetMethod().Invoke(handler, [||])
        let rec getMethodAndAuthPolicy (meth: obj) =
            match meth with
            | :? IWithAuthorization as wa ->
                if isNull authPolicyProvider then
                    failwithf "Remote method %s.%s is configured for authorization, \
                        but the application has no authorization policy. \
                        Add .AddAuthorization() to your server-side services."
                        ty.FullName method.Name
                else
                    let tAuthPolicy = AuthorizationPolicy.CombineAsync(authPolicyProvider, wa.AuthorizeData)
                    (wa :> IWithContext).Invoke, Some tAuthPolicy
            | :? IWithContext as wc ->
                wc.Invoke, None
            | _ ->
                // For some reason the WithContext returned by withContext and authorizeWith
                // is wrapped in a closure, so we need to retrieve it.
                meth.GetType().GetFields()
                |> Array.tryPick (fun field ->
                    if field.Name.StartsWith("clo") then
                        Some <| getMethodAndAuthPolicy (field.GetValue(meth))
                    else
                        None
                )
                |> Option.defaultValue ((fun _ -> meth), None)
        let getMeth, tAuthPolicy = getMethodAndAuthPolicy meth
        let callMeth = method.FunctionType.GetMethod("Invoke", instanceFlags)
        let getMeth ctx : (obj -> obj) =
            let meth = getMeth ctx
            fun arg ->
                printfn "%A" ctx
                callMeth.Invoke(meth, [|arg|])
        getMeth, tAuthPolicy

    let makeHandler (method: RemoteMethodDefinition) =
        let getMeth, tAuthPolicy = wrapHandler method
        let output =
            typeof<RemotingService>.GetMethod("Output", staticFlags)
                .MakeGenericMethod(method.ReturnType)
        let decoder = Json.GetDecoder method.ArgumentType
        let encoder = Json.GetEncoder method.ReturnType
        fun (auth: IAuthorizationService) (ctx: HttpContext) ->
            let meth = getMeth ctx
            let run() =
                task {
                    use reader = new StreamReader(ctx.Request.Body)
                    let! body = reader.ReadToEndAsync()
                    let arg = Json.Raw.Parse body |> decoder
                    let res = meth arg
                    return! output.Invoke(null, [|ctx; encoder; res|]) :?> Task
                } :> Task

            task {
                match tAuthPolicy with
                | None -> return! run()
                | Some tAuthPolicy ->
                    let! authPolicy = tAuthPolicy
                    let! authResult = auth.AuthorizeAsync(ctx.User, authPolicy)
                    if authResult.Succeeded then
                        return! run()
                    else
                        fail ctx
            }
            :> Task

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
        task {
            try
                let! x = a
                let v = encoder x
                let json = Json.Raw.Stringify v
                return! ctx.Response.WriteAsync(json, Encoding.UTF8)
            with RemoteUnauthorizedException ->
                fail ctx
        } :> Task

    static member UnwrapResult<'req, 'resp>(f: obj -> obj, ctx: HttpContext, auth: IAuthorizationService, authPolicy: option<Task<AuthorizationPolicy>>) : 'req -> Async<'resp> =
        fun req -> async {
            match authPolicy with
            | None ->
                return! unbox (f (box req))
            | Some tAuthPolicy ->
                let! authPolicy = tAuthPolicy |> Async.AwaitTask
                let! authResult = auth.AuthorizeAsync(ctx.User, authPolicy) |> Async.AwaitTask
                if authResult.Succeeded then
                    return! unbox (f (box req))
                else
                    return raise RemoteUnauthorizedException
        }

    member this.ServiceType = ty

    member this.GetService(ctx: HttpContext, auth: IAuthorizationService) =
        let args =
            methodData
            |> Array.map (fun method ->
                let getMeth, tAuthPolicy = wrapHandler method
                let call = getMeth ctx
                typeof<RemotingService>.GetMethod("UnwrapResult", staticFlags)
                    .MakeGenericMethod(method.ArgumentType, method.ReturnType)
                    .Invoke(null, [|call; ctx; auth; tAuthPolicy|])
            )
        FSharpValue.MakeRecord(ty, args)

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
type internal ServerRemoteProvider(services: seq<RemotingService>, ctxAccessor: IHttpContextAccessor, auth: IAuthorizationService) =

    member this.GetService<'T>() =
        services
        |> Seq.tryPick (fun s ->
            if s.ServiceType = typeof<'T> then
                Some (s.GetService(ctxAccessor.HttpContext, auth) :?> 'T)
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
                let authorizationPolicyProvider = services.GetService<IAuthorizationPolicyProvider>()
                RemotingService(PathString handler.BasePath, handler.GetType(), handler, authorizationPolicyProvider))
            .AddSingleton<IRemoteProvider, ServerRemoteProvider>()
            .AddHttpContextAccessor()

    [<Extension>]
    static member UseRemoting(this: IApplicationBuilder) =
        let handlers =
            this.ApplicationServices.GetServices<RemotingService>()
            |> Array.ofSeq
        let auth = this.ApplicationServices.GetService<IAuthorizationService>()
        this.Use(fun ctx next ->
            match handlers |> Array.tryPick (fun h -> h.TryHandle(ctx, auth)) with
            | Some t -> t
            | None -> next.Invoke()
        )
