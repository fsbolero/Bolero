module Elmish.MiniBlazor.Program

open Microsoft.JSInterop
open Elmish
open MiniBlazor.Html
open MiniBlazor.Render

let withMiniBlazor selector (program: Program<unit, 'Model, 'Message, Node>) =
    if isNull JSRuntime.Current then
        Mono.WebAssembly.Interop.MonoWebAssemblyJSRuntime()
        |> JSRuntime.SetCurrentJSRuntime
    let mutable tree = [||]
    let mutable isInitial = true
    let mutable dispatch = ignore
    let setState model d =
        let res = program.view model d
        if isInitial then
            dispatch <- d
            isInitial <- false
            tree <- toRenderedNodes [res]
            JSRuntime.Current.InvokeAsync<unit>("MiniBlazor.mount", selector, tree)
            |> ignore
        else
            let diff, newTree = diffSiblings tree [res]
            tree <- newTree
            JSRuntime.Current.InvokeAsync<unit>("MiniBlazor.applyDiff", selector, diff)
            |> ignore
    { program with setState = setState }
