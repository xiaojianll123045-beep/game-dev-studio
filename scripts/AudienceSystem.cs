using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>受众派系</summary>
public enum AudienceFaction { CoreGamers, Casuals, StoryLovers, SocialPlayers }

/// <summary>受众细分系统</summary>
public partial class AudienceSystem : Node
{
    private GameManager _gm;
    private GameDevManager _devMgr;
    private FanManager _fanMgr;

    // 各派系满意度 0~100
    public Dictionary<AudienceFaction, float> Satisfaction { get; private set; } = new();
    // 各派系期望值
    public Dictionary<AudienceFaction, float> Expectation { get; private set; } = new();
    // 各派系规模
    public Dictionary<AudienceFaction, int> Population { get; private set; } = new();

    // 粉丝→派系分布
    private Dictionary<AudienceFaction, float> _distribution = new();

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        _devMgr = _gm.GetNode<GameDevManager>("GameDevManager");
        _fanMgr = _gm.GetNode<FanManager>("FanManager");

        foreach (AudienceFaction f in Enum.GetValues(typeof(AudienceFaction)))
        {
            Satisfaction[f] = 50;
            Expectation[f] = 50;
            Population[f] = 0;
            _distribution[f] = 0.25f;
        }
    }

    /// <summary>根据游戏设计决策更新各派系满意度</summary>
    public void UpdateFromDesign(GameProject proj)
    {
        var factionEffects = GetFactionEffects(proj);
        foreach (var kv in factionEffects)
        {
            Satisfaction[kv.Key] = Mathf.Clamp(Satisfaction[kv.Key] + kv.Value, 0, 100);
        }
    }

    private Dictionary<AudienceFaction, float> GetFactionEffects(GameProject proj)
    {
        return new Dictionary<AudienceFaction, float>
        {
            [AudienceFaction.CoreGamers] = (proj.GameplayScore * 0.3f + proj.StabilityScore * 0.2f - 30) * 0.5f,
            [AudienceFaction.Casuals] = (proj.GameplayScore * 0.1f + proj.GraphicsScore * 0.3f - 20) * 0.5f,
            [AudienceFaction.StoryLovers] = (proj.StoryScore * 0.4f + proj.AudioScore * 0.15f - 25) * 0.5f,
            [AudienceFaction.SocialPlayers] = (proj.NetworkScore * 0.4f + proj.GameplayScore * 0.1f - 20) * 0.5f,
        };
    }

    /// <summary>获取派系对游戏的综合评分</summary>
    public float GetFactionScore(GameProject proj, AudienceFaction faction)
    {
        float baseScore = proj.FinalScore;
        float modifier = faction switch
        {
            AudienceFaction.CoreGamers => proj.GameplayScore * 0.4f + proj.StabilityScore * 0.2f - baseScore * 0.4f,
            AudienceFaction.Casuals => proj.GraphicsScore * 0.3f + proj.GameplayScore * 0.15f - baseScore * 0.3f,
            AudienceFaction.StoryLovers => proj.StoryScore * 0.4f + proj.AudioScore * 0.15f - baseScore * 0.35f,
            AudienceFaction.SocialPlayers => proj.NetworkScore * 0.4f + proj.GameplayScore * 0.1f - baseScore * 0.3f,
            _ => 0
        };
        return Mathf.Clamp(baseScore + modifier * 0.5f, 10, 100);
    }

    /// <summary>每月更新受众状态</summary>
    public void MonthlyUpdate()
    {
        // 期望值向50收敛
        foreach (AudienceFaction f in Enum.GetValues(typeof(AudienceFaction)))
        {
            Expectation[f] = Expectation[f] * 0.95f + 50 * 0.05f;
        }
    }

    /// <summary>游戏发售后更新受众</summary>
    public void OnGameReleased(GameProject proj)
    {
        foreach (AudienceFaction f in Enum.GetValues(typeof(AudienceFaction)))
        {
            float actual = GetFactionScore(proj, f);
            float expected = Expectation[f];

            // 满意度变化
            float delta = actual - expected;
            Satisfaction[f] = Mathf.Clamp(Satisfaction[f] + delta * 0.3f, 0, 100);

            // 期望值更新（下次会更接近这次的表现）
            Expectation[f] = Expectation[f] * 0.6f + actual * 0.4f;

            // 人口变化（满意度影响）
            float growth = (Satisfaction[f] - 50) * 0.01f;
            Population[f] = Mathf.Max(0, Population[f] + (int)(Population[f] * growth * 0.1f));
        }
    }

    /// <summary>获取某派系成为黑粉的概率</summary>
    public float GetTrollProbability(AudienceFaction faction)
    {
        if (Satisfaction[faction] < 20 && Expectation[faction] > 60)
            return 0.4f;
        return 0.05f;
    }
}
