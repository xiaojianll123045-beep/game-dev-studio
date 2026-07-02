using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

/// <summary>
/// Mod 管理系统。支持语言 / 数据 / 脚本三类 Mod。
/// </summary>
public static class ModManager
{
    /// <summary>已加载的所有 Mod 清单</summary>
    public static List<ModManifest> LoadedMods { get; } = new();

    /// <summary>启用状态（持久化）</summary>
    public static HashSet<string> EnabledMods { get; } = new();

    /// <summary>加载错误信息集合，供 UI 显示</summary>
    public static List<string> LoadErrors { get; } = new();
    /// <summary>Mod 加载状态：modId → "OK" / "SKIP:..." / "FAIL:..."</summary>
    public static Dictionary<string, string> ModStatus { get; } = new();

    /// <summary>当前游戏版本</summary>
    public const string GameVersion = "0.1";

    private const string ModsRoot = "res://mods/";
    private const string ConfigPath = "user://mods_enabled.json";

    // ── 初始化 ──

    /// <summary>扫描并加载所有 Mod（由 GameManager 在启动时调用）</summary>
    public static void Init()
    {
        LoadEnabledConfig();
        ScanMods();
    }

    private static void ScanMods()
    {
        LoadedMods.Clear();
        var dir = DirAccess.Open(ModsRoot);
        if (dir == null) return;
        dir.ListDirBegin();
        while (true)
        {
            var name = dir.GetNext();
            if (string.IsNullOrEmpty(name)) break;
            if (name.StartsWith(".")) continue;
            if (dir.CurrentIsDir())
            {
                var m = ModManifest.Load(ModsRoot + name);
                if (m != null) LoadedMods.Add(m);
            }
        }
        dir.ListDirEnd();
    }

    // ── 启用 / 禁用 ──

    public static bool IsEnabled(ModManifest m) => EnabledMods.Contains(m.Id);

    public static void SetEnabled(ModManifest m, bool enabled)
    {
        if (enabled) EnabledMods.Add(m.Id);
        else EnabledMods.Remove(m.Id);
        SaveEnabledConfig();
    }

    /// <summary>应用所有已启用的 Mod（在游戏启动时调用）</summary>
    public static void ApplyAll(GameManager gm)
    {
        LoadErrors.Clear();
        ModStatus.Clear();

        var enabled = LoadedMods.Where(m => IsEnabled(m)).ToList();

        // 版本检查
        var enabledIds = new HashSet<string>(enabled.Select(m => m.Id));
        var skipIds = new HashSet<string>();
        foreach (var mod in enabled)
        {
            if (!string.IsNullOrEmpty(mod.MinGameVersion) && CompareVersion(mod.MinGameVersion, GameVersion) > 0)
            {
                ModStatus[mod.Id] = $"SKIP:需要 v{mod.MinGameVersion}";
                string msg = $"[{mod.Name}] 需要 v{mod.MinGameVersion}，当前 v{GameVersion}，跳过";
                LoadErrors.Add(msg); DlcManager.Log("Mod", msg);
                skipIds.Add(mod.Id);
            }
            foreach (var conflict in mod.Conflicts)
            {
                if (enabledIds.Contains(conflict))
                {
                    ModStatus[mod.Id] = "FAIL:冲突";
                    string msg = $"[{mod.Name}] 与 [{conflict}] 冲突";
                    LoadErrors.Add(msg); DlcManager.Log("Mod", msg);
                }
            }
            foreach (var dep in mod.Dependencies)
            {
                if (!enabledIds.Contains(dep))
                {
                    ModStatus[mod.Id] = $"SKIP:缺依赖 {dep}";
                    string msg = $"[{mod.Name}] 缺少依赖 [{dep}]，跳过";
                    LoadErrors.Add(msg); DlcManager.Log("Mod", msg);
                    skipIds.Add(mod.Id);
                }
            }
        }

        // 拓扑排序：被依赖的 Mod 先加载
        var valid = enabled.Where(m => !skipIds.Contains(m.Id)).ToList();
        var sorted = TopologicalSort(valid);

        // 安全扫描：先扫所有高风险 Mod
        int highRiskCount = 0;
        foreach (var mod in sorted)
        {
            var scan = ModSecurityScanner.ScanMod(mod.Folder, mod.Id);
            if (scan.RiskLevel >= ModRiskLevel.Medium)
                highRiskCount++;
        }

        foreach (var mod in sorted)
        {
            if (!string.IsNullOrEmpty(mod.MinGameVersion) && CompareVersion(mod.MinGameVersion, GameVersion) > 0)
            {
                DlcManager.Log("Mod", $"[{mod.Name}] SKIP — game version too low");
                continue;
            }

            var scan = ModSecurityScanner.ScanMod(mod.Folder, mod.Id);
            if (scan.RiskLevel >= ModRiskLevel.Medium && !ConfirmedRiskyMods.Contains(mod.Id))
            {
                ModStatus[mod.Id] = "SKIP:未确认风险";
                DlcManager.Log("Mod", $"[{mod.Name}] SKIP — risk not confirmed");
                continue;
            }

            ApplyMod(mod, gm);
            ModStatus[mod.Id] = "OK";
            DlcManager.Log("Mod", $"[{mod.Name}] loaded OK (type={mod.Type})");
            foreach (var err in LoadErrors.FindAll(e => e.Contains($"[{mod.Name}]")))
                DlcManager.Log("Mod", err);
        }
        foreach (var template in BlackSwanModDB.ConvertToTemplates())
            BlackSwanManager.RegisterTemplate(template);

        GD.Print($"[Mod] 所有 Mod 加载完成，共 {enabled.Count} 个");
    }

