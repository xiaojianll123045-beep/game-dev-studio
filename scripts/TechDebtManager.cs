using Godot;
using System;
using System.Linq;

/// <summary>
/// 技术债务系统管理器
/// 模拟"为了赶进度写烂代码，未来必须偿还"的现实困境
/// </summary>
public partial class TechDebtManager : Node
{
    // ── 债务阶段 ──
    public enum DebtTier { Healthy, Warning, Dangerous, Critical }

    // ── 状态 ──
    public bool HasCrashed { get; set; }       // 是否触发过屎山崩溃
    public int CrashRecoveryMonths { get; set; } // 崩溃后强制重构剩余月数

    // ── 996 冲刺 ──
    public bool CrunchMode { get; set; }
    public int ActiveCrunchMonths { get; set; }
    public bool LeverageUnlocked { get; set; }      // 债务>50时开放杠杆
    public bool LeverageNotified { get; set; }      // 已弹出杠杆通知

    // ── 引用 ──
    private ResourceManager _res;
    private EmployeeManager _empMgr;
    private GameDevManager _devMgr;
    private GameManager _gm;
    private bool _warned80;

    // ── 静态辅助 ──
    public static DebtTier GetTier(float debt)
    {
        if (debt <= 30) return DebtTier.Healthy;
        if (debt <= 60) return DebtTier.Warning;
        if (debt <= 80) return DebtTier.Dangerous;
        return DebtTier.Critical;
    }

    public override void _Ready()
    {
        _res = GetNode<ResourceManager>("../ResourceManager");
        _empMgr = GetNode<EmployeeManager>("../EmployeeManager");
        _devMgr = GetNode<GameDevManager>("../GameDevManager");
        _gm = GetNode<GameManager>("..");
    }

    // Lazy fallback in case other managers are accessed before _Ready
    private ResourceManager Res => _res ??= GetNode<ResourceManager>("../ResourceManager");
    private EmployeeManager EmpMgr => _empMgr ??= GetNode<EmployeeManager>("../EmployeeManager");
    private GameManager GM => _gm ??= GetNode<GameManager>("..");

    // ═══════════════════════════════════════════
    // 每月固定债务累积
    // ═══════════════════════════════════════════

    /// <summary>每月检查（杠杆开放 + 冲刺效果）</summary>
    public void MonthlyTick()
    {
        if (CrashRecoveryMonths > 0) { CrashRecoveryMonths--; return; }

        float total = ComputeTotalDebt();

        // ── 杠杆开放通知 ──
        if (total > 50 && !LeverageNotified)
        {
            LeverageUnlocked = true;
            LeverageNotified = true;
            GM.ShowPopup(
                Loc.Tr("debt.leverage_title"),
                Loc.Tr("debt.leverage_msg"),
                new Color(1f, 0.5f, 0.15f)
            );
        }
        if (total <= 50) { LeverageUnlocked = false; LeverageNotified = false; }

        // ── 996冲刺每月效果 ──
        if (CrunchMode && LeverageUnlocked)
        {
            ActiveCrunchMonths++;
            // 冲刺加速债务（×1.5，合理杠杆而非自杀）
            foreach (var p in _devMgr.Projects.Where(p => p.Phase == DevPhase.Developing))
                p.TechDebt = Mathf.Min(100, p.TechDebt + 2.5f);
            // 员工满意度下降
            foreach (var e in EmpMgr.Employees)
                e.Satisfaction = Mathf.Max(0, e.Satisfaction - 3f);
        }
        else if (!CrunchMode)
        {
            ActiveCrunchMonths = 0;
        }

        CheckThresholds();
    }

    public void CheckThresholds()
    {
        float total = ComputeTotalDebt();

        // ── 满 100 崩溃事件 ──
        if (total >= 100 && !HasCrashed)
            TriggerDebtCrash();

        // ── 阈值 80 警告 ──
        if (total >= 80 && !_warned80)
        {
            _warned80 = true;
            GM.ShowPopup(
                Loc.TrF("debt.warn_title", total),
                Loc.Tr("debt.warn_msg"),
                new Color(1f, 0.5f, 0.15f)
            );
        }
        if (total < 80) _warned80 = false;
    }

