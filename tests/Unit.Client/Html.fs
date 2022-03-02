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

module Bolero.Tests.Client.Html

open Bolero
open Bolero.Html
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Routing
open Microsoft.JSInterop
open System.Threading.Tasks

type SomeUnion =
    | Empty
    | OneChar of char
    | ManyChars of string

    override this.ToString() =
        match this with
        | Empty -> ""
        | OneChar c -> string c
        | ManyChars s -> s

type BoleroComponent() =
    inherit Component()

    let mutable condBoolState = ""

    let mutable condUnionState = Empty

    let mutable forEachState = []

    [<Parameter>]
    member val Ident = "" with get, set

    override this.Render() =
        concat {
            div { attr.id this.Ident; "Component content" }

            input {
                attr.``class`` "condBoolInput"
                attr.value condBoolState
                on.input (fun e -> condBoolState <- e.Value :?> string)
            }
            cond (condBoolState.Length = 2) <| function
                | true -> span { attr.``class`` "condBoolIs2" }
                | false -> span { attr.``class`` "condBoolIsNot2" }

            input {
                attr.``class`` "condUnionInput"
                attr.value (string condUnionState)
                on.input (fun e ->
                    let s = e.Value :?> string
                    condUnionState <-
                        match s.Length with
                        | 0 -> Empty
                        | 1 -> OneChar s.[0]
                        | _ -> ManyChars s)
            }
            cond condUnionState <| function
                | Empty -> span { attr.``class`` "condUnionIsEmpty" }
                | OneChar _ -> span { attr.``class`` "condUnionIsOne" }
                | ManyChars _ -> span { attr.``class`` "condUnionIsMany" }

            input {
                attr.``class`` "forEachInput"
                attr.value (String.concat "" forEachState)
                on.input (fun e ->
                    forEachState <- [for c in (e.Value :?> string) -> string c])
            }
            forEach forEachState <| fun s ->
                span { attr.``class`` ("forEachIs" + s) }
            for s in forEachState do
                span { attr.``class`` ("forLoopIs" + s) }
        }

type Binds() =
    inherit Component()

    [<Parameter>]
    member val inputState = "" with get, set
    [<Parameter>]
    member val changeState = "" with get, set
    [<Parameter>]
    member val inputIntState = 0 with get, set
    [<Parameter>]
    member val changeIntState = 0 with get, set
    [<Parameter>]
    member val inputFloatState = 0. with get, set
    [<Parameter>]
    member val changeFloatState = 0. with get, set
    [<Parameter>]
    member val checkedState = false with get, set
    [<Parameter>]
    member val radioState = 0 with get, set

    override this.Render() =
        concat {
            input { attr.``class`` "bind-input"; bind.input.string this.inputState (fun x -> this.inputState <- x) }
            input { attr.``class`` "bind-input-2"; bind.input.string this.inputState (fun x -> this.inputState <- x) }
            span { attr.``class`` "bind-input-out"; this.inputState }
            input { attr.``class`` "bind-change"; bind.change.string this.changeState (fun x -> this.changeState <- x) }
            input { attr.``class`` "bind-change-2"; bind.change.string this.changeState (fun x -> this.changeState <- x) }
            span { attr.``class`` "bind-change-out"; this.changeState }
            input { attr.``type`` "number"; attr.``class`` "bind-input-int"; bind.input.int this.inputIntState (fun x -> this.inputIntState <- x) }
            input { attr.``type`` "number"; attr.``class`` "bind-input-int-2"; bind.input.int this.inputIntState (fun x -> this.inputIntState <- x) }
            span { attr.``class`` "bind-input-int-out"; $"{this.inputIntState}" }
            input { attr.``type`` "number"; attr.``class`` "bind-change-int"; bind.change.int this.changeIntState (fun x -> this.changeIntState <- x) }
            input { attr.``type`` "number"; attr.``class`` "bind-change-int-2"; bind.change.int this.changeIntState (fun x -> this.changeIntState <- x) }
            span { attr.``class`` "bind-change-int-out"; $"{this.changeIntState}" }
            input { attr.``type`` "number"; attr.``class`` "bind-input-float"; bind.input.float this.inputFloatState (fun x -> this.inputFloatState <- x) }
            input { attr.``type`` "number"; attr.``class`` "bind-input-float-2"; bind.input.float this.inputFloatState (fun x -> this.inputFloatState <- x) }
            span { attr.``class`` "bind-input-float-out"; $"{this.inputFloatState}" }
            input { attr.``type`` "number"; attr.``class`` "bind-change-float"; bind.change.float this.changeFloatState (fun x -> this.changeFloatState <- x) }
            input { attr.``type`` "number"; attr.``class`` "bind-change-float-2"; bind.change.float this.changeFloatState (fun x -> this.changeFloatState <- x) }
            span { attr.``class`` "bind-change-float-out"; $"{this.changeFloatState}" }
            input { attr.``type`` "checkbox"; attr.``class`` "bind-checked"; bind.``checked`` this.checkedState (fun x -> this.checkedState <- x) }
            input { attr.``type`` "checkbox"; attr.``class`` "bind-checked-2"; bind.``checked`` this.checkedState (fun x -> this.checkedState <- x) }
            span { attr.``class`` "bind-checked-out"; $"%b{this.checkedState}" }
            forEach {1..10} <| fun v ->
                input {
                    attr.``type`` "radio"
                    attr.name "bind-radio"
                    attr.``class`` ("bind-radio-" + string v)
                    bind.change.string (string v) (fun _ -> this.radioState <- v)
                }
            input { attr.``class`` "bind-radio-0"; bind.input.int this.radioState (fun x -> this.radioState <- x) }
            span {attr.``class`` "bind-radio-out"; $"{this.radioState}" }
        }

