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

module Utility
#load "../.fake/build.fsx/intellisense.fsx"

open System.IO
open Fake.Core
open Fake.DotNet
open Fake.IO

let [<Literal>] slnDir = __SOURCE_DIRECTORY__ + "/.."

let dotnet' dir env cmd args =
    Printf.kprintf (fun args ->
        let env =
            (Process.createEnvironmentMap(), env)
            ||> List.fold (fun map (k, v) -> Map.add k v map)
        let r =
            DotNet.exec
                (DotNet.Options.withWorkingDirectory dir
                >> DotNet.Options.withEnvironment env)
                cmd args
        for msg in r.Results do
            eprintfn "%s" msg.Message
        if not r.OK then
            failwithf "dotnet %s failed" cmd
    ) args

let dotnet cmd args =
    dotnet' slnDir [] cmd args

/// `cache f x` returns `f x` the first time,
/// and re-returns the first result on subsequent calls.
let cache f =
    let res = ref None
    fun x ->
        match !res with
        | Some y -> y
        | None ->
            let y = f x
            res := Some y
            y

let getArgOpt prefix = cache <| fun (o: TargetParameter) ->
    let rec go = function
        | s :: m :: _ when s = prefix -> Some m
        | _ :: rest -> go rest
        | [] -> None
    go o.Context.Arguments

let getArg prefix ``default`` =
    getArgOpt prefix
    >> Option.defaultValue ``default``

let getFlag flag = cache <| fun (o: TargetParameter) ->
    List.contains flag o.Context.Arguments

/// Generate a file at the given location, but leave it unchanged
/// if the generated contents are identical to the existing file.
/// `generate` receives the actual filename it should write to,
/// which may be a temp file.
let unchangedIfIdentical filename generate =
    if File.Exists(filename) then
        let tempFilename = Path.GetTempFileName()
        generate tempFilename
        if not (Shell.compareFiles true filename tempFilename) then
            File.Delete(filename)
            File.Move(tempFilename, filename)
    else
        generate filename

// Manage the fact that we run fake from the .paket/fake directory
let origDir = Directory.GetCurrentDirectory()
Directory.SetCurrentDirectory slnDir
Target.createFinal "reset-dir" (fun _ ->
    Directory.SetCurrentDirectory(origDir)
)
Target.activateFinal "reset-dir"
