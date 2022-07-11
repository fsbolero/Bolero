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

/// <summary>Render the current element or component's reference.</summary>
/// <param name="receiver">The containing component.</param>
/// <param name="builder">The rendering builder.</param>
/// <param name="sequence">The rendering sequence number.</param>
type RefContent = delegate of receiver: obj * builder: Rendering.RenderTreeBuilder * sequence: int -> int

/// <summary>Render the ChildContent attribute.</summary>
/// <param name="receiver">The containing component.</param>
/// <param name="builder">The rendering builder.</param>
/// <param name="sequence">The rendering sequence number.</param>
type ChildContentAttr = delegate of receiver: obj * builder: Rendering.RenderTreeBuilder * sequence: int -> int

/// <summary>
/// Render the current element or component's key, reference and child content.
/// The child content may be either direct children at the end, or a ChildContent attribute at the beginning.
/// </summary>
/// <param name="receiver">The containing component.</param>
/// <param name="builder">The rendering builder.</param>
/// <param name="sequence">The rendering sequence number.</param>
type ChildAndRefContent = delegate of receiver: obj * builder: Rendering.RenderTreeBuilder * sequence: int -> int

type [<Struct; NoComparison; NoEquality>] AttrBuilder =
    member inline _.Yield([<InlineIfLambda>] attr: Attr) = attr
    member inline _.Delay([<InlineIfLambda>] a: unit -> Attr) = Attr(fun c b i -> a().Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Attr) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))

type [<Struct; NoComparison; NoEquality>] ConcatBuilder =
    member inline _.Yield([<InlineIfLambda>] node: Node) = node
    member inline this.Yield(text: string) =
        this.Yield(Node(fun _ b i ->
            b.AddContent(i, text)
            i + 1))
    member inline this.Yield(fragment: RenderFragment) =
        this.Yield(Node(fun _ b i ->
            b.AddContent(i, fragment)
            i + 1))
    member inline this.Yield(eb: ElementBuilder) =
        this.Yield(Node(fun c b i ->
            b.OpenElement(i, eb.Name)
            b.CloseElement()
            i + 1))
    member inline this.Yield(_comp: ComponentBuilder<'T>) =
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentBuilder) =
        this.Yield(Node(fun c b i ->
            b.OpenComponent(i, comp.Type)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentWithAttrsBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            b.CloseComponent()
            i))
    member inline this.Yield(comp: ComponentWithAttrsAndNoChildrenBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            b.CloseComponent()
            i))

    member inline _.Delay([<InlineIfLambda>] n: unit -> Node) = Node(fun c b i -> n().Invoke(c, b, i))

    member inline _.Combine([<InlineIfLambda>] x1: Node, [<InlineIfLambda>] x2: Node) =
        Node(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))

    member inline this.For(s: seq<'T>, [<InlineIfLambda>] f: 'T -> Node) =
        this.Yield(Node.ForEach s f)

