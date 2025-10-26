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

#nowarn "3391"

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Text.Encodings.Web
open System.Threading.Tasks
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Endpoints
open Microsoft.AspNetCore.Components.RenderTree
open Microsoft.AspNetCore.Components.Rendering
open Microsoft.AspNetCore.Html
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc.Rendering
open Microsoft.AspNetCore.Mvc.ViewFeatures
open Bolero
open Bolero.Server

type RootComponent() =
    inherit ComponentBase()

    [<Parameter>]
    member val ComponentType = Unchecked.defaultof<Type> with get, set

    override this.BuildRenderTree(_builder) =
        failwith "RootComponent cannot be rendered as a Blazor component"

type BoleroScript() =
    inherit ComponentBase()

    [<Inject>]
    member val Config = Unchecked.defaultof<IBoleroHostConfig> with get, set

    override this.BuildRenderTree(builder) =
        builder.AddMarkupContent(0, BoleroHostConfig.Body(this.Config))

[<AbstractClass; StreamRendering true>]
type StreamRenderingComponent<'model>() =
    inherit Component<'model>()

    let mutable model = Unchecked.defaultof<'model>

    abstract InitialModel : 'model

    abstract LoadModel : 'model -> Task<'model>

    abstract Render : 'model -> Node

    override this.OnInitializedAsync() =
        model <- this.InitialModel
        task {
            let! newModel = this.LoadModel(model)
            model <- newModel
        }

    override this.Render() =
        this.Render(model)

