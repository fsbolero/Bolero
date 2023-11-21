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

module Build

open System.IO
open System.Net.Http
open System.Text
open System.Text.RegularExpressions
open System.Threading.Tasks
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO.FileSystemOperators
open Utility

let config = getArgWith "-c" <| fun o ->
    match o.Context.FinalTarget with
    | "test-debug" -> "Debug"
    | _ -> "Release"
let version = getArgOpt "-v" >> Option.defaultWith (fun () ->
    let v =
        let s = dotnetOutput "nbgv" ["get-version"; "-v"; "SemVer2"]
        s.Trim()
    if BuildServer.buildServer = BuildServer.LocalBuild then
        let v = v + if v.Contains("-") then ".local." else "-local."
        let p = "Bolero." + v
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
        v + string (currentVer + 1I)
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
        m.Groups[1].Value.ToUpperInvariant())

let replace rows marker writeItem input =
    Regex($"""(?<=// BEGIN %s{marker}\r?\n)(?:\w|\W)*(?=// END %s{marker})""",
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
            s.AppendLine($"""/// <summary>Computation expression to create an HTML <c>&lt;%s{tag.Name}&gt;</c> element.</summary>""")
             .AppendLine( """/// <category>HTML tag names</category>""")
             .AppendLine($"""let %s{ident} : ElementBuilder = elt "%s{tag.Name}" """)
             .AppendLine()
        )
        >> replace (Attrs.GetSample().Rows) "ATTRS" (fun s attr ->
            let esc = escapeDashes attr.Name
            let ident =
                if attr.NeedsRename then esc + "'"
                elif attr.NeedsEscape then "``" + esc + "``"
                else esc
            s.AppendLine($"""    /// <summary>Create an HTML <c>%s{attr.Name}</c> attribute.</summary>""")
             .AppendLine( """    /// <param name="v">The value of the attribute.</param>""")
             .AppendLine($"""    let inline %s{ident} (v: obj) : Attr = "%s{attr.Name}" => v""")
             .AppendLine()
        )
        >> replace (Events.GetSample().Rows) "EVENTS" (fun s event ->
            let esc = escapeDashes event.Name
            s.AppendLine($"""    /// <summary>Create a handler for HTML event <c>%s{event.Name}</c>.</summary>""")
             .AppendLine( """    /// <param name="callback">The event callback.</param>""")
             .AppendLine($"""    let inline %s{esc} (callback: %s{event.Type}EventArgs -> unit) : Attr =""")
             .AppendLine($"""        attr.callback<%s{event.Type}EventArgs> "on%s{esc}" callback""")
             .AppendLine()
        )
        >> replace (Events.GetSample().Rows) "ASYNCEVENTS" (fun s event ->
            let esc = escapeDashes event.Name
            s.AppendLine($"""        /// <summary>Create an asynchronous handler for HTML event <c>%s{event.Name}</c>.</summary>""")
             .AppendLine( """        /// <param name="callback">The event callback.</param>""")
             .AppendLine($"""        let inline %s{esc} (callback: %s{event.Type}EventArgs -> Async<unit>) : Attr =""")
             .AppendLine($"""            attr.async.callback<%s{event.Type}EventArgs> "on%s{esc}" callback""")
        )
        >> replace (Events.GetSample().Rows) "TASKEVENTS" (fun s event ->
            let esc = escapeDashes event.Name
            s.AppendLine($"""        /// <summary>Create an asynchronous handler for HTML event <c>%s{event.Name}</c>.</summary>""")
             .AppendLine( """        /// <param name="callback">The event callback.</param>""")
             .AppendLine($"""        let inline %s{esc} (callback: %s{event.Type}EventArgs -> Task) : Attr =""")
             .AppendLine($"""            attr.task.callback<%s{event.Type}EventArgs> "on%s{esc}" callback""")
        )
    )
    runTags "src/Bolero.Templating.Provider/Parsing.fs" (
        replace (Events.GetSample().Rows) "EVENTS" (fun s event ->
            if event.Type <> "" then
                s.AppendLine($"""        | "on%s{event.Name}" -> typeof<%s{event.Type}EventArgs>""")
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
    use c = new HttpClient()
    use f = File.OpenRead(results.FullName)
    use content = new StreamContent(f)
    c.PostAsync(url, content)
    :> Task
    |> Async.AwaitTask
    |> Async.RunSynchronously

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
            let v = v[v.LastIndexOf(' ') + 1 ..].Split('.')
            Some $"{v[0]}.{v[1]}"
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
        dotnet "paket" ["update"; "Selenium.WebDriver.ChromeDriver"; "-V"; $"~> {v}"; "-g"; "tests"])
)

Target.description "Build, test and pack"
Target.create "all" ignore

"corebuild"
    ==> "build"
    ==> "pack"
    |> ignore

"build" ==> "run-client" |> ignore
"build" ==> "run-server" |> ignore
"build" ==> "run-remoting" |> ignore

"build" ?=> "test" |> ignore
"build" ?=> "test-debug" |> ignore

"test" ==> "all" |> ignore
"pack" ==> "all" |> ignore

Target.runOrDefaultWithArguments "build"
