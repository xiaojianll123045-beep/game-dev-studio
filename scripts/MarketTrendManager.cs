using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 行业浪潮管理器 — 每隔3~5年市场偏好发生剧变，迫使玩家适应新环境
/// </summary>
public partial class MarketTrendManager : Node
{
    private GameManager _gm;

    /// <summary>当前活跃的浪潮列表</summary>
    public List<MarketTrend> ActiveTrends { get; private set; } = new();

    // ── 市场风口周期 ──
    public List<MarketHypeCycle> HypeCycles { get; private set; } = new();
    private int _hypeCycleCounter = 0;

    /// <summary>查询指定类型/主题的当前热度</summary>
    public MarketHypeCycle GetHypeForGenre(GameGenre genre)
    {
        var existing = HypeCycles.FirstOrDefault(h => h.Genre == genre);
        return existing ?? new MarketHypeCycle { Genre = genre, Popularity = 0.5f, MonthsLeft = 999 };
    }
    public MarketHypeCycle GetHypeForTheme(GameTheme theme)
    {
        var existing = HypeCycles.FirstOrDefault(h => h.Theme == theme);
        return existing ?? new MarketHypeCycle { Theme = theme, Popularity = 0.5f, MonthsLeft = 999 };
    }

    /// <summary>浪潮历史记录（最近10条）</summary>
    public List<string> TrendHistory { get; private set; } = new();

