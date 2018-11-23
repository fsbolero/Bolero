module Bolero.Tests.Web.App.Html

open Bolero
open Bolero.Html
open Microsoft.AspNetCore.Blazor.Routing
open Microsoft.AspNetCore.Blazor.Components

type BoleroComponent() =
    inherit Component()

    [<Parameter>]
    member val Ident = "" with get, set

    override this.Render() =
        div [attr.id this.Ident] [text "Component content"]

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
