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
