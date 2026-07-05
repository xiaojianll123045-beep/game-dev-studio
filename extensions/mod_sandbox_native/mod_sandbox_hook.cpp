/*
 * mod_sandbox_hook.cpp — Godot Mod 沙箱原生层
 * 通过 Win32 API Hook 拦截所有文件操作，实现底层路径重定向。
 * 
 * 编译（MinGW GCC）：
 *   g++ -shared -o mod_sandbox_hook.dll mod_sandbox_hook.cpp -static -static-libgcc -static-libstdc++ -O2
 *
 * 工作原理：
 *   1. C# ModSandbox 通过 P/Invoke 调用 sandbox_hook_init() 激活 Hook
 *   2. Hook 劫持 CreateFileW / DeleteFileW / CreateDirectoryW 等 Win32 API
 *   3. 每次文件操作前，检查路径是否需要重定向到沙箱目录
 *   4. 规则由 C# 端通过 sandbox_add_rule() 动态添加
 */

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <cstring>
#include <cstdio>

// ──────────────────── 共享状态 ────────────────────

static CRITICAL_SECTION g_Lock;
static bool g_Initialized = false;

// 沙箱模式
enum SandboxMode { MODE_OPEN = 0, MODE_STRICT = 1, MODE_ABSOLUTE = 2 };
static int g_Mode = MODE_STRICT;

// 白名单路径（前缀匹配，放行）
static wchar_t g_Whitelist[128][512];
static int g_WhitelistCount = 0;

// 重入保护标志（线程局部，防止 Hook 内部触发自身导致无限递归）
static thread_local bool g_InHook = false;

// user:// 目录的绝对路径（只拦截此路径下的操作）
static wchar_t g_UserRoot[512] = {0};
static size_t g_UserRootLen = 0;

// 线程局部当前 Mod 上下文（有值时说明正在执行 Mod 代码，游戏自己的操作为空直接放行）
static thread_local char g_CurrentMod[64] = {0};

// 每个 Mod 的沙箱目录
static wchar_t g_ModSandboxes[64][512];
static char g_ModIds[64][64];
static int g_ModCount = 0;

// ──────────────────── 路径工具 ────────────────────

static void ToLowerW(wchar_t* dst, const wchar_t* src) {
    int i = 0;
    while (src[i] && i < 511) { dst[i] = towlower(src[i]); i++; }
    dst[i] = 0;
}

static void NormalizeW(wchar_t* dst, const wchar_t* src) {
    int j = 0;
    for (int i = 0; src[i] && j < 1023; i++) {
        if (src[i] == L'/') dst[j++] = L'\\';
        else dst[j++] = src[i];
    }
    dst[j] = 0;
}

// 检查路径是否在白名单中
static bool IsWhitelisted(const wchar_t* path) {
    wchar_t lower[1024];
    ToLowerW(lower, path);
    for (int i = 0; i < g_WhitelistCount; i++) {
        if (wcscmp(lower, g_Whitelist[i]) == 0) return true;
        // 前缀匹配
        size_t wl = wcslen(g_Whitelist[i]);
        if (wl > 0 && wcsncmp(lower, g_Whitelist[i], wl) == 0) return true;
    }
    return false;
}

// 应用拦截规则
// 返回 true 表示需要阻止，output 存放重定向后的路径（如需重定向）
static bool ApplyBlock(const wchar_t* input, wchar_t* output, size_t outputSize) {
    if (g_Mode == MODE_OPEN) return false;

    // 非 user:// 路径一律放行
    if (g_UserRootLen == 0 || wcsncmp(input, g_UserRoot, g_UserRootLen) != 0)
        return false;

    // 没有当前 Mod 上下文 = 游戏自己的操作，直接放行
    if (g_CurrentMod[0] == 0) return false;

    // 严格模式：重定向到当前 Mod 的沙箱目录
    if (g_Mode == MODE_STRICT || g_Mode == MODE_ABSOLUTE) {
        // 在当前 Mod 的沙箱目录内 → 放行
        for (int i = 0; i < g_ModCount; i++) {
            if (strcmp(g_CurrentMod, g_ModIds[i]) == 0) {
                size_t sandboxLen = wcslen(g_ModSandboxes[i]);
                if (wcsncmp(input, g_ModSandboxes[i], sandboxLen) == 0)
                    return false; // 已在沙箱内，放行
                // 重定向到沙箱目录
                if (output && outputSize > sandboxLen + wcslen(input)) {
                    wcscpy(output, g_ModSandboxes[i]);
                    wcscat(output, L"\\");
                    // 去掉 user:// 前缀部分，保留相对路径
                    const wchar_t* relative = input + g_UserRootLen;
                    while (*relative == L'\\') relative++;
                    wcscat(output, relative);
                }
                return false; // 已重定向，不阻止
            }
        }
    }

    // 绝对严格模式且不在任何 Mod 沙箱内 → 阻止
    if (g_Mode == MODE_ABSOLUTE) return true;

    return false;
}