// This is the only builder where generated code becomes worse if it is a struct,
// but since the vast majority of its uses are stored in a global, it's not too big a deal to make it a class.
and [<Sealed; NoComparison; NoEquality>] ElementBuilder =
    val public Name: string
    new(name) = { Name = name }

    member inline _.Yield([<InlineIfLambda>] attr: Attr) = attr
    member inline _.Yield([<InlineIfLambda>] ref: RefContent) = ref
    member inline _.Yield([<InlineIfLambda>] node: Node) = node
    member inline this.Yield(text: string) =
        this.Yield(Node(fun _ b i ->
            b.AddContent(i, text)
            i + 1))
    member inline this.Yield(fragment: RenderFragment) =
        this.Yield(Node(fun _ b i ->
            b.AddContent(i, fragment)
            i + 1))
    member inline this.Yield(eb: ElementBuilder) =
        this.Yield(Node(fun c b i ->
            b.OpenElement(i, eb.Name)
            b.CloseElement()
            i + 1))
    member inline this.Yield(_comp: ComponentBuilder<'T>) =
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentBuilder) =
        this.Yield(Node(fun c b i ->
            b.OpenComponent(i, comp.Type)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentWithAttrsBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            b.CloseComponent()
            i))
    member inline this.Yield(comp: ComponentWithAttrsAndNoChildrenBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            b.CloseComponent()
            i))

    member inline _.Delay([<InlineIfLambda>] a: unit -> Attr) = Attr(fun c b i -> a().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] r: unit -> RefContent) = RefContent(fun c b i -> r().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] r: unit -> ChildAndRefContent) = ChildAndRefContent(fun c b i -> r().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] n: unit -> Node) = Node(fun c b i -> n().Invoke(c, b, i))

    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Attr) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: RefContent) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: ChildAndRefContent) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))


    member inline _.Combine([<InlineIfLambda>] x1: Node, [<InlineIfLambda>] x2: Node) =
        Node(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))

    member inline this.For(s: seq<'T>, [<InlineIfLambda>] f: 'T -> Node) =
        this.Yield(Node.ForEach s f)

    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Node) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: RefContent, [<InlineIfLambda>] x2: Node) =
        ChildAndRefContent(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))

    member inline _.Yield(ref: HtmlRef) = RefContent(fun _ b i -> ref.Render(b, i))

    member inline this.Run([<InlineIfLambda>] content: Node) =
        Node(fun c b i ->
            b.OpenElement(i, this.Name)
            let i = content.Invoke(c, b, i + 1)
            b.CloseElement()
            i)

    member inline this.Run([<InlineIfLambda>] content: Attr) =
        Node(fun c b i ->
            b.OpenElement(i, this.Name)
            let i = content.Invoke(c, b, i + 1)
            b.CloseElement()
            i)

    member inline this.Run([<InlineIfLambda>] content: RefContent) =
        Node(fun c b i ->
            b.OpenElement(i, this.Name)
            let i = content.Invoke(c, b, i + 1)
            b.CloseElement()
            i)

    member inline this.Run([<InlineIfLambda>] content: ChildAndRefContent) =
        Node(fun c b i ->
            b.OpenElement(i, this.Name)
            let i = content.Invoke(c, b, i + 1)
            b.CloseElement()
            i)

