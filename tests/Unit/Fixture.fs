// $begin{copyright}
//
// This file is part of Bolero
//
// Copyright (c) 2018 IntelliFactory and contributors
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License.  You may
// obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied.  See the License for the specific language governing
// permissions and limitations under the License.
//
// $end{copyright}

namespace Bolero.Tests.Web

open System
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open NUnit.Framework
open OpenQA.Selenium
open OpenQA.Selenium.Chrome
// open OpenQA.Selenium.Firefox
open OpenQA.Selenium.Remote
open OpenQA.Selenium.Support.UI

/// Defines the setup/teardown for all tests that use the web server and Selenium.
/// These web tests must be located in the namespace Bolero.Tests.Web
/// so that this fixture properly covers them.
[<SetUpFixture>]
type WebFixture() =

    static let mutable server = Unchecked.defaultof<IWebHost>

    static let mutable driver = Unchecked.defaultof<RemoteWebDriver>

    static let mutable root = Unchecked.defaultof<IWebElement>

    static let url = "http://localhost:51608"

    static let startChrome() =
        async {
            let options = ChromeOptions()
            options.AddArguments ["headless"; "disable-gpu"]
            driver <- new ChromeDriver(Environment.CurrentDirectory, options)
        }

    // static let startFirefox() =
    //     async {
    //         let options = FirefoxOptions()
    //         options.AddArgument "-headless"
    //         driver <- new FirefoxDriver(System.Environment.CurrentDirectory, options)
    //     }

    static member MkWait(timeout) =
        WebDriverWait(SystemClock(), driver, timeout, TimeSpan.FromMilliseconds(500.))

    [<OneTimeSetUp>]
    member this.SetUp() =
        async {
            // Start Selenium and ASP.NET Core in parallel
            let! _ = Async.Parallel [
                startChrome()
                async {
                    server <- WebHost.CreateDefaultBuilder([||])
                        .UseContentRoot(__SOURCE_DIRECTORY__)
                        .UseStartup<Startup>()
                        .UseUrls(url)
                        .Build()
                    return! server.StartAsync() |> Async.AwaitTask
                }
            ]

            // Once both are started, browse and wait for the page to render.
            driver.Navigate().GoToUrl(url)
            root <- WebFixture.MkWait(TimeSpan.FromSeconds(5.))
                .Until(fun d -> try d.FindElement(By.Id "test-fixture") with _ -> null)
        }
        |> Async.StartImmediateAsTask
        :> Task

    [<OneTimeTearDown>]
    member this.TearDown() =
        // Stop Selenium and ASP.NET Core in parallel
        Async.Parallel [
            async {
                driver.Dispose()
                driver <- Unchecked.defaultof<_>
            }
            async {
                do! server.StopAsync() |> Async.AwaitTask
                server <- Unchecked.defaultof<_>
            }
        ]
        |> Async.StartImmediateAsTask
        :> Task

    static member Server = server
    static member Driver = driver
    static member Url = url
    static member Root = root

and NodeFixture() =

    member val Root: IWebElement = null with get, set

    /// Initialize this node fixture.
    /// Must be called after WebFixture initialization.
    member this.Init(id) =
        this.Root <- WebFixture.Root.FindElement(By.Id id)

    /// Get a node fixture nested in the root of this one.
    /// The returned fixture doesn't need to be `Init()`ed.
    member this.Inner(by) =
        new NodeFixture(Root = this.Root.FindElement(by))

    /// Get child element by id.
    member this.ById(x) =
        try this.Root.FindElement(By.Id x)
        with :? NoSuchElementException -> null

    /// Get child element by class.
    member this.ByClass(x) =
        try this.Root.FindElement(By.ClassName x)
        with :? NoSuchElementException -> null

    /// Wait for the given callback to return a non-false non-null value.
    member this.Wait(cond, ?timeout) =
        WebFixture.MkWait(
            timeout |> Option.defaultWith (fun () -> TimeSpan.FromSeconds(5.)))
            .Until(fun _ -> cond())

    /// NUnit assertion that the given condition eventually becomes non-null non-false.
    member this.AssertEventually(cond, ?message, ?timeout) =
        let run() =
            match this.Wait(cond, ?timeout = timeout) |> box with
            | :? bool as b -> Assert.IsTrue(b)
            | x -> Assert.IsNotNull(x)
        match message with
        | None -> Assert.DoesNotThrow(TestDelegate run)
        | Some m -> Assert.DoesNotThrow(TestDelegate run, m)

    /// NUnit assertion that the actual value eventually becomes equal to the expected value.
    member this.AssertAreEqualEventually(expected, getActual, ?message: string, ?timeout) =
        let mutable actual = Unchecked.defaultof<_>
        try this.Wait(
                (fun () ->
                    actual <- getActual()
                    expected = actual),
                ?timeout = timeout) |> ignore
        with :? WebDriverTimeoutException -> ()
        match message with
        | None -> Assert.AreEqual(expected, actual)
        | Some m -> Assert.AreEqual(expected, actual, m)

[<AutoOpen>]
module Extensions =

    type IWebElement with
        member this.ByClass(cls) =
            try this.FindElement(By.ClassName cls)
            with :? NoSuchElementException -> null

        member this.SendIndividualKeys(s: string) =
            for c in s do
                this.SendKeys(string c)