    private static int CompareVersion(string a, string b)
    {
        var partsA = a.Split('.');
        var partsB = b.Split('.');
        int maxLen = Math.Max(partsA.Length, partsB.Length);
        for (int i = 0; i < maxLen; i++)
        {
            int va = i < partsA.Length && int.TryParse(partsA[i], out var x) ? x : 0;
            int vb = i < partsB.Length && int.TryParse(partsB[i], out var y) ? y : 0;
            if (va != vb) return va.CompareTo(vb);
        }
        return 0;
    }

    private static List<ModManifest> TopologicalSort(List<ModManifest> mods)
    {
        var idToMod = mods.ToDictionary(m => m.Id);
        var visited = new HashSet<string>();
        var result = new List<ModManifest>();
        var inProgress = new HashSet<string>();

        void Dfs(ModManifest mod)
        {
            if (visited.Contains(mod.Id)) return;
            if (inProgress.Contains(mod.Id))
            {
                LoadErrors.Add($"[{mod.Name}] 检测到循环依赖");
                return;
            }
            inProgress.Add(mod.Id);
            // 硬依赖：必须存在
            foreach (var dep in mod.Dependencies)
            {
                if (idToMod.TryGetValue(dep, out var depMod))
                    Dfs(depMod);
            }
            // 可选依赖：存在则排序，不存在跳过
            foreach (var dep in mod.OptionalDependencies)
            {
                if (idToMod.TryGetValue(dep, out var depMod))
                    Dfs(depMod);
            }
            inProgress.Remove(mod.Id);
            visited.Add(mod.Id);
            result.Add(mod);
        }

        foreach (var mod in mods)
            Dfs(mod);

        return result;
    }

    /// <summary>应用单个 Mod</summary>
    public static void ApplyMod(ModManifest mod, GameManager gm)
    {
        if (mod.IsLanguage) ApplyLanguageMod(mod, gm);
        else if (mod.IsData) ApplyDataMod(mod, gm);
        else if (mod.HasScripts) ApplyScriptMod(mod, gm);

        // 从 Mod 文件夹加载程序集
        string asmDir = mod.Folder + "/assemblies";
        if (DirAccess.DirExistsAbsolute(asmDir))
            ModAssemblyLoader.LoadAllFrom(asmDir, gm);
    }

