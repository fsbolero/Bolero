namespace Bolero.Tests.Web

open NUnit.Framework

/// Basic HTML functionality: node and attribute functions, Blazor components.
[<Category "HTML">]
module Html =

    let elt = NodeFixture()

    [<OneTimeSetUp>]
    let SetUp() =
        elt.Init("test-fixture-html")

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
    let ``Element with id and classes``() =
        Assert.IsNull(elt.ByClass("class-notset-1"))
        Assert.IsNull(elt.ByClass("class-notset-2"))
        Assert.IsNotNull(elt.ByClass("class-set-1"))
        Assert.IsNotNull(elt.ByClass("class-set-2"))

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
        for i = 0 to 10 do inp.SendKeys("\b")
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
        for i = 0 to 10 do inp.SendKeys("\b")
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
        for i = 0 to 10 do inp.SendKeys("\b")
        inp.SendKeys("ABC")
        elt.AssertEventually(fun () ->
            elt.ByClass("forEachIsA"))
        Assert.IsNotNull(elt.ByClass("forEachIsB"))
        Assert.IsNotNull(elt.ByClass("forEachIsC"))
        inp.SendKeys("\b")
        elt.AssertEventually(fun () ->
            isNull <| elt.ByClass("forEachIsC"))
        Assert.IsNotNull(elt.ByClass("forEachIsA"))
        Assert.IsNotNull(elt.ByClass("forEachIsB"))
