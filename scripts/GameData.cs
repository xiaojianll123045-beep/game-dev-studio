using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

// ==================== 引擎特性 ====================
public enum EnginePerk
{
    BlazingFast,        // 开发速度+30%，但技术债务增速翻倍
    Lightweight,        // 适合独立游戏，资源占用低
    GraphicalPowerhouse,// 画面评分上限+15，开发速度-20%
    StylizedMaster,     // 美术风格类游戏评分+10
    RockSolid,          // BUG生成率-40%，新功能开发速度-15%
    BattleTested,       // 技术债务增速减半
    NoobFriendly,       // 新员工上手速度+50%
    ModSupport,         // 社区活跃度+20%，粉丝转化率+10%
    NetworkPro,         // 联机游戏开发速度+40%
    CloudNative,        // 在线服务稳定性+30%
    LegacyCodebase,     // 开局自带大量功能，债务初始+30
    Experimental,       // 新技术集成速度+50%，BUG率翻倍
    IndieChampion,      // 小型项目开发速度+60%
}

// ==================== 引擎定位 ====================
public enum EnginePosition
{
    General,    // 通用型
    Graphical,  // 画面型
    Performance,// 性能型
    Network,    // 网络型
    Stable      // 稳定型
}

/// <summary>
/// 独立游戏引擎
/// </summary>
public partial class GameEngine
{
    public string Name { get; set; }
    public int Generation { get; set; } = 1;
    public string VersionTag { get; set; } = "1.0";
    public float TechDebt { get; set; }
    public List<string> AppliedTechs { get; set; } = new();
    public Dictionary<string, int> Capabilities { get; set; } = new(); // { "2d":3, "3d":0, "net":1, "ai":0, "audio":2 }

    // 品质
    public float Stability { get; set; } = 80f;       // 0~100，影响 BUG 生成率
    public float Performance { get; set; } = 70f;     // 0~100，运行效率
    public float DevEfficiency { get; set; } = 50f;   // 0~100，开发速度加成%

    // 特性
    public List<EnginePerk> Perks { get; set; } = new();
    public EnginePosition Position { get; set; } = EnginePosition.General;

    // 商业化
    public EngineBizModel BizModel { get; set; } = EngineBizModel.Closed;
    public float BuyoutPrice { get; set; } = 500000f;
    public float SubscriptionPrice { get; set; } = 20000f;
    public float RoyaltyRate { get; set; } = 0.1f;
    public int LicenseCount { get; set; }
    public float MarketShare { get; set; }
    public float Reputation { get; set; } = 10f;
    public float MonthlyRevenue { get; set; }
    public float TotalRevenue { get; set; }
    public bool IsDeprecated { get; set; }
    public bool IsDeveloping { get; set; }
    public int DevMonthsRemaining { get; set; }
    public Team DevTeam { get; set; }
    public string DevPhaseName { get; set; }

    public float QualityScore
    {
        get
        {
            float q = AppliedTechs.Count * 3f + Reputation * 0.5f - TechDebt * 0.3f;
            return Mathf.Max(1, q);
        }
    }

    /// <summary>自动推导引擎特性（基于集成的科技组合）</summary>
    public void DerivePerks()
    {
        Perks.Clear();
        int v2d = Capabilities.TryGetValue("2d", out var c) ? c : 0;
        int v3d = Capabilities.TryGetValue("3d", out var c2) ? c2 : 0;
        int net = Capabilities.TryGetValue("net", out var c3) ? c3 : 0;
        int ai = Capabilities.TryGetValue("ai", out var c4) ? c4 : 0;
        int audio = Capabilities.TryGetValue("audio", out var c5) ? c5 : 0;

        if (v2d >= 3 && v3d >= 2) Perks.Add(EnginePerk.GraphicalPowerhouse);
        if (v2d >= 2 && v3d == 0) Perks.Add(EnginePerk.StylizedMaster);
        if (net >= 2) Perks.Add(EnginePerk.NetworkPro);
        if (net >= 1 && ai >= 1) Perks.Add(EnginePerk.CloudNative);
        if (v2d >= 1 && v3d == 0 && net == 0 && ai == 0) Perks.Add(EnginePerk.Lightweight);
        if (ai >= 2) Perks.Add(EnginePerk.Experimental);

        switch (Position)
        {
            case EnginePosition.Performance: Perks.Add(EnginePerk.BlazingFast); break;
            case EnginePosition.Stable: Perks.Add(EnginePerk.RockSolid); break;
            case EnginePosition.General: Perks.Add(EnginePerk.NoobFriendly); break;
        }

        if (Perks.Count == 0) Perks.Add(EnginePerk.NoobFriendly);
    }

