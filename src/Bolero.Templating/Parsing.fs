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

module Bolero.Templating.Parsing

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open FSharp.Quotations
open FSharp.Reflection
open Microsoft.AspNetCore.Blazor
open Microsoft.AspNetCore.Blazor.Components
open HtmlAgilityPack
open Bolero
open Bolero.TemplatingInternals
open ProviderImplementation.ProvidedTypes

/// Available value types for a `bind` attribute.
type BindingType =
    | BindString
    | BindNumber
    | BindBool

/// Available hole kinds.
type HoleType =
    /// A plain string hole (eg an attribute value).
    | String
    /// An HTML node hole.
    | Html
    /// An `onXXX` event handler hole.
    | Event of argType: Type
    /// A `bind` attribute hole.
    | DataBinding of BindingType
    /// An attribute hole.
    | Attribute

module HoleType =

    /// Try to find a common supertype for two hole types.
    let Merge (holeName: string) (t1: HoleType) (t2: HoleType) : HoleType =
        if t1 = t2 then t1 else
        match t1, t2 with
        | String, Html | Html, String -> String
        | Event _, Event _ -> Event typeof<UIEventArgs>
        | DataBinding valType, String | String, DataBinding valType
        | DataBinding valType, Html | Html, DataBinding valType -> DataBinding valType
        | _ -> failwithf "Hole name used multiple times with incompatible types: %s" holeName

    /// Get the .NET type of the event handler argument for the given event name.
    let EventArg (name: string) : Type =
        match name with
// BEGIN EVENTS
        | "onfocus" -> typeof<UIFocusEventArgs>
        | "onblur" -> typeof<UIFocusEventArgs>
        | "onfocusin" -> typeof<UIFocusEventArgs>
        | "onfocusout" -> typeof<UIFocusEventArgs>
        | "onmouseover" -> typeof<UIMouseEventArgs>
        | "onmouseout" -> typeof<UIMouseEventArgs>
        | "onmousemove" -> typeof<UIMouseEventArgs>
        | "onmousedown" -> typeof<UIMouseEventArgs>
        | "onmouseup" -> typeof<UIMouseEventArgs>
        | "onclick" -> typeof<UIMouseEventArgs>
        | "ondblclick" -> typeof<UIMouseEventArgs>
        | "onwheel" -> typeof<UIMouseEventArgs>
        | "onmousewheel" -> typeof<UIMouseEventArgs>
        | "oncontextmenu" -> typeof<UIMouseEventArgs>
        | "ondrag" -> typeof<UIDragEventArgs>
        | "ondragend" -> typeof<UIDragEventArgs>
        | "ondragenter" -> typeof<UIDragEventArgs>
        | "ondragleave" -> typeof<UIDragEventArgs>
        | "ondragover" -> typeof<UIDragEventArgs>
        | "ondragstart" -> typeof<UIDragEventArgs>
        | "ondrop" -> typeof<UIDragEventArgs>
        | "onkeydown" -> typeof<UIKeyboardEventArgs>
        | "onkeyup" -> typeof<UIKeyboardEventArgs>
        | "onkeypress" -> typeof<UIKeyboardEventArgs>
        | "onchange" -> typeof<UIChangeEventArgs>
        | "oninput" -> typeof<UIChangeEventArgs>
        | "oncopy" -> typeof<UIClipboardEventArgs>
        | "oncut" -> typeof<UIClipboardEventArgs>
        | "onpaste" -> typeof<UIClipboardEventArgs>
        | "ontouchcancel" -> typeof<UITouchEventArgs>
        | "ontouchend" -> typeof<UITouchEventArgs>
        | "ontouchmove" -> typeof<UITouchEventArgs>
        | "ontouchstart" -> typeof<UITouchEventArgs>
        | "ontouchenter" -> typeof<UITouchEventArgs>
        | "ontouchleave" -> typeof<UITouchEventArgs>
        | "onpointercapture" -> typeof<UIPointerEventArgs>
        | "onlostpointercapture" -> typeof<UIPointerEventArgs>
        | "onpointercancel" -> typeof<UIPointerEventArgs>
        | "onpointerdown" -> typeof<UIPointerEventArgs>
        | "onpointerenter" -> typeof<UIPointerEventArgs>
        | "onpointerleave" -> typeof<UIPointerEventArgs>
        | "onpointermove" -> typeof<UIPointerEventArgs>
        | "onpointerout" -> typeof<UIPointerEventArgs>
        | "onpointerover" -> typeof<UIPointerEventArgs>
        | "onpointerup" -> typeof<UIPointerEventArgs>
        | "onloadstart" -> typeof<UIProgressEventArgs>
        | "ontimeout" -> typeof<UIProgressEventArgs>
        | "onabort" -> typeof<UIProgressEventArgs>
        | "onload" -> typeof<UIProgressEventArgs>
        | "onloadend" -> typeof<UIProgressEventArgs>
        | "onprogress" -> typeof<UIProgressEventArgs>
        | "onerror" -> typeof<UIProgressEventArgs>