module Rendering =

    let private emptyContent = Task.FromResult { new IHtmlContent with member _.WriteTo(_, _) = () }

    let internal renderComponentAsync (html: IHtmlHelper) (componentType: Type) (config: IBoleroHostConfig) (parameters: obj) =
        match config.IsServer, config.IsPrerendered with
        | true,  true  -> html.RenderComponentAsync(componentType, RenderMode.ServerPrerendered, parameters)
        | true,  false -> html.RenderComponentAsync(componentType, RenderMode.Server, parameters)
        | false, true  -> html.RenderComponentAsync(componentType, RenderMode.Static, parameters)
        | false, false -> emptyContent

    type [<Struct>] RenderType =
        | FromConfig of IBoleroHostConfig
        | Page

    let private renderCompTo
            (sb: StringBuilder)
            (componentType: Type)
            (httpContext: HttpContext)
            (htmlHelper: IHtmlHelper)
            (renderType: RenderType)
            (parameters: obj)
            (prerenderer: IComponentPrerenderer)
            = task {
        (htmlHelper :?> IViewContextAware).Contextualize(ViewContext(HttpContext = httpContext))
        let! htmlContent =
            match renderType with
            | FromConfig config -> renderComponentAsync htmlHelper componentType config parameters
            | Page -> htmlHelper.RenderComponentAsync(componentType, RenderMode.Static, parameters)
        use writer = new StringWriter(sb)
        return! prerenderer.Dispatcher.InvokeAsync(fun () -> htmlContent.WriteTo(writer, HtmlEncoder.Default))
    }

    let private selfClosingElements = HashSet [ "area"; "base"; "br"; "col"; "embed"; "hr"; "img"; "input"; "link"; "meta"; "param"; "source"; "track"; "wbr" ]

    type [<Struct>] private RenderState =
        | Normal
        | InElement

    type private IRenderComponents =
        abstract RenderComponent : componentType: Type * stringBuilder: StringBuilder * attributes: IDictionary<string, obj> * forceStatic: bool -> StringBuilder

    let private emptyAttributes = dict<string, obj> []

    let private getComponentAttributes (frames: ReadOnlySpan<RenderTreeFrame>) =
        if frames.IsEmpty then
            emptyAttributes
        else
            let attributes = Dictionary()
            for frame in frames do
                match frame.FrameType with
                | RenderTreeFrameType.Attribute -> attributes.Add(frame.AttributeName, frame.AttributeValue)
                | _ -> ()
            attributes

    let rec private render (renderComp: IRenderComponents) (frames: ReadOnlySpan<RenderTreeFrame>) (state: RenderState) (sb: StringBuilder) : RenderState =
        if frames.IsEmpty then
            state
        else
            let frame = &frames[0]
            match frame.FrameType with
            | RenderTreeFrameType.Element ->
                if state = InElement then sb.Append(">") |> ignore
                let state =
                    sb.Append("<").Append(frame.ElementName)
                    |> render renderComp (frames.Slice(1, frame.ElementSubtreeLength - 1)) InElement
                if state = InElement && selfClosingElements.Contains(frame.ElementName) then
                    sb.Append("/>")
                else
                    if state = InElement then sb.Append(">") |> ignore
                    sb.Append("</").Append(frame.ElementName).Append(">")
                |> render renderComp (frames.Slice(frame.ElementSubtreeLength)) Normal
            | RenderTreeFrameType.Text ->
                if state = InElement then sb.Append(">") |> ignore
                sb.Append(HtmlEncoder.Default.Encode frame.TextContent)
                |> render renderComp (frames.Slice(1)) Normal
            | RenderTreeFrameType.Markup ->
                if state = InElement then sb.Append(">") |> ignore
                sb.Append(frame.MarkupContent)
                |> render renderComp (frames.Slice(1)) Normal
            | RenderTreeFrameType.Region ->
                render renderComp (frames.Slice(1)) state sb
            | RenderTreeFrameType.Attribute ->
                if state <> InElement then failwith "Shouldn't happen: attribute outside of element"
                sb.Append(" ").Append(frame.AttributeName).Append("=\"")
                    .Append(HtmlEncoder.Default.Encode (frame.AttributeValue.ToString())).Append("\"")
                |> render renderComp (frames.Slice(1)) state
            | RenderTreeFrameType.Component ->
                if state = InElement then sb.Append(">") |> ignore
                if frame.ComponentType = typeof<RootComponent> then
                    let attributes = getComponentAttributes (frames.Slice(1, frame.ComponentSubtreeLength - 1))
                    match attributes.TryGetValue("ComponentType") with
                    | true, (:? Type as componentType) ->
                        attributes.Remove("ComponentType") |> ignore
                        renderComp.RenderComponent(componentType, sb, attributes, false)
                        |> render renderComp (frames.Slice(frame.ComponentSubtreeLength)) Normal
                    | _ ->
                        failwith "Invalid use of rootComp"
                else
                    let attributes = getComponentAttributes (frames.Slice(1, frame.ComponentSubtreeLength - 1))
                    let forceStatic = frame.ComponentType = typeof<BoleroScript>
                    renderComp.RenderComponent(frame.ComponentType, sb, attributes, forceStatic)
                    |> render renderComp (frames.Slice(frame.ComponentSubtreeLength)) Normal
            | RenderTreeFrameType.ComponentReferenceCapture
            | RenderTreeFrameType.ElementReferenceCapture
            | RenderTreeFrameType.None
            | _ ->
                state

    let private renderWith (renderComp: IRenderComponents) (node: Node) =
        use renderTreeBuilder = new RenderTreeBuilder()
        node.Invoke(null, renderTreeBuilder, 0) |> ignore
        let frames = renderTreeBuilder.GetFrames()
        let sb = StringBuilder()
        render renderComp (frames.Array.AsSpan(0, frames.Count)) Normal sb |> ignore
        sb.ToString()

    /// <summary>Render a Bolero page to a string.</summary>
    /// <param name="page">The Bolero page.</param>
    /// <param name="httpContext">The ASP.NET HTTP context.</param>
    /// <param name="htmlHelper">The Razor HTML helper.</param>
    /// <param name="boleroConfig">The Bolero configuration.</param>
    /// <returns>A string containing the HTML representation of the page.</returns>
    let renderPage (page: Node) httpContext htmlHelper boleroConfig (prerenderer: IComponentPrerenderer) =
        let renderComp =
            { new IRenderComponents with
                member _.RenderComponent(ty, sb, attributes, forceStatic) =
                    let renderType = if forceStatic then Page else FromConfig boleroConfig
                    (renderCompTo sb ty httpContext htmlHelper renderType attributes prerenderer).GetAwaiter().GetResult()
                    sb }
        renderWith renderComp page

    /// <summary>Render a Bolero node to a string.</summary>
    /// <param name="node">The Bolero node.</param>
    /// <returns>A string containing the HTML representation of the node.</returns>
    /// <remarks>Blazor components contained in the node are ignored.</remarks>
    let renderPlain (node: Node) =
        let renderComp =
            { new IRenderComponents with
                member _.RenderComponent(_, sb, _, _) = sb }
        renderWith renderComp node
