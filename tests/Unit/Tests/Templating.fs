namespace Bolero.Tests.Web

open System
open System.Globalization
open NUnit.Framework
open OpenQA.Selenium
open OpenQA.Selenium.Support.UI
open Swensen.Unquote
open Bolero.Tests

/// HTML Templates.
[<Category "Templating">]
module Templating =

    let elt = NodeFixture(By.Id "test-fixture-templating")

    let blur() =
        WebFixture.Driver
            .ExecuteScript("document.activeElement.blur()")
            |> ignore

    [<Test>]
    let ``Inline template is instantiated``() =
        testNotNull <@ elt.ByClass("inline") @>

    [<Test>]
    let ``File template is instantiated``() =
        testNotNull <@ elt.ByClass("file") @>

    [<Test>]
    let ``Node hole filled with string``() =
        test <@ elt.ByClass("nodehole1").Text = "NodeHole1 content" @>

    [<Test>]
    let ``File template node hole filled``() =
        testNotNull <@ elt.ByClass("file").ByClass("file-hole") @>

    [<Test>]
    let ``Node hole filled with node``() =
        let filledWith = elt.ByClass("nodehole2-content")
        testNotNull <@ filledWith @>
        test <@ filledWith.Text = "NodeHole2 content" @>

    [<Test>]
    [<TestCase("nodehole3-1")>]
    [<TestCase("nodehole3-2")>]
    let ``Node hole filled with string [multiple]``(id: string) =
        test <@ elt.ByClass(id).Text = "NodeHole3 content" @>

    [<Test>]
    [<TestCase("nodehole4-1")>]
    [<TestCase("nodehole4-2")>]
    let ``Node hole filled with node [multiple]``(id: string) =
        let elt = elt.ByClass(id)
        let filledWith = elt.ByClass("nodehole4-content")
        testNotNull <@ filledWith @>
        test <@ filledWith.Text = "NodeHole4 content" @>

    [<Test>]
    let ``Attr hole``() =
        test <@ elt.ByClass("attrhole1").GetAttribute("class").Split(' ')
                |> Seq.contains "attrhole1-content" @>

    [<Test>]
    [<TestCase("attrhole2-1")>]
    [<TestCase("attrhole2-2")>]
    let ``Attr hole [multiple]``(id: string) =
        test <@ elt.ByClass(id).GetAttribute("class").Split(' ')
                |> Seq.contains "attrhole2-content" @>

    [<Test>]
    let ``Attr hole mixed with node hole``() =
        test <@ elt.ByClass("attrhole3-1").GetAttribute("class").Split(' ')
                |> Seq.contains "attrhole3-content" @>
        test <@ elt.ByClass("attrhole3-2").Text = "attrhole3-content" @>

    [<Test>]
    let ``Full attr hole``() =
        let elt = elt.ByClass("fullattrhole")
        test <@ elt.GetAttribute("id") = "fullattrhole-content" @>
        test <@ elt.GetAttribute("data-fullattrhole") = "1234" @>

    [<Test>]
    let ``Attr hole obj value``() =
        let elt = elt.ByClass("attrhole4")
        test <@ elt.GetAttribute("data-value") = "5678" @>
        testNotNull <@ elt.GetAttribute("data-true") @>
        testNull <@ elt.GetAttribute("data-false") @>

    [<Test>]
    let ``Event hole``() =
        let elt = elt.Inner(By.ClassName "events")
        let state = elt.ByClass("currentstate")
        let position = elt.ByClass("position")
        let isNumber (s: string) =
            Double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, ref 0.)
        let isValidPosition (pos: string) =
            let a = pos.Split(',')
            isNumber a.[0] && isNumber a.[1]

        elt.ByClass("btn1").Click()
        elt.Eventually <@ state.Text = "clicked 1" @>
        test <@ isValidPosition position.Text @>

        elt.ByClass("btn2").Click()
        elt.Eventually <@ state.Text = "clicked 2" @>
        test <@ isValidPosition position.Text @>

        elt.ByClass("btn3").Click()
        elt.Eventually <@ state.Text = "clicked 1" @>
        test <@ isValidPosition position.Text @>

    [<Test>]
    [<TestCase("")>]
    [<TestCase("-onchange")>]
    let ``Bind string to normal input``(cls: string) =
        let elt = elt.Inner(By.ClassName "binds")
        let inp = elt.ByClass($"input{cls}1-1")
        inp.Clear()
        inp.SendKeys("hello")
        if cls.Contains("onchange") then blur()
        // Value propagation
        elt.Eventually <@ elt.ByClass($"display{cls}1").Text = "hello" @>
        // Propagation to other input
        test <@ elt.ByClass($"input{cls}1-2").GetAttribute("value") = "hello" @>
        // Propagation to textarea
        test <@ elt.ByClass($"textarea{cls}1").GetAttribute("value") = "hello" @>
        // Propagation to select
        test <@ elt.ByClass($"select{cls}1").GetAttribute("value") = "hello" @>

    [<Test>]
    [<TestCase("")>]
    [<TestCase("-onchange")>]
    let ``Bind string to textarea``(cls: string) =
        let elt = elt.Inner(By.ClassName "binds")
        let inp = elt.ByClass($"textarea{cls}1")
        inp.Clear()
        inp.SendKeys("hi textarea")
        if cls.Contains("onchange") then blur()
        // Value propagation
        elt.Eventually <@ elt.ByClass($"display{cls}1").Text = "hi textarea" @>
        // Propagation to input
        test <@ elt.ByClass($"input{cls}1-1").GetAttribute("value") = "hi textarea" @>
        // Propagation to other input
        test <@ elt.ByClass($"input{cls}1-2").GetAttribute("value") = "hi textarea" @>
        // Propagation to select
        test <@ elt.ByClass($"select{cls}1").GetAttribute("value") = "hi textarea" @>

    [<Test>]
    let ``Bind string to select``() =
        let elt = elt.Inner(By.ClassName "binds")
        SelectElement(elt.ByClass("select1"))
            .SelectByValue("hi select")
        // Value propagation
        elt.Eventually <@ elt.ByClass("display1").Text = "hi select" @>
        // Propagation to input
        test <@ elt.ByClass("input1-1").GetAttribute("value") = "hi select" @>
        // Propagation to other input
        test <@ elt.ByClass("input1-2").GetAttribute("value") = "hi select" @>
        // Propagation to textarea
        test <@ elt.ByClass("textarea1").GetAttribute("value") = "hi select" @>

    [<Test>]
    [<TestCase("")>]
    [<TestCase("-onchange")>]
    let ``Bind int``(cls: string) =
        let elt = elt.Inner(By.ClassName "binds")
        let inp = elt.ByClass($"input{cls}2-1")
        inp.Clear()
        inp.SendKeys("1234")
        if cls.Contains("onchange") then blur()
        // Value propagation
        elt.Eventually <@ elt.ByClass($"display{cls}2").Text = "1234" @>
        // Propagation to other input
        test <@ elt.ByClass($"input{cls}2-2").GetAttribute("value") = "1234" @>

    [<Test>]
    [<TestCase("", Ignore = "Char-by-char parsing may eat the dot, TODO fix")>]
    [<TestCase("-onchange")>]
    let ``Bind float``(cls: string) =
        let elt = elt.Inner(By.ClassName "binds")
        let inp = elt.ByClass($"input{cls}3-1")
        inp.Clear()
        inp.SendKeys("123.456")
        if cls.Contains("onchange") then blur()
        // Value propagation
        elt.Eventually <@ elt.ByClass($"display{cls}3").Text = "123.456" @>
        // Propagation to other input
        test <@ elt.ByClass($"input{cls}3-2").GetAttribute("value") = "123.456" @>

    [<Test>]
    let ``Bind checkbox``() =
        let elt = elt.Inner(By.ClassName "binds")
        let inp1 = elt.ByClass("input4-1")
        let inp2 = elt.ByClass("input4-2")
        let isChecked (inp: IWebElement) =
            match inp.GetAttribute("checked") with
            | null -> false
            | s -> bool.Parse s
        let initial = false
        test <@ isChecked inp1 = initial @>
        test <@ isChecked inp2 = initial @>

        inp1.Click()
        elt.Eventually <@ isChecked inp1 = not initial @>
        elt.Eventually <@ isChecked inp2 = not initial @>

        inp2.Click()
        elt.Eventually <@ isChecked inp1 = initial @>
        elt.Eventually <@ isChecked inp2 = initial @>

    [<Test>]
    let ``Nested template is instantiated``() =
        testNotNull <@ elt.ByClass("nested1") @>

    [<Test>]
    let ``Nested template is removed from its original parent``() =
        testNull <@ elt.ById("Nested1") @>

    [<Test>]
    let ``Nested template hole filled``() =
        testNotNull <@ elt.ByClass("nested1").ByClass("nested-hole") @>
        testNull <@ elt.ByClass("nested1").ByClass("file-hole") @>

    [<Test>]
    let ``Recursively nested template is instantiated``() =
        testNotNull <@ elt.ByClass("nested2") @>

    [<Test>]
    let ``Recursively nested template is removed from its original parent``() =
        testNull <@ elt.ById("Nested2") @>

    [<Test>]
    let ``Regression #11: common hole in attrs and children``() =
        test <@ elt.ByClass("regression-11").Text = "regression-11" @>

    [<Test>]
    let ``Regression #256: attribute name case is respected``() =
        test <@ elt.ByClass("regression-256").GetDomAttribute("viewbox") |> isNull @>
        test <@ elt.ByClass("regression-256").GetDomAttribute("viewBox") = "0 0 100 100" @>
