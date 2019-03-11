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
open System.Threading.Tasks
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

type internal RemotingService(basePath: PathString, ty: System.Type, handler: obj) =

    let flags = BindingFlags.Public ||| BindingFlags.NonPublic
    let staticFlags = flags ||| BindingFlags.Static
    let instanceFlags = flags ||| BindingFlags.Instance

    let makeHandler (method: RemoteMethodDefinition) =
        let decoder = Json.GetDecoder method.ArgumentType
        let encoder = Json.GetEncoder method.ReturnType
        let meth = ty.GetProperty(method.Name).GetGetMethod().Invoke(handler, [||])
        let callMeth = method.FunctionType.GetMethod("Invoke", instanceFlags)
        let output = typeof<RemotingService>.GetMethod("Output", staticFlags)
        let output = output.MakeGenericMethod(method.ReturnType)
        fun (ctx: HttpContext) ->
            let arg =
                using (new StreamReader(ctx.Request.Body)) Json.Raw.Read
                |> decoder
            let res = callMeth.Invoke(meth, [|arg|])
            output.Invoke(null, [|ctx; encoder; res|]) :?> Task

    let methods =
        match RemotingExtensions.ExtractRemoteMethods ty with
        | Error errors ->
            raise <| AggregateException(
                "Cannot create remoting handler for type " + ty.FullName,
                [| for e in errors -> exn e |])
        | Ok methods ->
            dict [for m in methods -> m.Name, makeHandler m]

    static member Make<'T when 'T :> IRemoteService>(handler: 'T) =
        RemotingService(PathString handler.BasePath, typeof<'T>, handler)

    static member Output<'Out>(ctx: HttpContext, encoder: Json.Encoder<obj>, a: Async<'Out>) : Task =
        async {
            let! x = a
            let v = encoder x
            use writer = new StreamWriter(ctx.Response.Body)
            Json.Raw.Write writer v
        }
        |> Async.StartAsTask
        :> _

    member this.ServiceType = ty

    member this.Service = handler

    member this.TryHandle(ctx: HttpContext) : option<Task> =
        if ctx.Request.Method = "POST" && ctx.Request.Path.StartsWithSegments(basePath) then
            let reqPath = ctx.Request.Path.ToString()
            let basePath = basePath.ToString()
            let methodName = reqPath.Substring(basePath.Length).TrimStart('/')
            match methods.TryGetValue(methodName) with
            | true, handle -> Some(handle ctx)
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
        this.AddSingleton<RemotingService>(RemotingService(basePath, typeof<'T>, handler))
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
                RemotingService(PathString handler.BasePath, handler.GetType(), handler))
            .AddSingleton<IRemoteProvider, ServerRemoteProvider>()

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