    /// <summary>扫描 Mod 并显示风险确认弹窗</summary>
    public static void ScanAndConfirmMods(GameManager gm)
    {
        var risky = new List<(ModManifest mod, ScanResult scan)>();
        var safe = new List<ModManifest>();

        foreach (var mod in LoadedMods)
        {
            if (!EnabledMods.Contains(mod.Id)) continue;
            var scan = ModSecurityScanner.ScanMod(mod.Folder, mod.Id);
            if (scan.RiskLevel >= ModRiskLevel.Medium)
                risky.Add((mod, scan));
            else if (mod.HasScripts)
                safe.Add(mod);
        }

        if (risky.Count == 0 && safe.Count == 0) return;

        if (risky.Count > 0)
        {
            ShowNextScanWarning(gm, risky, 0, () => ShowSafeConfirmAll(gm, safe));
        }
        else
        {
            ShowSafeConfirmAll(gm, safe);
        }
    }

    private static void ShowSafeConfirmAll(GameManager gm, List<ModManifest> safeMods)
    {
        if (safeMods.Count == 0) return;
        int idx = 0;
        ShowNextSafeConfirm(gm, safeMods, idx);
    }

    private static void ShowNextSafeConfirm(GameManager gm, List<ModManifest> safeMods, int idx)
    {
        if (idx >= safeMods.Count) return;
        var mod = safeMods[idx];
        gm.ShowChoicePopup(
            $"📋 {Loc.Tr("mod_risk.final_title_safe")}: {mod.Name}",
            Loc.Tr("mod_risk.final_msg_safe"),
            Loc.Tr("mod_risk.continue"),
            Loc.Tr("mod_risk.cancel"),
            () => ShowNextSafeConfirm(gm, safeMods, idx + 1),
            () => { EnabledMods.Remove(mod.Id); ShowNextSafeConfirm(gm, safeMods, idx + 1); },
            new Color(0.8f, 0.9f, 0.8f)
        );
    }

    private static void OpenModFolder(string folder)
    {
        string abs = ProjectSettings.GlobalizePath(folder);
        OS.ShellShowInFileManager(abs);
    }

    private static void ShowFinalConfirm(GameManager gm, ModManifest mod, ScanResult scan, Action onConfirm)
    {
        int totalRisks = scan.Patterns.Count + (scan.DllFiles.Count > 0 ? 1 : 0) + (scan.ScriptFiles.Count > 0 ? 1 : 0);
        bool hasRisk = totalRisks > 0;

        string title = hasRisk
            ? Loc.TrF("mod_risk.final_title", totalRisks)
            : Loc.Tr("mod_risk.final_title_safe");
        string msg = hasRisk
            ? Loc.TrF("mod_risk.final_msg_fmt", totalRisks)
            : Loc.Tr("mod_risk.final_msg_safe");

        // 第一层最终确认
        gm.ShowChoicePopup(title, msg,
            hasRisk ? Loc.Tr("mod_risk.final_accept") : Loc.Tr("mod_risk.continue"),
            Loc.Tr("mod_risk.cancel"),
            () =>
            {
                if (hasRisk && totalRisks >= 3)
                {
                    // 3+ 风险加一层嘲讽确认
                    gm.ShowChoicePopup(
                        Loc.Tr("mod_risk.troll_title"),
                        Loc.Tr("mod_risk.troll_msg"),
                        Loc.Tr("mod_risk.troll_confirm"),
                        Loc.Tr("mod_risk.cancel"),
                        () => ShowTimedConfirm(gm, mod, onConfirm),
                        () => { },
                        new Color(1f, 0.3f, 0.2f)
                    );
                }
                else
                {
                    ShowTimedConfirm(gm, mod, onConfirm);
                }
            },
            () => { },
            hasRisk ? new Color(1f, 0.85f, 0.7f) : new Color(0.8f, 0.9f, 0.8f)
        );
    }

    private static void ShowTimedConfirm(GameManager gm, ModManifest mod, Action onConfirm)
    {
        // 先显示等待提示
        gm.ShowPopup(
            Loc.Tr("mod_risk.timed_title"),
            Loc.TrF("mod_risk.timed_wait", 5),
            new Color(1f, 0.4f, 0.3f)
        );

        // 5秒后显示真正的确认按钮
        var timer = new Timer { WaitTime = 5, OneShot = true };
        gm.AddChild(timer);
        timer.Timeout += () =>
        {
            timer.QueueFree();
            gm.ShowChoicePopup(
                Loc.Tr("mod_risk.timed_title"),
                Loc.Tr("mod_risk.timed_msg"),
                Loc.Tr("mod_risk.timed_ready"),
                Loc.Tr("mod_risk.cancel"),
                () => { ConfirmedRiskyMods.Add(mod.Id); onConfirm(); },
                () => { },
                new Color(1f, 0.4f, 0.3f)
            );
        };
        timer.Start();
    }

