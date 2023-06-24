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
open Bolero.Builders

/// <summary>Computation expression to create an Attr that is the concatenation of multiple attributes.</summary>
/// <category>HTML attributes</category>
let attrs = AttrBuilder()

/// <summary>
/// Computation expression to create a Node that is the concatenation of multiple elements and components.
/// </summary>
/// <category>HTML elements</category>
let concat = ConcatBuilder()

/// <summary>Create an empty HTML fragment.</summary>
/// <category>HTML elements</category>
let inline empty() = Node.Empty()

/// <summary>Create an HTML text node.</summary>
/// <param name="str">The text.</param>
/// <category>HTML elements</category>
/// <remarks>
/// When inside an HTML computation expression, a text node can be inserted by simply yielding a string,
/// without having to wrap it in <see cref="M:text" />.
/// <code lang="fsharp">
/// let helloWorld =
///     div {
///         text "Hello, "  // `text` can be used to create a text node.
///         "world!"        // But inside an element, a simple string is equivalent.
///     }
/// </code>
/// </remarks>
let inline text str = Node.Text str

/// <summary>Create a raw HTML node.</summary>
/// <param name="str">The raw HTML string.</param>
/// <category>HTML elements</category>
let inline rawHtml str = Node.RawHtml str

/// <summary>Create an HTML text node using formatting.</summary>
/// <param name="format">The <see cref="M:Microsoft.FSharp.Core.ExtraTopLevelOperators.printf`1" />-style format string.</param>
/// <category>HTML elements</category>
let inline textf format = Printf.kprintf text format

/// <summary>Create an HTML element.</summary>
/// <param name="name">The name of the element.</param>
/// <returns>A computation expression builder to insert attributes and children in the element.</returns>
/// <example>
/// <code lang="fsharp">
/// let helloWorld =
///     elt "div" {
///         "id" =&gt; "hello-world"
///         "Hello, world!"
///     }
/// </code>
/// </example>
/// <remarks>
/// Builders such as <see cref="P:div" /> also exist for all standard HTML elements.
/// In general, it is only useful to call <see cref="M:elt" /> to create a non-standard element.
/// </remarks>
/// <category>HTML elements</category>
let inline elt name = ElementBuilder name

/// <summary>Create an HTML attribute or a component parameter.</summary>
/// <param name="name">The name of the attribute or parameter.</param>
/// <param name="value">The value of the attribute or parameter.</param>
/// <returns>An HTML attribute or component parameter.</returns>
/// <example>
/// <code lang="fsharp">
/// let helloWorld =
///     div {
///         "id" =&gt; "hello-world"
///         "Hello, world!"
///     }
/// </code>
/// </example>
/// <remarks>
/// Functions such as <see cref="attr.id" /> also exist for all standard HTML attributes.
/// In general, it is only useful to call <see cref="M:op_EqualsGreater" /> to create
/// a non-standard attribute or a component parameter.
/// </remarks>
/// <category>HTML elements</category>
let inline (=>) name value = Attr.Make name value

/// <summary>
/// Create a conditional fragment, ie. a fragment whose structure depends on a value.
/// </summary>
/// <param name="matching">
/// The value on which the structure of the fragment depends.
/// Must be either a boolean or an F# union.
/// </param>
/// <param name="mkNode">
/// The function that creates the node. If <paramref name="matching" /> is a union,
/// then <paramref name="mkNode" /> must only match on the case, without nested patterns.
/// </param>
/// <returns>The generated HTML fragment wrapped in a way that Blazor can render.</returns>
/// <example>
/// This function is necessary because Blazor cannot properly render HTML whose structure changes
/// depending on some runtime state. For example, the following would fail at runtime:
/// <code lang="fsharp">
/// let failing (isBold: bool) =
///     div {
///         if isBold then
///             b { text "Hello, world!" }
///         else
///             text "Hello, world!"
///     }
/// </code>
/// Instead, <see cref="M:cond" /> must be used:
/// <code lang="fsharp">
/// let succeeding (isBold: bool) =
///     div {
///         cond isBold &lt;| function
///             | true -> b { text "Hello, world!" }
///             | false -> text "Hello, world!"
///     }
/// </code>
/// </example>
/// <category>HTML elements</category>
let inline cond<'T> (matching: 'T) (mkNode: 'T -> Node) =
    Node.Match matching (mkNode matching)

/// <summary>Create a HTML fragment that concatenates nodes for each item in a sequence.</summary>
/// <typeparam name="T">The type of items to render into HTML fragments.</typeparam>
/// <param name="items">The sequence of items to render into HTML fragments.</param>
/// <param name="mkNode">The function that renders one item into a HTML fragment.</param>
/// <category>HTML elements</category>
let inline forEach<'T> (items: seq<'T>) (mkNode: 'T -> Node) =
    Node.ForEach items mkNode

/// <summary>Wrap a Blazor RenderFragment in a Bolero Node.</summary>
/// <param name="fragment">The Blazor RenderFragment.</param>
/// <returns>A Bolero Node representing the Blazor RenderFragment.</returns>
let inline fragment (fragment: RenderFragment) =
    Node.Fragment fragment

/// <summary>Computation expression builder to create a Blazor component.</summary>
/// <typeparam name="T">The Blazor component type.</typeparam>
/// <example>
/// <code lang="fsharp">
/// open Microsoft.AspNetCore.Components
///
/// let homeLink =
///     comp&lt;Routing.NavLink&gt; {
///         "Match" => Routing.NavLinkMatch.Prefix
///         attr.href "/"
///         "Go to the home page"
///     }
/// </code>
/// </example>
/// <category>Components</category>
let inline comp<'T when 'T :> IComponent> = ComponentBuilder<'T>()

/// <summary>Computation expression builder to create an Elmish component.</summary>
/// <typeparam name="T">The Elmish component type.</typeparam>
/// <typeparam name="model">The Elmish model type.</typeparam>
/// <typeparam name="msg">The Elmish message type.</typeparam>
/// <param name="model">The Elmish model.</param>
/// <param name="dispatch">The Elmish dispatch function.</param>
/// <returns>A computation expression builder to insert attributes and children in the component.</returns>
/// <category>Components</category>
let inline ecomp<'T, 'model, 'msg when 'T :> ElmishComponent<'model, 'msg>>
        (model: 'model) (dispatch: Elmish.Dispatch<'msg>) =
    ComponentWithAttrsAndNoChildrenBuilder<'T>(attrs {
        "Model" => model
        "Dispatch" => dispatch
    })

/// <summary>Create a fragment with a lazily rendered view function.</summary>
/// <typeparam name="model">The model passed to the view function.</typeparam>
/// <param name="model">The model passed to the view function.</param>
/// <param name="viewFunction">The view function.</param>
/// <category>Components</category>
let inline lazyComp ([<InlineIfLambda>] viewFunction: 'model -> Node) (model: 'model) =
    let viewFunction' : 'model -> Elmish.Dispatch<_> -> Node = fun m _ -> viewFunction m
    comp<LazyComponent<'model, obj>> {
        "Model" => model
        "ViewFunction" => viewFunction'
    }

/// <summary>Create a fragment with a lazily rendered view function and a custom equality.</summary>
/// <typeparam name="model">The model passed to the view function.</typeparam>
/// <param name="equal">The equality function used to determine if the view needs re-rendering.</param>
/// <param name="viewFunction">The view function.</param>
/// <param name="model">The model passed to the view function.</param>
/// <category>Components</category>
let inline lazyCompWith (equal: 'model -> 'model -> bool) ([<InlineIfLambda>] viewFunction: 'model -> Node) (model: 'model) =
    let viewFunction' : 'model -> Elmish.Dispatch<_> -> Node = fun m _ -> viewFunction m
    comp<LazyComponent<'model, obj>> {
        "Model" => model
        "ViewFunction" => viewFunction'
        "Equal" => equal
    }

/// <summary>Create a fragment with a lazily rendered view function.</summary>
/// <typeparam name="model">The model passed to the view function.</typeparam>
/// <typeparam name="msg">The Elmish message type.</typeparam>
/// <param name="viewFunction">The view function.</param>
/// <param name="model">The model passed to the view function.</param>
/// <param name="dispatch">The Elmish dispatch function.</param>
/// <category>Components</category>
let inline lazyComp2 (viewFunction: 'model -> Elmish.Dispatch<'msg> -> Node) (model: 'model) (dispatch: Elmish.Dispatch<'msg>) =
    comp<LazyComponent<'model, 'msg>> {
        "Model" => model
        "Dispatch" => dispatch
        "ViewFunction" => viewFunction
    }

/// <summary>Create a fragment with a lazily rendered view function and a custom equality.</summary>
/// <typeparam name="model">The model passed to the view function.</typeparam>
/// <typeparam name="msg">The Elmish message type.</typeparam>
/// <param name="equal">The equality function used to determine if the view needs re-rendering.</param>
/// <param name="viewFunction">The view function.</param>
/// <param name="model">The model passed to the view function.</param>
/// <param name="dispatch">The Elmish dispatch function.</param>
/// <category>Components</category>
let inline lazyComp2With (equal: 'model -> 'model -> bool) (viewFunction: 'model -> Elmish.Dispatch<'msg> -> Node) (model: 'model) (dispatch: Elmish.Dispatch<'msg>) =
    comp<LazyComponent<'model, 'msg>> {
        "Model" => model
        "Dispatch" => dispatch
        "ViewFunction" => viewFunction
        "Equal" => equal
    }

