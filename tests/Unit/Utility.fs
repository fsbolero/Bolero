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

namespace Bolero.Tests

open System
open FsCheck

[<AutoOpen>]
module Utility =

    let (.=.) left right = left = right |@ sprintf "\nActual: %A\nExpected: %A" left right

    let epsilon = 0.0001

    let (=~) left right = left - right < epsilon

    let (.=~.) left right = left =~ right |@ sprintf "\nActual: %A\nExpected: %A" left right

    /// FsCheck-generated alphanumerical string.
    type Alphanum =
        /// FsCheck-generated alphanumerical string.
        | Alphanum of string

    type ArbitraryModifiers =
        static member Alphanum() =
            { new Arbitrary<char[]>() with
                override __.Generator =
                    Gen.oneof [
                        Gen.choose(0, 25) |> Gen.map (fun i -> char (int 'a' + i))
                        Gen.choose(0, 25) |> Gen.map (fun i -> char (int 'A' + i))
                        Gen.choose(0, 9) |> Gen.map (fun i -> char (int '0' + i))
                    ]
                    |> Gen.arrayOf
                override __.Shrinker x =
                    if Array.isEmpty x then
                        Seq.empty
                    else
                        Seq.singleton x.[1..]
            }
            |> Arb.convert
                (String >> Alphanum)
                (fun (Alphanum s) -> s.ToCharArray())

[<NUnit.Framework.SetUpFixture>]
type GlobalFixture() =
    [<NUnit.Framework.OneTimeSetUp>]
    member this.SetUp() =
        Arb.register<ArbitraryModifiers>()
        |> ignore
