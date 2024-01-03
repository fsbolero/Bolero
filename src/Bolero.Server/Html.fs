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
open Microsoft.AspNetCore.Components
open Bolero
open Bolero.Builders
open Bolero.Server

/// <exclude />
type [<Struct; NoComparison; NoEquality>] DoctypeHtmlBuilder =

    member inline _.Yield([<InlineIfLambda>] attr: Attr) = attr
    member inline _.Yield([<InlineIfLambda>] node: Node) = node
    member inline this.Yield(text: string) =
        this.Yield(Node(fun _ b i ->
            b.AddContent(i, text)
            i + 1))
    member inline this.Yield(fragment: RenderFragment) =
        this.Yield(Node(fun _ b i ->
            b.AddContent(i, fragment)
            i + 1))
    member inline this.Yield(eb: ElementBuilder) =
        this.Yield(Node(fun c b i ->
            b.OpenElement(i, eb.Name)
            b.CloseElement()
            i + 1))
    member inline this.Yield(_comp: ComponentBuilder<'T>) =
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentBuilder) =
        this.Yield(Node(fun c b i ->
            b.OpenComponent(i, comp.Type)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentWithAttrsBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            b.CloseComponent()
            i))
    member inline this.Yield(comp: ComponentWithAttrsAndNoChildrenBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            b.CloseComponent()
            i))

    member inline _.Delay([<InlineIfLambda>] a: unit -> Attr) = Attr(fun c b i -> a().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] n: unit -> Node) = Node(fun c b i -> n().Invoke(c, b, i))

    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Attr) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Node) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: Node, [<InlineIfLambda>] x2: Node) =
        Node(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))

    member inline this.For(s: seq<'T>, [<InlineIfLambda>] f: 'T -> Node) =
        this.Yield(Node.ForEach s f)

    member inline this.Run([<InlineIfLambda>] content: Node) =
        Node(fun c b i ->
            b.AddMarkupContent(i, "<!DOCTYPE html>\n")
            b.OpenElement(i + 1, "html")
            let i = content.Invoke(c, b, i + 2)
            b.CloseElement()
            i)

    member inline this.Run([<InlineIfLambda>] content: Attr) =
        Node(fun c b i ->
            b.AddMarkupContent(i, "<!DOCTYPE html>\n")
            b.OpenElement(i + 1, "html")
            let i = content.Invoke(c, b, i + 2)
            b.CloseElement()
            i)


module Html =
    open Bolero.Html

    /// <summary>Insert a Blazor component inside a static page.</summary>
    /// <typeparam name="T">The component type.</typeparam>
    [<Obsolete "Use comp instead">]
    let rootComp<'T when 'T :> IComponent> =
        ComponentWithAttrsAndNoChildrenBuilder<Components.RootComponent>(attrs {
            "ComponentType" => typeof<'T>
        })

    /// Insert the required scripts to run Blazor components.
    let boleroScript = comp<Components.BoleroScript>

    /// <summary>Create a doctype declaration.</summary>
    /// <param name="decl">The declaration value.</param>
    let doctype (decl: string) = rawHtml $"<!DOCTYPE {decl}>\n"

    /// <summary>
    /// Computation expression builder to create an <c>&lt;html&gt;</c> element
    /// preceded by the standard "html" doctype declaration.
    /// </summary>
    let doctypeHtml = DoctypeHtmlBuilder()

#if NET8_0_OR_GREATER
    module attr =

        let renderMode (mode: IComponentRenderMode) =
            Attr(fun _ b i ->
                b.AddComponentRenderMode(mode)
                i)
#endif
