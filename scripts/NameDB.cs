using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

/// <summary>随机姓名/公司名数据库，支持 Mod 扩展</summary>
public static class NameDB
{
    private static readonly Dictionary<string, List<string>> _data = new();

    /// <summary>获取指定类别的名称列表</summary>
    public static string[] GetNames(string category)
    {
        return _data.TryGetValue(category, out var list) ? list.ToArray() : Array.Empty<string>();
    }

    /// <summary>Mod 合并数据</summary>
    public static void MergeFromJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            foreach (var kv in doc.RootElement.EnumerateObject())
            {
                string category = kv.Name;
                if (!_data.ContainsKey(category))
                    _data[category] = new List<string>();
                foreach (var el in kv.Value.EnumerateArray())
                    _data[category].Add(el.GetString());
            }
            GD.Print($"[Mod][NameDB] 已合并名称数据");
        }
        catch (Exception e) { GD.PrintErr($"[Mod] names.json parse error: {e.Message}"); }
    }
}
