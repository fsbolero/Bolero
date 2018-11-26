namespace Bolero.Tests.Web

open NUnit.Framework
open OpenQA.Selenium.Support.UI

/// Blazor router integration.
[<Category "Routing">]
module Routing =

    let elt = NodeFixture()

    [<OneTimeSetUp>]
    let SetUp() =
        elt.Init("test-fixture-routing")

    let links =
        App.Routing.links
        |> List.map TestCaseData

    [<Test; TestCaseSource("links"); NonParallelizable>]
    let ``Click link``(linkCls, page, url) =
        elt.ByClass("link-" + linkCls).Click()
        let resCls, resTxt = App.Routing.matchPage page
        let res = elt.Wait(fun () -> elt.ByClass(resCls))
        Assert.AreEqual(resTxt, res.Text)
        Assert.AreEqual(WebFixture.Url + url, WebFixture.Driver.Url)

    [<Test; TestCaseSource("links"); NonParallelizable>]
    let ``Set by model``(linkCls, page, url) =
        elt.ByClass("btn-" + linkCls).Click()
        let resCls, resTxt = App.Routing.matchPage page
        let res = elt.Wait(fun () -> elt.ByClass(resCls))
        Assert.AreEqual(resTxt, res.Text)
        Assert.AreEqual(WebFixture.Url + url, WebFixture.Driver.Url)
