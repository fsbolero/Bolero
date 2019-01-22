namespace Bolero.Tests.Web

open System.Threading
open NUnit.Framework
open OpenQA.Selenium

/// Blazor router integration.
[<Category "Routing">]
module Routing =
    open Bolero

    let elt = NodeFixture()

    [<OneTimeSetUp>]
    let SetUp() =
        elt.Init("test-fixture-routing")

    let links =
        App.Routing.links
        |> List.map (fun (url, page) ->
            let cls = App.Routing.pageClass page
            TestCaseData(cls, url, page).SetArgDisplayNames(
                (string page)
                    // Replace parentheses with unicode ones for nicer display in VS test explorer
                    .Replace("(", "❨")
                    .Replace(")", "❩")))

    [<Test; TestCaseSource("links"); NonParallelizable>]
    let ``Click link``(linkCls: string, url: string, page: App.Routing.Page) =
        elt.ByClass("link-" + linkCls).Click()
        let resCls = App.Routing.pageClass page
        let res =
            try Some <| elt.Wait(fun () -> elt.ByClass(resCls))
            with :? WebDriverTimeoutException -> None
        res |> Option.iter (fun res -> Assert.AreEqual(resCls, res.Text))
        Assert.AreEqual(WebFixture.Url + url, WebFixture.Driver.Url)

    [<Test; TestCaseSource("links"); NonParallelizable>]
    let ``Set by model``(linkCls: string, url: string, page: App.Routing.Page) =
        Thread.Sleep(500) // Some cases fail without this, mainly ones with empty strings. TODO: investigate
        elt.ByClass("btn-" + linkCls).Click()
        let resCls = App.Routing.pageClass page
        let res =
            try Some <| elt.Wait(fun () -> elt.ByClass(resCls))
            with :? WebDriverTimeoutException -> None
        res |> Option.iter (fun res -> Assert.AreEqual(resCls, res.Text))
        Assert.AreEqual(WebFixture.Url + url, WebFixture.Driver.Url)

    let failingRouter<'T>() =
        TestCaseData(fun () -> Router.infer<'T, _, _> id id |> ignore)
            .SetArgDisplayNames(typeof<'T>.Name)

    type ``Invalid parameter syntax`` =
        | [<EndPoint "/{x">] X of x: string
    type ``Unknown parameter name`` =
        | [<EndPoint "/{y}">] X of x: string
    type ``Incomplete parameter list`` =
        | [<EndPoint "/{x}/{z}">] X of x: string * y: string * z: string
    type ``Identical paths with different parameter names`` =
        | [<EndPoint "/foo/{x}">] X of x: string
        | [<EndPoint "/foo/{y}">] Y of y: string
    type ``Mismatched type parameters in same position`` =
        | [<EndPoint "/foo/{x}">] X of x: string
        | [<EndPoint "/foo/{x}/y">] Y of x: int
    type ``Rest parameter in non-final position`` =
        | [<EndPoint "/foo/{*x}/bar">] X of x: string

    let failingRouters = [
        failingRouter<``Invalid parameter syntax``>()
        failingRouter<``Unknown parameter name``>()
        failingRouter<``Incomplete parameter list``>()
        failingRouter<``Identical paths with different parameter names``>()
        failingRouter<``Mismatched type parameters in same position``>()
        failingRouter<``Rest parameter in non-final position``>()
    ]

    [<Test; TestCaseSource "failingRouters"; NonParallelizable>]
    let ``Invalid routers``(makeAndIgnoreRouter: unit -> unit) =
        Assert.Throws(fun () -> makeAndIgnoreRouter()) |> ignore
