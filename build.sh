#!/bin/bash
set -e

PATH="~/.dotnet:$PATH"
if [ "$OS" = "Windows_NT" ]; then
    .paket/paket.exe restore
else
    mono .paket/paket.exe restore
fi
cd .paket/fake
dotnet restore
dotnet fake run ../../build.fsx "$@"
