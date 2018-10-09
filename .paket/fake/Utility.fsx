module Utility
#load "../../.fake/build.fsx/intellisense.fsx"

open System.IO
open Fake.Core
open Fake.DotNet

let slnDir = Path.Combine(__SOURCE_DIRECTORY__, "..", "..")

let dotnet' dir cmd args =
    Printf.kprintf (fun args ->
        let r =
            DotNet.exec (fun o ->
                { o with WorkingDirectory = dir }
            ) cmd args
        for msg in r.Results do
            eprintfn "%s" msg.Message
        if not r.OK then
            failwithf "dotnet %s failed" cmd
    ) args

let dotnet cmd args =
    dotnet' slnDir cmd args

let getArg (o: TargetParameter) prefix ``default`` =
    let rec go = function
        | s :: m :: _ when s = prefix -> m
        | _ :: rest -> go rest
        | [] -> ``default``
    go o.Context.Arguments

// Manage the fact that we run fake from the .paket/fake directory
let origDir = Directory.GetCurrentDirectory()
Directory.SetCurrentDirectory slnDir
Target.createFinal "reset-dir" (fun _ ->
    Directory.SetCurrentDirectory(origDir)
)
Target.activateFinal "reset-dir"
