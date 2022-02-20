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

/// Create HTML elements, attributes and event handlers.
/// [category: HTML]
module Bolero.Html

open System.Threading.Tasks
open System.Globalization

open System
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web

/// Create an empty HTML fragment.
/// [category: HTML elements]
let inline empty() = Node.Empty()

/// Create an HTML text node.
/// [category: HTML elements]
let inline text str = Node.Text str

/// Create a raw HTML node.
/// [category: HTML elements]
let inline rawHtml str = Node.RawHtml str

/// Create an HTML text node using formatting.
/// [category: HTML elements]
let inline textf format = Printf.kprintf text format

/// Create an HTML element.
/// [category: HTML elements]
let inline elt name = ElementBuilder name

/// Create an HTML attribute.
/// [category: HTML elements]
let inline (=>) name value = Attr.Make name value

/// Create a conditional fragment. `matching` must be either a boolean or an F# union.
/// If it's a union, `mkNode` must only match on the case.
/// [category: HTML elements]
let inline cond<'T> (matching: 'T) (mkNode: 'T -> Node) =
    if typeof<'T> = typeof<bool> then
        Node.Cond (unbox<bool> matching) (mkNode matching)
    else
        Node.Match matching (mkNode matching)

/// Create a fragment that concatenates nodes for each item in a sequence.
/// [category: HTML elements]
let inline forEach<'T> (items: seq<'T>) (mkNode: 'T -> Node) =
    Node.ForEach items mkNode

/// Create a node from a Blazor RenderFragment.
let inline fragment (frag: RenderFragment) =
    Node.Fragment frag

/// Create a fragment from a Blazor component.
/// [category: Components]
let inline comp<'T when 'T :> IComponent> = ComponentBuilder<'T>()

/// Create a fragment from an Elmish component.
/// [category: Components]
let inline ecomp<'T, 'model, 'msg when 'T :> ElmishComponent<'model, 'msg>>
        (model: 'model) (dispatch: Elmish.Dispatch<'msg>) =
    ComponentWithAttrsBuilder<'T>(Html.attrs {
        "Model" => model
        "Dispatch" => dispatch
    })

/// Create a fragment with a lazily rendered view function.
/// [category: Components]
let inline lazyComp ([<InlineIfLambda>] viewFunction: 'model -> Node) (model: 'model) =
    let viewFunction' : 'model -> Elmish.Dispatch<_> -> Node = fun m _ -> viewFunction m
    comp<LazyComponent<'model, obj>> {
        "Model" => model
        "ViewFunction" => viewFunction'
    }

/// Create a fragment with a lazily rendered view function and a custom equality.
/// [category: Components]
let inline lazyCompWith (equal: 'model -> 'model -> bool) ([<InlineIfLambda>] viewFunction: 'model -> Node) (model: 'model) =
    let viewFunction' : 'model -> Elmish.Dispatch<_> -> Node = fun m _ -> viewFunction m
    comp<LazyComponent<'model, obj>> {
        "Model" => model
        "ViewFunction" => viewFunction'
        "Equal" => equal
    }

/// Create a fragment with a lazily rendered view function.
/// [category: Components]
let inline lazyComp2 (viewFunction: 'model -> Elmish.Dispatch<'msg> -> Node) (model: 'model) (dispatch: Elmish.Dispatch<'msg>) =
    comp<LazyComponent<'model, 'msg>> {
        "Model" => model
        "Dispatch" => dispatch
        "ViewFunction" => viewFunction
    }

/// Create a fragment with a lazily rendered view function and a custom equality.
/// [category: Components]
let inline lazyComp2With (equal: 'model -> 'model -> bool) (viewFunction: 'model -> Elmish.Dispatch<'msg> -> Node) (model: 'model) (dispatch: Elmish.Dispatch<'msg>) =
    comp<LazyComponent<'model, 'msg>> {
        "Model" => model
        "Dispatch" => dispatch
        "ViewFunction" => viewFunction
        "Equal" => equal
    }

