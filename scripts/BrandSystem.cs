using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>品牌五维</summary>
public class BrandProfile
{
    public float Innovation;     // 创新
    public float Quality;        // 品质
    public float Value;          // 性价比
    public float Hardcore;       // 硬核
    public float Artistic;       // 艺术性
}

/// <summary>品牌定位系统——IP不再只是数值加成</summary>
public partial class BrandSystem : Node
{
    private GameManager _gm;
    private GameDevManager _devMgr;

    public BrandProfile Profile { get; private set; } = new();
    public Dictionary<string, BrandProfile> IPProfiles { get; private set; } = new();

    // 粉丝对品牌的认知（偏差太大时惩罚）
    public float BrandCoherence => CalculateCoherence();

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        _devMgr = _gm.GetNode<GameDevManager>("GameDevManager");
    }

    /// <summary>游戏发售后更新品牌画像</summary>
    public void OnGameReleased(GameProject proj)
    {
        float innovation = Mathf.Clamp((proj.GameplayScore * 0.3f + proj.StoryScore * 0.2f) - 20, 0, 100);
        float quality = Mathf.Clamp(proj.FinalScore * 0.7f + proj.StabilityScore * 0.3f - 10, 0, 100);
        float value = Mathf.Clamp(60 - proj.SuggestedPrice * 2 + proj.FinalScore * 0.3f, 0, 100);
        float hardcore = Mathf.Clamp(proj.GameplayScore * 0.4f + proj.StabilityScore * 0.2f - 20, 0, 100);
        float artistic = Mathf.Clamp(proj.StoryScore * 0.3f + proj.AudioScore * 0.3f + proj.GraphicsScore * 0.2f - 20, 0, 100);

        // 平滑更新品牌画像
        float alpha = 0.3f;
        Profile.Innovation = Profile.Innovation * (1 - alpha) + innovation * alpha;
        Profile.Quality = Profile.Quality * (1 - alpha) + quality * alpha;
        Profile.Value = Profile.Value * (1 - alpha) + value * alpha;
        Profile.Hardcore = Profile.Hardcore * (1 - alpha) + hardcore * alpha;
        Profile.Artistic = Profile.Artistic * (1 - alpha) + artistic * alpha;

        // 更新IP画像
        string ipName = string.IsNullOrEmpty(proj.IPName) ? proj.Name : proj.IPName;
        if (!IPProfiles.ContainsKey(ipName))
            IPProfiles[ipName] = new BrandProfile();
        var ip = IPProfiles[ipName];
        ip.Innovation = ip.Innovation * (1 - alpha) + innovation * alpha;
        ip.Quality = ip.Quality * (1 - alpha) + quality * alpha;
        ip.Value = ip.Value * (1 - alpha) + value * alpha;
        ip.Hardcore = ip.Hardcore * (1 - alpha) + hardcore * alpha;
        ip.Artistic = ip.Artistic * (1 - alpha) + artistic * alpha;
    }

    /// <summary>计算品牌一致性</summary>
    private float CalculateCoherence()
    {
        float sum = Profile.Innovation + Profile.Quality + Profile.Value + Profile.Hardcore + Profile.Artistic;
        if (sum < 1) return 1f;

        // 方差越小越一致
        float avg = sum / 5f;
        float variance = (
            Mathf.Pow(Profile.Innovation - avg, 2) +
            Mathf.Pow(Profile.Quality - avg, 2) +
            Mathf.Pow(Profile.Value - avg, 2) +
            Mathf.Pow(Profile.Hardcore - avg, 2) +
            Mathf.Pow(Profile.Artistic - avg, 2)
        ) / 5f;

        return Mathf.Clamp(1f - variance / 2000f, 0.3f, 1f);
    }

    /// <summary>一致性惩罚（新游戏与品牌偏差过大时）</summary>
    public float GetCoherencePenalty(GameProject proj)
    {
        float newInnovation = Mathf.Clamp((proj.GameplayScore * 0.3f + proj.StoryScore * 0.2f) - 20, 0, 100);
        float deviation = Mathf.Abs(Profile.Innovation - newInnovation);
        deviation += Mathf.Abs(Profile.Quality - proj.FinalScore * 0.7f);

        if (deviation > 50) return -0.2f;
        if (deviation > 30) return -0.1f;
        return 0;
    }

    /// <summary>重置</summary>
    public void Reset()
    {
        Profile = new BrandProfile();
        IPProfiles.Clear();
    }
}