    /// <summary>根据集成科技更新 Capabilities</summary>
    public void UpdateCapabilities()
    {
        Capabilities = new() { ["2d"] = 0, ["3d"] = 0, ["net"] = 0, ["ai"] = 0, ["audio"] = 0 };
        foreach (var tid in AppliedTechs)
        {
            if (tid.StartsWith("2d_")) Capabilities["2d"] = Mathf.Max(Capabilities["2d"], int.Parse(tid.Replace("2d_v", "")));
            else if (tid.StartsWith("3d_")) Capabilities["3d"] = Mathf.Max(Capabilities["3d"], int.Parse(tid.Replace("3d_v", "")));
            else if (tid.StartsWith("net_")) Capabilities["net"] = Mathf.Max(Capabilities["net"], int.Parse(tid.Replace("net_v", "")));
            else if (tid.StartsWith("ai_")) Capabilities["ai"] = Mathf.Max(Capabilities["ai"], int.Parse(tid.Replace("ai_v", "")));
            else if (tid.StartsWith("audio_")) Capabilities["audio"] = Mathf.Max(Capabilities["audio"], int.Parse(tid.Replace("audio_v", "")));
        }
    }
}

public enum SkillType
{
    Program,    // 程序
    Art,        // 美术
    Audio,      // 音频
    Network,    // 网络
    AI          // AI
}

// ==================== 员工特质 ====================
public enum EmployeeTrait
{
    None,
    Workaholic,     // 工作狂
    Social,         // 社交达人
    Sensitive,      // 玻璃心
    Genius,         // 天才
    Mentor,         // 导师
    LoneWolf,       // 独行侠
    Perfectionist,  // 完美主义者
    Chill,          // 佛系
    Ambitious,      // 野心家
    Nostalgic,      // 恋旧
    TechClean,      // 技术洁癖
    Lucky           // 幸运星
}

// ==================== 设计哲学 ====================
public enum DesignPhilosophy
{
    Balanced,       // 平衡（默认）
    Innovative,     // 创新驱动：玩法权重↑、高方差
    Polished,       // 精雕细琢：画面权重↑、低波动
    Niche           // 小众蓝海：低期待值容忍度、高上限
}

// ==================== AI 竞争策略 ====================
public enum AIStrategy
{
    Balanced,       // 均衡
    Aggressive,      // 狙击者：检测玩家立项，提前发售撞档
    Copycat,         // 抄袭者：玩家高分游戏→同类型跟风
    NicheHunter      // 蓝海猎手：专攻冷门题材组合
}

// ==================== 游戏类型 ====================
public enum GameGenre
{
    RPG,        // 角色扮演
    ACT,        // 动作
    AVG,        // 冒险
    SLG,        // 策略
    FPS,        // 射击
    RAC,        // 竞速
    SIM,        // 模拟
    SPO,        // 体育
    MUS,        // 音乐
    FTG,        // 格斗
    MOBA,       // 多人在线竞技
    MMO,        // 大型多人在线
    RTS,        // 即时战略
    HOR,        // 恐怖
    SAN,        // 沙盒
    ROG,        // Roguelike
    VIS,        // 视觉小说
    PZL,        // 解谜
    ETC,        // 派对游戏
    TOW,        // 塔防
    SUR,        // 生存
    MOV         // 互动电影
}

// ==================== 游戏主题 ====================
public enum GameTheme
{
    Fantasy,        // 奇幻
    SciFi,          // 科幻
    Modern,         // 现代
    Historical,     // 历史
    PostApoc,       // 末日
    Cyberpunk,      // 赛博朋克
    Steampunk,      // 蒸汽朋克
    Horror,         // 恐怖
    Comedy,         // 喜剧
    Romance,        // 恋爱
    War,            // 战争
    Mystery,        // 悬疑
    School,         // 校园
    Myth,           // 神话
    Western,        // 西部
    Space,          // 太空
    Ninja,          // 忍者
    Pirate,         // 海盗
    Victorian,      // 维多利亚
    DeepSea,        // 深海
    Dungeon,        // 地牢
    Workplace       // 职场
}

