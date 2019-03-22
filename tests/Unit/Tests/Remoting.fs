namespace Bolero.Tests.Web

open System.Threading.Tasks
open NUnit.Framework
open OpenQA.Selenium
open FsCheck.NUnit
open FsCheck
open Bolero.Tests

/// Server remote calls.
[<Category "Remoting"; NonParallelizable>]
module Remoting =

    let elt = NodeFixture()

    [<OneTimeSetUp>]
    let SetUp() =
        elt.Init("test-fixture-remoting")


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
                let out = elt.Wait(fun () -> elt.ByClass("output-empty"))
                not (isNull out)
            )
            "add" @| (
                addBtn.Click()
                let out = elt.Wait(fun () -> elt.ByClass("output"))
                out.Text = v
            )
        ]

    [<Test; NonParallelizable>]
    let ``Authorized remote function succeeds when signed in`` () =
        let username = elt.ByClass("signin-input")
        username.Clear()
        username.SendKeys("someone")
        elt.ByClass("signin-button").Click()
        elt.AssertAreEqualEventually("someone", fun () -> elt.ByClass("is-signedin").Text)

    [<Test; NonParallelizable>]
    let ``Authorized remote function fails when signed out`` () =
        elt.ByClass("signout-button").Click()
        elt.AssertAreEqualEventually("<not logged in>", fun () -> elt.ByClass("is-signedin").Text)

    [<Test; NonParallelizable>]
    let ``Authorized remote function fails if role is missing`` () =
        let username = elt.ByClass("signin-input")
        username.Clear()
        username.SendKeys("someone")
        elt.ByClass("signin-button").Click()
        elt.AssertAreEqualEventually("someone", fun () -> elt.ByClass("is-signedin").Text)
        elt.ByClass("get-admin").Click()
        elt.AssertAreEqualEventually("<not admin>", fun () -> elt.ByClass("is-admin").Text)

    [<Test; NonParallelizable>]
    let ``Authorized remote function succeeds if role is present`` () =
        let username = elt.ByClass("signin-input")
        username.Clear()
        username.SendKeys("admin")
        elt.ByClass("signin-button").Click()
        elt.AssertAreEqualEventually("admin", fun () -> elt.ByClass("is-signedin").Text)
        elt.ByClass("get-admin").Click()
        elt.AssertAreEqualEventually("admin ok", fun () -> elt.ByClass("is-admin").Text)
