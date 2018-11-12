#r "paket: groupref fake //"
#load ".paket/fake/Utility.fsx"

open System.IO
open System.Net
open System.Text
open System.Text.RegularExpressions
open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO.FileSystemOperators
open Utility

type CommandLineOptions =
    {
        Config: string
        Version: string
        TestUploadUrl: option<string>
    }
let mutable options = None
type TargetParameter with
    member this.Options =
        match options with
        | Some o -> o
        | None ->
            let o = {
                Config = getArg this "-c" "Release"
                Version = getArg this "-v" "0.1.0"
                TestUploadUrl = getArgOpt this "--push-tests"
            }
            options <- Some o
            o

Target.create "corebuild" (fun o ->
    dotnet "build" "bolero.sln -c:%s" o.Options.Config
)

let [<Literal>] tagsFile = __SOURCE_DIRECTORY__ + "/src/Bolero/tags.csv"
type Tags = FSharp.Data.CsvProvider<tagsFile>
let [<Literal>] attrsFile = __SOURCE_DIRECTORY__ + "/src/Bolero/attrs.csv"
type Attrs = FSharp.Data.CsvProvider<attrsFile>
let [<Literal>] eventsFile = __SOURCE_DIRECTORY__ + "/src/Bolero/events.csv"
type Events = FSharp.Data.CsvProvider<eventsFile>

let escapeDashes s =
    Regex("-(.)").Replace(s, fun (m: Match) ->
        m.Groups.[1].Value.ToUpperInvariant())

let replace rows marker writeItem input =
    Regex(sprintf """(?<=// BEGIN %s\r?\n)(?:\w|\W)*(?=// END %s)""" marker marker,
        RegexOptions.Multiline)
        .Replace(input, fun _ ->
            let s = new StringBuilder()
            for tag in rows do
                writeItem s tag |> ignore
            s.ToString()
        )

let runTags filename apply =
    let input = File.ReadAllText(filename)
    let output = apply input
    if input <> output then
        File.WriteAllText(filename, output)

// Generate HTML tags and attributes from CSV
Target.create "tags" (fun _ ->
    runTags "src/Bolero/Html.fs" (
        replace (Tags.GetSample().Rows) "TAGS" (fun s tag ->
            let esc = escapeDashes tag.Name
            let ident = if tag.NeedsEscape then "``" + esc + "``" else esc
            let childrenArg = if tag.CanHaveChildren then " (children: list<Node>)" else ""
            let childrenVal = if tag.CanHaveChildren then "children" else "[]"
            s.AppendLine(sprintf """/// Create an HTML `<%s>` element.""" tag.Name)
             .AppendLine(sprintf """let %s (attrs: list<Attr>)%s : Node =""" ident childrenArg)
             .AppendLine(sprintf """    elt "%s" attrs %s""" tag.Name childrenVal)
             .AppendLine()
        )
        >> replace (Attrs.GetSample().Rows) "ATTRS" (fun s attr ->
            let esc = escapeDashes attr.Name
            let ident = if attr.NeedsEscape then "``" + esc + "``" else esc
            s.AppendLine(sprintf """    /// Create an HTML `%s` attribute.""" attr.Name)
             .AppendLine(sprintf """    let %s (v: obj) : Attr = "%s" => v""" ident attr.Name)
             .AppendLine()
        )
        >> replace (Events.GetSample().Rows) "EVENTS" (fun s event ->
            let esc = escapeDashes event.Name
            s.AppendLine(sprintf """    /// Create a handler for HTML event `%s`.""" event.Name)
             .AppendLine(sprintf """    let %s (callback: UI%sEventArgs -> unit) : Attr =""" esc event.Type)
             .AppendLine(sprintf """        "on%s" => BindMethods.GetEventHandlerValue callback""" esc)
             .AppendLine()
        )
    )
    runTags "src/Bolero.Templating/Parsing.fs" (
        replace (Events.GetSample().Rows) "EVENTS" (fun s event ->
            if event.Type <> "" then
                s.AppendLine(sprintf """        | "on%s" -> typeof<UI%sEventArgs>""" event.Name event.Type)
            else
                s
        )
    )
)

Target.create "build" ignore

Target.create "pack" (fun o ->

    Fake.DotNet.Paket.pack (fun p ->
        { p with
            OutputPath = "build"
            Version = o.Options.Version
        }
    )
)

Target.create "run-client" (fun o ->
    dotnet' "tests/Client" [] "run" "-c:%s" o.Options.Config
)

Target.create "run-server" (fun o ->
    dotnet' "tests/Server" [] "run" "-c:%s" o.Options.Config
)

Target.create "run-remoting" (fun o ->
    dotnet' "tests/Remoting.Server" [] "run" "-c:%s" o.Options.Config
)

let uploadTests (url: string) =
    let results =
        DirectoryInfo(__SOURCE_DIRECTORY__ </> "tests" </> "Unit" </> "TestResults")
            .EnumerateFiles("*.trx")
        |> Seq.maxBy (fun f -> f.CreationTime)
    use c = new WebClient()
    c.UploadFile(url, results.FullName) |> ignore

Target.create "test" (fun o ->
    dotnet' "tests/Unit" [] "test" "--logger:trx -c:%s" o.Options.Config
    Option.iter uploadTests o.Options.TestUploadUrl
)

Target.create "test-debug" (fun o ->
    dotnet' "tests/Unit" ["VSTEST_HOST_DEBUG", "1"] "test" "-c:%s" o.Options.Config
)

"corebuild"
    ==> "build"
    ==> "pack"

"build" ==> "run-client"
"build" ==> "run-server"
"build" ==> "run-remoting"
"build" ==> "test"
"build" ==> "test-debug"

Target.runOrDefaultWithArguments "build"
