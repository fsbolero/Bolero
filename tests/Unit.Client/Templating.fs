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

module Bolero.Tests.Client.Templating

open System.Threading.Tasks
open Bolero
open Bolero.Html
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web
open Microsoft.JSInterop

type Inline = Template<"""
    <div class="inline">
        <span class="nodehole1">${NodeHole1}</span>
        <span class="nodehole2">${NodeHole2}</span>
        <span class="nodehole3-1">${NodeHole3}</span>
        <span class="nodehole3-2">${NodeHole3}</span>
        <span class="nodehole4-1">${NodeHole4}</span>
        <span class="nodehole4-2">${NodeHole4}</span>
        <span class="attrhole1 ${AttrHole1}"></span>
        <span class="attrhole2-1 ${AttrHole2}"></span>
        <span class="attrhole2-2 ${AttrHole2}"></span>
        <span class="attrhole3-1 ${AttrHole3}"></span>
        <span class="attrhole3-2">${AttrHole3}</span>
        <span class="attrhole4" data-value="${AttrHole4}" data-true="${AttrHole4True}" data-false="${AttrHole4False}"></span>
        <span class="fullattrhole" attr="${FullAttrHole}"></span>
    </div>
""">

type File = Template<"wwwroot/testtemplate.html">

type Events = Template<"""
    <div class="events">
        <button class="btn1" onclick="${Click1}">btn1</button>
        <button class="btn2" onclick="${Click2}">btn2</button>
        <button class="btn3" onclick="${Click1}">btn3</button>
        <span class="currentstate">${CurrentState}</span>
        <span class="position">${ClickPosition}</span>
    </div>
""">

type EventTester() =
    inherit Component()

    let mutable currentState = "not clicked"
    let mutable lastPosition = ""

    /// Must be a method because StateHasChanged is protected,
    /// and therefore not visible to lambdas.
    member this.OnClick(id, e: MouseEventArgs) =
        currentState <- $"clicked {id}"
        lastPosition <- $"{e.ClientX},{e.ClientY}"
        this.StateHasChanged()

    override this.Render() =
        Events()
            .Click1(fun e -> this.OnClick(1, e))
            .Click2(fun e -> this.OnClick(2, e))
            .CurrentState(currentState)
            .ClickPosition(lastPosition)
            .Elt()

type Binds = Template<"""
    <div class="binds">
    <!-- normal -->
        <input class="input1-1" bind="${Var1}">
        <input class="input1-2" bind="${Var1}">
        <textarea class="textarea1" bind="${Var1}"></textarea>
        <select class="select1" bind="${Var1}">
            <option value="hello">Hello</option>
            <option value="hi textarea">Hi textarea</option>
            <option value="hi select">Hi select</option>
        </select>
        <span class="display1">${Var1}</span>

        <input type="number" class="input2-1" bind="${Var2}">
        <input type="number" class="input2-2" bind="${Var2}">
        <span class="display2">${Var2}</span>

        <input type="number" class="input3-1" bind="${Var3}">
        <input type="number" class="input3-2" bind="${Var3}">
        <span class="display3">${Var3}</span>

        <input type="checkbox" class="input4-1" bind="${Var4}">
        <input type="checkbox" class="input4-2" bind="${Var4}">
        <span class="display4">${Var4}</span>

    <!-- onchange -->
        <input class="input-onchange1-1" bind-onchange="${VarOnchange1}">
        <input class="input-onchange1-2" bind-onchange="${VarOnchange1}">
        <textarea class="textarea-onchange1" bind-onchange="${VarOnchange1}"></textarea>
        <select class="select-onchange1" bind-onchange="${VarOnchange1}">
            <option value="hello">Hello</option>
            <option value="hi textarea">Hi textarea</option>
            <option value="hi select">Hi select</option>
        </select>
        <span class="display-onchange1">${VarOnchange1}</span>

        <input type="number" class="input-onchange2-1" bind-onchange="${VarOnchange2}">
        <input type="number" class="input-onchange2-2" bind-onchange="${VarOnchange2}">
        <span class="display-onchange2">${VarOnchange2}</span>

        <input type="number" class="input-onchange3-1" bind-onchange="${VarOnchange3}">
        <input type="number" class="input-onchange3-2" bind-onchange="${VarOnchange3}">
        <span class="display-onchange3">${VarOnchange3}</span>
    </div>
""">

