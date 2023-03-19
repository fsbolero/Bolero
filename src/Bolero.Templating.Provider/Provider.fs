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

open System.Collections.Concurrent
open System.IO
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
    let cache = ConcurrentDictionary<string, ProvidedTypeDefinition * FileSystemWatcher option>()

    let watchFileChanges key fileName =
        let fullPath = Path.Combine(cfg.ResolutionFolder, fileName) |> Path.Canonicalize
        let fileWatcher = new FileSystemWatcher(
            Path = Path.GetDirectoryName(fullPath),
            Filter = Path.GetFileName(fullPath)
        )

        let lockObj = obj()
        let mutable disposed = false

        let changeHandler = fun _ ->
            lock lockObj (fun _ ->
                if disposed then () else
                cache.TryRemove key |> ignore
                fileWatcher.Dispose()
                disposed <- true
                this.Invalidate())

        fileWatcher.Changed.Add(changeHandler)
        fileWatcher.Deleted.Add(changeHandler)
        fileWatcher.EnableRaisingEvents <- true
        fileWatcher

    do try
        let templateTy = ProvidedTypeDefinition(thisAssembly, rootNamespace, "Template", None, isErased = false)
        let sp = ProvidedStaticParameter("pathOrHtml", typeof<string>)
        sp.AddXmlDoc("The path to an HTML file, or an HTML string directly.")
        templateTy.DefineStaticParameters([sp], fun typename pars ->
            match pars with
            | [| :? string as pathOrHtml |] ->
                let ty, _ =
                    cache.GetOrAdd(pathOrHtml, fun key ->
                        let asm = ProvidedAssembly()
                        let ty = ProvidedTypeDefinition(asm, rootNamespace, typename, Some typeof<TemplateNode>,
                                    isErased = false,
                                    hideObjectMethods = true)
                        let content = Parsing.ParseFileOrContent pathOrHtml cfg.ResolutionFolder
                        CodeGen.Populate ty content
                        asm.AddTypes([ty])
                        let fileWatcher = content.Filename |> Option.map (watchFileChanges key)
                        ty, fileWatcher)
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
