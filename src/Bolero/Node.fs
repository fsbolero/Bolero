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

#nowarn "42" // Use of (# ... #) syntax

open System
open FSharp.Reflection
open Microsoft.AspNetCore.Components

/// <summary>An HTML fragment.</summary>
/// <category>HTML</category>
type Node = delegate of obj * Rendering.RenderTreeBuilder * int -> int

/// <exclude />
[<Sealed; AbstractClass>]
type Matcher<'T> private () =

    // Reinterpret false as 0 and true as 1
    static let boolTagReader (x: bool) =
        (# "" x : int #)

    static member val CaseCount =
        if typeof<'T> = typeof<bool> then
            2
        else
            FSharpType.GetUnionCases(typeof<'T>, true).Length

    static member val TagReader =
        if typeof<'T> = typeof<bool> then
            unbox<'T -> int> boolTagReader
        else
            let tagReader = FSharpValue.PreComputeUnionTagReader(typeof<'T>)
            fun (x: 'T) -> tagReader (box x)

module Node =

    /// <summary>Create an empty HTML fragment.</summary>
    /// <returns>An empty HTML fragment.</returns>
    let inline Empty() = Node(fun _ _ i -> i)

    /// <summary>Create an HTML element using a list of attributes and a list of children.</summary>
    /// <param name="name">The name of the HTML element.</param>
    /// <param name="attrs">The attributes of the HTML element.</param>
    /// <param name="children">The children of the HTML element.</param>
    /// <returns>An HTML element.</returns>
    /// <seealso cref="M:Bolero.Html.elt" />
    let inline Elt name (attrs: seq<Attr>) (children: seq<Node>) = Node(fun comp tb i ->
        tb.OpenElement(i, name)
        let mutable i = i + 1
        for attr in attrs do
            i <- attr.Invoke(comp, tb, i)
        for node in children do
            i <- node.Invoke(comp, tb, i)
        tb.CloseElement()
        i)

    /// <summary>Create an HTML text node.</summary>
    /// <param name="text">The text of the node.</param>
    /// <returns>An HTML text node.</returns>
    /// <seealso cref="M:Bolero.Html.text" />
    let inline Text (text: string) = Node(fun _ tb i ->
        tb.AddContent(i, text)
        i + 1)

    /// <summary>Create an HTML fragment fram raw HTML.</summary>
    /// <param name="html">The raw HTML.</param>
    /// <returns>An HTML fragment.</returns>
    /// <seealso cref="M:Bolero.Html.rawHtml" />
    let inline RawHtml (html: string) = Node(fun _ tb i ->
        tb.AddMarkupContent(i, html)
        i + 1)

    /// <summary>Create a conditional HTML fragment whose structure depends on a boolean condition.</summary>
    /// <param name="cond">The condition.</param>
    /// <param name="node">The HTML fragment whose structure depends on the condition.</param>
    /// <returns>The same HTML fragment wrapped in a way that Blazor can render.</returns>
    /// <seealso cref="M:Bolero.Html.cond" />
    [<Obsolete "Use Node.Match">]
    let inline Cond (cond: bool) ([<InlineIfLambda>] node: Node) = Node(fun comp tb i ->
        tb.OpenRegion(if cond then i else i + 1)
        node.Invoke(comp, tb, 0) |> ignore
        tb.CloseRegion()
        i + 2)

    /// <summary>Create a conditional HTML fragment whose structure depends on a boolean value or the case of a union.</summary>
    /// <typeparam name="T">The type of the value that the fragment depends on. Must be bool or a discriminated union.</typeparam>
    /// <param name="value">The boolean or union value.</param>
    /// <param name="node">The HTML fragment whose structure depends on the boolean or union value.</param>
    /// <returns>The same HTML fragment wrapped in a way that Blazor can render.</returns>
    /// <seealso cref="M:Bolero.Html.cond" />
    let inline Match (value: 'T) ([<InlineIfLambda>] node: Node) = Node(fun comp tb i ->
        let caseCount = Matcher<'T>.CaseCount
        let matchedCase = Matcher<'T>.TagReader value
        tb.OpenRegion(i + matchedCase)
        node.Invoke(comp, tb, 0) |> ignore
        tb.CloseRegion()
        i + caseCount)

    /// <summary>Create an HTML fragment that is the concatenation of given HTML fragments.</summary>
    /// <param name="nodes">The HTML fragments.</param>
    /// <returns>The concatenated HTML fragments.</returns>
    /// <seealso cref="M:Bolero.Html.concat" />
    /// <remarks>
    /// This function can be used to build a concatenation of fragments whose structure does not vary across renders.
    /// To concatenate fragments based on a variable list of values, use <see cref="M:ForEach" /> instead.
    /// </remarks>
    let inline Concat (nodes: seq<Node>) = Node(fun comp tb i ->
        let mutable i = i
        for node in nodes do
            i <- node.Invoke(comp, tb, i)
        i)

    /// <summary>
    /// Create an HTML fragment that is the concatenation of fragments obtained by mapping a function on a sequence of items.
    /// </summary>
    /// <param name="items">The items used to generate HTML fragments.</param>
    /// <param name="mkNode">The function that generates an HTML fragment from an item.</param>
    /// <returns>The HTML fragments generated and concatenated into one.</param>
    let inline ForEach (items: seq<'T>) ([<InlineIfLambda>] mkNode: 'T -> Node) = Node(fun comp tb i ->
        tb.OpenRegion(i)
        for item in items do
            (mkNode item).Invoke(comp, tb, 0) |> ignore
        tb.CloseRegion()
        i + 1)

    /// <summary>Wrap a Blazor RenderFragment in a Bolero Node.</summary>
    /// <param name="fragment">The Blazor RenderFragment.</param>
    /// <returns>A Bolero Node representing the Blazor RenderFragment.</returns>
    /// <seealso cref="M:Bolero.Html.fragment" />
    let inline Fragment (fragment: RenderFragment) = Node(fun _ tb i ->
        tb.OpenRegion(i)
        fragment.Invoke(tb)
        tb.CloseRegion()
        i + 1)