    private static void ShowNextScanWarning(GameManager gm, List<(ModManifest mod, ScanResult scan)> queue, int idx, Action onAllDone = null)
    {
        if (idx >= queue.Count) { onAllDone?.Invoke(); return; }
        var (mod, scan) = queue[idx];

        int totalRisks = scan.Patterns.Count + (scan.DllFiles.Count > 0 ? 1 : 0) + (scan.ScriptFiles.Count > 0 ? 1 : 0);
        string summary;
        if (totalRisks >= 3) summary = Loc.Tr("mod_risk.summary_3");
        else if (totalRisks == 2) summary = Loc.Tr("mod_risk.summary_2");
        else if (totalRisks == 1) summary = Loc.Tr("mod_risk.summary_1");
        else summary = "";

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(summary)) parts.Add(summary);

        if (scan.Patterns.Count > 0)
        {
            parts.Add($"\n⚠️ {Loc.Tr("mod_risk.danger_patterns")}:");
            foreach (var p in scan.Patterns.Take(10))
                parts.Add($"   {mod.Id} 第{p.Line}行: {p.Code} ← {p.Reason}");
        }

        if (scan.DllFiles.Count > 0)
        {
            parts.Add($"\n⚠️ {Loc.Tr("mod_risk.dll_found")}:");
            foreach (var dll in scan.DllFiles)
                parts.Add($"   - {dll}");
            parts.Add(Loc.Tr("mod_risk.dll_warn"));
        }

        if (scan.ScriptFiles.Count > 0)
        {
            parts.Add($"\n⚠️ {Loc.Tr("mod_risk.script_files")}:");
            foreach (var sf in scan.ScriptFiles)
                parts.Add($"   - {sf}");
            parts.Add(Loc.Tr("mod_risk.script_warn"));
        }

        parts.Add($"\n{Loc.Tr("mod_risk.disclaimer")}");

        string fullMsg = string.Join("\n", parts);

