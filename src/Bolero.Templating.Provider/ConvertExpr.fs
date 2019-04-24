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

module Bolero.Templating.ConvertExpr

#nowarn "3220" // Using .Item1 instead of fst inside quotations for nicer output IL

open System
open FSharp.Quotations
open Microsoft.AspNetCore.Components
open ProviderImplementation.ProvidedTypes
open Bolero

/// Event handler type whose argument is the given type.
let EventHandlerOf (argType: Type) : Type =
    ProvidedTypeBuilder.MakeGenericType(typedefof<Action<_>>, [argType])

/// Get the .NET type corresponding to a hole type.
let TypeOf (holeType: Parsing.HoleType) : Type =
    match holeType with
    | Parsing.String -> typeof<string>
    | Parsing.Html -> typeof<Node>
    | Parsing.Event argType -> EventHandlerOf argType
    | Parsing.DataBinding _ -> typeof<obj * Action<UIChangeEventArgs>>
    | Parsing.Attribute -> typeof<Attr>
    | Parsing.AttributeValue -> typeof<obj>

/// Wrap the filler `expr`, of type `outerType`, to make it fit into a hole of type `innerType`.
/// Return `None` if no wrapping is needed.
let WrapExpr (innerType: Parsing.HoleType) (outerType: Parsing.HoleType) (expr: Expr) : option<Expr> =
    if innerType = outerType then None else
    match innerType, outerType with
    | Parsing.Html, Parsing.String ->
        <@@ Node.Text %%expr @@>
    | Parsing.AttributeValue, Parsing.String ->
        Expr.Coerce(expr, typeof<obj>)
    | Parsing.Event argTy, Parsing.Event _ ->
        Expr.Coerce(expr, EventHandlerOf argTy)
    | Parsing.String, Parsing.DataBinding _ ->
        <@@ (%%expr: obj * Action<UIChangeEventArgs>).Item1 @@>
    | Parsing.Html, Parsing.DataBinding _ ->
        <@@ Node.Text ((%%expr: obj * Action<UIChangeEventArgs>).Item1.ToString()) @@>
    | Parsing.AttributeValue, Parsing.DataBinding _ ->
        <@@ (%%expr: obj * Action<UIChangeEventArgs>).Item1.ToString() @@>
    | a, b -> failwithf "Hole name used multiple times with incompatible types (%A, %A)" a b
    |> Some

/// Map an expression's vars from its parent, wrapping the expression in let declarations.
let WrapAndConvert (vars: Map<string, Expr>) (subst: list<Parsing.VarSubstitution>) convert expr =
    let vars, addLets = ((vars, id), subst) ||> List.fold (fun (vars, addLets) wrap ->
        let unwrapped = vars.[wrap.name]
        let wrapped = WrapExpr wrap.innerType wrap.outerType unwrapped
        let var = Var(wrap.name, TypeOf wrap.innerType)
        let addLets e = Expr.Let(var, defaultArg wrapped unwrapped, addLets e) |> Expr.Cast
        let vars = Map.add wrap.name (Expr.Var var) vars
        (vars, addLets)
    )
    addLets (convert vars expr)

let rec ConvertAttrTextPart (vars: Map<string, Expr>) (text: Parsing.Expr) : Expr<string> =
    match text with
    | Parsing.Concat texts ->
        let texts = TExpr.Array<string>(Seq.map (ConvertAttrTextPart vars) texts)
        <@ String.Concat %texts @>
    | Parsing.PlainHtml text ->
        <@ text @>
    | Parsing.VarContent varName ->
        let e : Expr<obj> = Expr.Coerce(vars.[varName], typeof<obj>) |> Expr.Cast
        <@ (%e).ToString() @>
    | Parsing.WrapVars (subst, text) ->
        WrapAndConvert vars subst ConvertAttrTextPart text
    | Parsing.Fst _ | Parsing.Snd _ | Parsing.Attr _ | Parsing.Elt _ ->
        failwithf "Invalid text: %A" text

let rec ConvertAttrValue (vars: Map<string, Expr>) (text: Parsing.Expr) : Expr<obj> =
    let box e = Expr.Coerce(e, typeof<obj>) |> Expr.Cast
    match text with
    | Parsing.Concat texts ->
        let texts = TExpr.Array<string>(Seq.map (ConvertAttrTextPart vars) texts)
        box <@ String.Concat %texts @>
    | Parsing.PlainHtml text ->
        box <@ text @>
    | Parsing.VarContent varName ->
        box vars.[varName]
    | Parsing.Fst varName ->
        box (Expr.TupleGet(vars.[varName], 0))
    | Parsing.Snd varName ->
        box (Expr.TupleGet(vars.[varName], 1))
    | Parsing.WrapVars (subst, text) ->
        WrapAndConvert vars subst ConvertAttrValue text
    | Parsing.Attr _ | Parsing.Elt _ ->
        failwithf "Invalid attr value: %A" text

let rec ConvertAttr (vars: Map<string, Expr>) (attr: Parsing.Expr) : Expr<Attr> =
    match attr with
    | Parsing.Concat attrs ->
        let attrs = TExpr.Array<Attr> (Seq.map (ConvertAttr vars) attrs)
        <@ Attr.Attrs (List.ofArray %attrs) @>
    | Parsing.Attr (name, value) ->
        let value = ConvertAttrValue vars value
        <@ Attr.Attr (name, %value) @>
    | Parsing.VarContent varName ->
        vars.[varName] |> Expr.Cast
    | Parsing.WrapVars (subst, attr) ->
        WrapAndConvert vars subst ConvertAttr attr
    | Parsing.Fst _ | Parsing.Snd _ | Parsing.PlainHtml _ | Parsing.Elt _ ->
        failwithf "Invalid attribute: %A" attr

let rec ConvertNode (vars: Map<string, Expr>) (node: Parsing.Expr) : Expr<Node> =
    match node with
    | Parsing.Concat exprs ->
        let exprs = TExpr.Array<Node> (Seq.map (ConvertNode vars) exprs)
        <@ Node.Concat (List.ofArray %exprs) @>
    | Parsing.PlainHtml string ->
        <@ Node.RawHtml string @>
    | Parsing.Elt (name, attrs, children) ->
        let attrs = TExpr.Array<Attr> (Seq.map (ConvertAttr vars) attrs)
        let children = TExpr.Array<Node> (Seq.map (ConvertNode vars) children)
        <@ Node.Elt (name, List.ofArray %attrs, List.ofArray %children) @>
    | Parsing.VarContent varName ->
        vars.[varName] |> Expr.Cast
    | Parsing.WrapVars (subst, node) ->
        WrapAndConvert vars subst ConvertNode node
    | Parsing.Fst _ | Parsing.Snd _ | Parsing.Attr _ ->
        failwithf "Invalid node: %A" node