/// <summary>Create a fragment with a lazily rendered view function and two model values.</summary>
/// <typeparam name="model1">The first model passed to the view function.</typeparam>
/// <typeparam name="model2">The second model passed to the view function.</typeparam>
/// <typeparam name="msg">The Elmish message type.</typeparam>
/// <param name="viewFunction">The view function.</param>
/// <param name="model1">The first model passed to the view function.</param>
/// <param name="model2">The second model passed to the view function.</param>
/// <param name="dispatch">The Elmish dispatch function.</param>
/// <category>Components</category>
let inline lazyComp3 (viewFunction: ('model1 * 'model2') -> Elmish.Dispatch<'msg> -> Node) (model1: 'model1) (model2: 'model2) (dispatch: Elmish.Dispatch<'msg>) =
    comp<LazyComponent<'model1 * 'model2, 'msg>>{
        "Model" => (model1, model2)
        "Dispatch" => dispatch
        "ViewFunction" => viewFunction
    }

/// <summary>Create a fragment with a lazily rendered view function, two model values and a custom equality.</summary>
/// <typeparam name="model1">The first model passed to the view function.</typeparam>
/// <typeparam name="model2">The second model passed to the view function.</typeparam>
/// <typeparam name="msg">The Elmish message type.</typeparam>
/// <param name="equal">The equality function used to determine if the view needs re-rendering.</param>
/// <param name="viewFunction">The view function.</param>
/// <param name="model1">The first model passed to the view function.</param>
/// <param name="model2">The second model passed to the view function.</param>
/// <param name="dispatch">The Elmish dispatch function.</param>
/// <category>Components</category>
let inline lazyComp3With (equal: ('model1 * 'model2) -> ('model1 * 'model2) -> bool) (viewFunction: ('model1 * 'model2') -> Elmish.Dispatch<'msg> -> Node) (model1: 'model1) (model2: 'model2) (dispatch: Elmish.Dispatch<'msg>) =
    comp<LazyComponent<'model1 * 'model2, 'msg>> {
        "Model" => (model1, model2)
        "Dispatch" => dispatch
        "ViewFunction" => viewFunction
        "Equal" => equal
    }

/// <summary>Create a fragment with a lazily rendered view function and custom equality on model field.</summary>
/// <typeparam name="model">The model passed to the view function.</typeparam>
/// <param name="equal">The function used to extract the equality key that determines if the view needs re-rendering.</param>
/// <param name="viewFunction">The view function.</param>
/// <param name="model">The model passed to the view function.</param>
/// <category>Components</category>
let inline lazyCompBy (equal: 'model -> 'a) (viewFunction: 'model -> Node) (model: 'model) =
    let equal' model1 model2 = (equal model1) = (equal model2)
    lazyCompWith equal' viewFunction model

/// <summary>Create a fragment with a lazily rendered view function and custom equality on model field.</summary>
/// <typeparam name="model">The model passed to the view function.</typeparam>
/// <typeparam name="msg">The Elmish message type.</typeparam>
/// <param name="equal">The function used to extract the equality key that determines if the view needs re-rendering.</param>
/// <param name="viewFunction">The view function.</param>
/// <param name="model">The model passed to the view function.</param>
/// <param name="dispatch">The Elmish dispatch function.</param>
/// <category>Components</category>
let inline lazyComp2By (equal: 'model -> 'a) (viewFunction: 'model -> Elmish.Dispatch<'msg> -> Node) (model: 'model) (dispatch: Elmish.Dispatch<'msg>) =
    let equal' model1 model2 = (equal model1) = (equal model2)
    lazyComp2With equal' viewFunction model dispatch

/// <summary>Create a fragment with a lazily rendered view function, two model values and custom equality on model field.</summary>
/// <typeparam name="model1">The first model passed to the view function.</typeparam>
/// <typeparam name="model2">The second model passed to the view function.</typeparam>
/// <typeparam name="msg">The Elmish message type.</typeparam>
/// <param name="equal">The function used to extract the equality key that determines if the view needs re-rendering.</param>
/// <param name="viewFunction">The view function.</param>
/// <param name="model1">The first model passed to the view function.</param>
/// <param name="model2">The second model passed to the view function.</param>
/// <param name="dispatch">The Elmish dispatch function.</param>
/// <category>Components</category>
let inline lazyComp3By (equal: ('model1 * 'model2) -> 'a) (viewFunction: ('model1 * 'model2) -> Elmish.Dispatch<'msg> -> Node) (model1: 'model1) (model2: 'model2) (dispatch: Elmish.Dispatch<'msg>) =
    let equal' (model11, model12) (model21, model22) = (equal (model11, model12)) = (equal (model21, model22))
    lazyComp3With equal' viewFunction model1 model2 dispatch

/// <summary>
/// Computation expression builder to create a navigation link which toggles its <c>active</c> class
/// based on whether the current URI matches its <c>href</c>.
/// </summary>
/// <param name="match">The URL match behavior.</param>
/// <example>
/// <code lang="fsharp">
/// open Microsoft.AspNetCore.Components.Routing
///
/// let homeLink =
///     navLink NavLinkMatch.All {
///         attr.href "/home"
///         "Go to home"
///     }
/// </code>
/// </example>
/// <category>Components</category>
let inline navLink (``match``: Routing.NavLinkMatch) =
    ComponentWithAttrsBuilder<Routing.NavLink>(attrs {
        "Match" => ``match``
    })

// BEGIN TAGS
/// <summary>Computation expression to create an HTML <c>&lt;a&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let a : ElementBuilder = elt "a"

/// <summary>Computation expression to create an HTML <c>&lt;abbr&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let abbr : ElementBuilder = elt "abbr"

/// <summary>Computation expression to create an HTML <c>&lt;acronym&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let acronym : ElementBuilder = elt "acronym"

/// <summary>Computation expression to create an HTML <c>&lt;address&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let address : ElementBuilder = elt "address"

/// <summary>Computation expression to create an HTML <c>&lt;applet&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let applet : ElementBuilder = elt "applet"

/// <summary>Computation expression to create an HTML <c>&lt;area&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let area : ElementBuilder = elt "area"

/// <summary>Computation expression to create an HTML <c>&lt;article&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let article : ElementBuilder = elt "article"

/// <summary>Computation expression to create an HTML <c>&lt;aside&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let aside : ElementBuilder = elt "aside"

/// <summary>Computation expression to create an HTML <c>&lt;audio&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let audio : ElementBuilder = elt "audio"

/// <summary>Computation expression to create an HTML <c>&lt;b&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let b : ElementBuilder = elt "b"

/// <summary>Computation expression to create an HTML <c>&lt;base&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let ``base`` : ElementBuilder = elt "base"

/// <summary>Computation expression to create an HTML <c>&lt;basefont&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let basefont : ElementBuilder = elt "basefont"

/// <summary>Computation expression to create an HTML <c>&lt;bdi&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let bdi : ElementBuilder = elt "bdi"

/// <summary>Computation expression to create an HTML <c>&lt;bdo&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let bdo : ElementBuilder = elt "bdo"

/// <summary>Computation expression to create an HTML <c>&lt;big&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let big : ElementBuilder = elt "big"

/// <summary>Computation expression to create an HTML <c>&lt;blockquote&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let blockquote : ElementBuilder = elt "blockquote"

/// <summary>Computation expression to create an HTML <c>&lt;body&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let body : ElementBuilder = elt "body"

/// <summary>Computation expression to create an HTML <c>&lt;br&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let br : ElementBuilder = elt "br"

/// <summary>Computation expression to create an HTML <c>&lt;button&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let button : ElementBuilder = elt "button"

/// <summary>Computation expression to create an HTML <c>&lt;canvas&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let canvas : ElementBuilder = elt "canvas"

/// <summary>Computation expression to create an HTML <c>&lt;caption&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let caption : ElementBuilder = elt "caption"

/// <summary>Computation expression to create an HTML <c>&lt;center&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let center : ElementBuilder = elt "center"

/// <summary>Computation expression to create an HTML <c>&lt;cite&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let cite : ElementBuilder = elt "cite"

/// <summary>Computation expression to create an HTML <c>&lt;code&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let code : ElementBuilder = elt "code"

/// <summary>Computation expression to create an HTML <c>&lt;col&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let col : ElementBuilder = elt "col"

/// <summary>Computation expression to create an HTML <c>&lt;colgroup&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let colgroup : ElementBuilder = elt "colgroup"

/// <summary>Computation expression to create an HTML <c>&lt;content&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let content : ElementBuilder = elt "content"

/// <summary>Computation expression to create an HTML <c>&lt;data&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let data : ElementBuilder = elt "data"

/// <summary>Computation expression to create an HTML <c>&lt;datalist&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let datalist : ElementBuilder = elt "datalist"

/// <summary>Computation expression to create an HTML <c>&lt;dd&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let dd : ElementBuilder = elt "dd"

/// <summary>Computation expression to create an HTML <c>&lt;del&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let del : ElementBuilder = elt "del"

/// <summary>Computation expression to create an HTML <c>&lt;details&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let details : ElementBuilder = elt "details"

/// <summary>Computation expression to create an HTML <c>&lt;dfn&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let dfn : ElementBuilder = elt "dfn"

/// <summary>Computation expression to create an HTML <c>&lt;dialog&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let dialog : ElementBuilder = elt "dialog"

/// <summary>Computation expression to create an HTML <c>&lt;dir&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let dir : ElementBuilder = elt "dir"

/// <summary>Computation expression to create an HTML <c>&lt;div&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let div : ElementBuilder = elt "div"

/// <summary>Computation expression to create an HTML <c>&lt;dl&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let dl : ElementBuilder = elt "dl"

/// <summary>Computation expression to create an HTML <c>&lt;dt&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let dt : ElementBuilder = elt "dt"

/// <summary>Computation expression to create an HTML <c>&lt;element&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let element : ElementBuilder = elt "element"

/// <summary>Computation expression to create an HTML <c>&lt;em&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let em : ElementBuilder = elt "em"

/// <summary>Computation expression to create an HTML <c>&lt;embed&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let embed : ElementBuilder = elt "embed"

/// <summary>Computation expression to create an HTML <c>&lt;fieldset&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let fieldset : ElementBuilder = elt "fieldset"

/// <summary>Computation expression to create an HTML <c>&lt;figcaption&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let figcaption : ElementBuilder = elt "figcaption"

/// <summary>Computation expression to create an HTML <c>&lt;figure&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let figure : ElementBuilder = elt "figure"

/// <summary>Computation expression to create an HTML <c>&lt;font&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let font : ElementBuilder = elt "font"

/// <summary>Computation expression to create an HTML <c>&lt;footer&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let footer : ElementBuilder = elt "footer"

/// <summary>Computation expression to create an HTML <c>&lt;form&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let form : ElementBuilder = elt "form"

/// <summary>Computation expression to create an HTML <c>&lt;frame&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let frame : ElementBuilder = elt "frame"

/// <summary>Computation expression to create an HTML <c>&lt;frameset&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let frameset : ElementBuilder = elt "frameset"

/// <summary>Computation expression to create an HTML <c>&lt;h1&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let h1 : ElementBuilder = elt "h1"

/// <summary>Computation expression to create an HTML <c>&lt;h2&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let h2 : ElementBuilder = elt "h2"

/// <summary>Computation expression to create an HTML <c>&lt;h3&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let h3 : ElementBuilder = elt "h3"

/// <summary>Computation expression to create an HTML <c>&lt;h4&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let h4 : ElementBuilder = elt "h4"

/// <summary>Computation expression to create an HTML <c>&lt;h5&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let h5 : ElementBuilder = elt "h5"

/// <summary>Computation expression to create an HTML <c>&lt;h6&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let h6 : ElementBuilder = elt "h6"

/// <summary>Computation expression to create an HTML <c>&lt;head&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let head : ElementBuilder = elt "head"

/// <summary>Computation expression to create an HTML <c>&lt;header&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let header : ElementBuilder = elt "header"

/// <summary>Computation expression to create an HTML <c>&lt;hr&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let hr : ElementBuilder = elt "hr"

/// <summary>Computation expression to create an HTML <c>&lt;html&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let html : ElementBuilder = elt "html"

/// <summary>Computation expression to create an HTML <c>&lt;i&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let i : ElementBuilder = elt "i"

/// <summary>Computation expression to create an HTML <c>&lt;iframe&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let iframe : ElementBuilder = elt "iframe"

/// <summary>Computation expression to create an HTML <c>&lt;img&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let img : ElementBuilder = elt "img"

/// <summary>Computation expression to create an HTML <c>&lt;input&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let input : ElementBuilder = elt "input"

/// <summary>Computation expression to create an HTML <c>&lt;ins&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let ins : ElementBuilder = elt "ins"

/// <summary>Computation expression to create an HTML <c>&lt;kbd&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let kbd : ElementBuilder = elt "kbd"

/// <summary>Computation expression to create an HTML <c>&lt;label&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let label : ElementBuilder = elt "label"

/// <summary>Computation expression to create an HTML <c>&lt;legend&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let legend : ElementBuilder = elt "legend"

/// <summary>Computation expression to create an HTML <c>&lt;li&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let li : ElementBuilder = elt "li"

/// <summary>Computation expression to create an HTML <c>&lt;link&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let link : ElementBuilder = elt "link"

/// <summary>Computation expression to create an HTML <c>&lt;main&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let main : ElementBuilder = elt "main"

/// <summary>Computation expression to create an HTML <c>&lt;map&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let map : ElementBuilder = elt "map"

/// <summary>Computation expression to create an HTML <c>&lt;mark&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let mark : ElementBuilder = elt "mark"

/// <summary>Computation expression to create an HTML <c>&lt;menu&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let menu : ElementBuilder = elt "menu"

/// <summary>Computation expression to create an HTML <c>&lt;menuitem&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let menuitem : ElementBuilder = elt "menuitem"

/// <summary>Computation expression to create an HTML <c>&lt;meta&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let meta : ElementBuilder = elt "meta"

/// <summary>Computation expression to create an HTML <c>&lt;meter&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let meter : ElementBuilder = elt "meter"

/// <summary>Computation expression to create an HTML <c>&lt;nav&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let nav : ElementBuilder = elt "nav"

/// <summary>Computation expression to create an HTML <c>&lt;noembed&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let noembed : ElementBuilder = elt "noembed"

/// <summary>Computation expression to create an HTML <c>&lt;noframes&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let noframes : ElementBuilder = elt "noframes"

/// <summary>Computation expression to create an HTML <c>&lt;noscript&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let noscript : ElementBuilder = elt "noscript"

/// <summary>Computation expression to create an HTML <c>&lt;object&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let object : ElementBuilder = elt "object"

/// <summary>Computation expression to create an HTML <c>&lt;ol&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let ol : ElementBuilder = elt "ol"

/// <summary>Computation expression to create an HTML <c>&lt;optgroup&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let optgroup : ElementBuilder = elt "optgroup"

/// <summary>Computation expression to create an HTML <c>&lt;option&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let option : ElementBuilder = elt "option"

/// <summary>Computation expression to create an HTML <c>&lt;output&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let output : ElementBuilder = elt "output"

/// <summary>Computation expression to create an HTML <c>&lt;p&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let p : ElementBuilder = elt "p"

/// <summary>Computation expression to create an HTML <c>&lt;param&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let param : ElementBuilder = elt "param"

/// <summary>Computation expression to create an HTML <c>&lt;picture&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let picture : ElementBuilder = elt "picture"

/// <summary>Computation expression to create an HTML <c>&lt;pre&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let pre : ElementBuilder = elt "pre"

/// <summary>Computation expression to create an HTML <c>&lt;progress&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let progress : ElementBuilder = elt "progress"

/// <summary>Computation expression to create an HTML <c>&lt;q&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let q : ElementBuilder = elt "q"

/// <summary>Computation expression to create an HTML <c>&lt;rb&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let rb : ElementBuilder = elt "rb"

/// <summary>Computation expression to create an HTML <c>&lt;rp&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let rp : ElementBuilder = elt "rp"

/// <summary>Computation expression to create an HTML <c>&lt;rt&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let rt : ElementBuilder = elt "rt"

/// <summary>Computation expression to create an HTML <c>&lt;rtc&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let rtc : ElementBuilder = elt "rtc"

/// <summary>Computation expression to create an HTML <c>&lt;ruby&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let ruby : ElementBuilder = elt "ruby"

/// <summary>Computation expression to create an HTML <c>&lt;s&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let s : ElementBuilder = elt "s"

/// <summary>Computation expression to create an HTML <c>&lt;samp&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let samp : ElementBuilder = elt "samp"

/// <summary>Computation expression to create an HTML <c>&lt;script&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let script : ElementBuilder = elt "script"

/// <summary>Computation expression to create an HTML <c>&lt;section&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let section : ElementBuilder = elt "section"

/// <summary>Computation expression to create an HTML <c>&lt;select&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let select : ElementBuilder = elt "select"

/// <summary>Computation expression to create an HTML <c>&lt;shadow&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let shadow : ElementBuilder = elt "shadow"

/// <summary>Computation expression to create an HTML <c>&lt;slot&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let slot : ElementBuilder = elt "slot"

/// <summary>Computation expression to create an HTML <c>&lt;small&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let small : ElementBuilder = elt "small"

/// <summary>Computation expression to create an HTML <c>&lt;source&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let source : ElementBuilder = elt "source"

/// <summary>Computation expression to create an HTML <c>&lt;span&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let span : ElementBuilder = elt "span"

/// <summary>Computation expression to create an HTML <c>&lt;strike&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let strike : ElementBuilder = elt "strike"

/// <summary>Computation expression to create an HTML <c>&lt;strong&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let strong : ElementBuilder = elt "strong"

/// <summary>Computation expression to create an HTML <c>&lt;style&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let style : ElementBuilder = elt "style"

/// <summary>Computation expression to create an HTML <c>&lt;sub&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let sub : ElementBuilder = elt "sub"

/// <summary>Computation expression to create an HTML <c>&lt;summary&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let summary : ElementBuilder = elt "summary"

/// <summary>Computation expression to create an HTML <c>&lt;sup&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let sup : ElementBuilder = elt "sup"

/// <summary>Computation expression to create an HTML <c>&lt;svg&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let svg : ElementBuilder = elt "svg"

/// <summary>Computation expression to create an HTML <c>&lt;table&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let table : ElementBuilder = elt "table"

/// <summary>Computation expression to create an HTML <c>&lt;tbody&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let tbody : ElementBuilder = elt "tbody"

/// <summary>Computation expression to create an HTML <c>&lt;td&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let td : ElementBuilder = elt "td"

/// <summary>Computation expression to create an HTML <c>&lt;template&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let template : ElementBuilder = elt "template"

/// <summary>Computation expression to create an HTML <c>&lt;textarea&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let textarea : ElementBuilder = elt "textarea"

/// <summary>Computation expression to create an HTML <c>&lt;tfoot&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let tfoot : ElementBuilder = elt "tfoot"

/// <summary>Computation expression to create an HTML <c>&lt;th&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let th : ElementBuilder = elt "th"

/// <summary>Computation expression to create an HTML <c>&lt;thead&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let thead : ElementBuilder = elt "thead"

/// <summary>Computation expression to create an HTML <c>&lt;time&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let time : ElementBuilder = elt "time"

/// <summary>Computation expression to create an HTML <c>&lt;title&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let title : ElementBuilder = elt "title"

/// <summary>Computation expression to create an HTML <c>&lt;tr&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let tr : ElementBuilder = elt "tr"

/// <summary>Computation expression to create an HTML <c>&lt;track&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let track : ElementBuilder = elt "track"

/// <summary>Computation expression to create an HTML <c>&lt;tt&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let tt : ElementBuilder = elt "tt"

/// <summary>Computation expression to create an HTML <c>&lt;u&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let u : ElementBuilder = elt "u"

/// <summary>Computation expression to create an HTML <c>&lt;ul&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let ul : ElementBuilder = elt "ul"

/// <summary>Computation expression to create an HTML <c>&lt;var&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let var : ElementBuilder = elt "var"

/// <summary>Computation expression to create an HTML <c>&lt;video&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let video : ElementBuilder = elt "video"

/// <summary>Computation expression to create an HTML <c>&lt;wbr&gt;</c> element.</summary>
/// <category>HTML tag names</category>
let wbr : ElementBuilder = elt "wbr"

// END TAGS

/// HTML attributes.
module attr =
    /// <summary>Create an HTML <c>class</c> attribute containing the given class names.</summary>
    [<Obsolete "Use attr.``class`` and String.concat. Multiple class attributes on the same element are not combined anymore.">]
    let inline classes (classes: list<string>) : Attr =
        Attr.Make "class" (String.concat " " classes)

    /// <summary>Bind an element or component reference.</summary>
    /// <param name="r">The reference.</param>
    /// <remarks>
    /// Must be inserted in an element or component computation expression, after attributes and before child content.
    /// </remarks>
    let inline ref (r: Ref<'T>) : RefContent =
        RefContent(fun _ b i ->
            r.Render(b, i))

    /// <summary>Bind an element or component reference.</summary>
    [<Obsolete "Use attr.ref, or yield the ref directly in the element or component builder.">]
    let inline bindRef (r: Ref<'T>) : RefContent =
        ref r

    /// <summary>Set an element's unique key among a sequence of similar elements.</summary>
    /// <param name="k">The unique key.</param>
    let inline key (k: obj) : Attr =
        Attr(fun _ b i ->
            b.SetKey(k)
            i)

    /// <summary>Create an empty attribute.</summary>
    let inline empty() = Attr.Empty()

    /// <summary>Create an HTML <c>aria-X</c> attribute.</summary>
    /// <param name="name">The attribute name, minus the <c>aria-</c> prefix.</param>
    /// <param name="v">The attribute value.</param>
    let inline aria name (v: obj) = ("aria-" + name) => v

    /// <summary>
    /// Create an attribute whose value is a callback.
    /// Use this function for Blazor component attributes of type <see cref="T:Microsoft.AspNetCore.Components.EventCallback`1" />.
    /// </summary>
    /// <param name="name">The name of the attribute (including "on" prefix for HTML event handlers).</param>
    /// <param name="value">The function to use as callback.</param>
    /// <remarks>For HTML event handlers, prefer functions from the module <see cref="on" />.</remarks>
    let inline callback<'T> (name: string) ([<InlineIfLambda>] value: 'T -> unit) =
        Attr(fun receiver builder sequence ->
            builder.AddAttribute<'T>(sequence, name, EventCallback.Factory.Create(receiver, Action<'T>(value)))
            sequence + 1)

    module async =

        /// <summary>
        /// Create an attribute whose value is an asynchronous callback.
        /// Use this function for Blazor component attributes of type <see cref="T:Microsoft.AspNetCore.Components.EventCallback`1" />.
        /// </summary>
        /// <param name="name">The name of the attribute (including "on" prefix for HTML event handlers).</param>
        /// <param name="value">The function to use as callback.</param>
        /// <remarks>For HTML event handlers, prefer functions from the module <see cref="on.async" />.</remarks>
        let inline callback<'T> (name: string) ([<InlineIfLambda>] value: 'T -> Async<unit>) =
            Attr(fun receiver builder sequence ->
                builder.AddAttribute<'T>(sequence, name, EventCallback.Factory.Create(receiver, Func<'T, Task>(fun x -> Async.StartImmediateAsTask (value x) :> Task)))
                sequence + 1)

    module task =

        /// <summary>
        /// Create an attribute whose value is an asynchronous callback.
        /// Use this function for Blazor component attributes of type <see cref="T:Microsoft.AspNetCore.Components.EventCallback`1" />.
        /// </summary>
        /// <param name="name">The name of the attribute (including "on" prefix for HTML event handlers).</param>
        /// <param name="value">The function to use as callback.</param>
        /// <remarks>For HTML event handlers, prefer functions from the module <see cref="on.task" />.</remarks>
        let inline callback<'T> (name: string) ([<InlineIfLambda>] value: 'T -> Task) =
            Attr(fun receiver builder sequence ->
                builder.AddAttribute<'T>(sequence, name, EventCallback.Factory.Create(receiver, Func<'T, Task>(value)))
                sequence + 1)

    /// <summary>
    /// Create an attribute whose value is an HTML fragment.
    /// Use this function for Blazor component attributes of type <see cref="T:Microsoft.AspNetCore.Components.RenderFragment" />.
    /// </summary>
    /// <param name="name">The name of the attribute.</param>
    /// <param name="node">The value of the attribute.</param>
    let inline fragment name ([<InlineIfLambda>] node: Node) =
        Attr(fun receiver builder sequence ->
            builder.AddAttribute(sequence, name, RenderFragment(fun rt ->
                node.Invoke(receiver, rt, 0) |> ignore))
            sequence + 1)

    /// <summary>
    /// Create an attribute whose value is a parameterized HTML fragment.
    /// Use this function for Blazor component attributes of type <see cref="T:Microsoft.AspNetCore.Components.RenderFragment`1" />.
    /// </summary>
    /// <param name="name">The name of the attribute.</param>
    /// <param name="node">The value of the attribute.</param>
    let inline fragmentWith name ([<InlineIfLambda>] node: 'a -> Node) =
        Attr(fun receiver builder sequence ->
            builder.AddAttribute(sequence, name, RenderFragment<_>(fun ctx ->
                RenderFragment(fun rt ->
                    (node ctx).Invoke(receiver, rt, 0) |> ignore)))
            sequence + 1)

// BEGIN ATTRS
    /// <summary>Create an HTML <c>accept</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline accept (v: obj) : Attr = "accept" => v

    /// <summary>Create an HTML <c>accept-charset</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline acceptCharset (v: obj) : Attr = "accept-charset" => v

    /// <summary>Create an HTML <c>accesskey</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline accesskey (v: obj) : Attr = "accesskey" => v

    /// <summary>Create an HTML <c>action</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline action (v: obj) : Attr = "action" => v

    /// <summary>Create an HTML <c>align</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline align (v: obj) : Attr = "align" => v

    /// <summary>Create an HTML <c>allow</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline allow (v: obj) : Attr = "allow" => v

    /// <summary>Create an HTML <c>alt</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline alt (v: obj) : Attr = "alt" => v

    /// <summary>Create an HTML <c>async</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline async' (v: obj) : Attr = "async" => v

    /// <summary>Create an HTML <c>autocapitalize</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline autocapitalize (v: obj) : Attr = "autocapitalize" => v

    /// <summary>Create an HTML <c>autocomplete</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline autocomplete (v: obj) : Attr = "autocomplete" => v

    /// <summary>Create an HTML <c>autofocus</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline autofocus (v: obj) : Attr = "autofocus" => v

    /// <summary>Create an HTML <c>autoplay</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline autoplay (v: obj) : Attr = "autoplay" => v

    /// <summary>Create an HTML <c>bgcolor</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline bgcolor (v: obj) : Attr = "bgcolor" => v

    /// <summary>Create an HTML <c>border</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline border (v: obj) : Attr = "border" => v

    /// <summary>Create an HTML <c>buffered</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline buffered (v: obj) : Attr = "buffered" => v

    /// <summary>Create an HTML <c>challenge</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline challenge (v: obj) : Attr = "challenge" => v

    /// <summary>Create an HTML <c>charset</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline charset (v: obj) : Attr = "charset" => v

    /// <summary>Create an HTML <c>checked</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline ``checked`` (v: obj) : Attr = "checked" => v

    /// <summary>Create an HTML <c>cite</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline cite (v: obj) : Attr = "cite" => v

    /// <summary>Create an HTML <c>class</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline ``class`` (v: obj) : Attr = "class" => v

    /// <summary>Create an HTML <c>code</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline code (v: obj) : Attr = "code" => v

    /// <summary>Create an HTML <c>codebase</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline codebase (v: obj) : Attr = "codebase" => v

    /// <summary>Create an HTML <c>color</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline color (v: obj) : Attr = "color" => v

    /// <summary>Create an HTML <c>cols</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline cols (v: obj) : Attr = "cols" => v

    /// <summary>Create an HTML <c>colspan</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline colspan (v: obj) : Attr = "colspan" => v

    /// <summary>Create an HTML <c>content</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline content (v: obj) : Attr = "content" => v

    /// <summary>Create an HTML <c>contenteditable</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline contenteditable (v: obj) : Attr = "contenteditable" => v

    /// <summary>Create an HTML <c>contextmenu</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline contextmenu (v: obj) : Attr = "contextmenu" => v

    /// <summary>Create an HTML <c>controls</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline controls (v: obj) : Attr = "controls" => v

    /// <summary>Create an HTML <c>coords</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline coords (v: obj) : Attr = "coords" => v

    /// <summary>Create an HTML <c>crossorigin</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline crossorigin (v: obj) : Attr = "crossorigin" => v

    /// <summary>Create an HTML <c>csp</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline csp (v: obj) : Attr = "csp" => v

    /// <summary>Create an HTML <c>data</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline data (v: obj) : Attr = "data" => v

    /// <summary>Create an HTML <c>datetime</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline datetime (v: obj) : Attr = "datetime" => v

    /// <summary>Create an HTML <c>decoding</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline decoding (v: obj) : Attr = "decoding" => v

    /// <summary>Create an HTML <c>default</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline ``default`` (v: obj) : Attr = "default" => v

    /// <summary>Create an HTML <c>defer</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline defer (v: obj) : Attr = "defer" => v

    /// <summary>Create an HTML <c>dir</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline dir (v: obj) : Attr = "dir" => v

    /// <summary>Create an HTML <c>dirname</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline dirname (v: obj) : Attr = "dirname" => v

    /// <summary>Create an HTML <c>disabled</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline disabled (v: obj) : Attr = "disabled" => v

    /// <summary>Create an HTML <c>download</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline download (v: obj) : Attr = "download" => v

    /// <summary>Create an HTML <c>draggable</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline draggable (v: obj) : Attr = "draggable" => v

    /// <summary>Create an HTML <c>dropzone</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline dropzone (v: obj) : Attr = "dropzone" => v

    /// <summary>Create an HTML <c>enctype</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline enctype (v: obj) : Attr = "enctype" => v

    /// <summary>Create an HTML <c>for</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline ``for`` (v: obj) : Attr = "for" => v

    /// <summary>Create an HTML <c>form</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline form (v: obj) : Attr = "form" => v

    /// <summary>Create an HTML <c>formaction</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline formaction (v: obj) : Attr = "formaction" => v

    /// <summary>Create an HTML <c>headers</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline headers (v: obj) : Attr = "headers" => v

    /// <summary>Create an HTML <c>height</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline height (v: obj) : Attr = "height" => v

    /// <summary>Create an HTML <c>hidden</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline hidden (v: obj) : Attr = "hidden" => v

    /// <summary>Create an HTML <c>high</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline high (v: obj) : Attr = "high" => v

    /// <summary>Create an HTML <c>href</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline href (v: obj) : Attr = "href" => v

    /// <summary>Create an HTML <c>hreflang</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline hreflang (v: obj) : Attr = "hreflang" => v

    /// <summary>Create an HTML <c>http-equiv</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline httpEquiv (v: obj) : Attr = "http-equiv" => v

    /// <summary>Create an HTML <c>icon</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline icon (v: obj) : Attr = "icon" => v

    /// <summary>Create an HTML <c>id</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline id (v: obj) : Attr = "id" => v

    /// <summary>Create an HTML <c>importance</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline importance (v: obj) : Attr = "importance" => v

    /// <summary>Create an HTML <c>integrity</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline integrity (v: obj) : Attr = "integrity" => v

    /// <summary>Create an HTML <c>ismap</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline ismap (v: obj) : Attr = "ismap" => v

    /// <summary>Create an HTML <c>itemprop</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline itemprop (v: obj) : Attr = "itemprop" => v

    /// <summary>Create an HTML <c>keytype</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline keytype (v: obj) : Attr = "keytype" => v

    /// <summary>Create an HTML <c>kind</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline kind (v: obj) : Attr = "kind" => v

    /// <summary>Create an HTML <c>label</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline label (v: obj) : Attr = "label" => v

    /// <summary>Create an HTML <c>lang</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline lang (v: obj) : Attr = "lang" => v

    /// <summary>Create an HTML <c>language</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline language (v: obj) : Attr = "language" => v

    /// <summary>Create an HTML <c>lazyload</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline lazyload (v: obj) : Attr = "lazyload" => v

    /// <summary>Create an HTML <c>list</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline list (v: obj) : Attr = "list" => v

    /// <summary>Create an HTML <c>loop</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline loop (v: obj) : Attr = "loop" => v

    /// <summary>Create an HTML <c>low</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline low (v: obj) : Attr = "low" => v

    /// <summary>Create an HTML <c>manifest</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline manifest (v: obj) : Attr = "manifest" => v

    /// <summary>Create an HTML <c>max</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline max (v: obj) : Attr = "max" => v

    /// <summary>Create an HTML <c>maxlength</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline maxlength (v: obj) : Attr = "maxlength" => v

    /// <summary>Create an HTML <c>media</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline media (v: obj) : Attr = "media" => v

    /// <summary>Create an HTML <c>method</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline method (v: obj) : Attr = "method" => v

    /// <summary>Create an HTML <c>min</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline min (v: obj) : Attr = "min" => v

    /// <summary>Create an HTML <c>minlength</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline minlength (v: obj) : Attr = "minlength" => v

    /// <summary>Create an HTML <c>multiple</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline multiple (v: obj) : Attr = "multiple" => v

    /// <summary>Create an HTML <c>muted</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline muted (v: obj) : Attr = "muted" => v

    /// <summary>Create an HTML <c>name</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline name (v: obj) : Attr = "name" => v

    /// <summary>Create an HTML <c>novalidate</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline novalidate (v: obj) : Attr = "novalidate" => v

    /// <summary>Create an HTML <c>open</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline ``open`` (v: obj) : Attr = "open" => v

    /// <summary>Create an HTML <c>optimum</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline optimum (v: obj) : Attr = "optimum" => v

    /// <summary>Create an HTML <c>pattern</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline pattern (v: obj) : Attr = "pattern" => v

    /// <summary>Create an HTML <c>ping</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline ping (v: obj) : Attr = "ping" => v

    /// <summary>Create an HTML <c>placeholder</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline placeholder (v: obj) : Attr = "placeholder" => v

    /// <summary>Create an HTML <c>poster</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline poster (v: obj) : Attr = "poster" => v

    /// <summary>Create an HTML <c>preload</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline preload (v: obj) : Attr = "preload" => v

    /// <summary>Create an HTML <c>readonly</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline readonly (v: obj) : Attr = "readonly" => v

    /// <summary>Create an HTML <c>rel</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline rel (v: obj) : Attr = "rel" => v

    /// <summary>Create an HTML <c>required</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline required (v: obj) : Attr = "required" => v

    /// <summary>Create an HTML <c>reversed</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline reversed (v: obj) : Attr = "reversed" => v

    /// <summary>Create an HTML <c>rows</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline rows (v: obj) : Attr = "rows" => v

    /// <summary>Create an HTML <c>rowspan</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline rowspan (v: obj) : Attr = "rowspan" => v

    /// <summary>Create an HTML <c>sandbox</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline sandbox (v: obj) : Attr = "sandbox" => v

    /// <summary>Create an HTML <c>scope</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline scope (v: obj) : Attr = "scope" => v

    /// <summary>Create an HTML <c>selected</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline selected (v: obj) : Attr = "selected" => v

    /// <summary>Create an HTML <c>shape</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline shape (v: obj) : Attr = "shape" => v

    /// <summary>Create an HTML <c>size</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline size (v: obj) : Attr = "size" => v

    /// <summary>Create an HTML <c>sizes</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline sizes (v: obj) : Attr = "sizes" => v

    /// <summary>Create an HTML <c>slot</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline slot (v: obj) : Attr = "slot" => v

    /// <summary>Create an HTML <c>span</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline span (v: obj) : Attr = "span" => v

    /// <summary>Create an HTML <c>spellcheck</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline spellcheck (v: obj) : Attr = "spellcheck" => v

    /// <summary>Create an HTML <c>src</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline src (v: obj) : Attr = "src" => v

    /// <summary>Create an HTML <c>srcdoc</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline srcdoc (v: obj) : Attr = "srcdoc" => v

    /// <summary>Create an HTML <c>srclang</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline srclang (v: obj) : Attr = "srclang" => v

    /// <summary>Create an HTML <c>srcset</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline srcset (v: obj) : Attr = "srcset" => v

    /// <summary>Create an HTML <c>start</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline start (v: obj) : Attr = "start" => v

    /// <summary>Create an HTML <c>step</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline step (v: obj) : Attr = "step" => v

    /// <summary>Create an HTML <c>style</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline style (v: obj) : Attr = "style" => v

    /// <summary>Create an HTML <c>summary</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline summary (v: obj) : Attr = "summary" => v

    /// <summary>Create an HTML <c>tabindex</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline tabindex (v: obj) : Attr = "tabindex" => v

    /// <summary>Create an HTML <c>target</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline target (v: obj) : Attr = "target" => v

    /// <summary>Create an HTML <c>title</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline title (v: obj) : Attr = "title" => v

    /// <summary>Create an HTML <c>translate</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline translate (v: obj) : Attr = "translate" => v

    /// <summary>Create an HTML <c>type</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline ``type`` (v: obj) : Attr = "type" => v

    /// <summary>Create an HTML <c>usemap</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline usemap (v: obj) : Attr = "usemap" => v

    /// <summary>Create an HTML <c>value</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline value (v: obj) : Attr = "value" => v

    /// <summary>Create an HTML <c>width</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline width (v: obj) : Attr = "width" => v

    /// <summary>Create an HTML <c>wrap</c> attribute.</summary>
    /// <param name="v">The value of the attribute.</param>
    let inline wrap (v: obj) : Attr = "wrap" => v

// END ATTRS

/// Event handlers.
module on =

    /// <summary>Create a handler for a HTML event of type <typeparamref name="T" />.</summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="eventName">The name of the event, without the "on" prefix.</param>
    /// <param name="callback">The event callback.</param>
    let inline event<'T when 'T :> EventArgs> eventName (callback: ^T -> unit) =
        attr.callback<'T> ("on" + eventName) callback

    /// <summary>Prevent the default event behavior for a given HTML event.</summary>
    /// <param name="eventName">The name of the event, without the "on" prefix.</param>
    /// <param name="value">True to prevent the default event behavior.</param>
    let inline preventDefault eventName (value: bool) =
        Attr(fun _ builder sequence ->
            builder.AddEventPreventDefaultAttribute(sequence, eventName, value)
            sequence + 1
        )

    /// <summary>Stop the propagation to parent elements of a given HTML event.</summary>
    /// <param name="eventName">The name of the event, without the "on" prefix.</param>
    /// <param name="value">True to prevent the propagation.</param>
    let inline stopPropagation eventName (value: bool) =
        Attr(fun _ builder sequence ->
            builder.AddEventStopPropagationAttribute(sequence, eventName, value)
            sequence + 1
        )

// BEGIN EVENTS
    /// <summary>Create a handler for HTML event <c>focus</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline focus (callback: FocusEventArgs -> unit) : Attr =
        attr.callback<FocusEventArgs> "onfocus" callback

    /// <summary>Create a handler for HTML event <c>blur</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline blur (callback: FocusEventArgs -> unit) : Attr =
        attr.callback<FocusEventArgs> "onblur" callback

    /// <summary>Create a handler for HTML event <c>focusin</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline focusin (callback: FocusEventArgs -> unit) : Attr =
        attr.callback<FocusEventArgs> "onfocusin" callback

    /// <summary>Create a handler for HTML event <c>focusout</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline focusout (callback: FocusEventArgs -> unit) : Attr =
        attr.callback<FocusEventArgs> "onfocusout" callback

    /// <summary>Create a handler for HTML event <c>mouseover</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline mouseover (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "onmouseover" callback

    /// <summary>Create a handler for HTML event <c>mouseout</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline mouseout (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "onmouseout" callback

    /// <summary>Create a handler for HTML event <c>mousemove</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline mousemove (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "onmousemove" callback

    /// <summary>Create a handler for HTML event <c>mousedown</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline mousedown (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "onmousedown" callback

    /// <summary>Create a handler for HTML event <c>mouseup</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline mouseup (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "onmouseup" callback

    /// <summary>Create a handler for HTML event <c>click</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline click (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "onclick" callback

    /// <summary>Create a handler for HTML event <c>dblclick</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline dblclick (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "ondblclick" callback

    /// <summary>Create a handler for HTML event <c>wheel</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline wheel (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "onwheel" callback

    /// <summary>Create a handler for HTML event <c>mousewheel</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline mousewheel (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "onmousewheel" callback

    /// <summary>Create a handler for HTML event <c>contextmenu</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline contextmenu (callback: MouseEventArgs -> unit) : Attr =
        attr.callback<MouseEventArgs> "oncontextmenu" callback

    /// <summary>Create a handler for HTML event <c>drag</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline drag (callback: DragEventArgs -> unit) : Attr =
        attr.callback<DragEventArgs> "ondrag" callback

    /// <summary>Create a handler for HTML event <c>dragend</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline dragend (callback: DragEventArgs -> unit) : Attr =
        attr.callback<DragEventArgs> "ondragend" callback

    /// <summary>Create a handler for HTML event <c>dragenter</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline dragenter (callback: DragEventArgs -> unit) : Attr =
        attr.callback<DragEventArgs> "ondragenter" callback

    /// <summary>Create a handler for HTML event <c>dragleave</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline dragleave (callback: DragEventArgs -> unit) : Attr =
        attr.callback<DragEventArgs> "ondragleave" callback

    /// <summary>Create a handler for HTML event <c>dragover</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline dragover (callback: DragEventArgs -> unit) : Attr =
        attr.callback<DragEventArgs> "ondragover" callback

    /// <summary>Create a handler for HTML event <c>dragstart</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline dragstart (callback: DragEventArgs -> unit) : Attr =
        attr.callback<DragEventArgs> "ondragstart" callback

    /// <summary>Create a handler for HTML event <c>drop</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline drop (callback: DragEventArgs -> unit) : Attr =
        attr.callback<DragEventArgs> "ondrop" callback

    /// <summary>Create a handler for HTML event <c>keydown</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline keydown (callback: KeyboardEventArgs -> unit) : Attr =
        attr.callback<KeyboardEventArgs> "onkeydown" callback

    /// <summary>Create a handler for HTML event <c>keyup</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline keyup (callback: KeyboardEventArgs -> unit) : Attr =
        attr.callback<KeyboardEventArgs> "onkeyup" callback

    /// <summary>Create a handler for HTML event <c>keypress</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline keypress (callback: KeyboardEventArgs -> unit) : Attr =
        attr.callback<KeyboardEventArgs> "onkeypress" callback

    /// <summary>Create a handler for HTML event <c>change</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline change (callback: ChangeEventArgs -> unit) : Attr =
        attr.callback<ChangeEventArgs> "onchange" callback

    /// <summary>Create a handler for HTML event <c>input</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline input (callback: ChangeEventArgs -> unit) : Attr =
        attr.callback<ChangeEventArgs> "oninput" callback

    /// <summary>Create a handler for HTML event <c>invalid</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline invalid (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "oninvalid" callback

    /// <summary>Create a handler for HTML event <c>reset</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline reset (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onreset" callback

    /// <summary>Create a handler for HTML event <c>select</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline select (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onselect" callback

    /// <summary>Create a handler for HTML event <c>selectstart</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline selectstart (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onselectstart" callback

    /// <summary>Create a handler for HTML event <c>selectionchange</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline selectionchange (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onselectionchange" callback

    /// <summary>Create a handler for HTML event <c>submit</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline submit (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onsubmit" callback

    /// <summary>Create a handler for HTML event <c>beforecopy</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline beforecopy (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onbeforecopy" callback

    /// <summary>Create a handler for HTML event <c>beforecut</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline beforecut (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onbeforecut" callback

    /// <summary>Create a handler for HTML event <c>beforepaste</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline beforepaste (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onbeforepaste" callback

    /// <summary>Create a handler for HTML event <c>copy</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline copy (callback: ClipboardEventArgs -> unit) : Attr =
        attr.callback<ClipboardEventArgs> "oncopy" callback

    /// <summary>Create a handler for HTML event <c>cut</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline cut (callback: ClipboardEventArgs -> unit) : Attr =
        attr.callback<ClipboardEventArgs> "oncut" callback

    /// <summary>Create a handler for HTML event <c>paste</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline paste (callback: ClipboardEventArgs -> unit) : Attr =
        attr.callback<ClipboardEventArgs> "onpaste" callback

    /// <summary>Create a handler for HTML event <c>touchcancel</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline touchcancel (callback: TouchEventArgs -> unit) : Attr =
        attr.callback<TouchEventArgs> "ontouchcancel" callback

    /// <summary>Create a handler for HTML event <c>touchend</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline touchend (callback: TouchEventArgs -> unit) : Attr =
        attr.callback<TouchEventArgs> "ontouchend" callback

    /// <summary>Create a handler for HTML event <c>touchmove</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline touchmove (callback: TouchEventArgs -> unit) : Attr =
        attr.callback<TouchEventArgs> "ontouchmove" callback

    /// <summary>Create a handler for HTML event <c>touchstart</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline touchstart (callback: TouchEventArgs -> unit) : Attr =
        attr.callback<TouchEventArgs> "ontouchstart" callback

    /// <summary>Create a handler for HTML event <c>touchenter</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline touchenter (callback: TouchEventArgs -> unit) : Attr =
        attr.callback<TouchEventArgs> "ontouchenter" callback

    /// <summary>Create a handler for HTML event <c>touchleave</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline touchleave (callback: TouchEventArgs -> unit) : Attr =
        attr.callback<TouchEventArgs> "ontouchleave" callback

    /// <summary>Create a handler for HTML event <c>pointercapture</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline pointercapture (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointercapture" callback

    /// <summary>Create a handler for HTML event <c>lostpointercapture</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline lostpointercapture (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onlostpointercapture" callback

    /// <summary>Create a handler for HTML event <c>pointercancel</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline pointercancel (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointercancel" callback

    /// <summary>Create a handler for HTML event <c>pointerdown</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline pointerdown (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointerdown" callback

    /// <summary>Create a handler for HTML event <c>pointerenter</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline pointerenter (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointerenter" callback

    /// <summary>Create a handler for HTML event <c>pointerleave</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline pointerleave (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointerleave" callback

    /// <summary>Create a handler for HTML event <c>pointermove</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline pointermove (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointermove" callback

    /// <summary>Create a handler for HTML event <c>pointerout</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline pointerout (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointerout" callback

    /// <summary>Create a handler for HTML event <c>pointerover</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline pointerover (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointerover" callback

    /// <summary>Create a handler for HTML event <c>pointerup</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline pointerup (callback: PointerEventArgs -> unit) : Attr =
        attr.callback<PointerEventArgs> "onpointerup" callback

    /// <summary>Create a handler for HTML event <c>canplay</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline canplay (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "oncanplay" callback

    /// <summary>Create a handler for HTML event <c>canplaythrough</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline canplaythrough (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "oncanplaythrough" callback

    /// <summary>Create a handler for HTML event <c>cuechange</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline cuechange (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "oncuechange" callback

    /// <summary>Create a handler for HTML event <c>durationchange</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline durationchange (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "ondurationchange" callback

    /// <summary>Create a handler for HTML event <c>emptied</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline emptied (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onemptied" callback

    /// <summary>Create a handler for HTML event <c>pause</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline pause (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onpause" callback

    /// <summary>Create a handler for HTML event <c>play</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline play (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onplay" callback

    /// <summary>Create a handler for HTML event <c>playing</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline playing (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onplaying" callback

    /// <summary>Create a handler for HTML event <c>ratechange</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline ratechange (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onratechange" callback

    /// <summary>Create a handler for HTML event <c>seeked</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline seeked (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onseeked" callback

    /// <summary>Create a handler for HTML event <c>seeking</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline seeking (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onseeking" callback

    /// <summary>Create a handler for HTML event <c>stalled</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline stalled (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onstalled" callback

    /// <summary>Create a handler for HTML event <c>stop</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline stop (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onstop" callback

    /// <summary>Create a handler for HTML event <c>suspend</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline suspend (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onsuspend" callback

    /// <summary>Create a handler for HTML event <c>timeupdate</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline timeupdate (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "ontimeupdate" callback

    /// <summary>Create a handler for HTML event <c>volumechange</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline volumechange (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onvolumechange" callback

    /// <summary>Create a handler for HTML event <c>waiting</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline waiting (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onwaiting" callback

    /// <summary>Create a handler for HTML event <c>loadstart</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline loadstart (callback: ProgressEventArgs -> unit) : Attr =
        attr.callback<ProgressEventArgs> "onloadstart" callback

    /// <summary>Create a handler for HTML event <c>timeout</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline timeout (callback: ProgressEventArgs -> unit) : Attr =
        attr.callback<ProgressEventArgs> "ontimeout" callback

    /// <summary>Create a handler for HTML event <c>abort</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline abort (callback: ProgressEventArgs -> unit) : Attr =
        attr.callback<ProgressEventArgs> "onabort" callback

    /// <summary>Create a handler for HTML event <c>load</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline load (callback: ProgressEventArgs -> unit) : Attr =
        attr.callback<ProgressEventArgs> "onload" callback

    /// <summary>Create a handler for HTML event <c>loadend</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline loadend (callback: ProgressEventArgs -> unit) : Attr =
        attr.callback<ProgressEventArgs> "onloadend" callback

    /// <summary>Create a handler for HTML event <c>progress</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline progress (callback: ProgressEventArgs -> unit) : Attr =
        attr.callback<ProgressEventArgs> "onprogress" callback

    /// <summary>Create a handler for HTML event <c>error</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline error (callback: ProgressEventArgs -> unit) : Attr =
        attr.callback<ProgressEventArgs> "onerror" callback

    /// <summary>Create a handler for HTML event <c>activate</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline activate (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onactivate" callback

    /// <summary>Create a handler for HTML event <c>beforeactivate</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline beforeactivate (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onbeforeactivate" callback

    /// <summary>Create a handler for HTML event <c>beforedeactivate</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline beforedeactivate (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onbeforedeactivate" callback

    /// <summary>Create a handler for HTML event <c>deactivate</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline deactivate (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "ondeactivate" callback

    /// <summary>Create a handler for HTML event <c>ended</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline ended (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onended" callback

    /// <summary>Create a handler for HTML event <c>fullscreenchange</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline fullscreenchange (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onfullscreenchange" callback

    /// <summary>Create a handler for HTML event <c>fullscreenerror</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline fullscreenerror (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onfullscreenerror" callback

    /// <summary>Create a handler for HTML event <c>loadeddata</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline loadeddata (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onloadeddata" callback

    /// <summary>Create a handler for HTML event <c>loadedmetadata</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline loadedmetadata (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onloadedmetadata" callback

    /// <summary>Create a handler for HTML event <c>pointerlockchange</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline pointerlockchange (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onpointerlockchange" callback

    /// <summary>Create a handler for HTML event <c>pointerlockerror</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline pointerlockerror (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onpointerlockerror" callback

    /// <summary>Create a handler for HTML event <c>readystatechange</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline readystatechange (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onreadystatechange" callback

    /// <summary>Create a handler for HTML event <c>scroll</c>.</summary>
    /// <param name="callback">The event callback.</param>
    let inline scroll (callback: EventArgs -> unit) : Attr =
        attr.callback<EventArgs> "onscroll" callback

// END EVENTS

    /// <summary>Event handlers returning type <see cref="T:Async`1" />.</summary>
    module async =

        /// <summary>Create an asynchronous handler for a HTML event of type <typeparamref name="T" />.</summary>
        /// <typeparam name="T">The event type.</typeparam>
        /// <param name="eventName">The name of the event, without the "on" prefix.</param>
        /// <param name="callback">The event callback.</param>
        let inline event<'T> eventName (callback: 'T -> Async<unit>) =
            attr.async.callback<'T> ("on" + eventName) callback

