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
open System.Text.Encodings.Web
open System.Threading.Tasks
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc.Rendering
open Microsoft.AspNetCore.Mvc.ViewFeatures
open Bolero
open Bolero.Server
open FSharp.Control.Tasks

module internal Impl =

    let renderComponentAsync (html: IHtmlHelper) (componentType: Type) (config: IBoleroHostConfig) (parameters: obj) =
        match config.IsServer, config.IsPrerendered with
        | true,  true  -> html.RenderComponentAsync(componentType, RenderMode.ServerPrerendered, parameters)
        | true,  false -> html.RenderComponentAsync(componentType, RenderMode.Server, parameters)
        | false, true  -> html.RenderComponentAsync(componentType, RenderMode.Static, parameters)
        | false, false -> Task.FromResult(null)

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
            | ValueSome config -> renderComponentAsync htmlHelper componentType config parameters
            | ValueNone -> htmlHelper.RenderComponentAsync(componentType, RenderMode.Static, parameters)
        return using (new StringWriter()) <| fun writer ->
            htmlContent.WriteTo(writer, HtmlEncoder.Default)
            writer.ToString()
    }

type Page() =
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
