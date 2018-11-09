namespace MiniBlazor.Templating

open System.IO
open System.Reflection
open FSharp.Core.CompilerServices
open ProviderImplementation
open ProviderImplementation.ProvidedTypes
type Node = MiniBlazor.Node

[<AutoOpen>]
module private Impl =

    let Populate (ty: ProvidedTypeDefinition) (pathOrHtml: string) =
        let content = Parsing.ParseFileOrContent pathOrHtml
        ProvidedConstructor([], fun _ -> <@@ () @@>)
        |> ty.AddMember
        ProvidedMethod("Elt", [], typeof<Node>, fun _ -> content)
        |> ty.AddMember

[<TypeProvider>]
type Template (cfg: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces(cfg)

    let thisAssembly = Assembly.GetExecutingAssembly()
    let rootNamespace = "MiniBlazor"
    let templateTy = ProvidedTypeDefinition(thisAssembly, rootNamespace, "Template", None)

    do templateTy.DefineStaticParameters(
        [
            ProvidedStaticParameter("pathOrHtml", typeof<string>)
        ], fun typename pars ->
        match pars with
        | [| :? string as pathOrHtml |] ->
            let ty = ProvidedTypeDefinition(thisAssembly, rootNamespace, typename, None)
            Populate ty pathOrHtml
            ty
        | x -> failwithf "Unexpected parameter values: %A" x
    )
    do this.AddNamespace(rootNamespace, [templateTy])

[<assembly:FSharp.Core.CompilerServices.TypeProviderAssembly>]
do ()