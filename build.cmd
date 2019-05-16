@echo off

if not exist .paket\paket.exe dotnet tool install paket --tool-path .paket
if not exist .paket\fake.exe dotnet tool install fake-cli --tool-path .paket
if not exist .paket\nbgv.exe dotnet tool install nbgv --tool-path .paket

.paket\fake build %*
if errorlevel 1 exit /b %errorlevel%
