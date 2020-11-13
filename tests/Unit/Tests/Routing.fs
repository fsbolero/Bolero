namespace Bolero.Tests.Web

open System.Text.RegularExpressions
open FSharp.Reflection
open NUnit.Framework
open OpenQA.Selenium
open Swensen.Unquote
open Bolero.Tests

/// Blazor router integration.
[<Category "Routing">]
module Routing =
    open Bolero
    open System.Collections.Generic

    let elt = NodeFixture(By.Id "test-fixture-routing")

    let links =
        let re = Regex("\s+")
        Client.Routing.links
        |> List.map (fun (url, page) ->
            let cls = Client.Routing.pageClass page
            let print = re.Replace($"{page}", " ")
            TestCaseData(cls, url, print).SetArgDisplayNames(
                (string page)
                    // Replace parentheses with unicode ones for nicer display in VS test explorer
                    .Replace("(", "❨")
                    .Replace(")", "❩")))

    [<Test; TestCaseSource("links"); NonParallelizable>]
    let ``Click link``(linkCls: string, url: string, print: string) =
        elt.ByClass("link-" + linkCls).Click()
        elt.Eventually <@ elt.ByClass("current-page").Text = print @>
        test <@ WebFixture.Driver.Url = WebFixture.Url + url @>

    [<Test; TestCaseSource("links"); NonParallelizable>]
    let ``Set by model``(linkCls: string, url: string, print: string) =
        elt.ByClass("btn-" + linkCls).Click()
        elt.Eventually <@ elt.ByClass("current-page").Text = print @>
        test <@ WebFixture.Driver.Url = WebFixture.Url + url @>

    let failingRouter<'T> (expectedError: UnionCaseInfo[] -> InvalidRouterKind) =
        TestCaseData(
            (fun () -> Router.infer<'T, _, _> id id |> ignore),
            expectedError (try FSharpType.GetUnionCases typeof<'T> with _ -> [||])
        )
            .SetArgDisplayNames(typeof<'T>.Name)

    type ``Invalid parameter syntax`` =
        | [<EndPoint "/{x">] X of x: string
    type ``Unknown parameter name`` =
        | [<EndPoint "/{y}">] X of x: string
    type ``Duplicate field`` =
        | [<EndPoint"/{x}/{x}">] X of x: string
    type ``Incomplete parameter list`` =
        | [<EndPoint "/{x}/{z}">] X of x: string * y: string * z: string
    type ``Identical paths with different parameter names`` =
        | [<EndPoint "/foo/{x}">] X of x: string
        | [<EndPoint "/foo/{y}">] Y of y: string
    type ``Mismatched type parameters in same position`` =
        | [<EndPoint "/foo/{x}">] X of x: string
        | [<EndPoint "/foo/{y}/y">] Y of y: int
    type ``Rest parameter in non-final position`` =
        | [<EndPoint "/foo/{*x}/bar">] X of x: string

    let failingRouters = [
        failingRouter<``Invalid parameter syntax``> <| fun c ->
            InvalidRouterKind.ParameterSyntax(c.[0], "{x")
        failingRouter<``Unknown parameter name``> <| fun c ->
            InvalidRouterKind.UnknownField(c.[0], "y")
        failingRouter<``Duplicate field``> <| fun c ->
            InvalidRouterKind.DuplicateField(c.[0], "x")
        failingRouter<``Incomplete parameter list``> <| fun c ->
            InvalidRouterKind.MissingField(c.[0], "y")
        failingRouter<``Identical paths with different parameter names``> <| fun c ->
            InvalidRouterKind.IdenticalPath(c.[1], c.[0])
        failingRouter<``Mismatched type parameters in same position``> <| fun c ->
            InvalidRouterKind.ParameterTypeMismatch(c.[1], "y", c.[0], "x")
        failingRouter<``Rest parameter in non-final position``> <| fun c ->
            InvalidRouterKind.RestNotLast(c.[0])
        failingRouter<Dictionary<int, int>> <| fun _ ->
            InvalidRouterKind.UnsupportedType typeof<Dictionary<int, int>>
    ]

    [<Test; TestCaseSource "failingRouters"; NonParallelizable>]
    let ``Invalid routers``(makeAndIgnoreRouter: unit -> unit, expectedError: InvalidRouterKind) =
        raisesWith <@ makeAndIgnoreRouter() @>
            (fun exn -> <@ exn = InvalidRouter expectedError @>)
