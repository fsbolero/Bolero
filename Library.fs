namespace MiniBlazor

open System
open Microsoft.JSInterop
open FSharp.Control.Tasks.V2
open MiniBlazor.Html

module Say =

    let hello() = task {
        let el = i [style "color: blue"] [text "Hello Blazor!"]
        let! s = JSRuntime.Current.InvokeAsync("populate", el)
        printfn "Hi %s!" s
    }

    [<EntryPoint>]
    let Main args =
        Mono.WebAssembly.Interop.MonoWebAssemblyJSRuntime()
        |> JSRuntime.SetCurrentJSRuntime
        hello() |> ignore
        0
