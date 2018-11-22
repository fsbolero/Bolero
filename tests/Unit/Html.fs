// Test HTML functionality in-browser with Selenium.
namespace Bolero.Tests.Web

open NUnit.Framework

[<Category "HTML">]
module Html =

    [<Test>]
    let ``Content is created``() =
        Assert.NotNull(WebFixture.Driver.FindElementById("test-fixture"))
