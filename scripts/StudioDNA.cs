using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>工作室DNA——专业化路线系统</summary>
public partial class StudioDNA : Node
{
    private GameManager _gm;
    private GameDevManager _devMgr;

    // 类型专精度 (0~100)
    public Dictionary<GameGenre, float> GenreProficiency { get; private set; } = new();
    // 主题专精度
    public Dictionary<GameTheme, float> ThemeProficiency { get; private set; } = new();
    // 已解锁的专精标签
    public HashSet<string> UnlockedTags { get; private set; } = new();
    // 最近3款游戏的类型记录（用于检测连续性）
    private List<GameGenre> _recentGenres = new();
    // 工作室声誉标签
    public string StudioLabel { get; private set; } = "";

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        _devMgr = _gm.GetNode<GameDevManager>("GameDevManager");
    }

    /// <summary>项目完成后更新DNA</summary>
    public void OnProjectCompleted(GameProject proj)
    {
        // 更新专精度
        if (!GenreProficiency.ContainsKey(proj.Genre))
            GenreProficiency[proj.Genre] = 0;
        GenreProficiency[proj.Genre] = Mathf.Min(100, GenreProficiency[proj.Genre] + 10 + (proj.FinalScore >= 80 ? 5 : 0));

        if (!ThemeProficiency.ContainsKey(proj.Theme))
            ThemeProficiency[proj.Theme] = 0;
        ThemeProficiency[proj.Theme] = Mathf.Min(100, ThemeProficiency[proj.Theme] + 8 + (proj.FinalScore >= 80 ? 4 : 0));

        // 记录类型历史
        _recentGenres.Add(proj.Genre);
        if (_recentGenres.Count > 3) _recentGenres.RemoveAt(0);

        // 检测连续性（连续同类型→标签升级）
        CheckContinuity(proj);

        // 生成工作室标签
        UpdateStudioLabel();
    }

    private void CheckContinuity(GameProject proj)
    {
        if (_recentGenres.Count >= 2 && _recentGenres.All(g => g == proj.Genre))
        {
            string tag = $"expert_{proj.Genre.ToString().ToLower()}";
            if (!UnlockedTags.Contains(tag))
            {
                UnlockedTags.Add(tag);
                _gm.ShowToast("🏷️", Loc.TrF("dna.unlock_tag", Loc.Tr($"genre.{proj.Genre}")), new Color(0.7f, 0.5f, 0.2f));
            }
        }
    }

    private void UpdateStudioLabel()
    {
        // 找最高专精度
        var bestGenre = GenreProficiency.OrderByDescending(kv => kv.Value).FirstOrDefault();
        if (bestGenre.Value >= 40)
        {
            StudioLabel = Loc.TrF("dna.label", Loc.Tr($"genre.{bestGenre.Key}"));
        }
        else
        {
            StudioLabel = "";
        }
    }

    /// <summary>获取类型加成系数</summary>
    public float GetGenreBonus(GameGenre genre)
    {
        float baseBonus = GenreProficiency.GetValueOrDefault(genre, 0) * 0.003f;

        // 连续性加成
        bool isConsistent = _recentGenres.Count >= 2 && _recentGenres.All(g => g == genre);
        if (isConsistent) baseBonus += 0.1f;

        // 切换惩罚
        if (_recentGenres.Count >= 1 && _recentGenres.Last() != genre)
        {
            float penalty = GenreProficiency.GetValueOrDefault(_recentGenres.Last(), 0) * 0.002f;
            baseBonus -= penalty;
        }

        return Mathf.Clamp(baseBonus, -0.3f, 0.4f);
    }

    /// <summary>获取主题加成系数</summary>
    public float GetThemeBonus(GameTheme theme) =>
        Mathf.Clamp(ThemeProficiency.GetValueOrDefault(theme, 0) * 0.002f, -0.1f, 0.2f);

    /// <summary>获取系列疲劳惩罚（同类型连续做太多）</summary>
    public float GetFatiguePenalty(GameGenre genre)
    {
        if (_recentGenres.Count(g => g == genre) >= 3)
            return -0.15f;
        if (_recentGenres.Count(g => g == genre) >= 2)
            return -0.05f;
        return 0;
    }

    public void ResetForNewGame()
    {
        GenreProficiency.Clear();
        ThemeProficiency.Clear();
        UnlockedTags.Clear();
        _recentGenres.Clear();
        StudioLabel = "";
    }

    public Dictionary<string, float> SerializeProficiency()
    {
        var d = new Dictionary<string, float>();
        foreach (var kv in GenreProficiency) d[$"g_{kv.Key}"] = kv.Value;
        foreach (var kv in ThemeProficiency) d[$"t_{kv.Key}"] = kv.Value;
        return d;
    }

    public void DeserializeProficiency(Dictionary<string, float> data)
    {
        GenreProficiency.Clear();
        ThemeProficiency.Clear();
        foreach (var kv in data)
        {
            if (kv.Key.StartsWith("g_") && Enum.TryParse(kv.Key[2..], out GameGenre g))
                GenreProficiency[g] = kv.Value;
            else if (kv.Key.StartsWith("t_") && Enum.TryParse(kv.Key[2..], out GameTheme t))
                ThemeProficiency[t] = kv.Value;
        }
    }
}
