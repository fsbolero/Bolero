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

open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Routing
open Microsoft.JSInterop
open Elmish
open Bolero
open Bolero.Html
open System.Threading.Tasks
open Microsoft.AspNetCore.Components.Web.Virtualization

type Page =
    | [<EndPoint "/">] Form
    | [<EndPoint "/collection">] Collection
    | [<EndPoint "/collection-item/{key}">] Item of key: int * model: PageModel<int>
    | [<EndPoint "/lazy?{value}&v2={value2}">] Lazy of value: int * value2: string option
    | [<EndPoint "/virtual">] Virtual

type Item =
    {
        K: int
        V: string
    }

type LazyModel =
    {
        value: int
        value2: int
        nonEqVal: string
    }

type Model =
    {
        input: string
        submitted: option<string>
        addKey: int
        revOrder: bool
        items: Map<int, string>
        remoteResult: option<string>
        radioItem: option<int>
        checkbox: bool
        page: Page
        nonLazyValue: int
        lazyModel: LazyModel
        virtualItems: list<string>
    }

type Message =
    | SetInput of text: string
    | Submit
    | RemoveItem of key: int
    | SetAddKey of key: int
    | SetKeyOf of key: int
    | AddKey
    | SetRevOrder of rev: bool
    | SetRadioItem of int
    | SetCheckbox of bool
    | SetPage of Page
    | IncNonLazyVal
    | IncLazyVal
    | SetLazyNonEqVal of string

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
        radioItem = None
        checkbox = false
        page = Form
        remoteResult = None
        nonLazyValue = 0
        lazyModel = { LazyModel.value = 0; value2 = 0; nonEqVal = "I'm not tested in custom equality"; }
        virtualItems = [for i in 0..2000 -> $"Item #{i}"]
    }

let defaultPageModel = function
    | Form | Collection | Lazy _ | Virtual -> ()
    | Item (_, m) -> Router.definePageModel m 10
let router = Router.inferWithModel SetPage (fun m -> m.page) defaultPageModel

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
    | AddKey -> { model with items = Map.add model.addKey $"it's {model.addKey}" model.items }, []
    | SetKeyOf k ->
        match Map.tryFind k model.items with
        | None -> model, []
        | Some item ->
            let items = model.items |> Map.remove k |> Map.add model.addKey item
            { model with items = items }, []
    | SetRevOrder rev -> { model with revOrder = rev }, []
    | SetRadioItem i -> { model with radioItem = Some i }, []
    | SetCheckbox b -> { model with checkbox = b }, []
    | SetPage p -> { model with page = p }, []
    | IncNonLazyVal -> { model with nonLazyValue = model.nonLazyValue + 1 }, []
    | IncLazyVal -> { model with lazyModel = { model.lazyModel with LazyModel.value = model.lazyModel.value + 1; value2 = model.lazyModel.value2 + 1 } }, []
    | SetLazyNonEqVal s -> { model with lazyModel = { model.lazyModel with LazyModel.nonEqVal = s } }, []

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

let btnRef = HtmlRef()

let viewForm (js: IJSRuntime) model dispatch =
    div {
        input { attr.value model.input; on.change (fun e -> dispatch (SetInput (unbox e.Value))) }
        input {
            attr.``type`` "submit"
            on.click (fun _ ->
                js.InvokeAsync("console.log", btnRef.Value) |> ignore
                dispatch Submit
            )
            attr.style (if model.input = "" then "color:gray;" else null)
            btnRef
        }
        div { $"selected radio item: {model.radioItem}" }
        forEach {1..10} <| fun ix ->
            input {
                attr.``type`` "radio"
                attr.name "my-radio-item"
                bind.change.string (string ix) (fun _ -> dispatch (SetRadioItem ix))
            }
        div { defaultArg model.submitted "" }
        div {
            label {
                input {
                    attr.``type`` "checkbox"
                    bind.``checked`` model.checkbox (fun v -> dispatch (SetCheckbox v))
                }
                text "Checkbox (should be in sync with the one below)"
            }
        }
        div {
            label {
                input {
                    attr.``type`` "checkbox"
                    bind.``checked`` model.checkbox (fun v -> dispatch (SetCheckbox v))
                }
                text "Checkbox (should be in sync with the one above)"
            }
        }
        match model.submitted with
        | Some s ->
            concat {
                cond (s.Contains "secret") <| function
                    | true ->
                        SecretPw()
                            .Kind(b { "secret" })
                            .Clear(fun _ -> dispatch (SetInput ""))
                            .DblClick(fun e -> dispatch (SetInput $"({e.ClientX}, {e.ClientY})"))
                            .Input(model.input, fun s -> dispatch (SetInput s))
                            .Value(model.addKey, fun k -> dispatch (SetAddKey k))
                            .Elt()
                    | false -> empty()

                cond (s.Contains "super") <| function
                    | true ->
                        SecretPw()
                            .Kind("super secret")
                            .Clear(fun _ -> dispatch (SetInput ""))
                            .Elt()
                    | false -> empty()
            }
        | None -> empty()
    }

type CollectionTemplate = Template<"collection.html">

type ViewItem() =
    inherit ElmishComponent<int * string, Message>()

    override _.View ((k, v)) dispatch =
        CollectionTemplate.Item()
            .Value(v)
            .SetKey(fun _ -> dispatch (SetKeyOf k))
            .Remove(fun _ -> dispatch (RemoveItem k))
            .Url(router.Link (Item (k, Router.noModel)))
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
            ecomp<ViewItem,_,_> (k, v) dispatch { attr.key k })
        .Elt()

