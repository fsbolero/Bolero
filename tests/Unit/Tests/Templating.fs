namespace Bolero.Tests.Web

open System
open NUnit.Framework
open OpenQA.Selenium
open OpenQA.Selenium.Support.UI

/// HTML Templates.
[<Category "Templating">]
module Templating =
    open OpenQA.Selenium.Interactions

    let elt = NodeFixture()

    let blur() =
        WebFixture.Driver
            .ExecuteScript("document.activeElement.blur()")
            |> ignore

    [<OneTimeSetUp>]
    let SetUp() =
        elt.Init("test-fixture-templating")

    [<Test>]
    let ``Inline template is instantiated``() =
        Assert.IsNotNull(elt.ByClass("inline"))

    [<Test>]
    let ``Node hole filled with string``() =
        Assert.AreEqual("NodeHole1 content",
            elt.ByClass("nodehole1").Text)

    [<Test>]
    let ``Node hole filled with node``() =
        let filledWith = elt.ByClass("nodehole2-content")
        Assert.IsNotNull(filledWith)
        Assert.AreEqual("NodeHole2 content", filledWith.Text)

    [<Test>]
    [<TestCase("nodehole3-1")>]
    [<TestCase("nodehole3-2")>]
    let ``Node hole filled with string [multiple]``(id: string) =
        Assert.AreEqual("NodeHole3 content", elt.ByClass(id).Text)

    [<Test>]
    [<TestCase("nodehole4-1")>]
    [<TestCase("nodehole4-2")>]
    let ``Node hole filled with node [multiple]``(id: string) =
        let elt = elt.ByClass(id)
        let filledWith = elt.ByClass("nodehole4-content")
        Assert.IsNotNull(filledWith)
        Assert.AreEqual("NodeHole4 content", filledWith.Text)

    [<Test>]
    let ``Attr hole``() =
        Assert.Contains("attrhole1-content",
            elt.ByClass("attrhole1").GetAttribute("class").Split(' '))

    [<Test>]
    [<TestCase("attrhole2-1")>]
    [<TestCase("attrhole2-2")>]
    let ``Attr hole [multiple]``(id: string) =
        Assert.Contains("attrhole2-content",
            elt.ByClass(id).GetAttribute("class").Split(' '))

    [<Test>]
    let ``Attr hole mixed with node hole``() =
        Assert.Contains("attrhole3-content",
            elt.ByClass("attrhole3-1").GetAttribute("class").Split(' '))
        Assert.AreEqual("attrhole3-content",
            elt.ByClass("attrhole3-2").Text)

    [<Test>]
    let ``Event hole``() =
        let elt = elt.Inner(By.ClassName "events")
        let state = elt.ByClass("currentstate")
        let position = elt.ByClass("position")
        let isValidPosition() =
            let a = position.Text.Split(',')
            Int32.TryParse(a.[0], ref 0) && Int32.TryParse(a.[1], ref 0)

        elt.ByClass("btn1").Click()
        elt.AssertAreEqualEventually("clicked 1",
            (fun () -> state.Text),
            "First click")
        Assert.IsTrue(isValidPosition(), "Position: " + position.Text)

        elt.ByClass("btn2").Click()
        elt.AssertAreEqualEventually("clicked 2",
            (fun () -> state.Text),
            "Second click")
        Assert.IsTrue(isValidPosition(), "Position: " + position.Text)

        elt.ByClass("btn3").Click()
        elt.AssertAreEqualEventually("clicked 1", 
            (fun () -> state.Text),
            "Same event bound multiple times")
        Assert.IsTrue(isValidPosition(), "Position: " + position.Text)

    [<Test>]
    [<TestCase("")>]
    [<TestCase("-onchange")>]
    let ``Bind string to normal input``(cls: string) =
        let elt = elt.Inner(By.ClassName "binds")
        let inp = elt.ByClass(sprintf "input%s1-1" cls)
        inp.Clear()
        inp.SendKeys("hello")
        if cls.Contains("onchange") then blur()
        elt.AssertAreEqualEventually("hello",
            (fun () -> elt.ByClass(sprintf "display%s1" cls).Text),
            "Value propagation")
        Assert.AreEqual("hello",
            elt.ByClass(sprintf "input%s1-2" cls).GetAttribute("value"),
            "Propagation to other input")
        Assert.AreEqual("hello",
            elt.ByClass(sprintf "textarea%s1" cls).GetAttribute("value"),
            "Propagation to textarea")
        Assert.AreEqual("hello",
            elt.ByClass(sprintf "select%s1" cls).GetAttribute("value"),
            "Propagation to select")

    [<Test>]
    let ``Bind string to textarea``() =
        let elt = elt.Inner(By.ClassName "binds")
        let inp = elt.ByClass("textarea1")
        inp.Clear()
        inp.SendKeys("hi textarea")
        elt.AssertAreEqualEventually("hi textarea",
            (fun () -> elt.ByClass("display1").Text),
            "Value propagation")
        Assert.AreEqual("hi textarea",
            elt.ByClass("input1-1").GetAttribute("value"),
            "Propagation to input")
        Assert.AreEqual("hi textarea",
            elt.ByClass("input1-2").GetAttribute("value"),
            "Propagation to other input")
        Assert.AreEqual("hi textarea",
            elt.ByClass("select1").GetAttribute("value"),
            "Propagation to select")

    [<Test>]
    let ``Bind string to select``() =
        let elt = elt.Inner(By.ClassName "binds")
        SelectElement(elt.ByClass("select1"))
            .SelectByValue("hi select")
        elt.AssertAreEqualEventually("hi select",
            (fun () -> elt.ByClass("display1").Text),
            "Value propagation")
        Assert.AreEqual("hi select",
            elt.ByClass("input1-1").GetAttribute("value"),
            "Propagation to input")
        Assert.AreEqual("hi select",
            elt.ByClass("input1-2").GetAttribute("value"),
            "Propagation to other input")
        Assert.AreEqual("hi select",
            elt.ByClass("textarea1").GetAttribute("value"),
            "Propagation to textarea")

    [<Test>]
    [<TestCase("")>]
    [<TestCase("-onchange")>]
    let ``Bind int``(cls: string) =
        let elt = elt.Inner(By.ClassName "binds")
        let inp = elt.ByClass(sprintf "input%s2-1" cls)
        inp.Clear()
        inp.SendKeys("1234")
        if cls.Contains("onchange") then blur()
        elt.AssertAreEqualEventually("1234",
            (fun () -> elt.ByClass(sprintf "display%s2" cls).Text),
            "Value propagation")
        Assert.AreEqual("1234",
            elt.ByClass(sprintf "input%s2-2" cls).GetAttribute("value"),
            "Propagation to other input")

    [<Test>]
    [<TestCase("", Ignore = "Char-by-char parsing may eat the dot, TODO fix")>]
    [<TestCase("-onchange")>]
    let ``Bind float``(cls: string) =
        let elt = elt.Inner(By.ClassName "binds")
        let inp = elt.ByClass(sprintf "input%s3-1" cls)
        inp.Clear()
        inp.SendKeys("123.456")
        if cls.Contains("onchange") then blur()
        elt.AssertAreEqualEventually("123.456",
            (fun () -> elt.ByClass(sprintf "display%s3" cls).Text),
            "Value propagation")
        Assert.AreEqual("123.456",
            elt.ByClass(sprintf "input%s3-2" cls).GetAttribute("value"),
            "Propagation to other input")
