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

namespace Bolero.Server.RazorHost

open System.Runtime.CompilerServices
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Mvc.Rendering
open Microsoft.AspNetCore.Components
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Http

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

[<Extension>]
type RazorHostingExtensions =

    [<Extension>]
    static member RenderComponentAsync<'T when 'T :> IComponent>(html: IHtmlHelper, config: IBoleroHostConfig) =
        match config.IsServer, config.IsPrerendered with
        | true,  true  -> html.RenderComponentAsync<'T>(RenderMode.ServerPrerendered)
        | true,  false -> html.RenderComponentAsync<'T>(RenderMode.Server)
        | false, true  -> html.RenderComponentAsync<'T>(RenderMode.Static)
        | false, false -> Task.FromResult(null)


    [<Extension>]
    static member RenderBoleroScript(html: IHtmlHelper, config: IBoleroHostConfig) =
        let k = if config.IsServer then "server" else "webassembly"
        html.Raw($"""<script src="_framework/blazor.{k}.js"></script>""")

    [<Extension>]
    static member AddBoleroHost(this: IServiceCollection, ?server: bool, ?prerendered: bool, ?devToggle: bool) =
        let server = defaultArg server false
        let prerendered = defaultArg prerendered true
        let devToggle = defaultArg devToggle true
        if devToggle then
            this.AddSingleton(
                { new IBoleroHostBaseConfig with
                    member _.IsServer = server
                    member _.IsPrerendered = prerendered })
                .AddScoped<IBoleroHostConfig, BoleroHostConfig>()
                .AddHttpContextAccessor()
        else
            this.AddSingleton(
                { new IBoleroHostConfig with
                    member _.IsServer = server
                    member _.IsPrerendered = prerendered })
