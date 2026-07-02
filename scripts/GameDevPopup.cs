using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 游戏开发弹出窗口 - 独立Popup处理开发全流程
/// </summary>
public class GameDevPopup
{
    private readonly GameManager _gm;
    private readonly ResourceManager _res;
    private readonly TeamManager _teamMgr;
    private readonly GameDevManager _devMgr;
    private readonly TechManager _techMgr;
    private readonly FanManager _fanMgr;
    private readonly TechDebtManager _debtMgr;
    private float S(float v) => v * _gm.UIScale;

    private Panel _panel;
    private VBoxContainer _content;
    private ScrollContainer _scroll;
    private GameGenre? _selectedGenre;
    private GameTheme? _selectedTheme;
    private Platform _selectedPlatform = Platform.PC;
    private MarketingStrategy _selectedMkt = MarketingStrategy.Normal;
    private float _selectedMonths = 12f;
    private float _selectedBudget = 50000f;
    private float _selectedScale = 0.5f;            // 规模 0~1
    private string _selectedIPName = "";             // IP系列名
    private PriceModel _selectedPrice = PriceModel.BuyToPlay;
    private float _selectedAdIntensity;              // 广告强度 0~1
    private GameProject _currentProj;                  // 外部传入：直接查看指定项目状态时设置
    private bool _readOnly;                            // 只读模式（进度查看）：仅展示参数，不显示操作按钮

    public GameDevPopup(GameManager gm)
    {
        _gm = gm;
        _res = gm.GetNode<ResourceManager>("ResourceManager");
        _teamMgr = gm.GetNode<TeamManager>("TeamManager");
        _devMgr = gm.GetNode<GameDevManager>("GameDevManager");
        _techMgr = gm.GetNode<TechManager>("TechManager");
        _fanMgr = gm.GetNode<FanManager>("FanManager");
        _debtMgr = gm.GetNode<TechDebtManager>("TechDebtManager");
    }

    public void Close() { if (_panel != null && GodotObject.IsInstanceValid(_panel)) { _panel.QueueFree(); _panel = null; } }

    /// <summary>直接从外部打开指定项目的开发状态页（不经过创建立项流程）</summary>
    public void ShowProjectStatus(GameProject proj)
    {
        Show();
        // 跳过 Step0/1/2，直接进入状态面板。_currentProj 用于 RenderStep3 选中目标项目
        _readOnly = true;
        _currentProj = proj;
        RenderStep3_Status();
    }

