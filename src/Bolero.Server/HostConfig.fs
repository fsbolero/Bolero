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

namespace Bolero.Server

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting

/// <summary>
/// The Bolero hosting configuration set by <see cref="M:Bolero.Server.ServerComponentsExtensions.AddBoleroHost" />
/// or <see cref="M:Bolero.Server.ServerComponentsExtensions.AddBoleroComponents" />.
/// </summary>
type IBoleroHostConfig =
    /// <summary>
    /// If true, use Bolero with interactive render modes, and bypass <see cref="P:IsServer"/> and <see cref="P:IsPrerendered"/>.
    /// If false, use Bolero's legacy render mode handling.
    /// </summary>
    abstract IsInteractiveRender: bool
    /// <summary>
    /// If true, use server-side Bolero; if false, use WebAssembly.
    /// Only applies if <see cref="IsInteractiveRender"/> is false.
    /// </summary>
    abstract IsServer: bool
    /// <summary>
    /// If true, prerender the initial view in the served HTML.
    /// Only applies if <see cref="IsInteractiveRender"/> is false.
    /// </summary>
    abstract IsPrerendered: bool

/// <exclude />
type IBoleroHostBaseConfig =
    abstract IsServer: bool
    abstract IsPrerendered: bool

/// <exclude />
type BoleroHostConfig(baseConfig: IBoleroHostBaseConfig, env: IHostEnvironment, ctx: IHttpContextAccessor) =

    member val IsServer =
        let mutable queryParam = Unchecked.defaultof<_>
        let mutable parsed = false
        if env.IsDevelopment()
            && ctx.HttpContext.Request.Query.TryGetValue("server", &queryParam)
            && bool.TryParse(queryParam[0], &parsed)
        then
            parsed
        else
            baseConfig.IsServer

    member _.IsPrerendered = baseConfig.IsPrerendered

    interface IBoleroHostConfig with
        member this.IsServer = this.IsServer
        member this.IsPrerendered = this.IsPrerendered
        member this.IsInteractiveRender = false

    static member internal Body(config: IBoleroHostConfig) =
        let k =
            if config.IsInteractiveRender then "web"
            elif config.IsServer then "server"
            else "webassembly"
        $"""<script src="_framework/blazor.{k}.js"></script>"""
