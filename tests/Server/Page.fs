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

module Bolero.Test.Server.Page

open Bolero.Html
open Bolero.Server.Html

let index = doctypeHtml [] [
    head [] [
        title [] [text "Bolero (server side)"]
        meta [attr.charset "UTF-8"]
        ``base`` [attr.href "/"]
    ]
    body [] [
        div [attr.id "main"] [
            rootComp<Bolero.Test.Client.Main.MyApp>
        ]
        hr []
        a [attr.href "/external-link"] [
            text "Non-routed link (this link must point to a separate static page)"
        ]
        boleroScript
    ]
]