/// Create a fragment with a lazily rendered view function.
/// [category: Components]
let inline lazyComp3 (viewFunction: ('model1 * 'model2') -> Elmish.Dispatch<'msg> -> Node) (model1: 'model1) (model2: 'model2) (dispatch: Elmish.Dispatch<'msg>) =
    comp<LazyComponent<'model1 * 'model2, 'msg>>{
        "Model" => (model1, model2)
        "Dispatch" => dispatch
        "ViewFunction" => viewFunction
    }

/// Create a fragment with a lazily rendered view function and a custom equality.
/// [category: Components]
let inline lazyComp3With (equal: ('model1 * 'model2) -> ('model1 * 'model2) -> bool) (viewFunction: ('model1 * 'model2') -> Elmish.Dispatch<'msg> -> Node) (model1: 'model1) (model2: 'model2) (dispatch: Elmish.Dispatch<'msg>) =
    comp<LazyComponent<'model1 * 'model2, 'msg>> {
        "Model" => (model1, model2)
        "Dispatch" => dispatch
        "ViewFunction" => viewFunction
        "Equal" => equal
    }

/// Create a fragment with a lazily rendered view function and custom equality on model field.
/// [category: Components]
let inline lazyCompBy (equal: 'model -> 'a) (viewFunction: 'model -> Node) (model: 'model) =
    let equal' model1 model2 = (equal model1) = (equal model2)
    lazyCompWith equal' viewFunction model

/// Create a fragment with a lazily rendered view function and custom equality on model field.
/// [category: Components]
let inline lazyComp2By (equal: 'model -> 'a) (viewFunction: 'model -> Elmish.Dispatch<'msg> -> Node) (model: 'model) (dispatch: Elmish.Dispatch<'msg>) =
    let equal' model1 model2 = (equal model1) = (equal model2)
    lazyComp2With equal' viewFunction model dispatch

/// Create a fragment with a lazily rendered view function and custom equality on model field.
/// [category: Components]
let inline lazyComp3By (equal: ('model1 * 'model2) -> 'a) (viewFunction: ('model1 * 'model2) -> Elmish.Dispatch<'msg> -> Node) (model1: 'model1) (model2: 'model2) (dispatch: Elmish.Dispatch<'msg>) =
    let equal' (model11, model12) (model21, model22) = (equal (model11, model12)) = (equal (model21, model22))
    lazyComp3With equal' viewFunction model1 model2 dispatch

/// Create a navigation link which toggles its `active` class
/// based on whether the current URI matches its `href`.
/// [category: Components]
let inline navLink (``match``: Routing.NavLinkMatch) =
    ComponentWithAttrsBuilder<Routing.NavLink>(Html.attrs {
        "Match" => ``match``
    })

// BEGIN TAGS
/// Create an HTML `<a>` element.
/// [category: HTML tag names]
let a : ElementBuilder = elt "a"

/// Create an HTML `<abbr>` element.
/// [category: HTML tag names]
let abbr : ElementBuilder = elt "abbr"

/// Create an HTML `<acronym>` element.
/// [category: HTML tag names]
let acronym : ElementBuilder = elt "acronym"

/// Create an HTML `<address>` element.
/// [category: HTML tag names]
let address : ElementBuilder = elt "address"

/// Create an HTML `<applet>` element.
/// [category: HTML tag names]
let applet : ElementBuilder = elt "applet"

/// Create an HTML `<area>` element.
/// [category: HTML tag names]
let area : ElementBuilder = elt "area"

/// Create an HTML `<article>` element.
/// [category: HTML tag names]
let article : ElementBuilder = elt "article"

/// Create an HTML `<aside>` element.
/// [category: HTML tag names]
let aside : ElementBuilder = elt "aside"

/// Create an HTML `<audio>` element.
/// [category: HTML tag names]
let audio : ElementBuilder = elt "audio"

/// Create an HTML `<b>` element.
/// [category: HTML tag names]
let b : ElementBuilder = elt "b"

/// Create an HTML `<base>` element.
/// [category: HTML tag names]
let ``base`` : ElementBuilder = elt "base"

/// Create an HTML `<basefont>` element.
/// [category: HTML tag names]
let basefont : ElementBuilder = elt "basefont"

/// Create an HTML `<bdi>` element.
/// [category: HTML tag names]
let bdi : ElementBuilder = elt "bdi"

/// Create an HTML `<bdo>` element.
/// [category: HTML tag names]
let bdo : ElementBuilder = elt "bdo"

/// Create an HTML `<big>` element.
/// [category: HTML tag names]
let big : ElementBuilder = elt "big"

/// Create an HTML `<blockquote>` element.
/// [category: HTML tag names]
let blockquote : ElementBuilder = elt "blockquote"

/// Create an HTML `<body>` element.
/// [category: HTML tag names]
let body : ElementBuilder = elt "body"

/// Create an HTML `<br>` element.
/// [category: HTML tag names]
let br : ElementBuilder = elt "br"

/// Create an HTML `<button>` element.
/// [category: HTML tag names]
let button : ElementBuilder = elt "button"

/// Create an HTML `<canvas>` element.
/// [category: HTML tag names]
let canvas : ElementBuilder = elt "canvas"

/// Create an HTML `<caption>` element.
/// [category: HTML tag names]
let caption : ElementBuilder = elt "caption"

/// Create an HTML `<center>` element.
/// [category: HTML tag names]
let center : ElementBuilder = elt "center"

/// Create an HTML `<cite>` element.
/// [category: HTML tag names]
let cite : ElementBuilder = elt "cite"

/// Create an HTML `<code>` element.
/// [category: HTML tag names]
let code : ElementBuilder = elt "code"

/// Create an HTML `<col>` element.
/// [category: HTML tag names]
let col : ElementBuilder = elt "col"

/// Create an HTML `<colgroup>` element.
/// [category: HTML tag names]
let colgroup : ElementBuilder = elt "colgroup"

/// Create an HTML `<content>` element.
/// [category: HTML tag names]
let content : ElementBuilder = elt "content"

/// Create an HTML `<data>` element.
/// [category: HTML tag names]
let data : ElementBuilder = elt "data"

/// Create an HTML `<datalist>` element.
/// [category: HTML tag names]
let datalist : ElementBuilder = elt "datalist"

/// Create an HTML `<dd>` element.
/// [category: HTML tag names]
let dd : ElementBuilder = elt "dd"

/// Create an HTML `<del>` element.
/// [category: HTML tag names]
let del : ElementBuilder = elt "del"

/// Create an HTML `<details>` element.
/// [category: HTML tag names]
let details : ElementBuilder = elt "details"

/// Create an HTML `<dfn>` element.
/// [category: HTML tag names]
let dfn : ElementBuilder = elt "dfn"

/// Create an HTML `<dialog>` element.
/// [category: HTML tag names]
let dialog : ElementBuilder = elt "dialog"

/// Create an HTML `<dir>` element.
/// [category: HTML tag names]
let dir : ElementBuilder = elt "dir"

/// Create an HTML `<div>` element.
/// [category: HTML tag names]
let div : ElementBuilder = elt "div"

/// Create an HTML `<dl>` element.
/// [category: HTML tag names]
let dl : ElementBuilder = elt "dl"

/// Create an HTML `<dt>` element.
/// [category: HTML tag names]
let dt : ElementBuilder = elt "dt"

/// Create an HTML `<element>` element.
/// [category: HTML tag names]
let element : ElementBuilder = elt "element"

/// Create an HTML `<em>` element.
/// [category: HTML tag names]
let em : ElementBuilder = elt "em"

/// Create an HTML `<embed>` element.
/// [category: HTML tag names]
let embed : ElementBuilder = elt "embed"

/// Create an HTML `<fieldset>` element.
/// [category: HTML tag names]
let fieldset : ElementBuilder = elt "fieldset"

/// Create an HTML `<figcaption>` element.
/// [category: HTML tag names]
let figcaption : ElementBuilder = elt "figcaption"

/// Create an HTML `<figure>` element.
/// [category: HTML tag names]
let figure : ElementBuilder = elt "figure"

/// Create an HTML `<font>` element.
/// [category: HTML tag names]
let font : ElementBuilder = elt "font"

/// Create an HTML `<footer>` element.
/// [category: HTML tag names]
let footer : ElementBuilder = elt "footer"

/// Create an HTML `<form>` element.
/// [category: HTML tag names]
let form : ElementBuilder = elt "form"

/// Create an HTML `<frame>` element.
/// [category: HTML tag names]
let frame : ElementBuilder = elt "frame"

/// Create an HTML `<frameset>` element.
/// [category: HTML tag names]
let frameset : ElementBuilder = elt "frameset"

/// Create an HTML `<h1>` element.
/// [category: HTML tag names]
let h1 : ElementBuilder = elt "h1"

/// Create an HTML `<h2>` element.
/// [category: HTML tag names]
let h2 : ElementBuilder = elt "h2"

/// Create an HTML `<h3>` element.
/// [category: HTML tag names]
let h3 : ElementBuilder = elt "h3"

/// Create an HTML `<h4>` element.
/// [category: HTML tag names]
let h4 : ElementBuilder = elt "h4"

/// Create an HTML `<h5>` element.
/// [category: HTML tag names]
let h5 : ElementBuilder = elt "h5"

/// Create an HTML `<h6>` element.
/// [category: HTML tag names]
let h6 : ElementBuilder = elt "h6"

/// Create an HTML `<head>` element.
/// [category: HTML tag names]
let head : ElementBuilder = elt "head"

/// Create an HTML `<header>` element.
/// [category: HTML tag names]
let header : ElementBuilder = elt "header"

/// Create an HTML `<hr>` element.
/// [category: HTML tag names]
let hr : ElementBuilder = elt "hr"

/// Create an HTML `<html>` element.
/// [category: HTML tag names]
let html : ElementBuilder = elt "html"

/// Create an HTML `<i>` element.
/// [category: HTML tag names]
let i : ElementBuilder = elt "i"

/// Create an HTML `<iframe>` element.
/// [category: HTML tag names]
let iframe : ElementBuilder = elt "iframe"

/// Create an HTML `<img>` element.
/// [category: HTML tag names]
let img : ElementBuilder = elt "img"

/// Create an HTML `<input>` element.
/// [category: HTML tag names]
let input : ElementBuilder = elt "input"

/// Create an HTML `<ins>` element.
/// [category: HTML tag names]
let ins : ElementBuilder = elt "ins"

/// Create an HTML `<kbd>` element.
/// [category: HTML tag names]
let kbd : ElementBuilder = elt "kbd"

/// Create an HTML `<label>` element.
/// [category: HTML tag names]
let label : ElementBuilder = elt "label"

/// Create an HTML `<legend>` element.
/// [category: HTML tag names]
let legend : ElementBuilder = elt "legend"

/// Create an HTML `<li>` element.
/// [category: HTML tag names]
let li : ElementBuilder = elt "li"

/// Create an HTML `<link>` element.
/// [category: HTML tag names]
let link : ElementBuilder = elt "link"

/// Create an HTML `<main>` element.
/// [category: HTML tag names]
let main : ElementBuilder = elt "main"

/// Create an HTML `<map>` element.
/// [category: HTML tag names]
let map : ElementBuilder = elt "map"

/// Create an HTML `<mark>` element.
/// [category: HTML tag names]
let mark : ElementBuilder = elt "mark"

/// Create an HTML `<menu>` element.
/// [category: HTML tag names]
let menu : ElementBuilder = elt "menu"

/// Create an HTML `<menuitem>` element.
/// [category: HTML tag names]
let menuitem : ElementBuilder = elt "menuitem"

/// Create an HTML `<meta>` element.
/// [category: HTML tag names]
let meta : ElementBuilder = elt "meta"

/// Create an HTML `<meter>` element.
/// [category: HTML tag names]
let meter : ElementBuilder = elt "meter"

/// Create an HTML `<nav>` element.
/// [category: HTML tag names]
let nav : ElementBuilder = elt "nav"

/// Create an HTML `<noembed>` element.
/// [category: HTML tag names]
let noembed : ElementBuilder = elt "noembed"

/// Create an HTML `<noframes>` element.
/// [category: HTML tag names]
let noframes : ElementBuilder = elt "noframes"

/// Create an HTML `<noscript>` element.
/// [category: HTML tag names]
let noscript : ElementBuilder = elt "noscript"

/// Create an HTML `<object>` element.
/// [category: HTML tag names]
let object : ElementBuilder = elt "object"

/// Create an HTML `<ol>` element.
/// [category: HTML tag names]
let ol : ElementBuilder = elt "ol"

/// Create an HTML `<optgroup>` element.
/// [category: HTML tag names]
let optgroup : ElementBuilder = elt "optgroup"

/// Create an HTML `<option>` element.
/// [category: HTML tag names]
let option : ElementBuilder = elt "option"

/// Create an HTML `<output>` element.
/// [category: HTML tag names]
let output : ElementBuilder = elt "output"

/// Create an HTML `<p>` element.
/// [category: HTML tag names]
let p : ElementBuilder = elt "p"

/// Create an HTML `<param>` element.
/// [category: HTML tag names]
let param : ElementBuilder = elt "param"

/// Create an HTML `<picture>` element.
/// [category: HTML tag names]
let picture : ElementBuilder = elt "picture"

/// Create an HTML `<pre>` element.
/// [category: HTML tag names]
let pre : ElementBuilder = elt "pre"

/// Create an HTML `<progress>` element.
/// [category: HTML tag names]
let progress : ElementBuilder = elt "progress"

/// Create an HTML `<q>` element.
/// [category: HTML tag names]
let q : ElementBuilder = elt "q"

/// Create an HTML `<rb>` element.
/// [category: HTML tag names]
let rb : ElementBuilder = elt "rb"

/// Create an HTML `<rp>` element.
/// [category: HTML tag names]
let rp : ElementBuilder = elt "rp"

/// Create an HTML `<rt>` element.
/// [category: HTML tag names]
let rt : ElementBuilder = elt "rt"

/// Create an HTML `<rtc>` element.
/// [category: HTML tag names]
let rtc : ElementBuilder = elt "rtc"

/// Create an HTML `<ruby>` element.
/// [category: HTML tag names]
let ruby : ElementBuilder = elt "ruby"

/// Create an HTML `<s>` element.
/// [category: HTML tag names]
let s : ElementBuilder = elt "s"

/// Create an HTML `<samp>` element.
/// [category: HTML tag names]
let samp : ElementBuilder = elt "samp"

/// Create an HTML `<script>` element.
/// [category: HTML tag names]
let script : ElementBuilder = elt "script"

/// Create an HTML `<section>` element.
/// [category: HTML tag names]
let section : ElementBuilder = elt "section"

/// Create an HTML `<select>` element.
/// [category: HTML tag names]
let select : ElementBuilder = elt "select"

/// Create an HTML `<shadow>` element.
/// [category: HTML tag names]
let shadow : ElementBuilder = elt "shadow"

/// Create an HTML `<slot>` element.
/// [category: HTML tag names]
let slot : ElementBuilder = elt "slot"

/// Create an HTML `<small>` element.
/// [category: HTML tag names]
let small : ElementBuilder = elt "small"

/// Create an HTML `<source>` element.
/// [category: HTML tag names]
let source : ElementBuilder = elt "source"

/// Create an HTML `<span>` element.
/// [category: HTML tag names]
let span : ElementBuilder = elt "span"

/// Create an HTML `<strike>` element.
/// [category: HTML tag names]
let strike : ElementBuilder = elt "strike"

/// Create an HTML `<strong>` element.
/// [category: HTML tag names]
let strong : ElementBuilder = elt "strong"

/// Create an HTML `<style>` element.
/// [category: HTML tag names]
let style : ElementBuilder = elt "style"

/// Create an HTML `<sub>` element.
/// [category: HTML tag names]
let sub : ElementBuilder = elt "sub"

/// Create an HTML `<summary>` element.
/// [category: HTML tag names]
let summary : ElementBuilder = elt "summary"

/// Create an HTML `<sup>` element.
/// [category: HTML tag names]
let sup : ElementBuilder = elt "sup"

/// Create an HTML `<svg>` element.
/// [category: HTML tag names]
let svg : ElementBuilder = elt "svg"

/// Create an HTML `<table>` element.
/// [category: HTML tag names]
let table : ElementBuilder = elt "table"

/// Create an HTML `<tbody>` element.
/// [category: HTML tag names]
let tbody : ElementBuilder = elt "tbody"

/// Create an HTML `<td>` element.
/// [category: HTML tag names]
let td : ElementBuilder = elt "td"

/// Create an HTML `<template>` element.
/// [category: HTML tag names]
let template : ElementBuilder = elt "template"

/// Create an HTML `<textarea>` element.
/// [category: HTML tag names]
let textarea : ElementBuilder = elt "textarea"

/// Create an HTML `<tfoot>` element.
/// [category: HTML tag names]
let tfoot : ElementBuilder = elt "tfoot"

/// Create an HTML `<th>` element.
/// [category: HTML tag names]
let th : ElementBuilder = elt "th"

/// Create an HTML `<thead>` element.
/// [category: HTML tag names]
let thead : ElementBuilder = elt "thead"

/// Create an HTML `<time>` element.
/// [category: HTML tag names]
let time : ElementBuilder = elt "time"

/// Create an HTML `<title>` element.
/// [category: HTML tag names]
let title : ElementBuilder = elt "title"

/// Create an HTML `<tr>` element.
/// [category: HTML tag names]
let tr : ElementBuilder = elt "tr"

/// Create an HTML `<track>` element.
/// [category: HTML tag names]
let track : ElementBuilder = elt "track"

/// Create an HTML `<tt>` element.
/// [category: HTML tag names]
let tt : ElementBuilder = elt "tt"

/// Create an HTML `<u>` element.
/// [category: HTML tag names]
let u : ElementBuilder = elt "u"

/// Create an HTML `<ul>` element.
/// [category: HTML tag names]
let ul : ElementBuilder = elt "ul"

/// Create an HTML `<var>` element.
/// [category: HTML tag names]
let var : ElementBuilder = elt "var"

/// Create an HTML `<video>` element.
/// [category: HTML tag names]
let video : ElementBuilder = elt "video"

/// Create an HTML `<wbr>` element.
/// [category: HTML tag names]
let wbr : ElementBuilder = elt "wbr"

// END TAGS

/// HTML attributes.
module attr =
    /// Create an HTML `class` attribute containing the given class names.
    let inline classes (classes: list<string>) : Attr =
        Attr.Make "class" (String.concat " " classes)

    /// Bind an element or component reference.
    [<Obsolete "Use the ref directly in the element or component builder.">]
    let inline ref (r: Ref<'T>) =
        r

    /// Bind an element reference.
    [<Obsolete "Use the ref directly in the element or component builder.">]
    let inline bindRef (r: Ref<'T>) =
        r

    let inline key (k: obj) =
        Key(fun _ tb _ i ->
            tb.SetKey(k)
            i)

    /// Create an HTML `aria-X` attribute.
    let inline aria name (v: obj) = ("aria-" + name) => v

    /// Create an attribute whose value is a callback.
    /// Use this function for Blazor component attributes of type `EventCallback<T>`.
    /// Note: for HTML event handlers, prefer functions from the module `on`.
    let inline callback<'T> (name: string) ([<InlineIfLambda>] value: 'T -> unit) =
        Attr(fun receiver builder _ sequence ->
            builder.AddAttribute<'T>(sequence, name, EventCallback.Factory.Create(receiver, Action<'T>(value)))
            sequence + 1)

    module async =

        /// Create an attribute whose value is an asynchronous callback.
        /// Use this function for Blazor component attributes of type `EventCallback<T>`.
        /// Note: for HTML event handlers, prefer functions from the module `on.async`.
        let inline callback<'T> (name: string) ([<InlineIfLambda>] value: 'T -> Async<unit>) =
            Attr(fun receiver builder _ sequence ->
                builder.AddAttribute<'T>(sequence, name, EventCallback.Factory.Create(receiver, Func<'T, Task>(fun x -> Async.StartImmediateAsTask (value x) :> Task)))
                sequence + 1)

    module task =

        /// Create an attribute whose value is an asynchronous callback.
        /// Use this function for Blazor component attributes of type `EventCallback<T>`.
        /// Note: for HTML event handlers, prefer functions from the module `on.task`.
        let inline callback<'T> (name: string) ([<InlineIfLambda>] value: 'T -> Task) =
            Attr(fun receiver builder _ sequence ->
                builder.AddAttribute<'T>(sequence, name, EventCallback.Factory.Create(receiver, Func<'T, Task>(value)))
                sequence + 1)

    /// Create an attribute whose value is an HTML fragment.
    /// Use this function for Blazor component attributes of type `RenderFragment`.
    let inline fragment name ([<InlineIfLambda>] node: Node) =
        Attr(fun receiver builder matchCache sequence ->
            builder.AddAttribute(sequence, name, RenderFragment(fun rt ->
                node.Invoke(receiver, rt, matchCache, 0) |> ignore))
            sequence + 1)

    /// Create an attribute whose value is a parameterized HTML fragment.
    /// Use this function for Blazor component attributes of type `RenderFragment<T>`.
    let inline fragmentWith name ([<InlineIfLambda>] node: 'a -> Node) =
        Attr(fun receiver builder matchCache sequence ->
            builder.AddAttribute(sequence, name, RenderFragment<_>(fun ctx ->
                RenderFragment(fun rt ->
                    (node ctx).Invoke(receiver, rt, matchCache, 0) |> ignore)))
            sequence + 1)

// BEGIN ATTRS
    /// Create an HTML `accept` attribute.
    let inline accept (v: obj) : Attr = "accept" => v

    /// Create an HTML `accept-charset` attribute.
    let inline acceptCharset (v: obj) : Attr = "accept-charset" => v

    /// Create an HTML `accesskey` attribute.
    let inline accesskey (v: obj) : Attr = "accesskey" => v

    /// Create an HTML `action` attribute.
    let inline action (v: obj) : Attr = "action" => v

    /// Create an HTML `align` attribute.
    let inline align (v: obj) : Attr = "align" => v

    /// Create an HTML `allow` attribute.
    let inline allow (v: obj) : Attr = "allow" => v

    /// Create an HTML `alt` attribute.
    let inline alt (v: obj) : Attr = "alt" => v

    /// Create an HTML `async` attribute.
    let inline async' (v: obj) : Attr = "async" => v

    /// Create an HTML `autocapitalize` attribute.
    let inline autocapitalize (v: obj) : Attr = "autocapitalize" => v

    /// Create an HTML `autocomplete` attribute.
    let inline autocomplete (v: obj) : Attr = "autocomplete" => v

    /// Create an HTML `autofocus` attribute.
    let inline autofocus (v: obj) : Attr = "autofocus" => v

    /// Create an HTML `autoplay` attribute.
    let inline autoplay (v: obj) : Attr = "autoplay" => v

    /// Create an HTML `bgcolor` attribute.
    let inline bgcolor (v: obj) : Attr = "bgcolor" => v

    /// Create an HTML `border` attribute.
    let inline border (v: obj) : Attr = "border" => v

    /// Create an HTML `buffered` attribute.
    let inline buffered (v: obj) : Attr = "buffered" => v

    /// Create an HTML `challenge` attribute.
    let inline challenge (v: obj) : Attr = "challenge" => v

    /// Create an HTML `charset` attribute.
    let inline charset (v: obj) : Attr = "charset" => v

    /// Create an HTML `checked` attribute.
    let inline ``checked`` (v: obj) : Attr = "checked" => v

    /// Create an HTML `cite` attribute.
    let inline cite (v: obj) : Attr = "cite" => v

    /// Create an HTML `class` attribute.
    let inline ``class`` (v: obj) : Attr = "class" => v

    /// Create an HTML `code` attribute.
    let inline code (v: obj) : Attr = "code" => v

    /// Create an HTML `codebase` attribute.
    let inline codebase (v: obj) : Attr = "codebase" => v

    /// Create an HTML `color` attribute.
    let inline color (v: obj) : Attr = "color" => v

    /// Create an HTML `cols` attribute.
    let inline cols (v: obj) : Attr = "cols" => v

    /// Create an HTML `colspan` attribute.
    let inline colspan (v: obj) : Attr = "colspan" => v

    /// Create an HTML `content` attribute.
    let inline content (v: obj) : Attr = "content" => v

    /// Create an HTML `contenteditable` attribute.
    let inline contenteditable (v: obj) : Attr = "contenteditable" => v

    /// Create an HTML `contextmenu` attribute.
    let inline contextmenu (v: obj) : Attr = "contextmenu" => v

    /// Create an HTML `controls` attribute.
    let inline controls (v: obj) : Attr = "controls" => v

    /// Create an HTML `coords` attribute.
    let inline coords (v: obj) : Attr = "coords" => v

    /// Create an HTML `crossorigin` attribute.
    let inline crossorigin (v: obj) : Attr = "crossorigin" => v

    /// Create an HTML `csp` attribute.
    let inline csp (v: obj) : Attr = "csp" => v

    /// Create an HTML `data` attribute.
    let inline data (v: obj) : Attr = "data" => v

    /// Create an HTML `datetime` attribute.
    let inline datetime (v: obj) : Attr = "datetime" => v

    /// Create an HTML `decoding` attribute.
    let inline decoding (v: obj) : Attr = "decoding" => v

    /// Create an HTML `default` attribute.
    let inline ``default`` (v: obj) : Attr = "default" => v

    /// Create an HTML `defer` attribute.
    let inline defer (v: obj) : Attr = "defer" => v

    /// Create an HTML `dir` attribute.
    let inline dir (v: obj) : Attr = "dir" => v

    /// Create an HTML `dirname` attribute.
    let inline dirname (v: obj) : Attr = "dirname" => v

    /// Create an HTML `disabled` attribute.
    let inline disabled (v: obj) : Attr = "disabled" => v

    /// Create an HTML `download` attribute.
    let inline download (v: obj) : Attr = "download" => v

    /// Create an HTML `draggable` attribute.
    let inline draggable (v: obj) : Attr = "draggable" => v

    /// Create an HTML `dropzone` attribute.
    let inline dropzone (v: obj) : Attr = "dropzone" => v

    /// Create an HTML `enctype` attribute.
    let inline enctype (v: obj) : Attr = "enctype" => v

    /// Create an HTML `for` attribute.
    let inline ``for`` (v: obj) : Attr = "for" => v

    /// Create an HTML `form` attribute.
    let inline form (v: obj) : Attr = "form" => v

    /// Create an HTML `formaction` attribute.
    let inline formaction (v: obj) : Attr = "formaction" => v

    /// Create an HTML `headers` attribute.
    let inline headers (v: obj) : Attr = "headers" => v

    /// Create an HTML `height` attribute.
    let inline height (v: obj) : Attr = "height" => v

    /// Create an HTML `hidden` attribute.
    let inline hidden (v: obj) : Attr = "hidden" => v

    /// Create an HTML `high` attribute.
    let inline high (v: obj) : Attr = "high" => v

    /// Create an HTML `href` attribute.
    let inline href (v: obj) : Attr = "href" => v

    /// Create an HTML `hreflang` attribute.
    let inline hreflang (v: obj) : Attr = "hreflang" => v

    /// Create an HTML `http-equiv` attribute.
    let inline httpEquiv (v: obj) : Attr = "http-equiv" => v

    /// Create an HTML `icon` attribute.
    let inline icon (v: obj) : Attr = "icon" => v

    /// Create an HTML `id` attribute.
    let inline id (v: obj) : Attr = "id" => v

    /// Create an HTML `importance` attribute.
    let inline importance (v: obj) : Attr = "importance" => v

    /// Create an HTML `integrity` attribute.
    let inline integrity (v: obj) : Attr = "integrity" => v

    /// Create an HTML `ismap` attribute.
    let inline ismap (v: obj) : Attr = "ismap" => v

    /// Create an HTML `itemprop` attribute.
    let inline itemprop (v: obj) : Attr = "itemprop" => v

    /// Create an HTML `keytype` attribute.
    let inline keytype (v: obj) : Attr = "keytype" => v

    /// Create an HTML `kind` attribute.
    let inline kind (v: obj) : Attr = "kind" => v

    /// Create an HTML `label` attribute.
    let inline label (v: obj) : Attr = "label" => v

    /// Create an HTML `lang` attribute.
    let inline lang (v: obj) : Attr = "lang" => v

    /// Create an HTML `language` attribute.
    let inline language (v: obj) : Attr = "language" => v

    /// Create an HTML `lazyload` attribute.
    let inline lazyload (v: obj) : Attr = "lazyload" => v

    /// Create an HTML `list` attribute.
    let inline list (v: obj) : Attr = "list" => v

    /// Create an HTML `loop` attribute.
    let inline loop (v: obj) : Attr = "loop" => v

    /// Create an HTML `low` attribute.
    let inline low (v: obj) : Attr = "low" => v

    /// Create an HTML `manifest` attribute.
    let inline manifest (v: obj) : Attr = "manifest" => v

    /// Create an HTML `max` attribute.
    let inline max (v: obj) : Attr = "max" => v

    /// Create an HTML `maxlength` attribute.
    let inline maxlength (v: obj) : Attr = "maxlength" => v

    /// Create an HTML `media` attribute.
    let inline media (v: obj) : Attr = "media" => v

    /// Create an HTML `method` attribute.
    let inline method (v: obj) : Attr = "method" => v

    /// Create an HTML `min` attribute.
    let inline min (v: obj) : Attr = "min" => v

    /// Create an HTML `minlength` attribute.
    let inline minlength (v: obj) : Attr = "minlength" => v

    /// Create an HTML `multiple` attribute.
    let inline multiple (v: obj) : Attr = "multiple" => v

    /// Create an HTML `muted` attribute.
    let inline muted (v: obj) : Attr = "muted" => v

    /// Create an HTML `name` attribute.
    let inline name (v: obj) : Attr = "name" => v

    /// Create an HTML `novalidate` attribute.
    let inline novalidate (v: obj) : Attr = "novalidate" => v

    /// Create an HTML `open` attribute.
    let inline ``open`` (v: obj) : Attr = "open" => v

    /// Create an HTML `optimum` attribute.
    let inline optimum (v: obj) : Attr = "optimum" => v

    /// Create an HTML `pattern` attribute.
    let inline pattern (v: obj) : Attr = "pattern" => v

    /// Create an HTML `ping` attribute.
    let inline ping (v: obj) : Attr = "ping" => v

    /// Create an HTML `placeholder` attribute.
    let inline placeholder (v: obj) : Attr = "placeholder" => v

    /// Create an HTML `poster` attribute.
    let inline poster (v: obj) : Attr = "poster" => v

    /// Create an HTML `preload` attribute.
    let inline preload (v: obj) : Attr = "preload" => v

    /// Create an HTML `readonly` attribute.
    let inline readonly (v: obj) : Attr = "readonly" => v

    /// Create an HTML `rel` attribute.
    let inline rel (v: obj) : Attr = "rel" => v

    /// Create an HTML `required` attribute.
    let inline required (v: obj) : Attr = "required" => v

    /// Create an HTML `reversed` attribute.
    let inline reversed (v: obj) : Attr = "reversed" => v

    /// Create an HTML `rows` attribute.
    let inline rows (v: obj) : Attr = "rows" => v

    /// Create an HTML `rowspan` attribute.
    let inline rowspan (v: obj) : Attr = "rowspan" => v

    /// Create an HTML `sandbox` attribute.
    let inline sandbox (v: obj) : Attr = "sandbox" => v

    /// Create an HTML `scope` attribute.
    let inline scope (v: obj) : Attr = "scope" => v

    /// Create an HTML `selected` attribute.
    let inline selected (v: obj) : Attr = "selected" => v

    /// Create an HTML `shape` attribute.
    let inline shape (v: obj) : Attr = "shape" => v

    /// Create an HTML `size` attribute.
    let inline size (v: obj) : Attr = "size" => v

    /// Create an HTML `sizes` attribute.
    let inline sizes (v: obj) : Attr = "sizes" => v

    /// Create an HTML `slot` attribute.
    let inline slot (v: obj) : Attr = "slot" => v

    /// Create an HTML `span` attribute.
    let inline span (v: obj) : Attr = "span" => v

    /// Create an HTML `spellcheck` attribute.
    let inline spellcheck (v: obj) : Attr = "spellcheck" => v

    /// Create an HTML `src` attribute.
    let inline src (v: obj) : Attr = "src" => v

    /// Create an HTML `srcdoc` attribute.
    let inline srcdoc (v: obj) : Attr = "srcdoc" => v

    /// Create an HTML `srclang` attribute.
    let inline srclang (v: obj) : Attr = "srclang" => v

    /// Create an HTML `srcset` attribute.
    let inline srcset (v: obj) : Attr = "srcset" => v

    /// Create an HTML `start` attribute.
    let inline start (v: obj) : Attr = "start" => v

    /// Create an HTML `step` attribute.
    let inline step (v: obj) : Attr = "step" => v

    /// Create an HTML `style` attribute.
    let inline style (v: obj) : Attr = "style" => v

    /// Create an HTML `summary` attribute.
    let inline summary (v: obj) : Attr = "summary" => v

    /// Create an HTML `tabindex` attribute.
    let inline tabindex (v: obj) : Attr = "tabindex" => v

    /// Create an HTML `target` attribute.
    let inline target (v: obj) : Attr = "target" => v

    /// Create an HTML `title` attribute.
    let inline title (v: obj) : Attr = "title" => v

    /// Create an HTML `translate` attribute.
    let inline translate (v: obj) : Attr = "translate" => v

    /// Create an HTML `type` attribute.
    let inline ``type`` (v: obj) : Attr = "type" => v

    /// Create an HTML `usemap` attribute.
    let inline usemap (v: obj) : Attr = "usemap" => v

    /// Create an HTML `value` attribute.
    let inline value (v: obj) : Attr = "value" => v

    /// Create an HTML `width` attribute.
    let inline width (v: obj) : Attr = "width" => v

    /// Create an HTML `wrap` attribute.
    let inline wrap (v: obj) : Attr = "wrap" => v

// END ATTRS

/// Event handlers.
module on =

    /// Create a handler for a HTML event of type EventArgs.
    let inline event<'T when 'T :> EventArgs> eventName (callback: ^T -> unit) =
        attr.callback<'T> ("on" + eventName) callback

    /// Prevent the default event behavior for a given HTML event.
    let inline preventDefault eventName (value: bool) =
        Attr(fun _ builder _ sequence ->
            builder.AddEventPreventDefaultAttribute(sequence, eventName, value)
            sequence + 1
        )

    /// Stop the propagation to parent elements of a given HTML event.
    let inline stopPropagation eventName (value: bool) =
        Attr(fun _ builder _ sequence ->
            builder.AddEventStopPropagationAttribute(sequence, eventName, value)
            sequence + 1
        )

// BEGIN EVENTS
    /// Create a handler for HTML event `focus`.
    let inline focus (callback: FocusEventArgs -> unit) : Attr =
        attr.callback<FocusEventArgs> "onfocus" callback

    /// Create a handler for HTML event `blur`.
    let inline blur (callback: FocusEventArgs -> unit) : Attr =
        attr.callback<FocusEventArgs> "onblur" callback

    /// Create a handler for HTML event `focusin`.
    let inline focusin (callback: FocusEventArgs -> unit) : Attr =
        attr.callback<FocusEventArgs> "onfocusin" callback

    /// Create a handler for HTML event `focusout`.
    let inline focusout (callback: FocusEventArgs -> unit) : Attr =
        attr.callback<FocusEventArgs> "onfocusout" callback

    /// Create a handler for HTML event `mouseover`.
    let inline mouseover (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "onmouseover" callback

    /// Create a handler for HTML event `mouseout`.
    let inline mouseout (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "onmouseout" callback

    /// Create a handler for HTML event `mousemove`.
    let inline mousemove (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "onmousemove" callback

    /// Create a handler for HTML event `mousedown`.
    let inline mousedown (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "onmousedown" callback

    /// Create a handler for HTML event `mouseup`.
    let inline mouseup (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "onmouseup" callback

    /// Create a handler for HTML event `click`.
    let inline click (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "onclick" callback

    /// Create a handler for HTML event `dblclick`.
    let inline dblclick (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "ondblclick" callback

    /// Create a handler for HTML event `wheel`.
    let inline wheel (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "onwheel" callback

    /// Create a handler for HTML event `mousewheel`.
    let inline mousewheel (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "onmousewheel" callback

    /// Create a handler for HTML event `contextmenu`.
    let inline contextmenu (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "oncontextmenu" callback

    /// Create a handler for HTML event `drag`.
    let inline drag (callback: DragEventArgs -> unit) : Attr =
        attr.callback<DragEventArgs> "ondrag" callback

    /// Create a handler for HTML event `dragend`.
    let inline dragend (callback: DragEventArgs -> unit) : Attr =
        attr.callback<DragEventArgs> "ondragend" callback

    /// Create a handler for HTML event `dragenter`.
    let inline dragenter (callback: DragEventArgs -> unit) : Attr =
        attr.callback<DragEventArgs> "ondragenter" callback

    /// Create a handler for HTML event `dragleave`.
    let inline dragleave (callback: DragEventArgs -> unit) : Attr =
        attr.callback<DragEventArgs> "ondragleave" callback

    /// Create a handler for HTML event `dragover`.
    let inline dragover (callback: DragEventArgs -> unit) : Attr =
        attr.callback<DragEventArgs> "ondragover" callback

    /// Create a handler for HTML event `dragstart`.
    let inline dragstart (callback: DragEventArgs -> unit) : Attr =
        attr.callback<DragEventArgs> "ondragstart" callback

    /// Create a handler for HTML event `drop`.
    let inline drop (callback: DragEventArgs -> unit) : Attr =
        attr.callback<DragEventArgs> "ondrop" callback

    /// Create a handler for HTML event `keydown`.
    let inline keydown (callback: KeyboardEventArgs -> unit) : Attr =
        attr.callback<KeyboardEventArgs> "onkeydown" callback

    /// Create a handler for HTML event `keyup`.
    let inline keyup (callback: KeyboardEventArgs -> unit) : Attr =
        attr.callback<KeyboardEventArgs> "onkeyup" callback

    /// Create a handler for HTML event `keypress`.
    let inline keypress (callback: KeyboardEventArgs -> unit) : Attr =
        attr.callback<KeyboardEventArgs> "onkeypress" callback

    /// Create a handler for HTML event `change`.
    let inline change (callback: ChangeEventArgs -> unit) : Attr =
        attr.callback<ChangeEventArgs> "onchange" callback

    /// Create a handler for HTML event `input`.
    let inline input (callback: ChangeEventArgs -> unit) : Attr =
        attr.callback<ChangeEventArgs> "oninput" callback

    /// Create a handler for HTML event `invalid`.
    let inline invalid (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "oninvalid" callback

    /// Create a handler for HTML event `reset`.
    let inline reset (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onreset" callback

    /// Create a handler for HTML event `select`.
    let inline select (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onselect" callback

    /// Create a handler for HTML event `selectstart`.
    let inline selectstart (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onselectstart" callback

    /// Create a handler for HTML event `selectionchange`.
    let inline selectionchange (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onselectionchange" callback

    /// Create a handler for HTML event `submit`.
    let inline submit (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onsubmit" callback

    /// Create a handler for HTML event `beforecopy`.
    let inline beforecopy (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onbeforecopy" callback

    /// Create a handler for HTML event `beforecut`.
    let inline beforecut (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onbeforecut" callback

    /// Create a handler for HTML event `beforepaste`.
    let inline beforepaste (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onbeforepaste" callback

    /// Create a handler for HTML event `copy`.
    let inline copy (callback: ClipboardEventArgs -> unit) : Attr =
        attr.callback<ClipboardEventArgs> "oncopy" callback

    /// Create a handler for HTML event `cut`.
    let inline cut (callback: ClipboardEventArgs -> unit) : Attr =
        attr.callback<ClipboardEventArgs> "oncut" callback

    /// Create a handler for HTML event `paste`.
    let inline paste (callback: ClipboardEventArgs -> unit) : Attr =
        attr.callback<ClipboardEventArgs> "onpaste" callback

    /// Create a handler for HTML event `touchcancel`.
    let inline touchcancel (callback: TouchEventArgs -> unit) : Attr =
        attr.callback<TouchEventArgs> "ontouchcancel" callback

    /// Create a handler for HTML event `touchend`.
    let inline touchend (callback: TouchEventArgs -> unit) : Attr =
        attr.callback<TouchEventArgs> "ontouchend" callback

    /// Create a handler for HTML event `touchmove`.
    let inline touchmove (callback: TouchEventArgs -> unit) : Attr =
        attr.callback<TouchEventArgs> "ontouchmove" callback

    /// Create a handler for HTML event `touchstart`.
    let inline touchstart (callback: TouchEventArgs -> unit) : Attr =
        attr.callback<TouchEventArgs> "ontouchstart" callback

    /// Create a handler for HTML event `touchenter`.
    let inline touchenter (callback: TouchEventArgs -> unit) : Attr =
        attr.callback<TouchEventArgs> "ontouchenter" callback

    /// Create a handler for HTML event `touchleave`.
    let inline touchleave (callback: TouchEventArgs -> unit) : Attr =
        attr.callback<TouchEventArgs> "ontouchleave" callback

    /// Create a handler for HTML event `pointercapture`.
    let inline pointercapture (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointercapture" callback

    /// Create a handler for HTML event `lostpointercapture`.
    let inline lostpointercapture (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onlostpointercapture" callback

    /// Create a handler for HTML event `pointercancel`.
    let inline pointercancel (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointercancel" callback

    /// Create a handler for HTML event `pointerdown`.
    let inline pointerdown (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointerdown" callback

    /// Create a handler for HTML event `pointerenter`.
    let inline pointerenter (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointerenter" callback

    /// Create a handler for HTML event `pointerleave`.
    let inline pointerleave (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointerleave" callback

    /// Create a handler for HTML event `pointermove`.
    let inline pointermove (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointermove" callback

    /// Create a handler for HTML event `pointerout`.
    let inline pointerout (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointerout" callback

    /// Create a handler for HTML event `pointerover`.
    let inline pointerover (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointerover" callback

    /// Create a handler for HTML event `pointerup`.
    let inline pointerup (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointerup" callback

    /// Create a handler for HTML event `canplay`.
    let inline canplay (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "oncanplay" callback

    /// Create a handler for HTML event `canplaythrough`.
    let inline canplaythrough (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "oncanplaythrough" callback

    /// Create a handler for HTML event `cuechange`.
    let inline cuechange (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "oncuechange" callback

    /// Create a handler for HTML event `durationchange`.
    let inline durationchange (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "ondurationchange" callback

    /// Create a handler for HTML event `emptied`.
    let inline emptied (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onemptied" callback

    /// Create a handler for HTML event `pause`.
    let inline pause (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onpause" callback

    /// Create a handler for HTML event `play`.
    let inline play (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onplay" callback

    /// Create a handler for HTML event `playing`.
    let inline playing (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onplaying" callback

    /// Create a handler for HTML event `ratechange`.
    let inline ratechange (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onratechange" callback

    /// Create a handler for HTML event `seeked`.
    let inline seeked (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onseeked" callback

    /// Create a handler for HTML event `seeking`.
    let inline seeking (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onseeking" callback

    /// Create a handler for HTML event `stalled`.
    let inline stalled (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onstalled" callback

    /// Create a handler for HTML event `stop`.
    let inline stop (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onstop" callback

    /// Create a handler for HTML event `suspend`.
    let inline suspend (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onsuspend" callback

    /// Create a handler for HTML event `timeupdate`.
    let inline timeupdate (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "ontimeupdate" callback

    /// Create a handler for HTML event `volumechange`.
    let inline volumechange (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onvolumechange" callback

    /// Create a handler for HTML event `waiting`.
    let inline waiting (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onwaiting" callback

    /// Create a handler for HTML event `loadstart`.
    let inline loadstart (callback: ProgressEventArgs -> unit) : Attr =
        attr.callback<ProgressEventArgs> "onloadstart" callback

    /// Create a handler for HTML event `timeout`.
    let inline timeout (callback: ProgressEventArgs -> unit) : Attr =
        attr.callback<ProgressEventArgs> "ontimeout" callback

    /// Create a handler for HTML event `abort`.
    let inline abort (callback: ProgressEventArgs -> unit) : Attr =
        attr.callback<ProgressEventArgs> "onabort" callback

    /// Create a handler for HTML event `load`.
    let inline load (callback: ProgressEventArgs -> unit) : Attr =
        attr.callback<ProgressEventArgs> "onload" callback

    /// Create a handler for HTML event `loadend`.
    let inline loadend (callback: ProgressEventArgs -> unit) : Attr =
        attr.callback<ProgressEventArgs> "onloadend" callback

    /// Create a handler for HTML event `progress`.
    let inline progress (callback: ProgressEventArgs -> unit) : Attr =
        attr.callback<ProgressEventArgs> "onprogress" callback

    /// Create a handler for HTML event `error`.
    let inline error (callback: ProgressEventArgs -> unit) : Attr =
        attr.callback<ProgressEventArgs> "onerror" callback

    /// Create a handler for HTML event `activate`.
    let inline activate (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onactivate" callback

    /// Create a handler for HTML event `beforeactivate`.
    let inline beforeactivate (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onbeforeactivate" callback

    /// Create a handler for HTML event `beforedeactivate`.
    let inline beforedeactivate (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onbeforedeactivate" callback

    /// Create a handler for HTML event `deactivate`.
    let inline deactivate (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "ondeactivate" callback

    /// Create a handler for HTML event `ended`.
    let inline ended (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onended" callback

    /// Create a handler for HTML event `fullscreenchange`.
    let inline fullscreenchange (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onfullscreenchange" callback

    /// Create a handler for HTML event `fullscreenerror`.
    let inline fullscreenerror (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onfullscreenerror" callback

    /// Create a handler for HTML event `loadeddata`.
    let inline loadeddata (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onloadeddata" callback

    /// Create a handler for HTML event `loadedmetadata`.
    let inline loadedmetadata (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onloadedmetadata" callback

    /// Create a handler for HTML event `pointerlockchange`.
    let inline pointerlockchange (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onpointerlockchange" callback

    /// Create a handler for HTML event `pointerlockerror`.
    let inline pointerlockerror (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onpointerlockerror" callback

    /// Create a handler for HTML event `readystatechange`.
    let inline readystatechange (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onreadystatechange" callback

    /// Create a handler for HTML event `scroll`.
    let inline scroll (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onscroll" callback

// END EVENTS

    /// Event handlers returning type `Async<unit>`.
    module async =

        /// Create an asynchronous handler for a HTML event of type EventArgs.
        let inline event<'T> eventName (callback: 'T -> Async<unit>) =
            attr.async.callback<'T> ("on" + eventName) callback

// BEGIN ASYNCEVENTS
        /// Create an asynchronous handler for HTML event `focus`.
        let inline focus (callback: FocusEventArgs -> Async<unit>) : Attr =
            attr.async.callback<FocusEventArgs> "onfocus" callback
        /// Create an asynchronous handler for HTML event `blur`.
        let inline blur (callback: FocusEventArgs -> Async<unit>) : Attr =
            attr.async.callback<FocusEventArgs> "onblur" callback
        /// Create an asynchronous handler for HTML event `focusin`.
        let inline focusin (callback: FocusEventArgs -> Async<unit>) : Attr =
            attr.async.callback<FocusEventArgs> "onfocusin" callback
        /// Create an asynchronous handler for HTML event `focusout`.
        let inline focusout (callback: FocusEventArgs -> Async<unit>) : Attr =
            attr.async.callback<FocusEventArgs> "onfocusout" callback
        /// Create an asynchronous handler for HTML event `mouseover`.
        let inline mouseover (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "onmouseover" callback
        /// Create an asynchronous handler for HTML event `mouseout`.
        let inline mouseout (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "onmouseout" callback
        /// Create an asynchronous handler for HTML event `mousemove`.
        let inline mousemove (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "onmousemove" callback
        /// Create an asynchronous handler for HTML event `mousedown`.
        let inline mousedown (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "onmousedown" callback
        /// Create an asynchronous handler for HTML event `mouseup`.
        let inline mouseup (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "onmouseup" callback
        /// Create an asynchronous handler for HTML event `click`.
        let inline click (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "onclick" callback
        /// Create an asynchronous handler for HTML event `dblclick`.
        let inline dblclick (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "ondblclick" callback
        /// Create an asynchronous handler for HTML event `wheel`.
        let inline wheel (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "onwheel" callback
        /// Create an asynchronous handler for HTML event `mousewheel`.
        let inline mousewheel (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "onmousewheel" callback
        /// Create an asynchronous handler for HTML event `contextmenu`.
        let inline contextmenu (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "oncontextmenu" callback
        /// Create an asynchronous handler for HTML event `drag`.
        let inline drag (callback: DragEventArgs -> Async<unit>) : Attr =
            attr.async.callback<DragEventArgs> "ondrag" callback
        /// Create an asynchronous handler for HTML event `dragend`.
        let inline dragend (callback: DragEventArgs -> Async<unit>) : Attr =
            attr.async.callback<DragEventArgs> "ondragend" callback
        /// Create an asynchronous handler for HTML event `dragenter`.
        let inline dragenter (callback: DragEventArgs -> Async<unit>) : Attr =
            attr.async.callback<DragEventArgs> "ondragenter" callback
        /// Create an asynchronous handler for HTML event `dragleave`.
        let inline dragleave (callback: DragEventArgs -> Async<unit>) : Attr =
            attr.async.callback<DragEventArgs> "ondragleave" callback
        /// Create an asynchronous handler for HTML event `dragover`.
        let inline dragover (callback: DragEventArgs -> Async<unit>) : Attr =
            attr.async.callback<DragEventArgs> "ondragover" callback
        /// Create an asynchronous handler for HTML event `dragstart`.
        let inline dragstart (callback: DragEventArgs -> Async<unit>) : Attr =
            attr.async.callback<DragEventArgs> "ondragstart" callback
        /// Create an asynchronous handler for HTML event `drop`.
        let inline drop (callback: DragEventArgs -> Async<unit>) : Attr =
            attr.async.callback<DragEventArgs> "ondrop" callback
        /// Create an asynchronous handler for HTML event `keydown`.
        let inline keydown (callback: KeyboardEventArgs -> Async<unit>) : Attr =
            attr.async.callback<KeyboardEventArgs> "onkeydown" callback
        /// Create an asynchronous handler for HTML event `keyup`.
        let inline keyup (callback: KeyboardEventArgs -> Async<unit>) : Attr =
            attr.async.callback<KeyboardEventArgs> "onkeyup" callback
        /// Create an asynchronous handler for HTML event `keypress`.
        let inline keypress (callback: KeyboardEventArgs -> Async<unit>) : Attr =
            attr.async.callback<KeyboardEventArgs> "onkeypress" callback
        /// Create an asynchronous handler for HTML event `change`.
        let inline change (callback: ChangeEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ChangeEventArgs> "onchange" callback
        /// Create an asynchronous handler for HTML event `input`.
        let inline input (callback: ChangeEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ChangeEventArgs> "oninput" callback
        /// Create an asynchronous handler for HTML event `invalid`.
        let inline invalid (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "oninvalid" callback
        /// Create an asynchronous handler for HTML event `reset`.
        let inline reset (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onreset" callback
        /// Create an asynchronous handler for HTML event `select`.
        let inline select (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onselect" callback
        /// Create an asynchronous handler for HTML event `selectstart`.
        let inline selectstart (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onselectstart" callback
        /// Create an asynchronous handler for HTML event `selectionchange`.
        let inline selectionchange (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onselectionchange" callback
        /// Create an asynchronous handler for HTML event `submit`.
        let inline submit (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onsubmit" callback
        /// Create an asynchronous handler for HTML event `beforecopy`.
        let inline beforecopy (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onbeforecopy" callback
        /// Create an asynchronous handler for HTML event `beforecut`.
        let inline beforecut (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onbeforecut" callback
        /// Create an asynchronous handler for HTML event `beforepaste`.
        let inline beforepaste (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onbeforepaste" callback
        /// Create an asynchronous handler for HTML event `copy`.
        let inline copy (callback: ClipboardEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ClipboardEventArgs> "oncopy" callback
        /// Create an asynchronous handler for HTML event `cut`.
        let inline cut (callback: ClipboardEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ClipboardEventArgs> "oncut" callback
        /// Create an asynchronous handler for HTML event `paste`.
        let inline paste (callback: ClipboardEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ClipboardEventArgs> "onpaste" callback
        /// Create an asynchronous handler for HTML event `touchcancel`.
        let inline touchcancel (callback: TouchEventArgs -> Async<unit>) : Attr =
            attr.async.callback<TouchEventArgs> "ontouchcancel" callback
        /// Create an asynchronous handler for HTML event `touchend`.
        let inline touchend (callback: TouchEventArgs -> Async<unit>) : Attr =
            attr.async.callback<TouchEventArgs> "ontouchend" callback
        /// Create an asynchronous handler for HTML event `touchmove`.
        let inline touchmove (callback: TouchEventArgs -> Async<unit>) : Attr =
            attr.async.callback<TouchEventArgs> "ontouchmove" callback
        /// Create an asynchronous handler for HTML event `touchstart`.
        let inline touchstart (callback: TouchEventArgs -> Async<unit>) : Attr =
            attr.async.callback<TouchEventArgs> "ontouchstart" callback
        /// Create an asynchronous handler for HTML event `touchenter`.
        let inline touchenter (callback: TouchEventArgs -> Async<unit>) : Attr =
            attr.async.callback<TouchEventArgs> "ontouchenter" callback
        /// Create an asynchronous handler for HTML event `touchleave`.
        let inline touchleave (callback: TouchEventArgs -> Async<unit>) : Attr =
            attr.async.callback<TouchEventArgs> "ontouchleave" callback
        /// Create an asynchronous handler for HTML event `pointercapture`.
        let inline pointercapture (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointercapture" callback
        /// Create an asynchronous handler for HTML event `lostpointercapture`.
        let inline lostpointercapture (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onlostpointercapture" callback
        /// Create an asynchronous handler for HTML event `pointercancel`.
        let inline pointercancel (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointercancel" callback
        /// Create an asynchronous handler for HTML event `pointerdown`.
        let inline pointerdown (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointerdown" callback
        /// Create an asynchronous handler for HTML event `pointerenter`.
        let inline pointerenter (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointerenter" callback
        /// Create an asynchronous handler for HTML event `pointerleave`.
        let inline pointerleave (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointerleave" callback
        /// Create an asynchronous handler for HTML event `pointermove`.
        let inline pointermove (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointermove" callback
        /// Create an asynchronous handler for HTML event `pointerout`.
        let inline pointerout (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointerout" callback
        /// Create an asynchronous handler for HTML event `pointerover`.
        let inline pointerover (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointerover" callback
        /// Create an asynchronous handler for HTML event `pointerup`.
        let inline pointerup (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointerup" callback
        /// Create an asynchronous handler for HTML event `canplay`.
        let inline canplay (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "oncanplay" callback
        /// Create an asynchronous handler for HTML event `canplaythrough`.
        let inline canplaythrough (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "oncanplaythrough" callback
        /// Create an asynchronous handler for HTML event `cuechange`.
        let inline cuechange (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "oncuechange" callback
        /// Create an asynchronous handler for HTML event `durationchange`.
        let inline durationchange (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "ondurationchange" callback
        /// Create an asynchronous handler for HTML event `emptied`.
        let inline emptied (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onemptied" callback
        /// Create an asynchronous handler for HTML event `pause`.
        let inline pause (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onpause" callback
        /// Create an asynchronous handler for HTML event `play`.
        let inline play (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onplay" callback
        /// Create an asynchronous handler for HTML event `playing`.
        let inline playing (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onplaying" callback
        /// Create an asynchronous handler for HTML event `ratechange`.
        let inline ratechange (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onratechange" callback
        /// Create an asynchronous handler for HTML event `seeked`.
        let inline seeked (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onseeked" callback
        /// Create an asynchronous handler for HTML event `seeking`.
        let inline seeking (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onseeking" callback
        /// Create an asynchronous handler for HTML event `stalled`.
        let inline stalled (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onstalled" callback
        /// Create an asynchronous handler for HTML event `stop`.
        let inline stop (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onstop" callback
        /// Create an asynchronous handler for HTML event `suspend`.
        let inline suspend (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onsuspend" callback
        /// Create an asynchronous handler for HTML event `timeupdate`.
        let inline timeupdate (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "ontimeupdate" callback
        /// Create an asynchronous handler for HTML event `volumechange`.
        let inline volumechange (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onvolumechange" callback
        /// Create an asynchronous handler for HTML event `waiting`.
        let inline waiting (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onwaiting" callback
        /// Create an asynchronous handler for HTML event `loadstart`.
        let inline loadstart (callback: ProgressEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ProgressEventArgs> "onloadstart" callback
        /// Create an asynchronous handler for HTML event `timeout`.
        let inline timeout (callback: ProgressEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ProgressEventArgs> "ontimeout" callback
        /// Create an asynchronous handler for HTML event `abort`.
        let inline abort (callback: ProgressEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ProgressEventArgs> "onabort" callback
        /// Create an asynchronous handler for HTML event `load`.
        let inline load (callback: ProgressEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ProgressEventArgs> "onload" callback
        /// Create an asynchronous handler for HTML event `loadend`.
        let inline loadend (callback: ProgressEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ProgressEventArgs> "onloadend" callback
        /// Create an asynchronous handler for HTML event `progress`.
        let inline progress (callback: ProgressEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ProgressEventArgs> "onprogress" callback
        /// Create an asynchronous handler for HTML event `error`.
        let inline error (callback: ProgressEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ProgressEventArgs> "onerror" callback
        /// Create an asynchronous handler for HTML event `activate`.
        let inline activate (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onactivate" callback
        /// Create an asynchronous handler for HTML event `beforeactivate`.
        let inline beforeactivate (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onbeforeactivate" callback
        /// Create an asynchronous handler for HTML event `beforedeactivate`.
        let inline beforedeactivate (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onbeforedeactivate" callback
        /// Create an asynchronous handler for HTML event `deactivate`.
        let inline deactivate (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "ondeactivate" callback
        /// Create an asynchronous handler for HTML event `ended`.
        let inline ended (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onended" callback
        /// Create an asynchronous handler for HTML event `fullscreenchange`.
        let inline fullscreenchange (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onfullscreenchange" callback
        /// Create an asynchronous handler for HTML event `fullscreenerror`.
        let inline fullscreenerror (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onfullscreenerror" callback
        /// Create an asynchronous handler for HTML event `loadeddata`.
        let inline loadeddata (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onloadeddata" callback
        /// Create an asynchronous handler for HTML event `loadedmetadata`.
        let inline loadedmetadata (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onloadedmetadata" callback
        /// Create an asynchronous handler for HTML event `pointerlockchange`.
        let inline pointerlockchange (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onpointerlockchange" callback
        /// Create an asynchronous handler for HTML event `pointerlockerror`.
        let inline pointerlockerror (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onpointerlockerror" callback
        /// Create an asynchronous handler for HTML event `readystatechange`.
        let inline readystatechange (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onreadystatechange" callback
        /// Create an asynchronous handler for HTML event `scroll`.
        let inline scroll (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onscroll" callback
// END ASYNCEVENTS

    /// Event handlers returning type `Task`.
    module task =

        /// Create an asynchronous handler for a HTML event of type EventArgs.
        let inline event<'T> eventName (callback: 'T -> Task) =
            attr.task.callback<'T> ("on" + eventName) callback

// BEGIN TASKEVENTS
        /// Create an asynchronous handler for HTML event `focus`.
        let inline focus (callback: FocusEventArgs -> Task) : Attr =
            attr.task.callback<FocusEventArgs> "onfocus" callback
        /// Create an asynchronous handler for HTML event `blur`.
        let inline blur (callback: FocusEventArgs -> Task) : Attr =
            attr.task.callback<FocusEventArgs> "onblur" callback
        /// Create an asynchronous handler for HTML event `focusin`.
        let inline focusin (callback: FocusEventArgs -> Task) : Attr =
            attr.task.callback<FocusEventArgs> "onfocusin" callback
        /// Create an asynchronous handler for HTML event `focusout`.
        let inline focusout (callback: FocusEventArgs -> Task) : Attr =
            attr.task.callback<FocusEventArgs> "onfocusout" callback
        /// Create an asynchronous handler for HTML event `mouseover`.
        let inline mouseover (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "onmouseover" callback
        /// Create an asynchronous handler for HTML event `mouseout`.
        let inline mouseout (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "onmouseout" callback
        /// Create an asynchronous handler for HTML event `mousemove`.
        let inline mousemove (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "onmousemove" callback
        /// Create an asynchronous handler for HTML event `mousedown`.
        let inline mousedown (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "onmousedown" callback
        /// Create an asynchronous handler for HTML event `mouseup`.
        let inline mouseup (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "onmouseup" callback
        /// Create an asynchronous handler for HTML event `click`.
        let inline click (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "onclick" callback
        /// Create an asynchronous handler for HTML event `dblclick`.
        let inline dblclick (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "ondblclick" callback
        /// Create an asynchronous handler for HTML event `wheel`.
        let inline wheel (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "onwheel" callback
        /// Create an asynchronous handler for HTML event `mousewheel`.
        let inline mousewheel (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "onmousewheel" callback
        /// Create an asynchronous handler for HTML event `contextmenu`.
        let inline contextmenu (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "oncontextmenu" callback
        /// Create an asynchronous handler for HTML event `drag`.
        let inline drag (callback: DragEventArgs -> Task) : Attr =
            attr.task.callback<DragEventArgs> "ondrag" callback
        /// Create an asynchronous handler for HTML event `dragend`.
        let inline dragend (callback: DragEventArgs -> Task) : Attr =
            attr.task.callback<DragEventArgs> "ondragend" callback
        /// Create an asynchronous handler for HTML event `dragenter`.
        let inline dragenter (callback: DragEventArgs -> Task) : Attr =
            attr.task.callback<DragEventArgs> "ondragenter" callback
        /// Create an asynchronous handler for HTML event `dragleave`.
        let inline dragleave (callback: DragEventArgs -> Task) : Attr =
            attr.task.callback<DragEventArgs> "ondragleave" callback
        /// Create an asynchronous handler for HTML event `dragover`.
        let inline dragover (callback: DragEventArgs -> Task) : Attr =
            attr.task.callback<DragEventArgs> "ondragover" callback
        /// Create an asynchronous handler for HTML event `dragstart`.
        let inline dragstart (callback: DragEventArgs -> Task) : Attr =
            attr.task.callback<DragEventArgs> "ondragstart" callback
        /// Create an asynchronous handler for HTML event `drop`.
        let inline drop (callback: DragEventArgs -> Task) : Attr =
            attr.task.callback<DragEventArgs> "ondrop" callback
        /// Create an asynchronous handler for HTML event `keydown`.
        let inline keydown (callback: KeyboardEventArgs -> Task) : Attr =
            attr.task.callback<KeyboardEventArgs> "onkeydown" callback
        /// Create an asynchronous handler for HTML event `keyup`.
        let inline keyup (callback: KeyboardEventArgs -> Task) : Attr =
            attr.task.callback<KeyboardEventArgs> "onkeyup" callback
        /// Create an asynchronous handler for HTML event `keypress`.
        let inline keypress (callback: KeyboardEventArgs -> Task) : Attr =
            attr.task.callback<KeyboardEventArgs> "onkeypress" callback
        /// Create an asynchronous handler for HTML event `change`.
        let inline change (callback: ChangeEventArgs -> Task) : Attr =
            attr.task.callback<ChangeEventArgs> "onchange" callback
        /// Create an asynchronous handler for HTML event `input`.
        let inline input (callback: ChangeEventArgs -> Task) : Attr =
            attr.task.callback<ChangeEventArgs> "oninput" callback
        /// Create an asynchronous handler for HTML event `invalid`.
        let inline invalid (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "oninvalid" callback
        /// Create an asynchronous handler for HTML event `reset`.
        let inline reset (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onreset" callback
        /// Create an asynchronous handler for HTML event `select`.
        let inline select (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onselect" callback
        /// Create an asynchronous handler for HTML event `selectstart`.
        let inline selectstart (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onselectstart" callback
        /// Create an asynchronous handler for HTML event `selectionchange`.
        let inline selectionchange (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onselectionchange" callback
        /// Create an asynchronous handler for HTML event `submit`.
        let inline submit (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onsubmit" callback
        /// Create an asynchronous handler for HTML event `beforecopy`.
        let inline beforecopy (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onbeforecopy" callback
        /// Create an asynchronous handler for HTML event `beforecut`.
        let inline beforecut (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onbeforecut" callback
        /// Create an asynchronous handler for HTML event `beforepaste`.
        let inline beforepaste (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onbeforepaste" callback
        /// Create an asynchronous handler for HTML event `copy`.
        let inline copy (callback: ClipboardEventArgs -> Task) : Attr =
            attr.task.callback<ClipboardEventArgs> "oncopy" callback
        /// Create an asynchronous handler for HTML event `cut`.
        let inline cut (callback: ClipboardEventArgs -> Task) : Attr =
            attr.task.callback<ClipboardEventArgs> "oncut" callback
        /// Create an asynchronous handler for HTML event `paste`.
        let inline paste (callback: ClipboardEventArgs -> Task) : Attr =
            attr.task.callback<ClipboardEventArgs> "onpaste" callback
        /// Create an asynchronous handler for HTML event `touchcancel`.
        let inline touchcancel (callback: TouchEventArgs -> Task) : Attr =
            attr.task.callback<TouchEventArgs> "ontouchcancel" callback
        /// Create an asynchronous handler for HTML event `touchend`.
        let inline touchend (callback: TouchEventArgs -> Task) : Attr =
            attr.task.callback<TouchEventArgs> "ontouchend" callback
        /// Create an asynchronous handler for HTML event `touchmove`.
        let inline touchmove (callback: TouchEventArgs -> Task) : Attr =
            attr.task.callback<TouchEventArgs> "ontouchmove" callback
        /// Create an asynchronous handler for HTML event `touchstart`.
        let inline touchstart (callback: TouchEventArgs -> Task) : Attr =
            attr.task.callback<TouchEventArgs> "ontouchstart" callback
        /// Create an asynchronous handler for HTML event `touchenter`.
        let inline touchenter (callback: TouchEventArgs -> Task) : Attr =
            attr.task.callback<TouchEventArgs> "ontouchenter" callback
        /// Create an asynchronous handler for HTML event `touchleave`.
        let inline touchleave (callback: TouchEventArgs -> Task) : Attr =
            attr.task.callback<TouchEventArgs> "ontouchleave" callback
        /// Create an asynchronous handler for HTML event `pointercapture`.
        let inline pointercapture (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointercapture" callback
        /// Create an asynchronous handler for HTML event `lostpointercapture`.
        let inline lostpointercapture (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onlostpointercapture" callback
        /// Create an asynchronous handler for HTML event `pointercancel`.
        let inline pointercancel (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointercancel" callback
        /// Create an asynchronous handler for HTML event `pointerdown`.
        let inline pointerdown (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointerdown" callback
        /// Create an asynchronous handler for HTML event `pointerenter`.
        let inline pointerenter (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointerenter" callback
        /// Create an asynchronous handler for HTML event `pointerleave`.
        let inline pointerleave (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointerleave" callback
        /// Create an asynchronous handler for HTML event `pointermove`.
        let inline pointermove (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointermove" callback
        /// Create an asynchronous handler for HTML event `pointerout`.
        let inline pointerout (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointerout" callback
        /// Create an asynchronous handler for HTML event `pointerover`.
        let inline pointerover (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointerover" callback
        /// Create an asynchronous handler for HTML event `pointerup`.
        let inline pointerup (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointerup" callback
        /// Create an asynchronous handler for HTML event `canplay`.
        let inline canplay (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "oncanplay" callback
        /// Create an asynchronous handler for HTML event `canplaythrough`.
        let inline canplaythrough (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "oncanplaythrough" callback
        /// Create an asynchronous handler for HTML event `cuechange`.
        let inline cuechange (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "oncuechange" callback
        /// Create an asynchronous handler for HTML event `durationchange`.
        let inline durationchange (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "ondurationchange" callback
        /// Create an asynchronous handler for HTML event `emptied`.
        let inline emptied (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onemptied" callback
        /// Create an asynchronous handler for HTML event `pause`.
        let inline pause (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onpause" callback
        /// Create an asynchronous handler for HTML event `play`.
        let inline play (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onplay" callback
        /// Create an asynchronous handler for HTML event `playing`.
        let inline playing (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onplaying" callback
        /// Create an asynchronous handler for HTML event `ratechange`.
        let inline ratechange (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onratechange" callback
        /// Create an asynchronous handler for HTML event `seeked`.
        let inline seeked (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onseeked" callback
        /// Create an asynchronous handler for HTML event `seeking`.
        let inline seeking (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onseeking" callback
        /// Create an asynchronous handler for HTML event `stalled`.
        let inline stalled (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onstalled" callback
        /// Create an asynchronous handler for HTML event `stop`.
        let inline stop (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onstop" callback
        /// Create an asynchronous handler for HTML event `suspend`.
        let inline suspend (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onsuspend" callback
        /// Create an asynchronous handler for HTML event `timeupdate`.
        let inline timeupdate (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "ontimeupdate" callback
        /// Create an asynchronous handler for HTML event `volumechange`.
        let inline volumechange (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onvolumechange" callback
        /// Create an asynchronous handler for HTML event `waiting`.
        let inline waiting (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onwaiting" callback
        /// Create an asynchronous handler for HTML event `loadstart`.
        let inline loadstart (callback: ProgressEventArgs -> Task) : Attr =
            attr.task.callback<ProgressEventArgs> "onloadstart" callback
        /// Create an asynchronous handler for HTML event `timeout`.
        let inline timeout (callback: ProgressEventArgs -> Task) : Attr =
            attr.task.callback<ProgressEventArgs> "ontimeout" callback
        /// Create an asynchronous handler for HTML event `abort`.
        let inline abort (callback: ProgressEventArgs -> Task) : Attr =
            attr.task.callback<ProgressEventArgs> "onabort" callback
        /// Create an asynchronous handler for HTML event `load`.
        let inline load (callback: ProgressEventArgs -> Task) : Attr =
            attr.task.callback<ProgressEventArgs> "onload" callback
        /// Create an asynchronous handler for HTML event `loadend`.
        let inline loadend (callback: ProgressEventArgs -> Task) : Attr =
            attr.task.callback<ProgressEventArgs> "onloadend" callback
        /// Create an asynchronous handler for HTML event `progress`.
        let inline progress (callback: ProgressEventArgs -> Task) : Attr =
            attr.task.callback<ProgressEventArgs> "onprogress" callback
        /// Create an asynchronous handler for HTML event `error`.
        let inline error (callback: ProgressEventArgs -> Task) : Attr =
            attr.task.callback<ProgressEventArgs> "onerror" callback
        /// Create an asynchronous handler for HTML event `activate`.
        let inline activate (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onactivate" callback
        /// Create an asynchronous handler for HTML event `beforeactivate`.
        let inline beforeactivate (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onbeforeactivate" callback
        /// Create an asynchronous handler for HTML event `beforedeactivate`.
        let inline beforedeactivate (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onbeforedeactivate" callback
        /// Create an asynchronous handler for HTML event `deactivate`.
        let inline deactivate (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "ondeactivate" callback
        /// Create an asynchronous handler for HTML event `ended`.
        let inline ended (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onended" callback
        /// Create an asynchronous handler for HTML event `fullscreenchange`.
        let inline fullscreenchange (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onfullscreenchange" callback
        /// Create an asynchronous handler for HTML event `fullscreenerror`.
        let inline fullscreenerror (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onfullscreenerror" callback
        /// Create an asynchronous handler for HTML event `loadeddata`.
        let inline loadeddata (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onloadeddata" callback
        /// Create an asynchronous handler for HTML event `loadedmetadata`.
        let inline loadedmetadata (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onloadedmetadata" callback
        /// Create an asynchronous handler for HTML event `pointerlockchange`.
        let inline pointerlockchange (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onpointerlockchange" callback
        /// Create an asynchronous handler for HTML event `pointerlockerror`.
        let inline pointerlockerror (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onpointerlockerror" callback
        /// Create an asynchronous handler for HTML event `readystatechange`.
        let inline readystatechange (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onreadystatechange" callback
        /// Create an asynchronous handler for HTML event `scroll`.
        let inline scroll (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onscroll" callback
// END TASKEVENTS

/// Two-way binding for HTML input elements.
module bind =


    /// [omit]
    [<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>]
    let inline binder< ^T, ^F, ^B, ^O
                        when ^F : (static member CreateBinder : EventCallbackFactory * obj * Action< ^T> * ^T * CultureInfo -> EventCallback<ChangeEventArgs>)
                        and ^B : (static member FormatValue : ^T * CultureInfo -> ^O)>
            (eventName: string) (valueAttribute: string) (currentValue: ^T) (callback: ^T -> unit) cultureInfo =
        Attr(fun receiver builder matchCache sequence ->
            builder.AddAttribute(sequence, valueAttribute, (^B : (static member FormatValue : ^T * CultureInfo -> ^O)(currentValue, cultureInfo)))
            builder.AddAttribute(sequence + 1, eventName,
                (^F : (static member CreateBinder : EventCallbackFactory * obj * Action< ^T> * ^T * CultureInfo -> EventCallback<ChangeEventArgs>)
                    (EventCallback.Factory, receiver, Action<_> callback, currentValue, cultureInfo)))
            sequence + 2)

    /// Bind a boolean to the value of a checkbox.
    let inline ``checked`` value callback = binder<bool, EventCallbackFactoryBinderExtensions, BindConverter, bool> "onchange" "checked" value callback null

    /// Bind to the value of an input.
    /// The value is updated on the oninput event.
    module input =

        /// Bind a string to the value of an input.
        /// The value is updated on the oninput event.
        let inline string value callback = binder<string, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback null

        /// Bind an integer to the value of an input.
        /// The value is updated on the oninput event.
        let inline int value callback = binder<int, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback null

        /// Bind an int64 to the value of an input.
        /// The value is updated on the oninput event.
        let inline int64 value callback = binder<int64, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback null

        /// Bind a float to the value of an input.
        /// The value is updated on the oninput event.
        let inline float value callback = binder<float, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback null

        /// Bind a float32 to the value of an input.
        /// The value is updated on the oninput event.
        let inline float32 value callback = binder<float32, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback null

        /// Bind a decimal to the value of an input.
        /// The value is updated on the oninput event.
        let inline decimal value callback = binder<decimal, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback null

        /// Bind a DateTime to the value of an input.
        /// The value is updated on the oninput event.
        let inline dateTime value callback = binder<DateTime, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback null

        /// Bind a DateTimeOffset to the value of an input.
        /// The value is updated on the oninput event.
        let inline dateTimeOffset value callback = binder<DateTimeOffset, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback null

    /// Bind to the value of an input.
    /// The value is updated on the onchange event.
    module change =

        /// Bind a string to the value of an input.
        /// The value is updated on the onchange event.
        let inline string value callback = binder<string, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback null

        /// Bind an integer to the value of an input.
        /// The value is updated on the onchange event.
        let inline int value callback = binder<int, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback null

        /// Bind an int64 to the value of an input.
        /// The value is updated on the onchange event.
        let inline int64 value callback = binder<int64, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback null

        /// Bind a float to the value of an input.
        /// The value is updated on the onchange event.
        let inline float value callback = binder<float, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback null

        /// Bind a float32 to the value of an input.
        /// The value is updated on the onchange event.
        let inline float32 value callback = binder<float32, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback null

        /// Bind a decimal to the value of an input.
        /// The value is updated on the onchange event.
        let inline decimal value callback = binder<decimal, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback null

        /// Bind a DateTime to the value of an input.
        /// The value is updated on the onchange event.
        let inline dateTime value callback = binder<DateTime, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback null

        /// Bind a DateTimeOffset to the value of an input.
        /// The value is updated on the onchange event.
        let inline dateTimeOffset value callback = binder<DateTimeOffset, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback null

    /// Bind to the value of an input and convert using the given CultureInfo.
    module withCulture =

        /// Bind to the value of an input.
        /// The value is updated on the oninput event.
        module input =

            /// Bind a string to the value of an input.
            /// The value is updated on the oninput event.
            let inline string culture value callback = binder<string, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback culture

            /// Bind an integer to the value of an input.
            /// The value is updated on the oninput event.
            let inline int culture value callback = binder<int, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback culture

            /// Bind an int64 to the value of an input.
            /// The value is updated on the oninput event.
            let inline int64 culture value callback = binder<int64, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback culture

            /// Bind a float to the value of an input.
            /// The value is updated on the oninput event.
            let inline float culture value callback = binder<float, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback culture

            /// Bind a float32 to the value of an input.
            /// The value is updated on the oninput event.
            let inline float32 culture value callback = binder<float32, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback culture

            /// Bind a decimal to the value of an input.
            /// The value is updated on the oninput event.
            let inline decimal culture value callback = binder<decimal, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback culture

            /// Bind a DateTime to the value of an input.
            /// The value is updated on the oninput event.
            let inline dateTime culture value callback = binder<DateTime, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback culture

            /// Bind a DateTimeOffset to the value of an input.
            /// The value is updated on the oninput event.
            let inline dateTimeOffset culture value callback = binder<DateTimeOffset, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback culture

        /// Bind to the value of an input.
        /// The value is updated on the onchange event.
        module change =

            /// Bind a string to the value of an input.
            /// The value is updated on the onchange event.
            let inline string culture value callback = binder<string, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback culture

            /// Bind an integer to the value of an input.
            /// The value is updated on the onchange event.
            let inline int culture value callback = binder<int, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback culture

            /// Bind an int64 to the value of an input.
            /// The value is updated on the onchange event.
            let inline int64 culture value callback = binder<int64, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback culture

            /// Bind a float to the value of an input.
            /// The value is updated on the onchange event.
            let inline float culture value callback = binder<float, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback culture

            /// Bind a float32 to the value of an input.
            /// The value is updated on the onchange event.
            let inline float32 culture value callback = binder<float32, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback culture

            /// Bind a decimal to the value of an input.
            /// The value is updated on the onchange event.
            let inline decimal culture value callback = binder<decimal, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback culture

            /// Bind a DateTime to the value of an input.
            /// The value is updated on the onchange event.
            let inline dateTime culture value callback = binder<DateTime, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback culture

            /// Bind a DateTimeOffset to the value of an input.
            /// The value is updated on the onchange event.
            let inline dateTimeOffset culture value callback = binder<DateTimeOffset, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback culture

module virtualize =
    open System.Collections.Generic
    open Microsoft.AspNetCore.Components.Web.Virtualization

    let inline items (items: IReadOnlyCollection<'item>) =
        VirtualizeItemsDeclaration<'item>(fun b i ->
            b.AddAttribute(i, "Items", Virtualize.Internals.Collection<'item> items)
            i + 1)

    let inline itemsProvider ([<InlineIfLambda>] itemsProvider: ItemsProviderRequest -> ValueTask<ItemsProviderResult<'item>>) =
        VirtualizeItemsDeclaration<'item>(fun b i ->
            b.AddAttribute(i, "ItemsProvider", ItemsProviderDelegate<'item> itemsProvider)
            i + 1)

    let inline comp<'item> = VirtualizeBuilder<'item>()

    let inline placeholder ([<InlineIfLambda>] v: PlaceholderContext -> Node) =
        attr.fragmentWith "Placeholder" v

    let inline itemSize (v: single) =
        "ItemSize" => v

    let inline overscanCount (v: int) =
        "OverscanCount" => v
