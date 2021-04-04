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
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Mvc.Rendering
open Microsoft.Extensions.DependencyInjection
open Bolero
open FSharp.Control.Tasks

[<Extension>]
type BoleroServerComponentsExtensions =

    [<Extension>]
    static member RenderComponentAsync(html: IHtmlHelper, componentType: Type, config: IBoleroHostConfig, [<Optional; DefaultParameterValue null>] parameters: obj) =
        Components.Impl.renderComponentAsync html componentType config parameters

    [<Extension>]
    static member RenderComponentAsync<'T when 'T :> IComponent>(html: IHtmlHelper, config: IBoleroHostConfig, [<Optional; DefaultParameterValue null>] parameters: obj) =
        Components.Impl.renderComponentAsync html typeof<'T> config parameters

    /// Render the given page in the HTTP response body.
    [<Extension>]
    static member RenderPage(this: HttpContext, page: Node) = unitTask {
        let htmlHelper = this.RequestServices.GetRequiredService<IHtmlHelper>()
        let! body = Components.Impl.renderComp typeof<Components.Page> this htmlHelper Components.Impl.Page (dict ["Node", box page])
        let body = body |> System.Text.Encoding.UTF8.GetBytes
        return! this.Response.Body.WriteAsync(ReadOnlyMemory body)
    }

    [<Extension>]
    static member BoleroPage(_this: Controller, page: Node) =
        new BoleroPageResult(page)

    [<Extension>]
    static member RenderBoleroScript(html: IHtmlHelper, config: IBoleroHostConfig) =
        html.Raw(BoleroHostConfig.Body(config))

    [<Extension>]
    static member AddBoleroHost(this: IServiceCollection, ?server: bool, ?prerendered: bool, ?devToggle: bool) =
        let server = defaultArg server false
        let prerendered = defaultArg prerendered true
        let devToggle = defaultArg devToggle true
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
                    member _.IsServer = server
                    member _.IsPrerendered = prerendered })

    /// Adds a route endpoint that will match requests for non-file-names with the lowest possible priority.
    /// The request will be routed to a Bolero page.
    [<Extension>]
    static member MapFallbackToBolero(this: IEndpointRouteBuilder, page: Bolero.Node) =
        this.MapFallbackToBolero(fun _ctx -> page)

    /// Adds a route endpoint that will match requests for non-file-names with the lowest possible priority.
    /// The request will be routed to a Bolero page.
    [<Extension>]
    static member MapFallbackToBolero(this: IEndpointRouteBuilder, page: HttpContext -> Bolero.Node) =
        this.MapFallback(fun ctx ->
            let page = page ctx
            if isNull ctx.Response.ContentType then
                ctx.Response.ContentType <- "text/html; charset=UTF-8"
            ctx.RenderPage(page))

and BoleroPageResult(page: Node) =

    interface IActionResult with

        member _.ExecuteResultAsync(ctx) =
            ctx.HttpContext.RenderPage(page)
