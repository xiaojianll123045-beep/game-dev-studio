@echo off
setlocal

set VER=%~1
if "%VER%"=="" set VER=0.1

echo === Building GameTycoon v%VER% ===

echo === Exporting (Godot will compile C# internally) ===
set GODOT="C:\Users\xijil\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64.exe"
%GODOT% --path "%~dp0." --export-release "Windows Desktop"
if not errorlevel 1 (
    echo === Done! build\GameTycoon.exe v%VER% ===
) else (
    echo === Build failed (code %ERRORLEVEL%) ===
)

pause
