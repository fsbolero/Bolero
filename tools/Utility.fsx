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

#if UTILITY_FROM_PAKET
#load "../../../../.fake/build.fsx/intellisense.fsx"
let [<Literal>] slnDir = __SOURCE_DIRECTORY__ + "/../../../.."
#else
#load "../.fake/build.fsx/intellisense.fsx"
let [<Literal>] slnDir = __SOURCE_DIRECTORY__ + "/.."
#endif

open System.IO
open Fake.Core
open Fake.DotNet
open Fake.IO

let runProc dir env cmd args transform onError =
    let out =
        CreateProcess.fromRawCommand cmd args
        |> CreateProcess.withWorkingDirectory dir
        |> CreateProcess.withEnvironment env
        |> transform
        |> Proc.run
    if out.ExitCode <> 0 then onError out.Result
    out.Result

let dotnet' dir env cmd args =
    runProc dir env cmd args (CreateProcess.withToolType (ToolType.CreateLocalTool())) (fun _ -> failwithf "Command %s failed" cmd)

let dotnet cmd args =
    dotnet' slnDir [] cmd args

let dotnetOutput' dir env cmd args =
    let transform = CreateProcess.withToolType (ToolType.CreateLocalTool()) >> CreateProcess.redirectOutput
    let onError (r: ProcessOutput) = Trace.traceError r.Error
    (runProc dir env cmd args transform onError).Output

let dotnetOutput cmd args =
    dotnetOutput' slnDir [] cmd args

let shellOutput' dir env cmd args =
    let onError (r: ProcessOutput) = Trace.traceError r.Error
    (runProc dir env cmd args CreateProcess.redirectOutput onError).Output

let shellOutput cmd args =
    shellOutput' slnDir [] cmd args

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
