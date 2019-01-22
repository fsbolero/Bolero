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

module Bolero.Test.Client.Main

open Microsoft.AspNetCore.Blazor.Routing
open Microsoft.JSInterop
open Elmish
open Bolero
open Bolero.Html
open System.Net.Http

type Page =
    | [<EndPoint "/">] Form
    | [<EndPoint "/collection">] Collection
    | [<EndPoint "/collection-item/{key}">] Item of key: int

type Item =
    {
        K: int
        V: string
    }

type Model =
    {
        input: string
        submitted: option<string>
        addKey: int
        revOrder: bool
        items: Map<int, string>
        remoteResult: option<string>
        page: Page
    }

type Message =
    | SetInput of text: string
    | Submit
    | RemoveItem of key: int
    | SetAddKey of key: int
    | SetKeyOf of key: int
    | AddKey
    | SetRevOrder of rev: bool
    | SetPage of Page

let initModel _ =
    {
        input = ""
        submitted = None
        addKey = 4
        revOrder = false
        items = Map [
            0, "it's 0"
            1, "it's 1"
            2, "it's 2"
            3, "it's 3"
        ]
        page = Form
        remoteResult = None
    }

let router = Router.infer SetPage (fun m -> m.page)

type MyRemoting =
    {
        greet: string -> Async<string>
    }

let update message model =
    match message with
    | SetInput text -> { model with input = text }, []
    | Submit -> { model with submitted = Some model.input }, []
    | RemoveItem k -> { model with items = Map.filter (fun k' _ -> k' <> k) model.items }, []
    | SetAddKey i -> { model with addKey = i }, []
    | AddKey -> { model with items = Map.add model.addKey (sprintf "it's %i" model.addKey) model.items }, []
    | SetKeyOf k ->
        match Map.tryFind k model.items with
        | None -> model, []
        | Some item ->
            let items = model.items |> Map.remove k |> Map.add model.addKey item
            { model with items = items }, []
    | SetRevOrder rev -> { model with revOrder = rev }, []
    | SetPage p -> { model with page = p }, []

// ondblclick's handler uses UIMouseEventArgs properties to check that we do generate specific UI*EventArgs.
// ondblclick isn't handled in the "super" case to check that we correctly generate no-op when an event hole is unfilled.
// onclick and onkeypress point to the same event to check that different UI*EventArgs are merged as UIEventArgs. -->
type SecretPw = Template<"""<div>
                                You typed the ${Kind} <i>pass<span>word</span></i>&excl;
                                <!-- Testing a comment -->
                                <button onclick="${Clear}" onkeypress="${Clear}" ondblclick="${DblClick}">Clear</button>
                                <input value="(default value)" bind="${Input}" /> <- You typed: ${Input}
                                <input type="number" bind="${Value}" />
                            </div>""">

let btnRef = ElementRefBinder()

let viewForm model dispatch =
    div [] [
        input [attr.value model.input; on.change (fun e -> dispatch (SetInput (unbox e.Value)))]
        input [
            attr.bindRef btnRef
            attr.``type`` "submit"
            on.click (fun _ ->
                JSRuntime.Current.InvokeAsync("console.log", btnRef.Ref) |> ignore
                dispatch Submit
            )
            attr.style (if model.input = "" then "color:gray;" else null)
        ]
        div [] [text (defaultArg model.submitted "")]
        (match model.submitted with
        | Some s ->
            concat [
                cond (s.Contains "secret") <| function
                    | true ->
                        SecretPw()
                            .Kind(b [] [text "secret"])
                            .Clear(fun _ -> dispatch (SetInput ""))
                            .DblClick(fun e -> dispatch (SetInput (sprintf "(%i, %i)" e.ClientX e.ClientY)))
                            .Input(model.input, fun s -> dispatch (SetInput s))
                            .Value(model.addKey, fun k -> dispatch (SetAddKey k))
                            .Elt()
                    | false -> empty

                cond (s.Contains "super") <| function
                    | true ->
                        SecretPw()
                            .Kind("super secret")
                            .Clear(fun _ -> dispatch (SetInput ""))
                            .Elt()
                    | false -> empty
            ]
        | None -> empty)
    ]

type CollectionTemplate = Template<"collection.html">

type ViewItem() =
    inherit ElmishComponent<int * string, Message>()

    override this.View ((k, v)) dispatch =
        CollectionTemplate.Item()
            .Value(v)
            .SetKey(fun _ -> dispatch (SetKeyOf k))
            .Remove(fun _ -> dispatch (RemoveItem k))
            .Url(router.Link (Item k))
            .Elt()

let viewCollection model dispatch =
    let items =
        if model.revOrder then
            Seq.rev model.items
        else
            model.items :> _
    CollectionTemplate()
        .AddKeyValue(model.addKey, fun i -> dispatch (SetAddKey i))
        .AddKey(fun _ -> dispatch AddKey)
        .RevOrder(model.revOrder, fun rev -> dispatch (SetRevOrder rev))
        .Items(forEach items <| fun (KeyValue(k, v)) ->
            ecomp<ViewItem,_,_> (k, v) dispatch)
        .Elt()

type ViewItemPage() =
    inherit ElmishComponent<int * string, Message>()

    override this.View ((k, v)) dispatch =
        concat [
            p [] [text ("Viewing page for item #" + string k)]
            p [] [text ("Text is: " + v)]
            p [] [a [router.HRef Collection] [text "Back to collection"]]
        ]

let view model dispatch =
    concat [
        RawHtml """
            <div style="color:gray">The links below should have blue background based on the current page.</div>
            <style>.active { background: lightblue; }</style>
        """
        p [] [
            navLink NavLinkMatch.All [router.HRef Form] [text "Form"]
            text " "
            navLink NavLinkMatch.Prefix [router.HRef Collection] [text "Collection"]
        ]
        cond model.page <| function
            | Form -> viewForm model dispatch
            | Collection -> viewCollection model dispatch
            | Item k -> ecomp<ViewItemPage,_,_> (k, model.items.[k]) dispatch
    ]

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        Program.mkProgram (fun _ -> initModel(), []) update view
        |> Program.withConsoleTrace
        |> Program.withErrorHandler (fun (msg, exn) -> printfn "%s: %A" msg exn)
        |> Program.withRouter router
