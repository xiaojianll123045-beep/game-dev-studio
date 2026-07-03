@echo off
setlocal

set VER=%~1
if "%VER%"=="" set VER=0.1

echo === Building GameTycoon v%VER% (naked — standalone folder) ===

set GODOT="C:\Users\xijil\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64.exe"

echo === Compiling C# Release ===
dotnet build "%~dp0.\游戏开发商.csproj" --configuration Release
if errorlevel 1 (
    echo === C# build failed ===
    pause
    exit /b 1
)

echo === Copying project files to build\project\ ===
if exist "%~dp0build" rmdir /S /Q "%~dp0build"
mkdir "%~dp0build\project"

REM 只复制游戏需要的文件
xcopy "%~dp0*.dll" "%~dp0build\project\" /Y
xcopy "%~dp0*.pdb" "%~dp0build\project\" /Y
xcopy "%~dp0export_presets.cfg" "%~dp0build\project\" /Y
xcopy "%~dp0project.godot" "%~dp0build\project\" /Y
xcopy "%~dp0icon.png" "%~dp0build\project\" /Y
xcopy /E /I "%~dp0assets" "%~dp0build\project\assets"
xcopy /E /I "%~dp0locales" "%~dp0build\project\locales"
xcopy /E /I "%~dp0scripts" "%~dp0build\project\scripts"
REM DLC、mods、mod_docs 不打包（用户自行从 user:// 安装）
xcopy /E /I "%~dp0tutorial_data" "%~dp0build\project\tutorial_data" 2>nul

echo === Copying Godot executable ===
copy %GODOT% "%~dp0build\GameTycoon.exe" /Y

echo === Creating run.bat ===
echo @echo off > "%~dp0build\run.bat"
echo start "" "%%~dp0GameTycoon.exe" --path "%%~dp0project" --rendering-driver d3d12 >> "%~dp0build\run.bat"

echo === Done! build\GameTycoon.exe + project\ v%VER% ===
echo Double-click build\run.bat to play.

pause
