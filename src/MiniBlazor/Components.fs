namespace MiniBlazor

open System
open Microsoft.AspNetCore.Blazor.Components
open Microsoft.AspNetCore.Blazor.Services
open Elmish
open MiniBlazor.Render

/// A component built from `Html.Node`s.
[<AbstractClass>]
type Component() =
    inherit BlazorComponent()

    override this.BuildRenderTree(builder) =
        base.BuildRenderTree(builder)
        this.Render()
        |> renderNode builder 0
        |> ignore

    abstract Render : unit -> Node

/// A component that is part of an Elmish view.
[<AbstractClass>]
type ElmishComponent<'model, 'msg>() =
    inherit Component()

    let mutable oldModel = Unchecked.defaultof<'model>

    [<Parameter>]
    member val Model = Unchecked.defaultof<'model> with get, set

    [<Parameter>]
    member val Dispatch = Unchecked.defaultof<Dispatch<'msg>> with get, set

    abstract View : 'model -> Dispatch<'msg> -> Node

    override this.ShouldRender() =
       not <| obj.ReferenceEquals(oldModel, this.Model)

    override this.Render() =
        oldModel <- this.Model
        this.View this.Model this.Dispatch

type IRouter<'model, 'msg> =
    abstract GetRoute : 'model -> string
    abstract SetRoute : string -> option<'msg>

type Router<'model, 'msg> =
    {
        getRoute: 'model -> string
        setRoute: string -> option<'msg>
    }

    interface IRouter<'model, 'msg> with
        member this.GetRoute(model) = this.getRoute model
        member this.SetRoute(uri) = this.setRoute uri

type Router<'ep, 'model, 'msg> =
    {
        getEndPoint: 'model -> 'ep
        getRoute: 'ep -> string
        setRoute: string -> option<'msg>
    }

    member this.Link(ep) = this.getRoute ep

    interface IRouter<'model, 'msg> with
        member this.GetRoute(model) = this.getRoute (this.getEndPoint model)
        member this.SetRoute(uri) = this.setRoute uri

/// A component that runs an Elmish program.
[<AbstractClass>]
type ElmishProgramComponent<'model, 'msg>() =
    inherit Component()

    [<Inject>]
    member val UriHelper = Unchecked.defaultof<IUriHelper> with get, set
    member val private View = Empty with get, set
    member val private Dispatch = ignore with get, set
    member val private BaseUri = "/" with get, set
    member val private Router = None : option<IRouter<'model, 'msg>> with get, set

    abstract Program : Program<ElmishProgramComponent<'model, 'msg>, 'model, 'msg, Node>

    member private this.OnLocationChanged (_: obj) (uri: string) =
        this.Router |> Option.iter (fun router ->
            let uri = this.UriHelper.ToBaseRelativePath(this.BaseUri, uri)
            let route = router.SetRoute uri
            Option.iter this.Dispatch route)

    member internal this.GetCurrentUri() =
        let uri = this.UriHelper.GetAbsoluteUri()
        this.UriHelper.ToBaseRelativePath(this.BaseUri, uri)

    member internal this.SetState(program, model, dispatch) =
        this.View <- program.view model dispatch
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
