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

// 重定向规则：前缀匹配 → 替换前缀
struct RedirectRule {
    wchar_t match[512];
    wchar_t replace[512];
    size_t matchLen;
};
static RedirectRule g_Rules[64];
static int g_RuleCount = 0;

// 白名单路径（精确匹配，不重定向）
static wchar_t g_Whitelist[128][512];
static int g_WhitelistCount = 0;

// 当前重定向目标（线程局部，用于 CreateFileW 返回后让外部读取）
static thread_local wchar_t g_LastRedirected[1024] = {0};

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

// 应用重定向规则
static bool ApplyRedirect(wchar_t* output, const wchar_t* input) {
    if (g_Mode == MODE_OPEN) return false;

    wchar_t normalized[1024];
    NormalizeW(normalized, input);
    wchar_t lower[1024];
    ToLowerW(lower, normalized);

    // 先查白名单
    if (IsWhitelisted(lower)) return false;

    // 检查重定向规则
    for (int i = 0; i < g_RuleCount; i++) {
        if (g_RuleCount > 0 && i < g_RuleCount && wcsncmp(lower, g_Rules[i].match, g_Rules[i].matchLen) == 0) {
            wcscpy(output, g_Rules[i].replace);
            wcscat(output, normalized + g_Rules[i].matchLen);
            return true;
        }
    }

    return false;
}

// ──────────────────── Hook 基础设施（x64 14字节间接跳转） ────────────────────

struct HookData {
    void* target;
    void* hook;
    BYTE  origBytes[14];
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

    // x64: FF 25 00 00 00 00 xx xx xx xx xx xx xx xx
    //       jmp [rip+0]   followed by absolute address
    BYTE hookCode[14];
    hookCode[0] = 0xFF;
    hookCode[1] = 0x25;
    *(DWORD*)(hookCode + 2) = 0;
    *(void**)(hookCode + 6) = hookFunc;

    DWORD oldProt;
    if (!VirtualProtect(targetFunc, 14, PAGE_EXECUTE_READWRITE, &oldProt))
        return false;

    memcpy(h.origBytes, targetFunc, 14);
    memcpy(targetFunc, hookCode, 14);

    VirtualProtect(targetFunc, 14, oldProt, &oldProt);

