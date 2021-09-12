@echo off

dotnet tool restore
dotnet fake -v build %*
