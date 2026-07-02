using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 外包任务
/// </summary>
public class OutsourceTask
{
    public string Name;
    public string Description;
    public float Reward;          // 报酬
    public int Duration;          // 工期（月）
    public int Remaining;
    public SkillType RequiredSkill;
    public int RequiredLevel;
    public float Difficulty;      // 成功率影响
    public bool Accepted;
    public Team AssignedTeam;
    public int MonthsSpent;
    // ── 多样性 ──
    public bool IsHighRisk;        // 高风险: 失败降声誉
    public bool GivesInspiration;  // 奖励灵感而非金钱
    public bool GivesTechBoost;    // 奖励永久科技加速
    public bool GivesReputation;   // 奖励公司声誉
}

/// <summary>
/// 开发菜单——ESC风格全屏面板
/// </summary>
public class DevMenu
{
    private readonly GameManager _gm;
    private readonly ResourceManager _res;
    private readonly TeamManager _teamMgr;
    private readonly GameDevManager _devMgr;
    private readonly TechManager _techMgr;
    private readonly TechDebtManager _debtMgr;
    private readonly EmployeeManager _empMgr;
    private float Sf(float v) => v * _gm.UIScale;

    private Panel _panel;
    private ScrollContainer _pageHost; // 滚动容器，外层不变
    private VBoxContainer _pageInner;  // 滚动内部实际容器
    private VBoxContainer _content;    // 当前活跃页面
    private VBoxContainer _scorePanel;

    public List<OutsourceTask> OutsourceTasks => _gm.OutsourceTaskPool;
    private readonly Random _rng = new();

    // 分页状态
    private GameGenre? _selGenre;
    private GameTheme? _selTheme;
    private Platform _selPlatform = Platform.PC;
    private MarketingStrategy _selMkt = MarketingStrategy.Normal;
    private float _selMonths = 12f;
    private float _selBudget = 50000f;
    private float _selScale = 0.5f;
    private PriceModel _selPrice = PriceModel.BuyToPlay;
    private float _selAdIntensity;
    private List<string> _selComponents = new();
    private DesignPhilosophy _selPhilosophy = DesignPhilosophy.Balanced;
    private SequelStrategy _selSequelStrat = SequelStrategy.Cautious;
    private CheckBox _reuseCheck;
    private bool _canReuse;
    private bool _isSequel;
    private GameProject _sequelBase;

    public DevMenu(GameManager gm)
    {
        _gm = gm;
        _res = gm.GetNode<ResourceManager>("ResourceManager");
        _teamMgr = gm.GetNode<TeamManager>("TeamManager");
        _devMgr = gm.GetNode<GameDevManager>("GameDevManager");
        _techMgr = gm.GetNode<TechManager>("TechManager");
        _empMgr = gm.GetNode<EmployeeManager>("EmployeeManager");
        _debtMgr = gm.GetNode<TechDebtManager>("TechDebtManager");
    }

    public void Close()
    {
        if (_panel != null && GodotObject.IsInstanceValid(_panel)) _panel.QueueFree();
        if (_scorePanel != null && GodotObject.IsInstanceValid(_scorePanel)) _scorePanel.QueueFree();
        _panel = null; _scorePanel = null;
    }

