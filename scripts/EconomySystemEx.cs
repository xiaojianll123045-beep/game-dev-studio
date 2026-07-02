using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class EconomySystemEx : Node
{
    private GameManager _gm => Services.GameManager;
    private GameDevManager _devMgr => Services.GameDevManager;
    private CompetitorAI _compAI => Services.CompetitorAI;

    public float IndustryReputation { get; set; } = 10f;
    public float MaxIndustryReputation { get; set; } = 100f;
    public float CreativePotential { get; set; }
    public int CreativeBreakthroughCount { get; set; }
    public float MarketHeatMultiplier { get; set; } = 1.0f;
    public float CostIndex { get; set; } = 1.0f;
    public List<MarketInvestment> Investments { get; set; } = new();
    public List<PhysicalVenue> Venues { get; set; } = new();

    public void MonthlyTick()
    {
        ProcessReputation();
        ProcessCreativePotential();
        ProcessInvestments();
        ProcessVenues();
    }

    // ── 行业声望 ──
    private void ProcessReputation()
    {
        float decay = 1f;
        IndustryReputation = Mathf.Max(0, IndustryReputation - decay * 0.1f);
    }

    public void GainReputation(float amount, string source)
    {
        IndustryReputation = Mathf.Min(MaxIndustryReputation, IndustryReputation + amount);
    }

    public bool SpendReputation(float amount)
    {
        if (IndustryReputation >= amount) { IndustryReputation -= amount; return true; }
        return false;
    }

    // ── 创意能量 ──
    private void ProcessCreativePotential()
    {
        var res = _gm.ResMgr;
        if (res.Inspiration >= res.MaxInspiration)
        {
            float overflow = res.Inspiration - res.MaxInspiration + 1f;
            CreativePotential += overflow * 0.5f;
        }
    }

    public bool TryCreativeBreakthrough()
    {
        float required = 50f + CreativeBreakthroughCount * 30f;
        if (CreativePotential >= required)
        {
            CreativePotential -= required;
            CreativeBreakthroughCount++;
            _gm.ShowToast("创意突破!", $"灵感上限突破至 {100 + CreativeBreakthroughCount * 25}", Colors.Purple);
            return true;
        }
        return false;
    }

    // ── 投资系统 ──
    private void ProcessInvestments()
    {
        var rng = new Random();
        foreach (var inv in Investments)
        {
            float change = (float)(rng.NextDouble() - 0.45) * inv.Volatility;
            inv.CurrentValue *= (1f + change);
        }
    }

    // ── 线下实体 ──
    private void ProcessVenues()
    {
        foreach (var venue in Venues)
        {
            float revenue = venue.MonthlyRevenue * (0.8f + (float)new Random().NextDouble() * 0.4f);
            _gm.ResMgr.Money += revenue;
        }
    }
}

// ── ResourceManager 扩展 ──
public partial class ResourceManager
{
    public float IndustryReputation => GetReputation();

    private float GetReputation()
    {
        var eco = Services.GameManager.GetNodeOrNull<EconomySystemEx>("EconomySystemEx");
        return eco?.IndustryReputation ?? 10f;
    }

    public float CreativePotential
    {
        get
        {
            var eco = Services.GameManager.GetNodeOrNull<EconomySystemEx>("EconomySystemEx");
            return eco?.CreativePotential ?? 0;
        }
        set
        {
            var eco = Services.GameManager.GetNodeOrNull<EconomySystemEx>("EconomySystemEx");
            if (eco != null) eco.CreativePotential = value;
        }
    }
}
