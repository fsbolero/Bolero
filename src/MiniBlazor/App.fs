module MiniBlazor.App

open Microsoft.JSInterop

type App<'Message, 'Model> =
    { Init: 'Model
      Render: 'Model -> Html.Node<'Message>
      Update: 'Message -> 'Model -> 'Model }

let Create<'Message, 'Model> init update render : App<'Message, 'Model> =
    { Init = init; Render = render; Update = update }

type RunState<'Message, 'Model> =
    { mutable State : 'Model
      mutable Tree : Render.RenderedNode[]
      Update : 'Message -> 'Model -> 'Model
      Render : 'Model -> Html.Node<'Message> }

type RunState'<'Message, 'Model>
    (
        init: 'Model,
        update: 'Message -> 'Model -> 'Model,
        render: 'Model -> Html.Node<'Message>
    ) as this =
    let mutable state = init
    let renderer = Render.Renderer<'Message>(fun f args -> this.ApplyUpdate (f args))
    let mutable tree = renderer.ToRenderedNodes([render init])

    member this.ApplyUpdate(msg: 'Message) =
        let newState = update msg state
        let diff, newTree =
            renderer.DiffSiblings(tree, [render newState])
        tree <- newTree
        state <- newState
        diff

    member this.Tree = tree

let Run (selector: string) (app: App<'Message, 'Model>) =
    if isNull JSRuntime.Current then
        Mono.WebAssembly.Interop.MonoWebAssemblyJSRuntime()
        |> JSRuntime.SetCurrentJSRuntime
    let state = RunState'(app.Init, app.Update, app.Render)
    JSRuntime.Current.InvokeAsync<unit>("MiniBlazor.mount", selector, state.Tree)
    |> ignore
