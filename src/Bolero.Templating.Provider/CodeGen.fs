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

module Bolero.Templating.CodeGen

open System
open System.Reflection
open FSharp.Quotations
open Microsoft.AspNetCore.Components
open ProviderImplementation.ProvidedTypes
open Bolero
open Bolero.TemplatingInternals
open Bolero.Templating.ConvertExpr

let getThis (args: list<Expr>) : Expr<TemplateNode> =
    TExpr.Coerce<TemplateNode>(args.[0])

let MakeCtor (holes: Parsing.Vars) (containerTy: ProvidedTypeDefinition) =
    ProvidedConstructor([], fun args ->
        let holes = TExpr.Array<obj> [
            for KeyValue(_, type') in holes ->
                match type' with
                | Parsing.HoleType.String -> <@ box "" @>
                | Parsing.HoleType.Html -> <@ box Node.Empty @>
                | Parsing.HoleType.Event _ -> <@ box (Events.NoOp<EventArgs>()) @>
                | Parsing.HoleType.DataBinding _ -> <@ box (null, Events.NoOp<ChangeEventArgs>()) @>
                | Parsing.HoleType.Attribute -> <@ box (Attrs []) @>
                | Parsing.HoleType.AttributeValue -> <@ null @>
        ]
        <@@ (%getThis args).Holes <- %holes @@>)

/// Get the argument lists and bodies for methods that fill a hole of the given type.
let HoleMethodBodies (holeType: Parsing.HoleType) : (ProvidedParameter list * (Expr list -> Expr)) list =
    let (=>) name ty = ProvidedParameter(name, ty)
    match holeType with
    | Parsing.HoleType.String ->
        [
            ["value" => typeof<string>], fun args ->
                <@@ box (%%args.[1]: string) @@>
        ]
    | Parsing.HoleType.Html ->
        [
            ["value" => typeof<string>], fun args ->
                <@@ box (Node.Text (%%args.[1]: string)) @@>
            ["value" => typeof<Node>], fun args ->
                <@@ box (%%args.[1]: Node) @@>
        ]
    | Parsing.HoleType.Event argTy ->
        [
            ["value" => EventHandlerOf argTy], fun args ->
                Expr.Coerce(args.[1], typeof<obj>)
        ]
    | Parsing.HoleType.DataBinding Parsing.BindingType.BindString ->
        [
            ["value" => typeof<string>; "set" => typeof<Action<string>>], fun args ->
                <@@ box (box (%%args.[1]: string), Events.OnChange(%%args.[2])) @@>
        ]
    | Parsing.HoleType.DataBinding Parsing.BindingType.BindNumber ->
        [
            ["value" => typeof<int>; "set" => typeof<Action<int>>], fun args ->
                <@@ box (box (%%args.[1]: int), Events.OnChangeInt(%%args.[2])) @@>
            ["value" => typeof<float>; "set" => typeof<Action<float>>], fun args ->
                <@@ box (box (%%args.[1]: float), Events.OnChangeFloat(%%args.[2])) @@>
        ]
    | Parsing.HoleType.DataBinding Parsing.BindingType.BindBool ->
        [
            ["value" => typeof<bool>; "set" => typeof<Action<bool>>], fun args ->
                <@@ box (box (%%args.[1]: bool), Events.OnChangeBool(%%args.[2])) @@>
        ]
    | Parsing.HoleType.Attribute ->
        [
            ["value" => typeof<Attr>], fun args ->
                <@@ box (%%args.[1]: Attr) @@>
            ["value" => typeof<list<Attr>>], fun args ->
                <@@ box (Attrs(%%args.[1]: list<Attr>)) @@>
        ]
    | Parsing.HoleType.AttributeValue ->
        [
            ["value" => typeof<obj>], fun args ->
                <@@ %%args.[1] @@>
        ]

let MakeHoleMethods (holeName: string) (holeType: Parsing.HoleType) (index: int) (containerTy: ProvidedTypeDefinition) =
    [
        for args, value in HoleMethodBodies holeType do
            yield ProvidedMethod(holeName, args, containerTy, fun args ->
                let this = getThis args
                <@@ (%this).Holes.[index] <- %%(value args)
                    %this @@>) :> MemberInfo
    ]

let MakeFinalMethod (filename: option<string>) (subtemplatename: option<string>) (content: Parsing.Parsed) =
    ProvidedMethod("Elt", [], typeof<Node>, fun args ->
        let this = getThis args
        let directExpr =
            let vars = content.Vars |> Map.map (fun k v -> Var(k, TypeOf v))
            let varExprs = vars |> Map.map (fun _ v -> Expr.Var v)
            ((0, ConvertNode varExprs (Parsing.Concat content.Expr) :> Expr), vars)
            ||> Seq.fold (fun (i, e) (KeyValue(_, var)) ->
                let value = <@@ (%this).Holes.[i] @@>
                let value = Expr.Coerce(value, var.Type)
                i + 1, Expr.Let(var, value, e)
            )
            |> snd
        match filename with
        | None ->
            directExpr
        | Some filename ->
            let varNames = TExpr.Array [for KeyValue(k, _) in content.Vars -> <@ k @>]
            let subtemplatename = Option.toObj subtemplatename
            <@@ let vars = Map.ofArray (Array.zip %varNames (%this).Holes)
                match TemplateCache.client.RequestTemplate(filename, subtemplatename) with
                | Some f -> f vars
                | None -> %%directExpr @@>
    )

/// Populate the members of the provided type for one template.
let PopulateOne (filename: option<string>) (subtemplatename: option<string>) (ty: ProvidedTypeDefinition) (content: Parsing.Parsed) =
    ty.AddMembers [
        yield MakeCtor content.Vars ty :> MemberInfo
        yield! content.Vars |> Seq.mapi (fun i (KeyValue(name, type')) ->
            MakeHoleMethods name type' i ty
        ) |> Seq.concat
        yield MakeFinalMethod filename subtemplatename content :> MemberInfo
    ]

/// Populate the members of the provided type for a root template and its nested templates.
let Populate (mainTy: ProvidedTypeDefinition) (pathOrHtml: string) (rootFolder: string) =
    let content = Parsing.ParseFileOrContent pathOrHtml rootFolder
    let filename = content.Filename
    PopulateOne filename None mainTy content.Main
    for KeyValue(name, content) in content.Nested do
        let ty = ProvidedTypeDefinition(name, Some typeof<TemplateNode>,
                    isErased = false,
                    hideObjectMethods = true)
        mainTy.AddMember ty
        PopulateOne filename (Some name) ty content
