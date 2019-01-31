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

module Bolero.Templating.Client.ConvertExpr

open System
open Microsoft.AspNetCore.Blazor
open Bolero
open Bolero.Templating

/// Event handler type whose argument is the given type.
let EventHandlerOf (argType: Type) : Type =
    typedefof<Action<_>>.MakeGenericType([|argType|])

/// Get the .NET type corresponding to a hole type.
let TypeOf (holeType: Parsing.HoleType) : Type =
    match holeType with
    | Parsing.String -> typeof<string>
    | Parsing.Html -> typeof<Node>
    | Parsing.Event argType -> EventHandlerOf argType
    | Parsing.DataBinding _ -> typeof<obj * Action<UIChangeEventArgs>>
    | Parsing.Attribute -> typeof<Attr>

let WrapExpr (innerType: Parsing.HoleType) (outerType: Parsing.HoleType) (expr: obj) : option<obj> =
    if innerType = outerType then None else
    match innerType, outerType with
    | Parsing.Html, Parsing.String ->
        Some (box (Node.Text (unbox expr)))
    | Parsing.Event _, Parsing.Event _ ->
        Some expr
    | Parsing.String, Parsing.DataBinding _ ->
        Some (fst (expr :?> obj * Action<UIChangeEventArgs>))
    | Parsing.Html, Parsing.DataBinding _ ->
        Some (box (Node.Text (fst (expr :?> obj * Action<UIChangeEventArgs>) :?> string)))
    | a, b -> failwithf "Hole name used multiple times with incompatible types (%A, %A)" a b

let WrapAndConvert (vars: Map<string, obj>) (subst: list<Parsing.VarSubstitution>) convert expr =
    let vars = (vars, subst) ||> List.fold (fun vars wrap ->
        let unwrapped = vars.[wrap.name]
        let wrapped = WrapExpr wrap.innerType wrap.outerType unwrapped
        Map.add wrap.name (defaultArg wrapped unwrapped) vars
    )
    convert vars expr

let rec ConvertAttrTextPart (vars: Map<string, obj>) (text: Parsing.Expr) : string =
    match text with
    | Parsing.Concat texts ->
        texts |> List.map (ConvertAttrTextPart vars >> unbox<string>) |> String.Concat
    | Parsing.PlainHtml text ->
        text
    | Parsing.VarContent varName ->
        vars.[varName].ToString()
    | Parsing.WrapVars (subst, text) ->
        WrapAndConvert vars subst ConvertAttrTextPart text
    | Parsing.Fst _ | Parsing.Snd _ | Parsing.Attr _ | Parsing.Elt _ ->
        failwithf "Invalid text: %A" text

let rec ConvertAttrValue (vars: Map<string, obj>) (text: Parsing.Expr) : obj =
    match text with
    | Parsing.Concat texts ->
        texts |> List.map (ConvertAttrTextPart vars >> unbox<string>) |> String.Concat |> box
    | Parsing.PlainHtml text ->
        box text
    | Parsing.VarContent varName ->
        vars.[varName]
    | Parsing.Fst varName ->
        FSharp.Reflection.FSharpValue.GetTupleField(vars.[varName], 0)
    | Parsing.Snd varName ->
        FSharp.Reflection.FSharpValue.GetTupleField(vars.[varName], 1)
    | Parsing.WrapVars (subst, text) ->
        WrapAndConvert vars subst ConvertAttrValue text
    | Parsing.Attr _ | Parsing.Elt _ ->
        failwithf "Invalid attr value: %A" text

let rec ConvertAttr (vars: Map<string, obj>) (attr: Parsing.Expr) : Attr =
    match attr with
    | Parsing.Concat attrs ->
        Attrs (List.map (ConvertAttr vars) attrs)
    | Parsing.Attr (name, value) ->
        Attr (name, ConvertAttrValue vars value)
    | Parsing.VarContent varName ->
        vars.[varName] :?> Attr
    | Parsing.WrapVars (subst, attr) ->
        WrapAndConvert vars subst ConvertAttr attr
    | Parsing.Fst _ | Parsing.Snd _ | Parsing.PlainHtml _ | Parsing.Elt _ ->
        failwithf "Invalid attribute: %A" attr

let rec ConvertNode (vars: Map<string, obj>) (node: Parsing.Expr) : Node =
    match node with
    | Parsing.Concat nodes ->
        Node.Concat (List.map (ConvertNode vars) nodes)
    | Parsing.PlainHtml str ->
        Node.RawHtml str
    | Parsing.Elt (name, attrs, children) ->
        Node.Elt(name, List.map (ConvertAttr vars) attrs, List.map (ConvertNode vars) children)
    | Parsing.VarContent varName ->
        vars.[varName] :?> Node
    | Parsing.WrapVars (subst, node) ->
        WrapAndConvert vars subst ConvertNode node
    | Parsing.Fst _ | Parsing.Snd _ | Parsing.Attr _ ->
        failwithf "Invalid node: %A" node
