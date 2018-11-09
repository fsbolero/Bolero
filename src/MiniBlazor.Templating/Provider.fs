namespace MiniBlazor.Templating

open System.Reflection
open FSharp.Core.CompilerServices
open FSharp.Quotations
open ProviderImplementation.ProvidedTypes
type internal Node = MiniBlazor.Node

[<AutoOpen>]
module private Impl =

    let MakeCtor (holes: Parsing.Holes) (containerTy: ProvidedTypeDefinition) =
        ProvidedConstructor([], fun _ ->
            Expr.TypedArray<obj> [
                for KeyValue(_, hole) in holes ->
                    match hole.Type with
                    | Parsing.HoleType.String -> <@ box "" @>
                    | Parsing.HoleType.Html -> <@ box Node.Empty @>
                    | Parsing.HoleType.Event -> <@ box (ignore : unit -> unit) @>
            ]
            :> _)
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
                <@@ let this = (%%args.[0] : obj) :?> obj[]
                    this.[index] <- %%(value args.[1])
                    this @@>)
            |> containerTy.AddMember

    let MakeFinalMethod (content: Parsing.Parsed<Node>) (containerTy: ProvidedTypeDefinition) =
        ProvidedMethod("Elt", [], typeof<Node>, fun args ->
            let thisVar = Var("holes", typeof<obj[]>)
            Expr.Let(thisVar, <@ (%%args.[0] : obj) :?> obj[] @>,
                let this : Expr<obj[]> = Expr.Var thisVar |> Expr.Cast
                ((0, content.Expr :> Expr), content.Holes)
                ||> Seq.fold (fun (i, e) (KeyValue(_, hole)) ->
                    let value = <@@ (%this).[i] @@>
                    let value = Expr.Coerce(value, Parsing.HoleType.TypeOf hole.Type)
                    i + 1, Expr.Let(hole.Var, value, e)
                )
                |> snd
            )
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
        assemblyReplacementMap = ["MiniBlazor.Templating", "MiniBlazor"],
        addDefaultProbingLocation = true)

    let thisAssembly = Assembly.GetExecutingAssembly()
    let rootNamespace = "MiniBlazor"
    let templateTy = ProvidedTypeDefinition(thisAssembly, rootNamespace, "Template", Some typeof<obj>)

    do templateTy.DefineStaticParameters(
        [
            ProvidedStaticParameter("pathOrHtml", typeof<string>)
        ], fun typename pars ->
        match pars with
        | [| :? string as pathOrHtml |] ->
            let ty = ProvidedTypeDefinition(thisAssembly, rootNamespace, typename, Some typeof<obj>)
            Populate ty pathOrHtml
            ty
        | x -> failwithf "Unexpected parameter values: %A" x
    )
    do this.AddNamespace(rootNamespace, [templateTy])
