using System;
using System.Linq;
using Godot;

/// <summary>
/// 四维资源管理器：资金、灵感、技术债务、人力时间
/// </summary>
public partial class ResourceManager : Node
{
    // ===== 资金 =====
    public float Money { get; set; } = 500000f;          // 资金¥
    public float MonthlyIncome { get; set; }              // 本月收入
    public float MonthlyExpense { get; set; }             // 本月支出
    public float TotalRevenue { get; set; }               // 累计总收入

    // ===== 灵感 =====
    public float Inspiration { get; set; } = 30f;         // 当前灵感
    public float MaxInspiration { get; set; } = 100f;     // 灵感上限

    // ===== 技术债务 =====
    public float TechDebt { get; set; }                   // 技术债务 0~100
    public float MaxTechDebt { get; set; } = 100f;

    // ===== 人力相关（在GameManager中计算） =====
    public int TotalEmployees { get; set; }               // 总员工数

    // ===== 开销明细 =====
    public float SalaryExpense { get; set; }              // 工资
    public float RentExpense { get; set; } = 5000f;       // 场地租金
    public float MarketingExpense { get; set; }           // 宣发（一次性）
    public float OutsourceExpense { get; set; }           // 外包支出
    public float MonthlyMarketingBudget { get; set; }     // 月度营销投放预算（玩家可调）
    public bool HasLegalInsurance { get; set; }           // 法务顾问（5万/年）
    public bool HasCyberInsurance { get; set; }           // 网络安全（3万/年）
    public int InsuranceExpiryMonth { get; set; }         // 保险到期月份

    // ===== 引擎收入（汇总记账用，由 TechManager.CompetitorAI 设置主引擎引擎属性） =====
    public float EngineIncome { get; set; }               // 引擎授权累计收入

