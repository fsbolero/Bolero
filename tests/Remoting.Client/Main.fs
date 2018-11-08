module MiniBlazor.Tests.Remoting.Client

open System.Collections.Generic
open Microsoft.AspNetCore.Blazor.Components
open MiniBlazor
open MiniBlazor.Html
open MiniBlazor.Remoting
open Elmish

type MyApi =
    {
        getItems : unit -> Async<Map<int, string>>
        setItem : (int * string) -> Async<unit>
        removeItem : int -> Async<unit>
    }

    interface IRemoteService with
        member this.BasePath = "/myapi"

type Model =
    {
        currentKey: int
        currentValue: string
        items : Map<int, string>
        lastError : option<exn>
    }

let InitModel =
    {
        currentKey = 0
        currentValue = ""
        items = Map.empty
        lastError = None
    }

type Message =
    | SetCurrentKey of int
    | SetCurrentValue of string
    | RefreshItems
    | AddItem
    | RemoveItem of int
    | ItemsRefreshed of Map<int, string>
    | Error of exn

let Update (myApi: MyApi) msg model =
    match msg with
    | SetCurrentKey i ->
        { model with currentKey = i }, []
    | SetCurrentValue v ->
        { model with currentValue = v }, []
    | RefreshItems ->
        model,
        Cmd.ofAsync myApi.getItems () ItemsRefreshed Error
    | AddItem ->
        model,
        Cmd.ofAsync myApi.setItem (model.currentKey, model.currentValue)
            (fun () -> RefreshItems) Error
    | RemoveItem k ->
        model,
        Cmd.ofAsync myApi.removeItem k
            (fun () -> RefreshItems) Error
    | ItemsRefreshed items ->
        { model with items = items; lastError = None }, []
    | Error exn ->
        { model with lastError = Some exn }, []

type Item() =
    inherit ElmishComponent<KeyValuePair<int, string>, Message>()

    override __.View (KeyValue (k, v)) dispatch =
        li [] [
            textf "%i => %s" k v
            button [on.click (fun _ -> dispatch (RemoveItem k))] [text "Remove"]
        ]

let Display model dispatch =
    concat [
        div [] [
            input [
                attr.value (string model.currentKey)
                on.change (fun e -> dispatch (SetCurrentKey (int (e.Value :?> string))))
                attr.``type`` "number"
                attr.placeholder "Key"
            ]
            input [
                attr.value (string model.currentValue)
                on.change (fun e -> dispatch (SetCurrentValue (e.Value :?> string)))
                attr.placeholder "Value"
            ]
            button [on.click (fun _ -> dispatch AddItem)] [text "Add"]
        ]
        div [] [
            button [on.click (fun _ -> dispatch RefreshItems)] [text "Refresh"]
        ]
        ul [] [for item in model.items -> ecomp<Item, _, _> item dispatch]
        pre [] [
            match model.lastError with
            | None -> ()
            | Some exn -> yield text (string exn)
        ]
    ]

type MyApp() =
    inherit ElmishProgramComponent<Model, Message>()

    override this.Program =
        let myApi = this.Remote<MyApi>()
        Program.mkProgram (fun _ -> InitModel, Cmd.ofMsg RefreshItems) (Update myApi) Display
        |> Program.withConsoleTrace



open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Blazor.Builder
open Microsoft.AspNetCore.Blazor.Hosting

type Startup() =

    member __.ConfigureServices(services: IServiceCollection) =
        services.AddRemoting()
        |> ignore

    member __.Configure(app: IBlazorApplicationBuilder) =
        app.AddComponent<MyApp>("#main")

module Program =
    [<EntryPoint>]
    let Main args =
        BlazorWebAssemblyHost.CreateDefaultBuilder()
            .UseBlazorStartup<Startup>()
            .Build()
            .Run()
        0
