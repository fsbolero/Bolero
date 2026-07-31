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
open System.Threading.Tasks
open Bolero.Tests.Remoting.Client
open Microsoft.Extensions.Logging
open Bolero.Remoting.Server
open Bolero.Server

module Page =
    open Microsoft.AspNetCore.Components
    open Microsoft.AspNetCore.Components.Web
    open Bolero.Html
    open Bolero.Server.Html

    type MyStreamedComponent() =
        inherit Components.StreamRenderingComponent<string>()

        override _.InitialModel = "loading..."

        override _.LoadModel(_initialModel) = task {
            do! Task.Delay (TimeSpan.FromSeconds 2.)
            return "loaded!"
        }

        override _.Render(model) = div { $"Static stream content {model}" }

    let index = doctypeHtml {
        head {
            title { "Bolero (remoting)" }
            meta { attr.charset "UTF-8" }
            ``base`` { attr.href "/" }
        }
        body {
            div { attr.id "main"; comp<MyApp> { attr.renderMode (InteractiveWebAssemblyRenderMode(prerender = false)) } }
            comp<MyStreamedComponent>
            script { attr.src "_content/Microsoft.AspNetCore.Components.WebAssembly.Authentication/AuthenticationService.js" }
            boleroScript
        }
    }

    [<Route "/{*path}">]
    type Page() =
        inherit Bolero.Component()
        override _.Render() = index

type MyApiHandler(log: ILogger<MyApiHandler>, ctx: IRemoteContext) =
    inherit RemoteHandler<MyApi>()

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
            login = fun login -> async {
                log.LogInformation("User logging in: {0}", login)
                return! ctx.HttpContext.AsyncSignIn(login, TimeSpan.FromDays(365. * 10.))
            }
            logout = fun () -> async {
                log.LogInformation("User logging out: {0}", ctx.HttpContext.User.Identity.Name)
                return! ctx.HttpContext.AsyncSignOut()
            }
            getLogin = ctx.Authorize <| fun () -> async {
                log.LogInformation("User getting their login: {0}", ctx.HttpContext.User.Identity.Name)
                return ctx.HttpContext.User.Identity.Name
            }
            authDouble = ctx.Authorize <| fun i -> async {
                log.LogInformation("User {0} doubling {1}", ctx.HttpContext.User.Identity.Name, i)
                return i * 2
            }
        }

module Main =
    open Microsoft.AspNetCore.Authentication.Cookies
    open Microsoft.AspNetCore.Builder
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.Hosting

    #nowarn 20 // ignore method return values

    [<EntryPoint>]
    let Main args =
        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents()
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie()
        builder.Services
            .AddBoleroRemoting<MyApiHandler>()
            .AddBoleroComponents()
        builder.Services.AddSwaggerGen()
        builder.Services.AddEndpointsApiExplorer()

        let app = builder.Build()

        app.UseAntiforgery()
        app.UseStaticFiles()
        app.MapStaticAssets()
        app.MapBoleroRemoting()
        app.MapRazorComponents<Page.Page>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof<MyApp>.Assembly)

        if app.Environment.IsDevelopment() then
            app.UseDeveloperExceptionPage()
                .UseWebAssemblyDebugging()

        app.Run()
        0