    /// <summary>外部直接跳转到引擎管理页</summary>
    public void ShowEnginePage()
    {
        var vp = _gm.GetViewport().GetVisibleRect().Size;
        float m = Sf(40);
        _panel = new Panel
        {
            Position = new(m, m),
            Size = new(vp.X - m * 2, vp.Y - m * 2),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.97f, 0.96f, 0.94f, 0.96f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.2f, 0.4f, 0.8f, 0.6f),
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8
        });

        var title = new Label { Text = Loc.Tr("devmenu.engine_title"), Position = new(Sf(20), Sf(10)), Size = new(_panel.Size.X - Sf(80), Sf(30)) };
        title.AddThemeFontSizeOverride("font_size", 22);
        title.AddThemeColorOverride("font_color", new Color(0.15f, 0.4f, 0.7f));
        _panel.AddChild(title);

        var closeBtn = MkCloseBtn(Sf(_panel.Size.X - 50), Sf(8));
        closeBtn.Pressed += () => { _gm.CloseAll(); };
        _panel.AddChild(closeBtn);

        _pageHost = new ScrollContainer { Position = new(Sf(20), Sf(50)), Size = new(_panel.Size.X - Sf(40), _panel.Size.Y - Sf(60)) };
        _pageHost.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        _pageInner = new VBoxContainer { CustomMinimumSize = new(0, 0) };
        _pageHost.AddChild(_pageInner);
        _panel.AddChild(_pageHost);
        _gm.PushPanel(_panel);
        RenderEngineBiz();
    }

    public void Show()
    {
        var vp = _gm.GetViewport().GetVisibleRect().Size;
        float m = Sf(40);

        _panel = new Panel
        {
            Position = new(m, m),
            Size = new(vp.X - m * 2, vp.Y - m * 2),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.97f, 0.96f, 0.94f, 0.96f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.2f, 0.4f, 0.8f, 0.6f),
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8
        });

        var title = new Label { Text = Loc.Tr("devmenu.dev_center"), Position = new(Sf(20), Sf(10)), Size = new(_panel.Size.X - Sf(80), Sf(30)) };
        title.AddThemeFontSizeOverride("font_size", 22);
        title.AddThemeColorOverride("font_color", new Color(0.15f, 0.4f, 0.7f));
        _panel.AddChild(title);

        var closeBtn = MkCloseBtn(Sf(_panel.Size.X - 50), Sf(8));
        closeBtn.Pressed += () => { _gm.CloseAll(); };
        _panel.AddChild(closeBtn);

        _pageHost = new ScrollContainer { Position = new(Sf(20), Sf(50)), Size = new(_panel.Size.X - Sf(40), _panel.Size.Y - Sf(60)) };
        _pageHost.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        _pageInner = new VBoxContainer { CustomMinimumSize = new(0, 0) };
        _pageHost.AddChild(_pageInner);
        _panel.AddChild(_pageHost);

        _gm.PushPanel(_panel);
        RenderMainMenu();
    }

    private Button MkCloseBtn(float x, float y)
    {
        var b = new Button { Text = "✕", Position = new(x, y), Size = new(Sf(35), Sf(30)), Flat = true };
        b.AddThemeFontSizeOverride("font_size", 18);
        b.AddThemeColorOverride("font_color", new Color(0.8f, 0.3f, 0.3f));
        b.AddThemeColorOverride("font_hover_color", new Color(0.45f, 0.45f, 0.45f));
        b.AddThemeColorOverride("font_pressed_color", new Color(0.3f, 0.3f, 0.3f));
        return b;
    }

    private Label MkL(string t, float fs, Color c, HorizontalAlignment ha = HorizontalAlignment.Left)
    { var l = new Label { Text = t, HorizontalAlignment = ha }; l.AddThemeFontSizeOverride("font_size", (int)fs); l.AddThemeColorOverride("font_color", c); return l; }

    private Button MkB(string t, float w, float h, float fs = 14)
    { var b = new Button { Text = t, CustomMinimumSize = new(Sf(w), Sf(h)), Size = new(Sf(w), Sf(h)) }; b.AddThemeFontSizeOverride("font_size", (int)fs); b.AddThemeColorOverride("font_color", new Color(0.10f, 0.10f, 0.10f)); return b; }

    private Label MkLPos(string t, float x, float y, float w, float h, float fs, Color c)
    { var l = new Label { Text = t, Position = new(x, y), Size = new(w, h) }; l.AddThemeFontSizeOverride("font_size", (int)fs); l.AddThemeColorOverride("font_color", c); return l; }

    private Button MkBPos(string t, float x, float y, float w, float h, float fs)
    {
        var b = new Button { Text = t, Position = new(x, y), Size = new(w, h), Flat = true };
        b.AddThemeFontSizeOverride("font_size", (int)fs);
        b.AddThemeColorOverride("font_color", new Color(0.10f, 0.10f, 0.10f));
        b.AddThemeColorOverride("font_hover_color", new Color(0.40f, 0.40f, 0.40f));
        b.AddThemeColorOverride("font_pressed_color", new Color(0.05f, 0.05f, 0.05f));
        return b;
    }

    // 页面容器池（避免 QueueFree → 重建导致 CanvasLayer FBO 空帧闪白）
    private readonly Dictionary<string, VBoxContainer> _pagePool = new();
    private string _activePageKey;

    private void Clear()
    {
        foreach (var kv in _pagePool) kv.Value.Visible = false;
    }

    private VBoxContainer GetPage(string key)
    {
        if (_pagePool.TryGetValue(key, out var p)) return p;
        p = new VBoxContainer { CustomMinimumSize = new(0, 0) };
        _pageInner.AddChild(p);
        _pagePool[key] = p;
        return p;
    }

    private void ShowPage(string key)
    {
        Clear();
        var p = GetPage(key);
        p.Visible = true;
        foreach (var c in p.GetChildren()) c.QueueFree();
        _activePageKey = key;
        _content = p;
    }

    /// <summary>月度推进时重新渲染当前页面（仅引擎/开发进度等会随时间变化的数据）</summary>
    public void OnMonthChanged()
    {
        // 只重绘有进度/状态变化的页面
        if (_activePageKey == "engine") RenderEngineBiz();
        else if (_activePageKey == "team") RenderTeamAssign();
        else if (_activePageKey == "outsource") RenderOutsource();
    }

    // ══════════════════ 主菜单 ══════════════════
    private void RenderMainMenu()
    {
        ShowPage("main");
        _content.AddChild(MkL(Loc.Tr("devmenu.select_type"), 20, new Color(0.10f, 0.14f, 0.22f)));
        _content.AddChild(MkL("", 12, Colors.White));

        var grid = new GridContainer { Columns = 3 };

        AddMenuBtn(grid, Loc.Tr("devmenu.new_project_btn"), Loc.Tr("devmenu.new_project_desc"), () =>
        {
            _isSequel = false; _sequelBase = null;
            _selGenre = null; _selTheme = null; _customProjName = ""; _selEngine = null;
            RenderGenreSelect();
        });

        AddMenuBtn(grid, Loc.Tr("devmenu.dlc_btn"), Loc.Tr("devmenu.dlc_desc"), () =>
        {
            if (_devMgr.CompletedProjects.Count == 0)
            { _gm.ShowPopup(Loc.Tr("devmenu.no_game"), Loc.Tr("devmenu.no_dlc_msg"), new Color(0.9f, 0.5f, 0.2f)); return; }
            RenderDLCSelect();
        });

        AddMenuBtn(grid, Loc.Tr("devmenu.console_btn"), Loc.Tr("devmenu.console_desc"), () =>
        {
            if (!_techMgr.IsResearched("hardware_design"))
            { _gm.ShowPopup(Loc.Tr("devmenu.tech_locked"), Loc.Tr("devmenu.tech_locked_msg"), new Color(0.9f, 0.5f, 0.2f)); return; }
            RenderConsoleDev();
        });

        // ── 主机开发套件购买 ──
        if (_gm.CurrentConsoleGen >= 1 && !_gm.HasDevKitForPlatform(Platform.Console))
        {
            AddMenuBtn(grid, Loc.Tr("devmenu.buy_kit"), Loc.TrF("devmenu.buy_kit_desc", _gm.ConsoleKitCost), () =>
            {
                if (_gm.BuyDevKitNext())
                    _gm.ShowToast(Loc.Tr("devmenu.kit_ok"), Loc.Tr("devmenu.kit_ok_msg"), new Color(0.3f, 0.8f, 0.5f));
                else
                    _gm.ShowToast(Loc.Tr("ui.insufficient_funds"), Loc.TrF("ui.need_money", _gm.ConsoleKitCost), new Color(0.9f, 0.3f, 0.2f));
            });
        }

        AddMenuBtn(grid, Loc.Tr("devmenu.engine_btn"), Loc.Tr("devmenu.engine_btn_desc"), () =>
        {
            RenderEngineBiz();
        });

        AddMenuBtn(grid, Loc.Tr("devmenu.outsource_btn"), Loc.Tr("devmenu.outsource_btn_desc"), () =>
        {
            RenderOutsource();
        });

        _content.AddChild(grid);
    }

    private void AddMenuBtn(GridContainer grid, string title, string desc, Action action)
    {
        var btn = new Button { CustomMinimumSize = new(Sf(200), Sf(80)) };
        var h = new HBoxContainer();
        var v = new VBoxContainer();
        v.AddChild(MkL(title, 14, new Color(0.10f, 0.14f, 0.22f)));
        v.AddChild(MkL(desc, 10, new Color(0.25f, 0.28f, 0.32f)));
        h.AddChild(v);
        btn.AddChild(h);
        btn.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f, 0.9f), CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.7f, 0.7f, 0.7f, 0.5f) });
        btn.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = new Color(0.88f, 0.87f, 0.84f, 0.9f), CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.55f, 0.6f, 0.7f, 0.5f) });
        btn.AddThemeFontSizeOverride("font_size", 14);
        btn.Pressed += () => action();
        grid.AddChild(btn);
    }

    // ══════════════════ 新项目：选择类型 ══════════════════
    private void RenderGenreSelect()
    {
        ShowPage("genre");
        _content.AddChild(MkL(Loc.Tr("devmenu.genre_select"), 16, new Color(0.10f, 0.14f, 0.22f)));

        var grid = new GridContainer { Columns = 4 };
        foreach (var g in GetUnlockedGenres())
        {
            var btn = MkB(g.Name(), 120, 32, 12);
            if (_selGenre == g) btn.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.6f, 0.7f, 0.9f, 0.8f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
            var gg = g; btn.Pressed += () => { _selGenre = gg; RenderGenreSelect(); };
            grid.AddChild(btn);
        }
        _content.AddChild(grid);
        _content.AddChild(MkL("", 6, Colors.White));

        var grid2 = new GridContainer { Columns = 5 };
        foreach (var t in GetUnlockedThemes())
        {
            var btn = MkB(t.Name(), 120, 32, 12);
            if (_selTheme == t) btn.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.9f, 0.7f, 0.6f, 0.8f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
            var tt = t; btn.Pressed += () => { _selTheme = tt; RenderGenreSelect(); };
            grid2.AddChild(btn);
        }
        _content.AddChild(grid2);

        if (_selGenre != null && _selTheme != null)
        {
            _content.AddChild(MkL(Loc.TrF("devmenu.genre_selected", _selGenre.Value.Name(), _selTheme.Value.Name()), 15, new Color(0.15f, 0.45f, 0.15f)));
            var next = MkB(Loc.Tr("devmenu.next_config"), 200, 36);
            next.Pressed += () => RenderPlanning();
            _content.AddChild(next);
        }

        var back = MkB(Loc.Tr("devmenu.back_main"), 140, 30, 12);
        back.Pressed += () => RenderMainMenu();
        _content.AddChild(back);
    }

    // ══════════════════ DLC选择 ══════════════════
    private void RenderDLCSelect()
    {
        ShowPage("dlc");
        _content.AddChild(MkL(Loc.Tr("devmenu.dlc_select"), 16, new Color(0.10f, 0.14f, 0.22f)));

        foreach (var p in _devMgr.CompletedProjects)
        {
            var row = new HBoxContainer();
            row.AddChild(MkL($"{p.Name} ({p.Genre.Name()} × {p.Theme.Name()})", 13, new Color(0.18f, 0.20f, 0.25f)));
            var dlcBtn = MkB(Loc.Tr("devmenu.make_dlc"), 80, 28, 12);
            var proj = p;
            dlcBtn.Pressed += () =>
            {
                if (_empMgr.Employees.Count == 0)
                { _gm.ShowToast(Loc.Tr("toast.no_employee"), Loc.Tr("toast.no_employee_dev"), new Color(0.9f, 0.3f, 0.2f)); return; }
                _isSequel = false; _sequelBase = proj;
                _selGenre = proj.Genre; _selTheme = proj.Theme;
                _selMonths = proj.EstimatedMonths * 0.4f;
                _selBudget = proj.MarketingBudget * 0.5f;
                _canReuse = true;
                var dlc = _devMgr.CreateProject(
                    $"{proj.Name} DLC", proj.Genre, proj.Theme, proj.Platform,
                    _selMonths, _selMkt, _selBudget);
                if (dlc != null && _canReuse)
                    _debtMgr.ApplyCodeReuse(dlc);
                RenderTeamAssign();
            };
            row.AddChild(dlcBtn);
            _content.AddChild(row);
        }

        var back = MkB(Loc.Tr("devmenu.back_main"), 140, 30, 12);
        back.Pressed += () => RenderMainMenu();
        _content.AddChild(back);
    }

    // ══════════════════ 主机开发 ══════════════════
    private void RenderConsoleDev()
    {
        ShowPage("console");
        _content.AddChild(MkL(Loc.Tr("devmenu.console_dev"), 18, new Color(0.10f, 0.14f, 0.22f)));
        _content.AddChild(MkL(Loc.Tr("devmenu.console_dev_desc"), 14, new Color(0.18f, 0.20f, 0.25f)));
        _content.AddChild(MkL(Loc.Tr("devmenu.console_unlocked"), 13, new Color(0.15f, 0.45f, 0.15f)));
        _content.AddChild(MkL(Loc.Tr("devmenu.console_cost"), 14, new Color(0.8f, 0.6f, 0.3f)));

        var startBtn = MkB(Loc.Tr("devmenu.console_start"), 180, 40);
        startBtn.Pressed += () =>
        {
            var proj = _devMgr.CreateProject(Loc.Tr("devmenu.console_proj_name"), GameGenre.SAN, GameTheme.Modern, Platform.Console, 24, MarketingStrategy.Hype, 2000000);
            _gm.ShowPopup(Loc.Tr("devmenu.console_start_title"), Loc.Tr("devmenu.console_start_msg"), new Color(0.3f, 0.8f, 1f));
            RenderTeamAssign();
        };
        _content.AddChild(startBtn);

        var back = MkB(Loc.Tr("devmenu.back_main"), 140, 30, 12);
        back.Pressed += () => RenderMainMenu();
        _content.AddChild(back);
    }

    // ══════════════════ 引擎管理 ══════════════════
    private enum EnginePage { Overview, NewEngine, UpgradeEngine, BizSettings }
    private EnginePage _enginePage = EnginePage.Overview;
    private GameEngine _selectedEngine;
    private string _newEngineName = "";
    private EnginePosition _newEnginePos = EnginePosition.General;
    private List<string> _selectedTechs = new();
    private string _customProjName = "";            // 自定义项目名
    private GameEngine _selEngine;                  // 立项时选择的引擎

    private void RenderEngineBiz()
    {
        ShowPage("engine");
        var engines = _gm.Engines;
        if (engines.Count == 0) engines.Add(new GameEngine { Name = Loc.Tr("devmenu.no_game"), AppliedTechs = new List<string>() });

        switch (_enginePage)
        {
            case EnginePage.Overview: RenderEngineOverview(); break;
            case EnginePage.NewEngine: RenderNewEngine(); break;
            case EnginePage.UpgradeEngine: RenderUpgradeEngine(); break;
            case EnginePage.BizSettings: RenderEngineBizSettings(); break;
        }
    }

    private void RenderEngineOverview()
    {
        _content.AddChild(MkL(Loc.Tr("devmenu.engine_title"), 20, new Color(0.10f, 0.14f, 0.22f)));
        _content.AddChild(MkL("", 6, Colors.White));

        var engines = _gm.Engines;
        foreach (var eng in engines)
        {
            string status = eng.IsDeveloping ? Loc.TrF("devmenu.status_dev", eng.DevMonthsRemaining) : Loc.Tr("devmenu.status_ready");
            string biz = eng.BizModel switch
            {
                EngineBizModel.Closed => Loc.Tr("devmenu.biz_closed"),
                EngineBizModel.OpenSource => Loc.Tr("devmenu.biz_opensource"),
                EngineBizModel.Buyout => Loc.TrF("devmenu.biz_buyout", eng.BuyoutPrice / 10000),
                EngineBizModel.Subscription => Loc.TrF("devmenu.biz_sub", eng.SubscriptionPrice / 10000),
                EngineBizModel.Royalty => Loc.TrF("devmenu.biz_royalty", eng.RoyaltyRate),
                _ => "?"
            };
            float totalDebt = _debtMgr.ComputeEngineDebt(eng.Name);
            Color debtCol = totalDebt > 60 ? new Color(1f, 0.3f, 0.3f) : totalDebt > 30 ? new Color(1f, 0.7f, 0.2f) : new Color(0.4f, 0.9f, 0.4f);

            var card = new HBoxContainer();
            var v = new VBoxContainer();
            v.AddChild(MkL(Loc.TrF("devmenu.engine_card", eng.Name, eng.Generation, status, biz), 14, new Color(0.10f, 0.15f, 0.30f)));
            v.AddChild(MkL(Loc.TrF("devmenu.engine_func", eng.AppliedTechs.Count, totalDebt, eng.LicenseCount, eng.MarketShare), 11, new Color(0.35f, 0.38f, 0.42f)));
            string perks = eng.Perks.Count > 0 ? string.Join(" ", eng.Perks.Select(p => PerkIcon(p))) : "";
            if (!string.IsNullOrEmpty(perks))
                v.AddChild(MkL(Loc.TrF("devmenu.engine_perks", perks), 11, new Color(0.6f, 0.5f, 0.8f)));
            v.AddChild(MkL(Loc.TrF("devmenu.engine_rev", eng.Reputation, eng.MonthlyRevenue, eng.TotalRevenue), 11, new Color(0.4f, 0.5f, 0.6f)));
            card.AddChild(v);

            if (!eng.IsDeveloping)
            {
                var upgBtn = MkB(Loc.Tr("devmenu.upgrade_btn"), 60, 28, 11);
                var ee = eng; upgBtn.Pressed += () => { _selectedEngine = ee; _enginePage = EnginePage.UpgradeEngine; RenderEngineBiz(); };
                card.AddChild(upgBtn);
                var bizBtn = MkB(Loc.Tr("devmenu.bizmode_btn"), 60, 28, 11);
                bizBtn.Pressed += () => { _selectedEngine = ee; _enginePage = EnginePage.BizSettings; RenderEngineBiz(); };
                card.AddChild(bizBtn);
            }
            _content.AddChild(card);
            _content.AddChild(MkL("", 4, Colors.White));
        }

        _content.AddChild(MkL("", 8, Colors.White));
        var newEngBtn = MkB(Loc.Tr("devmenu.new_engine_btn"), 180, 40, 14);
        newEngBtn.Pressed += () => { _enginePage = EnginePage.NewEngine; _newEngineName = ""; _newEnginePos = EnginePosition.General; _selectedTechs.Clear(); RenderEngineBiz(); };
        _content.AddChild(newEngBtn);

        var back = MkB(Loc.Tr("devmenu.back_main"), 140, 30, 12);
        back.Pressed += () => { _enginePage = EnginePage.Overview; RenderMainMenu(); };
        _content.AddChild(back);
    }

    private void RenderNewEngine()
    {
        float cost = 800000;  // 引擎研发成本×4，匹配收入×5
        int months = 6;
        _content.AddChild(MkL(Loc.Tr("devmenu.new_engine_btn"), 18, new Color(0.10f, 0.14f, 0.22f)));
        _content.AddChild(MkL(Loc.TrF("devmenu.base_cost", cost, months), 13, new Color(0.18f, 0.20f, 0.25f)));

        var nameRow = new HBoxContainer();
        nameRow.AddChild(MkL(Loc.Tr("devmenu.engine_name"), 14, new Color(0.18f, 0.20f, 0.25f)));
        var nameEdit = new LineEdit { CustomMinimumSize = new(Sf(220), 0), Size = new(Sf(220), 0), ExpandToTextLength = false, Text = _newEngineName, PlaceholderText = Loc.Tr("devmenu.engine_name_hint") };
        nameEdit.TextChanged += (s) => _newEngineName = s;
        nameRow.AddChild(nameEdit);
        _content.AddChild(nameRow);

        // 引擎定位
        _content.AddChild(MkL("", 4, Colors.Transparent));
        var posRow = new HBoxContainer();
        posRow.AddChild(MkL(Loc.Tr("devmenu.engine_pos"), 14, new Color(0.18f, 0.20f, 0.25f)));
        foreach (EnginePosition pos in Enum.GetValues<EnginePosition>())
        {
            string label = pos switch { EnginePosition.General => Loc.Tr("devmenu.pos_general"), EnginePosition.Graphical => Loc.Tr("devmenu.pos_graphical"), EnginePosition.Performance => Loc.Tr("devmenu.pos_performance"), EnginePosition.Network => Loc.Tr("devmenu.pos_network"), EnginePosition.Stable => Loc.Tr("devmenu.pos_stable"), _ => "?" };
            var btn = MkB(label, 80, 28, 11);
            if (_newEnginePos == pos) btn.Modulate = new Color(0.5f, 1f, 0.5f);
            var p = pos; btn.Pressed += () => { _newEnginePos = p; RenderEngineBiz(); };
            posRow.AddChild(btn);
        }
        _content.AddChild(posRow);

        _content.AddChild(MkL("", 6, Colors.White));
        _content.AddChild(MkL(Loc.Tr("devmenu.avail_tech"), 13, new Color(0.18f, 0.20f, 0.25f)));

        var availTechs = TechTreeData.AllTech.Values
            .Where(t => _techMgr.IsResearched(t.Id) && !_selectedTechs.Contains(t.Id))
            .ToList();
        if (availTechs.Count == 0)
        {
            _content.AddChild(MkL(Loc.Tr("devmenu.no_tech"), 12, new Color(0.5f, 0.5f, 0.5f)));
        }
        else
        {
            foreach (var tech in availTechs.Take(12))
            {
                float addTime = tech.RequiredManMonths * 0.5f;
                var row = new HBoxContainer();
                row.AddChild(MkL($"{tech.Name} (+{addTime:F0}m  +{addTime:F0})", 12, new Color(0.6f, 0.7f, 0.8f)));
                var addBtn = MkB(Loc.Tr("devmenu.add"), 60, 24, 10);
                var tt = tech; addBtn.Pressed += () => { _selectedTechs.Add(tt.Id); RenderEngineBiz(); };
                row.AddChild(addBtn); _content.AddChild(row);
            }
        }

        if (_selectedTechs.Count > 0)
        {
            _content.AddChild(MkL(Loc.TrF("devmenu.selected", string.Join(", ", _selectedTechs.Select(id => TechTreeData.AllTech[id].Name))), 12, new Color(0.15f, 0.45f, 0.15f)));
            float extraMonths = _selectedTechs.Sum(id => TechTreeData.AllTech[id].RequiredManMonths * 0.5f);
            float extraDebt = _selectedTechs.Sum(id => TechTreeData.AllTech[id].RequiredManMonths * 0.5f);
            _content.AddChild(MkL(Loc.TrF("devmenu.total_months_prefix", months + extraMonths, extraDebt), 13, new Color(1f, 0.7f, 0.3f)));
        }

        if (!string.IsNullOrWhiteSpace(_newEngineName))
        {
            var startBtn = MkB(Loc.Tr("devmenu.start_engine"), 200, 40, 14);
            startBtn.Pressed += () =>
            {
                if (_res.Money < cost) { _gm.ShowPopup(Loc.Tr("devmenu.no_money"), Loc.TrF("devmenu.need_money", cost), new Color(0.9f, 0.3f, 0.3f)); return; }
                var team = _teamMgr.Teams.Find(t => t.Task == TeamTask.None && t.GetTotalSkillLevel(SkillType.Program) >= 3);
                if (team == null) { _gm.ShowPopup(Loc.Tr("devmenu.no_team"), Loc.Tr("devmenu.need_team"), new Color(0.9f, 0.5f, 0.2f)); return; }
                _res.SpendMoney(cost, "devmenu");
                float extraMonths = _selectedTechs.Sum(id => TechTreeData.AllTech[id].RequiredManMonths * 0.5f);
                float extraDebt = _selectedTechs.Sum(id => TechTreeData.AllTech[id].RequiredManMonths * 0.5f);
                var eng = new GameEngine
                {
                    Name = _newEngineName, Generation = 1, TechDebt = extraDebt,
                    AppliedTechs = new List<string>(_selectedTechs),
                    Position = _newEnginePos,
                    IsDeveloping = true, DevMonthsRemaining = (int)(months + extraMonths), DevTeam = team,
                    DevPhaseName = Loc.Tr("devmenu.dev_phase")
                };
                eng.UpdateCapabilities();
                eng.DerivePerks();
                team.Task = TeamTask.DevelopEngine;
                team.CurrentProject = null;
                _gm.Engines.Add(eng);
                _enginePage = EnginePage.Overview;
                _gm.ShowPopup(Loc.Tr("devmenu.engine_start"), Loc.TrF("devmenu.engine_start_msg", eng.Name, 0f), new Color(0.3f, 0.8f, 1f));
                RenderEngineBiz();
            };
            _content.AddChild(startBtn);
        }

        var back = MkB(Loc.Tr("devmenu.back_list"), 150, 30, 12);
        back.Pressed += () => { _enginePage = EnginePage.Overview; RenderEngineBiz(); };
        _content.AddChild(back);
    }

    private void RenderUpgradeEngine()
    {
        if (_selectedEngine == null) { _enginePage = EnginePage.Overview; RenderEngineBiz(); return; }
        var eng = _selectedEngine;
        _content.AddChild(MkL(Loc.TrF("devmenu.upgrade_title", eng.Name, eng.Generation), 18, new Color(0.10f, 0.14f, 0.22f)));
        _content.AddChild(MkL(Loc.TrF("devmenu.current_func", eng.AppliedTechs.Count, eng.TechDebt), 13, new Color(0.18f, 0.20f, 0.25f)));

        _content.AddChild(MkL("", 6, Colors.White));
        _content.AddChild(MkL(Loc.Tr("devmenu.avail_tech"), 14, new Color(0.7f, 0.9f, 0.3f)));

        var avail = TechTreeData.AllTech.Values
            .Where(t => _techMgr.IsResearched(t.Id) && !eng.AppliedTechs.Contains(t.Id) && !_selectedTechs.Contains(t.Id))
            .ToList();
        if (avail.Count == 0)
        {
            _content.AddChild(MkL(Loc.Tr("devmenu.all_integrated"), 12, new Color(0.5f, 0.5f, 0.5f)));
        }
        else
        {
            foreach (var tech in avail.Take(10))
            {
                float addTime = tech.RequiredManMonths * 0.6f;
                var row = new HBoxContainer();
                row.AddChild(MkL(Loc.TrF("devmenu.tech_integrate", tech.Name, addTime, tech.RequiredManMonths * 0.3f), 12, new Color(0.6f, 0.7f, 0.8f)));
                var addBtn = MkB(Loc.Tr("devmenu.add"), 50, 24, 10);
                var tt = tech; addBtn.Pressed += () => { _selectedTechs.Add(tt.Id); RenderEngineBiz(); };
                row.AddChild(addBtn); _content.AddChild(row);
            }
        }

        if (_selectedTechs.Count > 0)
        {
            float totalMonths = _selectedTechs.Sum(id => TechTreeData.AllTech[id].RequiredManMonths * 0.6f);
            float totalDebt = _selectedTechs.Sum(id => TechTreeData.AllTech[id].RequiredManMonths * 0.3f);
            _content.AddChild(MkL(Loc.TrF("devmenu.sel_integrate", string.Join(", ", _selectedTechs.Select(id => TechTreeData.AllTech[id].Name))), 12, new Color(0.15f, 0.45f, 0.15f)));
            _content.AddChild(MkL(Loc.TrF("devmenu.est_integrate", totalMonths, totalDebt), 13, new Color(1f, 0.7f, 0.3f)));
            var goBtn = MkB(Loc.Tr("devmenu.start_upgrade"), 140, 36, 13);
            goBtn.Pressed += () =>
            {
                float tMonths = _selectedTechs.Sum(id => TechTreeData.AllTech[id].RequiredManMonths * 0.6f);
                float tDebt = _selectedTechs.Sum(id => TechTreeData.AllTech[id].RequiredManMonths * 0.3f);
                var team = _teamMgr.Teams.Find(t => t.Task == TeamTask.None);
                if (team == null) { _gm.ShowPopup(Loc.Tr("devmenu.no_team"), Loc.Tr("devmenu.need_team"), new Color(0.9f, 0.3f, 0.3f)); return; }
                eng.AppliedTechs.AddRange(_selectedTechs);
                eng.Generation++;
                eng.TechDebt += tDebt;
                eng.IsDeveloping = true;
                eng.DevMonthsRemaining = (int)Mathf.Ceil(tMonths);
                eng.DevTeam = team;
                eng.DevPhaseName = Loc.TrF("devmenu.upgrade_phase", eng.Generation);
                eng.UpdateCapabilities();
                eng.DerivePerks();
                team.Task = TeamTask.Outsource;
                _selectedTechs.Clear();
                _enginePage = EnginePage.Overview;
                _gm.ShowPopup(Loc.Tr("devmenu.upgrade_start"), Loc.TrF("devmenu.upgrade_msg", eng.Name, 0f), new Color(0.3f, 0.8f, 1f));
                RenderEngineBiz();
            };
            _content.AddChild(goBtn);

            var clrBtn = MkB(Loc.Tr("devmenu.clear_sel"), 100, 28, 12);
            clrBtn.Pressed += () => { _selectedTechs.Clear(); RenderEngineBiz(); };
            _content.AddChild(clrBtn);
        }

        var back = MkB(Loc.Tr("devmenu.back_list"), 150, 30, 12);
        back.Pressed += () => { _selectedTechs.Clear(); _enginePage = EnginePage.Overview; RenderEngineBiz(); };
        _content.AddChild(back);
    }

    private void RenderEngineBizSettings()
    {
        if (_selectedEngine == null) { _enginePage = EnginePage.Overview; RenderEngineBiz(); return; }
        var eng = _selectedEngine;
        _content.AddChild(MkL(Loc.TrF("devmenu.biz_title", eng.Name, eng.Generation), 18, new Color(0.10f, 0.14f, 0.22f)));

        var modelRow = new HBoxContainer();
        modelRow.AddChild(MkL(Loc.Tr("devmenu.model"), 14, new Color(0.18f, 0.20f, 0.25f)));
        var modelOpt = new OptionButton();
        foreach (EngineBizModel m in Enum.GetValues<EngineBizModel>())
            modelOpt.AddItem(m switch { EngineBizModel.Closed => Loc.Tr("devmenu.closed"), EngineBizModel.OpenSource => Loc.Tr("devmenu.opensource"), EngineBizModel.Buyout => Loc.Tr("devmenu.buyout"), EngineBizModel.Subscription => Loc.Tr("devmenu.sub"), EngineBizModel.Royalty => Loc.Tr("devmenu.royalty"), _ => "?" });
        modelOpt.Selected = (int)eng.BizModel;
        modelOpt.AddThemeColorOverride("font_color", new Color(0.10f, 0.10f, 0.10f));
        modelOpt.ItemSelected += (long i) =>
        {
            eng.BizModel = (EngineBizModel)i;
            if (i == (int)EngineBizModel.OpenSource) eng.Reputation += 20;
            RenderEngineBiz();
        };
        modelRow.AddChild(modelOpt); _content.AddChild(modelRow);

        // 定价
        if (eng.BizModel == EngineBizModel.Buyout || eng.BizModel == EngineBizModel.Subscription)
        {
            var label = eng.BizModel == EngineBizModel.Buyout ? Loc.Tr("devmenu.buyout_price") : Loc.Tr("devmenu.monthly_fee");
            var priceRow = new HBoxContainer();
            priceRow.AddChild(MkL(label, 14, new Color(0.18f, 0.20f, 0.25f)));
            float curVal = eng.BizModel == EngineBizModel.Buyout ? eng.BuyoutPrice / 10000f : eng.SubscriptionPrice / 10000f;
            var spin = new SpinBox { MinValue = 1, MaxValue = 200, Value = curVal, Step = 1 };
            spin.ValueChanged += (v) =>
            {
                if (eng.BizModel == EngineBizModel.Buyout) eng.BuyoutPrice = (float)v * 10000;
                else eng.SubscriptionPrice = (float)v * 10000;
            };
            priceRow.AddChild(spin);
            _content.AddChild(priceRow);
        }
        if (eng.BizModel == EngineBizModel.Royalty)
        {
            var rateRow = new HBoxContainer();
            rateRow.AddChild(MkL(Loc.Tr("devmenu.royalty_rate"), 14, new Color(0.18f, 0.20f, 0.25f)));
            var spin = new SpinBox { MinValue = 1, MaxValue = 50, Value = eng.RoyaltyRate * 100, Step = 1 };
            spin.ValueChanged += (v) => eng.RoyaltyRate = (float)v / 100;
            rateRow.AddChild(spin); _content.AddChild(rateRow);
        }

        _content.AddChild(MkL(Loc.TrF("devmenu.license_stat", eng.LicenseCount, eng.MarketShare, eng.Reputation), 13, new Color(0.35f, 0.38f, 0.42f)));
        _content.AddChild(MkL(Loc.TrF("devmenu.revenue_stat", eng.MonthlyRevenue, eng.TotalRevenue), 13, new Color(0.15f, 0.45f, 0.15f)));

        // ── AI授权开关 ──
        var licenseAI = new CheckButton { Text = Loc.Tr("devmenu.allow_ai_license"), ButtonPressed = _techMgr.EngineOpenForLicense, SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd };
        licenseAI.Toggled += (v) => {
            _techMgr.EngineOpenForLicense = v;
            _gm.ShowToast(v ? Loc.Tr("devmenu.ai_license_on") : Loc.Tr("devmenu.ai_license_off"), "", v ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.7f, 0.3f, 0.2f));
        };
        _content.AddChild(licenseAI);

        var back = MkB(Loc.Tr("devmenu.back_list"), 150, 30, 12);
        back.Pressed += () => { _enginePage = EnginePage.Overview; RenderEngineBiz(); };
        _content.AddChild(back);
    }

    // ══════════════════ 外包中心（接包 + 发包）══════════════════
    private void RenderOutsource()
    {
        ShowPage("outsource");
        _content.AddChild(MkL(Loc.Tr("devmenu.outsource"), 18, new Color(0.10f, 0.14f, 0.22f)));

        // ── 接包（承接外部合同赚钱）──
        _content.AddChild(MkL(Loc.Tr("devmenu.avail_contracts"), 14, new Color(0.25f, 0.28f, 0.32f)));
        if (OutsourceTasks.Count == 0)
        {
            _content.AddChild(MkL(Loc.Tr("devmenu.no_contracts"), 12, new Color(0.35f, 0.38f, 0.42f)));
        }
        else
        {
            foreach (var task in OutsourceTasks)
            {
                string status = task.Accepted ? Loc.TrF("devmenu.in_progress", task.MonthsSpent, task.Duration) : "";
                string typeLabel = task.IsHighRisk ? "⚠高风险" : task.GivesInspiration ? "💡灵感" : task.GivesTechBoost ? "⚡科技" : task.GivesReputation ? "⭐声誉" : "";
                var row = new HBoxContainer();
                row.AddChild(MkL($"{typeLabel} {task.Name} | ¥{task.Reward:N0} | {task.Duration}{Loc.Tr("devmenu.months_unit")} | {Loc.Tr("devmenu.need_prefix")}{task.RequiredSkill.Name()}Lv{task.RequiredLevel} {status}", 11, task.Accepted ? new Color(0.15f, 0.45f, 0.15f) : new Color(0.18f, 0.20f, 0.25f)));
                if (!task.Accepted)
                {
                    var acceptBtn = MkB(Loc.Tr("devmenu.accept"), 60, 26, 11);
                    var t = task;
                    acceptBtn.Pressed += () =>
                    {
                        var team = _teamMgr.Teams.Find(x => x.Task == TeamTask.None);
                        if (team == null) { _gm.ShowPopup(Loc.Tr("devmenu.no_team"), Loc.Tr("devmenu.no_idle_free"), new Color(0.9f, 0.3f, 0.3f)); return; }
                        if (team.GetTotalSkillLevel(t.RequiredSkill) < t.RequiredLevel)
                        { _gm.ShowPopup(Loc.Tr("devmenu.no_team"), Loc.TrF("devmenu.skill_low", task.RequiredSkill.Name(), task.RequiredLevel), new Color(0.9f, 0.5f, 0.2f)); return; }
                        t.Accepted = true;
                        t.AssignedTeam = team;
                team.Task = TeamTask.DevelopEngine;
                        team.CurrentProject = null;
                        team.CurrentContract = new OutsourceContract { PrimarySkill = t.RequiredSkill, Name = t.Name };
                        RenderOutsource();
                    };
                    row.AddChild(acceptBtn);
                }
                _content.AddChild(row);
            }
        }

        // ── 发包（花钱外发项目模块）──
        _content.AddChild(MkL("", 6, Colors.White));
        _content.AddChild(MkL(Loc.Tr("devmenu.outsource_send"), 14, new Color(0.25f, 0.28f, 0.32f)));

        var devProjects = _devMgr.Projects.FindAll(p => p.Phase == DevPhase.Developing);
        if (devProjects.Count == 0)
        {
            _content.AddChild(MkL(Loc.Tr("devmenu.no_active_project"), 12, new Color(0.35f, 0.38f, 0.42f)));
        }
        else
        {
            foreach (var proj in devProjects)
            {
                var modNames = Loc.ParseModNames();
                if (modNames.Length < 6) modNames = new[] { "Core", "Visual", "Audio", "Story", "Stability", "Online" };
                float[] costs = { 50000, 30000, 20000, 15000, 30000, 50000 };
                int[] durations = { 3, 2, 2, 2, 2, 3 };

                for (int i = 0; i < 6; i++)
                {
                    int idx = i;
                    var row = new HBoxContainer();
                    row.AddChild(MkL($"  {modNames[i]}: ¥{costs[i]:N0} / {durations[i]}{Loc.Tr("devmenu.months_unit")}", 11, new Color(0.35f, 0.38f, 0.42f)));
                    var outBtn = MkB(Loc.Tr("devmenu.send_out"), 50, 22, 9);
                    var p = proj;
                    outBtn.Pressed += () =>
                    {
                        if (!_res.SpendMoney(costs[idx], "outsource"))
                        {
                            _gm.ShowPopup(Loc.Tr("devmenu.no_money"), Loc.Tr("devmenu.no_money_out_msg"), new Color(0.9f, 0.3f, 0.3f));
                            return;
                        }
                        float add = (float)durations[idx] / p.EstimatedMonths * 0.5f;
                        switch (idx)
                        {
                            case 0: p.ModuleProgressCore += add; p.GameplayScore += Random.Shared.Next(5, 16); break;
                            case 1: p.ModuleProgressVisual += add; p.GraphicsScore += Random.Shared.Next(5, 16); break;
                            case 2: p.ModuleProgressAudio += add; p.AudioScore += Random.Shared.Next(5, 16); break;
                            case 3: p.ModuleProgressStory += add; p.StoryScore += Random.Shared.Next(5, 16); break;
                            case 4: p.ModuleProgressStability += add; p.StabilityScore += 5; break;
                            case 5: p.ModuleProgressOnline += add; p.NetworkScore += Random.Shared.Next(5, 16); break;
                        }
                        p.DevProgress += add / 6;
                        p.DevLog.Add(Loc.TrF("devmenu.outsource_log", modNames[idx], add * 100));
                        RenderOutsource();
                    };
                    row.AddChild(outBtn);
                    _content.AddChild(row);
                }
            }
        }

        var back = MkB(Loc.Tr("devmenu.back_main"), 140, 30, 12);
        back.Pressed += () => RenderMainMenu();
        _content.AddChild(back);
    }

    // ══════════════════ 立项预估基础分 ───────────────
    private float EstimateRawScore()
    {
        if (_selGenre == null || _selTheme == null) return 50;
        var teams = _teamMgr.Teams.Where(t => t.Members.Count > 0).ToList();
        if (teams.Count == 0) return 40;

        // 模拟项目初始六维（和 CreateProject 的逻辑对齐）
        float compat = GameDevManager.GetCompat(_selGenre.Value, _selTheme.Value);
        float gfx = 30, gp = 25 + compat * 15, aud = 20, net = 10, story = 15, stab = 50;

        // 组件加成
        var equippedObjs = GameComponentDB.All.Where(c => _selComponents.Contains(c.Id)).ToArray();
        foreach (var c in equippedObjs)
            foreach (var (attr, bonus) in c.Effects)
                switch (attr)
                {
                    case "graphics": gfx += bonus; break;
                    case "gameplay": gp += bonus; break;
                    case "audio": aud += bonus; break;
                    case "story": story += bonus; break;
                    case "network": net += bonus; break;
                    case "stability": stab += bonus; break;
                }
        var synergies = GameComponentDB.ComputeSynergies(equippedObjs);
        gp += synergies.Values.Sum() * 0.5f;

        // 科技加成（简化为统计已研究科技数量加权）
        int techCount = _techMgr.ResearchedTech.Count(kv => kv.Value);
        float techMul = 1f + techCount * 0.03f;
        gfx *= techMul; gp *= techMul; aud *= techMul; story *= techMul; net *= techMul; stab *= techMul;

        // 评分公式（和 ReleaseGame 一致）
        float rawScore = (gfx * 0.2f + gp * 0.3f + aud * 0.1f + story * 0.15f + net * 0.1f + stab * 0.15f)
                       * (0.9f + _selScale * 0.2f);

        // 类型契合度奖励
        if (compat >= 0.9f) rawScore += 15;
        else if (compat <= 0.25f) rawScore -= 30;
        else if (compat <= 0.4f) rawScore -= 15;

        // 蓝海开拓者
        int uses = _devMgr.CountGenreThemeUses(_selGenre.Value, _selTheme.Value);
        if (uses == 0) rawScore += 25;

        return Mathf.Clamp(rawScore, 10, 95);
    }

    // ══════════════════ 立项配置 ══════════════════
    private void RenderPlanning()
    {
        ShowPage("planning");
        // 教程通知
        _gm.GetNodeOrNull<TutorialManager>("TutorialManager")?.NotifyAction("planning_opened");
        string prefix = _isSequel ? Loc.Tr("devmenu.sequel") : Loc.Tr("devmenu.new_project");
        _content.AddChild(MkL($"{prefix}: {_selGenre.Value.Name()} × {_selTheme.Value.Name()}", 18, new Color(0.10f, 0.14f, 0.22f)));

        // ── 蓝海/红海提示 ──
        int uses = _devMgr.CountGenreThemeUses(_selGenre.Value, _selTheme.Value);
        if (uses == 0)
            _content.AddChild(MkL(Loc.Tr("planning.blue_ocean"), 13, new Color(0.1f, 0.5f, 0.8f)));
        else if (uses >= 5)
            _content.AddChild(MkL(Loc.TrF("planning.red_ocean", uses), 11, new Color(0.7f, 0.2f, 0.2f)));

        // ── 项目名称 ──
        var nameRow = new HBoxContainer();
        nameRow.AddChild(MkL(Loc.Tr("devmenu.project_name"), 14, new Color(0.18f, 0.20f, 0.25f)));
        var projNameEdit = new LineEdit { CustomMinimumSize = new(Sf(260), 0), Text = _customProjName, PlaceholderText = Loc.Tr("devmenu.name_hint") };
        projNameEdit.TextChanged += (s) => _customProjName = s;
        nameRow.AddChild(projNameEdit);
        _content.AddChild(nameRow);

        // ── 设置前作/IP（可选）──
        _content.AddChild(MkL("", 4, Colors.Transparent));
        _content.AddChild(MkL(Loc.Tr("devmenu.predecessor"), 14, new Color(0.18f, 0.20f, 0.25f)));
        if (_sequelBase != null)
        {
            var predRow = new HBoxContainer();
            predRow.AddChild(MkL(Loc.TrF("devmenu.pred_current", _sequelBase.Name, _sequelBase.FinalScore), 13, new Color(0.9f, 0.45f, 0.15f)));
            var clearBtn = MkB(Loc.Tr("devmenu.pred_clear"), 60, 26, 11);
            clearBtn.Pressed += () =>
            {
                _sequelBase = null; _isSequel = false; _canReuse = false;
                RenderPlanning();
            };
            predRow.AddChild(clearBtn);
            _content.AddChild(predRow);

            // 续作策略
            _content.AddChild(MkL(Loc.TrF("devmenu.sequel_info", _sequelBase.Name, _sequelBase.FinalScore, Mathf.Clamp(_sequelBase.FinalScore * 1.2f, 40, 100)), 13, new Color(0.9f, 0.5f, 0.2f)));
            var seqRow = new HBoxContainer();
            seqRow.AddChild(MkL(Loc.Tr("devmenu.sequel_strategy"), 14, new Color(0.18f, 0.20f, 0.25f)));
            var seqOpt = new OptionButton();
            seqOpt.AddItem(Loc.Tr("devmenu.seq_stable"));
            seqOpt.AddItem(Loc.Tr("devmenu.seq_innovate"));
            seqOpt.AddItem(Loc.Tr("devmenu.seq_spinoff"));
            seqOpt.Selected = (int)_selSequelStrat;
            seqOpt.ItemSelected += (long i) => { _selSequelStrat = (SequelStrategy)i; };
            seqOpt.AddThemeColorOverride("font_color", new Color(0.10f, 0.10f, 0.10f));
            seqRow.AddChild(seqOpt);
            _content.AddChild(seqRow);
        }
        else
        {
            var completed = _devMgr.CompletedProjects.ToList();
            if (completed.Count > 0)
            {
                _content.AddChild(MkL("  " + Loc.Tr("devmenu.pred_none"), 12, new Color(0.4f, 0.45f, 0.5f)));
                foreach (var cp in completed)
                {
                    var predRow2 = new HBoxContainer();
                    predRow2.AddChild(MkL($"  {cp.Name}  [{cp.Genre.Name()}×{cp.Theme.Name()}]  {cp.FinalScore:F0}分", 12, new Color(0.30f, 0.33f, 0.38f)));
                    var pickBtn = MkB(Loc.Tr("devmenu.pred_use"), 60, 24, 10);
                    var pp = cp;
                    pickBtn.Pressed += () =>
                    {
                        _sequelBase = pp; _isSequel = true;
                        _selGenre = pp.Genre; _selTheme = pp.Theme;
                        _canReuse = true; _selPlatform = pp.Platform;
                        if (string.IsNullOrWhiteSpace(_customProjName))
                            _customProjName = $"{pp.Name} 2";
                        RenderPlanning();
                    };
                    predRow2.AddChild(pickBtn);
                    _content.AddChild(predRow2);
                }
            }
            else
                _content.AddChild(MkL("  " + Loc.Tr("devmenu.pred_no_completed"), 12, new Color(0.5f, 0.5f, 0.5f)));
        }

        // ── 选择引擎 ──
        _content.AddChild(MkL("", 4, Colors.Transparent));
        _content.AddChild(MkL(Loc.Tr("devmenu.select_engine"), 14, new Color(0.18f, 0.20f, 0.25f)));
        var readyEngines = _gm.Engines.Where(e => !e.IsDeveloping && !e.IsDeprecated).ToList();
        if (_selEngine == null && readyEngines.Count > 0) _selEngine = readyEngines[0];
        if (readyEngines.Count == 0)
            _content.AddChild(MkL(Loc.Tr("devmenu.no_engine"), 12, new Color(0.9f, 0.3f, 0.3f)));
        else
        {
            foreach (var eng in readyEngines)
            {
                string perks = eng.Perks.Count > 0 ? " " + string.Join("", eng.Perks.Take(2).Select(p => PerkIcon(p))) : "";
                string compatStr = GetEngineCompatShort(eng, _selGenre!.Value);
                string info = $"{eng.Name} G{eng.Generation}{(eng.TechDebt > 0 ? $" 债{eng.TechDebt}" : "")}{perks}{compatStr}";
                var engBtn = MkB(info, _selEngine == eng ? 380 : 360, 26, 11);
                if (_selEngine == eng) engBtn.Modulate = new Color(0.5f, 1f, 0.5f);
                var e = eng; engBtn.Pressed += () => { _selEngine = e; RenderPlanning(); };
                _content.AddChild(engBtn);
            }
        }

        if (_canReuse || _isSequel)
        {
            _reuseCheck = new CheckBox { Text = Loc.Tr("devmenu.reuse_code") };
            _reuseCheck.AddThemeFontSizeOverride("font_size", 12);
            _reuseCheck.AddThemeColorOverride("font_color", new Color(1f, 0.7f, 0.3f));
            _reuseCheck.AddThemeColorOverride("font_hover_color", new Color(0.40f, 0.40f, 0.40f));
            _reuseCheck.AddThemeColorOverride("icon_hover_color", new Color(0.40f, 0.40f, 0.40f));
            if (_isSequel) _reuseCheck.ButtonPressed = true;
            _content.AddChild(_reuseCheck);
        }

        var platRow = new HBoxContainer();
        platRow.AddChild(MkL(Loc.Tr("devmenu.platform"), 14, new Color(0.18f, 0.20f, 0.25f)));
        var platOpt = new OptionButton();
        foreach (Platform p in Enum.GetValues<Platform>())
        {
            bool canUse = p == Platform.PC || (p == Platform.Console && _techMgr.IsResearched("cross_v1")) || (p == Platform.All && _techMgr.IsResearched("cross_v2"));
            platOpt.AddItem(p.Name()); if (!canUse) platOpt.SetItemDisabled(platOpt.ItemCount - 1, true);
        }
        platOpt.Selected = (int)_selPlatform;
        platOpt.ItemSelected += (long i) => _selPlatform = (Platform)i;
        platOpt.AddThemeColorOverride("font_color", new Color(0.10f, 0.10f, 0.10f));
        platRow.AddChild(platOpt);
        _content.AddChild(platRow);

        var mktRow = new HBoxContainer();
        mktRow.AddChild(MkL(Loc.Tr("dev.marketing") + ":", 14, new Color(0.18f, 0.20f, 0.25f)));
        var mktOpt = new OptionButton();
        foreach (MarketingStrategy m in Enum.GetValues<MarketingStrategy>()) mktOpt.AddItem(m.Name());
        mktOpt.Selected = (int)_selMkt; mktOpt.ItemSelected += (long i) => _selMkt = (MarketingStrategy)i;
        mktOpt.AddThemeColorOverride("font_color", new Color(0.10f, 0.10f, 0.10f));
        mktRow.AddChild(mktOpt); _content.AddChild(mktRow);

        var budgetRow = new HBoxContainer();
        budgetRow.AddChild(MkL(Loc.Tr("dev.mkt_budget") + ": ¥", 14, new Color(0.18f, 0.20f, 0.25f)));
        var budgetInput = new LineEdit { Text = _selBudget.ToString("F0"), CustomMinimumSize = new(100, 0) };
        budgetInput.TextSubmitted += (s) => {
            if (long.TryParse(s, out var v)) _selBudget = Mathf.Clamp(v, 0, 5000000);
            budgetInput.Text = _selBudget.ToString("F0");
        };
        budgetInput.FocusExited += () => {
            if (long.TryParse(budgetInput.Text, out var v)) _selBudget = Mathf.Clamp(v, 0, 5000000);
            budgetInput.Text = _selBudget.ToString("F0");
        };
        budgetRow.AddChild(budgetInput); _content.AddChild(budgetRow);

        // ── 项目规模滑块 ──
        var scaleRow = new HBoxContainer();
        float maxScale = _gm.GameYear < 5 ? 0.4f : 1f;
        if (_selScale > maxScale) _selScale = maxScale;
        var scaleLabel = MkL($"{Loc.Tr("dev.scale")} {_selScale * 100:F0}%  → ${Mathf.Lerp(5, 60, _selScale):F0}", 14, new Color(0.18f, 0.20f, 0.25f));
        scaleRow.AddChild(scaleLabel);
        var scaleSlider = new HSlider { MinValue = 0, MaxValue = 1, Value = (_selScale - 0.1f) / (maxScale - 0.1f), Step = 0.01, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(Sf(100), Sf(16)) };
        scaleSlider.ValueChanged += (v) => { _selScale = Mathf.Clamp((float)v * (maxScale - 0.1f) + 0.1f, 0.1f, maxScale); scaleLabel.Text = $"{Loc.Tr("dev.scale")} {_selScale * 100:F0}%  → ${Mathf.Lerp(5, 60, _selScale):F0}"; };
        scaleRow.AddChild(scaleSlider); _content.AddChild(scaleRow);
        _content.AddChild(MkL(Loc.Tr("dev.scale_hint"), 11, new Color(0.4f, 0.5f, 0.6f)));

        // ── 付费模式 ──
        var priceRow = new HBoxContainer();
        priceRow.AddChild(MkL(Loc.Tr("dev.price_model") + ":", 14, new Color(0.18f, 0.20f, 0.25f)));
        var priceOpt = new OptionButton();
        priceOpt.AddItem(Loc.Tr("dev.buy_to_play"));
        priceOpt.AddItem(Loc.Tr("dev.free_play"));
        priceOpt.Selected = _selPrice == PriceModel.BuyToPlay ? 0 : 1;
        priceOpt.ItemSelected += (long i) => {
            _selPrice = i == 0 ? PriceModel.BuyToPlay : PriceModel.Free;
            RenderPlanning();
        };
        priceOpt.AddThemeColorOverride("font_color", new Color(0.10f, 0.10f, 0.10f));
        priceRow.AddChild(priceOpt); _content.AddChild(priceRow);

        // ── 广告/内购强度（仅免费游戏显示）──
        if (_selPrice == PriceModel.Free)
        {
            var adRow = new HBoxContainer();
            var adLabel = MkL(Loc.Tr("dev.ad_intensity") + $" {_selAdIntensity * 100:F0}%", 14, new Color(0.18f, 0.20f, 0.25f));
            adRow.AddChild(adLabel);
            var adSlider = new HSlider { MinValue = 0, MaxValue = 1, Value = _selAdIntensity, Step = 0.05, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(Sf(100), Sf(16)) };
            adSlider.ValueChanged += (v) => { _selAdIntensity = (float)v; adLabel.Text = Loc.Tr("dev.ad_intensity") + $" {_selAdIntensity * 100:F0}%"; };
            adRow.AddChild(adSlider); _content.AddChild(adRow);
            _content.AddChild(MkL(Loc.Tr("devmenu.ad_hint_dev"), 11, new Color(0.4f, 0.5f, 0.6f)));
        }

        // ── 设计哲学 ──
        _content.AddChild(MkL("", 4, Colors.Transparent));
        _content.AddChild(MkL(Loc.Tr("devmenu.design_philosophy"), 14, new Color(0.10f, 0.14f, 0.22f)));

        var philoRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        string curName = _selPhilosophy switch {
            DesignPhilosophy.Innovative => Loc.Tr("design.philosophy.innovative.name"),
            DesignPhilosophy.Polished => Loc.Tr("design.philosophy.polished.name"),
            DesignPhilosophy.Niche => Loc.Tr("design.philosophy.niche.name"),
            _ => Loc.Tr("design.philosophy.balanced.name"),
        };
        string curDesc = _selPhilosophy switch {
            DesignPhilosophy.Innovative => Loc.Tr("design.philosophy.innovative.desc"),
            DesignPhilosophy.Polished => Loc.Tr("design.philosophy.polished.desc"),
            DesignPhilosophy.Niche => Loc.Tr("design.philosophy.niche.desc"),
            _ => Loc.Tr("design.philosophy.balanced.desc"),
        };
        var philoName = MkL(curName, 12, new Color(0.2f, 0.5f, 0.2f));
        var philoDesc = MkL(curDesc, 10, new Color(0.4f, 0.5f, 0.4f));
        philoRow.AddChild(philoName); philoRow.AddChild(philoDesc);
        _content.AddChild(philoRow);

        // ── 动态预估分数区间 ──
        var philoPredict = MkL("", 11, new Color(0.25f, 0.35f, 0.5f));
        _content.AddChild(philoPredict);
        void RefreshPhiloPredict()
        {
            float estBase = EstimateRawScore(); // 基于当前立项配置估算基础分
            (string pred, Color c) = _selPhilosophy switch
            {
                DesignPhilosophy.Innovative => (Loc.TrF("design.philosophy.innovative.predict", estBase - 5f, estBase + 20f), new Color(0.15f, 0.4f, 0.7f)),
                DesignPhilosophy.Polished => (Loc.TrF("design.philosophy.polished.predict", estBase - 2f, estBase + 10f), new Color(0.15f, 0.5f, 0.3f)),
                DesignPhilosophy.Niche => (Loc.Tr("design.philosophy.niche.predict"), new Color(0.7f, 0.35f, 0.15f)),
                _ => (Loc.TrF("design.philosophy.balanced.predict", estBase - 5f, estBase + 8f), new Color(0.2f, 0.45f, 0.2f)),
            };
            philoPredict.Text = pred;
            philoPredict.AddThemeColorOverride("font_color", c);
        }
        RefreshPhiloPredict();

        // 设计哲学单选组（CheckBox 模拟单选）
        var philoGroup = new VBoxContainer();
        foreach (var ph in new[] { DesignPhilosophy.Balanced, DesignPhilosophy.Innovative, DesignPhilosophy.Polished, DesignPhilosophy.Niche })
        {
            var cb = new CheckBox { Text = Loc.Tr($"design.philosophy.{ph.ToString().ToLower()}.name"), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            var phCaptured = ph;
            cb.AddThemeFontSizeOverride("font_size", 12);
            cb.SelfModulate = new Color(0.15f, 0.18f, 0.22f);
            cb.MouseEntered += () => { if (!cb.ButtonPressed) cb.SelfModulate = new Color(0.4f, 0.4f, 0.4f); };
            cb.MouseExited += () => { if (!cb.ButtonPressed) cb.SelfModulate = new Color(0.15f, 0.18f, 0.22f); };
            if (ph == _selPhilosophy) { cb.ButtonPressed = true; cb.SelfModulate = new Color(0, 0.6f, 0.1f); }
            cb.Toggled += (on) => {
                cb.SelfModulate = on ? new Color(0, 0.6f, 0.1f) : new Color(0.15f, 0.18f, 0.22f);
                if (!on) return;
                _selPhilosophy = phCaptured;
                foreach (Node child in philoGroup.GetChildren())
                    if (child is CheckBox other && other != cb)
                    {
                        other.ButtonPressed = false;
                        other.SelfModulate = new Color(0.15f, 0.18f, 0.22f);
                    }
                (string n, string d) = _selPhilosophy switch {
                    DesignPhilosophy.Innovative => (Loc.Tr("design.philosophy.innovative.name"), Loc.Tr("design.philosophy.innovative.desc")),
                    DesignPhilosophy.Polished => (Loc.Tr("design.philosophy.polished.name"), Loc.Tr("design.philosophy.polished.desc")),
                    DesignPhilosophy.Niche => (Loc.Tr("design.philosophy.niche.name"), Loc.Tr("design.philosophy.niche.desc")),
                    _ => (Loc.Tr("design.philosophy.balanced.name"), Loc.Tr("design.philosophy.balanced.desc")),
                };
                philoName.Text = n; philoDesc.Text = d;
                RefreshPhiloPredict();
            };
            philoGroup.AddChild(cb);
        }
        _content.AddChild(philoGroup);

        // ── 游戏组件装配 ──
        _content.AddChild(MkL("", 6, Colors.White));
        int compPct = _selComponents.Count > 3 ? (_selComponents.Count - 3) * 10 : 0;
        _content.AddChild(MkL(Loc.TrF("devmenu.components_title", _selComponents.Count, compPct), 15, new Color(0.10f, 0.14f, 0.22f)));
        _content.AddChild(MkL(Loc.Tr("devmenu.components_hint"), 11, new Color(0.35f, 0.38f, 0.42f)));

        var unlockedComps = GameComponentDB.GetUnlocked(_techMgr);
        if (unlockedComps.Length == 0)
            _content.AddChild(MkL(Loc.Tr("devmenu.no_components"), 11, new Color(0.5f, 0.5f, 0.5f)));

        // 按类别分组显示
        foreach (ComponentCategory cat in Enum.GetValues<ComponentCategory>())
        {
            var catComps = unlockedComps.Where(c => c.Category == cat).ToArray();
            if (catComps.Length == 0) continue;

            var catRow = new HBoxContainer();
            catRow.AddChild(MkL($"{cat.Name()}:", 11, new Color(0.25f, 0.28f, 0.32f)));

            foreach (var comp in catComps)
            {
                bool equipped = _selComponents.Contains(comp.Id);
                // 联动检测：如果某个已装备组件与当前组件共享标签，高亮
                bool hasSynergy = equipped ? false : _selComponents.Any(cid =>
                {
                    var other = GameComponentDB.All.FirstOrDefault(x => x.Id == cid);
                    return other.SynergyTags != null && comp.SynergyTags != null && other.SynergyTags.Intersect(comp.SynergyTags).Any();
                });

                var compBtn = new Button
                {
                    Text = Loc.TrF("devmenu.comp_btn", comp.Name(), equipped ? Loc.Tr("devmenu.comp_equipped") : Loc.Tr("devmenu.comp_add")),
                    CustomMinimumSize = new(120, 26),
                    Size = new(120, 26)
                };
                compBtn.AddThemeFontSizeOverride("font_size", 10);
                compBtn.AddThemeColorOverride("font_color",
                    equipped ? new Color(0.15f, 0.5f, 0.15f) :
                    hasSynergy ? new Color(0.7f, 0.45f, 0.1f) :
                    new Color(0.18f, 0.20f, 0.25f));
                compBtn.AddThemeStyleboxOverride("normal", new StyleBoxFlat
                {
                    BgColor = equipped ? new Color(0.65f, 0.9f, 0.65f, 0.5f) :
                              hasSynergy ? new Color(1f, 0.9f, 0.6f, 0.5f) :
                              new Color(0.97f, 0.96f, 0.94f, 0.9f),
                    BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
                    BorderColor = hasSynergy ? new Color(0.9f, 0.6f, 0.2f, 0.6f) : new Color(0.7f, 0.7f, 0.7f, 0.4f),
                    CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
                    CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3
                });

                compBtn.Pressed += () =>
                {
                    if (equipped)
                        _selComponents.Remove(comp.Id);
                    else
                        _selComponents.Add(comp.Id);
                    RenderPlanning();
                };
                catRow.AddChild(compBtn);
            }
            _content.AddChild(catRow);
        }

        // 联动统计
        if (_selComponents.Count >= 2)
        {
            var synergies = GameComponentDB.ComputeSynergies(
                GameComponentDB.All.Where(c => _selComponents.Contains(c.Id)).ToArray());
            if (synergies.Count > 0)
                _content.AddChild(MkL(Loc.TrF("devmenu.synergy_bonus", string.Join(" | ", synergies.Select(kv => $"{Loc.Tr($"synergy.{kv.Key}")}+{kv.Value:F1}"))), 11, new Color(0.7f, 0.5f, 0.15f)));
        }

        // 进度示意（百分比）
        _content.AddChild(MkL("📊 开发进度: 0% → 100%（开发完成后发售）", 13, new Color(0.5f, 0.7f, 0.8f)));

        float baseMonths = 10;
        var btnRow = new HBoxContainer();
        var backBtn = MkB(Loc.Tr("dev.back"), 100, 36, 13);
        backBtn.Pressed += () => RenderGenreSelect();
        btnRow.AddChild(backBtn);

        var createBtn = MkB(Loc.Tr("dev.create_assign"), 180, 36, 13);
        createBtn.Pressed += () =>
        {
            if (_empMgr.Employees.Count == 0)
            { _gm.ShowToast(Loc.Tr("toast.no_employee"), Loc.Tr("toast.no_employee_dev"), new Color(0.9f, 0.3f, 0.2f)); return; }
            float months = baseMonths; // 基础工期，CreateProject会自行应用各种乘数
            string projName = string.IsNullOrWhiteSpace(_customProjName)
                ? Loc.TrF("devpop.proj_name", _selGenre!.Value.Name(), Guid.NewGuid().ToString()[..4])
                : _customProjName;
            var proj = _devMgr.CreateProject(projName, _selGenre!.Value, _selTheme!.Value, _selPlatform, months, _selMkt, _selBudget, _selScale, _selPrice, _selAdIntensity, _selComponents,
                philosophy: _selPhilosophy);
            if (proj != null)
            {
                // 绑定所选引擎
                if (_selEngine != null) proj.EngineName = _selEngine.Name;
                // 续作信息
                if (_isSequel && _sequelBase != null)
                {
                    proj.PredecessorScore = _sequelBase.FinalScore;
                    proj.PredecessorSales = _sequelBase.Sales;
                    proj.SequelStrat = _selSequelStrat;
                    // 应用续作策略
                    ApplySequelStrategy(proj);
                }
                if (_canReuse && _reuseCheck != null && _reuseCheck.ButtonPressed)
                    _debtMgr.ApplyCodeReuse(proj);
                RenderTeamAssign();
            }
        };
        btnRow.AddChild(createBtn);
        _content.AddChild(btnRow);
    }

    // ══════════════════ 分配团队 ══════════════════
    private void ApplySequelStrategy(GameProject proj)
    {
        switch (proj.SequelStrat)
        {
            case SequelStrategy.Cautious:
                proj.EstimatedMonths *= 0.7f;
                proj.DevLog.Add(Loc.Tr("devmenu.seq_stable_log"));
                break;
            case SequelStrategy.Revolutionary:
                proj.EstimatedMonths *= 1.5f;
                proj.DevLog.Add(Loc.Tr("devmenu.seq_innovate_log"));
                break;
            case SequelStrategy.Derivative:
                proj.DevLog.Add(Loc.Tr("devmenu.seq_spinoff_log"));
                break;
        }
    }

    private void RenderTeamAssign()
    {
        ShowPage("team");
        _content.AddChild(MkL("团队管理", 18, new Color(0.10f, 0.14f, 0.22f)));

        // 待分配项目提示
        var pending = _devMgr.Projects.FindAll(p => p.Phase == DevPhase.Planning);
        if (pending.Count > 0)
        {
            foreach (var proj in pending)
                _content.AddChild(MkL($"📋 待分配: {proj.Name} ({proj.Genre.Name()}×{proj.Theme.Name()})", 13, new Color(0.7f, 0.5f, 0.2f)));
            _content.AddChild(MkL("", 4, Colors.White));
        }

        // ── 团队列表（公司页面风格：双击团队名看详情）──
        _content.AddChild(MkL("点击团队名查看详情", 11, new Color(0.4f, 0.45f, 0.5f)));

        // 表头
        var header = new HBoxContainer();
        header.AddChild(MkL("团队名", 13, new Color(0.12f, 0.16f, 0.25f)));
        header.AddChild(MkL("人数", 13, new Color(0.12f, 0.16f, 0.25f)));
        header.AddChild(MkL("月工资", 13, new Color(0.12f, 0.16f, 0.25f)));
        header.AddChild(MkL("任务", 13, new Color(0.12f, 0.16f, 0.25f)));
        header.AddChild(MkL(" ", 13, new Color(0.12f, 0.16f, 0.25f)));
        _content.AddChild(header);

        if (_teamMgr.Teams.Count == 0)
        {
            _content.AddChild(MkL("暂无团队", 13, new Color(0.6f, 0.3f, 0.2f)));
        }
        else foreach (var team in _teamMgr.Teams)
        {
            var row = new HBoxContainer();

            string taskInfo = team.Task == TeamTask.None ? "空闲"
                : team.Task == TeamTask.DevelopGame ? $"开发《{team.CurrentProject?.Name}》"
                : team.Task == TeamTask.ResearchTech ? "研发科技"
                : team.Task == TeamTask.Refactor ? "重构中"
                : team.Task == TeamTask.DevelopEngine ? "开发引擎"
                : team.Task == TeamTask.Outsource ? "外包中"
                : "忙碌";

            Color taskColor = team.Task == TeamTask.None ? new Color(0.15f, 0.55f, 0.15f)
                : new Color(0.55f, 0.35f, 0.1f);

            // 团队名（可点击进详情）
            var nameBtn = new Button { Text = $"{team.Name}", Flat = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            nameBtn.AddThemeFontSizeOverride("font_size", 12);
            nameBtn.AddThemeColorOverride("font_color", new Color(0.08f, 0.14f, 0.25f));
            nameBtn.Alignment = HorizontalAlignment.Left;
            var tRef = team;
            nameBtn.Pressed += () => ShowTeamDetail(tRef);
            row.AddChild(nameBtn);

            // 人数
            row.AddChild(MkL($"{team.Members.Count}人", 12, new Color(0.2f, 0.25f, 0.3f)));

            // 团队月工资总和
            float teamSalary = team.Members.Sum(m => m.Salary);
            row.AddChild(MkL($"¥{teamSalary:N0}", 12, new Color(0.15f, 0.2f, 0.3f)));

            // 任务
            row.AddChild(MkL(taskInfo, 12, taskColor));

            // 操作按钮：空闲团队 + 有待分配项目 → 分配
            if (team.Task == TeamTask.None && pending.Count > 0)
            {
                var assignBtn = MkB(Loc.Tr("dev.assign_dev"), 100, 24, 10);
                var tAssign = team;
                assignBtn.Pressed += () =>
                {
                    var proj = pending[0];
                    if (_devMgr.StartDevelopment(proj, tAssign))
                        RenderTeamAssign();
                };
                row.AddChild(assignBtn);
            }
            else
            {
                row.AddChild(MkL(" ", 12, Colors.White));
            }

            _content.AddChild(row);
        }

        _content.AddChild(MkL("", 8, Colors.White));

        // ── 底部操作栏 ──
        var botRow = new HBoxContainer();
        var createTeamBtn = MkB("创建新团队", 140, 32, 12);
        createTeamBtn.Pressed += () =>
        {
            var newTeam = _teamMgr.CreateTeam($"团队{_teamMgr.Teams.Count + 1}", new List<Employee>());
            _gm.ShowToast("团队已创建", $"{newTeam.Name}: 0名成员（请手动分配）", new Color(0.3f, 0.7f, 0.5f));
            RenderTeamAssign();
        };
        botRow.AddChild(createTeamBtn);

        var sep = new Control { CustomMinimumSize = new(20, 1) };
        botRow.AddChild(sep);

        var viewAllBtn = MkB("查看所有员工 →", 150, 32, 12);
        viewAllBtn.Pressed += () => RenderEmployeePage();
        botRow.AddChild(viewAllBtn);
        _content.AddChild(botRow);

        // ── 底部汇总 ──
        int totalEmp = _empMgr.Employees.Count;
        float totalSalary = _empMgr.Employees.Sum(e => e.Salary);
        float idleCount = _empMgr.Employees.Count(e => e.TeamName == null);
        _content.AddChild(MkL($"📊 汇总: {_teamMgr.Teams.Count}团队 {totalEmp}人 | 空闲{idleCount}人 | 月薪¥{totalSalary:N0}", 10, new Color(0.30f, 0.32f, 0.35f)));

        var backBtn = MkB("返回主菜单", 140, 36, 13);
        backBtn.Pressed += () => RenderMainMenu();
        _content.AddChild(backBtn);
    }

    // ══════════════════ 团队详情弹窗 ══════════════════
    private void ShowTeamDetail(Team team)
    {
        var vp = _gm.GetViewport().GetVisibleRect().Size;
        float S(float v) => v * _gm.UIScale;
        float pw = S(460), ph = S(400);

        var dlg = new Panel { Position = new((vp.X - pw) / 2, (vp.Y - ph) / 2), Size = new(pw, ph), MouseFilter = Control.MouseFilterEnum.Stop };
        dlg.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.12f, 0.16f, 0.97f),
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.3f, 0.5f, 0.8f, 0.5f)
        });

        float ly = S(14);

        // 标题
        var title = new Label { Text = $"👥 {team.Name}", Position = new(S(14), ly), Size = new(pw - S(28), S(24)) };
        title.AddThemeFontSizeOverride("font_size", 18);
        title.AddThemeColorOverride("font_color", new Color(0.85f, 0.82f, 0.65f));
        dlg.AddChild(title);

        var closeBtn = new Button { Text = "✕", Position = new(pw - S(42), S(14)), Size = new(S(28), S(28)), Flat = true };
        closeBtn.Pressed += () => dlg.QueueFree();
        dlg.AddChild(closeBtn);

        ly += S(34);

        // 团队任务信息
        string taskInfo = team.Task == TeamTask.None ? Loc.Tr("dev.team_idle")
            : team.Task == TeamTask.DevelopGame ? Loc.TrF("dev.team_dev_game", team.CurrentProject?.Name ?? "???")
            : team.Task == TeamTask.ResearchTech ? Loc.Tr("dev.team_research")
            : team.Task == TeamTask.Refactor ? Loc.Tr("dev.team_refactor")
            : team.Task == TeamTask.DevelopEngine ? Loc.Tr("dev.team_dev_engine")
            : team.Task == TeamTask.Outsource ? Loc.Tr("dev.team_outsource")
            : Loc.Tr("dev.team_busy");
        var taskLabel = new Label { Text = Loc.TrF("dev.team_status_label", taskInfo, team.GetChemistryBonus()*100f), Position = new(S(14), ly), Size = new(pw - S(28), S(18)) };
        taskLabel.AddThemeFontSizeOverride("font_size", 12);
        taskLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.65f, 0.70f));
        dlg.AddChild(taskLabel);

        ly += S(24);

        // 成员详情列表（表头）
        var mHeader = new HBoxContainer { Position = new(S(14), ly), Size = new(pw - S(28), S(18)) };
        mHeader.AddChild(MkL("成员", 11, new Color(0.4f, 0.45f, 0.5f)));
        dlg.AddChild(mHeader);
        ly += S(20);

        // 每个成员
        foreach (var emp in team.Members)
        {
            var mRow = new HBoxContainer { Position = new(S(14), ly), Size = new(pw - S(28), S(22)) };
            string icon = GetTraitIcon(emp.Trait);
            int avgLv = emp.Skills.Count > 0 ? (int)emp.Skills.Values.Average(s => s.Level) : 0;
            Color fatigueColor = emp.Fatigue > 70 ? new Color(0.9f, 0.3f, 0.2f) : emp.Fatigue > 40 ? new Color(0.9f, 0.6f, 0.2f) : new Color(0.3f, 0.7f, 0.3f);
            Color satColor = emp.Satisfaction > 70 ? new Color(0.2f, 0.7f, 0.3f) : emp.Satisfaction > 40 ? new Color(0.9f, 0.6f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);

            mRow.AddChild(MkL($"{icon}{emp.Name} Lv{avgLv}  疲劳{emp.Fatigue:F0}%  满意{emp.Satisfaction:F0}%  ¥{emp.Salary:N0}/月", 11, new Color(0.7f, 0.72f, 0.75f)));
            dlg.AddChild(mRow);
            ly += S(22);
        }

        ly += S(8);

        // 团队操作
        var actRow = new HBoxContainer { Position = new(S(14), ly), Size = new(pw - S(28), S(28)) };
        var bbqBtn = new Button { Text = Loc.Tr("dev.team_bbq"), Flat = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        bbqBtn.AddThemeFontSizeOverride("font_size", 12);
        bbqBtn.AddThemeColorOverride("font_color", new Color(0.5f, 0.7f, 0.3f));
        bbqBtn.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.20f, 0.25f, 0.30f, 0.6f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        var tRef = team;
        bbqBtn.Pressed += () =>
        {
            if (_res.TeamBuilding(tRef))
            {
                _gm.ShowToast("豪华团建", $"{tRef.Name}全员疲劳-20, 满意度+10", new Color(0.5f, 0.7f, 0.3f));
                dlg.QueueFree();
                RenderTeamAssign();
            }
            else _gm.ShowToast(Loc.Tr("toast.funds_low"), Loc.TrF("toast.funds_need", 20000), new Color(0.9f, 0.3f, 0.2f));
        };
        actRow.AddChild(bbqBtn);
        dlg.AddChild(actRow);

        _gm.UiLayer.AddChild(dlg);
    }

    // ══════════════════ 分数显示 ══════════════════
    private void ShowScoreDisplay()
    {
        UpdateScorePanel();
        _gm.CloseAll();
        // 存持久委托给 GameManager 每帧调用
        _gm.SetDevScoreUpdater(() => UpdateScorePanel());
    }

    /// <summary>更新左上角分数面板（每帧由 GameManager 调）</summary>
    public void UpdateScorePanel()
    {
        var developing = _devMgr.Projects.FindAll(p => !p.IsReleased && p.Phase != DevPhase.Planning);
        if (developing.Count == 0)
        {
            if (_scorePanel != null) { _scorePanel.QueueFree(); _scorePanel = null; }
            return;
        }

        if (_scorePanel == null)
        {
            _scorePanel = new VBoxContainer { Position = new(Sf(8), Sf(50)) };
            _gm.UiLayer.AddChild(_scorePanel);
        }

        // 增量更新：不清理旧节点，直接更新文本
        int needed = developing.Count * 3;
        var children = _scorePanel.GetChildren();
        int existing = children.Count;

        for (int i = existing; i < needed; i++)
        {
            var lbl = new Label();
            lbl.AddThemeFontSizeOverride("font_size", 10);
            lbl.AddThemeColorOverride("font_color", new Color(0.6f, 0.7f, 0.8f));
            _scorePanel.AddChild(lbl);
        }
        for (int i = 0; i < existing; i++)
            ((Label)children[i]).Visible = i < needed;

        int idx = 0;
        foreach (var proj in developing)
        {
            var team = _teamMgr.Teams.Find(t => t.CurrentProject == proj);
            var scores = CalcLiveScores(proj, team);
            proj.GameplayScore = scores.Gameplay;
            proj.GraphicsScore = scores.Graphics;
            proj.AudioScore = scores.Audio;
            proj.StabilityScore = scores.Stability;
            proj.BugCount = scores.Bug;

            // 只改文本不动样式
            ((Label)children[idx]).Text = Loc.TrF("devmenu.proj_label", proj.Name);
            idx++;

            ((Label)children[idx]).Text = Loc.TrF("devmenu.score_detail", scores.Gameplay, scores.Graphics, scores.Audio, scores.Originality, scores.Stability, scores.Bug);
            idx++;

            ((Label)children[idx]).Text = proj.Phase == DevPhase.Developing
                ? Loc.TrF("devmenu.progress_fmt", proj.DevProgress * 100f, proj.ScoreTierIcon, proj.ScoreTier, proj.BaseProgramScore, proj.BaseArtScore)
                : Loc.TrF("devmenu.polish_fmt", proj.PolishMonths, proj.ScoreTierIcon, proj.ScoreTier, proj.BaseQualityScore);
            idx++;
        }
    }

    /// <summary>预估项目分数（立项时）</summary>
    private (float Gameplay, float Graphics, float Audio, float Originality, float Stability) EstimateScores(GameGenre genre, GameTheme theme, Platform plat)
    {
        float empCount = _empMgr.Employees.Count;
        float progLv = _empMgr.Employees.Sum(e => e.GetSkillLevel(SkillType.Program));
        float artLv = _empMgr.Employees.Sum(e => e.GetSkillLevel(SkillType.Art));
        float audioLv = _empMgr.Employees.Sum(e => e.GetSkillLevel(SkillType.Audio));
        float totalDebt = _debtMgr.ComputeTotalDebt();

        float graphics = Mathf.Clamp(20 + artLv * 3 + empCount * 2, 10, 95);
        float audio = Mathf.Clamp(15 + audioLv * 3 + empCount * 1.5f, 10, 90);
        float gameplay = Mathf.Clamp(15 + empCount * 2 + _rng.Next(10), 10, 90);
        float originality = Mathf.Clamp(10 + empCount * 1.5f + _rng.Next(15), 5, 90);
        float stability = Mathf.Clamp(30 + progLv * 4 - totalDebt * 0.4f + empCount * 1.5f, 10, 95);

        // ── 已选组件效果 ──
        var equippedObjs = GameComponentDB.All.Where(c => _selComponents.Contains(c.Id)).ToArray();
        foreach (var c in equippedObjs)
        {
            foreach (var (attr, bonus) in c.Effects)
            {
                switch (attr)
                {
                    case "graphics": graphics += bonus; break;
                    case "gameplay": gameplay += bonus; break;
                    case "audio": audio += bonus; break;
                    case "stability": stability += bonus; break;
                }
            }
        }
        // ── 联动加成 ──
        if (_selComponents.Count >= 2)
        {
            var synergies = GameComponentDB.ComputeSynergies(equippedObjs);
            gameplay += synergies.Values.Sum() * 0.5f;
        }

        return (gameplay, graphics, audio, originality, stability);
    }

    /// <summary>实时计算开发中分数</summary>
    private (float Gameplay, float Graphics, float Audio, float Originality, float Stability, int Bug) CalcLiveScores(GameProject proj, Team team)
    {
        if (team == null) return (proj.GameplayScore, proj.GraphicsScore, proj.AudioScore, proj.GameplayScore, 50, 0);

        float progLv = team.Members.Sum(e => e.GetSkillLevel(SkillType.Program));
        float artLv = team.Members.Sum(e => e.GetSkillLevel(SkillType.Art));
        float audioLv = team.Members.Sum(e => e.GetSkillLevel(SkillType.Audio));
        float n = team.Members.Count;

        // 基础分 + 每月累加
        float gBase = proj.GraphicsScore + artLv * 0.5f + n * 0.3f;
        float aBase = proj.AudioScore + audioLv * 0.5f + n * 0.2f;
        // 趣味性：随机波动 + 员工概率事件
        float gpBase = proj.GameplayScore + n * 0.3f;
        if (_rng.NextDouble() < 0.3f) gpBase += _rng.Next(3);
        // 独创性：纯随机
        float orig = proj.AIScore + n * 0.2f;
        if (_rng.NextDouble() < 0.2f) orig += _rng.Next(4);
        // 稳定性：程序 + 债务
        float totalDebt = _debtMgr.ComputeTotalDebt();
        float stab = 30 + progLv * 4 - totalDebt * 0.4f + n * 1.5f;
        // BUG：稳定性越低越多
        float bugRate = Mathf.Max(0.2f, (100 - stab) / 100f * 3);
        int bug = proj.BugCount + Mathf.Max(0, (int)(bugRate + _rng.NextDouble() * 2));

        return (
            Mathf.Clamp(gpBase, 5, 99),
            Mathf.Clamp(gBase, 5, 99),
            Mathf.Clamp(aBase, 5, 99),
            Mathf.Clamp(orig, 5, 99),
            Mathf.Clamp(stab, 10, 99),
            Mathf.Min(bug, 500)
        );
    }

    // ══════════════════ 辅助 ══════════════════

    private List<GameGenre> GetUnlockedGenres()
    {
        var list = new List<GameGenre>(GameInitialUnlocks.StartGenres);
        var map = new Dictionary<string, GameGenre>
        {
            ["rac"] = GameGenre.RAC, ["sim"] = GameGenre.SIM, ["spo"] = GameGenre.SPO,
            ["mus"] = GameGenre.MUS, ["ftg"] = GameGenre.FTG, ["moba"] = GameGenre.MOBA,
            ["mmo"] = GameGenre.MMO, ["rts"] = GameGenre.RTS, ["hor"] = GameGenre.HOR,
            ["san"] = GameGenre.SAN, ["rog"] = GameGenre.ROG, ["vis"] = GameGenre.VIS,
            ["pzl"] = GameGenre.PZL,
        };
        foreach (var kv in _techMgr.ResearchedTech)
            if (kv.Value && kv.Key.StartsWith("genre_"))
            { string s = kv.Key.Replace("genre_", ""); if (map.TryGetValue(s, out var g) && !list.Contains(g)) list.Add(g); }

        // 新手教程过滤
        var tutMgr = _gm.GetNode<TutorialManager>("TutorialManager");
        if (tutMgr != null)
            list = list.Where(g => tutMgr.GetAvailableGenres().Contains(g)).ToList();

        return list;
    }

    private List<GameTheme> GetUnlockedThemes()
    {
        var list = new List<GameTheme>(GameInitialUnlocks.StartThemes);
        var map = new Dictionary<string, GameTheme>
        {
            ["cyber"] = GameTheme.Cyberpunk, ["steam"] = GameTheme.Steampunk, ["horror"] = GameTheme.Horror,
            ["comedy"] = GameTheme.Comedy, ["romance"] = GameTheme.Romance, ["war"] = GameTheme.War,
            ["mystery"] = GameTheme.Mystery, ["school"] = GameTheme.School, ["myth"] = GameTheme.Myth,
            ["western"] = GameTheme.Western, ["space"] = GameTheme.Space, ["fantasy"] = GameTheme.Fantasy,
            ["scifi"] = GameTheme.SciFi, ["modern"] = GameTheme.Modern, ["historical"] = GameTheme.Historical,
            ["postapoc"] = GameTheme.PostApoc,
        };
        foreach (var kv in _techMgr.ResearchedTech)
            if (kv.Value && kv.Key.StartsWith("theme_"))
            { string s = kv.Key.Replace("theme_", ""); if (map.TryGetValue(s, out var t) && !list.Contains(t)) list.Add(t); }

        // 新手教程过滤
        var tutMgr2 = _gm.GetNode<TutorialManager>("TutorialManager");
        if (tutMgr2 != null)
            list = list.Where(t => tutMgr2.GetAvailableThemes().Contains(t)).ToList();

        return list;
    }

    private static float GetCompat(GameGenre g, GameTheme t) => GameDevManager.GetCompat(g, t); // 复用单一真理表

    private static string PerkIcon(EnginePerk p) => p switch
    {
        EnginePerk.BlazingFast => "⚡",
        EnginePerk.Lightweight => "📦",
        EnginePerk.GraphicalPowerhouse => "🎨",
        EnginePerk.StylizedMaster => "✨",
        EnginePerk.RockSolid => "🪨",
        EnginePerk.BattleTested => "🛡️",
        EnginePerk.NoobFriendly => "🧸",
        EnginePerk.ModSupport => "🔧",
        EnginePerk.NetworkPro => "🌐",
        EnginePerk.CloudNative => "☁️",
        EnginePerk.LegacyCodebase => "🏚️",
        EnginePerk.Experimental => "🧪",
        EnginePerk.IndieChampion => "🎯",
        _ => "?"
    };

    private static string GetEngineCompatShort(GameEngine eng, GameGenre genre)
    {
        if (eng.Capabilities.Count == 0) return "";
        int v2d = eng.Capabilities.TryGetValue("2d", out var c) ? c : 0;
        int v3d = eng.Capabilities.TryGetValue("3d", out var c2) ? c2 : 0;
        int net = eng.Capabilities.TryGetValue("net", out var c3) ? c3 : 0;

        bool is2D = genre is GameGenre.RPG or GameGenre.ACT or GameGenre.AVG or GameGenre.PZL or GameGenre.MUS or GameGenre.VIS;
        bool is3D = genre is GameGenre.FPS or GameGenre.RAC or GameGenre.SAN or GameGenre.SUR;
        bool isOnline = genre is GameGenre.MOBA or GameGenre.MMO;

        if (isOnline && net == 0) return " [!]";
        if (is3D && v3d == 0) return " [?]";
        if (is2D && v2d == 0) return " [?]";
        return " ✓";
    }

    private string GetEngineCompatLabel(GameEngine eng, GameGenre genre)
    {
        if (eng.Capabilities.Count == 0) return "";
        int v2d = eng.Capabilities.TryGetValue("2d", out var c) ? c : 0;
        int v3d = eng.Capabilities.TryGetValue("3d", out var c2) ? c2 : 0;
        int net = eng.Capabilities.TryGetValue("net", out var c3) ? c3 : 0;

        bool is2D = genre is GameGenre.RPG or GameGenre.ACT or GameGenre.AVG or GameGenre.PZL or GameGenre.MUS or GameGenre.VIS;
        bool is3D = genre is GameGenre.FPS or GameGenre.RAC or GameGenre.SAN or GameGenre.SUR;
        bool isOnline = genre is GameGenre.MOBA or GameGenre.MMO;

        if (isOnline && net >= 1) return Loc.Tr("devmenu.compat_ok_net");
        if (isOnline && net == 0) return Loc.Tr("devmenu.compat_miss_net");
        if (is3D && v3d >= 1) return Loc.Tr("devmenu.compat_ok_3d");
        if (is3D && v3d == 0) return Loc.Tr("devmenu.compat_warn_3d");
        if (is2D && v2d >= 1) return Loc.Tr("devmenu.compat_ok_2d");
        if (is2D && v2d == 0) return Loc.Tr("devmenu.compat_miss_2d");
        return Loc.Tr("devmenu.avail");
    }
    private static string GetStarRating(float compat) => compat >= 0.9f ? "★★★★★" : compat >= 0.7f ? "★★★★☆" : compat >= 0.5f ? "★★★☆☆" : compat >= 0.3f ? "★★☆☆☆" : "★☆☆☆☆";

    // ══════════════════ 后续内容选择（两步：选游戏→选类型）══════════════════
    private GameProject _selectedPostReleaseProject;

    public void RenderPostRelease()
    {
        if (_panel == null || !GodotObject.IsInstanceValid(_panel)) Show();
        ShowPage("post_release");
        _content.AddChild(MkL("📦 后续内容中心 — 为已发售游戏注入新生命", 16, new Color(0.10f, 0.14f, 0.22f)));

        var released = _devMgr.Projects.Where(p => p.IsReleased).OrderByDescending(p => p.Sales).ToList();
        if (released.Count == 0)
        {
            _content.AddChild(MkL("暂无已发售的游戏", 13, new Color(0.6f, 0.3f, 0.1f)));
            var b = MkB("返回", 100, 30, 12); b.Pressed += () => RenderMainMenu(); _content.AddChild(b);
            return;
        }

        // ── 第1步：选择目标游戏 ──
        _content.AddChild(MkL("", 4, Colors.White));
        _content.AddChild(MkL("▼ 选择已发售的游戏（点击展开操作）", 12, new Color(0.25f, 0.30f, 0.40f)));

        var rng = new Random();
        foreach (var proj in released)
        {
            // 游戏选择卡片
            var selCard = new Panel { CustomMinimumSize = new(0, Sf(48)), Size = new(Sf(520), Sf(48)) };
            var selStyle = new StyleBoxFlat
            {
                BgColor = _selectedPostReleaseProject == proj ? new Color(0.85f, 0.95f, 1f) : new Color(0.93f, 0.95f, 0.93f),
                CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            };
            selCard.AddThemeStyleboxOverride("panel", selStyle);

            float sx = Sf(10), sy = Sf(4);
            string selInfo = $"《{proj.Name}》 评分:{proj.FinalScore:F0} 销量:{proj.Sales/10000f:F1}万 品牌力:{proj.BrandPower:F2}";
            if (proj.HasModKit) selInfo += " 🔧Mod";
            selInfo += $"  已操作:{proj.PostReleaseCount}次";
            selCard.AddChild(MkLPos(selInfo, sx, sy, Sf(380), Sf(18), 12, new Color(0.12f, 0.16f, 0.24f)));

            // 销量生命进度条
            float maxLife = proj.IsLongTail ? 48 : 36;
            float lifeLeft = Mathf.Clamp(1f - proj.MonthsOnMarket / maxLife, 0, 1);
            var lifeBar = new ColorRect { Position = new(sx, sy + Sf(24)), Size = new(Sf(240), Sf(10)), Color = new Color(0.88f, 0.88f, 0.88f) };
            selCard.AddChild(lifeBar);
            var lifeFill = new ColorRect { Position = new(sx, sy + Sf(24)), Size = new(Sf(240) * lifeLeft, Sf(10)), Color = lifeLeft > 0.5f ? new Color(0.3f, 0.8f, 0.4f) : lifeLeft > 0.2f ? new Color(0.9f, 0.6f, 0.2f) : new Color(0.8f, 0.3f, 0.3f) };
            selCard.AddChild(lifeFill);
            selCard.AddChild(MkLPos($"销售生命:{lifeLeft*100:F0}%  {proj.MonthsOnMarket}/{maxLife}月", sx + Sf(248), sy + Sf(20), Sf(160), Sf(14), 9, new Color(0.45f, 0.50f, 0.55f)));

            // 选择按钮
            var selBtn = MkBPos(_selectedPostReleaseProject == proj ? "▼ 收起" : "▶ 展开", Sf(410), sy + Sf(6), Sf(100), Sf(30), 12);
            var pp = proj;
            selBtn.Pressed += () =>
            {
                _selectedPostReleaseProject = _selectedPostReleaseProject == pp ? null : pp;
                RenderPostRelease();
            };
            selCard.AddChild(selBtn);

            _content.AddChild(selCard);

            // ── 第2步：展开选中的游戏 → 显示8种后发内容卡片 ──
            if (_selectedPostReleaseProject == proj)
            {
                _content.AddChild(MkL("", 4, Colors.White));

                // 属性进度条：稳定性、Bug、粉丝满意度
                var barCanvas = new Control { CustomMinimumSize = new(Sf(520), Sf(36)), Size = new(Sf(520), Sf(36)) };
                float bx = Sf(10), by = Sf(2), bw = Sf(140), bh = Sf(10);
                // 稳定性
                barCanvas.AddChild(MkLPos($"稳定性", bx, by - Sf(1), Sf(50), Sf(10), 8, new Color(0.4f, 0.4f, 0.4f)));
                barCanvas.AddChild(new ColorRect { Position = new(bx + Sf(44), by), Size = new(bw, bh), Color = new Color(0.88f, 0.88f, 0.88f) });
                barCanvas.AddChild(new ColorRect { Position = new(bx + Sf(44), by), Size = new(bw * proj.StabilityScore / 100f, bh), Color = new Color(0.3f, 0.6f, 0.9f) });
                // Bug
                float bx2 = bx + Sf(195);
                barCanvas.AddChild(MkLPos($"Bug", bx2, by - Sf(1), Sf(40), Sf(10), 8, new Color(0.4f, 0.4f, 0.4f)));
                barCanvas.AddChild(new ColorRect { Position = new(bx2 + Sf(30), by), Size = new(bw, bh), Color = new Color(0.88f, 0.88f, 0.88f) });
                float bugRatio = Mathf.Clamp(proj.BugCount / 50f, 0, 1);
                barCanvas.AddChild(new ColorRect { Position = new(bx2 + Sf(30), by), Size = new(bw * bugRatio, bh), Color = bugRatio > 0.5f ? new Color(0.9f, 0.3f, 0.3f) : new Color(0.3f, 0.8f, 0.4f) });
                barCanvas.AddChild(MkLPos($"{proj.BugCount}", bx2 + Sf(173), by - Sf(2), Sf(30), Sf(12), 9, new Color(0.5f, 0.3f, 0.2f)));
                // 粉丝满意度
                float bx3 = bx2 + Sf(180);
                barCanvas.AddChild(MkLPos($"满意度", bx3, by - Sf(1), Sf(45), Sf(10), 8, new Color(0.4f, 0.4f, 0.4f)));
                barCanvas.AddChild(new ColorRect { Position = new(bx3 + Sf(40), by), Size = new(bw, bh), Color = new Color(0.88f, 0.88f, 0.88f) });
                barCanvas.AddChild(new ColorRect { Position = new(bx3 + Sf(40), by), Size = new(bw * proj.FanSatisfaction / 2f, bh), Color = new Color(0.9f, 0.5f, 0.6f) });
                barCanvas.AddChild(MkLPos($"{proj.FanSatisfaction:F1}", bx3 + Sf(183), by - Sf(2), Sf(30), Sf(12), 9, new Color(0.5f, 0.3f, 0.4f)));

                _content.AddChild(barCanvas);
                _content.AddChild(MkL("", 4, Colors.White));

                // 8种后发内容卡片（2列网格）
                var preTypes = new[]
                {
                    (type: PostReleaseType.BugFixPatch, icon: "🔧", label: "Bug修复", desc: "修复稳定性问题，恢复口碑"),
                    (type: PostReleaseType.ContentUpdate, icon: "🎁", label: "内容更新", desc: "小幅内容+延长销售寿命"),
                    (type: PostReleaseType.SkinDLC, icon: "👗", label: "皮肤DLC", desc: "按粉丝量赚快钱"),
                    (type: PostReleaseType.Expansion, icon: "📚", label: "资料片", desc: "大型内容扩展(需65分)"),
                    (type: PostReleaseType.Remaster, icon: "✨", label: "重制版", desc: "全面升级画面/音效(需70分)"),
                    (type: PostReleaseType.Port, icon: "🎮", label: "平台移植", desc: "登陆新平台再卖一波"),
                    (type: PostReleaseType.Sequel, icon: "🔗", label: "续作", desc: "IP传承·品牌加成(需70分+5万销)"),
                    (type: PostReleaseType.ModKit, icon: "🔓", label: "Mod工具包", desc: "社区创作·长尾衰减减半"),
                };

                float cardW = Sf(250), cardH = Sf(100), colGap = Sf(8);
                float gridW = cardW * 2 + colGap;
                float gridH = (cardH + Sf(6)) * 4;
                var gridCanvas = new Control { CustomMinimumSize = new(gridW, gridH), Size = new(gridW, gridH) };

                float rowY = 0; int cardsPlaced = 0;
                foreach (var (type, icon, label, desc) in preTypes)
                {
                    bool canCreate = GameDevManager.CanCreatePostRelease(type, proj);
                    string unlockNote = GameDevManager.GetPostReleaseUnlockReq(type, proj);
                    long cost = GameDevManager.GetPostReleaseCost(type, proj);
                    float months = GameDevManager.GetPostReleaseMonths(type, proj);
                    string costStr = type == PostReleaseType.ModKit ? "¥50,000" : $"¥{cost / 10000f:F1}万";
                    string effectPreview = type switch
                    {
                        PostReleaseType.BugFixPatch => $"稳定性+15, Bug-60%",
                        PostReleaseType.ContentUpdate => $"三维+5, 销售寿命+3月",
                        PostReleaseType.SkinDLC => $"按粉丝×满意度赚收入",
                        PostReleaseType.Expansion => $"玩法+10/剧情+8",
                        PostReleaseType.Remaster => $"画面+18/音频+15",
                        PostReleaseType.Port => $"降价30%上另一平台",
                        PostReleaseType.Sequel => $"品牌力{proj.BrandPower*15:F0}分加成",
                        PostReleaseType.ModKit => $"长尾衰减减半, 销售期+12月",
                        _ => ""
                    };

                    float colX = (cardsPlaced % 2) * (cardW + colGap);
                    if (cardsPlaced % 2 == 0 && cardsPlaced > 0) rowY += cardH + Sf(6);

                    var card = new Panel { Position = new(colX, rowY), Size = new(cardW, cardH) };
                    var cStyle = new StyleBoxFlat
                    {
                        BgColor = canCreate ? new Color(0.95f, 1f, 0.95f) : new Color(0.92f, 0.92f, 0.92f),
                        CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6, CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
                    };
                    card.AddThemeStyleboxOverride("panel", cStyle);

                    float ly = Sf(4), lx = Sf(6);
                    card.AddChild(MkLPos($"{icon} {label}", lx, ly, cardW - Sf(12), Sf(16), 11,
                        canCreate ? new Color(0.10f, 0.16f, 0.10f) : new Color(0.50f, 0.50f, 0.50f)));
                    ly += Sf(17);
                    card.AddChild(MkLPos($"  {desc}", lx, ly, cardW - Sf(12), Sf(14), 9, new Color(0.35f, 0.38f, 0.42f)));
                    ly += Sf(14);
                    card.AddChild(MkLPos($"  💰{costStr} ⏱{months:F0}月", lx, ly, cardW - Sf(12), Sf(14), 9, new Color(0.45f, 0.3f, 0.15f)));
                    ly += Sf(14);
                    card.AddChild(MkLPos($"  → {effectPreview}", lx, ly, cardW - Sf(12), Sf(14), 9, new Color(0.15f, 0.45f, 0.15f)));
                    ly += Sf(14);
                    card.AddChild(MkLPos($"  {unlockNote}", lx, ly, cardW - Sf(12), Sf(12), 8,
                        canCreate ? new Color(0.3f, 0.5f, 0.3f) : new Color(0.8f, 0.3f, 0.2f)));

                    if (canCreate)
                    {
                        var btn = MkBPos("立项开发", lx + cardW - Sf(100), cardH - Sf(24), Sf(92), Sf(20), 10);
                        var cp = proj; var ct = type;
                        btn.Pressed += () =>
                        {
                            ShowPostReleaseStrategyPopup(cp, ct);
                        };
                        card.AddChild(btn);
                    }

                    gridCanvas.AddChild(card);
                    cardsPlaced++;
                }
                _content.AddChild(gridCanvas);
                _content.AddChild(MkL("", 4, Colors.White));
            }
        }

        var back = MkB("返回", 100, 30, 12);
        back.Pressed += () => { _selectedPostReleaseProject = null; RenderMainMenu(); };
        _content.AddChild(back);
    }

    private void AssignTeamOrBack(GameProject proj)
    {
        if (_empMgr.Employees.Count == 0)
        { _gm.ShowToast(Loc.Tr("toast.no_employee"), Loc.Tr("toast.no_employee_dev"), new Color(0.9f, 0.3f, 0.2f)); return; }
        var idle = _teamMgr.Teams.FindAll(t => t.Task == TeamTask.None);
        if (idle.Count > 0)
        {
            _gm.CloseAll();
            var popup = new GameDevPopup(_gm);
            popup.ShowAssignTeam(proj);
            _gm.SetDevPopup(popup);
        }
        else
        {
            _gm.ShowToast(Loc.Tr("dev.no_idle_team"), "请先等待一个团队空闲", new Color(0.9f, 0.5f, 0.2f));
            RenderPostRelease();
        }
    }

    private void ShowPostReleaseStrategyPopup(GameProject baseProj, PostReleaseType type)
    {
        var vp = _gm.GetViewport().GetVisibleRect().Size;
        float pw = vp.X * 0.3f, ph = vp.Y * 0.2f;
        var dlg = new Panel { Position = new((vp.X - pw) / 2, (vp.Y - ph) / 2), Size = new(pw, ph) };
        dlg.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.97f, 0.98f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 });

        float S(float v) => v * _gm.UIScale;
        var strategyTitle = new Label { Text = Loc.Tr("ui.choose_strategy"), Position = new(S(14), S(8)), Size = new(pw - S(28), S(22)) };
        strategyTitle.AddThemeColorOverride("font_color", new Color(0, 0, 0));
        dlg.AddChild(strategyTitle);

        var row = new HBoxContainer();
        // 激进
        var aggCard = new Panel { CustomMinimumSize = new(S(110), S(70)), Size = new(S(110), S(70)) };
        aggCard.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(1f, 0.93f, 0.93f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        var aggV = new VBoxContainer(); aggCard.AddChild(aggV);
        aggV.AddChild(MkL(Loc.Tr("ui.aggressive_title"), 11, new Color(0.8f, 0.2f, 0.2f)));
        aggV.AddChild(MkL(Loc.Tr("ui.aggressive_desc1"), 9, new Color(0.5f, 0.3f, 0.3f)));
        aggV.AddChild(MkL(Loc.Tr("ui.aggressive_desc2"), 8, new Color(0.6f, 0.4f, 0.4f)));
        var aggBtn = new Button { Text = Loc.Tr("ui.select"), Size = new(S(100), S(20)) };
        aggBtn.Pressed += () => { var proj = _devMgr.CreatePostReleaseContent(baseProj, type, GameDevManager.PostReleaseStrategy.Aggressive); dlg.QueueFree(); if (proj != null) AssignTeamOrBack(proj); else RenderPostRelease(); };
        aggV.AddChild(aggBtn);

        // 平衡
        var balCard = new Panel { CustomMinimumSize = new(S(110), S(70)), Size = new(S(110), S(70)) };
        balCard.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.93f, 1f, 0.93f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        var balV = new VBoxContainer(); balCard.AddChild(balV);
        balV.AddChild(MkL("⚖ 平衡", 11, new Color(0.2f, 0.6f, 0.2f)));
        balV.AddChild(MkL("标准时间", 9, new Color(0.3f, 0.5f, 0.3f)));
        balV.AddChild(MkL("波动±8%", 8, new Color(0.4f, 0.6f, 0.4f)));
        var balBtn = new Button { Text = "选", Size = new(S(100), S(20)) };
        balBtn.Pressed += () => { var proj = _devMgr.CreatePostReleaseContent(baseProj, type, GameDevManager.PostReleaseStrategy.Balanced); dlg.QueueFree(); if (proj != null) AssignTeamOrBack(proj); else RenderPostRelease(); };
        balV.AddChild(balBtn);

        // 保守
        var conCard = new Panel { CustomMinimumSize = new(S(110), S(70)), Size = new(S(110), S(70)) };
        conCard.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.93f, 0.93f, 1f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        var conV = new VBoxContainer(); conCard.AddChild(conV);
        conV.AddChild(MkL(Loc.Tr("ui.conservative_title"), 11, new Color(0.2f, 0.3f, 0.8f)));
        conV.AddChild(MkL(Loc.Tr("ui.conservative_desc1"), 9, new Color(0.3f, 0.3f, 0.6f)));
        conV.AddChild(MkL(Loc.Tr("ui.conservative_desc2"), 8, new Color(0.3f, 0.4f, 0.6f)));
        var conBtn = new Button { Text = Loc.Tr("ui.select"), Size = new(S(100), S(20)) };
        conBtn.Pressed += () => { var proj = _devMgr.CreatePostReleaseContent(baseProj, type, GameDevManager.PostReleaseStrategy.Conservative); dlg.QueueFree(); if (proj != null) AssignTeamOrBack(proj); else RenderPostRelease(); };
        conV.AddChild(conBtn);

        row.AddChild(aggCard); row.AddChild(balCard); row.AddChild(conCard);
        dlg.AddChild(row);
        row.Position = new(S(14), S(34));

        var cancelBtn = new Button { Text = "取消", Position = new(pw - S(58), ph - S(30)), Size = new(S(50), S(24)), Flat = true };
        cancelBtn.Pressed += () => dlg.QueueFree();
        dlg.AddChild(cancelBtn);
        _gm.UiLayer.AddChild(dlg);
    }

    // ══════════════════ 发行中心（主动经营+谈判）══════════════════
    public void RenderPublishingCenter()
    {
        if (_panel == null || !GodotObject.IsInstanceValid(_panel)) Show();
        ShowPage("publishing");
        string repStars = _devMgr.PublisherReputation > 0.7f ? "金牌发行商" : _devMgr.PublisherReputation > 0.4f ? "知名发行商" : "新手发行商";
        _content.AddChild(MkL($"发行中心 — {repStars}（声誉:{_devMgr.PublisherReputation:P0}）", 16, new Color(0.10f, 0.14f, 0.22f)));

        if (_devMgr.CompletedProjects.Count < 2)
        {
            _content.AddChild(MkL("需要先发布2款自家游戏才能解锁发行功能", 13, new Color(0.6f, 0.3f, 0.1f)));
            var back2 = MkB("返回", 100, 30, 12); back2.Pressed += () => RenderMainMenu(); _content.AddChild(back2); return;
        }

        // ── 主动洽谈：AI工作室列表 ──
        _content.AddChild(MkL("── 主动发行洽谈 ──", 14, new Color(0.25f, 0.28f, 0.32f)));
        var compAI = _gm.GetNodeOrNull<CompetitorAI>("CompetitorAI");
        if (compAI != null)
        {
            var avails = compAI.Studios.Where(s => !s.IsAcquired && s.Releases.Count >= 1).Take(4).ToList();
            if (avails.Count == 0)
            {
                _content.AddChild(MkL("暂无愿意合作的工作室", 11, new Color(0.5f, 0.5f, 0.5f)));
            }
            else
            {
                var studioRow = new HBoxContainer();
                foreach (var s in avails)
                {
                    var sCard = new Panel { CustomMinimumSize = new(Sf(130), Sf(44)), Size = new(Sf(130), Sf(44)) };
                    sCard.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.93f, 0.96f, 1f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
                    var sv = new VBoxContainer(); sCard.AddChild(sv);
                    sv.AddChild(MkL($"{s.Name}", 10, new Color(0.2f, 0.3f, 0.5f)));
                    sv.AddChild(MkL($"声誉{s.Reputation} 资金{s.Money/10000f:F0}万", 8, new Color(0.4f, 0.4f, 0.4f)));
                    var proposeBtn = MkB("邀约 (￥" + (5000 + (int)(_devMgr.PublisherReputation * 20000) / 10000f).ToString("F0") + "万)", 120, 20, 9);
                    var ss = s;
                    proposeBtn.Pressed += () => { _devMgr.ProposeDealToStudio(ss); RenderPublishingCenter(); };
                    sv.AddChild(proposeBtn);
                    studioRow.AddChild(sCard);
                }
                _content.AddChild(studioRow);
            }
        }

        _content.AddChild(MkL("", 6, Colors.White));

        // ── 待处理的发行邀约 + 谈判 ──
        _content.AddChild(MkL("── 待处理邀约（点击谈判调整条款）──", 14, new Color(0.25f, 0.28f, 0.32f)));
        if (_devMgr.AvailableDeals.Count == 0)
        {
            _content.AddChild(MkL("暂无可用的发行邀约，等待AI工作室主动联系…", 11, new Color(0.45f, 0.5f, 0.55f)));
        }
        else
        {
            foreach (var deal in _devMgr.AvailableDeals)
            {
                string tag = deal.IsPlayerOffer ? "[主动]" : "[被动]";
                string dealInfo = $"{tag} {deal.GameName} | {deal.StudioName} | {deal.Genre.Name()}×{deal.Theme.Name()}";
                float effectiveRoyalty = deal.PlayerOfferRoyalty > 0 ? deal.PlayerOfferRoyalty : deal.RoyaltyRate;
                float effectiveMarketing = deal.PlayerOfferMarketing > 0 ? deal.PlayerOfferMarketing : deal.MarketingCost;
                string dealDetail = $"预计评分:{deal.ExpectedScore:F0} 宣发费:¥{effectiveMarketing/10000f:F1}万 版税:{effectiveRoyalty:P0}";
                _content.AddChild(MkL(dealInfo, 12, new Color(0.10f, 0.14f, 0.22f)));
                _content.AddChild(MkL($"  {dealDetail}  预计{deal.EstReleaseMonths}月后发售", 10, new Color(0.35f, 0.38f, 0.42f)));

                if (deal.IsNegotiating)
                {
                    _content.AddChild(MkL($"  满意度:{deal.StudioSatisfaction:P0} " + (deal.StudioSatisfaction >= 0.3f ? "✓可签约" : "✗不满"), 10,
                        deal.StudioSatisfaction >= 0.3f ? new Color(0.2f, 0.6f, 0.3f) : new Color(0.8f, 0.3f, 0.2f)));
                }

                var btnRow = new HBoxContainer();
                var d = deal;

                // 谈判：调整分成滑块和宣发
                var negBtn = MkB("谈判", 70, 26, 10);
                negBtn.Pressed += () => ShowNegotiationPopup(d);
                btnRow.AddChild(negBtn);

                var acceptBtn = MkB("签约", 70, 26, 10);
                acceptBtn.Pressed += () =>
                {
                    if (!d.IsNegotiating || d.StudioSatisfaction >= 0.3f)
                    {
                        if (_devMgr.AcceptPublishingDeal(d)) RenderPublishingCenter();
                    }
                    else _gm.ShowToast("无法签约", "对方对条件不满意（满意度<30%）", new Color(0.9f, 0.3f, 0.2f));
                };
                btnRow.AddChild(acceptBtn);

                if (deal.IsPlayerOffer)
                {
                    var cancelBtn = MkB("放弃", 70, 26, 10);
                    cancelBtn.Pressed += () => { _devMgr.AvailableDeals.Remove(d); RenderPublishingCenter(); };
                    btnRow.AddChild(cancelBtn);
                }
                _content.AddChild(btnRow);
            }
        }

        // ── 进行中的发行项目 ──
        var activePublished = _devMgr.PublishedProjects.Where(p => p.IsPublished).ToList();
        if (activePublished.Count > 0)
        {
            _content.AddChild(MkL("", 6, Colors.White));
            _content.AddChild(MkL("── 进行中的发行项目 ──", 14, new Color(0.25f, 0.28f, 0.32f)));
            foreach (var pp in activePublished)
            {
                string status = pp.IsReleased
                    ? $"已发售 评分:{pp.ActualScore:F0} 总版税:¥{pp.TotalRoyaltyEarned/10000f:F1}万"
                    : $"开发中... 预计{pp.ReleaseMonth - _gm.GameMonth}个月后发售";
                _content.AddChild(MkL($"《{pp.GameName}》({pp.StudioName}) {status}", 11,
                    pp.IsReleased ? new Color(0.15f, 0.45f, 0.15f) : new Color(0.6f, 0.4f, 0.1f)));
            }
        }

        // ── 发行品牌 ──
        var label = _devMgr.Labels.Find(l => l.Name == "发行品牌");
        if (label != null)
        {
            _content.AddChild(MkL("", 4, Colors.White));
            _content.AddChild(MkL($"发行品牌: 已发行{label.GameCount}款 | 均分{label.AvgScore:F0} | 声誉{label.Reputation:P0}", 12, new Color(0.2f, 0.4f, 0.7f)));
        }

        var backBtn = MkB("返回", 100, 30, 12);
        backBtn.Pressed += () => RenderMainMenu();
        _content.AddChild(backBtn);
    }

    private void ShowNegotiationPopup(GameDevManager.PublishedDeal deal)
    {
        var vp = _gm.GetViewport().GetVisibleRect().Size;
        float pw = vp.X * 0.35f, ph = vp.Y * 0.28f;
        var dlg = new Panel { Position = new((vp.X - pw) / 2, (vp.Y - ph) / 2), Size = new(pw, ph) };
        dlg.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.97f, 0.98f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.3f, 0.5f, 0.8f, 0.4f) });

        float S(float v) => v * _gm.UIScale;
        float ly = S(10);
        dlg.AddChild(new Label { Text = $"谈判：《{deal.GameName}》 × {deal.StudioName}", Position = new(S(14), ly), Size = new(pw - S(28), S(22)) });
        ly += S(26);

        // 分成滑块
        float initRoyalty = deal.PlayerOfferRoyalty > 0 ? deal.PlayerOfferRoyalty : deal.RoyaltyRate;
        dlg.AddChild(new Label { Text = $"版税分成: {initRoyalty,0:P0}", Position = new(S(14), ly), Size = new(S(160), S(18)) });
        var royaltySlider = new HSlider { Position = new(S(14), ly + S(18)), Size = new(pw - S(28), S(20)), MinValue = 0.10, MaxValue = 0.50, Step = 0.01, Value = initRoyalty };
        dlg.AddChild(royaltySlider);

        var royaltyLabel = dlg.GetChildren().OfType<Label>().Last();
        royaltySlider.ValueChanged += (double v) => { royaltyLabel.Text = $"版税分成: {v,0:P0}"; };
        ly += S(44);

        // 宣发费滑块
        float defMarketing = deal.MarketingCost;
        dlg.AddChild(new Label { Text = $"宣发预算: ¥{(deal.PlayerOfferMarketing > 0 ? deal.PlayerOfferMarketing : deal.MarketingCost) / 10000f:F1}万", Position = new(S(14), ly), Size = new(S(200), S(18)) });
        var marketingSlider = new HSlider { Position = new(S(14), ly + S(18)), Size = new(pw - S(28), S(20)), MinValue = defMarketing * 0.5f, MaxValue = defMarketing * 2f, Step = 5000, Value = deal.PlayerOfferMarketing > 0 ? deal.PlayerOfferMarketing : defMarketing };
        dlg.AddChild(marketingSlider);

        var marketingLabel = dlg.GetChildren().OfType<Label>().Last();
        marketingSlider.ValueChanged += (double v) => { marketingLabel.Text = $"宣发预算: ¥{v / 10000f:F1}万"; };
        ly += S(44);

        var satisfactionLabel = new Label { Position = new(S(14), ly), Size = new(pw - S(28), S(18)) };
        satisfactionLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.5f, 0.8f));
        dlg.AddChild(satisfactionLabel);

        void UpdateSatisfaction()
        {
            float sat = _devMgr.NegotiateDeal(deal, (float)royaltySlider.Value, (float)marketingSlider.Value);
            satisfactionLabel.Text = $"对方满意度: {sat:P0} " + (sat >= 0.3f ? "✓ 可签" : "✗ 不满");
            satisfactionLabel.AddThemeColorOverride("font_color", sat >= 0.3f ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.8f, 0.3f, 0.2f));
        }
        royaltySlider.ValueChanged += (_) => UpdateSatisfaction();
        marketingSlider.ValueChanged += (_) => UpdateSatisfaction();
        UpdateSatisfaction();

        ly += S(26);
        var okBtn = new Button { Text = Loc.Tr("ui.confirm"), Position = new(pw - S(110), ly), Size = new(S(50), S(26)), Flat = true };
        okBtn.Pressed += () => { dlg.QueueFree(); RenderPublishingCenter(); };
        dlg.AddChild(okBtn);
        var cancelBtn2 = new Button { Text = Loc.Tr("ui.cancel"), Position = new(pw - S(58), ly), Size = new(S(50), S(26)), Flat = true };
        cancelBtn2.Pressed += () => dlg.QueueFree();
        dlg.AddChild(cancelBtn2);

        _gm.UiLayer.AddChild(dlg);
    }

    // ══════════════════ 市场预测面板 ══════════════════
    public void RenderMarketPrediction()
    {
        if (_panel == null || !GodotObject.IsInstanceValid(_panel)) Show();
        ShowPage("prediction");

        var trendMgr = _gm.GetNodeOrNull<MarketTrendManager>("MarketTrendManager");

        // ── 先检查是否有风口，无风口不收灵感 ──
        if (trendMgr != null && trendMgr.HypeCycles.Count == 0)
        {
            _content.AddChild(MkL("市场风向预测", 16, new Color(0.10f, 0.14f, 0.22f)));
            _content.AddChild(MkL("", 8, Colors.White));
            _content.AddChild(MkL("当前无明显风口，市场风平浪静", 13, new Color(0.3f, 0.6f, 0.3f)));
            _content.AddChild(MkL("灵感未消耗 — 等待风口出现后再预测更有价值", 11, new Color(0.45f, 0.50f, 0.55f)));

            var back0 = MkB("返回", 100, 30, 12);
            back0.Pressed += () => RenderMainMenu();
            _content.AddChild(back0);
            return;
        }

        // 消耗10灵感
        if (_res.SpendInspiration(10))
        {
            _content.AddChild(MkL("市场风向预测（未来6个月）", 16, new Color(0.10f, 0.14f, 0.22f)));

            // ── 置信度 → 绑定AI科技 ──
            bool hasAI = _techMgr.IsResearched("ai_v1");
            bool hasML = _techMgr.IsResearched("ml_ai");
            float confidence = hasML ? 0.90f : hasAI ? 0.75f : 0.60f;
            string confLabel = hasML ? "高" : hasAI ? "中" : "低";
            Color confColor = hasML ? new Color(0.2f, 0.7f, 0.3f) : hasAI ? new Color(0.8f, 0.6f, 0.2f) : new Color(0.8f, 0.3f, 0.3f);
            _content.AddChild(MkL($"置信度: {confLabel}（{confidence:P0}）—— 研发AI科技提升准确性", 10, confColor));
            _content.AddChild(MkL("", 4, Colors.White));

            if (trendMgr != null)
            {
                var predictions = trendMgr.PredictHypes(6);

                // ── 风口保险：预测的热门类型/主题未来12月发售+5%销量 ──
                var buffedSet = new HashSet<string>();
                foreach (var kv in predictions)
                {
                    // kv.Key 格式如 "RPG-奇幻", 提取类型和主题分别加入保险
                    foreach (var part in kv.Key.Split('-'))
                    {
                        string trimmed = part.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && buffedSet.Add(trimmed))
                            _gm.WindInsurances.Add(new GameManager.WindInsurance { GenreOrTheme = trimmed, MonthsLeft = 12, SalesBonus = 0.05f });
                    }
                }
                if (buffedSet.Count > 0)
                {
                    _content.AddChild(MkL($"✅ 已获得风口保险Buff（{buffedSet.Count}项，+5%销量，持续12个月）", 11, new Color(0.2f, 0.6f, 0.3f)));
                    _content.AddChild(MkL("", 4, Colors.White));
                }

                foreach (var kv in predictions)
                {
                    _content.AddChild(MkL($"📌 {kv.Key}", 13, new Color(0.15f, 0.25f, 0.45f)));

                        // 热度走势条
                        float barH = Sf(14), barW = Sf(380), barGap = Sf(3);
                        float barCanvasH = barH + barGap;
                        var barCanvas = new Control { CustomMinimumSize = new(barW, barCanvasH), Size = new(barW, barCanvasH) };

                        float segW = (barW - Sf(2)) / (kv.Value.Count > 1 ? kv.Value.Count - 1 : 1);
                        for (int i = 0; i < kv.Value.Count; i++)
                        {
                            var e = kv.Value[i];
                            float h = Mathf.Clamp(e.Popularity, 0, 1) * barH;
                            var seg = new ColorRect
                            {
                                Position = new(i * segW, barCanvasH - h),
                                Size = new(segW - Sf(2), h),
                                Color = e.Popularity > 0.7f ? new Color(0.9f, 0.3f, 0.3f)
                                    : e.Popularity > 0.5f ? new Color(0.3f, 0.8f, 0.3f)
                                    : e.Popularity > 0.3f ? new Color(0.5f, 0.6f, 0.5f)
                                    : new Color(0.4f, 0.5f, 0.8f)
                            };
                            barCanvas.AddChild(seg);
                        }
                        _content.AddChild(barCanvas);

                        // 当月数值标签
                        string trendLine = "";
                        for (int i = 0; i < kv.Value.Count; i++)
                        {
                            var e = kv.Value[i];
                            trendLine += $"  {e.DirectionIcon}{e.Popularity*100:F0}%";
                        }
                        _content.AddChild(MkL(trendLine, 9, new Color(0.4f, 0.45f, 0.5f)));
                        _content.AddChild(MkL("", 4, Colors.White));
                }
            }
        }
        else
        {
            _content.AddChild(MkL("灵感不足（需10点）", 14, new Color(0.8f, 0.3f, 0.2f)));
            _content.AddChild(MkL("可通过完成游戏开发获得灵感点数", 11, new Color(0.5f, 0.5f, 0.5f)));
        }

        var back = MkB("返回", 100, 30, 12);
        back.Pressed += () => RenderMainMenu();
        _content.AddChild(back);
    }

    // ══════════════════ 财务运营面板（营销+保险+生态）══════════════════
    public void RenderFinanceOps()
    {
        if (_panel == null || !GodotObject.IsInstanceValid(_panel)) Show();
        ShowPage("finance");
        _content.AddChild(MkL("财务运营中心", 16, new Color(0.10f, 0.14f, 0.22f)));

        // ── 月度营销投放 ──
        _content.AddChild(MkL("", 6, Colors.White));
        _content.AddChild(MkL(Loc.Tr("dev.mkt_section"), 13, new Color(0.25f, 0.28f, 0.32f)));
        var mktRow = new HBoxContainer();
        var mktLabel = new Label { Text = $"预算: ¥{_res.MonthlyMarketingBudget/10000f:F1}万/月", Size = new(Sf(140), Sf(24)) };
        mktLabel.AddThemeFontSizeOverride("font_size", 12);
        mktRow.AddChild(mktLabel);
        var mktSlider = new HSlider { MinValue = 0, MaxValue = 500000, Step = 10000, Value = _res.MonthlyMarketingBudget, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(Sf(100), Sf(16)) };
        mktSlider.ValueChanged += (double v) => { _res.MonthlyMarketingBudget = (float)v; mktLabel.Text = $"预算: ¥{_res.MonthlyMarketingBudget/10000f:F1}万/月"; };
        mktRow.AddChild(mktSlider);
        _content.AddChild(mktRow);
        _content.AddChild(MkL("保底效果：已发售6个月以上的老游戏销量衰减减缓", 9, new Color(0.45f, 0.5f, 0.55f)));

        // ── 保险 ──
        _content.AddChild(MkL("", 8, Colors.White));
        _content.AddChild(MkL("── 风险保险（年付制，到期自动失效）──", 13, new Color(0.25f, 0.28f, 0.32f)));

        string legalStatus = _res.HasLegalInsurance ? $"已投保(到期:{_res.InsuranceExpiryMonth})" : "未投保";
        var legalRow = new HBoxContainer();
        legalRow.AddChild(MkL($"法务顾问 ¥50,000/年 - {legalStatus}", 11, _res.HasLegalInsurance ? new Color(0.2f, 0.6f, 0.3f) : new Color(0.6f, 0.3f, 0.2f)));
        if (!_res.HasLegalInsurance)
        {
            var legalBtn = MkB("购买", 80, 24, 10);
            legalBtn.Pressed += () => { if (_res.BuyLegalInsurance()) RenderFinanceOps(); else _gm.ShowToast(Loc.Tr("toast.funds_low"), Loc.TrF("toast.funds_need", 50000), new Color(0.9f, 0.3f, 0.2f)); };
            legalRow.AddChild(legalBtn);
        }
        _content.AddChild(legalRow);
        _content.AddChild(MkL("  效果：法律事件损失减半", 8, new Color(0.40f, 0.42f, 0.45f)));

        string cyberStatus = _res.HasCyberInsurance ? $"已投保(到期:{_res.InsuranceExpiryMonth})" : "未投保";
        var cyberRow = new HBoxContainer();
        cyberRow.AddChild(MkL($"网络安全 ¥30,000/年 - {cyberStatus}", 11, _res.HasCyberInsurance ? new Color(0.2f, 0.6f, 0.3f) : new Color(0.6f, 0.3f, 0.2f)));
        if (!_res.HasCyberInsurance)
        {
            var cyberBtn = MkB("购买", 80, 24, 10);
            cyberBtn.Pressed += () => { if (_res.BuyCyberInsurance()) RenderFinanceOps(); else _gm.ShowToast(Loc.Tr("toast.funds_low"), Loc.TrF("toast.funds_need", 30000), new Color(0.9f, 0.3f, 0.2f)); };
            cyberRow.AddChild(cyberBtn);
        }
        _content.AddChild(cyberRow);
        _content.AddChild(MkL("  效果：服务器事故玩家不流失", 8, new Color(0.40f, 0.42f, 0.45f)));

        // ── 引擎生态 ──
        _content.AddChild(MkL("", 8, Colors.White));
        _content.AddChild(MkL("── 引擎生态投资 ──", 13, new Color(0.25f, 0.28f, 0.32f)));
        var primaryEng = _gm.Engines.Count > 0 ? _gm.Engines[0] : null;
        if (primaryEng == null)
        {
            _content.AddChild(MkL("尚无引擎（需先研发引擎）", 11, new Color(0.5f, 0.5f, 0.5f)));
        }
        else
        {
            var summitRow = new HBoxContainer();
            summitRow.AddChild(MkL($"技术峰会 ¥200,000/次 → 声誉+12, 吸引5~10个授权", 11, new Color(0.2f, 0.3f, 0.5f)));
            var summitBtn = MkB("举办", 80, 24, 10);
            var eng = primaryEng;
            summitBtn.Pressed += () => { if (_res.HostTechSummit(eng)) RenderFinanceOps(); else _gm.ShowToast(Loc.Tr("toast.funds_low"), Loc.TrF("toast.funds_need", 200000), new Color(0.9f, 0.3f, 0.2f)); };
            summitRow.AddChild(summitBtn);
            _content.AddChild(summitRow);

            var indieRow = new HBoxContainer();
            indieRow.AddChild(MkL($"独立游戏基金 ¥100,000/次 → 注资一家AI工作室+获得引擎授权", 11, new Color(0.2f, 0.4f, 0.3f)));
            var indieBtn = MkB("资助", 80, 24, 10);
            indieBtn.Pressed += () => { if (_res.FundIndieGame(eng)) RenderFinanceOps(); else _gm.ShowToast(Loc.Tr("toast.funds_low"), Loc.TrF("toast.funds_need", 100000), new Color(0.9f, 0.3f, 0.2f)); };
            indieRow.AddChild(indieBtn);
            _content.AddChild(indieRow);
        }

        var backFin = MkB("返回", 100, 30, 12);
        backFin.Pressed += () => RenderMainMenu();
        _content.AddChild(backFin);
    }

    // ══════════════════ 员工管理页面 ══════════════════
    private void RenderEmployeePage()
    {
        ShowPage("employee");
        _content.AddChild(MkL("员工管理", 18, new Color(0.10f, 0.14f, 0.22f)));

        if (_empMgr.Employees.Count == 0)
        {
            _content.AddChild(MkL("当前无员工，请先在人事页面招聘", 13, new Color(0.6f, 0.3f, 0.2f)));
            var backEmp = MkB("返回", 100, 30, 12);
            backEmp.Pressed += () => RenderMainMenu();
            _content.AddChild(backEmp);
            return;
        }

        // ── 员工列表（参考公司页面风格）──
        _content.AddChild(MkL("", 6, Colors.White));
        _content.AddChild(MkL("点击员工查看详情 / 培训", 11, new Color(0.4f, 0.45f, 0.5f)));

        // 表头
        var header = new HBoxContainer();
        header.AddChild(MkL("姓名", 12, new Color(0.12f, 0.16f, 0.25f)));
        header.AddChild(MkL("等级", 12, new Color(0.12f, 0.16f, 0.25f)));
        header.AddChild(MkL("疲劳", 12, new Color(0.12f, 0.16f, 0.25f)));
        header.AddChild(MkL("满意度", 12, new Color(0.12f, 0.16f, 0.25f)));
        header.AddChild(MkL("操作", 12, new Color(0.12f, 0.16f, 0.25f)));
        _content.AddChild(header);

        foreach (var emp in _empMgr.Employees)
        {
            var row = new HBoxContainer();
            string traitIcon = GetTraitIcon(emp.Trait);

            // 姓名 + 标签
            string tags = "";
            if (emp.IsChiefArchitect) tags += "★";
            if (emp.TrainingLeaveMonths > 0) tags += " 🏫";
            if (emp.IsCaptain) tags += " 👤";
            if (emp.TeamName != null) tags += $" [{emp.TeamName}]";

            Color nameColor = emp.Satisfaction < 20 ? new Color(0.9f, 0.2f, 0.2f)
                : emp.Satisfaction < 40 ? new Color(0.9f, 0.6f, 0.2f)
                : new Color(0.12f, 0.16f, 0.25f);

            var nameBtn = new Button { Text = $"{traitIcon}{emp.Name}{tags}", Flat = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            nameBtn.AddThemeFontSizeOverride("font_size", 11);
            nameBtn.AddThemeColorOverride("font_color", nameColor);
            nameBtn.Alignment = HorizontalAlignment.Left;
            var e = emp;
            nameBtn.Pressed += () => ShowEmployeeDetail(e);
            row.AddChild(nameBtn);

            // 等级
            int avgLevel = emp.Skills.Count > 0 ? (int)emp.Skills.Values.Average(s => s.Level) : 0;
            row.AddChild(MkL(Loc.TrF("ui.lv", avgLevel), 11, new Color(0.3f, 0.5f, 0.3f)));

            // 疲劳
            Color fatigueColor = emp.Fatigue > 70 ? new Color(0.9f, 0.3f, 0.2f) : emp.Fatigue > 40 ? new Color(0.9f, 0.6f, 0.2f) : new Color(0.3f, 0.7f, 0.3f);
            row.AddChild(MkL($"{emp.Fatigue:F0}%", 11, fatigueColor));

            // 满意度
            Color satColor = emp.Satisfaction > 70 ? new Color(0.2f, 0.7f, 0.3f) : emp.Satisfaction > 40 ? new Color(0.9f, 0.6f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);
            row.AddChild(MkL($"{emp.Satisfaction:F0}%", 11, satColor));

            // 操作按钮
            var trainBtn = MkB("培训¥3万", 90, 24, 10);
            var e2 = emp;
            trainBtn.Pressed += () =>
            {
                if (_res.TrainEmployee(e2))
                    _gm.ShowToast("外部培训", $"{e2.Name}获得+30经验，当月休假", new Color(0.3f, 0.7f, 0.5f));
                RenderEmployeePage();
            };
            row.AddChild(trainBtn);

            _content.AddChild(row);
        }

        // ── 汇总行 ──
        int totalEmp1 = _empMgr.Employees.Count;
        float totalSalary1 = _empMgr.Employees.Sum(e => e.Salary);
        float avgFatigue1 = totalEmp1 > 0 ? _empMgr.Employees.Average(e => e.Fatigue) : 0;
        float avgSat1 = totalEmp1 > 0 ? _empMgr.Employees.Average(e => e.Satisfaction) : 0;
        var sumRow = new HBoxContainer();
        sumRow.AddChild(MkL($"📊 汇总 ({totalEmp1}人)", 11, new Color(0.15f, 0.20f, 0.28f)));
        sumRow.AddChild(MkL("", 11, new Color(0.5f, 0.5f, 0.5f)));
        sumRow.AddChild(MkL($"{avgFatigue1:F0}%", 11, new Color(0.5f, 0.5f, 0.5f)));
        sumRow.AddChild(MkL($"{avgSat1:F0}%", 11, new Color(0.5f, 0.5f, 0.5f)));
        sumRow.AddChild(MkL($"月薪¥{totalSalary1:N0}", 11, new Color(0.15f, 0.5f, 0.2f)));
        _content.AddChild(sumRow);

        var backEmp2 = MkB("返回主菜单", 120, 32, 12);
        backEmp2.Pressed += () => RenderMainMenu();
        _content.AddChild(backEmp2);
    }

    // ══════════════════ 员工详情弹窗 ══════════════════
    private void ShowEmployeeDetail(Employee emp)
    {
        // 参考公司页面风格：半透明深色面板 + 卡片式信息
        var vp = _gm.GetViewport().GetVisibleRect().Size;
        float S(float v) => v * _gm.UIScale;
        float pw = S(420), ph = S(350);

        var dlg = new Panel { Position = new((vp.X - pw) / 2, (vp.Y - ph) / 2), Size = new(pw, ph), MouseFilter = Control.MouseFilterEnum.Stop };
        dlg.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.12f, 0.16f, 0.97f),
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.3f, 0.5f, 0.8f, 0.5f)
        });

        float ly = S(14);

        // 标题行
        string traitIcon = GetTraitIcon(emp.Trait);
        var title = new Label { Text = $"{traitIcon} {emp.Name}", Position = new(S(14), ly), Size = new(pw - S(28), S(24)) };
        title.AddThemeFontSizeOverride("font_size", 18);
        title.AddThemeColorOverride("font_color", new Color(0.85f, 0.82f, 0.65f));
        dlg.AddChild(title);

        ly += S(33);

        // 基本信息卡片
        string teamInfo = emp.TeamName != null ? $"- 所属团队: {emp.TeamName}" : "- 未分配团队";
        string leaveInfo = emp.TrainingLeaveMonths > 0 ? $" (培训休假中，剩余{emp.TrainingLeaveMonths}月)" : "";
        string roles = "";
        if (emp.IsChiefArchitect) roles += "首席架构师 ";
        if (emp.IsCaptain) roles += "队长 ";
        if (emp.IsHardwareEngineer) roles += "硬件工程师 ";
        var infoLabel = new Label
        {
            Text = $"- 入职: {emp.MonthsEmployed}个月 ({emp.CompanyYears}年)\n- 月薪: ¥{emp.Salary:N0}{leaveInfo}\n{teamInfo}\n- 角色: {(string.IsNullOrEmpty(roles) ? "开发者" : roles)}\n- 特质: {(emp.Trait != EmployeeTrait.None ? emp.Trait.ToString() : "无")}\n- 完成项目: {emp.ProjectsCompleted}个 | 最高评分: {emp.HighScoreProjects:F0}",
            Position = new(S(14), ly), Size = new(pw - S(28), S(85))
        };
        infoLabel.AddThemeFontSizeOverride("font_size", 11);
        infoLabel.AddThemeColorOverride("font_color", new Color(0.72f, 0.75f, 0.80f));
        dlg.AddChild(infoLabel);

        ly += S(90);

        // 状态条：疲劳+满意度
        Color fatigueColor = emp.Fatigue > 70 ? new Color(0.9f, 0.3f, 0.2f) : emp.Fatigue > 40 ? new Color(0.9f, 0.6f, 0.2f) : new Color(0.3f, 0.7f, 0.3f);
        Color satColor = emp.Satisfaction > 70 ? new Color(0.2f, 0.7f, 0.3f) : emp.Satisfaction > 40 ? new Color(0.9f, 0.6f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);

        var statRow = new HBoxContainer { Position = new(S(14), ly), Size = new(pw - S(28), S(22)) };
        statRow.AddChild(MkL($"疲劳: {emp.Fatigue:F0}%  ", 12, fatigueColor));
        statRow.AddChild(MkL($"满意度: {emp.Satisfaction:F0}%", 12, satColor));
        dlg.AddChild(statRow);

        ly += S(28);

        // 技能详情
        if (emp.Skills.Count > 0)
        {
            foreach (var sk in emp.Skills.OrderByDescending(sk => sk.Value.Level))
            {
                var skRow = new HBoxContainer { Position = new(S(14), ly), Size = new(pw - S(28), S(18)) };
                skRow.AddChild(MkL($"{sk.Key.Name()}: ", 11, new Color(0.55f, 0.60f, 0.70f)));
                skRow.AddChild(MkL($"{Loc.TrF("ui.lv", sk.Value.Level)} ({Loc.TrF("ui.exp", sk.Value.Exp)})", 11, new Color(0.3f, 0.6f, 0.8f)));
                dlg.AddChild(skRow);
                ly += S(18);
            }
        }

        ly += S(8);

        // 培训按钮
        float btnY = ly;
        var trainBtn = new Button { Text = Loc.Tr("dev.team_train"), Position = new(S(14), btnY), Size = new(S(300), S(28)) };
        trainBtn.Flat = true;
        trainBtn.AddThemeFontSizeOverride("font_size", 12);
        trainBtn.AddThemeColorOverride("font_color", new Color(0.3f, 0.7f, 0.5f));
        trainBtn.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.20f, 0.25f, 0.30f, 0.6f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        trainBtn.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = new Color(0.25f, 0.30f, 0.35f, 0.8f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        var te = emp;
        trainBtn.Pressed += () =>
        {
            if (_res.TrainEmployee(te))
            {
                _gm.ShowToast("外部培训", $"{te.Name}获得+30经验，当月休假培训", new Color(0.3f, 0.7f, 0.5f));
                dlg.QueueFree();
                RenderEmployeePage();
            }
            else
            {
                _gm.ShowToast(Loc.Tr("toast.funds_low"), Loc.TrF("toast.funds_need", 30000), new Color(0.9f, 0.3f, 0.2f));
            }
        };
        dlg.AddChild(trainBtn);

        var closeBtn = new Button { Text = "关闭", Position = new(pw - S(80), S(16)), Size = new(S(64), S(26)), Flat = true };
        closeBtn.Pressed += () => dlg.QueueFree();
        dlg.AddChild(closeBtn);

        _gm.UiLayer.AddChild(dlg);
    }

    private static string GetTraitIcon(EmployeeTrait trait) => trait switch
    {
        EmployeeTrait.Genius => "🧠",
        EmployeeTrait.Workaholic => "🐴",
        EmployeeTrait.Social => "💬",
        EmployeeTrait.LoneWolf => "🐺",
        EmployeeTrait.Chill => "🧘",
        EmployeeTrait.Perfectionist => "✨",
        EmployeeTrait.Lucky => "🍀",
        EmployeeTrait.Mentor => "📚",
        EmployeeTrait.Ambitious => "🔥",
        EmployeeTrait.Nostalgic => "💾",
        EmployeeTrait.TechClean => "🧹",
        EmployeeTrait.Sensitive => "😢",
        _ => "👤"
    };
}
