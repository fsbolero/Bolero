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
open FSharp.Quotations
open NUnit.Framework
open OpenQA.Selenium
open OpenQA.Selenium.Chrome
// open OpenQA.Selenium.Firefox
open OpenQA.Selenium.Support.UI
open Swensen.Unquote
open Bolero.Tests

/// Defines the setup/teardown for all tests that use the web server and Selenium.
/// These web tests must be located in the namespace Bolero.Tests.Web
/// so that this fixture properly covers them.
[<SetUpFixture>]
type WebFixture() =

    static let mutable server = Unchecked.defaultof<IWebHost>

    static let mutable driver = Unchecked.defaultof<WebDriver>

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
                        .UseStaticWebAssets()
                        .UseStartup<Startup>()
                        .UseUrls(url)
                        .Build()
                    return! server.StartAsync() |> Async.AwaitTask
                }
            ]

            // Once both are started, browse and wait for the page to render.
            driver.Navigate().GoToUrl(url)
            WebFixture.MkWait(TimeSpan.FromSeconds(20.))
                .Until(fun d -> try d.FindElement(By.Id "test-fixture") with _ -> null)
            |> ignore
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
    static member Root() = driver.FindElement(By.Id "test-fixture")

and NodeFixture(parent: unit -> IWebElement, by: By) =

    new(by: By) = NodeFixture(WebFixture.Root, by)

    member this.Root() =
        parent().FindElement(by)

    /// Get a node fixture nested in the root of this one.
    /// The returned fixture doesn't need to be `Init()`ed.
    member this.Inner(by) =
        NodeFixture(this.Root, by)

    /// Get child element by id.
    member this.ById(x) =
        try this.Root().FindElement(By.Id x)
        with :? NoSuchElementException -> null

    /// Get child element by class.
    member this.ByClass(x) =
        try this.Root().FindElement(By.ClassName x)
        with :? NoSuchElementException -> null

    /// Wait for the given callback to return a non-false non-null value.
    member this.Wait(cond, ?timeout) =
        WebFixture.MkWait(
            timeout |> Option.defaultWith (fun () -> TimeSpan.FromSeconds(5.)))
            .Until(fun _ -> cond())

    member private this.WaitOrFalse(f: unit -> bool) =
        try this.Wait(f)
        with :? WebDriverTimeoutException -> false

    /// Assert that the given test eventually succeeds.
    member this.Eventually(expr: Expr<bool>) =
        if not (this.WaitOrFalse(fun () -> eval expr)) then
            test expr

    /// Assert that the given value is eventually null.
    member this.EventuallyNull(expr: Expr<'T>) =
        this.Eventually <@ isNull %expr @>

    /// Assert that the given value is eventually non-null.
    member this.EventuallyNotNull(expr: Expr<'T>) =
        this.Wait(fun () -> eval expr)

[<AutoOpen>]
module Extensions =

    type IWebElement with
        member this.ByClass(cls) =
            try this.FindElement(By.ClassName cls)
            with :? NoSuchElementException -> null

        member this.SendIndividualKeys(s: string) =
            for c in s do
                this.SendKeys(string c)
