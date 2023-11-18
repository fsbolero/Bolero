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
open System.Threading.Tasks
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Routing
open Microsoft.Extensions.Logging
open Microsoft.JSInterop
open Elmish

/// <summary>Base class for components built from <see cref="T:Bolero.Node" />s.</summary>
/// <category>Components</category>
[<AbstractClass>]
type Component() =
    inherit ComponentBase()

    abstract CssScope : string
    default _.CssScope = null

    override this.BuildRenderTree(builder) =
        base.BuildRenderTree(builder)
        this.Render().Invoke(this, builder, 0) |> ignore

    /// <summary>The rendered contents of the component.</summary>
    abstract Render : unit -> Node

/// <summary>Base class for components with a typed model.</summary>
/// <category>Components</category>
[<AbstractClass>]
type Component<'model>() =
    inherit Component()

    /// <summary>
    /// The custom equality check. By default, uses reference equality.
    /// </summary>
    [<Parameter>]
    member val Equal = (fun m1 m2 -> obj.ReferenceEquals(m1, m2)) with get, set

    /// <summary>
    /// Compare the old model with the new to decide whether this component needs to be re-rendered.
    /// By default, uses <see cref="M:Equal" />.
    /// </summary>
    abstract member ShouldRender : oldModel: 'model * newModel: 'model -> bool
    default this.ShouldRender(oldModel, newModel) =
        not <| this.Equal oldModel newModel

/// <summary>Base class for components that are part of an Elmish view.</summary>
/// <category>Components</category>
[<AbstractClass>]
type ElmishComponent<'model, 'msg>() =
    inherit Component<'model>()

    member val internal OldModel = Unchecked.defaultof<'model> with get, set

    /// <summary>The current value of the Elmish model. Can be just a part of the full program's model.</summary>
    [<Parameter>]
    member val Model = Unchecked.defaultof<'model> with get, set

    /// <summary>The Elmish dispatch function.</summary>
    [<Parameter>]
    member val Dispatch = Unchecked.defaultof<Dispatch<'msg>> with get, set

    /// <summary>The Elmish view function.</summary>
    /// <param name="model">The Elmish model.</param>
    /// <param name="dispatch">The Elmish dispatch function.</param>
    /// <returns>The rendered view.</returns>
    abstract View : model: 'model -> dispatch: Dispatch<'msg> -> Node

    override this.ShouldRender() =
        this.ShouldRender(this.OldModel, this.Model)

    override this.Render() =
        this.OldModel <- this.Model
        this.View this.Model this.Dispatch

/// <exclude />
type LazyComponent<'model,'msg>() =
    inherit ElmishComponent<'model,'msg>()

    /// The view function
    [<Parameter>]
    member val ViewFunction = Unchecked.defaultof<'model -> Dispatch<'msg> -> Node> with get, set

    override this.View model dispatch = this.ViewFunction model dispatch

/// <exclude />
type IProgramComponent =
    abstract Services : IServiceProvider

/// <exclude />
type Program<'model, 'msg> = Program<ProgramComponent<'model, 'msg>, 'model, 'msg, Node>

/// <summary>Base class for components that run an Elmish program.</summary>
/// <category>Components</category>
and [<AbstractClass>]
    ProgramComponent<'model, 'msg>() =
    inherit Component<'model>()

    let mutable oldModel = None
    let mutable view = Node.Empty()
    let mutable runProgramLoop = fun () -> ()
    let mutable dispatch = ignore<'msg>
    let mutable program = Unchecked.defaultof<Program<'model, 'msg>>
    let mutable router = None : option<IRouter<'model, 'msg>>
    let mutable routeHash = None : option<string>
    let mutable setState = fun model dispatch ->
        view <- Program.view program model dispatch
        oldModel <- Some model

    /// <exclude />
    [<Inject>]
    member val NavigationManager = Unchecked.defaultof<NavigationManager> with get, set
    /// <exclude />
    [<Inject>]
    member val Services = Unchecked.defaultof<IServiceProvider> with get, set
    /// <summary>The JavaScript interoperation runtime. Provided by dependency injection.</summary>
    [<Inject>]
    member val JSRuntime = Unchecked.defaultof<IJSRuntime> with get, set
    /// <exclude />
    [<Inject>]
    member val NavigationInterception = Unchecked.defaultof<INavigationInterception> with get, set
    [<Inject>]
    member val private Log = Unchecked.defaultof<ILogger<ProgramComponent<'model, 'msg>>> with get, set

    /// <summary>
    /// The component's dispatch method.
    /// This property is initialized during the component's OnInitialized phase.
    /// </summary>
    member _.Dispatch = dispatch

    /// <summary>The Elmish program to run.</summary>
    abstract Program : Program<'model, 'msg>

    interface IProgramComponent with
        member this.Services = this.Services

    member private this.ParseUri (uri: string) =
        let uri, hash =
            match uri.IndexOf('#') with
            | -1 -> uri, None
            | n -> uri[..n-1], Some uri[n+1..]
        routeHash <- hash
        this.NavigationManager.ToBaseRelativePath(uri)

    member private this.OnLocationChanged (_: obj) (e: LocationChangedEventArgs) =
        router |> Option.iter (fun router ->
            let uri = this.ParseUri e.Location
            match router.SetRoute uri with
            | Some route -> dispatch route
            | None ->
                // Based on https://github.com/dotnet/aspnetcore/blob/a9026f1e0ff2000eaf918aa0ec07f8d701f80dd6/src/Components/Components/src/Routing/Router.cs#L192-L205
                if e.IsNavigationIntercepted then
                    this.Log.LogInformation("Navigating to external address: {0}", e.Location)
                    this.NavigationManager.NavigateTo(e.Location, forceLoad = true)
                else
                    this.Log.LogInformation("No route found for this path: {0}", uri)
                    Option.iter dispatch router.NotFound)

    member internal this.GetCurrentUri() =
        this.ParseUri this.NavigationManager.Uri

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
                id id
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
            match r.NotFound with
            | Some msg -> update msg initModel
            | None -> initModel, []

    override this.OnAfterRenderAsync(firstRender) =
        task {
            if firstRender then
                runProgramLoop()
                if router.IsSome then
                    do! this.NavigationInterception.EnableNavigationInterceptionAsync()

            match routeHash with
            | None -> ()
            | Some h ->
                routeHash <- None
                let! elt = this.JSRuntime.InvokeAsync<IJSObjectReference>("document.getElementById", h)
                return! elt.InvokeVoidAsync("scrollIntoView")
        }

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
