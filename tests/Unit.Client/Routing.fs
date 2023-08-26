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

module Bolero.Tests.Client.Routing

open Bolero
open Bolero.Html
open Elmish

type Page =
    | [<EndPoint "/">] Home
    | [<EndPoint "/not-found">] NotFound
    | [<EndPoint "/with-anchor">] WithAnchor
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
    | [<EndPoint "/with-rest-string/{*rest}">] WithRestString of rest: string
    | [<EndPoint "/with-rest-list/{*rest}">] WithRestList of rest: list<int>
    | [<EndPoint "/with-rest-array/{*rest}">] WithRestArray of rest: (int * string)[]
    | [<EndPoint "/with-model">] WithModel of PageModel<int>
    | [<EndPoint "/with-model-args/{arg}">] WithModelAndArgs of arg: int * PageModel<string>
    | [<EndPoint "/with-query/{arg}?named={n}&{implicit}&{optional}&{voptional}">] WithQuery of arg: int * n: int * implicit: int * optional: int option * voptional: int voption

and InnerPage =
    | [<EndPoint "/">] InnerHome
    | [<EndPoint "/no-arg">] InnerNoArg
    | [<EndPoint "/with-arg">] InnerWithArg of string
    | [<EndPoint "/with-args">] InnerWithArgs of string * int

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
    try
        Router.infer SetPage (fun m -> m.page)
        |> Router.withNotFound NotFound
    with e ->
        eprintfn $"ROUTER ERROR: {e}"
        reraise()

let innerPageClass = function
    | InnerHome -> "home"
    | InnerNoArg -> "noarg"
    | InnerWithArg x -> $"witharg-{x}"
    | InnerWithArgs(x, y) -> $"withargs-{x}-{y}"

let rec pageClass = function
    | Home -> "home"
    | NotFound -> "notfound"
    | WithAnchor -> "withanchor"
    | NoArg -> "noarg"
    | WithArg x -> $"witharg-{x}"
    | WithArgs(x, y) -> $"withargs-{x}-{y}"
    | WithUnion u -> $"withunion-{innerPageClass u}"
    | WithUnionNotTerminal(u, s) -> $"withunion2-{innerPageClass u}-{s}"
    | WithNestedUnion u -> $"withnested-{pageClass u}"
    | WithTuple(x, y, z) -> $"withtuple-{x}-{y}-{z}"
    | WithRecord { x = x; y = y; z = z } -> $"withrecord-{x}-{innerPageClass y}-{z}"
    | WithList l -> $"""withlist-{String.concat "-" [for i, s in l -> $"{i}-{s}"]}"""
    | WithArray a -> $"""witharray-{String.concat "-" [for i, s in a -> $"{i}-{s}"]}"""
    | WithPath s -> $"withpath-{s}"
    | WithPathAndSuffix s -> $"withpathsuffix-{s}"
    | WithPathAndSuffix2(s, i) -> $"withpathsuffix2-{s}-{i}"
    | WithPathAndSuffix3 s -> $"withpathsuffix3-{s}"
    | WithPathConstant -> "withpathconstant"
    | WithPathRecord { x = x; y = y; z = z } -> $"withpathrecord-{x}-{innerPageClass y}-{z}"
    | WithRestString s -> $"""withreststring-{s.Replace("/", "-")}"""
    | WithRestList l -> $"""withrestlist-{l |> Seq.map string |> String.concat "-"}"""
    | WithRestArray a -> $"""withrestarray-{a |> Seq.map (fun (i, s) -> $"{i}-{s}") |> String.concat "-"}"""
    | WithModel _ -> "withmodel"
    | WithModelAndArgs (a, _) -> $"withmodelargs-{a}"
    | WithQuery(a, b, c, d, e) -> $"withquery-{a}-{b}-{c}-{d}-{e}"

let innerlinks =
    [
        "/",                InnerHome
        "/no-arg",          InnerNoArg
        "/with-arg/foo",    InnerWithArg "foo"
        "/with-arg/bar",    InnerWithArg "bar"
        "/with-arg/",       InnerWithArg ""
        "/with-args/foo/1", InnerWithArgs("foo", 1)
        "/with-args/bar/2", InnerWithArgs("bar", 2)
        "/with-args//3",    InnerWithArgs("", 3)
    ]

