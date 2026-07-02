using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>运营指标</summary>
public class LiveOpsMetrics
{
    public float DAU;          // 日活
    public float MAU;          // 月活
    public float ARPU;         // 每用户收入
    public float NegativeRate; // 差评率 0~1
    public float OpCost;       // 运营成本
    public int ActivePlayers;  // 活跃玩家
}

/// <summary>运营事件</summary>
public struct LiveOpsEvent
{
    public string TitleKey;
    public string DescKey;
    public List<LiveOpsOption> Options;
    public string Condition;      // 触发条件
    public float Probability;
}

public struct LiveOpsOption
{
    public string LabelKey;
    public string ResultKey;
    public Action<LiveOpsMetrics> Effect;
    public int TrustCost;
    public long MoneyCost;
}

/// <summary>游戏运营危机系统——发售后不是终点</summary>
public partial class LiveOpsSystem : Node
{
    private GameManager _gm;
    private ResourceManager _res;
    private GameDevManager _devMgr;
    private Random _rng = new();

    public Dictionary<string, LiveOpsMetrics> GameMetrics { get; private set; } = new();
    public List<LiveOpsEvent> ActiveCrises { get; private set; } = new();

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        _res = _gm.GetNode<ResourceManager>("ResourceManager");
        _devMgr = _gm.GetNode<GameDevManager>("GameDevManager");
    }

    /// <summary>新游戏发售后初始化运营指标</summary>
    public void InitGameMetrics(GameProject proj)
    {
        var m = new LiveOpsMetrics
        {
            DAU = proj.Sales * 0.3f,
            MAU = proj.Sales * 0.6f,
            ARPU = proj.SuggestedPrice * 0.1f + 2,
            NegativeRate = Mathf.Clamp((100 - proj.FinalScore) / 200f, 0, 0.5f),
            OpCost = 5000 + proj.Scale * 10000,
            ActivePlayers = proj.Sales
        };
        GameMetrics[proj.Name] = m;
    }

    /// <summary>每月运营更新</summary>
    public void MonthlyUpdate()
    {
        foreach (var kv in GameMetrics.ToList())
        {
            var m = kv.Value;
            // 自然衰减
            m.DAU *= 0.97f;
            m.MAU *= 0.98f;
            m.OpCost *= 1.01f;

            // 收入
            float revenue = m.DAU * m.ARPU;
            float profit = revenue - m.OpCost;
            if (profit > 0) _res?.EarnMoney((long)profit, "liveops");

            // 检查运营事件
            CheckCrises(kv.Key);
        }
    }

    private void CheckCrises(string gameName)
    {
        if (!GameMetrics.ContainsKey(gameName)) return;
        var m = GameMetrics[gameName];

        var pool = new List<LiveOpsEvent>
        {
            new LiveOpsEvent
            {
                TitleKey = "liveops.server_ddos", DescKey = "liveops.server_ddos_desc", Probability = 0.15f, Condition = "dau>10000",
                Options = new List<LiveOpsOption>
                {
                    new LiveOpsOption { LabelKey = "liveops.buy_protection", ResultKey = "liveops.buy_protection_result",
                        Effect = (metrics) => { metrics.OpCost += 5000; }, MoneyCost = 30000 },
                    new LiveOpsOption { LabelKey = "liveops.ignore_ddos", ResultKey = "liveops.ignore_ddos_result",
                        Effect = (metrics) => { metrics.NegativeRate += 0.1f; metrics.DAU *= 0.9f; } },
                }
            },
            new LiveOpsEvent
            {
                TitleKey = "liveops.bug_exploit", DescKey = "liveops.bug_exploit_desc", Probability = 0.2f, Condition = "dau>5000",
                Options = new List<LiveOpsOption>
                {
                    new LiveOpsOption { LabelKey = "liveops.emergency_patch", ResultKey = "liveops.emergency_patch_result",
                        Effect = (metrics) => { metrics.NegativeRate -= 0.05f; metrics.OpCost += 10000; }, MoneyCost = 20000 },
                    new LiveOpsOption { LabelKey = "liveops.rollback", ResultKey = "liveops.rollback_result",
                        Effect = (metrics) => { metrics.NegativeRate += 0.2f; metrics.DAU *= 0.7f; } },
                }
            },
            new LiveOpsEvent
            {
                TitleKey = "liveops.streamer_crash", DescKey = "liveops.streamer_crash_desc", Probability = 0.1f, Condition = "dau>2000",
                Options = new List<LiveOpsOption>
                {
                    new LiveOpsOption { LabelKey = "liveops.streamer_outreach", ResultKey = "liveops.streamer_outreach_result",
                        Effect = (metrics) => { metrics.DAU *= 1.15f; metrics.NegativeRate -= 0.03f; }, MoneyCost = 15000 },
                    new LiveOpsOption { LabelKey = "liveops.silent_fix", ResultKey = "liveops.silent_fix_result",
                        Effect = (metrics) => { metrics.NegativeRate += 0.05f; metrics.DAU *= 0.95f; } },
                }
            },
            new LiveOpsEvent
            {
                TitleKey = "liveops.competitor_launch", DescKey = "liveops.competitor_launch_desc", Probability = 0.12f, Condition = "dau>5000",
                Options = new List<LiveOpsOption>
                {
                    new LiveOpsOption { LabelKey = "liveops.free_update", ResultKey = "liveops.free_update_result",
                        Effect = (metrics) => { metrics.DAU *= 1.1f; metrics.OpCost += 20000; }, MoneyCost = 50000, TrustCost = 5 },
                    new LiveOpsOption { LabelKey = "liveops.price_drop", ResultKey = "liveops.price_drop_result",
                        Effect = (metrics) => { metrics.DAU *= 1.2f; metrics.ARPU *= 0.7f; } },
                    new LiveOpsOption { LabelKey = "liveops.ignore_comp", ResultKey = "liveops.ignore_comp_result",
                        Effect = (metrics) => { metrics.DAU *= 0.85f; } },
                }
            },
        };

        foreach (var evt in pool)
        {
            if (_rng.NextDouble() > evt.Probability) continue;
            if (evt.Condition == "dau>10000" && m.DAU < 10000) continue;
            if (evt.Condition == "dau>5000" && m.DAU < 5000) continue;
            if (evt.Condition == "dau>2000" && m.DAU < 2000) continue;

            ShowCrisis(gameName, evt);
            break; // 每月只触发一个
        }
    }

    private void ShowCrisis(string gameName, LiveOpsEvent evt)
    {
        var vp = _gm.GetViewport().GetVisibleRect().Size;
        var panel = new Panel { Position = new(vp.X * 0.2f, vp.Y * 0.25f), Size = new(vp.X * 0.6f, vp.Y * 0.5f) };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f, 0.97f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.8f, 0.5f, 0.2f, 0.7f), CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10 });
        _gm.UiLayer.AddChild(panel);

        float y = 15;
        var title = new Label { Text = $"📊 {Loc.Tr(evt.TitleKey)}", Position = new(20, y), Size = new(panel.Size.X - 40, 30) };
        title.AddThemeFontSizeOverride("font_size", 18); title.AddThemeColorOverride("font_color", new Color(0.7f, 0.3f, 0.1f));
        panel.AddChild(title); y += 40;

        var desc = new Label { Text = Loc.Tr(evt.DescKey), Position = new(20, y), Size = new(panel.Size.X - 40, 60),
            AutowrapMode = TextServer.AutowrapMode.Word };
        desc.AddThemeFontSizeOverride("font_size", 13); desc.AddThemeColorOverride("font_color", new Color(0.2f, 0.2f, 0.25f));
        panel.AddChild(desc); y += 80;

        foreach (var opt in evt.Options)
        {
            var btn = new Button { Text = Loc.Tr(opt.LabelKey), Position = new(20, y), Size = new(panel.Size.X - 40, 34) };
            btn.AddThemeFontSizeOverride("font_size", 13);
            var capturedOpt = opt;
            btn.Pressed += () =>
            {
                capturedOpt.Effect?.Invoke(GameMetrics[gameName]);
                if (capturedOpt.MoneyCost > 0) _res?.SpendMoney(capturedOpt.MoneyCost, "liveops");
                if (capturedOpt.TrustCost > 0) _devMgr.PlayerTrust = Mathf.Max(0, _devMgr.PlayerTrust - capturedOpt.TrustCost);
                panel.QueueFree();
                _gm.ShowToast("📊", Loc.Tr(capturedOpt.ResultKey), new Color(0.4f, 0.7f, 0.4f));
            };
            panel.AddChild(btn);
            y += 42;
        }
    }
}
