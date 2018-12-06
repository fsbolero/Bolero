#!/bin/bash
set -e

if [ "$OS" = "Windows_NT" ]; then
    if ! [ -f .paket/paket.exe ]; then dotnet tool install paket --tool-path .paket; fi
    if ! [ -f .paket/fake.exe ]; then dotnet tool install fake-cli --tool-path .paket; fi
else
    if ! [ -f .paket/paket ]; then dotnet tool install paket --tool-path .paket; fi
    if ! [ -f .paket/fake ]; then dotnet tool install fake-cli --tool-path .paket; fi
fi

PATH="~/.dotnet:$PATH"
.paket/fake build "$@"
