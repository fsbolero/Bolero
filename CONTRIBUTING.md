# Contributing to Bolero

## Working with this repository

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
    build -t run-client
    ```

* Run the test web application on the server side using Blazor.Server:

    ```
    build -t run-server
    ```

* Run the test web application for remoting:

    ```
    build -t run-remoting
    ```
