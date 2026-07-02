using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 员工管理器
/// </summary>
public partial class EmployeeManager : Node
{
    private GameManager _gm;
    private ResourceManager _res;
    public List<Employee> Employees { get; private set; } = new();
    private int _nextId = 1;

    // 随机名字池 — 从locale读取，支持多语言
    private static string[] GetFirstNames() {
        var raw = Loc.Tr("person.firstnames");
        if (raw.Contains(',') || raw.Contains('،'))
            return Loc.SplitLocaleList(raw);
        return raw.ToCharArray().Select(c => c.ToString()).ToArray();
    }
    private static string[] GetLastNames() {
        var raw = Loc.Tr("person.lastnames");
        if (raw.Contains(',') || raw.Contains('،'))
            return Loc.SplitLocaleList(raw);
        return raw.ToCharArray().Select(c => c.ToString()).ToArray();
    }
    private static readonly SkillType[] SkillTypes = {
        SkillType.Program, SkillType.Art, SkillType.Audio, SkillType.Network, SkillType.AI
    };

    // 外部雇佣价格（根据等级，Mod 可通过 BalanceModDB 覆盖）
    private float GetHireCost(int level)
    {
        float[] defaults = { 5000, 20000, 80000, 300000, 800000 };
        int idx = Mathf.Min(level, defaults.Length - 1);
        return BalanceModDB.Get($"hire.cost.level{idx}", defaults[idx]);
    }

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        _res = GetNode<ResourceManager>("../ResourceManager");
        // 员工初始化由 GameManager.InitGame() 负责，此时 Loc 等服务已就绪
    }

    /// <summary>
    /// 创始人从零开始——上手容易但需要成长
    /// </summary>
    public void InitStartingEmployees()
    {
        var founder = CreateEmployee(Loc.Tr("person.founder_name"));
        // 开局技能取自创始人配置
        var gm = GetNode<GameManager>("..");
        int ProgLv = Mathf.Clamp(gm.Founder.Programming, 1, 5);
        int ArtLv = Mathf.Clamp(gm.Founder.Art, 1, 5);
        int AudioLv = Mathf.Clamp(gm.Founder.Audio, 1, 5);
        int AiLv = Mathf.Clamp(gm.Founder.AI, 1, 5);
        int NetLv = Mathf.Clamp(gm.Founder.Network, 1, 5);
        float Eff(int lv) => 0.5f + lv * 0.3f;
        founder.Skills[SkillType.Program] = new SkillLevelInfo { Level = ProgLv, Exp = 0, ExpToNext = 0, Efficiency = Eff(ProgLv) };
        founder.Skills[SkillType.Art] = new SkillLevelInfo { Level = ArtLv, Exp = 0, ExpToNext = 0, Efficiency = Eff(ArtLv) };
        founder.Skills[SkillType.Audio] = new SkillLevelInfo { Level = AudioLv, Exp = 0, ExpToNext = 0, Efficiency = Eff(AudioLv) };
        founder.Skills[SkillType.AI] = new SkillLevelInfo { Level = AiLv, Exp = 0, ExpToNext = 0, Efficiency = Eff(AiLv) };
        founder.Skills[SkillType.Network] = new SkillLevelInfo { Level = NetLv, Exp = 0, ExpToNext = 0, Efficiency = Eff(NetLv) };
        founder.Salary = 0;
        founder.Fatigue = 0;
        Employees.Add(founder);
        _nextId = 2;
    }

    /// <summary>
    /// 创建员工基础信息
    /// </summary>
    private Employee CreateEmployee(string name)
    {
        return new Employee
        {
            Name = name,
            Id = _nextId++,
            Skills = new Dictionary<SkillType, SkillLevelInfo>(),
            MonthsEmployed = 0,
            ProjectsCompleted = 0,
            HighScoreProjects = 0
        };
    }

    /// <summary>
    /// 随机生成可招聘员工
    /// </summary>
    public Employee GenerateRecruit()
    {
        var rng = new RandomNumberGenerator();
        var fns = GetFirstNames(); var lns = GetLastNames();
        bool isAr = Loc.CurrentLang == 10;
        var emp = CreateEmployee(fns[rng.RandiRange(0, fns.Length - 1)] +
                                  (isAr ? " " : "") +
                                  lns[rng.RandiRange(0, lns.Length - 1)]);

        // 随机1~2个技能
        int skillCount = rng.RandiRange(1, 2);
        var chosen = new HashSet<int>();
        for (int i = 0; i < skillCount; i++)
        {
            int idx;
            do { idx = rng.RandiRange(0, 4); } while (chosen.Contains(idx));
            chosen.Add(idx);

            // 等级分布：80% Lv1, 15% Lv2, 4% Lv3, 1% Lv4
            int r = rng.RandiRange(0, 99);
            int level = r < 80 ? 1 : r < 95 ? 2 : r < 99 ? 3 : 4;
            int expThreshold = level switch
            {
                1 => 100, 2 => 300, 3 => 800, 4 => 2000, _ => 0
            };
            float eff = level switch
            {
                1 => 1.0f, 2 => 1.8f, 3 => 3.0f, 4 => 5.0f, 5 => 8.0f, _ => 0.5f
            };

            emp.Skills[SkillTypes[idx]] = new SkillLevelInfo
            {
                Level = level,
                Exp = expThreshold,
                ExpToNext = level < 5 ? (level == 1 ? 300 : level == 2 ? 800 : level == 3 ? 2000 : 5000) - expThreshold : 0,
                Efficiency = eff
            };
        }

        emp.Salary = 5000 + emp.GetHighestLevel() * 5000;

        // 随机特质（80%概率获得1个）
        if (rng.RandiRange(0, 99) < 80)
        {
            var traits = System.Enum.GetValues<EmployeeTrait>().Where(t => t != EmployeeTrait.None).ToArray();
            emp.Trait = traits[rng.RandiRange(0, traits.Length - 1)];
        }

        return emp;
    }

    /// <summary>
    /// 雇佣员工
    /// </summary>
    public bool HireEmployee(Employee emp)
    {
        ModAPI.FireHooks(ModAPI.GameHook.BeforeEmployeeHire);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeEmployeeHire)) return false;

        int maxLevel = emp.GetHighestLevel();
        float cost = GetHireCost(maxLevel);

        // ── 传奇遗产: 慕名而来的高等级员工成本减半 ──
        var gm = GetNode<GameManager>("/root/GameManager");
        if (gm?.HasLegendaryLegacy == true && maxLevel >= 3)
            cost *= 0.5f;

        if (_res.SpendMoney(cost, "salary"))
        {
            emp.Id = _nextId++;
            Employees.Add(emp);
            _res.TotalEmployees = Employees.Count;
            _gm.GetNodeOrNull<TutorialManager>("TutorialManager")?.NotifyAction("employee_hired");
            ModAPI.FireHooks(ModAPI.GameHook.AfterEmployeeHire);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 解雇员工
    /// </summary>
    public void FireEmployee(Employee emp)
    {
        ModAPI.FireHooks(ModAPI.GameHook.BeforeEmployeeLeave);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeEmployeeLeave)) return;
        emp.TeamName = null;
        emp.IsCaptain = false;
        // 从所有团队中移除
        var teamMgr = GetNode<TeamManager>("../TeamManager");
        foreach (var team in teamMgr.Teams)
        {
            team.Members.Remove(emp);
            if (team.Captain == emp) { team.Captain = team.Members.FirstOrDefault(); }
        }
        Employees.Remove(emp);
        _res.TotalEmployees = Employees.Count;
        ModAPI.FireHooks(ModAPI.GameHook.AfterEmployeeLeave);
    }

    /// <summary>
    /// 发放工资（每月）
    /// </summary>
    public void PaySalaries()
    {
        float total = 0;
        foreach (var e in Employees)
            total += e.Salary;
        _res.SpendMoney(total, "salary");
    }

    /// <summary>
    /// 每月工时结算：给所有被分配到任务的员工加EXP
    /// </summary>
    public void MonthlyExpSettle(List<Team> activeTeams, TeamManager teamMgr)
    {
        ModAPI.FireHooks(ModAPI.GameHook.BeforeMonthlyExpSettle);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeMonthlyExpSettle)) return;
        foreach (var team in activeTeams)
        {
            if (team.Task == TeamTask.None) continue;

            foreach (var emp in team.Members)
            {
                emp.MonthsEmployed++;

                // 培训休假员工不增加疲劳
                if (emp.TrainingLeaveMonths > 0) continue;

                // 疲劳增长（受特质影响，Mod 可通过 BalanceModDB 覆盖基础值）
                float fatigueBase = BalanceModDB.Get("fatigue.monthly_gain", 3f);
                emp.Fatigue = Mathf.Min(100, emp.Fatigue + fatigueBase * emp.GetTraitFatigueMod());
                if (emp.IsCaptain)
                    emp.Fatigue = Mathf.Max(0, emp.Fatigue - 1.5f);

                // 队长加成
                bool hasCaptainBonus = team.Captain != null && team.Captain != emp &&
                                       team.Captain.CanMentor;

                foreach (var kv in emp.Skills)
                {
                    bool matched = IsSkillMatched(kv.Key, team.Task, team);
                    int baseExp = matched ? 15 : 3;
                    if (hasCaptainBonus) baseExp = (int)(baseExp * 1.1f);
                    emp.AddExp(kv.Key, baseExp, matched);
                }
            }
        }

        // 空闲员工疲劳恢复
        foreach (var emp in Employees)
        {
            if (activeTeams.Any(t => t.Members.Contains(emp))) continue;
            emp.Fatigue = Mathf.Max(0, emp.Fatigue - 5f);
        }
        ModAPI.FireHooks(ModAPI.GameHook.AfterMonthlyExpSettle);
    }

    public void RemoveEmployee(Employee emp)
    {
        if (emp == null || !Employees.Contains(emp)) return;
        Employees.Remove(emp);
    }

    private bool IsSkillMatched(SkillType skill, TeamTask task, Team team)
    {
        if (task == TeamTask.DevelopGame) return true; // 开发匹配所有技能
        if (task == TeamTask.ResearchTech && team.TargetTech != null)
        {
            return skill == team.TargetTech.Value.PrimarySkill ||
                   skill == team.TargetTech.Value.SecondarySkill;
        }
        if (task == TeamTask.Outsource && team.CurrentContract != null)
        {
            return skill == team.CurrentContract.Value.PrimarySkill;
        }
        return false;
    }
}
