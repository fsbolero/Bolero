namespace Bolero.Templating

open System.Reflection
open FSharp.Core.CompilerServices
open FSharp.Quotations
open ProviderImplementation.ProvidedTypes
open Bolero

[<AutoOpen>]
module private Impl =

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
                    | Parsing.HoleType.Event -> <@ box (ignore : unit -> unit) @>
            ]
            <@@ (%getThis args).Holes <- %holes @@>)
        |> containerTy.AddMember

    let HoleMethodBodies (holeType: Parsing.HoleType) =
        match holeType with
        | Parsing.HoleType.String ->
            [
                typeof<string>, fun e -> <@@ box (%%e: string) @@>
            ]
        | Parsing.HoleType.Html ->
            [
                typeof<string>, fun e -> <@@ box (Node.Text (%%e: string)) @@>
                typeof<Node>, fun e -> <@@ box (%%e: Node) @@>
            ]
        | Parsing.HoleType.Event ->
            [
                typeof<unit -> unit>, fun e -> <@@ box (%%e: unit -> unit) @@>
            ]

    let MakeHoleMethods (holeName: string) (holeType: Parsing.HoleType) (index: int) (containerTy: ProvidedTypeDefinition) =
        for argTy, value in HoleMethodBodies holeType do
            ProvidedMethod(holeName, [ProvidedParameter("value", argTy)], containerTy, fun args ->
                let this = getThis args
                <@@ (%this).Holes.[index] <- %%(value args.[1])
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

[<TypeProvider>]
type Template (cfg: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces(cfg,
        assemblyReplacementMap = ["Bolero.Templating", "Bolero"],
        addDefaultProbingLocation = true)

    let asm = ProvidedAssembly()
    let rootNamespace = "Bolero"

    do try
        let templateTy = ProvidedTypeDefinition(asm, rootNamespace, "Template", None, isErased = false)
        templateTy.DefineStaticParameters(
            [
                ProvidedStaticParameter("pathOrHtml", typeof<string>)
            ], fun typename pars ->
            match pars with
            | [| :? string as pathOrHtml |] ->
                let ty = ProvidedTypeDefinition(asm, rootNamespace, typename, Some typeof<TemplateNode>, isErased = false)
                Populate ty pathOrHtml
                asm.AddTypes([ty])
                ty
            | x -> failwithf "Unexpected parameter values: %A" x
        )
        this.AddNamespace(rootNamespace, [templateTy])
        with exn ->
            // Put the full error, including stack, in the message
            // so that it shows up in the compiler output.
            failwithf "%A" exn