    /// <summary>距下次浪潮的月数</summary>
    public int MonthsUntilNextTrend { get; private set; } = 36;

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        if (GlobalSettings.NewGame)
            MonthsUntilNextTrend = 36 + new Random().Next(24); // 3~5年后第一波
    }

    /// <summary>每月调用</summary>
    public void MonthlyTick()
    {
        ModAPI.FireHooks(ModAPI.GameHook.BeforeMarketTrendTick);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeMarketTrendTick)) return;
        MonthsUntilNextTrend--;
        if (MonthsUntilNextTrend <= 0 && ActiveTrends.Count < 2)
        {
            SpawnNewTrend();
        }

        // ── 风口周期更新 ──
        UpdateHypeCycles();

        // 浪潮自然消退
        for (int i = ActiveTrends.Count - 1; i >= 0; i--)
        {
            ActiveTrends[i].RemainingMonths--;
            if (ActiveTrends[i].RemainingMonths <= 0)
            {
                string fadeMsg = Loc.TrF("trend.wave_fade", ActiveTrends[i].Name);
                TrendHistory.Add(fadeMsg);
                ActiveTrends.RemoveAt(i);
            }
        }
        ModAPI.FireHooks(ModAPI.GameHook.AfterMarketTrendTick);
    }

    private void SpawnNewTrend()
    {
        var rng = new Random();
        var trend = PickTrend(rng);
        ActiveTrends.Add(trend);
        TrendHistory.Add(Loc.TrF("trend.history_fmt", _gm.GameYear, _gm.MonthInYear, trend.Description));
        MonthsUntilNextTrend = 24 + rng.Next(24);

        _gm.ShowPopup(Loc.Tr("trend.popup_title"),
            $"{trend.Name}\n{trend.Description}\n\n{Loc.TrF("trend.popup_details", string.Join("、", trend.BoostedGenres.Take(3).Select(g => g.Name())), string.Join("、", trend.PenalizedGenres.Take(3).Select(g => g.Name())))}", new Color(0.2f, 0.6f, 1f));
    }

    private MarketTrend PickTrend(Random rng)
    {
        var all = new MarketTrend[]
        {
            // ── 类型浪潮 ──
            new()
            {
                Name = Loc.Tr("trend.openworld"),
                Description = Loc.Tr("trend.openworld_desc"),
                BoostedGenres = new() { GameGenre.RPG, GameGenre.ACT, GameGenre.SAN, GameGenre.SUR },
                PenalizedGenres = new() { GameGenre.AVG, GameGenre.FTG, GameGenre.PZL },
                ScoreBonus = 10f,
                SalesBonus = 0.25f,
                RemainingMonths = 18 + rng.Next(12)
            },
            new()
            {
                Name = Loc.Tr("trend.roguelike"),
                Description = Loc.Tr("trend.roguelike_desc"),
                BoostedGenres = new() { GameGenre.ROG, GameGenre.ACT, GameGenre.SLG, GameGenre.FPS },
                PenalizedGenres = new() { GameGenre.SIM, GameGenre.MUS, GameGenre.VIS },
                ScoreBonus = 8f,
                SalesBonus = 0.30f,
                RemainingMonths = 12 + rng.Next(12)
            },
            new()
            {
                Name = Loc.Tr("trend.casual"),
                Description = Loc.Tr("trend.casual_desc"),
                BoostedGenres = new() { GameGenre.PZL, GameGenre.SIM, GameGenre.MUS, GameGenre.ETC },
                PenalizedGenres = new() { GameGenre.HOR, GameGenre.FPS, GameGenre.MMO },
                ScoreBonus = 5f,
                SalesBonus = 0.20f,
                RemainingMonths = 12 + rng.Next(18)
            },
            new()
            {
                Name = Loc.Tr("trend.hardcore"),
                Description = Loc.Tr("trend.hardcore_desc"),
                BoostedGenres = new() { GameGenre.SLG, GameGenre.FTG, GameGenre.FPS, GameGenre.RTS },
                PenalizedGenres = new() { GameGenre.VIS, GameGenre.MUS, GameGenre.ETC },
                ScoreBonus = 7f,
                SalesBonus = 0.15f,
                RemainingMonths = 12 + rng.Next(12)
            },
            new()
            {
                Name = Loc.Tr("trend.online"),
                Description = Loc.Tr("trend.online_desc"),
                BoostedGenres = new() { GameGenre.MOBA, GameGenre.MMO, GameGenre.FPS, GameGenre.RTS },
                PenalizedGenres = new() { GameGenre.AVG, GameGenre.VIS, GameGenre.HOR },
                ScoreBonus = 6f,
                SalesBonus = 0.35f,
                RemainingMonths = 12 + rng.Next(18)
            },
            new()
            {
                Name = Loc.Tr("trend.narrative"),
                Description = Loc.Tr("trend.narrative_desc"),
                BoostedGenres = new() { GameGenre.AVG, GameGenre.VIS, GameGenre.RPG },
                PenalizedGenres = new() { GameGenre.RAC, GameGenre.SPO, GameGenre.FTG },
                ScoreBonus = 12f,
                SalesBonus = 0.18f,
                RemainingMonths = 15 + rng.Next(12)
            },
            // ── 主题浪潮 ──
            new()
            {
                Name = Loc.Tr("trend.cyberpunk"),
                Description = Loc.Tr("trend.cyberpunk_desc"),
                BoostedThemes = new() { GameTheme.Cyberpunk, GameTheme.SciFi, GameTheme.PostApoc },
                PenalizedThemes = new() { GameTheme.Historical, GameTheme.Western },
                ScoreBonus = 8f,
                SalesBonus = 0.40f,
                RemainingMonths = 12 + rng.Next(12)
            },
            new()
            {
                Name = Loc.Tr("trend.retro"),
                Description = Loc.Tr("trend.retro_desc"),
                BoostedThemes = new() { GameTheme.Historical, GameTheme.Western, GameTheme.Myth },
                PenalizedThemes = new() { GameTheme.Cyberpunk, GameTheme.Space },
                ScoreBonus = 6f,
                SalesBonus = 0.22f,
                RemainingMonths = 12 + rng.Next(18)
            },
            new()
            {
                Name = Loc.Tr("trend.romance"),
                Description = Loc.Tr("trend.romance_desc"),
                BoostedThemes = new() { GameTheme.Romance, GameTheme.School, GameTheme.Comedy },
                PenalizedThemes = new() { GameTheme.War, GameTheme.Horror },
                ScoreBonus = 5f,
                SalesBonus = 0.30f,
                RemainingMonths = 10 + rng.Next(14)
            },
            new()
            {
                Name = Loc.Tr("trend.horror"),
                Description = Loc.Tr("trend.horror_desc"),
                BoostedThemes = new() { GameTheme.Horror, GameTheme.Mystery, GameTheme.PostApoc },
                PenalizedThemes = new() { GameTheme.Comedy, GameTheme.Romance, GameTheme.School },
                ScoreBonus = 10f,
                SalesBonus = 0.28f,
                RemainingMonths = 12 + rng.Next(12)
            },
        };

        return all[rng.Next(all.Length)];
    }

    /// <summary>计算某个类型×主题在浪潮中的分数加成</summary>
    public float GetTrendScoreBonus(GameGenre genre, GameTheme theme)
    {
        float bonus = 0;
        foreach (var trend in ActiveTrends)
        {
            if (trend.BoostedGenres.Contains(genre)) bonus += trend.ScoreBonus;
            if (trend.PenalizedGenres.Contains(genre)) bonus -= trend.ScoreBonus * 0.5f;
            if (trend.BoostedThemes.Contains(theme)) bonus += trend.ScoreBonus * 0.7f;
            if (trend.PenalizedThemes.Contains(theme)) bonus -= trend.ScoreBonus * 0.4f;
        }
        return bonus;
    }

    /// <summary>计算某个类型×主题在浪潮中的销量加成</summary>
    public float GetTrendSalesBonus(GameGenre genre, GameTheme theme)
    {
        float bonus = 0;
        foreach (var trend in ActiveTrends)
        {
            if (trend.BoostedGenres.Contains(genre)) bonus += trend.SalesBonus;
            if (trend.PenalizedGenres.Contains(genre)) bonus -= trend.SalesBonus * 0.5f;
            if (trend.BoostedThemes.Contains(theme)) bonus += trend.SalesBonus * 0.6f;
            if (trend.PenalizedThemes.Contains(theme)) bonus -= trend.SalesBonus * 0.35f;
        }
        return Mathf.Clamp(bonus, -0.3f, 0.6f);
    }

    /// <summary>每月更新市场风口周期</summary>
    private void UpdateHypeCycles()
    {
        var rng = new Random();
        _hypeCycleCounter++;

        // 每6~12个月随机产生一个新风口
        if (_hypeCycleCounter % (6 + rng.Next(6)) == 0)
        {
            bool isGenre = rng.Next(2) == 0;
            if (isGenre)
            {
                var genres = Enum.GetValues<GameGenre>();
                var g = genres[rng.Next(genres.Length)];
                // 避免同时有两个同类型风口
                if (!HypeCycles.Any(h => h.Genre == g))
                {
                    HypeCycles.Add(new MarketHypeCycle
                    {
                        Genre = g,
                        Popularity = 0.4f,
                        Velocity = rng.NextDouble() > 0.5f ? 0.06f : 0.08f,
                        MonthsLeft = 12 + rng.Next(24),
                        PhaseMonths = 3 + rng.Next(6)
                    });
                }
            }
            else
            {
                var themes = Enum.GetValues<GameTheme>();
                var t = themes[rng.Next(themes.Length)];
                if (!HypeCycles.Any(h => h.Theme == t))
                {
                    HypeCycles.Add(new MarketHypeCycle
                    {
                        Theme = t,
                        Popularity = 0.4f,
                        Velocity = rng.NextDouble() > 0.5f ? 0.05f : 0.07f,
                        MonthsLeft = 10 + rng.Next(20),
                        PhaseMonths = 3 + rng.Next(6)
                    });
                }
            }
        }

        // 更新已有风口
        for (int i = HypeCycles.Count - 1; i >= 0; i--)
        {
            var h = HypeCycles[i];
            h.MonthsLeft--;
            h.PhaseMonths--;

            // 正弦波模拟：先升后降
            if (h.PhaseMonths > 0)
            {
                h.Popularity = Mathf.Clamp(h.Popularity + h.Velocity, 0.05f, 0.95f);
            }
            else
            {
                h.Popularity = Mathf.Clamp(h.Popularity - h.Velocity * 0.7f, 0.05f, 0.95f);
                // 可能重新升温
                if (rng.NextDouble() < 0.08f)
                {
                    h.PhaseMonths = 2 + rng.Next(5);
                    h.Velocity = 0.03f + (float)rng.NextDouble() * 0.05f;
                }
            }

            if (h.MonthsLeft <= 0 || h.Popularity < 0.08f)
                HypeCycles.RemoveAt(i);
        }
    }

    /// <summary>市场预测：模拟未来N个月热度变化</summary>
    public Dictionary<string, List<MarketPredictionEntry>> PredictHypes(int monthsAhead)
    {
        var result = new Dictionary<string, List<MarketPredictionEntry>>();

        // 为每个活跃的风口模拟未来走势
        foreach (var h in HypeCycles)
        {
            string label = h.Genre.HasValue ? h.Genre.Value.Name() : h.Theme.Value.Name();
            var entries = new List<MarketPredictionEntry>();

            float simPop = h.Popularity;
            float simVel = h.Velocity;
            int simPhase = h.PhaseMonths;

            for (int m = 0; m <= monthsAhead; m++)
            {
                string dir;
                if (simPhase > 0)
                {
                    simPop = Mathf.Clamp(simPop + simVel, 0.05f, 0.95f);
                    dir = "↑";
                    simPhase--;
                }
                else
                {
                    simPop = Mathf.Clamp(simPop - simVel * 0.7f, 0.05f, 0.95f);
                    dir = simPop < h.Popularity * 0.5f ? "↓" : "→";
                    // 随机重新升温
                    if (new Random().NextDouble() < 0.06f)
                    {
                        simPhase = 2 + new Random().Next(4);
                        simVel = 0.03f + (float)new Random().NextDouble() * 0.04f;
                    }
                }
                entries.Add(new MarketPredictionEntry
                {
                    MonthOffset = m,
                    Label = label,
                    Popularity = simPop,
                    DirectionIcon = dir
                });
            }
            result[label] = entries;
        }
        return result;
    }
}