// ==================== 平台 ====================
public enum Platform
{
    PC,         // PC（免费）
    Console,    // 主机（需授权）
    Mobile,     // 手机
    All         // 全平台（需跨平台科技）
}

/// <summary>平台数据</summary>
public static class PlatformData
{
    public static float AuthFee(this Platform p) => p switch
    {
        Platform.PC => 0,
        Platform.Console => 200000,
        Platform.Mobile => 100000,
        Platform.All => 300000,
        _ => 0
    };
    public static float DevDifficulty(this Platform p) => p switch
    {
        Platform.PC => 1.0f,
        Platform.Console => 1.2f,
        Platform.Mobile => 0.8f,
        Platform.All => 1.5f,
        _ => 1.0f
    };
    public static float SalesBase(this Platform p) => p switch
    {
        Platform.PC => 1.0f,
        Platform.Console => 1.5f,
        Platform.Mobile => 1.8f,
        Platform.All => 2.5f,
        _ => 1.0f
    };
}

/// <summary>打磨策略</summary>
public enum PolishStrategy { Standard, Deep, Extreme }

/// <summary>续作策略</summary>
public enum SequelStrategy
{
    Cautious,       // 稳扎稳打(换皮)：时间-30%，品质上限锁定前作-5
    Revolutionary,  // 创新突破(大改)：时间+50%，品质无上限但有±15随机浮动
    Derivative      // 衍生作品：用同一个世界观做不同类型
}

/// <summary>游戏组件类别</summary>
public enum ComponentCategory { Visual, Audio, Gameplay, Story, Stability, Online }

/// <summary>可装配游戏组件</summary>
public struct GameComponent
{
    public string Id { get; set; }
    public string Name { get; set; }
    public ComponentCategory Category { get; set; }
    public string RequiredTech { get; set; }     // 需要解锁的科技ID，空=默认可用
    public string[] SynergyTags { get; set; }    // 联动标签，同标签组件装备多个时触发加成
    public string Description { get; set; }
    /// <summary>对属性的加成（Graphics/Gameplay/Audio/Story/Network/Stability）</summary>
    public (string attr, float bonus)[] Effects { get; set; }
}

