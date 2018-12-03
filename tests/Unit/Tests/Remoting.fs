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
