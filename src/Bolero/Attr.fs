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
open Microsoft.AspNetCore.Components

/// HTML attribute or Blazor component parameter.
/// Use `Bolero.Html.attr` or `(=>)` to create attributes.
/// [category: HTML]
type Attr = delegate of obj * Rendering.RenderTreeBuilder * (Type -> int * (obj -> int)) * int -> int

module Attr =

    let inline Make (name: string) (value: 'T) = Attr(fun _ tb _ i ->
        tb.AddAttribute(i, name, box value)
        i + 1)

    let inline Attrs (attrs: seq<Attr>) = Attr(fun comp tb matchCache i ->
        let mutable i = i
        for attr in attrs do
            i <- attr.Invoke(comp, tb, matchCache, i)
        i)

    let inline Empty() = Attr(fun _ _ _ i -> i)
