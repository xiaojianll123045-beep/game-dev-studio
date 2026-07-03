using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>Mod 清单 — 每个 mod 文件夹根目录必须包含 mod.json</summary>
public class ModManifest
{
    public string Id { get; set; }          // 唯一标识（文件夹名）
    public string Name { get; set; } = "未命名 Mod";
    public string Version { get; set; } = "1.0";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "data"; // language / script / data
    public List<string> Dependencies { get; set; } = new();
    public List<string> OptionalDependencies { get; set; } = new();
    public List<string> Conflicts { get; set; } = new();
    public string MinGameVersion { get; set; } = "0.1";
    public string IconPath { get; set; } = "";
    public string Folder { get; set; }      // 实际路径

    public bool HasScripts => Type == "script";
    public bool IsLanguage => Type == "language";
    public bool IsData => Type == "data";

    public static ModManifest Load(string folderPath)
    {
        string id = folderPath.Split('/').LastOrDefault()?.Split('\\').LastOrDefault() ?? "unknown";
        string jsonPath = folderPath + "/mod.json";
        if (!Godot.FileAccess.FileExists(jsonPath)) return null;
        using var f = Godot.FileAccess.Open(jsonPath, Godot.FileAccess.ModeFlags.Read);
        if (f == null) return null;
        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(f.GetAsText());
            var r = doc.RootElement;
            var m = new ModManifest
            {
                Id = id,
                Folder = folderPath,
                Name = id,   // fallback: 文件夹名
                Version = GetStr(r, "version", "1.0"),
                Author = GetStr(r, "author", ""),
                Description = "",
                Type = GetStr(r, "type", "data"),
                MinGameVersion = GetStr(r, "min_game_version", "0.1"),
                IconPath = GetStr(r, "icon", ""),
                Dependencies = new List<string>(),
                Conflicts = new List<string>()
            };
            if (r.TryGetProperty("dependencies", out var deps))
                foreach (var d in deps.EnumerateArray()) m.Dependencies.Add(d.GetString());
            if (r.TryGetProperty("optional_dependencies", out var optDeps))
                foreach (var d in optDeps.EnumerateArray()) m.OptionalDependencies.Add(d.GetString());
            if (r.TryGetProperty("conflicts", out var confs))
                foreach (var c in confs.EnumerateArray()) m.Conflicts.Add(c.GetString());

            // 从 mod_{lang}.json 读取显示名称/描述
            string langCode = Loc.LangNames[Loc.CurrentLang];
            string locPath = folderPath + "/mod_" + langCode + ".json";
            if (FileAccess.FileExists(locPath))
            {
                try
                {
                    using var fl = FileAccess.Open(locPath, FileAccess.ModeFlags.Read);
                    if (fl != null)
                    {
                        var locDoc = System.Text.Json.JsonDocument.Parse(fl.GetAsText());
                        var lr = locDoc.RootElement;
                        if (lr.TryGetProperty("name", out var ln)) m.Name = ln.GetString() ?? m.Name;
                        if (lr.TryGetProperty("description", out var ld)) m.Description = ld.GetString() ?? "";
                    }
                }
                catch { }
            }
            // 兼容旧版：未找到语言文件时从 mod.json 读取
            else
            {
                if (r.TryGetProperty("name", out var on)) m.Name = on.GetString() ?? m.Name;
                if (r.TryGetProperty("description", out var od)) m.Description = od.GetString() ?? "";
            }

            return m;
        }
        catch { return null; }
    }

    private static string GetStr(System.Text.Json.JsonElement e, string key, string fallback) =>
        e.TryGetProperty(key, out var v) ? v.GetString() ?? fallback : fallback;
}
