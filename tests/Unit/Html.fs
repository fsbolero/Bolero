// Test HTML functionality in-browser with Selenium.
namespace Bolero.Tests.Web

open NUnit.Framework
open OpenQA.Selenium

[<Category "HTML">]
module Html =

    let mutable root = Unchecked.defaultof<IWebElement>

    let byId x =
        try root.FindElement(By.Id x)
        with :? NoSuchElementException -> null

    let byClass x =
        try root.FindElement(By.ClassName x)
        with :? NoSuchElementException -> null

    [<OneTimeSetUp>]
    let SetUp() =
        root <- WebFixture.Root.FindElement(By.Id "test-fixture-html")

    [<Test>]
    let ``Element with id and text content``() =
        Assert.AreEqual(
            "Contents of element with id",
            (byId "element-with-id").Text)

    [<Test>]
    let ``Element with content that must be escaped``() =
        Assert.AreEqual(
            "Escaped <b>text</b> & content",
            (byId "element-with-htmlentity").Text)

    [<Test>]
    let ``Element with id and classes``() =
        Assert.IsNull(byClass "class-notset-1")
        Assert.IsNull(byClass "class-notset-2")
        Assert.IsNotNull(byClass "class-set-1")
        Assert.IsNotNull(byClass "class-set-2")

    [<Test>]
    let ``Raw HTML``() =
        Assert.AreEqual(
            "Unescape <b>text</b> & content",
            (byClass "raw-html-element").Text)

    [<Test>]
    let ``Blazor Component``() =
        let navLink = byId "nav-link"
        Assert.AreEqual("active", navLink.GetAttribute("class"))
        Assert.AreEqual("NavLink content", navLink.Text)

    [<Test>]
    let ``Bolero Component``() =
        Assert.AreEqual("Component content", (byId "bolero-component").Text)
