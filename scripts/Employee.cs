using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 单名员工
/// </summary>
public partial class Employee
{
    public string Name { get; set; }
    public int Id { get; set; }
    public float Salary { get; set; } = 8000f;          // 月薪

    // 五维技能等级 Lv0~Lv5
    public Dictionary<SkillType, SkillLevelInfo> Skills { get; set; } = new();

    // 职业生涯
    public int MonthsEmployed { get; set; }              // 入职月数
    public int ProjectsCompleted { get; set; }           // 参与完成项目数
    public int HighScoreProjects { get; set; }           // 参与评分≥85项目数

    // 特殊能力
    public bool CanMentor => GetHighestLevel() >= 3;     // Lv3可带新人
    public bool CanWritePaper => GetHighestLevel() >= 4; // Lv4可写白皮书
    public bool IsGrandmaster => GetHighestLevel() >= 5; // Lv5大宗师
    public bool IsHardwareEngineer { get; set; }         // 已转职硬件工程师
    public bool IsChiefArchitect { get; set; }           // 已转职首席架构师

    // 培训
    public bool TrainingThisYear { get; set; }           // 今年已培训
    public int LastTrainAbsoluteMonth { get; set; }       // 上次培训的绝对月份（GameMonth + GameYear*12）
    public bool HadMentorEvent { get; set; }             // 已触发名师指导

    // 队长
    public bool IsCaptain { get; set; }
    public string TeamName { get; set; }

    // 疲劳
    public float Fatigue { get; set; }                  // 倦怠度 0~100

    // 满意度 0~100（低于30触发离职风险，高于80效率+10%）
    public float Satisfaction { get; set; } = 70f;

    // 性格深度
    public string Nickname { get; set; }                // 绰号（事件赋予）
    public List<string> Memories { get; set; } = new(); // 职业生涯重要事件
    public int SickDays { get; set; }                   // 本月病假
    public bool OnVacation { get; set; }                // 休假中
    public int TrainingLeaveMonths { get; set; }         // 外出培训剩余月数（0=正常）
    public int CompanyYears => MonthsEmployed / 12;

    // 开发贡献（用于UI显示"脑袋上冒球"）
    public float LastProgContrib { get; set; }          // 上次程序分贡献
    public float LastArtContrib { get; set; }            // 上次美术分贡献

    // 人际关系
    public HashSet<int> Friends { get; set; } = new();  // 好友ID列表
    public HashSet<int> Rivals { get; set; } = new();   // 竞争对手ID列表

    // ── 恩怨系统 ──
    public bool HoldingGrudge { get; set; }
    public int GrudgeTargetId { get; set; } = -1;
    public bool IsGrudgeTarget { get; set; }                     // 被人怀恨（防止自动离职断开恩怨链）
    public List<string> MemoryLog { get; set; } = new();  // 重大事件记忆

    public void RecordMemory(string msg) { MemoryLog.Add($"[{MemoryLog.Count + 1}] {msg}"); }
    public bool HoldsGrudgeAgainst(int id) => HoldingGrudge && GrudgeTargetId == id;

    // ── 野心/忠诚系统（王牌制作人）──
    /// <summary>野心 0~100，随成功项目膨胀</summary>
    public float Ambition { get; set; } = 10f;
    /// <summary>忠诚 0~100，受待遇和事件影响</summary>
    public float Loyalty { get; set; } = 60f;
    /// <summary>是否正在考虑对手挖角</summary>
    public bool ConsideringOffer { get; set; }
    /// <summary>对手开价</summary>
    public float OfferAmount { get; set; }
    /// <summary>挖角倒计时（月）</summary>
    public int OfferCountdown { get; set; }
    /// <summary>目标公司名</summary>
    public string TargetCompanyName { get; set; }
    /// <summary>连续成功项目数</summary>
    public int ConsecutiveHits { get; set; }
    /// <summary>是否王牌制作人（连续3款高分）</summary>
    public bool IsLegendary { get; set; }

    // 特质
    public EmployeeTrait Trait { get; set; } = EmployeeTrait.None;
    public string GetTraitsDesc() => Trait switch
    {
        EmployeeTrait.Workaholic => Loc.Tr("emp.trait_workaholic"),
        EmployeeTrait.Social => Loc.Tr("emp.trait_social"),
        EmployeeTrait.Sensitive => Loc.Tr("emp.trait_sensitive"),
        EmployeeTrait.Genius => Loc.Tr("emp.trait_genius"),
        EmployeeTrait.Mentor => Loc.Tr("emp.trait_mentor"),
        EmployeeTrait.LoneWolf => Loc.Tr("emp.trait_lonewolf"),
        EmployeeTrait.Perfectionist => Loc.Tr("emp.trait_perfectionist"),
        EmployeeTrait.Chill => Loc.Tr("emp.trait_chill"),
        EmployeeTrait.Ambitious => Loc.Tr("emp.trait_ambitious"),
        EmployeeTrait.Nostalgic => Loc.Tr("emp.trait_nostalgic"),
        EmployeeTrait.TechClean => Loc.Tr("emp.trait_techclean"),
        EmployeeTrait.Lucky => Loc.Tr("emp.trait_lucky"),
        _ => ""
    };

