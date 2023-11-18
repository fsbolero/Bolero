#!/usr/bin/env pwsh

dotnet tool restore
dotnet tool run paket restore
dotnet run --project .build -- $args
if ($LastExitCode -ne 0) { throw "Build failed" }
