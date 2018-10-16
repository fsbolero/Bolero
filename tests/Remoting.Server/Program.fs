namespace MiniBlazor.Tests.Remoting

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection

type Startup() =

    member this.ConfigureServices(services: IServiceCollection) =
        services.AddServerSideBlazor<Client.Startup>()
        |> ignore

    member this.Configure(app: IApplicationBuilder) =
        app.UseBlazor<Client.Startup>()
        |> ignore

module Main =
    [<EntryPoint>]
    let Main args =
        WebHost.CreateDefaultBuilder()
            .UseStartup<Startup>()
            .Build()
            .Run()
        0