/// <summary>全量游戏组件池 — 由科技解锁</summary>
public static class GameComponentDB
{
    public static readonly GameComponent[] All = {
        // ── 视觉效果类 ──
        new() { Id="dynamic_light", Name="动态光照", Category=ComponentCategory.Visual, RequiredTech="3d_v1", SynergyTags=new[]{"lighting","3d"}, Description="实时光影效果，画面+10", Effects=new[] { ("graphics", 10f) } },
        new() { Id="ray_trace", Name="光线追踪", Category=ComponentCategory.Visual, RequiredTech="3d_v3", SynergyTags=new[]{"lighting","premium"}, Description="顶级光影，画面+25但需强力引擎", Effects=new[] { ("graphics", 25f) } },
        new() { Id="cell_shade", Name="卡通渲染", Category=ComponentCategory.Visual, RequiredTech="2d_v2", SynergyTags=new[]{"style","2d"}, Description="独特美术风格，画面+8，趣味+5", Effects=new[] { ("graphics", 8f), ("gameplay", 5f) } },
        new() { Id="pixel_art", Name="像素美学", Category=ComponentCategory.Visual, RequiredTech="", SynergyTags=new[]{"retro","style","2d"}, Description="复古像素风，画面-5但核心玩法+8", Effects=new[] { ("graphics", -5f), ("gameplay", 8f) } },
        new() { Id="destruction", Name="可破坏场景", Category=ComponentCategory.Visual, RequiredTech="3d_v2", SynergyTags=new[]{"physics","action"}, Description="场景可破坏，画面+12 稳定性-5", Effects=new[] { ("graphics", 12f), ("stability", -5f) } },
        new() { Id="weather", Name="天气系统", Category=ComponentCategory.Visual, RequiredTech="", SynergyTags=new[]{"dynamic","atmosphere"}, Description="动态天气，画面+5，沉浸感+5", Effects=new[] { ("graphics", 5f), ("story", 5f) } },

        // ── 音频类 ──
        new() { Id="dynamic_music", Name="动态配乐", Category=ComponentCategory.Audio, RequiredTech="dynamic_music", SynergyTags=new[]{"dynamic","atmosphere","premium"}, Description="情境感知配乐，音频+15", Effects=new[] { ("audio", 15f) } },
        new() { Id="spatial_sound", Name="空间音频", Category=ComponentCategory.Audio, RequiredTech="spatial_audio", SynergyTags=new[]{"3d","immersion"}, Description="3D定位音效，音频+12", Effects=new[] { ("audio", 12f) } },
        new() { Id="voice_acting", Name="配音叙事", Category=ComponentCategory.Audio, RequiredTech="", SynergyTags=new[]{"story","premium"}, Description="全语音，剧情+10 音频+5", Effects=new[] { ("audio", 5f), ("story", 10f) } },
        new() { Id="procedural_audio", Name="程序化音效", Category=ComponentCategory.Audio, RequiredTech="ai_v1", SynergyTags=new[]{"procedural","style"}, Description="算法生成音效，音频+6 原创性+5", Effects=new[] { ("audio", 6f), ("gameplay", 5f) } },

        // ── 玩法类 ──
        new() { Id="open_world", Name="开放世界", Category=ComponentCategory.Gameplay, RequiredTech="", SynergyTags=new[]{"dynamic","action","3d","big"}, Description="无缝大地图，趣味+20 稳定性-8", Effects=new[] { ("gameplay", 20f), ("stability", -8f) } },
        new() { Id="stealth", Name="潜行系统", Category=ComponentCategory.Gameplay, RequiredTech="", SynergyTags=new[]{"action","atmosphere"}, Description="潜行暗杀玩法，趣味+10", Effects=new[] { ("gameplay", 10f) } },
        new() { Id="skill_tree", Name="技能树", Category=ComponentCategory.Gameplay, RequiredTech="", SynergyTags=new[]{"rpg","progression"}, Description="角色成长系统，趣味+12", Effects=new[] { ("gameplay", 12f) } },
        new() { Id="crafting", Name="制造系统", Category=ComponentCategory.Gameplay, RequiredTech="", SynergyTags=new[]{"rpg","progression"}, Description="材料合成制造，趣味+8", Effects=new[] { ("gameplay", 8f) } },
        new() { Id="multiplayer", Name="多人联机", Category=ComponentCategory.Online, RequiredTech="net_v1", SynergyTags=new[]{"social","big"}, Description="在线多人，网络+15 趣味+5 稳定-10", Effects=new[] { ("network", 15f), ("gameplay", 5f), ("stability", -10f) } },
        new() { Id="roguelike_gen", Name="程序生成", Category=ComponentCategory.Gameplay, RequiredTech="ai_v2", SynergyTags=new[]{"procedural","replay"}, Description="随机关卡生成，趣味+15 稳定-5", Effects=new[] { ("gameplay", 15f), ("stability", -5f) } },
        new() { Id="physics_play", Name="物理玩法", Category=ComponentCategory.Gameplay, RequiredTech="3d_v1", SynergyTags=new[]{"physics","action"}, Description="物理引擎驱动玩法，趣味+10", Effects=new[] { ("gameplay", 10f) } },

        // ── 剧情类 ──
        new() { Id="branching_narrative", Name="分支剧情", Category=ComponentCategory.Story, RequiredTech="save_system", SynergyTags=new[]{"story","replay"}, Description="多结局分支，剧情+18", Effects=new[] { ("story", 18f) } },
        new() { Id="ai_dialogue", Name="AI生成对话", Category=ComponentCategory.Story, RequiredTech="ai_v2", SynergyTags=new[]{"procedural","story"}, Description="NPC对话由AI实时生成，剧情+12 趣味+8", Effects=new[] { ("story", 12f), ("gameplay", 8f) } },
        new() { Id="lore_system", Name="世界观典籍", Category=ComponentCategory.Story, RequiredTech="", SynergyTags=new[]{"story","rpg","immersion"}, Description="内置百科全书，剧情+8", Effects=new[] { ("story", 8f) } },
        new() { Id="moral_system", Name="道德抉择", Category=ComponentCategory.Story, RequiredTech="", SynergyTags=new[]{"story","replay","immersion"}, Description="善恶选择影响世界，剧情+10 趣味+5", Effects=new[] { ("story", 10f), ("gameplay", 5f) } },

        // ── 稳定性类 ──
        new() { Id="auto_save", Name="自动存档", Category=ComponentCategory.Stability, RequiredTech="save_system", SynergyTags=new[]{"qol","big"}, Description="防丢档，稳定+10", Effects=new[] { ("stability", 10f) } },
        new() { Id="unit_test", Name="自动化测试", Category=ComponentCategory.Stability, RequiredTech="testing_v1", SynergyTags=new[]{"qol"}, Description="减少BUG生成，稳定+15", Effects=new[] { ("stability", 15f) } },
        new() { Id="cloud_save", Name="云存档", Category=ComponentCategory.Stability, RequiredTech="net_v1", SynergyTags=new[]{"social","qol"}, Description="跨设备同步，稳定+5 网络+5", Effects=new[] { ("stability", 5f), ("network", 5f) } },

        // ── 线上类 ──
        new() { Id="leaderboards", Name="排行榜", Category=ComponentCategory.Online, RequiredTech="net_v1", SynergyTags=new[]{"social","replay"}, Description="全球排行，趣味+6", Effects=new[] { ("gameplay", 6f) } },
        new() { Id="coop", Name="合作模式", Category=ComponentCategory.Online, RequiredTech="net_v1", SynergyTags=new[]{"social","action"}, Description="双人/多人合作，网络+10 趣味+10", Effects=new[] { ("network", 10f), ("gameplay", 10f) } },
        new() { Id="live_ops", Name="在线运营", Category=ComponentCategory.Online, RequiredTech="net_v2", SynergyTags=new[]{"social","big"}, Description="定期活动更新，长尾收入+30%", Effects=new[] { ("network", 8f) } },
    };

