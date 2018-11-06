namespace MiniBlazor.Tests.Remoting

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open MiniBlazor.Server

type Startup() =

    member this.ConfigureServices(services: IServiceCollection) =
        let remotingClient : Client.MyApi =
            let mutable items = Map.empty
            {
                getItems = fun () -> async {
                    return items
                }
                setItem = fun (k, v) -> async {
                    items <- Map.add k v items
                }
                removeItem = fun k -> async {
                    items <- Map.remove k items
                }
            }

        services
            .AddServerSideBlazor<Client.Startup>()
            .AddRemoting("/myapi", remotingClient)
        |> ignore

    member this.Configure(app: IApplicationBuilder) =
        app.UseRemoting()
            .UseBlazor<Client.Startup>()
        |> ignore

module Main =
    [<EntryPoint>]
    let Main args =
        WebHost.CreateDefaultBuilder()
            .UseStartup<Startup>()
            .Build()
            .Run()
        0