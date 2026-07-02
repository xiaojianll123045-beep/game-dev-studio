using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>冲刺任务项</summary>
public class SprintTask
{
    public string Key;
    public string NameKey;
    public string DescKey;
    public int StoryPoints;           // 消耗点数
    public int QualityScore;          // 专注此任务时品质收益
    public int BugRisk;               // bug 风险
    public float ProgressGain;        // 进度推进
    public string AffectedModule;     // 影响的模块名
    public bool IsUrgent;             // 是否紧急（必须在本冲刺完成）
    public string PrerequisiteKey;    // 前置任务key
    public string Category;           // "core", "content", "tech", "polish"
}

public static class SprintDefinitions
{
    public static List<SprintTask> AllTasks = new()
    {
        // ── 核心玩法 ──
        new SprintTask { Key="core_combat", NameKey="sprint.core_combat", DescKey="sprint.core_combat_desc", StoryPoints=3, QualityScore=8, BugRisk=3, ProgressGain=0.08f, AffectedModule="Gameplay", Category="core" },
        new SprintTask { Key="core_levels", NameKey="sprint.core_levels", DescKey="sprint.core_levels_desc", StoryPoints=3, QualityScore=6, BugRisk=2, ProgressGain=0.06f, AffectedModule="Gameplay", Category="core" },
        new SprintTask { Key="core_story", NameKey="sprint.core_story", DescKey="sprint.core_story_desc", StoryPoints=2, QualityScore=7, BugRisk=1, ProgressGain=0.04f, AffectedModule="Story", Category="core" },
        new SprintTask { Key="core_progression", NameKey="sprint.core_progression", DescKey="sprint.core_progression_desc", StoryPoints=2, QualityScore=5, BugRisk=2, ProgressGain=0.05f, AffectedModule="Gameplay", Category="core" },

        // ── 内容 ──
        new SprintTask { Key="content_enemies", NameKey="sprint.content_enemies", DescKey="sprint.content_enemies_desc", StoryPoints=2, QualityScore=4, BugRisk=1, ProgressGain=0.04f, AffectedModule="Gameplay", Category="content" },
        new SprintTask { Key="content_items", NameKey="sprint.content_items", DescKey="sprint.content_items_desc", StoryPoints=2, QualityScore=3, BugRisk=1, ProgressGain=0.03f, AffectedModule="Gameplay", Category="content" },
        new SprintTask { Key="content_music", NameKey="sprint.content_music", DescKey="sprint.content_music_desc", StoryPoints=1, QualityScore=5, BugRisk=0, ProgressGain=0.02f, AffectedModule="Audio", Category="content" },
        new SprintTask { Key="content_art", NameKey="sprint.content_art", DescKey="sprint.content_art_desc", StoryPoints=2, QualityScore=5, BugRisk=0, ProgressGain=0.03f, AffectedModule="Graphics", Category="content" },

        // ── 技术 ──
        new SprintTask { Key="tech_perf", NameKey="sprint.tech_perf", DescKey="sprint.tech_perf_desc", StoryPoints=2, QualityScore=2, BugRisk=3, ProgressGain=0.02f, AffectedModule="Stability", Category="tech" },
        new SprintTask { Key="tech_loading", NameKey="sprint.tech_loading", DescKey="sprint.tech_loading_desc", StoryPoints=1, QualityScore=1, BugRisk=2, ProgressGain=0.01f, AffectedModule="Stability", Category="tech" },
        new SprintTask { Key="tech_network", NameKey="sprint.tech_network", DescKey="sprint.tech_network_desc", StoryPoints=3, QualityScore=3, BugRisk=5, ProgressGain=0.03f, AffectedModule="Network", Category="tech" },
        new SprintTask { Key="tech_save", NameKey="sprint.tech_save", DescKey="sprint.tech_save_desc", StoryPoints=1, QualityScore=1, BugRisk=1, ProgressGain=0.01f, AffectedModule="Stability", Category="tech" },

        // ── 打磨 ──
        new SprintTask { Key="polish_bug", NameKey="sprint.polish_bug", DescKey="sprint.polish_bug_desc", StoryPoints=2, QualityScore=0, BugRisk=-5, ProgressGain=0, AffectedModule="Stability", Category="polish" },
        new SprintTask { Key="polish_ui", NameKey="sprint.polish_ui", DescKey="sprint.polish_ui_desc", StoryPoints=1, QualityScore=3, BugRisk=0, ProgressGain=0, AffectedModule="Graphics", Category="polish" },
        new SprintTask { Key="polish_tutorial", NameKey="sprint.polish_tutorial", DescKey="sprint.polish_tutorial_desc", StoryPoints=1, QualityScore=4, BugRisk=0, ProgressGain=0, AffectedModule="Story", Category="polish" },
        new SprintTask { Key="polish_accessibility", NameKey="sprint.polish_accessibility", DescKey="sprint.polish_accessibility_desc", StoryPoints=2, QualityScore=2, BugRisk=1, ProgressGain=0, AffectedModule="Stability", Category="polish" },
    };
}

