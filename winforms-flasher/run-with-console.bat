@echo off
echo Starting ESP Flasher with console output...
echo.
echo If the app crashes, you'll see the error here.
echo.

cd /d "%~dp0"
dotnet run --configuration Release --no-build

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo App exited with error code: %ERRORLEVEL%
)

echo.
pause
