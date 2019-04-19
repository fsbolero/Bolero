#!/bin/bash
set -e

if [ "$OS" = "Windows_NT" ]; then EXE_EXT=.exe; else EXE_EXT=; fi

if ! [ -f ".paket/paket$EXE_EXT" ]; then dotnet tool install paket --tool-path .paket; fi
if ! [ -f ".paket/fake$EXE_EXT" ]; then dotnet tool install fake-cli --tool-path .paket; fi
if ! [ -f ".paket/nbgv$EXE_EXT" ]; then dotnet tool install nbgv --tool-path .paket; fi

.paket/fake build "$@"
