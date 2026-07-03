using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

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
    public static IReadOnlyList<DlcManifest> Loaded => _loaded;
    public static IReadOnlyList<DlcManifest> ActiveMinigames => _activeMinigames;

    public static void ScanAll()
    {
        Log("DlcManager", "=== DLC Scan Start ===");
        _loaded.Clear();
        _activeMinigames.Clear();
        var dir = DirAccess.Open("res://DLC");
        if (dir == null) { Log("DlcManager", "DLC directory not found"); return; }

        dir.ListDirBegin();
        while (true)
        {
            var name = dir.GetNext();
            if (string.IsNullOrEmpty(name)) break;
            if (name.StartsWith(".")) continue;
            if (!dir.CurrentIsDir()) continue;

            string folder = "res://DLC/" + name;
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
                    Name = GetStr(r, "name", name),
                    Version = GetStr(r, "version", "1.0"),
                    Author = GetStr(r, "author", ""),
                    Description = GetStr(r, "description", ""),
                    Folder = folder,
                    Type = GetStr(r, "type", "data"),
                    Scene = GetStr(r, "scene", ""),
                };

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
        Log("DlcManager", $"DLC scan done, {_loaded.Count} total, {_activeMinigames.Count} minigames");
    }

    /// <summary>启动一个小游戏 DLC（挂载到 GameManager 下）</summary>
    public static Node LaunchMinigame(GameManager gm, DlcManifest dlc)
    {
        if (dlc.Type != "minigame") return null;
        if (dlc.LoadedScene != null)
        {
            var inst = dlc.LoadedScene.Instantiate();
            gm.AddChild(inst);
            return inst;
        }
        if (dlc.LoadedScript != null)
        {
            var n = new Node { Name = "DLC_" + dlc.Id };
            n.SetScript(dlc.LoadedScript);
            gm.AddChild(n);
            return n;
        }
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
