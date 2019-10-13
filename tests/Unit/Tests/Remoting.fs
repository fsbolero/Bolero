namespace Bolero.Tests.Web

open NUnit.Framework
open OpenQA.Selenium
open FsCheck.NUnit
open FsCheck
open Swensen.Unquote
open Bolero.Tests

/// Server remote calls.
[<Category "Remoting"; NonParallelizable>]
module Remoting =

    let elt = NodeFixture(By.Id "test-fixture-remoting")

    [<Ignore "Remoting test randomly fails; TODO fix">]
    [<Property(MaxTest = 10); NonParallelizable>]
    let ``Set and remove key`` (Alphanum k) (Alphanum v) =
        let keyInp = elt.ByClass("key-input")
        let valInp = elt.ByClass("value-input")
        let addBtn = elt.ByClass("add-btn")
        let remBtn = elt.ByClass("rem-btn")
        keyInp.Clear()
        // Add an "End" key so that some input is sent
        // even if the string is empty, to ensure that oninput is triggered
        keyInp.SendKeys(k + Keys.End)
        valInp.Clear()
        valInp.SendKeys(v + Keys.End)
        sprintf "%s => %s" k v @| [
            "remove" @| (
                remBtn.Click()
                testNotNull <@ elt.Wait(fun () -> elt.ByClass("output-empty")) @>
            )
            "add" @| (
                addBtn.Click()
                test <@ elt.Wait(fun () -> elt.ByClass("output")).Text = v @>
            )
        ]

    [<Test; NonParallelizable>]
    let ``Authorized remote function succeeds when signed in`` () =
        let username = elt.ByClass("signin-input")
        username.Clear()
        username.SendKeys("someone")
        elt.ByClass("signin-button").Click()
        elt.Eventually <@ elt.ByClass("is-signedin").Text = "someone" @>

    [<Test; NonParallelizable>]
    let ``Authorized remote function fails when signed out`` () =
        elt.ByClass("signout-button").Click()
        elt.Eventually <@ elt.ByClass("is-signedin").Text = "<not logged in>" @>

    [<Test; NonParallelizable>]
    let ``Authorized remote function fails if role is missing`` () =
        let username = elt.ByClass("signin-input")
        username.Clear()
        username.SendKeys("someone")
        elt.ByClass("signin-button").Click()
        elt.Eventually <@ elt.ByClass("is-signedin").Text = "someone" @>
        elt.ByClass("get-admin").Click()
        elt.Eventually <@ elt.ByClass("is-admin").Text = "<not admin>" @>

    [<Test; NonParallelizable>]
    let ``Authorized remote function succeeds if role is present`` () =
        let username = elt.ByClass("signin-input")
        username.Clear()
        username.SendKeys("admin")
        elt.ByClass("signin-button").Click()
        elt.Eventually <@ elt.ByClass("is-signedin").Text = "admin" @>
        elt.ByClass("get-admin").Click()
        elt.Eventually <@ elt.ByClass("is-admin").Text = "admin ok" @>
