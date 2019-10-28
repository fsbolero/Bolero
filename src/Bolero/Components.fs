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
open Microsoft.JSInterop
open Elmish
open Bolero.Render

/// A component built from `Html.Node`s.
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

/// A component that is part of an Elmish view.
[<AbstractClass>]
type ElmishComponent<'model, 'msg>() =
    inherit Component()

    let mutable oldModel = Unchecked.defaultof<'model>

    /// The current value of the Elmish model.
    /// Can be just a part of the full program's model.
    [<Parameter>]
    member val Model = Unchecked.defaultof<'model> with get, set

    /// The Elmish dispatch function.
    [<Parameter>]
    member val Dispatch = Unchecked.defaultof<Dispatch<'msg>> with get, set

    /// The Elmish view function.
    abstract View : 'model -> Dispatch<'msg> -> Node

    /// Compare the old model with the new to decide whether this component
    /// needs to be re-rendered.
    abstract ShouldRender : oldModel: 'model * newModel: 'model -> bool
    default this.ShouldRender(oldModel, newModel) =
        not <| obj.ReferenceEquals(oldModel, newModel)

    override this.ShouldRender() =
       this.ShouldRender(oldModel, this.Model)

    override this.Render() =
        oldModel <- this.Model
        this.View this.Model this.Dispatch

type IProgramComponent =
    abstract Services : System.IServiceProvider

/// A component that runs an Elmish program.
[<AbstractClass>]
type ProgramComponent<'model, 'msg>() =
    inherit Component()

    let mutable oldModel = Unchecked.defaultof<'model>
    let mutable navigationInterceptionEnabled = false

    [<Inject>]
    member val NavigationManager = Unchecked.defaultof<NavigationManager> with get, set
    [<Inject>]
    member val Services = Unchecked.defaultof<System.IServiceProvider> with get, set
    [<Inject>]
    member val JSRuntime = Unchecked.defaultof<IJSRuntime> with get, set
    [<Inject>]
    member val NavigationInterception = Unchecked.defaultof<INavigationInterception> with get, set

    member val private View = Empty with get, set
    member val private Dispatch = ignore with get, set
    member val private Router = None : option<IRouter<'model, 'msg>> with get, set

    /// The Elmish program to run.
    abstract Program : Program<ProgramComponent<'model, 'msg>, 'model, 'msg, Node>

    interface IProgramComponent with
        member this.Services = this.Services

    member private this.OnLocationChanged (_: obj) (e: LocationChangedEventArgs) =
        this.Router |> Option.iter (fun router ->
            let uri = this.NavigationManager.ToBaseRelativePath(e.Location)
            let route = router.SetRoute uri
            Option.iter this.Dispatch route)

    member internal this.GetCurrentUri() =
        let uri = this.NavigationManager.Uri
        this.NavigationManager.ToBaseRelativePath(uri)

    member internal this.SetState(program, model, dispatch) =
        if this.ShouldRender() then
            this.ForceSetState(program, model, dispatch)

    member internal this.StateHasChanged() =
        base.StateHasChanged()

    member private this.ForceSetState(program, model, dispatch) =
        this.View <- program.view model dispatch
        oldModel <- model
        this.InvokeAsync(this.StateHasChanged) |> ignore
        this.Router |> Option.iter (fun router ->
            let newUri = router.GetRoute model
            let oldUri = this.GetCurrentUri()
            if newUri <> oldUri then
                this.NavigationManager.NavigateTo(newUri)
        )

    member this.Rerender() =
        this.ForceSetState(this.Program, oldModel, this.Dispatch)

    override this.OnInitialized() =
        base.OnInitialized()
        let program = this.Program
        let setDispatch dispatch =
            this.Dispatch <- dispatch
        { program with
            setState = fun model dispatch ->
                this.SetState(program, model, dispatch)
            init = fun arg ->
                let model, cmd = program.init arg
                model, setDispatch :: cmd
        }
        |> Program.runWith this

    member internal this.InitRouter
        (
            r: IRouter<'model, 'msg>,
            program: Program<ProgramComponent<'model, 'msg>, 'model, 'msg, Node>,
            initModel: 'model
        ) =
        this.Router <- Some r
        System.EventHandler<_> this.OnLocationChanged
        |> this.NavigationManager.LocationChanged.AddHandler
        match r.SetRoute (this.GetCurrentUri()) with
        | Some msg ->
            program.update msg initModel
        | None ->
            initModel, []

    override this.OnAfterRenderAsync(firstRender) =
        if this.Router.IsSome && not navigationInterceptionEnabled then
            navigationInterceptionEnabled <- true
            this.NavigationInterception.EnableNavigationInterceptionAsync()
        else
            Task.CompletedTask

    override this.Render() =
        this.View

    interface System.IDisposable with
        member this.Dispose() =
            System.EventHandler<_> this.OnLocationChanged
            |> this.NavigationManager.LocationChanged.RemoveHandler

type ElementReferenceBinder() =

    let mutable ref = Unchecked.defaultof<ElementReference>

    member this.Ref = ref

    member internal this.SetRef(r) = ref <- r
