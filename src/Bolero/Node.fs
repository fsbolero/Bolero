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
open Microsoft.AspNetCore.Components
open Microsoft.FSharp.Reflection

/// HTML attribute or Blazor component parameter.
/// Use `Bolero.Html.attr` or `(=>)` to create attributes.
/// [category: HTML]
type Attr = delegate of obj * Rendering.RenderTreeBuilder * (Type -> int * (obj -> int)) * int -> int

/// HTML fragment.
/// [category: HTML]
type Node = delegate of obj * Rendering.RenderTreeBuilder * (Type -> int * (obj -> int)) * int -> int

type Key = delegate of obj * Rendering.RenderTreeBuilder * (Type -> int * (obj -> int)) * int -> int

type RefRender = delegate of obj * Rendering.RenderTreeBuilder * (Type -> int * (obj -> int)) * int -> int

type ChildContent = delegate of obj * Rendering.RenderTreeBuilder * (Type -> int * (obj -> int)) * int -> int

module Attr =

    let inline Make (name: string) (value: 'T) = Attr(fun _ tb _ i ->
        tb.AddAttribute(i, name, box value)
        i + 1)

    let inline Attrs (attrs: seq<Attr>) = Attr(fun comp tb matchCache i ->
        let mutable i = i
        for attr in attrs do
            i <- attr.Invoke(comp, tb, matchCache, i)
        i)

    let inline Empty() = Attr(fun _ _ _ i -> i)

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

type [<AbstractClass>] Ref() =
    /// [omit]
    abstract Render : Rendering.RenderTreeBuilder * int -> int

type AttrBuilder() =
    member inline _.Yield([<InlineIfLambda>] attr: Attr) = attr
    member inline _.Delay([<InlineIfLambda>] a: unit -> Attr) = Attr(fun c b m i -> a().Invoke(c, b, m, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Attr) =
        Attr(fun c b m i -> x2.Invoke(c, b, m, x1.Invoke(c, b, m, i)))

type NodeBuilderBase() =
    member inline _.Yield([<InlineIfLambda>] attr: Attr) = attr
    member inline _.Yield([<InlineIfLambda>] key: Key) = key
    member inline _.Yield(ref: Ref) = RefRender(fun _ b _ i -> ref.Render(b, i))
    member inline _.Yield([<InlineIfLambda>] node: Node) = node
    member inline this.Yield(text: string) =
        this.Yield(Node(fun _ b _ i ->
            b.AddContent(i, text)
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

    member inline _.Delay([<InlineIfLambda>] a: unit -> Attr) = Attr(fun c b m i -> a().Invoke(c, b, m, i))
    member inline _.Delay([<InlineIfLambda>] k: unit -> Key) = Key(fun c b m i -> k().Invoke(c, b, m, i))
    member inline _.Delay([<InlineIfLambda>] r: unit -> RefRender) = RefRender(fun c b m i -> r().Invoke(c, b, m, i))
    member inline _.Delay([<InlineIfLambda>] n: unit -> Node) = Node(fun c b m i -> n().Invoke(c, b, m, i))

    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Attr) =
        Attr(fun c b m i -> x2.Invoke(c, b, m, x1.Invoke(c, b, m, i)))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Key) =
        Attr(fun c b m i -> x2.Invoke(c, b, m, x1.Invoke(c, b, m, i)))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: RefRender) =
        Attr(fun c b m i -> x2.Invoke(c, b, m, x1.Invoke(c, b, m, i)))

    member inline _.Combine([<InlineIfLambda>] x1: Key, [<InlineIfLambda>] x2: Key) =
        Key(fun c b m i -> x2.Invoke(c, b, m, x1.Invoke(c, b, m, i)))
    member inline _.Combine([<InlineIfLambda>] x1: Key, [<InlineIfLambda>] x2: RefRender) =
        Key(fun c b m i -> x2.Invoke(c, b, m, x1.Invoke(c, b, m, i)))


    member inline _.Combine([<InlineIfLambda>] x1: Node, [<InlineIfLambda>] x2: Node) =
        Node(fun c b m i -> x2.Invoke(c, b, m, x1.Invoke(c, b, m, i)))

    member inline this.For(s: seq<'T>, [<InlineIfLambda>] f: 'T -> Node) =
        this.Yield(Node.ForEach s f)

and [<AbstractClass>] NodeBuilderWithDirectChildrenBase() =
    inherit NodeBuilderBase()

    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Node) =
        Attr(fun c b m i -> x2.Invoke(c, b, m, x1.Invoke(c, b, m, i)))
    member inline _.Combine([<InlineIfLambda>] x1: Key, [<InlineIfLambda>] x2: Node) =
        Key(fun c b m i -> x2.Invoke(c, b, m, x1.Invoke(c, b, m, i)))
    member inline _.Combine([<InlineIfLambda>] x1: RefRender, [<InlineIfLambda>] x2: Node) =
        RefRender(fun c b m i -> x2.Invoke(c, b, m, x1.Invoke(c, b, m, i)))

and ElementBuilder(name: string) =
    inherit NodeBuilderWithDirectChildrenBase()

    member _.Name = name

    member inline this.Run([<InlineIfLambda>] content: Node) =
        Node(fun c b m i ->
            b.OpenElement(i, this.Name)
            let i = content.Invoke(c, b, m, i + 1)
            b.CloseElement()
            i)

    member inline this.Run([<InlineIfLambda>] content: Attr) =
        Node(fun c b m i ->
            b.OpenElement(i, this.Name)
            let i = content.Invoke(c, b, m, i + 1)
            b.CloseElement()
            i)

    member inline this.Run([<InlineIfLambda>] content: Key) =
        Node(fun c b m i ->
            b.OpenElement(i, this.Name)
            let i = content.Invoke(c, b, m, i + 1)
            b.CloseElement()
            i)

    member inline this.Run([<InlineIfLambda>] content: RefRender) =
        Node(fun c b m i ->
            b.OpenElement(i, this.Name)
            let i = content.Invoke(c, b, m, i + 1)
            b.CloseElement()
            i)

