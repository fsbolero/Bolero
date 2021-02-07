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

namespace Bolero.Server.Components

open System
open System.IO
open System.Runtime.CompilerServices
open System.Text.Encodings.Web
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc.Rendering
open Microsoft.AspNetCore.Mvc.ViewFeatures
open Microsoft.Extensions.DependencyInjection
open Bolero
open Bolero.Server
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Routing

[<Extension>]
type BoleroServerComponentsExtensions =

    [<Extension>]
    static member RenderComponentAsync(html: IHtmlHelper, componentType: Type, config: IBoleroHostConfig, parameters: obj) =
        match config.IsServer, config.IsPrerendered with
        | true,  true  -> html.RenderComponentAsync(componentType, RenderMode.ServerPrerendered, parameters)
        | true,  false -> html.RenderComponentAsync(componentType, RenderMode.Server, parameters)
        | false, true  -> html.RenderComponentAsync(componentType, RenderMode.Static, parameters)
        | false, false -> Task.FromResult(null)

    [<Extension>]
    static member RenderComponentAsync<'T when 'T :> IComponent>(html: IHtmlHelper, config: IBoleroHostConfig, parameters: obj) =
        html.RenderComponentAsync(typeof<'T>, config, parameters)

    [<Extension>]
    static member RenderComponentAsync<'T when 'T :> IComponent>(html: IHtmlHelper, config: IBoleroHostConfig) =
        html.RenderComponentAsync<'T>(config, null)

    [<Extension>]
    static member RenderComponentAsync(html: IHtmlHelper, componentType: Type, config: IBoleroHostConfig) =
        html.RenderComponentAsync(componentType, config, null)

module internal Impl =

    let renderComp
            (componentType: Type)
            (httpContext: HttpContext)
            (htmlHelper: IHtmlHelper)
            (boleroConfig: IBoleroHostConfig voption)
            (parameters: obj)
            = task {
        (htmlHelper :?> IViewContextAware).Contextualize(ViewContext(HttpContext = httpContext))
        let! htmlContent =
            match boleroConfig with
            | ValueSome config -> htmlHelper.RenderComponentAsync(componentType, config, parameters)
            | ValueNone -> htmlHelper.RenderComponentAsync(componentType, RenderMode.Static, parameters)
        return using (new StringWriter()) <| fun writer ->
            htmlContent.WriteTo(writer, HtmlEncoder.Default)
            writer.ToString()
    }

type PageComponent() =
    inherit Component()

    [<Parameter>]
    member val Node = Unchecked.defaultof<Node> with get, set

    override this.Render() = this.Node

type RootComponent() =
    inherit ComponentBase()

    [<Parameter>]
    member val ComponentType = Unchecked.defaultof<Type> with get, set

    [<Inject>]
    member val HttpContextAccessor = Unchecked.defaultof<IHttpContextAccessor> with get, set

    [<Inject>]
    member val HtmlHelper = Unchecked.defaultof<IHtmlHelper> with get, set

    [<Inject>]
    member val BoleroConfig = Unchecked.defaultof<IBoleroHostConfig> with get, set

    override this.BuildRenderTree(builder) =
        let body = Impl.renderComp this.ComponentType this.HttpContextAccessor.HttpContext this.HtmlHelper (ValueSome this.BoleroConfig) null
        builder.AddMarkupContent(0, body.Result)

type BoleroScript() =
    inherit ComponentBase()

    [<Inject>]
    member val Config = Unchecked.defaultof<IBoleroHostConfig> with get, set

    override this.BuildRenderTree(builder) =
        builder.AddMarkupContent(0, BoleroHostConfig.Body(this.Config))

type BoleroServerComponentsExtensions with

    /// Render the given page in the HTTP response body.
    [<Extension>]
    static member RenderPage(this: HttpContext, page: Node) = unitTask {
        let htmlHelper = this.RequestServices.GetRequiredService<IHtmlHelper>()
        let! body = Impl.renderComp typeof<PageComponent> this htmlHelper ValueNone (dict ["Node", box page])
        let body = body |> System.Text.Encoding.UTF8.GetBytes
        return! this.Response.Body.WriteAsync(ReadOnlyMemory body)
    }

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
