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

type DoctypeHtmlBuilder() =
    inherit NodeBuilderWithDirectChildrenBase()

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