type BindTester() =
    inherit Component()

    let mutable var1 = ""
    let mutable var2 = 0
    let mutable var3 = 0.
    let mutable var4 = false
    let mutable varOnchange1 = ""
    let mutable varOnchange2 = 0
    let mutable varOnchange3 = 0.

    member this.Var1
        with get() = var1
        and set v = var1 <- v; this.StateHasChanged()

    member this.Var2
        with get() = var2
        and set v = var2 <- v; this.StateHasChanged()

    member this.Var3
        with get() = var3
        and set v = var3 <- v; this.StateHasChanged()

    member this.Var4
        with get() = var4
        and set v = var4 <- v; this.StateHasChanged()

    member this.VarOnchange1
        with get() = varOnchange1
        and set v = varOnchange1 <- v; this.StateHasChanged()

    member this.VarOnchange2
        with get() = varOnchange2
        and set v = varOnchange2 <- v; this.StateHasChanged()

    member this.VarOnchange3
        with get() = varOnchange3
        and set v = varOnchange3 <- v; this.StateHasChanged()

    override this.Render() =
        Binds()
            .Var1(this.Var1, fun (v: string) -> this.Var1 <- v)
            .Var2(this.Var2, fun (v: int) -> this.Var2 <- v)
            .Var3(this.Var3, fun (v: float) -> this.Var3 <- v)
            .Var4(this.Var4, fun (v: bool) -> this.Var4 <- v)
            .VarOnchange1(this.VarOnchange1, fun (v: string) -> this.VarOnchange1 <- v)
            .VarOnchange2(this.VarOnchange2, fun (v: int) -> this.VarOnchange2 <- v)
            .VarOnchange3(this.VarOnchange3, fun (v: float) -> this.VarOnchange3 <- v)
            .Elt()

type Refs = Template<"""<button class="${Class}" ref="${MyRef}" onclick="${Click}">Initial ref content</button>""">

type RefTester() =
    inherit Component()

    let elt = HtmlRef()

    [<Inject>]
    member val JSRuntime = Unchecked.defaultof<IJSRuntime> with get, set

    override this.Render() =
        concat {
            Refs()
                .MyRef(elt)
                .Class("template-ref")
                .Click(fun _ ->
                    match elt.Value with
                    | Some elt -> this.JSRuntime.InvokeVoidAsync("setContent", elt, "Template ref is bound") |> ignore
                    | None -> ())
                .Elt()

            // Check that not passing the ref doesn't break anything.
            Refs().Elt()
        }

type TemplateForScopedCss = Template<"""
<span id="holed-must-be-scoped">${Text}</span>
<span id="non-holed-must-be-scoped"></span>
""">

type ScopedCssTester() =
    inherit Component()

    override _.CssScope = "dummy-scope"

    override _.Render() =
        TemplateForScopedCss().Text("content").Elt()

type ``Regression #11`` = Template<"""<span class="${Hole}">${Hole}</span>""">

type ``Regression #256`` = Template<"""
<svg width="200" height="200" version="1.1" viewBox="0 0 100 100" class="regression-256-with-holes" xmlns="http://www.w3.org/2000/svg">
    <circle cx="50" cy="50" r="45" fill="${CircleFill}" stroke="${CircleStroke}" stroke-width="3"/>
</svg>
<svg width="200" height="200" version="1.1" viewBox="0 0 100 100" class="regression-256-without-holes" xmlns="http://www.w3.org/2000/svg">
    <circle cx="50" cy="50" r="45" fill="red" stroke="black" stroke-width="3"/>
</svg>""">

let Tests() =
    div {
        attr.id "test-fixture-templating"
        Inline()
            .NodeHole1("NodeHole1 content")
            .NodeHole2(div { attr.``class`` "nodehole2-content"; "NodeHole2 content" })
            .NodeHole3("NodeHole3 content")
            .NodeHole4(div { attr.``class`` "nodehole4-content"; "NodeHole4 content" })
            .AttrHole1("attrhole1-content")
            .AttrHole2("attrhole2-content")
            .AttrHole3("attrhole3-content")
            .AttrHole4(5678)
            .AttrHole4True(true)
            .AttrHole4False(false)
            .FullAttrHole([attr.id "fullattrhole-content"; "data-fullattrhole" => 1234])
            .Elt()
        File()
            .SimpleHole(div { attr.``class`` "file-hole" })
            .Elt()
        File.Nested1()
            .SimpleHole(div { attr.``class`` "nested-hole" })
            .Elt()
        File.Nested2()
            .Elt()
        ``Regression #11``()
            .Hole("regression-11")
            .Elt()
        ``Regression #256``()
            .CircleFill("red")
            .CircleStroke("black")
            .Elt()
        comp<EventTester>
        comp<BindTester>
        comp<RefTester>
        comp<ScopedCssTester>
    }
