# MiniBlazor - F# Tools for Blazor

MiniBlazor is a set of tools and libraries to run F# applications in WebAssembly using [Blazor](https://blazor.net/).

It includes:

* **HTML** functions using a familiar syntax similar to WebSharper.UI or Elmish.React.
* [**Elmish**](https://elmish.github.io/elmish/) integration: run an Elmish program as a Blazor component, and Blazor components as Elmish views.
* **Routing**: associate the page's current URL with a field in the Elmish model, defined as a discriminated union. Define path parameters directly on this union.
* F#-specific **optimizations**: MiniBlazor strips F# signature data from assemblies to reduce the size of the served application.

## Getting started with MiniBlazor

To get started, you need the following installed:

* .NET Core SDK 2.1.403 with .NET Core Runtime 2.1.3. Download it [here](https://www.microsoft.com/net/download/dotnet-core/2.1).
* On Linux / OSX: Mono 5.x. Download it [here](https://www.mono-project.com/download/stable/).



## This repository

In the instructions below, `build` means `./build.sh` on Linux and OSX, and `.\build.cmd` on Windows.

Alternatively, you can install Fake globally with `dotnet tool install -g fake-cli`, and run `fake build`.

### How to build

```
build
```

### How to run the test projects

* Run the unit tests:

    ```
    build -t test
    ```

* Debug the unit tests in Visual Studio Code:

    1. Run the unit tests in debug mode:

        ```
        build -t test-debug
        ```

        (Or in VS Code: "Run Test Task" -> "Unit Tests (debug)")

    2. When the output shows the following:

        ```
        Starting test execution, please wait...
        Host debugging is enabled. Please attach debugger to testhost process to continue.
        Process Id: 24780, Name: dotnet
        ```

        Go to the Debug panel, select "Attach" in the dropdown and click ▶️ "Start debugging".

    3. From the popup, select the process with the process id that was printed above.

    4. The tests may still not proceed properly at this point; in this case, click 🔄 "Reconnect" in the debug toolbar.

* Run the test web application on the client in WebAssembly:

    ```
    build -t runclient
    ```

* Run the test web application on the server side using Blazor.Server:

    ```
    build -t runserver
    ```
