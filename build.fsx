#r "paket: groupref fake //"
#load ".paket/fake/Utility.fsx"

open System.IO
open System.Text
open System.Text.RegularExpressions
open Fake.Core
open Fake.Core.TargetOperators
open Mono.Cecil
open Utility

Target.create "corebuild" (fun o ->
    dotnet "build" "miniblazor.sln %s"
        <| String.concat " " o.Context.Arguments
)

let [<Literal>] tagsFile = __SOURCE_DIRECTORY__ + "/src/MiniBlazor/tags.csv"
type Tags = FSharp.Data.CsvProvider<tagsFile>
let [<Literal>] attrsFile = __SOURCE_DIRECTORY__ + "/src/MiniBlazor/attrs.csv"
type Attrs = FSharp.Data.CsvProvider<attrsFile>
let [<Literal>] eventsFile = __SOURCE_DIRECTORY__ + "/src/MiniBlazor/events.csv"
type Events = FSharp.Data.CsvProvider<eventsFile>

// Generate HTML tags and attributes from CSV
Target.create "tags" (fun _ ->
    let file = "src/MiniBlazor/Html.fs"
    let input = File.ReadAllText(file)
    let escapeDashes s =
        Regex("-(.)").Replace(s, fun (m: Match) ->
            m.Groups.[1].Value.ToUpperInvariant())
    let replace rows marker writeItem input =
        Regex(sprintf """(?<=// BEGIN %s\r\n)(?:\w|\W)*(?=// END %s)""" marker marker,
            RegexOptions.Multiline)
            .Replace(input, fun _ ->
                let s = new StringBuilder()
                for tag in rows do
                    writeItem s tag |> ignore
                s.ToString()
            )
    let output =
        input
        |> replace (Tags.GetSample().Rows) "TAGS" (fun s tag ->
            let esc = escapeDashes tag.Name
            s.AppendLine(
                sprintf """let %s (attrs: list<Attr>)%s : Node =
    elt "%s" attrs %s"""
                    (if tag.NeedsEscape then "``" + esc + "``" else esc)
                    (if tag.CanHaveChildren then " (children: list<Node>)" else "")
                    tag.Name
                    (if tag.CanHaveChildren then "children" else "[]")
            )
        )
        |> replace (Attrs.GetSample().Rows) "ATTRS" (fun s tag ->
            let esc = escapeDashes tag.Name
            s.AppendLine(
                sprintf """    let %s (v: obj) : Attr = "%s" => v"""
                    (if tag.NeedsEscape then "``" + esc + "``" else esc)
                    tag.Name
            )
        )
        |> replace (Events.GetSample().Rows) "EVENTS" (fun s tag ->
            let esc = escapeDashes tag.Name
            s.AppendLine(
                sprintf """    let %s (callback: UI%sEventArgs -> unit) : Attr =
        "on%s" => BindMethods.GetEventHandlerValue callback"""
                    esc
                    tag.Type
                    esc
            )
        )
    if input <> output then
        File.WriteAllText(file, output)
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
    for app in ["tests/client"; "tests/server"] do
        for config in ["Debug"; "Release"] do
            let dir = Path.Combine(app, "bin", config, "netstandard2.0/dist/_framework/_bin")
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

Target.create "runiso" (fun _ ->
    dotnet' "tests/isomorphic" "run" ""
)

"corebuild"
    ==> "strip"
    ==> "build"

"build" ==> "runclient"
"build" ==> "runserver"
"build" ==> "runiso"

Target.runOrDefaultWithArguments "build"
