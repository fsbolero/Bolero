module Bolero.TemplatingInternals

open System
open Microsoft.AspNetCore.Blazor

/// This indirection resolves two problems:
/// 1. TPs can't generate delegate constructor calls;
/// 2. Generative TPs have problems with `_ -> unit`, see https://github.com/fsprojects/FSharp.TypeProviders.SDK/issues/279
type Events =

    static member NoOp<'T>() =
        Action<'T>(ignore)

    static member OnChange(f: Action<string>) =
        Action<UIChangeEventArgs>(fun e ->
            f.Invoke(unbox<string> e.Value)
        )

    static member OnChangeInt(f: Action<int>) =
        Events.OnChange(fun s ->
            match Int32.TryParse(s) with
            | true, x -> f.Invoke(x)
            | false, _ -> ()
        )

    static member OnChangeFloat(f: Action<float>) =
        Events.OnChange(fun s ->
            match Double.TryParse(s) with
            | true, x -> f.Invoke(x)
            | false, _ -> ()
        )

    static member OnChangeBool(f: Action<bool>) =
        Action<UIChangeEventArgs>(fun e ->
            f.Invoke(unbox<bool> e.Value)
        )

type TemplateNode() =
    /// For internal use only.
    member val Holes : obj[] = null with get, set