        gm.ShowTriChoicePopup(
            $"⚠️ {Loc.Tr("mod_risk.title")}: {mod.Name}",
            fullMsg,
            Loc.Tr("mod_risk.confirm_risk"),
            Loc.Tr("mod_risk.reject"),
            Loc.Tr("mod_risk.open_folder"),
            () => ShowFinalConfirm(gm, mod, scan, () => ShowNextScanWarning(gm, queue, idx + 1, onAllDone)),
            () => { EnabledMods.Remove(mod.Id); ShowNextScanWarning(gm, queue, idx + 1, onAllDone); },
            () => OpenModFolder(mod.Folder),
            scan.RiskLevel switch
            {
                ModRiskLevel.Medium => new Color(1f, 0.6f, 0.2f),
                ModRiskLevel.High => new Color(1f, 0.4f, 0.2f),
                ModRiskLevel.Critical => new Color(1f, 0.2f, 0.2f),
                ModRiskLevel.Dangerous => new Color(0.8f, 0.1f, 0.1f),
                _ => new Color(1f, 0.6f, 0.2f)
            }
        );
    }

    // ── 语言 Mod ──

    private static void ApplyLanguageMod(ModManifest mod, GameManager gm)
    {
        // 扫描 mod 目录下的 locale/*.json 文件
        var localeDir = mod.Folder + "/locale";
        var dir = DirAccess.Open(localeDir);
        if (dir == null) return;

        dir.ListDirBegin();
        while (true)
        {
            var file = dir.GetNext();
            if (string.IsNullOrEmpty(file)) break;
            if (file.EndsWith(".json"))
            {
                string langCode = file.Replace(".json", "");
                string path = localeDir + "/" + file;
                using var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
                if (f != null)
                {
                    string raw = f.GetAsText();
                    var dict = new Dictionary<string, string>();
                    ParseSimpleJson(dict, raw);
                    Loc.MergeDictionary(langCode, dict);
                }
            }
        }
        dir.ListDirEnd();
        GD.Print($"[Mod] 语言 Mod 已加载: {mod.Name}");
    }

    // ── 数据 Mod ──

    private static void ApplyDataMod(ModManifest mod, GameManager gm)
    {
        string dataDir = mod.Folder + "/data";

            TryLoadAndMerge(dataDir + "/components.json", GameComponentDB.MergeFromJson);
            TryLoadAndMerge(dataDir + "/techs.json", TechTreeData.MergeFromJson);
            TryLoadAndMerge(dataDir + "/names.json", NameDB.MergeFromJson);
            TryLoadAndMerge(dataDir + "/balance.json", BalanceModDB.MergeFromJson);
            TryLoadAndMerge(dataDir + "/traits.json", TraitModDB.MergeFromJson);
            TryLoadAndMerge(dataDir + "/events.json", EventModDB.MergeFromJson);
            TryLoadAndMerge(dataDir + "/achievements.json", AchievementModDB.MergeFromJson);
            TryLoadAndMerge(dataDir + "/crisis.json", CrisisModDB.MergeFromJson);
            TryLoadAndMerge(dataDir + "/blackswan.json", BlackSwanModDB.MergeFromJson);

        GD.Print($"[Mod] 数据 Mod 已加载: {mod.Name}");
    }

    private static void TryLoadAndMerge(string path, Action<string> mergeAction)
    {
        if (!FileAccess.FileExists(path)) return;
        using var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (f != null) mergeAction(f.GetAsText());
    }

    // ── 脚本 Mod ──

    /// <summary>脚本 Mod 是否已被用户确认风险</summary>
    public static HashSet<string> ConfirmedRiskyMods { get; } = new();

    /// <summary>是否需要显示风险确认</summary>
    public static bool NeedsRiskConfirm(ModManifest mod) =>
        mod.HasScripts && !ConfirmedRiskyMods.Contains(mod.Id);

    /// <summary>活跃的脚本 Mod 对象（持久化引用，避免 GC）</summary>
    public static List<GodotObject> ActiveScriptMods { get; } = new();

    private static void ApplyScriptMod(ModManifest mod, GameManager gm)
    {
        // 脚本 Mod 通过 GDScript 执行
        string scriptDir = mod.Folder + "/scripts";
        var dir = DirAccess.Open(scriptDir);
        if (dir == null) return;

        dir.ListDirBegin();
        while (true)
        {
            var file = dir.GetNext();
            if (string.IsNullOrEmpty(file)) break;
            if (file.EndsWith(".gd"))
            {
                string path = scriptDir + "/" + file;
                if (FileAccess.FileExists(path))
                {
                    try
                    {
                        GD.Print($"[Mod] loading script: {path}");
                        var script = GD.Load<GDScript>(path);
                        if (script != null)
                        {
                            GD.Print($"[Mod] script loaded OK");
                            var obj = new Node();
                            obj.Name = "Mod_" + mod.Id + "_" + file.Replace(".gd", "");
                            obj.SetScript(script);
                            gm.AddChild(obj);
                            var bridge = gm.GetNodeOrNull<ModBridge>("ModBridge");
                            try { obj.Call("OnLoad", gm, bridge); } catch { }
                            ActiveScriptMods.Add(obj);
                            GD.Print($"[Mod] script mod active: {mod.Name}/{file}");
                        }
                        else
                        {
                            GD.PrintErr($"[Mod] GD.Load returned null for {path}");
                        }
                    }
                    catch (Exception ex)
                    {
                        ModStatus[mod.Id] = $"FAIL:{ex.Message}";
                        string msg = $"[Mod] 脚本执行失败 {mod.Name}/{file}: {ex.Message}";
                        GD.PrintErr(msg);
                        DlcManager.Log("Mod", msg);
                    }
                }
            }
        }
        dir.ListDirEnd();
        GD.Print($"[Mod] 脚本 Mod 已加载: {mod.Name}");
    }

    // ── 持久化 ──

    private static void LoadEnabledConfig()
    {
        EnabledMods.Clear();
        if (!FileAccess.FileExists(ConfigPath)) return;
        using var f = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (f == null) return;
        try
        {
            var doc = JsonDocument.Parse(f.GetAsText());
            foreach (var item in doc.RootElement.EnumerateArray())
                EnabledMods.Add(item.GetString());
        }
        catch { }
    }

    private static void SaveEnabledConfig()
    {
        using var f = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Write);
        if (f == null) return;
        var list = EnabledMods.ToList();
        f.StoreString(JsonSerializer.Serialize(list));
    }

    // ── JSON 解析（与 Loc.ParseSimpleJson 相同实现） ──

    private static void ParseSimpleJson(Dictionary<string, string> dict, string raw)
    {
        int i = 0;
        while (i < raw.Length)
        {
            while (i < raw.Length && raw[i] != '"') i++;
            if (i >= raw.Length) break;
            i++; int keyStart = i;
            while (i < raw.Length && raw[i] != '"') i++;
            string key = raw.Substring(keyStart, i - keyStart);
            i++;
            while (i < raw.Length && raw[i] != ':') i++;
            i++;
            while (i < raw.Length && (raw[i] == ' ' || raw[i] == '\r' || raw[i] == '\n' || raw[i] == '\t')) i++;
            if (i >= raw.Length) break;
            if (raw[i] == '"')
            {
                i++; int valStart = i;
                while (i < raw.Length)
                {
                    if (raw[i] == '\\') { i++; if (i < raw.Length && raw[i] == 'u') i += 5; else i++; continue; }
                    if (raw[i] == '"') break;
                    i++;
                }
                string val = raw.Substring(valStart, i - valStart).Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\t", "\t").Replace("\\r", "\r").Replace("\\\\", "\\");
                dict[key] = val;
                i++;
            }
            else if (raw[i] == '[')
            {
                int arrStart = i; i++; int depth = 1;
                while (i < raw.Length && depth > 0)
                {
                    if (raw[i] == '[') depth++;
                    else if (raw[i] == ']') depth--;
                    if (raw[i] == '"') { i++; while (i < raw.Length && raw[i] != '"') { if (raw[i] == '\\') i++; i++; } }
                    i++;
                }
                dict[key] = raw.Substring(arrStart, i - arrStart);
            }
            while (i < raw.Length && raw[i] != ',' && raw[i] != '}') i++;
            if (i < raw.Length && raw[i] == ',') i++;
        }
    }

    // ── Mod 设置系统（每个 Mod 可读取自己的 settings.json） ──

    private static Dictionary<string, Dictionary<string, string>> _modSettings = new();

    /// <summary>加载 Mod 设置（由 UI 调用）</summary>
    public static void LoadModSettings(ModManifest mod)
    {
        string path = mod.Folder + "/settings.json";
        if (!FileAccess.FileExists(path)) return;
        using var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (f == null) return;
        var dict = new Dictionary<string, string>();
        ParseSimpleJson(dict, f.GetAsText());
        _modSettings[mod.Id] = dict;
    }

    /// <summary>获取 Mod 设置值</summary>
    public static string GetModSetting(string modId, string key, string fallback = "")
    {
        return _modSettings.TryGetValue(modId, out var settings) && settings.TryGetValue(key, out var val) ? val : fallback;
    }

    /// <summary>保存 Mod 设置到 settings.json</summary>
    public static void SetModSetting(string modId, string key, string value)
    {
        if (!_modSettings.ContainsKey(modId))
            _modSettings[modId] = new Dictionary<string, string>();
        _modSettings[modId][key] = value;
        string path = $"user://mods/{modId}/settings.json";
        using var f = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (f != null)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{\n");
            bool first = true;
            foreach (var kv in _modSettings[modId])
            {
                if (!first) sb.Append(",\n");
                first = false;
                sb.Append($"  \"{kv.Key}\": \"{kv.Value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"");
            }
            sb.Append("\n}");
            f.StoreString(sb.ToString());
        }
    }
}
