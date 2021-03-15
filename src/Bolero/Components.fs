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

namespace Bolero

open System
open System.Collections.Generic
open System.Threading.Tasks
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Routing
open Microsoft.Extensions.Logging
open Microsoft.JSInterop
open Elmish
open Bolero.Render

/// Base class for components built from `Bolero.Node`s.
/// [category: Components]
[<AbstractClass>]
type Component() =
    inherit ComponentBase()

    let matchCache = Dictionary()

    override this.BuildRenderTree(builder) =
        base.BuildRenderTree(builder)
        this.Render()
        |> RenderNode this builder matchCache

    /// The rendered contents of the component.
    abstract Render : unit -> Node

/// Base class for components with a typed model.
/// [category: Components]
[<AbstractClass>]
type Component<'model>() =
    inherit Component()

    /// The optional custom equality check
    // Uses default equality from base class
    [<Parameter>]
    member val Equal = (fun m1 m2 -> obj.ReferenceEquals(m1, m2)) with get, set

    /// Compare the old model with the new to decide whether this component
    /// needs to be re-rendered.
    abstract member ShouldRender : oldModel: 'model * newModel: 'model -> bool
    default this.ShouldRender(oldModel, newModel) =
        not <| this.Equal oldModel newModel

/// Base class for components that are part of an Elmish view.
/// [category: Components]
[<AbstractClass>]
type ElmishComponent<'model, 'msg>() =
    inherit Component<'model>()

    member val internal OldModel = Unchecked.defaultof<'model> with get, set

    /// The current value of the Elmish model.
    /// Can be just a part of the full program's model.
    [<Parameter>]
    member val Model = Unchecked.defaultof<'model> with get, set

    /// The Elmish dispatch function.
    [<Parameter>]
    member val Dispatch = Unchecked.defaultof<Dispatch<'msg>> with get, set

    /// The Elmish view function.
    abstract View : 'model -> Dispatch<'msg> -> Node

    override this.ShouldRender() =
        this.ShouldRender(this.OldModel, this.Model)

    override this.Render() =
        this.OldModel <- this.Model
        this.View this.Model this.Dispatch

/// [omit]
type LazyComponent<'model,'msg>() =
    inherit ElmishComponent<'model,'msg>()

    /// The view function
    [<Parameter>]
    member val ViewFunction = Unchecked.defaultof<'model -> Dispatch<'msg> -> Node> with get, set

    override this.View model dispatch = this.ViewFunction model dispatch

/// [omit]
type IProgramComponent =
    abstract Services : IServiceProvider

/// [omit]
type Program<'model, 'msg> = Program<ProgramComponent<'model, 'msg>, 'model, 'msg, Node>

/// Base class for components that run an Elmish program.
/// [category: Components]
and [<AbstractClass>]
    ProgramComponent<'model, 'msg>() =
    inherit Component<'model>()

    let mutable oldModel = None
    let mutable view = Node.Empty
    let mutable runProgramLoop = fun () -> ()
    let mutable dispatch = ignore<'msg>
    let mutable program = Unchecked.defaultof<Program<'model, 'msg>>
    let mutable router = None : option<IRouter<'model, 'msg>>
    let mutable setState = fun model dispatch ->
        view <- Program.view program model dispatch
        oldModel <- Some model

    /// [omit]
    [<Inject>]
    member val NavigationManager = Unchecked.defaultof<NavigationManager> with get, set
    /// [omit]
    [<Inject>]
    member val Services = Unchecked.defaultof<IServiceProvider> with get, set
    /// The JavaScript interoperation runtime.
    [<Inject>]
    member val JSRuntime = Unchecked.defaultof<IJSRuntime> with get, set
    /// [omit]
    [<Inject>]
    member val NavigationInterception = Unchecked.defaultof<INavigationInterception> with get, set
    [<Inject>]
    member val private Log = Unchecked.defaultof<ILogger<ProgramComponent<'model, 'msg>>> with get, set

    /// The component's dispatch method.
    /// This property is initialized during the component's OnInitialized phase.
    member _.Dispatch = dispatch

    /// The Elmish program to run.
    abstract Program : Program<'model, 'msg>

    interface IProgramComponent with
        member this.Services = this.Services

    member private this.OnLocationChanged (_: obj) (e: LocationChangedEventArgs) =
        router |> Option.iter (fun router ->
            let uri = this.NavigationManager.ToBaseRelativePath(e.Location)
            match router.SetRoute uri with
            | Some route -> dispatch route
            | None ->
                // Based on https://github.com/dotnet/aspnetcore/blob/a9026f1e0ff2000eaf918aa0ec07f8d701f80dd6/src/Components/Components/src/Routing/Router.cs#L192-L205
                if e.IsNavigationIntercepted then
                    this.Log.LogInformation("Navigating to external address: {0}", e.Location)
                    this.NavigationManager.NavigateTo(e.Location, forceLoad = true)
                else
                    this.Log.LogInformation("No route found for this path: {0}", uri))

    member internal this.GetCurrentUri() =
        let uri = this.NavigationManager.Uri
        this.NavigationManager.ToBaseRelativePath(uri)

    member internal _.StateHasChanged() =
        base.StateHasChanged()

    member private this.ForceSetState(model, dispatch) =
        view <- Program.view program model dispatch
        oldModel <- Some model
        this.InvokeAsync(this.StateHasChanged) |> ignore
        router |> Option.iter (fun router ->
            let newUri = router.GetRoute(model).TrimStart('/')
            let oldUri = this.GetCurrentUri()
            if newUri <> oldUri then
                try this.NavigationManager.NavigateTo(newUri)
                with _ -> () // fails if run in prerender
        )

    override this.OnInitialized() =
        base.OnInitialized()
        let setDispatch d =
            dispatch <- d
        program <-
            this.Program
            |> Program.map
                (fun init arg ->
                    let model, cmd = init arg
                    model, setDispatch :: cmd)
                id id
                (fun _ model dispatch -> setState model dispatch)
                id
        runProgramLoop <- Program'.runFirstRender this program
        setState <- fun model dispatch ->
            match oldModel with
            | Some oldModel when this.ShouldRender(oldModel, model) -> this.ForceSetState(model, dispatch)
            | _ -> ()

    member internal this.InitRouter
        (
            r: IRouter<'model, 'msg>,
            update: 'msg -> 'model -> 'model * Cmd<'msg>,
            initModel: 'model
        ) =
        router <- Some r
        EventHandler<_> this.OnLocationChanged
        |> this.NavigationManager.LocationChanged.AddHandler
        match r.SetRoute (this.GetCurrentUri()) with
        | Some msg ->
            update msg initModel
        | None ->
            initModel, []

    override this.OnAfterRenderAsync(firstRender) =
        if firstRender then
            runProgramLoop()
            if router.IsSome then
                this.NavigationInterception.EnableNavigationInterceptionAsync()
            else
                Task.CompletedTask
        else
            Task.CompletedTask

    override this.Render() =
        view

    member this.Rerender() =
        match oldModel with
        | None -> ()
        | Some model ->
            oldModel <- None
            this.ForceSetState(model, dispatch)

    interface IDisposable with
        member this.Dispose() =
            EventHandler<_> this.OnLocationChanged
            |> this.NavigationManager.LocationChanged.RemoveHandler
