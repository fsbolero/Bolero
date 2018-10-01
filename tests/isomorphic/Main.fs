module MiniBlazor.Test.Server.Main

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open MiniBlazor
open MiniBlazor.Test.Client

type MyApp() =
    inherit MiniBlazorStartup(IsomorphicApp.Of Main.MyApp)

type Startup() =
    member this.ConfigureServices(services: IServiceCollection) =
        services.AddIsomorphic<MyApp>()
        |> ignore

    member this.Configure(app: IApplicationBuilder) =
        app.UseServerSideIsomorphic<MyApp>("/")
        |> ignore

[<EntryPoint>]
let Main args =
    WebHost.CreateDefaultBuilder()
        .UseStartup<Startup>()
        .Build()
        .Run()
    0