/// <summary>冲刺规划系统</summary>
public partial class SprintSystem : Node
{
    private GameManager _gm;
    private GameDevManager _devMgr;
    private ResourceManager _res;

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        _devMgr = _gm.GetNode<GameDevManager>("GameDevManager");
        _res = _gm.GetNode<ResourceManager>("ResourceManager");
    }

    /// <summary>获取团队的可用点数（编程+美术效率综合）</summary>
    public int GetAvailablePoints(Team team)
    {
        if (team?.CurrentProject == null) return 0;
        float progSkill = team.GetTotalSkillLevel(SkillType.Program);
        float artSkill = team.GetTotalSkillLevel(SkillType.Art);
        float basePts = 4 + (progSkill + artSkill) * 0.5f;
        // 项目规模影响
        float scaleFactor = 0.8f + team.CurrentProject.Scale * 0.4f;
        return Mathf.Max(1, (int)(basePts * scaleFactor));
    }

    /// <summary>执行冲刺——按选中的任务推进项目</summary>
    public void ExecuteSprint(Team team, List<SprintTask> selectedTasks)
    {
        ModAPI.FireHooks(ModAPI.GameHook.BeforeSprint);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeSprint)) return;

        var proj = team.CurrentProject;
        if (proj == null) return;

        int totalPts = selectedTasks.Sum(t => t.StoryPoints);
        int available = GetAvailablePoints(team);
        if (totalPts > available) return; // 超额

        foreach (var task in selectedTasks)
        {
            ApplyTask(proj, task);
        }

        proj.DevLog.Add($"[冲刺] 完成 {selectedTasks.Count} 项任务，进度 {proj.DevProgress:F1}%");

        // 标记本轮冲刺完成
        proj.MonthsSpent += 3;
        proj.LastSprintMonth = proj.MonthsSpent;
        proj.DevProgress = Mathf.Min(1f, proj.DevProgress + selectedTasks.Sum(t => t.ProgressGain));

        // 检查是否进入下一阶段
        if (proj.DevProgress >= 1f && proj.Phase == DevPhase.Developing)
        {
            proj.Phase = DevPhase.Polishing;
            _gm.ShowToast("✅", Loc.TrF("toast.dev_complete", proj.Name), new Color(0.3f, 0.8f, 0.3f));
        }

        ModAPI.FireHooks(ModAPI.GameHook.AfterSprint);
    }

    private void ApplyTask(GameProject proj, SprintTask task)
    {
        switch (task.AffectedModule)
        {
            case "Gameplay": proj.GameplayScore = Mathf.Min(100, proj.GameplayScore + task.QualityScore * 0.5f); break;
            case "Graphics": proj.GraphicsScore = Mathf.Min(100, proj.GraphicsScore + task.QualityScore * 0.5f); break;
            case "Audio": proj.AudioScore = Mathf.Min(100, proj.AudioScore + task.QualityScore * 0.5f); break;
            case "Story": proj.StoryScore = Mathf.Min(100, proj.StoryScore + task.QualityScore * 0.5f); break;
            case "Network": proj.NetworkScore = Mathf.Min(100, proj.NetworkScore + task.QualityScore * 0.5f); break;
            case "Stability": proj.StabilityScore = Mathf.Min(100, proj.StabilityScore + task.QualityScore * 0.5f); break;
        }
        proj.BugCount = Mathf.Max(0, proj.BugCount + task.BugRisk);
    }

    /// <summary>是否该进行冲刺规划了（每 3 个月）</summary>
    public bool ShouldPlanSprint(Team team)
    {
        var proj = team?.CurrentProject;
        return proj != null && proj.Phase == DevPhase.Developing && (proj.MonthsSpent - proj.LastSprintMonth >= 3) && proj.DevProgress < 1f;
    }

    /// <summary>获取紧急任务（当前项目需要关注的问题）</summary>
    public List<SprintTask> GetUrgentTasks(GameProject proj)
    {
        var urgent = new List<SprintTask>();
        if (proj.BugCount > 20) urgent.Add(SprintDefinitions.AllTasks.Find(t => t.Key == "polish_bug"));
        if (proj.MemoryUsage > proj.PlatformMemoryLimit * 0.8f) urgent.Add(SprintDefinitions.AllTasks.Find(t => t.Key == "tech_perf"));
        return urgent;
    }
}
