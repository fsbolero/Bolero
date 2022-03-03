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

namespace Bolero.Builders

open System
open Microsoft.AspNetCore.Components
open Bolero

/// Render the current element or component's key and reference.
type KeyAndRef = delegate of obj * Rendering.RenderTreeBuilder * (Type -> int * (obj -> int)) * int -> int

/// Render the ChildContent attribute.
type ChildContentAttr = delegate of obj * Rendering.RenderTreeBuilder * (Type -> int * (obj -> int)) * int -> int

/// Render the current element or component's key, reference and child content.
/// The child content may be either direct children at the end, or a ChildContent attribute at the beginning.
type ChildKeyAndRefContent = delegate of obj * Rendering.RenderTreeBuilder * (Type -> int * (obj -> int)) * int -> int

type NodeBuilderBase() =
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

and [<AbstractClass>] NodeBuilderWithDirectChildrenBase() =
    inherit NodeBuilderBase()

    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Node) =
        Attr(fun c b m i ->
            let i = x1.Invoke(c, b, m, i)
            x2.Invoke(c, b, m, i))
    member inline _.Combine([<InlineIfLambda>] x1: KeyAndRef, [<InlineIfLambda>] x2: Node) =
        ChildKeyAndRefContent(fun c b m i ->
            let i = x1.Invoke(c, b, m, i)
            x2.Invoke(c, b, m, i))

and ElementBuilder(name: string) =
    inherit NodeBuilderWithDirectChildrenBase()

    member _.Name = name

    member inline _.Yield(ref: HtmlRef) = KeyAndRef(fun _ b _ i -> ref.Render(b, i))

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

    member inline this.Run([<InlineIfLambda>] content: KeyAndRef) =
        Node(fun c b m i ->
            b.OpenElement(i, this.Name)
            let i = content.Invoke(c, b, m, i + 1)
            b.CloseElement()
            i)

    member inline this.Run([<InlineIfLambda>] content: ChildKeyAndRefContent) =
        Node(fun c b m i ->
            b.OpenElement(i, this.Name)
            let i = content.Invoke(c, b, m, i + 1)
            b.CloseElement()
            i)

and NodeBuilderWithChildContentBase() =
    inherit NodeBuilderBase()

    static member inline WrapNode([<InlineIfLambda>] n: Node) =
        ChildContentAttr(fun c b m i ->
            b.AddAttribute(i, "ChildContent", RenderFragment(fun b ->
                n.Invoke(c, b, m, 0) |> ignore))
            i + 1)

    member inline _.Delay([<InlineIfLambda>] a: unit -> ChildContentAttr) = ChildContentAttr(fun c b m i -> a().Invoke(c, b, m, i))

    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: ChildContentAttr) =
        Attr(fun c b m i ->
            let i = x1.Invoke(c, b, m, i)
            x2.Invoke(c, b, m, i))
    member inline _.Combine([<InlineIfLambda>] x1: KeyAndRef, [<InlineIfLambda>] x2: ChildContentAttr) =
        ChildKeyAndRefContent(fun c b m i ->
            // Swap x2 and x1, because the ChildContent attr must come before the key and ref.
            let i = x2.Invoke(c, b, m, i)
            x1.Invoke(c, b, m, i))

    member inline this.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Node) =
        this.Combine(x1, NodeBuilderWithChildContentBase.WrapNode(x2))
    member inline this.Combine([<InlineIfLambda>] x1: KeyAndRef, [<InlineIfLambda>] x2: Node) =
        this.Combine(x1, NodeBuilderWithChildContentBase.WrapNode(x2))

and ComponentBuilder<'T when 'T :> IComponent>() =
    inherit NodeBuilderWithChildContentBase()

    member inline _.Yield(ref: Ref<'T>) = KeyAndRef(fun _ b _ i -> ref.Render(b, i))

    member inline this.Run([<InlineIfLambda>] x: ChildContentAttr) =
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

    member inline this.Run([<InlineIfLambda>] x: KeyAndRef) =
        Node(fun c b m i ->
            b.OpenComponent<'T>(i)
            let i = x.Invoke(c, b, m, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: ChildKeyAndRefContent) =
        Node(fun c b m i ->
            b.OpenComponent<'T>(i)
            let i = x.Invoke(c, b, m, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Node) =
        this.Run(NodeBuilderWithChildContentBase.WrapNode(x))

and ComponentBuilder(ty: Type) =
    inherit NodeBuilderWithChildContentBase()

    member inline _.Yield(ref: Ref) = KeyAndRef(fun _ b _ i -> ref.Render(b, i))

    member _.Type = ty

    member inline this.Run([<InlineIfLambda>] x: ChildContentAttr) =
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

    member inline this.Run([<InlineIfLambda>] x: KeyAndRef) =
        Node(fun c b m i ->
            b.OpenComponent(i, this.Type)
            let i = x.Invoke(c, b, m, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: ChildKeyAndRefContent) =
        Node(fun c b m i ->
            b.OpenComponent(i, this.Type)
            let i = x.Invoke(c, b, m, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Node) =
        this.Run(NodeBuilderWithChildContentBase.WrapNode(x))

and ComponentWithAttrsAndNoChildrenBuilder<'T when 'T :> IComponent>(attrs: Attr) =
    inherit NodeBuilderBase()

    member _.Attrs = attrs

    member inline _.Yield(ref: Ref<'T>) = KeyAndRef(fun _ b _ i -> ref.Render(b, i))

    member inline this.Run([<InlineIfLambda>] x: Attr) =
        let attrs = this.Attrs
        Node(fun c b m i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, m, i + 1)
            let i = x.Invoke(c, b, m, i)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: KeyAndRef) =
        let attrs = this.Attrs
        Node(fun c b m i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, m, i + 1)
            let i = x.Invoke(c, b, m, i)
            b.CloseComponent()
            i)

and ComponentWithAttrsBuilder<'T when 'T :> IComponent>(attrs: Attr) =
    inherit NodeBuilderWithChildContentBase()

    member _.Attrs = attrs

    member inline _.Yield(ref: Ref<'T>) = KeyAndRef(fun _ b _ i -> ref.Render(b, i))

    member inline this.Run([<InlineIfLambda>] x: ChildContentAttr) =
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

    member inline this.Run([<InlineIfLambda>] x: KeyAndRef) =
        let attrs = this.Attrs
        Node(fun c b m i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, m, i + 1)
            let i = x.Invoke(c, b, m, i)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: ChildKeyAndRefContent) =
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
        ChildContentAttr(fun c b m i ->
            b.AddAttribute(i, "ItemContent", RenderFragment<'Item>(fun ctx ->
                RenderFragment(fun rt ->
                    (cont ctx).Invoke(c, rt, m, 0) |> ignore)))
            items.Invoke(b, i + 1))
