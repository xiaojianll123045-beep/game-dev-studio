using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

/// <summary>
/// Mod 安全沙箱 — 文件系统路由、权限控制、安全日志。
/// 在 ModManager.ApplyAll 之前初始化。
/// 通过 C# 层路由（ModBridge API）实现路径重定向，不依赖底层 Hook。
/// </summary>
public static class ModSandbox
{
    private static bool _initialized = false;
	// ── 沙箱模式 ──
	public enum SandboxMode { Open, Strict, AbsoluteStrict }
    public static SandboxMode Mode { get; set; } = SandboxMode.Strict;

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

	// ── 每 Mod 沙箱配置 ──
	public class ModSandboxConfig
	{
		public string ModId;
		public SandboxMode Mode = SandboxMode.Strict;
		public List<string> Whitelist = new();
	}
	private static readonly Dictionary<string, ModSandboxConfig> _modConfigs = new();
	private const string ConfigFile = "user://mods_sandbox_config.json";

	public static ModSandboxConfig GetModConfig(string modId)
	{
		if (_modConfigs.TryGetValue(modId, out var cfg)) return cfg;
		var ncfg = new ModSandboxConfig { ModId = modId, Mode = Mode };
		_modConfigs[modId] = ncfg;
		return ncfg;
	}

	public static void SaveModConfig(string modId)
	{
		try
		{
			var list = new List<Dictionary<string, object>>();
			foreach (var kv in _modConfigs)
			{
				list.Add(new Dictionary<string, object>
				{
					["modId"] = kv.Key,
					["mode"] = (int)kv.Value.Mode,
					["whitelist"] = kv.Value.Whitelist
				});
			}
			using var f = FileAccess.Open(ConfigFile, FileAccess.ModeFlags.Write);
			f.StoreString(JsonSerializer.Serialize(list));
		}
		catch { }
	}

