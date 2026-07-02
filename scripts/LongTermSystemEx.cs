using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class LongTermSystemEx : Node
{
    private GameManager _gm => Services.GameManager;
    private GameDevManager _devMgr => Services.GameDevManager;
    private FounderLegacy _legacySys => Services.GameManager?.GetNodeOrNull<FounderLegacy>("FounderLegacy");
    private EconomySystemEx _eco => Services.GameManager?.GetNodeOrNull<EconomySystemEx>("EconomySystemEx");

    public CompanyMission Mission { get; set; } = CompanyMission.GamerFirst;
    public EconomyPhase CurrentPhase { get; set; } = EconomyPhase.Recovery;
    public int PhaseMonths { get; set; }
    public OrgStage OrgStage { get; set; } = OrgStage.Flat;
    public FounderLife FounderLife { get; set; } = new();
    public List<OkrGoal> CurrentOKRs { get; set; } = new();
    public int LastOKRYear { get; set; } = 3;
    public float EmployerBrand { get; set; } = 0.5f;

    public void MonthlyTick()
    {
        ProcessEconomyPhase();
        ProcessOKRs();
        ProcessFounderLife();
        ProcessOrgUpgrade();
        ProcessCompanyCulture();

        // 年度颁奖（每年12月）
        if (_gm.MonthInYear == 12)
        {
            var market = Services.GameManager.GetNodeOrNull<MarketSystemEx>("MarketSystemEx");
            market?.HoldAwards(_gm.GameYear);
        }

        // 毕业季（每年6月）
        if (_gm.MonthInYear == 6)
        {
            var market = Services.GameManager.GetNodeOrNull<MarketSystemEx>("MarketSystemEx");
            market?.GenerateGraduateBatch();
        }
    }

    // ── 宏观经济 ──
    private void ProcessEconomyPhase()
    {
        PhaseMonths++;
        if (PhaseMonths > 36 + new Random().Next(24))
        {
            PhaseMonths = 0;
            CurrentPhase = (EconomyPhase)(((int)CurrentPhase + 1) % 4);
            _gm.ShowToast("📈", $"经济周期进入：{CurrentPhase}", Colors.Yellow);

            if (_eco != null)
            {
                _eco.MarketHeatMultiplier = CurrentPhase switch
                {
                    EconomyPhase.Boom => 1.5f,
                    EconomyPhase.Recession => 0.6f,
                    EconomyPhase.Depression => 0.4f,
                    EconomyPhase.Recovery => 0.9f,
                    _ => 1.0f
                };
                _eco.CostIndex = CurrentPhase switch
                {
                    EconomyPhase.Boom => 1.3f,
                    EconomyPhase.Recession => 0.8f,
                    _ => 1.0f
                };
            }
        }
    }

    // ── OKR目标系统 ──
    private void ProcessOKRs()
    {
        if (_gm.GameYear - LastOKRYear >= 2 && CurrentOKRs.Count == 0)
        {
            LastOKRYear = _gm.GameYear;
            CurrentOKRs = new List<OkrGoal>
            {
                new OkrGoal { Id = "okr_3games", Name = "快速迭代", Desc = "2年内发售3款游戏", Progress = 0 },
                new OkrGoal { Id = "okr_90avg", Name = "精品策略", Desc = "2年内平均评分≥85", Progress = 0 },
                new OkrGoal { Id = "okr_50m", Name = "营收冲刺", Desc = "2年内总收入≥5000万", Progress = 0 },
            };
            _gm.ShowToast("🎯", "新OKR目标已设定！", Colors.Cyan);
        }

        if (CurrentOKRs.Count > 0)
        {
            foreach (var goal in CurrentOKRs)
            {
                goal.Progress = goal.Id switch
                {
                    "okr_3games" => Mathf.Min(1, _devMgr.CompletedProjects.Count / 3f),
                    "okr_90avg" => _devMgr.CompletedProjects.Count > 0
                        ? _devMgr.CompletedProjects.Average(p => p.FinalScore) / 85f : 0,
                    "okr_50m" => Mathf.Min(1, _gm.ResMgr.TotalRevenue / 50_000_000f),
                    _ => goal.Progress
                };
                if (goal.Progress >= 1f && !goal.IsCompleted)
                {
                    goal.IsCompleted = true;
                    _gm.ShowToast("🎯", $"{goal.Name} 完成！", Colors.Gold);
                }
            }
            if (CurrentOKRs.Count > 0 && CurrentOKRs.All(g => g.IsCompleted))
            {
                CurrentOKRs.Clear();
                _gm.ShowToast("🏆", "所有 OKR 已完成！新的目标将在下个周期生成", Colors.Gold);
            }
        }
    }

    // ── 创始人生活 ──
    private void ProcessFounderLife()
    {
        FounderLife.Health = Mathf.Max(0, FounderLife.Health - 0.5f);
        FounderLife.Happiness = Mathf.Clamp(FounderLife.Happiness + 0.2f, 0, 100);
        FounderLife.FamilyRelation = Mathf.Max(0, FounderLife.FamilyRelation - 0.3f);
        FounderLife.SocialNetwork = Mathf.Min(100, FounderLife.SocialNetwork + 0.1f);

        if (FounderLife.Health < 20 && new Random().NextDouble() < 0.02f)
            _gm.ShowPopup("💔 健康警报", "您的健康状况堪忧，建议适当休息", Colors.Red);
    }

    // ── 组织架构 ──
    private void ProcessOrgUpgrade()
    {
        int empCount = Services.EmployeeManager.Employees.Count;
        OrgStage newStage = empCount switch
        {
            <= 20 => OrgStage.Flat,
            <= 80 => OrgStage.Departmental,
            <= 200 => OrgStage.Division,
            <= 500 => OrgStage.Matrix,
            _ => OrgStage.Conglomerate
        };

        if (newStage != OrgStage)
        {
            OrgStage = newStage;
            _gm.ShowToast("🏢", $"公司组织架构升级至：{OrgStage}", Colors.Green);
        }
    }

    // ── 公司文化 ──
    private void ProcessCompanyCulture()
    {
        EmployerBrand = Mathf.Clamp(EmployerBrand + 0.002f, 0, 1);
        var avgScore = _devMgr.CompletedProjects.Count > 0
            ? _devMgr.CompletedProjects.Average(p => p.FinalScore) : 0;
        if (avgScore > 80) EmployerBrand = Mathf.Min(1, EmployerBrand + 0.005f);
    }

    // ── 公司博物馆数据 ──
    public List<MuseumSnapshot> MuseumData { get; set; } = new();

    public void RecordMuseumSnapshot()
    {
        MuseumData.Add(new MuseumSnapshot
        {
            Month = _gm.GameMonth,
            Money = _gm.ResMgr.Money,
            BestScore = _devMgr.CompletedProjects.Count > 0 ? _devMgr.CompletedProjects.Max(p => p.FinalScore) : 0,
            EmployeeCount = Services.EmployeeManager.Employees.Count,
            ProjectCount = _devMgr.CompletedProjects.Count,
        });
    }

    public class MuseumSnapshot
    {
        public int Month;
        public float Money;
        public float BestScore;
        public int EmployeeCount;
        public int ProjectCount;
    }

    // ── 新游戏+ ──
    public void PrepareNewGamePlus()
    {
        GameSettingsEx.NgPlusLevel++;
        _gm.ShowPopup("✨ 新游戏+", $"第{GameSettingsEx.NgPlusLevel}周目开启！继承部分资产开始新旅程", Colors.Gold);
    }
}
