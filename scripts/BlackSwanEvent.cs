using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>黑天鹅事件 — 行业突变，玩家必须做选择应对</summary>
public class BlackSwanEvent
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int DurationMonths { get; set; } = 6;
    public int RemainingMonths { get; set; }

    /// <summary>当前生效的全局 modifiers</summary>
    public Dictionary<string, float> ActiveModifiers { get; set; } = new();

    /// <summary>玩家是否已做出应对</summary>
    public bool PlayerResponded { get; set; }

    /// <summary>玩家选择（0=选项A，1=选项B）</summary>
    public int PlayerChoice { get; set; }

    public string ChoiceATitle { get; set; }
    public string ChoiceADesc { get; set; }
    public string ChoiceBTitle { get; set; }
    public string ChoiceBDesc { get; set; }

    /// <summary>选择的效果（延迟一帧执行）</summary>
    public Action<int> OnChoose { get; set; }

    /// <summary>每月勾选效果</summary>
    public Action<GameManager> MonthlyEffect { get; set; }

    /// <summary>该事件是否为正面</summary>
    public bool IsPositive => ActiveModifiers.Values.Any(v => v > 1f);
}

/// <summary>全局黑天鹅事件管理器</summary>
public static class BlackSwanManager
{
    public static List<BlackSwanEvent> ActiveEvents { get; } = new();
    public static List<BlackSwanEvent> EventHistory { get; } = new();

    private static readonly Random _rng = new();

    /// <summary>Mod 注册自定义事件模板</summary>
    public static void RegisterTemplate(Func<BlackSwanEvent> template)
    {
        if (template != null) _templates.Add(template);
    }

    /// <summary>所有可用事件模板</summary>
    private static readonly List<Func<BlackSwanEvent>> _templates = new()
    {
        CreateOpenWorldFatigue,
        CreatePlatformBoom,
        CreateEconomicCrisis,
        CreateGenreRevival,
        CreateTechLeap,
        CreatePiracyWave,
        CreateIndieSpring,
    };

    /// <summary>每月调用，极低概率触发新事件</summary>
    public static void MonthlyTick(GameManager gm)
    {
        // 已有事件到期
        for (int i = ActiveEvents.Count - 1; i >= 0; i--)
        {
            ActiveEvents[i].RemainingMonths--;
            if (ActiveEvents[i].RemainingMonths <= 0)
            {
                EventHistory.Add(ActiveEvents[i]);
                ActiveEvents.RemoveAt(i);
            }
        }

        // 触发新事件（~3% 每月概率）
        if (ActiveEvents.Count == 0 && _rng.NextDouble() < 0.03 && gm.GameMonth > 3)
        {
            ModAPI.FireHooks(ModAPI.GameHook.BeforeBlackSwan);
            var evt = _templates[_rng.Next(_templates.Count)]();
            evt.RemainingMonths = evt.DurationMonths;
            ActiveEvents.Add(evt);

            var eRef = evt;
            gm.ShowChoicePopup(evt.Title, evt.Description,
                evt.ChoiceATitle, evt.ChoiceBTitle,
                () =>
                {
                    eRef.PlayerResponded = true;
                    eRef.PlayerChoice = 0;
                    eRef.OnChoose?.Invoke(0);
                    gm.ShowToast(eRef.Title, $"选择了「{eRef.ChoiceATitle}」", new Color(0.3f, 0.7f, 0.4f));
                    ModAPI.FireHooks(ModAPI.GameHook.AfterBlackSwanResponse);
                },
                () =>
                {
                    eRef.PlayerResponded = true;
                    eRef.PlayerChoice = 1;
                    eRef.OnChoose?.Invoke(1);
                    gm.ShowToast(eRef.Title, $"选择了「{eRef.ChoiceBTitle}」", new Color(0.7f, 0.5f, 0.2f));
                    ModAPI.FireHooks(ModAPI.GameHook.AfterBlackSwanResponse);
                },
                new Color(0.8f, 0.3f, 0.2f));
        }
    }

    /// <summary>获取当前对所有游戏的销量 modifier</summary>
    public static float GetSalesModifier(GameProject proj)
    {
        float mod = 1f;
        foreach (var evt in ActiveEvents)
            foreach (var kv in evt.ActiveModifiers)
            {
                if (kv.Key == "all_sales") mod *= kv.Value;
                if (kv.Key == "genre_sales" && proj != null && proj.Genre.ToString() == evt.Id.Split('_')[1]) mod *= kv.Value;
            }
        return mod;
    }

    // ═══════════════════ 事件模板 ═══════════════════

    private static BlackSwanEvent CreateOpenWorldFatigue() => new()
    {
        Id = "openworld_fatigue",
        Title = "🔄 开放世界审美疲劳",
        Description = "市场研究报告显示——玩家已经厌倦了千篇一律的开放世界。\n\n未来6个月所有开放世界元素游戏的销量将大幅下滑。\n\n你可以选择：",
        DurationMonths = 6,
        ActiveModifiers = new() { ["genre_sales_openworld"] = 0.6f },
        ChoiceATitle = "🎯 转型：精简设计",
        ChoiceADesc = "放弃开放世界，改做线性关卡。\n销量惩罚减半，但开发速度+10%",
        ChoiceBTitle = "🏋️ 硬抗：品质为王",
        ChoiceBDesc = "继续做开放世界，用内容质量说话。\n销量惩罚不变，但如果评分>85，反而获得+20%销量",
        OnChoose = (c) => { if (c == 0) GD.Print("[黑天鹅] 选择转型"); },
    };

