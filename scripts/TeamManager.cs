using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 团队管理器
/// </summary>
public partial class TeamManager : Node
{
    public List<Team> Teams { get; private set; } = new();
    private GameManager _gm;
    private EmployeeManager _empMgr;

    // 外包合同池
    public List<OutsourceContract> AvailableContracts { get; private set; } = new();
    private int _contractRefreshTick;

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        _empMgr = GetNode<EmployeeManager>("../EmployeeManager");
        RefreshContracts();

        if (GlobalSettings.NewGame)
            InitStartingTeam();
    }

    private void InitStartingTeam()
    {
        var all = new List<Employee>(_empMgr.Employees);
        var captain = all.FirstOrDefault(e => e.Name == Loc.Tr("team.founder")) ?? all.FirstOrDefault(e => e.CanMentor);
        CreateTeam(Loc.Tr("team.initial"), all, captain);
    }

    /// <summary>
    /// 创建团队
    /// </summary>
    public Team CreateTeam(string name, List<Employee> members, Employee captain = null)
    {
        // 从原团队中移除成员
        foreach (var e in members)
        {
            if (!string.IsNullOrEmpty(e.TeamName))
            {
                var oldTeam = Teams.Find(t => t.Name == e.TeamName);
                oldTeam?.Members.Remove(e);
            }
            e.TeamName = name;
        }
        var team = new Team { Name = name, Members = members };
        if (captain != null && captain.CanMentor)
        {
            team.Captain = captain;
            captain.IsCaptain = true;
        }
        team.UpdateChemistry();
        Teams.Add(team);
        return team;
    }

    /// <summary>
    /// 解散团队
    /// </summary>
    public void DisbandTeam(Team team)
    {
        if (team.Captain != null)
            team.Captain.IsCaptain = false;
        foreach (var e in team.Members) e.TeamName = null;
        team.Chemistry.Clear(); // 拆散归零
        Teams.Remove(team);
    }

    /// <summary>
    /// 拖拽员工入队
    /// </summary>
    public bool AddToTeam(Team team, Employee emp)
    {
        if (team.Members.Contains(emp)) return false;
        // 从原队移除
        foreach (var t in Teams)
            t.Members.Remove(emp);
        team.Members.Add(emp);
        emp.TeamName = team.Name;
        team.Chemistry.Clear(); // 新成员加入，默契清零
        return true;
    }

    /// <summary>
    /// 从团队移除员工
    /// </summary>
    public void RemoveFromTeam(Team team, Employee emp)
    {
        team.Members.Remove(emp);
        emp.TeamName = null;
        emp.IsCaptain = false;
        if (team.Captain == emp)
        {
            team.Captain = team.Members.FirstOrDefault(m => m.CanMentor);
            if (team.Captain != null) team.Captain.IsCaptain = true;
        }
        if (team.Members.Count == 0)
            Teams.Remove(team);
        else
            team.Chemistry.Clear();
    }

    /// <summary>
    /// 设置团队任务
    /// </summary>
    public void SetTeamTask(Team team, TeamTask task)
    {
        team.Task = task;
    }

    /// <summary>
    /// 获取空闲员工
    /// </summary>
    public List<Employee> GetIdleEmployees()
    {
        var busy = new HashSet<Employee>();
        foreach (var t in Teams)
        {
            foreach (var m in t.Members)
                busy.Add(m);
        }
        return _empMgr.Employees.Where(e => !busy.Contains(e)).ToList();
    }

    /// <summary>
    /// 获取在执行任务的团队
    /// </summary>
    public List<Team> GetActiveTeams()
    {
        return Teams.Where(t => t.Task != TeamTask.None).ToList();
    }

    /// <summary>
    /// 刷新外包合同
    /// </summary>
    public void RefreshContracts()
    {
        AvailableContracts.Clear();
        var rng = new RandomNumberGenerator();
        int count = rng.RandiRange(2, 5);

        for (int i = 0; i < count; i++)
        {
            var contract = new OutsourceContract
            {
                Name = GenerateContractName(),
                Difficulty = (OutsourceDifficulty)rng.RandiRange(0, 3),
                RequiredMonths = rng.RandiRange(2, 8),
                PrimarySkill = (SkillType)rng.RandiRange(0, 4),
                MinSkillLevel = rng.RandiRange(1, 3),
            };

            // 难度影响回报
            float basePay = contract.Difficulty switch
            {
                OutsourceDifficulty.Easy => 30000,
                OutsourceDifficulty.Medium => 80000,
                OutsourceDifficulty.Hard => 200000,
                OutsourceDifficulty.Epic => 500000,
                _ => 50000
            };
            contract.Payment = basePay * (1 + contract.RequiredMonths * 0.2f);
            contract.PenaltyRate = 0.1f;
            contract.ExpReward = 20 + (int)contract.Difficulty * 15;

            AvailableContracts.Add(contract);
        }
    }

    private string GenerateContractName()
    {
        string[] names = {
            Loc.Tr("outsource.mobile_ui"), Loc.Tr("outsource.web_port"), Loc.Tr("outsource.indie_audio"), Loc.Tr("outsource.aaa_model"),
            Loc.Tr("outsource.server_arch"), Loc.Tr("outsource.ai_path"), Loc.Tr("outsource.particle_fx"), Loc.Tr("outsource.ost_license"),
            Loc.Tr("outsource.localization"), Loc.Tr("outsource.anticheat"), Loc.Tr("outsource.vr_demo"), Loc.Tr("outsource.metaverse")
        };
        return names[new RandomNumberGenerator().RandiRange(0, names.Length - 1)];
    }

    /// <summary>
    /// 接包
    /// </summary>
    public bool AcceptContract(Team team, OutsourceContract contract)
    {
        if (team.Task != TeamTask.None) return false;
        team.Task = TeamTask.Outsource;
        team.CurrentContract = contract;
        team.OutsourceMonthsRemaining = contract.RequiredMonths;
        AvailableContracts.Remove(contract);
        return true;
    }

    public void RemoveFromAllTeams(Employee emp)
    {
        foreach (var team in Teams)
        {
            if (team.Members.Contains(emp))
            {
                if (team.Captain == emp)
                {
                    team.Captain = team.Members.Count > 1 ? team.Members.Find(m => m != emp) : null;
                }
                team.Members.Remove(emp);
                if (team.CurrentProject != null && team.Members.Count == 0)
                {
                    team.CurrentProject = null;
                    team.Task = TeamTask.None;
                }
            }
        }
        emp.TeamName = null;
        emp.IsCaptain = false;
    }
}
