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
    ]
