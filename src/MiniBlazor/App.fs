module MiniBlazor.App

#nowart "40" // Run: recursive dispatch and tree

open Microsoft.JSInterop

type Dispatch<'Message> = 'Message -> unit

type App<'Message, 'Model> =
    { Init: 'Model
      Render: 'Model -> Dispatch<'Message> -> Html.Node
      Update: 'Message -> 'Model -> 'Model }

let Create<'Message, 'Model> init update render : App<'Message, 'Model> =
    { Init = init; Render = render; Update = update }

let Run (selector: string) (app: App<'Message, 'Model>) =
    if isNull JSRuntime.Current then
        Mono.WebAssembly.Interop.MonoWebAssemblyJSRuntime()
        |> JSRuntime.SetCurrentJSRuntime
    let mutable state = app.Init
    let mutable tree = [||]
    let rec dispatch msg =
        let newState = app.Update msg state
        let newView = app.Render newState dispatch
        let diff, newTree = Render.diffSiblings tree [newView]
        tree <- newTree
        state <- newState
        JSRuntime.Current.InvokeAsync<unit>("MiniBlazor.applyDiff", selector, diff)
        |> ignore
    tree <- Render.toRenderedNodes [app.Render app.Init dispatch]
    JSRuntime.Current.InvokeAsync<unit>("MiniBlazor.mount", selector, tree)
    |> ignore
