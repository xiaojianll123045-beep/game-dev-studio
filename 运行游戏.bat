@echo off
chcp 65001 >nul
setlocal

set ENG=C:\Users\xijil\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64.exe

echo === Dev Run ===
echo.

echo [1/3] Sandbox native DLL
pushd "%~dp0extensions\mod_sandbox_native"
g++ -shared -o mod_sandbox_hook.dll mod_sandbox_hook.cpp -static -static-libgcc -static-libstdc++ -O2 -Wl,--exclude-all-symbols >nul 2>&1
if errorlevel 1 goto native_warn
copy /Y mod_sandbox_hook.dll "%~dp0" >nul
echo   Native DLL OK
goto native_done
:native_warn
echo   Native DLL skipped - no MinGW GCC, using C# fallback
:native_done
popd
echo.

echo [2/3] Clean and compile C#
if exist "%~dp0.godot\mono\temp" rmdir /S /Q "%~dp0.godot\mono\temp"
dotnet build "%~dp0GameTycoon.csproj"
if errorlevel 1 goto fail

echo.
echo [3/3] Launching game
echo.
call :run_game
echo.
echo === Game closed ===
pause
exit /b 0

:fail
echo === Build FAILED ===
pause
exit /b 1

:run_game
set ENG_PATH=%ENG%
if not exist "%ENG_PATH%" (
    echo Godot engine not found: %ENG_PATH%
    exit /b 1
)
"%ENG_PATH%" --path "%~dp0." --rendering-driver d3d12
exit /b %ERRORLEVEL%