type BindElementRef() =
    inherit Component()

    let elt = HtmlRef()

    [<Inject>]
    member val JSRuntime = Unchecked.defaultof<IJSRuntime> with get, set

    override this.Render() =
        button {
            attr.``class`` "element-ref"
            on.task.event "click" (fun _ ->
                match elt.Value with
                | Some elt -> this.JSRuntime.InvokeVoidAsync("setContent", elt, "ElementRef is bound").AsTask()
                | None -> Task.CompletedTask)
            elt
            "Click me"
        }

type BindComponentRef() =
    inherit Component()

    let cmp = Ref<NavLink>()
    let mutable txt = "Click me"

    override _.Render() =
        concat {
            navLink NavLinkMatch.All {
                 "ActiveClass" => "component-ref-is-bound"
                 attr.``class`` "nav-link"
                 cmp
                 "Home"
            }
            button {
                attr.``class`` "component-ref"
                on.event "click" (fun _ -> txt <- match cmp.Value with Some c -> c.ActiveClass | None -> "Component ref is unbound")
                txt
            }
        }

type SimpleComponent() =
    inherit Component()

    [<Parameter>]
    member val CompClass = "" with get, set

    [<Parameter>]
    member val SuccessText = "" with get, set

    [<Parameter>]
    member val ChildContent = Unchecked.defaultof<RenderFragment> with get, set

    override this.Render() =
        li { attr.``class`` this.CompClass; this.ChildContent }

type ComponentChildContent() =
    inherit Component()

    override _.Render() =
        comp<SimpleComponent> {
            "CompClass" => "comp-child-content"
            span {
                attr.``class`` "comp-child-elt"
                "comp-child-text-1"
            }
            "comp-child-text-2"
        }

type BindKeyAndRef() =
    inherit Component()

    let htmlRef = HtmlRef()
    let mutable htmlText = "elt-keyref is not bound"
    let compRef = Ref<SimpleComponent>()
    let mutable compText = "comp-keyref is not bound"

    [<Inject>]
    member val JSRuntime = Unchecked.defaultof<IJSRuntime> with get, set

    override this.Render() =
        ul {
            li {
                attr.``class`` "elt-keyref1"
                attr.key "elt-keyref1"
                button {
                    attr.``class`` "elt-keyref-btn"
                    on.task.click (fun _ ->
                        match htmlRef.Value with
                        | Some elt -> this.JSRuntime.InvokeVoidAsync("setContent", elt, "elt-keyref is bound").AsTask()
                        | None -> Task.CompletedTask)
                }
            }
            li {
                attr.``class`` "elt-keyref2"
                attr.keyAndRef "elt-keyref2" htmlRef
                htmlText
            }
            comp<SimpleComponent> {
                "CompClass" => "comp-keyref1"
                attr.key "comp-keyref1"
                button {
                    attr.``class`` "comp-keyref-btn"
                    on.click (fun _ ->
                        compText <-
                            match compRef.Value with
                            | Some c -> c.SuccessText
                            | None -> "comp-keyref is unbound")
                }
            }
            comp<SimpleComponent> {
                "CompClass" => "comp-keyref2"
                "SuccessText" => "comp-keyref is bound"
                attr.keyAndRef "comp-keyref2" compRef
                compText
            }
        }

let Tests() =
    div {
        attr.id "test-fixture-html"
        p { attr.id "element-with-id"; "Contents of element with id" }
        p { attr.id "element-with-htmlentity"; "Escaped <b>text</b> & content" }
        rawHtml """<div class="raw-html-element">Unescape &lt;b&gt;text&lt;/b&gt; &amp; content</div>"""
        comp<NavLink> {
            attr.id "nav-link"
            attr.href "/"
            "Match" => NavLinkMatch.Prefix
            "NavLink content"
        }
        comp<BoleroComponent> { "Ident" => "bolero-component" }
        comp<Binds>
        comp<BindElementRef>
        comp<BindComponentRef>
        comp<ComponentChildContent>
        comp<BindKeyAndRef>
    }
