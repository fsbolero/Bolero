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

#r "paket:
nuget Fake.Core.Target
nuget Fake.IO.FileSystem
nuget Fake.DotNet.AssemblyInfoFile
nuget Fake.DotNet.Cli
nuget Fake.DotNet.Paket
nuget FSharp.Data ~> 3.0-beta
//"
#load "tools/Utility.fsx"

open System.IO
open System.Net
open System.Text
open System.Text.RegularExpressions
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO.FileSystemOperators
open Utility

let config = getArg "-c" "Debug"
let version = getArgOpt "-v" >> Option.defaultWith (fun () ->
    let v =
        let s = dotnetOutput "nbgv" ["get-version"; "-v"; "SemVer2"]
        s.Trim()
    if BuildServer.buildServer = BuildServer.LocalBuild then
        let p = "Bolero." + v + if v.Contains("-") then ".local." else "-local."
        let currentVer =
            if Directory.Exists "build" then
                Directory.EnumerateFiles ("build", p + "*")
            else
                Seq.empty
            |> Seq.choose (fun dir ->
                let n = Path.GetFileName dir
                let v = n.Substring(p.Length, n.Length - p.Length - ".nupkg".Length)
                match System.Numerics.BigInteger.TryParse(v) with
                | true, v -> Some v
                | _ ->
                    eprintfn "Could not parse '%s' to a bigint to retrieve the latest version (from '%s')" v dir
                    None)
            |> Seq.append [ 0I ]
            |> Seq.max
        v + ".local." + string (currentVer + 1I)
    else v
)
let testUploadUrl = getArgOpt "--push-tests"
let verbosity = getFlag "--verbose" >> function
    | true -> "n"
    | false -> "m"
let sourceLink = getFlag "--sourceLink"
let buildArgs o =
    [$"-c:{config o}"; $"-v:{verbosity o}"; $"/p:SourceLinkCreate={sourceLink o}"]

Target.description "Run the compilation phase proper"
Target.create "corebuild" (fun o ->
    dotnet "paket" ["restore"]
    dotnet "build" ("Bolero.sln" :: buildArgs o)
)

let [<Literal>] tagsFile = slnDir + "/src/Bolero/tags.csv"
type Tags = FSharp.Data.CsvProvider<tagsFile>
let [<Literal>] attrsFile = slnDir + "/src/Bolero/attrs.csv"
type Attrs = FSharp.Data.CsvProvider<attrsFile>
let [<Literal>] eventsFile = slnDir + "/src/Bolero/events.csv"
type Events = FSharp.Data.CsvProvider<eventsFile>

let escapeDashes s =
    Regex("-(.)").Replace(s, fun (m: Match) ->
        m.Groups.[1].Value.ToUpperInvariant())

let replace rows marker writeItem input =
    Regex(sprintf """(?<=// BEGIN %s\r?\n)(?:\w|\W)*(?=// END %s)""" marker marker,
        RegexOptions.Multiline)
        .Replace(input, fun _ ->
            let s = StringBuilder()
            for tag in rows do
                writeItem s tag |> ignore
            s.ToString()
        )

let runTags filename apply =
    let input = File.ReadAllText(filename)
    let output = apply input
    if input <> output then
        File.WriteAllText(filename, output)

Target.description "Generate HTML tags and attributes from CSV"
Target.create "tags" (fun _ ->
    runTags "src/Bolero.Html/Html.fs" (
        replace (Tags.GetSample().Rows) "TAGS" (fun s tag ->
            let esc = escapeDashes tag.Name
            let ident = if tag.NeedsEscape then "``" + esc + "``" else esc
            let childrenArg = if tag.CanHaveChildren then " (children: list<Node>)" else ""
            let childrenVal = if tag.CanHaveChildren then "children" else "[]"
            s.AppendLine(sprintf """/// Create an HTML `<%s>` element.""" tag.Name)
             .AppendLine(        """/// [category: HTML tag names]""")
             .AppendLine(sprintf """let inline %s (attrs: list<Attr>)%s : Node =""" ident childrenArg)
             .AppendLine(sprintf """    elt "%s" attrs %s""" tag.Name childrenVal)
             .AppendLine()
        )
        >> replace (Attrs.GetSample().Rows) "ATTRS" (fun s attr ->
            let esc = escapeDashes attr.Name
            let ident =
                if attr.NeedsRename then esc + "'"
                elif attr.NeedsEscape then "``" + esc + "``"
                else esc
            s.AppendLine(sprintf """    /// Create an HTML `%s` attribute.""" attr.Name)
             .AppendLine(sprintf """    let inline %s (v: obj) : Attr = "%s" => v""" ident attr.Name)
             .AppendLine()
        )
        >> replace (Events.GetSample().Rows) "EVENTS" (fun s event ->
            let esc = escapeDashes event.Name
            s.AppendLine(sprintf """    /// Create a handler for HTML event `%s`.""" event.Name)
             .AppendLine(sprintf """    let inline %s (callback: %sEventArgs -> unit) : Attr =""" esc event.Type)
             .AppendLine(sprintf """        attr.callback<%sEventArgs> ("on%s") callback""" event.Type esc)
             .AppendLine()
        )
        >> replace (Events.GetSample().Rows) "ASYNCEVENTS" (fun s event ->
            let esc = escapeDashes event.Name
            s.AppendLine(sprintf """        /// Create an asynchronous handler for HTML event `%s`.""" event.Name)
             .AppendLine(sprintf """        let inline %s (callback: %sEventArgs -> Async<unit>) : Attr =""" esc event.Type)
             .AppendLine(sprintf """            attr.async.callback<%sEventArgs> "on%s" callback""" event.Type esc)
        )
        >> replace (Events.GetSample().Rows) "TASKEVENTS" (fun s event ->
            let esc = escapeDashes event.Name
            s.AppendLine(sprintf """        /// Create an asynchronous handler for HTML event `%s`.""" event.Name)
             .AppendLine(sprintf """        let inline %s (callback: %sEventArgs -> Task) : Attr =""" esc event.Type)
             .AppendLine(sprintf """            attr.task.callback<%sEventArgs> "on%s" callback""" event.Type esc)
        )
    )
    runTags "src/Bolero.Templating.Provider/Parsing.fs" (
        replace (Events.GetSample().Rows) "EVENTS" (fun s event ->
            if event.Type <> "" then
                s.AppendLine(sprintf """        | "on%s" -> typeof<%sEventArgs>""" event.Name event.Type)
            else
                s
        )
    )
)

