// ASP.NET Core and Blazor startup for web tests.
namespace Bolero.Tests.Web

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Blazor.Builder
open Bolero.Remoting

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
        app.Use(fun ctx next ->
            if ctx.Request.Path.Value = "/" then
                use w = new System.IO.StreamWriter(ctx.Response.Body)
                w.WriteAsync("""<!DOCTYPE html>
<html>
    <head><base href="/"></head>
    <body>
        <div id="app"></div>
        <script src="_framework/blazor.server.js"></script>
    </body>
</html>"""
                )
            else
                next.Invoke())
            .UseServerSideBlazor<BlazorStartup>()
        |> ignore
