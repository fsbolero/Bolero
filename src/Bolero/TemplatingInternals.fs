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

[<AllowNullLiteral>]
type IClient =
#if !IS_DESIGNTIME
    [<Microsoft.JSInterop.JSInvokable>]
#endif
    abstract FileChanged : filename: string * content: string -> unit

    abstract RequestFile : filename: string -> option<Map<string, obj> -> Node>

module TemplateCache =

    let mutable client =
        { new IClient with
            member this.RequestFile(_) = None
            member this.FileChanged(_, _) = () }

#if !IS_DESIGNTIME
[<assembly:FSharp.Core.CompilerServices.TypeProviderAssembly "Bolero.Templating.Provider">]
do ()
#endif
