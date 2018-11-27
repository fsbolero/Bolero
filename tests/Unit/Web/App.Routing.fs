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
    | [<EndPoint "/with-nested-union">] WithNestedUnion of Page
    | [<EndPoint "/with-tuple">] WithTuple of (int * string * bool)
    | [<EndPoint "/with-record">] WithRecord of Record
    | [<EndPoint "/with-list">] WithList of list<int * string>
    | [<EndPoint "/with-array">] WithArray of (int * string)[]

    member this.ExpectedUrl'(isInitial: bool) =
        match this with
        | Home -> if isInitial then "/" else ""
        | NoArg -> "/no-arg"
        | WithArg s -> sprintf "/with-arg/%s" s
        | WithArgs(s, i) -> sprintf "/with-args/%s/%i" s i
        | WithUnion u -> sprintf "/with-union%s" u.ExpectedUrl
        | WithUnionNotTerminal(u, s) -> sprintf "/with-union2%s/%s" u.ExpectedUrl s
        | WithNestedUnion u -> sprintf "/with-nested-union%s" (u.ExpectedUrl' false)
        | WithTuple((i, s, b)) -> sprintf "/with-tuple/%i/%s/%b" i s b
        | WithRecord { x = x; y = y; z = z } -> sprintf "/with-record/%i%s/%b" x y.ExpectedUrl z
        | WithList l -> sprintf "/with-list/%i%s" l.Length
                        <| String.concat "" [for i, s in l -> sprintf "/%i/%s" i s]
        | WithArray a -> sprintf "/with-array/%i%s" a.Length
                        <| String.concat "" [for i, s in a -> sprintf "/%i/%s" i s]

    member this.ExpectedUrl = this.ExpectedUrl'(true)

and InnerPage =
    | [<EndPoint "/">] InnerHome
    | [<EndPoint "/no-arg">] InnerNoArg
    | [<EndPoint "/with-arg">] InnerWithArg of string
    | [<EndPoint "/with-args">] InnerWithArgs of string * int

    member this.ExpectedUrl =
        match this with
        | InnerHome -> ""
        | InnerNoArg -> "/no-arg"
        | InnerWithArg s -> sprintf "/with-arg/%s" s
        | InnerWithArgs(s, i) -> sprintf "/with-args/%s/%i" s i

and Record =
    {
        x: int
        y: InnerPage
        z: bool
    }

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
        "home", InnerHome
        "noarg", InnerNoArg
        "witharg1", InnerWithArg "foo"
        "witharg2", InnerWithArg "bar"
        "witharg3", InnerWithArg ""
        "withargs1", InnerWithArgs("foo", 1)
        "withargs2", InnerWithArgs("bar", 2)
        "withargs3", InnerWithArgs("", 3)
    ]

let baseLinks =
    [
        yield! [
            "home", Home
            "noarg", NoArg
            "witharg1", WithArg "foo"
            "witharg2", WithArg "bar"
            "witharg3", WithArg ""
            "withargs1", WithArgs("foo", 1)
            "withargs2", WithArgs("bar", 2)
            "withargs3", WithArgs("", 3)
            "withtuple1", WithTuple(42, "hi", true)
            "withtuple2", WithTuple(324, "", false)
            "withlist1", WithList [2, "a"; 34, "b"]
            "withlist2", WithList [2, ""; 34, "b"]
            "witharray1", WithArray [|2, "a"; 34, "b"|]
            "witharray2", WithArray [|2, ""; 34, "b"|]
        ]
        for cls, page in innerlinks true do
            yield "inner" + cls, WithUnion page
        for cls, page in innerlinks false do
            yield "innernonterminal1" + cls, WithUnionNotTerminal (page, "foo")
            yield "innernonterminal2" + cls, WithUnionNotTerminal (page, "")
            yield "withrecord" + cls, WithRecord { x = 1; y = page; z = true }
    ]

let links =
    baseLinks @
    List.map (fun (cls, page) -> "withnested" + cls, WithNestedUnion page) baseLinks

let matchInnerPage = function
    | InnerHome -> "home"
    | InnerNoArg -> "noarg"
    | InnerWithArg x -> sprintf "witharg-%s" x
    | InnerWithArgs(x, y) -> sprintf "withargs-%s-%i" x y

let rec matchPage = function
    | Home -> "home"
    | NoArg -> "noarg"
    | WithArg x -> sprintf "witharg-%s" x
    | WithArgs(x, y) -> sprintf "withargs-%s-%i" x y
    | WithUnion u -> "withunion-" + matchInnerPage u
    | WithUnionNotTerminal(u, s) -> sprintf "withunion2-%s-%s" (matchInnerPage u) s
    | WithNestedUnion u -> sprintf "withnested-%s" (matchPage u)
    | WithTuple(x, y, z) -> sprintf "withtuple-%i-%s-%b" x y z
    | WithRecord { x = x; y = y; z = z } -> sprintf "withrecord-%i-%s-%b" x (matchInnerPage y) z
    | WithList l -> sprintf "withlist-%s" (String.concat "-" [for i, s in l -> sprintf "%i-%s" i s])
    | WithArray a -> sprintf "witharray-%s" (String.concat "-" [for i, s in a -> sprintf "%i-%s" i s])

let view model dispatch =
    concat [
        for cls, page in links do
            let url = page.ExpectedUrl
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
