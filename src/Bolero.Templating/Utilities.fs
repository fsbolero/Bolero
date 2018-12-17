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

[<AutoOpen>]
module internal Bolero.Templating.Utilities

open FSharp.Quotations

#if DEBUG
let logFile = lazy new System.IO.StreamWriter(__SOURCE_DIRECTORY__ + "/../../tpl_log.txt", AutoFlush = true)
let logf format = Printf.kprintf logFile.Value.WriteLine format
#endif

/// Typed expression helpers
module TExpr =

    let Array<'T> (items: seq<Expr<'T>>) : Expr<'T[]> =
        Expr.NewArray(typeof<'T>, [ for i in items -> upcast i ])
        |> Expr.Cast

    let Coerce<'T> (e: Expr) : Expr<'T> =
        if e.Type = typeof<'T> then e else Expr.Coerce(e, typeof<'T>)
        |> Expr.Cast
