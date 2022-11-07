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

/// HTML fragment.
/// [category: HTML]
type Node = delegate of obj * Rendering.RenderTreeBuilder * int -> int

/// [omit]
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

    let inline Empty() = Node(fun _ _ i -> i)

    let inline Elt name (attrs: seq<Attr>) (children: seq<Node>) = Node(fun comp tb i ->
        tb.OpenElement(i, name)
        let mutable i = i + 1
        for attr in attrs do
            i <- attr.Invoke(comp, tb, i)
        for node in children do
            i <- node.Invoke(comp, tb, i)
        tb.CloseElement()
        i)

    let inline Text (text: string) = Node(fun _ tb i ->
        tb.AddContent(i, text)
        i + 1)

    let inline RawHtml (html: string) = Node(fun _ tb i ->
        tb.AddMarkupContent(i, html)
        i + 1)

    [<Obsolete "Use Node.Match">]
    let inline Cond (cond: bool) ([<InlineIfLambda>] node: Node) = Node(fun comp tb i ->
        tb.OpenRegion(if cond then i else i + 1)
        node.Invoke(comp, tb, 0) |> ignore
        tb.CloseRegion()
        i + 2)

    let inline Match (value: 'T) ([<InlineIfLambda>] node: Node) = Node(fun comp tb i ->
        let caseCount = Matcher<'T>.CaseCount
        let matchedCase = Matcher<'T>.TagReader value
        tb.OpenRegion(i + matchedCase)
        node.Invoke(comp, tb, 0) |> ignore
        tb.CloseRegion()
        i + caseCount)

    let inline Concat (nodes: seq<Node>) = Node(fun comp tb i ->
        let mutable i = i
        for node in nodes do
            i <- node.Invoke(comp, tb, i)
        i)

    let inline ForEach (items: seq<'T>) ([<InlineIfLambda>] mkNode: 'T -> Node) = Node(fun comp tb i ->
        tb.OpenRegion(i)
        for item in items do
            (mkNode item).Invoke(comp, tb, 0) |> ignore
        tb.CloseRegion()
        i + 1)

    let inline Fragment (fragment: RenderFragment) = Node(fun _ tb i ->
        tb.OpenRegion(i)
        fragment.Invoke(tb)
        tb.CloseRegion()
        i + 1)
