using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>危机事件链节点</summary>
public class CrisisNode
{
    public string Id;
    public string TitleKey;
    public string DescKey;
    public string TriggerCondition;     // 触发条件（lua式表达式，如 "bug>20 && scale>0.5"）
    public float TriggerProbability;     // 触发概率 0~1
    public int DelayMonths;              // 触发后延迟月数
    public List<CrisisOption> Options;
    public string ParentEventId;        // 父事件（链式反应）
    public string ChainId;              // 链ID
    public int ChainStep;               // 链步数

    // 触发条件评估
    public bool Evaluate(GameProject proj)
    {
        if (string.IsNullOrEmpty(TriggerCondition)) return false;
        // 简单条件解析
        var parts = TriggerCondition.Split(' ');
        if (parts.Length < 3) return false;

        string left = parts[0];
        string op = parts[1];
        string right = parts[2];

        float leftVal = GetValue(proj, left);
        float rightVal = float.TryParse(right, out var r) ? r : GetValue(proj, right);

        return op switch
        {
            ">" => leftVal > rightVal,
            ">=" => leftVal >= rightVal,
            "<" => leftVal < rightVal,
            "<=" => leftVal <= rightVal,
            "==" => Mathf.Abs(leftVal - rightVal) < 0.01f,
            _ => false
        };
    }

    private float GetValue(GameProject proj, string name) => name switch
    {
        "bug" => proj.BugCount,
        "scale" => proj.Scale,
        "debt" => proj.TechDebt,
        "progress" => proj.DevProgress * 100,
        "gameplay" => proj.GameplayScore,
        "graphics" => proj.GraphicsScore,
        "audio" => proj.AudioScore,
        "story" => proj.StoryScore,
        "stability" => proj.StabilityScore,
        "network" => proj.NetworkScore,
        "months" => proj.MonthsSpent,
        "delayCount" => proj.DelayCount,
        _ => 0
    };
}

public class CrisisOption
{
    public string LabelKey;
    public string ResultDescKey;
    public Dictionary<string, float> Effects;  // 属性名→变化值
    public string NextEventId;                 // 触发的下一个事件
    public int ReputationChange;
    public float TrustChange;
}

/// <summary>Mod 自定义危机事件工厂</summary>
public static class ModCrisisRegistry
{
    public static List<Func<CrisisNode>> CustomFactories { get; } = new();
    public static void RegisterFactory(Func<CrisisNode> factory) => CustomFactories.Add(factory);
}

/// <summary>危机事件树——因果链事件发生器</summary>
public partial class CrisisEventTree : Node
{
    private GameManager _gm;
    private GameDevManager _devMgr;
    private ResourceManager _res;
    private Random _rng = new();

