#r "paket: groupref fake //"
#load "src/fake/Utility.fsx"

open System.IO
open Fake.Core
open Fake.Core.TargetOperators
open Mono.Cecil
open Utility

Target.create "corebuild" (fun o ->
    dotnet "build" "miniblazor.sln %s"
        <| String.concat " " o.Context.Arguments
)

// Strip F# assemblies of their optdata / sigdata
Target.create "strip" (fun _ ->
    let stripFile f =
        let mutable anyChanged = false
        let bytes = File.ReadAllBytes(f)
        use mem = new MemoryStream(bytes)
        use asm = AssemblyDefinition.ReadAssembly(mem)
        let resources = asm.MainModule.Resources
        for i = resources.Count - 1 downto 0 do
            let name = resources.[i].Name
            if name = "FSharpOptimizationData." + asm.Name.Name
                || name = "FSharpSignatureData." + asm.Name.Name
                || name = "FSharpOptimizationInfo." + asm.Name.Name
                || name = "FSharpSignatureInfo." + asm.Name.Name
            then
                resources.RemoveAt(i)
                anyChanged <- true
        if anyChanged then
            printfn "Stripped F# data from %s" f
            asm.Write(f)
    for config in ["Debug"; "Release"] do
        let dir = Path.Combine("tests/client/bin", config, "netstandard2.0/dist/_framework/_bin")
        if Directory.Exists(dir) then
            for f in Directory.EnumerateFiles(dir, "*.dll") do
                stripFile f
)

Target.create "build" ignore

Target.create "runclient" (fun _ ->
    dotnet' "tests/client" "blazor" "serve"
)

Target.create "runserver" (fun _ ->
    dotnet' "tests/server" "run" ""
)

"corebuild"
    ==> "strip"
    ==> "build"

"build" ==> "runclient"
"build" ==> "runserver"

Target.runOrDefaultWithArguments "build"