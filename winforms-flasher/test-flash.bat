@echo off
echo Building test flasher...
dotnet build --configuration Debug

if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Running test flasher...
echo.
dotnet run --project ESPFlasher.csproj --configuration Debug -- --test-flash

pause
