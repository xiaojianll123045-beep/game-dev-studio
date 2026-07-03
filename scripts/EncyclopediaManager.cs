using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public partial class EncyclopediaManager : Node
{
    private Dictionary<string, EncyclopediaData> _cache = new();
    private string _currentLang = "zh";

    private static readonly string[] Branches = {
        "ProgramBase", "Render2D", "Render3D", "Audio", "Network", "AI",
        "Platform", "GenreUnlock", "ThemeUnlock"
    };

    private static readonly string[] BranchNames = {
        "程序基础", "2D渲染", "3D渲染", "音频", "网络", "AI",
        "平台与硬件", "类型解锁", "主题解锁"
    };

    public override void _Ready()
    {
        LoadData(GetLangCode());
    }

    private string GetLangCode()
    {
        int idx = Loc.CurrentLang;
        if (idx >= 0 && idx < Loc.LangNames.Length)
            return Loc.LangNames[idx];
        return "zh";
    }

    public void Reload()
    {
        string code = GetLangCode();
        if (_currentLang != code)
        {
            _cache.Remove(code);
            _currentLang = code;
        }
        LoadData(code);
    }

    private void LoadData(string lang)
    {
        if (_cache.ContainsKey(lang)) return;
        string path = $"res://encyclopedia/{lang}.json";
        if (!Godot.FileAccess.FileExists(path))
        {
            path = "res://encyclopedia/zh.json";
            if (!Godot.FileAccess.FileExists(path)) return;
        }
        using var f = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        string raw = f.GetAsText();
        var data = JsonSerializer.Deserialize<EncyclopediaData>(raw);
        if (data?.mechanics != null)
            data.mechanics.DeserializeItems();
        if (data != null)
            _cache[lang] = data;
    }

    private EncyclopediaData GetData()
    {
        string code = GetLangCode();
        if (!_cache.ContainsKey(code))
            LoadData(code);
        return _cache.GetValueOrDefault(code);
    }

    public GenreInfo GetGenreInfo(string id)
    {
        var data = GetData();
        return data?.genre_guide?.FirstOrDefault(g => g.id == id);
    }

    public ThemeInfo GetThemeInfo(string id)
    {
        var data = GetData();
        return data?.theme_guide?.FirstOrDefault(t => t.id == id);
    }

    public TechInfoEntry GetTechInfo(string id)
    {
        var data = GetData();
        return data?.tech_tree?.FirstOrDefault(t => t.id == id);
    }

    public List<GenreInfo> GetAllGenres()
    {
        return GetData()?.genre_guide ?? new();
    }

    public List<ThemeInfo> GetAllThemes()
    {
        return GetData()?.theme_guide ?? new();
    }

    public List<TechInfoEntry> GetAllTech()
    {
        return GetData()?.tech_tree ?? new();
    }

    public MechanicsData GetMechanics()
    {
        return GetData()?.mechanics;
    }

    public List<TechInfoEntry> GetTechByBranch(string branch)
    {
        return GetAllTech().Where(t => t.branch == branch).OrderBy(t => t.level).ToList();
    }

    public List<string> GetBranchNames()
    {
        return BranchNames.ToList();
    }

    public string GetBranchKey(int index)
    {
        if (index >= 0 && index < Branches.Length) return Branches[index];
        return "";
    }

    public string GetBranchName(string branchKey)
    {
        int idx = System.Array.IndexOf(Branches, branchKey);
        return idx >= 0 ? BranchNames[idx] : branchKey;
    }

    public List<object> Search(string query)
    {
        var results = new List<object>();
        if (string.IsNullOrWhiteSpace(query)) return results;
        var data = GetData();
        if (data == null) return results;
        var comp = StringComparison.OrdinalIgnoreCase;

        foreach (var g in data.genre_guide)
        {
            if (g.id.Contains(query, comp) || g.name.Contains(query, comp))
                results.Add(new { type = "genre", id = g.id, name = g.name });
        }
        foreach (var t in data.theme_guide)
        {
            if (t.id.Contains(query, comp) || t.name.Contains(query, comp))
                results.Add(new { type = "theme", id = t.id, name = t.name });
        }
        foreach (var t in data.tech_tree)
        {
            if (t.id.Contains(query, comp) || t.name.Contains(query, comp))
                results.Add(new { type = "tech", id = t.id, name = t.name });
        }

        return results;
    }
}

public class EncyclopediaData
{
    public List<GenreInfo> genre_guide { get; set; } = new();
    public List<ThemeInfo> theme_guide { get; set; } = new();
    public List<TechInfoEntry> tech_tree { get; set; } = new();
    public MechanicsData mechanics { get; set; } = new();
}

public class GenreInfo
{
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public List<string> compatible_themes { get; set; } = new();
    public int base_score_bonus { get; set; }
    public string tips { get; set; }
}

public class ThemeInfo
{
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public List<string> compatible_genres { get; set; } = new();
}

public class TechInfoEntry
{
    public string id { get; set; }
    public string name { get; set; }
    public string branch { get; set; }
    public int level { get; set; }
    public int required_months { get; set; }
    public string prerequisites { get; set; }
    public string primary_skill { get; set; }
    public int primary_skill_level { get; set; }
    public string secondary_skill { get; set; }
    public int secondary_skill_level { get; set; }
    public string description { get; set; }
    public string effect { get; set; }
}

public class MechanicsData
{
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement> _Raw { get; set; } = new();
    [System.Text.Json.Serialization.JsonIgnore]
    public Dictionary<string, MechanicsCategory> Items { get; set; } = new();

    public void DeserializeItems()
    {
        if (_Raw == null || Items.Count > 0) return;
        foreach (var kv in _Raw)
        {
            var cat = JsonSerializer.Deserialize<MechanicsCategory>(kv.Value.GetRawText());
            if (cat != null) Items[kv.Key] = cat;
        }
    }
}

public class MechanicsCategory
{
    public string title { get; set; }
    public List<MechanicsSection> sections { get; set; } = new();
}

public class MechanicsSection
{
    public string heading { get; set; }
    public string content { get; set; }
}
