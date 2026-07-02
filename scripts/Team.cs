using Godot;
using System.Collections.Generic;

/// <summary>
/// 团队
/// </summary>
public partial class Team
{
    public string Name { get; set; }
    public List<Employee> Members { get; set; } = new();
    public Employee Captain { get; set; }
    public TeamTask Task { get; set; } = TeamTask.None;

    // 默契度
    public Dictionary<int, float> Chemistry { get; set; } = new(); // employeeId -> monthsTogether
    public const float MaxChemistry = 12f;  // 12个月满默契

    // 任务目标
    public GameProject CurrentProject { get; set; }
    public TechInfo? TargetTech { get; set; }
    public OutsourceContract? CurrentContract { get; set; }

    // 产能滑块（0~1，0=全部重构，1=全部新内容）
    public float ProdSlider { get; set; } = 0.7f;

    // 外包计时
    public int OutsourceMonthsRemaining { get; set; }

    /// <summary>
    /// 获取团队对指定技能的总等级
    /// </summary>
    public int GetTotalSkillLevel(SkillType type)
    {
        int total = 0;
        foreach (var m in Members)
            total += m.GetSkillLevel(type);
        return total;
    }

    /// <summary>获取团队某技能的综合效率（等级×人数）</summary>
    public float GetSkillEff(SkillType type)
    {
        float eff = 0;
        foreach (var m in Members)
            eff += m.GetEfficiency(type) * m.GetTraitEfficiencyMod();
        return eff;
    }

    /// <summary>
    /// 获取默契加成
    /// </summary>
    public float GetChemistryBonus()
    {
        if (Members.Count < 2) return 0;
        float minChem = float.MaxValue;
        int friendPairs = 0;
        foreach (var a in Members)
        {
            foreach (var b in Members)
            {
                if (a.Id >= b.Id) continue;
                int key = a.Id * 10000 + b.Id;
                if (!Chemistry.TryGetValue(key, out float chem)) chem = 0;
                if (chem < minChem) minChem = chem;
                // 战场兄弟：互相是好友的组合额外+5%默契
                if (a.Friends.Contains(b.Id) && b.Friends.Contains(a.Id)) friendPairs++;
            }
        }
        if (minChem < 12) return 0;
        float baseBonus = minChem / MaxChemistry * 0.15f; // 最高+15%
        float friendBonus = friendPairs * 0.05f;            // 每对战场兄弟 +5%
        return baseBonus + friendBonus;
    }

    /// <summary>
    /// 每月更新默契
    /// </summary>
    public void UpdateChemistry()
    {
        if (Members.Count < 2) return;
        var keys = new List<int>();
        for (int i = 0; i < Members.Count; i++)
        {
            for (int j = i + 1; j < Members.Count; j++)
            {
                int key = Members[i].Id * 10000 + Members[j].Id;
                keys.Add(key);
                if (!Chemistry.ContainsKey(key)) Chemistry[key] = 0;
                Chemistry[key] = Mathf.Min(MaxChemistry, Chemistry[key] + 1);
            }
        }
    }
}
