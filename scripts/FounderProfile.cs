using System;
using System.Collections.Generic;

/// <summary>
/// 创始人配置 — 开局角色创建，影响全局加成
/// </summary>
public class FounderProfile
{
    public string Name { get; set; } = "创始人";
    public string CompanyName { get; set; } = "独立游戏工作室";

    // 六维技能 (1~10)
    public int Programming { get; set; } = 3;
    public int Art { get; set; } = 2;
    public int Audio { get; set; } = 1;
    public int Network { get; set; } = 1;
    public int AI { get; set; } = 1;
    public int Management { get; set; } = 2;

    public FounderTrait Trait { get; set; } = FounderTrait.Balanced;
    public int UnusedPoints { get; set; } = 8;
    public bool HasCreated { get; set; }

    /// <summary>获取性格给予的全局加成描述</summary>
    public string GetTraitDescription()
    {
        return Trait switch
        {
            FounderTrait.Visionary => Loc.Tr("founder.trait_desc_0"),
            FounderTrait.Technical => Loc.Tr("founder.trait_desc_1"),
            FounderTrait.Business => Loc.Tr("founder.trait_desc_2"),
            FounderTrait.Indie => Loc.Tr("founder.trait_desc_3"),
            FounderTrait.RiskTaker => Loc.Tr("founder.trait_desc_4"),
            FounderTrait.Balanced => Loc.Tr("founder.trait_desc_5"),
            _ => ""
        };
    }

    /// <summary>性格加成：游戏评分上限</summary>
    public float GetScoreCapBonus() => Trait == FounderTrait.Visionary ? 5f : 0f;

    /// <summary>性格加成：研发速度</summary>
    public float GetResearchSpeedBonus() => Trait == FounderTrait.Technical ? 0.15f : 0f;

    /// <summary>性格加成：初始资金倍数</summary>
    public float GetStartingMoneyMultiplier() => Trait == FounderTrait.Business ? 1.5f : 1f;

    /// <summary>性格加成：粉丝增长速度</summary>
    public float GetFanGrowthBonus() => Trait == FounderTrait.Indie ? 0.20f : 0f;

    /// <summary>性格加成：高风险事件成功率</summary>
    public float GetRiskBonus() => Trait == FounderTrait.RiskTaker ? 0.15f : 0f;

    public Dictionary<string, object> Serialize()
    {
        return new()
        {
            ["name"] = Name,
            ["company"] = CompanyName,
            ["programming"] = Programming,
            ["art"] = Art,
            ["audio"] = Audio,
            ["network"] = Network,
            ["ai"] = AI,
            ["management"] = Management,
            ["trait"] = Trait.ToString(),
            ["unused"] = UnusedPoints,
            ["created"] = HasCreated
        };
    }

    public static FounderProfile Deserialize(Dictionary<string, object> d)
    {
        return new()
        {
            Name = d.GetValueOrDefault("name", "创始人")?.ToString() ?? "创始人",
            CompanyName = d.GetValueOrDefault("company", "独立游戏工作室")?.ToString() ?? "独立游戏工作室",
            Programming = Convert.ToInt32(d.GetValueOrDefault("programming", 3)),
            Art = Convert.ToInt32(d.GetValueOrDefault("art", 2)),
            Audio = Convert.ToInt32(d.GetValueOrDefault("audio", 1)),
            Network = Convert.ToInt32(d.GetValueOrDefault("network", 1)),
            AI = Convert.ToInt32(d.GetValueOrDefault("ai", 1)),
            Management = Convert.ToInt32(d.GetValueOrDefault("management", 2)),
            Trait = Enum.TryParse<FounderTrait>(d.GetValueOrDefault("trait", "Balanced")?.ToString(), out var t) ? t : FounderTrait.Balanced,
            UnusedPoints = Convert.ToInt32(d.GetValueOrDefault("unused", 8)),
            HasCreated = Convert.ToBoolean(d.GetValueOrDefault("created", false))
        };
    }
}

public enum FounderTrait
{
    Visionary,   // 远见者：游戏评分上限+5
    Technical,   // 技术宅：研发速度+15%
    Business,    // 商人：初始资金+50%
    Indie,       // 独立魂：粉丝增长速度+20%
    RiskTaker,   // 赌徒：高风险事件成功率+15%
    Balanced     // 均衡：全技能+1
}
