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

/// <summary>Functions to enable the router in an Elmish program.</summary>
/// <category>Elmish</category>
module Bolero.Program

open System.Reflection
open System.Threading.Tasks
open Elmish

/// <summary>
/// Create a simple program for a component that uses StreamRendering.
/// </summary>
/// <param name="initialModel">The model that is shown initially.</param>
/// <param name="load">Load the model to be stream-rendered.</param>
/// <param name="update">The Elmish update function.</param>
/// <param name="view">The Elmish view function.</param>
let mkSimpleStreamRendering
        (initialModel: 'model)
        (load: 'model -> Task<'model>)
        (update: 'msg -> 'model -> 'model)
        (view: 'model -> Dispatch<'msg> -> Node)
        : Program<'model, 'msg> =
    Program.mkSimple (fun (comp: ProgramComponent<'model, 'msg>) ->
            comp.StreamingInit <- Some (fun x -> task {
                let! model = load x
                return model, Cmd.none
            })
            initialModel)
        update view

/// <summary>
/// Create a program for a component that uses StreamRendering.
/// </summary>
/// <param name="initialModel">The model that is shown initially.</param>
/// <param name="load">Load the model to be stream-rendered.</param>
/// <param name="update">The Elmish update function.</param>
/// <param name="view">The Elmish view function.</param>
let mkStreamRendering
        (initialModel: 'model)
        (load: 'model -> Task<'model * Cmd<'msg>>)
        (update: 'msg -> 'model -> 'model * Cmd<'msg>)
        (view: 'model -> Dispatch<'msg> -> Node)
        : Program<'model, 'msg>  =
    Program.mkProgram (fun (comp: ProgramComponent<'model, 'msg>) ->
            comp.StreamingInit <- Some load
            initialModel, [])
        update view

/// <summary>
/// Attach `router` to `program` when it is run as the `Program` of a `ProgramComponent`.
/// </summary>
/// <param name="router">The router.</param>
/// <param name="program">The Elmish program.</param>
/// <returns>The Elmish program configured with routing.</returns>
let withRouter
        (router: IRouter<'model, 'msg>)
        (program: Program<'model, 'msg>) =
    program
    |> Program.map
        (fun init comp ->
            let model, initCmd = init comp
            let update = typeof<Program<'model, 'msg>>.GetProperty("update", BindingFlags.NonPublic ||| BindingFlags.Instance).GetValue(program) :?> _
            let model, compCmd = comp.InitRouter(router, update, model)
            model, initCmd @ compCmd)
        id id id id id

/// <summary>
/// Attach a router inferred from `makeMessage` and `getEndPoint` to `program`
/// when it is run as the `Program` of a `ProgramComponent`.
/// </summary>
/// <param name="makeMessage">Function that creates a message from an endpoint value.</param>
/// <param name="getEndPoint">Function that extracts the current endpoint from the model.</param>
/// <param name="program">The Elmish program.</param>
/// <returns>The Elmish program configured with routing.</returns>
let withRouterInfer
        (makeMessage: 'ep -> 'msg)
        (getEndPoint: 'model -> 'ep)
        (program: Program<'model, 'msg>) =
    program
    |> withRouter (Router.infer makeMessage getEndPoint)