// BEGIN ASYNCEVENTS
        /// <summary>Create an asynchronous handler for HTML event <c>focus</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline focus (callback: FocusEventArgs -> Async<unit>) : Attr =
            attr.async.callback<FocusEventArgs> "onfocus" callback
        /// <summary>Create an asynchronous handler for HTML event <c>blur</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline blur (callback: FocusEventArgs -> Async<unit>) : Attr =
            attr.async.callback<FocusEventArgs> "onblur" callback
        /// <summary>Create an asynchronous handler for HTML event <c>focusin</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline focusin (callback: FocusEventArgs -> Async<unit>) : Attr =
            attr.async.callback<FocusEventArgs> "onfocusin" callback
        /// <summary>Create an asynchronous handler for HTML event <c>focusout</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline focusout (callback: FocusEventArgs -> Async<unit>) : Attr =
            attr.async.callback<FocusEventArgs> "onfocusout" callback
        /// <summary>Create an asynchronous handler for HTML event <c>mouseover</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline mouseover (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "onmouseover" callback
        /// <summary>Create an asynchronous handler for HTML event <c>mouseout</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline mouseout (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "onmouseout" callback
        /// <summary>Create an asynchronous handler for HTML event <c>mousemove</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline mousemove (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "onmousemove" callback
        /// <summary>Create an asynchronous handler for HTML event <c>mousedown</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline mousedown (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "onmousedown" callback
        /// <summary>Create an asynchronous handler for HTML event <c>mouseup</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline mouseup (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "onmouseup" callback
        /// <summary>Create an asynchronous handler for HTML event <c>click</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline click (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "onclick" callback
        /// <summary>Create an asynchronous handler for HTML event <c>dblclick</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline dblclick (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "ondblclick" callback
        /// <summary>Create an asynchronous handler for HTML event <c>wheel</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline wheel (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "onwheel" callback
        /// <summary>Create an asynchronous handler for HTML event <c>mousewheel</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline mousewheel (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "onmousewheel" callback
        /// <summary>Create an asynchronous handler for HTML event <c>contextmenu</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline contextmenu (callback: MouseEventArgs -> Async<unit>) : Attr =
            attr.async.callback<MouseEventArgs> "oncontextmenu" callback
        /// <summary>Create an asynchronous handler for HTML event <c>drag</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline drag (callback: DragEventArgs -> Async<unit>) : Attr =
            attr.async.callback<DragEventArgs> "ondrag" callback
        /// <summary>Create an asynchronous handler for HTML event <c>dragend</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline dragend (callback: DragEventArgs -> Async<unit>) : Attr =
            attr.async.callback<DragEventArgs> "ondragend" callback
        /// <summary>Create an asynchronous handler for HTML event <c>dragenter</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline dragenter (callback: DragEventArgs -> Async<unit>) : Attr =
            attr.async.callback<DragEventArgs> "ondragenter" callback
        /// <summary>Create an asynchronous handler for HTML event <c>dragleave</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline dragleave (callback: DragEventArgs -> Async<unit>) : Attr =
            attr.async.callback<DragEventArgs> "ondragleave" callback
        /// <summary>Create an asynchronous handler for HTML event <c>dragover</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline dragover (callback: DragEventArgs -> Async<unit>) : Attr =
            attr.async.callback<DragEventArgs> "ondragover" callback
        /// <summary>Create an asynchronous handler for HTML event <c>dragstart</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline dragstart (callback: DragEventArgs -> Async<unit>) : Attr =
            attr.async.callback<DragEventArgs> "ondragstart" callback
        /// <summary>Create an asynchronous handler for HTML event <c>drop</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline drop (callback: DragEventArgs -> Async<unit>) : Attr =
            attr.async.callback<DragEventArgs> "ondrop" callback
        /// <summary>Create an asynchronous handler for HTML event <c>keydown</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline keydown (callback: KeyboardEventArgs -> Async<unit>) : Attr =
            attr.async.callback<KeyboardEventArgs> "onkeydown" callback
        /// <summary>Create an asynchronous handler for HTML event <c>keyup</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline keyup (callback: KeyboardEventArgs -> Async<unit>) : Attr =
            attr.async.callback<KeyboardEventArgs> "onkeyup" callback
        /// <summary>Create an asynchronous handler for HTML event <c>keypress</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline keypress (callback: KeyboardEventArgs -> Async<unit>) : Attr =
            attr.async.callback<KeyboardEventArgs> "onkeypress" callback
        /// <summary>Create an asynchronous handler for HTML event <c>change</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline change (callback: ChangeEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ChangeEventArgs> "onchange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>input</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline input (callback: ChangeEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ChangeEventArgs> "oninput" callback
        /// <summary>Create an asynchronous handler for HTML event <c>invalid</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline invalid (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "oninvalid" callback
        /// <summary>Create an asynchronous handler for HTML event <c>reset</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline reset (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onreset" callback
        /// <summary>Create an asynchronous handler for HTML event <c>select</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline select (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onselect" callback
        /// <summary>Create an asynchronous handler for HTML event <c>selectstart</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline selectstart (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onselectstart" callback
        /// <summary>Create an asynchronous handler for HTML event <c>selectionchange</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline selectionchange (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onselectionchange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>submit</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline submit (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onsubmit" callback
        /// <summary>Create an asynchronous handler for HTML event <c>beforecopy</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline beforecopy (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onbeforecopy" callback
        /// <summary>Create an asynchronous handler for HTML event <c>beforecut</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline beforecut (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onbeforecut" callback
        /// <summary>Create an asynchronous handler for HTML event <c>beforepaste</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline beforepaste (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onbeforepaste" callback
        /// <summary>Create an asynchronous handler for HTML event <c>copy</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline copy (callback: ClipboardEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ClipboardEventArgs> "oncopy" callback
        /// <summary>Create an asynchronous handler for HTML event <c>cut</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline cut (callback: ClipboardEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ClipboardEventArgs> "oncut" callback
        /// <summary>Create an asynchronous handler for HTML event <c>paste</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline paste (callback: ClipboardEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ClipboardEventArgs> "onpaste" callback
        /// <summary>Create an asynchronous handler for HTML event <c>touchcancel</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline touchcancel (callback: TouchEventArgs -> Async<unit>) : Attr =
            attr.async.callback<TouchEventArgs> "ontouchcancel" callback
        /// <summary>Create an asynchronous handler for HTML event <c>touchend</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline touchend (callback: TouchEventArgs -> Async<unit>) : Attr =
            attr.async.callback<TouchEventArgs> "ontouchend" callback
        /// <summary>Create an asynchronous handler for HTML event <c>touchmove</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline touchmove (callback: TouchEventArgs -> Async<unit>) : Attr =
            attr.async.callback<TouchEventArgs> "ontouchmove" callback
        /// <summary>Create an asynchronous handler for HTML event <c>touchstart</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline touchstart (callback: TouchEventArgs -> Async<unit>) : Attr =
            attr.async.callback<TouchEventArgs> "ontouchstart" callback
        /// <summary>Create an asynchronous handler for HTML event <c>touchenter</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline touchenter (callback: TouchEventArgs -> Async<unit>) : Attr =
            attr.async.callback<TouchEventArgs> "ontouchenter" callback
        /// <summary>Create an asynchronous handler for HTML event <c>touchleave</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline touchleave (callback: TouchEventArgs -> Async<unit>) : Attr =
            attr.async.callback<TouchEventArgs> "ontouchleave" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointercapture</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointercapture (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointercapture" callback
        /// <summary>Create an asynchronous handler for HTML event <c>lostpointercapture</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline lostpointercapture (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onlostpointercapture" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointercancel</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointercancel (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointercancel" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointerdown</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointerdown (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointerdown" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointerenter</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointerenter (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointerenter" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointerleave</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointerleave (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointerleave" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointermove</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointermove (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointermove" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointerout</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointerout (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointerout" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointerover</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointerover (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointerover" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointerup</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointerup (callback: PointerEventArgs -> Async<unit>) : Attr =
            attr.async.callback<PointerEventArgs> "onpointerup" callback
        /// <summary>Create an asynchronous handler for HTML event <c>canplay</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline canplay (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "oncanplay" callback
        /// <summary>Create an asynchronous handler for HTML event <c>canplaythrough</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline canplaythrough (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "oncanplaythrough" callback
        /// <summary>Create an asynchronous handler for HTML event <c>cuechange</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline cuechange (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "oncuechange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>durationchange</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline durationchange (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "ondurationchange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>emptied</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline emptied (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onemptied" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pause</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pause (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onpause" callback
        /// <summary>Create an asynchronous handler for HTML event <c>play</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline play (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onplay" callback
        /// <summary>Create an asynchronous handler for HTML event <c>playing</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline playing (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onplaying" callback
        /// <summary>Create an asynchronous handler for HTML event <c>ratechange</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline ratechange (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onratechange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>seeked</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline seeked (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onseeked" callback
        /// <summary>Create an asynchronous handler for HTML event <c>seeking</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline seeking (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onseeking" callback
        /// <summary>Create an asynchronous handler for HTML event <c>stalled</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline stalled (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onstalled" callback
        /// <summary>Create an asynchronous handler for HTML event <c>stop</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline stop (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onstop" callback
        /// <summary>Create an asynchronous handler for HTML event <c>suspend</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline suspend (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onsuspend" callback
        /// <summary>Create an asynchronous handler for HTML event <c>timeupdate</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline timeupdate (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "ontimeupdate" callback
        /// <summary>Create an asynchronous handler for HTML event <c>volumechange</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline volumechange (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onvolumechange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>waiting</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline waiting (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onwaiting" callback
        /// <summary>Create an asynchronous handler for HTML event <c>loadstart</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline loadstart (callback: ProgressEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ProgressEventArgs> "onloadstart" callback
        /// <summary>Create an asynchronous handler for HTML event <c>timeout</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline timeout (callback: ProgressEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ProgressEventArgs> "ontimeout" callback
        /// <summary>Create an asynchronous handler for HTML event <c>abort</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline abort (callback: ProgressEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ProgressEventArgs> "onabort" callback
        /// <summary>Create an asynchronous handler for HTML event <c>load</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline load (callback: ProgressEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ProgressEventArgs> "onload" callback
        /// <summary>Create an asynchronous handler for HTML event <c>loadend</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline loadend (callback: ProgressEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ProgressEventArgs> "onloadend" callback
        /// <summary>Create an asynchronous handler for HTML event <c>progress</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline progress (callback: ProgressEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ProgressEventArgs> "onprogress" callback
        /// <summary>Create an asynchronous handler for HTML event <c>error</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline error (callback: ProgressEventArgs -> Async<unit>) : Attr =
            attr.async.callback<ProgressEventArgs> "onerror" callback
        /// <summary>Create an asynchronous handler for HTML event <c>activate</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline activate (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onactivate" callback
        /// <summary>Create an asynchronous handler for HTML event <c>beforeactivate</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline beforeactivate (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onbeforeactivate" callback
        /// <summary>Create an asynchronous handler for HTML event <c>beforedeactivate</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline beforedeactivate (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onbeforedeactivate" callback
        /// <summary>Create an asynchronous handler for HTML event <c>deactivate</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline deactivate (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "ondeactivate" callback
        /// <summary>Create an asynchronous handler for HTML event <c>ended</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline ended (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onended" callback
        /// <summary>Create an asynchronous handler for HTML event <c>fullscreenchange</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline fullscreenchange (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onfullscreenchange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>fullscreenerror</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline fullscreenerror (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onfullscreenerror" callback
        /// <summary>Create an asynchronous handler for HTML event <c>loadeddata</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline loadeddata (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onloadeddata" callback
        /// <summary>Create an asynchronous handler for HTML event <c>loadedmetadata</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline loadedmetadata (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onloadedmetadata" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointerlockchange</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointerlockchange (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onpointerlockchange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointerlockerror</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointerlockerror (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onpointerlockerror" callback
        /// <summary>Create an asynchronous handler for HTML event <c>readystatechange</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline readystatechange (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onreadystatechange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>scroll</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline scroll (callback: EventArgs -> Async<unit>) : Attr =
            attr.async.callback<EventArgs> "onscroll" callback
// END ASYNCEVENTS

    /// <summary>Event handlers returning type <see cref="System.Threading.Tasks.Task" />.</summary>
    module task =

        /// <summary>Create an asynchronous handler for a HTML event of type <typeparamref name="T" />.</summary>
        /// <typeparam name="T">The event type.</typeparam>
        /// <param name="eventName">The name of the event, without the "on" prefix.</param>
        /// <param name="callback">The event callback.</param>
        let inline event<'T> eventName (callback: 'T -> Task) =
            attr.task.callback<'T> ("on" + eventName) callback

