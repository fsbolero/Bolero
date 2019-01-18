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
    | [<EndPoint "/with-path/{arg}">] WithPath of arg: string
    | [<EndPoint "/with-path/{arg}/and-suffix">] WithPathAndSuffix of arg: string
    | [<EndPoint "/with-path/{arg}/and-suffix/{arg2}">] WithPathAndSuffix2 of arg: string * arg2: int
    | [<EndPoint "/with-path/{arg}/other-suffix">] WithPathAndSuffix3 of arg: string
    | [<EndPoint "/with-path/and/constant">] WithPathConstant
    | [<EndPoint "/with-path-record/{arg}">] WithPathRecord of arg: Record

    member this.ExpectedUrl =
        match this with
        | Home -> "/"
        | NoArg -> "/no-arg"
        | WithArg s -> sprintf "/with-arg/%s" s
        | WithArgs(s, i) -> sprintf "/with-args/%s/%i" s i
        | WithUnion u -> sprintf "/with-union%s" u.ExpectedUrl
        | WithUnionNotTerminal(u, s) -> sprintf "/with-union2%s/%s" u.ExpectedUrl s
        | WithNestedUnion u -> sprintf "/with-nested-union%s" u.ExpectedUrl
        | WithTuple((i, s, b)) -> sprintf "/with-tuple/%i/%s/%b" i s b
        | WithRecord { x = x; y = y; z = z } -> sprintf "/with-record/%i%s/%b" x y.ExpectedUrl z
        | WithList l -> sprintf "/with-list/%i%s" l.Length
                        <| String.concat "" [for i, s in l -> sprintf "/%i/%s" i s]
        | WithArray a -> sprintf "/with-array/%i%s" a.Length
                        <| String.concat "" [for i, s in a -> sprintf "/%i/%s" i s]
        | WithPath s -> sprintf "/with-path/%s" s
        | WithPathAndSuffix s -> sprintf "/with-path/%s/and-suffix" s
        | WithPathAndSuffix2(s, i) -> sprintf "/with-path/%s/and-suffix/%i" s i
        | WithPathAndSuffix3 s -> sprintf "/with-path/%s/other-suffix" s
        | WithPathConstant -> "/with-path/and/constant"
        | WithPathRecord { x = x; y = y; z = z } -> sprintf "/with-path-record/%i%s/%b" x y.ExpectedUrl z

and InnerPage =
    | [<EndPoint "/">] InnerHome
    | [<EndPoint "/no-arg">] InnerNoArg
    | [<EndPoint "/with-arg">] InnerWithArg of string
    | [<EndPoint "/with-args">] InnerWithArgs of string * int

    member this.ExpectedUrl =
        match this with
        | InnerHome -> "/"
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

let router =
    try Router.infer SetPage (fun m -> m.page)
    with e ->
        eprintfn "ROUTER ERROR: %A" e
        reraise()

let matchInnerPage = function
    | InnerHome -> "home"
    | InnerNoArg -> "noarg"
    | InnerWithArg x -> sprintf "witharg-%s" x
    | InnerWithArgs(x, y) -> sprintf "withargs-%s-%i" x y

let rec pageClass = function
    | Home -> "home"
    | NoArg -> "noarg"
    | WithArg x -> sprintf "witharg-%s" x
    | WithArgs(x, y) -> sprintf "withargs-%s-%i" x y
    | WithUnion u -> "withunion-" + matchInnerPage u
    | WithUnionNotTerminal(u, s) -> sprintf "withunion2-%s-%s" (matchInnerPage u) s
    | WithNestedUnion u -> sprintf "withnested-%s" (pageClass u)
    | WithTuple(x, y, z) -> sprintf "withtuple-%i-%s-%b" x y z
    | WithRecord { x = x; y = y; z = z } -> sprintf "withrecord-%i-%s-%b" x (matchInnerPage y) z
    | WithList l -> sprintf "withlist-%s" (String.concat "-" [for i, s in l -> sprintf "%i-%s" i s])
    | WithArray a -> sprintf "witharray-%s" (String.concat "-" [for i, s in a -> sprintf "%i-%s" i s])
    | WithPath s -> sprintf "withpath-%s" s
    | WithPathAndSuffix s -> sprintf "withpathsuffix-%s" s
    | WithPathAndSuffix2(s, i) -> sprintf "withpathsuffix2-%s-%i" s i
    | WithPathAndSuffix3 s -> sprintf "withpathsuffix3-%s" s
    | WithPathConstant -> "withpathconstant"
    | WithPathRecord { x = x; y = y; z = z } -> sprintf "withpathrecord-%i-%s-%b" x (matchInnerPage y) z

let innerlinks =
    [
        InnerHome
        InnerNoArg
        InnerWithArg "foo"
        InnerWithArg "bar"
        InnerWithArg ""
        InnerWithArgs("foo", 1)
        InnerWithArgs("bar", 2)
        InnerWithArgs("", 3)
    ]

let baseLinks =
    [
        yield! [
            Home
            NoArg
            WithArg "foo"
            WithArg "bar"
            WithArg ""
            WithArgs("foo", 1)
            WithArgs("bar", 2)
            WithArgs("", 3)
            WithTuple(42, "hi", true)
            WithTuple(324, "", false)
            WithList [2, "a"; 34, "b"]
            WithList [2, ""; 34, "b"]
            WithArray [|2, "a"; 34, "b"|]
            WithArray [|2, ""; 34, "b"|]
            WithPath "abc"
            WithPath ""
            WithPathAndSuffix "abc"
            WithPathAndSuffix ""
            WithPathAndSuffix2("abc", 123)
            WithPathAndSuffix2("", 123)
            WithPathAndSuffix3 "abc"
            WithPathAndSuffix3 ""
            WithPathConstant
        ]
        for page in innerlinks do
            yield WithUnion page
            yield WithUnionNotTerminal (page, "foo")
            yield WithUnionNotTerminal (page, "")
            yield WithRecord { x = 1; y = page; z = true }
            yield WithPathRecord { x = 3; y = page; z = false }
    ]

let links =
    baseLinks @
    List.map WithNestedUnion baseLinks

let view model dispatch =
    concat [
        for page in links do
            let url = page.ExpectedUrl
            let cls = pageClass page
            yield a [attr.classes ["link-" + cls]; router.HRef page] [text url]
            yield button [
                attr.classes ["btn-" + cls]
                attr.value (router.Link page)
                on.click (fun _ -> dispatch (SetPage page))
            ] [text url]
        yield cond model.page <| fun x ->
            let cls = pageClass x
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
