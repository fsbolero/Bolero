module Utility
#load "../../.fake/build.fsx/intellisense.fsx"

open System.IO
open Fake.Core
open Fake.DotNet

let slnDir = Path.Combine(__SOURCE_DIRECTORY__, "..", "..")

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

let getArgOpt (o: TargetParameter) prefix =
    let rec go = function
        | s :: m :: _ when s = prefix -> Some m
        | _ :: rest -> go rest
        | [] -> None
    go o.Context.Arguments

let getArg o prefix ``default`` =
    getArgOpt o prefix
    |> Option.defaultValue ``default``

// Manage the fact that we run fake from the .paket/fake directory
let origDir = Directory.GetCurrentDirectory()
Directory.SetCurrentDirectory slnDir
Target.createFinal "reset-dir" (fun _ ->
    Directory.SetCurrentDirectory(origDir)
)
Target.activateFinal "reset-dir"