    /// <summary>获取已解锁的组件</summary>
    public static GameComponent[] GetUnlocked(TechManager techMgr)
    {
        var all = GetAllWithMods();
        var list = new List<GameComponent>();
        foreach (var c in all)
        {
            if (string.IsNullOrEmpty(c.RequiredTech) || techMgr.IsResearched(c.RequiredTech))
                list.Add(c);
        }
        return list.ToArray();
    }

    /// <summary>检查联动：返回（所有已装备组件的联动加成）</summary>
    public static Dictionary<string, float> ComputeSynergies(GameComponent[] equipped)
    {
        var tagCounts = new Dictionary<string, int>();
        foreach (var c in equipped)
            foreach (var t in c.SynergyTags)
            {
                if (!tagCounts.ContainsKey(t)) tagCounts[t] = 0;
                tagCounts[t]++;
            }

        // 每个标签出现≥2次即触发联动，非线性衰减，上限10分
        var bonuses = new Dictionary<string, float>();
        foreach (var kv in tagCounts)
        {
            if (kv.Value >= 2)
            {
                // 2个=3分, 3个=5分, 4个=7分, 5个=8分, 6个=10→上限
                float b = kv.Value switch { 2 => 3f, 3 => 5f, 4 => 7f, 5 => 8f, _ => 10f };
                bonuses[kv.Key] = b;
            }
        }
        return bonuses;
    }

    private static readonly List<GameComponent> _modComponents = new();

