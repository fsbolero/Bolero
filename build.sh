#!/bin/bash
set -e

if ! [ -f .paket/paket.exe ]; then dotnet tool install paket --tool-path .paket; fi
if ! [ -f .paket/fake.exe ]; then dotnet tool install fake-cli --tool-path .paket; fi

PATH="~/.dotnet:$PATH"
.paket/paket restore
dotnet restore
.paket/fake build "$@"
