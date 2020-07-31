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
        concat [
            div [attr.id this.Ident] [text "Component content"]

            input [
                attr.classes ["condBoolInput"]
                attr.value condBoolState
                on.input (fun e -> condBoolState <- e.Value :?> string)
            ]
            cond (condBoolState.Length = 2) <| function
                | true -> span [attr.classes ["condBoolIs2"]] []
                | false -> span [attr.classes ["condBoolIsNot2"]] []

            input [
                attr.classes ["condUnionInput"]
                attr.value (string condUnionState)
                on.input (fun e ->
                    let s = e.Value :?> string
                    condUnionState <-
                        match s.Length with
                        | 0 -> Empty
                        | 1 -> OneChar s.[0]
                        | _ -> ManyChars s)
            ]
            cond condUnionState <| function
                | Empty -> span [attr.classes ["condUnionIsEmpty"]] []
                | OneChar _ -> span [attr.classes ["condUnionIsOne"]] []
                | ManyChars _ -> span [attr.classes ["condUnionIsMany"]] []

            input [
                attr.classes ["forEachInput"]
                attr.value (String.concat "" forEachState)
                on.input (fun e ->
                    forEachState <- [for c in (e.Value :?> string) -> string c])
            ]
            forEach forEachState <| fun s ->
                span [attr.classes ["forEachIs" + s]] []
        ]

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
        concat [
            input [attr.``class`` "bind-input"; bind.input.string this.inputState (fun x -> this.inputState <- x)]
            input [attr.``class`` "bind-input-2"; bind.input.string this.inputState (fun x -> this.inputState <- x)]
            span [attr.``class`` "bind-input-out"] [text this.inputState]
            input [attr.``class`` "bind-change"; bind.change.string this.changeState (fun x -> this.changeState <- x)]
            input [attr.``class`` "bind-change-2"; bind.change.string this.changeState (fun x -> this.changeState <- x)]
            span [attr.``class`` "bind-change-out"] [text this.changeState]
            input [attr.``type`` "number"; attr.``class`` "bind-input-int"; bind.input.int this.inputIntState (fun x -> this.inputIntState <- x)]
            input [attr.``type`` "number"; attr.``class`` "bind-input-int-2"; bind.input.int this.inputIntState (fun x -> this.inputIntState <- x)]
            span [attr.``class`` "bind-input-int-out"] [textf "%i" this.inputIntState]
            input [attr.``type`` "number"; attr.``class`` "bind-change-int"; bind.change.int this.changeIntState (fun x -> this.changeIntState <- x)]
            input [attr.``type`` "number"; attr.``class`` "bind-change-int-2"; bind.change.int this.changeIntState (fun x -> this.changeIntState <- x)]
            span [attr.``class`` "bind-change-int-out"] [textf "%i" this.changeIntState]
            input [attr.``type`` "number"; attr.``class`` "bind-input-float"; bind.input.float this.inputFloatState (fun x -> this.inputFloatState <- x)]
            input [attr.``type`` "number"; attr.``class`` "bind-input-float-2"; bind.input.float this.inputFloatState (fun x -> this.inputFloatState <- x)]
            span [attr.``class`` "bind-input-float-out"] [textf "%f" this.inputFloatState]
            input [attr.``type`` "number"; attr.``class`` "bind-change-float"; bind.change.float this.changeFloatState (fun x -> this.changeFloatState <- x)]
            input [attr.``type`` "number"; attr.``class`` "bind-change-float-2"; bind.change.float this.changeFloatState (fun x -> this.changeFloatState <- x)]
            span [attr.``class`` "bind-change-float-out"] [textf "%f" this.changeFloatState]
            input [attr.``type`` "checkbox"; attr.``class`` "bind-checked"; bind.``checked`` this.checkedState (fun x -> this.checkedState <- x)]
            input [attr.``type`` "checkbox"; attr.``class`` "bind-checked-2"; bind.``checked`` this.checkedState (fun x -> this.checkedState <- x)]
            span [attr.``class`` "bind-checked-out"] [textf "%b" this.checkedState]
            forEach {1..10} <| fun v ->
                input [
                    attr.``type`` "radio"
                    attr.name "bind-radio"
                    attr.``class`` ("bind-radio-" + string v)
                    bind.change.string (string v) (fun _ -> this.radioState <- v)
                ]
            input [attr.``class`` "bind-radio-0"; bind.input.int this.radioState (fun x -> this.radioState <- x)]
            span [attr.``class`` "bind-radio-out"] [textf "%i" this.radioState]
        ]

type BindElementRef() =
    inherit Component()

    let elt = HtmlRef()

    [<Inject>]
    member val JSRuntime = Unchecked.defaultof<IJSRuntime> with get, set

    override this.Render() =
        button [
            attr.``class`` "element-ref"
            attr.ref elt
            on.task.event "click" (fun _ ->
                this.JSRuntime.InvokeVoidAsync("setContent", elt.Value, "ElementRef is bound").AsTask())
        ] [text "Click me"]

type BindComponentRef() =
    inherit Component()

    let cmp = Ref<NavLink>()
    let mutable txt = "Click me"

    override _.Render() =
        concat [
            navLink NavLinkMatch.All [attr.ref cmp; "ActiveClass" => "component-ref-is-bound"] []
            button [
                attr.``class`` "component-ref"
                on.event "click" (fun _ -> txt <- cmp.Value.ActiveClass)
            ] [text txt]
        ]

let Tests() =
    div [attr.id "test-fixture-html"] [
        p [attr.id "element-with-id"] [text "Contents of element with id"]
        p [attr.id "element-with-htmlentity"] [text "Escaped <b>text</b> & content"]
        concat [
            span [
                attr.classes ["class-set-1"]
                attr.``class`` "class-set-2"
                attr.classes ["class-set-3"; "class-set-4"]
            ] []
            span [
                attr.classes ["class-set-5"]
                attr.``class`` "class-set-6"
            ] []
            span [attr.``class`` "class-set-7"] []
        ]
        RawHtml """<div class="raw-html-element">Unescape &lt;b&gt;text&lt;/b&gt; &amp; content</div>"""
        comp<NavLink> [
            attr.id "nav-link"
            attr.href "/"
            "Match" => NavLinkMatch.Prefix
        ] [text "NavLink content"]
        comp<BoleroComponent> ["Ident" => "bolero-component"] []
        comp<Binds> [] []
        comp<BindElementRef> [] []
        comp<BindComponentRef> [] []
    ]
