#!/bin/bash
set -e

dotnet tool restore
dotnet fake -v build "$@"
