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
            input [attr.``class`` "bind-input"; bind.input this.inputState (fun x -> this.inputState <- x)]
            span [attr.``class`` "bind-input-out"] [text this.inputState]
            input [attr.``class`` "bind-change"; bind.change this.changeState (fun x -> this.changeState <- x)]
            span [attr.``class`` "bind-change-out"] [text this.changeState]
            input [attr.``type`` "number"; attr.``class`` "bind-input-int"; bind.inputInt this.inputIntState (fun x -> this.inputIntState <- x)]
            span [attr.``class`` "bind-input-int-out"] [textf "%i" this.inputIntState]
            input [attr.``type`` "number"; attr.``class`` "bind-change-int"; bind.changeInt this.changeIntState (fun x -> this.changeIntState <- x)]
            span [attr.``class`` "bind-change-int-out"] [textf "%i" this.changeIntState]
            input [attr.``type`` "number"; attr.``class`` "bind-input-float"; bind.inputFloat this.inputFloatState (fun x -> this.inputFloatState <- x)]
            span [attr.``class`` "bind-input-float-out"] [textf "%f" this.inputFloatState]
            input [attr.``type`` "number"; attr.``class`` "bind-change-float"; bind.changeFloat this.changeFloatState (fun x -> this.changeFloatState <- x)]
            span [attr.``class`` "bind-change-float-out"] [textf "%f" this.changeFloatState]
            input [attr.``type`` "checkbox"; attr.``class`` "bind-checked"; bind.``checked`` this.checkedState (fun x -> this.checkedState <- x)]
            span [attr.``class`` "bind-checked-out"] [textf "%b" this.checkedState]
            forEach {1..10} <| fun v ->
                input [
                    attr.``type`` "radio"
                    attr.name "bind-radio"
                    attr.``class`` ("bind-radio-" + string v)
                    bind.change (string v) (fun _ -> this.radioState <- v)
                ]
            span [attr.``class`` "bind-radio-out"] [textf "%i" this.radioState]
        ]

type BindElementRef() =
    inherit Component()

    let mutable elt1 = Unchecked.defaultof<ElementReference>
    let elt2 = ElementReferenceBinder()

    [<Inject>]
    member val JSRuntime = Unchecked.defaultof<IJSRuntime> with get, set

    override this.Render() =
        concat [
            button [
                attr.``class`` "element-ref"
                attr.ref (fun r -> elt1 <- r)
                on.eventAsync "click" (fun _ ->
                    this.JSRuntime.InvokeAsync("setContent", elt1, "ElementRef 1 is bound").AsTask() :> _
                )
            ] [text "Click me"]
            button [
                attr.``class`` "element-ref-binder"
                attr.bindRef elt2
                on.asyncEvent "click" (fun _ -> async {
                    do! this.JSRuntime.InvokeAsync("setContent", elt2.Ref, "ElementRef 2 is bound").AsTask() |> Async.AwaitTask
                })
            ] [text "Click me"]
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
    ]
