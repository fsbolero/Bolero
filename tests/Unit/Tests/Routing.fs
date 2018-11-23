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

    [<Test>]
    let ``Click link``() =
        for linkCls, page, url in App.Routing.links do
            elt.ByClass("link-" + linkCls).Click()
            let resCls, resTxt = App.Routing.matchPage page
            let res = elt.Wait(fun () -> elt.ByClass(resCls))
            Assert.IsTrue(res.Text = resTxt)
            Assert.AreEqual(WebFixture.Url + url, WebFixture.Driver.Url)

    [<Test>]
    let ``Set by model``() =
        for linkCls, page, url in App.Routing.links do
            elt.ByClass("btn-" + linkCls).Click()
            let resCls, resTxt = App.Routing.matchPage page
            let res = elt.Wait(fun () -> elt.ByClass(resCls))
            Assert.IsTrue(res.Text = resTxt)
            Assert.AreEqual(WebFixture.Url + url, WebFixture.Driver.Url)
