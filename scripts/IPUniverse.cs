using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// IP 宇宙 — 可养成的系列品牌。
/// 由同系列多款游戏共同塑造世界观、积累粉丝、提升热度。
/// </summary>
public partial class IPUniverse
{
    public string Id { get; set; }                // 唯一标识
    public string Name { get; set; }               // 系列名
    public string WorldDescription { get; set; }   // 世界观描述（自动融合生成）
    public int FanCount { get; set; } = 100;       // IP 独立粉丝
    public int HeatLevel { get; set; } = 1;        // 热度等级 1~10
    public float HeatProgress { get; set; }        // 热度进度 0~1 升级

    /// <summary>该 IP 下的所有游戏名（按发售顺序）</summary>
    public List<string> GameTitles { get; set; } = new();
    /// <summary>该 IP 使用的类型/主题组合</summary>
    public HashSet<string> Genres { get; set; } = new();
    public HashSet<string> Themes { get; set; } = new();
    /// <summary>最高分</summary>
    public float BestScore { get; set; }
    /// <summary>平均分</summary>
    public float AverageScore { get; set; }
    /// <summary>已解锁的衍生权利</summary>
    public HashSet<string> UnlockedRights { get; set; } = new();

    /// <summary>最近一款游戏的评分（用于热度变化）</summary>
    public float LastGameScore { get; set; }

    /// <summary>饥饿年数（多久没出新作）</summary>
    public int HungerYears { get; set; }

    public IPUniverse() { }

    public IPUniverse(string id, string name)
    {
        Id = id;
        Name = name;
        WorldDescription = GenerateWorldDesc(new HashSet<string>(), new HashSet<string>());
    }

    // ═══════════════════ 世界观融合 ═══════════════════

    private static readonly Dictionary<string, string[]> WorldTemplates = new()
    {
        ["Fantasy"] = new[] { "剑与魔法的传奇大陆", "被古老魔法笼罩的神秘世界", "龙与骑士的史诗家园" },
        ["SciFi"] = new[] { "银河系的星际联邦", "赛博朋克风格的未来都市", "人类殖民的边疆星域" },
        ["Modern"] = new[] { "与现代社会无异的日常世界", "隐藏着超凡力量的平凡都市", "科技与人性交织的现代舞台" },
        ["Historical"] = new[] { "被历史尘埃掩埋的古代王国", "文艺复兴时期的繁荣城邦", "战争与和平交织的年代" },
        ["PostApoc"] = new[] { "文明崩溃后的荒芜废土", "末日幸存者重建的家园", "辐射笼罩下的最后堡垒" },
        ["Horror"] = new[] { "被黑暗笼罩的诅咒之地", "不可名状之物栖息的深渊", "绝望与恐惧交织的噩梦" },
        ["Comedy"] = new[] { "充满欢笑与意外的滑稽世界", "荒诞离奇的幽默宇宙", "轻松愉快的幻想乡" },
        ["Romance"] = new[] { "情感交织的温柔世界", "爱与羁绊编织的故事舞台", "心动与泪水的物语" },
        ["War"] = new[] { "硝烟弥漫的战火大地", "英雄与战略的钢铁舞台", "永无止境的战场" },
        ["Mystery"] = new[] { "充满谜团的迷雾之城", "真相隐藏在每一个角落", "逻辑与直觉交锋的舞台" },
    };

    public string GenerateWorldDesc(HashSet<string> genres, HashSet<string> themes)
    {
        if (themes.Count == 0) return "未知世界";
        var primary = themes.First();
        var secondary = themes.Skip(1).FirstOrDefault();
        var genreStr = genres.Count > 0 ? $"[{string.Join("/", genres)}]" : "";

        if (WorldTemplates.TryGetValue(primary, out var templates))
        {
            var rng = new Random(GameTitles.Count + 1);
            string baseDesc = templates[rng.Next(templates.Length)];
            return string.IsNullOrEmpty(genreStr) ? baseDesc : $"{baseDesc} ({genreStr})";
        }
        return $"{primary}风格的世界 ({genreStr})";
    }

