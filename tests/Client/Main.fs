module Bolero.Test.Client.Main

open Microsoft.AspNetCore.Blazor.Routing
open Elmish
open Bolero
open Bolero.Html
open System.Net.Http

type Page =
    | [<EndPoint "/">] Form
    | [<EndPoint "/collection">] Collection
    | [<EndPoint "/collection-item">] Item of key: int

type Item =
    {
        K: int
        V: string
    }

type Model =
    {
        input: string
        submitted: option<string>
        addKey: int
        revOrder: bool
        items: Map<int, string>
        remoteResult: option<string>
        page: Page
    }

type Message =
    | SetInput of text: string
    | Submit
    | RemoveItem of key: int
    | SetAddKey of key: int
    | SetKeyOf of key: int
    | AddKey
    | ToggleRevOrder
    | SetPage of Page

let initModel _ =
    {
        input = ""
        submitted = None
        addKey = 4
        revOrder = false
        items = Map [
            0, "it's 0"
            1, "it's 1"
            2, "it's 2"
            3, "it's 3"
        ]
        page = Form
        remoteResult = None
    }

let router = Router.infer SetPage (fun m -> m.page)

type MyRemoting =
    {
        greet: string -> Async<string>
    }

let update message model =
    match message with
    | SetInput text -> { model with input = text }, []
    | Submit -> { model with submitted = Some model.input }, []
    | RemoveItem k -> { model with items = Map.filter (fun k' _ -> k' <> k) model.items }, []
    | SetAddKey i -> { model with addKey = i }, []
    | AddKey -> { model with items = Map.add model.addKey (sprintf "it's %i" model.addKey) model.items }, []
    | SetKeyOf k ->
        match Map.tryFind k model.items with
        | None -> model, []
        | Some item ->
            let items = model.items |> Map.remove k |> Map.add model.addKey item
            { model with items = items }, []
    | ToggleRevOrder -> { model with revOrder = not model.revOrder }, []
    | SetPage p -> { model with page = p }, []

// ondblclick's handler uses UIMouseEventArgs properties to check that we do generate specific UI*EventArgs.
// ondblclick isn't handled in the "super" case to check that we correctly generate no-op when an event hole is unfilled.
// onclick and onkeypress point to the same event to check that different UI*EventArgs are merged as UIEventArgs. -->
type SecretPw = Template<"""<div>
                                You typed the ${Kind} <i>pass<span>word</span></i>&excl;
                                <!-- Testing a comment -->
                                <button onclick="${Clear}" onkeypress="${Clear}" ondblclick="${DblClick}">Clear</button>
                                <input value="(default value)" bind="${Input}" /> <- You typed: ${Input}
                                <input type="number" bind="${Value}" />
                            </div>""">

let viewForm model dispatch =
    div [] [
        input [attr.value model.input; on.change (fun e -> dispatch (SetInput (unbox e.Value)))]
        input [
            attr.``type`` "submit"
            on.click (fun _ -> dispatch Submit)
            attr.style (if model.input = "" then "color:gray;" else null)
        ]
        div [] [text (defaultArg model.submitted "")]
        (match model.submitted with
        | Some s ->
            concat [
                cond (s.Contains "secret") <| function
                    | true ->
                        SecretPw()
                            .Kind(b [] [text "secret"])
                            .Clear(fun _ -> dispatch (SetInput ""))
                            .DblClick(fun e -> dispatch (SetInput (sprintf "(%i, %i)" e.ClientX e.ClientY)))
                            .Input(model.input, fun s -> dispatch (SetInput s))
                            .Value(model.addKey, fun k -> dispatch (SetAddKey k))
                            .Elt()
                    | false -> empty

                cond (s.Contains "super") <| function
                    | true ->
                        SecretPw()
                            .Kind("super secret")
                            .Clear(fun _ -> dispatch (SetInput ""))
                            .Elt()
                    | false -> empty
            ]
        | None -> empty)
    ]

type ItemTemplate = Template<"item.html">

type ViewItem() =
    inherit ElmishComponent<int * string, Message>()

    override this.View ((k, v)) dispatch =
        ItemTemplate()
            .Value(v)
            .SetKey(fun _ -> dispatch (SetKeyOf k))
            .Remove(fun _ -> dispatch (RemoveItem k))
            .Url(router.Link (Item k))
            .Elt()

let viewCollection model dispatch =
    let items =
        if model.revOrder then
            Seq.rev model.items
        else
            model.items :> _
    div [] [
        input [
            attr.``type`` "number"
            attr.value (string model.addKey)
            on.change (fun e -> dispatch (SetAddKey (int (unbox<string> e.Value))))
        ]
        button [on.click (fun _ -> dispatch AddKey)] [text "Add"]
        br []
        button [on.click (fun _ -> dispatch ToggleRevOrder)] [text "Toggle order"]
        ul [] [
            forEach items <| fun (KeyValue(k, v)) ->
                ecomp<ViewItem,_,_> (k, v) dispatch
        ]
    ]

type ViewItemPage() =
    inherit ElmishComponent<int * string, Message>()

    override this.View ((k, v)) dispatch =
        concat [
            p [] [text ("Viewing page for item #" + string k)]
            p [] [text ("Text is: " + v)]
            p [] [a [router.HRef Collection] [text "Back to collection"]]
        ]

let view model dispatch =
    concat [
        RawHtml """
            <div style="color:gray">The links below should have blue background based on the current page.</div>
            <style>.active { background: lightblue; }</style>
        """
        p [] [
            navLink NavLinkMatch.All [router.HRef Form] [text "Form"]
            text " "
            navLink NavLinkMatch.Prefix [router.HRef Collection] [text "Collection"]
        ]
        cond model.page <| function
            | Form -> viewForm model dispatch
            | Collection -> viewCollection model dispatch
            | Item k -> ecomp<ViewItemPage,_,_> (k, model.items.[k]) dispatch
    ]

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        Program.mkProgram (fun _ -> initModel(), []) update view
        |> Program.withConsoleTrace
        |> Program.withErrorHandler (fun (msg, exn) -> printfn "%s: %A" msg exn)
        |> Program.withRouter router
