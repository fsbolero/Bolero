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

/// Functions to enable the router in an Elmish program.
/// [category: Routing]
module Bolero.Program

open Elmish

/// Attach `router` to `program` when it is run as the `Program` of a `ProgramComponent`.
let withRouter
        (router: IRouter<'model, 'msg>)
        (program: Program<'model, 'msg>) =
    program
    |> Program.map
        (fun init comp ->
            let model, initCmd = init comp
            let model, compCmd = comp.InitRouter(router, program, model)
            model, initCmd @ compCmd)
        id id id id

/// Attach a router inferred from `makeMessage` and `getEndPoint` to `program`
/// when it is run as the `Program` of a `ProgramComponent`.
let withRouterInfer
        (makeMessage: 'ep -> 'msg)
        (getEndPoint: 'model -> 'ep)
        (program: Program<'model, 'msg>) =
    program
    |> withRouter (Router.infer makeMessage getEndPoint)
