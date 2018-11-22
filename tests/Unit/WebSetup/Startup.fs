// ASP.NET Core and Blazor startup for web tests.
namespace Bolero.Tests.Web

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Blazor.Builder

type BlazorStartup() =

    member this.ConfigureServices(services: IServiceCollection) =
        ()

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