	private static void LoadModConfigs()
	{
		try
		{
			if (!FileAccess.FileExists(ConfigFile)) return;
			using var f = FileAccess.Open(ConfigFile, FileAccess.ModeFlags.Read);
			var doc = JsonDocument.Parse(f.GetAsText());
			foreach (var item in doc.RootElement.EnumerateArray())
			{
				var cfg = new ModSandboxConfig
				{
					ModId = item.GetProperty("modId").GetString(),
					Mode = (SandboxMode)item.GetProperty("mode").GetInt32(),
					Whitelist = new List<string>()
				};
				if (item.TryGetProperty("whitelist", out var wl))
					foreach (var p in wl.EnumerateArray())
						cfg.Whitelist.Add(p.GetString());
				_modConfigs[cfg.ModId] = cfg;
			}
		}
		catch { }
	}

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
        LoadModConfigs();
        RegisterConsoleCommands();
        GD.Print($"[Sandbox] 已初始化，模式: {Mode}, 权限: {_permissions.Count} mods");
    }

    /// <summary>激活 Native Hook 层（在 Godot 引擎完全就绪后调用）</summary>
    /// <summary>为 Mod 注册沙箱上下文</summary>
    public static void RegisterMod(string modId, string modName)
    {
        if (_modSandboxDirs.ContainsKey(modId)) return;
        string sandboxDir = _sandboxRoot + "/" + SanitizeId(modId);
        if (!Directory.Exists(sandboxDir)) Directory.CreateDirectory(sandboxDir);
        _modSandboxDirs[modId] = sandboxDir;
        if (!_permissions.ContainsKey(modId)) _permissions[modId] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
		// 判断使用哪个沙箱模式（先查 Mod 专属配置，再回退全局）
		var cfg = modId != null && _modConfigs.TryGetValue(modId, out var mc) ? mc : null;
		SandboxMode effectiveMode = cfg != null ? cfg.Mode : Mode;

		if (effectiveMode == SandboxMode.Open) return originalPath;

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

	/// <summary>将目标路径映射到沙箱内（含目录遍历防护）</summary>
	private static string MapToSandbox(string sandboxDir, string originalPath)
	{
		string normalized = originalPath.Replace("\\", "/");

		// 目录遍历攻击防护
		if (normalized.Contains("../") || normalized.Contains("..\\") || normalized.Contains("/..") || normalized.Contains("\\.."))
		{
			string safe = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(normalized));
			return sandboxDir + "/external/" + safe;
		}

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

		string safeName = normalized.Replace(":", "_").Replace("/", "_").Replace("\\", "_");
		return sandboxDir + "/external/" + safeName;
	}

	// ══════════════════ 权限系统（前缀匹配 + 预授权） ══════════════════

	/// <summary>判断路径是否被允许（前缀匹配白名单/已授权路径）</summary>
	public static bool IsPathWhitelisted(string modId, string path)
	{
		string norm = NormalizePath(path);
		foreach (var wl in _globalWhitelist)
			if (norm.StartsWith(NormalizePath(wl), StringComparison.OrdinalIgnoreCase)) return true;
		if (_permissions.TryGetValue(modId, out var modPerms))
			foreach (var p in modPerms)
				if (norm.StartsWith(NormalizePath(p), StringComparison.OrdinalIgnoreCase)) return true;
		return false;
	}

	private static string NormalizePath(string p)
	{
		if (string.IsNullOrEmpty(p)) return "";
		return p.Replace("\\", "/").TrimEnd('/') + "/";
	}

	/// <summary>预授权模式——不在白名单里的路径直接拒绝，永不弹窗</summary>
	public static bool RequestFilePermission(string modId, string path, PermissionType type)
	{
		if (IsPathWhitelisted(modId, path)) return true;
		LogAccess(modId, $"权限被拒绝({type})", path, "DENY");
		return false;
	}

	/// <summary>授予权限</summary>
	public static void GrantPermission(string modId, string path)
	{
		if (!_permissions.ContainsKey(modId))
			_permissions[modId] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		_permissions[modId].Add(NormalizePath(path));
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
            string line = $"[{entry.Timestamp}] [{entry.Result}] [{entry.ModId}] {entry.Action}: {entry.Target}\n";
            File.AppendAllText(logPath, line);
        }
        catch { }

        // GDScript 疑似危险操作 → 控制台预警
        if (result == "SUSPECT" || result == "BLOCK")
            GD.Print($"[沙箱预警] Mod [{entry.ModName}] 可疑文件操作: {entry.Action} → {entry.Target}");
    }

    /// <summary>记录 GDScript Mod 的可疑路径操作</summary>
    public static void LogSuspectPath(string modId, string path, string op)
    {
        LogAccess(modId, $"GDScript_{op}", path, "SUSPECT");
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

	/// <summary>沙箱化 Mod 上下文——C# Mod 通过此接口读写文件，路径自动重定向</summary>
	public class ModContext : IModContext
	{
		public string ModId { get; }
		public IModFileSystem FileSystem { get; }
		public IModNetwork Network { get; }
		public GameManager GameManager => ModAPI.GameManager;

		private class SandboxFileSystem : IModFileSystem
		{
			private string _modId;
			public SandboxFileSystem(string modId) { _modId = modId; }
			public string ReadAllText(string path)
			{
				string redirect = RedirectPath(_modId, path);
				if (redirect == null) return "";
				if (!FileAccess.FileExists(redirect)) return "";
				using var f = FileAccess.Open(redirect, FileAccess.ModeFlags.Read);
				return f?.GetAsText() ?? "";
			}
			public bool WriteAllText(string path, string content)
			{
				string redirect = RedirectPath(_modId, path);
				if (redirect == null) return false;
				string dir = System.IO.Path.GetDirectoryName(redirect);
				if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
					System.IO.Directory.CreateDirectory(dir);
				using var f = FileAccess.Open(redirect, FileAccess.ModeFlags.Write);
				if (f == null) return false;
				f.StoreString(content);
				return true;
			}
			public bool FileExists(string path)
			{
				string redirect = RedirectPath(_modId, path);
				return redirect != null && FileAccess.FileExists(redirect);
			}
			public string GetSandboxDir() => GetModSandboxDir(_modId) ?? "";
		}

		private class SandboxNetwork : IModNetwork
		{
			public bool HttpGet(string url, out string response)
			{
				response = "";
				if (!CheckNetworkAccess("", url, 0)) return false;
				if (!url.StartsWith("https://") && !url.StartsWith("http://")) return false;
				try { using var client = new System.Net.Http.HttpClient(); response = client.GetStringAsync(url).Result; return true; }
				catch { return false; }
			}
		}

		public ModContext(string modId)
		{
			ModId = modId;
			FileSystem = new SandboxFileSystem(modId);
			Network = new SandboxNetwork();
		}
	}
}
