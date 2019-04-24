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
open Microsoft.AspNetCore.Components
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
        |> RenderNode builder matchCache

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

    [<Inject>]
    member val UriHelper = Unchecked.defaultof<IUriHelper> with get, set
    [<Inject>]
    member val Services = Unchecked.defaultof<System.IServiceProvider> with get, set
    [<Inject>]
    member val JSRuntime = Unchecked.defaultof<IJSRuntime> with get, set
    member val private View = Empty with get, set
    member val private Dispatch = ignore with get, set
    member val private BaseUri = "/" with get, set
    member val private Router = None : option<IRouter<'model, 'msg>> with get, set

    /// The Elmish program to run.
    abstract Program : Program<ProgramComponent<'model, 'msg>, 'model, 'msg, Node>

    interface IProgramComponent with
        member this.Services = this.Services

    member private this.OnLocationChanged (_: obj) (uri: string) =
        this.Router |> Option.iter (fun router ->
            let uri = this.UriHelper.ToBaseRelativePath(this.BaseUri, uri)
            let route = router.SetRoute uri
            Option.iter this.Dispatch route)

    member internal this.GetCurrentUri() =
        let uri = this.UriHelper.GetAbsoluteUri()
        this.UriHelper.ToBaseRelativePath(this.BaseUri, uri)

    member internal this.SetState(program, model, dispatch) =
        if not <| obj.ReferenceEquals(model, oldModel) then
            this.ForceSetState(program, model, dispatch)

    member internal this.StateHasChanged() =
        base.StateHasChanged()

    member private this.ForceSetState(program, model, dispatch) =
        this.View <- program.view model dispatch
        oldModel <- model
        this.Invoke(fun () -> this.StateHasChanged()) |> ignore
        this.Router |> Option.iter (fun router ->
            let newUri = router.GetRoute model
            let oldUri = this.GetCurrentUri()
            if newUri <> oldUri then
                this.UriHelper.NavigateTo(newUri)
        )

    member this.Rerender() =
        this.ForceSetState(this.Program, oldModel, this.Dispatch)

    override this.OnInit() =
        base.OnInit()
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
        this.BaseUri <- this.UriHelper.GetBaseUri()
        System.EventHandler<string> this.OnLocationChanged
        |> this.UriHelper.OnLocationChanged.AddHandler
        match r.SetRoute (this.GetCurrentUri()) with
        | Some msg ->
            program.update msg initModel
        | None ->
            initModel, []

    override this.Render() =
        this.View

    interface System.IDisposable with
        member this.Dispose() =
            System.EventHandler<string> this.OnLocationChanged
            |> this.UriHelper.OnLocationChanged.RemoveHandler

type ElementRefBinder() =

    let mutable ref = Unchecked.defaultof<ElementRef>

    member this.Ref = ref

    member internal this.SetRef(r) = ref <- r
