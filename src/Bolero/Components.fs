namespace Bolero

open System
open System.Collections.Generic
open Microsoft.AspNetCore.Blazor
open Microsoft.AspNetCore.Blazor.Components
open Microsoft.AspNetCore.Blazor.Services
open Microsoft.Extensions.DependencyInjection
open Elmish
open Bolero.Render

/// A component built from `Html.Node`s.
[<AbstractClass>]
type Component() =
    inherit BlazorComponent()

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

type IElmishProgramComponent =
    abstract Services : System.IServiceProvider

/// A component that runs an Elmish program.
[<AbstractClass>]
type ElmishProgramComponent<'model, 'msg>() =
    inherit Component()

    let mutable oldModel = Unchecked.defaultof<'model>

    [<Inject>]
    member val UriHelper = Unchecked.defaultof<IUriHelper> with get, set
    [<Inject>]
    member val Services = Unchecked.defaultof<System.IServiceProvider> with get, set
    member val private View = Empty with get, set
    member val private Dispatch = ignore with get, set
    member val private BaseUri = "/" with get, set
    member val private Router = None : option<IRouter<'model, 'msg>> with get, set

    /// The Elmish program to run.
    abstract Program : Program<ElmishProgramComponent<'model, 'msg>, 'model, 'msg, Node>

    interface IElmishProgramComponent with
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
            this.View <- program.view model dispatch
            oldModel <- model
            this.StateHasChanged()
            this.Router |> Option.iter (fun router ->
                let newUri = router.GetRoute model
                let oldUri = this.GetCurrentUri()
                if newUri <> oldUri then
                    this.UriHelper.NavigateTo(newUri)
            )

    override this.OnInit() =
        base.OnInit()
        let program = this.Program
        { program with
            setState = fun model dispatch ->
                this.SetState(program, model, dispatch)
        }
        |> Program.runWith this

    member internal this.InitRouter
        (
            r: IRouter<'model, 'msg>,
            program: Program<ElmishProgramComponent<'model, 'msg>, 'model, 'msg, Node>,
            initModel: 'model
        ) =
        this.Router <- Some r
        this.BaseUri <- this.UriHelper.GetBaseUri()
        System.EventHandler<string> this.OnLocationChanged
        |> this.UriHelper.OnLocationChanged.AddHandler
        let setDispatch dispatch =
            this.Dispatch <- dispatch
        match r.SetRoute (this.GetCurrentUri()) with
        | Some msg ->
            let model, routeCmd = program.update msg initModel
            model, setDispatch :: routeCmd
        | None ->
            initModel, [setDispatch]

    override this.Render() =
        this.View

    interface System.IDisposable with
        member this.Dispose() =
            System.EventHandler<string> this.OnLocationChanged
            |> this.UriHelper.OnLocationChanged.RemoveHandler
