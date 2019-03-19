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
open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open FSharp.Reflection
open Bolero

exception RemoteUnauthorizedException with
    override this.Message = "Unauthorized remote operation"

/// Indicate that this type is a remote service, served at the given base URL path.
type IRemoteService =
    abstract BasePath : string

/// Provides remote service implementations.
type IRemoteProvider =
    abstract GetService<'T> : basePath: string -> 'T
    abstract GetService<'T when 'T :> IRemoteService> : unit -> 'T

type RemoteMethodDefinition =
    {
        Name: string
        FunctionType: Type
        ArgumentType: Type
        ReturnType: Type
    }

[<Extension>]
type RemotingExtensions =

    [<Extension>]
    static member RemoteProvider(this: IProgramComponent) =
        this.Services.GetRequiredService<IRemoteProvider>()

    /// Get an instance of the given remote service, whose URL has the given base path.
    [<Extension>]
    static member Remote<'T>(this: IProgramComponent, basePath: string) =
        this.RemoteProvider().GetService<'T>(basePath)

    /// Get an instance of the given remote service.
    [<Extension>]
    static member Remote<'T when 'T :> IRemoteService>(this: IProgramComponent) =
        this.RemoteProvider().GetService<'T>()

    static member ExtractRemoteMethods(ty: Type) : Result<RemoteMethodDefinition[], list<string>> =
        if not (FSharpType.IsRecord ty) then
            Error [sprintf "Remote type must be a record: %s" ty.FullName]
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
