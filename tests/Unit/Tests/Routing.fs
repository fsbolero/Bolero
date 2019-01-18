namespace Bolero.Tests.Web

open System.Threading
open NUnit.Framework
open OpenQA.Selenium

/// Blazor router integration.
[<Category "Routing">]
module Routing =

    let elt = NodeFixture()

    [<OneTimeSetUp>]
    let SetUp() =
        elt.Init("test-fixture-routing")

    let links =
        App.Routing.links
        |> List.map (fun page ->
            let cls = App.Routing.pageClass page
            TestCaseData(cls, page).SetArgDisplayNames(
                (string page)
                    // Replace parentheses with unicode ones for nicer display in VS test explorer
                    .Replace("(", "❨")
                    .Replace(")", "❩")))

    [<Test; TestCaseSource("links"); NonParallelizable>]
    let ``Click link``(linkCls, page: App.Routing.Page) =
        let url = page.ExpectedUrl
        elt.ByClass("link-" + linkCls).Click()
        let resCls = App.Routing.pageClass page
        let res =
            try Some <| elt.Wait(fun () -> elt.ByClass(resCls))
            with :? WebDriverTimeoutException -> None
        res |> Option.iter (fun res -> Assert.AreEqual(resCls, res.Text))
        Assert.AreEqual(WebFixture.Url + url, WebFixture.Driver.Url)

    [<Test; TestCaseSource("links"); NonParallelizable>]
    let ``Set by model``(linkCls, page: App.Routing.Page) =
        Thread.Sleep(500) // Some cases fail without this, mainly ones with empty strings. TODO: investigate
        let url = page.ExpectedUrl
        elt.ByClass("btn-" + linkCls).Click()
        let resCls = App.Routing.pageClass page
        let res =
            try Some <| elt.Wait(fun () -> elt.ByClass(resCls))
            with :? WebDriverTimeoutException -> None
        res |> Option.iter (fun res -> Assert.AreEqual(resCls, res.Text))
        Assert.AreEqual(WebFixture.Url + url, WebFixture.Driver.Url)