    // 已触发的链ID + 步数
    private Dictionary<string, int> _chainProgress = new();

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        _devMgr = _gm.GetNode<GameDevManager>("GameDevManager");
        _res = _gm.GetNode<ResourceManager>("ResourceManager");
    }

    /// <summary>获取所有危机事件定义（包含 Mod 自定义事件）</summary>
    public List<CrisisNode> GetCrisisPool()
    {
        var pool = new List<CrisisNode>
        {
            // ══ 链1: 服务器噩梦 ══
            new CrisisNode
            {
                Id = "server_crash_1", ChainId = "server_crash", ChainStep = 1,
                TitleKey = "crisis.server_crash_title", DescKey = "crisis.server_crash_desc",
                TriggerCondition = "network > 0 && months > 3",
                TriggerProbability = 0.3f, DelayMonths = 0,
                ParentEventId = null,
                Options = new List<CrisisOption>
                {
                    new CrisisOption { LabelKey = "crisis.server_emergency", ResultDescKey = "crisis.server_emergency_result",
                        Effects = new Dictionary<string, float> { {"money", -50000}, {"stability", 5} }, NextEventId = "server_chaos_1", ReputationChange = 0, TrustChange = -3 },
                    new CrisisOption { LabelKey = "crisis.server_apology", ResultDescKey = "crisis.server_apology_result",
                        Effects = new Dictionary<string, float> { {"sales", -15} }, NextEventId = null, ReputationChange = -5, TrustChange = -5 },
                }
            },
            new CrisisNode
            {
                Id = "server_chaos_1", ChainId = "server_crash", ChainStep = 2,
                TitleKey = "crisis.server_chaos_title", DescKey = "crisis.server_chaos_desc",
                TriggerCondition = "1 > 0", TriggerProbability = 0.8f, DelayMonths = 2,
                ParentEventId = "server_crash_1",
                Options = new List<CrisisOption>
                {
                    new CrisisOption { LabelKey = "crisis.server_refactor", ResultDescKey = "crisis.server_refactor_result",
                        Effects = new Dictionary<string, float> { {"money", -100000}, {"stability", 10}, {"progress", -5} }, NextEventId = null, ReputationChange = 3, TrustChange = 5 },
                    new CrisisOption { LabelKey = "crisis.server_bandaid", ResultDescKey = "crisis.server_bandaid_result",
                        Effects = new Dictionary<string, float> { {"debt", 15}, {"stability", -3} }, NextEventId = "server_debt_collapse", ReputationChange = -2, TrustChange = -3 },
                }
            },
            new CrisisNode
            {
                Id = "server_debt_collapse", ChainId = "server_crash", ChainStep = 3,
                TitleKey = "crisis.server_debt_title", DescKey = "crisis.server_debt_desc",
                TriggerCondition = "1 > 0", TriggerProbability = 1f, DelayMonths = 3,
                ParentEventId = "server_chaos_1",
                Options = new List<CrisisOption>
                {
                    new CrisisOption { LabelKey = "crisis.server_overhaul", ResultDescKey = "crisis.server_overhaul_result",
                        Effects = new Dictionary<string, float> { {"money", -200000}, {"stability", 20}, {"debt", -30}, {"progress", -10} }, NextEventId = null, ReputationChange = 5, TrustChange = 8 },
                    new CrisisOption { LabelKey = "crisis.server_ignore", ResultDescKey = "crisis.server_ignore_result",
                        Effects = new Dictionary<string, float> { {"network", -30}, {"stability", -20}, {"sales", -30} }, NextEventId = null, ReputationChange = -15, TrustChange = -20 },
                }
            },

            // ══ 链2: Feature Creep ══
            new CrisisNode
            {
                Id = "feature_creep_1", ChainId = "feature_creep", ChainStep = 1,
                TitleKey = "crisis.feature_creep_title", DescKey = "crisis.feature_creep_desc",
                TriggerCondition = "progress > 30 && progress < 70",
                TriggerProbability = 0.25f, DelayMonths = 0,
                Options = new List<CrisisOption>
                {
                    new CrisisOption { LabelKey = "crisis.creep_accept", ResultDescKey = "crisis.creep_accept_result",
                        Effects = new Dictionary<string, float> { {"gameplay", 8}, {"progress", -10}, {"debt", 10} }, NextEventId = "feature_creep_2", ReputationChange = 0, TrustChange = -2 },
                    new CrisisOption { LabelKey = "crisis.creep_reject", ResultDescKey = "crisis.creep_reject_result",
                        Effects = new Dictionary<string, float> { {"gameplay", -3}, {"delayCount", 0} }, NextEventId = null, ReputationChange = 2, TrustChange = 3 },
                }
            },
            new CrisisNode
            {
                Id = "feature_creep_2", ChainId = "feature_creep", ChainStep = 2,
                TitleKey = "crisis.creep_again_title", DescKey = "crisis.creep_again_desc",
                TriggerCondition = "1 > 0", TriggerProbability = 0.6f, DelayMonths = 3,
                ParentEventId = "feature_creep_1",
                Options = new List<CrisisOption>
                {
                    new CrisisOption { LabelKey = "crisis.creep_accept2", ResultDescKey = "crisis.creep_accept2_result",
                        Effects = new Dictionary<string, float> { {"gameplay", 12}, {"story", 8}, {"progress", -15}, {"debt", 15} }, NextEventId = "feature_creep_3", ReputationChange = 3, TrustChange = -5 },
                    new CrisisOption { LabelKey = "crisis.creep_cut", ResultDescKey = "crisis.creep_cut_result",
                        Effects = new Dictionary<string, float> { {"gameplay", -5}, {"progress", 5} }, NextEventId = null, ReputationChange = -2, TrustChange = 2 },
                }
            },
            new CrisisNode
            {
                Id = "feature_creep_3", ChainId = "feature_creep", ChainStep = 3,
                TitleKey = "crisis.creep_hell_title", DescKey = "crisis.creep_hell_desc",
                TriggerCondition = "1 > 0", TriggerProbability = 0.9f, DelayMonths = 4,
                ParentEventId = "feature_creep_2",
                Options = new List<CrisisOption>
                {
                    new CrisisOption { LabelKey = "crisis.creep_ship", ResultDescKey = "crisis.creep_ship_result",
                        Effects = new Dictionary<string, float> { {"progress", 20}, {"bug", 20} }, NextEventId = null, ReputationChange = -8, TrustChange = -10 },
                    new CrisisOption { LabelKey = "crisis.creep_reset", ResultDescKey = "crisis.creep_reset_result",
                        Effects = new Dictionary<string, float> { {"progress", -20}, {"debt", -30}, {"gameplay", -8} }, NextEventId = null, ReputationChange = 5, TrustChange = 3 },
                }
            },

            // ══ 链3: 发行商干预 ══
            new CrisisNode
            {
                Id = "publisher_intervention", ChainId = "publisher", ChainStep = 1,
                TitleKey = "crisis.publisher_title", DescKey = "crisis.publisher_desc",
                TriggerCondition = "progress > 60",
                TriggerProbability = 0.2f, DelayMonths = 0,
                Options = new List<CrisisOption>
                {
                    new CrisisOption { LabelKey = "crisis.publisher_accept", ResultDescKey = "crisis.publisher_accept_result",
                        Effects = new Dictionary<string, float> { {"money", 100000}, {"stability", -10} }, NextEventId = "publisher_backlash", ReputationChange = -3, TrustChange = -8 },
                    new CrisisOption { LabelKey = "crisis.publisher_reject", ResultDescKey = "crisis.publisher_reject_result",
                        Effects = new Dictionary<string, float> { {"money", -50000}, {"story", 5} }, NextEventId = null, ReputationChange = 5, TrustChange = 5 },
                }
            },
            new CrisisNode
            {
                Id = "publisher_backlash", ChainId = "publisher", ChainStep = 2,
                TitleKey = "crisis.publisher_backlash_title", DescKey = "crisis.publisher_backlash_desc",
                TriggerCondition = "1 > 0", TriggerProbability = 0.7f, DelayMonths = 2,
                ParentEventId = "publisher_intervention",
                Options = new List<CrisisOption>
                {
                    new CrisisOption { LabelKey = "crisis.publisher_revert", ResultDescKey = "crisis.publisher_revert_result",
                        Effects = new Dictionary<string, float> { {"money", -100000}, {"stability", 5}, {"trust", 5} }, NextEventId = null, ReputationChange = 5, TrustChange = 8 },
                    new CrisisOption { LabelKey = "crisis.publisher_double_down", ResultDescKey = "crisis.publisher_double_down_result",
                        Effects = new Dictionary<string, float> { {"money", 150000}, {"stability", -20}, {"bug", 15} }, NextEventId = null, ReputationChange = -10, TrustChange = -15 },
                }
            },

            // ══ 链4: 技术债务爆炸 ══
            new CrisisNode
            {
                Id = "debt_warning", ChainId = "debt", ChainStep = 1,
                TitleKey = "crisis.debt_warn_title", DescKey = "crisis.debt_warn_desc",
                TriggerCondition = "debt > 50",
                TriggerProbability = 0.4f, DelayMonths = 0,
                Options = new List<CrisisOption>
                {
                    new CrisisOption { LabelKey = "crisis.debt_refactor", ResultDescKey = "crisis.debt_refactor_result",
                        Effects = new Dictionary<string, float> { {"debt", -20}, {"progress", -5} }, NextEventId = null, ReputationChange = 3, TrustChange = 3 },
                    new CrisisOption { LabelKey = "crisis.debt_ignore", ResultDescKey = "crisis.debt_ignore_result",
                        Effects = new Dictionary<string, float> { {"bug", 10} }, NextEventId = "debt_collapse", ReputationChange = -2, TrustChange = -5 },
                }
            },
            new CrisisNode
            {
                Id = "debt_collapse", ChainId = "debt", ChainStep = 2,
                TitleKey = "crisis.debt_collapse_title", DescKey = "crisis.debt_collapse_desc",
                TriggerCondition = "1 > 0", TriggerProbability = 0.9f, DelayMonths = 3,
                ParentEventId = "debt_warning",
                Options = new List<CrisisOption>
                {
                    new CrisisOption { LabelKey = "crisis.debt_rewrite", ResultDescKey = "crisis.debt_rewrite_result",
                        Effects = new Dictionary<string, float> { {"debt", -40}, {"progress", -15}, {"bug", -15} }, NextEventId = null, ReputationChange = 5, TrustChange = 5 },
                    new CrisisOption { LabelKey = "crisis.debt_crunch", ResultDescKey = "crisis.debt_crunch_result",
                        Effects = new Dictionary<string, float> { {"bug", 20}, {"debt", 10}, {"progress", 5} }, NextEventId = "debt_meltdown", ReputationChange = -5, TrustChange = -8 },
                }
            },
            new CrisisNode
            {
                Id = "debt_meltdown", ChainId = "debt", ChainStep = 3,
                TitleKey = "crisis.debt_meltdown_title", DescKey = "crisis.debt_meltdown_desc",
                TriggerCondition = "1 > 0", TriggerProbability = 1f, DelayMonths = 2,
                ParentEventId = "debt_collapse",
                Options = new List<CrisisOption>
                {
                    new CrisisOption { LabelKey = "crisis.debt_abandon", ResultDescKey = "crisis.debt_abandon_result",
                        Effects = new Dictionary<string, float> { {"progress", -30}, {"debt", -60} }, NextEventId = null, ReputationChange = -10, TrustChange = -5 },
                    new CrisisOption { LabelKey = "crisis.debt_ship", ResultDescKey = "crisis.debt_ship_result",
                        Effects = new Dictionary<string, float> { {"bug", 30}, {"stability", -30} }, NextEventId = null, ReputationChange = -15, TrustChange = -20 },
                }
            },
        };

        // Mod 自定义危机事件（工厂注册）
        foreach (var factory in ModCrisisRegistry.CustomFactories)
        {
            try
            {
                var node = factory();
                if (node != null) pool.Add(node);
            }
            catch (Exception e) { GD.PrintErr($"[Mod] 自定义危机事件工厂错误: {e.Message}"); }
        }

        // Mod 数据文件定义的危机事件（crisis.json）
        pool.AddRange(CrisisModDB.ConvertToNodes());

        return pool;
    }

    /// <summary>每月检查可触发的事件</summary>
    public void CheckEvents(GameProject proj, Team team)
    {
        if (proj == null) return;

        var pool = GetCrisisPool();

        // 过滤出可触发的事件
        var candidates = pool.Where(c =>
        {
            // 没有父事件 or 父事件已触发且在正确步数
            if (string.IsNullOrEmpty(c.ParentEventId))
            {
                if (_triggeredEvents.Contains(c.Id)) return false;
            }
            else
            {
                string chainKey = $"{c.ChainId}_{c.ChainStep - 1}";
                var parentId = pool.Find(p => p.Id == c.ParentEventId);
                if (parentId == null || !_triggeredEvents.Contains(parentId.Id)) return false;
                if (_chainProgress.ContainsKey(c.ChainId) && _chainProgress[c.ChainId] >= c.ChainStep) return false;
            }

            // 含有 sales 效果的事件需要项目已发售
            if (c.Options.Any(o => o.Effects.ContainsKey("sales")) && !proj.IsReleased) return false;

            return c.Evaluate(proj) && _rng.NextDouble() < c.TriggerProbability;
        }).ToList();

        foreach (var crisis in candidates)
        {
            if (crisis.DelayMonths > 0)
            {
                // 延迟触发
                _pendingEvents.Add((crisis, proj, _gm.GameMonth + crisis.DelayMonths));
            }
            else
            {
                TriggerEvent(crisis, proj, team);
            }
        }

        // 检查待触发事件
        for (int i = _pendingEvents.Count - 1; i >= 0; i--)
        {
            if (_pendingEvents[i].triggerMonth <= _gm.GameMonth)
            {
                TriggerEvent(_pendingEvents[i].crisis, _pendingEvents[i].proj, team);
                _pendingEvents.RemoveAt(i);
            }
        }
    }

    private HashSet<string> _triggeredEvents = new();
    private List<(CrisisNode crisis, GameProject proj, int triggerMonth)> _pendingEvents = new();

    private void TriggerEvent(CrisisNode crisis, GameProject proj, Team team)
    {
        ModAPI.FireHooks(ModAPI.GameHook.BeforeCrisisTrigger);
        _triggeredEvents.Add(crisis.Id);
        if (!string.IsNullOrEmpty(crisis.ChainId))
        {
            _chainProgress[crisis.ChainId] = crisis.ChainStep;
        }

        // 弹出决策界面
        ShowCrisisDialog(crisis, proj, team);
    }

    private void ShowCrisisDialog(CrisisNode crisis, GameProject proj, Team team)
    {
        var vp = _gm.GetViewport().GetVisibleRect().Size;
        var panel = new Panel { Position = new(vp.X * 0.15f, vp.Y * 0.2f), Size = new(vp.X * 0.7f, vp.Y * 0.6f) };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.97f, 0.96f, 0.94f, 0.97f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.8f, 0.3f, 0.2f, 0.7f),
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10
        });
        _gm.UiLayer.AddChild(panel);

        float y = 15;
        var title = new Label
        {
            Text = $"🔥 {Loc.Tr(crisis.TitleKey)}",
            Position = new(20, y), Size = new(panel.Size.X - 40, 30)
        };
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeColorOverride("font_color", new Color(0.8f, 0.2f, 0.15f));
        panel.AddChild(title);

        y += 40;
        var chainInfo = string.IsNullOrEmpty(crisis.ChainId) ? "" : $" [{Loc.Tr("crisis.chain")} {crisis.ChainStep}/3]";
        var desc = new Label
        {
            Text = Loc.Tr(crisis.DescKey) + chainInfo,
            Position = new(20, y), Size = new(panel.Size.X - 40, 80),
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        desc.AddThemeFontSizeOverride("font_size", 14);
        desc.AddThemeColorOverride("font_color", new Color(0.2f, 0.2f, 0.25f));
        panel.AddChild(desc);

        y += 100;
        foreach (var opt in crisis.Options)
        {
            int idx = crisis.Options.IndexOf(opt);
            var btn = new Button
            {
                Text = Loc.Tr(opt.LabelKey),
                Position = new(20, y), Size = new(panel.Size.X - 40, 36)
            };
            btn.AddThemeFontSizeOverride("font_size", 14);
            int capturedIdx = idx;
            btn.Pressed += () =>
            {
                ApplyCrisisChoice(crisis, capturedIdx, proj, team);
                panel.QueueFree();
            };
            panel.AddChild(btn);

            var effectHint = new Label
            {
                Text = Loc.Tr(opt.ResultDescKey),
                Position = new(25, y + 38), Size = new(panel.Size.X - 50, 30)
            };
            effectHint.AddThemeFontSizeOverride("font_size", 10);
            effectHint.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
            panel.AddChild(effectHint);

            y += 80;
        }

        // 关闭按钮
        var closeBtn = new Button { Text = Loc.Tr("dlg.cancel"), Position = new(20, y), Size = new(panel.Size.X - 40, 30), Flat = true };
        closeBtn.AddThemeFontSizeOverride("font_size", 11);
        closeBtn.AddThemeColorOverride("font_color", new Color(0.7f, 0.3f, 0.3f));
        closeBtn.Pressed += () => panel.QueueFree();
        panel.AddChild(closeBtn);
    }

    private void ApplyCrisisChoice(CrisisNode crisis, int optionIdx, GameProject proj, Team team)
    {
        var opt = crisis.Options[optionIdx];
        if (opt == null) return;
        var devMgr = _gm.GetNodeOrNull<GameDevManager>("GameDevManager");

        foreach (var kv in opt.Effects)
        {
            switch (kv.Key)
            {
                case "money": _res?.EarnMoney((long)kv.Value, "crisis"); break;
                case "sales": if (proj != null) proj.Sales = (int)(proj.Sales * (1 + kv.Value / 100f)); break;
                case "bug": if (proj != null) proj.BugCount = Mathf.Max(0, proj.BugCount + (int)kv.Value); break;
                case "debt": if (proj != null) proj.TechDebt = Mathf.Clamp(proj.TechDebt + kv.Value, 0, 100); break;
                case "gameplay": if (proj != null) proj.GameplayScore = Mathf.Clamp(proj.GameplayScore + kv.Value, 0, 100); break;
                case "graphics": if (proj != null) proj.GraphicsScore = Mathf.Clamp(proj.GraphicsScore + kv.Value, 0, 100); break;
                case "audio": if (proj != null) proj.AudioScore = Mathf.Clamp(proj.AudioScore + kv.Value, 0, 100); break;
                case "story": if (proj != null) proj.StoryScore = Mathf.Clamp(proj.StoryScore + kv.Value, 0, 100); break;
                case "stability": if (proj != null) proj.StabilityScore = Mathf.Clamp(proj.StabilityScore + kv.Value, 0, 100); break;
                case "network": if (proj != null) proj.NetworkScore = Mathf.Clamp(proj.NetworkScore + kv.Value, 0, 100); break;
                case "progress": if (proj != null) proj.DevProgress = Mathf.Clamp(proj.DevProgress + kv.Value / 100f, 0, 1); break;
                case "trust": if (devMgr != null) devMgr.PlayerTrust = Mathf.Clamp(devMgr.PlayerTrust + kv.Value, 0, 100); break;
            }
        }

        if (opt.ReputationChange != 0 && devMgr != null)
            devMgr.PublisherReputation = Mathf.Clamp(devMgr.PublisherReputation + opt.ReputationChange / 100f, 0, 1);

        proj?.DevLog.Add($"[危机] {Loc.Tr(crisis.TitleKey)} → {Loc.Tr(opt.LabelKey)}");
        ModAPI.FireHooks(ModAPI.GameHook.AfterCrisisChoice);

        // 触发下一事件
        if (!string.IsNullOrEmpty(opt.NextEventId))
        {
            var pool = GetCrisisPool();
            var next = pool.Find(c => c.Id == opt.NextEventId);
            if (next != null)
            {
                _pendingEvents.Add((next, proj, _gm.GameMonth + next.DelayMonths));
            }
        }
    }

    public List<string> GetTriggeredEvents() => _triggeredEvents.ToList();
    public void SetTriggeredEvents(List<string> events) { _triggeredEvents = new HashSet<string>(events); }
}
