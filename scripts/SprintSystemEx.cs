using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class SprintSystemEx : Node
{
    private GameManager _gm;
    private GameDevManager _devMgr;
    private TeamManager _teamMgr;

    public override void _Ready()
    {
        _gm = Services.GameManager;
        _devMgr = Services.GameDevManager;
        _teamMgr = Services.TeamManager;
    }

    // ── Scrum Board 冲突系统 ──
    private static readonly Dictionary<(string, string), string> TaskConflicts = new()
    {
        [("core_combat", "tech_perf")] = "人手冲突：战斗系统与性能优化需要同一批引擎程序员",
        [("content_art", "polish_ui")] = "美术资源争抢：新内容和UI打磨共用美术池",
        [("core_ai", "tech_net")] = "架构冲突：AI系统与网络模块的底层设计矛盾",
    };

    public List<(string, string, string)> GetConflicts(List<string> selectedTasks)
    {
        var result = new List<(string, string, string)>();
        foreach (var kv in TaskConflicts)
        {
            if (selectedTasks.Contains(kv.Key.Item1) && selectedTasks.Contains(kv.Key.Item2))
                result.Add((kv.Key.Item1, kv.Key.Item2, kv.Value));
        }
        return result;
    }

    public bool HasConflict(List<string> selectedTasks)
    {
        return TaskConflicts.Keys.Any(k => selectedTasks.Contains(k.Item1) && selectedTasks.Contains(k.Item2));
    }

    // ── Sprint 回顾机制 ──
    public float CalculateVelocityChange(Team team, float devProgressGained)
    {
        float baseChange = 1.0f;
        if (devProgressGained > 0.15f) baseChange += 0.05f;
        else if (devProgressGained < 0.05f) baseChange -= 0.1f;
        if (team.Morale > 70) baseChange += 0.03f;
        else if (team.Morale < 30) baseChange -= 0.05f;
        return Mathf.Clamp(baseChange, 0.7f, 1.3f);
    }

    // ── 董事会会议 ──
    public BoardMood CalculateBoardMood()
    {
        var recent = _devMgr.CompletedProjects
            .OrderByDescending(p => p.OriginalReleaseMonth)
            .Take(3).ToList();
        float avgScore = recent.Count > 0 ? recent.Average(p => p.FinalScore) : 50f;
        float trust = _devMgr.PlayerTrust;
        float cashRatio = _gm.ResMgr.Money / Mathf.Max(1, _gm.ResMgr.MonthlyExpense * 6);

        if (avgScore >= 80 && trust >= 70 && cashRatio > 2) return BoardMood.Supportive;
        if (avgScore >= 60 && trust >= 40 && cashRatio > 1) return BoardMood.Neutral;
        if (trust < 30 || cashRatio < 0.5) return BoardMood.Angry;
        return BoardMood.Pressuring;
    }
}

// ── Team 扩展 ──
public partial class Team
{
    public float VelocityModifier { get; set; } = 1.0f;
    public float Morale { get; set; } = 0.5f;
    public string MiddlewareTarget { get; set; }
    public float MiddlewareProgress { get; set; }
    public int MiddlewareTargetMonths { get; set; }
}

// ── GameProject 扩展 ──
public partial class GameProject
{
    public GameGenre? SecondaryGenre { get; set; }
    public float FusionScore { get; set; }
    public string FusionTag { get; set; }
    public bool HasPrototype { get; set; }
    public int PrototypeMonths { get; set; }
    public float PrototypeWishlists { get; set; }
    public float ProtoFeedbackScore { get; set; }
    public bool IsCancelledAfterProto { get; set; }
    public bool IsEarlyAccess { get; set; }
    public int EAMonths { get; set; }
    public float EARevenue { get; set; }
    public float EAScorePenalty { get; set; }
    public float EAFeedbackScore { get; set; }
    public bool EACompleted { get; set; }
    public bool HasAttendedShow { get; set; }
    public GameShow? AttendedShow { get; set; }
    public float ShowDemoScore { get; set; }
    public CrowdfundingCampaign Crowdfunding { get; set; }
    public PivotInfo PivotInfo { get; set; } = new();
    public int PivotCount { get => PivotInfo.PivotCount; set => PivotInfo.PivotCount = value; }
    public Dictionary<string, string> DesignChoices { get; set; } = new();
    public float CodeReusability { get => PivotInfo.CodeReusability; set => PivotInfo.CodeReusability = value; }
}
