@echo off
chcp 65001 >nul
setlocal

set ENG=C:\Users\xijil\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64.exe

echo [1/2] Cleaning cache and compiling C#
if exist "%~dp0.godot\mono\temp" rmdir /S /Q "%~dp0.godot\mono\temp"
dotnet build "%~dp0GameTycoon.csproj"
if errorlevel 1 (
    echo === Build failed ===
    pause
    exit /b 1
)

echo.
echo [2/2] Launching game (close game window to return here)
echo.
"%ENG%" --path "%~dp0." --rendering-driver d3d12

echo.
echo === Game closed ===
pause
