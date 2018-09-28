# MiniBlazor

Experiment in minimal use of the Blazor tooling in F#.

In the instructions below, `build` means `./build.sh` on Linux and OSX, and `.\build.cmd` on Windows.

Alternatively, you can install Fake globally with `dotnet tool install -g fake-cli`, and run `fake build`.

## Requirements

* .NET Core SDK with .NET Core Runtime 2.1.3. Download it [here](https://www.microsoft.com/net/download/dotnet-core/2.1).
* On Linux / OSX: Mono 5.x.
* On Linux / OSX: Nodejs.

## How to build

```
build
```

## How to run the test project

```
build -t run
```