    /// <summary>
    /// 根据特质调整效率
    /// </summary>
    public float GetTraitEfficiencyMod()
    {
        float def = Trait switch
        {
            EmployeeTrait.Workaholic => 1.1f,
            EmployeeTrait.LoneWolf => 1.2f,
            EmployeeTrait.Chill => 0.9f,
            EmployeeTrait.Perfectionist => 1.05f,
            _ => 1.0f
        };
        return BalanceModDB.Get($"trait.{Trait.ToString().ToLower()}.efficiency", def);
    }

    /// <summary>
    /// 根据特质调整疲劳增速
    /// </summary>
    public float GetTraitFatigueMod()
    {
        float def = Trait switch
        {
            EmployeeTrait.Workaholic => 0.5f,
            EmployeeTrait.Chill => 0.7f,
            EmployeeTrait.Ambitious => 1.2f,
            _ => 1.0f
        };
        return BalanceModDB.Get($"trait.{Trait.ToString().ToLower()}.fatigue", def);
    }

    /// <summary>
    /// 满意度对效率的加成（≥80 → +10%, <30 → -20%）
    /// </summary>
    public float GetSatisfactionEfficiencyMod()
    {
        if (Satisfaction >= 80) return 1.10f;
        if (Satisfaction < 30) return 0.80f;
        return 1.0f;
    }

    /// <summary>
    /// 满意度月度自然衰减（根据特质）
    /// </summary>
    public float GetSatisfactionDecay()
    {
        float baseDecay = 1.0f;
        float def = Trait switch
        {
            EmployeeTrait.Ambitious => baseDecay * 2.0f,
            EmployeeTrait.Chill => baseDecay * 0.3f,
            EmployeeTrait.Sensitive => baseDecay * 1.8f,
            EmployeeTrait.Lucky => baseDecay * 0.5f,
            _ => baseDecay
        };
        return BalanceModDB.Get($"trait.{Trait.ToString().ToLower()}.satisfaction_decay", def);
    }

    /// <summary>
    /// 添加职业生涯记忆
    /// </summary>
    public void AddMemory(string memory)
    {
        Memories.Add($"[第{MonthsEmployed / 12 + 1}年] {memory}");
        if (Memories.Count > 10) Memories.RemoveAt(0);
    }

    /// <summary>
    /// 获取最高技能等级
    /// </summary>
    public int GetHighestLevel()
    {
        int max = 0;
        foreach (var kv in Skills)
            if (kv.Value.Level > max) max = kv.Value.Level;
        return max;
    }

    /// <summary>
    /// 获取指定技能等级
    /// </summary>
    public int GetSkillLevel(SkillType type)
    {
        return Skills.TryGetValue(type, out var info) ? info.Level : 0;
    }

    /// <summary>
    /// 获取效率倍率
    /// </summary>
    public float GetEfficiency(SkillType type)
    {
        return Skills.TryGetValue(type, out var info) ? info.Efficiency : 0.5f;
    }

    /// <summary>
    /// 添加EXP（配对技能获得全额，非配对获得20%）
    /// </summary>
    public void AddExp(SkillType type, int amount, bool matched)
    {
        if (!Skills.ContainsKey(type))
        {
            // 如果没有该技能，不获得EXP（跨领域无用）
            if (!matched) return;
            Skills[type] = new SkillLevelInfo { Level = 0, Exp = 0, ExpToNext = 100, Efficiency = 0.5f };
        }

        var info = Skills[type];
        int effectiveExp = matched ? amount : (int)(amount * 0.2f);
        info.Exp += effectiveExp;
        Skills[type] = info;

        CheckLevelUp(type);
    }

    /// <summary>
    /// 检查升级
    /// </summary>
    private void CheckLevelUp(SkillType type)
    {
        var info = Skills[type];
        int[] thresholds = { 100, 300, 800, 2000, 5000 }; // Lv1~Lv5
        float[] efficiencies = { 1.0f, 1.8f, 3.0f, 5.0f, 8.0f };

        for (int i = info.Level; i < 5; i++)
        {
            if (info.Exp >= thresholds[i])
            {
                // 瓶颈检查
                if (i == 2 && info.Level == 2) // Lv3→Lv4
                {
                    if (ProjectsCompleted < 3) break;
                }
                if (i == 3 && info.Level == 3) // Lv4→Lv5
                {
                    if (HighScoreProjects < 1) break;
                }

                info.Level = i + 1;
                info.Efficiency = efficiencies[i];
                info.ExpToNext = i < 4 ? thresholds[i + 1] - thresholds[i] : 0;
            }
            else break;
        }
        Skills[type] = info;
    }
}
