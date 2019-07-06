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

namespace Bolero.Tests.Remoting

open System
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Bolero.Remoting.Server
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.Extensions.Hosting

type MyApiHandler(log: ILogger<MyApiHandler>) =
    inherit RemoteHandler<Client.MyApi>()

    let mutable items = Map.empty

    override this.Handler =
        {
            getItems = fun () -> async {
                log.LogInformation("Getting items")
                return items
            }
            setItem = fun (k, v) -> async {
                log.LogInformation("Setting {0} => {1}", k, v)
                items <- Map.add k v items
            }
            removeItem = fun k -> async {
                log.LogInformation("Removing {0}", k)
                items <- Map.remove k items
            }
            login = Remote.withContext <| fun http login -> async {
                log.LogInformation("User logging in: {0}", login)
                return! http.AsyncSignIn(login, TimeSpan.FromDays(365. * 10.))
            }
            logout = Remote.withContext <| fun http () -> async {
                log.LogInformation("User logging out: {0}", http.User.Identity.Name)
                return! http.AsyncSignOut()
            }
            getLogin = Remote.authorize <| fun http () -> async {
                log.LogInformation("User getting their login: {0}", http.User.Identity.Name)
                return http.User.Identity.Name
            }
            authDouble = Remote.authorize <| fun http i -> async {
                log.LogInformation("User {0} doubling {1}", http.User.Identity.Name, i)
                return i * 2
            }
        }

type Startup() =

    member this.ConfigureServices(services: IServiceCollection) =
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie()
                .Services
            .AddRemoting<MyApiHandler>()
            .AddServerSideBlazor()
        |> ignore

    member this.Configure(app: IApplicationBuilder, env: IHostEnvironment) =
        app.UseAuthentication()
            .UseRemoting()
            .UseBlazor<Client.Startup>()
        |> ignore

module Main =
    [<EntryPoint>]
    let Main args =
        WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .Build()
            .Run()
        0