    // ===== 月结算系统 =====
    private int _currentMonth;
    private GameManager _gm;

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        if (GlobalSettings.NewGame)
        {
            Money = GlobalSettings.StartingMoney;
            Inspiration = GlobalSettings.StartingInspiration;
            MaxInspiration = GlobalSettings.MaxInspiration;
        }
    }

    /// <summary>
    /// 消耗资金（只能用于4项：工资/租金/宣发/外包）
    /// </summary>
    public bool SpendMoney(float amount, string category)
    {
        if (Money >= amount)
        {
            Money -= amount;
            Services.AchievementManager?.CheckNow();
            MonthlyExpense += amount;
            switch (category)
            {
                case "salary": SalaryExpense += amount; break;
                case "rent": RentExpense += amount; break;
                case "marketing": MarketingExpense += amount; break;
                case "ongoing_marketing": MarketingExpense += amount; break;
                case "outsource": OutsourceExpense += amount; break;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获得资金
    /// </summary>
    public void EarnMoney(float amount, string source)
    {
        Money += amount;
        Services.AchievementManager?.CheckNow();
        MonthlyIncome += amount;
        TotalRevenue += amount;

        switch (source)
        {
            case "game_sales": break;
            case "outsource": break;
            case "engine": EngineIncome += amount; break;
        }
    }

    /// <summary>
    /// 消耗灵感
    /// </summary>
    public bool SpendInspiration(float amount)
    {
        if (Inspiration >= amount)
        {
            Inspiration -= amount;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获得灵感（上限100）
    /// </summary>
    public void GainInspiration(float amount)
    {
        Inspiration = Mathf.Min(MaxInspiration, Inspiration + amount);
    }

    /// <summary>
    /// 增加技术债务
    /// </summary>
    public void AddTechDebt(float amount)
    {
        TechDebt = Mathf.Min(MaxTechDebt, TechDebt + amount);
    }

    /// <summary>
    /// 减少技术债务
    /// </summary>
    public void ReduceTechDebt(float amount)
    {
        TechDebt = Mathf.Max(0, TechDebt - amount);
    }

    /// <summary>
    /// 月度结算
    /// </summary>
    public void MonthEndSettle()
    {
        _currentMonth++;
        // 重置月度统计
        MonthlyIncome = 0;
        MonthlyExpense = SalaryExpense + RentExpense + MarketingExpense + OutsourceExpense;
        SalaryExpense = 0;
        RentExpense = 5000f;
        MarketingExpense = 0;
        OutsourceExpense = 0;

        // ── 月度营销投放：每¥1万 ≈ 获取玩家选择的粉丝量 ──
        if (MonthlyMarketingBudget > 0)
        {
            SpendMoney((long)MonthlyMarketingBudget, "ongoing_marketing");
            var fanMgr = _gm.GetNodeOrNull<FanManager>("FanManager");
            var devMgr = _gm.GetNodeOrNull<GameDevManager>("GameDevManager");
            if (fanMgr != null)
            {
                // ── 边际效应递减：粉丝越多，获取新粉成本越高 ──
                float costPerFan = 500f * (1f + fanMgr.TotalFans / 500000f);
                int fansGained = (int)(MonthlyMarketingBudget / costPerFan);
                fanMgr.CasualFans += fansGained;
            }
            if (devMgr != null)
            {
                // 老游戏年轻化最多延长12个月
                foreach (var p in devMgr.Projects.Where(pp => pp.IsReleased && pp.MonthsOnMarket > 6 && pp.MonthsOnMarket <= pp.MonthsOnMarket + 12))
                {
                    p.MonthsOnMarket = Mathf.Max(6, p.MonthsOnMarket - 1);
                }
            }
        }

        // ── 保险到期前1个月Toast提醒 ──
        if (InsuranceExpiryMonth > 0)
        {
            int monthsLeft = InsuranceExpiryMonth - _gm.GameMonth;
            if (monthsLeft == 1)
            {
                string which = HasLegalInsurance ? "法务顾问" : HasCyberInsurance ? "网络安全" : "保险";
                _gm.ShowToast("⚠ 保险即将到期", $"{which}将在下月到期，请及时续费", new Color(0.9f, 0.6f, 0.2f), 4f);
            }
        }

        // ── 保险续费检查 ──
        if (_gm.GameMonth > InsuranceExpiryMonth)
        {
            if (HasLegalInsurance || HasCyberInsurance)
                _gm.ShowToast("保险已到期", "法务/网络安全保护已失效，请尽快续费", new Color(0.9f, 0.3f, 0.2f), 5f);
            HasLegalInsurance = false;
            HasCyberInsurance = false;
        }

        // 引擎订阅收入
        var primaryEngine = _gm.Engines.Count > 0 ? _gm.Engines[0] : null;
        if (primaryEngine != null && primaryEngine.BizModel == EngineBizModel.Subscription && primaryEngine.LicenseCount > 0)
        {
            EarnMoney(primaryEngine.SubscriptionPrice * primaryEngine.LicenseCount, "engine");
        }
    }

    // ══════════════════ 保险订阅 ══════════════════
    public bool BuyLegalInsurance()
    {
        if (HasLegalInsurance) return false;
        if (SpendMoney(50000, "insurance")) { HasLegalInsurance = true; InsuranceExpiryMonth = _gm.GameMonth + 12; return true; }
        return false;
    }
    public bool BuyCyberInsurance()
    {
        if (HasCyberInsurance) return false;
        if (SpendMoney(30000, "insurance")) { HasCyberInsurance = true; InsuranceExpiryMonth = _gm.GameMonth + 12; return true; }
        return false;
    }

    // ══════════════════ 引擎生态投资 ══════════════════
    public bool HostTechSummit(GameEngine engine)
    {
        if (!SpendMoney(200000, "engine_eco")) return false;
        engine.Reputation = Mathf.Min(100, engine.Reputation + 12);
        // 吸引 AI 工作室购买
        var compAI = _gm.GetNodeOrNull<CompetitorAI>("CompetitorAI");
        if (compAI != null)
        {
            int newLicenses = 5 + Random.Shared.Next(6);
            engine.LicenseCount += newLicenses;
            _gm.ShowToast("技术峰会举办成功", $"引擎声誉+12\n吸引了{newLicenses}个新授权用户", new Color(0.3f, 0.6f, 0.9f));
        }
        return true;
    }

    public bool FundIndieGame(GameEngine engine)
    {
        if (!SpendMoney(100000, "engine_eco")) return false;
        var compAI = _gm.GetNodeOrNull<CompetitorAI>("CompetitorAI");
        if (compAI != null)
        {
            var pool = compAI.Studios.Where(s => !s.IsAcquired).ToList();
            if (pool.Count > 0)
            {
                var studio = pool[Random.Shared.Next(pool.Count)];
                studio.Money += 500000; // 注资
                studio.FundedByPlayer = true;
                studio.FundedMonth = _gm.GameMonth;
                engine.LicenseCount++;
                _gm.ShowToast("独立游戏基金", $"资助了{studio.Name}\n该工作室获得¥500,000资金", new Color(0.5f, 0.7f, 0.3f));
            }
        }
        return true;
    }

    // ══════════════════ 员工培训团建 ══════════════════
    public bool TrainEmployee(Employee emp)
    {
        if (!SpendMoney(30000, "training")) return false;
        // 找最高技能加经验
        if (emp.Skills.Count > 0)
        {
            var topSkill = emp.Skills.OrderByDescending(kv => kv.Value.Level).First();
            emp.AddExp(topSkill.Key, 30, false);
        }
        emp.Fatigue = Mathf.Max(0, emp.Fatigue - 10);
        emp.Satisfaction = Mathf.Min(100, emp.Satisfaction + 5);
        emp.TrainingLeaveMonths = 1; // 当月休假培训，不参与开发
        emp.LastTrainAbsoluteMonth = _gm.GameMonth + _gm.GameYear * 12;
        return true;
    }

    public bool TeamBuilding(Team team)
    {
        if (!SpendMoney(20000, "teambuilding")) return false;
        foreach (var emp in team.Members)
        {
            emp.Fatigue = Mathf.Max(0, emp.Fatigue - 20);
            emp.Satisfaction = Mathf.Min(100, emp.Satisfaction + 10);
        }
        return true;
    }

    // ══════════════════ 生态投资反馈 ══════════════════
    public void ShowEcoSuccess(string studioName, string gameName, float score)
    {
        var engine = _gm.Engines.Count > 0 ? _gm.Engines[0] : null;
        if (engine != null)
        {
            engine.Reputation = Mathf.Min(100, engine.Reputation + 5);
            engine.LicenseCount += 3;
        }
        _gm.ShowToast("🌱 生态繁荣！", $"你资助的{studioName}大获成功！\n《{gameName}》评分{score:F0}分\n引擎声誉+5, +3授权", new Color(0.3f, 0.8f, 0.5f), 6f);
    }
    public void ShowEcoFailure(string studioName)
    {
        var engine = _gm.Engines.Count > 0 ? _gm.Engines[0] : null;
        if (engine != null) engine.Reputation = Mathf.Max(10, engine.Reputation - 2);
        _gm.ShowToast("📉 投资失利", $"{studioName}的新作表现不佳\n你的投资眼光被市场质疑\n引擎声誉-2", new Color(0.8f, 0.4f, 0.3f), 5f);
    }
}
