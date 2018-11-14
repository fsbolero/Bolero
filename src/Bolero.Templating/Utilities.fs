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
