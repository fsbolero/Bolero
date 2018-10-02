@echo off

.paket\paket restore
if errorlevel 1 exit /b %errorlevel%

cd .paket\fake

dotnet restore
if errorlevel 1 exit /b %errorlevel%

dotnet fake run ..\..\build.fsx %*
if errorlevel 1 exit /b %errorlevel%

cd ..\..
