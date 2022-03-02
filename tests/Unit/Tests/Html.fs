namespace Bolero.Tests.Web

open System
open NUnit.Framework
open OpenQA.Selenium
open Swensen.Unquote
open Bolero.Tests

/// Basic HTML functionality: node and attribute functions, Blazor components.
[<Category "HTML">]
module Html =

    let elt = NodeFixture(By.Id "test-fixture-html")

    let blur() =
        WebFixture.Driver
            .ExecuteScript("document.activeElement.blur()")
            |> ignore

    [<Test>]
    let ``Element with id and text content``() =
        test <@ elt.ById("element-with-id").Text = "Contents of element with id" @>

    [<Test>]
    let ``Element with content that must be escaped``() =
        test <@ elt.ById("element-with-htmlentity").Text = "Escaped <b>text</b> & content" @>

    [<Test>]
    let ``Raw HTML``() =
        test <@ elt.ByClass("raw-html-element").Text = "Unescape <b>text</b> & content" @>

    [<Test>]
    let ``Blazor Component``() =
        let navLink = elt.ById("nav-link")
        test <@ navLink.GetAttribute("class") = "active" @>
        test <@ navLink.Text = "NavLink content" @>

    [<Test>]
    let ``Bolero Component``() =
        test <@ elt.ById("bolero-component").Text = "Component content" @>

    [<Test>]
    let ``Boolean cond reacts to events``() =
        let inp = elt.ByClass("condBoolInput")
        inp.Clear()

        inp.SendKeys("ab")
        elt.EventuallyNotNull <@ elt.ByClass("condBoolIs2") @>
        testNull <@ elt.ByClass("condBoolIsNot2") @>

        inp.SendKeys("c")
        elt.EventuallyNotNull <@ elt.ByClass("condBoolIsNot2") @>
        testNull <@ elt.ByClass("condBoolIs2") @>

    [<Test>]
    let ``Union cond reacts to events``() =
        let inp = elt.ByClass("condUnionInput")
        inp.Clear()

        elt.EventuallyNotNull <@ elt.ByClass("condUnionIsEmpty") @>
        testNull <@ elt.ByClass("condUnionIsOne") @>
        testNull <@ elt.ByClass("condUnionIsMany") @>

        inp.SendKeys("a")
        elt.EventuallyNotNull <@ elt.ByClass("condUnionIsOne") @>
        testNull <@ elt.ByClass("condUnionIsEmpty") @>
        testNull <@ elt.ByClass("condUnionIsMany") @>

        inp.SendKeys("b")
        elt.EventuallyNotNull <@ elt.ByClass("condUnionIsMany") @>
        testNull <@ elt.ByClass("condUnionIsOne") @>
        testNull <@ elt.ByClass("condUnionIsEmpty") @>

    [<Test>]
    let ``Render many forEach items``() =
        let inp = elt.ByClass("forEachInput")
        inp.Clear()

        inp.SendKeys("ABC")
        elt.EventuallyNotNull <@ elt.ByClass("forEachIsA") @>
        testNotNull <@ elt.ByClass("forEachIsB") @>
        testNotNull <@ elt.ByClass("forEachIsC") @>

        inp.SendKeys(Keys.Backspace)
        elt.EventuallyNull <@ elt.ByClass("forEachIsC") @>
        testNotNull <@ elt.ByClass("forEachIsA") @>
        testNotNull <@ elt.ByClass("forEachIsB") @>

    [<Test>]
    let ``bind.input``() =
        let inp = elt.ByClass("bind-input")
        inp.Clear()

        inp.SendKeys("ABC")
        elt.Eventually <@ elt.ByClass("bind-input-out").Text = "ABC" @>
        elt.Eventually <@ elt.ByClass("bind-input-2").GetProperty("value") = "ABC" @>

    [<Test>]
    let ``bind.change``() =
        let inp = elt.ByClass("bind-change")
        inp.Clear()

        inp.SendKeys("DEF")
        blur()
        elt.Eventually <@ elt.ByClass("bind-change-out").Text = "DEF" @>
        elt.Eventually <@ elt.ByClass("bind-change-2").GetProperty("value") = "DEF" @>

    [<Test>]
    let ``bind.inputInt``() =
        let inp = elt.ByClass("bind-input-int")
        inp.Clear()

        inp.SendKeys("123")
        elt.Eventually <@ elt.ByClass("bind-input-int-out").Text = "123" @>
        elt.Eventually <@ elt.ByClass("bind-input-int-2").GetProperty("value") = "123" @>

    [<Test>]
    let ``bind.changeInt``() =
        let inp = elt.ByClass("bind-change-int")
        inp.Clear()

        inp.SendKeys("456")
        blur()
        elt.Eventually <@ elt.ByClass("bind-change-int-out").Text = "456" @>
        elt.Eventually <@ elt.ByClass("bind-change-int-2").GetProperty("value") = "456" @>

    [<Test>]
    let ``bind.inputFloat``() =
        let inp = elt.ByClass("bind-input-float")
        inp.Clear()

        inp.SendKeys("1234.5")
        elt.Eventually <@ elt.ByClass("bind-input-float-out").Text.TrimEnd('0') = "1234.5" @>
        elt.Eventually <@ elt.ByClass("bind-input-float-2").GetProperty("value").TrimEnd('0') = "1234.5" @>

    [<Test>]
    let ``bind.changeFloat``() =
        let inp = elt.ByClass("bind-change-float")
        inp.Clear()

        inp.SendKeys("54.321")
        blur()
        elt.Eventually <@ elt.ByClass("bind-change-float-out").Text.TrimEnd('0') = "54.321" @>
        elt.Eventually <@ elt.ByClass("bind-change-float-2").GetProperty("value").TrimEnd('0') = "54.321" @>

    [<Test>]
    let ``bind.checked``() =
        let inp = elt.ByClass("bind-checked")
        let out = elt.ByClass("bind-checked-out")
        let inp2 = elt.ByClass("bind-checked-2")
        test <@ out.Text = "false" @>

        inp.Click()
        elt.Eventually <@ out.Text = "true" @>
        elt.Eventually <@ inp2.GetProperty("checked").Equals("true", StringComparison.OrdinalIgnoreCase) @>

        inp.Click()
        elt.Eventually <@ out.Text = "false" @>
        elt.Eventually <@ inp2.GetProperty("checked").Equals("false", StringComparison.OrdinalIgnoreCase) @>

    [<Test>]
    let ``bind.change radio``() =
        let out = elt.ByClass("bind-radio-out")
        let other = elt.ByClass("bind-radio-0")
        for v in 1..10 do
            let inp = elt.ByClass("bind-radio-" + string v)
            inp.Click()
            elt.Eventually <@ out.Text = string v @>
            elt.Eventually <@ other.GetProperty("value") = string v @>

    [<Test>]
    let ElementRefBinder() =
        let btn = elt.ByClass("element-ref")
        testNotNull <@ btn @>
        btn.Click()
        elt.Eventually <@ btn.Text = "ElementRef is bound" @>

    [<Test>]
    let ComponentRefBinder() =
        let btn = elt.ByClass("component-ref")
        testNotNull <@ btn @>
        btn.Click()
        elt.Eventually <@ btn.Text = "component-ref-is-bound" @>

    [<Test>]
    let ComponentRefBinderRendersChildren() =
        let nav = elt.ByClass("nav-link")
        test <@ nav.Text = "Home" @>

    [<Test>]
    let ComponentChildContent() =
        let comp = elt.ByClass("comp-child-content")
        testNotNull <@ comp @>
        let child = comp.ByClass("comp-child-elt")
        testNotNull <@ child @>
        test <@ child.Text = "comp-child-text-1" @>
        test <@ comp.Text = "comp-child-text-1comp-child-text-2" @>

    [<Test>]
    let ElementBindKeyAndRef() =
        let btn = elt.ByClass("elt-keyref1").ByClass("elt-keyref-btn")
        let target = elt.ByClass("elt-keyref2")
        testNotNull <@ btn @>
        btn.Click()
        elt.Eventually <@ target.Text = "elt-keyref is bound" @>

    [<Test>]
    let ComponentBindKeyAndRef() =
        let btn = elt.ByClass("comp-keyref1").ByClass("comp-keyref-btn")
        let target = elt.ByClass("comp-keyref2")
        testNotNull <@ btn @>
        btn.Click()
        elt.Eventually <@ target.Text = "comp-keyref is bound" @>
