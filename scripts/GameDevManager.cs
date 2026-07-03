using System;
using System.Linq;
using Godot;
using System.Collections.Generic;

/// <summary>
/// 发售后的额外内容类型（参考Game Dev Tycoon+mod社区成熟设计）
/// </summary>
public enum PostReleaseType
{
    None,
    BugFixPatch,      // Bug修复补丁（2月/3%成本）→ 稳定性↑、Bug↓
    ContentUpdate,    // 小型内容更新（4月/10%成本）→ 三围+5、延长寿命
    SkinDLC,          // 外观/皮肤DLC（2月/5%成本）→ 按粉丝数赚快钱
    Expansion,        // 大型资料片（5月/30%成本）→ 0.5规模新项目
    Remaster,         // 重制版（6月/50%成本）→ 全属性+15~20、换平台
    Port,             // 平台移植（3月/20%成本）→ 降价30%上另一平台
    Sequel,           // 续作（标准周期/60%成本）→ 品牌加成+15%~30%
    ModKit            // Mod工具包（2月/固定5万）→ 长尾销量衰减减半
}

/// <summary>
/// 游戏开发项目
/// </summary>
public partial class GameProject
{
    public string Name { get; set; }
    public GameGenre Genre { get; set; }
    public GameTheme Theme { get; set; }
    public Platform Platform { get; set; }
    public DevPhase Phase { get; set; } = DevPhase.Idle;

    // ── 项目规模/付费 ──
    public float Scale { get; set; } = 0.5f;
    public PriceModel PriceModel { get; set; } = PriceModel.BuyToPlay;
    public float AdIntensity { get; set; }
    public float SuggestedPrice { get; set; }

    // ── 六大模块品质属性 ──
    public float GraphicsScore { get; set; }
    public float GameplayScore { get; set; }
    public float AudioScore { get; set; }
    public float StoryScore { get; set; }       // 剧情/文案
    public float NetworkScore { get; set; }
    public float StabilityScore { get; set; }
    public float AIScore { get; set; }          // AI（保留兼容，影响玩法）

    // ── 六大模块进度 ──
    public float ModuleProgressCore { get; set; }     // 核心玩法
    public float ModuleProgressVisual { get; set; }   // 视觉表现
    public float ModuleProgressAudio { get; set; }    // 听觉设计
    public float ModuleProgressStory { get; set; }    // 剧情/文案
    public float ModuleProgressStability { get; set; }// 程序稳定性
    public float ModuleProgressOnline { get; set; }   // 线上服务

    // ── 打磨 ──
    public PolishStrategy PolishStrat { get; set; } = PolishStrategy.Standard;
    public int PolishMonths { get; set; }

    // ── 游戏组件 ──
    public List<string> EquippedComponents { get; set; } = new(); // 存组件ID
    public float SynergyBonus { get; set; }                       // 联动总加分
    public float ComponentDevPenalty { get; set; }                // 多组件导致的开发时间加成

    // 续作/IP
    public float PredecessorScore { get; set; }
    public int PredecessorSales { get; set; }
    public SequelStrategy SequelStrat { get; set; } = SequelStrategy.Cautious;
    public string IPName { get; set; } // IP系列名（空=新IP）
    public string LabelName { get; set; } // 发行标签（空=无标签）

    // ── 宣发冲刺期 ──
    public int MarketingSprintMonths { get; set; }        // 宣发期总月数 (3~5)
    public int MarketingSprintSpent { get; set; }         // 已过月数
    public float MarketingHype { get; set; }              // 期待值 (0~100+)
    public bool MarketingSprintStarted { get; set; }      // 是否已触发宣发期

    // 引擎
    public string EngineName { get; set; }        // 使用的引擎名称

    // 开发中累积分数（受团队技能×开发月数影响，后期越大）
    public float BaseProgramScore { get; set; }   // 程序分
    public float BaseArtScore { get; set; }       // 美术分
    public float BaseQualityScore { get; set; }   // 综合质量分（程序+美术均值）
    public string ScoreTier => BaseQualityScore < 10 ? "D" : BaseQualityScore < 20 ? "C" : BaseQualityScore < 35 ? "B" : BaseQualityScore < 55 ? "A" : BaseQualityScore < 80 ? "S" : "SS";
    public string ScoreTierIcon => ScoreTier switch { "SS" => "💎", "S" => "⭐", "A" => "🟢", "B" => "🟡", "C" => "🟠", _ => "🔴" };

    // 开发进度
    public float DevProgress { get; set; }        // 0~1
    public float EstimatedMonths { get; set; }
    public float MonthsSpent { get; set; }
    public float LastSprintMonth { get; set; } = -3f;  // 上次冲刺规划时的月份（初始-3确保首月触发）
    public int BugCount { get; set; }             // BUG数量

    // 宣发
    public MarketingStrategy Marketing { get; set; }
    public float MarketingBudget { get; set; }
    public float ExpectedScore { get; set; }      // 期待值（宣发画饼）

    // ── 卡牌加成
    public float MonthlySalesBonus { get; set; } = 1f;

    // ── 最终
    public float FinalScore { get; set; }
    public int Sales { get; set; }
    public float Revenue { get; set; }
    public bool IsReleased { get; set; }
    public int MonthsOnMarket { get; set; }               // 上市月数
    public float TotalLifetimeSales { get; set; }         // 预估终身总销量
    public List<float> MonthlySalesHistory { get; set; } = new(); // 每月实际入账

    // 契合度
    public float GenreThemeCompatibility { get; set; } // 类型×主题契合度
    public int MissingFeatures { get; set; }              // 缺失的引擎功能数

    // 历史记录
    public List<string> DevLog { get; set; } = new();

    // ── 性能仪表盘 ──
    public float MemoryUsage { get; set; }                // 内存占用 (GB)
    public float FpsEstimate { get; set; } = 60f;         // 帧率预估
    public float CrashRate { get; set; }                  // 崩溃率 0~1
    public int ComponentCount { get; set; }               // 已装备组件数
    // 平台红线
    public float PlatformMemoryLimit { get; set; }        // 当前平台内存上限 (GB)
    public float PlatformFpsTarget { get; set; }          // 当前平台帧率目标
    public bool MemoryOverLimit => MemoryUsage > PlatformMemoryLimit;
    public bool FpsBelowTarget => FpsEstimate < PlatformFpsTarget;
    public string PlatformStressLevel => (MemoryOverLimit || FpsBelowTarget) ? "danger" :
                                          (MemoryUsage > PlatformMemoryLimit * 0.8f || FpsEstimate < PlatformFpsTarget * 1.3f) ? "warn" : "ok";

    // 债务可视化
    public float DebtInterestRate { get; set; }           // 当前债务月利率 (0~0.1)
    public int NextMonthBugFromDebt { get; set; }         // 下月预计产生Bug数
    public float NextMonthSlowFromDebt { get; set; }      // 下月开发速度降低比例

    // 技术债务（项目级，续作复用代码或赶工产生）
    public float TechDebt { get; set; }
    // 跳票次数
    public int DelayCount { get; set; }

    // 中途决策
    public bool HasTriggeredMidDevDecision { get; set; }
    public bool HasTriggeredMidDevDecision2 { get; set; }

    // ── 开发资源分配滑块（玩家在开发期间可精细调节）──
    public float BudgetGraphics { get; set; } = 0.33f;    // 图形资源占比
    public float BudgetAudio { get; set; } = 0.33f;       // 音效资源占比
    public float BudgetGameplay { get; set; } = 0.34f;    // 玩法资源占比

    // ── 设计哲学（取代纯数值滑块的核心策略选择）──
    public DesignPhilosophy Philosophy { get; set; } = DesignPhilosophy.Balanced;

    // ── 阈值裂变标签 ──
    public string ThresholdTag { get; set; }               // 触发阈值裂变的标签（如"屎山神作"）

    // ── 绝境翻盘传奇 ──
    public bool LegendaryLegacy { get; set; }
    public float LegacyReputationBonus { get; set; }

    // ── 创意表达 ──
    public string GameDescription { get; set; }            // 游戏简介（玩家自定义）
    public string AutoTagline { get; set; }                // 自动生成的宣传语
    public string GameCoverStyle { get; set; }             // 封面风格描述
    // ── 后处理内容 ──
    public PostReleaseType PostRelease { get; set; } = PostReleaseType.None;
    public GameProject BaseProject { get; set; }          // 资料片/重制版/续作的原项目
    public string QATestReport { get; set; }              // QA测试报告
    public long DevelopmentCost { get; set; }             // 开发总成本（用于后续内容定价）
    public float FanSatisfaction { get; set; } = 1f;      // 粉丝满意度(0~2)，影响后续内容销量
    public float BrandPower { get; set; }                 // IP品牌力（自动计算：销量+评分）
    public int PostReleaseCount { get; set; }             // 已发布的后发内容数量
    public bool HasModKit { get; set; }                  // 是否已安装Mod工具包
    public int OriginalReleaseMonth { get; set; }        // 首次发售月份（续作间隔用）
    public bool IsLongTail { get; set; }                 // 是否享受长尾衰减减半
}

/// <summary>
/// 游戏开发流程管理器（5阶段）
/// </summary>
public partial class GameDevManager : Node
{
    // ── 跳票延期系统 ──
    /// <summary>玩家信任度 0~100，影响预售和首周销量</summary>
    public float PlayerTrust { get; set; } = 50f;
    /// <summary>连续跳票次数（惩罚递增）</summary>
    public int ConsecutiveDelays { get; set; }
    /// <summary>承诺未兑现次数</summary>
    public int BrokenPromises { get; set; }
    /// <summary>信任度历史曲线（用于UI显示）</summary>
    public List<float> TrustHistory { get; set; } = new();
    
    // 跳票原因文本
    public static readonly string[] DelayReasons = {
        "delay.quality",    // 为了品质打磨
        "delay.tech",       // 技术遇到瓶颈
        "delay.content",    // 需要增加更多内容
        "delay.staff",      // 开发团队需要调整
    };
    // ══════════════════ 公司级系统 ══════════════════
    public List<PublishingLabel> Labels { get; private set; } = new();
    public bool IsListed { get; set; }
    public float SharePrice { get; set; } = 100f;
    public int SharesOutstanding { get; set; } = 10000;
    public float IPOProceeds { get; set; }
    public float DividendRate { get; set; }
    public float ExpectedProfit { get; set; }
    public List<(int month, float price)> PriceHistory { get; set; } = new();
    public List<string> AcquiredIPs { get; private set; } = new();
    public bool HasTriggeredBankruptcyOffer { get; set; } // 限一次破产收购操作
    public bool IsPublisher { get; set; }                   // 是否已解锁发行商身份

    // ══════════════════ 发行系统 ══════════════════
    public List<PublishedProject> PublishedProjects { get; private set; } = new();
    public List<PublishedDeal> AvailableDeals { get; private set; } = new();
    public int PublishedGameCount { get; set; }              // 累计发行游戏数
    public float PublisherReputation { get; set; }           // 发行商声誉(0~1)
    public int _dealRefreshCounter;

    public class PublishingLabel
    {
        public string Name { get; set; }
        public GameGenre? PreferredGenre { get; set; }
        public GameTheme? PreferredTheme { get; set; }
        public int FoundedMonth { get; set; }
        public float Reputation { get; set; }
        public int GameCount { get; set; }
        public float TotalScore { get; set; }
        public float AvgScore => GameCount > 0 ? TotalScore / GameCount : 0;
    }

    /// <summary>发行项目（为其他工作室发行的游戏）</summary>
    public class PublishedProject
    {
        public string GameName { get; set; }
        public string StudioName { get; set; }
        public GameGenre Genre { get; set; }
        public GameTheme Theme { get; set; }
        public float ExpectedScore { get; set; }
        public float ActualScore { get; set; }
        public int Sales { get; set; }
        public float MarketingCost { get; set; }        // 玩家承担的宣发费
        public float RoyaltyRate { get; set; }          // 分成比例(0.15~0.40)
        public int ReleaseMonth { get; set; }
        public int MonthsOnMarket { get; set; }
        public float TotalRoyaltyEarned { get; set; }
        public List<float> MonthlyRoyaltyHistory { get; set; } = new();
        public bool IsReleased { get; set; }
        public bool IsPublished { get; set; }           // 是否已投产（资金已付）
    }

    /// <summary>发行邀约（AI工作室求发行）支持谈判</summary>
    public class PublishedDeal
    {
        public string StudioName { get; set; }
        public string GameName { get; set; }
        public GameGenre Genre { get; set; }
        public GameTheme Theme { get; set; }
        public float ExpectedScore { get; set; }
        public float MarketingCost { get; set; }          // 建议宣发费
        public float PlayerOfferMarketing { get; set; }   // 玩家出价宣发费
        public float RoyaltyRate { get; set; }            // 对方提议分成
        public float PlayerOfferRoyalty { get; set; }     // 玩家出价分成
        public int EstReleaseMonths { get; set; }
        public int MonthsRemaining { get; set; }
        public int DealMonth { get; set; }
        public bool IsNegotiating { get; set; }            // 是否正在谈判中
        public float StudioSatisfaction { get; set; } = 0.5f;  // 对方满意度(0~1)，≥0.3签
        public bool IsPlayerOffer { get; set; }            // 是否为玩家主动发起的邀约
    }

    // ══════════════════ 后发内容开发策略 ══════════════════
    public enum PostReleaseStrategy { Balanced, Aggressive, Conservative }

    // ══════════════════ 项目管理 ══════════════════
    public List<GameProject> Projects { get; private set; } = new();
    public List<GameProject> CompletedProjects { get; private set; } = new();
    public List<(int month, string gameName, long revenue)> MonthlyRevenueLog { get; private set; } = new();
    public List<(int month, long revenue, long expense)> MonthlyProfitLog { get; private set; } = new();
    public long TotalRevenue { get; set; }

    private GameManager _gm;
    private TechManager _techMgr;
    private EmployeeManager _empMgr;
    private ResourceManager _res;

    private TechDebtManager DebtMgr => _gm?.GetNodeOrNull<TechDebtManager>("TechDebtManager");