/// <summary>
/// 单个市场浪潮
/// </summary>
public class MarketTrend
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<GameGenre> BoostedGenres { get; set; } = new();
    public List<GameGenre> PenalizedGenres { get; set; } = new();
    public List<GameTheme> BoostedThemes { get; set; } = new();
    public List<GameTheme> PenalizedThemes { get; set; } = new();
    public float ScoreBonus { get; set; }      // 评分加成
    public float SalesBonus { get; set; }      // 销量加成倍率
    public int RemainingMonths { get; set; }
}

/// <summary>
/// 市场风口周期 — 类型/主题周期性流行与衰退
/// </summary>
public class MarketHypeCycle
{
    public GameGenre? Genre { get; set; }
    public GameTheme? Theme { get; set; }
    public float Popularity { get; set; } = 0.5f; // 0-1
    public float Velocity { get; set; } = 0f;      // 正数=升温中
    public int MonthsLeft { get; set; }            // 剩余月份
    public int PhaseMonths { get; set; }           // 当前阶段月数

    public string PopularityLabel => Popularity > 0.8f ? "🔥 火爆"
        : Popularity > 0.6f ? "📈 热门"
        : Popularity > 0.4f ? "📊 平稳"
        : Popularity > 0.2f ? "📉 降温"
        : "❄ 寒冬";

    public float SalesBonus => 1f + (Popularity - 0.5f) * 0.4f; // 0.8-1.2
    public float ScoreBonus => (Popularity - 0.5f) * 8f;         // -4到+4
}

/// <summary>
/// 市场预测结果条目
/// </summary>
public class MarketPredictionEntry
{
    public int MonthOffset { get; set; }
    public string Label { get; set; }
    public float Popularity { get; set; }
    public string DirectionIcon { get; set; } // ↑ → ↓
}
