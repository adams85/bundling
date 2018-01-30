msbuild /p:TagVersion=1.0.0-0-x /p:Revision=0
IF %ERRORLEVEL% NEQ 0 goto:eof

nuget delete Karambolo.AspNetCore.Bundling.1.0.0.nupkg -Source %1
nuget delete Karambolo.AspNetCore.Bundling.NUglify.1.0.0.nupkg -Source %1
nuget delete Karambolo.AspNetCore.Bundling.Less.1.0.0.nupkg -Source %1

nuget add Karambolo.AspNetCore.Bundling.1.0.0.nupkg -Source %1
IF %ERRORLEVEL% NEQ 0 goto:eof

nuget add Karambolo.AspNetCore.Bundling.NUglify.1.0.0.nupkg -Source %1
IF %ERRORLEVEL% NEQ 0 goto:eof

nuget add Karambolo.AspNetCore.Bundling.WebMarkupMin.1.0.0.nupkg -Source %1
IF %ERRORLEVEL% NEQ 0 goto:eof

nuget add Karambolo.AspNetCore.Bundling.Less.1.0.0.nupkg -Source %1
IF %ERRORLEVEL% NEQ 0 goto:eof
