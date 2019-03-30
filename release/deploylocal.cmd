@echo off

SET PKGVER=1.0.0

msbuild /p:TagVersion=%PKGVER%-0-x /p:Revision=0
IF %ERRORLEVEL% NEQ 0 goto:eof

nuget delete Karambolo.AspNetCore.Bundling %PKGVER% -NonInteractive -Source %1
nuget delete Karambolo.AspNetCore.Bundling.EcmaScript %PKGVER% -NonInteractive -Source %1
nuget delete Karambolo.AspNetCore.Bundling.Less %PKGVER% -NonInteractive -Source %1
nuget delete Karambolo.AspNetCore.Bundling.NUglify %PKGVER% -NonInteractive -Source %1
nuget delete Karambolo.AspNetCore.Bundling.Sass %PKGVER% -NonInteractive -Source %1
nuget delete Karambolo.AspNetCore.Bundling.Tools %PKGVER% -NonInteractive -Source %1
nuget delete Karambolo.AspNetCore.Bundling.WebMarkupMin %PKGVER% -NonInteractive -Source %1

nuget add Karambolo.AspNetCore.Bundling.%PKGVER%.nupkg -Source %1
IF %ERRORLEVEL% NEQ 0 goto:eof

nuget add Karambolo.AspNetCore.Bundling.EcmaScript.%PKGVER%.nupkg -Source %1
IF %ERRORLEVEL% NEQ 0 goto:eof

nuget add Karambolo.AspNetCore.Bundling.Less.%PKGVER%.nupkg -Source %1
IF %ERRORLEVEL% NEQ 0 goto:eof

nuget add Karambolo.AspNetCore.Bundling.NUglify.%PKGVER%.nupkg -Source %1
IF %ERRORLEVEL% NEQ 0 goto:eof

nuget add Karambolo.AspNetCore.Bundling.Sass.%PKGVER%.nupkg -Source %1
IF %ERRORLEVEL% NEQ 0 goto:eof

nuget add Karambolo.AspNetCore.Bundling.Tools.%PKGVER%.nupkg -Source %1
IF %ERRORLEVEL% NEQ 0 goto:eof

nuget add Karambolo.AspNetCore.Bundling.WebMarkupMin.%PKGVER%.nupkg -Source %1
IF %ERRORLEVEL% NEQ 0 goto:eof
