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
