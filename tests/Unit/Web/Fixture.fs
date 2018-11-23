namespace Bolero.Tests.Web

open System
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open NUnit.Framework
open OpenQA.Selenium
open OpenQA.Selenium.Chrome
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

    [<OneTimeSetUp>]
    member this.SetUp() =
        async {
            // Start Selenium and ASP.NET Core in parallel
            let! _ = Async.Parallel [
                async {
                    let options = ChromeOptions()
                    options.AddArguments ["headless"; "disable-gpu"]
                    driver <- new ChromeDriver(System.Environment.CurrentDirectory, options)
                }
                async {
                    server <- WebHost.CreateDefaultBuilder()
                        .UseStartup<Startup>()
                        .UseUrls(url)
                        .Build()
                    return! server.StartAsync() |> Async.AwaitTask
                }
            ]

            // Once both are started, browse and wait for the page to render.
            driver.Navigate().GoToUrl(url)
            root <- WebDriverWait(driver, TimeSpan.FromSeconds(5.))
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
        WebDriverWait(WebFixture.Driver,
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
