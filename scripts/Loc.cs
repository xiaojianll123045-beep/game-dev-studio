using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 国际化管理器 — 从 JSON 加载多语言文本
/// 用法: Loc.Tr("ui.company_list") 或 Loc.TrF("ui.money_fmt", 12345)
/// </summary>
public static class Loc
{
    public static int CurrentLang { get; set; } = 0;
    public static string[] LangNames = { "zh", "en" };
    public static string[] LangLabels = { "中文", "English" };
    /// <summary>当前语言是否为 RTL</summary>
    public static bool IsRTL => false;

    private static Dictionary<string, string>[] _dicts = new Dictionary<string, string>[LangNames.Length];
    /// <summary>Mod 注册的额外语言（语言代码 → 字典）</summary>
    private static readonly Dictionary<string, Dictionary<string, string>> _modDicts = new();

    public static void Init()
    {
        for (int i = 0; i < LangNames.Length; i++)
        {
            _dicts[i] = new Dictionary<string, string>();
            string path = $"res://locales/{LangNames[i]}.json";
            if (!Godot.FileAccess.FileExists(path)) continue;
            using var f = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            string raw = f.GetAsText();
            ParseSimpleJson(_dicts[i], raw);

            // 加载教程子文件
            string tutPath = $"res://locales/tutorial/{LangNames[i]}.json";
            if (Godot.FileAccess.FileExists(tutPath))
            {
                using var f2 = Godot.FileAccess.Open(tutPath, Godot.FileAccess.ModeFlags.Read);
                ParseSimpleJson(_dicts[i], f2.GetAsText());
            }
        }

        // 首次启动检测系统语言
        if (!_initialized)
        {
            _initialized = true;
            CurrentLang = DetectSystemLanguage();
        }
    }

    private static bool _initialized = false;

    /// <summary>根据系统语言自动匹配合适的语言 index</summary>
    public static int DetectSystemLanguage()
    {
        string sysLang = Godot.OS.GetLocale().ToLowerInvariant();
        // 简体中文
        if (sysLang.StartsWith("zh")) return 0;
        // 法语
        if (sysLang.StartsWith("fr")) return 4;
        // 德语
        if (sysLang.StartsWith("de")) return 5;
        // 西班牙语
        if (sysLang.StartsWith("es")) return 6;
        // 葡萄牙语
        if (sysLang.StartsWith("pt")) return 7;
        // 俄语
        if (sysLang.StartsWith("ru")) return 8;
        // 意大利语
        if (sysLang.StartsWith("it")) return 9;
        // 阿拉伯语
        if (sysLang.StartsWith("ar")) return 10;
        // 泰语
        if (sysLang.StartsWith("th")) return 11;
        // 越南语
        if (sysLang.StartsWith("vi")) return 12;
        // 土耳其语
        if (sysLang.StartsWith("tr")) return 13;
        // 日语
        if (sysLang.StartsWith("ja")) return 2;
        // 韩语
        if (sysLang.StartsWith("ko")) return 3;
        // 英语（默认）
        return 1;
    }

    public static string Tr(string key)
    {
        var dict = GetDict(CurrentLang);
        if (dict != null && dict.TryGetValue(key, out string val))
            return val;
        // fallback to English (index 1) for non-English languages
        if (CurrentLang != 1)
        {
            var enDict = GetDict(1);
            if (enDict != null && enDict.TryGetValue(key, out string en))
                return en;
        }
        // fallback to Chinese
        if (CurrentLang != 0)
        {
            var zhDict = GetDict(0);
            if (zhDict != null && zhDict.TryGetValue(key, out string zh))
                return zh;
        }
        return key;
    }

    private static Dictionary<string, string> GetDict(int idx)
    {
        if (idx >= 0 && idx < _dicts.Length && _dicts[idx] != null) return _dicts[idx];
        string code = idx >= 0 && idx < LangNames.Length ? LangNames[idx] : null;
        return code != null && _modDicts.TryGetValue(code, out var d) ? d : null;
    }

    public static string TrF(string key, params object[] args)
    {
        string fmt = Tr(key);
        try { return string.Format(fmt, args); } catch { return fmt; }
    }

    public static void SetLang(int idx)
    {
        if (idx >= 0 && idx < LangNames.Length)
            CurrentLang = idx;
    }

