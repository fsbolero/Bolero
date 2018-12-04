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
