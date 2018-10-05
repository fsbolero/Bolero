namespace MiniBlazor.Test.Server

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open MiniBlazor.Test

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