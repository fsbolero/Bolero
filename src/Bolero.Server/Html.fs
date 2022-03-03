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

open Microsoft.AspNetCore.Components
open Bolero
open Bolero.Builders
open Bolero.Server

type [<Struct; NoComparison; NoEquality>] DoctypeHtmlBuilder =

    member inline _.Yield([<InlineIfLambda>] attr: Attr) = attr
    member inline _.Yield([<InlineIfLambda>] keyAndRef: KeyAndRef) = keyAndRef
    member inline _.Yield([<InlineIfLambda>] node: Node) = node
    member inline this.Yield(text: string) =
        this.Yield(Node(fun _ b _ i ->
            b.AddContent(i, text)
            i + 1))
    member inline this.Yield(fragment: RenderFragment) =
        this.Yield(Node(fun _ b _ i ->
            b.AddContent(i, fragment)
            i + 1))
    member inline this.Yield(eb: ElementBuilder) =
        this.Yield(Node(fun c b m i ->
            b.OpenElement(i, eb.Name)
            b.CloseElement()
            i + 1))
    member inline this.Yield(_comp: ComponentBuilder<'T>) =
        this.Yield(Node(fun c b m i ->
            b.OpenComponent<'T>(i)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentBuilder) =
        this.Yield(Node(fun c b m i ->
            b.OpenComponent(i, comp.Type)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentWithAttrsBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b m i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, m, i + 1)
            b.CloseComponent()
            i))
    member inline this.Yield(comp: ComponentWithAttrsAndNoChildrenBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b m i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, m, i + 1)
            b.CloseComponent()
            i))

    member inline _.Delay([<InlineIfLambda>] a: unit -> Attr) = Attr(fun c b m i -> a().Invoke(c, b, m, i))
    member inline _.Delay([<InlineIfLambda>] r: unit -> KeyAndRef) = KeyAndRef(fun c b m i -> r().Invoke(c, b, m, i))
    member inline _.Delay([<InlineIfLambda>] r: unit -> ChildKeyAndRefContent) = ChildKeyAndRefContent(fun c b m i -> r().Invoke(c, b, m, i))
    member inline _.Delay([<InlineIfLambda>] n: unit -> Node) = Node(fun c b m i -> n().Invoke(c, b, m, i))

    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Attr) =
        Attr(fun c b m i ->
            let i = x1.Invoke(c, b, m, i)
            x2.Invoke(c, b, m, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: KeyAndRef) =
        Attr(fun c b m i ->
            let i = x1.Invoke(c, b, m, i)
            x2.Invoke(c, b, m, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: ChildKeyAndRefContent) =
        Attr(fun c b m i ->
            let i = x1.Invoke(c, b, m, i)
            x2.Invoke(c, b, m, i))


    member inline _.Combine([<InlineIfLambda>] x1: Node, [<InlineIfLambda>] x2: Node) =
        Node(fun c b m i ->
            let i = x1.Invoke(c, b, m, i)
            x2.Invoke(c, b, m, i))

    member inline this.For(s: seq<'T>, [<InlineIfLambda>] f: 'T -> Node) =
        this.Yield(Node.ForEach s f)

    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Node) =
        Attr(fun c b m i ->
            let i = x1.Invoke(c, b, m, i)
            x2.Invoke(c, b, m, i))
    member inline _.Combine([<InlineIfLambda>] x1: KeyAndRef, [<InlineIfLambda>] x2: Node) =
        ChildKeyAndRefContent(fun c b m i ->
            let i = x1.Invoke(c, b, m, i)
            x2.Invoke(c, b, m, i))

    member inline this.Run([<InlineIfLambda>] content: Node) =
        Node(fun c b m i ->
            b.AddMarkupContent(i, "<!DOCTYPE html>\n")
            b.OpenElement(i + 1, "html")
            let i = content.Invoke(c, b, m, i + 2)
            b.CloseElement()
            i)

    member inline this.Run([<InlineIfLambda>] content: Attr) =
        Node(fun c b m i ->
            b.AddMarkupContent(i, "<!DOCTYPE html>\n")
            b.OpenElement(i + 1, "html")
            let i = content.Invoke(c, b, m, i + 2)
            b.CloseElement()
            i)

    member inline this.Run([<InlineIfLambda>] content: KeyAndRef) =
        Node(fun c b m i ->
            b.AddMarkupContent(i, "<!DOCTYPE html>\n")
            b.OpenElement(i + 1, "html")
            let i = content.Invoke(c, b, m, i + 2)
            b.CloseElement()
            i)

    member inline this.Run([<InlineIfLambda>] content: ChildKeyAndRefContent) =
        Node(fun c b m i ->
            b.AddMarkupContent(i, "<!DOCTYPE html>\n")
            b.OpenElement(i + 1, "html")
            let i = content.Invoke(c, b, m, i + 2)
            b.CloseElement()
            i)


module Html =
    open Bolero.Html

    /// Insert a Blazor component inside a static page.
    let rootComp<'T when 'T :> IComponent> =
        ComponentWithAttrsAndNoChildrenBuilder<Components.RootComponent>(attrs {
            "ComponentType" => typeof<'T>
        })

    /// Insert the required scripts to run Blazor components.
    let boleroScript = comp<Components.BoleroScript> { attr.empty() }

    /// Create a doctype declaration.
    let doctype (decl: string) = rawHtml $"<!DOCTYPE {decl}>\n"

    /// Create an `<html>` element preceded by the standard "html" doctype declaration.
    let doctypeHtml = DoctypeHtmlBuilder()
