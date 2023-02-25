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

namespace Bolero.Remoting

open System
open System.Net.Http
open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open FSharp.Reflection
open Bolero

/// <summary>Exception thrown when a remote function fails to authorize a call.</summary>
exception RemoteUnauthorizedException with
    override this.Message = "Unauthorized remote operation"

/// <summary>Exception thrown on the client when a remote call fails.</summary>
exception RemoteException of HttpResponseMessage

/// <summary>Indicate that this type is a remote service, served at the given base URL path.</summary>
/// <remarks>
/// The type must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.
/// </remarks>
type IRemoteService =
    abstract BasePath : string

/// <summary>Provides a remote service implementation.</summary>
/// <typeparam name="Service">The remote service.</typeparam>
type IRemoteProvider<'Service> =
    /// <summary>Get the remote service of the given type, whose URL has the given base path.</summary>
    /// <typeparam name="T">The remote service type. Must be a record whose fields are functions of the form <c>Request -&gt; Async&lt;Response&gt;</c>.</typeparam>
    /// <param name="getBasePath">The base URL path.</param>
    /// <returns>The remote service.</returns>
    abstract GetService : getBasePath: ('Service -> string) -> 'Service

/// <exclude />
type RemoteMethodDefinition =
    {
        Name: string
        FunctionType: Type
        ArgumentType: Type
        ReturnType: Type
    }

/// <summary>Extension methods to retrieve remote services from a program component.</summary>
[<Extension>]
type RemotingExtensions =

    /// <exclude />
    [<Extension>]
    static member RemoteProvider<'T>(this: IProgramComponent) =
        this.Services.GetRequiredService<IRemoteProvider<'T>>()

    /// <summary>Get an instance of the given remote service, whose URL has the given base path.</summary>
    /// <typeparam name="T">The remote service type.</typeparam>
    /// <param name="basePath">The base URL path.</param>
    /// <returns>The remote service.</returns>
    [<Extension>]
    static member Remote<'T>(this: IProgramComponent, basePath: string) =
        this.RemoteProvider<'T>().GetService(fun _ -> basePath)

    /// <summary>
    /// Get an instance of the given remote service, whose URL is determined by its <see cref="T:IRemoteService" /> implementation.
    /// </summary>
    /// <typeparam name="T">The remote service type.</typeparam>
    /// <returns>The remote service.</returns>
    [<Extension>]
    static member Remote<'T when 'T :> IRemoteService>(this: IProgramComponent) =
        this.RemoteProvider<'T>().GetService(fun x -> x.BasePath)

    /// <exclude />
    static member ExtractRemoteMethods(ty: Type) : Result<RemoteMethodDefinition[], list<string>> =
        if not (FSharpType.IsRecord ty) then
            Error [$"Remote type must be a record: {ty.FullName}"]
        else
        let fields = FSharpType.GetRecordFields(ty, true)
        (fields, Ok [])
        ||> Array.foldBack (fun field res ->
            let fail fmt =
                let msg = sprintf fmt (ty.FullName + "." + field.Name)
                match res with
                | Ok _ -> Error [msg]
                | Error e -> Error (msg :: e)
            let ok x =
                res |> Result.map (fun l -> x :: l)
            if not (FSharpType.IsFunction field.PropertyType) then
                fail "Remote type field must be an F# function: %s"
            else
            let argTy, resTy = FSharpType.GetFunctionElements(field.PropertyType)
            if FSharpType.IsFunction resTy then
                fail "Remote type field must be an F# function with only one argument: %s. Use a tuple if several arguments are needed"
            elif not (resTy.IsGenericType && resTy.GetGenericTypeDefinition() = typedefof<Async<_>>) then
                fail "Remote function must return Async<_>: %s"
            else
            let resValueTy = resTy.GetGenericArguments().[0]
            ok {
                Name = field.Name
                FunctionType = field.PropertyType
                ArgumentType = argTy
                ReturnType = resValueTy
            }
        )
        |> Result.map Array.ofList
