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

open System
open System.Text
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Bolero.Server

module Program =

    #nowarn 20 // ignore method return values

    [<EntryPoint>]
    let Main args =
        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddMvc()
        builder.Services.AddServerSideBlazor()
        builder.Services.AddBoleroHost(prerendered = true)
        builder.Services.AddLogging()

        let app = builder.Build()

        app.UseDeveloperExceptionPage()
        app.UseStaticFiles()
        app.UseBlazorFrameworkFiles()
        app.MapGet("/external-link", fun ctx ->
            let body = "This is a static non-Bolero page" |> Encoding.UTF8.GetBytes
            ctx.Response.Body.WriteAsync(ReadOnlyMemory body).AsTask()
        )
        app.MapBlazorHub()
        app.MapFallbackToBolero(Page.index)

        app.Run()
        0
