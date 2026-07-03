using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;

public class DlcManifest
{
    public string Id;
    public string Name;
    public string Version;
    public string Author;
    public string Description;
    public string Folder;
    public string Type;      // "minigame", "data", "script", "language"
    public string Scene;     // 入口场景路径（用于 minigame 类型）
    public PackedScene LoadedScene;
    public Script LoadedScript;
}

public static class DlcManager
{
    private static List<DlcManifest> _loaded = new();
    private static List<DlcManifest> _activeMinigames = new();
    private static HashSet<string> _enabledDlcIds = new();
    private static HashSet<string> _runningDlcIds = new();
    public static IReadOnlyList<DlcManifest> Loaded => _loaded;
    public static IReadOnlyList<DlcManifest> ActiveMinigames => _activeMinigames;
    public static bool IsDlcRunning(string id) => _runningDlcIds.Contains(id);
    public static bool IsDlcEnabled(string id) => _enabledDlcIds.Contains(id);
    public static int EnabledDlcCount => _enabledDlcIds.Count;
    public static void EnableDlc(string id) { _enabledDlcIds.Add(id); SaveEnabled(); }
    public static void DisableDlc(string id) { _enabledDlcIds.Remove(id); _runningDlcIds.Remove(id); SaveEnabled(); }
    public static void MarkRunning(string id, Node trackNode) { _runningDlcIds.Add(id); if (trackNode != null) trackNode.TreeExited += () => _runningDlcIds.Remove(id); }

    private static string SavePath => ProjectSettings.GlobalizePath("user://dlc_enabled.json");

    public static void SaveEnabled()
    {
        try
        {
            var arr = new System.Text.Json.Nodes.JsonArray();
            foreach (var id in _enabledDlcIds) arr.Add(id);
            var json = arr.ToJsonString();
            using var f = FileAccess.Open("user://dlc_enabled.json", FileAccess.ModeFlags.Write);
            if (f != null) { f.StoreString(json); Log("DlcManager", $"saved {_enabledDlcIds.Count} enabled DLCs"); }
        }
        catch (Exception ex) { GD.PrintErr($"[DLC] save failed: {ex.Message}"); }
    }

