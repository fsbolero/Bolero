#!/bin/bash
set -e

dotnet tool restore
dotnet paket restore
dotnet run --project .build -- "$@"
