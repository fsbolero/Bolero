namespace Bolero.Tests.Web

open NUnit.Framework
open OpenQA.Selenium
open Swensen.Unquote
open Bolero.Tests

/// Elmish program integration.
[<Category "Elmish">]
module Elmish =

    let elt = NodeFixture(By.Id "test-fixture-elmish")

    [<Test>]
    let ``ProgramComponent is rendered``() =
        testNotNull <@ elt.ByClass("container") @>
        test <@ elt.ByClass("constValue-input").GetAttribute("value") = "constant value" @>

    [<Test; NonParallelizable>]
    let ``Input event handler dispatches message``() =
        let el = elt.ByClass("stringValue-input")
        el.SendKeys("Changed!")
        el.SendKeys(Keys.Backspace)
        elt.Eventually <@ elt.ByClass("stringValue-repeat").Text = "stringValueInitChanged" @>

    [<Test>]
    let ``ElmishComponent is rendered``() =
        testNotNull <@ elt.ByClass("intValue-input") @>

    [<Test>]
    let ``ElmishComponent dispatches message``() =
        let el = elt.ByClass("intValue-input")
        el.Clear()
        el.SendKeys("35")
        elt.Eventually <@ elt.ByClass("intValue-repeat").Text = "35" @>
