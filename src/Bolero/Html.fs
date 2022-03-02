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

module Bolero.Html

open Bolero.Builders

type AttrBuilder() =
    member inline _.Yield([<InlineIfLambda>] attr: Attr) = attr
    member inline _.Delay([<InlineIfLambda>] a: unit -> Attr) = Attr(fun c b m i -> a().Invoke(c, b, m, i))
    member inline _.Combine([<InlineIfLambda>] x1: Attr, [<InlineIfLambda>] x2: Attr) =
        Attr(fun c b m i ->
            let i = x1.Invoke(c, b, m, i)
            x2.Invoke(c, b, m, i))

let attrs = AttrBuilder()

let concat = NodeBuilderBase()