    private static BlackSwanEvent CreatePlatformBoom() => new()
    {
        Id = "platform_boom",
        Title = "🚀 新平台爆发！",
        Description = "某新兴游戏平台突然爆火，用户量暴增！\n\n如果在3个月内推出一款该平台的游戏，\n将获得巨大的先发优势。\n\n但时间紧迫——研发周期只有正常的一半。",
        DurationMonths = 3,
        ActiveModifiers = new(),
        ChoiceATitle = "⚡ 全力跟进",
        ChoiceADesc = "立即启动新平台项目。\n研发速度+50%，但疲劳度+15%",
        ChoiceBTitle = "🧐 观望",
        ChoiceBDesc = "看看再说，不急于跟进。\n无加成也无惩罚",
        OnChoose = (c) =>
        {
            if (c == 0) GD.Print("[黑天鹅] 选择跟进新平台");
        },
    };

    private static BlackSwanEvent CreateEconomicCrisis() => new()
    {
        Id = "economic_crisis",
        Title = "💥 经济危机来袭！",
        Description = "全球游戏行业遭遇经济寒冬。\n\n所有公司资金缩水20%，\n未来6个月游戏销量整体下降。",
        DurationMonths = 6,
        ActiveModifiers = new() { ["all_sales"] = 0.8f },
        ChoiceATitle = "✂️ 裁员瘦身",
        ChoiceADesc = "裁员20%，节省开支。\n声誉-15，但每月支出-25%",
        ChoiceBTitle = "🛡️ 坚持不裁员",
        ChoiceBDesc = "保持团队完整，共渡时艰。\n每月支出不变，但团队满意度+10",
        OnChoose = (c) => GD.Print("[黑天鹅] 选择", c == 0 ? "裁员" : "坚持"),
    };

    private static BlackSwanEvent CreateGenreRevival() => new()
    {
        Id = "genre_revival",
        Title = "📈 复古风潮来袭",
        Description = "一夜之间，复古类型游戏重新成为市场热点！\n\n像素风、回合制、横版过关——\n这些被遗忘的类型迎来了第二春。",
        DurationMonths = 8,
        ActiveModifiers = new() { ["genre_bonus_retro"] = 1.5f },
        ChoiceATitle = "🎨 快速跟风",
        ChoiceADesc = "用现有引擎快速做一款复古游戏。\n开发时间-30%，销量+40%",
        ChoiceBTitle = "💎 精雕细琢",
        ChoiceBDesc = "认真做一款复古大作。\n开发时间+20%，但评分+15，销量+80%",
        OnChoose = (c) => GD.Print("[黑天鹅] 选择", c == 0 ? "跟风" : "精雕"),
    };

    private static BlackSwanEvent CreateTechLeap() => new()
    {
        Id = "tech_leap",
        Title = "🔬 技术突破！",
        Description = "一项革命性技术突然成熟——\nAI辅助开发工具大幅提升效率！\n\n所有公司的研发速度+30%，持续6个月。\n但技术债务积累速度也翻倍。",
        DurationMonths = 6,
        ActiveModifiers = new(),
        ChoiceATitle = "🤖 全面采用",
        ChoiceADesc = "全面引入AI工具。\n研发速度+50%，但技术债务+20%",
        ChoiceBTitle = "⚖️ 谨慎引入",
        ChoiceBDesc = "只在小范围试用。\n研发速度+15%，技术债务不变",
        OnChoose = (c) => GD.Print("[黑天鹅] 选择", c == 0 ? "全面AI" : "谨慎"),
    };

    private static BlackSwanEvent CreatePiracyWave() => new()
    {
        Id = "piracy_wave",
        Title = "🏴‍☠️ 盗版浪潮",
        Description = "一款新破解工具流传开来，\n导致未来6个月游戏销量整体下降。\n\n但这也是提升品牌影响力的机会——",
        DurationMonths = 6,
        ActiveModifiers = new() { ["all_sales"] = 0.7f },
        ChoiceATitle = "🛡️ 加强加密",
        ChoiceADesc = "投入资金加强DRM保护。\n花费¥50,000，销量惩罚减半",
        ChoiceBTitle = "❤️ 以德服人",
        ChoiceBDesc = "放弃加密，改为做玩家社群运营。\n销量惩罚不变，但粉丝增长速度+50%",
        OnChoose = (c) => GD.Print("[黑天鹅] 选择", c == 0 ? "加密" : "社群"),
    };

    private static BlackSwanEvent CreateIndieSpring() => new()
    {
        Id = "indie_spring",
        Title = "🌱 独立游戏之春",
        Description = "独立游戏突然成为市场宠儿！\n\n大厂大作销量下滑，\n小而美的独立作品反而大放异彩。\n\n中小型工作室迎来黄金时代。",
        DurationMonths = 9,
        ActiveModifiers = new() { ["indie_bonus"] = 1.6f },
        ChoiceATitle = "🎪 转型独立",
        ChoiceADesc = "缩小团队规模，专注独立游戏。\n解散一个团队（可指定），其他团队效率+20%",
        ChoiceBTitle = "🏢 坚持规模",
        ChoiceBDesc = "继续做大作，用工业化流程降低成本。\n开发成本-20%，但销量不享受独立加成",
        OnChoose = (c) => GD.Print("[黑天鹅] 选择", c == 0 ? "独立" : "规模"),
    };
}