    /// <summary>Mod 数据合并入口（接受 JSON 字符串，按 ID 合并/覆盖组件）</summary>
    public static void MergeFromJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var id = el.GetProperty("id").GetString();
                var comp = new GameComponent
                {
                    Id = id,
                    Name = el.GetProperty("name").GetString(),
                    Category = Enum.Parse<ComponentCategory>(el.GetProperty("category").GetString()),
                    RequiredTech = el.TryGetProperty("required_tech", out var rt) ? rt.GetString() ?? "" : "",
                    Description = el.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                    SynergyTags = el.TryGetProperty("synergy_tags", out var st) ? st.EnumerateArray().Select(x => x.GetString()).ToArray() : Array.Empty<string>(),
                    Effects = el.TryGetProperty("effects", out var eff) ? eff.EnumerateArray().Select(x => (x.GetProperty("attr").GetString(), x.GetProperty("bonus").GetSingle())).ToArray() : Array.Empty<(string, float)>(),
                };
                var existing = _modComponents.FindIndex(c => c.Id == id);
                if (existing >= 0) _modComponents[existing] = comp;
                else _modComponents.Add(comp);
            }
            GD.Print($"[Mod][GameComponentDB] 已合并 {doc.RootElement.GetArrayLength()} 个组件");
        }
        catch (Exception e) { GD.PrintErr($"[Mod] components.json parse error: {e.Message}"); }
    }

    /// <summary>获取包含 Mod 组件的完整列表</summary>
    public static GameComponent[] GetAllWithMods()
    {
        if (_modComponents.Count == 0) return All;
        var list = All.ToList();
        foreach (var mc in _modComponents)
        {
            var idx = list.FindIndex(c => c.Id == mc.Id);
            if (idx >= 0) list[idx] = mc;
            else list.Add(mc);
        }
        return list.ToArray();
    }
}

/// <summary>宣发策略（带资金字段）</summary>
public static class MarketingData
{
    public static float Cost(this MarketingStrategy m) => m switch
    {
        MarketingStrategy.LowKey => 50000,
        MarketingStrategy.Normal => 200000,
        MarketingStrategy.Hype => 500000,
        _ => 50000
    };
    public static float ExpectationBoost(this MarketingStrategy m) => m switch
    {
        MarketingStrategy.LowKey => 0.1f,
        MarketingStrategy.Normal => 0.3f,
        MarketingStrategy.Hype => 0.6f,
        _ => 0.1f
    };
    public static float RiskThreshold(this MarketingStrategy m) => m switch
    {
        MarketingStrategy.LowKey => 0,
        MarketingStrategy.Normal => 80f,
        MarketingStrategy.Hype => 85f,
        _ => 0
    };
    public static float RiskPenalty(this MarketingStrategy m) => m switch
    {
        MarketingStrategy.LowKey => 0,
        MarketingStrategy.Normal => 0.2f,
        MarketingStrategy.Hype => 0.4f,
        _ => 0
    };
}

// ==================== 游戏开发阶段 ====================
public enum DevPhase
{
    Idle,
    Planning,
    Developing,
    Polishing,
    Testing,         // QA测试阶段（开发完成后的质量检查）
    ReadyToRelease,  // 测试完成，等待发售
    Marketing,
    Released
}

// ==================== 引擎商业模式 ====================
public enum EngineBizModel
{
    Closed,
    OpenSource,
    Buyout,
    Subscription,
    Royalty
}

// ==================== 科技分支 ====================
public enum TechBranch
{
    ProgramBase,
    Render2D,
    Render3D,
    Audio,
    Network,
    AI,
    Platform,
    GenreUnlock,    // 类型解锁
    ThemeUnlock     // 主题解锁
}

// ==================== 团队任务类型 ====================
public enum TeamTask
{
    None,
    DevelopGame,
    ResearchTech,
    Refactor,
    Outsource,
    DevelopEngine       // 开发/升级引擎
}

// ==================== 宣发策略 ====================
public enum MarketingStrategy
{
    LowKey,
    Normal,
    Hype
}

public enum PriceModel
{
    Free,       // 免费 + 广告/内购
    BuyToPlay   // 买断制
}

// ==================== 外包难度 ====================
public enum OutsourceDifficulty
{
    Easy,
    Medium,
    Hard,
    Epic
}

// ==================== 房子等级 ====================
public enum HouseTier
{
    Garage,         // 车库创业（2人）免费
    SmallOffice,    // 小办公室（5人）
    MediumOffice,   // 中办公室（12人）
    LargeOffice,    // 大办公楼（25人）
    HighRise        // 豪华大楼（50人）
}

// ==================== 额外房间类型 ====================
public enum BonusRoom
{
    MeetingRoom,    // 会议室（团队默契+10%）
    ServerRoom,     // 服务器机房（研发速度+10%）
    ArtStudio,      // 美术工作室（美术效率+15%）
    AudioLab,       // 音频实验室（音频效率+15%）
    Lounge          // 休息区（疲劳恢复+20%）
}

