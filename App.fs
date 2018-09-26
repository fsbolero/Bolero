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
      mutable Tree : Render.RenderedNode
      Update : 'Message -> 'Model -> 'Model
      Render : 'Model -> Html.Node<'Message> }

let rec private applyUpdate (state: RunState<'Message, 'Model>) (msg: 'Message) =
    let newState = state.Update msg state.State
    let diff, newTree =
        state.Render newState
        |> Render.diff (fun f args -> applyUpdate state (f args)) state.Tree
    state.Tree <- newTree
    state.State <- newState
    diff

let Run (selector: string) (app: App<'Message, 'Model>) =
    if isNull JSRuntime.Current then
        Mono.WebAssembly.Interop.MonoWebAssemblyJSRuntime()
        |> JSRuntime.SetCurrentJSRuntime
    let initTree = app.Render app.Init
    let state =
        { State = app.Init
          Tree = Unchecked.defaultof<_>
          Update = fun msg model -> app.Update msg model
          Render = fun model -> app.Render model }
    let rInitTree =
        Render.toRenderedNode (fun f args -> applyUpdate state (f args)) initTree
    state.Tree <- rInitTree
    JSRuntime.Current.InvokeAsync<unit>("MiniBlazor.mount", selector, rInitTree)
    |> ignore