// BEGIN TASKEVENTS
        /// <summary>Create an asynchronous handler for HTML event <c>focus</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline focus (callback: FocusEventArgs -> Task) : Attr =
            attr.task.callback<FocusEventArgs> "onfocus" callback
        /// <summary>Create an asynchronous handler for HTML event <c>blur</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline blur (callback: FocusEventArgs -> Task) : Attr =
            attr.task.callback<FocusEventArgs> "onblur" callback
        /// <summary>Create an asynchronous handler for HTML event <c>focusin</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline focusin (callback: FocusEventArgs -> Task) : Attr =
            attr.task.callback<FocusEventArgs> "onfocusin" callback
        /// <summary>Create an asynchronous handler for HTML event <c>focusout</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline focusout (callback: FocusEventArgs -> Task) : Attr =
            attr.task.callback<FocusEventArgs> "onfocusout" callback
        /// <summary>Create an asynchronous handler for HTML event <c>mouseover</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline mouseover (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "onmouseover" callback
        /// <summary>Create an asynchronous handler for HTML event <c>mouseout</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline mouseout (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "onmouseout" callback
        /// <summary>Create an asynchronous handler for HTML event <c>mousemove</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline mousemove (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "onmousemove" callback
        /// <summary>Create an asynchronous handler for HTML event <c>mousedown</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline mousedown (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "onmousedown" callback
        /// <summary>Create an asynchronous handler for HTML event <c>mouseup</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline mouseup (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "onmouseup" callback
        /// <summary>Create an asynchronous handler for HTML event <c>click</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline click (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "onclick" callback
        /// <summary>Create an asynchronous handler for HTML event <c>dblclick</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline dblclick (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "ondblclick" callback
        /// <summary>Create an asynchronous handler for HTML event <c>wheel</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline wheel (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "onwheel" callback
        /// <summary>Create an asynchronous handler for HTML event <c>mousewheel</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline mousewheel (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "onmousewheel" callback
        /// <summary>Create an asynchronous handler for HTML event <c>contextmenu</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline contextmenu (callback: MouseEventArgs -> Task) : Attr =
            attr.task.callback<MouseEventArgs> "oncontextmenu" callback
        /// <summary>Create an asynchronous handler for HTML event <c>drag</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline drag (callback: DragEventArgs -> Task) : Attr =
            attr.task.callback<DragEventArgs> "ondrag" callback
        /// <summary>Create an asynchronous handler for HTML event <c>dragend</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline dragend (callback: DragEventArgs -> Task) : Attr =
            attr.task.callback<DragEventArgs> "ondragend" callback
        /// <summary>Create an asynchronous handler for HTML event <c>dragenter</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline dragenter (callback: DragEventArgs -> Task) : Attr =
            attr.task.callback<DragEventArgs> "ondragenter" callback
        /// <summary>Create an asynchronous handler for HTML event <c>dragleave</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline dragleave (callback: DragEventArgs -> Task) : Attr =
            attr.task.callback<DragEventArgs> "ondragleave" callback
        /// <summary>Create an asynchronous handler for HTML event <c>dragover</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline dragover (callback: DragEventArgs -> Task) : Attr =
            attr.task.callback<DragEventArgs> "ondragover" callback
        /// <summary>Create an asynchronous handler for HTML event <c>dragstart</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline dragstart (callback: DragEventArgs -> Task) : Attr =
            attr.task.callback<DragEventArgs> "ondragstart" callback
        /// <summary>Create an asynchronous handler for HTML event <c>drop</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline drop (callback: DragEventArgs -> Task) : Attr =
            attr.task.callback<DragEventArgs> "ondrop" callback
        /// <summary>Create an asynchronous handler for HTML event <c>keydown</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline keydown (callback: KeyboardEventArgs -> Task) : Attr =
            attr.task.callback<KeyboardEventArgs> "onkeydown" callback
        /// <summary>Create an asynchronous handler for HTML event <c>keyup</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline keyup (callback: KeyboardEventArgs -> Task) : Attr =
            attr.task.callback<KeyboardEventArgs> "onkeyup" callback
        /// <summary>Create an asynchronous handler for HTML event <c>keypress</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline keypress (callback: KeyboardEventArgs -> Task) : Attr =
            attr.task.callback<KeyboardEventArgs> "onkeypress" callback
        /// <summary>Create an asynchronous handler for HTML event <c>change</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline change (callback: ChangeEventArgs -> Task) : Attr =
            attr.task.callback<ChangeEventArgs> "onchange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>input</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline input (callback: ChangeEventArgs -> Task) : Attr =
            attr.task.callback<ChangeEventArgs> "oninput" callback
        /// <summary>Create an asynchronous handler for HTML event <c>invalid</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline invalid (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "oninvalid" callback
        /// <summary>Create an asynchronous handler for HTML event <c>reset</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline reset (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onreset" callback
        /// <summary>Create an asynchronous handler for HTML event <c>select</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline select (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onselect" callback
        /// <summary>Create an asynchronous handler for HTML event <c>selectstart</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline selectstart (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onselectstart" callback
        /// <summary>Create an asynchronous handler for HTML event <c>selectionchange</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline selectionchange (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onselectionchange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>submit</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline submit (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onsubmit" callback
        /// <summary>Create an asynchronous handler for HTML event <c>beforecopy</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline beforecopy (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onbeforecopy" callback
        /// <summary>Create an asynchronous handler for HTML event <c>beforecut</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline beforecut (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onbeforecut" callback
        /// <summary>Create an asynchronous handler for HTML event <c>beforepaste</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline beforepaste (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onbeforepaste" callback
        /// <summary>Create an asynchronous handler for HTML event <c>copy</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline copy (callback: ClipboardEventArgs -> Task) : Attr =
            attr.task.callback<ClipboardEventArgs> "oncopy" callback
        /// <summary>Create an asynchronous handler for HTML event <c>cut</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline cut (callback: ClipboardEventArgs -> Task) : Attr =
            attr.task.callback<ClipboardEventArgs> "oncut" callback
        /// <summary>Create an asynchronous handler for HTML event <c>paste</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline paste (callback: ClipboardEventArgs -> Task) : Attr =
            attr.task.callback<ClipboardEventArgs> "onpaste" callback
        /// <summary>Create an asynchronous handler for HTML event <c>touchcancel</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline touchcancel (callback: TouchEventArgs -> Task) : Attr =
            attr.task.callback<TouchEventArgs> "ontouchcancel" callback
        /// <summary>Create an asynchronous handler for HTML event <c>touchend</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline touchend (callback: TouchEventArgs -> Task) : Attr =
            attr.task.callback<TouchEventArgs> "ontouchend" callback
        /// <summary>Create an asynchronous handler for HTML event <c>touchmove</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline touchmove (callback: TouchEventArgs -> Task) : Attr =
            attr.task.callback<TouchEventArgs> "ontouchmove" callback
        /// <summary>Create an asynchronous handler for HTML event <c>touchstart</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline touchstart (callback: TouchEventArgs -> Task) : Attr =
            attr.task.callback<TouchEventArgs> "ontouchstart" callback
        /// <summary>Create an asynchronous handler for HTML event <c>touchenter</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline touchenter (callback: TouchEventArgs -> Task) : Attr =
            attr.task.callback<TouchEventArgs> "ontouchenter" callback
        /// <summary>Create an asynchronous handler for HTML event <c>touchleave</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline touchleave (callback: TouchEventArgs -> Task) : Attr =
            attr.task.callback<TouchEventArgs> "ontouchleave" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointercapture</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointercapture (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointercapture" callback
        /// <summary>Create an asynchronous handler for HTML event <c>lostpointercapture</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline lostpointercapture (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onlostpointercapture" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointercancel</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointercancel (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointercancel" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointerdown</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointerdown (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointerdown" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointerenter</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointerenter (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointerenter" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointerleave</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointerleave (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointerleave" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointermove</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointermove (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointermove" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointerout</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointerout (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointerout" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointerover</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointerover (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointerover" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointerup</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointerup (callback: PointerEventArgs -> Task) : Attr =
            attr.task.callback<PointerEventArgs> "onpointerup" callback
        /// <summary>Create an asynchronous handler for HTML event <c>canplay</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline canplay (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "oncanplay" callback
        /// <summary>Create an asynchronous handler for HTML event <c>canplaythrough</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline canplaythrough (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "oncanplaythrough" callback
        /// <summary>Create an asynchronous handler for HTML event <c>cuechange</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline cuechange (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "oncuechange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>durationchange</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline durationchange (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "ondurationchange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>emptied</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline emptied (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onemptied" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pause</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pause (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onpause" callback
        /// <summary>Create an asynchronous handler for HTML event <c>play</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline play (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onplay" callback
        /// <summary>Create an asynchronous handler for HTML event <c>playing</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline playing (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onplaying" callback
        /// <summary>Create an asynchronous handler for HTML event <c>ratechange</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline ratechange (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onratechange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>seeked</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline seeked (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onseeked" callback
        /// <summary>Create an asynchronous handler for HTML event <c>seeking</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline seeking (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onseeking" callback
        /// <summary>Create an asynchronous handler for HTML event <c>stalled</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline stalled (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onstalled" callback
        /// <summary>Create an asynchronous handler for HTML event <c>stop</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline stop (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onstop" callback
        /// <summary>Create an asynchronous handler for HTML event <c>suspend</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline suspend (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onsuspend" callback
        /// <summary>Create an asynchronous handler for HTML event <c>timeupdate</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline timeupdate (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "ontimeupdate" callback
        /// <summary>Create an asynchronous handler for HTML event <c>volumechange</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline volumechange (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onvolumechange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>waiting</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline waiting (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onwaiting" callback
        /// <summary>Create an asynchronous handler for HTML event <c>loadstart</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline loadstart (callback: ProgressEventArgs -> Task) : Attr =
            attr.task.callback<ProgressEventArgs> "onloadstart" callback
        /// <summary>Create an asynchronous handler for HTML event <c>timeout</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline timeout (callback: ProgressEventArgs -> Task) : Attr =
            attr.task.callback<ProgressEventArgs> "ontimeout" callback
        /// <summary>Create an asynchronous handler for HTML event <c>abort</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline abort (callback: ProgressEventArgs -> Task) : Attr =
            attr.task.callback<ProgressEventArgs> "onabort" callback
        /// <summary>Create an asynchronous handler for HTML event <c>load</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline load (callback: ProgressEventArgs -> Task) : Attr =
            attr.task.callback<ProgressEventArgs> "onload" callback
        /// <summary>Create an asynchronous handler for HTML event <c>loadend</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline loadend (callback: ProgressEventArgs -> Task) : Attr =
            attr.task.callback<ProgressEventArgs> "onloadend" callback
        /// <summary>Create an asynchronous handler for HTML event <c>progress</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline progress (callback: ProgressEventArgs -> Task) : Attr =
            attr.task.callback<ProgressEventArgs> "onprogress" callback
        /// <summary>Create an asynchronous handler for HTML event <c>error</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline error (callback: ProgressEventArgs -> Task) : Attr =
            attr.task.callback<ProgressEventArgs> "onerror" callback
        /// <summary>Create an asynchronous handler for HTML event <c>activate</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline activate (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onactivate" callback
        /// <summary>Create an asynchronous handler for HTML event <c>beforeactivate</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline beforeactivate (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onbeforeactivate" callback
        /// <summary>Create an asynchronous handler for HTML event <c>beforedeactivate</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline beforedeactivate (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onbeforedeactivate" callback
        /// <summary>Create an asynchronous handler for HTML event <c>deactivate</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline deactivate (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "ondeactivate" callback
        /// <summary>Create an asynchronous handler for HTML event <c>ended</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline ended (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onended" callback
        /// <summary>Create an asynchronous handler for HTML event <c>fullscreenchange</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline fullscreenchange (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onfullscreenchange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>fullscreenerror</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline fullscreenerror (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onfullscreenerror" callback
        /// <summary>Create an asynchronous handler for HTML event <c>loadeddata</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline loadeddata (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onloadeddata" callback
        /// <summary>Create an asynchronous handler for HTML event <c>loadedmetadata</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline loadedmetadata (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onloadedmetadata" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointerlockchange</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointerlockchange (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onpointerlockchange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>pointerlockerror</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline pointerlockerror (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onpointerlockerror" callback
        /// <summary>Create an asynchronous handler for HTML event <c>readystatechange</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline readystatechange (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onreadystatechange" callback
        /// <summary>Create an asynchronous handler for HTML event <c>scroll</c>.</summary>
        /// <param name="callback">The event callback.</param>
        let inline scroll (callback: EventArgs -> Task) : Attr =
            attr.task.callback<EventArgs> "onscroll" callback
// END TASKEVENTS

/// Two-way binding for HTML input elements.
module bind =


    /// <exclude />
    [<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>]
    let inline binder< ^T, ^F, ^B, ^O
                        when ^F : (static member CreateBinder : EventCallbackFactory * obj * Action< ^T> * ^T * CultureInfo -> EventCallback<ChangeEventArgs>)
                        and ^B : (static member FormatValue : ^T * CultureInfo -> ^O)>
            (eventName: string) (valueAttribute: string) (currentValue: ^T) (callback: ^T -> unit) cultureInfo =
        Attr(fun receiver builder sequence ->
            builder.AddAttribute(sequence, valueAttribute, (^B : (static member FormatValue : ^T * CultureInfo -> ^O)(currentValue, cultureInfo)))
            builder.AddAttribute(sequence + 1, eventName,
                (^F : (static member CreateBinder : EventCallbackFactory * obj * Action< ^T> * ^T * CultureInfo -> EventCallback<ChangeEventArgs>)
                    (EventCallback.Factory, receiver, Action<_> callback, currentValue, cultureInfo)))
            sequence + 2)

    /// <summary>Bind a boolean to the value of a checkbox.</summary>
    /// <param name="value">The current checked state.</param>
    /// <param name="callback">The function called when the checked state changes.</param>
    let inline ``checked`` value callback = binder<bool, EventCallbackFactoryBinderExtensions, BindConverter, bool> "onchange" "checked" value callback null

    /// <summary>
    /// Bind to the value of an input. The value is updated on the <c>oninput</c> event.
    /// </summary>
    module input =

        /// <summary>
        /// Bind a string to the value of an input. The value is updated on the <c>oninput</c> event.
        /// </summary>
        /// <param name="value">The current input state.</param>
        /// <param name="callback">The function called when the input state changes.</param>
        let inline string value callback = binder<string, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback null

        /// <summary>
        /// Bind an integer to the value of an input. The value is updated on the <c>oninput</c> event.
        /// </summary>
        /// <param name="value">The current input state.</param>
        /// <param name="callback">The function called when the input state changes.</param>
        let inline int value callback = binder<int, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback null

        /// <summary>
        /// Bind an int64 to the value of an input. The value is updated on the <c>oninput</c> event.
        /// </summary>
        /// <param name="value">The current input state.</param>
        /// <param name="callback">The function called when the input state changes.</param>
        let inline int64 value callback = binder<int64, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback null

        /// <summary>
        /// Bind a float to the value of an input. The value is updated on the <c>oninput</c> event.
        /// </summary>
        /// <param name="value">The current input state.</param>
        /// <param name="callback">The function called when the input state changes.</param>
        let inline float value callback = binder<float, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback null

        /// <summary>
        /// Bind a float32 to the value of an input. The value is updated on the <c>oninput</c> event.
        /// </summary>
        /// <param name="value">The current input state.</param>
        /// <param name="callback">The function called when the input state changes.</param>
        let inline float32 value callback = binder<float32, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback null

        /// <summary>
        /// Bind a decimal to the value of an input. The value is updated on the <c>oninput</c> event.
        /// </summary>
        /// <param name="value">The current input state.</param>
        /// <param name="callback">The function called when the input state changes.</param>
        let inline decimal value callback = binder<decimal, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback null

        /// <summary>
        /// Bind a DateTime to the value of an input. The value is updated on the <c>oninput</c> event.
        /// </summary>
        /// <param name="value">The current input state.</param>
        /// <param name="callback">The function called when the input state changes.</param>
        let inline dateTime value callback = binder<DateTime, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback null

        /// <summary>
        /// Bind a DateTimeOffset to the value of an input. The value is updated on the <c>oninput</c> event.
        /// </summary>
        /// <param name="value">The current input state.</param>
        /// <param name="callback">The function called when the input state changes.</param>
        let inline dateTimeOffset value callback = binder<DateTimeOffset, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback null

    /// <summary>
    /// Bind to the value of an input. The value is updated on the <c>onchange</c> event.
    /// </summary>
    module change =

        /// <summary>
        /// Bind a string to the value of an input. The value is updated on the <c>onchange</c> event.
        /// </summary>
        /// <param name="value">The current input state.</param>
        /// <param name="callback">The function called when the input state changes.</param>
        let inline string value callback = binder<string, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback null

        /// <summary>
        /// Bind an integer to the value of an input. The value is updated on the <c>onchange</c> event.
        /// </summary>
        /// <param name="value">The current input state.</param>
        /// <param name="callback">The function called when the input state changes.</param>
        let inline int value callback = binder<int, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback null

        /// <summary>
        /// Bind an int64 to the value of an input. The value is updated on the <c>onchange</c> event.
        /// </summary>
        /// <param name="value">The current input state.</param>
        /// <param name="callback">The function called when the input state changes.</param>
        let inline int64 value callback = binder<int64, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback null

        /// <summary>
        /// Bind a float to the value of an input. The value is updated on the <c>onchange</c> event.
        /// </summary>
        /// <param name="value">The current input state.</param>
        /// <param name="callback">The function called when the input state changes.</param>
        let inline float value callback = binder<float, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback null

        /// <summary>
        /// Bind a float32 to the value of an input. The value is updated on the <c>onchange</c> event.
        /// </summary>
        /// <param name="value">The current input state.</param>
        /// <param name="callback">The function called when the input state changes.</param>
        let inline float32 value callback = binder<float32, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback null

        /// <summary>
        /// Bind a decimal to the value of an input. The value is updated on the <c>onchange</c> event.
        /// </summary>
        /// <param name="value">The current input state.</param>
        /// <param name="callback">The function called when the input state changes.</param>
        let inline decimal value callback = binder<decimal, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback null

        /// <summary>
        /// Bind a DateTime to the value of an input. The value is updated on the <c>onchange</c> event.
        /// </summary>
        /// <param name="value">The current input state.</param>
        /// <param name="callback">The function called when the input state changes.</param>
        let inline dateTime value callback = binder<DateTime, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback null

        /// <summary>
        /// Bind a DateTimeOffset to the value of an input. The value is updated on the <c>onchange</c> event.
        /// </summary>
        /// <param name="value">The current input state.</param>
        /// <param name="callback">The function called when the input state changes.</param>
        let inline dateTimeOffset value callback = binder<DateTimeOffset, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback null

    /// <summary>
    /// Bind to the value of an input and convert using the given <see cref="T:System.Globalization.CultureInfo" />.
    /// </summary>
    module withCulture =

        /// <summary>
        /// Bind to the value of an input. The value is updated on the <c>oninput</c> event.
        /// </summary>
        module input =

            /// <summary>
            /// Bind a string to the value of an input. The value is updated on the <c>oninput</c> event.
            /// </summary>
            /// <param name="culture">The culture to use to parse the value.</param>
            /// <param name="value">The current input state.</param>
            /// <param name="callback">The function called when the input state changes.</param>
            let inline string culture value callback = binder<string, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback culture

            /// <summary>
            /// Bind an integer to the value of an input. The value is updated on the <c>oninput</c> event.
            /// </summary>
            /// <param name="culture">The culture to use to parse the value.</param>
            /// <param name="value">The current input state.</param>
            /// <param name="callback">The function called when the input state changes.</param>
            let inline int culture value callback = binder<int, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback culture

            /// <summary>
            /// Bind an int64 to the value of an input. The value is updated on the <c>oninput</c> event.
            /// </summary>
            /// <param name="culture">The culture to use to parse the value.</param>
            /// <param name="value">The current input state.</param>
            /// <param name="callback">The function called when the input state changes.</param>
            let inline int64 culture value callback = binder<int64, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback culture

            /// <summary>
            /// Bind a float to the value of an input. The value is updated on the <c>oninput</c> event.
            /// </summary>
            /// <param name="culture">The culture to use to parse the value.</param>
            /// <param name="value">The current input state.</param>
            /// <param name="callback">The function called when the input state changes.</param>
            let inline float culture value callback = binder<float, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback culture

            /// <summary>
            /// Bind a float32 to the value of an input. The value is updated on the <c>oninput</c> event.
            /// </summary>
            /// <param name="culture">The culture to use to parse the value.</param>
            /// <param name="value">The current input state.</param>
            /// <param name="callback">The function called when the input state changes.</param>
            let inline float32 culture value callback = binder<float32, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback culture

            /// <summary>
            /// Bind a decimal to the value of an input. The value is updated on the <c>oninput</c> event.
            /// </summary>
            /// <param name="culture">The culture to use to parse the value.</param>
            /// <param name="value">The current input state.</param>
            /// <param name="callback">The function called when the input state changes.</param>
            let inline decimal culture value callback = binder<decimal, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback culture

            /// <summary>
            /// Bind a DateTime to the value of an input. The value is updated on the <c>oninput</c> event.
            /// </summary>
            /// <param name="culture">The culture to use to parse the value.</param>
            /// <param name="value">The current input state.</param>
            /// <param name="callback">The function called when the input state changes.</param>
            let inline dateTime culture value callback = binder<DateTime, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback culture

            /// <summary>
            /// Bind a DateTimeOffset to the value of an input. The value is updated on the <c>oninput</c> event.
            /// </summary>
            /// <param name="culture">The culture to use to parse the value.</param>
            /// <param name="value">The current input state.</param>
            /// <param name="callback">The function called when the input state changes.</param>
            let inline dateTimeOffset culture value callback = binder<DateTimeOffset, EventCallbackFactoryBinderExtensions, BindConverter, string> "oninput" "value" value callback culture

        /// <summary>
        /// Bind to the value of an input. The value is updated on the <c>onchange</c> event.
        /// </summary>
        module change =

            /// <summary>
            /// Bind a string to the value of an input. The value is updated on the <c>onchange</c> event.
            /// </summary>
            /// <param name="culture">The culture to use to parse the value.</param>
            /// <param name="value">The current input state.</param>
            /// <param name="callback">The function called when the input state changes.</param>
            let inline string culture value callback = binder<string, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback culture

            /// <summary>
            /// Bind an integer to the value of an input. The value is updated on the <c>onchange</c> event.
            /// </summary>
            /// <param name="culture">The culture to use to parse the value.</param>
            /// <param name="value">The current input state.</param>
            /// <param name="callback">The function called when the input state changes.</param>
            let inline int culture value callback = binder<int, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback culture

            /// <summary>
            /// Bind an int64 to the value of an input. The value is updated on the <c>onchange</c> event.
            /// </summary>
            /// <param name="culture">The culture to use to parse the value.</param>
            /// <param name="value">The current input state.</param>
            /// <param name="callback">The function called when the input state changes.</param>
            let inline int64 culture value callback = binder<int64, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback culture

            /// <summary>
            /// Bind a float to the value of an input. The value is updated on the <c>onchange</c> event.
            /// </summary>
            /// <param name="culture">The culture to use to parse the value.</param>
            /// <param name="value">The current input state.</param>
            /// <param name="callback">The function called when the input state changes.</param>
            let inline float culture value callback = binder<float, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback culture

            /// <summary>
            /// Bind a float32 to the value of an input. The value is updated on the <c>onchange</c> event.
            /// </summary>
            /// <param name="culture">The culture to use to parse the value.</param>
            /// <param name="value">The current input state.</param>
            /// <param name="callback">The function called when the input state changes.</param>
            let inline float32 culture value callback = binder<float32, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback culture

            /// <summary>
            /// Bind a decimal to the value of an input. The value is updated on the <c>onchange</c> event.
            /// </summary>
            /// <param name="culture">The culture to use to parse the value.</param>
            /// <param name="value">The current input state.</param>
            /// <param name="callback">The function called when the input state changes.</param>
            let inline decimal culture value callback = binder<decimal, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback culture

            /// <summary>
            /// Bind a DateTime to the value of an input. The value is updated on the <c>onchange</c> event.
            /// </summary>
            /// <param name="culture">The culture to use to parse the value.</param>
            /// <param name="value">The current input state.</param>
            /// <param name="callback">The function called when the input state changes.</param>
            let inline dateTime culture value callback = binder<DateTime, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback culture

            /// <summary>
            /// Bind a DateTimeOffset to the value of an input. The value is updated on the <c>onchange</c> event.
            /// </summary>
            /// <param name="culture">The culture to use to parse the value.</param>
            /// <param name="value">The current input state.</param>
            /// <param name="callback">The function called when the input state changes.</param>
            let inline dateTimeOffset culture value callback = binder<DateTimeOffset, EventCallbackFactoryBinderExtensions, BindConverter, string> "onchange" "value" value callback culture

/// <summary>Functions to create Virtualize components.</summary>
module virtualize =
    open System.Collections.Generic
    open Microsoft.AspNetCore.Components.Web.Virtualization

    /// <summary>Pass a direct collection of items to a Virtualize component.</summary>
    /// <param name="items">The collection of items.</param>
    /// <example>
    /// <code lang="fsharp">
    /// virtualize.comp {
    ///     let! item = virtualize.items (seq { 1 .. 1000 })
    ///     div { $"Item number {item}" }
    /// }
    /// </code>
    /// </example>
    let inline items (items: IReadOnlyCollection<'item>) =
        VirtualizeItemsDeclaration<'item>(fun b i ->
            let coll =
                match items with
                | :? ICollection<'item> as coll -> coll
                | _ -> Virtualize.Internals.Collection<'item> items
            b.AddAttribute(i, "Items", coll)
            i + 1)

    /// <summary>Generate the items of a Virtualize component on the fly.</summary>
    /// <param name="itemsProvider">The function that generates items on the fly.</param>
    /// <example>
    /// <code lang="fsharp">
    /// virtualize.comp {
    ///     let! item = virtualize.itemsProvider &lt;| fun request ->
    ///         ValueTask(task {
    ///             let items = seq { request.StartIndex .. request.StartIndex + request.Count - 1 }
    ///             return ItemsProviderResult(items, 1000)
    ///         })
    ///     div { $"Item number {item}" }
    /// }
    /// </code>
    /// </example>
    let inline itemsProvider ([<InlineIfLambda>] itemsProvider: ItemsProviderRequest -> ValueTask<ItemsProviderResult<'item>>) =
        VirtualizeItemsDeclaration<'item>(fun b i ->
            b.AddAttribute(i, "ItemsProvider", ItemsProviderDelegate<'item> itemsProvider)
            i + 1)

    /// <summary>Computation expression builder to create a Virtualize component.</summary>
    /// <typeparam name="item">The type of item in the collection to virtualize.</typeparam>
    /// <remarks>
    /// The contents of the computation expression should be:
    ///   1. component parameters, if any;
    ///   2. <c>let!</c> to bind the current item to <see cref="M:items" /> or <see cref="M:itemsProvider" />;
    ///   3. the body of an item.
    /// </remarks>
    /// <example>
    /// <code lang="fsharp">
    /// virtualize.comp {
    ///     virtualize.itemSize 30
    ///     let! item = virtualize.items (seq { 1 .. 1000 })
    ///     div {
    ///         $"Item number {item}"
    ///     }
    /// }
    /// </code>
    /// </example>
    let inline comp<'item> = VirtualizeBuilder<'item>()

    /// <summary>A component parameter indicating the placeholder content shown while an item is loading.</summary>
    /// <param name="v">The placeholder content.</param>
    let inline placeholder ([<InlineIfLambda>] v: PlaceholderContext -> Node) =
        attr.fragmentWith "Placeholder" v

    /// <summary>A component parameter indicating the height of an item in CSS points.</summary>
    /// <param name="v">The height of an item.</param>
    let inline itemSize (v: single) =
        "ItemSize" => v

    /// <summary>A component parameter indicating the number of extra items to load for smooth scrolling.</summary>
    /// <param name="v">The number of extra items to load.</param>
    let inline overscanCount (v: int) =
        "OverscanCount" => v