// ==================== 数据结构定义 ====================

public struct SkillLevelInfo
{
    public int Level { get; set; }
    public int Exp { get; set; }
    public int ExpToNext { get; set; }
    public float Efficiency { get; set; }
}

public struct TechInfo
{
    public string Id { get; set; }
    public string Name { get { string key = $"tech.{Id}"; string r = Loc.Tr(key); return r == key ? (_rawName ?? Id) : r; } set { _rawName = value; } }
    private string _rawName;
    public TechBranch Branch { get; set; }
    public int Level { get; set; }
    public int RequiredManMonths { get; set; }
    public SkillType PrimarySkill { get; set; }
    public int PrimarySkillLevel { get; set; }
    public SkillType? SecondarySkill { get; set; }
    public int SecondarySkillLevel { get; set; }
    public string Prerequisite { get; set; }
    public string Description { get; set; }
    public string EffectDescription { get { string key = $"tech.effect.{Id}"; string r = Loc.Tr(key); return r == key ? (_rawEffect ?? "") : r; } set { _rawEffect = value; } }
    private string _rawEffect;
    public string TagDescription { get { string key = $"tech.desc.{Id}"; string r = Loc.Tr(key); return r == key ? (_rawTag ?? "") : r; } set { _rawTag = value; } }
    private string _rawTag;
    public bool IsResearched { get; set; }

    public TechInfo(string id, string name, TechBranch branch, int level, int manMonths,
        SkillType priSkill, int priLevel, SkillType? secSkill, int secLevel,
        string prereq, string desc, string effect)
    {
        Id = id; _rawName = name; Branch = branch; Level = level;
        RequiredManMonths = manMonths;
        PrimarySkill = priSkill; PrimarySkillLevel = priLevel;
        SecondarySkill = secSkill; SecondarySkillLevel = secLevel;
        Prerequisite = prereq; _rawEffect = effect; _rawTag = desc;
        IsResearched = false;
    }
}

public struct HouseInfo
{
    public string Name { get; set; }
    public int Capacity { get; set; }
    public float MoveCost { get; set; }         // 搬家费用
    public float MonthlyRent { get; set; }      // 月租
    public Vector3 Size { get; set; }           // 房子3D尺寸
    public Color Color { get; set; }
    public int WindowCount { get; set; }
}

public struct BonusRoomInfo
{
    public string Name { get; set; }
    public int Capacity { get; set; }
    public float Cost { get; set; }
    public float MonthlyRent { get; set; }
    public string BonusDesc { get; set; }
}

public struct OutsourceContract
{
    public string Name { get; set; }
    public OutsourceDifficulty Difficulty { get; set; }
    public int RequiredMonths { get; set; }
    public float Payment { get; set; }
    public float PenaltyRate { get; set; }
    public SkillType PrimarySkill { get; set; }
    public int MinSkillLevel { get; set; }
    public int ExpReward { get; set; }
}

/// <summary>
/// 初始解锁的类型（5个）
/// </summary>
public static class GameInitialUnlocks
{
    public static readonly GameGenre[] StartGenres = {
        GameGenre.RPG, GameGenre.ACT, GameGenre.AVG, GameGenre.SLG,
        GameGenre.FPS, GameGenre.SIM, GameGenre.RAC, GameGenre.SPO,
        GameGenre.MUS, GameGenre.PZL
    };
    public static readonly GameTheme[] StartThemes = {
        GameTheme.Fantasy, GameTheme.SciFi, GameTheme.Modern,
        GameTheme.Historical, GameTheme.PostApoc,
        GameTheme.Comedy, GameTheme.Horror, GameTheme.Mystery, GameTheme.Romance,
        GameTheme.War, GameTheme.School
    };
}

