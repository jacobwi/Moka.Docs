@echo off
setlocal EnableDelayedExpansion

rem MokaDocs - build and run the sample projects.
rem
rem Interactive:     RunSamples.bat
rem Non-interactive: RunSamples.bat <choice> [tfm]     e.g. RunSamples.bat 2 net9.0
rem
rem The CLI and every sample multi-target net9.0;net10.0 (Directory.Build.props
rem sets TargetFrameworks, which overrides the singular TargetFramework the
rem AspNetCore sample declares), so each dotnet run needs an explicit -f.

set "ROOT=%~dp0.."
set "CLI=%ROOT%\src\Moka.Docs.Cli"
set "BUILT="
set "ONESHOT="
set "TFM=net10.0"

if not "%~2"=="" set "TFM=%~2"
if not "%~1"=="" (
	set "ONESHOT=1"
	set "CHOICE=%~1"
	goto dispatch
)

:menu
cls
echo ==========================================
echo   MokaDocs samples
echo ==========================================
echo   Target framework : %TFM%
if defined BUILT (echo   Solution         : built this session) else (echo   Solution         : not built yet)
echo.
echo   Static sites [mokadocs serve / build]
echo     1^) Library sample      - serve   http://localhost:5080
echo     2^) Library sample      - build to _site
echo     3^) Api sample          - serve   http://localhost:5081
echo     4^) Api sample          - build to _site
echo     P^) Python sample       - serve   http://localhost:5082  [needs Python 3.9+]
echo.
echo   Embedded docs [runs the web app, docs at /docs]
echo     5^) Api sample app          http://localhost:5312/docs
echo     6^) AspNetCore sample app   http://localhost:5311/docs
echo.
echo   Other
echo     7^) Build solution
echo     8^) Build every sample static site
echo     9^) Run the test suite
echo     F^) Toggle framework [net9.0 / net10.0]
echo     0^) Exit
echo.
set "CHOICE="
set /p "CHOICE=Select: "

:dispatch
if /i "%CHOICE%"=="1" goto lib_serve
if /i "%CHOICE%"=="2" goto lib_build
if /i "%CHOICE%"=="3" goto api_serve
if /i "%CHOICE%"=="4" goto api_build
if /i "%CHOICE%"=="5" goto api_app
if /i "%CHOICE%"=="6" goto aspnet_app
if /i "%CHOICE%"=="7" goto build_sln
if /i "%CHOICE%"=="8" goto build_all
if /i "%CHOICE%"=="9" goto run_tests
if /i "%CHOICE%"=="P" goto py_serve
if /i "%CHOICE%"=="F" goto toggle_tfm
if /i "%CHOICE%"=="0" goto end
if defined ONESHOT (
	echo Unknown choice: %CHOICE%
	exit /b 2
)
goto menu

:toggle_tfm
if "%TFM%"=="net10.0" (set "TFM=net9.0") else (set "TFM=net10.0")
goto menu

rem Builds once per session. Sets BUILD_FAILED rather than jumping, because a
rem goto out of a called routine leaves the call stack unbalanced.
:ensure_build
set "BUILD_FAILED="
if defined BUILT goto :eof
echo.
echo Building solution [first run this session]...
dotnet build "%ROOT%\MokaDocs.sln" -c Debug -v minimal
if errorlevel 1 (
	set "BUILD_FAILED=1"
	goto :eof
)
set "BUILT=1"
goto :eof

:build_sln
call :ensure_build
if defined BUILD_FAILED goto failed
echo.
echo Build complete.
goto after

:run_tests
call :ensure_build
if defined BUILD_FAILED goto failed
echo.
dotnet test "%ROOT%\MokaDocs.sln" --no-build -c Debug
goto after

:lib_serve
call :ensure_build
if defined BUILD_FAILED goto failed
echo.
echo Serving the Library sample on http://localhost:5080 - Ctrl+C to stop.
dotnet run --project "%CLI%" -f %TFM% --no-build -- serve --config "%ROOT%\samples\Moka.Docs.Samples.Library\mokadocs.yaml" --output "%ROOT%\samples\Moka.Docs.Samples.Library\_site" --port 5080
goto after

