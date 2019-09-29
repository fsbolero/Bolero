@echo off

dotnet tool restore
dotnet fake build %*
