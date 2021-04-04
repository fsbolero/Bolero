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
open Bolero.Server

module Html =

    /// Insert a Blazor component inside a static page.
    let rootComp<'T when 'T :> IComponent> =
        Node.BlazorComponent<Components.RootComponent>([Attr("ComponentType", typeof<'T>)], [])

    /// Insert the required scripts to run Blazor components.
    let boleroScript = Node.BlazorComponent<Components.BoleroScript>([], [])

    /// Create a doctype declaration.
    let doctype (decl: string) = RawHtml $"<!DOCTYPE {decl}>\n"

    /// Create an `<html>` element preceded by the standard "html" doctype declaration.
    let doctypeHtml attrs body = Concat [doctype "html"; Elt("html", attrs, body)]
