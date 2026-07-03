@echo off
chcp 65001 >nul
setlocal

set VER=%~1
if "%VER%"=="" set VER=0.1
set ENG=C:\Users\xijil\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64.exe
set PROJ=%~dp0.

echo === Building GameTycoon v%VER% (naked) ===
echo.

echo [1/4] Compiling C#
dotnet build "%PROJ%\GameTycoon.csproj" --configuration Release
if errorlevel 1 (
    echo === C# build FAILED ===
    pause
    exit /b 1
)

echo [2/4] Cleaning build folder
if exist "%~dp0build" rmdir /S /Q "%~dp0build"
mkdir "%~dp0build\project"

echo [3/4] Copying game files
xcopy "%PROJ%\icon.png" "%~dp0build\project\" /Y >nul
xcopy "%PROJ%\project.godot" "%~dp0build\project\" /Y >nul
for %%D in (assets locales scripts tutorial_data) do (
    if exist "%PROJ%\%%D" (
        xcopy /E /I "%PROJ%\%%D" "%~dp0build\project\%%D" >nul
    )
)

echo [4/4] Copying Godot engine and creating launcher
copy "%ENG%" "%~dp0build\GameTycoon.exe" /Y >nul
echo @echo off > "%~dp0build\run.bat"
echo start "" "%%~dp0GameTycoon.exe" --path "%%~dp0project" --rendering-driver d3d12 >> "%~dp0build\run.bat"

echo.
echo === Done! build\GameTycoon.exe + project\ v%VER% ===
echo Double-click build\run.bat to play.
pause
