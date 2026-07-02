using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>DLC 管理器 — 扫描 res://DLC/ 下的独立内容包</summary>
public static class DlcManager
{
    private static List<DlcManifest> _loaded = new();

    public static IReadOnlyList<DlcManifest> Loaded => _loaded;

    /// <summary>扫描并加载所有 DLC</summary>
    public static void ScanAll()
    {
        Log("DlcManager", "=== DLC Scan Start ===");
        _loaded.Clear();
        var dir = DirAccess.Open("res://DLC");
        if (dir == null) return;

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
                var doc = System.Text.Json.JsonDocument.Parse(f.GetAsText());
                var r = doc.RootElement;
                var m = new DlcManifest
                {
                    Id = name,
                    Name = GetStr(r, "name", name),
                    Version = GetStr(r, "version", "1.0"),
                    Author = GetStr(r, "author", ""),
                    Description = GetStr(r, "description", ""),
                    Folder = folder
                };
                _loaded.Add(m);
                GD.Print($"[DLC] loaded: {m.Name} v{m.Version}");
                Log("DLC", $"[{m.Name}] loaded v{m.Version}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[DLC] failed to load {name}: {ex.Message}");
                Log("DLC", $"[{name}] FAILED: {ex.Message}");
            }
        }
        dir.ListDirEnd();
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

    private static string GetStr(System.Text.Json.JsonElement e, string key, string fallback) =>
        e.TryGetProperty(key, out var v) ? v.GetString() ?? fallback : fallback;
}

public class DlcManifest
{
    public string Id;
    public string Name;
    public string Version;
    public string Author;
    public string Description;
    public string Folder;
}
