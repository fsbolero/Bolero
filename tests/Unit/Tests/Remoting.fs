namespace Bolero.Tests.Web

open System.Threading.Tasks
open NUnit.Framework
open OpenQA.Selenium.Support.UI
open FsCheck.NUnit
open FsCheck
open Bolero.Tests

/// Server remote calls.
[<Category "Remoting">]
module Remoting =

    let elt = NodeFixture()

    [<OneTimeSetUp>]
    let SetUp() =
        elt.Init("test-fixture-remoting")


    [<Property(MaxTest = 10)>]
    let ``Set and remove key`` (Alphanum k) (Alphanum v) =
        let keyInp = elt.ByClass("key-input")
        let valInp = elt.ByClass("value-input")
        let addBtn = elt.ByClass("add-btn")
        let remBtn = elt.ByClass("rem-btn")
        keyInp.Clear()
        // Add a letter and a backspace so that some input is sent
        // even if the string is empty, to ensure that oninput is triggered
        keyInp.SendKeys(k + "a\b")
        valInp.Clear()
        valInp.SendKeys(v + "a\b")
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
