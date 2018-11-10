module Bolero.TemplatingInternals

open System

/// This indirection resolves two problems:
/// 1. TPs can't generate delegate constructor calls;
/// 2. TPs have problems with `_ -> unit`, see https://github.com/fsprojects/FSharp.TypeProviders.SDK/issues/279
let NoOp<'T>() =
    Action<'T>(ignore)

type TemplateNode() =
    /// For internal use only.
    member val Holes : obj[] = null with get, set
