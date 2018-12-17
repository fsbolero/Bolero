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

namespace Bolero.Test.Server

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Bolero.Test

type Startup() =

    member this.ConfigureServices(services: IServiceCollection) =
        services.AddServerSideBlazor<Client.Startup>()
        |> ignore

    member this.Configure(app: IApplicationBuilder) =
        app
            // UseServerSideBlazor will try to serve Client's index.html, which points to blazor.webassembly.js.
            // This overrides it with Server's index.html.
            .UseDefaultFiles()
            .UseStaticFiles()
            // Then we can let ServerSideBlazor do its thing.
            .UseServerSideBlazor<Client.Startup>()
        |> ignore

module Program =
    [<EntryPoint>]
    let Main args =
        WebHost.CreateDefaultBuilder()
            .UseStartup<Startup>()
            .Build()
            .Run()
        0