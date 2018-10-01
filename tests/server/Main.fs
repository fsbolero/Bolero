module MiniBlazor.Test.Server.Main

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Blazor.Builder
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection

type ClientStartup() =
    static member val args = [||] with get, set

    member this.ConfigureServices(services: IServiceCollection) =
        ()

    member this.Configure(app: IBlazorApplicationBuilder) =
        MiniBlazor.Test.Client.Main.Main ClientStartup.args
        |> ignore

type Startup() =

    member this.ConfigureServices(services: IServiceCollection) =
        services.AddServerSideBlazor<ClientStartup>()
        |> ignore

    member this.Configure(app: IApplicationBuilder) =
        app.UseServerSideBlazor<ClientStartup>()
        |> ignore

[<EntryPoint>]
let Main args =
    ClientStartup.args <- args
    WebHost.CreateDefaultBuilder()
        .UseStartup<Startup>()
        .Build()
        .Run()
    0