and NodeBuilderWithChildContentBase() =
    inherit NodeBuilderBase()

    static member inline WrapNode([<InlineIfLambda>] n: Node) =
        ChildContent(fun c b m i ->
            b.AddAttribute(i, "ChildContent", RenderFragment(fun b ->
                n.Invoke(c, b, m, 0) |> ignore))
            i + 1)

    member inline _.Delay([<InlineIfLambda>] a: unit -> ChildContent) = ChildContent(fun c b m i -> a().Invoke(c, b, m, i))

    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: ChildContent) =
        Attr(fun c b m i -> x2.Invoke(c, b, m, x1.Invoke(c, b, m, i)))
    member inline _.Combine([<InlineIfLambda>] x1: Key, [<InlineIfLambda>] x2: ChildContent) =
        Key(fun c b m i -> x2.Invoke(c, b, m, x1.Invoke(c, b, m, i)))
    member inline _.Combine([<InlineIfLambda>] x1: RefRender, [<InlineIfLambda>] x2: ChildContent) =
        RefRender(fun c b m i -> x2.Invoke(c, b, m, x1.Invoke(c, b, m, i)))

    member inline this.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Node) =
        this.Combine(x1, NodeBuilderWithChildContentBase.WrapNode(x2))
    member inline this.Combine([<InlineIfLambda>] x1: Key, [<InlineIfLambda>] x2: Node) =
        this.Combine(x1, NodeBuilderWithChildContentBase.WrapNode(x2))
    member inline this.Combine([<InlineIfLambda>] x1: RefRender, [<InlineIfLambda>] x2: Node) =
        this.Combine(x1, NodeBuilderWithChildContentBase.WrapNode(x2))

and ComponentBuilder<'T when 'T :> IComponent>() =
    inherit NodeBuilderWithChildContentBase()

    member inline this.Run([<InlineIfLambda>] x: ChildContent) =
        Node(fun c b m i ->
            b.OpenComponent<'T>(i)
            let i = x.Invoke(c, b, m, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Attr) =
        Node(fun c b m i ->
            b.OpenComponent<'T>(i)
            let i = x.Invoke(c, b, m, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Key) =
        Node(fun c b m i ->
            b.OpenComponent<'T>(i)
            let i = x.Invoke(c, b, m, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: RefRender) =
        Node(fun c b m i ->
            b.OpenComponent<'T>(i)
            let i = x.Invoke(c, b, m, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Node) =
        this.Run(NodeBuilderWithChildContentBase.WrapNode(x))

and ComponentBuilder(ty: Type) =
    inherit NodeBuilderWithChildContentBase()

    member _.Type = ty

    member inline this.Run([<InlineIfLambda>] x: ChildContent) =
        Node(fun c b m i ->
            b.OpenComponent(i, this.Type)
            let i = x.Invoke(c, b, m, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Attr) =
        Node(fun c b m i ->
            b.OpenComponent(i, this.Type)
            let i = x.Invoke(c, b, m, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Key) =
        Node(fun c b m i ->
            b.OpenComponent(i, this.Type)
            let i = x.Invoke(c, b, m, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: RefRender) =
        Node(fun c b m i ->
            b.OpenComponent(i, this.Type)
            let i = x.Invoke(c, b, m, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Node) =
        this.Run(NodeBuilderWithChildContentBase.WrapNode(x))

and ComponentWithAttrsBuilder<'T when 'T :> IComponent>(attrs: Attr) =
    inherit NodeBuilderWithChildContentBase()

    member _.Attrs = attrs

    member inline this.Run([<InlineIfLambda>] x: ChildContent) =
        let attrs = this.Attrs
        Node(fun c b m i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, m, i + 1)
            let i = x.Invoke(c, b, m, i)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Attr) =
        let attrs = this.Attrs
        Node(fun c b m i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, m, i + 1)
            let i = x.Invoke(c, b, m, i)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Key) =
        let attrs = this.Attrs
        Node(fun c b m i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, m, i + 1)
            let i = x.Invoke(c, b, m, i)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: RefRender) =
        let attrs = this.Attrs
        Node(fun c b m i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, m, i + 1)
            let i = x.Invoke(c, b, m, i)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Node) =
        this.Run(NodeBuilderWithChildContentBase.WrapNode(x))

type VirtualizeItemsDeclaration<'T> = delegate of Rendering.RenderTreeBuilder * int -> int

type VirtualizeBuilder<'Item>() =
    inherit ComponentBuilder<Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<'Item>>()

    member inline this.Bind([<InlineIfLambda>] items: VirtualizeItemsDeclaration<'Item>, [<InlineIfLambda>] cont: 'Item -> Node) =
        ChildContent(fun c b m i ->
            b.AddAttribute(i, "ItemContent", RenderFragment<'Item>(fun ctx ->
                RenderFragment(fun rt ->
                    (cont ctx).Invoke(c, rt, m, 0) |> ignore)))
            items.Invoke(b, i + 1))

module Html =

    let attrs = AttrBuilder()

    let concat = NodeBuilderBase()
