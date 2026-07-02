using System;
using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 科技研发管理器
/// </summary>
public partial class TechManager : Node
{
    // 已研发科技及进度
    public Dictionary<string, bool> ResearchedTech { get; private set; } = new();
    public Dictionary<string, float> ResearchProgress { get; private set; } = new(); // techId -> 已用人月

    private GameManager _gm;
    private EmployeeManager _empMgr;

    // 引擎商业化 — 委托给主引擎（_gm.Engines[0]），统一数据源
    private GameEngine PrimaryEngine => _gm?.Engines?.Count > 0 ? _gm.Engines[0] : null;
    public bool EngineOpenForLicense { get; set; } = true;
    public EngineBizModel EngineModel
    {
        get => PrimaryEngine?.BizModel ?? EngineBizModel.Closed;
        set { if (PrimaryEngine != null) PrimaryEngine.BizModel = value; }
    }
    public float RoyaltyRate
    {
        get => PrimaryEngine?.RoyaltyRate ?? 0.1f;
        set { if (PrimaryEngine != null) PrimaryEngine.RoyaltyRate = value; }
    }
    public float BuyoutPrice
    {
        get => PrimaryEngine?.BuyoutPrice ?? 500000f;
        set { if (PrimaryEngine != null) PrimaryEngine.BuyoutPrice = value; }
    }
    public float SubscriptionPrice
    {
        get => PrimaryEngine?.SubscriptionPrice ?? 20000f;
        set { if (PrimaryEngine != null) PrimaryEngine.SubscriptionPrice = value; }
    }
    public int EngineLicenseCount
    {
        get => PrimaryEngine?.LicenseCount ?? 0;
        set { if (PrimaryEngine != null) PrimaryEngine.LicenseCount = value; }
    }
    public float EngineMarketShare
    {
        get => PrimaryEngine?.MarketShare ?? 0;
        set { if (PrimaryEngine != null) PrimaryEngine.MarketShare = value; }
    }
    public float EngineReputation
    {
        get => PrimaryEngine?.Reputation ?? 0;
        set { if (PrimaryEngine != null) PrimaryEngine.Reputation = value; }
    }
    // ↑ 以上7个商业属性统一委托主引擎，不再在 TechManager / ResourceManager 中重复定义
    public List<string> EngineBugReports { get; private set; } = new(); // 待修复BUG
    public float EngineTechDebt { get; set; } // 引擎迭代累积的技术债务
    public int EngineGeneration { get; set; }  // 引擎代际（迭代次数）

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        _empMgr = GetNode<EmployeeManager>("../EmployeeManager");