type ViewItemPage() =
    inherit ElmishComponent<int * string * int, Message>()

    [<Inject>]
    member val JS = Unchecked.defaultof<IJSRuntime> with get, set

    override this.View ((k, v, m)) dispatch =
        concat {
            p { "Viewing page for item #" + string k }
            p { "Text is: " + v }
            p { a { router.HRef Collection; "Back to collection" } }
            p {
                "Model: "
                button { on.click (fun _ -> dispatch (SetPage (Item (k, { Model = m - 1 })))); "-" }
                $"{m}"
                button { on.click (fun _ -> dispatch (SetPage (Item (k, { Model = m + 1 })))); "+" }
            }
            script { rawHtml "
                function changeBg(elt) {
                    var bg = elt.style.backgroundColor;
                    elt.style.backgroundColor = bg == 'red' ? 'lightblue' : 'red';
                }" }
            ul {
                for x in 1..3 do
                    let ref = HtmlRef()
                    li {
                        attr.key x
                        attr.style "background-color: lightblue"
                        on.task.click (fun _ ->
                            this.JS.InvokeVoidAsync("changeBg", ref.Value.Value).AsTask())
                        attr.ref ref
                        "Testing key + attr + ref + children in one element"
                    }
            }
        }

let viewLazy i model dispatch =
    let lazyViewFunction = (fun m -> text $"Lazy values: ({m.value},{m.nonEqVal}), re-render random number check: {System.Random().Next()}")
    div {
        p { string i }
        pre {
            text """
let viewLazy model dispatch =
    div [] [
        p [] [button [on.click (fun _ -> dispatch IncNonLazyVal)] [text "Increase non-lazy value"]]
        p [] [button [on.click (fun _ -> dispatch IncLazyVal)] [text "Increase lazy value"]]
        p [] [text $"Non-lazy value: {model.nonLazyValue}, re-render random number check: {System.Random().Next()}"]
        p [] [lazyComp (fun m -> text $"Lazy values: ({m.value},{m.nonEqVal}), re-render random number check: {System.Random().Next()}") model.lazyModel]
    ]
            """
        }
        p {
            button { on.click (fun _ -> dispatch IncNonLazyVal); "Increase non-lazy value" }
            button { on.click (fun _ -> dispatch IncLazyVal); "Increase lazy value" }
            input {
                attr.value model.lazyModel.nonEqVal
                on.change (fun e -> e.Value |> string |> SetLazyNonEqVal |> dispatch)
            }
        }
        p { $"Non-lazy value: {model.nonLazyValue}, re-render random number check: {System.Random().Next()}" }
        p { lazyComp lazyViewFunction model.lazyModel }
        p { lazyCompWith (fun m1 m2 -> m1.value = m2.value) lazyViewFunction model.lazyModel }
        p { lazyCompBy (fun m -> (m.value, m.value2)) lazyViewFunction model.lazyModel }
    }

let viewVirtual model dispatch =
    div {
        attr.style "display:flex; flex-direction:row;"
        div {
            attr.style "flex:1; height: 300px; border: solid 1px black; overflow-y: auto;"
            virtualize.comp {
                let! text = virtualize.items model.virtualItems
                div { attr.style "border: solid 1px gray;"; text }
            }
        }
        div {
            attr.style "flex:1; height: 300px; border: solid 1px black; overflow-y: auto;"
            virtualize.comp {
                virtualize.placeholder <| fun p ->
                    div { attr.style "border: solid 1px gray;"; $"Placeholder #{p.Index}" }
                let! text = virtualize.itemsProvider <| fun r ->
                    ValueTask<ItemsProviderResult<_>>(task {
                        do! Task.Delay 1000
                        return ItemsProviderResult([r.StartIndex..r.StartIndex+r.Count-1], 2000)
                    })
                div { attr.style "border: solid 1px gray;"; $"Item #{text}" }
            }
        }
    }

let view js model dispatch =
    concat {
        rawHtml """
            <div style="color:gray">The links below should have blue background based on the current page.</div>
        """
        p {
            navLink NavLinkMatch.All { router.HRef Form; "Form" }
            text " "
            navLink NavLinkMatch.Prefix { router.HRef Collection; "Collection" }
            text " "
            navLink NavLinkMatch.Prefix { attr.href (router.Link (Lazy (123, Some "abc"))); "Lazy" }
            text " "
            navLink NavLinkMatch.Prefix { attr.href (router.Link (Lazy (123, None))); "Lazy" }
            text " "
            navLink NavLinkMatch.All { router.HRef Virtual; "Virtual" }
        }
        cond model.page <| function
            | Form -> viewForm js model dispatch
            | Collection -> viewCollection model dispatch
            | Item (k, m) -> ecomp<ViewItemPage,_,_> (k, model.items.[k], m.Model) dispatch { attr.empty() }
            | Lazy (x, y) -> viewLazy (x, y) model dispatch
            | Virtual -> viewVirtual model dispatch
    }

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override _.CssScope = CssScopes.CustomScope

    override this.Program =
        Program.mkProgram (fun _ -> initModel(), []) update (view this.JSRuntime)
        //|> Program.withConsoleTrace
        |> Program.withErrorHandler (fun (msg, exn) -> printfn $"{msg}: {exn}")
        |> Program.withRouter router
