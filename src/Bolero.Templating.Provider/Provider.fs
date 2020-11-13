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
        let sp = ProvidedStaticParameter("pathOrHtml", typeof<string>)
        sp.AddXmlDoc("The path to an HTML file, or an HTML string directly.")
        templateTy.DefineStaticParameters([sp], fun typename pars ->
            let asm = ProvidedAssembly()
            match pars with
            | [| :? string as pathOrHtml |] ->
                let ty = ProvidedTypeDefinition(asm, rootNamespace, typename, Some typeof<TemplateNode>,
                            isErased = false,
                            hideObjectMethods = true)
                CodeGen.Populate ty pathOrHtml cfg.ResolutionFolder
                asm.AddTypes([ty])
                ty
            | x -> failwith $"Unexpected parameter values: {x}"
        )
        templateTy.AddXmlDoc("\
            Provide content from a template HTML file.\n\
            [category: HTML]")
        this.AddNamespace(rootNamespace, [templateTy])
        with exn ->
            // Put the full error, including stack, in the message
            // so that it shows up in the compiler output.
            failwith $"{exn}"
