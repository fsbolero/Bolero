namespace Bolero.Tests.Web

open NUnit.Framework
open OpenQA.Selenium

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
        Assert.AreEqual(
            "Contents of element with id",
            elt.ById("element-with-id").Text)

    [<Test>]
    let ``Element with content that must be escaped``() =
        Assert.AreEqual(
            "Escaped <b>text</b> & content",
            elt.ById("element-with-htmlentity").Text)

    [<Test>]
    let ``Element with classes``() =
        Assert.IsNull(elt.ByClass("class-notset-1"), "class-notset-1")
        Assert.IsNull(elt.ByClass("class-notset-2"), "class-notset-2")
        Assert.IsNotNull(elt.ByClass("class-set-1"), "class-set-1")
        Assert.IsNotNull(elt.ByClass("class-set-2"), "class-set-2")
        Assert.IsNotNull(elt.ByClass("class-set-3"), "class-set-3")
        Assert.IsNotNull(elt.ByClass("class-set-4"), "class-set-4")
        Assert.IsNotNull(elt.ByClass("class-set-5"), "class-set-5")

    [<Test>]
    let ``Raw HTML``() =
        Assert.AreEqual(
            "Unescape <b>text</b> & content",
            elt.ByClass("raw-html-element").Text)

    [<Test>]
    let ``Blazor Component``() =
        let navLink = elt.ById("nav-link")
        Assert.AreEqual("active", navLink.GetAttribute("class"))
        Assert.AreEqual("NavLink content", navLink.Text)

    [<Test>]
    let ``Bolero Component``() =
        Assert.AreEqual("Component content", elt.ById("bolero-component").Text)

    [<Test>]
    let ``Boolean cond reacts to events``() =
        let inp = elt.ByClass("condBoolInput")
        inp.Clear()
        inp.SendKeys("ab")
        elt.AssertEventually(fun () ->
            elt.ByClass("condBoolIs2"))
        Assert.IsNull(elt.ByClass("condBoolIsNot2"))
        inp.SendKeys("c")
        elt.AssertEventually(fun () ->
            elt.ByClass("condBoolIsNot2"))
        Assert.IsNull(elt.ByClass("condBoolIs2"))

    [<Test>]
    let ``Union cond reacts to events``() =
        let inp = elt.ByClass("condUnionInput")
        inp.Clear()
        elt.AssertEventually(fun () ->
            elt.ByClass("condUnionIsEmpty"))
        Assert.IsNull(elt.ByClass("condUnionIsOne"))
        Assert.IsNull(elt.ByClass("condUnionIsMany"))
        inp.SendKeys("a")
        elt.AssertEventually(fun () ->
            elt.ByClass("condUnionIsOne"))
        Assert.IsNull(elt.ByClass("condUnionIsEmpty"))
        Assert.IsNull(elt.ByClass("condUnionIsMany"))
        inp.SendKeys("b")
        elt.AssertEventually(fun () ->
            elt.ByClass("condUnionIsMany"))
        Assert.IsNull(elt.ByClass("condUnionIsOne"))
        Assert.IsNull(elt.ByClass("condUnionIsEmpty"))

    [<Test>]
    let ``Render many forEach items``() =
        let inp = elt.ByClass("forEachInput")
        inp.Clear()
        inp.SendKeys("ABC")
        elt.AssertEventually(fun () ->
            elt.ByClass("forEachIsA"))
        Assert.IsNotNull(elt.ByClass("forEachIsB"))
        Assert.IsNotNull(elt.ByClass("forEachIsC"))
        inp.SendKeys(Keys.Backspace)
        elt.AssertEventually(fun () ->
            isNull <| elt.ByClass("forEachIsC"))
        Assert.IsNotNull(elt.ByClass("forEachIsA"))
        Assert.IsNotNull(elt.ByClass("forEachIsB"))

    [<Test>]
    let ``bind.input``() =
        let inp = elt.ByClass("bind-input")
        inp.Clear()
        inp.SendKeys("ABC")
        elt.AssertEventually(fun () ->
            elt.ByClass("bind-input-out").Text = "ABC")

    [<Test>]
    let ``bind.change``() =
        let inp = elt.ByClass("bind-change")
        inp.Clear()
        inp.SendKeys("DEF")
        blur()
        elt.AssertEventually(fun () ->
            elt.ByClass("bind-change-out").Text = "DEF")

    [<Test>]
    let ``bind.inputInt``() =
        let inp = elt.ByClass("bind-input-int")
        inp.Clear()
        inp.SendKeys("123")
        elt.AssertEventually(fun () ->
            elt.ByClass("bind-input-int-out").Text = "123")

    [<Test>]
    let ``bind.changeInt``() =
        let inp = elt.ByClass("bind-change-int")
        inp.Clear()
        inp.SendKeys("456")
        blur()
        elt.AssertEventually(fun () ->
            elt.ByClass("bind-change-int-out").Text = "456")

    [<Test>]
    let ``bind.inputFloat``() =
        let inp = elt.ByClass("bind-input-float")
        inp.Clear()
        inp.SendKeys("1234.5")
        elt.AssertEventually(fun () ->
            elt.ByClass("bind-input-float-out").Text.TrimEnd('0') = "1234.5")

    [<Test>]
    let ``bind.changeFloat``() =
        let inp = elt.ByClass("bind-change-float")
        inp.Clear()
        inp.SendKeys("54.321")
        blur()
        elt.AssertEventually(fun () ->
            elt.ByClass("bind-change-float-out").Text.TrimEnd('0') = "54.321")

    [<Test>]
    let ``bind.checked``() =
        let inp = elt.ByClass("bind-checked")
        let out = elt.ByClass("bind-checked-out")
        Assert.AreEqual(out.Text, "false")
        inp.Click()
        elt.AssertEventually(fun () -> out.Text = "true")
        inp.Click()
        elt.AssertEventually(fun () -> out.Text = "false")

    [<Test>]
    let ``bind.change radio``() =
        let out = elt.ByClass("bind-radio-out")
        for v in 1..10 do
            let inp = elt.ByClass("bind-radio-" + string v)
            inp.Click()
            elt.AssertEventually(fun () -> out.Text = string v)

    [<Test>]
    let ElementRefBinder() =
        let btn = elt.ByClass("element-ref")
        Assert.IsNotNull(btn)
        btn.Click()
        elt.AssertEventually(
            (fun () -> btn.Text = "ElementRef 1 is bound"),
            "attr.ref")
        let btn = elt.ByClass("element-ref-binder")
        Assert.IsNotNull(btn)
        btn.Click()
        elt.AssertEventually(
            (fun () -> btn.Text = "ElementRef 2 is bound"),
            "attr.bindRef")