    public static void LoadEnabled()
    {
        try
        {
            if (!FileAccess.FileExists("user://dlc_enabled.json")) return;
            using var f = FileAccess.Open("user://dlc_enabled.json", FileAccess.ModeFlags.Read);
            if (f == null) return;
            var raw = f.GetAsText();
            var doc = JsonDocument.Parse(raw);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var id = el.GetString();
                if (!string.IsNullOrEmpty(id)) _enabledDlcIds.Add(id);
            }
            Log("DlcManager", $"loaded {_enabledDlcIds.Count} enabled DLCs");
        }
        catch (Exception ex) { GD.PrintErr($"[DLC] load failed: {ex.Message}"); }
    }

    public static void ScanAll()
    {
        Log("DlcManager", "=== DLC Scan Start ===");
        _loaded.Clear();
        _activeMinigames.Clear();
        // 扫 exe 同目录（安装包根目录），用户放 DLC/ 文件夹在旁边
        string exeDir = System.IO.Path.GetDirectoryName(OS.GetExecutablePath());
        if (DirAccess.DirExistsAbsolute(exeDir + "/DLC"))
            ScanDlcFrom(exeDir + "/DLC/");
        // 始终扫 res://DLC（开发环境 / 内置 DLC）
        ScanDlcFrom("res://DLC/");
        LoadEnabled();
        Log("DlcManager", $"DLC scan done, {_loaded.Count} total, {_activeMinigames.Count} minigames, {_enabledDlcIds.Count} enabled");
    }

    private static void ScanDlcFrom(string root)
    {
        var dir = DirAccess.Open(root);
        if (dir == null) return;

        dir.ListDirBegin();
        while (true)
        {
            var name = dir.GetNext();
            if (string.IsNullOrEmpty(name)) break;
            if (name.StartsWith(".")) continue;
            if (!dir.CurrentIsDir()) continue;

            string folder = root + name;
            string jsonPath = folder + "/dlc.json";
            if (!FileAccess.FileExists(jsonPath)) continue;

            try
            {
                using var f = FileAccess.Open(jsonPath, FileAccess.ModeFlags.Read);
                if (f == null) continue;
                var doc = JsonDocument.Parse(f.GetAsText());
                var r = doc.RootElement;
                var m = new DlcManifest
                {
                    Id = name,
                    Name = name,   // fallback: 先用文件夹名
                    Version = GetStr(r, "version", "1.0"),
                    Author = GetStr(r, "author", ""),
                    Description = "",
                    Folder = folder,
                    Type = GetStr(r, "type", "data"),
                    Scene = GetStr(r, "scene", ""),
                };

                // 从 dlc_{lang}.json 读取显示名称/描述
                string langCode = Loc.LangNames[Loc.CurrentLang];
                string locPath = folder + "/dlc_" + langCode + ".json";
                if (FileAccess.FileExists(locPath))
                {
                    try
                    {
                        using var fl = FileAccess.Open(locPath, FileAccess.ModeFlags.Read);
                        if (fl != null)
                        {
                            var locDoc = JsonDocument.Parse(fl.GetAsText());
                            var lr = locDoc.RootElement;
                            if (lr.TryGetProperty("name", out var ln)) m.Name = ln.GetString() ?? m.Name;
                            if (lr.TryGetProperty("description", out var ld)) m.Description = ld.GetString() ?? "";
                        }
                    }
                    catch { }
                }
                // 兼容旧版：未找到语言文件时从 dlc.json 读取
                else
                {
                    if (r.TryGetProperty("name", out var on)) m.Name = on.GetString() ?? m.Name;
                    if (r.TryGetProperty("description", out var od)) m.Description = od.GetString() ?? "";
                }

                // 预加载入口场景/脚本
                if (!string.IsNullOrEmpty(m.Scene) && FileAccess.FileExists(m.Scene))
                {
                    m.LoadedScene = ResourceLoader.Load<PackedScene>(m.Scene);
                    Log("DLC", $"[{m.Name}] scene loaded: {m.Scene}");
                }
                // 尝试加载 scripts/ 下的 GDScript
                var scriptPath = folder + "/scripts";
                if (DirAccess.DirExistsAbsolute(scriptPath))
                {
                    var sd = DirAccess.Open(scriptPath);
                    if (sd != null)
                    {
                        sd.ListDirBegin();
                        while (true)
                        {
                            var sf = sd.GetNext();
                            if (string.IsNullOrEmpty(sf)) break;
                            if (sf.EndsWith(".gd"))
                            {
                                var gdPath = scriptPath + "/" + sf;
                                m.LoadedScript = ResourceLoader.Load<Script>(gdPath);
                                Log("DLC", $"[{m.Name}] script loaded: {sf}");
                                break;
                            }
                        }
                        sd.ListDirEnd();
                    }
                }

                _loaded.Add(m);
                if (m.Type == "minigame" && (m.LoadedScene != null || m.LoadedScript != null))
                    _activeMinigames.Add(m);

                GD.Print($"[DLC] loaded: {m.Name} v{m.Version} (type={m.Type})");
                Log("DLC", $"[{m.Name}] loaded v{m.Version} type={m.Type}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[DLC] failed to load {name}: {ex.Message}");
                Log("DLC", $"[{name}] FAILED: {ex.Message}");
            }
        }
        dir.ListDirEnd();
    }

    /// <summary>启动一个小游戏 DLC（挂载到 GameManager 下）</summary>
    public static Node LaunchMinigame(GameManager gm, DlcManifest dlc)
    {
        if (gm == null || dlc == null) return null;
        if (dlc.Type != "minigame") return null;

        // 启动时从源码创建 GDScript，彻底绕过编辑器资源缓存
        Script script = null;
        if (!string.IsNullOrEmpty(dlc.Folder))
        {
            var sd = DirAccess.Open(dlc.Folder + "/scripts");
            if (sd != null)
            {
                sd.ListDirBegin();
                while (true)
                {
                    var sf = sd.GetNext();
                    if (string.IsNullOrEmpty(sf)) break;
                    if (sf.EndsWith(".gd"))
                    {
                        string gdPath = dlc.Folder + "/scripts/" + sf;
                        string source = FileAccess.GetFileAsString(gdPath);
                        if (!string.IsNullOrEmpty(source))
                        {
                            if (source.TrimStart().StartsWith("extends"))
                            {
                                var gd = new GDScript();
                                gd.SourceCode = source;
                                var err = gd.Reload();
                                if (err == Error.Ok)
                                {
                                    script = gd;
                                    Log("DLC", $"[{dlc.Name}] script loaded from source OK: {sf}");
                                }
                                else
                                {
                                    GD.PrintErr($"[DLC] GDScript source has errors (code={err}), fallback to ResourceLoader");
                                    script = ResourceLoader.Load<Script>(gdPath);
                                }
                            }
                            else
                            {
                                Log("DLC", $"[{dlc.Name}] fallback to ResourceLoader for non-extends: {sf}");
                                script = ResourceLoader.Load<Script>(gdPath);
                            }
                        }
                        else
                        {
                            Log("DLC", $"[{dlc.Name}] empty source, fallback to ResourceLoader");
                            script = ResourceLoader.Load<Script>(gdPath);
                        }
                        break;
                    }
                }
                sd.ListDirEnd();
            }
        }

        if (script == null && dlc.LoadedScript != null)
            script = dlc.LoadedScript;

        if (script != null)
        {
            var n = new Node { Name = "DLC_" + dlc.Id };
            n.SetScript(script);
            gm.AddChild(n);
            _runningDlcIds.Add(dlc.Id);
            n.TreeExited += () => { _runningDlcIds.Remove(dlc.Id); };
            var bridge = gm.GetNodeOrNull<ModBridge>("ModBridge");
            try { n.Call("OnLoad", gm, bridge); } catch (Exception ex) { GD.PrintErr($"[DLC] OnLoad error: {ex.Message}"); }
            return n;
        }
        if (dlc.LoadedScene != null)
        {
            var inst = dlc.LoadedScene.Instantiate();
            gm.AddChild(inst);
            return inst;
        }
        Log("DlcManager", $"LaunchMinigame: no valid scene or script for {dlc.Name}");
        return null;
    }

    private static string _logText = "=== Mod Log ===\n";
    public static void Log(string source, string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] [{source}] {message}";
        GD.Print(line);
        _logText += line + "\n";
    }

    public static string ReadLog() => _logText;
    public static void ClearLog() { _logText = "=== Mod Log ===\n"; Log("DlcManager", "Log cleared"); }

    private static string GetStr(JsonElement e, string key, string fallback) =>
        e.TryGetProperty(key, out var v) ? v.GetString() ?? fallback : fallback;
}
