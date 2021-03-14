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

open System.Runtime.CompilerServices
open Microsoft.AspNetCore.Mvc.Rendering
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

type IBoleroHostConfig =
    abstract IsServer: bool
    abstract IsPrerendered: bool

type IBoleroHostBaseConfig =
    abstract IsServer: bool
    abstract IsPrerendered: bool

type BoleroHostConfig(baseConfig: IBoleroHostBaseConfig, env: IHostEnvironment, ctx: IHttpContextAccessor) =

    member val IsServer =
        let mutable queryParam = Unchecked.defaultof<_>
        let mutable parsed = false
        if env.IsDevelopment()
            && ctx.HttpContext.Request.Query.TryGetValue("server", &queryParam)
            && bool.TryParse(queryParam.[0], &parsed)
        then
            parsed
        else
            baseConfig.IsServer

    member _.IsPrerendered = baseConfig.IsPrerendered

    interface IBoleroHostConfig with
        member this.IsServer = this.IsServer
        member this.IsPrerendered = this.IsPrerendered

    static member internal Body(config: IBoleroHostConfig) =
        let k = if config.IsServer then "server" else "webassembly"
        $"""<script src="_framework/blazor.{k}.js"></script>"""