and [<Struct; NoComparison; NoEquality>] ComponentBuilder<'T when 'T :> IComponent> =

    member inline _.Yield([<InlineIfLambda>] attr: Attr) = attr
    member inline _.Yield([<InlineIfLambda>] ref: RefContent) = ref
    member inline _.Yield([<InlineIfLambda>] node: Node) = node
    member inline this.Yield(text: string) =
        this.Yield(Node(fun _ b i ->
            b.AddContent(i, text)
            i + 1))
    member inline this.Yield(fragment: RenderFragment) =
        this.Yield(Node(fun _ b i ->
            b.AddContent(i, fragment)
            i + 1))
    member inline this.Yield(eb: ElementBuilder) =
        this.Yield(Node(fun c b i ->
            b.OpenElement(i, eb.Name)
            b.CloseElement()
            i + 1))
    member inline this.Yield(_comp: ComponentBuilder<'T>) =
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentBuilder) =
        this.Yield(Node(fun c b i ->
            b.OpenComponent(i, comp.Type)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentWithAttrsBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            b.CloseComponent()
            i))
    member inline this.Yield(comp: ComponentWithAttrsAndNoChildrenBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            b.CloseComponent()
            i))

    member inline _.Delay([<InlineIfLambda>] a: unit -> Attr) = Attr(fun c b i -> a().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] r: unit -> RefContent) = RefContent(fun c b i -> r().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] r: unit -> ChildAndRefContent) = ChildAndRefContent(fun c b i -> r().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] n: unit -> Node) = Node(fun c b i -> n().Invoke(c, b, i))

    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Attr) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: RefContent) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: ChildAndRefContent) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))


    member inline _.Combine([<InlineIfLambda>] x1: Node, [<InlineIfLambda>] x2: Node) =
        Node(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))

    member inline this.For(s: seq<'T>, [<InlineIfLambda>] f: 'T -> Node) =
        this.Yield(Node.ForEach s f)

    member inline _.WrapNode([<InlineIfLambda>] n: Node) =
        ChildContentAttr(fun c b i ->
            b.AddAttribute(i, "ChildContent", RenderFragment(fun b ->
                n.Invoke(c, b, 0) |> ignore))
            i + 1)

    member inline _.Delay([<InlineIfLambda>] a: unit -> ChildContentAttr) = ChildContentAttr(fun c b i -> a().Invoke(c, b, i))

    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: ChildContentAttr) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: RefContent, [<InlineIfLambda>] x2: ChildContentAttr) =
        ChildAndRefContent(fun c b i ->
            // Swap x2 and x1, because the ChildContent attr must come before the key and ref.
            let i = x2.Invoke(c, b, i)
            x1.Invoke(c, b, i))

    member inline this.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Node) =
        this.Combine(x1, this.WrapNode(x2))
    member inline this.Combine([<InlineIfLambda>] x1: RefContent, [<InlineIfLambda>] x2: Node) =
        this.Combine(x1, this.WrapNode(x2))

    member inline _.Yield(ref: Ref<'T>) = RefContent(fun _ b i -> ref.Render(b, i))

    member inline this.Run([<InlineIfLambda>] x: ChildContentAttr) =
        Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = x.Invoke(c, b, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Attr) =
        Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = x.Invoke(c, b, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: RefContent) =
        Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = x.Invoke(c, b, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: ChildAndRefContent) =
        Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = x.Invoke(c, b, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Node) =
        this.Run(this.WrapNode(x))

and [<Struct; NoComparison; NoEquality>] ComponentBuilder =

    val public Type : Type
    new(ty: Type) = { Type = ty }

    member inline _.Yield([<InlineIfLambda>] attr: Attr) = attr
    member inline _.Yield([<InlineIfLambda>] ref: RefContent) = ref
    member inline _.Yield([<InlineIfLambda>] node: Node) = node
    member inline this.Yield(text: string) =
        this.Yield(Node(fun _ b i ->
            b.AddContent(i, text)
            i + 1))
    member inline this.Yield(fragment: RenderFragment) =
        this.Yield(Node(fun _ b i ->
            b.AddContent(i, fragment)
            i + 1))
    member inline this.Yield(eb: ElementBuilder) =
        this.Yield(Node(fun c b i ->
            b.OpenElement(i, eb.Name)
            b.CloseElement()
            i + 1))
    member inline this.Yield(_comp: ComponentBuilder<'T>) =
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentBuilder) =
        this.Yield(Node(fun c b i ->
            b.OpenComponent(i, comp.Type)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentWithAttrsBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            b.CloseComponent()
            i))
    member inline this.Yield(comp: ComponentWithAttrsAndNoChildrenBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            b.CloseComponent()
            i))

    member inline _.Delay([<InlineIfLambda>] a: unit -> Attr) = Attr(fun c b i -> a().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] r: unit -> RefContent) = RefContent(fun c b i -> r().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] r: unit -> ChildAndRefContent) = ChildAndRefContent(fun c b i -> r().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] n: unit -> Node) = Node(fun c b i -> n().Invoke(c, b, i))


    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Attr) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: RefContent) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: ChildAndRefContent) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))


    member inline _.Combine([<InlineIfLambda>] x1: Node, [<InlineIfLambda>] x2: Node) =
        Node(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))

    member inline this.For(s: seq<'T>, [<InlineIfLambda>] f: 'T -> Node) =
        this.Yield(Node.ForEach s f)

    member inline _.WrapNode([<InlineIfLambda>] n: Node) =
        ChildContentAttr(fun c b i ->
            b.AddAttribute(i, "ChildContent", RenderFragment(fun b ->
                n.Invoke(c, b, 0) |> ignore))
            i + 1)

    member inline _.Delay([<InlineIfLambda>] a: unit -> ChildContentAttr) = ChildContentAttr(fun c b i -> a().Invoke(c, b, i))

    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: ChildContentAttr) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: RefContent, [<InlineIfLambda>] x2: ChildContentAttr) =
        ChildAndRefContent(fun c b i ->
            // Swap x2 and x1, because the ChildContent attr must come before the key and ref.
            let i = x2.Invoke(c, b, i)
            x1.Invoke(c, b, i))

    member inline this.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Node) =
        this.Combine(x1, this.WrapNode(x2))
    member inline this.Combine([<InlineIfLambda>] x1: RefContent, [<InlineIfLambda>] x2: Node) =
        this.Combine(x1, this.WrapNode(x2))

    member inline _.Yield(ref: Ref) = RefContent(fun _ b i -> ref.Render(b, i))

    member inline this.Run([<InlineIfLambda>] x: ChildContentAttr) =
        let ty = this.Type
        Node(fun c b i ->
            b.OpenComponent(i, ty)
            let i = x.Invoke(c, b, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Attr) =
        let ty = this.Type
        Node(fun c b i ->
            b.OpenComponent(i, ty)
            let i = x.Invoke(c, b, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: RefContent) =
        let ty = this.Type
        Node(fun c b i ->
            b.OpenComponent(i, ty)
            let i = x.Invoke(c, b, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: ChildAndRefContent) =
        let ty = this.Type
        Node(fun c b i ->
            b.OpenComponent(i, ty)
            let i = x.Invoke(c, b, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Node) =
        this.Run(this.WrapNode(x))

and [<Struct; NoComparison; NoEquality>] ComponentWithAttrsAndNoChildrenBuilder<'T when 'T :> IComponent> =
    val public Attrs : Attr
    new (attrs: Attr) = { Attrs = attrs }

    member inline _.Yield([<InlineIfLambda>] attr: Attr) = attr
    member inline _.Yield([<InlineIfLambda>] ref: RefContent) = ref
    member inline _.Yield([<InlineIfLambda>] node: Node) = node
    member inline this.Yield(text: string) =
        this.Yield(Node(fun _ b i ->
            b.AddContent(i, text)
            i + 1))
    member inline this.Yield(fragment: RenderFragment) =
        this.Yield(Node(fun _ b i ->
            b.AddContent(i, fragment)
            i + 1))
    member inline this.Yield(eb: ElementBuilder) =
        this.Yield(Node(fun c b i ->
            b.OpenElement(i, eb.Name)
            b.CloseElement()
            i + 1))
    member inline this.Yield(_comp: ComponentBuilder<'T>) =
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentBuilder) =
        this.Yield(Node(fun c b i ->
            b.OpenComponent(i, comp.Type)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentWithAttrsBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            b.CloseComponent()
            i))
    member inline this.Yield(comp: ComponentWithAttrsAndNoChildrenBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            b.CloseComponent()
            i))

    member inline _.Delay([<InlineIfLambda>] a: unit -> Attr) = Attr(fun c b i -> a().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] r: unit -> RefContent) = RefContent(fun c b i -> r().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] r: unit -> ChildAndRefContent) = ChildAndRefContent(fun c b i -> r().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] n: unit -> Node) = Node(fun c b i -> n().Invoke(c, b, i))


    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Attr) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: RefContent) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: ChildAndRefContent) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))


    member inline _.Combine([<InlineIfLambda>] x1: Node, [<InlineIfLambda>] x2: Node) =
        Node(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))

    member inline this.For(s: seq<'T>, [<InlineIfLambda>] f: 'T -> Node) =
        this.Yield(Node.ForEach s f)

    member inline _.Yield(ref: Ref<'T>) = RefContent(fun _ b i -> ref.Render(b, i))

    member inline this.Run([<InlineIfLambda>] x: Attr) =
        let attrs = this.Attrs
        Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            let i = x.Invoke(c, b, i)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: RefContent) =
        let attrs = this.Attrs
        Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            let i = x.Invoke(c, b, i)
            b.CloseComponent()
            i)

