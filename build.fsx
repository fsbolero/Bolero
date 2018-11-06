#r "paket: groupref fake //"
#load ".paket/fake/Utility.fsx"

open System.IO
open System.Text
open System.Text.RegularExpressions
open Fake.Core
open Fake.Core.TargetOperators
open Utility

Target.create "corebuild" (fun o ->
    let config = getArg o "-c" "Release"
    dotnet "build" "miniblazor.sln -c %s" config
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
            let ident = if tag.NeedsEscape then "``" + esc + "``" else esc
            let childrenArg = if tag.CanHaveChildren then " (children: list<Node>)" else ""
            let childrenVal = if tag.CanHaveChildren then "children" else "[]"
            s.AppendLine(sprintf """/// Create an HTML `<%s>` element.""" tag.Name)
             .AppendLine(sprintf """let %s (attrs: list<Attr>)%s : Node =""" ident childrenArg)
             .AppendLine(sprintf """    elt "%s" attrs %s""" tag.Name childrenVal)
             .AppendLine()
        )
        |> replace (Attrs.GetSample().Rows) "ATTRS" (fun s attr ->
            let esc = escapeDashes attr.Name
            let ident = if attr.NeedsEscape then "``" + esc + "``" else esc
            s.AppendLine(sprintf """    /// Create an HTML `%s` attribute.""" attr.Name)
             .AppendLine(sprintf """    let %s (v: obj) : Attr = "%s" => v""" ident attr.Name)
             .AppendLine()
        )
        |> replace (Events.GetSample().Rows) "EVENTS" (fun s event ->
            let esc = escapeDashes event.Name
            s.AppendLine(sprintf """    /// Create a handler for HTML event `%s`.""" event.Name)
             .AppendLine(sprintf """    let %s (callback: UI%sEventArgs -> unit) : Attr =""" esc event.Type)
             .AppendLine(sprintf """        "on%s" => BindMethods.GetEventHandlerValue callback""" esc)
             .AppendLine()
        )
    if input <> output then
        File.WriteAllText(file, output)
)

Target.create "build" ignore

Target.create "pack" (fun o ->
    let version = getArg o "-v" "0.1.0"
    Fake.DotNet.Paket.pack (fun p ->
        { p with
            OutputPath = "build"
            Version = version
        }
    )
)

Target.create "run-client" (fun _ ->
    dotnet' "tests/Client" [] "blazor" "serve"
)

Target.create "run-server" (fun _ ->
    dotnet' "tests/Server" [] "run" ""
)

Target.create "run-remoting" (fun _ ->
    dotnet' "tests/Remoting.Server" [] "run" ""
)

Target.create "test" (fun _ ->
    dotnet' "tests/Unit" [] "test" ""
)

Target.create "test-debug" (fun _ ->
    dotnet' "tests/Unit" ["VSTEST_HOST_DEBUG", "1"] "test" ""
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