// END EVENTS
        | _ -> typeof<UIEventArgs>

/// Matches a ${HoleName} anywhere in a string.
let HoleRE = Regex(@"\${(\w+)}", RegexOptions.Compiled)

/// Map the var named `name`: it has type `innerType` in a given subexpression,
/// and `outerType` in its parent.
type VarSubstitution =
    {
        name: string
        innerType: HoleType
        outerType: HoleType
    }

type Expr =
    | Concat of list<Expr>
    | PlainHtml of string
    | Elt of name: string * attrs: list<Expr> * children: list<Expr>
    | Attr of name: string * value: Expr
    | VarContent of varName: string
    | WrapVars of vars: list<VarSubstitution> * expr: Expr
    | Fst of varName: string
    | Snd of varName: string

type Vars = Map<string, HoleType>

module Vars =

    /// Merge the vars from two subsets of a template.
    let Merge (vars1: Vars) (vars2: Vars) =
        (vars1, vars2) ||> Map.fold (fun map key type' ->
            let type' =
                match Map.tryFind key map with
                | None -> type'
                | Some type2 -> HoleType.Merge key type' type2
            Map.add key type' map
        )

    /// Merge the vars from many subsets of a template.
    let MergeMany (vars: seq<Vars>) =
        Seq.fold Merge Map.empty vars

/// A compiled expression for a subset of a template together with the vars it contains.
type Parsed =
    {
        Vars: Vars
        Expr: list<Expr>
    }

let NoVars e =
    { Vars = Map.empty; Expr = e }

let WithVars vars e =
    { Vars = vars; Expr = e }

let HasVars p =
    not (Map.isEmpty p.Vars)