and [<Struct; NoComparison; NoEquality>] ComponentWithAttrsBuilder<'T when 'T :> IComponent> =
    val public Attrs : Attr
    new (attrs: Attr) = { Attrs = attrs }

    member inline _.Yield([<InlineIfLambda>] attr: Attr) = attr
    member inline _.Yield([<InlineIfLambda>] ref: RefContent) = ref
    member inline _.Yield([<InlineIfLambda>] node: Node) = node
    member inline this.Yield(text: string) =
        this.Yield(Node(fun _ b i ->
            b.AddContent(i, text)
            i + 1))
    member inline this.Yield(fragment: RenderFragment) =
        this.Yield(Node(fun _ b i ->
            b.AddContent(i, fragment)
            i + 1))
    member inline this.Yield(eb: ElementBuilder) =
        this.Yield(Node(fun c b i ->
            b.OpenElement(i, eb.Name)
            b.CloseElement()
            i + 1))
    member inline this.Yield(_comp: ComponentBuilder<'T>) =
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentBuilder) =
        this.Yield(Node(fun c b i ->
            b.OpenComponent(i, comp.Type)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentWithAttrsBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            b.CloseComponent()
            i))
    member inline this.Yield(comp: ComponentWithAttrsAndNoChildrenBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            b.CloseComponent()
            i))

    member inline _.Delay([<InlineIfLambda>] a: unit -> Attr) = Attr(fun c b i -> a().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] r: unit -> RefContent) = RefContent(fun c b i -> r().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] r: unit -> ChildAndRefContent) = ChildAndRefContent(fun c b i -> r().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] n: unit -> Node) = Node(fun c b i -> n().Invoke(c, b, i))


    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Attr) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: RefContent) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: ChildAndRefContent) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))


    member inline _.Combine([<InlineIfLambda>] x1: Node, [<InlineIfLambda>] x2: Node) =
        Node(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))

    member inline this.For(s: seq<'T>, [<InlineIfLambda>] f: 'T -> Node) =
        this.Yield(Node.ForEach s f)

    member inline _.WrapNode([<InlineIfLambda>] n: Node) =
        ChildContentAttr(fun c b i ->
            b.AddAttribute(i, "ChildContent", RenderFragment(fun b ->
                n.Invoke(c, b, 0) |> ignore))
            i + 1)

    member inline _.Delay([<InlineIfLambda>] a: unit -> ChildContentAttr) = ChildContentAttr(fun c b i -> a().Invoke(c, b, i))

    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: ChildContentAttr) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: RefContent, [<InlineIfLambda>] x2: ChildContentAttr) =
        ChildAndRefContent(fun c b i ->
            // Swap x2 and x1, because the ChildContent attr must come before the key and ref.
            let i = x2.Invoke(c, b, i)
            x1.Invoke(c, b, i))

    member inline this.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Node) =
        this.Combine(x1, this.WrapNode(x2))
    member inline this.Combine([<InlineIfLambda>] x1: RefContent, [<InlineIfLambda>] x2: Node) =
        this.Combine(x1, this.WrapNode(x2))

    member inline _.Yield(ref: Ref<'T>) = RefContent(fun _ b i -> ref.Render(b, i))

    member inline this.Run([<InlineIfLambda>] x: ChildContentAttr) =
        let attrs = this.Attrs
        Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            let i = x.Invoke(c, b, i)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Attr) =
        let attrs = this.Attrs
        Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            let i = x.Invoke(c, b, i)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: RefContent) =
        let attrs = this.Attrs
        Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            let i = x.Invoke(c, b, i)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: ChildAndRefContent) =
        let attrs = this.Attrs
        Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            let i = x.Invoke(c, b, i)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Node) =
        this.Run(this.WrapNode(x))

