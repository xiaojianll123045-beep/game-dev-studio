using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class EmployeeSystemEx : Node
{
    private GameManager _gm => Services.GameManager;
    private EmployeeManager _empMgr => Services.EmployeeManager;

    public void MonthlyTick()
    {
        foreach (var emp in _empMgr.Employees)
        {
            ProcessDream(emp);
            ProcessMentalHealth(emp);
            ProcessMentorship(emp);
        }
    }

    // ── 员工梦想系统 ──
    private void ProcessDream(Employee emp)
    {
        if (emp.Dream == EmployeeDream.None || emp.DreamFulfilled) return;
        emp.DreamUrgency = Mathf.Min(100, emp.DreamUrgency + 1.5f);

        bool fulfilled = emp.Dream switch
        {
            EmployeeDream.MakeMasterpiece => emp.ConsecutiveHits >= 1,
            EmployeeDream.BuyHome => emp.Salary > 15000 && emp.MonthsEmployed > 36,
            EmployeeDream.LearnMastery => emp.GetHighestLevel() >= 5,
            EmployeeDream.MentorNextGen => emp.MentorshipCount >= 3,
            _ => false
        };

        if (fulfilled)
        {
            emp.DreamFulfilled = true;
            emp.Satisfaction = Mathf.Min(100, emp.Satisfaction + 25);
            emp.RecordMemory($"梦想实现！{GetDreamName(emp.Dream)}");
        }

        if (emp.DreamUrgency > 80 && !fulfilled)
            emp.Satisfaction = Mathf.Max(0, emp.Satisfaction - 2);
    }

    private string GetDreamName(EmployeeDream d) => d switch
    {
        EmployeeDream.MakeMasterpiece => "做出一款传世神作",
        EmployeeDream.StartOwnStudio => "自己开工作室",
        EmployeeDream.BuyHome => "买房安家",
        EmployeeDream.LearnMastery => "精通一门技能",
        EmployeeDream.MentorNextGen => "培养出优秀后辈",
        _ => "实现梦想"
    };

    // ── 心理健康系统 ──
    private void ProcessMentalHealth(Employee emp)
    {
        emp.MentalHealth = Mathf.Clamp(emp.MentalHealth + 1f, 0, 100);
        if (emp.Fatigue > 70) emp.MentalHealth -= 3;
        if (emp.Fatigue > 85) emp.MentalHealth -= 6;
        if (emp.Friends.Count > 0 && emp.Satisfaction > 50) emp.MentalHealth += 1;
        if (emp.OnVacation) emp.MentalHealth += 8;

        emp.BurnoutStage = emp.MentalHealth switch
        {
            >= 70 => BurnoutStage.None,
            >= 50 => BurnoutStage.Mild,
            >= 30 => BurnoutStage.Moderate,
            >= 15 => BurnoutStage.Severe,
            _ => BurnoutStage.Crisis
        };

        if (emp.BurnoutStage == BurnoutStage.Crisis && !emp.HasCounselling)
        {
            emp.Satisfaction = Mathf.Max(0, emp.Satisfaction - 5);
            emp.Fatigue = Mathf.Min(100, emp.Fatigue + 5);
        }
    }

    // ── 师徒系统 ──
    private void ProcessMentorship(Employee emp)
    {
        if (emp.MentorId == null) return;
        var mentor = _empMgr.Employees.Find(e => e.Id == emp.MentorId);
        if (mentor == null) return;

        int mentorLevel = mentor.GetHighestLevel();
        emp.Satisfaction += mentorLevel * 0.5f;
        mentor.Satisfaction += 0.3f;
    }

    // ── 创意火花大会（季度触发） ──
    public List<CreativeProposal> GenerateProposals(Team team)
    {
        // 重置本季度提案标记
        foreach (var e in _empMgr.Employees) e.SuggestedThisQuarter = false;

        var proposals = new List<CreativeProposal>();
        foreach (var emp in team.Members)
        {
            if (emp == null || emp.GetHighestLevel() < 2) continue;
            if (emp.SuggestedThisQuarter) continue;

            proposals.Add(new CreativeProposal
            {
                ProposerId = emp.Id,
                Title = $"{GetRandomProposalName()}",
                Description = $"{emp.Name}的创意提案",
                EstimatedMonths = 1 + new Random().Next(3),
                ScoreImpact = 5 + new Random().Next(20),
                RiskLevel = (float)new Random().NextDouble() * 0.5f,
            });
            emp.SuggestedThisQuarter = true;
        }
        return proposals;
    }

    private string GetRandomProposalName()
    {
        string[] names = { "重写渲染管线", "美术资源翻新", "物理引擎升级", "UI大改版", "音效系统重构", "网络协议优化" };
        return names[new Random().Next(names.Length)];
    }
}

// ── Employee 扩展 ──
public partial class Employee
{
    public EmployeeDream Dream { get; set; } = EmployeeDream.None;
    public int DreamProgress { get; set; }
    public bool DreamFulfilled { get; set; }
    public float DreamUrgency { get; set; }
    public int? MentorId { get; set; }
    public List<int> MenteeIds { get; set; } = new();
    public int MentorshipCount { get; set; }
    public float MentalHealth { get; set; } = 100f;
    public BurnoutStage BurnoutStage { get; set; } = BurnoutStage.None;
    public bool HasCounselling { get; set; }
    public bool SuggestedThisQuarter { get; set; }
    public int ApprovedProposals { get; set; }
    public int RejectedProposals { get; set; }
    public float PersonalFame { get; set; }
}
