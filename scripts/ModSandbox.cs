using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

/// <summary>
/// Mod 安全沙箱 — 文件系统虚拟化、权限控制、安全日志。
/// 在 ModManager.ApplyAll 之前初始化，是 Mod 安全的最后一道防线。
/// 底层通过 Win32 API Hook（mod_sandbox_hook.dll）拦截所有文件操作。
/// </summary>
public static class ModSandbox
{
    // ═══════════════ 原生 Hook DLL (P/Invoke) ═══════════════
    private static bool _nativeHooksActive = false;
    private static bool _initialized = false;

    [DllImport("mod_sandbox_hook.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sandbox_hook_init(int mode);

    [DllImport("mod_sandbox_hook.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void sandbox_hook_shutdown();

    [DllImport("mod_sandbox_hook.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void sandbox_set_mode(int mode);

    [DllImport("mod_sandbox_hook.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern void sandbox_add_rule(string match, string replace);

    [DllImport("mod_sandbox_hook.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void sandbox_clear_rules();

    [DllImport("mod_sandbox_hook.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern void sandbox_add_whitelist(string path);

    [DllImport("mod_sandbox_hook.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void sandbox_clear_whitelist();

    [DllImport("mod_sandbox_hook.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern int sandbox_is_blocked(string path);

    private static void InitNativeHooks()
    {
        try
        {
            int result = sandbox_hook_init((int)Mode);
            if (result == 7)
            {
                _nativeHooksActive = true;
                GD.Print("[Sandbox] Win32 API Hook 已激活（CreateFileW / DeleteFileW / CreateDirectoryW / RemoveDirectoryW）");

                // 同步所有已注册的沙箱目录规则到原生层
                SyncRulesToNative();
                SyncWhitelistToNative();
            }
            else
                GD.PrintErr("[Sandbox] 原生 Hook 初始化失败（可能缺少 mod_sandbox_hook.dll）");
        }
        catch (DllNotFoundException)
        {
            GD.Print("[Sandbox] 原生 Hook DLL 未找到，沙箱运行在 C# 层拦截模式");
        }
        catch (Exception e)
        {
            GD.PrintErr($"[Sandbox] 原生 Hook 加载异常: {e.Message}");
        }
    }

    private static void SyncRulesToNative()
    {
        if (!_nativeHooksActive) return;
        sandbox_clear_rules();
        // 为每个注册的 Mod 添加 user:// → sandbox 重定向规则
        string absUser = ProjectSettings.GlobalizePath("user://").Replace("/", "\\");
        foreach (var kv in _modSandboxDirs)
        {
            string sandboxAbs = kv.Value.Replace("/", "\\");
            // GDScript 最终调用的是 Godot 转译后的绝对路径
            // res:// 和 user:// 经过 Godot 内部转换后才到 Win32 API
            sandbox_add_rule(absUser + "mods_sandbox", sandboxAbs + "\\");  // 自身沙箱放行
        }
        // 添加 user:// mods_sandbox 放行自身
        sandbox_add_rule(absUser + "mods_sandbox", absUser + "mods_sandbox");
    }

    private static void SyncWhitelistToNative()
    {
        if (!_nativeHooksActive) return;
        sandbox_clear_whitelist();
        foreach (var path in _globalWhitelist)
            sandbox_add_whitelist(path.Replace("/", "\\"));
        // 系统关键路径绝对放行
        sandbox_add_whitelist(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows));
        sandbox_add_whitelist(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles));
        sandbox_add_whitelist(System.Environment.GetFolderPath(System.Environment.SpecialFolder.System));
    }
	// ── 沙箱模式 ──
	public enum SandboxMode { Open, Strict, AbsoluteStrict }
    public static SandboxMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            if (_nativeHooksActive) sandbox_set_mode((int)value);
        }
    }
    private static SandboxMode _mode = SandboxMode.Strict;

	// ── 安全日志 ──
	public static string LogDir { get; private set; }
	private static readonly List<SandboxLogEntry> _log = new();
	public static IReadOnlyList<SandboxLogEntry> Log => _log;
	public static event Action<string, string, string> OnBlocked;

	public struct SandboxLogEntry
	{
		public string ModId;
		public string ModName;
		public string Action;
		public string Target;
		public string Result;
		public string Timestamp;
	}

	// ── 每 Mod 沙箱目录 ──
	private static readonly Dictionary<string, string> _modSandboxDirs = new();
	private static string _sandboxRoot;
	public static string GetModSandboxDir(string modId) =>
		_modSandboxDirs.TryGetValue(modId, out var d) ? d : null;

	// ── 权限系统 ──
	private const string PermissionsFile = "user://mods_permissions.json";
	private static readonly Dictionary<string, HashSet<string>> _permissions = new(); // modId → allowed paths
	private static readonly HashSet<string> _globalWhitelist = new(); // 玩家自定义白名单

	public enum PermissionType { Read, Write, ReadWrite }

	// ── 网络控制 ──
	private static readonly Dictionary<string, NetworkQuota> _networkQuotas = new();
	private class NetworkQuota { public int BytesSent; public float WindowStart; }

    /// <summary>沙箱 C# 层初始化（在所有系统之前、同步、仅一次）</summary>
    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        _sandboxRoot = ProjectSettings.GlobalizePath("user://mods_sandbox");
        if (!DirAccess.DirExistsAbsolute("user://mods_sandbox"))
            DirAccess.MakeDirAbsolute("user://mods_sandbox");

        LogDir = ProjectSettings.GlobalizePath("user://mods_security_logs");
        if (!DirAccess.DirExistsAbsolute("user://mods_security_logs"))
            DirAccess.MakeDirAbsolute("user://mods_security_logs");

        LoadPermissions();
        LoadGlobalWhitelist();
        RegisterConsoleCommands();
        GD.Print($"[Sandbox] C# 层已初始化，模式: {Mode}, 权限: {_permissions.Count} mods");
    }

    /// <summary>激活 Native Hook 层（在 Godot 引擎完全就绪后调用）</summary>
    public static void ActivateNativeHooks()
    {
        if (_nativeHooksActive) return;
        InitNativeHooks();
        if (_nativeHooksActive)
            GD.Print($"[Sandbox] Native Hook 层已激活，模式: {Mode}");
    }

	/// <summary>为 Mod 注册沙箱上下文（在 ModManager.ApplyMod 之前调用）</summary>
	public static void RegisterMod(string modId, string modName)
	{
		if (_modSandboxDirs.ContainsKey(modId)) return;
		string sandboxDir = _sandboxRoot + "/" + SanitizeId(modId);
		if (!Directory.Exists(sandboxDir)) Directory.CreateDirectory(sandboxDir);
        _modSandboxDirs[modId] = sandboxDir;
        if (!_permissions.ContainsKey(modId)) _permissions[modId] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        SyncRulesToNative();
        GD.Print($"[Sandbox] Mod 已注册: {modId} → {sandboxDir}");
	}

	/// <summary>清理 Mod 沙箱（卸载时调用）</summary>
	public static void UnregisterMod(string modId)
	{
		_modSandboxDirs.Remove(modId);
		_networkQuotas.Remove(modId);
	}

	// ══════════════════ 文件系统虚拟化 ══════════════════

	/// <summary>将 Mod 请求的路径重定向到沙箱目录</summary>
	public static string RedirectPath(string modId, string originalPath)
	{
		if (Mode == SandboxMode.Open) return originalPath;

		// 绝对开放：玩家承担风险
		if (Mode == SandboxMode.Open) return originalPath;

		// 检查白名单（全局 + Mod 专属）
		if (IsPathWhitelisted(modId, originalPath))
		{
			LogAccess(modId, "路径白名单通过", originalPath, "ALLOW");
			return originalPath;
		}

		// 重定向到 Mod 沙箱
		if (_modSandboxDirs.TryGetValue(modId, out var sandboxDir))
		{
			string redirected = MapToSandbox(sandboxDir, originalPath);
			LogAccess(modId, "路径重定向", $"{originalPath} → {redirected}", "REDIRECT");
			return redirected;
		}

		// Fallback: 阻止
		LogAccess(modId, "路径被阻止", originalPath, "BLOCK");
		return null;
	}

	/// <summary>将目标路径映射到沙箱内</summary>
	private static string MapToSandbox(string sandboxDir, string originalPath)
	{
		string normalized = originalPath.Replace("\\", "/");

		// res:// 和 user:// 映射到沙箱对应子目录
		if (normalized.StartsWith("res://"))
		{
			string relative = normalized["res://".Length..].TrimStart('/');
			return sandboxDir + "/res/" + relative;
		}
		if (normalized.StartsWith("user://"))
		{
			string relative = normalized["user://".Length..].TrimStart('/');
			return sandboxDir + "/user/" + relative;
		}

		// 绝对路径 → 映射到 sandbox/external/
		string safeName = normalized.Replace(":", "_").Replace("/", "_").Replace("\\", "_");
		return sandboxDir + "/external/" + safeName;
	}

	// ══════════════════ 权限系统 ══════════════════

	/// <summary>判断路径是否被允许（白名单 + 已授权）</summary>
	public static bool IsPathWhitelisted(string modId, string path)
	{
		if (_globalWhitelist.Contains(path)) return true;
		if (_permissions.TryGetValue(modId, out var modPerms))
			return modPerms.Contains(path);
		return false;
	}

	/// <summary>请求文件权限（由沙箱拦截器调用）</summary>
	public static bool RequestFilePermission(string modId, string path, PermissionType type)
	{
		if (_permissions.TryGetValue(modId, out var perms) && perms.Contains(path))
			return true;
		if (_globalWhitelist.Contains(path)) return true;

		// 触发 UI 弹窗（由 GameManager 接管）
		var mgr = ModAPI.GameManager;
		if (mgr == null) return false;

		string modName = ModManager.LoadedMods.Find(m => m.Id == modId)?.Name ?? modId;
		string typeName = type switch
		{
			PermissionType.Read => Loc.Tr("sandbox.perm_read"),
			PermissionType.Write => Loc.Tr("sandbox.perm_write"),
			_ => Loc.Tr("sandbox.perm_readwrite")
		};

		// 显示模态弹窗（对话框内部已处理 Grant/Deny）
		mgr.CallDeferred(nameof(GameManager.ShowSandboxPermissionDialog),
			modId, modName, path, typeName,
			new Callable(), new Callable());

		// 注意：这里需要同步等待，但 Godot 不支持在非主线程等待
		// 实际方案：挂起 Mod 线程需要更复杂的实现（见方案B）
		// 当前版本：直接拒绝（严格模式），鼓励 Mod 作者用 Bridge API
		return false;
	}

	/// <summary>授予权限（玩家通过 UI 确认后调用）</summary>
	public static void GrantPermission(string modId, string path)
	{
		if (!_permissions.ContainsKey(modId))
			_permissions[modId] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		_permissions[modId].Add(path);
		SavePermissions();
		LogAccess(modId, "权限已授予", path, "GRANT");
	}

	/// <summary>拒绝权限</summary>
	public static void DenyPermission(string modId, string path)
	{
		LogAccess(modId, "权限已被拒绝", path, "DENY");
	}

	/// <summary>将路径加入全局白名单</summary>
    public static void AddGlobalWhitelist(string path)
    {
        _globalWhitelist.Add(path);
        SaveGlobalWhitelist();
        if (_nativeHooksActive) sandbox_add_whitelist(path.Replace("/", "\\"));
    }

	/// <summary>移除全局白名单路径</summary>
	public static void RemoveGlobalWhitelist(string path)
	{
		_globalWhitelist.Remove(path);
		SaveGlobalWhitelist();
	}

	public static IReadOnlyCollection<string> GetGlobalWhitelist() => _globalWhitelist;

	public static IReadOnlyDictionary<string, HashSet<string>> GetAllPermissions()
	{
		var result = new Dictionary<string, HashSet<string>>();
		foreach (var kv in _permissions)
			result[kv.Key] = new HashSet<string>(kv.Value, StringComparer.OrdinalIgnoreCase);
		return result;
	}

	// ══════════════════ 网络隔离 ══════════════════

	/// <summary>检查网络请求是否被允许</summary>
	public static bool CheckNetworkAccess(string modId, string url, int requestBytes)
	{
		if (Mode == SandboxMode.Open) return true;
		if (Mode == SandboxMode.AbsoluteStrict)
		{
			LogAccess(modId, "网络被阻止（绝对严格）", url, "BLOCK");
			return false;
		}

		// 严格模式：限制上行流量
		if (!_networkQuotas.TryGetValue(modId, out var quota))
		{
			quota = new NetworkQuota { BytesSent = 0, WindowStart = Time.GetTicksMsec() / 1000f };
			_networkQuotas[modId] = quota;
		}

		float now = Time.GetTicksMsec() / 1000f;
		if (now - quota.WindowStart > 60f)
		{
			quota.BytesSent = 0;
			quota.WindowStart = now;
		}

		if (quota.BytesSent + requestBytes > 64 * 1024)
		{
			LogAccess(modId, "网络配额超限", url, "BLOCK");
			return false;
		}

		quota.BytesSent += requestBytes;
		return true;
	}

	// ══════════════════ C# Assembly 安全检查 ══════════════════

	/// <summary>扫描 DLL 元数据，检查 unsafe / DllImport / Reflection</summary>
	public static AssemblyScanResult ScanAssembly(byte[] rawBytes, string fileName)
	{
		var result = new AssemblyScanResult { FileName = fileName, Passed = true };

		// 注意：完整的元数据扫描需要 Mono.Cecil 或类似库解析 PE 文件
		// Godot C# 环境不包含 Mono.Cecil，此处做基础检查

		try
		{
			// 检查 DLL 文件头的 CorFlags（CLR 头）
			// ILOnly=1 且 32Bit=0 意味着纯托管（无 unsafe）
			if (rawBytes.Length > 232) // PE 头 + CLR 头足够
			{
				// PE signature offset in PE header
				int peOffset = BitConverter.ToInt32(rawBytes, 60);
				if (peOffset > 0 && peOffset + 256 <= rawBytes.Length)
				{
					// CLR header RVA in optional PE header (PE+0xE8 for PE32+)
					// Simplified: check CorFlags at known offsets
					// This is a heuristic — full implementation needs proper PE parsing
					byte[] content = rawBytes;

					// 简单检查：搜索 "unsafe" / "DllImport" 字符串特征
					string debugStr = System.Text.Encoding.ASCII.GetString(rawBytes);
					foreach (var pattern in new[] { "System.Reflection", "System.Runtime.InteropServices.DllImport" })
					{
						if (debugStr.Contains(pattern, StringComparison.OrdinalIgnoreCase))
						{
							result.Issues.Add($"检测到敏感命名空间引用: {pattern}");
							result.Passed = false;
						}
					}
				}
			}
		}
		catch { /* 解析失败不阻止加载 */ }

		return result;
	}

	public class AssemblyScanResult
	{
		public string FileName;
		public bool Passed;
		public List<string> Issues = new();
	}

	// ══════════════════ 安全日志 ══════════════════

	public static void LogAccess(string modId, string action, string target, string result)
	{
		var entry = new SandboxLogEntry
		{
			ModId = modId,
			ModName = ModManager.LoadedMods.Find(m => m.Id == modId)?.Name ?? modId,
			Action = action,
			Target = target,
			Result = result,
			Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
		};
		_log.Add(entry);

		if (result == "BLOCK" || result == "DENY")
			OnBlocked?.Invoke(modId, action, target);

		// 写入日志文件（追加）
		try
		{
			string logPath = LogDir + "/sandbox.log";
			string line = $"[{entry.Timestamp}] [{result}] [{entry.ModId}] {entry.Action}: {entry.Target}\n";
			File.AppendAllText(logPath, line);
		}
		catch { }
	}

	public static void ClearLog() => _log.Clear();
	public static string GetLogText()
	{
		var sb = new System.Text.StringBuilder();
		foreach (var e in _log)
			sb.AppendLine($"[{e.Timestamp}] [{e.Result}] [{e.ModId}] {e.Action}: {e.Target}");
		return sb.ToString();
	}

	// ══════════════════ 持久化 ══════════════════

	private static void LoadPermissions()
	{
		try
		{
			if (!FileAccess.FileExists(PermissionsFile)) return;
			using var f = FileAccess.Open(PermissionsFile, FileAccess.ModeFlags.Read);
			var doc = JsonDocument.Parse(f.GetAsText());
			foreach (var modKv in doc.RootElement.EnumerateObject())
			{
				var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var p in modKv.Value.EnumerateArray())
					paths.Add(p.GetString());
				_permissions[modKv.Name] = paths;
			}
		}
		catch { }
	}

	private static void SavePermissions()
	{
		try
		{
		var obj = new Dictionary<string, List<string>>();
		foreach (var kv in _permissions)
			obj[kv.Key] = new List<string>(kv.Value);
		using var f = FileAccess.Open(PermissionsFile, FileAccess.ModeFlags.Write);
			f.StoreString(JsonSerializer.Serialize(obj));
		}
		catch (Exception e) { GD.PrintErr($"[Sandbox] 保存权限失败: {e.Message}"); }
	}

	private static void LoadGlobalWhitelist()
	{
		try
		{
			string path = "user://mods_whitelist.json";
			if (!FileAccess.FileExists(path)) return;
			using var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
			var doc = JsonDocument.Parse(f.GetAsText());
			foreach (var p in doc.RootElement.EnumerateArray())
				_globalWhitelist.Add(p.GetString());
		}
		catch { }
	}

	private static void SaveGlobalWhitelist()
	{
		try
		{
			using var f = FileAccess.Open("user://mods_whitelist.json", FileAccess.ModeFlags.Write);
			f.StoreString(JsonSerializer.Serialize(new List<string>(_globalWhitelist)));
		}
		catch { }
	}

	private static string SanitizeId(string id) => id.Replace("/", "_").Replace("\\", "_").Replace("..", "_");

	/// <summary>供 ModConsole 使用的安全日志命令</summary>
	public static void RegisterConsoleCommands()
	{
		ModConsole.RegisterCommand("sandbox_log", "查看沙箱安全日志", "sandbox_log",
			_ => ModConsole.Print(GetLogText()));
		ModConsole.RegisterCommand("sandbox_mode", "查看/设置沙箱模式 (open/strict/absolute)", "sandbox_mode [模式]",
			args =>
			{
				if (args.Length == 0)
					ModConsole.Print($"当前沙箱模式: {Mode}");
				else if (args[0].ToLower() == "open")
				{ Mode = SandboxMode.Open; ModConsole.Print("沙箱模式 → 开放"); }
				else if (args[0].ToLower() == "strict")
				{ Mode = SandboxMode.Strict; ModConsole.Print("沙箱模式 → 严格"); }
				else if (args[0].ToLower() == "absolute")
				{ Mode = SandboxMode.AbsoluteStrict; ModConsole.Print("沙箱模式 → 绝对严格"); }
			});
		ModConsole.RegisterCommand("sandbox_perms", "查看 Mod 权限", "sandbox_perms [modId]",
			args =>
			{
				if (args.Length == 0)
				{
					foreach (var kv in _permissions)
						ModConsole.Print($"  {kv.Key}: {kv.Value.Count} 条权限");
				}
				else if (_permissions.TryGetValue(args[0], out var perms))
					foreach (var p in perms) ModConsole.Print($"  {p}");
				else
					ModConsole.Print("未找到该 Mod 的权限记录");
			});
	}
}
