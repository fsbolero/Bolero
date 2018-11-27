module Bolero.Tests.Web.App.Routing

open Bolero
open Bolero.Html
open Elmish

type Page =
    | [<EndPoint "/">] Home
    | [<EndPoint "/no-arg">] NoArg
    | [<EndPoint "/with-arg">] WithArg of string
    | [<EndPoint "/with-args">] WithArgs of string * int
    | [<EndPoint "/with-union">] WithUnion of InnerPage
    | [<EndPoint "/with-union2">] WithUnionNotTerminal of InnerPage * string
    | [<EndPoint "/with-tuple">] WithTuple of (int * string * bool)

and InnerPage =
    | [<EndPoint "/">] InnerHome
    | [<EndPoint "/no-arg">] InnerNoArg
    | [<EndPoint "/with-arg">] InnerWithArg of string
    | [<EndPoint "/with-args">] InnerWithArgs of string * int

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

let innerlinks isTerminal =
    [
        "home", InnerHome, ""
        "noarg", InnerNoArg, "/no-arg"
        "witharg1", (InnerWithArg "foo"), "/with-arg/foo"
        "witharg2", (InnerWithArg "bar"), "/with-arg/bar"
        "witharg3", (InnerWithArg ""), "/with-arg/"
        "withargs1", (InnerWithArgs("foo", 1)), "/with-args/foo/1"
        "withargs2", (InnerWithArgs("bar", 2)), "/with-args/bar/2"
        "withargs3", (InnerWithArgs("", 3)), "/with-args//3"
    ]

let links =
    [
        yield! [
            "home", Home, "/"
            "noarg", NoArg, "/no-arg"
            "witharg1", WithArg "foo", "/with-arg/foo"
            "witharg2", WithArg "bar", "/with-arg/bar"
            "witharg3", WithArg "", "/with-arg/"
            "withargs1", WithArgs("foo", 1), "/with-args/foo/1"
            "withargs2", WithArgs("bar", 2), "/with-args/bar/2"
            "withargs3", WithArgs("", 3), "/with-args//3"
            "withtuple1", WithTuple(42, "hi", true), "/with-tuple/42/hi/True"
            "withtuple2", WithTuple(324, "", false), "/with-tuple/324//False"
        ]
        for cls, page, url in innerlinks true do
            yield "inner" + cls, WithUnion page, "/with-union" + url
        for cls, page, url in innerlinks false do
            yield "innernonterminal1" + cls, WithUnionNotTerminal (page, "foo"), "/with-union2" + url + "/foo"
            yield "innernonterminal2" + cls, WithUnionNotTerminal (page, ""), "/with-union2" + url + "/"
    ]

let matchInnerPage = function
    | InnerHome -> "home"
    | InnerNoArg -> "noarg"
    | InnerWithArg x -> sprintf "witharg-%s" x
    | InnerWithArgs(x, y) -> sprintf "withargs-%s-%i" x y

let matchPage = function
    | Home -> "home"
    | NoArg -> "noarg"
    | WithArg x -> sprintf "witharg-%s" x
    | WithArgs(x, y) -> sprintf "withargs-%s-%i" x y
    | WithUnion u -> "withunion-" + matchInnerPage u
    | WithUnionNotTerminal(u, s) -> sprintf "withunion2-%s-%s" (matchInnerPage u) s
    | WithTuple(x, y, z) -> sprintf "withtuple-%i-%s-%b" x y z

let view model dispatch =
    concat [
        for cls, page, url in links do
            yield a [attr.classes ["link-" + cls]; router.HRef page] [text url]
            yield button [
                attr.classes ["btn-" + cls]
                attr.value (router.Link page)
                on.click (fun _ -> dispatch (SetPage page))
            ] [text url]
        yield cond model.page <| fun x ->
            let cls = matchPage x
            span [attr.classes [cls]] [text cls]
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
