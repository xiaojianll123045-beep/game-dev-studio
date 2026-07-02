using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MarketSystemEx : Node
{
    private GameManager _gm => Services.GameManager;
    private CompetitorAI _compAI => Services.CompetitorAI;
    private GameDevManager _devMgr => Services.GameDevManager;
    private EmployeeManager _empMgr => Services.EmployeeManager;

    // ── 年度颁奖典礼 ──
    public Dictionary<string, string> AwardHistory { get; set; } = new();

    public void HoldAwards(int gameYear)
    {
        var winners = new Dictionary<string, string>
        {
            ["game_of_year"] = GetNomineeWithHighestScore(),
            ["studio_of_year"] = GetBestStudio(),
        };

        foreach (var kv in winners)
        {
            AwardHistory[$"{gameYear}_{kv.Key}"] = kv.Value;
            _gm.ShowToast("🏆", $"年度大奖得主：{kv.Value}！", Colors.Gold);
        }

        var companyName = _gm.Founder?.CompanyName ?? "玩家";
        if (winners.Values.Contains(companyName))
        {
            _devMgr.PlayerTrust = Mathf.Min(100, _devMgr.PlayerTrust + 10);
            _gm.ResMgr.Money += 500000;
        }
    }

    private string GetNomineeWithHighestScore()
    {
        float bestScore = 0;
        string best = "未知工作室";
        foreach (var s in _compAI.Studios)
        {
            foreach (var r in s.Releases)
            {
                if (r.Score > bestScore) { bestScore = r.Score; best = s.Name; }
            }
        }
        var companyName = _gm.Founder?.CompanyName ?? "玩家";
        foreach (var p in _devMgr.CompletedProjects)
        {
            if (p.FinalScore > bestScore) { bestScore = p.FinalScore; best = companyName; }
        }
        return best;
    }

    private string GetBestStudio()
    {
        var companyName = _gm.Founder?.CompanyName ?? "玩家";
        var allStudios = _compAI.Studios.Select(s => new { Name = s.Name, Score = s.Reputation + (float)s.Fans / 1000 }).ToList();
        allStudios.Add(new { Name = companyName, Score = _devMgr.PlayerTrust + (_gm.ResMgr.Money / 100000) });
        return allStudios.OrderByDescending(s => s.Score).First().Name;
    }

    // ── 工作室排行榜 ──
    public List<(int rank, string name, float score)> GetRankings()
    {
        var list = new List<(string, float)>();
        foreach (var s in _compAI.Studios.Where(s => !s.IsAcquired))
            list.Add((s.Name, s.Reputation + s.Fans / 1000f + s.EmployeeCount * 2));
        var companyName = _gm.Founder?.CompanyName ?? "玩家";
        list.Add((companyName, _devMgr.PlayerTrust + _gm.ResMgr.Money / 100000f + _empMgr.Employees.Count * 2));
        return list.OrderByDescending(x => x.Item2).Select((x, i) => (i + 1, x.Item1, x.Item2)).ToList();
    }

    // ── 专利系统 ──
    public Dictionary<string, PatentInfo> Patents { get; set; } = new();

    public bool ApplyPatent(string techId)
    {
        if (Patents.ContainsKey(techId)) return false;
        Patents[techId] = new PatentInfo { HolderName = _gm.Founder?.CompanyName ?? "玩家", TechId = techId, GrantedMonth = _gm.GameMonth };
        _gm.ShowToast("📜", $"专利申请成功：{techId}", Colors.LightBlue);
        return true;
    }

    // ── 人才市场 ──
    public void GenerateGraduateBatch()
    {
        int count = new Random().Next(3, 6);
        _gm.ShowToast("🎓", $"毕业季！{count}名新毕业生进入人才市场", Colors.Green);
    }

    // ── CEO人格系统 ──
    public void AssignCEOPersonality(AIStudio studio)
    {
        studio.CEOArchetype = (CEOArchetype)new Random().Next(8);
    }

    // ── 行业流言 ──
    public List<Rumor> ActiveRumors { get; set; } = new();

    public void SpreadRumor(string content, float credibility)
    {
        ActiveRumors.Add(new Rumor
        {
            Content = content,
            Credibility = credibility,
            SpawnMonth = _gm.GameMonth,
            ExpireMonth = _gm.GameMonth + 3 + new Random().Next(6)
        });
    }

    public void ProcessRumors()
    {
        for (int i = ActiveRumors.Count - 1; i >= 0; i--)
        {
            var r = ActiveRumors[i];
            r.Credibility += (float)(new Random().NextDouble() - 0.5) * 0.2f;
            if (_gm.GameMonth >= r.ExpireMonth) ActiveRumors.RemoveAt(i);
            else ActiveRumors[i] = r;
        }
    }
}

// ── AIStudio 扩展 ──
public partial class AIStudio
{
    public CEOArchetype CEOArchetype { get; set; } = CEOArchetype.Balanced;
    public string CEOName { get; set; }
    public int RivalryWithPlayer { get; set; }
}