    // 类型×主题契合度表（隐藏——玩家需自行探索最佳组合）
    private static readonly Dictionary<(GameGenre, GameTheme), float> Compatibility = new()
    {
        // ── RPG ──
        [(GameGenre.RPG, GameTheme.Fantasy)] = 1.0f,
        [(GameGenre.RPG, GameTheme.SciFi)] = 0.85f,
        [(GameGenre.RPG, GameTheme.Historical)] = 0.8f,
        [(GameGenre.RPG, GameTheme.Myth)] = 0.95f,
        [(GameGenre.RPG, GameTheme.PostApoc)] = 0.7f,
        [(GameGenre.RPG, GameTheme.Western)] = 0.65f,
        [(GameGenre.RPG, GameTheme.School)] = 0.5f,
        [(GameGenre.RPG, GameTheme.Cyberpunk)] = 0.6f,
        [(GameGenre.RPG, GameTheme.Steampunk)] = 0.55f,
        [(GameGenre.RPG, GameTheme.Space)] = 0.7f,
        [(GameGenre.RPG, GameTheme.Horror)] = 0.4f,
        [(GameGenre.RPG, GameTheme.War)] = 0.65f,
        [(GameGenre.RPG, GameTheme.Mystery)] = 0.55f,
        [(GameGenre.RPG, GameTheme.Romance)] = 0.5f,
        [(GameGenre.RPG, GameTheme.Modern)] = 0.45f,
        [(GameGenre.RPG, GameTheme.Comedy)] = 0.35f,
        // ── ACT ──
        [(GameGenre.ACT, GameTheme.Fantasy)] = 0.85f,
        [(GameGenre.ACT, GameTheme.SciFi)] = 0.8f,
        [(GameGenre.ACT, GameTheme.PostApoc)] = 0.9f,
        [(GameGenre.ACT, GameTheme.Myth)] = 0.8f,
        [(GameGenre.ACT, GameTheme.War)] = 0.75f,
        [(GameGenre.ACT, GameTheme.Western)] = 0.7f,
        [(GameGenre.ACT, GameTheme.Cyberpunk)] = 0.85f,
        [(GameGenre.ACT, GameTheme.Modern)] = 0.75f,
        [(GameGenre.ACT, GameTheme.Steampunk)] = 0.6f,
        [(GameGenre.ACT, GameTheme.Historical)] = 0.7f,
        [(GameGenre.ACT, GameTheme.Space)] = 0.65f,
        [(GameGenre.ACT, GameTheme.Horror)] = 0.55f,
        [(GameGenre.ACT, GameTheme.School)] = 0.4f,
        [(GameGenre.ACT, GameTheme.Mystery)] = 0.5f,
        [(GameGenre.ACT, GameTheme.Romance)] = 0.3f,
        [(GameGenre.ACT, GameTheme.Comedy)] = 0.45f,
        // ── FPS ──
        [(GameGenre.FPS, GameTheme.Modern)] = 0.95f,
        [(GameGenre.FPS, GameTheme.War)] = 1.0f,
        [(GameGenre.FPS, GameTheme.SciFi)] = 0.9f,
        [(GameGenre.FPS, GameTheme.Cyberpunk)] = 0.95f,
        [(GameGenre.FPS, GameTheme.PostApoc)] = 0.85f,
        [(GameGenre.FPS, GameTheme.Horror)] = 0.65f,
        [(GameGenre.FPS, GameTheme.Steampunk)] = 0.55f,
        [(GameGenre.FPS, GameTheme.Space)] = 0.8f,
        [(GameGenre.FPS, GameTheme.Historical)] = 0.6f,
        [(GameGenre.FPS, GameTheme.Fantasy)] = 0.5f,
        [(GameGenre.FPS, GameTheme.Western)] = 0.55f,
        [(GameGenre.FPS, GameTheme.Myth)] = 0.35f,
        [(GameGenre.FPS, GameTheme.School)] = 0.2f,
        [(GameGenre.FPS, GameTheme.Comedy)] = 0.25f,
        [(GameGenre.FPS, GameTheme.Romance)] = 0.15f,
        [(GameGenre.FPS, GameTheme.Mystery)] = 0.3f,
        // ── HOR ──
        [(GameGenre.HOR, GameTheme.Horror)] = 1.0f,
        [(GameGenre.HOR, GameTheme.Mystery)] = 0.9f,
        [(GameGenre.HOR, GameTheme.PostApoc)] = 0.85f,
        [(GameGenre.HOR, GameTheme.Modern)] = 0.6f,
        [(GameGenre.HOR, GameTheme.SciFi)] = 0.55f,
        [(GameGenre.HOR, GameTheme.School)] = 0.5f,
        [(GameGenre.HOR, GameTheme.Cyberpunk)] = 0.7f,
        [(GameGenre.HOR, GameTheme.Fantasy)] = 0.4f,
        [(GameGenre.HOR, GameTheme.Myth)] = 0.35f,
        [(GameGenre.HOR, GameTheme.Steampunk)] = 0.45f,
        [(GameGenre.HOR, GameTheme.Historical)] = 0.4f,
        [(GameGenre.HOR, GameTheme.War)] = 0.35f,
        [(GameGenre.HOR, GameTheme.Western)] = 0.3f,
        [(GameGenre.HOR, GameTheme.Space)] = 0.5f,
        [(GameGenre.HOR, GameTheme.Comedy)] = 0.2f,
        [(GameGenre.HOR, GameTheme.Romance)] = 0.15f,
        // ── SIM ──
        [(GameGenre.SIM, GameTheme.Modern)] = 0.9f,
        [(GameGenre.SIM, GameTheme.Historical)] = 0.7f,
        [(GameGenre.SIM, GameTheme.SciFi)] = 0.65f,
        [(GameGenre.SIM, GameTheme.Space)] = 0.7f,
        [(GameGenre.SIM, GameTheme.School)] = 0.6f,
        [(GameGenre.SIM, GameTheme.Fantasy)] = 0.55f,
        [(GameGenre.SIM, GameTheme.Cyberpunk)] = 0.6f,
        [(GameGenre.SIM, GameTheme.Steampunk)] = 0.5f,
        [(GameGenre.SIM, GameTheme.PostApoc)] = 0.45f,
        [(GameGenre.SIM, GameTheme.War)] = 0.4f,
        [(GameGenre.SIM, GameTheme.Myth)] = 0.35f,
        [(GameGenre.SIM, GameTheme.Western)] = 0.45f,
        [(GameGenre.SIM, GameTheme.Horror)] = 0.3f,
        [(GameGenre.SIM, GameTheme.Comedy)] = 0.5f,
        [(GameGenre.SIM, GameTheme.Romance)] = 0.4f,
        [(GameGenre.SIM, GameTheme.Mystery)] = 0.4f,
        // ── SLG ──
        [(GameGenre.SLG, GameTheme.Historical)] = 0.9f,
        [(GameGenre.SLG, GameTheme.War)] = 1.0f,
        [(GameGenre.SLG, GameTheme.Modern)] = 0.75f,
        [(GameGenre.SLG, GameTheme.SciFi)] = 0.7f,
        [(GameGenre.SLG, GameTheme.Fantasy)] = 0.7f,
        [(GameGenre.SLG, GameTheme.PostApoc)] = 0.65f,
        [(GameGenre.SLG, GameTheme.Space)] = 0.8f,
        [(GameGenre.SLG, GameTheme.Cyberpunk)] = 0.6f,
        [(GameGenre.SLG, GameTheme.Steampunk)] = 0.55f,
        [(GameGenre.SLG, GameTheme.Myth)] = 0.65f,
        [(GameGenre.SLG, GameTheme.Western)] = 0.5f,
        [(GameGenre.SLG, GameTheme.Horror)] = 0.3f,
        [(GameGenre.SLG, GameTheme.School)] = 0.35f,
        [(GameGenre.SLG, GameTheme.Comedy)] = 0.4f,
        [(GameGenre.SLG, GameTheme.Romance)] = 0.3f,
        [(GameGenre.SLG, GameTheme.Mystery)] = 0.35f,
        // ── AVG ──
        [(GameGenre.AVG, GameTheme.Fantasy)] = 0.9f,
        [(GameGenre.AVG, GameTheme.Mystery)] = 0.85f,
        [(GameGenre.AVG, GameTheme.Horror)] = 0.7f,
        [(GameGenre.AVG, GameTheme.Modern)] = 0.75f,
        [(GameGenre.AVG, GameTheme.SciFi)] = 0.75f,
        [(GameGenre.AVG, GameTheme.School)] = 0.8f,
        [(GameGenre.AVG, GameTheme.Myth)] = 0.75f,
        [(GameGenre.AVG, GameTheme.Historical)] = 0.65f,
        [(GameGenre.AVG, GameTheme.Romance)] = 0.85f,
        [(GameGenre.AVG, GameTheme.Comedy)] = 0.7f,
        [(GameGenre.AVG, GameTheme.PostApoc)] = 0.6f,
        [(GameGenre.AVG, GameTheme.Cyberpunk)] = 0.7f,
        [(GameGenre.AVG, GameTheme.Steampunk)] = 0.6f,
        [(GameGenre.AVG, GameTheme.Space)] = 0.65f,
        [(GameGenre.AVG, GameTheme.Western)] = 0.5f,
        [(GameGenre.AVG, GameTheme.War)] = 0.45f,
        // ── RAC ──
        [(GameGenre.RAC, GameTheme.Modern)] = 0.95f,
        [(GameGenre.RAC, GameTheme.SciFi)] = 0.75f,
        [(GameGenre.RAC, GameTheme.Fantasy)] = 0.6f,
        [(GameGenre.RAC, GameTheme.Cyberpunk)] = 0.85f,
        [(GameGenre.RAC, GameTheme.PostApoc)] = 0.7f,
        [(GameGenre.RAC, GameTheme.Steampunk)] = 0.55f,
        [(GameGenre.RAC, GameTheme.Space)] = 0.65f,
        [(GameGenre.RAC, GameTheme.Historical)] = 0.35f,
        [(GameGenre.RAC, GameTheme.School)] = 0.4f,
        [(GameGenre.RAC, GameTheme.Comedy)] = 0.6f,
        [(GameGenre.RAC, GameTheme.Western)] = 0.45f,
        [(GameGenre.RAC, GameTheme.War)] = 0.3f,
        [(GameGenre.RAC, GameTheme.Horror)] = 0.25f,
        [(GameGenre.RAC, GameTheme.Myth)] = 0.3f,
        [(GameGenre.RAC, GameTheme.Mystery)] = 0.35f,
        [(GameGenre.RAC, GameTheme.Romance)] = 0.4f,
        // ── SPO ──
        [(GameGenre.SPO, GameTheme.Modern)] = 0.95f,
        [(GameGenre.SPO, GameTheme.School)] = 0.8f,
        [(GameGenre.SPO, GameTheme.Historical)] = 0.5f,
        [(GameGenre.SPO, GameTheme.Fantasy)] = 0.6f,
        [(GameGenre.SPO, GameTheme.SciFi)] = 0.7f,
        [(GameGenre.SPO, GameTheme.Comedy)] = 0.75f,
        [(GameGenre.SPO, GameTheme.Cyberpunk)] = 0.55f,
        [(GameGenre.SPO, GameTheme.Space)] = 0.5f,
        [(GameGenre.SPO, GameTheme.PostApoc)] = 0.4f,
        [(GameGenre.SPO, GameTheme.War)] = 0.35f,
        [(GameGenre.SPO, GameTheme.Steampunk)] = 0.4f,
        [(GameGenre.SPO, GameTheme.Western)] = 0.45f,
        [(GameGenre.SPO, GameTheme.Myth)] = 0.3f,
        [(GameGenre.SPO, GameTheme.Horror)] = 0.2f,
        [(GameGenre.SPO, GameTheme.Mystery)] = 0.35f,
        [(GameGenre.SPO, GameTheme.Romance)] = 0.55f,
        // ── MUS ──
        [(GameGenre.MUS, GameTheme.Comedy)] = 0.85f,
        [(GameGenre.MUS, GameTheme.Modern)] = 0.8f,
        [(GameGenre.MUS, GameTheme.Romance)] = 0.75f,
        [(GameGenre.MUS, GameTheme.School)] = 0.7f,
        [(GameGenre.MUS, GameTheme.Fantasy)] = 0.55f,
        [(GameGenre.MUS, GameTheme.SciFi)] = 0.5f,
        [(GameGenre.MUS, GameTheme.Historical)] = 0.4f,
        [(GameGenre.MUS, GameTheme.Myth)] = 0.45f,
        [(GameGenre.MUS, GameTheme.Cyberpunk)] = 0.55f,
        [(GameGenre.MUS, GameTheme.Space)] = 0.45f,
        [(GameGenre.MUS, GameTheme.PostApoc)] = 0.3f,
        [(GameGenre.MUS, GameTheme.War)] = 0.2f,
        [(GameGenre.MUS, GameTheme.Western)] = 0.35f,
        [(GameGenre.MUS, GameTheme.Steampunk)] = 0.4f,
        [(GameGenre.MUS, GameTheme.Horror)] = 0.15f,
        [(GameGenre.MUS, GameTheme.Mystery)] = 0.3f,
        // ── FTG ──
        [(GameGenre.FTG, GameTheme.Fantasy)] = 0.85f,
        [(GameGenre.FTG, GameTheme.Modern)] = 0.75f,
        [(GameGenre.FTG, GameTheme.SciFi)] = 0.7f,
        [(GameGenre.FTG, GameTheme.Myth)] = 0.8f,
        [(GameGenre.FTG, GameTheme.PostApoc)] = 0.65f,
        [(GameGenre.FTG, GameTheme.Historical)] = 0.7f,
        [(GameGenre.FTG, GameTheme.Cyberpunk)] = 0.75f,
        [(GameGenre.FTG, GameTheme.Steampunk)] = 0.55f,
        [(GameGenre.FTG, GameTheme.War)] = 0.45f,
        [(GameGenre.FTG, GameTheme.Space)] = 0.6f,
        [(GameGenre.FTG, GameTheme.Western)] = 0.5f,
        [(GameGenre.FTG, GameTheme.School)] = 0.45f,
        [(GameGenre.FTG, GameTheme.Horror)] = 0.4f,
        [(GameGenre.FTG, GameTheme.Comedy)] = 0.35f,
        [(GameGenre.FTG, GameTheme.Romance)] = 0.2f,
        [(GameGenre.FTG, GameTheme.Mystery)] = 0.3f,
        // ── MOBA ──
        [(GameGenre.MOBA, GameTheme.Fantasy)] = 0.9f,
        [(GameGenre.MOBA, GameTheme.SciFi)] = 0.8f,
        [(GameGenre.MOBA, GameTheme.Modern)] = 0.6f,
        [(GameGenre.MOBA, GameTheme.Myth)] = 0.75f,
        [(GameGenre.MOBA, GameTheme.Cyberpunk)] = 0.7f,
        [(GameGenre.MOBA, GameTheme.PostApoc)] = 0.55f,
        [(GameGenre.MOBA, GameTheme.Steampunk)] = 0.5f,
        [(GameGenre.MOBA, GameTheme.Historical)] = 0.55f,
        [(GameGenre.MOBA, GameTheme.War)] = 0.5f,
        [(GameGenre.MOBA, GameTheme.Space)] = 0.65f,
        [(GameGenre.MOBA, GameTheme.Western)] = 0.35f,
        [(GameGenre.MOBA, GameTheme.School)] = 0.4f,
        [(GameGenre.MOBA, GameTheme.Horror)] = 0.3f,
        [(GameGenre.MOBA, GameTheme.Comedy)] = 0.35f,
        [(GameGenre.MOBA, GameTheme.Romance)] = 0.25f,
        [(GameGenre.MOBA, GameTheme.Mystery)] = 0.3f,
        // ── MMO ──
        [(GameGenre.MMO, GameTheme.Fantasy)] = 0.95f,
        [(GameGenre.MMO, GameTheme.SciFi)] = 0.85f,
        [(GameGenre.MMO, GameTheme.Modern)] = 0.65f,
        [(GameGenre.MMO, GameTheme.PostApoc)] = 0.6f,
        [(GameGenre.MMO, GameTheme.Cyberpunk)] = 0.7f,
        [(GameGenre.MMO, GameTheme.Myth)] = 0.75f,
        [(GameGenre.MMO, GameTheme.Space)] = 0.8f,
        [(GameGenre.MMO, GameTheme.Steampunk)] = 0.55f,
        [(GameGenre.MMO, GameTheme.Historical)] = 0.6f,
        [(GameGenre.MMO, GameTheme.War)] = 0.5f,
        [(GameGenre.MMO, GameTheme.Western)] = 0.35f,
        [(GameGenre.MMO, GameTheme.School)] = 0.4f,
        [(GameGenre.MMO, GameTheme.Horror)] = 0.35f,
        [(GameGenre.MMO, GameTheme.Comedy)] = 0.35f,
        [(GameGenre.MMO, GameTheme.Romance)] = 0.3f,
        [(GameGenre.MMO, GameTheme.Mystery)] = 0.35f,
        // ── RTS ──
        [(GameGenre.RTS, GameTheme.SciFi)] = 0.9f,
        [(GameGenre.RTS, GameTheme.War)] = 0.95f,
        [(GameGenre.RTS, GameTheme.Historical)] = 0.8f,
        [(GameGenre.RTS, GameTheme.Fantasy)] = 0.7f,
        [(GameGenre.RTS, GameTheme.Modern)] = 0.75f,
        [(GameGenre.RTS, GameTheme.PostApoc)] = 0.65f,
        [(GameGenre.RTS, GameTheme.Space)] = 0.7f,
        [(GameGenre.RTS, GameTheme.Cyberpunk)] = 0.6f,
        [(GameGenre.RTS, GameTheme.Steampunk)] = 0.5f,
        [(GameGenre.RTS, GameTheme.Myth)] = 0.55f,
        [(GameGenre.RTS, GameTheme.Western)] = 0.4f,
        [(GameGenre.RTS, GameTheme.School)] = 0.25f,
        [(GameGenre.RTS, GameTheme.Horror)] = 0.3f,
        [(GameGenre.RTS, GameTheme.Comedy)] = 0.25f,
        [(GameGenre.RTS, GameTheme.Romance)] = 0.15f,
        [(GameGenre.RTS, GameTheme.Mystery)] = 0.2f,
        // ── SAN ──
        [(GameGenre.SAN, GameTheme.Modern)] = 0.95f,
        [(GameGenre.SAN, GameTheme.Space)] = 0.9f,
        [(GameGenre.SAN, GameTheme.Fantasy)] = 0.8f,
        [(GameGenre.SAN, GameTheme.PostApoc)] = 0.75f,
        [(GameGenre.SAN, GameTheme.SciFi)] = 0.85f,
        [(GameGenre.SAN, GameTheme.Cyberpunk)] = 0.7f,
        [(GameGenre.SAN, GameTheme.Steampunk)] = 0.6f,
        [(GameGenre.SAN, GameTheme.Historical)] = 0.55f,
        [(GameGenre.SAN, GameTheme.War)] = 0.5f,
        [(GameGenre.SAN, GameTheme.Myth)] = 0.5f,
        [(GameGenre.SAN, GameTheme.Western)] = 0.6f,
        [(GameGenre.SAN, GameTheme.School)] = 0.35f,
        [(GameGenre.SAN, GameTheme.Horror)] = 0.4f,
        [(GameGenre.SAN, GameTheme.Comedy)] = 0.45f,
        [(GameGenre.SAN, GameTheme.Romance)] = 0.3f,
        [(GameGenre.SAN, GameTheme.Mystery)] = 0.35f,
        // ── ROG ──
        [(GameGenre.ROG, GameTheme.Fantasy)] = 0.95f,
        [(GameGenre.ROG, GameTheme.Horror)] = 0.8f,
        [(GameGenre.ROG, GameTheme.SciFi)] = 0.85f,
        [(GameGenre.ROG, GameTheme.PostApoc)] = 0.75f,
        [(GameGenre.ROG, GameTheme.Myth)] = 0.8f,
        [(GameGenre.ROG, GameTheme.Cyberpunk)] = 0.7f,
        [(GameGenre.ROG, GameTheme.Steampunk)] = 0.65f,
        [(GameGenre.ROG, GameTheme.Modern)] = 0.5f,
        [(GameGenre.ROG, GameTheme.Historical)] = 0.5f,
        [(GameGenre.ROG, GameTheme.War)] = 0.4f,
        [(GameGenre.ROG, GameTheme.Space)] = 0.7f,
        [(GameGenre.ROG, GameTheme.Western)] = 0.4f,
        [(GameGenre.ROG, GameTheme.School)] = 0.35f,
        [(GameGenre.ROG, GameTheme.Comedy)] = 0.4f,
        [(GameGenre.ROG, GameTheme.Romance)] = 0.2f,
        [(GameGenre.ROG, GameTheme.Mystery)] = 0.55f,
        // ── VIS ──
        [(GameGenre.VIS, GameTheme.Romance)] = 0.9f,
        [(GameGenre.VIS, GameTheme.School)] = 0.85f,
        [(GameGenre.VIS, GameTheme.Comedy)] = 0.7f,
        [(GameGenre.VIS, GameTheme.Mystery)] = 0.8f,
        [(GameGenre.VIS, GameTheme.Modern)] = 0.75f,
        [(GameGenre.VIS, GameTheme.Historical)] = 0.7f,
        [(GameGenre.VIS, GameTheme.Fantasy)] = 0.65f,
        [(GameGenre.VIS, GameTheme.Horror)] = 0.6f,
        [(GameGenre.VIS, GameTheme.SciFi)] = 0.55f,
        [(GameGenre.VIS, GameTheme.Cyberpunk)] = 0.5f,
        [(GameGenre.VIS, GameTheme.Myth)] = 0.5f,
        [(GameGenre.VIS, GameTheme.PostApoc)] = 0.4f,
        [(GameGenre.VIS, GameTheme.War)] = 0.3f,
        [(GameGenre.VIS, GameTheme.Space)] = 0.4f,
        [(GameGenre.VIS, GameTheme.Steampunk)] = 0.45f,
        [(GameGenre.VIS, GameTheme.Western)] = 0.35f,
        // ── PZL ──
        [(GameGenre.PZL, GameTheme.Comedy)] = 0.85f,
        [(GameGenre.PZL, GameTheme.Modern)] = 0.8f,
        [(GameGenre.PZL, GameTheme.Fantasy)] = 0.7f,
        [(GameGenre.PZL, GameTheme.Mystery)] = 0.75f,
        [(GameGenre.PZL, GameTheme.SciFi)] = 0.65f,
        [(GameGenre.PZL, GameTheme.School)] = 0.7f,
        [(GameGenre.PZL, GameTheme.Myth)] = 0.55f,
        [(GameGenre.PZL, GameTheme.Historical)] = 0.5f,
        [(GameGenre.PZL, GameTheme.Cyberpunk)] = 0.5f,
        [(GameGenre.PZL, GameTheme.Space)] = 0.55f,
        [(GameGenre.PZL, GameTheme.Steampunk)] = 0.5f,
        [(GameGenre.PZL, GameTheme.PostApoc)] = 0.4f,
        [(GameGenre.PZL, GameTheme.War)] = 0.3f,
        [(GameGenre.PZL, GameTheme.Western)] = 0.4f,
        [(GameGenre.PZL, GameTheme.Horror)] = 0.35f,
        [(GameGenre.PZL, GameTheme.Romance)] = 0.55f,

        // ── ETC（派对游戏）──
        [(GameGenre.ETC, GameTheme.Comedy)] = 1.0f,
        [(GameGenre.ETC, GameTheme.Modern)] = 0.85f,
        [(GameGenre.ETC, GameTheme.School)] = 0.9f,
        [(GameGenre.ETC, GameTheme.Fantasy)] = 0.6f,
        [(GameGenre.ETC, GameTheme.SciFi)] = 0.55f,
        [(GameGenre.ETC, GameTheme.Space)] = 0.5f,
        [(GameGenre.ETC, GameTheme.Myth)] = 0.45f,
        [(GameGenre.ETC, GameTheme.Ninja)] = 0.6f,
        [(GameGenre.ETC, GameTheme.Pirate)] = 0.65f,
        [(GameGenre.ETC, GameTheme.Historical)] = 0.4f,
        [(GameGenre.ETC, GameTheme.Western)] = 0.55f,
        [(GameGenre.ETC, GameTheme.Cyberpunk)] = 0.35f,
        [(GameGenre.ETC, GameTheme.Steampunk)] = 0.4f,
        [(GameGenre.ETC, GameTheme.Horror)] = 0.25f,
        [(GameGenre.ETC, GameTheme.War)] = 0.2f,
        [(GameGenre.ETC, GameTheme.Romance)] = 0.5f,
        [(GameGenre.ETC, GameTheme.Mystery)] = 0.4f,
        [(GameGenre.ETC, GameTheme.PostApoc)] = 0.3f,
        [(GameGenre.ETC, GameTheme.Dungeon)] = 0.45f,
        [(GameGenre.ETC, GameTheme.Workplace)] = 0.7f,
        [(GameGenre.ETC, GameTheme.DeepSea)] = 0.4f,
        [(GameGenre.ETC, GameTheme.Victorian)] = 0.5f,

        // ── TOW（塔防）──
        [(GameGenre.TOW, GameTheme.War)] = 0.95f,
        [(GameGenre.TOW, GameTheme.Fantasy)] = 0.9f,
        [(GameGenre.TOW, GameTheme.SciFi)] = 0.85f,
        [(GameGenre.TOW, GameTheme.Historical)] = 0.8f,
        [(GameGenre.TOW, GameTheme.Cyberpunk)] = 0.75f,
        [(GameGenre.TOW, GameTheme.PostApoc)] = 0.85f,
        [(GameGenre.TOW, GameTheme.Space)] = 0.7f,
        [(GameGenre.TOW, GameTheme.Myth)] = 0.75f,
        [(GameGenre.TOW, GameTheme.Steampunk)] = 0.65f,
        [(GameGenre.TOW, GameTheme.Modern)] = 0.6f,
        [(GameGenre.TOW, GameTheme.Horror)] = 0.55f,
        [(GameGenre.TOW, GameTheme.Comedy)] = 0.45f,
        [(GameGenre.TOW, GameTheme.Romance)] = 0.2f,
        [(GameGenre.TOW, GameTheme.School)] = 0.35f,
        [(GameGenre.TOW, GameTheme.Mystery)] = 0.4f,
        [(GameGenre.TOW, GameTheme.Western)] = 0.5f,
        [(GameGenre.TOW, GameTheme.Ninja)] = 0.6f,
        [(GameGenre.TOW, GameTheme.Pirate)] = 0.65f,
        [(GameGenre.TOW, GameTheme.Dungeon)] = 0.8f,
        [(GameGenre.TOW, GameTheme.Workplace)] = 0.35f,
        [(GameGenre.TOW, GameTheme.DeepSea)] = 0.55f,
        [(GameGenre.TOW, GameTheme.Victorian)] = 0.45f,

        // ── SUR（生存）──
        [(GameGenre.SUR, GameTheme.PostApoc)] = 1.0f,
        [(GameGenre.SUR, GameTheme.Horror)] = 0.9f,
        [(GameGenre.SUR, GameTheme.DeepSea)] = 0.95f,
        [(GameGenre.SUR, GameTheme.Dungeon)] = 0.85f,
        [(GameGenre.SUR, GameTheme.Space)] = 0.85f,
        [(GameGenre.SUR, GameTheme.Modern)] = 0.7f,
        [(GameGenre.SUR, GameTheme.Historical)] = 0.75f,
        [(GameGenre.SUR, GameTheme.Fantasy)] = 0.6f,
        [(GameGenre.SUR, GameTheme.SciFi)] = 0.65f,
        [(GameGenre.SUR, GameTheme.War)] = 0.75f,
        [(GameGenre.SUR, GameTheme.Western)] = 0.7f,
        [(GameGenre.SUR, GameTheme.Pirate)] = 0.8f,
        [(GameGenre.SUR, GameTheme.Cyberpunk)] = 0.6f,
        [(GameGenre.SUR, GameTheme.Steampunk)] = 0.5f,
        [(GameGenre.SUR, GameTheme.Myth)] = 0.45f,
        [(GameGenre.SUR, GameTheme.School)] = 0.3f,
        [(GameGenre.SUR, GameTheme.Comedy)] = 0.35f,
        [(GameGenre.SUR, GameTheme.Romance)] = 0.15f,
        [(GameGenre.SUR, GameTheme.Mystery)] = 0.4f,
        [(GameGenre.SUR, GameTheme.Ninja)] = 0.55f,
        [(GameGenre.SUR, GameTheme.Workplace)] = 0.4f,
        [(GameGenre.SUR, GameTheme.Victorian)] = 0.5f,

        // ── MOV（互动电影）──
        [(GameGenre.MOV, GameTheme.Mystery)] = 0.95f,
        [(GameGenre.MOV, GameTheme.Modern)] = 0.9f,
        [(GameGenre.MOV, GameTheme.Horror)] = 0.85f,
        [(GameGenre.MOV, GameTheme.Romance)] = 0.9f,
        [(GameGenre.MOV, GameTheme.SciFi)] = 0.8f,
        [(GameGenre.MOV, GameTheme.Cyberpunk)] = 0.85f,
        [(GameGenre.MOV, GameTheme.Historical)] = 0.75f,
        [(GameGenre.MOV, GameTheme.Western)] = 0.7f,
        [(GameGenre.MOV, GameTheme.Fantasy)] = 0.65f,
        [(GameGenre.MOV, GameTheme.Space)] = 0.65f,
        [(GameGenre.MOV, GameTheme.DeepSea)] = 0.7f,
        [(GameGenre.MOV, GameTheme.War)] = 0.6f,
        [(GameGenre.MOV, GameTheme.PostApoc)] = 0.75f,
        [(GameGenre.MOV, GameTheme.Steampunk)] = 0.55f,
        [(GameGenre.MOV, GameTheme.Myth)] = 0.5f,
        [(GameGenre.MOV, GameTheme.School)] = 0.6f,
        [(GameGenre.MOV, GameTheme.Comedy)] = 0.55f,
        [(GameGenre.MOV, GameTheme.Dungeon)] = 0.4f,
        [(GameGenre.MOV, GameTheme.Workplace)] = 0.65f,
        [(GameGenre.MOV, GameTheme.Ninja)] = 0.5f,
        [(GameGenre.MOV, GameTheme.Pirate)] = 0.55f,
        [(GameGenre.MOV, GameTheme.Victorian)] = 0.7f,

        // ── 新主题 × 现有类型 ──
        [(GameGenre.RPG, GameTheme.Ninja)] = 0.7f,
        [(GameGenre.RPG, GameTheme.Pirate)] = 0.6f,
        [(GameGenre.RPG, GameTheme.Victorian)] = 0.55f,
        [(GameGenre.RPG, GameTheme.DeepSea)] = 0.5f,
        [(GameGenre.RPG, GameTheme.Dungeon)] = 0.9f,
        [(GameGenre.RPG, GameTheme.Workplace)] = 0.25f,
        [(GameGenre.ACT, GameTheme.Ninja)] = 0.9f,
        [(GameGenre.ACT, GameTheme.Pirate)] = 0.8f,
        [(GameGenre.ACT, GameTheme.Victorian)] = 0.55f,
        [(GameGenre.ACT, GameTheme.DeepSea)] = 0.5f,
        [(GameGenre.ACT, GameTheme.Dungeon)] = 0.85f,
        [(GameGenre.ACT, GameTheme.Workplace)] = 0.3f,
        [(GameGenre.FPS, GameTheme.Ninja)] = 0.4f,
        [(GameGenre.FPS, GameTheme.Pirate)] = 0.4f,
        [(GameGenre.FPS, GameTheme.Victorian)] = 0.35f,
        [(GameGenre.FPS, GameTheme.DeepSea)] = 0.35f,
        [(GameGenre.FPS, GameTheme.Dungeon)] = 0.3f,
        [(GameGenre.FPS, GameTheme.Workplace)] = 0.25f,
        [(GameGenre.AVG, GameTheme.Ninja)] = 0.6f,
        [(GameGenre.AVG, GameTheme.Pirate)] = 0.65f,
        [(GameGenre.AVG, GameTheme.Victorian)] = 0.75f,
        [(GameGenre.AVG, GameTheme.DeepSea)] = 0.7f,
        [(GameGenre.AVG, GameTheme.Dungeon)] = 0.7f,
        [(GameGenre.AVG, GameTheme.Workplace)] = 0.5f,
        [(GameGenre.SLG, GameTheme.Ninja)] = 0.65f,
        [(GameGenre.SLG, GameTheme.Pirate)] = 0.6f,
        [(GameGenre.SLG, GameTheme.Victorian)] = 0.55f,
        [(GameGenre.SLG, GameTheme.DeepSea)] = 0.5f,
        [(GameGenre.SLG, GameTheme.Dungeon)] = 0.6f,
        [(GameGenre.SLG, GameTheme.Workplace)] = 0.45f,
        [(GameGenre.SIM, GameTheme.Ninja)] = 0.35f,
        [(GameGenre.SIM, GameTheme.Pirate)] = 0.4f,
        [(GameGenre.SIM, GameTheme.Victorian)] = 0.55f,
        [(GameGenre.SIM, GameTheme.DeepSea)] = 0.5f,
        [(GameGenre.SIM, GameTheme.Dungeon)] = 0.4f,
        [(GameGenre.SIM, GameTheme.Workplace)] = 0.8f,
        [(GameGenre.RAC, GameTheme.Ninja)] = 0.35f,
        [(GameGenre.RAC, GameTheme.Pirate)] = 0.3f,
        [(GameGenre.RAC, GameTheme.Victorian)] = 0.4f,
        [(GameGenre.RAC, GameTheme.DeepSea)] = 0.2f,
        [(GameGenre.RAC, GameTheme.Dungeon)] = 0.2f,
        [(GameGenre.RAC, GameTheme.Workplace)] = 0.3f,
        [(GameGenre.SPO, GameTheme.Ninja)] = 0.3f,
        [(GameGenre.SPO, GameTheme.Pirate)] = 0.2f,
        [(GameGenre.SPO, GameTheme.Victorian)] = 0.35f,
        [(GameGenre.SPO, GameTheme.DeepSea)] = 0.2f,
        [(GameGenre.SPO, GameTheme.Dungeon)] = 0.2f,
        [(GameGenre.SPO, GameTheme.Workplace)] = 0.4f,
        [(GameGenre.MUS, GameTheme.Ninja)] = 0.3f,
        [(GameGenre.MUS, GameTheme.Pirate)] = 0.35f,
        [(GameGenre.MUS, GameTheme.Victorian)] = 0.55f,
        [(GameGenre.MUS, GameTheme.DeepSea)] = 0.4f,
        [(GameGenre.MUS, GameTheme.Dungeon)] = 0.25f,
        [(GameGenre.MUS, GameTheme.Workplace)] = 0.35f,
        [(GameGenre.FTG, GameTheme.Ninja)] = 0.9f,
        [(GameGenre.FTG, GameTheme.Pirate)] = 0.7f,
        [(GameGenre.FTG, GameTheme.Victorian)] = 0.5f,
        [(GameGenre.FTG, GameTheme.DeepSea)] = 0.4f,
        [(GameGenre.FTG, GameTheme.Dungeon)] = 0.65f,
        [(GameGenre.FTG, GameTheme.Workplace)] = 0.35f,
        [(GameGenre.MOBA, GameTheme.Ninja)] = 0.3f,
        [(GameGenre.MOBA, GameTheme.Pirate)] = 0.3f,
        [(GameGenre.MOBA, GameTheme.Victorian)] = 0.2f,
        [(GameGenre.MOBA, GameTheme.DeepSea)] = 0.3f,
        [(GameGenre.MOBA, GameTheme.Dungeon)] = 0.35f,
        [(GameGenre.MOBA, GameTheme.Workplace)] = 0.25f,
        [(GameGenre.MMO, GameTheme.Ninja)] = 0.3f,
        [(GameGenre.MMO, GameTheme.Pirate)] = 0.4f,
        [(GameGenre.MMO, GameTheme.Victorian)] = 0.3f,
        [(GameGenre.MMO, GameTheme.DeepSea)] = 0.3f,
        [(GameGenre.MMO, GameTheme.Dungeon)] = 0.55f,
        [(GameGenre.MMO, GameTheme.Workplace)] = 0.2f,
        [(GameGenre.RTS, GameTheme.Ninja)] = 0.55f,
        [(GameGenre.RTS, GameTheme.Pirate)] = 0.5f,
        [(GameGenre.RTS, GameTheme.Victorian)] = 0.5f,
        [(GameGenre.RTS, GameTheme.DeepSea)] = 0.35f,
        [(GameGenre.RTS, GameTheme.Dungeon)] = 0.4f,
        [(GameGenre.RTS, GameTheme.Workplace)] = 0.3f,
        [(GameGenre.HOR, GameTheme.Ninja)] = 0.35f,
        [(GameGenre.HOR, GameTheme.Pirate)] = 0.4f,
        [(GameGenre.HOR, GameTheme.Victorian)] = 0.6f,
        [(GameGenre.HOR, GameTheme.DeepSea)] = 0.7f,
        [(GameGenre.HOR, GameTheme.Dungeon)] = 0.65f,
        [(GameGenre.HOR, GameTheme.Workplace)] = 0.3f,
        [(GameGenre.SAN, GameTheme.Ninja)] = 0.4f,
        [(GameGenre.SAN, GameTheme.Pirate)] = 0.5f,
        [(GameGenre.SAN, GameTheme.Victorian)] = 0.45f,
        [(GameGenre.SAN, GameTheme.DeepSea)] = 0.6f,
        [(GameGenre.SAN, GameTheme.Dungeon)] = 0.65f,
        [(GameGenre.SAN, GameTheme.Workplace)] = 0.35f,
        [(GameGenre.ROG, GameTheme.Ninja)] = 0.55f,
        [(GameGenre.ROG, GameTheme.Pirate)] = 0.55f,
        [(GameGenre.ROG, GameTheme.Victorian)] = 0.45f,
        [(GameGenre.ROG, GameTheme.DeepSea)] = 0.55f,
        [(GameGenre.ROG, GameTheme.Dungeon)] = 0.95f,
        [(GameGenre.ROG, GameTheme.Workplace)] = 0.2f,
        [(GameGenre.VIS, GameTheme.Ninja)] = 0.4f,
        [(GameGenre.VIS, GameTheme.Pirate)] = 0.4f,
        [(GameGenre.VIS, GameTheme.Victorian)] = 0.7f,
        [(GameGenre.VIS, GameTheme.DeepSea)] = 0.45f,
        [(GameGenre.VIS, GameTheme.Dungeon)] = 0.4f,
        [(GameGenre.VIS, GameTheme.Workplace)] = 0.6f,
        [(GameGenre.PZL, GameTheme.Ninja)] = 0.4f,
        [(GameGenre.PZL, GameTheme.Pirate)] = 0.45f,
        [(GameGenre.PZL, GameTheme.Victorian)] = 0.5f,
        [(GameGenre.PZL, GameTheme.DeepSea)] = 0.4f,
        [(GameGenre.PZL, GameTheme.Dungeon)] = 0.55f,
        [(GameGenre.PZL, GameTheme.Workplace)] = 0.5f,
    };

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        _techMgr = GetNode<TechManager>("../TechManager");
        _empMgr = GetNode<EmployeeManager>("../EmployeeManager");
        _res = GetNode<ResourceManager>("../ResourceManager");
    }

    /// <summary>
    /// 立项
    /// </summary>
    public GameProject CreateProject(string name, GameGenre genre, GameTheme theme,
        Platform platform, float estimatedMonths, MarketingStrategy marketing,
        float marketingBudget, float scale = 0.5f, PriceModel priceModel = PriceModel.BuyToPlay,
        float adIntensity = 0, List<string> componentIds = null, string ipName = null,
        string labelName = null,
        float predScore = 0, int predSales = 0, SequelStrategy sequelStrat = SequelStrategy.Cautious,
        DesignPhilosophy philosophy = DesignPhilosophy.Balanced,
        float budgetGraphics = 0.33f, float budgetAudio = 0.33f, float budgetGameplay = 0.34f)
    {
        ModAPI.FireHooks(ModAPI.GameHook.BeforeProjectCreate);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeProjectCreate)) return null;

        // 随机同名检测（触发彩蛋"巧合？"）
        if (Projects.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            Services.AchievementManager?.TrackNameClash();
        // 创建项目时立即检查"律师函"成就
        if (AchievementManager.IsKnownBigGame(name))
            Services.AchievementManager?.TryUnlock("easter_cease_desist", true);

        var proj = new GameProject
        {
            Name = name,
            Genre = genre,
            Theme = theme,
            Platform = platform,
            Phase = DevPhase.Planning,
            EstimatedMonths = estimatedMonths,
            Marketing = marketing,
            MarketingBudget = marketingBudget,
            Scale = scale,
            PriceModel = priceModel,
            AdIntensity = adIntensity,
            IPName = ipName,
            LabelName = labelName,
            PredecessorScore = predScore,
            PredecessorSales = predSales,
            SequelStrat = sequelStrat,
            Philosophy = philosophy,
            BudgetGraphics = budgetGraphics,
            BudgetAudio = budgetAudio,
            BudgetGameplay = budgetGameplay,
        };

        // 建议售价：规模决定了内容量，影响定价
        proj.SuggestedPrice = priceModel == PriceModel.Free ? 0 :
            Mathf.Lerp(5f, 60f, scale); // $5 ~ $60

        // 开发周期受规模影响（基础值来自玩家调整）
        float playerEstimate = estimatedMonths;
        proj.EstimatedMonths *= 0.5f + scale; // 小规模 50%，大规模 150%

        // 计算契合度
        proj.GenreThemeCompatibility = GetCompatibility(genre, theme);
        proj.DevLog.Add($"立项：《{name}》- {genre.Name()} × {theme.Name()} - 契合度 {proj.GenreThemeCompatibility:P0}");

        // ── 自动生成宣传语 ──
        proj.AutoTagline = GenerateTagline(genre, theme);
        proj.GameCoverStyle = GenerateCoverStyle(genre, theme, scale);
        proj.DevLog.Add($"🎨 风格定位：{proj.GameCoverStyle}");
        if (!string.IsNullOrEmpty(ipName))
        {
            if (predScore > 0)
                proj.DevLog.Add($"📦 续作！IP「{ipName}」前作评分 {predScore:F0}，续作获老粉丝销量加成");
            else
                proj.DevLog.Add($"🆕 新IP「{ipName}」系列启动");
        }

        // 严重不符惩罚
        if (proj.GenreThemeCompatibility < 0.4f)
        {
            proj.EstimatedMonths *= 1.6f; // +60%开发时间
            proj.DevLog.Add("⚠ 类型与主题严重不符，开发时间+60%");
        }

        // 检查引擎功能匹配
        CheckEngineCompatibility(proj);

        // 初始品质基线（Mod 可通过 BalanceModDB 覆盖）
        proj.GraphicsScore = BalanceModDB.Get("project.init.graphics", 30f);
        proj.GameplayScore = BalanceModDB.Get("project.init.gameplay_base", 25f) + proj.GenreThemeCompatibility * BalanceModDB.Get("project.init.gameplay_compat", 15f);
        proj.AudioScore = BalanceModDB.Get("project.init.audio", 20f);
        proj.NetworkScore = BalanceModDB.Get("project.init.network", 10f);
        proj.StoryScore = BalanceModDB.Get("project.init.story", 15f);
        proj.StabilityScore = BalanceModDB.Get("project.init.stability", 50f);

        // 科技加成
        ApplyTechBonuses(proj);

        // ── 组件加成 ──
        if (componentIds != null)
        {
            proj.EquippedComponents = new List<string>(componentIds);
            var equippedObjs = GameComponentDB.All.Where(c => componentIds.Contains(c.Id)).ToArray();
            foreach (var c in equippedObjs)
            {
                foreach (var (attr, bonus) in c.Effects)
                {
                    switch (attr)
                    {
                        case "graphics": proj.GraphicsScore += bonus; break;
                        case "gameplay": proj.GameplayScore += bonus; break;
                        case "audio": proj.AudioScore += bonus; break;
                        case "story": proj.StoryScore += bonus; break;
                        case "network": proj.NetworkScore += bonus; break;
                        case "stability": proj.StabilityScore += bonus; break;
                    }
                }
            }
            var synergies = GameComponentDB.ComputeSynergies(equippedObjs);
            proj.SynergyBonus = synergies.Values.Sum();
            proj.GameplayScore += proj.SynergyBonus * 0.5f;
            if (synergies.Count > 0)
                proj.DevLog.Add($"组件联动加成: +{proj.SynergyBonus:F1}分 ({string.Join(", ", synergies.Select(kv => $"{kv.Key}+{kv.Value:F1}"))})");

            // 组件越多，开发时间越长（无上限，但边际递增）
            int compCount = componentIds.Count;
            if (compCount > 3)
            {
                float extra = (compCount - 3) * 0.10f; // 第4个起每个+10%
                if (compCount > 6) extra += (compCount - 6) * 0.05f; // 第7个起再加5%
                proj.ComponentDevPenalty = extra;
                proj.EstimatedMonths *= (1 + extra);
                proj.DevLog.Add($"组件复杂度惩罚: +{extra*100:F0}%开发时间（{compCount}个组件）");
            }
        }

        Projects.Add(proj);
        ModAPI.FireHooks(ModAPI.GameHook.AfterProjectCreate);

        // ── 新风格红利：首次尝试新类型/主题 +5~8分临时加成 ──
        if (CompletedProjects.Count > 0)
        {
            var usedGenres = CompletedProjects.Select(p => p.Genre).ToHashSet();
            var usedThemes = CompletedProjects.Select(p => p.Theme).ToHashSet();
            if (!usedGenres.Contains(genre))
            {
                proj.GameplayScore += 5;
                proj.DevLog.Add($"新类型红利！首次尝试 {genre.Name()} 类型 +5 趣味");
            }
            if (!usedThemes.Contains(theme))
            {
                proj.GameplayScore += 3;
                proj.DevLog.Add($"新主题红利！首次尝试 {theme.Name()} 主题 +3 趣味");
            }
        }

        // ── 教程通知 ──
        _gm.GetNodeOrNull<TutorialManager>("TutorialManager")?.NotifyAction("project_created");

        return proj;
    }

    private float GetCompatibility(GameGenre genre, GameTheme theme)
    {
        if (Compatibility.TryGetValue((genre, theme), out var v))
            return v;
        // 默认随机值
        return 0.5f + ((int)genre + (int)theme) * 0.03f % 0.4f;
    }

    /// <summary>外部查询契合度 — DevMenu用</summary>
    public static float GetCompat(GameGenre g, GameTheme t)
    {
        if (Compatibility.TryGetValue((g, t), out var v)) return v;
        return 0.5f + ((int)g + (int)t) * 0.03f % 0.4f;
    }

    private void CheckEngineCompatibility(GameProject proj)
    {
        // 根据游戏类型检查所需引擎功能
        proj.DevLog.Add("引擎匹配检查：");

        // 大多数游戏需要2D V1（已有）
        if (_techMgr.IsResearched("2d_v2"))
            proj.DevLog.Add("  √ 2D V2 高清渲染可用");

        if (proj.Genre == GameGenre.RPG || proj.Genre == GameGenre.AVG)
        {
            if (!_techMgr.IsResearched("save_system"))
            {
                proj.GraphicsScore -= 20;
                proj.DevLog.Add("  × 缺少存档机制，预计评分-20");
            }
        }
    }

    private void ApplyTechBonuses(GameProject proj)
    {
        // 2D渲染
        if (_techMgr.IsResearched("2d_v2")) proj.GraphicsScore += 15;
        if (_techMgr.IsResearched("2d_v3")) { proj.GraphicsScore += 10; proj.EstimatedMonths *= 0.75f; }
        if (_techMgr.IsResearched("2d_v4")) { proj.GraphicsScore += 15; proj.EstimatedMonths *= 0.7f; }
        if (_techMgr.IsResearched("2d_v5")) { proj.GraphicsScore += 20; proj.EstimatedMonths *= 0.5f; }

        // 3D渲染
        if (_techMgr.IsResearched("3d_v1")) proj.GraphicsScore += 10;
        if (_techMgr.IsResearched("3d_v2")) proj.GraphicsScore += 15;
        if (_techMgr.IsResearched("3d_v3")) proj.GraphicsScore += 20;
        if (_techMgr.IsResearched("3d_v4")) proj.GraphicsScore += 25;

        // 音频
        if (_techMgr.IsResearched("audio_v2")) proj.AudioScore += 20;
        if (_techMgr.IsResearched("dynamic_music")) proj.AudioScore += 15;
        if (_techMgr.IsResearched("spatial_audio")) proj.AudioScore += 10;

        // AI
        if (_techMgr.IsResearched("ai_v2")) proj.AIScore += 20;
        if (_techMgr.IsResearched("ml_ai")) proj.AIScore += 30;
        if (_techMgr.IsResearched("gen_story")) proj.GameplayScore += 15;

        // 网络
        if (_techMgr.IsResearched("net_v1")) proj.NetworkScore += 15;
        if (_techMgr.IsResearched("net_v2")) proj.NetworkScore += 20;
        if (_techMgr.IsResearched("net_v3")) proj.NetworkScore += 25;

        // 程序基础
        if (_techMgr.IsResearched("memory_v1")) proj.StabilityScore += 10;
        if (_techMgr.IsResearched("memory_v2")) proj.StabilityScore += 15;
        if (_techMgr.IsResearched("multithread")) proj.EstimatedMonths *= 0.8f;
    }

    /// <summary>
    /// 开始开发
    /// </summary>
    public bool StartDevelopment(GameProject proj, Team team)
    {
        if (proj.Phase != DevPhase.Planning) return false;
        if (team.Task != TeamTask.None) return false;

        proj.Phase = DevPhase.Developing;
        team.Task = TeamTask.DevelopGame;
        team.CurrentProject = proj;
        proj.DevLog.Add("开始开发...");
        _gm.GetNodeOrNull<TutorialManager>("TutorialManager")?.NotifyAction("development_started");
        return true;
    }

    /// <summary>
    /// 每月开发推进
    /// </summary>
    public void ProcessMonthlyDev(Team team, float speedFactor = 1f)
    {
        if (team.Task != TeamTask.DevelopGame) return;
        var proj = team.CurrentProject;
        if (proj == null || proj.Phase != DevPhase.Developing) return;
        if (team.Members.Count == 0) return; // 空团队不产生开发进度

        // 计算开发效率
        float speed = CalculateDevSpeed(team, proj) * speedFactor;

        // 技术债务减速
        if (DebtMgr != null)
        {
            speed *= (1 + DebtMgr.DevSpeedPenalty);
            if (DebtMgr.CrashRecoveryMonths > 0) speed = 0; // 崩溃恢复期无法开发
        }
        if (DebtMgr?.CrunchMode == true && (DebtMgr?.LeverageUnlocked == true)) speed *= 1.8f; // 996冲刺：速度×1.8，债务月增×3

        float progress = speed / proj.EstimatedMonths;

        // ── 六大模块独立推进（受玩家分配滑块偏置）──
        float coreEff = team.GetSkillEff(SkillType.Program) * 0.4f + team.GetSkillEff(SkillType.AI) * 0.6f;
        float visualEff = team.GetSkillEff(SkillType.Art) * 0.7f + team.GetSkillEff(SkillType.Program) * 0.3f;
        float audioEff = team.GetSkillEff(SkillType.Audio);
        float storyEff = team.GetSkillEff(SkillType.Program) * 0.5f + team.GetSkillEff(SkillType.Art) * 0.5f;
        float stabEff = team.GetSkillEff(SkillType.Program) * 0.6f + team.GetSkillEff(SkillType.Network) * 0.4f;
        float onlineEff = team.GetSkillEff(SkillType.Network);

        float totalModEff = coreEff + visualEff + audioEff + storyEff + stabEff + onlineEff;
        if (totalModEff < 1) totalModEff = 1;

        // 玩家滑块偏置：基础分布 × 滑块加成（滑块高的模块快1~3倍，低的慢0.3~0.8倍）
        float visualBias = 0.3f + proj.BudgetGraphics * 2.7f;   // 0.3 ~ 3.0
        float audioBias = 0.3f + proj.BudgetAudio * 2.7f;
        float coreBias = 0.3f + proj.BudgetGameplay * 2.7f;
        float otherBias = Mathf.Max(0.2f, 1.0f - (visualBias + audioBias + coreBias - 3.0f) * 0.5f);

        float coreDelta = progress * (coreEff / totalModEff * 6) * 0.5f * coreBias + progress * 0.3f;
        float visualDelta = progress * (visualEff / totalModEff * 6) * 0.5f * visualBias + progress * 0.3f;
        float audioDelta = progress * (audioEff / totalModEff * 6) * 0.5f * audioBias + progress * 0.3f;
        float storyDelta = progress * (storyEff / totalModEff * 6) * 0.5f * otherBias + progress * 0.3f;
        float stabDelta = progress * (stabEff / totalModEff * 6) * 0.5f * otherBias + progress * 0.4f;
        float onlineDelta = progress * (onlineEff / totalModEff * 6) * 0.5f * otherBias + progress * 0.2f;

        proj.ModuleProgressCore += coreDelta;
        proj.ModuleProgressVisual += visualDelta;
        proj.ModuleProgressAudio += audioDelta;
        proj.ModuleProgressStory += storyDelta;
        proj.ModuleProgressStability += stabDelta;
        proj.ModuleProgressOnline += onlineDelta;

        // 边际递减：属性越高增长越慢（单人无法闭门造车冲满分）
        float Diminish(float val) => Mathf.Clamp(1f - (val / 100f) * (val / 100f * 0.7f), 0.08f, 1f);
        proj.GameplayScore = Mathf.Min(100, proj.GameplayScore + coreDelta * 30 * Diminish(proj.GameplayScore));
        proj.GraphicsScore = Mathf.Min(100, proj.GraphicsScore + visualDelta * 30 * Diminish(proj.GraphicsScore));
        proj.AudioScore = Mathf.Min(100, proj.AudioScore + audioDelta * 30 * Diminish(proj.AudioScore));
        proj.StoryScore = Mathf.Min(100, proj.StoryScore + storyDelta * 30 * Diminish(proj.StoryScore));
        proj.StabilityScore = Mathf.Min(100, proj.StabilityScore + stabDelta * 30 * Diminish(proj.StabilityScore));
        proj.NetworkScore = Mathf.Min(100, proj.NetworkScore + onlineDelta * 30 * Diminish(proj.NetworkScore));

        proj.DevProgress += progress;

        // ── 性能仪表盘更新 ──
        // 内存占用：基础值 + 组件数×非线性增长 (组件越多增长越快)
        proj.ComponentCount = proj.EquippedComponents.Count;
        float baseMem = proj.Platform switch { Platform.Console => 2.0f, Platform.Mobile => 0.5f, _ => 3.0f };
        float compMem = proj.ComponentCount * 0.8f * (1 + proj.ComponentCount * 0.15f); // 非线性
        proj.MemoryUsage = baseMem + compMem + proj.TechDebt * 0.02f;
        // 帧率预估：基础60fps - 组件压力 - 技术债务
        float fpsDrop = proj.ComponentCount * 4f + proj.TechDebt * 0.3f;
        proj.FpsEstimate = Mathf.Max(5, 60f - fpsDrop);
        // 崩溃率：BUG数量 + 技术债务
        proj.CrashRate = Mathf.Clamp(proj.BugCount * 0.01f + proj.TechDebt * 0.005f, 0, 0.9f);
        // 平台红线
        (proj.PlatformMemoryLimit, proj.PlatformFpsTarget) = proj.Platform switch
        {
            Platform.Console => (3.5f, 30f),
            Platform.Mobile => (1.5f, 30f),
            _ => (7f, 60f)  // PC
        };
        // 债务利息计算
        proj.DebtInterestRate = proj.TechDebt > 30 ? Mathf.Clamp(proj.TechDebt * 0.0015f, 0, 0.1f) : 0;
        proj.NextMonthBugFromDebt = (int)(proj.TechDebt * proj.DebtInterestRate * 0.3f * 100);
        proj.NextMonthSlowFromDebt = proj.TechDebt * 0.001f;

        // ── 宣发冲刺期触发 (90%进度) ──
        if (proj.DevProgress >= 0.9f && !proj.MarketingSprintStarted && proj.PostRelease == PostReleaseType.None)
        {
            proj.MarketingSprintStarted = true;
            proj.MarketingSprintMonths = 3 + Random.Shared.Next(3); // 3~5个月
            proj.MarketingSprintSpent = 0;
            proj.MarketingHype = 10f; // 基础期待值
            proj.DevProgress = 0.9f; // 卡在90%
            proj.Phase = DevPhase.Marketing;
            proj.DevLog.Add(Loc.TrF("dev.marketing_start", proj.MarketingSprintMonths));
            _gm.Paused = true;
            ShowMarketingAction(proj);
            return;
        }

        // BUG累积
        float bugRate = 0.5f + (DebtMgr?.ComputeTotalDebt() ?? 0) * 0.01f;
        bugRate *= (1 - team.ProdSlider) * 0.5f; // 重构多则BUG少

        // 技术债务 BUG 倍率
        if (DebtMgr != null)
            bugRate *= DebtMgr.BugRateMultiplier;

        proj.BugCount += (int)(bugRate * 3);

        // 灵感随机产出（受债务影响）
        if (Random.Shared.NextDouble() < 0.15f)
        {
            float inspMult = DebtMgr?.InspirationMultiplier ?? 1f;
            _res.GainInspiration(3 * inspMult);
        }

        proj.MonthsSpent++;

        // ── 债务利滚利：每月自动产生利息 ──
        if (proj.TechDebt > 10 && proj.DebtInterestRate > 0)
        {
            float interestBug = proj.NextMonthBugFromDebt;
            proj.BugCount += (int)interestBug;
            float speedSlow = proj.NextMonthSlowFromDebt * proj.EstimatedMonths;
            proj.EstimatedMonths += speedSlow;
        }

        // ── 月度微决策：开发中每月有概率触发轻量二选一（不打断时间流）──
        if (!_gm.Paused && Random.Shared.NextDouble() < 0.20f)
            TriggerMonthlyStandup(proj, team);

        // ── 开发中途决策：每完成~25%进度触发一次互动选择 ──
        if (!proj.HasTriggeredMidDevDecision && proj.DevProgress >= 0.25f && proj.DevProgress < 0.30f)
            TriggerMidDevDecision(proj, "early");
        else if (proj.DevProgress >= 0.6f && proj.DevProgress < 0.65f && !proj.HasTriggeredMidDevDecision2)
            TriggerMidDevDecision(proj, "late");

        // ── 开发分数累积：每个员工独立贡献程序分/美术分 ──
        float scaleMul = 0.8f + proj.Scale * 1.2f;
        float bonusMul = 1f + proj.GameplayScore * 0.003f + proj.GraphicsScore * 0.003f;
        int teamLvSum = team.Members.Sum(m => m.GetHighestLevel());
        float teamMul = 0.7f + teamLvSum * 0.06f;
        foreach (var m in team.Members)
        {
            // 外出培训当月不参与开发
            if (m.TrainingLeaveMonths > 0) { m.TrainingLeaveMonths--; continue; }
            float traitEff = m.GetTraitEfficiencyMod();
            float p = m.GetEfficiency(SkillType.Program) * scaleMul * bonusMul * teamMul * 0.8f * traitEff;
            float a = m.GetEfficiency(SkillType.Art) * scaleMul * bonusMul * teamMul * 0.8f * traitEff;
            m.LastProgContrib = p;
            m.LastArtContrib = a;
            proj.BaseProgramScore += p;
            proj.BaseArtScore += a;
        }
        proj.BaseQualityScore = (proj.BaseProgramScore + proj.BaseArtScore) / 2f;

        // 每月有概率产生一条风味日志
        if (Random.Shared.NextDouble() < 0.3f)
        {
            var storyEvt = _gm.GetNodeOrNull<StoryEvents>("StoryEvents");
            if (storyEvt != null)
                proj.DevLog.Add(storyEvt.GenerateFlavorLog(proj));
        }

        // ── 开发里程碑叙事 ──
        var se = _gm.GetNodeOrNull<StoryEvents>("StoryEvents");
        se?.CheckDevMilestones(proj, team);

        if (proj.DevProgress >= 1.0f)
        {
            proj.DevProgress = 1.0f;

            // ── 后续内容项目：开发完成后直接应用效果，不走打磨/发售流程 ──
            if (proj.PostRelease != PostReleaseType.None && proj.BaseProject != null)
            {
                ApplyPostReleaseEffect(proj);
                proj.Phase = DevPhase.Released;
                proj.IsReleased = true;
                team.Task = TeamTask.None;
                team.CurrentProject = null;
                return;
            }

            proj.Phase = DevPhase.Polishing;
            proj.DevLog.Add($"开发完成！耗时 {proj.MonthsSpent} 个月，BUG {proj.BugCount} 个");
            _gm.GetNodeOrNull<TutorialManager>("TutorialManager")?.NotifyAction("project_polishing");
            // 释放团队，打磨不需要团队驻留
            team.Task = TeamTask.None;
            team.CurrentProject = null;
            // ═══ 开发完成后自动暂停，让玩家决定是否发售还是继续打磨 ═══
            _gm.Paused = true;
            _gm.ShowPopup(Loc.Tr("dev.complete_title"), Loc.TrF("dev.complete_msg", proj.Name, proj.MonthsSpent, proj.BugCount, proj.BaseProgramScore, proj.BaseArtScore), new Color(0.3f, 0.8f, 0.3f));
        }
    }

    /// <summary>开发中途决策 — 让玩家感觉在参与而非旁观</summary>
    private void TriggerMidDevDecision(GameProject proj, string phase)
    {
        string pctText = phase == "early" ? Loc.Tr("dev.mid_early") : Loc.Tr("dev.mid_late");
        string status = Loc.TrF("dev.mid_status", proj.Name, pctText, proj.MonthsSpent, Mathf.Max(0, proj.EstimatedMonths - proj.MonthsSpent), proj.BugCount);

        bool isRPG = proj.Genre is GameGenre.RPG or GameGenre.AVG or GameGenre.VIS or GameGenre.MUS;
        bool isAction = proj.Genre is GameGenre.ACT or GameGenre.FPS or GameGenre.FTG or GameGenre.RAC or GameGenre.SPO;
        bool isOnline = proj.Genre is GameGenre.MOBA or GameGenre.MMO;

        if (phase == "early")
        {
            proj.HasTriggeredMidDevDecision = true;
            var allChoices = new List<(string title, string A, string B, Action onA, Action onB, bool relevant)>()
            {
                (Loc.Tr("devmid.art_title"), Loc.Tr("devmid.art_a"), Loc.Tr("devmid.art_b"),
                    () => { proj.GraphicsScore += 5; proj.StoryScore -= 2; },
                    () => { proj.StoryScore += 3; proj.GraphicsScore -= 1; },
                    isRPG || isAction),
                (Loc.Tr("devmid.mech_title"), Loc.Tr("devmid.mech_a"), Loc.Tr("devmid.mech_b"),
                    () => { proj.GameplayScore += 8; proj.EstimatedMonths += 1; },
                    () => { proj.GameplayScore += 2; },
                    true),
                (Loc.Tr("devmid.music_title"), Loc.Tr("devmid.music_a"), Loc.Tr("devmid.music_b"),
                    () => { proj.AudioScore += 6; proj.EstimatedMonths += 0.5f; },
                    () => { proj.AudioScore += 1; },
                    isRPG),
            };
            var relevant = allChoices.Where(c => c.relevant).ToList();
            if (relevant.Count == 0) relevant = allChoices; // fallback
            var c = relevant[Random.Shared.Next(relevant.Count)];
            _gm.ShowChoicePopup(c.title, Loc.TrF("devmid.pick_early", status, c.title), c.A, c.B, c.onA, c.onB, new Color(0.4f, 0.6f, 0.9f));
        }
        else
        {
            proj.HasTriggeredMidDevDecision2 = true;
            var allChoices = new List<(string title, string A, string B, Action onA, Action onB, bool relevant)>()
            {
                (Loc.Tr("devmid.story_title"), Loc.Tr("devmid.story_a"), Loc.Tr("devmid.story_b"),
                    () => { proj.StoryScore += 8; proj.EstimatedMonths += 1.5f; },
                    () => { proj.StabilityScore += 3; },
                    isRPG),
                (Loc.Tr("devmid.perf_title"), Loc.Tr("devmid.perf_a"), Loc.Tr("devmid.perf_b"),
                    () => { proj.StabilityScore += 8; proj.BugCount = Mathf.Max(0, proj.BugCount - 3); proj.EstimatedMonths += 0.5f; },
                    () => { proj.GameplayScore += 2; },
                    isAction || isOnline),
                (Loc.Tr("devmid.online_title"), Loc.Tr("devmid.online_a"), Loc.Tr("devmid.online_b"),
                    () => { proj.NetworkScore += 12; proj.EstimatedMonths += 2; proj.BugCount += 5; },
                    () => { proj.GameplayScore += 4; },
                    isOnline || isAction),
            };
            var relevant = allChoices.Where(c => c.relevant).ToList();
            if (relevant.Count == 0) relevant = allChoices;
            var c = relevant[Random.Shared.Next(relevant.Count)];
            _gm.ShowChoicePopup(c.title, Loc.TrF("devmid.pick_late", status, c.title), c.A, c.B, c.onA, c.onB, new Color(0.3f, 0.7f, 0.4f));
        }
    }

    public float CalculateDevSpeed(Team team, GameProject proj)
    {
        float totalEff = 0;
        foreach (var m in team.Members)
        {
            float traitEff = m.GetTraitEfficiencyMod();
            totalEff += m.GetEfficiency(SkillType.Program) * traitEff;
            totalEff += m.GetEfficiency(SkillType.Art) * traitEff;
            totalEff += m.GetEfficiency(SkillType.Audio) * traitEff;
        }

        float chem = team.GetChemistryBonus();
        totalEff *= (1 + chem);

        if (team.Captain != null && team.Captain.CanMentor)
            totalEff *= 1.1f;

        // 产能滑块：重构减慢新内容
        totalEff *= 0.5f + team.ProdSlider * 0.5f;

        // 标准化：良好团队≈1.0，精英≈1.5+，确保 EstimatedMonths 与真实耗时一致
        return totalEff / 5f;
    }

    /// <summary>
    /// 打磨阶段（三种策略 + 边际递减）
    /// </summary>
    public void ProcessMonthlyPolish(Team team = null)
    {
        var proj = team?.CurrentProject ?? Projects.FirstOrDefault(p => p.Phase == DevPhase.Polishing);
        if (proj == null || proj.Phase != DevPhase.Polishing) return;

        proj.PolishMonths++;

        // 边际递减：前3月最优，4~9月减半，10~12月缓慢，13月+几乎无效
        float decay = proj.PolishMonths switch
        {
            <= 3 => 1.0f,
            <= 9 => 0.5f,
            <= 12 => 0.2f,
            _ => 0.05f
        };

        float baseEffect = (team != null ? CalculateDevSpeed(team, proj) : 1f) * 0.3f * decay;

        switch (proj.PolishStrat)
        {
            case PolishStrategy.Standard:
                proj.BugCount = Mathf.Max(0, proj.BugCount - (int)(baseEffect * 10));
                proj.GameplayScore = Mathf.Min(100, proj.GameplayScore + baseEffect * 2);
                break;

            case PolishStrategy.Deep:
                if (_res.Inspiration >= 10)
                {
                    _res.Inspiration -= 10;
                    proj.BugCount = Mathf.Max(0, proj.BugCount - (int)(baseEffect * 15));
                    proj.GameplayScore = Mathf.Min(100, proj.GameplayScore + baseEffect * 5);
                    proj.DevLog.Add($"深度打磨！消耗10灵感，BUG-15，趣味+{baseEffect * 5:F1}");
                }
                else
                {
                    proj.DevLog.Add("灵感不足，降级为标准打磨");
                    proj.PolishStrat = PolishStrategy.Standard;
                    goto case PolishStrategy.Standard;
                }
                break;

            case PolishStrategy.Extreme:
                if (team != null)
                {
                    float fatigue = 15 * decay;
                    foreach (var m in team.Members)
                        m.Fatigue = Mathf.Min(100, m.Fatigue + fatigue);
                    proj.DevLog.Add($"极限打磨！全员疲劳+{fatigue:F0}，BUG-20");
                }
                proj.BugCount = Mathf.Max(0, proj.BugCount - (int)(baseEffect * 20));
                proj.GameplayScore = Mathf.Min(100, proj.GameplayScore + baseEffect * 8);
                proj.StabilityScore = Mathf.Min(100, proj.StabilityScore + baseEffect);
                break;
        }

        proj.DevLog.Add($"打磨第{proj.PolishMonths}个月... BUG剩余 {proj.BugCount}，趣味性 {proj.GameplayScore:F1}");

        // 打磨期间分数极缓增长，每个员工独立贡献
        if (team != null)
        {
            float polishMul = 0.15f * decay;
            foreach (var m in team.Members)
            {
                float traitEff = m.GetTraitEfficiencyMod();
                float p = m.GetEfficiency(SkillType.Program) * 0.8f * polishMul * traitEff;
                float a = m.GetEfficiency(SkillType.Art) * 0.8f * polishMul * traitEff;
                m.LastProgContrib = p;
                m.LastArtContrib = a;
                proj.BaseProgramScore += p;
                proj.BaseArtScore += a;
            }
            proj.BaseQualityScore = (proj.BaseProgramScore + proj.BaseArtScore) / 2f;
        }
    }

    private int _lastStandupMonth = -999;

    private void TriggerMonthlyStandup(GameProject proj, Team team)
    {
        if (_gm.GameMonth - _lastStandupMonth < 3) return;
        _lastStandupMonth = _gm.GameMonth;

        var rng = new Random();
        var pool = new List<(string title, string a, string b, Action onA, Action onB)>();

        if (_res.Inspiration >= 3)
            pool.Add((Loc.Tr("devpol.idea"), Loc.Tr("devpol.idea_a"), Loc.Tr("devpol.idea_b"),
                () => { _res.SpendInspiration(3); proj.GameplayScore += 5; proj.DevLog.Add("灵感投入：核心玩法+5"); },
                () => { }));

        if (proj.BugCount > 5)
            pool.Add((Loc.Tr("devpol.bugfix"), Loc.Tr("devpol.bugfix_a"), Loc.Tr("devpol.bugfix_b"),
                () => { proj.BugCount = Mathf.Max(0, proj.BugCount - 8); proj.DevLog.Add("集中修BUG：-8"); },
                () => { }));

        if (team.Members.Any(m => m.Fatigue > 50))
            pool.Add((Loc.Tr("devpol.fatigue"), Loc.Tr("devpol.fatigue_a"), Loc.Tr("devpol.fatigue_b"),
                () => { proj.DevProgress -= 0.03f; proj.DevLog.Add("休息一周"); foreach (var m in team.Members) m.Fatigue = Mathf.Max(0, m.Fatigue - 8); },
                () => { foreach (var m in team.Members) m.Fatigue = Mathf.Min(100, m.Fatigue + 5); proj.DevLog.Add("坚持，全员疲劳+5"); }));

        pool.Add((Loc.Tr("devpol.design"), Loc.Tr("devpol.design_a"), Loc.Tr("devpol.design_b"),
            () => { proj.GameplayScore += 4; proj.EstimatedMonths += 0.3f; proj.DevLog.Add("加深玩法，工期微增"); },
            () => { proj.DevProgress += 0.05f; proj.DevLog.Add("设计评审提速5%"); }));

        pool.Add((Loc.Tr("devpol.visual"), Loc.Tr("devpol.visual_a"), Loc.Tr("devpol.visual_b"),
            () => { proj.GraphicsScore += 3; proj.BugCount += 2; proj.DevLog.Add("视觉冲刺：画面+3，BUG+2"); },
            () => { }));

        if (pool.Count == 0) return;

        var pick = pool[rng.Next(pool.Count)];
        _gm.ShowChoicePopup(pick.title, Loc.TrF("devpol.context", proj.Name, proj.MonthsSpent), pick.a, pick.b, pick.onA, pick.onB, new Color(0.3f, 0.6f, 0.9f));
    }

    /// <summary>
    /// 发售（完整公式）
    /// </summary>
    // ══════════════════ 跳票延期 ══════════════════
    /// <summary>玩家决定延期，返回是否成功</summary>
    public bool DelayRelease(GameProject proj, int months, int reasonIndex)
    {
        if (proj == null || proj.DelayCount >= 3) return false;

        proj.DelayCount++;
        proj.Phase = DevPhase.Polishing; // 退回打磨阶段
        proj.EstimatedMonths += months;

        // 延期带来品质加成
        float qualityBoost = 2f + months * 1.5f;
        proj.StabilityScore = Mathf.Min(100, proj.StabilityScore + qualityBoost * 0.3f);
        proj.GameplayScore = Mathf.Min(100, proj.GameplayScore + qualityBoost * 0.2f);

        // 信任度惩罚
        float penalty = 5f + ConsecutiveDelays * 3f;
        PlayerTrust = Mathf.Max(0, PlayerTrust - penalty);
        ConsecutiveDelays++;
        TrustHistory.Add(PlayerTrust);

        // 惩罚加倍：连续延期导致渠道和媒体惩罚
        if (ConsecutiveDelays >= 2)
        {
            proj.MarketingHype = Mathf.Max(0, proj.MarketingHype - 15f);
        }

        _gm.ShowToast(Loc.Tr("toast.delayed"), Loc.TrF("toast.delayed_msg", proj.Name, months, DelayReasons[reasonIndex]), new Color(0.9f, 0.6f, 0.1f));
        proj.DevLog.Add($"延期 {months} 个月（{Loc.Tr(DelayReasons[reasonIndex])}），信任度 {PlayerTrust:F0}");

        // 触发延期新闻
        _gm.GetNodeOrNull<StoryEvents>("StoryEvents")?.TriggerDelayEvent(proj, months, reasonIndex);
        return true;
    }

    /// <summary>更新信任度（发售时调用）</summary>
    public void UpdateTrustAfterRelease(GameProject proj, float finalScore)
    {
        float promisedQuality = proj.ExpectedScore > 0 ? proj.ExpectedScore : 50f;
        float delta = finalScore - promisedQuality;

        if (delta >= 10)
        {
            PlayerTrust = Mathf.Min(100, PlayerTrust + 10f);
            ConsecutiveDelays = 0;
        }
        else if (delta >= 0)
        {
            PlayerTrust = Mathf.Min(100, PlayerTrust + 3f);
            ConsecutiveDelays = Mathf.Max(0, ConsecutiveDelays - 1);
        }
        else if (delta >= -10)
        {
            PlayerTrust = Mathf.Max(0, PlayerTrust + delta * 0.5f);
        }
        else
        {
            PlayerTrust = Mathf.Max(0, PlayerTrust + delta * 0.8f);
            BrokenPromises++;
            if (BrokenPromises >= 2)
                _gm.GetNodeOrNull<StoryEvents>("StoryEvents")?.TriggerReviewBomb();
        }
        TrustHistory.Add(PlayerTrust);

        // 信任度影响首周销量
        float trustMultiplier = 0.3f + (PlayerTrust / 100f) * 0.7f;
        proj.Sales = (int)(proj.Sales * trustMultiplier);
    }

    public void ReleaseGame(Team team) => ReleaseGameProject(team?.CurrentProject, team);

    public void ReleaseGameDirect(GameProject proj) => ReleaseGameProject(proj, null);

    private void ReleaseGameProject(GameProject proj, Team team)
    {
        if (proj == null) return;
        ModAPI.FireHooks(ModAPI.GameHook.BeforeGameRelease);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeGameRelease)) return;

        // 1. 品质基础分 = 六维加权
        ModAPI.FireHooks(ModAPI.GameHook.BeforeScoreCalc);
        float wGfx = BalanceModDB.Get("score.weight.graphics", 0.2f);
        float wGp = BalanceModDB.Get("score.weight.gameplay", 0.3f);
        float wAud = BalanceModDB.Get("score.weight.audio", 0.1f);
        float wSto = BalanceModDB.Get("score.weight.story", 0.15f);
        float wNet = BalanceModDB.Get("score.weight.network", 0.1f);
        float wStab = BalanceModDB.Get("score.weight.stability", 0.15f);
        float wScale = BalanceModDB.Get("score.weight.scale_factor", 0.2f);
        float wScaleBase = BalanceModDB.Get("score.weight.scale_base", 0.9f);
        float rawScore = (
            proj.GraphicsScore * wGfx +
            proj.GameplayScore * wGp +
            proj.AudioScore * wAud +
            proj.StoryScore * wSto +
            proj.NetworkScore * wNet +
            proj.StabilityScore * wStab
        ) * (wScaleBase + proj.Scale * wScale)
        ;

        // 价格/广告影响媒体评分（买断制价格高→玩家更挑剔，免费广告多→媒体反感）
        float priceMediaPenalty = 0;
        if (proj.PriceModel == PriceModel.Free)
            priceMediaPenalty = proj.AdIntensity * 12f;  // 广告越多媒体评分越低
        else
            priceMediaPenalty = Mathf.Max(0, (proj.SuggestedPrice - 15f) * 0.5f); // 超过$15每多$1扣0.5分

        // 2. 科技修正（引擎匹配度）
        float techBonus = 1.0f;
        if (proj.MissingFeatures == 0) techBonus = 1.3f;
        else if (proj.MissingFeatures <= 2) techBonus = 1.1f;
        rawScore *= techBonus;

        // 3. BUG惩罚
        rawScore -= proj.BugCount * 0.3f;

        // 4. 契合度奖励
        float compatBonus = proj.GenreThemeCompatibility switch
        {
            >= 0.9f => 15f,
            <= 0.25f => -30f,
            <= 0.4f => -15f,
            _ => 0
        };
        rawScore += compatBonus;

        // ── 蓝海开拓者加分：首次使用的题材组合获得额外评分 ──
        float blueOceanScore = GetBlueOceanBonus(proj.Genre, proj.Theme) > 1.0f ? (GetBlueOceanBonus(proj.Genre, proj.Theme) - 1f) * 50f : 0f;
        rawScore += blueOceanScore;
        if (blueOceanScore > 0) proj.DevLog.Add(Loc.TrF("dev.blue_ocean_bonus", blueOceanScore));

        // ── 宣发期待值乘数：宣发Hype越高，评分越受正向拉高 ──
        float hypeMultiplier = proj.MarketingHype > 0 ? 1f + Mathf.Clamp(proj.MarketingHype / 150f, 0, 0.3f) : 1f;
        rawScore *= hypeMultiplier;

        // 价格/广告影响最终媒体分
        rawScore -= priceMediaPenalty;

        // ── 市场风口：类型/主题热度影响评分 ──
        var trendMgr = _gm.GetNodeOrNull<MarketTrendManager>("MarketTrendManager");
        if (trendMgr != null)
        {
            float hypeScoreBonus = trendMgr.GetHypeForGenre(proj.Genre).ScoreBonus
                + trendMgr.GetHypeForTheme(proj.Theme).ScoreBonus;
            rawScore += hypeScoreBonus;
            // 浪潮加成（不同类型/主题市场偏好巨变）
            rawScore += trendMgr.GetTrendScoreBonus(proj.Genre, proj.Theme);
        }

        // ── 续作品牌加成 ──
        if (proj.PostRelease == PostReleaseType.Sequel && proj.BaseProject != null)
        {
            float brandBonus = proj.BaseProject.BrandPower * 15f;
            float sequelPenalty = proj.BaseProject.PostReleaseCount > 2 ? -(proj.BaseProject.PostReleaseCount - 2) * 3f : 0;
            rawScore += Mathf.Clamp(brandBonus + sequelPenalty, -5f, 30f);
            proj.DevLog.Add($"🔗 续作品牌加成: +{brandBonus + sequelPenalty:F1}分 (IP品牌力{proj.BaseProject.BrandPower:F2})");
        }
        else if ((proj.PostRelease == PostReleaseType.Expansion || proj.PostRelease == PostReleaseType.Remaster
            || proj.PostRelease == PostReleaseType.Port) && proj.BaseProject != null)
        {
            float minorBonus = proj.BaseProject.BrandPower * 8f;
            rawScore += Mathf.Clamp(minorBonus, -3f, 15f);
        }

        // ══════════ 保存基础分（用于绝境翻盘判定）══════════
        float scoreBeforeModifiers = rawScore;

        // ══════════════════ 绝境翻盘传奇（在阈值/哲学修正前判定）══════════════════
        var debtMgr = _gm.GetNodeOrNull<TechDebtManager>("TechDebtManager");
        if (debtMgr != null && debtMgr.ComputeTotalDebt() > 80 && debtMgr.CrunchMode && scoreBeforeModifiers > 75)
        {
            proj.LegendaryLegacy = true;
            proj.LegacyReputationBonus = 20f;
            proj.DevLog.Add($"🏆 绝境翻盘传奇! 永久声誉+20, +8 额外评分（基础分{scoreBeforeModifiers:F1}→触发）");
        }

        // ══════════════════ 阈值裂变评分（加法偏移，禁止乘法叠加）══════════════════
        float gfx = proj.GraphicsScore, gp = proj.GameplayScore, stab = proj.StabilityScore, aud = proj.AudioScore;
        string thresholdTag = null;
        float additiveBonus = 0f;  // 全局加法偏移上限 +25

        // 屎山神作：稳定性极烂但玩法顶级 → 高风险高回报
        if (stab < 30f && gp > 90f)
        {
            additiveBonus = Mathf.Min(additiveBonus + 12f, 25f);
            thresholdTag = Loc.Tr("threshold.shit_masterpiece");
            proj.DevLog.Add($"🔥 {thresholdTag}: 稳定性{stab:F0} + 玩法{gp:F0} → +12");
        }
        // 视听盛宴：画面+玩法双高
        else if (gfx > 80f && gp > 70f)
        {
            additiveBonus = Mathf.Min(additiveBonus + 8f, 25f);
            thresholdTag = Loc.Tr("threshold.visual_feast");
            proj.DevLog.Add($"✨ {thresholdTag}: 画面{gfx:F0} + 玩法{gp:F0} → +8");
        }
        // 平庸惩罚：三核心全在60-70灰色地带
        else if (gfx >= 60f && gfx <= 70f && gp >= 60f && gp <= 70f && aud >= 60f && aud <= 70f)
        {
            additiveBonus = Mathf.Min(additiveBonus - 15f, 25f);
            thresholdTag = Loc.Tr("threshold.mediocre");
            proj.DevLog.Add($"😞 {thresholdTag}: 三核心全部平庸 → -15");
        }

        if (thresholdTag != null) proj.ThresholdTag = thresholdTag;

        // ══════════════════ 设计哲学修正（加法偏移）══════════════════
        switch (proj.Philosophy)
        {
            case DesignPhilosophy.Innovative:
                additiveBonus = Mathf.Min(additiveBonus + 6f, 25f);               // 创新 +6
                additiveBonus += (gfx + gp + aud) * 0.02f;                        // 高方差微调
                proj.DevLog.Add($"💡 设计哲学·创新: +6（高方差）");
                break;
            case DesignPhilosophy.Polished:
                additiveBonus = Mathf.Min(additiveBonus + 4f, 25f);               // 精雕 +4
                rawScore += (gfx + aud) * 0.04f;                                    // 画面/音效权重（不影响加法池）
                rawScore -= gp * 0.02f;
                proj.DevLog.Add($"🖌️ 设计哲学·精雕: +4，画面/音效加权");
                break;
            case DesignPhilosophy.Niche:
                // 小众蓝海：单核突出直接给固定 +15，不再打折再乘
                if (gp > 85f || gfx > 85f)
                {
                    additiveBonus = Mathf.Min(additiveBonus + 15f, 25f);
                    proj.DevLog.Add($"🎯 设计哲学·小众蓝海: 单核突出 → +15");
                }
                else
                {
                    additiveBonus = Mathf.Min(additiveBonus - 5f, 25f);
                    proj.DevLog.Add($"🎯 设计哲学·小众蓝海: 平庸 → -5");
                }
                break;
        }

        // 应用加法偏移（含各种加成）
        rawScore += additiveBonus;
        if (proj.LegendaryLegacy) rawScore += 8f;

        // 加成不能超过基准分的一半：防止低品质靠塞满加成冲满分
        float baseAttrScore = (proj.GameplayScore * 0.3f + proj.GraphicsScore * 0.2f
            + proj.AudioScore * 0.1f + proj.StoryScore * 0.15f
            + proj.NetworkScore * 0.1f + proj.StabilityScore * 0.15f)
            * (0.9f + proj.Scale * 0.2f);
        rawScore = Mathf.Min(rawScore, baseAttrScore + Mathf.Max(baseAttrScore * 0.35f, 8f));

        float scoreCap = _gm.Founder.GetScoreCapBonus() + 98f;
        proj.FinalScore = Mathf.Clamp(rawScore, 0, scoreCap);
        ModAPI.FireHooks(ModAPI.GameHook.AfterScoreCalc);

        // 8.5 和自己竞争：用可比评分覆盖最终分，后续销量/品牌统一使用此值
        // 首款游戏不参与归一化（没有历史可比）
        if (CompletedProjects.Count > 0)
        {
            float bestRaw = CompletedProjects.Max(p => p.FinalScore);
            if (bestRaw > 0)
            {
                float selfComp = Mathf.Clamp(proj.FinalScore / bestRaw * 100f, 20f, 100f);
                proj.FinalScore = selfComp;
            }
        }

        // 随机波动 ±3 分，避免千篇一律满分
        float scoreVariance = (float)(new Random().NextDouble() * 6 - 3);
        proj.FinalScore = Mathf.Clamp(proj.FinalScore + scoreVariance, 0, 100);

        // 5. 评分→销量系数（Mod 可通过 BalanceModDB 覆盖）
        float scoreMultiplier = proj.FinalScore switch
        {
            >= 95 => BalanceModDB.Get("sales.multiplier.95", 1.5f),
            >= 90 => BalanceModDB.Get("sales.multiplier.90", 1.2f),
            >= 80 => BalanceModDB.Get("sales.multiplier.80", 1.0f),
            >= 70 => BalanceModDB.Get("sales.multiplier.70", 0.8f),
            >= 60 => BalanceModDB.Get("sales.multiplier.60", 0.6f),
            _ => BalanceModDB.Get("sales.multiplier.below60", 0.3f)
        };

        // 6. 宣发：期待值差异
        float expectationBoost = proj.Marketing.ExpectationBoost();
        float riskThreshold = proj.Marketing.RiskThreshold();
        float riskPenalty = proj.Marketing.RiskPenalty();
        float expected = 50 + expectationBoost * 50;
        if (proj.FinalScore < riskThreshold)
            scoreMultiplier -= riskPenalty;

        // 7. 销量计算（增加技能/科技权重，降低营销膨胀）
        float baseSalesConst = BalanceModDB.Get("sales.base_constant", 3000f);
        float baseSalesPerScore = BalanceModDB.Get("sales.base_per_score", 350f);
        float baseSales = baseSalesConst + proj.FinalScore * baseSalesPerScore;
        baseSales *= proj.Platform.SalesBase();
        baseSales *= (0.5f + proj.Scale * 1.5f);
        baseSales *= scoreMultiplier;

        // ── 宣发期待值对销量的加成 ──
        float hypeSalesMult = 1f + Mathf.Clamp(proj.MarketingHype / 100f, 0, 0.6f);
        baseSales *= hypeSalesMult;
        if (proj.MarketingHype > 10) proj.DevLog.Add(Loc.TrF("dev.hype_sales", proj.MarketingHype, hypeSalesMult));

        // ── 红海惩罚：同题材过度使用 → 市场饱和销量衰减 ──
        float redOcean = GetRedOceanPenalty(proj.Genre, proj.Theme);
        if (redOcean < 1.0f) { baseSales *= redOcean; proj.DevLog.Add(Loc.TrF("dev.red_ocean", (1f - redOcean) * 100f)); }

        // 营销预算收益递减（超过15万边际效益下降）
        float marketingRatio = proj.MarketingBudget / 200000f;
        float marketingBonus = marketingRatio < 0.5f ? 1f + marketingRatio
            : marketingRatio < 1.5f ? 1.25f + (marketingRatio - 0.5f) * 0.3f
            : 1.55f + (marketingRatio - 1.5f) * 0.1f;
        baseSales *= marketingBonus;

        // 技术债务惩罚：高债务直接扣销量（口碑差）
        float curDebt = DebtMgr?.ComputeTotalDebt() ?? 0;
        if (curDebt > 40) baseSales *= 0.65f;
        else if (curDebt > 25) baseSales *= 0.8f;
        else if (curDebt > 10) baseSales *= 0.92f;

        // IP/系列加成（同IP续作获得老粉丝基础，Software Inc. 式品牌积累）
        if (proj.PredecessorScore > 0)
            baseSales *= 1.15f + (proj.PredecessorScore / 100f) * 0.35f;

        // 发行标签品牌加成（匹配偏好类型/主题额外加成）
        if (!string.IsNullOrEmpty(proj.LabelName))
        {
            var label = Labels.FirstOrDefault(l => l.Name == proj.LabelName);
            if (label != null)
            {
                float labelBonus = 1f + label.AvgScore / 100f * 0.4f; // 高声誉标签 +0~40%
                if (label.PreferredGenre != null && label.PreferredGenre == proj.Genre)
                    labelBonus += 0.1f;
                if (label.PreferredTheme != null && label.PreferredTheme == proj.Theme)
                    labelBonus += 0.05f;
                baseSales *= labelBonus;
            }
        }

        // 8. 黄金窗口（暑期/圣诞+15%~25%）
        int month = _gm.GameMonth % 12;
        if ((month >= 6 && month <= 8) || month >= 11)
            baseSales *= 1.25f;

        // ══════ 粉丝效应（最外层乘数，不受其他系数稀释）══════
        var fanMgr = _gm.GetNodeOrNull<FanManager>("FanManager");
        float fanMultApplied = 1f;
        if (fanMgr != null && fanMgr.TotalFans > 0)
        {
            float faithBonus = fanMgr.GetFaithBonus();
            float diehardRatio = (float)fanMgr.DiehardFans / fanMgr.TotalFans;
            // 死忠粉基础保底: 每个死忠粉贡献5份保底销量
            baseSales += fanMgr.DiehardFans * 5f;
            fanMultApplied = 1f + faithBonus * 0.5f + diehardRatio * 0.3f;
            baseSales *= fanMultApplied;
            proj.DevLog.Add(Loc.TrF("dev.fan_sales", faithBonus, fanMultApplied, diehardRatio * 100f));
        }

        // ── 市场风口：热度影响销量 ──
        if (trendMgr != null)
        {
            float hypeSalesBonus = trendMgr.GetHypeForGenre(proj.Genre).SalesBonus
                * trendMgr.GetHypeForTheme(proj.Theme).SalesBonus;
            float trendSalesBonus = 1f + trendMgr.GetTrendSalesBonus(proj.Genre, proj.Theme);
            baseSales *= hypeSalesBonus * trendSalesBonus;
        }

        // ── 风口保险 (WindInsurance)：预测热度后发售匹配类型享+5%销量 ──
        foreach (var w in _gm.WindInsurances)
        {
            if (w.MonthsLeft > 0 && (w.GenreOrTheme == proj.Genre.ToString() || w.GenreOrTheme == proj.Theme.ToString()))
            {
                baseSales *= (1f + w.SalesBonus);
                proj.DevLog.Add($"🛡 风口保险Buff: {w.GenreOrTheme} 销量+{(w.SalesBonus*100):F0}%");
            }
        }

        // 9. 自我竞争点评信息（FinalScore 已在前面统一计算）
        string selfCompMsg = "";
        if (CompletedProjects.Count > 0)
        {
            float bestRaw = CompletedProjects.Max(p => p.FinalScore);
            if (rawScore < bestRaw * 0.6f)
                selfCompMsg = Loc.TrF("review.best_worse", bestRaw, proj.FinalScore, rawScore);
            else if (rawScore >= bestRaw * 0.9f)
                selfCompMsg = Loc.TrF("review.best_better", bestRaw, rawScore);
            else
                selfCompMsg = Loc.TrF("review.best_vs", bestRaw, proj.FinalScore, rawScore);
        }

        // 10. 免费游戏调整
        if (proj.PriceModel == PriceModel.Free)
        {
            float adjAd = Mathf.Max(0.2f, proj.AdIntensity);
            baseSales *= 2.5f * (1 - adjAd * 0.4f);
            proj.Sales = (int)baseSales;
            proj.Revenue = proj.Sales * Mathf.Lerp(0.3f, 3f, adjAd);
            proj.TotalLifetimeSales = proj.Revenue; // 免费游戏用广告收入作为终身总入账
        }
        else
        {
            proj.Sales = (int)baseSales;
            proj.TotalLifetimeSales = proj.Sales * proj.SuggestedPrice;
            proj.Revenue = 0; // 改为每月入账
        }

        // 首次发布当月的销售（~18%的终身销量在首月）
        float firstMonthRevenue = proj.PriceModel == PriceModel.Free ? proj.TotalLifetimeSales : proj.TotalLifetimeSales * 0.18f;

        // ── 平台抽成（Steam 30%/主机35%/手机40%/全平台45%）──
        float platformCut = proj.Platform switch
        {
            Platform.PC => 0.70f,
            Platform.Console => 0.65f,
            Platform.Mobile => 0.60f,
            Platform.All => 0.55f,
            _ => 0.70f
        };
        float actualRevenue = firstMonthRevenue * platformCut;
        proj.Revenue = actualRevenue;
        proj.MonthlySalesHistory.Add(actualRevenue);
        _res.EarnMoney(actualRevenue, "game_sales");
        proj.MonthsOnMarket = 1; // ProcessMonthlySales 从第1月开始（首月已单独处理）

        // 11. 粉丝增长
        fanMgr.OnGameReleased(proj);

        proj.Phase = DevPhase.Released;
        proj.IsReleased = true;
        proj.OriginalReleaseMonth = _gm.GameMonth;
        proj.BrandPower = (proj.Sales / 100000f + proj.FinalScore / 100f) / 2f; // 销量+评分归一化
        proj.FanSatisfaction = 1f;

        // 更新发行标签声誉
        if (!string.IsNullOrEmpty(proj.LabelName))
        {
            var label = Labels.FirstOrDefault(l => l.Name == proj.LabelName);
            if (label != null) { label.GameCount++; label.TotalScore += proj.FinalScore; }
        }

        // ── 模块差异化：找到最强模块 ──
        var modules = new (string name, float val)[] {
            (Loc.Tr("review.gameplay"), proj.GameplayScore), (Loc.Tr("review.graphics"), proj.GraphicsScore),
            (Loc.Tr("review.audio"), proj.AudioScore), (Loc.Tr("review.story"), proj.StoryScore),
            (Loc.Tr("review.stability"), proj.StabilityScore), (Loc.Tr("review.network"), proj.NetworkScore)
        };
        var best = modules.OrderByDescending(m => m.val).First();
        var worst = modules.OrderBy(m => m.val).First();

        // 双轨评分：评审分(和自己竞争) + 绝对质量分（用于媒体评分页展示）
        proj.DevLog.Add($"发售！首月收入 ¥{proj.Revenue:N0}，预估终身 ¥{proj.TotalLifetimeSales:N0}{selfCompMsg}");

        // ══════════ 发售时自动暂停 ══════════
        _gm.Paused = true;

        // ══════════ 媒体评分页面 ══════════
        ShowReviewPage(proj, rawScore, priceMediaPenalty, selfCompMsg, best.name, worst.name);

        // 首月营收记录
        MonthlyRevenueLog.Add((_gm.GameMonth, proj.Name, (long)proj.Revenue));
        TotalRevenue += (long)proj.Revenue;

        // ══════════ 粉丝请愿检查 ══════════
        _gm.CheckFanPetitionReward(proj);

        // ══════════ 发售时成就检查 ══════════
        Services.AchievementManager.MonthlyCheck();

        // 12. 续作满意度检查
        if (proj.PredecessorScore > 0)
        {
            float expectation = Mathf.Clamp(proj.PredecessorScore * 1.2f, 40, 100);
            float satisfaction = proj.FinalScore - expectation;
            string seqResult = satisfaction switch
            {
                > 10 => Loc.TrF("dev.seq_result_10", expectation, proj.FinalScore),
                >= 5 => Loc.TrF("dev.seq_result_5", expectation),
                >= -5 => Loc.TrF("dev.seq_result_0", expectation),
                >= -10 => Loc.TrF("dev.seq_result_m5", expectation),
                _ => Loc.TrF("dev.seq_result_m10", expectation)
            };
            proj.DevLog.Add($"续作满意度: {seqResult}");
        }

        // 13. 发售日事件
        var storyEvt = Services.StoryEvents;
        string releaseEvt = storyEvt?.PickReleaseEvent(proj, this);
        if (releaseEvt != null)
            proj.DevLog.Add($"发售日事件: {releaseEvt}");

        if (team != null)
        {
            team.Task = TeamTask.None;
            team.CurrentProject = null;
        }

        // 员工项目计数
        if (team != null) foreach (var emp in team.Members)
        {
            emp.ProjectsCompleted++;
            if (proj.FinalScore >= 85)
                emp.HighScoreProjects++;
        }

        // ── 蓝海开拓者粉丝暴增 ──
        int noveltyFanBoost = GetNoveltyFanBoost(proj.Genre, proj.Theme);
        if (noveltyFanBoost > 0)
        {
            if (fanMgr != null) fanMgr.CasualFans += noveltyFanBoost;
            proj.Sales += noveltyFanBoost / 2;
            proj.DevLog.Add(Loc.TrF("dev.novelty_fans", noveltyFanBoost));
        }

        CompletedProjects.Add(proj);

        // 发售时立即检查成就
        Services.AchievementManager.OnGameReleased();
        _gm.GetNodeOrNull<TutorialManager>("TutorialManager")?.NotifyAction("game_released");
        ModAPI.FireHooks(ModAPI.GameHook.AfterGameRelease);

        // 更新信任度
        if (proj.DelayCount > 0 || proj.ExpectedScore > 0) {
            UpdateTrustAfterRelease(proj, proj.FinalScore);
        }

        // ── 新系统联动 ──
        var gm = _gm;
        var dna = gm?.GetNodeOrNull<StudioDNA>("StudioDNA");
        dna?.OnProjectCompleted(proj);
        var audience = gm?.GetNodeOrNull<AudienceSystem>("AudienceSystem");
        audience?.OnGameReleased(proj);
        var brand = gm?.GetNodeOrNull<BrandSystem>("BrandSystem");
        brand?.OnGameReleased(proj);
        var liveops = gm?.GetNodeOrNull<LiveOpsSystem>("LiveOpsSystem");
        liveops?.InitGameMetrics(proj);
        // 注册档期
        var cal = gm?.GetNodeOrNull<ReleaseCalendar>("ReleaseCalendar");
        cal?.RegisterCompetitorSlot(gm.GameMonth, gm.ResMgr?.ToString() ?? "player", proj.Name, proj.FinalScore);

        // 应用DNA加成到评分
        if (dna != null)
        {
            float genreBonus = dna.GetGenreBonus(proj.Genre);
            float fatigue = dna.GetFatiguePenalty(proj.Genre);
            proj.FinalScore = Mathf.Clamp(proj.FinalScore * (1 + genreBonus + fatigue), 0, 100);
        }
        // 品牌一致性惩罚
        if (brand != null)
            proj.FinalScore = Mathf.Clamp(proj.FinalScore * (1 + brand.GetCoherencePenalty(proj)), 0, 100);
        // 档期倍率
        if (cal != null)
        {
            float slotMult = cal.CalculateFinalMultiplier(gm.GameMonth, proj.Genre);
            proj.Sales = (int)(proj.Sales * slotMult);
        }
    }

    // ══════════════════ 媒体评分页面（动画增强版） ══════════════════
    private void ShowReviewPage(GameProject proj, float origScore, float pricePenalty, string selfCompMsg, string bestModule, string worstModule)
    {
        var vp = _gm.GetViewport().GetVisibleRect().Size;
        var S = (Func<float, float>)(v => v * _gm.UIScale);
        float pw = S(540), ph = S(520);

        // 全屏遮罩拦截所有鼠标事件（含滚轮）
        var overlay = new ColorRect { Color = new Color(0, 0, 0, 0.35f), MouseFilter = Control.MouseFilterEnum.Stop };
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _gm.UiLayer.AddChild(overlay);

        var panel = new Panel { Position = new((vp.X - pw) / 2, (vp.Y - ph) / 2), Size = new(pw, ph) };
        panel.MouseFilter = Control.MouseFilterEnum.Stop;
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.97f, 0.96f, 0.94f, 0.97f),
            CornerRadiusTopLeft = 14, CornerRadiusTopRight = 14, CornerRadiusBottomLeft = 14, CornerRadiusBottomRight = 14,
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = proj.FinalScore >= 80 ? new Color(0.2f, 0.5f, 0.2f, 0.6f) : proj.FinalScore >= 50 ? new Color(0.6f, 0.5f, 0.1f, 0.6f) : new Color(0.7f, 0.2f, 0.2f, 0.6f)
        });

        var title = new Label { Text = Loc.TrF("dev.review_title", proj.Name), Position = new(S(20), S(12)), Size = new(pw - S(40), S(28)) };
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeColorOverride("font_color", new Color(0.2f, 0.2f, 0.3f));
        panel.AddChild(title);

        // ── 评分揭晓：先显示 ???，0.5秒后揭晓 ──
        string stars = proj.FinalScore >= 95 ? "⭐⭐⭐⭐⭐" : proj.FinalScore >= 85 ? "⭐⭐⭐⭐" : proj.FinalScore >= 70 ? "⭐⭐⭐" : proj.FinalScore >= 50 ? "⭐⭐" : "⭐";
        string grade = proj.FinalScore >= 95 ? "S" : proj.FinalScore >= 85 ? "A" : proj.FinalScore >= 70 ? "B" : proj.FinalScore >= 50 ? "C" : "D";
        Color gradeColor = proj.FinalScore >= 85 ? new Color(0.2f, 0.9f, 0.3f) : proj.FinalScore >= 70 ? new Color(0.6f, 0.8f, 0.2f) : proj.FinalScore >= 50 ? new Color(0.9f, 0.7f, 0.2f) : new Color(0.9f, 0.3f, 0.2f);

        var scoreReveal = new Label { Text = Loc.Tr("dev.score_reveal"), Position = new(S(20), S(46)), Size = new(pw - S(40), S(28)) };
        scoreReveal.AddThemeFontSizeOverride("font_size", 16);
        scoreReveal.AddThemeColorOverride("font_color", new Color(0.3f, 0.3f, 0.4f));
        panel.AddChild(scoreReveal);

        var salesLabel = new Label { Text = Loc.Tr("dev.sales_reveal"), Position = new(S(20), S(76)), Size = new(pw - S(40), S(20)) };
        salesLabel.AddThemeFontSizeOverride("font_size", 13);
        salesLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.55f, 0.6f));
        panel.AddChild(salesLabel);

        if (!string.IsNullOrEmpty(selfCompMsg))
        {
            var selfLine = new Label { Text = selfCompMsg.Replace("\n", " ").Trim(), Position = new(S(20), S(96)), Size = new(pw - S(40), S(24)) };
            selfLine.AddThemeFontSizeOverride("font_size", 11);
            selfLine.AddThemeColorOverride("font_color", new Color(0.5f, 0.6f, 0.7f));
            selfLine.AutowrapMode = TextServer.AutowrapMode.Word;
            panel.AddChild(selfLine);
        }

        // ── 0.5秒后揭晓评分：数字跳动 + 颜色闪变 ──
        _gm.GetTree().CreateTimer(0.5f).Timeout += () =>
        {
            scoreReveal.Text = Loc.TrF("dev.score_final", proj.FinalScore, grade);
            scoreReveal.AddThemeColorOverride("font_color", gradeColor);
            var tween = panel.CreateTween();
            tween.TweenProperty(scoreReveal, "scale", new Vector2(1.15f, 1.15f), 0.15f);
            tween.TweenProperty(scoreReveal, "scale", Vector2.One, 0.25f);

            salesLabel.Text = Loc.TrF("dev.sales_final", proj.Sales, proj.Revenue, stars);
            salesLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.85f, 1f));
        };

        // ── 10家媒体评测逐条飞入 ──
        var mediaNames = new[] { "IGN", "GameSpot", "PC Gamer", "Metacritic", "Edge", "Kotaku", "Eurogamer", "Fami通", "游民星空", "3DM" };
        var rng = new Random(proj.Name.GetHashCode());
        var scroll = new ScrollContainer { Position = new(S(16), S(122)), Size = new(pw - S(32), S(320)) };
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        var list = new VBoxContainer();
        scroll.AddChild(list);
        panel.AddChild(scroll);

        for (int i = 0; i < 10; i++)
        {
            float bias = -(float)(rng.NextDouble() * 25);
            float mediaScore = Mathf.Clamp(proj.FinalScore + bias, 5, 98);

            // ── 每家媒体基于实际评分(mediaScore)给出与分数一致的评语 ──
            string comment;
            switch (i)
            {
                case 0: comment = mediaScore >= 85 ? "玩法出色，整体体验顶尖，毫无疑问的年度候选"
                    : mediaScore >= 70 ? "核心玩法有亮点，整体体验值得肯定"
                    : mediaScore >= 50 ? "玩法尚可，但其他短板拖了后腿"
                    : "玩法平庸，缺乏让人玩下去的驱动力"; break;
                case 1: comment = mediaScore >= 85 ? "视觉表现令人惊叹，每一帧都堪称艺术"
                    : mediaScore >= 70 ? "画面风格鲜明，整体观感舒适"
                    : mediaScore >= 50 ? "美术表现中规中矩，缺乏惊艳感"
                    : "画面粗糙，严重拉低了整体品质"; break;
                case 2: comment = mediaScore >= 85 ? "音画俱佳，全方位无短板的优秀作品"
                    : mediaScore >= 70 ? "音频表现合格，与整体风格匹配"
                    : mediaScore >= 50 ? "音乐无功无过，音效偶有出戏"
                    : "音频表现糟糕，建议关声音游玩"; break;
                case 3: comment = mediaScore >= 85 ? "剧情与玩法相辅相成，沉浸感极强"
                    : mediaScore >= 70 ? "故事有亮点，节奏把控得当"
                    : mediaScore >= 50 ? "剧情平淡可预测，人物塑造不够立体"
                    : "剧情混乱，存在感极低"; break;
                case 4: comment = mediaScore >= 85 ? "技术力拉满，稳定性堪称行业标杆"
                    : mediaScore >= 70 ? "运行稳定，偶有小问题但不影响体验"
                    : mediaScore >= 50 ? "Bug频繁出现，影响正常游玩"
                    : "稳定性是最大短板，建议等待补丁"; break;
                case 5: comment = mediaScore >= 85 ? "联机体验丝般顺滑，服务器令人放心"
                    : mediaScore >= 70 ? "联机功能完备，体验可接受"
                    : mediaScore >= 50 ? "线上模式体验欠佳，掉线频繁"
                    : "联机部分基本不可用"; break;
                case 6: comment = mediaScore >= 85 ? "综合素质出色，年度游戏的有力竞争者"
                    : mediaScore >= 70 ? "各方面协调性好，看得出团队的用心"
                    : mediaScore >= 50 ? "优缺点都很明显，再打磨数月会更好"
                    : "充满遗憾的作品，各方面都欠火候"; break;
                case 7: comment = mediaScore >= 85 ? "沉浸式体验，听觉与叙事的双重盛宴"
                    : mediaScore >= 70 ? "叙事是最大亮点，整体体验不错"
                    : mediaScore >= 50 ? "叙事中规中矩，玩法才是重点"
                    : "乏善可陈，没有让人记住的瞬间"; break;
                case 8: comment = mediaScore >= 85 ? "重新定义了同类作品的天花板"
                    : mediaScore >= 70 ? "合格的作品，值得推荐给类型爱好者"
                    : mediaScore >= 50 ? "优缺点明显，建议观望或等折扣"
                    : "很难找到推荐理由"; break;
                default: comment = mediaScore >= 85 ? "无明显短板，各方面都展现了高水准"
                    : mediaScore >= 70 ? "整体协调，是合格的商业作品"
                    : mediaScore >= 50 ? "整体中规中矩，没有突出亮点"
                    : "整体质量堪忧，不推荐"; break;
            }

            var row = new VBoxContainer { CustomMinimumSize = new(0, S(42)), Modulate = new Color(1, 1, 1, 0) };

            var topRow = new HBoxContainer();
            var mediaLabel = new Label { Text = mediaNames[i], CustomMinimumSize = new(S(75), S(18)) };
            mediaLabel.AddThemeFontSizeOverride("font_size", 12);
            mediaLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.65f, 0.7f));
            topRow.AddChild(mediaLabel);

            var scoreLabel = new Label { Text = $"{mediaScore:F0}", CustomMinimumSize = new(S(36), S(18)) };
            scoreLabel.AddThemeFontSizeOverride("font_size", 13);
            scoreLabel.AddThemeColorOverride("font_color", mediaScore >= 85 ? new Color(0.2f, 0.9f, 0.3f) : mediaScore >= 70 ? new Color(0.6f, 0.8f, 0.2f) : mediaScore >= 50 ? new Color(0.9f, 0.7f, 0.2f) : new Color(0.9f, 0.3f, 0.2f));
            topRow.AddChild(scoreLabel);
            row.AddChild(topRow);

            var commentLabel = new Label { Text = $"  \"{comment}\"", CustomMinimumSize = new(0, S(18)) };
            commentLabel.AddThemeFontSizeOverride("font_size", 11);
            commentLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.73f, 0.78f));
            row.AddChild(commentLabel);
            list.AddChild(row);

            // 每条延迟0.3秒飞入
            float delay = 1.0f + i * 0.3f;
            _gm.GetTree().CreateTimer(delay).Timeout += () =>
            {
                row.Modulate = new Color(1, 1, 1, 0);
                var rowTween = row.CreateTween();
                rowTween.TweenProperty(row, "modulate", new Color(1, 1, 1, 1), 0.3f);
            };
        }

        // 关闭按钮（等媒体全部展示完才显示）
        var closeBtn = new Button { Text = Loc.Tr("ui.got_it"), Position = new((pw - S(120)) / 2, ph - S(44)), Size = new(S(120), S(36)), Modulate = new Color(1, 1, 1, 0) };
        closeBtn.AddThemeFontSizeOverride("font_size", 15);
        closeBtn.Pressed += () => { panel.QueueFree(); overlay.QueueFree(); _gm.Paused = false; };
        panel.AddChild(closeBtn);

        _gm.GetTree().CreateTimer(4.5f).Timeout += () =>
        {
            var btnTween = closeBtn.CreateTween();
            btnTween.TweenProperty(closeBtn, "modulate", new Color(1, 1, 1, 1), 0.3f);
        };

        _gm.UiLayer.AddChild(panel);
    }

    // ══════════════════ 月度销售处理 ══════════════════
    /// <summary>每月对所有已发售游戏计算持续销量（长期衰减，细水长流）</summary>
    public void ProcessMonthlySales()
    {
        ModAPI.FireHooks(ModAPI.GameHook.BeforeMonthlySales);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeMonthlySales)) return;
        // 首月=18%（已单独处理），剩余82%按幂律分布到18或36个月
        foreach (var proj in Projects)
        {
            if (!proj.IsReleased) continue;
            int maxMonths = proj.PriceModel == PriceModel.Free ? 18 : 36;
            int idx = proj.MonthsOnMarket;
            if (idx < 1 || idx >= maxMonths) continue;

            // 幂律衰减：第i月份额 ∝ 1/i^exp
            float exponent = proj.PriceModel == PriceModel.Free ? 1.3f : 1.15f;

            // ── Mod工具包：长尾衰减减半 ──
            if (proj.IsLongTail)
            {
                exponent = Mathf.Max(0.5f, exponent - 0.5f);
                maxMonths = Mathf.Min(48, maxMonths + 12); // 额外延长销售周期
            }

            // 预计算归一化分母（每月相同，可优化但懒得搞）
            float denom = 0f;
            for (int i = 1; i < maxMonths; i++)
                denom += 1f / Mathf.Pow(i, exponent);
            float share = (1f / Mathf.Pow(idx, exponent)) / denom * 0.82f;

            float thisMonth = proj.TotalLifetimeSales * share + 50; // 保底50

            // 轻度随机波动（±30%），模拟淡旺季
            float rngFactor = 0.7f + (float)Random.Shared.NextDouble() * 0.6f;
            thisMonth *= rngFactor;

            // ── 平台抽成 ──
            float platCut = proj.Platform switch
            {
                Platform.PC => 0.70f, Platform.Console => 0.65f,
                Platform.Mobile => 0.60f, Platform.All => 0.55f, _ => 0.70f
            };
            thisMonth *= platCut;

            // ── 主机世代影响 ──
            if ((proj.Platform == Platform.Console || proj.Platform == Platform.All)
                && _gm.ConsoleSwitchMonth > 0)
            {
                // 上一代主机游戏（在新主机发售前发布的）：销量额外衰减
                if (proj.OriginalReleaseMonth < _gm.ConsoleSwitchMonth)
                    thisMonth *= 0.95f; // 旧主机游戏每月多衰减5%
                // 当前世代主机发布后推出的新游戏：获得热度加成
                else if (proj.OriginalReleaseMonth >= _gm.ConsoleSwitchMonth
                    && _gm.GameMonth - _gm.ConsoleSwitchMonth <= 24)
                    thisMonth *= 1.10f; // 新主机游戏在前两年有10%销量加成
            }

            proj.Revenue += thisMonth;
            proj.MonthlySalesHistory.Add(thisMonth);
            _res.EarnMoney(thisMonth, "game_sales");
            MonthlyRevenueLog.Add((_gm.GameMonth, proj.Name, (long)thisMonth));
            TotalRevenue += (long)thisMonth;
            proj.MonthsOnMarket++;
        }
        ModAPI.FireHooks(ModAPI.GameHook.AfterMonthlySales);
    }

    /// <summary>
    /// 消耗灵感提升属性
    /// </summary>
    public void BoostWithInspiration(GameProject proj, string attribute, float cost)
    {
        if (!_res.SpendInspiration(cost)) return;

        switch (attribute)
        {
            case "gameplay":
                proj.GameplayScore += 5 + Random.Shared.Next(5);
                break;
            case "graphics":
                proj.GraphicsScore += 5 + Random.Shared.Next(5);
                break;
            case "audio":
                proj.AudioScore += 5 + Random.Shared.Next(5);
                break;
            case "stability":
                proj.StabilityScore += 3 + Random.Shared.Next(3);
                proj.BugCount = (int)(proj.BugCount * 0.7f);
                break;
        }
        proj.DevLog.Add($"✨ 消耗灵感{cost}点提升{attribute}");
    }

    // ══════════════════ 后续内容系统（8种类型）══════════════════

    /// <summary>获取后续内容类型的成本系数</summary>
    public static float GetPostReleaseCostFactor(PostReleaseType type) => type switch
    {
        PostReleaseType.BugFixPatch => 0.03f,
        PostReleaseType.ContentUpdate => 0.10f,
        PostReleaseType.SkinDLC => 0.05f,
        PostReleaseType.Expansion => 0.30f,
        PostReleaseType.Remaster => 0.50f,
        PostReleaseType.Port => 0.20f,
        PostReleaseType.Sequel => 0.60f,
        PostReleaseType.ModKit => 0f, // 固定费用
        _ => 0f
    };

    /// <summary>获取后续内容需要的月份数</summary>
    public static float GetPostReleaseMonths(PostReleaseType type, GameProject baseProj) => type switch
    {
        PostReleaseType.BugFixPatch => 2f,
        PostReleaseType.ContentUpdate => 4f,
        PostReleaseType.SkinDLC => 2f,
        PostReleaseType.Expansion => baseProj.EstimatedMonths * 0.35f,
        PostReleaseType.Remaster => baseProj.EstimatedMonths * 0.5f,
        PostReleaseType.Port => 3f,
        PostReleaseType.Sequel => baseProj.EstimatedMonths * 0.8f,
        PostReleaseType.ModKit => 2f,
        _ => 1f
    };

    /// <summary>获取后续内容所需成本</summary>
    public static long GetPostReleaseCost(PostReleaseType type, GameProject baseProj) => type switch
    {
        PostReleaseType.ModKit => 50000,
        _ => (long)(baseProj.DevelopmentCost * GetPostReleaseCostFactor(type))
    };

    /// <summary>为已发售游戏创建后续内容项目（8种类型）</summary>
    public GameProject CreatePostReleaseContent(GameProject baseProj, PostReleaseType type, PostReleaseStrategy strategy = PostReleaseStrategy.Balanced)
    {
        if (!baseProj.IsReleased) return null;

        long cost = GetPostReleaseCost(type, baseProj);
        if (cost > 0 && !_res.SpendMoney(cost, "post_release"))
            return null;

        // ── 策略影响：激进快但波动大，保守慢但稳定 ──
        float strategyTimeMult = strategy == PostReleaseStrategy.Aggressive ? 0.7f
            : strategy == PostReleaseStrategy.Conservative ? 1.3f : 1f;
        float strategyQualityFlat = strategy == PostReleaseStrategy.Conservative ? 5f
            : strategy == PostReleaseStrategy.Aggressive ? -3f : 0f;
        float strategyQualityVar = strategy == PostReleaseStrategy.Aggressive ? 0.15f
            : strategy == PostReleaseStrategy.Conservative ? 0.03f : 0.08f;

        string nameSuffix = type switch
        {
            PostReleaseType.BugFixPatch => "大型补丁",
            PostReleaseType.ContentUpdate => "内容更新",
            PostReleaseType.SkinDLC => "皮肤包",
            PostReleaseType.Expansion => "资料片",
            PostReleaseType.Remaster => "重制版",
            PostReleaseType.Port => "移植版",
            PostReleaseType.Sequel => "续作",
            PostReleaseType.ModKit => "Mod工具包",
            _ => "后续内容"
        };

        float months = GetPostReleaseMonths(type, baseProj) * strategyTimeMult;
        float scaleBase = type switch
        {
            PostReleaseType.BugFixPatch => 0.10f,
            PostReleaseType.ContentUpdate => 0.15f,
            PostReleaseType.SkinDLC => 0.08f,
            PostReleaseType.Expansion => 0.50f,
            PostReleaseType.Remaster => 0.70f,
            PostReleaseType.Port => 0.30f,
            PostReleaseType.Sequel => 1.0f,  // 续作=新游戏规模
            PostReleaseType.ModKit => 0.05f,
            _ => 0.2f
        };

        var proj = new GameProject
        {
            Name = $"《{baseProj.Name}》{nameSuffix}",
            Genre = baseProj.Genre,
            Theme = baseProj.Theme,
            Platform = baseProj.Platform,
            Phase = DevPhase.Planning,
            Scale = scaleBase,
            EstimatedMonths = months,
            PriceModel = baseProj.PriceModel == PriceModel.Free
                ? PriceModel.Free : (type == PostReleaseType.SkinDLC ? PriceModel.BuyToPlay : baseProj.PriceModel),
            PostRelease = type,
            BaseProject = baseProj,
            IPName = baseProj.IPName,
            LabelName = baseProj.LabelName,
            BudgetGraphics = baseProj.BudgetGraphics,
            BudgetAudio = baseProj.BudgetAudio,
            BudgetGameplay = baseProj.BudgetGameplay,
            FanSatisfaction = baseProj.FanSatisfaction,
        };

        // ── 续作品牌加成 ──
        if (type == PostReleaseType.Sequel)
        {
            // 续作继承品牌力并在原类型/主题基础上小幅变化
            proj.Genre = (GameGenre)(((int)baseProj.Genre + Random.Shared.Next(1, 3)) % Enum.GetValues<GameGenre>().Length);
        }

        // ── 定价 ──
        proj.SuggestedPrice = type switch
        {
            PostReleaseType.Expansion => baseProj.SuggestedPrice * 0.5f,
            PostReleaseType.Remaster => baseProj.SuggestedPrice * 0.8f,
            PostReleaseType.Port => baseProj.SuggestedPrice * 0.7f,
            PostReleaseType.SkinDLC => Mathf.Max(5, baseProj.SuggestedPrice * 0.2f),
            PostReleaseType.BugFixPatch => 0,
            PostReleaseType.ModKit => 0,
            PostReleaseType.ContentUpdate => baseProj.SuggestedPrice * 0.15f,
            PostReleaseType.Sequel => baseProj.SuggestedPrice,
            _ => baseProj.SuggestedPrice * 0.3f
        };

        // ── 继承属性 ──
        proj.GraphicsScore = baseProj.GraphicsScore;
        proj.GameplayScore = baseProj.GameplayScore;
        proj.AudioScore = baseProj.AudioScore;
        proj.StoryScore = baseProj.StoryScore;
        proj.StabilityScore = baseProj.StabilityScore;

        // ── 按类型追加属性加成 ──
        switch (type)
        {
            case PostReleaseType.BugFixPatch:
                proj.StabilityScore = Mathf.Min(100, baseProj.StabilityScore + 15);
                proj.BugCount = (int)(baseProj.BugCount * 0.4f);
                break;
            case PostReleaseType.ContentUpdate:
                proj.GraphicsScore = Mathf.Min(100, baseProj.GraphicsScore + 5);
                proj.GameplayScore = Mathf.Min(100, baseProj.GameplayScore + 5);
                proj.AudioScore = Mathf.Min(100, baseProj.AudioScore + 5);
                break;
            case PostReleaseType.SkinDLC:
                proj.GraphicsScore = Mathf.Min(100, baseProj.GraphicsScore + 8);
                break;
            case PostReleaseType.Expansion:
                proj.GameplayScore = Mathf.Min(100, baseProj.GameplayScore + 10);
                proj.StoryScore = Mathf.Min(100, baseProj.StoryScore + 8);
                break;
            case PostReleaseType.Remaster:
                proj.GraphicsScore = Mathf.Min(100, baseProj.GraphicsScore + 18);
                proj.AudioScore = Mathf.Min(100, baseProj.AudioScore + 15);
                proj.StabilityScore = Mathf.Min(100, baseProj.StabilityScore + 10);
                break;
            case PostReleaseType.Port:
                // 移植不改变属性
                break;
            case PostReleaseType.Sequel:
                proj.GraphicsScore = Mathf.Min(100, baseProj.GraphicsScore + 8 + Random.Shared.Next(5));
                proj.GameplayScore = Mathf.Min(100, baseProj.GameplayScore + 10 + Random.Shared.Next(5));
                proj.StoryScore = Mathf.Min(100, baseProj.StoryScore + 6);
                break;
            case PostReleaseType.ModKit:
                // Mod工具包无属性变化
                break;
        }

        // ── 策略质量修正 ──
        if (strategyQualityFlat != 0)
        {
            float varBias = (float)(Random.Shared.NextDouble() - 0.5) * strategyQualityVar * 20f;
            proj.GraphicsScore = Mathf.Clamp(proj.GraphicsScore + strategyQualityFlat + varBias, 0, 100);
            proj.GameplayScore = Mathf.Clamp(proj.GameplayScore + strategyQualityFlat + varBias * 0.7f, 0, 100);
            proj.AudioScore = Mathf.Clamp(proj.AudioScore + strategyQualityFlat * 0.8f + varBias * 0.5f, 0, 100);
        }
        string strategyLabel = strategy == PostReleaseStrategy.Aggressive ? "[激进]" : strategy == PostReleaseStrategy.Conservative ? "[保守]" : "";

        proj.DevLog.Add($"[后续内容/{(int)type}]{strategyLabel} {nameSuffix} 基于《{baseProj.Name}》立项");
        CheckEngineCompatibility(proj);
        ApplyTechBonuses(proj);
        Projects.Add(proj);
        baseProj.PostReleaseCount++;

        _gm.ShowToast("✅ 后续内容立项", $"已创建{nameSuffix}项目\n成本:¥{cost:N0}  预计{months:F0}个月", new Color(0.3f, 0.7f, 0.5f));
        return proj;
    }

    /// <summary>后续内容项目开发完成时自动调用——直接将效果应用到原项目</summary>
    private void ApplyPostReleaseEffect(GameProject proj)
    {
        var baseProj = proj.BaseProject;
        if (baseProj == null) return;

        string nameSuffix = proj.PostRelease switch
        {
            PostReleaseType.BugFixPatch => "Bug修复补丁",
            PostReleaseType.ContentUpdate => "内容更新",
            PostReleaseType.SkinDLC => "皮肤DLC",
            PostReleaseType.Expansion => "大型资料片",
            PostReleaseType.Remaster => "重制版",
            PostReleaseType.Port => "平台移植",
            PostReleaseType.Sequel => "续作",
            PostReleaseType.ModKit => "Mod工具包",
            _ => "后续内容"
        };

        string icon = proj.PostRelease switch
        {
            PostReleaseType.BugFixPatch => "🔧",
            PostReleaseType.ContentUpdate => "🎁",
            PostReleaseType.SkinDLC => "👗",
            PostReleaseType.Expansion => "📚",
            PostReleaseType.Remaster => "✨",
            PostReleaseType.Port => "🎮",
            PostReleaseType.Sequel => "🔗",
            PostReleaseType.ModKit => "🔓",
            _ => "📦"
        };

        switch (proj.PostRelease)
        {
            case PostReleaseType.BugFixPatch:
                baseProj.StabilityScore = Mathf.Min(100, baseProj.StabilityScore + 15);
                baseProj.BugCount = Mathf.Max(0, (int)(baseProj.BugCount * 0.4f));
                baseProj.FanSatisfaction = Mathf.Min(2f, baseProj.FanSatisfaction + 0.15f);
                baseProj.MonthsOnMarket = Mathf.Max(1, baseProj.MonthsOnMarket - 2);
                break;
            case PostReleaseType.ContentUpdate:
                baseProj.GraphicsScore = Mathf.Min(100, baseProj.GraphicsScore + 5);
                baseProj.GameplayScore = Mathf.Min(100, baseProj.GameplayScore + 5);
                baseProj.AudioScore = Mathf.Min(100, baseProj.AudioScore + 5);
                baseProj.MonthsOnMarket = Mathf.Max(1, baseProj.MonthsOnMarket - 3);
                baseProj.FanSatisfaction = Mathf.Min(2f, baseProj.FanSatisfaction + 0.1f);
                break;
            case PostReleaseType.SkinDLC:
                var fanMgr = Services.FanManager;
                float totalFans = fanMgr?.TotalFans ?? 1000;
                float buyRate = Mathf.Clamp(baseProj.FanSatisfaction * 0.25f, 0.05f, 0.6f);
                long skinRevenue = (long)(totalFans * buyRate * proj.SuggestedPrice * 0.2f);
                _res.EarnMoney(skinRevenue, "skin_dlc");
                baseProj.FanSatisfaction = Mathf.Clamp(baseProj.FanSatisfaction - 0.03f, 0.2f, 2f);
                _gm.ShowToast("👗 皮肤DLC发售", $"《{baseProj.Name}》皮肤包售出约{buyRate*100:F0}%粉丝\n收入:¥{skinRevenue:N0}", new Color(0.8f, 0.5f, 0.9f));
                break;
            case PostReleaseType.Expansion:
                baseProj.GameplayScore = Mathf.Min(100, baseProj.GameplayScore + 10);
                baseProj.StoryScore = Mathf.Min(100, baseProj.StoryScore + 8);
                baseProj.MonthsOnMarket = Mathf.Max(1, baseProj.MonthsOnMarket - 6);
                break;
            case PostReleaseType.Remaster:
                baseProj.GraphicsScore = Mathf.Min(100, baseProj.GraphicsScore + 18);
                baseProj.AudioScore = Mathf.Min(100, baseProj.AudioScore + 15);
                baseProj.StabilityScore = Mathf.Min(100, baseProj.StabilityScore + 10);
                break;
            case PostReleaseType.ModKit:
                baseProj.HasModKit = true;
                baseProj.IsLongTail = true;
                baseProj.FanSatisfaction = Mathf.Min(2f, baseProj.FanSatisfaction + 0.2f);
                break;
            case PostReleaseType.Port:
                // 移植：重新走发售流程
                ReleaseGameProject(proj, null);
                return; // 不走后续
            case PostReleaseType.Sequel:
                // 续作：重新走发售流程
                ReleaseGameProject(proj, null);
                return;
        }

        // ── 市场浪潮动态加成 ──
        var trendMgr = _gm.GetNodeOrNull<MarketTrendManager>("MarketTrendManager");
        if (trendMgr != null)
        {
            float hypeBoost = trendMgr.GetHypeForGenre(baseProj.Genre).SalesBonus
                * trendMgr.GetHypeForTheme(baseProj.Theme).SalesBonus;
            if (hypeBoost > 1.05f)
            {
                // 风口期发布后发内容，效果额外+20%
                if (proj.PostRelease == PostReleaseType.ContentUpdate)
                    baseProj.MonthsOnMarket = Mathf.Max(1, baseProj.MonthsOnMarket - 2);
                else if (proj.PostRelease == PostReleaseType.Expansion)
                    baseProj.GameplayScore = Mathf.Min(100, baseProj.GameplayScore + 3);
                else if (proj.PostRelease == PostReleaseType.SkinDLC)
                    baseProj.FanSatisfaction = Mathf.Min(2f, baseProj.FanSatisfaction + 0.1f);
            }
        }

        baseProj.PostReleaseCount++;
        _gm.ShowToast($"{icon} {nameSuffix}完成", $"《{baseProj.Name}》{nameSuffix}已上线！\n{GetEffectSummary(proj.PostRelease)}", new Color(0.3f, 0.7f, 0.5f));
    }

    private static string GetEffectSummary(PostReleaseType type) => type switch
    {
        PostReleaseType.BugFixPatch => "稳定性提升，Bug大幅减少",
        PostReleaseType.ContentUpdate => "三维属性提升，销售寿命延长",
        PostReleaseType.SkinDLC => "按粉丝规模获得额外收入",
        PostReleaseType.Expansion => "玩法与剧情深度提升",
        PostReleaseType.Remaster => "画面与音频全面翻新",
        PostReleaseType.Port => "登陆新平台，再掀销售热潮",
        PostReleaseType.Sequel => "IP传承，品牌加成",
        PostReleaseType.ModKit => "Mod社区激活，长尾销售延长",
        _ => ""
    };

    /// <summary>续作/移植完成时直接发售（跳过打磨流程）</summary>
    private void LegacyReleaseGameDirect(GameProject proj)
    {
        // 复用ReleaseGame的评分计算，但不走打磨/QA流程
        float rawScore = (proj.GraphicsScore + proj.GameplayScore + proj.AudioScore
            + proj.StoryScore + proj.StabilityScore) / 5f;
        float scoreCap = _gm.Founder.GetScoreCapBonus() + 100f;
        proj.FinalScore = Mathf.Clamp(rawScore, 0, scoreCap);
        proj.Sales = (int)(3000 + proj.FinalScore * 150 + proj.SuggestedPrice * 2);
        proj.Phase = DevPhase.Released;
        proj.IsReleased = true;
        proj.OriginalReleaseMonth = _gm.GameMonth;
        proj.BrandPower = (proj.Sales / 100000f + proj.FinalScore / 100f) / 2f;

        CompletedProjects.Add(proj);

        // 发售时立即检查成就
        Services.AchievementManager.OnGameReleased();

        // 教程通知
        _gm.GetNodeOrNull<TutorialManager>("TutorialManager")?.NotifyAction("game_released");

        _gm.ShowPopup($"🚀 《{proj.Name}》发售！",
            $"评分:{proj.FinalScore:F0}  首月销量:{proj.Sales:N0}\n{GetEffectSummary(proj.PostRelease)}",
            new Color(0.3f, 0.7f, 0.5f));
    }

    /// <summary>获取后发内容类型的解锁条件说明</summary>
    public static string GetPostReleaseUnlockReq(PostReleaseType type, GameProject baseProj)
    {
        if (baseProj == null || !baseProj.IsReleased) return "游戏未发售";
        return type switch
        {
            PostReleaseType.BugFixPatch => (baseProj.StabilityScore < 70 || baseProj.BugCount > 10) ? "⚠ 需要修复" : "✓ 可用（稳定性不佳时效果更好）",
            PostReleaseType.ContentUpdate => "✓ 始终可用",
            PostReleaseType.SkinDLC => baseProj.FinalScore >= 50 ? "✓ 可用" : $"需评分≥50（当前{baseProj.FinalScore:F0}）",
            PostReleaseType.Expansion => baseProj.FinalScore >= 65 ? "✓ 可用" : $"需原游戏评分≥65（当前{baseProj.FinalScore:F0}）",
            PostReleaseType.Remaster => baseProj.FinalScore >= 70 ? "✓ 可用" : $"需原游戏评分≥70（当前{baseProj.FinalScore:F0}）",
            PostReleaseType.Port => "✓ 可用（需拥有目标平台授权）",
            PostReleaseType.Sequel => baseProj.FinalScore >= 70 && baseProj.Sales >= 50000
                ? "✓ 可用" : $"需评分≥70且销量≥5万（当前{baseProj.FinalScore:F0}分/{baseProj.Sales:N0}套）",
            PostReleaseType.ModKit => "✓ 可用（固定费用¥50,000）",
            _ => "?"
        };
    }

    /// <summary>检查后发内容是否满足解锁条件</summary>
    public static bool CanCreatePostRelease(PostReleaseType type, GameProject baseProj)
    {
        if (baseProj == null || !baseProj.IsReleased) return false;
        return type switch
        {
            PostReleaseType.BugFixPatch => true,
            PostReleaseType.ContentUpdate => true,
            PostReleaseType.SkinDLC => baseProj.FinalScore >= 50,
            PostReleaseType.Expansion => baseProj.FinalScore >= 65,
            PostReleaseType.Remaster => baseProj.FinalScore >= 70,
            PostReleaseType.Port => true,
            PostReleaseType.Sequel => baseProj.FinalScore >= 70 && baseProj.Sales >= 50000,
            PostReleaseType.ModKit => true,
            _ => false
        };
    }

    // ══════════════════ 深层设计 + QA测试 ══════════════════

    /// <summary>统计各模块质量得分（0~1归一化）</summary>
    public Dictionary<string, float> GetDesignScores(GameProject proj)
    {
        var scores = new Dictionary<string, float> {
            ["graphics"] = Mathf.Clamp(proj.GraphicsScore / 120f, 0, 1),
            ["gameplay"] = Mathf.Clamp(proj.GameplayScore / 100f, 0, 1),
            ["audio"] = Mathf.Clamp(proj.AudioScore / 80f, 0, 1),
            ["story"] = Mathf.Clamp(proj.StoryScore / 80f, 0, 1),
            ["stability"] = Mathf.Clamp(proj.StabilityScore / 100f, 0, 1),
            ["ai_design"] = Mathf.Clamp(proj.ModuleProgressOnline / 1f, 0.2f, 1f), // AI/关卡设计水平
        };
        return scores;
    }

    /// <summary>执行QA测试（开发完成后、发售前）返回测试报告摘要</summary>
    public string RunQATest(GameProject proj)
    {
        var scores = GetDesignScores(proj);
        var report = new System.Text.StringBuilder();
        report.AppendLine($"QA测试报告：《{proj.Name}》");
        report.AppendLine(new string('-', 30));

        float stabilityScore = scores["stability"];
        int foundBugs = Random.Shared.Next(3, 15);
        if (proj.BugCount > 50) foundBugs += proj.BugCount / 5;
        proj.BugCount = Mathf.Max(0, proj.BugCount - foundBugs / 2);

        report.AppendLine($"发现BUG: {foundBugs}个（已修复{foundBugs/2}个）");
        report.AppendLine($"稳定性: {stabilityScore:P0}");

        // 设计维度评价
        string[] aspects = { "图形质量", "玩法设计", "音效水准", "剧情叙事", "AI/关卡设计" };
        string[] keys = { "graphics", "gameplay", "audio", "story", "ai_design" };
        for (int i = 0; i < aspects.Length; i++)
        {
            float s = scores[keys[i]];
            string grade = s > 0.75f ? "★★★★★" : s > 0.55f ? "★★★★" : s > 0.35f ? "★★★" : s > 0.2f ? "★★" : "★";
            report.AppendLine($"{aspects[i]}: {grade} ({s:P0})");
        }

        // 短板警告
        var (weakest, wScore) = scores.OrderBy(kv => kv.Value).First();
        if (wScore < 0.3f)
            report.AppendLine($"⚠ 警告：{weakest} 是明显短板，可能影响评分");

        proj.QATestReport = report.ToString();
        proj.Phase = DevPhase.ReadyToRelease;

        // 找免费QA团队
        if (proj.BugCount < 20) proj.StabilityScore += 3;
        return report.ToString();
    }

    // ══════════════════ 创意表达：宣传语与封面风格 ══════════════════

    private static string GenerateTagline(GameGenre genre, GameTheme theme)
    {
        var taglines = new Dictionary<(string, string), string[]>
        {
            [("RPG", "Fantasy")] = new[] { "在剑与魔法的世界书写你的传奇", "命运之轮开始转动", "黑暗将至，英雄何在？" },
            [("RPG", "SciFi")] = new[] { "跨越银河的史诗冒险", "合金之躯，人类之心", "在群星之间寻找答案" },
            [("RPG", "PostApoc")] = new[] { "废墟之上，重建希望", "末日之后，新的秩序", "幸存不是终点，是起点" },
            [("ACT", "Fantasy")] = new[] { "每一剑都是史诗", "超越极限的战斗", "唯有最强才能存活" },
            [("FPS", "War")] = new[] { "战火纷飞，英雄无畏", "每一颗子弹都是正义", "前线召唤，使命必达" },
            [("FPS", "Modern")] = new[] { "枪林弹雨中定义传奇", "扣动扳机，改变世界", "没有回头路" },
            [("HOR", "Horror")] = new[] { "黑暗中，有人在看着你", "别回头", "恐惧只是开始" },
            [("SIM", "Modern")] = new[] { "经营属于你的人生", "从零开始，建立帝国", "每一个决定都重要" },
            [("SLG", "War")] = new[] { "运筹帷幄，决胜千里", "一兵一卒皆是关键", "战争没有如果" },
            [("AVG", "Mystery")] = new[] { "真相只有一个", "解开层层迷雾", "每一个选择都会改变结局" },
            [("RAC", "Modern")] = new[] { "速度与激情，永不停歇", "弯道超车，直道加速", "冠军只有一个" },
            [("MOBA", "Fantasy")] = new[] { "团队至上，荣誉永恒", "五人的力量可以撼动天地", "你的英雄，你的传奇" },
            [("SAN", "Space")] = new[] { "浩瀚宇宙，无限可能", "探索未知，触碰星辰", "人类的终极边疆" },
            [("ROG", "Fantasy")] = new[] { "每次冒险都是全新的故事", "死亡不是结束", "无限可能性在等待" },
            [("VIS", "Romance")] = new[] { "一段跨越命运的爱情", "在数字世界里寻找真心", "爱能改变一切" },
            [("MUS", "Comedy")] = new[] { "跟着节奏，释放自我", "音乐就是答案", "让旋律带你飞翔" },
        };

        string g = genre.ToString(), t = theme.ToString();
        if (taglines.TryGetValue((g, t), out var arr))
            return arr[Random.Shared.Next(arr.Length)];

        // 通用回退
        string[] generic = { "全新的冒险等待着你", "改变游戏规则的力作", "从未有过的体验", "重新定义游戏边界" };
        return generic[Random.Shared.Next(generic.Length)];
    }

    private static string GenerateCoverStyle(GameGenre genre, GameTheme theme, float scale)
    {
        string[] styleBases = { "写实主义", "手绘水彩", "像素复古", "赛博朋克霓虹", "极简几何", "暗黑哥特", "梦幻柔光", "油画质感", "剪纸艺术", "机械蓝图" };
        string baseStyle = styleBases[Random.Shared.Next(styleBases.Length)];

        string[] accents = { "金色边框镶边", "光影对比强烈", "角色剪影构图", "爆炸元素点缀", "渐变星空背景", "碎片化拼贴", "水墨渲染", "几何图案装饰" };
        string accent = accents[Random.Shared.Next(accents.Length)];

        string scaleDesc = scale > 0.7f ? "史诗级大制作风格" : scale > 0.4f ? "精致独立游戏风格" : "极简小品风格";
        return $"{baseStyle} + {accent}，{scaleDesc}";
    }

    // ══════════════════ 发行商系统（主动经营+谈判+品牌积累）══════════════════

    /// <summary>接受发行合约（使用谈判结果）</summary>
    public bool AcceptPublishingDeal(PublishedDeal deal)
    {
        float finalMarketing = deal.PlayerOfferMarketing > 0 ? deal.PlayerOfferMarketing : deal.MarketingCost;
        float finalRoyalty = deal.PlayerOfferRoyalty > 0 ? deal.PlayerOfferRoyalty : deal.RoyaltyRate;

        if (!_res.SpendMoney((long)finalMarketing, "publishing"))
            return false;

        var proj = new PublishedProject
        {
            GameName = deal.GameName,
            StudioName = deal.StudioName,
            Genre = deal.Genre,
            Theme = deal.Theme,
            ExpectedScore = deal.ExpectedScore,
            MarketingCost = finalMarketing,
            RoyaltyRate = finalRoyalty,
            ReleaseMonth = _gm.GameMonth + deal.EstReleaseMonths,
            IsPublished = true
        };
        PublishedProjects.Add(proj);
        PublishedGameCount++;

        // 谈判过的合约影响声誉双向
        if (deal.IsNegotiating)
            PublisherReputation = Mathf.Clamp(PublisherReputation + deal.StudioSatisfaction * 0.04f - 0.02f, 0, 1);

        _gm.ShowToast("签约发行", $"已签约发行《{deal.GameName}》\n预计{deal.EstReleaseMonths}个月后发售，分成{finalRoyalty:P0}", new Color(0.3f, 0.8f, 0.5f));
        return true;
    }

    /// <summary>主动向AI工作室发起发行邀约</summary>
    public bool ProposeDealToStudio(AIStudio studio)
    {
        if (CompletedProjects.Count < 2) return false;
        IsPublisher = true;

        int cost = 5000 + (int)(PublisherReputation * 20000);
        if (!_res.SpendMoney(cost, "publishing_propose")) return false;

        var genres = Enum.GetValues<GameGenre>();
        var themes = Enum.GetValues<GameTheme>();
        var g = genres[Random.Shared.Next(genres.Length)];
        var t = themes[Random.Shared.Next(themes.Length)];

        // AI工作室响应概率 = 玩家声誉 × 工作室资金状况 × 玩家发行成绩
        float responseChance = PublisherReputation * 0.6f + Mathf.Clamp(studio.Money / 5000000f, 0, 1) * 0.2f
            + (PublishedGameCount > 0 ? Mathf.Clamp(PublishedGameCount / 10f, 0, 0.2f) : 0f);

        if (Random.Shared.NextDouble() > responseChance)
        {
            _gm.ShowToast("合作被拒", $"{studio.Name}拒绝了您的发行邀约\n（声誉{PublisherReputation:P0}，对方信心不足）", new Color(0.8f, 0.4f, 0.3f));
            PublisherReputation = Mathf.Max(0, PublisherReputation - 0.03f);
            return false;
        }

        float repBonus = Mathf.Max(0.3f, PublisherReputation + 0.3f);
        var deal = new PublishedDeal
        {
            StudioName = studio.Name,
            GameName = GeneratePublishedGameName(g, t),
            Genre = g, Theme = t,
            ExpectedScore = 40 + Random.Shared.Next(40),
            MarketingCost = 20000 + Random.Shared.Next(120000),
            RoyaltyRate = 0.15f + (float)Random.Shared.NextDouble() * 0.25f * repBonus,
            EstReleaseMonths = 3 + Random.Shared.Next(9),
            MonthsRemaining = 6,
            DealMonth = _gm.GameMonth,
            IsNegotiating = true,
            IsPlayerOffer = true,
            StudioSatisfaction = 0.7f
        };
        deal.PlayerOfferRoyalty = deal.RoyaltyRate;
        deal.PlayerOfferMarketing = deal.MarketingCost;
        AvailableDeals.Add(deal);

        _gm.ShowToast("合作成功", $"{studio.Name}同意发行合作！\n进入谈判阶段…", new Color(0.3f, 0.7f, 0.5f));
        PublisherReputation = Mathf.Min(1f, PublisherReputation + 0.02f);
        return true;
    }

    /// <summary>谈判：调整出价并计算对方满意度</summary>
    public float NegotiateDeal(PublishedDeal deal, float newRoyaltyRate, float newMarketingCost)
    {
        deal.PlayerOfferRoyalty = Mathf.Clamp(newRoyaltyRate, 0.10f, 0.50f);
        deal.PlayerOfferMarketing = Mathf.Max(deal.MarketingCost * 0.5f, Mathf.Min(deal.MarketingCost * 2f, newMarketingCost));
        deal.IsNegotiating = true;

        // 满意度公式：对方希望高分成+高宣发
        float royaltySat = (deal.PlayerOfferRoyalty - deal.RoyaltyRate + 0.1f) / 0.2f; // 比初始提议高则满意
        float marketingSat = (deal.PlayerOfferMarketing - deal.MarketingCost * 0.8f) / (deal.MarketingCost * 1.2f);

        // 玩家声誉影响谈判空间
        float repBonus = PublisherReputation * 0.15f;
        deal.StudioSatisfaction = Mathf.Clamp(0.5f + royaltySat * 0.3f + marketingSat * 0.2f + repBonus, 0f, 1f);

        return deal.StudioSatisfaction;
    }

    /// <summary>每月处理发行项目</summary>
    public void ProcessPublishingMonthly()
    {
        // 淘汰过期被动邀约
        for (int i = AvailableDeals.Count - 1; i >= 0; i--)
        {
            var d = AvailableDeals[i];
            if (!d.IsPlayerOffer && !d.IsNegotiating)
            {
                d.MonthsRemaining--;
                if (d.MonthsRemaining <= 0) AvailableDeals.RemoveAt(i);
            }
        }

        for (int i = PublishedProjects.Count - 1; i >= 0; i--)
        {
            var p = PublishedProjects[i];
            if (!p.IsReleased)
            {
                if (_gm.GameMonth >= p.ReleaseMonth)
                {
                    float variation = (float)(Random.Shared.NextDouble() - 0.45) * 20f;
                    p.ActualScore = Mathf.Clamp(p.ExpectedScore + variation + PublisherReputation * 5f, 20, 98);
                    p.Sales = (int)(5000 + p.ActualScore * 200 + p.MarketingCost / 30);
                    p.IsReleased = true; p.MonthsOnMarket = 0;

                    float firstMonthRoyalty = p.Sales * 50f * p.RoyaltyRate;
                    p.TotalRoyaltyEarned += firstMonthRoyalty;
                    p.MonthlyRoyaltyHistory.Add(firstMonthRoyalty);
                    _res.EarnMoney(firstMonthRoyalty, "publishing_royalty");

                    // 声誉变动：高评分涨，低评分跌，暴死惩罚大
                    if (p.ActualScore >= 80) PublisherReputation = Mathf.Min(1f, PublisherReputation + 0.05f);
                    else if (p.ActualScore >= 65) PublisherReputation = Mathf.Min(1f, PublisherReputation + 0.02f);
                    else if (p.ActualScore >= 50) PublisherReputation = Mathf.Max(0, PublisherReputation - 0.02f);
                    else PublisherReputation = Mathf.Max(0, PublisherReputation - 0.06f);

                    string grade = p.ActualScore >= 80 ? "🔥" : p.ActualScore >= 65 ? "👍" : p.ActualScore >= 50 ? "😐" : "👎";
                    _gm.ShowPopup("发行游戏发售", $"《{p.GameName}》({p.StudioName})正式发售！\n评分:{p.ActualScore:F0} {grade}  销量:{p.Sales:N0}\n首月版税:¥{firstMonthRoyalty:N0}", new Color(0.3f, 0.7f, 1f));

                    // 发行标签积累
                    var existingLabel = Labels.Find(l => l.Name == "发行品牌");
                    if (existingLabel == null && PublishedGameCount >= 3)
                    {
                        Labels.Add(new PublishingLabel { Name = "发行品牌", FoundedMonth = _gm.GameMonth, Reputation = PublisherReputation });
                    }
                    if (existingLabel != null) { existingLabel.GameCount++; existingLabel.TotalScore += p.ActualScore; existingLabel.Reputation = PublisherReputation; }
                }
            }
            else
            {
                p.MonthsOnMarket++;
                float decayRate = p.MonthsOnMarket <= 6 ? 0.8f : p.MonthsOnMarket <= 12 ? 0.5f : p.MonthsOnMarket <= 18 ? 0.3f : 0.1f;
                int monthlySales = (int)(p.Sales * 0.08f * decayRate);
                float monthlyRoyalty = monthlySales * 50f * p.RoyaltyRate;
                if (monthlyRoyalty < 500) monthlyRoyalty = 500;
                p.TotalRoyaltyEarned += monthlyRoyalty;
                p.MonthlyRoyaltyHistory.Add(monthlyRoyalty);
                _res.EarnMoney(monthlyRoyalty, "publishing_royalty");
                if (p.MonthsOnMarket > 24) PublishedProjects.RemoveAt(i);
            }
        }

        _dealRefreshCounter++;
        if (_dealRefreshCounter >= 6)
        {
            _dealRefreshCounter = 0;
            // 被动邀约上限3个
            if (AvailableDeals.Count(d => !d.IsPlayerOffer) < 3)
                RefreshPublishingDeals();
        }
    }

    private void RefreshPublishingDeals()
    {
        var compAI = _gm.GetNodeOrNull<CompetitorAI>("CompetitorAI");
        if (compAI == null) return;
        var studios = compAI.Studios.Where(s => !s.IsAcquired && s.Releases.Count >= 1).ToList();
        if (studios.Count == 0 || CompletedProjects.Count < 2) return;
        IsPublisher = true;

        int existing = AvailableDeals.Count(d => !d.IsPlayerOffer);
        int needed = Mathf.Min(3 - existing, studios.Count);
        for (int i = 0; i < needed; i++)
        {
            var studio = studios[Random.Shared.Next(studios.Count)];
            var genres = Enum.GetValues<GameGenre>();
            var themes = Enum.GetValues<GameTheme>();
            var g = genres[Random.Shared.Next(genres.Length)];
            var t = themes[Random.Shared.Next(themes.Length)];
            float repBonus = Mathf.Max(0.3f, PublisherReputation + 0.3f);

            AvailableDeals.Add(new PublishedDeal
            {
                StudioName = studio.Name, GameName = GeneratePublishedGameName(g, t),
                Genre = g, Theme = t,
                ExpectedScore = 45 + Random.Shared.Next(35),
                MarketingCost = 30000 + Random.Shared.Next(150000),
                RoyaltyRate = 0.15f + (float)Random.Shared.NextDouble() * 0.25f * repBonus,
                EstReleaseMonths = 3 + Random.Shared.Next(9), MonthsRemaining = 6,
                DealMonth = _gm.GameMonth
            });
            studios.Remove(studio);
        }
    }

    // ══════════════════ 局部重构 ══════════════════

    public bool PartialRefactor(GameProject proj)
    {
        if (proj.TechDebt < 20) return false;
        // 消耗1个月进度
        float monthsCost = 1f + proj.TechDebt * 0.015f;
        proj.DevProgress = Mathf.Max(0.05f, proj.DevProgress - 0.08f);
        proj.MonthsSpent += monthsCost;
        proj.EstimatedMonths += monthsCost;
        // 债务减半
        float oldDebt = proj.TechDebt;
        proj.TechDebt = Mathf.Max(5, proj.TechDebt * 0.5f);
        proj.DevLog.Add(Loc.TrF("dev.refactor_done", oldDebt, proj.TechDebt, monthsCost));
        // 重置利息
        proj.DebtInterestRate = proj.TechDebt > 30 ? Mathf.Clamp(proj.TechDebt * 0.0015f, 0, 0.1f) : 0;
        proj.NextMonthBugFromDebt = (int)(proj.TechDebt * proj.DebtInterestRate * 0.3f * 100);
        return true;
    }

    // ══════════════════ 宣发冲刺期 ══════════════════

    private void ShowMarketingAction(GameProject proj)
    {
        ModAPI.FireHooks(ModAPI.GameHook.BeforeMarketing);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeMarketing)) return;

        if (proj.MarketingSprintSpent >= proj.MarketingSprintMonths)
        {
            // 宣发期结束，恢复开发
            proj.Phase = DevPhase.Developing;
            proj.DevProgress = 0.9f; // 从90%继续
            proj.DevLog.Add(Loc.TrF("dev.marketing_done", proj.MarketingHype));
            _gm.Paused = false;
            return;
        }

        int remaining = proj.MarketingSprintMonths - proj.MarketingSprintSpent;
        string title = Loc.TrF("dev.marketing_title", remaining);
        string msg = Loc.TrF("dev.marketing_status", proj.Name, proj.MarketingSprintSpent + 1, proj.MarketingSprintMonths, proj.MarketingHype);

        _gm.ShowTriChoicePopup(title, msg,
            Loc.TrF("dev.marketing_ad_cost", 8),
            Loc.TrF("dev.marketing_con_cost", 12),
            Loc.TrF("dev.marketing_demo_cost", 5),
            () => ApplyMarketingAction(proj, "ad", 80000),
            () => ApplyMarketingAction(proj, "con", 120000),
            () => ApplyMarketingAction(proj, "demo", 50000),
            new Color(0.2f, 0.6f, 0.9f));
    }

    private void ApplyMarketingAction(GameProject proj, string action, long cost)
    {
        // 月度必须先推进，防止资金不足时死循环
        proj.MarketingSprintSpent++;
        proj.MonthsSpent++;

        bool paid = false;
        if (cost > 0)
        {
            if (_res.Money < cost)
            {
                // 资金不足 → 本月白费，期待值小幅流失
                proj.MarketingHype = Mathf.Max(0, proj.MarketingHype - 3);
                proj.DevLog.Add(Loc.TrF("dev.marketing_broke", cost));
                _gm.ShowToast(Loc.Tr("ui.insufficient_funds"), Loc.TrF("dev.marketing_skip_month", proj.MarketingSprintSpent, proj.MarketingSprintMonths), new Color(0.9f, 0.5f, 0.2f));
                ShowMarketingAction(proj);
                return;
            }
            _res.SpendMoney(cost, "marketing");
            paid = true;
        }

        float hype = action switch
        {
            "ad" => 10 + Random.Shared.Next(15),
            "con" => 15 + Random.Shared.Next(20),
            "demo" => 8 + Random.Shared.Next(12),
            _ => 5
        };

        // 受资金投入影响
        if (paid) hype *= 1 + Mathf.Min((float)cost / 200000f, 0.5f);

        // 随机事件偶尔翻倍
        if (Random.Shared.NextDouble() < 0.1f) { hype *= 2; proj.DevLog.Add(Loc.Tr("dev.marketing_viral")); }

        proj.MarketingHype += hype;
        ModAPI.FireHooks(ModAPI.GameHook.AfterMarketing);
        ShowMarketingAction(proj);
    }

    // ══════════════════ 红海/蓝海机制 ══════════════════

    /// <summary>统计某个题材组合在玩家已完成项目中的使用次数（不含AI历史，避免信息欺诈）</summary>
    public int CountGenreThemeUses(GameGenre genre, GameTheme theme)
    {
        int count = 0;
        foreach (var p in CompletedProjects.Where(p => p.IsReleased))
        {
            if (p.Genre == genre && p.Theme == theme) count++;
        }
        return count;
    }

    /// <summary>红海惩罚：玩家同题材使用过多，市场饱和销量衰减（0.5~1.0）</summary>
    public float GetRedOceanPenalty(GameGenre genre, GameTheme theme)
    {
        int uses = CountGenreThemeUses(genre, theme);
        if (uses <= 2) return 1.0f;
        if (uses <= 4) return 0.85f;
        if (uses <= 6) return 0.70f;
        return 0.5f;
    }

    /// <summary>蓝海奖励：玩家首次使用的题材组合开拓者加成 (1.0~1.5)</summary>
    public float GetBlueOceanBonus(GameGenre genre, GameTheme theme)
    {
        int uses = CountGenreThemeUses(genre, theme);
        if (uses >= 3) return 1.0f;
        if (uses == 2) return 1.1f;
        if (uses == 1) return 1.25f;
        // 玩家从未用过 → 真正蓝海
        return 1.5f;
    }

    /// <summary>蓝海开拓者粉丝暴增</summary>
    public int GetNoveltyFanBoost(GameGenre genre, GameTheme theme)
    {
        int uses = CountGenreThemeUses(genre, theme);
        if (uses > 0) return 0;
        return 2000 + Random.Shared.Next(5000); // 首次使用的组合额外粉丝
    }

    public bool ShouldShowNoveltyIndicator(GameGenre genre, GameTheme theme)
    {
        return CountGenreThemeUses(genre, theme) == 0;
    }

    private static string GeneratePublishedGameName(GameGenre g, GameTheme t)
    {
        string[] prefixes = { "幻", "星", "暗", "龙", "光", "影", "铁", "炎", "冰", "风", "虚空", "极限", "传说", "永恒", "深渊" };
        string[] suffixes = { "之旅", "传奇", "战争", "传说", "纪元", "编年史", "觉醒", "起源", "回归", "远征", "征程", "之刃", "之心", "契约" };
        return $"{prefixes[Random.Shared.Next(prefixes.Length)]}之{suffixes[Random.Shared.Next(suffixes.Length)]}";
    }
}
