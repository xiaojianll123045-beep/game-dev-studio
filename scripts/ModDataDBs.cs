using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public static class BalanceModDB
{
    public static Dictionary<string, float> Overrides { get; } = new();
    public static void MergeFromJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            foreach (var kv in doc.RootElement.EnumerateObject())
                Overrides[kv.Name] = kv.Value.GetSingle();
        }
        catch (Exception e) { GD.PrintErr($"[Mod] balance.json parse error: {e.Message}"); }
    }
    public static float Get(string key, float fallback) => Overrides.GetValueOrDefault(key, fallback);
}

public static class TraitModDB
{
    public static List<Dictionary<string, JsonElement>> CustomTraits { get; } = new();
    public static void MergeFromJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var dict = new Dictionary<string, JsonElement>();
                foreach (var kv in el.EnumerateObject()) dict[kv.Name] = kv.Value;
                CustomTraits.Add(dict);
            }
        }
        catch (Exception e) { GD.PrintErr($"[Mod] traits.json parse error: {e.Message}"); }
    }
}

public static class EventModDB
{
    public static List<Dictionary<string, JsonElement>> CustomEvents { get; } = new();
    public static void MergeFromJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var dict = new Dictionary<string, JsonElement>();
                foreach (var kv in el.EnumerateObject()) dict[kv.Name] = kv.Value;
                CustomEvents.Add(dict);
            }
        }
        catch (Exception e) { GD.PrintErr($"[Mod] events.json parse error: {e.Message}"); }
    }
}

public static class AchievementModDB
{
    public static List<Dictionary<string, JsonElement>> CustomAchievements { get; } = new();
    public static void MergeFromJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var dict = new Dictionary<string, JsonElement>();
                foreach (var kv in el.EnumerateObject()) dict[kv.Name] = kv.Value;
                CustomAchievements.Add(dict);
            }
        }
        catch (Exception e) { GD.PrintErr($"[Mod] achievements.json parse error: {e.Message}"); }
    }
}

public static class CrisisModDB
{
    public static List<Dictionary<string, JsonElement>> CustomCrisisNodes { get; } = new();
    public static void MergeFromJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var dict = new Dictionary<string, JsonElement>();
                foreach (var kv in el.EnumerateObject()) dict[kv.Name] = kv.Value;
                CustomCrisisNodes.Add(dict);
            }
            GD.Print($"[Mod] 已加载 {doc.RootElement.GetArrayLength()} 个自定义危机事件");
        }
        catch (Exception e) { GD.PrintErr($"[Mod] crisis.json parse error: {e.Message}"); }
    }

    /// <summary>将 Mod 数据转换为 CrisisNode 列表，供 CrisisEventTree 使用</summary>
    public static List<CrisisNode> ConvertToNodes()
    {
        var nodes = new List<CrisisNode>();
        foreach (var dict in CustomCrisisNodes)
        {
            try
            {
                string TryGetStr(string key, string fallback = "")
                {
                    if (dict.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String) return el.GetString() ?? "";
                    return string.IsNullOrEmpty(fallback) ? "" : fallback;
                }
                string TryGetStrOrNull(string key)
                {
                    if (dict.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String) return el.GetString();
                    return null;
                }
                float TryGetFloat(string key, float fallback = 0)
                {
                    if (dict.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.Number) return el.GetSingle();
                    return fallback;
                }
                int TryGetInt(string key, int fallback = 0)
                {
                    if (dict.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.Number) return el.GetInt32();
                    return fallback;
                }
                var node = new CrisisNode
                {
                    Id = TryGetStr("id", "mod_crisis_" + nodes.Count),
                    TitleKey = TryGetStr("title", "Mod Crisis"),
                    DescKey = TryGetStr("description"),
                    TriggerCondition = TryGetStr("trigger_condition", "1 > 0"),
                    TriggerProbability = TryGetFloat("trigger_probability", 0.3f),
                    DelayMonths = TryGetInt("delay_months"),
                    ParentEventId = TryGetStrOrNull("parent_event_id"),
                    ChainId = TryGetStrOrNull("chain_id"),
                    ChainStep = TryGetInt("chain_step", 1),
                    Options = new List<CrisisOption>(),
                };
                // 解析选项
                if (dict.TryGetValue("options", out var optsEl) && optsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var optEl in optsEl.EnumerateArray())
                    {
                        var opt = new CrisisOption
                        {
                            LabelKey = optEl.TryGetProperty("label", out var lbl) ? lbl.GetString() ?? "Option" : "Option",
                            ResultDescKey = optEl.TryGetProperty("result_desc", out var rd) ? rd.GetString() ?? "" : "",
                            NextEventId = optEl.TryGetProperty("next_event", out var ne) ? ne.GetString() : null,
                            ReputationChange = optEl.TryGetProperty("reputation", out var rep) ? rep.GetInt32() : 0,
                            TrustChange = optEl.TryGetProperty("trust", out var tr) ? tr.GetSingle() : 0,
                            Effects = new Dictionary<string, float>(),
                        };
                        if (optEl.TryGetProperty("effects", out var effEl) && effEl.ValueKind == JsonValueKind.Object)
                            foreach (var eff in effEl.EnumerateObject())
                                opt.Effects[eff.Name] = eff.Value.GetSingle();
                        node.Options.Add(opt);
                    }
                }
                nodes.Add(node);
            }
            catch (Exception e) { GD.PrintErr($"[Mod] 危机事件转换错误: {e.Message}"); }
        }
        return nodes;
    }
}