    public void Show()
    {
        var vp = _gm.GetViewport().GetVisibleRect().Size;
        float margin = S(40);

        _panel = new Panel
        {
            Position = new(margin, margin),
            Size = new(vp.X - margin * 2, vp.Y - margin * 2),
        };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.97f, 0.96f, 0.94f, 0.98f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.2f, 0.4f, 0.8f, 0.6f),
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8
        });

        var title = new Label { Text = Loc.Tr("devpop.title"), Position = new(S(20), S(10)), Size = new(_panel.Size.X - S(80), S(30)) };
        title.AddThemeFontSizeOverride("font_size", 22);
        title.AddThemeColorOverride("font_color", new Color(0.15f, 0.4f, 0.7f));
        _panel.AddChild(title);

        var closeBtn = new Button { Text = "✕", Position = new(_panel.Size.X - S(50), S(8)), Size = new(S(35), S(30)), Flat = true };
        closeBtn.AddThemeFontSizeOverride("font_size", 18);
        closeBtn.AddThemeColorOverride("font_color", new Color(0.8f, 0.3f, 0.3f));
        closeBtn.AddThemeColorOverride("font_hover_color", new Color(0.45f, 0.45f, 0.45f));
        closeBtn.AddThemeColorOverride("font_pressed_color", new Color(0.3f, 0.3f, 0.3f));
        closeBtn.Pressed += () => _gm.CloseAll();
        _panel.AddChild(closeBtn);

        _scroll = new ScrollContainer { Position = new(S(20), S(50)), Size = new(_panel.Size.X - S(40), _panel.Size.Y - S(60)) };
        _scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        _content = new VBoxContainer();
        _scroll.AddChild(_content);
        _panel.AddChild(_scroll);

        _gm.PushPanel(_panel);
    }

    private void ClearContent()
    {
        foreach (var c in _content.GetChildren()) c.QueueFree();
    }

    private Label MakeLabel(string text, float fs, Color color, HorizontalAlignment ha = HorizontalAlignment.Left)
    {
        var l = new Label { Text = text, HorizontalAlignment = ha };
        l.AddThemeFontSizeOverride("font_size", (int)fs);
        l.AddThemeColorOverride("font_color", color);
        return l;
    }

    private Button MakeBtn(string text, float w, float h, int fontSize = 14)
    {
        var b = new Button { Text = text, CustomMinimumSize = new(S(w), S(h)), Size = new(S(w), S(h)) };
        b.AddThemeFontSizeOverride("font_size", fontSize);
        return b;
    }

    // ===== Step 0: 选择项目 =====
    public void RenderStep0_SelectProject()
    {
        ClearContent();
        _content.AddChild(MakeLabel(Loc.Tr("dev.select"), 16, new Color(0.18f, 0.20f, 0.25f)));

        var grid = new GridContainer { Columns = 5 };
        var unlockedGenres = GetUnlockedGenres();

        foreach (var genre in unlockedGenres)
        {
            var g = genre;
            var btn = new Button { Text = g.Name(), CustomMinimumSize = new(S(110), S(32)), Size = new(S(110), S(32)) };
            btn.AddThemeFontSizeOverride("font_size", 12);

            if (_selectedGenre == g)
            {
                btn.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.6f, 0.7f, 0.9f, 0.8f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
            }
            btn.Pressed += () => { _selectedGenre = g; RenderStep0_SelectProject(); };
            grid.AddChild(btn);
        }
        _content.AddChild(grid);

        _content.AddChild(MakeLabel("", 8, Colors.White));

        var grid2 = new GridContainer { Columns = 5 };
        var unlockedThemes = GetUnlockedThemes();

        foreach (var theme in unlockedThemes)
        {
            var t = theme;
            var btn = new Button { Text = t.Name(), CustomMinimumSize = new(S(110), S(32)), Size = new(S(110), S(32)) };
            btn.AddThemeFontSizeOverride("font_size", 12);

            if (_selectedTheme == t)
            {
                btn.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.9f, 0.7f, 0.6f, 0.8f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
            }
            btn.Pressed += () => { _selectedTheme = t; RenderStep0_SelectProject(); };
            grid2.AddChild(btn);
        }
        _content.AddChild(grid2);

        if (_selectedGenre != null && _selectedTheme != null)
        {
            _content.AddChild(MakeLabel(Loc.TrF("dev.selected", _selectedGenre.Value.Name(), _selectedTheme.Value.Name()), 15, new Color(0.10f, 0.14f, 0.22f)));
            var nextBtn = MakeBtn(Loc.Tr("dev.next_step"), 200, 36);
            nextBtn.Pressed += () => { RenderStep1_Planning(); };
            _content.AddChild(nextBtn);
        }
    }

    private List<GameGenre> GetUnlockedGenres()
    {
        var list = new List<GameGenre>(GameInitialUnlocks.StartGenres);
        var map = new Dictionary<string, GameGenre>
        {
            ["rac"] = GameGenre.RAC, ["sim"] = GameGenre.SIM,
            ["spo"] = GameGenre.SPO, ["mus"] = GameGenre.MUS,
            ["ftg"] = GameGenre.FTG, ["moba"] = GameGenre.MOBA,
            ["mmo"] = GameGenre.MMO, ["rts"] = GameGenre.RTS,
            ["hor"] = GameGenre.HOR, ["san"] = GameGenre.SAN,
            ["rog"] = GameGenre.ROG, ["vis"] = GameGenre.VIS,
            ["pzl"] = GameGenre.PZL,
        };
        foreach (var kv in _techMgr.ResearchedTech)
        {
            if (kv.Value && kv.Key.StartsWith("genre_"))
            {
                string suffix = kv.Key.Replace("genre_", "");
                if (map.TryGetValue(suffix, out var g) && !list.Contains(g))
                    list.Add(g);
            }
        }
        // 新手教程过滤
        var tutMgr = _gm.GetNode<TutorialManager>("TutorialManager");
        if (tutMgr != null)
            list = list.Where(g => tutMgr.GetAvailableGenres().Contains(g)).ToList();
        return list;
    }

    private List<GameTheme> GetUnlockedThemes()
    {
        var list = new List<GameTheme>(GameInitialUnlocks.StartThemes);
        // 技术ID → 枚举 映射表
        var map = new Dictionary<string, GameTheme>
        {
            ["cyber"] = GameTheme.Cyberpunk, ["steam"] = GameTheme.Steampunk,
            ["horror"] = GameTheme.Horror, ["comedy"] = GameTheme.Comedy,
            ["romance"] = GameTheme.Romance, ["war"] = GameTheme.War,
            ["mystery"] = GameTheme.Mystery, ["school"] = GameTheme.School,
            ["myth"] = GameTheme.Myth, ["western"] = GameTheme.Western,
            ["space"] = GameTheme.Space, ["fantasy"] = GameTheme.Fantasy,
            ["scifi"] = GameTheme.SciFi, ["modern"] = GameTheme.Modern,
            ["historical"] = GameTheme.Historical, ["postapoc"] = GameTheme.PostApoc,
        };
        foreach (var kv in _techMgr.ResearchedTech)
        {
            if (kv.Value && kv.Key.StartsWith("theme_"))
            {
                string suffix = kv.Key.Replace("theme_", "");
                if (map.TryGetValue(suffix, out var t) && !list.Contains(t))
                    list.Add(t);
            }
        }
        // 新手教程过滤
        var tutMgr2 = _gm.GetNode<TutorialManager>("TutorialManager");
        if (tutMgr2 != null)
            list = list.Where(t => tutMgr2.GetAvailableThemes().Contains(t)).ToList();
        return list;
    }

    private static float GetCompatibility(GameGenre genre, GameTheme theme)
    {
        var dict = new Dictionary<(GameGenre, GameTheme), float>
        {
            [(GameGenre.RPG, GameTheme.Fantasy)] = 1.0f,
            [(GameGenre.RPG, GameTheme.SciFi)] = 0.85f,
            [(GameGenre.RPG, GameTheme.Historical)] = 0.8f,
            [(GameGenre.RPG, GameTheme.Myth)] = 0.95f,
            [(GameGenre.ACT, GameTheme.Fantasy)] = 0.85f,
            [(GameGenre.ACT, GameTheme.PostApoc)] = 0.9f,
            [(GameGenre.FPS, GameTheme.Modern)] = 0.95f,
            [(GameGenre.FPS, GameTheme.War)] = 1.0f,
            [(GameGenre.FPS, GameTheme.SciFi)] = 0.9f,
            [(GameGenre.FPS, GameTheme.Cyberpunk)] = 0.95f,
            [(GameGenre.HOR, GameTheme.Horror)] = 1.0f,
            [(GameGenre.HOR, GameTheme.Mystery)] = 0.9f,
            [(GameGenre.SLG, GameTheme.Historical)] = 0.9f,
            [(GameGenre.SLG, GameTheme.War)] = 1.0f,
            [(GameGenre.AVG, GameTheme.Fantasy)] = 0.9f,
            [(GameGenre.AVG, GameTheme.Mystery)] = 0.85f,
            [(GameGenre.RAC, GameTheme.Modern)] = 0.95f,
            [(GameGenre.SPO, GameTheme.Modern)] = 0.95f,
            [(GameGenre.MUS, GameTheme.Comedy)] = 0.85f,
            [(GameGenre.FTG, GameTheme.Fantasy)] = 0.85f,
            [(GameGenre.MOBA, GameTheme.Fantasy)] = 0.9f,
            [(GameGenre.MMO, GameTheme.Fantasy)] = 0.95f,
            [(GameGenre.RTS, GameTheme.SciFi)] = 0.9f,
            [(GameGenre.RTS, GameTheme.War)] = 0.95f,
            [(GameGenre.SAN, GameTheme.Modern)] = 0.95f,
            [(GameGenre.SAN, GameTheme.Space)] = 0.9f,
            [(GameGenre.PZL, GameTheme.Comedy)] = 0.85f,
            [(GameGenre.VIS, GameTheme.Romance)] = 0.9f,
            [(GameGenre.VIS, GameTheme.School)] = 0.85f,
        };
        return dict.TryGetValue((genre, theme), out var v) ? v : 0.4f + ((int)genre % 5) * 0.08f;
    }

    // ===== Step 1: 立项详情 =====
    private CheckBox _reuseCheck;
    private bool _canReuse;

    private void RenderStep1_Planning()
    {
        ClearContent();
        _content.AddChild(MakeLabel(Loc.TrF("devpop.plan_title", _selectedGenre.Value.Name(), _selectedTheme.Value.Name()), 18, new Color(0.10f, 0.14f, 0.22f)));

        // 复用前作代码
        _canReuse = false;
        if (_devMgr.CompletedProjects.Count > 0)
        {
            foreach (var p in _devMgr.CompletedProjects)
                if (p.Genre == _selectedGenre!.Value || p.Theme == _selectedTheme!.Value)
                { _canReuse = true; break; }
        }
        if (_canReuse)
        {
            _reuseCheck = new CheckBox { Text = Loc.Tr("dev.reuse_code") };
            _reuseCheck.AddThemeFontSizeOverride("font_size", 12);
            _reuseCheck.AddThemeColorOverride("font_color", new Color(1f, 0.7f, 0.3f));
            _reuseCheck.AddThemeColorOverride("font_hover_color", new Color(0.40f, 0.40f, 0.40f));
            _reuseCheck.AddThemeColorOverride("icon_hover_color", new Color(0.40f, 0.40f, 0.40f));
            _content.AddChild(_reuseCheck);
        }

        // 平台选择
        var platRow = new HBoxContainer();
        platRow.AddChild(MakeLabel(Loc.Tr("dev.platform") + ":", 14, new Color(0.18f, 0.20f, 0.25f)));
        var platOpt = new OptionButton();
        foreach (Platform p in System.Enum.GetValues<Platform>())
        {
            bool canUse = p == Platform.PC || (p == Platform.Console && _techMgr.IsResearched("cross_v1")) || (p == Platform.All && _techMgr.IsResearched("cross_v2"));
            platOpt.AddItem(p.Name());
            if (!canUse) platOpt.SetItemDisabled(platOpt.ItemCount - 1, true);
        }
        platOpt.Selected = (int)_selectedPlatform;
        platOpt.ItemSelected += (long i) => _selectedPlatform = (Platform)i;
        platRow.AddChild(platOpt);
        _content.AddChild(platRow);

        // 预计月份
        var monthsRow = new HBoxContainer();
        monthsRow.AddChild(MakeLabel(Loc.Tr("dev.est_months") + ":", 14, new Color(0.18f, 0.20f, 0.25f)));
        var monthsSpin = new SpinBox { MinValue = 1, MaxValue = 60, Value = _selectedMonths };
        monthsSpin.ValueChanged += (v) => _selectedMonths = (float)v;
        monthsRow.AddChild(monthsSpin);
        _content.AddChild(monthsRow);

        // 宣发策略
        var mktRow = new HBoxContainer();
        mktRow.AddChild(MakeLabel(Loc.Tr("devpop.marketing"), 14, new Color(0.18f, 0.20f, 0.25f)));
        var mktOpt = new OptionButton();
        foreach (MarketingStrategy m in System.Enum.GetValues<MarketingStrategy>())
            mktOpt.AddItem(m.Name());
        mktOpt.Selected = (int)_selectedMkt;
        mktOpt.ItemSelected += (long i) => _selectedMkt = (MarketingStrategy)i;
        mktRow.AddChild(mktOpt);
        _content.AddChild(mktRow);

        // 预算（输入框）
        var budgetRow = new HBoxContainer();
        budgetRow.AddChild(MakeLabel(Loc.Tr("devpop.mkt_budget"), 14, new Color(0.18f, 0.20f, 0.25f)));
        var budgetInput = new LineEdit { Text = _selectedBudget.ToString("F0"), CustomMinimumSize = new(100, 0) };
        budgetInput.TextSubmitted += (s) => {
            if (long.TryParse(s, out var v)) _selectedBudget = Mathf.Clamp(v, 0, 5000000);
            budgetInput.Text = _selectedBudget.ToString("F0");
        };
        budgetInput.FocusExited += () => {
            if (long.TryParse(budgetInput.Text, out var v)) _selectedBudget = Mathf.Clamp(v, 0, 5000000);
            budgetInput.Text = _selectedBudget.ToString("F0");
        };
        budgetRow.AddChild(budgetInput);
        _content.AddChild(budgetRow);

        // ── 项目规模滑块 ──
        var scaleRow = new HBoxContainer();
        var scaleLabel = MakeLabel(Loc.TrF("devpop.scale_fmt", _selectedScale * 100f, Mathf.Lerp(5, 60, _selectedScale)), 14, new Color(0.18f, 0.20f, 0.25f));
        scaleRow.AddChild(scaleLabel);
        var scaleSlider = new HSlider { MinValue = 0, MaxValue = 1, Value = (_selectedScale - 0.1f) / 0.9f, Step = 0.01, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(S(100), S(16)) };
        scaleSlider.ValueChanged += (v) => { _selectedScale = Mathf.Clamp((float)v * 0.9f + 0.1f, 0.1f, 1f); scaleLabel.Text = Loc.TrF("devpop.scale_fmt", _selectedScale * 100f, Mathf.Lerp(5, 60, _selectedScale)); };
        scaleRow.AddChild(scaleSlider);
        _content.AddChild(scaleRow);
        _content.AddChild(MakeLabel(Loc.Tr("devpop.scale_hint"), 11, new Color(0.35f, 0.38f, 0.42f)));

        // ── IP名称（留空=新IP） ──
        var ipRow = new HBoxContainer();
        ipRow.AddChild(MakeLabel(Loc.Tr("devpop.ip_series"), 14, new Color(0.18f, 0.20f, 0.25f)));
        var ipInput = new LineEdit { Text = _selectedIPName, PlaceholderText = Loc.Tr("dev.ip_placeholder"), CustomMinimumSize = new(200, 0) };
        ipInput.TextChanged += (s) => _selectedIPName = s.Trim();
        ipRow.AddChild(ipInput);
        _content.AddChild(ipRow);
        _content.AddChild(MakeLabel(Loc.Tr("devpop.ip_hint"), 11, new Color(0.35f, 0.38f, 0.42f)));

        // ── 付费模式 ──
        var priceRow = new HBoxContainer();
        priceRow.AddChild(MakeLabel(Loc.Tr("dev.price_model") + ":", 14, new Color(0.18f, 0.20f, 0.25f)));
        var priceOpt = new OptionButton();
        priceOpt.AddItem(Loc.Tr("dev.buy_to_play"));
        priceOpt.AddItem(Loc.Tr("dev.free_play"));
        priceOpt.Selected = _selectedPrice == PriceModel.BuyToPlay ? 0 : 1;
        priceOpt.ItemSelected += (long i) => { _selectedPrice = i == 0 ? PriceModel.BuyToPlay : PriceModel.Free; };
        priceRow.AddChild(priceOpt);
        _content.AddChild(priceRow);

        // ── 广告/内购强度（仅免费游戏显示）──
        var adRow = new HBoxContainer();
        var adLabel = MakeLabel(Loc.Tr("dev.ad_intensity") + $" {_selectedAdIntensity * 100:F0}%", 14, new Color(0.18f, 0.20f, 0.25f));
        adRow.AddChild(adLabel);
        var adSlider = new HSlider { MinValue = 0, MaxValue = 1, Value = _selectedAdIntensity, Step = 0.05, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(S(100), S(16)) };
        adSlider.ValueChanged += (v) => { _selectedAdIntensity = (float)v; adLabel.Text = Loc.Tr("dev.ad_intensity") + $" {_selectedAdIntensity * 100:F0}%"; };
        adRow.AddChild(adSlider);
        if (_selectedPrice == PriceModel.Free) _content.AddChild(adRow);
        _content.AddChild(MakeLabel(_selectedPrice == PriceModel.Free ? Loc.Tr("dev.ad_hint") : Loc.Tr("dev.buy_hint"), 11, new Color(0.35f, 0.38f, 0.42f)));

        // ── 开发资源分配 ──
        _content.AddChild(MakeLabel(Loc.Tr("devmenu.res_alloc"), 14, new Color(0.10f, 0.14f, 0.22f)));
        float sG = 0.33f, sA = 0.33f, sP = 0.34f;
        Label aLabel2 = null!, pLabel2 = null!; HSlider aS = null!;
        var gRow = new HBoxContainer();
        var gLabel = MakeLabel($"图形: {sG*100:F0}%", 13, new Color(0.2f, 0.4f, 0.8f));
        gRow.AddChild(gLabel);
        var gS = new HSlider { MinValue = 0, MaxValue = 1, Value = sG, Step = 0.01, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(S(80), S(16)) };
        gS.ValueChanged += (v) => {
            sG = (float)v; sA = Mathf.Min(sA, 1 - sG); sP = 1 - sG - sA;
            gLabel.Text = $"图形: {sG*100:F0}%"; aLabel2.Text = $"音效: {sA*100:F0}%"; pLabel2.Text = $"玩法: {sP*100:F0}%";
            aS.MaxValue = 1 - sG - 0.01f; aS.Value = Mathf.Min((float)aS.Value, (float)aS.MaxValue);
        };
        gRow.AddChild(gS); _content.AddChild(gRow);

        var aRow = new HBoxContainer();
        aLabel2 = MakeLabel($"音效: {sA*100:F0}%", 13, new Color(0.5f, 0.3f, 0.8f));
        aRow.AddChild(aLabel2);
        aS = new HSlider { MinValue = 0, MaxValue = 1 - sG - 0.01f, Value = sA, Step = 0.01, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(S(80), S(16)) };
        aS.ValueChanged += (v) => { sA = (float)v; sP = 1 - sG - sA; aLabel2.Text = $"音效: {sA*100:F0}%"; pLabel2.Text = $"玩法: {sP*100:F0}%"; };
        aRow.AddChild(aS); _content.AddChild(aRow);

        var pRow = new HBoxContainer();
        pLabel2 = MakeLabel($"玩法: {sP*100:F0}%", 13, new Color(0.8f, 0.3f, 0.2f));
        pRow.AddChild(pLabel2);
        pRow.AddChild(MakeLabel(Loc.Tr("devmenu.auto"), 10, new Color(0.15f, 0.15f, 0.15f)));
        _content.AddChild(pRow);

        var btnRow = new HBoxContainer();
        var backBtn = MakeBtn(Loc.Tr("dev.back"), 100, 36);
        backBtn.Pressed += () => { RenderStep0_SelectProject(); };
        btnRow.AddChild(backBtn);

        var createBtn = MakeBtn(Loc.Tr("dev.create_assign"), 180, 36);
        createBtn.Pressed += () =>
        {
            float months = _selectedMonths;
            // 查询同IP前作数据
            float predScore = 0;
            int predSales = 0;
            string ipName = _selectedIPName;
            if (!string.IsNullOrWhiteSpace(ipName))
            {
                var pred = _devMgr.CompletedProjects.Where(p => p.IPName == ipName)
                    .OrderByDescending(p => p.FinalScore).FirstOrDefault();
                if (pred != null) { predScore = pred.FinalScore; predSales = pred.Sales; }
            }
            var proj = _devMgr.CreateProject(
                Loc.TrF("devpop.proj_name", "", System.Guid.NewGuid().ToString()[..4]),
                _selectedGenre!.Value, _selectedTheme!.Value, _selectedPlatform,
                months, _selectedMkt, _selectedBudget,
                _selectedScale, _selectedPrice, _selectedAdIntensity,
                ipName: ipName, predScore: predScore, predSales: predSales,
                budgetGraphics: sG, budgetAudio: sA, budgetGameplay: sP);
            if (proj != null)
            {
                if (_canReuse && _reuseCheck != null && _reuseCheck.ButtonPressed)
                {
                    _debtMgr.ApplyCodeReuse(proj);
                }
                RenderStep2_AssignTeam();
            }
        };
        btnRow.AddChild(createBtn);
        _content.AddChild(btnRow);
    }

    // ===== Step 2: 分配团队 =====
    /// <summary>从项目面板直接跳转到团队分配</summary>
    public void ShowAssignTeam(GameProject targetProj)
    {
        Show();
        ClearContent();
        _content.AddChild(MakeLabel(Loc.TrF("devpop.pending_fmt", targetProj.Name, targetProj.Genre.Name(), targetProj.Theme.Name(), targetProj.Platform.Name()), 16, new Color(0.10f, 0.14f, 0.22f)));
        _content.AddChild(MakeLabel(Loc.Tr("dev.assign_team"), 14, new Color(0.3f, 0.5f, 0.7f)));
        _content.AddChild(MakeLabel("", 6, Colors.White));

        var idleTeams = _teamMgr.Teams.FindAll(t => t.Task == TeamTask.None);
        if (idleTeams.Count == 0)
        {
            _content.AddChild(MakeLabel(Loc.Tr("dev.no_idle_team"), 14, new Color(0.9f, 0.3f, 0.3f)));
        }
        else
        {
            foreach (var team in idleTeams)
            {
                var row = new HBoxContainer();
                row.AddChild(MakeLabel(Loc.TrF("dev.team_row", team.Name, team.Members.Count), 14, new Color(0.18f, 0.20f, 0.25f)));
                foreach (var emp in team.Members)
                {
                    string skills = "";
                    foreach (var sk in emp.Skills) skills += $"{sk.Key.Name()}Lv{sk.Value.Level} ";
                    row.AddChild(MakeLabel($"{emp.Name}({skills.Trim()}) ", 11, new Color(0.5f, 0.6f, 0.7f)));
                }
                var assignBtn = MakeBtn(Loc.Tr("dev.assign_dev"), 100, 30);
                var capturedTeam = team;
                assignBtn.Pressed += () =>
                {
                    if (_devMgr.StartDevelopment(targetProj, capturedTeam))
                    {
                        ShowProjectStatus(targetProj);
                    }
                };
                row.AddChild(assignBtn);
                _content.AddChild(row);
            }
        }

        var backBtn = MakeBtn(Loc.Tr("dev.back"), 100, 36);
        backBtn.Pressed += () => { _gm.CloseAll(); _gm.RefreshHUD(); };
        _content.AddChild(backBtn);
    }

    public void RenderStep2_AssignTeam()
    {
        ClearContent();
        _content.AddChild(MakeLabel(Loc.Tr("dev.assign_team"), 18, new Color(0.10f, 0.14f, 0.22f)));

        var pending = _devMgr.Projects.FindAll(p => p.Phase == DevPhase.Planning);
        foreach (var proj in pending)
        {
            _content.AddChild(MakeLabel(Loc.TrF("devpop.pending_fmt", proj.Name, proj.Genre.Name(), proj.Theme.Name(), proj.Platform.Name()), 14, new Color(0.15f, 0.45f, 0.15f)));
        }

        _content.AddChild(MakeLabel("", 8, Colors.White));

        var idleTeams = _teamMgr.Teams.FindAll(t => t.Task == TeamTask.None);
        if (idleTeams.Count == 0)
        {
            _content.AddChild(MakeLabel(Loc.Tr("dev.no_idle_team"), 14, new Color(0.9f, 0.3f, 0.3f)));
        }
        else
        {
            foreach (var team in idleTeams)
            {
                var row = new HBoxContainer();
                row.AddChild(MakeLabel(Loc.TrF("dev.team_row", team.Name, team.Members.Count), 14, new Color(0.18f, 0.20f, 0.25f)));
                foreach (var emp in team.Members)
                {
                    string skills = "";
                    foreach (var sk in emp.Skills) skills += $"{sk.Key.Name()}Lv{sk.Value.Level} ";
                    row.AddChild(MakeLabel($"{emp.Name}({skills.Trim()}) ", 11, new Color(0.5f, 0.6f, 0.7f)));
                }
                var assignBtn = MakeBtn(Loc.Tr("dev.assign_dev"), 100, 30);
                assignBtn.Pressed += () =>
                {
                    var proj = pending.Count > 0 ? pending[0] : null;
                    if (proj != null && _devMgr.StartDevelopment(proj, team))
                    {
                        RenderStep3_Status();
                    }
                };
                row.AddChild(assignBtn);
                _content.AddChild(row);
            }
        }

        var backBtn = MakeBtn(Loc.Tr("dev.back"), 100, 36);
        backBtn.Pressed += () => { _gm.CloseAll(); _gm.RefreshHUD(); };
        _content.AddChild(backBtn);
    }

    // ===== Step 3: 项目状态 =====
    private void RenderStep3_Status()
    {
        ClearContent();
        _content.AddChild(MakeLabel(Loc.Tr("dev.project_status"), 18, new Color(0.10f, 0.14f, 0.22f)));

        var activeProjects = _currentProj != null ? new List<GameProject> { _currentProj } : _devMgr.Projects.FindAll(p => !p.IsReleased);
        _currentProj = null;

        if (_readOnly)
        {
            foreach (var proj in activeProjects) RenderReadOnlySpec(proj);
        }
        else
        {
            foreach (var proj in activeProjects) RenderActiveProjectPanel(proj);
        }

        if (_devMgr.Projects.FindAll(p => !p.IsReleased).Count == 0)
            _content.AddChild(MakeLabel(Loc.Tr("dev.no_active"), 14, new Color(0.35f, 0.38f, 0.42f)));

        var closeBtn = MakeBtn(Loc.Tr("dev.close"), 100, 36);
        closeBtn.Pressed += () => _gm.CloseAll();
        _content.AddChild(closeBtn);
    }

    /// ── 只读参数一览（进度查看入口）──
    private void RenderReadOnlySpec(GameProject proj)
    {
        var c = new Color(0.08f, 0.12f, 0.20f); // 标题色
        var v = new Color(0.16f, 0.19f, 0.24f); // 值色
        var d = new Color(0.28f, 0.31f, 0.36f); // 次级色
        var btnColor = new Color(0.55f, 0.55f, 0.55f);
        int fs = 12;

        // 页眉
        string phase = proj.Phase switch
        {
            DevPhase.Developing => Loc.TrF("dev.dev_progress", proj.DevProgress * 100),
            DevPhase.Polishing => Loc.TrF("dev.polish_progress", proj.BugCount),
            _ => proj.Phase.Name()
        };
        _content.AddChild(MakeLabel($"▎{proj.Name}  [{proj.Genre.Name()}×{proj.Theme.Name()}]", 15, c));
        _content.AddChild(MakeLabel(phase, 12, new Color(0.15f, 0.45f, 0.15f)));
        _content.AddChild(MakeLabel("", S(4), Colors.Transparent));

        // ══ 1. 基本配置 ══
        _content.AddChild(MakeLabel("══ " + Loc.Tr("dev.ro_basic") + " ══", 13, c));
        _content.AddChild(MakeLabel(Loc.TrF("dev.ro_engine", string.IsNullOrEmpty(proj.EngineName) ? Loc.Tr("dev.ro_none") : proj.EngineName), fs, v));
        _content.AddChild(MakeLabel(Loc.TrF("dev.ro_platform", proj.Platform.Name()), fs, v));
        _content.AddChild(MakeLabel(Loc.TrF("dev.ro_scale", proj.Scale * 100), fs, v));
        _content.AddChild(MakeLabel(Loc.TrF("dev.ro_progress", proj.DevProgress * 100), fs, v));
        float bSum = proj.BudgetGraphics + proj.BudgetAudio + proj.BudgetGameplay;
        if (bSum < 0.01f) bSum = 1f;
        _content.AddChild(MakeLabel(Loc.TrF("dev.ro_res_alloc", proj.BudgetGraphics / bSum * 100, proj.BudgetAudio / bSum * 100, proj.BudgetGameplay / bSum * 100), fs, v));

        // ══ 2. 商业化配置 ══
        _content.AddChild(MakeLabel("══ " + Loc.Tr("dev.ro_monet") + " ══", 13, c));
        string priceStr = proj.PriceModel switch
        {
            PriceModel.Free => Loc.Tr("dev.ro_price_free"),
            PriceModel.BuyToPlay => Loc.TrF("dev.ro_price_buy", proj.SuggestedPrice),
            _ => proj.SuggestedPrice.ToString()
        };
        _content.AddChild(MakeLabel(Loc.TrF("dev.ro_price_model", priceStr), fs, v));
        _content.AddChild(MakeLabel(Loc.TrF("dev.ro_ad_intensity", proj.AdIntensity * 100), fs, v));
        _content.AddChild(MakeLabel(Loc.TrF("dev.ro_mkt_strat", proj.Marketing.Name(), proj.MarketingBudget), fs, v));
        _content.AddChild(MakeLabel(Loc.TrF("dev.ro_plat_params", proj.PlatformMemoryLimit, proj.PlatformFpsTarget), fs, d));

        // ══ 3. 六大模块进度 ══
        _content.AddChild(MakeLabel("══ " + Loc.Tr("dev.quad_content") + " ══", 13, c));
        AddModuleBar(Loc.Tr("dev.core_gameplay"), proj.ModuleProgressCore, SkillType.Program);
        AddModuleBar(Loc.Tr("dev.visual"), proj.ModuleProgressVisual, SkillType.Art);
        AddModuleBar(Loc.Tr("dev.audio_design"), proj.ModuleProgressAudio, SkillType.Audio);
        AddModuleBar(Loc.Tr("dev.story_text"), proj.ModuleProgressStory, SkillType.Program);
        AddModuleBar(Loc.Tr("dev.program_stable"), proj.ModuleProgressStability, SkillType.Program);
        AddModuleBar(Loc.Tr("dev.online_service"), proj.ModuleProgressOnline, SkillType.Network);
        float estRaw = proj.GraphicsScore * 0.2f + proj.GameplayScore * 0.3f + proj.AudioScore * 0.1f + proj.StoryScore * 0.15f + proj.NetworkScore * 0.1f + proj.StabilityScore * 0.15f;
        float est = estRaw * (0.9f + proj.Scale * 0.2f) - proj.BugCount * 0.3f;
        string estGrade = est >= 85 ? "A" : est >= 70 ? "B" : est >= 50 ? "C" : "D";
        _content.AddChild(MakeLabel(Loc.TrF("dev.est_score", Mathf.Clamp(est, 0, 100), estGrade), fs, new Color(0.35f, 0.50f, 0.65f)));

        // ══ 4. 技术指标 ══
        _content.AddChild(MakeLabel("══ " + Loc.Tr("dev.quad_tech") + " ══", 13, c));
        float memPct = Mathf.Clamp(proj.MemoryUsage / proj.PlatformMemoryLimit, 0, 1.5f);
        Color mc = memPct > 1f ? new Color(0.9f, 0.1f, 0.1f) : memPct > 0.8f ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.2f, 0.65f, 0.3f);
        _content.AddChild(MakeLabel(Loc.TrF("dev.mem_gauge", proj.MemoryUsage, proj.PlatformMemoryLimit), fs, mc));
        Color fc = proj.FpsBelowTarget ? new Color(0.9f, 0.1f, 0.1f) : proj.FpsEstimate < 45 ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.2f, 0.65f, 0.3f);
        _content.AddChild(MakeLabel(Loc.TrF("dev.fps_label", proj.FpsEstimate, proj.PlatformFpsTarget), fs, fc));
        float cp = proj.CrashRate * 100;
        Color cc = cp > 30 ? new Color(0.9f, 0.1f, 0.1f) : cp > 15 ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.2f, 0.65f, 0.3f);
        _content.AddChild(MakeLabel(Loc.TrF("dev.crash_label", cp), fs, cc));
        string psStr = proj.PlatformStressLevel == "danger" ? Loc.Tr("dev.plat_fail") : proj.PlatformStressLevel == "warn" ? Loc.Tr("dev.plat_warn") : Loc.Tr("dev.plat_ok");
        Color pc = proj.PlatformStressLevel == "danger" ? new Color(0.9f, 0.2f, 0.2f) : proj.PlatformStressLevel == "warn" ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.2f, 0.65f, 0.3f);
        _content.AddChild(MakeLabel(Loc.TrF("dev.plat_status", proj.Platform.Name(), psStr), fs, pc));

        // ══ 5. 技术债务 ══
        _content.AddChild(MakeLabel("══ " + Loc.Tr("dev.quad_risk") + " ══", 13, c));
        Color dt = proj.TechDebt > 50 ? new Color(0.75f, 0.15f, 0.05f) : proj.TechDebt > 30 ? new Color(0.7f, 0.45f, 0.05f) : v;
        _content.AddChild(MakeLabel(Loc.TrF("dev.debt_val", proj.TechDebt), fs, dt));
        if (proj.TechDebt > 30)
            _content.AddChild(MakeLabel(Loc.TrF("dev.debt_interest", proj.NextMonthBugFromDebt, proj.NextMonthSlowFromDebt * 100), fs, new Color(0.8f, 0.3f, 0.1f)));
        if (proj.TechDebt > 60)
            _content.AddChild(MakeLabel(Loc.Tr("dev.spaghetti"), fs, new Color(0.9f, 0.15f, 0.1f)));

        // ── 996 冲刺按钮（债务>50解锁）──
        if (proj.Phase == DevPhase.Developing && _debtMgr.LeverageUnlocked)
        {
            bool crunch = _debtMgr.CrunchMode;
            var crunchBtn = new Button
            {
                Text = crunch ? Loc.Tr("debt.crunch_on") : Loc.Tr("debt.crunch_off"),
                Flat = true,
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
                CustomMinimumSize = new(0, 28),
            };
            crunchBtn.AddThemeFontSizeOverride("font_size", 13);
            crunchBtn.AddThemeColorOverride("font_color", crunch ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 0.6f, 0.15f));
            crunchBtn.AddThemeColorOverride("font_hover_color", new Color(1f, 0.2f, 0.2f));
            crunchBtn.Pressed += () =>
            {
                _debtMgr.CrunchMode = !_debtMgr.CrunchMode;
                bool now = _debtMgr.CrunchMode;
                crunchBtn.Text = now ? Loc.Tr("debt.crunch_on") : Loc.Tr("debt.crunch_off");
                crunchBtn.AddThemeColorOverride("font_color", now ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 0.6f, 0.15f));
                _gm.ShowToast(
                    now ? Loc.Tr("debt.crunch_started") : Loc.Tr("debt.crunch_stopped"),
                    now ? Loc.Tr("debt.crunch_started_msg") : Loc.Tr("debt.crunch_stopped_msg"),
                    now ? new Color(0.9f, 0.3f, 0.1f) : new Color(0.3f, 0.7f, 0.3f));
            };
            _content.AddChild(crunchBtn);
            if (crunch)
                _content.AddChild(MakeLabel(Loc.TrF("debt.crunch_active", _debtMgr.ActiveCrunchMonths), 11, new Color(0.9f, 0.2f, 0.1f)));
        }

        // ══ 6. 灵感 & 碎片 ══
        _content.AddChild(MakeLabel("══ " + Loc.Tr("dev.quad_res") + " ══", 13, c));
        _content.AddChild(MakeLabel(Loc.TrF("dev.inspire_cost", _res.Inspiration), fs, new Color(0.3f, 0.35f, 0.4f)));
        var storyEvt = _gm.GetNode<StoryEvents>("StoryEvents");
        if (storyEvt != null && storyEvt.Fragments.Count > 0)
            _content.AddChild(MakeLabel(Loc.TrF("dev.frag_count", storyEvt.Fragments.Count, storyEvt.Fragments[0].Bonus), fs, new Color(0.7f, 0.3f, 0.6f)));

        // ══ 7. 打磨 / QA / 发售 ══
        if (proj.Phase == DevPhase.Polishing || proj.Phase == DevPhase.Testing || proj.Phase == DevPhase.ReadyToRelease)
        {
            _content.AddChild(MakeLabel("══ " + Loc.Tr("dev.ro_polish") + " ══", 13, c));
            string polName = proj.PolishStrat switch
            {
                PolishStrategy.Standard => Loc.Tr("dev.polish_standard"),
                PolishStrategy.Deep => Loc.Tr("dev.polish_deep"),
                _ => Loc.Tr("dev.polish_extreme")
            };
            _content.AddChild(MakeLabel(Loc.TrF("dev.polish_progress_fmt", proj.PolishMonths, polName), fs, v));
            if (!string.IsNullOrEmpty(proj.QATestReport))
            {
                _content.AddChild(MakeLabel(Loc.Tr("dev.ro_qa_report"), fs, new Color(0.6f, 0.4f, 0.9f)));
                foreach (var line in proj.QATestReport.Split('\n'))
                    if (!string.IsNullOrWhiteSpace(line))
                        _content.AddChild(MakeLabel("  " + line, 10, new Color(0.3f, 0.25f, 0.5f)));
            }
        }

        // ══ 8. 续作 / IP ══
        if (proj.PredecessorScore > 0)
        {
            _content.AddChild(MakeLabel("══ IP " + Loc.Tr("dev.ro_sequel_hdr") + " ══", 13, new Color(0.8f, 0.35f, 0.1f)));
            _content.AddChild(MakeLabel(Loc.TrF("dev.ro_sequel", proj.PredecessorScore, proj.PredecessorSales), fs, new Color(0.8f, 0.35f, 0.1f)));
            string seqName = proj.SequelStrat switch
            {
                SequelStrategy.Cautious => Loc.Tr("devmenu.seq_stable"),
                SequelStrategy.Revolutionary => Loc.Tr("devmenu.seq_innovate"),
                SequelStrategy.Derivative => Loc.Tr("devmenu.seq_spinoff"),
                _ => ""
            };
            _content.AddChild(MakeLabel(Loc.TrF("dev.ro_sequel_strat", seqName), fs, new Color(0.8f, 0.35f, 0.1f)));
        }

        // ══ 9. 开发日志 ══
        if (proj.DevLog.Count > 0)
        {
            _content.AddChild(MakeLabel("══ " + Loc.Tr("dev.log_title") + " ══", 11, btnColor));
            foreach (var line in proj.DevLog.TakeLast(5))
                if (!string.IsNullOrWhiteSpace(line))
                    _content.AddChild(MakeLabel(line, 9, new Color(0.25f, 0.22f, 0.45f)));
        }
        _content.AddChild(MakeLabel("", S(8), Colors.Transparent));
    }

    /// ── 可操作项目面板（四象限仪表盘）──
    private void RenderActiveProjectPanel(GameProject proj)
    {
        string phase = proj.Phase switch
        {
            DevPhase.Developing => Loc.TrF("dev.dev_progress", proj.DevProgress * 100),
            DevPhase.Polishing => Loc.TrF("dev.polish_progress", proj.BugCount),
            _ => proj.Phase.Name()
        };
        string monet = proj.PriceModel == PriceModel.Free ? Loc.TrF("dev.free_ad", proj.AdIntensity * 100) : Loc.TrF("dev.buy_price", proj.SuggestedPrice);
        _content.AddChild(MakeLabel($"{proj.Name} {proj.Genre.Name()} × {proj.Theme.Name()} {Loc.Tr("dev.scale")}{proj.Scale * 100:F0}% {monet}", 13, new Color(0.10f, 0.14f, 0.22f)));
        _content.AddChild(MakeLabel(phase, 12, new Color(0.15f, 0.45f, 0.15f)));

        if (proj.Phase == DevPhase.Developing)
        {
                float quadW = (_panel.Size.X - S(50)) / 2;
                float quadH = S(170);

                var topRow = new HBoxContainer();
                topRow.AddThemeConstantOverride("separation", (int)S(8));

                // ── 左上：内容象限 ──
                var qContent = new Panel { CustomMinimumSize = new(quadW, quadH) };
                qContent.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.92f, 0.96f, 0.98f, 0.9f), BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.3f, 0.5f, 0.8f, 0.3f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
                var vQ1 = new VBoxContainer { Position = new(S(6), S(4)) };
                vQ1.AddChild(MakeLabel("\U0001F9E9 " + Loc.Tr("dev.quad_content"), 11, new Color(0.15f, 0.3f, 0.65f)));
                AddModuleBar(Loc.Tr("dev.core_gameplay"), proj.ModuleProgressCore, SkillType.Program);
                AddModuleBar(Loc.Tr("dev.visual"), proj.ModuleProgressVisual, SkillType.Art);
                AddModuleBar(Loc.Tr("dev.audio_design"), proj.ModuleProgressAudio, SkillType.Audio);
                AddModuleBar(Loc.Tr("dev.story_text"), proj.ModuleProgressStory, SkillType.Program);
                AddModuleBar(Loc.Tr("dev.program_stable"), proj.ModuleProgressStability, SkillType.Program);
                AddModuleBar(Loc.Tr("dev.online_service"), proj.ModuleProgressOnline, SkillType.Network);
                float estRaw = proj.GraphicsScore * 0.2f + proj.GameplayScore * 0.3f + proj.AudioScore * 0.1f + proj.StoryScore * 0.15f + proj.NetworkScore * 0.1f + proj.StabilityScore * 0.15f;
                float est = estRaw * (0.9f + proj.Scale * 0.2f) - proj.BugCount * 0.3f;
                string estGrade = est >= 85 ? "A" : est >= 70 ? "B" : est >= 50 ? "C" : "D";
                vQ1.AddChild(MakeLabel(Loc.TrF("dev.est_score", Mathf.Clamp(est, 0, 100), estGrade), 10, new Color(0.4f, 0.55f, 0.7f)));
                qContent.AddChild(vQ1);
                topRow.AddChild(qContent);

                // ── 右上：技术象限 ──
                var qTech = new Panel { CustomMinimumSize = new(quadW, quadH) };
                qTech.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.98f, 0.94f, 0.92f, 0.9f), BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.8f, 0.3f, 0.2f, 0.35f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
                var vQ2 = new VBoxContainer { Position = new(S(6), S(4)) };
                bool memDanger = proj.MemoryOverLimit, fpsDanger = proj.FpsBelowTarget;
                Color techTitleColor = (memDanger || fpsDanger) ? new Color(0.85f, 0.15f, 0.1f) : new Color(0.55f, 0.25f, 0.15f);
                vQ2.AddChild(MakeLabel("\u2699\uFE0F " + Loc.Tr("dev.quad_tech"), 11, techTitleColor));
                float memPct = Mathf.Clamp(proj.MemoryUsage / proj.PlatformMemoryLimit, 0, 1.5f);
                Color memColor = memPct > 1f ? new Color(0.9f, 0.1f, 0.1f) : memPct > 0.8f ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.2f, 0.7f, 0.3f);
                vQ2.AddChild(MakeLabel(Loc.TrF("dev.mem_gauge", proj.MemoryUsage, proj.PlatformMemoryLimit), 11, memColor));
                float mbw = quadW - S(90), mbh = S(12);
                var memWrap = new Control { CustomMinimumSize = new(0, mbh + S(2)), MouseFilter = Control.MouseFilterEnum.Ignore };
                memWrap.AddChild(new ColorRect { Position = new(0, 0), Size = new(mbw, mbh), Color = new Color(0.25f, 0.25f, 0.28f, 0.4f) });
                memWrap.AddChild(new ColorRect { Position = new(0, 0), Size = new(mbw * Mathf.Min(memPct, 1.5f) / 1.5f, mbh), Color = memColor });
                if (memDanger)
                {
                    var warnLb = new Label { Text = "\u26A0", Position = new(mbw + S(4), -S(2)), Size = new(S(18), mbh + S(4)) };
                    warnLb.AddThemeFontSizeOverride("font_size", 12);
                    warnLb.AddThemeColorOverride("font_color", new Color(0.9f, 0.2f, 0.2f));
                    memWrap.AddChild(warnLb);
                }
                vQ2.AddChild(memWrap);
                Color fpsColor = fpsDanger ? new Color(0.9f, 0.1f, 0.1f) : proj.FpsEstimate < 45 ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.2f, 0.65f, 0.3f);
                vQ2.AddChild(MakeLabel(Loc.TrF("dev.fps_label", proj.FpsEstimate, proj.PlatformFpsTarget), 11, fpsColor));
                float crashPct = proj.CrashRate * 100;
                Color crashColor = crashPct > 30 ? new Color(0.9f, 0.1f, 0.1f) : crashPct > 15 ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.2f, 0.65f, 0.3f);
                vQ2.AddChild(MakeLabel(Loc.TrF("dev.crash_label", crashPct), 11, crashColor));
                string platStatus = proj.PlatformStressLevel == "danger" ? Loc.Tr("dev.plat_fail") : proj.PlatformStressLevel == "warn" ? Loc.Tr("dev.plat_warn") : Loc.Tr("dev.plat_ok");
                Color platColor = proj.PlatformStressLevel == "danger" ? new Color(0.9f, 0.2f, 0.2f) : proj.PlatformStressLevel == "warn" ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.2f, 0.65f, 0.3f);
                vQ2.AddChild(MakeLabel(Loc.TrF("dev.plat_status", proj.Platform.Name(), platStatus), 11, platColor));
                qTech.AddChild(vQ2); topRow.AddChild(qTech);
                _content.AddChild(topRow);

                // ── 左下：资源象限 ──
                var botRow = new HBoxContainer();
                botRow.AddThemeConstantOverride("separation", (int)S(8));
                var qRes = new Panel { CustomMinimumSize = new(quadW, quadH) };
                qRes.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.94f, 0.98f, 0.92f, 0.9f), BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.3f, 0.7f, 0.3f, 0.3f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
                var vQ3 = new VBoxContainer { Position = new(S(6), S(4)) };
                vQ3.AddChild(MakeLabel("\U0001F4A1 " + Loc.Tr("dev.quad_res"), 11, new Color(0.15f, 0.5f, 0.15f)));
                if (!_readOnly)
                {
                    string[] modNames = Loc.ParseModNames();
                    var inspRow = new HBoxContainer();
                    for (int i = 0; i < 6; i++)
                    {
                        int idx = i; string sn = modNames[i]; string sl = sn.Length > 2 ? sn[..2] : sn;
                        var btn = MakeBtn(sl, 48, 20, 9);
                        btn.TooltipText = Loc.TrF("dev.inspire_btn", sn);
                        btn.Pressed += () => { InjectInspiration(proj, idx); RenderStep3_Status(); };
                        inspRow.AddChild(btn);
                    }
                    vQ3.AddChild(inspRow);
                    vQ3.AddChild(MakeLabel(Loc.TrF("dev.inspire_cost", _res.Inspiration), 10, new Color(0.3f, 0.35f, 0.4f)));
                }
                else
                {
                    vQ3.AddChild(MakeLabel(Loc.TrF("dev.inspire_cost", _res.Inspiration), 10, new Color(0.3f, 0.35f, 0.4f)));
                }
                var storyEvt2 = _gm.GetNode<StoryEvents>("StoryEvents");
                if (storyEvt2 != null && storyEvt2.Fragments.Count > 0)
                {
                    vQ3.AddChild(MakeLabel(Loc.TrF("dev.frag_count", storyEvt2.Fragments.Count, storyEvt2.Fragments[0].Bonus), 10, new Color(0.7f, 0.3f, 0.6f)));
                    if (!_readOnly)
                    {
                        var fragBtn = MakeBtn(Loc.Tr("dev.use_fragment"), 70, 20, 9);
                        var p2 = proj;
                        fragBtn.Pressed += () =>
                        {
                            string name = storyEvt2.ConsumeFragment(p2, 0);
                            if (name != null) RenderStep3_Status();
                        };
                        vQ3.AddChild(fragBtn);
                    }
                }
                qRes.AddChild(vQ3); botRow.AddChild(qRes);

                // ── 右下：风险象限 ──
                var qRisk = new Panel { CustomMinimumSize = new(quadW, quadH) };
                Color riskBg = proj.TechDebt > 50 ? new Color(0.98f, 0.90f, 0.82f, 0.95f) : new Color(0.95f, 0.95f, 0.90f, 0.9f);
                qRisk.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = riskBg, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.8f, 0.6f, 0.1f, 0.35f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
                var vQ4 = new VBoxContainer { Position = new(S(6), S(4)) };
                Color riskTitle = proj.TechDebt > 50 ? new Color(0.75f, 0.15f, 0.05f) : new Color(0.45f, 0.35f, 0.15f);
                vQ4.AddChild(MakeLabel("\u26A0 " + Loc.Tr("dev.quad_risk"), 11, riskTitle));
                vQ4.AddChild(MakeLabel(Loc.TrF("dev.debt_val", proj.TechDebt), 11, riskTitle));
                if (proj.TechDebt > 30)
                    vQ4.AddChild(MakeLabel(Loc.TrF("dev.debt_interest", proj.NextMonthBugFromDebt, proj.NextMonthSlowFromDebt * 100), 10, new Color(0.8f, 0.3f, 0.1f)));
                if (!_readOnly && proj.TechDebt >= 20)
                {
                    float refCost = 1f + proj.TechDebt * 0.015f;
                    string refLabel = proj.TechDebt > 50 ? Loc.TrF("dev.refactor_urgent", proj.TechDebt * 0.5f, refCost) : Loc.TrF("dev.refactor_btn", proj.TechDebt * 0.5f, refCost);
                    Color refColor = proj.TechDebt > 50 ? new Color(0.9f, 0.2f, 0.1f) : new Color(0.7f, 0.4f, 0.1f);
                    var refBtn = MakeBtn(refLabel, 200, 22, 9);
                    var rproj = proj;
                    refBtn.Pressed += () => { if (_devMgr.PartialRefactor(rproj)) RenderStep3_Status(); };
                    vQ4.AddChild(refBtn);
                    if (proj.TechDebt > 60) vQ4.AddChild(MakeLabel(Loc.Tr("dev.spaghetti"), 10, new Color(0.9f, 0.15f, 0.1f)));
                }
                else if (_readOnly && proj.TechDebt > 60)
                    vQ4.AddChild(MakeLabel(Loc.Tr("dev.spaghetti"), 10, new Color(0.9f, 0.15f, 0.1f)));
                qRisk.AddChild(vQ4); botRow.AddChild(qRisk);
                _content.AddChild(botRow);

                // ── 开发日志 ──
                if (proj.DevLog.Count > 0)
                {
                    _content.AddChild(MakeLabel(Loc.Tr("dev.log_title"), 10, new Color(0.35f, 0.38f, 0.42f)));
                    foreach (var line in proj.DevLog.TakeLast(3))
                        if (!string.IsNullOrWhiteSpace(line)) _content.AddChild(MakeLabel(line, 9, new Color(0.25f, 0.22f, 0.45f)));
                }
            }

            if (proj.Phase == DevPhase.Polishing || proj.Phase == DevPhase.Testing || proj.Phase == DevPhase.ReadyToRelease)
            {
                if (proj.Phase == DevPhase.Polishing && !_readOnly)
                {
                    // ── 打磨策略选择 ──
                    string polName = proj.PolishStrat switch
                    {
                        PolishStrategy.Standard => Loc.Tr("dev.polish_standard"),
                        PolishStrategy.Deep => Loc.Tr("dev.polish_deep"),
                        _ => Loc.Tr("dev.polish_extreme")
                    };
                    _content.AddChild(MakeLabel(Loc.TrF("dev.polish_progress_fmt", proj.PolishMonths, polName), 12, new Color(0.18f, 0.20f, 0.25f)));
                    var polRow = new HBoxContainer();
                    foreach (PolishStrategy ps in Enum.GetValues<PolishStrategy>())
                    {
                        var polBtn = MakeBtn(
                            ps == PolishStrategy.Standard ? Loc.Tr("dev.polish_standard") :
                            ps == PolishStrategy.Deep ? Loc.Tr("dev.polish_deep") : Loc.Tr("dev.polish_extreme"),
                            ps == PolishStrategy.Standard ? 90 : ps == PolishStrategy.Deep ? 130 : 120,
                            24, 10);
                        polBtn.Pressed += () => { proj.PolishStrat = ps; RenderStep3_Status(); };
                        polRow.AddChild(polBtn);
                    }
                    _content.AddChild(polRow);

                    // ── QA测试按钮 ──
                    var qaBtn = MakeBtn("🔍 执行QA测试", 150, 36);
                    qaBtn.Pressed += () =>
                    {
                        _gm.Paused = true;
                        string report = _devMgr.RunQATest(proj);
                        _gm.ShowPopup("QA测试完成", report, new Color(0.6f, 0.4f, 0.9f));
                        RenderStep3_Status();
                    };
                    _content.AddChild(qaBtn);
                    _content.AddChild(MakeLabel("开发完成后建议先QA测试再发售", 10, new Color(0.45f, 0.5f, 0.55f)));
                }

                // ── QA测试报告显示 ──
                if (!string.IsNullOrEmpty(proj.QATestReport))
                {
                    _content.AddChild(MakeLabel("--- QA测试报告 ---", 13, new Color(0.6f, 0.4f, 0.9f)));
                    foreach (var line in proj.QATestReport.Split('\n'))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            _content.AddChild(MakeLabel(line, 10, new Color(0.3f, 0.25f, 0.5f)));
                    }
                }

                // ── 发售按钮 ──
                if (!_readOnly)
                {
                    string relLabel = proj.Phase == DevPhase.ReadyToRelease ? "🚀 正式发售（已测试）" : "🚀 直接发售（跳过QA）";
                    var releaseBtn = MakeBtn(relLabel, 200, 36);
                    releaseBtn.Pressed += () =>
                    {
                        var team = _teamMgr.Teams.Find(t => t.CurrentProject == proj);
                        if (team != null)
                        {
                            if (!string.IsNullOrEmpty(proj.QATestReport) && proj.BugCount < 15)
                                proj.DevLog.Add("QA测试通过，品质保证加成");
                            _devMgr.ReleaseGame(team);
                            ShowReleaseResult(proj);
                        }
                    };
                    _content.AddChild(releaseBtn);
                }
            }
    }

    private void AddModuleBar(string label, float progress, SkillType skill)
    {
        float pct = Mathf.Clamp(progress * 100, 0, 100);
        var barColor = pct switch
        {
            < 25 => new Color(0.9f, 0.3f, 0.2f),
            < 50 => new Color(0.95f, 0.6f, 0.2f),
            < 75 => new Color(0.85f, 0.8f, 0.15f),
            _ => new Color(0.2f, 0.75f, 0.3f)
        };

        float lw = S(70), bw = S(140), bh = S(12), rowH = S(18);

        // 裸 Control 承载所有子元素：VBoxContainer 只给 Control 分空间，不动内部
        var ctrl = new Control { CustomMinimumSize = new(0, rowH), MouseFilter = Control.MouseFilterEnum.Ignore };
        // 标签
        var lb = new Label { Text = label, Position = new(0, 0), Size = new(lw, rowH) };
        lb.AddThemeFontSizeOverride("font_size", 11);
        lb.AddThemeColorOverride("font_color", new Color(0.25f, 0.28f, 0.32f));
        ctrl.AddChild(lb);
        // 底条
        ctrl.AddChild(new ColorRect { Position = new(lw, S(2)), Size = new(bw, bh), Color = new Color(0.25f, 0.25f, 0.28f, 0.4f) });
        // 填充条
        ctrl.AddChild(new ColorRect { Position = new(lw, S(2)), Size = new(bw * pct / 100f, bh), Color = barColor });
        // 百分比
        var pctLb = new Label { Text = $"{pct:F0}%", Position = new(lw + bw + S(4), 0), Size = new(S(36), rowH) };
        pctLb.AddThemeFontSizeOverride("font_size", 9);
        pctLb.AddThemeColorOverride("font_color", new Color(0.30f, 0.33f, 0.37f));
        ctrl.AddChild(pctLb);
        _content.AddChild(ctrl);
    }

    private void InjectInspiration(GameProject proj, int moduleIdx)
    {
        if (_res.Inspiration < 10)
        {
            _gm.ShowPopup(Loc.Tr("devpop.inspire_low"), Loc.Tr("devpop.inspire_low_msg"), new Color(0.9f, 0.5f, 0.2f));
            return;
        }

        if (!_res.SpendInspiration(10)) return;

        float boost = 0.05f;
        switch (moduleIdx)
        {
            case 0: proj.ModuleProgressCore += boost; proj.GameplayScore += 2; break;
            case 1: proj.ModuleProgressVisual += boost; proj.GraphicsScore += 2; break;
            case 2: proj.ModuleProgressAudio += boost; proj.AudioScore += 2; break;
            case 3: proj.ModuleProgressStory += boost; proj.StoryScore += 2; break;
            case 4: proj.ModuleProgressStability += boost; proj.StabilityScore += 3; break;
            case 5: proj.ModuleProgressOnline += boost; proj.NetworkScore += 2; break;
        }
        string[] modNames2 = Loc.ParseModNames();
        proj.DevLog.Add(Loc.TrF("dev.inspire_log", modNames2[moduleIdx]));
    }

    private void ShowReleaseResult(GameProject proj)
    {
        ClearContent();

        // ══════ 绝境翻盘传奇专页 ══════
        if (proj.LegendaryLegacy)
        {
            _gm.HasLegendaryLegacy = true;
            // 金色背景
            _content.AddChild(MakeLabel("", 6, new Color(0.9f, 0.7f, 0.1f)));
            _content.AddChild(MakeLabel(Loc.Tr("legend.title"), 24, new Color(1f, 0.85f, 0.1f)));
            _content.AddChild(MakeLabel("", 4, new Color(1f, 0.85f, 0.1f)));
            _content.AddChild(MakeLabel(Loc.TrF("legend.story", proj.Name), 14, new Color(0.9f, 0.8f, 0.3f)));
            _content.AddChild(MakeLabel("", 6, new Color(1f, 0.85f, 0.1f)));
            _content.AddChild(MakeLabel(Loc.Tr("legend.media_frenzy"), 13, new Color(0.85f, 0.75f, 0.4f)));
            _content.AddChild(MakeLabel(Loc.Tr("legend.studio_buzz"), 13, new Color(0.8f, 0.7f, 0.5f)));
            _content.AddChild(MakeLabel("", 6, new Color(1f, 0.85f, 0.1f)));
            _content.AddChild(MakeLabel(Loc.TrF("legend.bonus", "+20"), 15, new Color(1f, 0.9f, 0.2f)));
            _content.AddChild(MakeLabel("", 10, new Color(1f, 0.85f, 0.1f)));
        }

        _content.AddChild(MakeLabel(Loc.TrF("dev.release_result", proj.Name), 20, new Color(0.3f, 0.9f, 1.0f)));
        _content.AddChild(MakeLabel(Loc.TrF("dev.sales_count", proj.Sales), 16, new Color(0.18f, 0.20f, 0.25f)));
        _content.AddChild(MakeLabel(Loc.TrF("dev.revenue_count", proj.Revenue), 16, new Color(0.15f, 0.4f, 0.15f)));

        int fans = _fanMgr.TotalFans;
        _content.AddChild(MakeLabel(Loc.TrF("dev.fans_now", fans, _fanMgr.DiehardFans), 14, new Color(0.8f, 0.6f, 0.9f)));

        // ── 风口保险激活提示 ──
        var activeBuffs = _gm.WindInsurances.Where(w => w.MonthsLeft > 0).ToList();
        if (activeBuffs.Count > 0)
        {
            _content.AddChild(MakeLabel(Loc.Tr("dev.wind_ins_title"), 13, new Color(0.15f, 0.5f, 0.7f)));
            foreach (var w in activeBuffs)
                _content.AddChild(MakeLabel($"  🛡 {w.GenreOrTheme}  +{w.SalesBonus*100:F0}%  ({w.MonthsLeft}个月剩余)", 11, new Color(0.2f, 0.5f, 0.6f)));
        }

        var closeBtn = MakeBtn(Loc.Tr("dev.confirm"), 100, 36);
        closeBtn.Pressed += () => _gm.CloseAll();
        _content.AddChild(closeBtn);
    }
}
