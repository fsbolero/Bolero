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

namespace Bolero

/// <summary>Functions to create HTML attributes.</summary>
/// <category>HTML</category>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Attr =

    /// <summary>Create an HTML attribute or a component parameter.</summary>
    /// <param name="name">The name of the attribute or parameter.</param>
    /// <param name="value">The value of the attribute or parameter.</param>
    /// <returns>An HTML attribute or component parameter.</returns>
    /// <remarks>It is generally recommended to use <see cref="M:Bolero.Html.op_EqualsGreater" /> instead.</remarks>
    let inline Make (name: string) (value: 'T) = Attr(fun _ tb i ->
        tb.AddAttribute(i, name, box value)
        i + 1)

    /// <summary>Create an HTML attribute or a component parameter.</summary>
    /// <param name="name">The name of the attribute or parameter.</param>
    /// <param name="value">The value of the attribute or parameter. Takes the receiving or containing component as argument.</param>
    /// <returns>An HTML attribute or component parameter.</returns>
    let inline MakeWithReceiver<'T> (name: string) (value: obj -> 'T) = Attr(fun r tb i ->
        tb.AddAttribute(i, name, box (value r))
        i + 1)

    /// <summary>Group multiple HTML attributes and component parameters as a single value.</summary>
    /// <param name="attrs">The HTML attributes and component parameters.</param>
    /// <returns>An Attr value representing all the given HTML attributes and component parameters.</returns>
    /// <remarks>It is generally recommended to use <see cref="M:Bolero.Html.attrs" /> instead.</remarks>
    let inline Attrs (attrs: seq<Attr>) = Attr(fun comp tb i ->
        let mutable i = i
        for attr in attrs do
            i <- attr.Invoke(comp, tb, i)
        i)

    /// <summary>Create an Attr value representing no attributes or parameters.</summary>
    /// <returns>An Attr value representing no attributes or parameters.</returns>
    /// <remarks>It is generally recommended to use <see cref="M:Bolero.Html.attr.empty" /> instead.</remarks>
    let inline Empty() = Attr(fun _ _ i -> i)
