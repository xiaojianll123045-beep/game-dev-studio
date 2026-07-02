using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>档期窗口</summary>
public struct ReleaseSlot
{
    public int Month;
    public string SeasonKey;
    public float SalesMultiplier;
    public int CompetitorCount;
    public float MarketingCostMultiplier;
}

/// <summary>档期战争系统——日历+撞档</summary>
public partial class ReleaseCalendar : Node
{
    private GameManager _gm;
    private CompetitorAI _compAI;
    private GameDevManager _devMgr;

    // 已知的AI档期
    public List<(int month, string studio, string game, float score)> CompetitorSlots { get; private set; } = new();

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        _compAI = _gm.GetNode<CompetitorAI>("CompetitorAI");
        _devMgr = _gm.GetNode<GameDevManager>("GameDevManager");
    }

    /// <summary>获取某月的档期信息</summary>
    public ReleaseSlot GetSlot(int month)
    {
        int m = month % 12;
        string season;
        float mult;
        int comps;
        float costMult;

        if (m >= 10) // 11-12月 圣诞
        { season = "slot.christmas"; mult = 3.0f; comps = 7 + _compAI?.Studios.Count / 3 ?? 2; costMult = 2.5f; }
        else if (m >= 6) // 7-8月 暑期
        { season = "slot.summer"; mult = 2.2f; comps = 5 + _compAI?.Studios.Count / 4 ?? 2; costMult = 1.8f; }
        else if (m >= 2) // 3-4月 春季
        { season = "slot.spring"; mult = 1.0f; comps = 2 + _compAI?.Studios.Count / 5 ?? 1; costMult = 1.0f; }
        else // 1-2月 死亡区
        { season = "slot.dead"; mult = 0.6f; comps = Mathf.Max(0, (_compAI?.Studios.Count / 6 ?? 1) - 1); costMult = 0.5f; }

        return new ReleaseSlot { Month = month, SeasonKey = season, SalesMultiplier = mult,
            CompetitorCount = comps, MarketingCostMultiplier = costMult };
    }

    /// <summary>获取当月撞档的竞品数量（同类型）</summary>
    public int GetCompetitorsSameGenre(int month, GameGenre genre)
    {
        return CompetitorSlots.Count(s => s.month == month);
    }

    /// <summary>注册AI档期</summary>
    public void RegisterCompetitorSlot(int month, string studio, string game, float score)
    {
        CompetitorSlots.Add((month, studio, game, score));
        // 只保留未来12个月的
        CompetitorSlots.RemoveAll(s => s.month < _gm.GameMonth - 1);
    }

    /// <summary>计算最终销量倍率（含档期和竞争）</summary>
    public float CalculateFinalMultiplier(int releaseMonth, GameGenre genre)
    {
        var slot = GetSlot(releaseMonth);
        float mult = slot.SalesMultiplier;

        // 竞争惩罚
        int comp = GetCompetitorsSameGenre(releaseMonth, genre);
        mult *= Mathf.Max(0.3f, 1f - comp * 0.08f);

        return mult;
    }
}
