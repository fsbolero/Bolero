namespace Bolero.Tests.Remoting

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Bolero.Remoting

type MyApiHandler(log: ILogger<MyApiHandler>) =
    inherit RemoteHandler<Client.MyApi>()

    let mutable items = Map.empty

    override this.Handler =
        {
            getItems = fun () -> async {
                log.LogInformation("Getting items")
                return items
            }
            setItem = fun (k, v) -> async {
                log.LogInformation("Setting {0} => {1}", k, v)
                items <- Map.add k v items
            }
            removeItem = fun k -> async {
                log.LogInformation("Removing {0}", k)
                items <- Map.remove k items
            }
        }

type Startup() =

    member this.ConfigureServices(services: IServiceCollection) =
        services
            .AddServerSideBlazor<Client.Startup>()
            .AddRemoting<MyApiHandler>()
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