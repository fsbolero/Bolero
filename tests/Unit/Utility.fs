namespace Bolero.Tests

open FsCheck

[<AutoOpen>]
module Utility =

    let (.=.) left right = left = right |@ sprintf "\nActual: %A\nExpected: %A" left right

    let epsilon = 0.0001

    let (=~) left right = left - right < epsilon

    let (.=~.) left right = left =~ right |@ sprintf "\nActual: %A\nExpected: %A" left right
