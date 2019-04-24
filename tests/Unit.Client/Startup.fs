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

// ASP.NET Core and Blazor startup for web tests.
namespace Bolero.Tests.Client

open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Components.Builder
open Microsoft.AspNetCore.Blazor.Hosting

type Startup() =

    member this.ConfigureServices(services: IServiceCollection) =
        Bolero.Remoting.Client.ClientRemotingExtensions.AddRemoting(services)
        |> ignore

    member this.Configure(app: IComponentsApplicationBuilder) =
        app.AddComponent<Tests>("#app")

module Program =
    [<EntryPoint>]
    let Main args =
        BlazorWebAssemblyHost.CreateDefaultBuilder()
            .UseBlazorStartup<Startup>()
            .Build()
            .Run()
        0
