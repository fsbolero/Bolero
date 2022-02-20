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
open Microsoft.AspNetCore.Components

type Key = delegate of obj * Rendering.RenderTreeBuilder * (Type -> int * (obj -> int)) * int -> int

type RefRender = delegate of obj * Rendering.RenderTreeBuilder * (Type -> int * (obj -> int)) * int -> int

type ChildContent = delegate of obj * Rendering.RenderTreeBuilder * (Type -> int * (obj -> int)) * int -> int

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


