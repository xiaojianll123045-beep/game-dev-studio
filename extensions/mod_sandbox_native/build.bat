@echo off
chcp 65001 >nul
echo === 编译 mod_sandbox_hook.dll (MinGW GCC) ===
g++ -shared -o mod_sandbox_hook.dll mod_sandbox_hook.cpp -static -static-libgcc -static-libstdc++ -O2 -Wl,--exclude-all-symbols
if errorlevel 1 (
    echo 编译失败！
    pause
    exit /b 1
)
echo 编译成功: mod_sandbox_hook.dll
echo.
echo 安装：复制 mod_sandbox_hook.dll 到 build\ 目录或游戏 exe 同目录
pause