module Parsed =

    let private substVars (finalVars: Vars) (p: Parsed) =
        let substs = ([], p.Vars) ||> Map.fold (fun substs k type' ->
            match Map.tryFind k finalVars with
            | Some type2 when type' <> type2 ->
                { name = k; innerType = type'; outerType = type2 } :: substs
            | _ -> substs
        )
        match substs with
        | [] -> p.Expr
        | l -> [WrapVars(l, Concat p.Expr)]

    let private mergeConsecutiveTexts (exprs: seq<Expr>) : seq<Expr> =
        let currentHtml = StringBuilder()
        let res = ResizeArray()
        let pushHtml () =
            let s = currentHtml.ToString()
            if s <> "" then
                res.Add(PlainHtml s)
                currentHtml.Clear() |> ignore
        let rec go = function
            | Concat es ->
                List.iter go es
            | PlainHtml t ->
                currentHtml.Append(t) |> ignore
            | e ->
                pushHtml()
                res.Add(e)
        Seq.iter go exprs
        pushHtml()
        res :> _

    let Concat (p: seq<Parsed>) : Parsed =
        let vars =
            p
            |> Seq.map (fun p -> p.Vars)
            |> Vars.MergeMany
        let exprs =
            p
            |> Seq.collect (substVars vars)
            |> mergeConsecutiveTexts
            |> List.ofSeq
        WithVars vars exprs

    let Map2 (f: list<Expr> -> list<Expr> -> list<Expr>) (p1: Parsed) (p2: Parsed) : Parsed =
        let vars = Vars.Merge p1.Vars p2.Vars
        let e1 = substVars vars p1
        let e2 = substVars vars p2
        WithVars vars (f e1 e2)

/// Parse a piece of text, which can be either a text node or an attribute value.
let ParseText (t: string) (varType: HoleType) : Parsed =
    let parse = HoleRE.Matches(t) |> Seq.cast<Match> |> Array.ofSeq
    if Array.isEmpty parse then NoVars [PlainHtml t] else
    let parts = ResizeArray()
    let mutable lastHoleEnd = 0
    let mutable vars = Map.empty
    for p in parse do
        if p.Index > lastHoleEnd then
            parts.Add(PlainHtml t.[lastHoleEnd..p.Index - 1])
        let varName = p.Groups.[1].Value
        if not (Map.containsKey varName vars) then
            vars <- Map.add varName varType vars
        parts.Add(VarContent varName)
        lastHoleEnd <- p.Index + p.Length
    if lastHoleEnd < t.Length then
        parts.Add(PlainHtml t.[lastHoleEnd..t.Length - 1])
    WithVars vars (parts.ToArray() |> List.ofSeq)

/// None if this is not a data binding.
/// Some None if this is a data binding without specified event.
/// Some (Some "onxyz") if this is a data binding with a specified event.
let GetDataBindingEvent = function
    | "bind-oninput" -> Some (Some "oninput")
    | "bind-onchange" -> Some (Some "onchange")
    | "bind" -> Some None
    | _ -> None

/// Figure out if this is a data binding attribute, and if so, what value type and event it binds.
let (|DataBinding|_|) (ownerNode: HtmlNode) (attrName: string) : option<BindingType * string> =
    match GetDataBindingEvent attrName with
    | None -> None
    | Some ev ->
    let nodeName = ownerNode.Name
    if nodeName = "textarea" then
        Some (BindingType.BindString, defaultArg ev "oninput")
    elif nodeName = "select" then
        Some (BindingType.BindString, defaultArg ev "onchange")
    elif nodeName = "input" then
        match ownerNode.GetAttributeValue("type", "text") with
        | "number" -> Some (BindingType.BindNumber, defaultArg ev "oninput")
        | "checkbox" -> Some (BindingType.BindBool, defaultArg ev "onchange")
        | _ -> Some (BindingType.BindString, defaultArg ev "oninput")
    else None

let MakeEventHandler (attrName: string) (varName: string) : Parsed =
    let argType = HoleType.EventArg attrName
    WithVars (Map [varName, Event argType]) [Attr(attrName, VarContent varName)]

let MakeDataBinding (varName: string) (valType: BindingType) (eventName: string) : Parsed =
    let valueAttrName =
        match valType with
        | BindingType.BindNumber | BindingType.BindString -> "value"
        | BindingType.BindBool -> "checked"
    WithVars (Map [varName, DataBinding valType]) [
        Attr(valueAttrName, Fst varName)
        Attr(eventName, Snd varName)
    ]

let ParseAttribute (ownerNode: HtmlNode) (attr: HtmlAttribute) : Parsed =
    let name = attr.Name
    let parsed = ParseText attr.Value HoleType.String
    match name, parsed.Expr with
    | _, [VarContent _] when name = "attr" ->
        parsed
    | _, [VarContent varName] when name.StartsWith "on" ->
        MakeEventHandler name varName
    | DataBinding ownerNode (valType, eventName), [VarContent varName] ->
        MakeDataBinding varName valType eventName
    | _ ->
        WithVars parsed.Vars [Attr(name, Concat parsed.Expr)]

let rec ParseNode (node: HtmlNode) : Parsed =
    match node.NodeType with
    | HtmlNodeType.Element ->
        let name = node.Name
        let attrs =
            node.Attributes
            |> Seq.map (ParseAttribute node)
            |> Parsed.Concat
        let children =
            node.ChildNodes
            |> Seq.map ParseNode
            |> Parsed.Concat
        if HasVars attrs || HasVars children then
            (attrs, children)
            ||> Parsed.Map2 (fun attrs children ->
                [Elt (name, attrs, children)])
        else
            // Node has no vars, we can represent it as raw HTML for performance.
            let rec removeComments (n: HtmlNode) =
                if isNull n then () else
                let nxt = n.NextSibling
                match n.NodeType with
                | HtmlNodeType.Text | HtmlNodeType.Element -> ()
                | _ -> n.Remove()
                removeComments nxt
            NoVars [PlainHtml node.OuterHtml]
    | HtmlNodeType.Text ->
        // Using .InnerHtml and RawHtml to properly interpret HTML entities.
        ParseText (node :?> HtmlTextNode).InnerHtml HoleType.Html
    | _ ->
        NoVars [] // Ignore comments

let ParseOneTemplate (nodes: HtmlNodeCollection) : Parsed =
    nodes
    |> Seq.map ParseNode
    |> Parsed.Concat

type ParsedTemplates =
    {
        Main: Parsed
        Nested: Map<string, Parsed>
    }

let ParseDoc (doc: HtmlDocument) : ParsedTemplates =
    let nested =
        let templateNodes =
            match doc.DocumentNode.SelectNodes("//template") with
            | null -> [||]
            | nodes -> Array.ofSeq nodes
        // Remove before processing so that 2-level nested templates don't appear in their parent
        templateNodes
        |> Seq.iter (fun n -> n.Remove())
        templateNodes
        |> Seq.map (fun n ->
            match n.GetAttributeValue("id", null) with
            | null ->
                failwithf "Nested template must have an id" // at %i:%i" n.Line n.LinePosition
            | id ->
                let parsed = ParseOneTemplate n.ChildNodes
                n.Remove()
                (id, parsed)
        )
        |> Map.ofSeq
    let main = ParseOneTemplate doc.DocumentNode.ChildNodes
    { Main = main; Nested = nested }

/// Get the HTML document for the given type provider argument, either inline or from a file.
let GetDoc (fileOrContent: string) (rootFolder: string) : HtmlDocument =
    let doc = HtmlDocument()
    if fileOrContent.Contains("<") then
        doc.LoadHtml(fileOrContent)
    else
        doc.Load(Path.Combine(rootFolder, fileOrContent))
    doc

/// Parse a type provider argument into a set of templates.
let ParseFileOrContent (fileOrContent: string) (rootFolder: string) : ParsedTemplates =
    GetDoc fileOrContent rootFolder
    |> ParseDoc
