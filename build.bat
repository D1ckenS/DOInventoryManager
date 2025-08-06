@echo off
echo Stopping application if running...
taskkill /f /im DOInventoryManager.exe 2>nul
if %errorlevel% == 0 (
    echo Application stopped.
    timeout /t 2 /nobreak >nul
) else (
    echo Application was not running.
)

echo Building application...
dotnet build
if %errorlevel% == 0 (
    echo Build completed successfully!
) else (
    echo Build failed!
)
pause