let baseLinks =
    [
        yield! [
            "/",                                Home
            "/no-arg",                          NoArg
            "/with-arg/foo",                    WithArg "foo"
            "/with-arg/bar",                    WithArg "bar"
            "/with-arg/%E6%97%A5%E6%9C%AC%E8%AA%9E", WithArg "日本語"
            "/with-arg/",                       WithArg ""
            "/with-args/foo/1",                 WithArgs("foo", 1)
            "/with-args/bar/2",                 WithArgs("bar", 2)
            "/with-args//3",                    WithArgs("", 3)
            "/with-tuple/42/hi/true",           WithTuple(42, "hi", true)
            "/with-tuple/324//false",           WithTuple(324, "", false)
            "/with-list/2/2/a/34/b",            WithList [2, "a"; 34, "b"]
            "/with-list/2/2//34/b",             WithList [2, ""; 34, "b"]
            "/with-array/2/2/a/34/b",           WithArray [|2, "a"; 34, "b"|]
            "/with-array/2/2//34/b",            WithArray [|2, ""; 34, "b"|]
            "/with-path/abc",                   WithPath "abc"
            "/with-path/",                      WithPath ""
            "/with-path/abc/and-suffix",        WithPathAndSuffix "abc"
            "/with-path//and-suffix",           WithPathAndSuffix ""
            "/with-path/abc/and-suffix/123",    WithPathAndSuffix2("abc", 123)
            "/with-path//and-suffix/123",       WithPathAndSuffix2("", 123)
            "/with-path/abc/other-suffix",      WithPathAndSuffix3 "abc"
            "/with-path//other-suffix",         WithPathAndSuffix3 ""
            "/with-path/and/constant",          WithPathConstant
            "/with-rest-string",                WithRestString ""
            "/with-rest-string/foo",            WithRestString "foo"
            "/with-rest-string/foo/bar",        WithRestString "foo/bar"
            "/with-rest-list",                  WithRestList []
            "/with-rest-list/42",               WithRestList [42]
            "/with-rest-list/12/34/56",         WithRestList [12; 34; 56]
            "/with-rest-array",                 WithRestArray [||]
            "/with-rest-array/1/foo",           WithRestArray [|(1, "foo")|]
            "/with-rest-array/1/foo/2/bar",     WithRestArray [|(1, "foo"); (2, "bar")|]
            "/with-model",                      WithModel { Model = Unchecked.defaultof<_> }
            "/with-model-args/42",              WithModelAndArgs(42, { Model = Unchecked.defaultof<_> })
            "/with-query/42?implicit=2&named=1&optional=3", WithQuery(42, 1, 2, Some 3, ValueNone)
            "/with-query/42?implicit=5&named=4&voptional=3", WithQuery(42, 4, 5, None, ValueSome 3)
        ]
        for link, page in innerlinks do
            yield "/with-union" + link,                     WithUnion page
            yield "/with-union2" + link + "/foo",           WithUnionNotTerminal (page, "foo")
            yield "/with-union2" + link + "/",              WithUnionNotTerminal (page, "")
            yield "/with-record/1" + link + "/true",        WithRecord { x = 1; y = page; z = true }
            yield "/with-path-record/3" + link + "/false",  WithPathRecord { x = 3; y = page; z = false }
    ]

let links =
    baseLinks @
    List.map (fun (l, p) -> "/with-nested-union" + l, WithNestedUnion p) baseLinks

let view model dispatch =
    concat {
        for url, page in links do
            let cls = pageClass page
            a { attr.``class`` $"link-{cls}"; router.HRef page; url }
            button {
                attr.``class`` $"btn-{cls}"
                attr.value (router.Link page)
                on.click (fun _ -> dispatch (SetPage page))
                url
            }
        a {
            attr.``class`` "link-notfound"
            attr.href "/invalid-url"
            "/not-found"
        }
        span { attr.``class`` "current-page"; $"{model.page}" }
        div {
            a {
                attr.``class`` "link-to-anchor"
                router.HRef(WithAnchor, "the-anchor")
                "go to anchor"
            }
        }
        cond model.page <| function
            | WithAnchor ->
                concat {
                    div { attr.style "height: 3000px" }
                    div {
                        attr.id "the-anchor"
                        attr.style "height: 3000px"
                        "the anchor"
                    }
                }
            | _ -> empty()
    }

type Test() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        Program.mkSimple (fun _ -> initModel) update view
        |> Program.withRouter router

let Tests() =
    div {
        attr.id "test-fixture-routing"
        comp<Test>
    }
