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

open System
open System.Collections.Generic
open FSharp.Reflection
open Microsoft.AspNetCore.Components

/// HTML fragment.
/// [category: HTML]
type Node = delegate of obj * Rendering.RenderTreeBuilder * (Type -> int * (obj -> int)) * int -> int

module Node =

    let inline Empty() = Node(fun _ _ _ i -> i)

    let MakeMatchCache() =
        let matchCache = Dictionary<Type, _>()
        fun (ty: Type) ->
            match matchCache.TryGetValue(ty) with
            | true, x -> x
            | false, _ ->
                let caseCount = FSharpType.GetUnionCases(ty, true).Length
                let r = FSharpValue.PreComputeUnionTagReader(ty)
                let v = (caseCount, r)
                matchCache.[ty] <- v
                v

    let inline Elt name (attrs: seq<Attr>) (children: seq<Node>) = Node(fun comp tb matchCache i ->
        tb.OpenElement(i, name)
        let mutable i = i + 1
        for attr in attrs do
            i <- attr.Invoke(comp, tb, matchCache, i)
        for node in children do
            i <- node.Invoke(comp, tb, matchCache, i)
        tb.CloseElement()
        i)

    let inline Text (text: string) = Node(fun _ tb _ i ->
        tb.AddContent(i, text)
        i + 1)

    let inline RawHtml (html: string) = Node(fun _ tb _ i ->
        tb.AddMarkupContent(i, html)
        i + 1)

    let inline Cond (cond: bool) ([<InlineIfLambda>] node: Node) = Node(fun comp tb matchCache i ->
        tb.OpenRegion(if cond then i else i + 1)
        node.Invoke(comp, tb, matchCache, 0) |> ignore
        tb.CloseRegion()
        i + 2)

    let inline Match (value: 'T) ([<InlineIfLambda>] node: Node) = Node(fun comp tb matchCache i ->
        let caseCount, getMatchedCase = matchCache typeof<'T>
        let matchedCase = getMatchedCase value
        tb.OpenRegion(i + matchedCase)
        node.Invoke(comp, tb, matchCache, 0) |> ignore
        tb.CloseRegion()
        i + caseCount)

    let inline Concat (nodes: seq<Node>) = Node(fun comp tb matchCache i ->
        let mutable i = i
        for node in nodes do
            i <- node.Invoke(comp, tb, matchCache, i)
        i)

    let inline ForEach (items: seq<'T>) ([<InlineIfLambda>] mkNode: 'T -> Node) = Node(fun comp tb matchCache i ->
        tb.OpenRegion(i)
        for item in items do
            (mkNode item).Invoke(comp, tb, matchCache, 0) |> ignore
        tb.CloseRegion()
        i + 1)

    let inline Fragment (fragment: RenderFragment) = Node(fun _ tb _ i ->
        tb.OpenRegion(i)
        fragment.Invoke(tb)
        tb.CloseRegion()
        i + 1)
