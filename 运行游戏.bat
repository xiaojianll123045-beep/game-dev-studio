@echo off
chcp 65001 >nul
setlocal

set ENG=C:\Users\xijil\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64.exe

echo ===================== Dev Run =====================
echo.

echo [1/3] Compiling sandbox native DLL
pushd "%~dp0extensions\mod_sandbox_native"
g++ -shared -o mod_sandbox_hook.dll mod_sandbox_hook.cpp -static -static-libgcc -static-libstdc++ -O2 -Wl,--exclude-all-symbols
if errorlevel 1 (
    echo ===================== Native DLL build FAILED =====================
    echo (If no MinGW GCC, sandbox runs in C# fallback mode)
    echo (没有 native DLL 时沙箱将运行在 C# 层拦截模式)
    popd
) else (
    copy /Y mod_sandbox_hook.dll "%~dp0" >nul
    echo Native DLL compiled OK
    popd
)
echo.

echo [2/3] Cleaning cache and compiling C#
if exist "%~dp0.godot\mono\temp" rmdir /S /Q "%~dp0.godot\mono\temp"
dotnet build "%~dp0GameTycoon.csproj"
if errorlevel 1 (
    echo ===================== Build failed =====================
    pause
    exit /b 1
)

echo.
echo [3/3] Launching game (close game window to return here)
echo.
"%ENG%" --path "%~dp0." --rendering-driver d3d12

echo.
echo ===================== Game closed =====================
pause