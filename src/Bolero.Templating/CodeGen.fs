module Bolero.Templating.CodeGen

open System
open FSharp.Quotations
open Microsoft.AspNetCore.Blazor
open ProviderImplementation.ProvidedTypes
open Bolero
open Bolero.TemplatingInternals

let getThis (args: list<Expr>) : Expr<TemplateNode> =
    Expr.Coerce(args.[0], typeof<TemplateNode>)
    |> Expr.Cast

let MakeCtor (holes: Parsing.Holes) (containerTy: ProvidedTypeDefinition) =
    ProvidedConstructor([], fun args ->
        let holes = Expr.TypedArray<obj> [
            for KeyValue(_, hole) in holes ->
                match hole.Type with
                | Parsing.HoleType.String -> <@ box "" @>
                | Parsing.HoleType.Html -> <@ box Node.Empty @>
                | Parsing.HoleType.Event _ -> <@ box (Events.NoOp<UIEventArgs>()) @>
                | Parsing.HoleType.DataBinding -> <@ box ((null: string), Events.NoOp<string>()) @>
        ]
        <@@ (%getThis args).Holes <- %holes @@>)
    |> containerTy.AddMember

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
    | Parsing.HoleType.DataBinding ->
        [
            ["value" => typeof<string>; "set" => typeof<Action<string>>], fun args ->
                <@@ box ((%%args.[1]: string), (%%args.[2]: Action<string>)) @@>
        ]

let MakeHoleMethods (holeName: string) (holeType: Parsing.HoleType) (index: int) (containerTy: ProvidedTypeDefinition) =
    for args, value in HoleMethodBodies holeType do
        ProvidedMethod(holeName, args, containerTy, fun args ->
            let this = getThis args
            <@@ (%this).Holes.[index] <- %%(value args)
                %this @@>)
        |> containerTy.AddMember

let MakeFinalMethod (content: Parsing.Parsed<Node>) (containerTy: ProvidedTypeDefinition) =
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
    |> containerTy.AddMember

let Populate (ty: ProvidedTypeDefinition) (pathOrHtml: string) =
    let content = Parsing.ParseFileOrContent pathOrHtml
    MakeCtor content.Holes ty
    content.Holes |> Seq.iteri (fun i (KeyValue(name, hole)) ->
        MakeHoleMethods name hole.Type i ty
    )
    MakeFinalMethod content ty
