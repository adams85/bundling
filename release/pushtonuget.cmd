@echo off

SET PKGVER=%1
SET APIKEY=%2
SET SOURCE=https://api.nuget.org/v3/index.json

dotnet nuget push Karambolo.AspNetCore.Bundling.%PKGVER%.nupkg -k %APIKEY% -s %SOURCE%
IF %ERRORLEVEL% NEQ 0 goto:eof

dotnet nuget push Karambolo.AspNetCore.Bundling.EcmaScript.%PKGVER%.nupkg -k %APIKEY% -s %SOURCE%
IF %ERRORLEVEL% NEQ 0 goto:eof

dotnet nuget push Karambolo.AspNetCore.Bundling.Less.%PKGVER%.nupkg -k %APIKEY% -s %SOURCE%
IF %ERRORLEVEL% NEQ 0 goto:eof

dotnet nuget push Karambolo.AspNetCore.Bundling.NUglify.%PKGVER%.nupkg -k %APIKEY% -s %SOURCE%
IF %ERRORLEVEL% NEQ 0 goto:eof

dotnet nuget push Karambolo.AspNetCore.Bundling.Sass.%PKGVER%.nupkg -k %APIKEY% -s %SOURCE%
IF %ERRORLEVEL% NEQ 0 goto:eof

dotnet nuget push Karambolo.AspNetCore.Bundling.Tools.%PKGVER%.nupkg -k %APIKEY% -s %SOURCE%
IF %ERRORLEVEL% NEQ 0 goto:eof

dotnet nuget push Karambolo.AspNetCore.Bundling.WebMarkupMin.%PKGVER%.nupkg -k %APIKEY% -s %SOURCE%
IF %ERRORLEVEL% NEQ 0 goto:eof
