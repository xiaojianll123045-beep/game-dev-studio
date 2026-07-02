using System;
using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

/// <summary>
/// AI竞争对手工作室
/// </summary>
public partial class AIStudio
{
    public string Name { get; set; }
    public int FoundedMonth { get; set; }
    public int Reputation { get; set; } = 50;               // 声誉 0~100
    public float Money { get; set; } = 1000000f;
    public int EmployeeCount { get; set; } = 5;

    // 科技水平
    public Dictionary<string, bool> TechLevels { get; set; } = new();

    // 已发售游戏
    public List<AIGameRelease> Releases { get; set; } = new();

    // 粉丝与IP
    public int Fans { get; set; }
    public int IPCount => Releases.GroupBy(r => (r.Genre, r.Theme)).Count(g => g.Count() >= 2);

    // 是否购买了玩家的引擎
    public bool HasPlayerEngine { get; set; }
    public bool IsAcquired { get; set; }
    public bool FundedByPlayer { get; set; }              // 被玩家独立基金资助
    public int FundedMonth { get; set; }                   // 被资助月份

    // ══════ AI 策略 ══════
    public AIStrategy Strategy { get; set; } = AIStrategy.Balanced;

    // ══════ 股市 ══════
    public bool IsListed { get; set; }
    public float SharePrice { get; set; } = 100f;
    public int SharesOutstanding { get; set; } = 10000;
    public float MarketCap => SharePrice * SharesOutstanding;
    public float LastQuarterProfit { get; set; }
    public float LastQuarterRevenue { get; set; }
    public float LastQuarterExpense { get; set; }
    public float ExpectedProfit { get; set; }      // 分析师预期（业绩是否超预期）
    public float DividendRate { get; set; }         // 0~1，分红比例
    public int BankruptcyCounter { get; set; }     // 资金<0的连续月数
    public float TradingVolume { get; set; }       // 本季度交易量

    // 股东：谁持有多少股（Key=股东名，Value=股数），Loc.Tr("comp.shareholder_public")=市场流通，Loc.Tr("compname.founder")=创始团队
    public Dictionary<string, int> Shareholders { get; set; } = new();

    // 该工作室持有的其他公司股份（Key=公司名，Value=股数）
    public Dictionary<string, int> Portfolio { get; set; } = new();
    // 股价历史（最近 36 个月）
    public List<(int month, float price)> PriceHistory { get; set; } = new();

    public struct AIGameRelease
    {
        public string Name;
        public float Score;
        public int Sales;
        public int ReleaseMonth;
        public GameGenre Genre;
        public GameTheme Theme;
    }
}

/// <summary>
/// 竞争对手与市场动态管理器
/// </summary>
public partial class CompetitorAI : Node
{
    public List<AIStudio> Studios { get; private set; } = new();
    private int _gameMonth;

    // 即将发布的AI游戏（档期）
    public List<(AIStudio studio, AIStudio.AIGameRelease game, int monthsUntil)> UpcomingReleases { get; private set; } = new();

    // ══════ 新闻播报 ══════
    public struct NewsItem
    {
        public int Month;
        public string Emoji;
        public string Headline;
        public string Detail;
        public Color Color;
    }
    public List<NewsItem> NewsFeed { get; private set; } = new();
    public int MaxNews = 10000;

    // ══════ 市场情绪 ══════
    public float MarketSentiment { get; set; } = 1.0f; // 1.0=中性，>1牛市，<1熊市
    private float _sentimentBase = 1.0f;
    private int _sentimentPhase = 0; // 当前牛熊阶段
    public string MarketPhase => _sentimentBase > 1.2f ? $"{Loc.Tr("ui.market_phase_bull")}" : _sentimentBase > 1.05f ? $"{Loc.Tr("ui.market_phase_rise")}" : _sentimentBase > 0.95f ? $"{Loc.Tr("ui.market_phase_stable")}" : _sentimentBase > 0.8f ? $"{Loc.Tr("ui.market_phase_fall")}" : $"{Loc.Tr("ui.market_phase_bear")}";

    private static List<string> _namePrefixes;
    private static List<string> _surnames;
    private static int _loadedLang = -1;

    private static string[] GetNameSuffixes()
    {
        return Loc.SplitLocaleList(Loc.Tr("compname.suffixes"));
    }

    private TechManager _techMgr;
    private ResourceManager _res;
    private GameManager _gm;

    private static string LangSuffix()
    {
        var map = new[] { "zh", "en", "ja", "ko" };
        int idx = Math.Clamp(Loc.CurrentLang, 0, map.Length - 1);
        return map[idx];
    }

    public override void _Ready()
    {
        _techMgr = GetNode<TechManager>("../TechManager");
        _res = GetNode<ResourceManager>("../ResourceManager");
        _gm = GetNode<GameManager>("..");
        string ls = LangSuffix();
        if (_loadedLang != Loc.CurrentLang)
        {
            _namePrefixes = LoadJsonArray($"res://assets/公司名随机前缀_{ls}.json");
            _surnames = LoadJsonArray($"res://assets/姓氏_{ls}.json");
            _loadedLang = Loc.CurrentLang;
        }
        if (GlobalSettings.NewGame) InitStudios();
    }