    // 分配跳板（原指令 + 跳回）
    h.trampoline = VirtualAlloc(NULL, 32, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    if (!h.trampoline) return false;

    memcpy(h.trampoline, h.origBytes, 14);
    BYTE* trampEnd = (BYTE*)h.trampoline + 14;
    trampEnd[0] = 0xFF;
    trampEnd[1] = 0x25;
    *(DWORD*)(trampEnd + 2) = 0;
    *(void**)(trampEnd + 6) = (BYTE*)targetFunc + 14;

    h.installed = true;
    return true;
}

static void UninstallAllHooks() {
    for (int i = 0; i < g_HookCount; i++) {
        if (!g_Hooks[i].installed) continue;
        DWORD oldProt;
        VirtualProtect(g_Hooks[i].target, 14, PAGE_EXECUTE_READWRITE, &oldProt);
        memcpy(g_Hooks[i].target, g_Hooks[i].origBytes, 14);
        VirtualProtect(g_Hooks[i].target, 14, oldProt, &oldProt);
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

static bool ShouldBlock(const wchar_t* path) {
    if (g_Mode != MODE_ABSOLUTE) return false;
    // 绝对严格模式：只允许沙箱目录和系统目录
    if (wcsstr(path, L"mods_sandbox")) return false;
    if (wcsstr(path, L"Windows") || wcsstr(path, L"Program Files")) return false;
    wchar_t lower[1024];
    ToLowerW(lower, path);
    return !IsWhitelisted(lower);
}

HANDLE WINAPI HookCreateFileW(
    LPCWSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode,
    LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition,
    DWORD dwFlagsAndAttributes, HANDLE hTemplateFile)
{
    if (!g_Initialized) return TrueCreateFileW(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);

    wchar_t redirected[1024] = {0};
    const wchar_t* usePath = lpFileName;

    if (lpFileName && wcslen(lpFileName) < 1000) {
        if (ShouldBlock(lpFileName)) {
            SetLastError(ERROR_ACCESS_DENIED);
            return INVALID_HANDLE_VALUE;
        }

        if (ApplyRedirect(redirected, lpFileName)) {
            // 确保目标目录存在
            if (dwCreationDisposition == CREATE_NEW || dwCreationDisposition == CREATE_ALWAYS || dwCreationDisposition == OPEN_ALWAYS) {
                wchar_t dirPath[1024];
                wcscpy(dirPath, redirected);
                for (int i = wcslen(dirPath) - 1; i > 0; i--) {
                    if (dirPath[i] == L'\\') {
                        dirPath[i] = 0;
                        if (GetFileAttributesW(dirPath) == INVALID_FILE_ATTRIBUTES)
                            CreateDirectoryW(dirPath, NULL);
                        dirPath[i] = L'\\';
                        break;
                    }
                }
            }
            usePath = redirected;
            wcscpy(g_LastRedirected, redirected);
        }
    }

    return TrueCreateFileW(usePath, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
}

BOOL WINAPI HookDeleteFileW(LPCWSTR lpFileName) {
    if (!g_Initialized || !lpFileName) return TrueDeleteFileW(lpFileName);

    if (wcslen(lpFileName) < 1000 && ShouldBlock(lpFileName)) {
        SetLastError(ERROR_ACCESS_DENIED);
        return FALSE;
    }

    wchar_t redirected[1024] = {0};
    const wchar_t* usePath = ApplyRedirect(redirected, lpFileName) ? redirected : lpFileName;
    return TrueDeleteFileW(usePath);
}

BOOL WINAPI HookCreateDirectoryW(LPCWSTR lpPathName, LPSECURITY_ATTRIBUTES lpSecurityAttributes) {
    if (!g_Initialized || !lpPathName) return TrueCreateDirectoryW(lpPathName, lpSecurityAttributes);

    if (wcslen(lpPathName) < 1000 && ShouldBlock(lpPathName)) {
        SetLastError(ERROR_ACCESS_DENIED);
        return FALSE;
    }

    wchar_t redirected[1024] = {0};
    const wchar_t* usePath = ApplyRedirect(redirected, lpPathName) ? redirected : lpPathName;
    return TrueCreateDirectoryW(usePath, lpSecurityAttributes);
}

BOOL WINAPI HookRemoveDirectoryW(LPCWSTR lpPathName) {
    if (!g_Initialized || !lpPathName) return TrueRemoveDirectoryW(lpPathName);

    if (wcslen(lpPathName) < 1000 && ShouldBlock(lpPathName)) {
        SetLastError(ERROR_ACCESS_DENIED);
        return FALSE;
    }

    wchar_t redirected[1024] = {0};
    const wchar_t* usePath = ApplyRedirect(redirected, lpPathName) ? redirected : lpPathName;
    return TrueRemoveDirectoryW(usePath);
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

__declspec(dllexport) void sandbox_add_rule(const wchar_t* match, const wchar_t* replace) {
    EnterCriticalSection(&g_Lock);
    if (g_RuleCount < 64) {
        wcscpy(g_Rules[g_RuleCount].match, match);
        wcscpy(g_Rules[g_RuleCount].replace, replace);
        wcscpy(g_Rules[g_RuleCount].match, match);
        NormalizeW(g_Rules[g_RuleCount].match, match);
        ToLowerW(g_Rules[g_RuleCount].match, g_Rules[g_RuleCount].match);
        g_Rules[g_RuleCount].matchLen = wcslen(g_Rules[g_RuleCount].match);
        NormalizeW(g_Rules[g_RuleCount].replace, replace);
        g_RuleCount++;
    }
    LeaveCriticalSection(&g_Lock);
}

__declspec(dllexport) void sandbox_clear_rules() {
    EnterCriticalSection(&g_Lock);
    g_RuleCount = 0;
    LeaveCriticalSection(&g_Lock);
}

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

__declspec(dllexport) const wchar_t* sandbox_last_redirected() {
    return g_LastRedirected;
}

__declspec(dllexport) int sandbox_is_blocked(const wchar_t* path) {
    return ShouldBlock(path) ? 1 : 0;
}

} // extern "C"
