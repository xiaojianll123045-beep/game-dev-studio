@echo off
setlocal

set VER=%~1
if "%VER%"=="" set VER=0.1

echo === Building GameTycoon v%VER% (naked) ===

set ENG=C:\Users\xijil\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64.exe

echo === Compiling C# Release ===
dotnet build "%~dp0游戏开发商.csproj" --configuration Release
if errorlevel 1 (
    echo === C# build failed ===
    pause
    exit /b 1
)

echo === Cleaning build folder ===
if exist "%~dp0build" rmdir /S /Q "%~dp0build"
mkdir "%~dp0build\project"

echo === Copying game files ===
xcopy "%~dp0icon.png" "%~dp0build\project\" /Y
xcopy "%~dp0project.godot" "%~dp0build\project\" /Y
xcopy /E /I "%~dp0assets" "%~dp0build\project\assets"
xcopy /E /I "%~dp0locales" "%~dp0build\project\locales"
xcopy /E /I "%~dp0scripts" "%~dp0build\project\scripts"
xcopy /E /I "%~dp0tutorial_data" "%~dp0build\project\tutorial_data" 2>nul

echo === Copying Godot engine ===
copy "%ENG%" "%~dp0build\GameTycoon.exe" /Y

echo === Creating launcher ===
echo @echo off > "%~dp0build\run.bat"
echo start "" "%%~dp0GameTycoon.exe" --path "%%~dp0project" --rendering-driver d3d12 >> "%~dp0build\run.bat"

echo === Done! build\GameTycoon.exe + project\ v%VER% ===
echo Double-click build\run.bat to play.

pause