public static class BlackSwanModDB
{
    public static List<Dictionary<string, JsonElement>> CustomBlackSwans { get; } = new();
    public static void MergeFromJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var dict = new Dictionary<string, JsonElement>();
                foreach (var kv in el.EnumerateObject()) dict[kv.Name] = kv.Value;
                CustomBlackSwans.Add(dict);
            }
            GD.Print($"[Mod] 已加载 {doc.RootElement.GetArrayLength()} 个自定义黑天鹅事件");
        }
        catch (Exception e) { GD.PrintErr($"[Mod] blackswan.json parse error: {e.Message}"); }
    }

    /// <summary>将 Mod 数据转换为 BlackSwanEvent 工厂函数列表</summary>
    public static List<Func<BlackSwanEvent>> ConvertToTemplates()
    {
        var templates = new List<Func<BlackSwanEvent>>();
        foreach (var dict in CustomBlackSwans)
        {
            templates.Add(() =>
            {
                try
                {
                    string GStr(string key, string fallback = "")
                    {
                        if (dict.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String) return el.GetString() ?? fallback;
                        return fallback;
                    }
                    int GInt(string key, int fallback = 0)
                    {
                        if (dict.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.Number) return el.GetInt32();
                        return fallback;
                    }
                    var evt = new BlackSwanEvent
                    {
                        Id = GStr("id", "mod_swan_" + templates.Count),
                        Title = GStr("title", "Mod Event"),
                        Description = GStr("description"),
                        DurationMonths = GInt("duration", 6),
                        ChoiceATitle = GStr("choice_a_title", "选项A"),
                        ChoiceADesc = GStr("choice_a_desc"),
                        ChoiceBTitle = GStr("choice_b_title", "选项B"),
                        ChoiceBDesc = GStr("choice_b_desc"),
                        ActiveModifiers = new Dictionary<string, float>(),
                    };
                    if (dict.TryGetValue("modifiers", out var modEl) && modEl.ValueKind == JsonValueKind.Object)
                        foreach (var m in modEl.EnumerateObject())
                            evt.ActiveModifiers[m.Name] = m.Value.GetSingle();
                    return evt;
                }
                catch (Exception e) { GD.PrintErr($"[Mod] 黑天鹅事件转换错误: {e.Message}"); return null; }
            });
        }
        return templates;
    }
}
