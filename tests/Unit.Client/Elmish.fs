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

module Bolero.Tests.Client.Elmish

open Microsoft.AspNetCore.Components
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

    [<Parameter>]
    member val ExtraClass = "" with get, set

    override this.View model dispatch =
        concat {
            input {
                attr.classes ["intValue-input"; this.ExtraClass]
                attr.value model
                on.input (fun e -> dispatch (int (e.Value :?> string)))
            }
            span { attr.classes ["intValue-repeat"]; $"{model}" }
        }

let view model dispatch =
    div {
        attr.classes ["container"]
        input { attr.classes ["constValue-input"]; attr.value model.constValue }
        input {
            attr.classes ["stringValue-input"]
            attr.value model.stringValue
            on.input (fun e -> dispatch (SetStringValue (e.Value :?> string)))
        }
        span { attr.classes ["stringValue-repeat"]; model.stringValue }
        ecomp<IntInput,_,_> model.intValue (SetIntValue >> dispatch) { "ExtraClass" => "intValue-extraClass" }
    }

type Test() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        Program.mkProgram (fun _ -> initModel, []) update view

let Tests() =
    div {
        attr.id "test-fixture-elmish"
        comp<Test>
    }
