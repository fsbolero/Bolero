[<AutoOpen>]
module internal Bolero.Templating.Utilities

open FSharp.Quotations

module Map =

    let merge m1 m2 =
        Map.foldBack Map.add m1 m2

module Expr =

    let TypedArray<'T> (items: seq<Expr<'T>>) : Expr<'T[]> =
        Expr.NewArray(typeof<'T>, [ for i in items -> upcast i ])
        |> Expr.Cast