type VirtualizeItemsDeclaration<'T> = delegate of Rendering.RenderTreeBuilder * int -> int

type [<Struct; NoComparison; NoEquality>] VirtualizeBuilder<'Item> =

    member inline _.Yield([<InlineIfLambda>] attr: Attr) = attr
    member inline _.Yield([<InlineIfLambda>] ref: RefContent) = ref
    member inline _.Yield([<InlineIfLambda>] node: Node) = node
    member inline this.Yield(text: string) =
        this.Yield(Node(fun _ b i ->
            b.AddContent(i, text)
            i + 1))
    member inline this.Yield(fragment: RenderFragment) =
        this.Yield(Node(fun _ b i ->
            b.AddContent(i, fragment)
            i + 1))
    member inline this.Yield(eb: ElementBuilder) =
        this.Yield(Node(fun c b i ->
            b.OpenElement(i, eb.Name)
            b.CloseElement()
            i + 1))
    member inline this.Yield(_comp: ComponentBuilder<'T>) =
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentBuilder) =
        this.Yield(Node(fun c b i ->
            b.OpenComponent(i, comp.Type)
            b.CloseComponent()
            i + 1))
    member inline this.Yield(comp: ComponentWithAttrsBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            b.CloseComponent()
            i))
    member inline this.Yield(comp: ComponentWithAttrsAndNoChildrenBuilder<'T>) =
        let attrs : Attr = comp.Attrs
        this.Yield(Node(fun c b i ->
            b.OpenComponent<'T>(i)
            let i = attrs.Invoke(c, b, i + 1)
            b.CloseComponent()
            i))


    member inline _.Delay([<InlineIfLambda>] a: unit -> Attr) = Attr(fun c b i -> a().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] r: unit -> RefContent) = RefContent(fun c b i -> r().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] r: unit -> ChildAndRefContent) = ChildAndRefContent(fun c b i -> r().Invoke(c, b, i))
    member inline _.Delay([<InlineIfLambda>] n: unit -> Node) = Node(fun c b i -> n().Invoke(c, b, i))


    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Attr) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: RefContent) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: ChildAndRefContent) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))


    member inline _.Combine([<InlineIfLambda>] x1: Node, [<InlineIfLambda>] x2: Node) =
        Node(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))

    member inline this.For(s: seq<'T>, [<InlineIfLambda>] f: 'T -> Node) =
        this.Yield(Node.ForEach s f)

    member inline _.WrapNode([<InlineIfLambda>] n: Node) =
        ChildContentAttr(fun c b i ->
            b.AddAttribute(i, "ChildContent", RenderFragment(fun b ->
                n.Invoke(c, b, 0) |> ignore))
            i + 1)

    member inline _.Delay([<InlineIfLambda>] a: unit -> ChildContentAttr) = ChildContentAttr(fun c b i -> a().Invoke(c, b, i))

    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: ChildContentAttr) =
        Attr(fun c b i ->
            let i = x1.Invoke(c, b, i)
            x2.Invoke(c, b, i))
    member inline _.Combine([<InlineIfLambda>] x1: RefContent, [<InlineIfLambda>] x2: ChildContentAttr) =
        ChildAndRefContent(fun c b i ->
            // Swap x2 and x1, because the ChildContent attr must come before the key and ref.
            let i = x2.Invoke(c, b, i)
            x1.Invoke(c, b, i))

    member inline this.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Node) =
        this.Combine(x1, this.WrapNode(x2))
    member inline this.Combine([<InlineIfLambda>] x1: RefContent, [<InlineIfLambda>] x2: Node) =
        this.Combine(x1, this.WrapNode(x2))

    member inline _.Yield(ref: Ref<'T>) = RefContent(fun _ b i -> ref.Render(b, i))

    member inline this.Run([<InlineIfLambda>] x: ChildContentAttr) =
        Node(fun c b i ->
            b.OpenComponent<Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<'Item>>(i)
            let i = x.Invoke(c, b, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Attr) =
        Node(fun c b i ->
            b.OpenComponent<Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<'Item>>(i)
            let i = x.Invoke(c, b, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: RefContent) =
        Node(fun c b i ->
            b.OpenComponent<Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<'Item>>(i)
            let i = x.Invoke(c, b, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: ChildAndRefContent) =
        Node(fun c b i ->
            b.OpenComponent<Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<'Item>>(i)
            let i = x.Invoke(c, b, i + 1)
            b.CloseComponent()
            i)

    member inline this.Run([<InlineIfLambda>] x: Node) =
        this.Run(this.WrapNode(x))

    member inline this.Bind([<InlineIfLambda>] items: VirtualizeItemsDeclaration<'Item>, [<InlineIfLambda>] cont: 'Item -> Node) =
        ChildContentAttr(fun c b i ->
            b.AddAttribute(i, "ItemContent", RenderFragment<'Item>(fun ctx ->
                RenderFragment(fun rt ->
                    (cont ctx).Invoke(c, rt, 0) |> ignore)))
            items.Invoke(b, i + 1))
