#r "paket: groupref fake //"
#load "src/fake/Utility.fsx"

open Fake.Core
open Fake.Core.TargetOperators
open Utility

Target.create "build" (fun o ->
    dotnet "build" "miniblazor.sln %s"
        <| String.concat " " o.Context.Arguments
)

Target.create "run" (fun _ ->
    dotnet' "tests/client" "blazor" "serve"
)

Target.create "watch" (fun _ ->
    dotnet "watch" "-p miniblazor.sln build"
)

"build" ==> "run"

Target.runOrDefaultWithArguments "build"