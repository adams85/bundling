@echo off

IF NOT [%2] == [] (
  SET PKGVER=%2
)

IF [%2] == [] (
  SET PKGVER=1.0.0
)

echo %PKGVER%

msbuild /p:TagVersion=%PKGVER%-0-x /p:Revision=0
IF %ERRORLEVEL% NEQ 0 goto:eof

dotnet nuget delete Karambolo.AspNetCore.Bundling %PKGVER% --source %1 --non-interactive
dotnet nuget delete Karambolo.AspNetCore.Bundling.EcmaScript %PKGVER% --source %1 --non-interactive
dotnet nuget delete Karambolo.AspNetCore.Bundling.Less %PKGVER% --source %1 --non-interactive
dotnet nuget delete Karambolo.AspNetCore.Bundling.NUglify %PKGVER% --source %1 --non-interactive
dotnet nuget delete Karambolo.AspNetCore.Bundling.Sass %PKGVER% --source %1 --non-interactive
dotnet nuget delete Karambolo.AspNetCore.Bundling.Tools %PKGVER% --source %1 --non-interactive
dotnet nuget delete Karambolo.AspNetCore.Bundling.WebMarkupMin %PKGVER% --source %1 --non-interactive

dotnet nuget push Karambolo.AspNetCore.Bundling.%PKGVER%.nupkg --source %1
IF %ERRORLEVEL% NEQ 0 goto:eof

dotnet nuget push Karambolo.AspNetCore.Bundling.EcmaScript.%PKGVER%.nupkg --source %1
IF %ERRORLEVEL% NEQ 0 goto:eof

dotnet nuget push Karambolo.AspNetCore.Bundling.Less.%PKGVER%.nupkg --source %1
IF %ERRORLEVEL% NEQ 0 goto:eof

dotnet nuget push Karambolo.AspNetCore.Bundling.NUglify.%PKGVER%.nupkg --source %1
IF %ERRORLEVEL% NEQ 0 goto:eof

dotnet nuget push Karambolo.AspNetCore.Bundling.Sass.%PKGVER%.nupkg --source %1
IF %ERRORLEVEL% NEQ 0 goto:eof

dotnet nuget push Karambolo.AspNetCore.Bundling.Tools.%PKGVER%.nupkg --source %1
IF %ERRORLEVEL% NEQ 0 goto:eof

dotnet nuget push Karambolo.AspNetCore.Bundling.WebMarkupMin.%PKGVER%.nupkg --source %1
IF %ERRORLEVEL% NEQ 0 goto:eof
