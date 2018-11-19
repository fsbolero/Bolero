namespace Bolero.Templating

open System.Reflection
open FSharp.Core.CompilerServices
open ProviderImplementation.ProvidedTypes
open Bolero.TemplatingInternals

[<TypeProvider>]
type Template (cfg: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces(cfg,
        assemblyReplacementMap = ["Bolero.Templating", "Bolero"],
        addDefaultProbingLocation = true)

    let thisAssembly = Assembly.GetExecutingAssembly()
    let rootNamespace = "Bolero"

    do try
        let templateTy = ProvidedTypeDefinition(thisAssembly, rootNamespace, "Template", None, isErased = false)
        templateTy.DefineStaticParameters(
            [
                ProvidedStaticParameter("pathOrHtml", typeof<string>)
            ], fun typename pars ->
            let asm = ProvidedAssembly()
            match pars with
            | [| :? string as pathOrHtml |] ->
                let ty = ProvidedTypeDefinition(asm, rootNamespace, typename, Some typeof<TemplateNode>,
                            isErased = false,
                            hideObjectMethods = true)
                CodeGen.Populate ty pathOrHtml cfg.ResolutionFolder
                asm.AddTypes([ty])
                ty
            | x -> failwithf "Unexpected parameter values: %A" x
        )
        this.AddNamespace(rootNamespace, [templateTy])
        with exn ->
            // Put the full error, including stack, in the message
            // so that it shows up in the compiler output.
            failwithf "%A" exn
