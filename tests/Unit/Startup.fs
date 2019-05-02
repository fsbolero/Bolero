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

// ASP.NET Core and Blazor startup for web tests.
namespace Bolero.Tests.Web

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Authentication.Cookies
open Bolero.Remoting.Server
open System.Security.Claims
open Microsoft.AspNetCore.Authorization
open Bolero.Tests

type Startup() =

    let mutable items = Map.empty

    let remoteHandler : Client.Remoting.RemoteApi =
        {
            getValue = fun k -> async {
                return Map.tryFind k items
            }
            setValue = fun (k, v) -> async {
                items <- Map.add k v items
            }
            removeValue = fun k -> async {
                items <- Map.remove k items
            }
            signIn = Remote.withContext <| fun http username -> async {
                let claims =
                    match username with
                    | "admin" -> [Claim(ClaimTypes.Role, "admin")]
                    | _ -> []
                return! http.AsyncSignIn(username, claims = claims)
            }
            signOut = Remote.withContext <| fun http () -> async {
                return! http.AsyncSignOut()
            }
            getUsername = Remote.authorize <| fun http () -> async {
                return http.User.Identity.Name
            }
            getAdmin = Remote.authorizeWith [AuthorizeAttribute(Roles = "admin")] <| fun _ () -> async {
                return "admin ok"
            }
        }

    member this.ConfigureServices(services: IServiceCollection) =
        services
            .AddAuthorization()
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie()
                .Services
            .AddRemoting(remoteHandler)
            .AddServerSideBlazor()
        |> ignore

    member this.Configure(app: IApplicationBuilder) =
        let serverSide = false
        app .UseAuthentication()
            .UseRemoting()
            |> ignore
        if serverSide then
            app .UseStaticFiles()
                .UseRouting()
                .UseEndpoints(fun endpoints ->
                    endpoints.MapBlazorHub<Client.Tests>("#app") |> ignore
                    endpoints.MapFallbackToFile("index.html") |> ignore
                )
        else
            app .UseBlazor<Client.Startup>()
        |> ignore
