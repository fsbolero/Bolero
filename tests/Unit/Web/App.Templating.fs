module Bolero.Tests.Web.App.Templating

open Bolero
open Bolero.Html
open Microsoft.AspNetCore.Blazor

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
    </div>
""">

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
    member this.OnClick(id, e: UIMouseEventArgs) =
        currentState <- sprintf "clicked %i" id
        lastPosition <- sprintf "%i,%i" e.ClientX e.ClientY
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
            .VarOnchange1(this.VarOnchange1, fun (v: string) -> this.VarOnchange1 <- v)
            .VarOnchange2(this.VarOnchange2, fun (v: int) -> this.VarOnchange2 <- v)
            .VarOnchange3(this.VarOnchange3, fun (v: float) -> this.VarOnchange3 <- v)
            .Elt()

let Tests() =
    div [attr.id "test-fixture-templating"] [
        Inline()
            .NodeHole1("NodeHole1 content")
            .NodeHole2(div [attr.classes ["nodehole2-content"]] [text "NodeHole2 content"])
            .NodeHole3("NodeHole3 content")
            .NodeHole4(div [attr.classes ["nodehole4-content"]] [text "NodeHole4 content"])
            .AttrHole1("attrhole1-content")
            .AttrHole2("attrhole2-content")
            .AttrHole3("attrhole3-content")
            .Elt()
        comp<EventTester> [] []
        comp<BindTester> [] []
    ]
