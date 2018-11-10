# Bolero - F# Tools for Blazor

Bolero is a set of tools and libraries to run F# applications in WebAssembly using [Blazor](https://blazor.net/).

It includes:

* **HTML** functions using a familiar syntax similar to WebSharper.UI or Elmish.React.
* [**Elmish**](https://elmish.github.io/elmish/) integration: run an Elmish program as a Blazor component, and Blazor components as Elmish views.
* **Routing**: associate the page's current URL with a field in the Elmish model, defined as a discriminated union. Define path parameters directly on this union.
* **Remoting**: define asynchronous functions running on the server and simply call them from the client.
* F#-specific **optimizations**: Bolero strips F# signature data from assemblies to reduce the size of the served application.

## Getting started with Bolero

To get started, you need the following installed:

* .NET Core SDK 2.1.403 with .NET Core Runtime 2.1.3. Download it [here](https://www.microsoft.com/net/download/dotnet-core/2.1).
* On Linux / OSX: Mono 5.x. Download it [here](https://www.mono-project.com/download/stable/).

To learn more, you can check [the documentation](https://github.com/intellifactory/bolero/wiki).