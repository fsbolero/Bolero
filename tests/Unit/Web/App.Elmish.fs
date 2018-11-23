module Bolero.Tests.Web.App.Elmish

open Bolero
open Bolero.Html
open Elmish

type Model =
    {
        constValue: string
        stringValue: string
        intValue: int
    }

let initModel =
    {
        constValue = "constant value"
        stringValue = "stringValueInit"
        intValue = 42
    }

type Message =
    | SetStringValue of string
    | SetIntValue of int

let update msg model =
    match msg with
    | SetStringValue v -> { model with stringValue = v }, []
    | SetIntValue v -> { model with intValue = v }, []

type IntInput() =
    inherit ElmishComponent<int, int>()

    override this.View model dispatch =
        concat [
            input [
                attr.classes ["intValue-input"]
                attr.value model
                on.input (fun e -> dispatch (int (e.Value :?> string)))
            ]
            span [attr.classes ["intValue-repeat"]] [textf "%i" model]
        ]

let view model dispatch =
    div [attr.classes ["container"]] [
        input [attr.classes ["constValue-input"]; attr.value model.constValue]
        input [
            attr.classes ["stringValue-input"]
            attr.value model.stringValue
            on.input (fun e -> dispatch (SetStringValue (e.Value :?> string)))
        ]
        span [attr.classes ["stringValue-repeat"]] [text model.stringValue]
        ecomp<IntInput,_,_> model.intValue (SetIntValue >> dispatch)
    ]

type Test() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        Program.mkProgram (fun _ -> initModel, []) update view

let Tests() =
    div [attr.id "test-fixture-elmish"] [
        comp<Test> [] []
    ]