    /// <summary>阿拉伯语名字缩写：保留第一个名字+最后一个全名</summary>
    public static string AbbreviateArabicName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return fullName;
        if (CurrentLang != 10 || !GlobalSettings.ArabicNameAbbr) return fullName;
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 2) return fullName;
        // "Mohammed Ali Ahmed" → "Mohammed Ahmed"（去中间名，保留全姓）
        return parts[0] + " " + parts[^1];
    }

    /// <summary>获取显示用的名字（自动缩写阿拉伯语名字）</summary>
    public static string DisplayName(string name) => AbbreviateArabicName(name);

    /// <summary>分割多语言列表，兼容阿拉伯语逗号（، U+060C）和 ASCII 逗号</summary>
    public static string[] SplitLocaleList(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
        // 将阿拉伯语逗号替换为 ASCII 逗号
        raw = raw.Replace('،', ',');
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(s => s.Trim()).ToArray();
    }

    public static string[] ParseModNames()
    {
        string raw = Tr("dev.mod_names");
        if (string.IsNullOrWhiteSpace(raw) || !raw.StartsWith("["))
            return new[] { "Core", "Visual", "Audio", "Story", "Stability", "Online" };
        var trimmed = raw.Trim().Trim('[').Trim(']');
        var names = new List<string>(SplitLocaleList(trimmed));
        return names.Count >= 6 ? names.ToArray() : new[] { "Core", "Visual", "Audio", "Story", "Stability", "Online" };
    }

    private static string DecodeUnicodeEscapes(string s)
    {
        if (string.IsNullOrEmpty(s) || !s.Contains("\\u")) return s;
        return System.Text.RegularExpressions.Regex.Replace(s, @"\\u([0-9a-fA-F]{4})",
            m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
    }

    private static void ParseSimpleJson(Dictionary<string, string> dict, string raw)
    {
        int i = 0;
        while (i < raw.Length)
        {
            // skip to "
            while (i < raw.Length && raw[i] != '"') i++;
            if (i >= raw.Length) break;
            i++; // skip "
            int keyStart = i;
            while (i < raw.Length && raw[i] != '"') i++;
            string key = raw.Substring(keyStart, i - keyStart);
            i++; // skip "
            // skip to :
            while (i < raw.Length && raw[i] != ':') i++;
            i++; // skip :
            // skip whitespace
            while (i < raw.Length && (raw[i] == ' ' || raw[i] == '\r' || raw[i] == '\n' || raw[i] == '\t')) i++;
            if (i >= raw.Length) break;
            if (raw[i] == '"')
            {
                i++; // skip "
                int valStart = i;
                while (i < raw.Length)
                {
                    if (raw[i] == '\\')
                    {
                        i++; // skip backslash
                        if (i < raw.Length && raw[i] == 'u') { i += 5; continue; } // skip \uXXXX
                        i++; // skip escaped char like \", \n, \\
                        continue;
                    }
                    if (raw[i] == '"') break;
                    i++;
                }
                string val = raw.Substring(valStart, i - valStart)
                    .Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\t", "\t").Replace("\\r", "\r").Replace("\\\\", "\\");
                val = DecodeUnicodeEscapes(val);
                dict[key] = val;
                i++; // skip "
            }
            else if (raw[i] == '[')
            {
                // JSON array value: read until ]
                int arrStart = i;
                i++; // skip [
                int depth = 1;
                while (i < raw.Length && depth > 0)
                {
                    if (raw[i] == '[') depth++;
                    else if (raw[i] == ']') depth--;
                    if (raw[i] == '"') { i++; while (i < raw.Length && raw[i] != '"') { if (raw[i] == '\\') i++; i++; } }
                    i++;
                }
                string arrVal = raw.Substring(arrStart, i - arrStart);
                dict[key] = arrVal;
            }
            // skip to next
            while (i < raw.Length && raw[i] != ',' && raw[i] != '}') i++;
            if (i < raw.Length && raw[i] == ',') i++;
        }
    }

    // ═══════════════════════ Mod 支持 ═══════════════════════

    /// <summary>外部 Mod 合并语言数据（覆盖或新增键）</summary>
    public static void MergeDictionary(string langCode, Dictionary<string, string> entries)
    {
        int idx = System.Array.IndexOf(LangNames, langCode);
        if (idx >= 0)
        {
            foreach (var kv in entries)
                _dicts[idx][kv.Key] = kv.Value;
        }
        else
        {
            // 新语言注册到 _modDicts
            if (_modDicts.TryGetValue(langCode, out var existing))
            {
                foreach (var kv in entries) existing[kv.Key] = kv.Value;
            }
            else
            {
                _modDicts[langCode] = new Dictionary<string, string>(entries);
            }
            // 也注册到语言列表
            var newNames = new string[LangNames.Length + 1];
            var newLabels = new string[LangLabels.Length + 1];
            for (int i = 0; i < LangNames.Length; i++)
            {
                newNames[i] = LangNames[i];
                newLabels[i] = LangLabels[i];
            }
            newNames[LangNames.Length] = langCode;
            newLabels[LangNames.Length] = langCode;
            LangNames = newNames;
            LangLabels = newLabels;
            // 扩展 _dicts
            var newDicts = new Dictionary<string, string>[_dicts.Length + 1];
            for (int i = 0; i < _dicts.Length; i++) newDicts[i] = _dicts[i];
            newDicts[_dicts.Length] = new Dictionary<string, string>();
            _dicts = newDicts;
        }
    }
}
