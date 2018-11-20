module Bolero.Templating.CodeGen

open System
open FSharp.Quotations
open Microsoft.AspNetCore.Blazor
open ProviderImplementation.ProvidedTypes
open Bolero
open Bolero.TemplatingInternals
open System.Reflection

let getThis (args: list<Expr>) : Expr<TemplateNode> =
    TExpr.Coerce<TemplateNode>(args.[0])

let MakeCtor (holes: Parsing.Holes) (containerTy: ProvidedTypeDefinition) =
    ProvidedConstructor([], fun args ->
        let holes = TExpr.Array<obj> [
            for KeyValue(_, hole) in holes ->
                match hole.Type with
                | Parsing.HoleType.String -> <@ box "" @>
                | Parsing.HoleType.Html -> <@ box Node.Empty @>
                | Parsing.HoleType.Event _ -> <@ box (Events.NoOp<UIEventArgs>()) @>
                | Parsing.HoleType.DataBinding _ -> <@ box ("", Events.NoOp<UIChangeEventArgs>()) @>
                | Parsing.HoleType.Attribute -> <@ box (Attrs []) @>
        ]
        <@@ (%getThis args).Holes <- %holes @@>)

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
            ["value" => Parsing.HoleType.EventHandlerOf argTy], fun args ->
                Expr.Coerce(args.[1], typeof<obj>)
        ]
    | Parsing.HoleType.DataBinding Parsing.BindingType.String ->
        [
            ["value" => typeof<string>; "set" => typeof<Action<string>>], fun args ->
                <@@ box ((%%args.[1]: string), Events.OnChange(%%args.[2])) @@>
        ]
    | Parsing.HoleType.DataBinding Parsing.BindingType.Number ->
        [
            ["value" => typeof<int>; "set" => Parsing.HoleType.EventHandlerOf typeof<int>], fun args ->
                <@@ box (string (%%args.[1]: int), Events.OnChangeInt(%%args.[2])) @@>
            ["value" => typeof<float>; "set" => typeof<Action<float>>], fun args ->
                <@@ box (string (%%args.[1]: float), Events.OnChangeFloat(%%args.[2])) @@>
        ]
    | Parsing.HoleType.DataBinding Parsing.BindingType.Bool ->
        [
            ["value" => typeof<bool>; "set" => typeof<Action<bool>>], fun args ->
                <@@ box (string (%%args.[1]: bool), Events.OnChangeBool(%%args.[2])) @@>
        ]
    | Parsing.HoleType.Attribute ->
        [
            ["value" => typeof<Attr>], fun args ->
                <@@ box (%%args.[1]: Attr) @@>
            ["value" => typeof<list<Attr>>], fun args ->
                <@@ box (Attrs(%%args.[1]: list<Attr>)) @@>
        ]

let MakeHoleMethods (holeName: string) (holeType: Parsing.HoleType) (index: int) (containerTy: ProvidedTypeDefinition) =
    [
        for args, value in HoleMethodBodies holeType do
            yield ProvidedMethod(holeName, args, containerTy, fun args ->
                let this = getThis args
                <@@ (%this).Holes.[index] <- %%(value args)
                    %this @@>) :> MemberInfo
    ]

let MakeFinalMethod (content: Parsing.Parsed<Node>) =
    ProvidedMethod("Elt", [], typeof<Node>, fun args ->
        let this = getThis args
        ((0, content.Expr :> Expr), content.Holes)
        ||> Seq.fold (fun (i, e) (KeyValue(_, hole)) ->
            let value = <@@ (%this).Holes.[i] @@>
            let value = Expr.Coerce(value, Parsing.HoleType.TypeOf hole.Type)
            i + 1, Expr.Let(hole.Var, value, e)
        )
        |> snd
    )

let PopulateOne (ty: ProvidedTypeDefinition) (content: Parsing.Parsed<Node>) =
    ty.AddMembers [
        yield MakeCtor content.Holes ty :> MemberInfo
        yield! content.Holes |> Seq.mapi (fun i (KeyValue(name, hole)) ->
            MakeHoleMethods name hole.Type i ty
        ) |> Seq.concat
        yield MakeFinalMethod content :> MemberInfo
    ]

let Populate (mainTy: ProvidedTypeDefinition) (pathOrHtml: string) (rootFolder: string) =
    let content = Parsing.ParseFileOrContent pathOrHtml rootFolder
    PopulateOne mainTy content.Main
    for KeyValue(name, content) in content.Nested do
        let ty = ProvidedTypeDefinition(name, Some typeof<TemplateNode>,
                    isErased = false,
                    hideObjectMethods = true)
        mainTy.AddMember ty
        PopulateOne ty content