:lib_build
call :ensure_build
if defined BUILD_FAILED goto failed
echo.
dotnet run --project "%CLI%" -f %TFM% --no-build -- build --config "%ROOT%\samples\Moka.Docs.Samples.Library\mokadocs.yaml" --output "%ROOT%\samples\Moka.Docs.Samples.Library\_site"
goto after

:api_serve
call :ensure_build
if defined BUILD_FAILED goto failed
echo.
echo Serving the Api sample docs on http://localhost:5081 - Ctrl+C to stop.
dotnet run --project "%CLI%" -f %TFM% --no-build -- serve --config "%ROOT%\samples\Moka.Docs.Samples.Api\mokadocs.yaml" --output "%ROOT%\samples\Moka.Docs.Samples.Api\_site" --port 5081
goto after

:api_build
call :ensure_build
if defined BUILD_FAILED goto failed
echo.
dotnet run --project "%CLI%" -f %TFM% --no-build -- build --config "%ROOT%\samples\Moka.Docs.Samples.Api\mokadocs.yaml" --output "%ROOT%\samples\Moka.Docs.Samples.Api\_site"
goto after

rem The plugin shells out to Python, so this needs Python 3.9+ on PATH. It falls
rem back from python3 to python on Windows, where python3 is a Store alias.
:py_serve
call :ensure_build
if defined BUILD_FAILED goto failed
echo.
echo Serving the Python sample on http://localhost:5082 - Ctrl+C to stop.
dotnet run --project "%CLI%" -f %TFM% --no-build -- serve --config "%ROOT%\samples\Moka.Docs.Samples.Python\mokadocs.yaml" --output "%ROOT%\samples\Moka.Docs.Samples.Python\_site" --port 5082
goto after

:api_app
call :ensure_build
if defined BUILD_FAILED goto failed
echo.
echo Api sample app. Docs at http://localhost:5312/docs - Ctrl+C to stop.
dotnet run --project "%ROOT%\samples\Moka.Docs.Samples.Api" -f %TFM% --no-build --no-launch-profile --urls "http://localhost:5312"
goto after

:aspnet_app
call :ensure_build
if defined BUILD_FAILED goto failed
echo.
echo AspNetCore sample app. Docs at http://localhost:5311/docs - Ctrl+C to stop.
dotnet run --project "%ROOT%\samples\Moka.Docs.Samples.AspNetCore" -f %TFM% --no-build --no-launch-profile --urls "http://localhost:5311"
goto after

:build_all
call :ensure_build
if defined BUILD_FAILED goto failed
echo.
echo === Library sample ===
dotnet run --project "%CLI%" -f %TFM% --no-build -- build --config "%ROOT%\samples\Moka.Docs.Samples.Library\mokadocs.yaml" --output "%ROOT%\samples\Moka.Docs.Samples.Library\_site"
echo.
echo === Api sample ===
dotnet run --project "%CLI%" -f %TFM% --no-build -- build --config "%ROOT%\samples\Moka.Docs.Samples.Api\mokadocs.yaml" --output "%ROOT%\samples\Moka.Docs.Samples.Api\_site"
echo.
echo === Python sample [needs Python on PATH] ===
dotnet run --project "%CLI%" -f %TFM% --no-build -- build --config "%ROOT%\samples\Moka.Docs.Samples.Python\mokadocs.yaml" --output "%ROOT%\samples\Moka.Docs.Samples.Python\_site"
echo.
echo === Self-documenting site ===
dotnet run --project "%CLI%" -f %TFM% --no-build -- build --config "%ROOT%\mokadocs.yaml" --output "%ROOT%\_site"
echo.
echo All sites built.
goto after

:failed
echo.
echo Build FAILED. Fix the errors above before running a sample.
if defined ONESHOT exit /b 1
pause
goto menu

:after
if defined ONESHOT goto end
echo.
pause
goto menu

:end
endlocal
