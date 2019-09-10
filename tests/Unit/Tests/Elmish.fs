namespace Bolero.Tests.Web

open NUnit.Framework
open OpenQA.Selenium

/// Elmish program integration.
[<Category "Elmish">]
module Elmish =

    let elt = NodeFixture(By.Id "test-fixture-elmish")

    [<Test>]
    let ``ProgramComponent is rendered``() =
        Assert.IsNotNull(elt.ByClass("container"))
        Assert.AreEqual(
            "constant value",
            elt.ByClass("constValue-input").GetAttribute("value"))

    [<Test; NonParallelizable>]
    let ``Input event handler dispatches message``() =
        let el = elt.ByClass("stringValue-input")
        el.SendKeys("Changed!")
        el.SendKeys(Keys.Backspace)
        elt.AssertAreEqualEventually(
            "stringValueInitChanged",
            (fun () -> elt.ByClass("stringValue-repeat").Text),
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