    // ═══════════════════ 核心逻辑 ═══════════════════

    /// <summary>添加一款新游戏到 IP，更新世界观、粉丝、热度</summary>
    public void AddGame(string title, float score, string genre, string theme, bool isSequel)
    {
        GameTitles.Add(title);
        Genres.Add(genre);
        Themes.Add(theme);
        LastGameScore = score;

        // 更新最佳/平均分
        float total = AverageScore * (GameTitles.Count - 1) + score;
        AverageScore = GameTitles.Count > 0 ? total / GameTitles.Count : score;
        if (score > BestScore) BestScore = score;

        // 世界观融合
        WorldDescription = GenerateWorldDesc(Genres, Themes);

        // 饥饿清零
        HungerYears = 0;

        // 粉丝变化
        float fanDelta = (score - 50) * 10 + (isSequel ? 20 : 50);
        FanCount = Mathf.Max(50, FanCount + (int)fanDelta);

        // 热度变化
        UpdateHeat(score);
    }

    /// <summary>每月衰减 / 饥饿增加</summary>
    public void MonthlyTick()
    {
        // 长期不出新作 → 热度缓慢下降
        if (GameTitles.Count > 0)
        {
            HeatProgress -= 0.005f;
            if (HeatProgress < 0 && HeatLevel > 1)
            {
                HeatLevel--;
                HeatProgress = 0.9f;
            }
        }
    }

    /// <summary>每年末饥饿增加</summary>
    public void YearlyTick()
    {
        if (GameTitles.Count > 0)
            HungerYears++;
    }

    private void UpdateHeat(float score)
    {
        float heatGain = (score - 50) / 50f * 0.3f;
        if (HeatProgress + heatGain >= 1f && HeatLevel < 10)
        {
            HeatLevel++;
            HeatProgress = 0;
            // 解锁新权利
            UnlockRights();
        }
        else
        {
            HeatProgress = Mathf.Clamp(HeatProgress + heatGain, 0, 1f);
        }
    }

    private void UnlockRights()
    {
        if (HeatLevel >= 3 && !UnlockedRights.Contains("derivative"))
        {
            UnlockedRights.Add("derivative");
            GD.Print($"[IP] {Name} 解锁衍生作品权");
        }
        if (HeatLevel >= 5 && !UnlockedRights.Contains("adaptation"))
        {
            UnlockedRights.Add("adaptation");
            GD.Print($"[IP] {Name} 解锁改编授权权");
        }
        if (HeatLevel >= 8 && !UnlockedRights.Contains("movie"))
        {
            UnlockedRights.Add("movie");
            GD.Print($"[IP] {Name} 解锁影视化权");
        }
    }

    /// <summary>热度等级名称</summary>
    public string HeatLabel => HeatLevel switch
    {
        1 => "冷淡", 2 => "微温", 3 => "温和", 4 => "热门",
        5 => "火爆", 6 => "炽热", 7 => "传说", 8 => "神话",
        9 => "不朽", 10 => "永恒",
        _ => "未知"
    };

    /// <summary>首月销量加成系数</summary>
    public float SalesBonus => 1f + (HeatLevel - 1) * 0.05f + FanCount / 10000f * 0.02f;
}

/// <summary>全局 IP 宇宙管理器</summary>
public static class IPManager
{
    public static Dictionary<string, IPUniverse> AllIPs { get; } = new();

    public static IPUniverse GetOrCreate(string ipId, string name)
    {
        if (!AllIPs.TryGetValue(ipId, out var ip))
        {
            ip = new IPUniverse(ipId, name);
            AllIPs[ipId] = ip;
        }
        return ip;
    }

    public static void MonthlyTick()
    {
        foreach (var ip in AllIPs.Values)
            ip.MonthlyTick();
    }

    public static void YearlyTick()
    {
        foreach (var ip in AllIPs.Values)
            ip.YearlyTick();
    }

    public static void Clear() => AllIPs.Clear();
}
