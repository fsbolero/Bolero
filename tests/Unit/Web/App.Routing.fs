module Bolero.Tests.Web.App.Routing

open Bolero
open Bolero.Html
open Elmish

type Page =
    | [<EndPoint "/">] Home
    | [<EndPoint "/no-arg">] NoArg
    | [<EndPoint "/with-arg">] WithArg of string
    | [<EndPoint "/with-args">] WithArgs of string * int

type Model =
    {
        page: Page
    }

let initModel =
    {
        page = Home
    }

type Message =
    | SetPage of Page

let update msg model =
    match msg with
    | SetPage p -> { model with page = p }

let router = Router.infer SetPage (fun m -> m.page)

let links =
    [
        "home", Home, "/"
        "noarg", NoArg, "/no-arg"
        "witharg1", WithArg "foo", "/with-arg/foo"
        "witharg2", WithArg "bar", "/with-arg/bar"
        "withargs1", WithArgs("foo", 1), "/with-args/foo/1"
        "withargs2", WithArgs("bar", 2), "/with-args/bar/2"
    ]

let matchPage = function
    | Home -> "Home", ""
    | NoArg -> "NoArg", ""
    | WithArg x -> "WithArg", x
    | WithArgs(x, y) -> "WithArgs", sprintf "%s %i" x y

let view model dispatch =
    concat [
        for cls, page, _ in links do
            yield a [attr.classes ["link-" + cls]; router.HRef page] [text "click"]
            yield button [
                attr.classes ["btn-" + cls]
                attr.value (router.Link page)
                on.click (fun _ -> dispatch (SetPage page))
            ] [text "click"]
        yield cond model.page <| fun x ->
            let cls, txt = matchPage x
            span [attr.classes [cls]] [text txt]
    ]

type Test() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        Program.mkSimple (fun _ -> initModel) update view
        |> Program.withRouter router

let Tests() =
    div [attr.id "test-fixture-routing"] [
        comp<Test> [] []
    ]
