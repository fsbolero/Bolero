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
                    driver.Manage().Logs.AvailableLogTypes
                    |> Seq.iter (eprintfn "LOG TYPE: %s")
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
            WebDriverWait(driver, TimeSpan.FromMilliseconds(5000.))
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
