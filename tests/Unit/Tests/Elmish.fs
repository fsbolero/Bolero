namespace Bolero.Tests.Web

open NUnit.Framework
open OpenQA.Selenium.Support.UI

/// Elmish program integration.
[<Category "Elmish">]
module Elmish =

    let elt = NodeFixture()

    [<OneTimeSetUp>]
    let SetUp() =
        elt.Init("test-fixture-elmish")

    [<Test>]
    let ``ProgramComponent is rendered``() =
        Assert.IsNotNull(elt.ByClass("container"))
        Assert.AreEqual(
            "constant value",
            elt.ByClass("constValue-input").GetAttribute("value"))

    [<Test>]
    let ``Input event handler dispatches message``() =
        elt.ByClass("stringValue-input").SendKeys("Changed!\b")
        elt.AssertEventually((fun () ->
            elt.ByClass("stringValue-repeat").Text = "stringValueInitChanged"),
            "Element not updated")

    [<Test>]
    let ``ElmishComponent is rendered``() =
        Assert.IsNotNull(elt.ByClass("intValue-input"))

    [<Test>]
    let ``ElmishComponent dispatches message``() =
        let el = elt.ByClass("intValue-input")
        el.Clear()
        el.SendKeys("35")
        elt.AssertEventually((fun () ->
            elt.ByClass("intValue-repeat").Text = "35"))
