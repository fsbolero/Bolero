// Web application to run and test with Selenium.
module Bolero.Tests.Web.WebApp

open Bolero
open Bolero.Html

type RootComponent() =
    inherit Component()

    override this.Render() =
        div [attr.id "test-fixture"] [
            // TODO: insert tests here
        ]
