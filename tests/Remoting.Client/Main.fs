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

module Bolero.Tests.Remoting.Client

open System.Collections.Generic
open Microsoft.AspNetCore.Components.Authorization
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Elmish

type MyApi =
    {
        getItems : unit -> Async<Map<int, string>>
        setItem : (int * string) -> Async<unit>
        removeItem : int -> Async<unit>
        login : string -> Async<unit>
        logout : unit -> Async<unit>
        getLogin : unit -> Async<string>
        authDouble : int -> Async<int>
    }

    interface IRemoteService with
        member this.BasePath = "/myapi"

type Page =
    | Home
    | Custom of int

type Model =
    {
        page: Page
        currentKey: int
        currentValue: string
        items : Map<int, string>
        lastError : option<string>
        loginInput : string
        currentLogin : option<string>
        authDoubleInput : int
        authDoubleResult : string
    }

let InitModel =
    {
        page = Home
        currentKey = 0
        currentValue = ""
        items = Map.empty
        lastError = None
        loginInput = ""
        currentLogin = None
        authDoubleInput = 0
        authDoubleResult = ""
    }

type Message =
    | SetPage of Page
    | SetCurrentKey of int
    | SetCurrentValue of string
    | RefreshItems
    | AddItem
    | RemoveItem of int
    | ItemsRefreshed of Map<int, string>
    | GetLogin
    | SetLoginInput of string
    | Login
    | Logout
    | LoggedIn of option<string>
    | LoggedOut
    | SetAuthDoubleInput of int
    | SendAuthDouble
    | RecvAuthDouble of int
    | Exn of exn

let Update (myApi: MyApi) msg model =
    match msg with
    | SetPage p ->
        { model with page = p }, []
    | SetCurrentKey i ->
        { model with currentKey = i }, []
    | SetCurrentValue v ->
        { model with currentValue = v }, []
    | RefreshItems ->
        model,
        Cmd.OfAsync.either myApi.getItems () ItemsRefreshed Exn
    | AddItem ->
        model,
        Cmd.OfAsync.either myApi.setItem (model.currentKey, model.currentValue)
            (fun () -> RefreshItems) Exn
    | RemoveItem k ->
        model,
        Cmd.OfAsync.either myApi.removeItem k
            (fun () -> RefreshItems) Exn
    | ItemsRefreshed items ->
        { model with items = items; lastError = None }, []
    | GetLogin ->
        model, Cmd.OfAuthorized.either myApi.getLogin () LoggedIn Exn
    | SetLoginInput s ->
        { model with loginInput = s }, []
    | Login ->
        model, Cmd.OfAsync.either myApi.login model.loginInput (fun _ -> GetLogin) Exn
    | Logout ->
        model, Cmd.OfAsync.either myApi.logout () (fun () -> LoggedOut) Exn
    | LoggedIn res ->
        let error = if res.IsNone then Some "Failed to retrieve login: user not authenticated" else None
        { model with currentLogin = res; lastError = error }, []
    | LoggedOut ->
        { model with currentLogin = None; lastError = None }, []
    | SetAuthDoubleInput i ->
        { model with authDoubleInput = i }, []
    | SendAuthDouble ->
        model, Cmd.OfAsync.either myApi.authDouble model.authDoubleInput RecvAuthDouble Exn
    | RecvAuthDouble v ->
        { model with authDoubleResult = string v }, []
    | Exn RemoteUnauthorizedException ->
        { model with authDoubleResult = "Error: you must be logged in" }, []
    | Exn exn ->
        { model with lastError = Some (string exn) }, []

let router : Router<Page, Model, Message> =
    {
        getEndPoint = fun m -> m.page
        getRoute = function
            | Home -> "/"
            | Custom i -> sprintf "/custom/%i" i
        setRoute = fun s ->
            match s.Trim('/').Split('/') with
            | [|""|] -> Some (SetPage Home)
            | [|"custom"; i|] -> Some (SetPage (Custom (int i)))
            | _ -> None
    }

type Tpl = Template<"main.html">
type Form = Template<"subdir/form.html">

type Item() =
    inherit ElmishComponent<KeyValuePair<int, string>, Message>()

    override __.View (KeyValue (k, v)) dispatch =
        Tpl.item().key(string k).value(v)
            .remove(fun _ -> dispatch (RemoveItem k))
            .Elt()

let Display model dispatch =
    let form =
        Form()
            .key(string model.currentKey)
            .setKey(fun e -> dispatch (SetCurrentKey (int (e.Value :?> string))))
            .value(string model.currentValue)
            .setValue(fun e -> dispatch (SetCurrentValue (e.Value :?> string)))
            .add(fun _ -> dispatch AddItem)
            .Elt()
    concat [
        Tpl()
            .form(form)
            .refresh(fun _ -> dispatch RefreshItems)
            .items(forEach model.items <| fun item -> ecomp<Item, _, _> [attr.key item.Key] item dispatch)
            .error(
                cond model.lastError <| function
                | None -> empty
                | Some exn -> text (string exn)
            )
            .Elt()
        hr []
        cond model.currentLogin <| function
        | None ->
            concat [
                input [bind.change.string model.loginInput (dispatch << SetLoginInput)]
                button [on.click (fun _ -> dispatch Login)] [text "Log in"]
            ]
        | Some login ->
            concat [
                textf "Logged in as %s" login
                button [on.click (fun _ -> dispatch Logout)] [text "Log out"]
            ]
        hr []
        text "2 * "
        input [
            attr.``type`` "number"
            bind.change.int model.authDoubleInput (dispatch << SetAuthDoubleInput)
        ]
        text " = "
        button [on.click (fun _ -> dispatch SendAuthDouble)] [text "Send"]
        text model.authDoubleResult
        div [] [
            a [router.HRef Home] [text "Goto: home"]
            br []
            a [router.HRef (Custom 123)] [text "Goto: custom 123"]
        ]
        comp<CascadingAuthenticationState> [] [
            comp<AuthorizeView> [
                attr.fragmentWith "Authorized" <| fun (context: AuthenticationState) ->
                    printfn "Rendering Authorized"
                    div [] [textf "You're authorized! Welcome %s" context.User.Identity.Name]
                attr.fragmentWith "NotAuthorized" <| fun (_: AuthenticationState) ->
                    printfn "Rendering NotAuthorized"
                    div [] [text "You're not authorized :("]
            ] []
        ]
    ]

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let myApi = this.Remote<MyApi>()
        Program.mkProgram (fun _ -> InitModel, Cmd.batch [
            Cmd.ofMsg RefreshItems
            Cmd.ofMsg GetLogin
        ]) (Update myApi) Display
        |> Program.withRouter router


open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.DependencyInjection
open System.Security.Claims
open System.Threading.Tasks

type DummyAuthProvider() =
    inherit AuthenticationStateProvider()

    override _.GetAuthenticationStateAsync() =
        let identity = ClaimsIdentity([|Claim(ClaimTypes.Name, "loic")|], "Fake auth type")
        let user = ClaimsPrincipal(identity)
        Task.FromResult(AuthenticationState(user))

module Program =
    [<EntryPoint>]
    let Main args =
        let builder = WebAssemblyHostBuilder.CreateDefault(args)
        builder.RootComponents.Add<MyApp>("#main")
        builder.Services.AddRemoting(builder.HostEnvironment) |> ignore
        builder.Services.AddScoped<AuthenticationStateProvider, DummyAuthProvider>() |> ignore
        builder.Services.AddAuthorizationCore() |> ignore
        builder.Build().RunAsync() |> ignore
        0