    private static List<string> LoadJsonArray(string path)
    {
        try
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null) return new List<string>();
            string text = file.GetAsText();
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(text) ?? new List<string>();
        }
        catch { return new List<string>(); }
    }

    private string[] GetLocalePrefixes() => Loc.SplitLocaleList(Loc.Tr("compname.prefixes"));
    private string[] GetLocalePersonNames() => Loc.SplitLocaleList(Loc.Tr("person.firstnames"));
    private string[] GetLocalePersonLasts() => Loc.SplitLocaleList(Loc.Tr("person.lastnames"));
    // 人名用字/词：支持逗号分隔（英文/日文/韩文）或连续汉字（中文逐字拆开）
    private static string[] SplitPersonNames(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return new[] { "?" };
        // 阿拉伯语逗号和 ASCII 逗号都支持
        if (raw.Contains(',') || raw.Contains('،'))
            return Loc.SplitLocaleList(raw);
        return raw.ToCharArray().Select(c => c.ToString()).ToArray();
    }
    private string[] GetPersonGivenNames()
    {
        var raw = Loc.Tr("person.lastnames");
        return SplitPersonNames(raw);
    }

    private string RandomChineseName(RandomNumberGenerator rng)
    {
        // 中文用原有JSON文件，其他语言用locale
        if (Loc.CurrentLang == 0)
        {
            var pfx = _namePrefixes;
            var sfx = GetNameSuffixes();
            if (pfx == null || pfx.Count == 0) return Loc.TrF("compname.fallback", rng.RandiRange(100, 999));
            int count = rng.RandiRange(1, 2);
            string name = "";
            for (int c = 0; c < count; c++)
                name += pfx[rng.RandiRange(0, pfx.Count - 1)];
            name += sfx[rng.RandiRange(0, sfx.Length - 1)];
            return name;
        }
        else
        {
            var pfx = GetLocalePrefixes();
            var sfx = GetNameSuffixes();
            if (pfx.Length == 0) return Loc.TrF("compname.fallback", rng.RandiRange(100, 999));
            string name = pfx[rng.RandiRange(0, pfx.Length - 1)].Trim();
            // 阿拉伯语只取1个前缀（名字已够长）
            bool isAr = Loc.CurrentLang == 10;
            if (!isAr && rng.RandiRange(0, 1) == 0 && pfx.Length > 1)
                name += " " + pfx[rng.RandiRange(0, pfx.Length - 1)].Trim();
            name += " " + sfx[rng.RandiRange(0, sfx.Length - 1)];
            return name;
        }
    }

    private string RandomPersonName(RandomNumberGenerator rng)
    {
        var sn = _surnames;
        var givenPool = GetPersonGivenNames();
        if (sn == null || sn.Count == 0) return Loc.Tr("compname.employee");
        string surname = sn[rng.RandiRange(0, sn.Count - 1)];
        // 阿拉伯语只取1个given name
        bool isAr = Loc.CurrentLang == 10;
        int parts = isAr ? 1 : rng.RandiRange(1, 2);
        string given = "";
        for (int k = 0; k < parts; k++)
            given += givenPool[rng.RandiRange(0, givenPool.Length - 1)];
        return Loc.CurrentLang switch
        {
            1 => given + " " + surname, // 英文: John Smith
            10 => surname + " " + given, // 阿拉伯语: محمد علي
            _ => surname + given,         // 中日韩: 张伟 / 佐藤太郎 / 김민수
        };
    }

    public string GetRandomName() { var rng = new RandomNumberGenerator(); return RandomChineseName(rng); }
    public string GetRandomPersonName() { var rng = new RandomNumberGenerator(); return RandomPersonName(rng); }

    private void InitStudios()
    {
        var rng = new RandomNumberGenerator();
        int count = rng.RandiRange(90, 115);
        var usedNames = new HashSet<string>();
        for (int i = 0; i < count; i++)
        {
            // 生成不重名公司（最多尝试100次）
            string name;
            int tries = 0;
            do { name = RandomChineseName(rng); tries++; }
            while (usedNames.Contains(name) && tries < 100);
            // 极端情况加数字后缀
            if (usedNames.Contains(name)) name = name + (usedNames.Count + 1);
            usedNames.Add(name);

            var studio = new AIStudio
            {
                Name = name,
                FoundedMonth = rng.RandiRange(-120, -1),
                Reputation = rng.RandiRange(10, 85),
                Money = rng.RandiRange(50000, 8000000),
                EmployeeCount = rng.RandiRange(1, 35),
                IsListed = rng.RandiRange(0, 100) < 25,
            };
            studio.FoundedMonth = -Mathf.Max(1, Mathf.Abs(studio.FoundedMonth));

            // ── 初始作品：成立时间越久、声誉越高，作品越多 ──
            int monthsAlive = Mathf.Abs(studio.FoundedMonth);
            int baseWorks = Mathf.Max(1, monthsAlive / 12 + rng.RandiRange(-1, 3));
            int works = Mathf.Clamp(baseWorks + (studio.Reputation / 15), 1, 25);
            int totalSales = 0;
            var genres = Enum.GetValues<GameGenre>();
            var themes = Enum.GetValues<GameTheme>();
            float totalInitialRevenue = 0;
            for (int w = 0; w < works; w++)
            {
                int releaseMonth = studio.FoundedMonth + rng.RandiRange(1, Mathf.Max(2, monthsAlive - 1));
                var game = new AIStudio.AIGameRelease
                {
                    Name = Loc.TrF("compname.ai_game", studio.Name, Guid.NewGuid().ToString()[..3]),
                    ReleaseMonth = releaseMonth,
                    Genre = genres[rng.RandiRange(1, genres.Length - 1)],
                    Theme = themes[rng.RandiRange(1, themes.Length - 1)],
                };
                float baseScore = studio.Reputation * 0.5f + rng.RandiRange(15, 45);
                game.Score = Mathf.Clamp(baseScore, 25, 92);
                game.Sales = (int)(game.Score * 200 + game.Score * game.Score * 8 + rng.RandiRange(-500, 500));
                if (game.Sales < 100) game.Sales = rng.RandiRange(100, 500);
                totalSales += game.Sales;
                totalInitialRevenue += game.Sales * 50f * 0.4f; // 历史累计收入
                studio.Releases.Add(game);
            }
            // 作品按发售时间排序
            studio.Releases = studio.Releases.OrderBy(r => r.ReleaseMonth).ToList();
            // 粉丝 ≈ 累计销量 / 50，加一点随机
            studio.Fans = Mathf.Max(50, totalSales / 50 + rng.RandiRange(-500, 2000));
            // 初始季利润（基于最后3部作品）
            var last3 = studio.Releases.TakeLast(3).ToList();
            studio.LastQuarterProfit = last3.Sum(r => r.Sales * 50f * 0.4f) - studio.EmployeeCount * 12000f;
            studio.LastQuarterRevenue = last3.Sum(r => r.Sales * 50f * 0.4f);
            studio.LastQuarterExpense = studio.EmployeeCount * 12000f;

            // ── 资金：历史收入 - 成立至今的运营支出 ──
            float historicalExpenses = monthsAlive * (studio.EmployeeCount * 4000f);
            float baseMoney = Mathf.Max(10000f, totalInitialRevenue - historicalExpenses + rng.RandiRange(-50000, 500000));
            studio.Money = baseMoney;

            // 股价和市值基于声誉和资金
            if (studio.IsListed)
            {
                studio.SharePrice = Mathf.Max(10, studio.Reputation * 2f + rng.RandiRange(-30, 60));
                studio.SharesOutstanding = rng.RandiRange(3000, 25000);
                studio.Money = Mathf.Max(studio.Money, studio.MarketCap * 0.05f);
            }
            else
            {
                studio.SharePrice = 0;
                studio.SharesOutstanding = 0;
            }

            // 初始化股东（仅上市）
            if (studio.IsListed)
            {
                // 真实分布：创始人持股15%~65%，其余归公众/机构
                float founderPct = (rng.RandiRange(10, 60)) / 100f;
                int founderShares = Mathf.Max(1, (int)(studio.SharesOutstanding * founderPct));
                int publicShares = studio.SharesOutstanding - founderShares;
                studio.Shareholders[Loc.Tr("compname.founder")] = founderShares;
                if (rng.RandiRange(0, 100) < 35) // 35%有机构投资者
                {
                    int instShares = (int)(publicShares * (rng.RandiRange(20, 60) / 100f));
                    studio.Shareholders[Loc.Tr("stock.holder_institution")] = instShares;
                    studio.Shareholders[Loc.Tr("stock.holder_public")] = publicShares - instShares;
                }
                else
                    studio.Shareholders[Loc.Tr("stock.holder_public")] = publicShares;
                studio.ExpectedProfit = studio.LastQuarterProfit;
                studio.DividendRate = rng.RandiRange(0, 25) / 100f;
            }

            // 科技
            foreach (var kv in TechTreeData.AllTech)
            {
                if (rng.RandiRange(0, 100) <
                    (kv.Value.Branch == TechBranch.Render2D ? 30 :
                     kv.Value.Branch == TechBranch.ProgramBase ? 25 :
                     kv.Value.Branch == TechBranch.Audio ? 30 :
                     10))
                    studio.TechLevels[kv.Key] = true;
            }
            Studios.Add(studio);
        }
    }

    /// <summary>
    /// 每月推进
    /// </summary>
    public void MonthlyUpdate()
    {
        ModAPI.FireHooks(ModAPI.GameHook.BeforeCompetitorUpdate, new() { ["month"] = _gameMonth });
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeCompetitorUpdate)) return;
        _gameMonth++;

        var rng = new RandomNumberGenerator();

        // ── AI公司每月支出（工资+运营）──
        foreach (var studio in Studios)
        {
            if (studio.IsAcquired) continue;
            float salary = studio.EmployeeCount * 3000f; // 人均¥3000/月
            float rent = studio.EmployeeCount * 1000f;   // 房租运维
            float totalExp = salary + rent;
            studio.Money = Mathf.Max(0, studio.Money - totalExp);
            // 资金耗尽影响声誉
            if (studio.Money < 10000 && rng.RandiRange(0, 100) < 15)
                studio.Reputation = Mathf.Max(5, studio.Reputation - rng.RandiRange(1, 5));
            // 粉丝自然衰减（每月-0.2%，多作品公司粉丝黏性高，衰减减半）
            int fanDecay = (int)(studio.Fans * 0.002f);
            fanDecay = Mathf.Max(1, fanDecay) / (studio.Releases.Count >= 5 ? 2 : 1);
            studio.Fans = Mathf.Max(0, studio.Fans - fanDecay);
        }

        // ── 处理即将发布 ──
        for (int i = UpcomingReleases.Count - 1; i >= 0; i--)
        {
            var info = UpcomingReleases[i];
            info.monthsUntil--;
            if (info.monthsUntil <= 0)
            {
                info.studio.Releases.Add(info.game);
                info.studio.Fans += info.game.Sales / 50;
                // 发售收入（均价≈¥50/份，扣除平台分成后40%实收）
                float revenue = info.game.Sales * 50f * 0.4f;
                info.studio.Money += revenue;
                info.studio.Reputation = (int)Mathf.Clamp(info.studio.Reputation + (info.game.Score - 50) * 0.3f, 10, 100);
                int scoreI = (int)info.game.Score;
                string grade = scoreI >= 90 ? "🔥" : scoreI >= 75 ? "👍" : "👎";
                PushNews("🎮", Loc.TrF("news.release_title", info.studio.Name, info.game.Name),
                    $"{info.game.Genre.ToString()}×{info.game.Theme.ToString()}  " + Loc.TrF("news.release_detail", info.game.Score, grade, FormatSales(info.game.Sales)),
                    info.game.Score >= 80 ? new Color(0.3f, 0.8f, 0.4f) : new Color(0.8f, 0.8f, 0.3f));

                // ── 玩家资助的工作室发布游戏 → 生态反馈 ──
                if (info.studio.FundedByPlayer && _gameMonth - info.studio.FundedMonth >= 4 && _gameMonth - info.studio.FundedMonth <= 14)
                {
                    var resMgr = _gm.GetNodeOrNull<ResourceManager>("ResourceManager");
                    if (info.game.Score >= 75)
                        resMgr?.ShowEcoSuccess(info.studio.Name, info.game.Name, info.game.Score);
                    else if (info.game.Score < 50)
                        resMgr?.ShowEcoFailure(info.studio.Name);
                    info.studio.FundedByPlayer = false;
                }

                UpcomingReleases.RemoveAt(i);
            }
            else
            {
                UpcomingReleases[i] = info;
            }
        }

        // ── 自适应AI：安排新游戏 ──
        AllocateStudioStrategies(rng);
        if (rng.RandiRange(0, 3) == 0)
        {
            foreach (var studio in Studios)
            {
                if (studio.IsAcquired) continue;
                if (rng.RandiRange(0, 100) > 25) continue;

                CheckEnginePurchase(studio);

                var game = new AIStudio.AIGameRelease();
                int devMonths = rng.RandiRange(3, 12);

                // 自适应策略
                switch (studio.Strategy)
                {
                    case AIStrategy.Copycat:
                        // 抄袭者：检测玩家最高分游戏
                        var playerTop = GetPlayerTopRelease(_gameMonth);
                        if (playerTop.HasValue && rng.RandiRange(0, 100) < 60)
                        {
                            game.Genre = playerTop.Value.Item1;
                            game.Theme = playerTop.Value.Item2;
                            devMonths = Math.Max(2, (int)(devMonths * 0.8f)); // -20%开发时间
                            game.Name = Loc.TrF("comp.game_copycat", studio.Name, game.Genre.ToString(), game.Theme.ToString(), Guid.NewGuid().ToString()[..3]);
                        }
                        else goto default;
                        break;

                    case AIStrategy.Aggressive:
                        // 狙击者：检测玩家正在开发的类型
                        var playerPlanning = GetPlayerPlanningGenre();
                        if (playerPlanning.HasValue && rng.RandiRange(0, 100) < 50)
                        {
                            game.Genre = playerPlanning.Value;
                            int playerTarget = GetPlayerTargetMonth();
                            // 至少需要 4 个月开发时间，不能草草赶工自杀
                            devMonths = Mathf.Clamp(playerTarget - _gameMonth - 1, 4, 18);
                            game.Name = Loc.TrF("comp.game_sniper", studio.Name, game.Genre.ToString(), Guid.NewGuid().ToString()[..3]);
                        }
                        else goto default;
                        break;

                    case AIStrategy.NicheHunter:
                        // 蓝海猎手：选冷门组合
                        var (nicheG, nicheT) = GetNicheCombo(rng);
                        game.Genre = nicheG;
                        game.Theme = nicheT;
                        game.Name = Loc.TrF("comp.game_niche", studio.Name, game.Genre.ToString(), game.Theme.ToString(), Guid.NewGuid().ToString()[..3]);
                        break;

                    default:
                        var genres = Enum.GetValues<GameGenre>();
                        var themes = Enum.GetValues<GameTheme>();
                        game.Genre = genres[rng.RandiRange(1, genres.Length - 1)];
                        game.Theme = themes[rng.RandiRange(1, themes.Length - 1)];
                        game.Name = Loc.TrF("comp.game_hit", studio.Name, Guid.NewGuid().ToString()[..4]);
                        break;
                }

                game.ReleaseMonth = _gameMonth + devMonths;

                float baseScore = studio.Reputation * 0.5f + rng.RandiRange(20, 40);
                // 开发周期影响质量：低于 6 个月有惩罚
                if (devMonths < 6) baseScore -= (6 - devMonths) * 4f;
                if (studio.HasPlayerEngine) baseScore += 15;
                if (studio.Strategy == AIStrategy.Aggressive) baseScore += 5; // 狙击加成
                if (studio.Strategy == AIStrategy.NicheHunter) baseScore += 8; // 蓝海红利
                // 狙击者如果声誉太低也做不出像样的狙击作
                if (studio.Strategy == AIStrategy.Aggressive && studio.Reputation < 40)
                    baseScore = Mathf.Min(baseScore, 50);
                game.Score = Mathf.Clamp(baseScore, 20, 95);
                game.Sales = (int)(game.Score * 300 + game.Score * game.Score * 10);

                UpcomingReleases.Add((studio, game, devMonths));
                PushNews("🎮", Loc.TrF("news.announce_title", studio.Name),
                    Loc.TrF("news.announce_detail", game.Name, game.Genre.ToString(), game.Theme.ToString(), game.ReleaseMonth),
                    studio.Strategy == AIStrategy.Aggressive ? new Color(1f, 0.4f, 0.3f) :
                    studio.Strategy == AIStrategy.Copycat ? new Color(0.9f, 0.5f, 0.2f) :
                    new Color(0.4f, 0.7f, 1f));
            }
        }

        // ── 传奇遗产引来的关注 ──
        if (_gm.HasLegendaryLegacy && _gameMonth % 6 == 0 && rng.RandiRange(0, 100) < 40)
        {
            PushNews("🏆", Loc.Tr("news.legend_industry_title"),
                Loc.Tr("news.legend_industry_detail"),
                new Color(1f, 0.85f, 0.1f));
        }

        // ── 季度财报 + 股市模拟 ──
        if (_gameMonth % 3 == 0)
        {
            // 市场情绪漂移（缓慢随机游走）
            _sentimentPhase++;
            _sentimentBase += rng.RandiRange(-3, 3) / 100f;
            _sentimentBase = Mathf.Clamp(_sentimentBase + (float)Mathf.Sin(_sentimentPhase * 0.3f) * 0.005f, 0.75f, 1.3f);
            MarketSentiment = _sentimentBase + rng.RandiRange(-3, 3) / 100f; // 日内波动

            // 牛市/熊市转向时发新闻
            float prevPhase = _sentimentBase - rng.RandiRange(-3, 3) / 100f;
            if ((prevPhase < 1.0f && _sentimentBase >= 1.05f) || (prevPhase > 1.0f && _sentimentBase <= 0.95f))
            {
                string direction = _sentimentBase >= 1.05f ? Loc.Tr("news.bull_come") : Loc.Tr("news.bear_come");
                PushNews(_sentimentBase >= 1.05f ? "🐂" : "🐻", direction,
                    $"Sentiment {_sentimentBase:P0}  {(_sentimentBase >= 1.05f ? Loc.Tr("news.sentiment_high") : Loc.Tr("news.sentiment_low"))}",
                    _sentimentBase >= 1.05f ? new Color(0.9f, 0.2f, 0.1f) : new Color(0.3f, 0.4f, 0.9f));
            }

            foreach (var studio in Studios)
            {
                if (studio.IsAcquired) continue;

                // ── 计算本季度真实财务 ──
                // 季度营收：最近3个月发售的游戏收入 + License收入
                float qRevenue = 0;
                var recentReleases = studio.Releases
                    .Where(r => r.ReleaseMonth > _gameMonth - 3 && r.ReleaseMonth <= _gameMonth);
                foreach (var r in recentReleases)
                    qRevenue += r.Sales * 50f * 0.4f;
                float qExpense = studio.EmployeeCount * 12000f; // 3个月工资+运维
                float qProfit = qRevenue - qExpense;
                // 兜底：哪怕没游戏也有微小收支波动
                if (qRevenue < 1000) qRevenue = rng.RandiRange(500, 5000);
                if (qProfit > -qExpense * 0.5f) qProfit += rng.RandiRange(-5000, 10000); // 小型杂项

                studio.LastQuarterRevenue = qRevenue;
                studio.LastQuarterExpense = qExpense;
                studio.LastQuarterProfit = qProfit;

                // 资金影响
                studio.Money = Mathf.Max(-50000f, studio.Money + qProfit);

                // ── 破产风险 ──
                if (studio.Money < 0)
                {
                    studio.BankruptcyCounter++;
                    studio.Reputation = Mathf.Max(5, studio.Reputation - rng.RandiRange(2, 8));
                    // 破产退市
                    if (studio.BankruptcyCounter >= 6 || studio.Money < -studio.MarketCap * 0.5f)
                    {
                        studio.IsListed = false;
                        studio.SharePrice = 0;
                        studio.Money = 0;
                        PushNews("💀", $"{studio.Name} {Loc.Tr("news.bankruptcy")}",
                            $"{studio.BankruptcyCounter}q loss, insolvent",
                            new Color(0.6f, 0.15f, 0.1f));
                        // 持股者的股票清零
                        studio.Shareholders.Clear();
                        continue;
                    }
                    if (studio.BankruptcyCounter == 3)
                        PushNews("⚠️", Loc.TrF("news.crisis_title", studio.Name),
                            Loc.TrF("news.crisis_detail", studio.Money),
                            new Color(0.9f, 0.5f, 0.1f));
                }
                else
                    studio.BankruptcyCounter = Mathf.Max(0, studio.BankruptcyCounter - 1);

                // ── IPO ──
                if (!studio.IsListed && studio.Reputation >= 55
                    && _gameMonth - studio.FoundedMonth > 24
                    && studio.Money > 50000
                    && rng.RandiRange(0, 100) < 10)
                {
                    studio.IsListed = true;
                    // 发行价基于P/E倍数：利润越高倍数越高
                    float peRatio = qProfit > 0 ? Mathf.Clamp(8 + qProfit / 200000f, 5, 30) : 10;
                    studio.SharePrice = Mathf.Max(15f, (qProfit > 0 ? qProfit : 10000f) * peRatio / 10000f + rng.RandiRange(-10, 15));
                    studio.SharesOutstanding = 5000 + rng.RandiRange(5000, 15000);
                    float founderPct2 = (rng.RandiRange(10, 60)) / 100f;
                    int fShares2 = Mathf.Max(1, (int)(studio.SharesOutstanding * founderPct2));
                    int pShares = studio.SharesOutstanding - fShares2;
                    studio.Shareholders[Loc.Tr("compname.founder")] = fShares2;
                    studio.Shareholders[Loc.Tr("comp.shareholder_public")] = pShares;
                    studio.ExpectedProfit = qProfit;
                    studio.DividendRate = rng.RandiRange(0, 25) / 100f;
                    studio.PriceHistory.Clear();
                    studio.PriceHistory.Add((_gameMonth, studio.SharePrice));
                    PushNews("📈", Loc.TrF("news.ipo_title", studio.Name),
                        Loc.TrF("news.ipo_detail_fmt", studio.SharePrice, studio.MarketCap/1000000f, peRatio),
                        new Color(0.3f, 0.85f, 0.5f));
                    continue; // 刚上市不参与波动
                }

                if (!studio.IsListed) continue;

                // ── PE 定价模型 ──
                // EPS = 每股收益 = 季度利润 / 总股本
                float eps = studio.SharesOutstanding > 0 ? qProfit / (float)studio.SharesOutstanding : 0;
                // 行业 PE（基于声誉和市场规模）
                float industryPE = Mathf.Clamp(8 + studio.Reputation * 0.15f + studio.MarketCap / 5000000f, 5, 40);
                // 增长溢价（利润增长时 PE 更高）
                float growthPremium = studio.ExpectedProfit > 1000 && studio.ExpectedProfit < qProfit ? 1.2f : 1f;
                // 市场情绪溢价
                float sentimentPremium = 0.5f + MarketSentiment * 0.5f;
                // 目标价 = EPS × PE × 增长溢价 × 情绪溢价
                float targetPrice = eps * industryPE * growthPremium * sentimentPremium;
                targetPrice = Mathf.Max(3f, targetPrice);

                // ── 实际价格向目标价回归（每季度回归30%）──
                float oldPrice = studio.SharePrice;
                float priceChange = (targetPrice - oldPrice) * 0.3f;

                // ── 交易量影响（大额交易改变价格）──
                float volumeImpact = (studio.TradingVolume / (float)studio.SharesOutstanding) * 20f;
                priceChange += volumeImpact;
                studio.TradingVolume = 0; // 季度重置

                // 噪音
                priceChange += rng.RandiRange(-5, 5);
                studio.SharePrice = Mathf.Max(3f, studio.SharePrice + priceChange);
                // 更新预期
                studio.ExpectedProfit = studio.ExpectedProfit * 0.6f + qProfit * 0.4f;

                // ── 分红 ──
                if (qProfit > 0 && studio.DividendRate > 0 && rng.RandiRange(0, 100) < 60)
                {
                    float divPerShare = qProfit * studio.DividendRate / studio.SharesOutstanding;
                    if (divPerShare > 0.01f)
                    {
                        float totalDiv = divPerShare * studio.SharesOutstanding;
                        studio.Money -= totalDiv;
                        studio.SharePrice += divPerShare * 3f;
                    }
                }

                // ── 重大波动新闻 ──
                float pctChange = (studio.SharePrice - oldPrice) / oldPrice * 100;
                if (Mathf.Abs(pctChange) > 15)
                {
                    string dir = pctChange > 0 ? Loc.Tr("news.price_surge") : Loc.Tr("news.price_plunge");
                    float surprise = studio.ExpectedProfit > 1000 ? (qProfit - studio.ExpectedProfit) / Mathf.Abs(studio.ExpectedProfit) : 0;
                    string reason = surprise > 0.3f ? Loc.Tr("news.earnings_beat") : surprise < -0.3f ? Loc.Tr("news.earnings_miss") :
                                    MarketSentiment > 1.1f ? Loc.Tr("news.bull_drag") : MarketSentiment < 0.9f ? Loc.Tr("news.bear_drag") : "";
                    PushNews(pctChange > 0 ? "🚀" : "💥",
                        Loc.TrF("news.price_dir", dir) + $" {studio.Name} {pctChange:F0}%",
                        Loc.TrF("news.price_detail", oldPrice, studio.SharePrice, qProfit, reason),
                        pctChange > 0 ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.9f, 0.25f, 0.2f));
                }
                // 财报新闻（不带股价大波动的）
                else if (Mathf.Abs(qProfit) > 200000)
                {
                    string prefix = qProfit > 0 ? "📊" : "📉";
                    PushNews(prefix, $"{studio.Name} {Loc.Tr("news.quarterly")}",
                        Loc.TrF("news.quarterly_detail", qRevenue, qProfit, qProfit > 0 ? Loc.Tr("news.beat_expect") : ""),
                        qProfit > 0 ? new Color(0.4f, 0.7f, 0.4f) : new Color(0.5f, 0.4f, 0.3f));
                }
            }

            // ── 公司间互相买卖股份 ──
            SimulateInterCompanyTrading(rng);
        }

        // ── 挖角系统：AI 挖玩家墙角 ──
        if (rng.RandiRange(0, 100) < 30) // 每月 30% 概率尝试挖角
        {
            var empMgr = _gm.GetNodeOrNull<EmployeeManager>("EmployeeManager");
            var devMgr = _gm.GetNodeOrNull<GameDevManager>("GameDevManager");
            if (empMgr != null && devMgr != null)
            {
                // 找最容易被挖的员工：高野心+低忠诚
                var targets = empMgr.Employees
                    .Where(e => e.Ambition >= 50 && e.Loyalty <= 50 && !e.ConsideringOffer)
                    .OrderByDescending(e => e.Ambition - e.Loyalty)
                    .ToList();
                if (targets.Count > 0)
                {
                    var target = targets[0];
                    // 找有钱的AI工作室来挖
                    var richStudios = Studios.Where(s => !s.IsAcquired && s.Money > 50000).ToList();
                    if (richStudios.Count > 0)
                    {
                        var raider = richStudios[rng.RandiRange(0, richStudios.Count - 1)];
                        float offer = target.Salary * (1.5f + target.Ambition / 100f * 2f);
                        if (raider.Money >= offer * 0.3f)
                        {
                            target.ConsideringOffer = true;
                            target.OfferAmount = offer;
                            target.OfferCountdown = rng.RandiRange(1, 4);
                            target.TargetCompanyName = raider.Name;
                            raider.Money -= offer * 0.1f; // 挖角定金
                            _gm.ShowToast("🔍", Loc.TrF("toast.poach_attempt", target.Name, raider.Name), new Color(0.9f, 0.5f, 0.1f));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 公司间互相买卖股份，生成交叉持股网络
    /// </summary>
    private void SimulateInterCompanyTrading(RandomNumberGenerator rng)
    {
        var listed = Studios.Where(s => s.IsListed && !s.IsAcquired).ToList();
        if (listed.Count < 2) return;

        // 每季度有~60%的上市公司进行一次交易
        foreach (var buyer in Studios)
        {
            if (buyer.IsAcquired) continue;
            if (rng.RandiRange(0, 100) > 60) continue; // 40%概率参与交易

            float cashReserve = buyer.Money * (rng.RandiRange(10, 30) / 100f); // 用10%~30%闲置资金投资
            if (cashReserve < 5000) continue;

            // ── 决定买入还是卖出 ──
            bool isSelling = buyer.Portfolio.Count > 0 && rng.RandiRange(0, 100) < 30; // 30%概率抛售

            if (isSelling)
            {
                // 抛售：随机选一笔持股卖出
                var targets = buyer.Portfolio.Where(kv =>
                {
                    var target = Studios.FirstOrDefault(s => s.Name == kv.Key);
                    return target is { IsListed: true, IsAcquired: false };
                }).ToList();
                if (targets.Count == 0) continue;

                var pick = targets[rng.RandiRange(0, targets.Count - 1)];
                var targetStudio = Studios.First(s => s.Name == pick.Key);
                int sellQty = Mathf.Min(pick.Value, rng.RandiRange(pick.Value / 4, pick.Value)); // 卖25%~100%
                if (sellQty <= 0) continue;

                float sellPrice = targetStudio.SharePrice;
                float proceeds = sellQty * sellPrice;
                buyer.Money += proceeds;
                buyer.Portfolio[pick.Key] -= sellQty;
                if (buyer.Portfolio[pick.Key] <= 0) buyer.Portfolio.Remove(pick.Key);
                // 卖的股份回到公众池
                targetStudio.Shareholders[Loc.Tr("comp.shareholder_public")] = targetStudio.Shareholders.GetValueOrDefault(Loc.Tr("comp.shareholder_public"), 0) + sellQty;
                // 大量抛售压股价
                float totalShares = targetStudio.SharesOutstanding;
                float impact = (float)sellQty / totalShares * 0.3f;
                float oldPrice = targetStudio.SharePrice;
                targetStudio.SharePrice = Mathf.Max(5f, targetStudio.SharePrice * (1f - impact));
                targetStudio.TradingVolume += sellQty;

                if (sellQty > totalShares * 0.03f) // 超过3%发新闻
                    PushNews("📉", $"{buyer.Name} {Loc.Tr("news.reduce_hold")} {targetStudio.Name}",
                        Loc.TrF("news.sell_news", sellQty, sellPrice, oldPrice, targetStudio.SharePrice),
                        new Color(0.9f, 0.35f, 0.25f));
            }
            else
            {
                // 买入：选一家上市公司（不是自己，不是已经大量持有的）
                var targets = listed.Where(s => s != buyer && !buyer.Portfolio.Any(p =>
                    p.Key == s.Name && (float)p.Value / s.SharesOutstanding > 0.15f)).ToList(); // 不买超过15%的
                if (targets.Count == 0) continue;
                var target = targets[rng.RandiRange(0, targets.Count - 1)];

                // 可购股数：公众持有的，不超过自己现金
                int publicAvail = target.Shareholders.GetValueOrDefault(Loc.Tr("comp.shareholder_public"), 0);
                if (publicAvail <= 0) continue;
                int maxBuy = Mathf.Min(publicAvail, (int)(cashReserve / target.SharePrice));
                if (maxBuy < 5) continue;
                int buyQty = rng.RandiRange(Mathf.Max(1, maxBuy / 4), maxBuy);

                float cost = buyQty * target.SharePrice;
                if (buyer.Money < cost) continue;
                buyer.Money -= cost;
                buyer.Portfolio.TryGetValue(target.Name, out int existing);
                buyer.Portfolio[target.Name] = existing + buyQty;
                target.Shareholders[Loc.Tr("comp.shareholder_public")] -= buyQty;
                if (target.Shareholders[Loc.Tr("comp.shareholder_public")] <= 0) target.Shareholders.Remove(Loc.Tr("comp.shareholder_public"));
                // 买方标识存入被买方股东列表
                target.Shareholders.TryGetValue(buyer.Name, out int already);
                target.Shareholders[buyer.Name] = already + buyQty;

                // 大量买入推升股价
                float totalSh = target.SharesOutstanding;
                float impact2 = (float)buyQty / totalSh * 0.3f;
                float oldPr = target.SharePrice;
                target.SharePrice = Mathf.Min(5000f, target.SharePrice * (1f + impact2));
                target.TradingVolume += buyQty;

                if (buyQty > totalSh * 0.02f) // 超过2%发新闻
                    PushNews("📈", Loc.TrF("news.buy_title", buyer.Name, target.Name),
                        Loc.TrF("news.buy_detail", buyQty, target.SharePrice, (float)(existing + buyQty) / totalSh * 100),
                        new Color(0.3f, 0.7f, 0.3f));
            }
        }
        ModAPI.FireHooks(ModAPI.GameHook.AfterCompetitorUpdate);
    }

    public void PushNews(string emoji, string headline, string detail, Color color)
    {
        NewsFeed.Insert(0, new NewsItem { Month = _gameMonth, Emoji = emoji, Headline = headline, Detail = detail, Color = color });
        while (NewsFeed.Count > MaxNews) NewsFeed.RemoveAt(NewsFeed.Count - 1);
    }

    private static string FormatSales(int n) => n >= 1000000 ? $"{n / 1000000f:F1}M" : n >= 1000 ? $"{n / 1000f:F1}K" : $"{n}";

    private static string LvDesc(Employee e) => e.GetHighestLevel() switch
    {
        >= 5 => Loc.Tr("grade.5"),
        >= 4 => Loc.Tr("grade.4"),
        >= 3 => Loc.Tr("grade.3"),
        _ => Loc.Tr("grade.0")
    };

    /// <summary>
    /// AI是否购买玩家引擎
    /// </summary>
    private void CheckEnginePurchase(AIStudio studio)
    {
        if (!_techMgr.EngineOpenForLicense || studio.HasPlayerEngine) return;

        var rng = new RandomNumberGenerator();
        bool willBuy = _techMgr.EngineModel switch
        {
            EngineBizModel.OpenSource => rng.RandiRange(0, 100) > 30,  // 70%会免费拿
            EngineBizModel.Buyout => rng.RandiRange(0, 100) > 60,       // 40%会买
            EngineBizModel.Subscription => rng.RandiRange(0, 100) > 70, // 30%会订阅
            EngineBizModel.Royalty => rng.RandiRange(0, 100) > 50,      // 50%会签分成
            _ => false
        };

        if (willBuy)
        {
            studio.HasPlayerEngine = true;
            // 同步玩家所有科技
            foreach (var kv in _techMgr.ResearchedTech)
                if (kv.Value)
                    studio.TechLevels[kv.Key] = true;

            _techMgr.EngineLicenseCount++;

            // 收入
            float income = _techMgr.EngineModel switch
            {
                EngineBizModel.Buyout => _techMgr.BuyoutPrice,
                EngineBizModel.Subscription => _techMgr.SubscriptionPrice,
                _ => 0
            };
            if (income > 0)
                _res.EarnMoney(income * 5, "engine");   // 引擎授权核心收入×5

            _techMgr.EngineMarketShare += 0.02f;
            GD.Print($"[引擎] {studio.Name} 购买了你的引擎授权！");
        }
    }

    /// <summary>
    /// 检查档期冲突
    /// </summary>
    public float GetScheduleConflictPenalty(int currentMonth)
    {
        int nearby = UpcomingReleases.Count(r => r.monthsUntil >= -1 && r.monthsUntil <= 1);
        if (nearby == 0) return 0;
        return Mathf.Min(0.3f, nearby * 0.1f); // 最多-30%
    }

    // ══════════════════ AI自适应策略辅助 ══════════════════

    private void AllocateStudioStrategies(RandomNumberGenerator rng)
    {
        foreach (var s in Studios)
        {
            if (s.Strategy != AIStrategy.Balanced) continue;
            int roll = rng.RandiRange(0, 100);
            s.Strategy = roll switch
            {
                < 25 => AIStrategy.Aggressive,
                < 50 => AIStrategy.Copycat,
                < 75 => AIStrategy.NicheHunter,
                _ => AIStrategy.Balanced
            };
        }
    }

    private (GameGenre, GameTheme)? GetPlayerTopRelease(int currentMonth)
    {
        var devMgr = _gm?.GetNodeOrNull<GameDevManager>("GameDevManager");
        if (devMgr == null) return null;
        var top = devMgr.CompletedProjects
            .Where(p => p.FinalScore > 80 && currentMonth - p.OriginalReleaseMonth <= 12)
            .OrderByDescending(p => p.FinalScore)
            .FirstOrDefault();
        return top == null ? null : ((GameGenre, GameTheme)?)(top.Genre, top.Theme);
    }

    private GameGenre? GetPlayerPlanningGenre()
    {
        var devMgr = _gm?.GetNodeOrNull<GameDevManager>("GameDevManager");
        if (devMgr == null) return null;
        return devMgr.Projects.FirstOrDefault(p => p.Phase == DevPhase.Developing)?.Genre;
    }

    private int GetPlayerTargetMonth()
    {
        var devMgr = _gm?.GetNodeOrNull<GameDevManager>("GameDevManager");
        if (devMgr == null) return 24;
        var p = devMgr.Projects.FirstOrDefault(p => p.Phase == DevPhase.Developing);
        if (p == null) return 24;
        return _gameMonth + (int)Math.Max(2, p.EstimatedMonths * (1f - p.DevProgress));
    }

    private (GameGenre, GameTheme) GetNicheCombo(RandomNumberGenerator rng)
    {
        var genres = Enum.GetValues<GameGenre>();
        var themes = Enum.GetValues<GameTheme>();
        var comboCounts = new Dictionary<(GameGenre, GameTheme), int>();
        foreach (var r in UpcomingReleases)
        {
            var key = (r.game.Genre, r.game.Theme);
            comboCounts[key] = comboCounts.GetValueOrDefault(key) + 1;
        }
        var all = new List<(GameGenre, GameTheme)>();
        foreach (var g in genres)
        foreach (var t in themes)
            all.Add((g, t));
        return all.OrderBy(k => comboCounts.GetValueOrDefault(k, 0)).ThenBy(_ => rng.RandiRange(0, 999)).First();
    }

    /// <summary>王牌制作人叛逃到目标公司</summary>
    public void HandleDefection(Employee emp, string companyName)
    {
        var studio = Studios.FirstOrDefault(s => s.Name == companyName);
        if (studio == null) return;

        studio.Reputation = Mathf.Min(100, studio.Reputation + 5);
        studio.EmployeeCount++;

        // 王牌带来的技术增益：该工作室后续游戏获得风格加成
        _defectedLegends[companyName] = _defectedLegends.GetValueOrDefault(companyName, 0) + 1;

        PushNews("💀", Loc.TrF("news.defection_title", emp.Name, companyName),
            Loc.TrF("news.defection_detail", emp.Name, companyName, emp.GetHighestLevel()),
            new Color(0.8f, 0.2f, 0.2f));
    }

    private Dictionary<string, int> _defectedLegends = new();
}