        if (GlobalSettings.NewGame)
        {
            // 开局自带2D V1
            ResearchedTech["2d_v1"] = true;
        }
    }

    /// <summary>
    /// 是否已研发
    /// </summary>
    public bool IsResearched(string techId) =>
        ResearchedTech.TryGetValue(techId, out var v) && v;

    /// <summary>
    /// 获取某分支已研发的最高等级科技ID
    /// </summary>
    public string GetHighestInBranch(TechBranch branch)
    {
        string best = null;
        int bestLevel = -1;
        foreach (var kv in ResearchedTech)
        {
            if (kv.Value && TechTreeData.AllTech.TryGetValue(kv.Key, out var info))
            {
                if (info.Branch == branch && info.Level > bestLevel)
                {
                    best = kv.Key;
                    bestLevel = info.Level;
                }
            }
        }
        return best;
    }

    /// <summary>
    /// 检查是否可以研发
    /// </summary>
    public bool CanResearch(string techId, Team team)
    {
        if (!TechTreeData.AllTech.TryGetValue(techId, out var info)) return false;
        if (IsResearched(techId)) return false;
        if (!TechTreeData.PrerequisiteMet(info.Prerequisite, ResearchedTech)) return false;

        // 检查团队技能
        int priLevel = team.GetTotalSkillLevel(info.PrimarySkill);
        if (priLevel < info.PrimarySkillLevel) return false;

        if (info.SecondarySkill != null)
        {
            int secLevel = team.GetTotalSkillLevel(info.SecondarySkill.Value);
            if (secLevel < info.SecondarySkillLevel) return false;
        }

        return true;
    }

    /// <summary>
    /// 开始研发科技
    /// </summary>
    public bool StartResearch(string techId, Team team)
    {
        if (!CanResearch(techId, team)) return false;

        team.Task = TeamTask.ResearchTech;
        team.TargetTech = TechTreeData.AllTech[techId];

        if (!ResearchProgress.ContainsKey(techId))
            ResearchProgress[techId] = 0.01f; // 至少 >0 让 UI 立即显示

        return true;
    }

    /// <summary>
    /// 每月研发进度推进
    /// </summary>
    public void ProcessMonthlyResearch(Team team)
    {
        if (team.Task != TeamTask.ResearchTech || team.TargetTech == null) return;

        var tech = team.TargetTech.Value;
        string techId = tech.Id;

        if (!ResearchProgress.ContainsKey(techId))
            ResearchProgress[techId] = 0;

        // 计算研发速率
        float progress = CalculateResearchSpeed(team, tech);
        ResearchProgress[techId] += progress;

        // 检查是否完成
        if (ResearchProgress[techId] >= tech.RequiredManMonths)
        {
            CompleteResearch(techId, team);
        }
    }

    /// <summary>
    /// 计算研发速度（人月/月）
    /// </summary>
    private float CalculateResearchSpeed(Team team, TechInfo tech)
    {
        // 基础速度：主技能等级 × 0.5（1人月≈2个技能等级）
        float baseSpeed = team.GetTotalSkillLevel(tech.PrimarySkill) * 0.5f;

        // 次级技能加成
        if (tech.SecondarySkill != null)
            baseSpeed += team.GetTotalSkillLevel(tech.SecondarySkill.Value) * 0.25f;

        // 默契度加成
        baseSpeed *= (1 + team.GetChemistryBonus());

        // 队长加成
        if (team.Captain != null && team.Captain.CanMentor)
            baseSpeed *= 1.1f;

        // 首席架构师加成（降为1.3倍，避免瞬秒低等级科技）
        if (team.Members.Any(m => m.IsChiefArchitect))
            baseSpeed *= 1.3f;

        // 跨大类研发惩罚：如果团队完全没有匹配技能员工
        if (team.GetTotalSkillLevel(tech.PrimarySkill) == 0)
            baseSpeed *= 0.05f; // 几乎无法研发

        // 时代倍率
        baseSpeed *= _gm.EraResearchSpeedMul;

        // 创始人性格：技术宅 +15%
        baseSpeed *= 1f + _gm.Founder.GetResearchSpeedBonus();

        // 保底 0.3人月/月，防止永远无法完成
        return Mathf.Max(0.3f, baseSpeed);
    }

    /// <summary>
    /// 完成研发
    /// </summary>
    private void CompleteResearch(string techId, Team team)
    {
        ModAPI.FireHooks(ModAPI.GameHook.BeforeResearchComplete);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeResearchComplete)) return;

        ResearchedTech[techId] = true;

        // 灵感产出
        var res = _gm.GetNode<ResourceManager>("ResourceManager");
        res.GainInspiration(5 + Random.Shared.Next(10));

        // 团队
        team.Task = TeamTask.None;
        team.TargetTech = null;

        // 自动解锁高等级覆盖低等级功能
        if (TechTreeData.AllTech.TryGetValue(techId, out var info))
        {
            if (info.Level > 1)
            {
                string baseId = techId.Substring(0, techId.LastIndexOf('_'));
                for (int i = info.Level - 1; i >= 1; i--)
                {
                    string lowerId = $"{baseId}_v{i}";
                    if (TechTreeData.AllTech.ContainsKey(lowerId))
                        ResearchedTech[lowerId] = true;
                }
            }

            // 如果是引擎类科技，增加代际计数（但不弹窗，玩家手动升级引擎）
            bool isEngineTech = info.Branch == TechBranch.ProgramBase || info.Branch == TechBranch.Render2D || info.Branch == TechBranch.Render3D;
            if (isEngineTech)
                EngineGeneration++;
        }

        ModAPI.FireHooks(ModAPI.GameHook.AfterResearchComplete);
    }

    /// <summary>增量功能：引擎迭代时加债</summary>
    public void AddEngineFeature()
    {
        EngineTechDebt = Mathf.Min(100, EngineTechDebt + 8 + Random.Shared.Next(7));
    }

    /// <summary>彻底重构：引擎迭代时归零债务</summary>
    public void FullEngineRefactor()
    {
        EngineTechDebt = 0;
        var res = _gm.GetNode<ResourceManager>("ResourceManager");
        res.GainInspiration(10);
    }

    /// <summary>
    /// 设置引擎商业模式
    /// </summary>
    public void SetEngineModel(EngineBizModel model)
    {
        ModAPI.FireHooks(ModAPI.GameHook.BeforeEngineLicense);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeEngineLicense)) return;
        EngineModel = model; // 通过属性 setter → PrimaryEngine.BizModel
        _gm.GetNodeOrNull<TutorialManager>("TutorialManager")?.NotifyAction("engine_biz_set");

        switch (model)
        {
            case EngineBizModel.OpenSource:
                EngineReputation += 20;
                break;
            case EngineBizModel.Buyout:
                // 后续厂商购买时触发
                break;
            case EngineBizModel.Subscription:
                break;
            case EngineBizModel.Royalty:
                break;
        }
        ModAPI.FireHooks(ModAPI.GameHook.AfterEngineLicense);
    }

    /// <summary>
    /// 获取可用科技列表
    /// </summary>
    public List<TechInfo> GetAvailableTech(TechBranch branch)
    {
        var available = new List<TechInfo>();
        foreach (var info in TechTreeData.GetBranchTech(branch))
        {
            if (!IsResearched(info.Id) &&
                TechTreeData.PrerequisiteMet(info.Prerequisite, ResearchedTech))
                available.Add(info);
        }
        return available;
    }
}
