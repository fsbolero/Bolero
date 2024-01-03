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

namespace Bolero.Server

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Components
#if NET8_0
open Microsoft.AspNetCore.Components.Endpoints
#endif
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Mvc.Rendering
open Microsoft.Extensions.DependencyInjection
open Bolero

/// Extension methods to enable support for hosting server-side and WebAssembly Bolero components in ASP.NET Core.
[<Extension>]
type ServerComponentsExtensions =

    /// <summary>Render a Bolero component in a Razor page.</summary>
    /// <param name="html">The injected HTML helper.</param>
    /// <param name="componentType">The Bolero component type.</param>
    /// <param name="config">The injected Bolero hosting configuration.</param>
    /// <param name="parameters">An <see cref="T:System.Object" /> containing the parameters to pass to the component.</param>
    [<Extension>]
    static member RenderComponentAsync(html: IHtmlHelper, componentType: Type, config: IBoleroHostConfig, [<Optional; DefaultParameterValue null>] parameters: obj) =
        Components.Rendering.renderComponentAsync html componentType config parameters

    /// <summary>Render a Bolero component in a Razor page.</summary>
    /// <typeparam name="T">The Bolero component type.</typeparam>
    /// <param name="html">The injected HTML helper.</param>
    /// <param name="config">The injected Bolero hosting configuration.</param>
    /// <param name="parameters">An <see cref="T:System.Object" /> containing the parameters to pass to the component.</param>
    [<Extension>]
    static member RenderComponentAsync<'T when 'T :> IComponent>(html: IHtmlHelper, config: IBoleroHostConfig, [<Optional; DefaultParameterValue null>] parameters: obj) =
        Components.Rendering.renderComponentAsync html typeof<'T> config parameters

    /// <summary>Render the given page in the HTTP response body.</summary>
    /// <param name="page">The page to render.</param>
    [<Extension>]
    static member RenderPage(this: HttpContext, page: Node) : Task = upcast task {
        let htmlHelper = this.RequestServices.GetRequiredService<IHtmlHelper>()
        let boleroConfig = this.RequestServices.GetRequiredService<IBoleroHostConfig>()
#if NET8_0
        let prerenderer = this.RequestServices.GetRequiredService<IComponentPrerenderer>()
#endif
        let body =
            Components.Rendering.renderPage page this htmlHelper boleroConfig
#if NET8_0
                prerenderer
#endif
            |> System.Text.Encoding.UTF8.GetBytes
        return! this.Response.Body.WriteAsync(ReadOnlyMemory body)
    }

    /// <summary>Create a HTML page from the given Bolero element as MVC content.</summary>
    /// <param name="page">The page to return.</param>
    [<Extension>]
    static member BoleroPage(_this: Controller, page: Node) =
        BoleroPageResult(page)

    /// <summary>Render the JavaScript tag needed by Bolero in a Razor page.</summary>
    /// <param name="config">The injected Bolero hosting configuration.</param>
    [<Extension>]
    static member RenderBoleroScript(html: IHtmlHelper, config: IBoleroHostConfig) =
        html.Raw(BoleroHostConfig.Body(config))

    /// <summary>
    /// Configure the hosting of server-side and WebAssembly Bolero components using Bolero's legacy render mode handling.
    /// </summary>
    /// <param name="server">If true, use server-side Bolero; if false, use WebAssembly. Default is false.</param>
    /// <param name="prerendered">If true, prerender the initial view in the served HTML. Default is true.</param>
    /// <param name="devToggle">
    /// If true and ASP.NET Core is running in development environment, query parameter <c>?server=bool</c>
    /// can be passed to use the given mode.
    /// </param>
    [<Extension>]
    static member AddBoleroHost(
            this: IServiceCollection,
            [<Optional; DefaultParameterValue false>] server: bool,
            [<Optional; DefaultParameterValue true>] prerendered: bool,
            [<Optional; DefaultParameterValue true>] devToggle: bool) =
        if devToggle then
            this.AddSingleton(
                { new IBoleroHostBaseConfig with
                    member _.IsServer = server
                    member _.IsPrerendered = prerendered })
                .AddScoped<IBoleroHostConfig, BoleroHostConfig>()
                .AddHttpContextAccessor()
        else
            this.AddSingleton(
                { new IBoleroHostConfig with
                    member _.IsInteractiveRender = false
                    member _.IsServer = server
                    member _.IsPrerendered = prerendered })

    /// <summary>
    /// Configure the hosting of Bolero components using interactive render modes.
    /// </summary>
    [<Extension>]
    static member AddBoleroComponents(this: IServiceCollection) =
        this.AddSingleton(
            { new IBoleroHostConfig with
                member _.IsInteractiveRender = true
                member _.IsServer = false
                member _.IsPrerendered = false })

    /// <summary>
    /// Adds a route endpoint that will match requests for non-file-names with the lowest possible priority.
    /// The request will be routed to a Bolero page.
    /// </summary>
    /// <param name="page">The page to serve.</param>
    [<Extension>]
    static member MapFallbackToBolero(this: IEndpointRouteBuilder, page: Bolero.Node) =
        this.MapFallbackToBolero(fun _ctx -> page)

    /// <summary>
    /// Adds a route endpoint that will match requests for non-file-names with the lowest possible priority.
    /// The request will be routed to a Bolero page.
    /// </summary>
    /// <param name="page">A function that generates the page to serve.</param>
    [<Extension>]
    static member MapFallbackToBolero(this: IEndpointRouteBuilder, page: Func<HttpContext, Bolero.Node>) =
        this.MapFallback(fun ctx ->
            let page = page.Invoke(ctx)
            if isNull ctx.Response.ContentType then
                ctx.Response.ContentType <- "text/html; charset=UTF-8"
            ctx.RenderPage(page))

/// <exclude />
and BoleroPageResult(page: Node) =

    interface IActionResult with

        member _.ExecuteResultAsync(ctx) =
            ctx.HttpContext.RenderPage(page)
