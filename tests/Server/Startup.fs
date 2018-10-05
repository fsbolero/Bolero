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
        app.UseServerSideBlazor<Client.Startup>()
        |> ignore

module Program =
    [<EntryPoint>]
    let Main args =
        WebHost.CreateDefaultBuilder()
            .UseStartup<Startup>()
            .Build()
            .Run()
        0