Target.description "Run a full compilation"
Target.create "build" (fun _ ->
    dotnet "build-server" ["shutdown"] // Using this to avoid locking of the output dlls
)

Target.description "Create the NuGet packages"
Target.create "pack" (fun o ->
    Paket.pack <| fun p ->
        { p with
            OutputPath = "build"
            Version = version o
            ToolType = ToolType.CreateLocalTool()
        }
)

Target.description "Run the Client test project"
Target.create "run-client" (fun o ->
    dotnet' "tests/Client" [] "run" (buildArgs o)
)

Target.description "Run the Server test project"
Target.create "run-server" (fun o ->
    dotnet' "tests/Server" [] "run" (buildArgs o)
)

Target.description "Run the Remoting test project"
Target.create "run-remoting" (fun o ->
    dotnet' "tests/Remoting.Server" [] "run" (buildArgs o)
)

let uploadTests (url: string) =
    let results =
        DirectoryInfo(slnDir </> "tests" </> "Unit" </> "TestResults")
            .EnumerateFiles("*.trx")
        |> Seq.maxBy (fun f -> f.CreationTime)
    use c = new WebClient()
    c.UploadFile(url, results.FullName) |> ignore

let unitTests o =
    try dotnet' "tests/Unit" [] "test" ("--logger:trx" :: buildArgs o)
    finally Option.iter uploadTests (testUploadUrl o)

let publishTests o =
    dotnet' "tests/Server" [] "publish" (buildArgs o)

Target.description "Run the unit tests"
Target.create "test" (fun o ->
    unitTests o
    publishTests o
)

Target.description "Run the unit tests waiting for a debugger to connect"
Target.create "test-debug" (fun o ->
    dotnet' "tests/Unit" ["VSTEST_HOST_DEBUG", "1"] "test" (buildArgs o)
)

Target.description "Update the Selenium driver to match the installed Chrome version"
Target.create "update-chromedriver" (fun _ ->
    if Environment.isUnix then
        try
            let v = (shellOutput "google-chrome" ["--version"]).Trim()
            let v = v.[v.LastIndexOf(' ') + 1 ..].Split('.')
            Some $"{v.[0]}.{v.[1]}"
        with _ ->
            Trace.traceImportant "Cannot find installed google-chrome version."
            None
    else
        match
            [
                Environment.environVarOrNone "ProgramFiles"
                Environment.environVarOrNone "ProgramFiles(x86)"
            ]
            |> Seq.choose (Option.map (fun e -> $@"{e}\Google\Chrome\Application\chrome.exe"))
            |> Seq.tryFind File.Exists
            with
        | None ->
            Trace.traceImportant "Cannot find installed chrome version."
            None
        | Some f ->
            let ver = System.Diagnostics.FileVersionInfo.GetVersionInfo(f)
            Some $"{ver.FileMajorPart}.{ver.FileMinorPart}"
    |> Option.iter (fun v ->
        dotnet "paket" ["update"; "Selenium.WebDriver.ChromeDriver"; "-V"; $"~> {v}"])
)

Target.description "Build, test and pack"
Target.create "all" ignore

"corebuild"
    ==> "build"
    ==> "pack"

"build" ==> "run-client"
"build" ==> "run-server"
"build" ==> "run-remoting"

"build" ?=> "test"
"build" ?=> "test-debug"

"test" ==> "all"
"pack" ==> "all"

Target.runOrDefaultWithArguments "build"
