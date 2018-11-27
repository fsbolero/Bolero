// Web application to run and test with Selenium.
namespace Bolero.Tests.Web.App

open Bolero
open Bolero.Html

type Tests() =
    inherit Component()

    override this.Render() =
        div [attr.id "test-fixture"] [
            Html.Tests()
            Elmish.Tests()
            Routing.Tests()
            Remoting.Tests()
            // insert tests here
        ]