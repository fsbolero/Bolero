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
    abstract Services : System.IServiceProvider

/// [omit]
type Program<'model, 'msg> = Program<ProgramComponent<'model, 'msg>, 'model, 'msg, Node>

/// Base class for components that run an Elmish program.
/// [category: Components]
and [<AbstractClass>]
    ProgramComponent<'model, 'msg>() =
    inherit Component<'model>()

    let mutable oldModel = Unchecked.defaultof<'model>
    let mutable navigationInterceptionEnabled = false
    let mutable view = Node.Empty
    let mutable dispatch = ignore<'msg>
    let mutable update = fun _ x -> x, Cmd.none

    /// [omit]
    [<Inject>]
    member val NavigationManager = Unchecked.defaultof<NavigationManager> with get, set
    /// [omit]
    [<Inject>]
    member val Services = Unchecked.defaultof<System.IServiceProvider> with get, set
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
    member val private Router = None : option<IRouter<'model, 'msg>> with get, set

    /// The Elmish program to run. Either this or AsyncProgram must be overridden.
    abstract Program : Program<'model, 'msg>
    default _.Program = Unchecked.defaultof<_>

    interface IProgramComponent with
        member this.Services = this.Services

    member private this.OnLocationChanged (_: obj) (e: LocationChangedEventArgs) =
        this.Router |> Option.iter (fun router ->
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

    member internal this.SetState(program, model, dispatch) =
        if this.ShouldRender(oldModel, model) then
            this.ForceSetState(program, model, dispatch)

    member internal _.StateHasChanged() =
        base.StateHasChanged()

    member private this.ForceSetState(program, model, dispatch) =
        view <- Program.view program model dispatch
        oldModel <- model
        this.InvokeAsync(this.StateHasChanged) |> ignore
        this.Router |> Option.iter (fun router ->
            let newUri = router.GetRoute(model).TrimStart('/')
            let oldUri = this.GetCurrentUri()
            if newUri <> oldUri then
                try this.NavigationManager.NavigateTo(newUri)
                with _ -> () // fails if run in prerender
        )

    member this.Rerender() =
        this.ForceSetState(this.Program, oldModel, dispatch)

    member internal _._OnInitialized() =
        base.OnInitialized()

    override this.OnInitialized() =
        this._OnInitialized()
        let program = this.Program
        let setDispatch d =
            dispatch <- d
        program
        |> Program.map
            (fun init arg ->
                let model, cmd = init arg
                model, setDispatch :: cmd)
            (fun u -> update <- u; u)
            id
            (fun _ model dispatch ->
                this.SetState(program, model, dispatch))
            id
        |> Program.runWith this

    member internal this.InitRouter
        (
            r: IRouter<'model, 'msg>,
            program: Program<'model, 'msg>,
            initModel: 'model
        ) =
        this.Router <- Some r
        EventHandler<_> this.OnLocationChanged
        |> this.NavigationManager.LocationChanged.AddHandler
        match r.SetRoute (this.GetCurrentUri()) with
        | Some msg ->
            update msg initModel
        | None ->
            initModel, []

    override this.OnAfterRenderAsync(_) =
        if this.Router.IsSome && not navigationInterceptionEnabled then
            navigationInterceptionEnabled <- true
            this.NavigationInterception.EnableNavigationInterceptionAsync()
        else
            Task.CompletedTask

    override this.Render() =
        view

    interface System.IDisposable with
        member this.Dispose() =
            EventHandler<_> this.OnLocationChanged
            |> this.NavigationManager.LocationChanged.RemoveHandler

/// A utility to bind a reference to a rendered component.
/// See https://fsbolero.io/docs/Blazor#html-element-references
/// [category: HTML]
type Ref<'T>() =
    inherit Ref()

    /// The element or component reference.
    member val Value = Unchecked.defaultof<'T> with get, set

    override this.Render(builder, sequence) =
        builder.AddComponentReferenceCapture(sequence, fun v -> this.Value <- unbox<'T> v)
        sequence + 1

/// A utility to bind a reference to a rendered HTML element.
/// See https://fsbolero.io/docs/Blazor#html-element-references
/// [category: HTML]
type HtmlRef() =
    inherit Ref<ElementReference>()

    override this.Render(builder, sequence) =
        builder.AddElementReferenceCapture(sequence, fun v -> this.Value <- v)
        sequence + 1

/// A utility to bind a reference to a rendered HTML element.
/// See https://fsbolero.io/docs/Blazor#html-element-references
/// [category: HTML]
[<Obsolete "Use HtmlRef.">]
type ElementReferenceBinder = HtmlRef