// ──────────────────── Hook 基础设施（x64 16字节对齐间接跳转） ────────────────────
// 16 字节对齐确保不会截断 x64 指令（多数 API 函数前 3 条指令共 15 字节）

static const int HOOK_SIZE = 16;

struct HookData {
    void* target;
    void* hook;
    BYTE  origBytes[16];
    void* trampoline;
    bool  installed;
};
static HookData g_Hooks[16];
static int g_HookCount = 0;

static bool InstallHook(void* targetFunc, void* hookFunc) {
    if (g_HookCount >= 16) return false;

    HookData& h = g_Hooks[g_HookCount++];
    h.target = targetFunc;
    h.hook = hookFunc;

    // x64: FF 25 00 00 00 00 [8-byte addr] 90 90  (16 bytes total)
    BYTE hookCode[16];
    hookCode[0] = 0xFF;
    hookCode[1] = 0x25;
    *(DWORD*)(hookCode + 2) = 0;
    *(void**)(hookCode + 6) = hookFunc;
    hookCode[14] = 0x90;  // NOP
    hookCode[15] = 0x90;  // NOP

    DWORD oldProt;
    if (!VirtualProtect(targetFunc, HOOK_SIZE, PAGE_EXECUTE_READWRITE, &oldProt))
        return false;

    memcpy(h.origBytes, targetFunc, HOOK_SIZE);
    memcpy(targetFunc, hookCode, HOOK_SIZE);

    VirtualProtect(targetFunc, HOOK_SIZE, oldProt, &oldProt);

    // 分配跳板（原指令 + 跳回）
    h.trampoline = VirtualAlloc(NULL, 48, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    if (!h.trampoline) return false;

    memcpy(h.trampoline, h.origBytes, HOOK_SIZE);
    BYTE* trampEnd = (BYTE*)h.trampoline + HOOK_SIZE;
    trampEnd[0] = 0xFF;
    trampEnd[1] = 0x25;
    *(DWORD*)(trampEnd + 2) = 0;
    *(void**)(trampEnd + 6) = (BYTE*)targetFunc + HOOK_SIZE;

    h.installed = true;
    return true;
}

static void UninstallAllHooks() {
    for (int i = 0; i < g_HookCount; i++) {
        if (!g_Hooks[i].installed) continue;
        DWORD oldProt;
        VirtualProtect(g_Hooks[i].target, HOOK_SIZE, PAGE_EXECUTE_READWRITE, &oldProt);
        memcpy(g_Hooks[i].target, g_Hooks[i].origBytes, HOOK_SIZE);
        VirtualProtect(g_Hooks[i].target, HOOK_SIZE, oldProt, &oldProt);
        VirtualFree(g_Hooks[i].trampoline, 0, MEM_RELEASE);
        g_Hooks[i].installed = false;
    }
    g_HookCount = 0;
}

// ──────────────────── 原始函数指针 ────────────────────

typedef HANDLE (WINAPI *CreateFileW_t)(LPCWSTR, DWORD, DWORD, LPSECURITY_ATTRIBUTES, DWORD, DWORD, HANDLE);
typedef BOOL   (WINAPI *DeleteFileW_t)(LPCWSTR);
typedef BOOL   (WINAPI *CreateDirectoryW_t)(LPCWSTR, LPSECURITY_ATTRIBUTES);
typedef BOOL   (WINAPI *RemoveDirectoryW_t)(LPCWSTR);
typedef HANDLE (WINAPI *FindFirstFileW_t)(LPCWSTR, LPWIN32_FIND_DATAW);

static CreateFileW_t       TrueCreateFileW = NULL;
static DeleteFileW_t       TrueDeleteFileW = NULL;
static CreateDirectoryW_t  TrueCreateDirectoryW = NULL;
static RemoveDirectoryW_t  TrueRemoveDirectoryW = NULL;
static FindFirstFileW_t    TrueFindFirstFileW = NULL;

// ──────────────────── Hook 函数实现 ────────────────────

HANDLE WINAPI HookCreateFileW(
    LPCWSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode,
    LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition,
    DWORD dwFlagsAndAttributes, HANDLE hTemplateFile)
{
    // 快速路径：非绝对严格模式 + 没有 Mod 在运行 → 零开销透传
    if (g_Mode != MODE_ABSOLUTE && g_CurrentMod[0] == 0)
        return TrueCreateFileW(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);

    if (!g_Initialized || g_InHook) return TrueCreateFileW(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
    g_InHook = true;

    if (lpFileName && wcslen(lpFileName) < 1000) {
        wchar_t redirected[1024] = {0};
        if (ApplyBlock(lpFileName, redirected, 1024)) {
            g_InHook = false;
            SetLastError(ERROR_ACCESS_DENIED);
            return INVALID_HANDLE_VALUE;
        }
        if (redirected[0] != 0) {
            HANDLE result = TrueCreateFileW(redirected, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
            g_InHook = false;
            return result;
        }
    }

    HANDLE result = TrueCreateFileW(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
    g_InHook = false;
    return result;
}

BOOL WINAPI HookDeleteFileW(LPCWSTR lpFileName) {
    if (g_Mode != MODE_ABSOLUTE && g_CurrentMod[0] == 0) return TrueDeleteFileW(lpFileName);
    if (!g_Initialized || !lpFileName || g_InHook) return TrueDeleteFileW(lpFileName);
    g_InHook = true;

    if (wcslen(lpFileName) < 1000) {
        wchar_t redirected[1024] = {0};
        if (ApplyBlock(lpFileName, redirected, 1024)) { g_InHook = false; SetLastError(ERROR_ACCESS_DENIED); return FALSE; }
        if (redirected[0] != 0) { BOOL ret = TrueDeleteFileW(redirected); g_InHook = false; return ret; }
    }

    BOOL ret = TrueDeleteFileW(lpFileName);
    g_InHook = false;
    return ret;
}

BOOL WINAPI HookCreateDirectoryW(LPCWSTR lpPathName, LPSECURITY_ATTRIBUTES lpSecurityAttributes) {
    if (g_Mode != MODE_ABSOLUTE && g_CurrentMod[0] == 0) return TrueCreateDirectoryW(lpPathName, lpSecurityAttributes);
    if (!g_Initialized || !lpPathName || g_InHook) return TrueCreateDirectoryW(lpPathName, lpSecurityAttributes);
    g_InHook = true;

    if (wcslen(lpPathName) < 1000) {
        wchar_t redirected[1024] = {0};
        if (ApplyBlock(lpPathName, redirected, 1024)) { g_InHook = false; SetLastError(ERROR_ACCESS_DENIED); return FALSE; }
        if (redirected[0] != 0) { BOOL ret = TrueCreateDirectoryW(redirected, lpSecurityAttributes); g_InHook = false; return ret; }
    }

    BOOL ret = TrueCreateDirectoryW(lpPathName, lpSecurityAttributes);
    g_InHook = false;
    return ret;
}

BOOL WINAPI HookRemoveDirectoryW(LPCWSTR lpPathName) {
    if (g_Mode != MODE_ABSOLUTE && g_CurrentMod[0] == 0) return TrueRemoveDirectoryW(lpPathName);
    if (!g_Initialized || !lpPathName || g_InHook) return TrueRemoveDirectoryW(lpPathName);
    g_InHook = true;

    if (wcslen(lpPathName) < 1000) {
        wchar_t redirected[1024] = {0};
        if (ApplyBlock(lpPathName, redirected, 1024)) { g_InHook = false; SetLastError(ERROR_ACCESS_DENIED); return FALSE; }
        if (redirected[0] != 0) { BOOL ret = TrueRemoveDirectoryW(redirected); g_InHook = false; return ret; }
    }

    BOOL ret = TrueRemoveDirectoryW(lpPathName);
    g_InHook = false;
    return ret;
}

// ──────────────────── C 导出接口（P/Invoke 调用） ────────────────────

extern "C" {

__declspec(dllexport) int sandbox_hook_init(int mode) {
    InitializeCriticalSection(&g_Lock);
    g_Mode = mode;
    g_Initialized = true;

    HMODULE k32 = GetModuleHandleW(L"kernel32.dll");
    if (!k32) return 0;

    TrueCreateFileW      = (CreateFileW_t)GetProcAddress(k32, "CreateFileW");
    TrueDeleteFileW       = (DeleteFileW_t)GetProcAddress(k32, "DeleteFileW");
    TrueCreateDirectoryW  = (CreateDirectoryW_t)GetProcAddress(k32, "CreateDirectoryW");
    TrueRemoveDirectoryW  = (RemoveDirectoryW_t)GetProcAddress(k32, "RemoveDirectoryW");
    TrueFindFirstFileW    = (FindFirstFileW_t)GetProcAddress(k32, "FindFirstFileW");

    if (!TrueCreateFileW) return 0;

    bool ok = true;
    ok &= InstallHook((void*)TrueCreateFileW, (void*)HookCreateFileW);
    ok &= InstallHook((void*)TrueDeleteFileW, (void*)HookDeleteFileW);
    ok &= InstallHook((void*)TrueCreateDirectoryW, (void*)HookCreateDirectoryW);
    ok &= InstallHook((void*)TrueRemoveDirectoryW, (void*)HookRemoveDirectoryW);

    return ok ? 7 : 0;
}

__declspec(dllexport) void sandbox_hook_shutdown() {
    UninstallAllHooks();
    EnterCriticalSection(&g_Lock);
    g_Initialized = false;
    LeaveCriticalSection(&g_Lock);
    DeleteCriticalSection(&g_Lock);
}

__declspec(dllexport) void sandbox_set_mode(int mode) {
    EnterCriticalSection(&g_Lock);
    g_Mode = mode;
    LeaveCriticalSection(&g_Lock);
}

__declspec(dllexport) void sandbox_set_user_root(const wchar_t* root) {
    EnterCriticalSection(&g_Lock);
    wcscpy(g_UserRoot, root);
    NormalizeW(g_UserRoot, root);
    g_UserRootLen = wcslen(g_UserRoot);
    LeaveCriticalSection(&g_Lock);
}

__declspec(dllexport) void sandbox_set_current_mod(const char* modId) {
    // 线程局部，无需加锁
    if (modId && modId[0]) {
        strncpy(g_CurrentMod, modId, 63);
        g_CurrentMod[63] = 0;
    } else {
        g_CurrentMod[0] = 0;
    }
}

__declspec(dllexport) void sandbox_register_mod(const char* modId, const wchar_t* sandboxDir) {
    EnterCriticalSection(&g_Lock);
    if (g_ModCount < 64) {
        strncpy(g_ModIds[g_ModCount], modId, 63);
        wcscpy(g_ModSandboxes[g_ModCount], sandboxDir);
        NormalizeW(g_ModSandboxes[g_ModCount], sandboxDir);
        g_ModCount++;
    }
    LeaveCriticalSection(&g_Lock);
}

__declspec(dllexport) void sandbox_unregister_mod(const char* modId) {
    EnterCriticalSection(&g_Lock);
    for (int i = 0; i < g_ModCount; i++) {
        if (strcmp(g_ModIds[i], modId) == 0) {
            g_ModCount--;
            if (i < g_ModCount) {
                memcpy(g_ModIds[i], g_ModIds[g_ModCount], 64);
                memcpy(g_ModSandboxes[i], g_ModSandboxes[g_ModCount], 512 * sizeof(wchar_t));
            }
            break;
        }
    }
    LeaveCriticalSection(&g_Lock);
}

__declspec(dllexport) void sandbox_clear_rules() { /* 兼容旧接口 */ }

__declspec(dllexport) void sandbox_add_whitelist(const wchar_t* path) {
    EnterCriticalSection(&g_Lock);
    if (g_WhitelistCount < 128) {
        wchar_t normalized[1024];
        NormalizeW(normalized, path);
        ToLowerW(g_Whitelist[g_WhitelistCount], normalized);
        g_WhitelistCount++;
    }
    LeaveCriticalSection(&g_Lock);
}

__declspec(dllexport) void sandbox_clear_whitelist() {
    EnterCriticalSection(&g_Lock);
    g_WhitelistCount = 0;
    LeaveCriticalSection(&g_Lock);
}

__declspec(dllexport) int sandbox_is_blocked(const wchar_t* path) {
    wchar_t dummy[1024];
    return ApplyBlock(path, dummy, 1024) ? 1 : 0;
}

} // extern "C"