    /// <summary>计算全局总债务 = 所有引擎债务 + 所有项目债务</summary>
    public float ComputeTotalDebt()
    {
        float total = 0;
        var devMgr = GM.GetNode<GameDevManager>("GameDevManager");
        // 引擎债务
        foreach (var eng in GM.Engines) total += eng.TechDebt;
        // 项目债务
        if (devMgr != null)
        {
            foreach (var p in devMgr.Projects) total += p.TechDebt;
            foreach (var p in devMgr.CompletedProjects) total += p.TechDebt;
        }
        return Mathf.Min(total, 100);
    }

    /// <summary>计算单个引擎的债务</summary>
    public float ComputeEngineDebt(string engineName)
    {
        var eng = GM.Engines.Find(e => e.Name == engineName);
        return eng?.TechDebt ?? 0;
    }

    // ═══════════════════════════════════════════
    // 触发事件
    // ═══════════════════════════════════════════

    private void TriggerDebtCrash()
    {
        HasCrashed = true;
        CrashRecoveryMonths = 3;

        GM.ShowPopup(
            Loc.Tr("debt.collapse_title"),
            Loc.Tr("debt.collapse_msg"),
            new Color(1f, 0.2f, 0.2f)
        );
    }

    // ═══════════════════════════════════════════
    // 后果查询（供其他系统调用）
    // ═══════════════════════════════════════════

    /// <summary>BUG 生成速率倍率</summary>
    public float BugRateMultiplier
    {
        get
        {
            float total = ComputeTotalDebt();
            float baseMult = 1f;
            var tier = GetTier(total);
            switch (tier)
            {
                case DebtTier.Warning: baseMult = 1.2f; break;
                case DebtTier.Dangerous: baseMult = 1.5f; break;
                case DebtTier.Critical: baseMult = 2.0f; break;
            }
            if (HasCrashed) baseMult += 0.3f;
            return baseMult;
        }
    }

    public float DevSpeedPenalty
    {
        get
        {
            var tier = GetTier(ComputeTotalDebt());
            switch (tier)
            {
                case DebtTier.Warning: return -0.05f;
                case DebtTier.Dangerous: return -0.15f;
                case DebtTier.Critical: return -0.30f;
                default: return 0;
            }
        }
    }

    public float InspirationMultiplier
    {
        get
        {
            float mult = 1f;
            float total = ComputeTotalDebt();
            if (total > 80) mult -= 0.5f;
            return mult;
        }
    }

    public float TrainingTimeMultiplier
    {
        get
        {
            var tier = GetTier(ComputeTotalDebt());
            return tier switch
            {
                DebtTier.Dangerous => 1.5f,
                DebtTier.Critical => 1.5f,
                _ => 1f
            };
        }
    }

    public float FatiguePerMonth
    {
        get
        {
            var tier = GetTier(ComputeTotalDebt());
            return tier == DebtTier.Critical ? 5f : 0;
        }
    }

    /// <summary>完全重构 — 清除引擎债务和所有项目债务</summary>
    public void FullRefactor(Team team)
    {
        int months = team.GetTotalSkillLevel(SkillType.Program) >= 15 ? 1 :
                     team.GetTotalSkillLevel(SkillType.Program) >= 10 ? 2 : 3;

        // 清除所有引擎债务
        foreach (var eng in GM.Engines) eng.TechDebt = 0;

        // 清除所有项目债务
        var devMgr = GM.GetNode<GameDevManager>("GameDevManager");
        if (devMgr != null)
        {
            foreach (var p in devMgr.Projects) p.TechDebt = 0;
            foreach (var p in devMgr.CompletedProjects) p.TechDebt = 0;
        }

        HasCrashed = false;
        CrashRecoveryMonths = 0;
        team.Task = TeamTask.None;
        team.CurrentProject = null;

        GM.ShowPopup(
            Loc.Tr("debt.refactor_done_title"),
            Loc.TrF("debt.refactor_done_msg", months),
            new Color(0.3f, 0.8f, 1f)
        );
    }

    /// <summary>复用前作代码（续作时调用）— 减30%开发时间，增加项目债务 15~25</summary>
    public void ApplyCodeReuse(GameProject proj)
    {
        float add = 15 + (float)new Random().NextDouble() * 10;
        proj.TechDebt += add;
        proj.EstimatedMonths *= 0.7f; // 30% 更快
        proj.DevLog.Add($"复用前作代码，开发工时-30%，技术债务 +{add:F0}");
    }
}
