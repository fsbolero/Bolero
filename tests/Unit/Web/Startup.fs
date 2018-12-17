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
namespace Bolero.Tests.Web

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Blazor.Builder
open Bolero.Remoting
open Microsoft.Extensions.FileProviders

type BlazorStartup() =

    member this.ConfigureServices(services: IServiceCollection) =
        services.AddRemoting(
            let mutable items = Map.empty
            {
                getValue = fun k -> async {
                    return Map.tryFind k items
                }
                setValue = fun (k, v) -> async {
                    items <- Map.add k v items
                }
                removeValue = fun k -> async {
                    items <- Map.remove k items
                }
            } : App.Remoting.RemoteApi
        )
        |> ignore

    member this.Configure(app: IBlazorApplicationBuilder) =
        app.AddComponent<App.Tests>("#app")

type Startup() =

    member this.ConfigureServices(services: IServiceCollection) =
        services.AddServerSideBlazor<BlazorStartup>()
        |> ignore

    member this.Configure(app: IApplicationBuilder) =
        let fileProvider = new PhysicalFileProvider(__SOURCE_DIRECTORY__)
        app
            .UseDefaultFiles(DefaultFilesOptions(FileProvider = fileProvider))
            .UseStaticFiles(StaticFileOptions(FileProvider = fileProvider))
            .UseServerSideBlazor<BlazorStartup>()
        |> ignore
