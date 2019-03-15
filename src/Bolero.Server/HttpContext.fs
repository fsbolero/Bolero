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

open System.Runtime.CompilerServices
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open System.Security.Claims
open System

[<Extension>]
type HttpContextExtensions private () =

    static let [<Literal>] DefaultAuthenticationType = "Bolero.Remoting"

    /// Sign in a user with the given name.
    [<Extension>]
    static member AsyncSignIn
        (
            this: HttpContext,
            name: string,
            ?persistFor: TimeSpan,
            ?claims: seq<Claim>,
            ?properties: AuthenticationProperties,
            ?authenticationType: string
        ) =
        let properties = properties |> Option.defaultWith AuthenticationProperties
        persistFor |> Option.iter (fun persistFor ->
            properties.IsPersistent <- true
            properties.ExpiresUtc <- Nullable(DateTimeOffset.UtcNow.Add(persistFor))
        )
        let authenticationType = defaultArg authenticationType DefaultAuthenticationType
        let claims = Seq.append [Claim(ClaimTypes.Name, name)] (defaultArg claims Seq.empty)
        let principal = ClaimsPrincipal(ClaimsIdentity(claims, authenticationType))
        this.SignInAsync(principal, properties)
        |> Async.AwaitTask

    /// Sign out a user.
    [<Extension>]
    static member AsyncSignOut(this: HttpContext, ?properties: AuthenticationProperties) =
        match properties with
        | None -> this.SignOutAsync()
        | Some properties -> this.SignOutAsync(properties)
        |> Async.AwaitTask
