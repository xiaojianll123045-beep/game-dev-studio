@echo off
chcp 65001 >nul
setlocal

set VER=%~1
if "%VER%"=="" set VER=0.1
set ENG=C:\Users\xijil\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64.exe

echo === Building GameTycoon v%VER% ===
echo.

echo [1/3] Compiling sandbox native DLL (MinGW GCC)
pushd "%~dp0extensions\mod_sandbox_native"
g++ -shared -o mod_sandbox_hook.dll mod_sandbox_hook.cpp -static -static-libgcc -static-libstdc++ -O2 -Wl,--exclude-all-symbols
if errorlevel 1 (
    echo === Native DLL build FAILED (确保已安装 MinGW GCC) ===
    popd
    pause
    exit /b 1
)
rem Copy to build directory for P/Invoke
if not exist "%~dp0build" mkdir "%~dp0build"
copy /Y mod_sandbox_hook.dll "%~dp0build\" >nul
popd
echo.

echo [2/3] Cleaning cache and compiling C#
if exist "%~dp0.godot\mono\temp" rmdir /S /Q "%~dp0.godot\mono\temp"
dotnet build "%~dp0GameTycoon.csproj" --configuration Release
if errorlevel 1 (
    echo === C# build FAILED ===
    pause
    exit /b 1
)

echo [3/3] Exporting (Godot packs PCK with excluded dirs)
%ENG% --headless --path "%~dp0." --export-release "Windows Desktop"
if errorlevel 1 (
    echo === Export FAILED ===
    pause
    exit /b 1
)

echo.
echo === Done! build\GameTycoon.exe v%VER% ===
echo (DLC and mods excluded from PCK - use user:// for external content)
echo (沙箱安全: mod_sandbox_hook.dll 已打包到 build\)
pause
