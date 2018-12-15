module Bolero.Tests.Web.App.Html

open Bolero
open Bolero.Html
open Microsoft.AspNetCore.Blazor.Routing
open Microsoft.AspNetCore.Blazor.Components

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

    let inputState = ref ""
    let changeState = ref ""
    let inputIntState = ref 0
    let changeIntState = ref 0
    let inputFloatState = ref 0.
    let changeFloatState = ref 0.
    let checkedState = ref false

    override this.Render() =
        concat [
            input [attr.``class`` "bind-input"; bind.input inputState.Value inputState.set_Value]
            span [attr.``class`` "bind-input-out"] [text inputState.Value]
            input [attr.``class`` "bind-change"; bind.change changeState.Value changeState.set_Value]
            span [attr.``class`` "bind-change-out"] [text changeState.Value]
            input [attr.``type`` "number"; attr.``class`` "bind-input-int"; bind.inputInt inputIntState.Value inputIntState.set_Value]
            span [attr.``class`` "bind-input-int-out"] [textf "%i" inputIntState.Value]
            input [attr.``type`` "number"; attr.``class`` "bind-change-int"; bind.changeInt changeIntState.Value changeIntState.set_Value]
            span [attr.``class`` "bind-change-int-out"] [textf "%i" changeIntState.Value]
            input [attr.``type`` "number"; attr.``class`` "bind-input-float"; bind.inputFloat inputFloatState.Value inputFloatState.set_Value]
            span [attr.``class`` "bind-input-float-out"] [textf "%f" inputFloatState.Value]
            input [attr.``type`` "number"; attr.``class`` "bind-change-float"; bind.changeFloat changeFloatState.Value changeFloatState.set_Value]
            span [attr.``class`` "bind-change-float-out"] [textf "%f" changeFloatState.Value]
            input [attr.``type`` "checkbox"; attr.``class`` "bind-checked"; bind.``checked`` checkedState.Value checkedState.set_Value]
            span [attr.``class`` "bind-checked-out"] [textf "%b" checkedState.Value]
        ]

let Tests() =
    div [attr.id "test-fixture-html"] [
        p [attr.id "element-with-id"] [text "Contents of element with id"]
        p [attr.id "element-with-htmlentity"] [text "Escaped <b>text</b> & content"]
        span [
            // The last of all `class` and `classes` sets the attribute.
            attr.classes ["class-notset-1"]
            attr.``class`` "class-notset-2"
            attr.classes ["class-set-1"; "class-set-2"]
        ] []
        RawHtml """<div class="raw-html-element">Unescape &lt;b&gt;text&lt;/b&gt; &amp; content</div>"""
        comp<NavLink> [
            attr.id "nav-link"
            attr.href "/"
            "Match" => NavLinkMatch.Prefix
        ] [text "NavLink content"]
        comp<BoleroComponent> ["Ident" => "bolero-component"] []
        comp<Binds> [] []
    ]