/// <summary>
/// 房子等级数据
/// </summary>
public static class HouseData
{
    public static readonly Dictionary<HouseTier, HouseInfo> Data = new()
    {
        [HouseTier.Garage] = new HouseInfo { Name = "车库创业", Capacity = 2, MoveCost = 0, MonthlyRent = 1500, Size = new Vector3(7, 2.2f, 5), Color = new Color(0.55f, 0.45f, 0.35f), WindowCount = 2 },
        [HouseTier.SmallOffice] = new HouseInfo { Name = "小办公室", Capacity = 5, MoveCost = 80000, MonthlyRent = 5000, Size = new Vector3(8, 2.4f, 6), Color = new Color(0.6f, 0.65f, 0.7f), WindowCount = 4 },
        [HouseTier.MediumOffice] = new HouseInfo { Name = "中办公室", Capacity = 12, MoveCost = 300000, MonthlyRent = 15000, Size = new Vector3(12, 2.8f, 9), Color = new Color(0.65f, 0.7f, 0.75f), WindowCount = 6 },
        [HouseTier.LargeOffice] = new HouseInfo { Name = "大办公楼", Capacity = 25, MoveCost = 800000, MonthlyRent = 40000, Size = new Vector3(16, 3.2f, 12), Color = new Color(0.7f, 0.75f, 0.8f), WindowCount = 10 },
        [HouseTier.HighRise] = new HouseInfo { Name = "豪华大楼", Capacity = 50, MoveCost = 2500000, MonthlyRent = 120000, Size = new Vector3(22, 4.0f, 16), Color = new Color(0.75f, 0.8f, 0.85f), WindowCount = 16 },
    };

    public static HouseTier NextTier(HouseTier current)
    {
        return current switch
        {
            HouseTier.Garage => HouseTier.SmallOffice,
            HouseTier.SmallOffice => HouseTier.MediumOffice,
            HouseTier.MediumOffice => HouseTier.LargeOffice,
            HouseTier.LargeOffice => HouseTier.HighRise,
            _ => HouseTier.HighRise
        };
    }

    public static bool CanUpgrade(HouseTier current) => current != HouseTier.HighRise;
}

/// <summary>
/// 额外房间数据
/// </summary>
public static class BonusRoomData
{
    public static readonly Dictionary<BonusRoom, BonusRoomInfo> Data = new()
    {
        [BonusRoom.MeetingRoom] = new BonusRoomInfo { Name = "会议室", Capacity = 0, Cost = 30000, MonthlyRent = 4000, BonusDesc = "团队默契+10%" },
        [BonusRoom.ServerRoom] = new BonusRoomInfo { Name = "服务器机房", Capacity = 0, Cost = 100000, MonthlyRent = 10000, BonusDesc = "研发速度+10%" },
        [BonusRoom.ArtStudio] = new BonusRoomInfo { Name = "美术工作室", Capacity = 2, Cost = 80000, MonthlyRent = 8000, BonusDesc = "美术效率+15%" },
        [BonusRoom.AudioLab] = new BonusRoomInfo { Name = "音频实验室", Capacity = 2, Cost = 60000, MonthlyRent = 6000, BonusDesc = "音频效率+15%" },
        [BonusRoom.Lounge] = new BonusRoomInfo { Name = "休息区", Capacity = 0, Cost = 40000, MonthlyRent = 5000, BonusDesc = "疲劳恢复+20%" },
    };
}

// ═══════════════════════ 枚举中文名（支持国际化） ═══════════════════════
public static class EnumNames
{
    public static string Name(this GameGenre g) => Loc.Tr($"genre.{g}");
    public static string Name(this GameTheme t) => Loc.Tr($"theme.{t}");
    public static string Name(this Platform p) => Loc.Tr($"plat.{p}");
    public static string Name(this DevPhase d) => Loc.Tr($"phase.{d}");
    public static string Name(this SkillType s) => Loc.Tr($"skill.{s}");
    public static string Name(this MarketingStrategy m) => Loc.Tr($"mkt.{m}");
    public static string Name(this EngineBizModel e) => Loc.Tr($"eng.{e}");
    public static string Name(this BonusRoom b) => Loc.Tr($"room.{b}");
    public static string Name(this HouseTier h) => Loc.Tr($"house.{h}");
    public static string BonusDesc(this BonusRoom b) => Loc.Tr($"room.{b}_desc");
    public static string Name(this GameComponent c) => Loc.Tr($"comp.{c.Id}");
    public static string CompDesc(this GameComponent c) => Loc.Tr($"comp.{c.Id}_desc");
    public static string Name(this ComponentCategory cat) => Loc.Tr($"compcat.{cat}");
}
