namespace MiniBlazor

open System
open Microsoft.JSInterop
open FSharp.Control.Tasks.V2

module Say =

    let hello() = task {
        let! s = JSRuntime.Current.InvokeAsync("populate", "<b>Hi there</b>")
        printfn "Hi %s!" s
    }

    [<EntryPoint>]
    let Main args =
        Mono.WebAssembly.Interop.MonoWebAssemblyJSRuntime()
        |> JSRuntime.SetCurrentJSRuntime
        hello() |> ignore
        0
