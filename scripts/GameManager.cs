using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;

public partial class GameManager : Node3D
{
    public int GameMonth { get; set; }
    public int GameYear => GameMonth / 12 + 1;
    public int MonthInYear => GameMonth % 12 + 1;
    public float MonthProgress { get; set; }
    private bool _paused;
    public bool Paused
    {
        get => _paused;
        set
        {
            _paused = value;
            if (!_paused)
            {
                CloseSimplePause();
                // 确保残留 tooltip 不阻塞输入
                if (_tooltipPanel != null) _tooltipPanel.Visible = false;
                HideTooltip();
            }
        }
    }

    // 倍速 1~8，快捷键数字键/小键盘
    private int _gameSpeed = 1;
    public int GameSpeed => _gameSpeed;
    private static readonly float BaseMonthSpeed = 0.05f;

    private ResourceManager _res;
    private EmployeeManager _empMgr;
    private TeamManager _teamMgr;
    private TechManager _techMgr;
    private GameDevManager _devMgr;
    private CompetitorAI _competitor;
    private FanManager _fanMgr;
    private RoomManager _roomMgr;
    private TechDebtManager _debtMgr;
    private MarketTrendManager _trendMgr;
    private AchievementManager _achMgr;
    private ServerManager _serverMgr;
    private TutorialManager _tutorialMgr;
    private CardSystem _cardSystem;
    private SprintSystem _sprintSys;
    private SoundManager _soundMgr;
    private CrisisEventTree _crisisSys;
    private StudioDNA _dnaSys;
    private AudienceSystem _audienceSys;
    private ReleaseCalendar _calSys;
    private LiveOpsSystem _liveOpsSys;
    private BrandSystem _brandSys;
    private FounderLegacy _legacySys;

    // ── 新系统字段 ──
    private SprintSystemEx _sprintSysEx;
    private EmployeeSystemEx _empSysEx;
    private EconomySystemEx _economySys;
    private MarketSystemEx _marketSys;
    private CommunitySystemEx _communitySys;
    private IPSystemEx _ipSysEx;
    private UISystemEx _uiSysEx;
    private LongTermSystemEx _longTermSys;

    public ResourceManager ResMgr => _res;
    public CardSystem CardSys => _cardSystem;
    public Node3D BuildingNode { get; private set; }
    private Camera3D _camera;

    // 相机围绕中心点旋转
    private Vector3 _camTarget = new(0, 1, 0);
    private float _camYaw, _camPitch = Mathf.DegToRad(45), _camDist = 12;
    private float _camRotSensitivity = 0.3f;       // 右键旋转灵敏度
    private float _camPanSpeed = 12f;               // WASD平移速度
    private float _camZoomSpeed = 2f;
    private float _camMinDist = 5, _camMaxDist = 40;
    private float _camMinPitch = Mathf.DegToRad(15), _camMaxPitch = Mathf.DegToRad(80);

    // 输入状态
    private bool _isRotating;  // 右键拖拽旋转
    private Vector2 _lastMouseGlobal;

    public float UIScale = 1.0f;
    public Control UiLayer => _uiLayer;
    private Control _uiLayer;
    private Panel _bottomNav;
    private List<Button> _tabButtons = new();
    private Panel _hoverTooltipPanel;
    private Label _hoverTooltipLabel;
    private Label _moneyLabel, _inspirationLabel, _fanLabel, _dateLabel, _empLabel, _bestScoreLabel, _debtWarnLabel, _trustLabel;
    private Button _pauseBtn;
    private OptionButton _speedOpt;
    private Label _predictionLabel;       // 主界面项目预测面板
    private Label _newsTickerLabel;        // 新闻滚动条
    private int _lastNewsIdx = -1;         // 上次显示的新闻索引
    private float _newsScrollTimer;
    private Panel _companyPanel;           // 公司列表面板引用
    private string _lastMoneyTxt, _lastInspTxt, _lastFanTxt, _lastDateTxt, _lastEmpTxt, _lastBestScore, _lastDebtTxt;
    private int _lastSpeed = -1;
    private bool _lastPaused = false; // 强制首次刷新 + 暂停按钮初始为 ▶
    private float _scoreUpdateTimer;
    private int _hudFrameCounter;           // UI 节流帧计数器
    private int _frameCount;                // 成就检查帧计数器
    private bool _hudNeedsFullRefresh;      // 完整 HUD 刷新脏标记
    private bool _predictionDirty = true;   // 预测面板脏标记
    private bool _needCompanyDetailRefresh;
    private string _lastCompanyDetailName;
    private bool _lastCompanyDetailIsPlayer;
    private AIStudio _lastCompanyDetailStudio;
    private float _companyRefreshTimer;    // 公司列表刷新人
    private ScrollContainer _companyScroll; // 公司列表滚动容器
    private VBoxContainer _companyList;     // 公司行容器
    private PanelContainer _companySummaryRow; // 汇总行
    private HashSet<string> _selectedCompanies = new(); // 选中的公司名
    private int _lastClickIndex = -1;       // 上次点击的行索引（Shift选区间用）
    private bool _companyListNeedRebuild;   // 下次刷新是否重建
    private float _lastCompanyClickTime;    // 双击检测时间
    private string _lastCompanyClicked;     // 双击检测上次点击的公司
    private string _hoveredCompany;         // 当前悬停的公司名
    private Panel _tooltipPanel;
    private Label _tooltipLabel;
    private ProgressBar _debtProgBar;
    private int _autoSaveCounter;

    // 跳票延期理由（与GameDevManager.DelayReasons同步）
    private static readonly string[] delayReasons = {
        "delay.quality", "delay.tech", "delay.content", "delay.staff"
    };

    // 加载遮罩
    private ColorRect _loadingOverlay;
    private Panel _pausePanel;
    private Panel _saveLoadPanel;
    private ColorRect _pauseOverlay;

    private string SavePath => GlobalSettings.GetSavePath();

    // 外包中心
    public List<OutsourceTask> OutsourceTaskPool { get; set; } = new();

    // 引擎列表（每个引擎独立）
    public List<GameEngine> Engines { get; set; } = new();

    // ── 传奇标记 ──
    public bool HasLegendaryLegacy { get; set; }

    // ── 风口保险 ──
    public List<WindInsurance> WindInsurances { get; set; } = new();
    public struct WindInsurance { public string GenreOrTheme; public int MonthsLeft; public float SalesBonus; }

    // ── 胜利系统 ──
    private VictoryManager _victoryMgr;
    // ── Mod API 桥接 ──
    private ModBridge _modBridge;
    // ── 创始人配置 ──
    public FounderProfile Founder { get; set; } = new();
    // ── 贷款系统 ──
    public LoanSystem Loan { get; set; } = new();
    // ── 商战系统 ──
    public CorporateActions CorpActions { get; set; } = new();

    // ── 统一模态栈：所有弹窗/面板入栈，ESC只关栈顶 ──
    private Stack<Panel> _openPanels = new();
    private Panel _openPanel; // 便捷引用，指向栈顶
    private DevMenu _openPopup;
    private GameDevPopup _openDevPopup;
    public void SetDevPopup(GameDevPopup p) => _openDevPopup = p;
    private int _activeTab = -1;
    private Panel _techTopPanel; // 科技面板的研发中横条，用于实时刷新进度
    private Action _devScoreUpdater;

    /// <summary>将一个面板压入模态栈并显示（自动暂停）</summary>
    public void PushPanel(Panel p)
    {
        _openPanels.Push(p);
        _openPanel = p;
        _uiLayer.AddChild(p);
        Paused = true;
    }

    /// <summary>关闭栈顶面板（跳过 Protected 面板）</summary>
    private void PopTopPanel()
    {
        if (_openPanels.Count == 0) return;
        var p = _openPanels.Peek();
        if (p is DragPanel dp && dp.Protected) return; // 受保护面板不关闭
        _openPanels.Pop();
        p?.QueueFree();
        _openPanel = _openPanels.Count > 0 ? _openPanels.Peek() : null;
        if (_openPanels.Count == 0 && !IsAnyModalOpen)
        {
            _activeTab = -1;
            Paused = false;
        }
        RefreshActivePanelCallback();
    }



    /// <summary>根据当前顶层面板重新注册 Ctrl+A 回调</summary>
    private void RefreshActivePanelCallback()
    {
        if (_openPanels.Count == 0) { _activePanelSelectAllAction = null; _activePanelEnterAction = null; return; }
        var top = _openPanels.Peek();
        // 检查是否是公司面板
        if (_companyPanel != null && GodotObject.IsInstanceValid(_companyPanel) && top == _companyPanel)
        {
            _activePanelSelectAllAction = () =>
            {
                _selectedCompanies.Clear();
                var devMgr = GetNodeOrNull<GameDevManager>("GameDevManager");
                if (devMgr == null) return;
                var all = BuildCompanyList(devMgr);
                foreach (var c in all) _selectedCompanies.Add(c.name);
                ApplyAllRowHighlights();
                _lastClickIndex = -1;
            };
        }
    }

    public override void _Ready()
    {
        UIScale = GlobalSettings.UIScale;

        // Mod 桥接（必须先于 ApplyAll，供 GDScript Mod 使用）
        _modBridge = new ModBridge { Name = "ModBridge" };
        AddChild(_modBridge);
        _modBridge.Init(this);
        // 注册为 GDScript 全局单例，Mod 可直接用 ModBridge.add_money() 调用
        Engine.RegisterSingleton("ModBridge", _modBridge);

        // Mod 系统初始化
        ModManager.Init();
        ModManager.ApplyAll(this);

        // DLC 系统
        DlcManager.ScanAll();
        DlcManager.Log("System", $"Game started — v{ModManager.GameVersion}");
        DlcManager.Log("System", $"Mods loaded: {ModManager.LoadedMods.Count} total, {ModManager.ActiveScriptMods.Count} script");
        DlcManager.Log("System", "Press F9 to view this log");

        // 程序集 Mod 加载
        string asmModDir = ProjectSettings.GlobalizePath("res://mods/assemblies");
        ModAssemblyLoader.LoadAllFrom(asmModDir, this);

        // 资源覆盖索引
        ModAssetOverride.Initialize();

        // UI 补丁系统
        ModUIPatch.Init(this);



        // Mod 控制台（预创建让 _Input 能接收 F12 事件）
        if (!InputMap.HasAction("toggle_console"))
        {
            InputMap.AddAction("toggle_console");
            var evt = new InputEventKey { Keycode = Key.F12 };
            InputMap.ActionAddEvent("toggle_console", evt);
        }
        ModConsole.Init(this);

        ModMethodOverride.CallVoid("game_init_mod_loaded", ModMethodOverride.Args(), () => { });

        // 国际化初始化
        Loc.Init();
        // 如果用户已保存语言偏好，覆盖自动检测
        if (GlobalSettings.Language >= 0)
            Loc.CurrentLang = GlobalSettings.Language;

        // ── 加载页 ──
        var loadOverlay = new ColorRect { Color = new Color(0.78f, 0.82f, 0.88f) };
        loadOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(loadOverlay);

        var loadLabel = new Label
        {
            Text = Loc.Tr("ui.loading_game"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        loadLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        loadLabel.AddThemeFontSizeOverride("font_size", 28);
        loadLabel.AddThemeColorOverride("font_color", new Color(0.35f, 0.38f, 0.42f));
        loadOverlay.AddChild(loadLabel);
        _loadingOverlay = loadOverlay;

        // UI 层提前创建，供 BuildFounderCreationScreen 使用
        _uiLayer = new Control();
        _uiLayer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _uiLayer.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_uiLayer);

        // 节点引用必须在 _Ready 同步获取
        _res = GetNode<ResourceManager>("ResourceManager");
        _empMgr = GetNode<EmployeeManager>("EmployeeManager");
        _teamMgr = GetNode<TeamManager>("TeamManager");
        _techMgr = GetNode<TechManager>("TechManager");
        _devMgr = GetNode<GameDevManager>("GameDevManager");
        _competitor = GetNode<CompetitorAI>("CompetitorAI");
        _fanMgr = GetNode<FanManager>("FanManager");
        _roomMgr = GetNode<RoomManager>("RoomManager");
        BuildingNode = GetNode<Node3D>("Building");
        _camera = GetNode<Camera3D>("Camera3D");

        // 动态创建子节点
        _debtMgr = new TechDebtManager { Name = "TechDebtManager" };
        AddChild(_debtMgr);
        _trendMgr = new MarketTrendManager { Name = "MarketTrendManager" };
        AddChild(_trendMgr);
        _achMgr = new AchievementManager { Name = "AchievementManager" };
        AddChild(_achMgr);
        _serverMgr = new ServerManager { Name = "ServerManager" };
        AddChild(_serverMgr);
        _tutorialMgr = new TutorialManager { Name = "TutorialManager" };
        AddChild(_tutorialMgr);
        var storyEvt = new StoryEvents { Name = "StoryEvents" };
        AddChild(storyEvt);
        _cardSystem = new CardSystem { Name = "CardSystem" };
        AddChild(_cardSystem);

        _soundMgr = new SoundManager { Name = "SoundManager" };
        AddChild(_soundMgr);
        CardUI.OnClickSound = () => _soundMgr?.PlayClick();

        // 新系统注册
        _sprintSys = new SprintSystem { Name = "SprintSystem" }; AddChild(_sprintSys);
        _crisisSys = new CrisisEventTree { Name = "CrisisEventTree" }; AddChild(_crisisSys);
        _dnaSys = new StudioDNA { Name = "StudioDNA" }; AddChild(_dnaSys);
        _audienceSys = new AudienceSystem { Name = "AudienceSystem" }; AddChild(_audienceSys);
        _calSys = new ReleaseCalendar { Name = "ReleaseCalendar" }; AddChild(_calSys);
        _liveOpsSys = new LiveOpsSystem { Name = "LiveOpsSystem" }; AddChild(_liveOpsSys);
        _brandSys = new BrandSystem { Name = "BrandSystem" }; AddChild(_brandSys);
        _legacySys = new FounderLegacy { Name = "FounderLegacy" }; AddChild(_legacySys);

        // ══════ 新系统注册 ══════
        _sprintSysEx = new SprintSystemEx { Name = "SprintSystemEx" }; AddChild(_sprintSysEx);
        _empSysEx = new EmployeeSystemEx { Name = "EmployeeSystemEx" }; AddChild(_empSysEx);
        _economySys = new EconomySystemEx { Name = "EconomySystemEx" }; AddChild(_economySys);
        _marketSys = new MarketSystemEx { Name = "MarketSystemEx" }; AddChild(_marketSys);
        _communitySys = new CommunitySystemEx { Name = "CommunitySystemEx" }; AddChild(_communitySys);
        _ipSysEx = new IPSystemEx { Name = "IPSystemEx" }; AddChild(_ipSysEx);
        _uiSysEx = new UISystemEx { Name = "UISystemEx" }; AddChild(_uiSysEx);
        _longTermSys = new LongTermSystemEx { Name = "LongTermSystemEx" }; AddChild(_longTermSys);
        var encyMgr = new EncyclopediaManager { Name = "EncyclopediaManager" }; AddChild(encyMgr);
        _victoryMgr = new VictoryManager { Name = "VictoryManager" }; AddChild(_victoryMgr);

        // 所有子节点就绪后再注册全局服务定位器
        Services.Initialize(this);
        CardUI.Init(this);
        ModAPI.Init(this);
        ModConsole.CreateNow(); // 预创建控制台（_uiLayer 已就绪）

        ModMethodOverride.CallVoid("game_init_nodes_ready", ModMethodOverride.Args(), () => { });

        RenderingServer.SetDefaultClearColor(new Color(0.65f, 0.80f, 0.95f));

        // 延迟一帧执行重初始化，让加载页先渲染出来
        ModMethodOverride.CallVoid("game_init_before_start", ModMethodOverride.Args(),
            () => CallDeferred(nameof(InitGame)));
    }

    private void InitGame()
    {
        _modalLock = 0; // 清空所有模态锁

        if (GlobalSettings.NewGame)
            GameMonth = 0;

        if (!GlobalSettings.NewGame && !GlobalSettings.LoadGame)
            GlobalSettings.NewGame = true;

        ModMethodOverride.CallVoid("game_init_before_load", ModMethodOverride.Args(),
            () =>
        {
            if (GlobalSettings.LoadGame)
            {
                bool loaded = false;
                try
                {
                    // 如果主菜单传了指定存档路径，用那个
                    string overridden = MenuManager.GetLoadPathOverride();
                    if (!string.IsNullOrEmpty(overridden)) LoadGame(overridden);
                    else LoadGame();
                    loaded = true;
                    ModAPI.FireHooks(ModAPI.GameHook.OnGameLoad);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"LoadGame failed: {ex.Message}\n{ex.StackTrace}");
                }
                if (!loaded)
                {
                    GlobalSettings.LoadGame = false;
                    GlobalSettings.NewGame = true;
                }
            }
        });

        // ═══ 员工初始化：必须在 InitStartingHouse 之前（SpawnEmployees 依赖员工列表）═══
        if (_empMgr.Employees.Count == 0)
        {
            _empMgr.InitStartingEmployees();
            // 把创始人加入 Startup 团队（_Ready 时团队已建好但员工还不存在）
            var startupTeam = _teamMgr.Teams.FirstOrDefault();
            var founder = _empMgr.Employees.FirstOrDefault();
            if (startupTeam != null && founder != null && startupTeam.Members.Count == 0)
            {
                startupTeam.Members.Add(founder);
                founder.TeamName = startupTeam.Name;
            }
        }

        ModMethodOverride.CallVoid("game_init_before_new", ModMethodOverride.Args(),
            () =>
        {
            if (GlobalSettings.NewGame)
            {
                ModAPI.FireHooks(ModAPI.GameHook.OnGameStart);
                _legacySys.FoundedYear = GameYear;

                _roomMgr.InitStartingHouse();

                // 创始人创建 — 新游戏且未创建则展示角色创建页面
                if (!Founder.HasCreated)
                {
                    BuildFounderCreationScreen();
                    if (GlobalSettings.NewGame && !_tutorialMgr.TutorialCompleted)
                        _tutorialMgr.StartTutorial();
                }
                Engines.Clear();
                var starter = new GameEngine
                {
                    Name = Loc.Tr("devmenu.starter_engine"),
                    Generation = 1,
                    AppliedTechs = new List<string> { "2d_v1" },
                    BizModel = EngineBizModel.Closed,
                    Reputation = 10
                };
                starter.UpdateCapabilities();
                starter.DerivePerks();
                Engines.Add(starter);
            }
        });

        ModMethodOverride.CallVoid("game_init_hud", ModMethodOverride.Args(),
            () =>
        {
            if (!Founder.HasCreated)
                CallDeferred(nameof(InitHUDPostFounder));
            else
            {
                BuildHUD();
                CallDeferred(nameof(StartTutorialDeferred));
            }
        });

        // 移除加载遮罩
        if (_loadingOverlay != null)
        {
            _loadingOverlay.QueueFree();
            _loadingOverlay = null;
        }

        ModMethodOverride.CallVoid("game_init_complete", ModMethodOverride.Args(), () => { });
    }

    /// <summary>延迟构建全部 HUD（创始人创建后或开局时）</summary>
    private void InitHUDPostFounder()
    {
        BuildHUD();

        if (!Founder.HasCreated)
        {
            // HUD 已建但创始人还在设置 → 只保留创始人画面可见
            for (int i = 0; i < _uiLayer.GetChildCount(); i++)
                if (_uiLayer.GetChild(i) is Control c)
                    c.Visible = false;
            // 还原创始人画面
            for (int i = _uiLayer.GetChildCount() - 1; i >= 0; i--)
                if (_uiLayer.GetChild(i) is ColorRect cr) { cr.Visible = true; break; }
            for (int i = _uiLayer.GetChildCount() - 1; i >= 0; i--)
            {
                var child = _uiLayer.GetChild(i);
                if (child is ColorRect cr && cr.Visible)
                {
                    // 显示它的子节点（创始人卡片）
                    foreach (Node grandchild in cr.GetChildren())
                        if (grandchild is Control gc)
                            gc.Visible = true;
                    break;
                }
            }
            // 显示教程弹窗（如果有的话）
            for (int i = 0; i < _uiLayer.GetChildCount(); i++)
                if (_uiLayer.GetChild(i) is TutorialPopup tp)
                    tp.Visible = true;
        }

        if (GlobalSettings.NewGame && !_tutorialMgr.TutorialCompleted && Founder.HasCreated)
            _tutorialMgr.StartTutorial();
    }

    private void StartTutorialDeferred()
    {
        if (GlobalSettings.NewGame && !_tutorialMgr.TutorialCompleted)
            _tutorialMgr.StartTutorial();
    }

    public override void _Process(double delta)
    {
        var args = ModMethodOverride.Args(("delta", delta));
        ModMethodOverride.CallVoid("game_process_tick", args, () => ProcessGameTick(delta));
    }

    private void ProcessGameTick(double delta)
    {
        if (_res == null) return; // 防御：初始化未完成时跳过
        UpdateCameraPos();
        // 每120帧检查成就（暂停时也触发）
        if (_frameCount++ % 120 == 0)
            Services.AchievementManager?.CheckNow();
        if (!IsUIPanelOpen) HandleCameraKeys((float)delta);
        if (Paused) { UpdateHUDTimeSensitive(); return; }
        MonthProgress += (float)delta * BaseMonthSpeed * _gameSpeed;
        if (MonthProgress >= 1.0f) { MonthProgress = 0; OnMonthEnd(); }
        UpdateHUDTimeSensitive();
        _hudNeedsFullRefresh = _hudNeedsFullRefresh || _hudFrameCounter++ % 60 == 0;
        if (_hudNeedsFullRefresh) { _hudNeedsFullRefresh = false; UpdateHUDFull(); }

        // 开发分数每0.5秒刷新
        _scoreUpdateTimer += (float)delta;
        if (_scoreUpdateTimer > 0.5f) { _scoreUpdateTimer = 0; _devScoreUpdater?.Invoke(); }

        // 科技面板进度条实时刷新
        RefreshTechProgressBar();

        // 预测面板 + 营收图表：数据变化或每60帧刷新
        if (_predictionDirty || _hudFrameCounter % 60 == 0)
        {
            _predictionDirty = false;
            UpdatePredictionPanel();
            UpdateRevenueChart();
        }

        // 买卖股票后刷新公司详情
        if (_needCompanyDetailRefresh)
        {
            _needCompanyDetailRefresh = false;
            RefreshCompanyDetailDelayed();
        }

        // 新闻滚动（每2.5秒翻一条）
        _newsScrollTimer += (float)delta;
        if (_newsScrollTimer > 2.5f && _newsTickerLabel != null)
        {
            _newsScrollTimer -= 2.5f;
            var feed = _competitor.NewsFeed;
            if (feed.Count > 0)
            {
                _lastNewsIdx = (_lastNewsIdx + 1) % feed.Count;
                var n = feed[_lastNewsIdx];
                _newsTickerLabel.Text = $" {n.Emoji}  {n.Headline} — {n.Detail}";
                _newsTickerLabel.AddThemeColorOverride("font_color", n.Color);
            }
        }

        // 公司列表面板实时刷新（暂时完全禁用——跟其他面板有冲突）
        // TODO: 之后改为信号驱动而不是轮询刷新
        // if (_companyPanel != null && ...)
    }

    public override void _Input(InputEvent @event)
    {
        // ESC 关闭日志覆盖层（最优先，不被任何 UI 拦截）
        if (@event is InputEventKey esk && esk.Pressed && !esk.Echo && esk.Keycode == Key.Escape)
        {
            if (_logOverlay != null) { _logOverlay.QueueFree(); _logOverlay = null; GetViewport().SetInputAsHandled(); return; }
        }
        // Mod 注册的自定义按键（在 ProcessGameInput 之前拦截）
        if (@event is InputEventKey ek && ek.Pressed && !ek.Echo)
        {
            var bridge = GetNodeOrNull<ModBridge>("ModBridge");
            if (bridge != null && bridge.HandleRegisteredKey(ek))
            {
                GetViewport().SetInputAsHandled();
                return;
            }
        }
        var args = ModMethodOverride.Args(("event", @event));
        ModMethodOverride.CallVoid("game_process_input", args, () => ProcessGameInput(@event));
    }

    private void ProcessGameInput(InputEvent @event)
    {
        // ── 弹窗打开时拦截鼠标滚轮，防止穿透到3D世界 ──
        if (_toastPanel != null && @event is InputEventMouseButton scrollEv
            && (scrollEv.ButtonIndex == MouseButton.WheelUp || scrollEv.ButtonIndex == MouseButton.WheelDown))
        {
            GetViewport().SetInputAsHandled(); return;
        }

        // 全局 UI 点击音效
        if (@event is InputEventMouseButton btnEv && btnEv.Pressed && btnEv.ButtonIndex == MouseButton.Left)
        {
            if (!_isRotating) _soundMgr?.PlayClick();
        }

        bool uiOpen = IsUIPanelOpen;
        var vp = GetViewport();

        // ── 右键击中时，先关闭已有的上下文菜单 ──
        if (_ctxMenuOpen && @event is InputEventMouseButton rmb && rmb.Pressed && rmb.ButtonIndex == MouseButton.Right)
        {
            foreach (var c in _uiLayer.GetChildren())
                if (c is PopupMenu pm && (pm.Name == "_ctxMenu" || pm.Name == "_batchMenu"))
                { pm.Hide(); pm.QueueFree(); }
            _ctxMenuOpen = false;
            // 不 SetInputAsHandled，让右键穿透到下层控件
        }

        // ── 面板打开时：_Input 先于 GUI，在这里拦截 Ctrl+A/Enter，屏蔽鼠标 ──
        if (uiOpen)
        {
            if (@event is InputEventKey ke && ke.Pressed && !ke.Echo)
            {
                if (ke.Keycode == Key.Escape)
                {
                    // 逐层：模态遮罩(百科/成就馆/冲刺) → 存档/设置 → 暂停 → 底部面板
                    if (_modalLock > 0 && _founderOverlay == null)
                    {
                        for (int i = _uiLayer.GetChildCount() - 1; i >= 0; i--)
                        {
                            var c = _uiLayer.GetChild(i);
                            if (c is ColorRect col && col.Color.A > 0.1f) { col.QueueFree(); break; }
                            if (c is Panel && c != _bottomNav) { c.QueueFree(); break; }
                        }
                        _modalLock = 0;
                        if (_openPanels.Count == 0) Paused = false;
                    }
                    else if (_saveLoadPanel != null) CloseSaveLoad();
                    else if (_pausePanel != null) ClosePauseMenu();
                    else if (_openPanels.Count > 0) PopTopPanel();
                    else TogglePauseMenu();
                    vp.SetInputAsHandled();
                    return;
                }
                // Ctrl+A 全选 — 统一通过 _activePanelSelectAllAction
                if (ke.Keycode == Key.A && (ke.CtrlPressed || ke.MetaPressed))
                {
                    _activePanelSelectAllAction?.Invoke();
                    vp.SetInputAsHandled();
                    return;
                }
                // Enter
                if (ke.Keycode == Key.Enter && _activePanelEnterAction != null)
                {
                    _activePanelEnterAction();
                    vp.SetInputAsHandled();
                    return;
                }
                // 弹窗内空格切换暂停/继续（仅在非输入框焦点时生效，有弹窗时锁定）
                if (ke.Keycode == Key.Space)
                {
                    var focus = vp.GuiGetFocusOwner();
                    if (!(focus is LineEdit || focus is TextEdit || focus is CheckBox)
                        && _openPanels.Count == 0 && _openPopup == null && _openDevPopup == null
                        && _toastPanel == null && !IsAnyModalOpen && _saveLoadPanel == null)
                    {
                        Paused = !Paused;
                        if (Paused) ShowSimplePause(); else CloseSimplePause();
                        vp.SetInputAsHandled();
                        return;
                    }
                }
                // 其他键盘不拦，但 return 避免走到 3D 交互
                return;
            }
            // 面板打开时屏蔽鼠标拖拽/旋转/缩放
            if (@event is InputEventMouseButton rmb2 && rmb2.ButtonIndex == MouseButton.Right)
            {
                // 右键：先关闭已有上下文菜单，然后穿透到下层控件
                foreach (var c in _uiLayer.GetChildren())
                    if (c is PopupMenu pm && (pm.Name == "_ctxMenu" || pm.Name == "_batchMenu"))
                        { pm.Hide(); pm.QueueFree(); }
                // 不 return，让右键继续传递给下层 GUI
            }
            else if (@event is InputEventMouse)
                return;
        }

        // ── 右键拖拽旋转（无面板时）──
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Right)
            {
                _isRotating = mb.Pressed;
                if (mb.Pressed) _lastMouseGlobal = vp.GetMousePosition();
            }
        }

        // ── 鼠标移动：右键旋转 ──
        if (@event is InputEventMouseMotion && _isRotating)
        {
            var m = vp.GetMousePosition();
            var d = m - _lastMouseGlobal;
            _lastMouseGlobal = m;

            _camYaw -= d.X * _camRotSensitivity * 0.01f;
            _camPitch += d.Y * _camRotSensitivity * 0.01f;
            _camPitch = Mathf.Clamp(_camPitch, _camMinPitch, _camMaxPitch);
        }

        // ── 滚轮缩放（面板打开时禁用） ──
        if (@event is InputEventMouseButton scroll && !IsUIPanelOpen)
        {
            if (scroll.ButtonIndex == MouseButton.WheelUp)
                _camDist = Mathf.Clamp(_camDist - _camZoomSpeed, _camMinDist, _camMaxDist);
            if (scroll.ButtonIndex == MouseButton.WheelDown)
                _camDist = Mathf.Clamp(_camDist + _camZoomSpeed, _camMinDist, _camMaxDist);
        }

        // ── 快捷键 ──
        if (@event is InputEventKey ke2 && ke2.Pressed)
        {
            // 倍速切换：主键盘数字 1~8
            if (ke2.Keycode >= Key.Key1 && ke2.Keycode <= Key.Key8)
            {
                _gameSpeed = (int)(ke2.Keycode - Key.Key1) + 1;
                vp.SetInputAsHandled();
                return;
            }
            // 倍速切换：小键盘数字 KP1~KP8
            if (ke2.Keycode >= Key.Kp1 && ke2.Keycode <= Key.Kp8)
            {
                _gameSpeed = (int)(ke2.Keycode - Key.Kp1) + 1;
                vp.SetInputAsHandled();
                return;
            }
            // F9：Mod 日志查看（F8 在编辑器中已被调试器占用）
            if (ke2.Keycode == Key.F9)
            {
                ShowModLog();
                vp.SetInputAsHandled();
                return;
            }
            // 空格：暂停/继续（任何弹窗打开时锁定）
            if (ke2.Keycode == Key.Space && _pausePanel == null && _saveLoadPanel == null
                && _openPanels.Count == 0 && _openPopup == null && _openDevPopup == null
                && _toastPanel == null && !IsAnyModalOpen)
            {
                Paused = !Paused;
                if (Paused) ShowSimplePause();
                else { CloseSimplePause(); if (_tooltipPanel != null) _tooltipPanel.Visible = false; HideTooltip(); }
                vp.SetInputAsHandled();
                return;
            }
            if (ke2.Keycode == Key.Escape)
            {
                // 逐层关闭：Toast → 存档/设置 → Popup → DevPopup → 暂停 → 模态栈
                if (_toastPanel != null && GodotObject.IsInstanceValid(_toastPanel)) { _toastPanel.QueueFree(); _toastPanel = null; }
                else if (_saveLoadPanel != null) CloseSaveLoad();
                else if (_openPopup != null) { _openPopup.Close(); _openPopup = null; _activeTab = -1; }
                else if (_openDevPopup != null) { _openDevPopup.Close(); _openDevPopup = null; _activeTab = -1; }
                else if (_pausePanel != null) ClosePauseMenu();
                else if (_openPanels.Count > 0) PopTopPanel();
                else TogglePauseMenu();
            }
            // C：打开卡牌抽屉（仅主界面无弹窗时）
            if (ke2.Keycode == Key.C && !IsUIPanelOpen)
            {
                CardUI.ToggleDrawer();
            }
            // F1：打开游戏百科
            if (ke2.Keycode == Key.F1)
            {
                _uiSysEx?.ShowEncyclopedia();
                vp.SetInputAsHandled(); return;
            }
            // F2：打开成就馆
            if (ke2.Keycode == Key.F2)
            {
                _uiSysEx?.ShowAchievementGallery();
                vp.SetInputAsHandled(); return;
            }
        }
    }

    private bool IsUIPanelOpen => _openPanels.Count > 0 || _saveLoadPanel != null || _pausePanel != null || IsAnyModalOpen;
    private int _modalLock;
    private ColorRect _founderOverlay;
    public bool IsAnyModalOpen
    {
        get => _modalLock > 0;
        set
        {
            if (value) { _modalLock++; Paused = true; }
            else { _modalLock = Mathf.Max(0, _modalLock - 1); if (_modalLock == 0 && _openPanels.Count == 0) Paused = false; }
        }
    }

    /// <summary>工具栏调用的速度设置</summary>
    public void SetGameSpeed(int speed)
    {
        _gameSpeed = Mathf.Clamp(speed, 1, 8);
        if (_speedOpt != null) _speedOpt.Selected = _gameSpeed - 1;
    }

    /// <summary>工具栏调用的新建项目</summary>
    public void StartNewProject()
    {
        if (IsUIPanelOpen) return;
        OnTabClick(0);
    }

    // ── WASD 键盘平移相机 ──
    private void HandleCameraKeys(float delta)
    {
        if (_camera == null) return;
        // 输入框获得焦点时不处理摄像头移动
        var focus = GetViewport().GuiGetFocusOwner();
        if (focus is LineEdit || focus is TextEdit) return;

        float s = _camPanSpeed * delta;
        if (Input.IsKeyPressed(Key.W)) { var f = _camera.GlobalBasis.Z; f.Y = 0; _camTarget -= f * s; }
        if (Input.IsKeyPressed(Key.S)) { var f = _camera.GlobalBasis.Z; f.Y = 0; _camTarget += f * s; }
        if (Input.IsKeyPressed(Key.A)) { var r = _camera.GlobalBasis.X; r.Y = 0; _camTarget -= r * s; }
        if (Input.IsKeyPressed(Key.D)) { var r = _camera.GlobalBasis.X; r.Y = 0; _camTarget += r * s; }
    }

    private void UpdateCameraPos()
    {
        if (_camera == null) return;
        float y = Mathf.Sin(_camPitch) * _camDist;
        float xz = Mathf.Cos(_camPitch) * _camDist;
        _camera.Position = _camTarget + new Vector3(Mathf.Sin(_camYaw) * xz, y, Mathf.Cos(_camYaw) * xz);
        _camera.LookAt(_camTarget);
    }

    public void CloseAll()
    {
        // 反向遍历，跳过 Protected 面板
        var toFree = new List<Panel>();
        while (_openPanels.Count > 0)
        {
            var p = _openPanels.Pop();
            if (p is DragPanel dp && dp.Protected) continue;
            toFree.Add(p);
        }
        foreach (var p in toFree) if (GodotObject.IsInstanceValid(p)) p.QueueFree();
        _openPopup?.Close();
        _openPopup = null;
        _openDevPopup?.Close();
        _openDevPopup = null;
        _devScoreUpdater = null;
        _activeTab = -1;
        _modalLock = 0;
        Paused = false;
        HideTooltip();
    }

    public void SetDevScoreUpdater(Action updater) => _devScoreUpdater = updater;

    // ══════════════════ Mod 方法覆写辅助 ══════════════════
    private void DoMonthPhase(ModAPI.MonthlyPhase phase, Action impl)
    {
        string methodName = "monthly_" + phase.ToString();
        try
        {
            ModMethodOverride.CallVoid(methodName, ModMethodOverride.Args(("phase", phase.ToString())), impl);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[MonthPhase] Error in {phase}: {e.Message}\n{e.StackTrace}");
        }
    }

    // ==================== 月度结算 ====================
    private void OnMonthEnd()
    {
        ModAPI.FireHooks(ModAPI.GameHook.BeforeMonthEnd, new() { ["month"] = GameMonth });
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeMonthEnd)) return;

        GameMonth++;

        DoMonthPhase(ModAPI.MonthlyPhase.IPUniverseTick, MonthEndPhase_IPUniverseTick);
        DoMonthPhase(ModAPI.MonthlyPhase.BlackSwanTick, MonthEndPhase_BlackSwanTick);
        DoMonthPhase(ModAPI.MonthlyPhase.AchievementCardCheck, MonthEndPhase_AchievementCardCheck);
        DoMonthPhase(ModAPI.MonthlyPhase.DebtCrashRecovery, MonthEndPhase_DebtCrashRecovery);
        DoMonthPhase(ModAPI.MonthlyPhase.PaySalaries, MonthEndPhase_PaySalaries);
        DoMonthPhase(ModAPI.MonthlyPhase.PayRent, MonthEndPhase_PayRent);
        DoMonthPhase(ModAPI.MonthlyPhase.StoryEventsTick, MonthEndPhase_StoryEventsTick);
        DoMonthPhase(ModAPI.MonthlyPhase.QuarterlyReport, MonthEndPhase_QuarterlyReport);
        DoMonthPhase(ModAPI.MonthlyPhase.IndustryNews, MonthEndPhase_IndustryNews);
        DoMonthPhase(ModAPI.MonthlyPhase.WindInsuranceDecay, MonthEndPhase_WindInsuranceDecay);
        DoMonthPhase(ModAPI.MonthlyPhase.TeamDevelopment, MonthEndPhase_TeamDevelopment);
        DoMonthPhase(ModAPI.MonthlyPhase.TeamChemistry, MonthEndPhase_TeamChemistry);
        DoMonthPhase(ModAPI.MonthlyPhase.EmployeeExpSettle, MonthEndPhase_EmployeeExpSettle);
        DoMonthPhase(ModAPI.MonthlyPhase.MonthlySales, MonthEndPhase_MonthlySales);
        DoMonthPhase(ModAPI.MonthlyPhase.EmployeeFatigueTick, MonthEndPhase_EmployeeFatigueTick);
        DoMonthPhase(ModAPI.MonthlyPhase.TechDebtTick, MonthEndPhase_TechDebtTick);
        DoMonthPhase(ModAPI.MonthlyPhase.MarketTrendTick, MonthEndPhase_MarketTrendTick);
        DoMonthPhase(ModAPI.MonthlyPhase.NewSystemsTick, MonthEndPhase_NewSystemsTick);
        DoMonthPhase(ModAPI.MonthlyPhase.LoanProcessing, MonthEndPhase_LoanProcessing);
        DoMonthPhase(ModAPI.MonthlyPhase.FanUpdate, MonthEndPhase_FanUpdate);
        DoMonthPhase(ModAPI.MonthlyPhase.PlayerTrustDecay, MonthEndPhase_PlayerTrustDecay);
        DoMonthPhase(ModAPI.MonthlyPhase.EmployeePoaching, MonthEndPhase_EmployeePoaching);
        DoMonthPhase(ModAPI.MonthlyPhase.CompetitorTick, MonthEndPhase_CompetitorTick);
        DoMonthPhase(ModAPI.MonthlyPhase.AudienceTick, MonthEndPhase_AudienceTick);
        DoMonthPhase(ModAPI.MonthlyPhase.LiveOpsTick, MonthEndPhase_LiveOpsTick);
        DoMonthPhase(ModAPI.MonthlyPhase.FounderLegacyTick, MonthEndPhase_FounderLegacyTick);
        DoMonthPhase(ModAPI.MonthlyPhase.ResourceMonthEnd, MonthEndPhase_ResourceMonthEnd);
        DoMonthPhase(ModAPI.MonthlyPhase.EngineMaintenance, MonthEndPhase_EngineMaintenance);
        DoMonthPhase(ModAPI.MonthlyPhase.ProfitLogging, MonthEndPhase_ProfitLogging);
        DoMonthPhase(ModAPI.MonthlyPhase.AnnualAwards, MonthEndPhase_AnnualAwards);
        DoMonthPhase(ModAPI.MonthlyPhase.EmployeeSatisfaction, MonthEndPhase_EmployeeSatisfaction);
        DoMonthPhase(ModAPI.MonthlyPhase.OutsourceTick, MonthEndPhase_OutsourceTick);
        DoMonthPhase(ModAPI.MonthlyPhase.PublishingMonthly, MonthEndPhase_PublishingMonthly);
        DoMonthPhase(ModAPI.MonthlyPhase.EngineMonthlyTick, MonthEndPhase_EngineMonthlyTick);
        DoMonthPhase(ModAPI.MonthlyPhase.ContractRefresh, MonthEndPhase_ContractRefresh);
        DoMonthPhase(ModAPI.MonthlyPhase.GameOverCheck, MonthEndPhase_GameOverCheck);
        DoMonthPhase(ModAPI.MonthlyPhase.FanPetition, MonthEndPhase_FanPetition);
        DoMonthPhase(ModAPI.MonthlyPhase.ConsoleLifecycle, MonthEndPhase_ConsoleLifecycle);
        DoMonthPhase(ModAPI.MonthlyPhase.EraMilestone, MonthEndPhase_EraMilestone);
        DoMonthPhase(ModAPI.MonthlyPhase.AchievementCheck, MonthEndPhase_AchievementCheck);
        DoMonthPhase(ModAPI.MonthlyPhase.ServerMonthlyTick, MonthEndPhase_ServerMonthlyTick);
        DoMonthPhase(ModAPI.MonthlyPhase.TutorialTick, MonthEndPhase_TutorialTick);
        DoMonthPhase(ModAPI.MonthlyPhase.VictoryCheck, MonthEndPhase_VictoryCheck);
        DoMonthPhase(ModAPI.MonthlyPhase.AutoSave, MonthEndPhase_AutoSave);

        ModAPI.FireHooks(ModAPI.GameHook.AfterMonthEnd);
    }

    // ══════════════════ 月度阶段实现 ══════════════════
    private void MonthEndPhase_IPUniverseTick()
    {
        IPManager.MonthlyTick();
        if (GameMonth % 12 == 0) IPManager.YearlyTick();
    }

    private void MonthEndPhase_BlackSwanTick()
    {
        if (!ModAPI.IsFeatureEnabled("feature.blackswan")) return;
        BlackSwanManager.MonthlyTick(this);
    }

    private void MonthEndPhase_AchievementCardCheck()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.AchievementSystem)) return;
        if (GameMonth % 3 == 0) AchievementCard.Check(this);
    }

    private void MonthEndPhase_DebtCrashRecovery()
    {
        if (_debtMgr.CrashRecoveryMonths > 0)
        {
            foreach (var team in _teamMgr.Teams)
                if (team.Task != TeamTask.Refactor && team.Task != TeamTask.None)
                    team.Task = TeamTask.Refactor;
        }
    }

    private void MonthEndPhase_PaySalaries()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.EmployeeSystem)) return;
        ModAPI.FireHooks(ModAPI.GameHook.BeforeMonthlySalary);
        if (!ModAPI.IsCancelled(ModAPI.GameHook.BeforeMonthlySalary))
            _empMgr.PaySalaries();
        ModAPI.FireHooks(ModAPI.GameHook.AfterMonthlySalary);
    }

    private void MonthEndPhase_PayRent()
    {
        _roomMgr.PayMonthlyRent();
    }

    private void MonthEndPhase_StoryEventsTick()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.StoryEventSystem)) return;
        bool hasActiveDev = _teamMgr.Teams.Any(t => t.Task != TeamTask.None);
        if (hasActiveDev)
        {
            var storyEvt = GetNode<StoryEvents>("StoryEvents");
            if (new Random().Next(100) < 10)
                storyEvt.PickInternalEvent();
            if (new Random().Next(100) < 3)
                storyEvt.GainInspirationFragment(3 + new Random().Next(3));
            foreach (var team in _teamMgr.Teams)
                if (team.Task == TeamTask.DevelopGame && team.CurrentProject != null && new Random().Next(100) < 8)
                    storyEvt.PickInspirationGamble(team.CurrentProject, team);
            if (new Random().Next(100) < 12)
                storyEvt.PickFanCommunityEvent();
            storyEvt.PickJuicyEvent();
        }
    }

    private void MonthEndPhase_QuarterlyReport()
    {
        if (GameMonth % 3 == 0)
        {
            if (_devMgr.IsListed)
            {
                long qProfit = _devMgr.MonthlyProfitLog.TakeLast(3).Sum(l => (long)(l.revenue - l.expense));
                float oldPrice = _devMgr.SharePrice;
                // PE-based pricing model (matching AI competitors)
                float eps = _devMgr.SharesOutstanding > 0 ? qProfit / (float)_devMgr.SharesOutstanding : 0;
                float repScore = _devMgr.PublisherReputation * 100f + _res.TotalRevenue / 10000000f;
                float industryPE = Mathf.Clamp(8 + repScore * 0.08f + (_devMgr.SharePrice * _devMgr.SharesOutstanding) / 5000000f, 5, 40);
                float growthPremium = _devMgr.ExpectedProfit > 1000 && _devMgr.ExpectedProfit < qProfit ? 1.2f : 1f;
                float sentimentPremium = 0.5f + _competitor.MarketSentiment * 0.5f;
                float targetPrice = Mathf.Max(3f, eps * industryPE * growthPremium * sentimentPremium);
                _devMgr.SharePrice = Mathf.Max(3f, oldPrice + (targetPrice - oldPrice) * 0.3f);
                _devMgr.ExpectedProfit = _devMgr.ExpectedProfit * 0.6f + qProfit * 0.4f;
                _devMgr.PriceHistory.Add((GameMonth, _devMgr.SharePrice));
                if (_devMgr.PriceHistory.Count > 36) _devMgr.PriceHistory.RemoveAt(0);
                // Player company dividend
                if (qProfit > 0 && _devMgr.DividendRate > 0)
                {
                    float divPerShare = qProfit * _devMgr.DividendRate / _devMgr.SharesOutstanding;
                    if (divPerShare > 0.01f)
                    {
                        float totalDiv = divPerShare * _devMgr.SharesOutstanding;
                        _res.SpendMoney((long)totalDiv, "dividend");
                        _devMgr.SharePrice += divPerShare * 3f;
                    }
                }
                if (Mathf.Abs(_devMgr.SharePrice - oldPrice) > 15)
                {
                    string dir = _devMgr.SharePrice > oldPrice ? "📈" : "📉";
                    ShowPopup(Loc.Tr("popup.quarterly_report"), Loc.TrF("popup.quarterly_change", dir, Mathf.Abs(_devMgr.SharePrice - oldPrice), _devMgr.SharePrice, qProfit), new Color(0.6f, 0.8f, 1f));
                }
            }
            foreach (var s in _competitor.Studios)
            {
                if (s.IsListed)
                {
                    s.PriceHistory.Add((GameMonth, s.SharePrice));
                    if (s.PriceHistory.Count > 36) s.PriceHistory.RemoveAt(0);
                }
            }
            ModAPI.FireHooks(ModAPI.GameHook.OnQuarterlyReport);
            GetNode<StoryEvents>("StoryEvents").ShowQuarterlyReview();
        }
    }

    private void MonthEndPhase_IndustryNews()
    {
        GetNode<StoryEvents>("StoryEvents").GenerateIndustryNews();
    }

    private void MonthEndPhase_WindInsuranceDecay()
    {
        for (int i = WindInsurances.Count - 1; i >= 0; i--)
        {
            var w = WindInsurances[i];
            w.MonthsLeft--;
            if (w.MonthsLeft <= 0) WindInsurances.RemoveAt(i);
            else WindInsurances[i] = w;
        }
    }

    private void MonthEndPhase_TeamDevelopment()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.EmployeeSystem)) return;
        foreach (var team in _teamMgr.Teams)
        {
            switch (team.Task)
            {
                case TeamTask.DevelopGame:
                    if (_sprintSys.ShouldPlanSprint(team))
                        ShowSprintPlanning(team);
                    else
                        _devMgr.ProcessMonthlyDev(team);
                    if (team.CurrentProject?.Phase == DevPhase.Polishing) _devMgr.ProcessMonthlyPolish(team);
                    _crisisSys.CheckEvents(team.CurrentProject, team);
                    break;
                case TeamTask.None:
                    // 无任务的团队：检查是否有项目需要打磨推进（无团队时只减BUG不减属性）
                    foreach (var proj in _devMgr.Projects)
                        if (proj.Phase == DevPhase.Polishing) _devMgr.ProcessMonthlyPolish(null);
                    break;
                case TeamTask.ResearchTech:
                    _techMgr.ProcessMonthlyResearch(team);
                    break;
                case TeamTask.Outsource:
                    if (team.CurrentProject != null && team.CurrentProject.Phase == DevPhase.Developing)
                    {
                        var proj = team.CurrentProject;
                        float halfSpeed = _devMgr.CalculateDevSpeed(team, proj) * 0.5f;
                        float prog = halfSpeed / proj.EstimatedMonths;
                        proj.DevProgress = Mathf.Min(1f, proj.DevProgress + prog);
                        float bugRate = _debtMgr.ComputeTotalDebt() > 40 ? 3f : _debtMgr.ComputeTotalDebt() > 20 ? 1.5f : 0.5f;
                        proj.BugCount += (int)(bugRate * 0.5f);
                        if (proj.TechDebt > 0)
                            proj.TechDebt = Mathf.Min(100, proj.TechDebt + proj.TechDebt * 0.02f);
                    }
                    break;
                case TeamTask.Refactor:
                    float reduce = 5 + team.GetTotalSkillLevel(SkillType.Program) * 0.5f;
                    foreach (var eng in Engines) eng.TechDebt = Mathf.Max(0, eng.TechDebt - reduce * 0.3f);
                    break;
            }
        }
    }

    private void MonthEndPhase_TeamChemistry()
    {
        foreach (var team in _teamMgr.Teams)
            if (team.Task != TeamTask.None) team.UpdateChemistry();
    }

    private void MonthEndPhase_EmployeeExpSettle()
    {
        _empMgr.MonthlyExpSettle(_teamMgr.GetActiveTeams(), _teamMgr);
    }

    private void MonthEndPhase_MonthlySales()
    {
        _devMgr.ProcessMonthlySales();
    }

    private void MonthEndPhase_EmployeeFatigueTick()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.EmployeeSystem)) return;
        foreach (var emp in _empMgr.Employees)
            emp.Fatigue += _debtMgr.FatiguePerMonth;
    }

    private void MonthEndPhase_TechDebtTick()
    {
        _debtMgr.MonthlyTick();
    }

    private void MonthEndPhase_MarketTrendTick()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.MarketTrendSystem)) return;
        _trendMgr.MonthlyTick();
    }

    private void MonthEndPhase_NewSystemsTick()
    {
        if (ModAPI.IsFeatureEnabled(ModAPI.Features.EmployeeSystem)) _empSysEx.MonthlyTick();
        _economySys.MonthlyTick();
        _marketSys.ProcessRumors();
        if (ModAPI.IsFeatureEnabled("feature.community")) _communitySys.MonthlyTick();
        _ipSysEx.MonthlyTick();
        _longTermSys.MonthlyTick();
        ModAPI.ProcessMonthlyCallbacks();
        if (GameMonth % 3 == 0) _longTermSys.RecordMuseumSnapshot();
    }

    private void MonthEndPhase_LoanProcessing()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.LoanSystem)) return;
        float loanPayment = Loan.ProcessMonthly();
        if (loanPayment > 0)
        {
            if (_res.SpendMoney(loanPayment, "loan_payment"))
            {
                Loan.OverdueMonths = 0;
            }
            else
            {
                Loan.OverdueMonths++;
                if (Loan.OverdueMonths > 2)
                {
                    var devMgrForLoan = GetNode<GameDevManager>("GameDevManager");
                    if (devMgrForLoan != null) devMgrForLoan.PublisherReputation = Math.Max(0, devMgrForLoan.PublisherReputation - 0.1f);
                    ShowToast(Loc.Tr("loan.overdue"), Loc.Tr("loan.overdue_msg"), new Color(0.9f, 0.3f, 0.3f));
                }
            }
        }
    }

    private void MonthEndPhase_FanUpdate()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.FanSystem)) return;
        _fanMgr.MonthlyUpdate();
    }

    private void MonthEndPhase_PlayerTrustDecay()
    {
        _devMgr.PlayerTrust = Mathf.Max(0, _devMgr.PlayerTrust - 0.5f);
    }

    private void MonthEndPhase_EmployeePoaching()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.EmployeeSystem)) return;
        foreach (var emp in _empMgr.Employees)
        {
            if (emp.ConsideringOffer)
            {
                emp.OfferCountdown--;
                if (emp.OfferCountdown <= 0)
                    ShowEmployeeRetentionDialog(emp);
            }
        }
    }

    private void MonthEndPhase_CompetitorTick()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.CompetitorSystem)) return;
        _competitor.MonthlyUpdate();
    }

    private void MonthEndPhase_AudienceTick()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.AudienceSystem)) return;
        _audienceSys.MonthlyUpdate();
    }

    private void MonthEndPhase_LiveOpsTick()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.LiveOpsSystem)) return;
        _liveOpsSys.MonthlyUpdate();
    }

    private void MonthEndPhase_FounderLegacyTick()
    {
        _legacySys.CheckSuccession();
    }

    private void MonthEndPhase_ResourceMonthEnd()
    {
        _res.MonthEndSettle();
        MarkHUDDirty();
    }

    private void MonthEndPhase_EngineMaintenance()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.EngineSystem)) return;
        foreach (var eng in Engines)
        {
            if (eng.AppliedTechs.Count == 0) continue;
            long maintCost = eng.AppliedTechs.Count * 2000L;
            _res.SpendMoney(maintCost, "engine_maintenance");
        }
    }

    private void MonthEndPhase_ProfitLogging()
    {
        long thisMonthRev = _devMgr.MonthlyRevenueLog.Where(l => l.month == GameMonth).Sum(l => l.revenue);
        long thisMonthExp = (long)_res.MonthlyExpense;
        _devMgr.MonthlyProfitLog.Add((GameMonth, thisMonthRev, thisMonthExp));
        while (_devMgr.MonthlyProfitLog.Count > 36)
            _devMgr.MonthlyProfitLog.RemoveAt(0);
    }

    private void MonthEndPhase_AnnualAwards()
    {
        if (GameMonth % 12 == 0 && _devMgr.CompletedProjects.Count > 0)
        {
            ModAPI.FireHooks(ModAPI.GameHook.BeforeAnnualAwards);
            ShowAnnualAwards();
            ModAPI.FireHooks(ModAPI.GameHook.AfterAnnualAwards);
        }
    }

    private void MonthEndPhase_EmployeeSatisfaction()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.EmployeeSatisfaction)) return;
        ModAPI.FireHooks(ModAPI.GameHook.BeforeEmployeeSatisfaction);
        ProcessEmployeeSatisfaction();
        ModAPI.FireHooks(ModAPI.GameHook.AfterEmployeeSatisfaction);
    }

    private void MonthEndPhase_OutsourceTick()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.OutsourceSystem)) return;
        RefreshOutsourcePool();
        ProcessOutsourceMonthly();
    }

    private void MonthEndPhase_PublishingMonthly()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.PublishingSystem)) return;
        _devMgr.ProcessPublishingMonthly();
    }

    private void MonthEndPhase_EngineMonthlyTick()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.EngineSystem)) return;
        TickEnginesMonthly();
    }

    private void MonthEndPhase_ContractRefresh()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.EmployeeSystem)) return;
        if (GameMonth % 6 == 0) _teamMgr.RefreshContracts();
    }

    private void MonthEndPhase_GameOverCheck()
    {
        if (_res.Money <= 0 && _res.TotalRevenue <= 0) ShowGameOver();
        else if (_res.Money < 40000 && GameMonth > 3 && new Random().Next(100) < 25)
            OfferEmergencyContract();
        _openPopup?.OnMonthChanged();
    }

    private void MonthEndPhase_FanPetition()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.FanSystem)) return;
        TryFanPetition();
    }

    private void MonthEndPhase_ConsoleLifecycle()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.ConsoleLifecycle)) return;
        ModAPI.FireHooks(ModAPI.GameHook.BeforeConsoleLifecycle);
        CheckConsoleLifecycle();
        ModAPI.FireHooks(ModAPI.GameHook.AfterConsoleLifecycle);
    }

    private void MonthEndPhase_EraMilestone()
    {
        TryEraMilestone();
    }

    private void MonthEndPhase_AchievementCheck()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.AchievementSystem)) return;
        _achMgr.MonthlyCheck();
    }

    private void MonthEndPhase_ServerMonthlyTick()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.ServerSystem)) return;
        _serverMgr.MonthlyTick();
    }

    private void MonthEndPhase_TutorialTick()
    {
        if (!ModAPI.IsFeatureEnabled(ModAPI.Features.TutorialSystem)) return;
        _tutorialMgr.MonthlyTick();
    }

    private void MonthEndPhase_VictoryCheck()
    {
        _victoryMgr?.MonthlyCheck();
    }

    private void MonthEndPhase_AutoSave()
    {
        if (GlobalSettings.AutoSaveEnabled && GlobalSettings.AutoSaveIntervalMonths > 0)
        {
            if (GameMonth % GlobalSettings.AutoSaveIntervalMonths == 0)
                SaveGame(GlobalSettings.GetAutoSavePath());
        }
    }

    private void RefreshOutsourcePool()
    {
        var rng = new Random();
        OutsourceTaskPool.RemoveAll(t => t.Remaining <= 0);
        // 每月最多刷 2 个新任务
        int count = rng.Next(1, 3);
        for (int i = 0; i < count; i++)
        {
            int taskType = rng.Next(6); // 0-1:常规/ 2:灵感型/ 3:科技加速型/ 4:高风险高回报/ 5:声誉型
            bool highRisk = taskType == 4;
            bool inspirationType = taskType == 2;
            bool techBoost = taskType == 3;
            bool repTask = taskType == 5;

            OutsourceTaskPool.Add(new OutsourceTask
            {
                Name = Loc.TrF("outsource.generic_name", rng.Next(1000, 9999)),
                Description = inspirationType ? Loc.Tr("outsource.creative") : techBoost ? Loc.Tr("outsource.engine_work") : highRisk ? Loc.Tr("outsource.high_risk") : repTask ? Loc.Tr("outsource.prestige") : (rng.Next(2) == 0 ? Loc.Tr("outsource.quality_test") : Loc.Tr("outsource.func_dev")),
                Reward = inspirationType ? 8000 + rng.Next(20000) : techBoost ? 15000 + rng.Next(30000) : highRisk ? 50000 + rng.Next(150000) : repTask ? 10000 + rng.Next(30000) : 20000 + rng.Next(80000),
                Duration = techBoost ? 1 + rng.Next(2) : inspirationType ? 2 + rng.Next(3) : 1 + rng.Next(4),
                Remaining = techBoost ? 1 + rng.Next(2) : inspirationType ? 2 + rng.Next(3) : 1 + rng.Next(4),
                RequiredSkill = (SkillType)rng.Next(5),
                RequiredLevel = highRisk ? 3 + rng.Next(2) : 1 + rng.Next(4),
                Difficulty = highRisk ? 0.5f + (float)rng.NextDouble() * 0.4f : 0.3f + (float)rng.NextDouble() * 0.4f,
                IsHighRisk = highRisk,
                GivesInspiration = inspirationType,
                GivesTechBoost = techBoost,
                GivesReputation = repTask
            });
        }
    }

    private void ProcessOutsourceMonthly()
    {
        var rng = new Random();
        foreach (var task in OutsourceTaskPool)
        {
            if (!task.Accepted || task.AssignedTeam == null) continue;
            task.MonthsSpent++;
            if (task.MonthsSpent >= task.Remaining)
            {
                float success = 0.5f + task.AssignedTeam.GetTotalSkillLevel(task.RequiredSkill) * 0.1f - task.Difficulty * 0.3f;
                success = Mathf.Clamp(success, 0.2f, 0.95f);
                bool won = rng.NextDouble() < success;

                if (won)
                {
                    if (task.GivesInspiration) { _res.GainInspiration(5 + rng.Next(11)); _res.EarnMoney(task.Reward, "outsource"); }
                    else if (task.GivesTechBoost) { _res.EarnMoney(task.Reward, "outsource"); _res.GainInspiration(3); }
                    else if (task.GivesReputation) { _res.EarnMoney(task.Reward, "outsource"); _res.GainInspiration(2); }
                    else { _res.EarnMoney(task.Reward, "outsource"); _res.GainInspiration(3); }
                }
                else
                {
                    if (task.IsHighRisk) { /* 声誉下降 */ }
                    else { /* 无报酬 */ }
                }
                task.AssignedTeam.Task = TeamTask.None;
                task.AssignedTeam.CurrentContract = null;
                task.AssignedTeam = null;
                task.Remaining = 0;
            }
        }
        OutsourceTaskPool.RemoveAll(t => t.Remaining <= 0);
    }

    // ══════════════════ 系统碰撞：员工满意度 + 债务连锁效应 ══════════════════
    // ══════════════════ 死亡螺旋安全网 ══════════════════
    /// <summary>资金紧张时提供紧急外包合同</summary>
    private void OfferEmergencyContract()
    {
        if (GameMonth - _lastEmergencyOfferMonth < 6) return; // 6个月冷却
        _lastEmergencyOfferMonth = GameMonth;

        float bailout = 50000 + GameMonth * 1000;
        ShowChoicePopup(Loc.Tr("popup.emergency"),
            Loc.TrF("popup.emergency_msg", _res.Money, bailout),
            Loc.Tr("popup.accept_out"), Loc.Tr("popup.reject"),
            () =>
            {
                _res.EarnMoney(bailout, "emergency_contract");
                ShowPopup(Loc.Tr("popup.emergency_done"), Loc.TrF("popup.emergency_done_msg", bailout), new Color(0.9f, 0.6f, 0.2f));
            },
            () => { /* 拒绝，生死自负 */ },
            new Color(0.9f, 0.3f, 0.1f));
    }

    // ══════════════════ 粉丝请愿 ══════════════════
    private GameGenre? _petitionGenre;
    private GameTheme? _petitionTheme;
    private int _petitionExpireMonth;

    private void TryFanPetition()
    {
        var fanMgr = GetNode<FanManager>("FanManager");
        if (fanMgr.DiehardFans < 30) return;
        if (_petitionGenre != null && GameMonth < _petitionExpireMonth) return; // 上一份请愿还在有效期内
        if (new Random().Next(100) > 8) return; // 每月8%

        var genres = Enum.GetValues<GameGenre>();
        var themes = Enum.GetValues<GameTheme>();
        var rng = new Random();
        var g = genres[rng.Next(genres.Length)];
        var t = themes[rng.Next(themes.Length)];
        _petitionGenre = g; _petitionTheme = t; _petitionExpireMonth = GameMonth + 12;

        ShowChoicePopup(Loc.Tr("popup.fan_petition"),
            Loc.TrF("popup.fan_petition_msg", g.Name(), t.Name()),
            Loc.Tr("popup.fan_try"), Loc.Tr("popup.fan_ignore"),
            () => { }, () => { _petitionGenre = null; _petitionTheme = null; },
            new Color(0.3f, 0.6f, 0.9f));
    }

    /// <summary>检查发售时是否满足粉丝请愿</summary>
    public void CheckFanPetitionReward(GameProject proj)
    {
        if (_petitionGenre == null || _petitionTheme == null) return;
        if (proj.Genre == _petitionGenre.Value && proj.Theme == _petitionTheme.Value && GameMonth <= _petitionExpireMonth)
        {
            proj.Sales = (int)(proj.Sales * 1.4f);
            var fanMgr = GetNode<FanManager>("FanManager");
            fanMgr.DiehardFans += (int)(fanMgr.DiehardFans * 0.2f);
            ShowPopup(Loc.Tr("popup.fan_done"), Loc.Tr("popup.fan_done_msg"), new Color(0.3f, 0.7f, 1f));
            _petitionGenre = null; _petitionTheme = null;
        }
    }

    // ══════════════════ 时代里程碑 ══════════════════
    private readonly HashSet<int> _triggeredEras = new();
    private int _lastEmergencyOfferMonth; // 紧急外包冷却

    private void TryEraMilestone()
    {
        int year = GameYear;
        if ((year == 5 || year == 10 || year == 15 || year == 20 || year == 25) && !_triggeredEras.Contains(year))
        {
            _triggeredEras.Add(year);
            string desc = year switch
            {
                5 => Loc.Tr("popup.era_desc_5"),
                10 => Loc.Tr("popup.era_desc_10"),
                15 => Loc.Tr("popup.era_desc_15"),
                20 => Loc.Tr("popup.era_desc_20"),
                25 => CompletedProjects().Count >= 20 ? Loc.Tr("popup.era_desc_25_legacy") : Loc.Tr("popup.era_desc_25_ongoing"),
                _ => Loc.Tr("popup.era_default")
            };
            string extra = "";
            var fanMgr = GetNodeOrNull<FanManager>("FanManager");
            var resMgr = GetNodeOrNull<ResourceManager>("ResourceManager");
            if (year == 5 && fanMgr != null) { fanMgr.CasualFans += 1000; extra = "\n📈 粉丝 +1000"; }
            if (year == 10 && fanMgr != null) { fanMgr.DiehardFans += 200; fanMgr.CasualFans += 5000; extra = "\n📈 核心粉丝 +200，普通粉丝 +5000"; }
            if (year == 15 && fanMgr != null) { fanMgr.CasualFans += 20000; extra = "\n📈 普通粉丝 +20000"; }
            if (year == 20 && resMgr != null) { resMgr.EarnMoney(500000, "era_milestone"); extra = "\n💰 里程碑奖金 ¥500,000"; }
            if (year == 25 && resMgr != null) { resMgr.EarnMoney(1000000, "era_milestone"); extra = "\n💰 里程碑奖金 ¥1,000,000"; }
            ShowPopup(Loc.TrF("popup.era_title", year),
                $"{desc}{extra}\n\n{Loc.Tr("popup.era_hint")}", year switch { 5 => new Color(0.3f, 0.7f, 0.5f), 10 => new Color(0.7f, 0.5f, 0.3f), 15 => new Color(0.3f, 0.5f, 0.9f), 20 => new Color(0.6f, 0.3f, 0.8f), _ => new Color(0.6f, 0.6f, 0.2f) });
        }
    }

    /// <summary>时代的科技研发加速</summary>
    public float EraResearchSpeedMul => GameYear >= 20 ? 2f : GameYear >= 15 ? 1.5f : GameYear >= 10 ? 1.5f : 1f;
    private List<GameProject> CompletedProjects() => GetNode<GameDevManager>("GameDevManager").CompletedProjects;

    private void ProcessEmployeeSatisfaction()
    {
        var rng = new Random();
        float totalDebt = _debtMgr.ComputeTotalDebt();
        var storyEvt = GetNode<StoryEvents>("StoryEvents");

        foreach (var emp in _empMgr.Employees)
        {
            if (emp.Name == Loc.Tr("person.founder_name")) continue; // 创始人不受影响

            // 1. 满意度变化
            float satDelta = -emp.GetSatisfactionDecay(); // 自然衰减

            // 好友在身边+满意度
            foreach (var friendId in emp.Friends)
            {
                var friend = _empMgr.Employees.Find(e => e.Id == friendId);
                if (friend != null && _teamMgr.Teams.Any(t => t.Members.Contains(emp) && t.Members.Contains(friend)))
                    satDelta += 1.5f;
            }
            // 死对头在同一团队-满意度
            foreach (var rivalId in emp.Rivals)
            {
                var rival = _empMgr.Employees.Find(e => e.Id == rivalId);
                if (rival != null && _teamMgr.Teams.Any(t => t.Members.Contains(emp) && t.Members.Contains(rival)))
                    satDelta -= 2.5f;
            }

            // 高债务→全公司满意度下降
            if (totalDebt > 50) satDelta -= 1.5f;
            if (totalDebt > 80) satDelta -= 3.0f;

            // 高疲劳→不满
            if (emp.Fatigue > 70) satDelta -= 2.0f;
            if (emp.Fatigue > 90) satDelta -= 3.0f;

            // 太久没涨薪
            if (emp.CompanyYears > 1 && emp.Salary < 15000 + emp.CompanyYears * 5000)
                satDelta -= 1.5f;

            // 休假恢复满意度
            if (emp.OnVacation) satDelta += 8.0f;

            emp.Satisfaction = Mathf.Clamp(emp.Satisfaction + satDelta, 0, 100);

            // 2. 低满意度→离职风险（怀恨者+被嫉恨者均保护，给恩怨事件链留触发时间）
            if (emp.Satisfaction < 20 && rng.Next(100) < 15 && !emp.HoldingGrudge && !emp.IsGrudgeTarget)
            {
                _empMgr.FireEmployee(emp);
                _roomMgr?.RefreshEmployees();
                ShowPopup(Loc.Tr("popup.emp_leave"), Loc.TrF("popup.emp_leave_msg", Loc.DisplayName(emp.Name), emp.GetTraitsDesc(), emp.Satisfaction), new Color(0.9f, 0.3f, 0.3f));
                continue;
            }

            // 3. 高债务→额外离职（怀恨者+被嫉恨者同样受保护）
            if (totalDebt > 85 && rng.Next(100) < 5 && !emp.HoldingGrudge && !emp.IsGrudgeTarget)
            {
                _empMgr.FireEmployee(emp);
                _roomMgr?.RefreshEmployees();
                ShowPopup(Loc.Tr("popup.overwhelm"), Loc.TrF("popup.overwhelm_msg", Loc.DisplayName(emp.Name), totalDebt), new Color(0.9f, 0.4f, 0.1f));
                continue;
            }

            // 4. 倦怠→负灵感
            if (emp.Fatigue > 85 && rng.Next(100) < 20)
            {
                _res.Inspiration = Mathf.Max(0, _res.Inspiration - 3);
                ShowPopup(Loc.Tr("popup.burnout"), Loc.TrF("popup.burnout_msg", emp.Name, emp.GetTraitsDesc()), new Color(0.6f, 0.3f, 0.6f));
            }

            // 5. 随机人格事件（降低频率）
            if (rng.Next(100) < 4)
                storyEvt.PickPersonalityEvent(emp);

            // 6. 病假
            if (rng.Next(100) < 6 && emp.Fatigue > 40)
            {
                emp.SickDays = 1 + rng.Next(3);
                ShowPopup(Loc.Tr("popup.sick"), Loc.TrF("popup.sick_msg", Loc.DisplayName(emp.Name), emp.GetTraitsDesc(), emp.SickDays, emp.Fatigue), new Color(0.5f, 0.6f, 0.7f));
            }
        }

        // 7. 员工关系演化
        float avgSat = _empMgr.Employees
            .Where(e => e.Name != Loc.Tr("person.founder_name"))
            .Select(e => e.Satisfaction)
            .DefaultIfEmpty(70).Average();
        bool allFatigued = _empMgr.Employees
            .Where(e => e.Name != Loc.Tr("person.founder_name"))
            .All(e => e.Fatigue > 80);
        bool lowMorale = avgSat < 25 || (avgSat < 35 && allFatigued);

        if (lowMorale && _res.Money >= 25000)
        {
            // 自动团建安全网：全员强制带薪休假一周
            int relieved = 0;
            foreach (var t in _teamMgr.Teams)
            {
                if (_res.TeamBuilding(t)) relieved++;
            }
            if (relieved > 0)
            {
                string msg = Loc.TrF("popup.auto_teambuilding_msg", avgSat, relieved);
                ShowPopup(Loc.Tr("popup.auto_teambuilding"), msg, new Color(0.3f, 0.75f, 0.4f));
            }
        }

        if (!lowMorale && _empMgr.Employees.Count >= 2 && rng.Next(100) < 15)
        {
            var a = _empMgr.Employees[rng.Next(_empMgr.Employees.Count)];
            var b = _empMgr.Employees[rng.Next(_empMgr.Employees.Count)];
            if (a.Id != b.Id && !a.Friends.Contains(b.Id) && !a.Rivals.Contains(b.Id))
            {
                if (rng.Next(2) == 0)
                {
                    a.Friends.Add(b.Id);
                    b.Friends.Add(a.Id);
                    ShowPopup(Loc.Tr("popup.friend"), Loc.TrF("popup.friend_msg", a.Name, b.Name), new Color(0.3f, 0.8f, 0.4f));
                }
                else
                {
                    a.Rivals.Add(b.Id);
                    b.Rivals.Add(a.Id);
                    ShowPopup(Loc.Tr("popup.conflict_title"), Loc.TrF("popup.conflict_msg", a.Name, b.Name), new Color(0.9f, 0.5f, 0.2f));
                }
            }
        }
    }

    private void TickEnginesMonthly()
    {
        foreach (var engine in Engines)
        {
            // 引擎开发进度
            if (engine.IsDeveloping && engine.DevTeam != null)
            {
                engine.DevMonthsRemaining--;
                if (engine.DevMonthsRemaining <= 0)
                {
                    engine.IsDeveloping = false;
                    engine.DevTeam.Task = TeamTask.None;
                    engine.DevTeam = null;
                    engine.Generation++;
                    engine.Reputation += 5;
                    ShowPopup("🔧 " + Loc.Tr("ui.tech_complete"), Loc.TrF("ui.tech_complete_detail", engine.Name, engine.Generation), new Color(0.3f, 0.8f, 1f));
                }
                continue;
            }

            // 授权收入
            float revenue = 0;
            switch (engine.BizModel)
            {
                case EngineBizModel.Subscription:
                    revenue = engine.LicenseCount * engine.SubscriptionPrice;
                    break;
                case EngineBizModel.Royalty:
                    revenue = engine.LicenseCount * 8000; // 模拟平均分成
                    break;
                case EngineBizModel.OpenSource:
                    engine.Reputation += 1; // 声誉自然增长
                    break;
            }
            if (engine.BizModel == EngineBizModel.Buyout)
            {
                // 买断：每月可能新增买家
                float attract = engine.QualityScore * 0.01f + engine.Reputation * 0.005f;
                if (new Random().NextDouble() < attract)
                {
                    engine.LicenseCount++;
                    _res.EarnMoney(engine.BuyoutPrice, "engine");
                    engine.MonthlyRevenue = engine.BuyoutPrice;
                    engine.TotalRevenue += engine.BuyoutPrice;
                    engine.MarketShare = Mathf.Min(1, engine.MarketShare + 0.01f);
                }
            }
            else if (revenue > 0)
            {
                _res.EarnMoney(revenue, "engine");
                engine.MonthlyRevenue = revenue;
                engine.TotalRevenue += revenue;
                // 市场占有率自然浮动
                engine.MarketShare = Mathf.Clamp(engine.QualityScore / 100f + engine.Reputation * 0.001f, 0, 0.8f);
            }
        }
    }

    private void ShowGameOver()
    {
        var vp = GetViewport().GetVisibleRect().Size;
        var p = new Panel { Position = new(vp.X / 2 - 200, vp.Y / 2 - 80), Size = new(400, 160) };
        p.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.92f, 0.92f, 0.95f) });
        var l = new Label { Text = Loc.Tr("ui.game_over"), Position = new(20, 20), Size = new(360, 60), HorizontalAlignment = HorizontalAlignment.Center };
        l.AddThemeFontSizeOverride("font_size", 16); l.AddThemeColorOverride("font_color", new Color(0.7f, 0.2f, 0.2f));
        p.AddChild(l);
        var b = new Button { Text = Loc.Tr("ui.back_menu"), Position = new(100, 100), Size = new(200, 40) };
        b.AddThemeFontSizeOverride("font_size", 14);
        b.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/menu.tscn");
        p.AddChild(b);
        AddChild(p); Paused = true;
    }

    /// <summary>通用事件弹窗</summary>
    public void ShowEventPopup(string title, string message)
    {
        var vp = GetViewport().GetVisibleRect().Size;
        var p = new DragPanel { Position = new(vp.X / 2 - 260, vp.Y / 2 - 140), Size = new(520, 280) };
        p.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.97f, 0.96f, 0.94f, 0.97f),
            BorderWidthLeft = 2, BorderWidthTop = 2,
            BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.8f, 0.3f, 0.3f, 0.8f),
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10
        });

        var tl = new Label { Text = title, Position = new(20, 15), Size = new(480, 30) };
        tl.AddThemeFontSizeOverride("font_size", 22);
        tl.AddThemeColorOverride("font_color", new Color(1, 0.5f, 0.3f));
        p.AddChild(tl);

        var ml = new Label { Text = message, Position = new(20, 55), Size = new(480, 155) };
        ml.AddThemeFontSizeOverride("font_size", 14);
        ml.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 1f));
        ml.AutowrapMode = TextServer.AutowrapMode.Word;
        p.AddChild(ml);

        var ok = new Button { Text = Loc.Tr("ui.got_it"), Position = new(160, 225), Size = new(200, 38) };
        ok.AddThemeFontSizeOverride("font_size", 16);
        ok.Pressed += () => { p.QueueFree(); Paused = false; };
        p.AddChild(ok);

        AddChild(p);
        Paused = true;
    }

    // ==================== HUD ====================
    /// <summary>
    /// 给 VBoxContainer 列表追加汇总行。
    /// 放在列表所有数据行添加完毕后调用。
    /// </summary>
    private void AddListSummary(VBoxContainer list, string[] labels, float[] widths, Color? bg = null)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = bg ?? new Color(0.92f, 0.90f, 0.86f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        var hb = new HBoxContainer();
        hb.AddThemeConstantOverride("separation", 4);
        for (int i = 0; i < labels.Length; i++)
        {
            var l = new Label { Text = labels[i], CustomMinimumSize = new(widths[i], UIScale * 28) };
            l.AddThemeFontSizeOverride("font_size", 11);
            l.AddThemeColorOverride("font_color", new Color(0.08f, 0.12f, 0.2f));
            hb.AddChild(l);
        }
        panel.AddChild(hb);
        list.AddChild(panel);
    }

    private void AddHoverTip(Label label, string tip)
    {
        label.MouseFilter = Control.MouseFilterEnum.Stop;
        label.MouseEntered += () =>
        {
            if (_tooltipPanel == null)
            {
                _tooltipPanel = new Panel();
                _tooltipPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f, 0.94f), BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.4f, 0.5f, 0.7f, 0.6f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
                _tooltipLabel = new Label { Position = new(6, 4) };
                _tooltipLabel.AddThemeFontSizeOverride("font_size", 11);
                _tooltipLabel.AddThemeColorOverride("font_color", new Color(0f, 0f, 0f));
                _tooltipPanel.AddChild(_tooltipLabel);
                _uiLayer.AddChild(_tooltipPanel);
            }
            _tooltipLabel.Text = tip;
            var size = new Vector2(_tooltipLabel.GetMinimumSize().X + 14, _tooltipLabel.GetMinimumSize().Y + 10);
            _tooltipPanel.Size = size;
            var mouse = GetViewport().GetMousePosition();
            var vpSz = GetViewport().GetVisibleRect().Size;
            float tx = mouse.X + 20, ty = mouse.Y + 24;
            if (tx + size.X > vpSz.X) tx = mouse.X - size.X - 8;
            if (ty + size.Y > vpSz.Y) ty = mouse.Y - size.Y - 8;
            _tooltipPanel.Position = new Vector2(tx, ty);
            _tooltipPanel.Visible = true;
        };
        label.MouseExited += HideTooltip;
    }

    private void HideTooltip() { if (_tooltipPanel != null) _tooltipPanel.Visible = false; }

    private void BuildHUD()
    {
        var vp = GetViewport().GetVisibleRect().Size;
        float S(float v) => v * UIScale;

        // 顶部状态栏背景
        var topBar = new Panel { Position = new(0, 0), Size = new(vp.X, S(38)), SelfModulate = new Color(0.97f, 0.96f, 0.94f, 0.95f) };
        _uiLayer.AddChild(topBar);
        float rowY = S(6);

        // 资金
        var moneyIcon = new Label { Text = "¥", Position = new(S(10), rowY), Size = new(S(24), S(26)), MouseFilter = Control.MouseFilterEnum.Ignore };
        moneyIcon.AddThemeFontSizeOverride("font_size", 18); moneyIcon.AddThemeColorOverride("font_color", new Color(0.2f, 0.9f, 0.3f));
        _uiLayer.AddChild(moneyIcon);
        _moneyLabel = new Label { Text = "500,000", Position = new(S(36), rowY + S(2)), Size = new(S(100), S(24)) };
        _moneyLabel.AddThemeFontSizeOverride("font_size", 16); _moneyLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.9f, 0.3f));
        _uiLayer.AddChild(_moneyLabel);
        AddHoverTip(_moneyLabel, Loc.Tr("tip.money"));

        // 灵感
        var inspIcon = new Label { Text = "💡", Position = new(S(150), rowY), Size = new(S(24), S(26)), MouseFilter = Control.MouseFilterEnum.Ignore };
        inspIcon.AddThemeFontSizeOverride("font_size", 18);
        _uiLayer.AddChild(inspIcon);
        _inspirationLabel = new Label { Text = "30", Position = new(S(176), rowY + S(2)), Size = new(S(60), S(24)) };
        _inspirationLabel.AddThemeFontSizeOverride("font_size", 16); _inspirationLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.2f));
        _uiLayer.AddChild(_inspirationLabel);
        AddHoverTip(_inspirationLabel, Loc.Tr("tip.inspiration"));

        // 粉丝数（债务标签已移除——现在是每个项目独立计算）

        // 粉丝
        _fanLabel = new Label { Text = Loc.TrF("fan.label", 520, 100), Position = new(S(320), rowY + S(2)), Size = new(S(160), S(24)) };
        _fanLabel.AddThemeFontSizeOverride("font_size", 14); _fanLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.5f, 0.7f));
        _uiLayer.AddChild(_fanLabel);
        AddHoverTip(_fanLabel, Loc.Tr("fan.hover_tip"));

        // 债务警示灯
        _debtWarnLabel = new Label { Text = "", Position = new(S(485), rowY + S(4)), Size = new(S(20), S(20)) };
        _debtWarnLabel.AddThemeFontSizeOverride("font_size", 14);
        _uiLayer.AddChild(_debtWarnLabel);
        AddHoverTip(_debtWarnLabel, Loc.Tr("tip.debt"));

        // ── 顶部栏右侧：从右往左排列 ──
        float rx = vp.X;
        // 暂停按钮（最右）
        _pauseBtn = new Button { Text = "⏸", Position = new(rx - S(36), S(4)), Size = new(S(28), S(28)), Flat = true };
        _pauseBtn.AddThemeFontSizeOverride("font_size", 14);
        _pauseBtn.Pressed += () => { Paused = !Paused; if (Paused) ShowSimplePause(); else CloseSimplePause(); };
        _uiLayer.AddChild(_pauseBtn); rx -= S(36) + S(6);

        // 速度选择器
        _speedOpt = new OptionButton { Position = new(rx - S(44), S(6)), Size = new(S(44), S(24)) };
        for (int i = 1; i <= 8; i++) _speedOpt.AddItem($"{i}x");
        _speedOpt.Selected = 0;
        _speedOpt.AddThemeFontSizeOverride("font_size", 11);
        _speedOpt.ItemSelected += (long i) => { _gameSpeed = (int)i + 1; };
        _uiLayer.AddChild(_speedOpt); rx -= S(44) + S(4);

        // 信任度
        _trustLabel = new Label { Text = "", Position = new(rx - S(70), rowY + S(2)), Size = new(S(70), S(24)), HorizontalAlignment = HorizontalAlignment.Right };
        _trustLabel.AddThemeFontSizeOverride("font_size", 12);
        _trustLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.8f, 1.0f));
        _uiLayer.AddChild(_trustLabel);
        AddHoverTip(_trustLabel, Loc.Tr("tip.trust")); rx -= S(70) + S(4);

        // 最佳得分
        _bestScoreLabel = new Label { Text = "", Position = new(rx - S(60), rowY + S(2)), Size = new(S(60), S(24)), HorizontalAlignment = HorizontalAlignment.Right };
        _bestScoreLabel.AddThemeFontSizeOverride("font_size", 12); _bestScoreLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.3f));
        _uiLayer.AddChild(_bestScoreLabel);
        AddHoverTip(_bestScoreLabel, Loc.Tr("tip.best_score")); rx -= S(60) + S(4);

        // 员工
        _empLabel = new Label { Text = Loc.TrF("emp.topbar", 0, 2), Position = new(rx - S(90), rowY + S(2)), Size = new(S(90), S(24)), HorizontalAlignment = HorizontalAlignment.Right, MouseFilter = Control.MouseFilterEnum.Ignore };
        _empLabel.AddThemeFontSizeOverride("font_size", 13); _empLabel.AddThemeColorOverride("font_color", new Color(0.65f, 0.82f, 0.95f));
        _uiLayer.AddChild(_empLabel);
        AddHoverTip(_empLabel, Loc.Tr("emp.hover_tip")); rx -= S(90) + S(4);

        // 日期
        _dateLabel = new Label { Text = Loc.Tr("date.initial_date"), Position = new(rx - S(120), rowY + S(2)), Size = new(S(120), S(24)), HorizontalAlignment = HorizontalAlignment.Right };
        _dateLabel.AddThemeFontSizeOverride("font_size", 13); _dateLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.7f, 0.9f));
        _uiLayer.AddChild(_dateLabel);

        // 经济周期指示器
        var ecoLabel = new Label { Text = "", Position = new(rx - S(170), rowY + S(2)), Size = new(S(45), S(24)), HorizontalAlignment = HorizontalAlignment.Right };
        ecoLabel.AddThemeFontSizeOverride("font_size", 11); ecoLabel.Name = "EcoPhaseLabel";
        _uiLayer.AddChild(ecoLabel);
        AddHoverTip(ecoLabel, Loc.Tr("tip.economy_phase"));

        // ── 预测面板：位于顶部栏下方，显示当前项目预估 ──
        var predPanel = new Panel { Position = new(0, S(45)), Size = new(vp.X, S(24)) };
        predPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f, 0.85f), CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        _predictionLabel = new Label { Text = "", Position = new(S(10), S(2)), Size = new(vp.X - S(20), S(20)), HorizontalAlignment = HorizontalAlignment.Center };
        _predictionLabel.AddThemeFontSizeOverride("font_size", 12);
        _predictionLabel.AddThemeColorOverride("font_color", new Color(0.15f, 0.18f, 0.25f));
        predPanel.AddChild(_predictionLabel);
        predPanel.MouseFilter = Control.MouseFilterEnum.Stop;
        predPanel.GuiInput += (evt) =>
        {
            if (evt is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
            {
                var active = _devMgr.Projects.FindAll(p => !p.IsReleased);
                var proj = active.Count > 0 ? active[0] : null;
                if (proj != null)
                {
                    var popup = new GameDevPopup(this);
                    _openDevPopup = popup;
                    popup.ShowProjectStatus(proj);
                }
            }
        };
        predPanel.Visible = false;
        _uiLayer.AddChild(predPanel);

        // ── 左上角营收柱状图 ──
        BuildRevenueChart(vp, S);

        // ── 新闻滚动条 ──
        BuildNewsTicker(vp, S);

        BuildBottomNav(vp, S);
    }

    private Control _revList;
    private int _lastRevKey;
    private Label _revHoverLabel;
    private Label _revHeaderLabel;
    private bool _revForceRedraw;
    private static readonly string[] _monthShortKeys =
        { "date.m1", "date.m2", "date.m3", "date.m4", "date.m5", "date.m6",
          "date.m7", "date.m8", "date.m9", "date.m10", "date.m11", "date.m12" };

    private void BuildRevenueChart(Vector2 vp, Func<float, float> S)
    {
        var chartPanel = new Panel { Position = new(S(4), S(52)), Size = new(S(280), S(200)) };
        chartPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.97f, 0.96f, 0.94f, 0.95f),
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
        });
        chartPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        _uiLayer.AddChild(chartPanel);

        _revHeaderLabel = new Label { Text = Loc.Tr("fin.monthly_profit"), Position = new(S(4), S(1)), Size = new(S(272), S(16)) };
        _revHeaderLabel.AddThemeFontSizeOverride("font_size", 11);
        _revHeaderLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.5f, 0.2f));
        _revHeaderLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        chartPanel.AddChild(_revHeaderLabel);

        _revList = new Control { Position = new(S(2), S(19)), Size = new(S(276), S(179)), MouseFilter = Control.MouseFilterEnum.Ignore };
        chartPanel.AddChild(_revList);

        _revHoverLabel = new Label { Visible = false, Position = new(S(0), S(0)), Size = new(S(180), S(18)) };
        _revHoverLabel.AddThemeFontSizeOverride("font_size", 11);
        _revHoverLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.2f, 0.15f));
        _revHoverLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.9f, 0.9f, 0.85f, 0.9f), CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3 });
        _uiLayer.AddChild(_revHoverLabel);
    }

    private void UpdateRevenueChart()
    {
        if (_revList == null) return;
        // 标题始终跟随语言刷新
        if (_revHeaderLabel != null) _revHeaderLabel.Text = Loc.Tr("fin.monthly_profit");
        var log = GetNode<GameDevManager>("GameDevManager").MonthlyProfitLog;
        if (log.Count == 0) return;

        int count = log.Count;
        int key = (log.Last().month << 16) ^ (int)(log.Last().revenue + log.Last().expense * 31);
        if (key == _lastRevKey && !_revForceRedraw) return;
        _lastRevKey = key;
        _revForceRedraw = false;

        // 清除旧元素
        for (int i = _revList.GetChildCount() - 1; i >= 0; i--)
            _revList.GetChild(i).QueueFree();

        float chartW = 272f;
        float chartH = 179f;
        float topPad = 18f;
        float botPad = 16f;
        float labelZone = 52f;
        float plotLeft = labelZone;
        float plotW = chartW - labelZone - 2f;
        float availableH = chartH - topPad - botPad;
        float zeroY = topPad + availableH * 0.5f;
        float maxBarH = availableH * 0.5f - 2f;

        float colW = 14f;
        float gap = 4f;
        int maxBars = (int)(plotW / (colW + gap));
        int start = Math.Max(0, count - maxBars);
        int visible = count - start;

        // 比例尺
        long maxAbs = 5000;
        for (int i = start; i < count; i++)
        {
            long p = log[i].revenue - log[i].expense;
            long ap = p > 0 ? p : -p;
            if (ap > maxAbs) maxAbs = ap;
        }

        // 零线
        var zeroLine = new ColorRect { Color = new Color(0.4f, 0.4f, 0.45f, 0.6f), Position = new(plotLeft, zeroY), Size = new(plotW, 1) };
        _revList.AddChild(zeroLine);

        // 刻度 — 所有标签左对齐到同一X基准线
        for (int i = 0; i <= 2; i++)
        {
            float h = maxBarH * i / 2f;
            // 上方刻度线
            var tickUp = new ColorRect { Color = new Color(0.25f, 0.25f, 0.3f, 0.3f), Position = new(0, zeroY - h), Size = new(chartW, 1) };
            _revList.AddChild(tickUp);
            var tklUp = new Label { Text = $"+{FormatMoney(maxAbs * i / 2)}", Position = new(2, zeroY - h - 12), Size = new(48, 12) };
            tklUp.AddThemeFontSizeOverride("font_size", 9); tklUp.AddThemeColorOverride("font_color", new Color(0.3f, 0.6f, 0.3f));
            _revList.AddChild(tklUp);
            // 下方刻度线
            var tickDn = new ColorRect { Color = new Color(0.25f, 0.25f, 0.3f, 0.3f), Position = new(0, zeroY + h), Size = new(chartW, 1) };
            _revList.AddChild(tickDn);
            var tklDn = new Label { Text = $"-{FormatMoney(maxAbs * i / 2)}", Position = new(2, zeroY + h + 2), Size = new(48, 12) };
            tklDn.AddThemeFontSizeOverride("font_size", 9); tklDn.AddThemeColorOverride("font_color", new Color(0.7f, 0.3f, 0.3f));
            _revList.AddChild(tklDn);
        }

        for (int i = start; i < count; i++)
        {
            long profit = log[i].revenue - log[i].expense;
            long absProfit = profit > 0 ? profit : -profit;
            int idx = i - start;
            float x = plotLeft + idx * (colW + gap);

            // 月份标签 — 柱子密集时跳过部分标签避免重叠
            int m = log[i].month % 12;
            if (m == 0) m = 12;
            bool skipLabel = maxBars > 18 && i > start && (i - start) % (maxBars > 24 ? 3 : 2) != 0;
            if (!skipLabel)
            {
                var lbl = new Label { Text = $"{m}", Position = new(x, chartH - botPad + 2), Size = new(colW + gap, 12), HorizontalAlignment = HorizontalAlignment.Center };
                lbl.AddThemeFontSizeOverride("font_size", 9);
                lbl.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.45f));
                _revList.AddChild(lbl);
            }

            if (absProfit == 0) continue;

            float barH = Mathf.Max(2f, (float)absProfit / maxAbs * maxBarH);
            Color col = profit > 0 ? new Color(0.15f, 0.8f, 0.3f) : new Color(0.95f, 0.25f, 0.2f);
            float barY = profit > 0 ? zeroY - barH : zeroY;

            var bar = new ColorRect { Color = col, Position = new(x + 1, barY), Size = new(colW, barH) };
            int capI = i;
            long capProfit = profit;
            bar.MouseFilter = Control.MouseFilterEnum.Stop;
            bar.MouseEntered += () =>
            {
                _revHoverLabel.Text = Loc.TrF("fin.hover_fmt", log[capI].month, FormatMoney(log[capI].revenue), FormatMoney(log[capI].expense), FormatMoney(capProfit));
                _revHoverLabel.Visible = true;
                var gp = _revList.GlobalPosition;
                _revHoverLabel.Position = gp + new Vector2(Math.Min(x, chartW - 190), barY - 20);
            };
            bar.MouseExited += () => _revHoverLabel.Visible = false;
            _revList.AddChild(bar);
        }
    }

    private static string FormatMoney(long amount) => amount >= 1_000_000 ? Loc.TrF("ui.money_m", amount / 1_000_000f) : amount >= 1_000 ? Loc.TrF("ui.money_k", amount / 1_000f) : Loc.TrF("ui.money", amount);

    private void BuildNewsTicker(Vector2 vp, Func<float, float> S)
    {
        var tickerPanel = new Panel { Position = new(S(4), S(38)), Size = new(S(392), S(14)) };
        tickerPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f, 0.6f) });
        tickerPanel.MouseFilter = Control.MouseFilterEnum.Stop;
        tickerPanel.GuiInput += (ie) => { if (ie is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left) ShowNewsHistoryPanel(); };
        _newsTickerLabel = new Label { Position = new(S(4), S(0)), Size = new(S(384), S(14)), Text = "", MouseFilter = Control.MouseFilterEnum.Ignore };
        _newsTickerLabel.AddThemeFontSizeOverride("font_size", 9);
        _newsTickerLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.22f, 0.28f));
        _newsTickerLabel.ClipContents = true;
        tickerPanel.AddChild(_newsTickerLabel);
        _uiLayer.AddChild(tickerPanel);
    }

    // ═══════════════════════════════════════════════
    //  创始人创建页面
    // ═══════════════════════════════════════════════
    private void BuildFounderCreationScreen()
    {
        var vp = GetViewport().GetVisibleRect().Size;
        float S(float v) => v * UIScale;

        _founderOverlay = new ColorRect { Color = new Color(0.97f, 0.96f, 0.94f, 0.95f), MouseFilter = Control.MouseFilterEnum.Stop };
        _founderOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        IsAnyModalOpen = true;
        _founderOverlay.TreeExiting += () => IsAnyModalOpen = false;
        _uiLayer.AddChild(_founderOverlay);

        float pw = Math.Min(640, vp.X - S(40));
        float gap = S(4), rowH = S(24), pad = S(8);
        // 标题28 + nameRow24 + ptLabel24 + 6技能行×(24+4) + traitTitle24 + traitDesc24 + grid52 + startBtnRow24 + 顶部底部padding
        float contentH = S(28) + gap + rowH + gap + rowH + gap + 6 * (rowH + gap) + rowH + gap + rowH + gap + S(52) + gap + rowH + pad * 2;
        contentH = Mathf.Min(contentH, vp.Y - S(40));
        var card = new Panel { Position = new((vp.X - pw) / 2, (vp.Y - contentH) / 2), Size = new(pw, contentH) };
        card.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(1f, 1f, 1f, 1f), CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12 });

        var vbox = new VBoxContainer { Position = new(S(20), pad), Size = new(pw - S(40), contentH - pad * 2) };
        vbox.AddThemeConstantOverride("separation", (int)gap);
        card.AddChild(vbox); _founderOverlay.AddChild(card);

        // 标题
        var title = new Label { Text = "🎮 " + Loc.Tr("founder.title"), HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 20); title.AddThemeColorOverride("font_color", new Color(0.2f, 0.3f, 0.5f));
        vbox.AddChild(title);

        // 姓名 / 公司名（HBox）
        var nameRow = new HBoxContainer();
        nameRow.AddThemeConstantOverride("separation", (int)S(8));
        var nameLabel = new Label { Text = Loc.Tr("founder.name") };
        nameLabel.AddThemeFontSizeOverride("font_size", 13); nameLabel.AddThemeColorOverride("font_color", new Color(0.15f, 0.15f, 0.2f));
        nameRow.AddChild(nameLabel);
        var nameInput = new LineEdit { Text = Founder.Name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        nameInput.AddThemeFontSizeOverride("font_size", 13); nameRow.AddChild(nameInput);
        var compLabel = new Label { Text = Loc.Tr("founder.company") };
        compLabel.AddThemeFontSizeOverride("font_size", 13); compLabel.AddThemeColorOverride("font_color", new Color(0.15f, 0.15f, 0.2f));
        nameRow.AddChild(compLabel);
        var compInput = new LineEdit { Text = Founder.CompanyName, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        compInput.AddThemeFontSizeOverride("font_size", 13); nameRow.AddChild(compInput);
        vbox.AddChild(nameRow);

        // 技能点
        var ptLabel = new Label { Text = Loc.TrF("founder.points", Founder.UnusedPoints) };
        ptLabel.AddThemeFontSizeOverride("font_size", 14); ptLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.4f, 0.1f));
        vbox.AddChild(ptLabel);
        var ptHint = new Label { Text = Loc.Tr("founder.points_hint") };
        ptHint.AddThemeFontSizeOverride("font_size", 10); ptHint.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
        vbox.AddChild(ptHint);

        // 六维技能条（每行 HBox）
        string[] skillNames = { "founder.prog", "founder.art", "founder.audio", "founder.net", "founder.ai", "founder.mgmt" };
        int[] skillVals = { Founder.Programming, Founder.Art, Founder.Audio, Founder.Network, Founder.AI, Founder.Management };

        for (int i = 0; i < 6; i++)
        {
            int idx = i;
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", (int)S(4));

            var sl = new Label { Text = Loc.Tr(skillNames[i]), CustomMinimumSize = new(S(60), 0) };
            sl.AddThemeFontSizeOverride("font_size", 12); sl.AddThemeColorOverride("font_color", new Color(0.3f, 0.3f, 0.4f));
            row.AddChild(sl);

            var bar = new ProgressBar { MinValue = 0, MaxValue = 10, Value = skillVals[i], Step = 0, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            row.AddChild(bar);

            var minus = new Button { Text = "−", Flat = true, CustomMinimumSize = new(24, 22) };
            minus.AddThemeFontSizeOverride("font_size", 14);
            minus.AddThemeColorOverride("font_color", new Color(0, 0, 0));
            minus.AddThemeColorOverride("font_hover_color", new Color(0.2f, 0.2f, 0.2f));
            minus.Pressed += () =>
            {
                if (GetSkillRef(idx) <= 1) return;
                SetSkillRef(idx, GetSkillRef(idx) - 1);
                Founder.UnusedPoints++;
                bar.Value = GetSkillRef(idx);
                ptLabel.Text = Loc.TrF("founder.points", Founder.UnusedPoints);
            };
            row.AddChild(minus);

            var plus = new Button { Text = "+", Flat = true, CustomMinimumSize = new(24, 22) };
            plus.AddThemeFontSizeOverride("font_size", 14);
            plus.AddThemeColorOverride("font_color", new Color(0, 0, 0));
            plus.AddThemeColorOverride("font_hover_color", new Color(0.2f, 0.2f, 0.2f));
            plus.Pressed += () =>
            {
                if (Founder.UnusedPoints <= 0 || GetSkillRef(idx) >= 10) return;
                SetSkillRef(idx, GetSkillRef(idx) + 1);
                Founder.UnusedPoints--;
                bar.Value = GetSkillRef(idx);
                ptLabel.Text = Loc.TrF("founder.points", Founder.UnusedPoints);
            };
            row.AddChild(plus);

            var lvLabel = new Label { Text = Loc.TrF("ui.lv", skillVals[i]), CustomMinimumSize = new(40, 0) };
            lvLabel.AddThemeFontSizeOverride("font_size", 12); lvLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.5f, 0.2f));
            row.AddChild(lvLabel);

            int cap = i;
            var capLv = lvLabel;
            plus.Pressed += () => { capLv.Text = Loc.TrF("ui.lv", GetSkillRef(cap)); };
            minus.Pressed += () => { capLv.Text = Loc.TrF("ui.lv", GetSkillRef(cap)); };

            vbox.AddChild(row);
        }

        // 性格选择
        var traitLabel = new Label { Text = Loc.Tr("founder.trait_title") };
        traitLabel.AddThemeFontSizeOverride("font_size", 13); traitLabel.AddThemeColorOverride("font_color", new Color(0.15f, 0.15f, 0.2f));
        vbox.AddChild(traitLabel);

        var traitDesc = new Label { Text = Founder.GetTraitDescription() };
        traitDesc.AddThemeFontSizeOverride("font_size", 11); traitDesc.AddThemeColorOverride("font_color", new Color(0.3f, 0.3f, 0.4f));
        traitDesc.AutowrapMode = TextServer.AutowrapMode.Word;
        vbox.AddChild(traitDesc);

        // 性格按钮 Grid
        FounderTrait[] traits = { FounderTrait.Visionary, FounderTrait.Technical, FounderTrait.Business, FounderTrait.Indie, FounderTrait.RiskTaker, FounderTrait.Balanced };
        string[] traitIcons = { "🌟", "🔧", "💰", "🎯", "⚡", "🧘" };
        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", (int)S(8));
        grid.AddThemeConstantOverride("v_separation", (int)S(4));
        for (int i = 0; i < 6; i++)
        {
            var tb = new Button { Text = $"{traitIcons[i]} {Loc.Tr($"founder.trait_{i}")}" };
            tb.AddThemeFontSizeOverride("font_size", 11);
            int cap = i;
            tb.Pressed += () => { Founder.Trait = traits[cap]; traitDesc.Text = Founder.GetTraitDescription(); };
            if (Founder.Trait == traits[i]) tb.AddThemeColorOverride("font_color", new Color(0.6f, 0.4f, 0.1f));
            grid.AddChild(tb);
        }
        vbox.AddChild(grid);

        // 开始游戏按钮
        var btnRow = new HBoxContainer();
        btnRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        var startBtn = new Button { Text = "▶ " + Loc.Tr("founder.start") };
        startBtn.AddThemeFontSizeOverride("font_size", 16);
        startBtn.AddThemeColorOverride("font_color", new Color(0.2f, 0.6f, 0.3f));
        startBtn.AddThemeColorOverride("font_hover_color", new Color(0.1f, 0.5f, 0.2f));
        startBtn.AddThemeColorOverride("font_pressed_color", new Color(0.05f, 0.3f, 0.1f));
        startBtn.Flat = true;
        startBtn.Pressed += () =>
        {
            Founder.Name = string.IsNullOrWhiteSpace(nameInput.Text) ? "创始人" : nameInput.Text;
            Founder.CompanyName = string.IsNullOrWhiteSpace(compInput.Text) ? "独立游戏工作室" : compInput.Text;
            if (Founder.Trait == FounderTrait.Balanced)
            {
                Founder.Programming = Math.Min(10, Founder.Programming + 1);
                Founder.Art = Math.Min(10, Founder.Art + 1);
                Founder.Audio = Math.Min(10, Founder.Audio + 1);
                Founder.Network = Math.Min(10, Founder.Network + 1);
                Founder.AI = Math.Min(10, Founder.AI + 1);
                Founder.Management = Math.Min(10, Founder.Management + 1);
            }
            if (Founder.Trait == FounderTrait.Business)
                _res.Money = (long)(500_000 * 1.5f);
            Founder.HasCreated = true;
            _founderOverlay.QueueFree(); _founderOverlay = null;
            _soundMgr?.PlayGameBgm();
            // 启动教程
            if (!_tutorialMgr.TutorialCompleted)
                CallDeferred(nameof(StartTutorialDeferred));
            // 恢复全部 HUD 可见
            for (int i = 0; i < _uiLayer.GetChildCount(); i++)
                if (_uiLayer.GetChild(i) is Control c)
                    c.Visible = true;
            // 恢复各元素自身设计的隐藏状态
            if (_revHoverLabel != null) _revHoverLabel.Visible = false;
            CorpActions.Init(this, _competitor);
        };
        btnRow.AddChild(startBtn);
        btnRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        vbox.AddChild(btnRow);
        // 底部内边距
        vbox.AddChild(new Control { CustomMinimumSize = new(0, S(4)) });

        // locale helpers
        int GetSkillRef(int idx) => idx switch
        {
            0 => Founder.Programming, 1 => Founder.Art, 2 => Founder.Audio,
            3 => Founder.Network, 4 => Founder.AI, 5 => Founder.Management,
            _ => 0
        };
        void SetSkillRef(int idx, int v)
        {
            switch (idx)
            {
                case 0: Founder.Programming = v; break; case 1: Founder.Art = v; break;
                case 2: Founder.Audio = v; break; case 3: Founder.Network = v; break;
                case 4: Founder.AI = v; break; case 5: Founder.Management = v; break;
            }
        }
    }

    private void BuildBottomNav(Vector2 vp, Func<float, float> S)
    {
        float btnH = S(36);
        float gap = S(8);
        _bottomNav = new Panel { Position = new(S(8), vp.Y - btnH - gap * 2), Size = new(vp.X - S(16), btnH + gap * 2) };
        _bottomNav.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f, 0.92f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 });
        _uiLayer.AddChild(_bottomNav);
        _tabButtons.Clear();

        // 10个 SVG 图标按钮（Feather Icons 风格一致）
        bool hasDlc = (DlcManager.EnabledDlcCount > 0 && DlcManager.ActiveMinigames.Any(d => DlcManager.IsDlcEnabled(d.Id))) || DlcManager.ModMinigames.Count > 0;
        string[] svgPaths = {
            "res://assets/icons/play.svg", "res://assets/icons/file-text.svg", "res://assets/icons/file.svg",
            "res://assets/icons/users.svg", "res://assets/icons/user.svg", "res://assets/icons/zap.svg",
            "res://assets/icons/server.svg", "res://assets/icons/bar-chart-2.svg",
            "res://assets/icons/shield.svg", "res://assets/icons/home.svg"
        };
        string[] tips = { "hud.dev", "hud.projects", "hud.contracts", "hud.teams", "hud.employees", "hud.tech", "hud.server", "hud.company", "hud.attack", "hud.room" };
        if (hasDlc) { svgPaths = svgPaths.Concat(new[] { "res://assets/icons/play.svg" }).ToArray(); tips = tips.Concat(new[] { "hud.dlc" }).ToArray(); }
        // 从 Godot 导入管线加载 SVG 纹理（同时适配编辑器和导出）
        Texture2D[] iconTextures = new Texture2D[svgPaths.Length];
        for (int si = 0; si < svgPaths.Length; si++)
        {
            iconTextures[si] = ResourceLoader.Load<Texture2D>(svgPaths[si]);
        }
        float tabW = (vp.X - S(24)) / svgPaths.Length;
        for (int i = 0; i < svgPaths.Length; i++)
        {
            float btnW = tabW - S(2);
            var btn = new Panel { Position = new(S(4) + tabW * i, gap), Size = new(btnW, btnH), MouseFilter = Control.MouseFilterEnum.Stop };
            // 自定义按钮背景（圆角悬停）
            var bgStyle = new StyleBoxFlat { BgColor = Colors.Transparent, CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
            var hoverStyle = new StyleBoxFlat { BgColor = new Color(0.3f, 0.5f, 0.8f, 0.12f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
            btn.AddThemeStyleboxOverride("panel", bgStyle);
            // 图标居中
            if (iconTextures[i] != null)
            {
                var texRect = new TextureRect { Texture = iconTextures[i], ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepCentered };
                texRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                btn.AddChild(texRect);
            }
            int idx = i;

            // 悬停显示名称 — 宽度匹配按钮，居中，圆角
            string tipText = Loc.Tr(tips[idx]);
            btn.MouseEntered += () => {
                btn.AddThemeStyleboxOverride("panel", hoverStyle);
                if (_hoverTooltipPanel == null) {
                    _hoverTooltipPanel = new Panel();
                    _hoverTooltipPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f, 0.95f), CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.2f, 0.4f, 0.8f, 0.3f) });
                _hoverTooltipPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
                    _hoverTooltipLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    _hoverTooltipLabel.AddThemeFontSizeOverride("font_size", 10);
                    _hoverTooltipLabel.AddThemeColorOverride("font_color", new Color(0.15f, 0.15f, 0.2f));
                    _hoverTooltipLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    _hoverTooltipLabel.AddThemeFontSizeOverride("font_size", 10);
                    _hoverTooltipLabel.AddThemeColorOverride("font_color", new Color(0.15f, 0.15f, 0.2f));
                    _hoverTooltipPanel.AddChild(_hoverTooltipLabel);
                    // 添加到根节点（_uiLayer 可能会被面板覆盖），确保始终在顶层
                    AddChild(_hoverTooltipPanel);
                }
                _hoverTooltipLabel.Text = tipText;
                float tipH = S(22);
                _hoverTooltipPanel.Size = new(btnW, tipH);
                _hoverTooltipLabel.Size = new(btnW - 8, tipH);
                _hoverTooltipLabel.Position = new(4, 0);
                var bp = btn.GlobalPosition;
                _hoverTooltipPanel.Position = new(bp.X, bp.Y - tipH - 3);
                // 确保在根节点最顶层
                if (_hoverTooltipPanel.GetParent() != null && GodotObject.IsInstanceValid(_hoverTooltipPanel.GetParent()))
                    _hoverTooltipPanel.GetParent().MoveChild(_hoverTooltipPanel, _hoverTooltipPanel.GetParent().GetChildCount() - 1);
                _hoverTooltipPanel.Visible = true;
            };
            btn.MouseExited += () => {
                btn.AddThemeStyleboxOverride("panel", bgStyle);
                if (_hoverTooltipPanel != null) _hoverTooltipPanel.Visible = false;
            };

            // 鼠标点击（用 GuiInput 模拟按钮点击）
            btn.MouseEntered += () => _soundMgr?.PlayHover();
            btn.GuiInput += (ie) => {
                if (ie is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                {
                    OnTabClick(idx);
                    _soundMgr?.PlayClick();
                }
            };
            _bottomNav.AddChild(btn);
        }

        CardUI.EnsureHandle();

        // ── 左侧快捷按钮 ──
        var sidePanel = new Panel { Position = new Vector2(0, 460), Size = new Vector2(40, 72) };
        sidePanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0) });
        var achBtn = new Button { Text = "\U0001f3c6", Position = new Vector2(2, 4), Size = new Vector2(36, 30) };
        achBtn.AddThemeFontSizeOverride("font_size", 16);
        var capAch = _uiSysEx;
        achBtn.Pressed += () => { capAch?.ShowAchievementGallery(); };
        sidePanel.AddChild(achBtn);
        var bookBtn = new Button { Text = "\U0001f4d6", Position = new Vector2(2, 36), Size = new Vector2(36, 30) };
        bookBtn.AddThemeFontSizeOverride("font_size", 16);
        var capBook = _uiSysEx;
        bookBtn.Pressed += () => { capBook?.ShowEncyclopedia(); };
        sidePanel.AddChild(bookBtn);
        _uiLayer.AddChild(sidePanel);

        // 初始化新系统UI
        _uiSysEx?.InitUI(_uiLayer);

        // 启动 BGM
        _soundMgr?.PlayGameBgm();
    }

    private void OnTabClick(int tab)
    {
        if (_hoverTooltipPanel != null) _hoverTooltipPanel.Visible = false;
        if (_openDevPopup != null) { _openDevPopup.Close(); _openDevPopup = null; }
        if (_openPopup != null)
        {
            if (tab == _activeTab) { _openPopup.Close(); _openPopup = null; return; }
            _openPopup.Close(); _openPopup = null;
        }
        if (_openPanels.Count > 0)
        {
            if (tab == _activeTab) { while (_openPanels.Count > 0) PopTopPanel(); return; }
            while (_openPanels.Count > 0) PopTopPanel();
        }
        _activeTab = tab;

        switch (tab)
        {
            case 0: ShowDevCenterPanel(); break;
            case 1: ShowProjectPanel(); break;
            case 2: ShowContractsPanel(); break;
            case 3: ShowTeamPanel(); break;
            case 4: ShowAllEmployeesPanel(); break;
            case 5: ShowTechPanel(); break;
            case 6: ShowServerPanel(); break;
            case 7: ShowCompanyPanel(); break;
            case 8: ShowAttackPanel(); break;
            case 9: ShowRoomPanel(); break;
            case 10: LaunchDlcMinigame(); break;
        }
    }

    private void LaunchDlcMinigame()
    {
        var games = DlcManager.ActiveMinigames.Where(d => DlcManager.IsDlcEnabled(d.Id)).ToList();
        var modGames = DlcManager.ModMinigames.ToList();
        if (games.Count == 0 && modGames.Count == 0) return;
        // 只有一个 DLC 小游戏且没有 mod 小游戏 → 直接启动
        if (games.Count == 1 && modGames.Count == 0)
        {
            DlcManager.LaunchMinigame(this, games[0]);
            return;
        }
        var menu = new PopupMenu();
        int i = 0;
        foreach (var g in games)
        {
            menu.AddItem($"{g.Name} v{g.Version}", i);
            menu.SetItemTooltip(i, g.Description);
            i++;
        }
        foreach (var m in modGames)
        {
            menu.AddItem(m.Name, i);
            i++;
        }
        int dlcCount = games.Count;
        menu.Position = (Vector2I)GetViewport().GetMousePosition();
        menu.IdPressed += (long id) =>
        {
            if (id >= 0 && id < dlcCount)
                DlcManager.LaunchMinigame(this, games[(int)id]);
            else if (id >= dlcCount && id < dlcCount + modGames.Count)
            {
                var cb = modGames[(int)id - dlcCount].LaunchFunc;
                cb.Call();
            }
            menu.QueueFree();
        };
        _uiLayer.AddChild(menu);
        menu.Popup();
    }

    // ==================== 面板弹出 ====================
    internal DragPanel MakePanel(string title)
    {
        // 跳过 Protected 面板，只关闭普通面板
        var keep = new List<Panel>();
        while (_openPanels.Count > 0)
        {
            var item = _openPanels.Pop();
            if (item is DragPanel adp && adp.Protected) { keep.Add(item); continue; }
            item.QueueFree();
        }
        foreach (var k in keep) _openPanels.Push(k);
        var vp = GetViewport().GetVisibleRect().Size;
        float margin = UIScale * 40;
        var p = new DragPanel { Position = new(margin, margin), Size = new(vp.X - margin * 2, vp.Y - margin * 2) };
        p.SetScale(UIScale);
        p.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f, 0.97f), BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, BorderColor = new Color(0.2f, 0.4f, 0.8f, 0.5f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 });

        var tl = new Label { Text = title, Position = new(UIScale * 20, UIScale * 6), Size = new(p.Size.X - UIScale * 80, UIScale * 26) };
        tl.AddThemeFontSizeOverride("font_size", 20); tl.AddThemeColorOverride("font_color", new Color(0.10f, 0.14f, 0.22f));
        p.AddChild(tl);

        var cb = new Button { Text = "✕", Position = new(p.Size.X - UIScale * 50, UIScale * 4), Size = new(UIScale * 35, UIScale * 28), Flat = true };
        cb.AddThemeFontSizeOverride("font_size", 16); cb.AddThemeColorOverride("font_color", new Color(0.8f, 0.3f, 0.3f));
        cb.AddThemeColorOverride("font_hover_color", new Color(0.45f, 0.45f, 0.45f));
        cb.AddThemeColorOverride("font_pressed_color", new Color(0.3f, 0.3f, 0.3f));
        cb.Pressed += () => CloseAll();
        p.AddChild(cb);

        PushPanel(p);
        return p;
    }

    internal ScrollContainer AddScroll(Panel p)
    {
        var sc = new ScrollContainer();
        sc.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        sc.OffsetLeft = UIScale * 15;
        sc.OffsetTop = UIScale * 48;
        sc.OffsetRight = -(UIScale * 15);
        sc.OffsetBottom = -(UIScale * 12);
        p.AddChild(sc);
        return sc;
    }

    private Label MkPLabel(string text, float fs, Color color)
    {
        var l = new Label { Text = text };
        l.AddThemeFontSizeOverride("font_size", (int)fs); l.AddThemeColorOverride("font_color", color);
        return l;
    }

    private Label MkPLabel(string text, float fs, Color color, float width, bool center = false, bool wrap = true)
    {
        var l = new Label
        {
            Text = text,
            CustomMinimumSize = new(width, 0),
            AutowrapMode = wrap ? TextServer.AutowrapMode.Word : TextServer.AutowrapMode.Off
        };
        l.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin | Control.SizeFlags.Expand;
        l.AddThemeFontSizeOverride("font_size", (int)fs);
        l.AddThemeColorOverride("font_color", color);
        if (center) l.HorizontalAlignment = HorizontalAlignment.Center;
        return l;
    }

    private Button MkPButton(string text, float w, float h)
    {
        var b = new Button { Text = text, CustomMinimumSize = new(UIScale * w, UIScale * h), Size = new(UIScale * w, UIScale * h) };
        b.AddThemeFontSizeOverride("font_size", 13);
        return b;
    }

    private static string GetTraitName(EmployeeTrait trait) => trait switch
    {
        EmployeeTrait.Workaholic => Loc.Tr("trait.workaholic"),
        EmployeeTrait.Social => Loc.Tr("trait.social"),
        EmployeeTrait.Sensitive => Loc.Tr("trait.sensitive"),
        EmployeeTrait.Genius => Loc.Tr("trait.genius"),
        EmployeeTrait.Mentor => Loc.Tr("trait.mentor"),
        EmployeeTrait.LoneWolf => Loc.Tr("trait.lone_wolf"),
        EmployeeTrait.Perfectionist => Loc.Tr("trait.perfectionist"),
        EmployeeTrait.Chill => Loc.Tr("trait.chill"),
        EmployeeTrait.Ambitious => Loc.Tr("trait.ambitious"),
        EmployeeTrait.Nostalgic => Loc.Tr("trait.nostalgic"),
        EmployeeTrait.TechClean => Loc.Tr("trait.tech_clean"),
        EmployeeTrait.Lucky => Loc.Tr("trait.lucky"),
        _ => Loc.Tr("trait.none")
    };

    // ==================== 各面板 ====================
    // ══════════════════ 科技面板（新布局） ══════════════════

    private void ShowTechPanel()
    {
        var p = MakePanel(Loc.Tr("panel.tech_title"));

        // 顶部：研发中区块（直接用绝对定位确保可见）
        _techTopPanel = new Panel { Position = new(UIScale * 20, UIScale * 48), Size = new(p.Size.X - UIScale * 40, UIScale * 66) };
        _techTopPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f, 0.85f), CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6 });
        p.AddChild(_techTopPanel);
        var topLbl = LUI.Label(Loc.Tr("panel.tech_researching"), 12, new Color(0.4f, 0.7f, 1f));
        topLbl.Position = new(UIScale * 8, UIScale * 6);
        _techTopPanel.AddChild(topLbl);
        BuildTopResearchBar(_techTopPanel, UIScale * 82, UIScale * 8, UIScale * 50, p.Size.X - UIScale * 120, UIScale * 48);

        // 底部：分类科技列表（可滚动）
        var sc = new ScrollContainer { Position = new(UIScale * 20, UIScale * 48 + UIScale * 72), Size = new(p.Size.X - UIScale * 40, p.Size.Y - UIScale * 48 - UIScale * 90) };
        p.AddChild(sc);
        var listC = new VBoxContainer();
        sc.AddChild(listC);
        float listW = p.Size.X - UIScale * 80;
        BuildCategorizedTechList(listC, listW > 0 ? listW : 400);
    }

    /// <summary>每帧刷新科技面板顶部进度条（如果面板打开且还存活）</summary>
    private void RefreshTechProgressBar()
    {
        if (_techTopPanel == null || !IsInstanceValid(_techTopPanel)) return;

        // 清空旧的进度卡片
        foreach (var child in _techTopPanel.GetChildren())
        {
            if (child is Label) continue; // 保留标题 Label
            child.QueueFree();
        }
        // 重新渲染
        float topH = _techTopPanel.Size.Y;
        BuildTopResearchBar(_techTopPanel, UIScale * 82, UIScale * 8, topH - UIScale * 12,
            _techTopPanel.Size.X - UIScale * 120, topH - UIScale * 16);
    }

    private static StyleBoxFlat StyleBg(Color bg) => new() { BgColor = bg, CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6 };

    /// <summary>顶部横条：研发中（HBox 可滚动）</summary>
    private void BuildTopResearchBar(Control parent, float x, float y, float h, float maxW, float cardH)
    {
        var researching = TechTreeData.AllTech.Values
            .Where(t => _techMgr.ResearchProgress.TryGetValue(t.Id, out var p) && p > 0 && !_techMgr.IsResearched(t.Id))
            .ToList();

        var hbox = new HBoxContainer { Position = new(x, y), CustomMinimumSize = new(maxW, h) };
        hbox.AddThemeConstantOverride("separation", 8);

        if (researching.Count == 0)
        {
            var empty = new Label { Text = Loc.Tr("ui.no_research"), CustomMinimumSize = new(maxW - x, h) };
            empty.AddThemeFontSizeOverride("font_size", 11);
            empty.AddThemeColorOverride("font_color", new Color(0.35f, 0.4f, 0.5f));
            empty.HorizontalAlignment = HorizontalAlignment.Center;
            empty.VerticalAlignment = VerticalAlignment.Center;
            hbox.AddChild(empty);
        }
        else
        {
            foreach (var tech in researching)
            {
                float prog = _techMgr.ResearchProgress[tech.Id];
                float pct = Mathf.Clamp(prog / Mathf.Max(tech.RequiredManMonths, 0.01f), 0, 1);
                float remain = Mathf.Max(0, tech.RequiredManMonths - prog);
                int bars = 10;
                string bar = new string('▰', (int)(pct * bars)) + new string('▱', bars - (int)(pct * bars));

                // 查找负责团队（可能有多个团队同时研发同一科技）
                var tList = _teamMgr.Teams.Where(x => x.Task == TeamTask.ResearchTech && x.TargetTech?.Id == tech.Id).ToList();
                string teamLabel = tList.Count > 0 ? $"👥 {string.Join(", ", tList.Select(x => x.Name))}" : "";

                var card = new Panel { CustomMinimumSize = new(170, cardH) };
                card.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f, 0.7f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
                var v = new VBoxContainer { Position = new(8, 6) };
                v.AddChild(MkPLabel($"⏳ {tech.Name}", 11, new Color(0.4f, 0.7f, 1f)));
                v.AddChild(MkPLabel(Loc.TrF("tech.progress_card", bar, (int)(pct * 100), remain), 9, new Color(0.5f, 0.6f, 0.8f)));
                v.AddChild(MkPLabel(teamLabel, 8, new Color(0.4f, 0.6f, 0.4f)));
                card.AddChild(v);
                hbox.AddChild(card);
            }
        }

        parent.AddChild(hbox);
    }

    /// <summary>底部列表：按分类卡片网格排列（3列），点击卡片直接研发</summary>
    private void BuildCategorizedTechList(Control container, float width)
    {
        var groups = new (string label, TechBranch branch, Color color)[]
        {
            (Loc.Tr("tech.branch_prog"), TechBranch.ProgramBase, new Color(0.2f, 0.6f, 0.9f)),
            (Loc.Tr("tech.branch_2d"), TechBranch.Render2D, new Color(0.3f, 0.8f, 0.4f)),
            (Loc.Tr("tech.branch_3d"), TechBranch.Render3D, new Color(0.9f, 0.4f, 0.2f)),
            (Loc.Tr("tech.branch_audio"), TechBranch.Audio, new Color(0.9f, 0.3f, 0.7f)),
            (Loc.Tr("tech.branch_network"), TechBranch.Network, new Color(0.55f, 0.52f, 0.45f)),
            (Loc.Tr("tech.branch_ai"), TechBranch.AI, new Color(0.75f, 0.7f, 0.15f)),
            (Loc.Tr("tech.branch_platform"), TechBranch.Platform, new Color(0.4f, 0.6f, 0.9f)),
            (Loc.Tr("tech.branch_genre"), TechBranch.GenreUnlock, new Color(0.7f, 0.6f, 0.2f)),
            (Loc.Tr("tech.branch_theme"), TechBranch.ThemeUnlock, new Color(0.6f, 0.4f, 0.8f)),
        };

        foreach (var (label, branch, color) in groups)
        {
            var available = TechTreeData.GetBranchTech(branch)
                .Where(t => !_techMgr.IsResearched(t.Id) &&
                           !_techMgr.ResearchProgress.ContainsKey(t.Id) &&
                           TechTreeData.PrerequisiteMet(t.Prerequisite, _techMgr.ResearchedTech))
                .ToList();
            if (available.Count == 0) continue;

            AddSectionHeader(container, label, color);

            var grid = new GridContainer { Columns = 3 };
            grid.AddThemeConstantOverride("h_separation", 6);
            grid.AddThemeConstantOverride("v_separation", 4);

            foreach (var tech in available)
            {
                var card = BuildTechCard(tech, color);
                grid.AddChild(card);
            }
            container.AddChild(grid);
            // 间隔
            var spacer = new Control { CustomMinimumSize = new(10, 10) };
            container.AddChild(spacer);
        }
    }

    private Control BuildTechCard(TechInfo tech, Color accentColor)
    {
        var card = new Panel();
        card.CustomMinimumSize = new(UIScale * 200, UIScale * 82);
        card.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.97f, 0.96f, 0.94f, 0.6f),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            BorderColor = new Color(accentColor.R, accentColor.G, accentColor.B, 0.25f),
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        });

        // 使用 LinearLayout 排布卡片内容
        var ll = LUI.VBox(2);
        ll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        ll.SetPadding(UIScale * 6, UIScale * 6, UIScale * 6, UIScale * 6);
        card.AddChild(ll);

        // 顶部色条（用 ColorRect + 锚点）
        var topBar = new ColorRect { Color = accentColor };
        topBar.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        topBar.OffsetBottom = -(card.CustomMinimumSize.Y - UIScale * 3);
        card.AddChild(topBar);

        // 名称
        ll.Add(LUI.Label(tech.Name, 13, new Color(0.10f, 0.14f, 0.22f)));

        // 效果
        ll.Add(LUI.Label(tech.EffectDescription, 10, new Color(0.15f, 0.45f, 0.15f)));

        // 耗时+需求
        string secReq = tech.SecondarySkill != null ? $" + {SkillName(tech.SecondarySkill.Value)}Lv{tech.SecondarySkillLevel}" : "";
        ll.Add(LUI.Label(Loc.TrF("tech.cost_fmt", tech.RequiredManMonths, SkillName(tech.PrimarySkill), tech.PrimarySkillLevel, secReq), 9, new Color(0.4f, 0.45f, 0.5f)));

        // 研发按钮（靠右对齐）— 点击后弹出多选团队列表
        string tid = tech.Id;
        bool hasIdle = _teamMgr.Teams.Any(t => t.Task == TeamTask.None && t.Members.Count > 0);
        var btn = LUI.Button(hasIdle ? Loc.Tr("ui.research_btn") : Loc.Tr("ui.research_busy"), () =>
        {
            var pickTeams = _teamMgr.Teams.Where(t => t.Task == TeamTask.None && t.Members.Count > 0).ToList();
            if (pickTeams.Count == 0) { ShowToast(Loc.Tr("toast.no_idle_team"), "", new Color(0.9f, 0.5f, 0.2f)); return; }

            var vp = GetViewport().GetVisibleRect().Size;
            float pw = 400f * UIScale, ph = Mathf.Min(vp.Y * 0.75f, (60f + pickTeams.Count * 36f) * UIScale);
            var overlay = new ColorRect { Color = new Color(0, 0, 0, 0.5f), MouseFilter = Control.MouseFilterEnum.Stop };
            overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var panel = new Panel { Position = new((vp.X - pw) / 2, (vp.Y - ph) / 2), Size = new(pw, ph) };
            panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 });
            overlay.AddChild(panel);

            var title = new Label { Text = Loc.Tr("tech.select_team"), Position = new(12f * UIScale, 8f * UIScale), Size = new(pw - 24f * UIScale, 24f * UIScale) };
            title.AddThemeFontSizeOverride("font_size", 16);
            title.AddThemeColorOverride("font_color", new Color(0, 0, 0));
            panel.AddChild(title);

            bool[] selected = new bool[pickTeams.Count];
            Panel[] rows = new Panel[pickTeams.Count];

            for (int i = 0; i < pickTeams.Count; i++)
            {
                int idx = i;
                var t = pickTeams[i];
                float y = (36f + i * 34f) * UIScale;
                bool canRes = _techMgr.CanResearch(tid, t);
                var row = new Panel { Position = new(8f * UIScale, y), Size = new(pw - 16f * UIScale, 30f * UIScale) };
                row.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.92f, 0.92f, 0.90f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });

                var check = new Label { Text = "☐", Position = new(4f * UIScale, 3f * UIScale), Size = new(20f * UIScale, 24f * UIScale) };
                check.AddThemeFontSizeOverride("font_size", 14);
                check.AddThemeColorOverride("font_color", new Color(0, 0, 0));
                row.AddChild(check);

                string desc = $"{t.Name} ({t.Members.Count}人)" + (t.Captain != null ? $" 队长:{Loc.DisplayName(t.Captain.Name)}" : "");
                if (!canRes) desc += $" [{Loc.Tr("tech.skill_insufficient")}]";
                var nameLbl = new Label { Text = desc, Position = new(28f * UIScale, 3f * UIScale), Size = new(pw - 80f * UIScale, 24f * UIScale) };
                nameLbl.AddThemeFontSizeOverride("font_size", 12);
                nameLbl.AddThemeColorOverride("font_color", new Color(0, 0, 0));
                if (!canRes) nameLbl.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.4f));
                row.AddChild(nameLbl);

                row.MouseFilter = Control.MouseFilterEnum.Stop;
                if (canRes)
                {
                    row.GuiInput += (ie) => { if (ie is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left) { selected[idx] = !selected[idx]; check.Text = selected[idx] ? "☑" : "☐"; } };
                }
                panel.AddChild(row);
                rows[i] = row;
            }

            float btnY = ph - 36f * UIScale;
            var startBtn = new Button { Text = Loc.Tr("ui.research_btn"), Position = new(8f * UIScale, btnY), Size = new(pw / 2 - 12f * UIScale, 28f * UIScale) };
            startBtn.Pressed += () =>
            {
                int started = 0;
                for (int i = 0; i < pickTeams.Count; i++)
                    if (selected[i] && _techMgr.StartResearch(tid, pickTeams[i]))
                        started++;
                overlay.QueueFree();
                CloseAll(); ShowTechPanel();
                if (started > 0) ShowToast(Loc.Tr("ui.research_btn"), $"{started} {Loc.Tr("tech.team_assigned")}", new Color(0.3f, 0.8f, 0.5f));
            };
            panel.AddChild(startBtn);
            var cancelBtn = new Button { Text = Loc.Tr("ui.cancel"), Position = new(pw / 2 + 4f * UIScale, btnY), Size = new(pw / 2 - 12f * UIScale, 28f * UIScale) };
            cancelBtn.Pressed += () => overlay.QueueFree();
            panel.AddChild(cancelBtn);

            _uiLayer.AddChild(overlay);
        });
        if (!hasIdle) btn.Disabled = true;
        btn.AddThemeColorOverride("font_color", new Color(0.15f, 0.18f, 0.22f));
        var btnRow = LUI.HBox(); btnRow.Add(new Control(), 1); btnRow.Add(btn);
        ll.Add(btnRow);

        // 悬停高亮
        var normalStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.97f, 0.96f, 0.94f, 0.6f),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            BorderColor = new Color(accentColor.R, accentColor.G, accentColor.B, 0.25f),
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        };
        var hoverStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.90f, 0.93f, 0.92f, 0.7f),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            BorderColor = new Color(accentColor.R, accentColor.G, accentColor.B, 0.5f),
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        };
        card.MouseEntered += () => card.AddThemeStyleboxOverride("panel", hoverStyle);
        card.MouseExited += () => card.AddThemeStyleboxOverride("panel", normalStyle);

        return card;
    }

    /// <summary>引擎迭代抉择弹窗</summary>
    public void ShowEngineIterationPopup(TechInfo tech, TechManager techMgr)
    {
        CloseAll();
        Paused = true;

        var vp = GetViewport().GetVisibleRect().Size;
        var S = (Func<float, float>)(v => v * UIScale);
        float pw = S(480), ph = S(270);
        float px = (vp.X - pw) / 2, py = (vp.Y - ph) / 2;

        _openPanel = new DragPanel { Position = new(px, py), Size = new(pw, ph) };
        _openPanel.MouseFilter = Control.MouseFilterEnum.Stop;
        ((DragPanel)_openPanel).SetScale(UIScale);
        _openPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = Colors.White,
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.8f, 0.5f, 0.2f, 0.6f),
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10
        });
        PushPanel(_openPanel);

        var tl = new Label { Text = Loc.TrF("ui.engine_iter_title", techMgr.EngineGeneration, tech.Name), Position = new(S(20), S(16)), Size = new(pw - S(40), S(30)) };
        tl.AddThemeFontSizeOverride("font_size", 17);
        tl.AddThemeColorOverride("font_color", new Color(0.10f, 0.14f, 0.22f));
        _openPanel.AddChild(tl);

        var details = new Label { Text = Loc.Tr("ui.engine_iter_desc"), Position = new(S(20), S(56)), Size = new(pw - S(40), S(20)) };
        details.AddThemeFontSizeOverride("font_size", 12);
        details.AddThemeColorOverride("font_color", new Color(0.35f, 0.38f, 0.42f));
        _openPanel.AddChild(details);

        var addBtn = new Button { Text = Loc.Tr("dev.engine_add_feature"), Position = new(S(20), S(100)), Size = new(pw / 2 - S(30), S(70)) };
        addBtn.AddThemeFontSizeOverride("font_size", 12);
        addBtn.Pressed += () =>
        {
            techMgr.AddEngineFeature();
            var debtMgr = GetNode<TechDebtManager>("TechDebtManager");
            if (debtMgr != null) debtMgr.CheckThresholds();
            PopTopPanel();
            Paused = false;
        };
        _openPanel.AddChild(addBtn);

        var refactorBtn = new Button { Text = Loc.Tr("ui.engine_refactor"), Position = new(pw / 2 + S(10), S(100)), Size = new(pw / 2 - S(30), S(70)) };
        refactorBtn.AddThemeFontSizeOverride("font_size", 12);
        refactorBtn.Pressed += () =>
        {
            techMgr.FullEngineRefactor();
            var debtMgr = GetNode<TechDebtManager>("TechDebtManager");
            if (debtMgr != null) debtMgr.CheckThresholds();
            PopTopPanel();
            Paused = false;
        };
        _openPanel.AddChild(refactorBtn);
    }

    private void AddSectionHeader(Control parent, string label, Color color)
    {
        var h = new HBoxContainer();
        var lbl = new Label { Text = $"▶ {label}" };
        lbl.AddThemeFontSizeOverride("font_size", 11);
        lbl.AddThemeColorOverride("font_color", color);
        h.AddChild(lbl);
        parent.AddChild(h);
    }

    private static string SkillName(SkillType s) => s.Name();

    // ══════════════════ 文本自适应工具 ══════════════════

    /// <summary>
    /// 测量文本在指定宽度/字号下的实际高度（含自动换行）。
    /// 如果放不下就逐级缩小字号，返回 (实际高度, 最终字号)。
    /// </summary>
    private (float Height, int FontSize) FitTextSize(string text, float maxWidth, float maxHeight,
        int maxFontSize, int minFontSize = 9)
    {
        var font = ThemeDB.FallbackFont;
        int fs = maxFontSize;
        while (fs >= minFontSize)
        {
            float h = font.GetMultilineStringSize(text, HorizontalAlignment.Left, maxWidth, fs).Y;
            if (h <= maxHeight || fs == minFontSize)
                return (h, fs);
            fs--;
        }
        return (0, minFontSize);
    }

    /// <summary>
    /// 计算文本在给定宽度下所需的高度（自动换行），不缩放字号
    /// </summary>
    private float MeasureTextHeight(string text, float maxWidth, int fontSize)
    {
        return ThemeDB.FallbackFont.GetMultilineStringSize(text, HorizontalAlignment.Left, maxWidth, fontSize).Y;
    }

    // ══════════════════ 通用弹窗 ══════════════════

    private Panel _toastPanel;
    private Panel _noticePanel;                      // 右下角非阻塞通知
    private bool _wasPausedBeforePopup; // 弹窗前是否已暂停（用户手动暂停）
    private int _popupDepth;            // 弹窗嵌套深度，深度归零才恢复暂停

    /// <summary>右下角非阻塞通知 — 不暂停游戏，自动消失</summary>
    public void ShowToast(string title, string message, Color titleColor, float duration = 6f)
    {
        if (_uiLayer == null) return;
        _noticePanel?.QueueFree();
        var vp = GetViewport().GetVisibleRect().Size;
        var S = (Func<float, float>)(v => v * UIScale);

        float pw = S(340);
        float pad = S(14);
        float contentW = pw - pad * 2;
        float titleH = S(22), gapT = S(8);
        float maxPh = Mathf.Min(vp.Y * 0.55f, S(220)); // 面板最高不超过屏幕55%
        float maxMsgH = maxPh - pad * 2 - titleH - gapT;

        var (msgH, msgFont) = FitTextSize(message, contentW, maxMsgH, 11, 8);

        // 消息实际占高（可能超出可显示范围则裁剪）
        float displayMsgH = Mathf.Min(msgH, maxMsgH);
        float ph = pad + titleH + gapT + displayMsgH + pad;
        ph = Mathf.Max(ph, S(100));

        _noticePanel = new Panel { Position = new(vp.X - pw - S(16), vp.Y - ph - S(60)), Size = new(pw, ph) };
        _noticePanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        _noticePanel.ClipContents = true;
        _noticePanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.97f, 0.96f, 0.94f, 0.92f),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            BorderColor = new Color(titleColor.R, titleColor.G, titleColor.B, 0.4f),
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        });

        float topY = pad;
        var tl = new Label { Text = title, Position = new(pad, topY), Size = new(contentW, titleH) };
        tl.AddThemeFontSizeOverride("font_size", 14);
        tl.AddThemeColorOverride("font_color", titleColor);
        _noticePanel.AddChild(tl);

        var ml = new Label { Text = message, Position = new(pad, topY + titleH + gapT), Size = new(contentW, displayMsgH) };
        ml.AddThemeFontSizeOverride("font_size", msgFont);
        ml.AddThemeColorOverride("font_color", new Color(0.2f, 0.22f, 0.25f));
        ml.AutowrapMode = TextServer.AutowrapMode.Word;
        ml.ClipContents = true;
        _noticePanel.AddChild(ml);

        _uiLayer.AddChild(_noticePanel);

        // ── 滑入动画 ──
        _noticePanel.Modulate = new Color(1, 1, 1, 0);
        var startX = _noticePanel.Position.X + S(30);
        var targetX = _noticePanel.Position.X;
        _noticePanel.Position = new(startX, _noticePanel.Position.Y);
        var tween = GetTree().CreateTween();
        tween.TweenProperty(_noticePanel, "modulate", new Color(1, 1, 1, 1), 0.25f);
        tween.Parallel().TweenProperty(_noticePanel, "position:x", targetX, 0.25f);

        GetTree().CreateTimer(duration).Timeout += () =>
        {
            if (_noticePanel == null) return;
            var fadeOut = GetTree().CreateTween();
            fadeOut.TweenProperty(_noticePanel, "modulate", new Color(1, 1, 1, 0), 0.3f);
            fadeOut.TweenCallback(Callable.From(() => { _noticePanel?.QueueFree(); _noticePanel = null; }));
        };
    }

    private struct QueuedPopup { public string title, msg, optA, optB, optC; public Color color; public Action onA, onB, onC; public bool isChoice, isTriChoice; }
    private Queue<QueuedPopup> _popupQueue = new();
    private void ShowNextQueuedPopup()
    {
        if (_popupQueue.Count == 0) return;
        var q = _popupQueue.Dequeue();
        if (q.isTriChoice)
            ShowTriChoicePopup(q.title, q.msg, q.optA, q.optB, q.optC, q.onA, q.onB, q.onC, q.color);
        else if (q.isChoice)
            ShowChoicePopup(q.title, q.msg, q.optA, q.optB, q.onA, q.onB, q.color);
        else
            ShowPopup(q.title, q.msg, q.color);
    }

    public void ShowPopup(string title, string message, Color titleColor)
    {
        if (_uiLayer == null) return;
        if (_toastPanel != null) { _popupQueue.Enqueue(new QueuedPopup { title = title, msg = message, color = titleColor, isChoice = false }); return; }
        _popupDepth++;
        if (_popupDepth == 1) _wasPausedBeforePopup = Paused;
        Paused = true;
        var vp = GetViewport().GetVisibleRect().Size;
        var S = (Func<float, float>)(v => v * UIScale);

        float pw = S(440);
        float pad = S(20);
        float contentW = pw - pad * 2;
        float titleH = S(30), gapT = S(14), gapM = S(16);
        float btnH = S(36);

        float availableMsgH = vp.Y * 0.82f - titleH - gapT - gapM - btnH - pad * 2;
        float msgW = contentW - S(4);
        var (msgH, msgFont) = FitTextSize(message, msgW, availableMsgH, 13, 9);

        float ph = pad + titleH + gapT + msgH + gapM + btnH + pad;
        ph = Mathf.Min(ph, vp.Y * 0.82f);

        _toastPanel = new Panel { Position = new((vp.X - pw) / 2, (vp.Y - ph) / 2), Size = new(pw, ph) };
        _toastPanel.MouseFilter = Control.MouseFilterEnum.Stop;
        _toastPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.97f, 0.96f, 0.94f, 0.97f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(titleColor.R, titleColor.G, titleColor.B, 0.6f),
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10
        });

        // 标题
        var tl = new Label { Text = title, Position = new(pad, pad), Size = new(contentW, titleH) };
        tl.AddThemeFontSizeOverride("font_size", 18);
        tl.AddThemeColorOverride("font_color", titleColor);
        _toastPanel.AddChild(tl);

        // 消息正文
        float msgY = pad + titleH + gapT;
        var ml = new Label { Text = message, Position = new(pad, msgY), Size = new(msgW, msgH) };
        ml.AddThemeFontSizeOverride("font_size", msgFont);
        ml.AddThemeColorOverride("font_color", new Color(0.10f, 0.14f, 0.22f));
        ml.AutowrapMode = TextServer.AutowrapMode.Arbitrary;
        _toastPanel.AddChild(ml);

        // 确定按钮
        float btnY = msgY + msgH + gapM;
        var ok = new Button { Text = Loc.Tr("ui.got_it"), Position = new(pw / 2 - S(50), btnY), Size = new(S(100), btnH) };
        ok.AddThemeFontSizeOverride("font_size", 14);
        ok.Pressed += () => { _toastPanel?.QueueFree(); _toastPanel = null; _popupDepth--; if (_popupDepth <= 0) Paused = _wasPausedBeforePopup; ShowNextQueuedPopup(); };
        _toastPanel.AddChild(ok);

        _uiLayer.AddChild(_toastPanel);

        // ── 弹出动画 ──
        _toastPanel.Modulate = new Color(1, 1, 1, 0);
        _toastPanel.Scale = new Vector2(0.92f, 0.92f);
        var popTween = GetTree().CreateTween();
        popTween.TweenProperty(_toastPanel, "modulate", new Color(1, 1, 1, 1), 0.2f);
        popTween.Parallel().TweenProperty(_toastPanel, "scale", Vector2.One, 0.2f);
    }

    /// <summary>带A/B选择的事件弹窗 — 屏幕中央，自适应高度，暂停时间。
    /// 按钮文本过长时自动切换为纵向排列。</summary>
    public void ShowChoicePopup(string title, string message, string optA, string optB, Action onA, Action onB, Color titleColor)
    {
        if (_uiLayer == null) return;
        if (_toastPanel != null) { _popupQueue.Enqueue(new QueuedPopup { title = title, msg = message, color = titleColor, optA = optA, optB = optB, onA = onA, onB = onB, isChoice = true }); return; }
        _popupDepth++;
        if (_popupDepth == 1) _wasPausedBeforePopup = Paused;
        Paused = true;
        var vp = GetViewport().GetVisibleRect().Size;
        var S = (Func<float, float>)(v => v * UIScale);
        var font = ThemeDB.FallbackFont;

        float pw = S(480);
        float pad = S(24);
        float contentW = pw - pad * 2;
        float titleH = S(32), gapT = S(14), gapM = S(14);
        float btnH = S(44), btnGap = S(10);
        int btnFont = 14;

        // 测量按钮文本宽度，决定横排还是纵排
        float btnTextWA = font.GetStringSize(optA, fontSize: btnFont).X;
        float btnTextWB = font.GetStringSize(optB, fontSize: btnFont).X;
        float maxBtnTextW = Mathf.Max(btnTextWA, btnTextWB);
        float halfW = contentW / 2 - S(6);
        bool verticalBtns = maxBtnTextW > halfW - S(16); // 16=按钮内边距估计

        float btnAreaH = verticalBtns ? btnH * 2 + btnGap : btnH;

        float availableMsgH = vp.Y * 0.82f - titleH - gapT - gapM - btnAreaH - pad * 2;
        float msgW = contentW - S(4);
        var (msgH, msgFont) = FitTextSize(message, msgW, availableMsgH, 13, 9);

        float ph = pad + titleH + gapT + msgH + gapM + btnAreaH + pad;
        ph = Mathf.Min(ph, vp.Y * 0.82f);

        _toastPanel = new Panel { Position = new((vp.X - pw) / 2, (vp.Y - ph) / 2), Size = new(pw, ph) };
        _toastPanel.MouseFilter = Control.MouseFilterEnum.Stop;
        _toastPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = Colors.White,
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(titleColor.R, titleColor.G, titleColor.B, 0.6f),
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12
        });

        var tl = new Label { Text = title, Position = new(pad, pad), Size = new(contentW, titleH) };
        tl.AddThemeFontSizeOverride("font_size", 18);
        tl.AddThemeColorOverride("font_color", titleColor);
        _toastPanel.AddChild(tl);

        float msgY = pad + titleH + gapT;
        var ml = new Label { Text = message, Position = new(pad, msgY), Size = new(msgW, msgH) };
        ml.AddThemeFontSizeOverride("font_size", msgFont);
        ml.AddThemeColorOverride("font_color", new Color(0.10f, 0.14f, 0.22f));
        ml.AutowrapMode = TextServer.AutowrapMode.Arbitrary;
        _toastPanel.AddChild(ml);

        float btnY = msgY + msgH + gapM;
        if (verticalBtns)
        {
            // 纵向排列：各占满宽
            var btnA = new Button { Text = optA, Position = new(pad, btnY), Size = new(contentW, btnH) };
            btnA.AddThemeFontSizeOverride("font_size", btnFont);
            btnA.Pressed += () => { _toastPanel?.QueueFree(); _toastPanel = null; _popupDepth--; if (_popupDepth <= 0) Paused = _wasPausedBeforePopup; ShowNextQueuedPopup(); onA?.Invoke(); };
            _toastPanel.AddChild(btnA);

            var btnB = new Button { Text = optB, Position = new(pad, btnY + btnH + btnGap), Size = new(contentW, btnH) };
            btnB.AddThemeFontSizeOverride("font_size", btnFont);
            btnB.Pressed += () => { _toastPanel?.QueueFree(); _toastPanel = null; _popupDepth--; if (_popupDepth <= 0) Paused = _wasPausedBeforePopup; ShowNextQueuedPopup(); onB?.Invoke(); };
            _toastPanel.AddChild(btnB);
        }
        else
        {
            // 横向排列
            float btnW = halfW;
            var btnA = new Button { Text = optA, Position = new(pad, btnY), Size = new(btnW, btnH) };
            btnA.AddThemeFontSizeOverride("font_size", btnFont);
            btnA.Pressed += () => { _toastPanel?.QueueFree(); _toastPanel = null; _popupDepth--; if (_popupDepth <= 0) Paused = _wasPausedBeforePopup; ShowNextQueuedPopup(); onA?.Invoke(); };
            _toastPanel.AddChild(btnA);

            var btnB = new Button { Text = optB, Position = new(pad + btnW + S(12), btnY), Size = new(btnW, btnH) };
            btnB.AddThemeFontSizeOverride("font_size", btnFont);
            btnB.Pressed += () => { _toastPanel?.QueueFree(); _toastPanel = null; _popupDepth--; if (_popupDepth <= 0) Paused = _wasPausedBeforePopup; ShowNextQueuedPopup(); onB?.Invoke(); };
            _toastPanel.AddChild(btnB);
        }

        _uiLayer.AddChild(_toastPanel);

        // ── 弹出动画 ──（ShowChoicePopup）
        _toastPanel.Modulate = new Color(1, 1, 1, 0);
        _toastPanel.Scale = new Vector2(0.92f, 0.92f);
        var anim = GetTree().CreateTween();
        anim.TweenProperty(_toastPanel, "modulate", new Color(1, 1, 1, 1), 0.2f);
        anim.Parallel().TweenProperty(_toastPanel, "scale", Vector2.One, 0.2f);
    }

    /// <summary>带A/B/C三选一的事件弹窗 — 自适应高度，暂停时间</summary>
    public void ShowTriChoicePopup(string title, string message, string optA, string optB, string optC, Action onA, Action onB, Action onC, Color titleColor)
    {
        if (_uiLayer == null) return;
        if (!GodotObject.IsInstanceValid(this)) return;
        if (_toastPanel != null) { _popupQueue.Enqueue(new QueuedPopup { title = title, msg = message, color = titleColor, optA = optA, optB = optB, optC = optC, onA = onA, onB = onB, onC = onC, isChoice = true, isTriChoice = true }); return; }
        _popupDepth++;
        _wasPausedBeforePopup = Paused;
        Paused = true;
        var vp = GetViewport().GetVisibleRect().Size;
        var S = (Func<float, float>)(v => v * UIScale);

        float pw = S(500);
        float pad = S(24);
        float contentW = pw - pad * 2;
        float titleH = S(32), gapT = S(12);
        float btnH = S(36), btnGap = S(8);
        float btnAreaH = (btnH + btnGap) * 3 - btnGap;

        float availableMsgH = vp.Y * 0.82f - titleH - gapT - btnAreaH - pad * 2;
        float msgW = contentW - S(4);
        var (msgH, msgFont) = FitTextSize(message, msgW, availableMsgH, 13, 9);

        float ph = pad + titleH + gapT + msgH + btnAreaH + pad;
        ph = Mathf.Min(ph, vp.Y * 0.82f);

        _toastPanel = new Panel { Position = new((vp.X - pw) / 2, (vp.Y - ph) / 2), Size = new(pw, ph) };
        _toastPanel.MouseFilter = Control.MouseFilterEnum.Stop;
        _toastPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = Colors.White,
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(titleColor.R, titleColor.G, titleColor.B, 0.6f),
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12
        });

        var tl = new Label { Text = title, Position = new(pad, pad), Size = new(contentW, titleH) };
        tl.AddThemeFontSizeOverride("font_size", 18);
        tl.AddThemeColorOverride("font_color", titleColor);
        _toastPanel.AddChild(tl);

        float msgY = pad + titleH + gapT;
        var ml = new Label { Text = message, Position = new(pad, msgY), Size = new(msgW, msgH) };
        ml.AddThemeFontSizeOverride("font_size", msgFont);
        ml.AddThemeColorOverride("font_color", new Color(0.10f, 0.14f, 0.22f));
        ml.AutowrapMode = TextServer.AutowrapMode.Arbitrary;
        _toastPanel.AddChild(ml);

        float btnAreaY = msgY + msgH;
        var btnA = new Button { Text = optA, Position = new(pad, btnAreaY), Size = new(contentW, btnH) };
        btnA.AddThemeFontSizeOverride("font_size", 13);
        btnA.Pressed += () => { _toastPanel?.QueueFree(); _toastPanel = null; _popupDepth--; if (_popupDepth <= 0) Paused = _wasPausedBeforePopup; onA?.Invoke(); ShowNextQueuedPopup(); };
        _toastPanel.AddChild(btnA);

        var btnB = new Button { Text = optB, Position = new(pad, btnAreaY + btnH + btnGap), Size = new(contentW, btnH) };
        btnB.AddThemeFontSizeOverride("font_size", 13);
        btnB.Pressed += () => { _toastPanel?.QueueFree(); _toastPanel = null; _popupDepth--; if (_popupDepth <= 0) Paused = _wasPausedBeforePopup; onB?.Invoke(); ShowNextQueuedPopup(); };
        _toastPanel.AddChild(btnB);

        var btnC = new Button { Text = optC, Position = new(pad, btnAreaY + (btnH + btnGap) * 2), Size = new(contentW, btnH) };
        btnC.AddThemeFontSizeOverride("font_size", 13);
        btnC.Pressed += () => { _toastPanel?.QueueFree(); _toastPanel = null; _popupDepth--; if (_popupDepth <= 0) Paused = _wasPausedBeforePopup; onC?.Invoke(); ShowNextQueuedPopup(); };
        _toastPanel.AddChild(btnC);

        _uiLayer.AddChild(_toastPanel);

        // ── 弹出动画 ──
        _toastPanel.Modulate = new Color(1, 1, 1, 0);
        _toastPanel.Scale = new Vector2(0.92f, 0.92f);
        var aTween = GetTree().CreateTween();
        aTween.TweenProperty(_toastPanel, "modulate", new Color(1, 1, 1, 1), 0.2f);
        aTween.Parallel().TweenProperty(_toastPanel, "scale", Vector2.One, 0.2f);
    }

    // ══════════════════ 年度颁奖典礼 ══════════════════
    private void ShowAnnualAwards()
    {
        int year = GameYear;
        var thisYearGames = _devMgr.CompletedProjects
            .Where(p => (int)(p.DevLog.FirstOrDefault()?.Length ?? 0) != 0)
            .ToList();
        if (thisYearGames.Count == 0) return;

        var bestGame = thisYearGames.OrderByDescending(p => p.FinalScore).First();
        var rng = new Random(year * 137);

        // 只发一条简短新闻通知，不弹窗
        _competitor.PushNews("🏆", Loc.TrF("misc.best_game", year), $"《{bestGame.Name}》{Loc.Tr("misc.game_score")}{bestGame.FinalScore:F0}",
            new Color(0.9f, 0.75f, 0.15f));

        // AI公司也有概率拿奖
        var aiBest = _competitor.Studios
            .SelectMany(s => s.Releases.Where(r => r.ReleaseMonth > (GameMonth - 12)).Select(r => (s, r)))
            .OrderByDescending(x => x.r.Score)
            .FirstOrDefault();
        if (aiBest.s != null && rng.Next(100) < 40)
            _competitor.PushNews("🌟", $"{aiBest.s.Name} {Loc.Tr("news.ai_best")}", $"《{aiBest.r.Name}》{Loc.Tr("misc.game_score")}{aiBest.r.Score:F0}，{Loc.Tr("news.market_good")}",
                new Color(0.7f, 0.7f, 0.75f));
    }

    // ══════════════════ 团队面板（公司页面风格） ══════════════════
    private Panel _teamPanel;
    private ScrollContainer _teamScroll;
    private VBoxContainer _teamList;

    // 招聘系统
    private List<Employee> _recruitCandidates = new();
    private int _lastRecruitMonth = -1;

    // 员工多选
    private HashSet<int> _selectedEmployees = new();
    private int _lastEmpClickIndex = -1;
    private int _hoveredEmpId = -1;
    private string _hoveredTeamName;
    private bool _teamListNeedRebuild = true;
    private Dictionary<VBoxContainer, List<Employee>> _empListSources = new();
    private List<Employee> _activePanelEmpSource;     // 当前面板的员工来源（Ctrl+A用）
    private Action _activePanelOnSelectChanged;        // 选择变化回调（如更新雇佣按钮）
    private Action _activePanelEnterAction;            // 当前面板的回车操作
    private Action _activePanelSelectAllAction;        // 当前面板的 Ctrl+A 全选操作
    private Action _refreshEmployeeList;               // 当前员工面板的刷新回调（就地更新，不关不重开）

    private void ShowTeamPanel()
    {
        bool firstBuild = _teamPanel == null || !GodotObject.IsInstanceValid(_teamPanel) || !_teamPanel.IsInsideTree();

        if (firstBuild)
        {
            var p = MakePanel(Loc.Tr("panel.team_manage"));
            _teamPanel = p;

            // 表头
            float y0 = UIScale * 32;
            var header = new HBoxContainer { Position = new(UIScale * 20, y0) };
            header.AddThemeConstantOverride("separation", (int)(UIScale * 4));
            void AddHCol(string label, float w)
            {
                var l = MkPLabel(label, 12, new Color(0.08f, 0.12f, 0.2f));
                l.CustomMinimumSize = new(w, 0);
                header.AddChild(l);
            }
            AddHCol(" "+Loc.Tr("panel.col_team"), UIScale * 170);
            AddHCol(Loc.Tr("panel.col_members"), UIScale * 70);
            AddHCol(Loc.Tr("panel.col_salary_total"), UIScale * 110);
            AddHCol(Loc.Tr("panel.col_task"), UIScale * 200);
            p.AddChild(header);

            // 操作提示
            var hintLabel = MkPLabel(Loc.Tr("panel.team_hint"), 10, new Color(0.55f, 0.58f, 0.6f));
            hintLabel.Position = new(UIScale * 22, y0 + UIScale * 18);
            p.AddChild(hintLabel);

            // 滚动容器
            _teamScroll = new ScrollContainer { Position = new(UIScale * 20, y0 + UIScale * 34), Size = new(p.Size.X - UIScale * 40, p.Size.Y - y0 - UIScale * 109) };
            _teamScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
            _teamList = new VBoxContainer();
            _teamList.AddThemeConstantOverride("separation", (int)(UIScale * 2));
            _teamScroll.AddChild(_teamList);
            p.AddChild(_teamScroll);

            // 底部操作栏
            float botY = p.Size.Y - UIScale * 58;
            var botBar = new HBoxContainer { Position = new(UIScale * 20, botY) };
            botBar.AddThemeConstantOverride("separation", (int)(UIScale * 12));

            var createTeamBtn = MkPButton(Loc.Tr("panel.btn_create_team"), 140, 32);
            createTeamBtn.Pressed += () =>
            {
                var idle = _teamMgr.GetIdleEmployees();
                int cnt = Mathf.Min(4, idle.Count);
                var members = idle.Take(cnt).ToList();
                var captain = members.FirstOrDefault(e => e.CanMentor);
                _teamMgr.CreateTeam(Loc.TrF("ui.team_fmt", _teamMgr.Teams.Count + 1), members, captain);
                ShowTeamPanel();
            };
            botBar.AddChild(createTeamBtn);

            var viewAllBtn = MkPButton(Loc.Tr("panel.btn_view_all"), 160, 32);
            viewAllBtn.Pressed += () => ShowAllEmployeesPanel();
            botBar.AddChild(viewAllBtn);

            var recruitBtn = MkPButton(Loc.Tr("panel.recruit_btn"), 120, 32);
            recruitBtn.Pressed += () =>
            {
                if (_empMgr.Employees.Count >= _roomMgr.TotalCapacity)
                {
                    ShowPopup(Loc.Tr("toast.room_full"), Loc.TrF("ui.employee_cap", _roomMgr.TotalCapacity, _empMgr.Employees.Count), new Color(0.9f, 0.3f, 0.2f));
                    return;
                }
                ShowRecruitPanel();
            };
            botBar.AddChild(recruitBtn);

            p.AddChild(botBar);
        }

        // ── 重建列表行 ──
        foreach (var ch in _teamList.GetChildren()) ch.QueueFree();

        if (_teamMgr.Teams.Count == 0)
        {
            _teamList.AddChild(MkPLabel(Loc.Tr("panel.team_empty"), 13, new Color(0.6f, 0.3f, 0.2f)));
        }
        else foreach (var team in _teamMgr.Teams)
        {
            var row = MakeTeamRow(team);
            _teamList.AddChild(row);
        }

        // ── 汇总行 ──
        int totalEmp = _empMgr.Employees.Count;
        float totalSalary = _empMgr.Employees.Sum(e => e.Salary);
        var sumPc = new PanelContainer();
        sumPc.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        sumPc.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.92f, 0.90f, 0.86f, 0.6f), BorderWidthTop = 1, BorderColor = new Color(0.4f, 0.45f, 0.5f, 0.3f) });
        var sumHb = new HBoxContainer();
        sumHb.AddThemeConstantOverride("separation", (int)(UIScale * 4));
        void AddSumL(string text, float w, int r, int g, int b)
        {
            var l = new Label { Text = text, CustomMinimumSize = new(w, 0) };
            l.AddThemeFontSizeOverride("font_size", 12);
            l.AddThemeColorOverride("font_color", new Color(r / 255f, g / 255f, b / 255f));
            l.MouseFilter = Control.MouseFilterEnum.Ignore;
            sumHb.AddChild(l);
        }
        int idleEmp = _teamMgr.GetIdleEmployees().Count;
        AddSumL(Loc.TrF("panel.team_summary", _teamMgr.Teams.Count, totalEmp, idleEmp), UIScale * 170, 20, 20, 25);
        AddSumL("", UIScale * 70, 20, 20, 25);
        AddSumL(Loc.TrF("ui.monthly", totalSalary), UIScale * 110, 15, 55, 20);
        AddSumL("", UIScale * 200, 20, 20, 25);
        sumPc.AddChild(sumHb);
        _teamList.AddChild(sumPc);
    }

    /// <summary>员工行高亮：完全照搬 ApplyPCRowHighlight，读 _hoveredEmpId</summary>
    /// <summary>员工行高亮：和公司列表 ApplyPCRowHighlight 完全一致的色号和逻辑</summary>
    private void ApplyEmpRowHighlight(Control pc, int empId)
    {
        bool selected = _selectedEmployees.Contains(empId);
        bool hovered = _hoveredEmpId == empId;
        const int BW = 1, BL = 3;

        if (selected)
        {
            pc.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color(0.15f, 0.4f, 0.85f, 0.15f),
                BorderWidthLeft = BL, BorderWidthTop = BW, BorderWidthRight = BW, BorderWidthBottom = BW,
                BorderColor = new Color(0.15f, 0.5f, 0.9f, 0.7f),
                ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 2, ContentMarginBottom = 2,
            });
        }
        else if (hovered)
        {
            pc.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color(1, 1, 1, 0.02f),
                BorderWidthLeft = BL, BorderWidthTop = BW, BorderWidthRight = BW, BorderWidthBottom = BW,
                BorderColor = new Color(0.6f, 0.6f, 0.6f, 0.35f),
                ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 2, ContentMarginBottom = 2,
            });
        }
        else
        {
            pc.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color(1, 1, 1, 0.01f),
                BorderWidthLeft = BL, BorderWidthTop = BW, BorderWidthRight = BW, BorderWidthBottom = BW,
                BorderColor = new Color(0, 0, 0, 0),
                ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 2, ContentMarginBottom = 2,
            });
        }
    }

    private void RefreshEmpListHighlights(Control row)
    {
        var parent = row.GetParent();
        if (parent == null) { DlcManager.Log("EmpHL", "parent is null"); return; }
        if (parent is VBoxContainer list && _empListSources.TryGetValue(list, out var sourceList))
        {
            int idx = 0;
            foreach (var ch in list.GetChildren())
            {
                if (ch is Control ctrl)
                {
                    if (idx < sourceList.Count)
                        ApplyEmpRowHighlight(ctrl, sourceList[idx].Id);
                    idx++;
                }
            }
        }
    }

    private delegate void EmpRowBuilder(VBoxContainer list, Employee emp, int idx, List<Employee> source);

    private void RebuildEmpListInPlace(VBoxContainer list, List<Employee> source, EmpRowBuilder builder)
    {
        if (!_empListSources.ContainsKey(list)) return;
        // 隐藏列表避免重建闪烁，保存滚动位置
        var scroll = list.GetParent() as ScrollContainer;
        int oldPos = scroll?.ScrollVertical ?? 0;
        list.Visible = false;

        var children = list.GetChildren();
        PanelContainer summaryPc = null;
        if (children.Count > 0 && children[children.Count - 1] is PanelContainer lastPc)
            summaryPc = lastPc;

        var toRemove = new System.Collections.Generic.List<Node>();
        foreach (var ch in children)
            if (ch != summaryPc) toRemove.Add(ch);
        foreach (var ch in toRemove) { list.RemoveChild(ch); ch.QueueFree(); }
        for (int i = 0; i < source.Count; i++)
            builder(list, source[i], i, source);
        if (summaryPc != null) list.AddChild(summaryPc);
        _empListSources[list] = source;

        // 恢复可见和滚动位置
        list.Visible = true;
        if (scroll != null) scroll.ScrollVertical = oldPos;
    }

    private void ApplyAllEmpHighlights()
    {
        foreach (var kv in _empListSources)
        {
            var list = kv.Key;
            var sourceList = kv.Value;
            if (!GodotObject.IsInstanceValid(list) || !list.IsInsideTree()) continue;
            int idx = 0;
            foreach (var ch in list.GetChildren())
            {
                if (ch is Control ctrl)
                {
                    if (idx < sourceList.Count)
                        ApplyEmpRowHighlight(ctrl, sourceList[idx].Id);
                    idx++;
                }
            }
        }
    }

    /// <summary>注册员工列表 + 对应的数据源（用于索引匹配高亮）</summary>
    private void RegisterEmpList(VBoxContainer list, List<Employee> source)
    {
        _empListSources[list] = source;
        list.TreeExited += () => { if (_empListSources.ContainsKey(list)) _empListSources.Remove(list); };
    }

    /// <summary>创建标准的可多选员工行容器（PanelContainer + HBoxContainer，类似团队列表样式）</summary>
    private (PanelContainer pc, HBoxContainer hb) MakeEmpRowContainer(Employee emp)
    {
        var pc = new PanelContainer();
        pc.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        pc.MouseFilter = Control.MouseFilterEnum.Stop;
        pc.SetMeta("_empId", emp.Id);

        ApplyEmpRowHighlight(pc, emp.Id);

        var hb = new HBoxContainer();
        hb.AddThemeConstantOverride("separation", (int)(UIScale * 4));
        hb.MouseFilter = Control.MouseFilterEnum.Ignore;
        pc.AddChild(hb);

        pc.MouseEntered += () =>
        {
            if (!GodotObject.IsInstanceValid(pc)) return;
            _hoveredEmpId = emp.Id;
            ApplyEmpRowHighlight(pc, emp.Id);
        };
        pc.MouseExited += () =>
        {
            if (!GodotObject.IsInstanceValid(pc)) return;
            _hoveredEmpId = -1;
            ApplyEmpRowHighlight(pc, emp.Id);
        };

        return (pc, hb);
    }

    /// <summary>创建标准的点击多选处理器（包含左键单选/多选，右键弹出菜单）</summary>
    private void AttachEmpClickHandler(Control row, Employee emp, int empIdx, List<Employee> shiftSource, Team teamContext = null)
    {
        row.GuiInput += (ie) =>
        {
            if (!(ie is InputEventMouseButton mb) || !mb.Pressed) return;
            if (!GodotObject.IsInstanceValid(row) || row.GetParent() == null) return;

            bool ctrl = (mb.CtrlPressed || mb.MetaPressed);
            bool shift = mb.ShiftPressed;

            if (mb.ButtonIndex == MouseButton.Right)
            {
                bool wasSelected = _selectedEmployees.Contains(emp.Id);
                if (!wasSelected)
                {
                    DlcManager.Log("Select", $"right-click on emp {emp.Id} (not selected), clearing {_selectedEmployees.Count} existing");
                    _selectedEmployees.Clear();
                    _selectedEmployees.Add(emp.Id);
                    _lastEmpClickIndex = empIdx;
                }
                else DlcManager.Log("Select", $"right-click on selected emp {emp.Id}, keeping {_selectedEmployees.Count} selections");
                RefreshEmpListHighlights(row);
                DlcManager.Log("Select", $"after right-click: count={_selectedEmployees.Count}, {( _selectedEmployees.Count > 1 ? "batch menu" : "context menu")}");
                if (_selectedEmployees.Count > 1) ShowBatchEmployeeMenu();
                else ShowEmployeeContextMenu(emp, teamContext);
                return;
            }

            if (mb.ButtonIndex != MouseButton.Left) return;
            ProcessSelect(ctrl, shift);
            DlcManager.Log("Select", $"left-click emp {emp.Id} ctrl={ctrl} shift={shift} → sel=[{string.Join(",", _selectedEmployees)}]");
            RefreshEmpListHighlights(row);

            void ProcessSelect(bool c, bool s)
            {
                if (c)
                {
                    if (_selectedEmployees.Contains(emp.Id)) _selectedEmployees.Remove(emp.Id);
                    else _selectedEmployees.Add(emp.Id);
                    _lastEmpClickIndex = empIdx;
                }
                else if (s && _lastEmpClickIndex >= 0)
                {
                    int start = Mathf.Min(_lastEmpClickIndex, empIdx);
                    int end = Mathf.Max(_lastEmpClickIndex, empIdx);
                    for (int i = start; i <= end && i < shiftSource.Count; i++)
                        _selectedEmployees.Add(shiftSource[i].Id);
                }
                else
                {
                    _selectedEmployees.Clear();
                    _selectedEmployees.Add(emp.Id);
                    _lastEmpClickIndex = empIdx;
                }
            }
        };
    }

    /// <summary>注册当前面板快捷键：source=Ctrl+A全选源, onSelChg=选择变化回调, onEnter=回车操作</summary>
    private void SetupEmpSelectAllKeys(Panel panel, List<Employee> source, Action onSelChanged = null, Action onEnter = null)
    {
        _activePanelEmpSource = source;
        _activePanelOnSelectChanged = onSelChanged;
        _activePanelEnterAction = onEnter;
        // 统一的全选回调（_Input 直接调用，不依赖事件路由）
        _activePanelSelectAllAction = source != null && source.Count > 0
            ? () =>
            {
                _selectedEmployees.Clear();
                foreach (var e in source) _selectedEmployees.Add(e.Id);
                _activePanelOnSelectChanged?.Invoke();
            }
            : null;
    }
    private bool _ctxMenuOpen;
    private void ApplyRowHover(Control pc, bool hovered)
    {
        if (hovered)
        {
            pc.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color(0.25f, 0.25f, 0.25f, 0.10f),
                BorderWidthLeft = 2, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
                BorderColor = new Color(0.35f, 0.35f, 0.35f, 0.25f),
                ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 2, ContentMarginBottom = 2,
            });
        }
        else
        {
            pc.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color(1, 1, 1, 0.01f),
                BorderWidthLeft = 2, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
                BorderColor = new Color(1, 1, 1, 0f),
                ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 2, ContentMarginBottom = 2,
            });
        }
    }

    private PanelContainer MakeTeamRow(Team team)
    {
        var pc = new PanelContainer();
        pc.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        pc.MouseFilter = Control.MouseFilterEnum.Stop;
        ApplyRowHover(pc, false);

        var hb = new HBoxContainer();
        hb.AddThemeConstantOverride("separation", (int)(UIScale * 4));
        hb.MouseFilter = Control.MouseFilterEnum.Ignore;
        pc.AddChild(hb);

        void AddL(string text, float w, int r, int g, int b)
        {
            var l = new Label { Text = text, CustomMinimumSize = new(w, 0) };
            l.AddThemeFontSizeOverride("font_size", 12);
            l.AddThemeColorOverride("font_color", new Color(r / 255f, g / 255f, b / 255f));
            l.MouseFilter = Control.MouseFilterEnum.Ignore;
            hb.AddChild(l);
        }

        float teamSalary = team.Members.Sum(m => m.Salary);
        string taskInfo = team.Task switch
        {
            TeamTask.DevelopGame => Loc.TrF("status.developing_game", team.CurrentProject?.Name ?? "???"),
            TeamTask.ResearchTech => Loc.TrF("status.researching", team.TargetTech?.Name ?? "???"),
            TeamTask.Outsource => Loc.TrF("status.outsourcing_n", team.CurrentContract?.Name ?? "?", team.OutsourceMonthsRemaining),
            TeamTask.Refactor => Loc.Tr("status.refactoring"),
            TeamTask.DevelopEngine => Loc.Tr("status.developing_engine"),
            _ => Loc.Tr("status.idle")
        };
        Color taskColor = team.Task == TeamTask.None ? new Color(0.15f, 0.5f, 0.15f) : new Color(0.55f, 0.35f, 0.1f);

        AddL($" {team.Name}", UIScale * 170, 8, 17, 28);
        AddL(Loc.TrF("ui.people_count", team.Members.Count), UIScale * 70, 20, 25, 30);
        AddL(Loc.TrF("ui.monthly", teamSalary), UIScale * 110, 13, 25, 30);
        AddL(taskInfo, UIScale * 200, (int)(taskColor.R * 255), (int)(taskColor.G * 255), (int)(taskColor.B * 255));

        // 双击打开团队员工管理
        Team tRef = team;
        float lastClickTime = 0;
        pc.GuiInput += (ie) =>
        {
            if (!(ie is InputEventMouseButton mb) || !mb.Pressed)
                return;
            if (mb.ButtonIndex == MouseButton.Right)
            {
                ShowTeamContextMenu(tRef);
                return;
            }
            if (mb.ButtonIndex != MouseButton.Left)
                return;
            float now = (float)Time.GetTicksMsec() / 1000f;
            if (now - lastClickTime < 0.5f)
                ShowTeamEmployeeDetail(tRef);
            lastClickTime = now;
        };

        // 悬停
        pc.MouseEntered += () => ApplyRowHover(pc, true);
        pc.MouseExited += () => ApplyRowHover(pc, false);

        return pc;
    }

    /// <summary>团队员工管理弹窗（双击团队打开）</summary>
    private void ShowTeamEmployeeDetail(Team team)
    {
        var S = (Func<float, float>)(v => v * UIScale);
        var p = MakePanel($"👥 {team.Name} - 员工管理");

        // 团队信息行
        float teamSalary = team.Members.Sum(m => m.Salary);
        string taskInfo = team.Task switch
        {
            TeamTask.None => Loc.Tr("status.idle"),
            TeamTask.DevelopGame => Loc.TrF("status.developing_game", team.CurrentProject?.Name ?? "???"),
            TeamTask.ResearchTech => Loc.Tr("status.researching_tech"),
            TeamTask.Refactor => Loc.Tr("status.refactoring"),
            TeamTask.DevelopEngine => Loc.Tr("status.developing_engine"),
            TeamTask.Outsource => Loc.Tr("status.outsourcing"),
            _ => Loc.Tr("status.busy")
        };
        var infoBar = new HBoxContainer { Position = new(S(20), S(8)) };
        infoBar.AddChild(MkPLabel(Loc.TrF("toast.team_info_status", taskInfo, team.GetChemistryBonus()*100f, teamSalary), 12, new Color(0.3f, 0.35f, 0.4f)));
        p.AddChild(infoBar);

        // 右键提示
        var hintLabel = MkPLabel(Loc.Tr("panel.all_emp_hint"), 10, new Color(0.55f, 0.58f, 0.6f));
        hintLabel.Position = new(S(22), S(28));
        p.AddChild(hintLabel);

        // 表头
        float y0 = S(54);
        var header = new HBoxContainer { Position = new(S(20), y0) };
        header.AddThemeConstantOverride("separation", (int)(S(4)));
        void AddH(string text, float w) { var l = MkPLabel(text, 12, new Color(0.08f, 0.12f, 0.2f)); l.CustomMinimumSize = new(w, 0); header.AddChild(l); }
        AddH(Loc.Tr("panel.col_employee"), S(130));
        AddH(Loc.Tr("panel.col_level"), S(50));
        AddH(Loc.Tr("panel.col_status"), S(55));
        AddH(Loc.Tr("panel.col_salary"), S(75));
        AddH(Loc.Tr("panel.col_fatigue"), S(60));
        AddH(Loc.Tr("panel.col_satisfaction"), S(60));
        p.AddChild(header);

        // 滚动列表
        float scrollH = p.Size.Y - y0 - S(100);
        var scroll = new ScrollContainer { Position = new(S(20), y0 + S(20)), Size = new(p.Size.X - S(40), scrollH) };
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        RegisterEmpList(list, team.Members);
        list.AddThemeConstantOverride("separation", (int)(S(2)));
        scroll.AddChild(list);
        p.AddChild(scroll);

        SetupEmpSelectAllKeys(p, team.Members);

        for (int ei = 0; ei < team.Members.Count; ei++)
        {
            var emp = team.Members[ei];
            int avgLv = emp.Skills.Count > 0 ? (int)emp.Skills.Values.Average(s => s.Level) : 0;
            Color fatigueC = emp.Fatigue > 70 ? new Color(0.9f, 0.3f, 0.2f) : emp.Fatigue > 40 ? new Color(0.9f, 0.6f, 0.2f) : new Color(0.3f, 0.7f, 0.3f);
            Color satC = emp.Satisfaction > 70 ? new Color(0.2f, 0.7f, 0.3f) : emp.Satisfaction > 40 ? new Color(0.9f, 0.6f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);
            string icon = emp.IsCaptain ? "👤" : "  ";
            string traitTag = emp.Trait != EmployeeTrait.None ? $" {GetTraitName(emp.Trait)}" : "";
            string status = emp.TrainingLeaveMonths > 0 ? $"培训中({emp.TrainingLeaveMonths}月)" : "-";

            var (pc, hb) = MakeEmpRowContainer(emp);
            void AddL(string text, float w, int r, int g, int b)
            {
                var l = new Label { Text = text, CustomMinimumSize = new(w, UIScale * 30) };
                l.AutowrapMode = TextServer.AutowrapMode.Word;
                l.AddThemeFontSizeOverride("font_size", 12);
                l.AddThemeColorOverride("font_color", new Color(r / 255f, g / 255f, b / 255f));
                l.MouseFilter = Control.MouseFilterEnum.Ignore;
                hb.AddChild(l);
            }

            AddL($"{icon} {Loc.DisplayName(emp.Name)}{traitTag}", S(130), 8, 17, 28);
            AddL(Loc.TrF("ui.lv", avgLv), S(50), 76, 128, 89);
            AddL(status, S(55), emp.TrainingLeaveMonths > 0 ? 200 : 120, emp.TrainingLeaveMonths > 0 ? 100 : 120, emp.TrainingLeaveMonths > 0 ? 40 : 120);
            AddL($"¥{emp.Salary:N0}", S(75), 38, 51, 76);
            AddL($"{emp.Fatigue:F0}%", S(60), (int)(fatigueC.R * 255), (int)(fatigueC.G * 255), (int)(fatigueC.B * 255));
            AddL($"{emp.Satisfaction:F0}%", S(60), (int)(satC.R * 255), (int)(satC.G * 255), (int)(satC.B * 255));

            AttachEmpClickHandler(pc, emp, ei, team.Members, team);
            list.AddChild(pc);
        }

        // 汇总行
        int eCnt = team.Members.Count;
        float avgFat = eCnt > 0 ? team.Members.Average(m => m.Fatigue) : 0;
        float avgS = eCnt > 0 ? team.Members.Average(m => m.Satisfaction) : 0;
        AddListSummary(list,
            new[] { Loc.TrF("ui.summary_emp", eCnt), "", "", $"¥{teamSalary:N0}", $"{avgFat:F0}%", $"{avgS:F0}%" },
            new[] { S(130), S(50), S(55), S(75), S(60), S(60) });

        // 底部操作
        float botY = p.Size.Y - S(58);
        var botBar = new HBoxContainer { Position = new(S(20), botY) };
        botBar.AddThemeConstantOverride("separation", (int)(S(12)));

        var backBtn = new Button { Text = Loc.Tr("panel.btn_back_team_list"), Flat = true };
        backBtn.AddThemeFontSizeOverride("font_size", 12);
        backBtn.AddThemeColorOverride("font_color", new Color(0f, 0f, 0f));
        backBtn.AddThemeColorOverride("font_hover_color", new Color(0.45f, 0.45f, 0.45f));
        backBtn.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.60f, 0.60f, 0.60f, 0.5f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        backBtn.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = new Color(0.18f, 0.18f, 0.18f, 0.75f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        backBtn.Pressed += () => ShowTeamPanel();
        botBar.AddChild(backBtn);

        var bbqBtn = new Button { Text = Loc.Tr("panel.btn_bbq"), Flat = true };
        bbqBtn.AddThemeFontSizeOverride("font_size", 12);
        bbqBtn.AddThemeColorOverride("font_color", new Color(0f, 0f, 0f));
        bbqBtn.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.60f, 0.60f, 0.60f, 0.5f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        bbqBtn.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = new Color(0.18f, 0.18f, 0.18f, 0.75f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        var tRef = team;
        bbqBtn.Pressed += () =>
        {
            if (_res.TeamBuilding(tRef))
            {
                ShowToast(Loc.Tr("toast.team_building_title"), Loc.TrF("toast.team_building_msg", tRef.Name), new Color(0.3f, 0.7f, 0.4f));
                CloseAll();
                ShowTeamPanel();
            }
            else ShowToast(Loc.Tr("toast.funds_low"), Loc.TrF("toast.funds_need", 20000), new Color(0.9f, 0.3f, 0.2f));
        };
        botBar.AddChild(bbqBtn);
        p.AddChild(botBar);
    }

    /// <summary>单击3D小人：关闭所有弹窗，打开员工菜单</summary>
    public void OnCharacterClicked(Employee emp)
    {
        // 关闭已有弹窗
        if (_openPopup != null) { _openPopup.Close(); _openPopup = null; }
        if (_openDevPopup != null) { _openDevPopup.Close(); _openDevPopup = null; }
        // 显示右键菜单（单员工操作）
        Team fromTeam = _teamMgr.Teams.Find(t => t.Members.Contains(emp));
        ShowEmployeeContextMenu(emp, fromTeam);
    }

    /// <summary>右键菜单：员工操作</summary>
    private void CloseExistingContextMenu()
    {
        var toFree = new System.Collections.Generic.List<PopupMenu>();
        foreach (var child in _uiLayer.GetChildren())
            if (child is PopupMenu pm && (pm.Name == "_ctxMenu" || pm.Name == "_batchMenu"))
                toFree.Add(pm);
        foreach (var pm in toFree)
        {
            pm.Hide();
            pm.QueueFree();
        }
    }

    private void ShowEmployeeContextMenu(Employee emp, Team fromTeam = null)
    {
        // 关闭已有右键菜单
        CloseExistingContextMenu();
        var menu = new PopupMenu();
        menu.Name = "_ctxMenu";

        menu.AddItem(Loc.TrF("menu.emp_title", Loc.DisplayName(emp.Name)));
        menu.SetItemDisabled(0, true);
        menu.AddSeparator();

        // 从团队移除
        if (fromTeam != null)
        {
            menu.AddItem(Loc.Tr("menu.remove_from_team"), 1);
        }

        // 分配团队
        menu.AddItem(Loc.Tr("menu.assign_team"), 2);

        // 设为队长
        if (fromTeam != null && !emp.IsCaptain)
        {
            menu.AddItem(Loc.Tr("menu.set_captain"), 3);
        }

        // 培训（距上次>=12个月）
        int currentAbsMonth = GameMonth + GameYear * 12;
        bool canTrain = emp.TrainingLeaveMonths <= 0 && (currentAbsMonth - emp.LastTrainAbsoluteMonth >= 12);
        int trainIdx = menu.ItemCount;
        if (canTrain)
            menu.AddItem(Loc.Tr("menu.train"), trainIdx);
        else if (emp.TrainingLeaveMonths > 0)
            menu.AddItem(Loc.TrF("menu.training_in_progress", emp.TrainingLeaveMonths), trainIdx);
        else
            menu.AddItem(Loc.TrF("menu.train_cooldown", 12 - (currentAbsMonth - emp.LastTrainAbsoluteMonth)), trainIdx);
        menu.SetItemDisabled(trainIdx, !canTrain);

        menu.AddSeparator();

        // 解雇
        menu.AddItem(Loc.Tr("menu.fire"), 5);
        menu.AddSeparator();
        menu.AddItem(Loc.Tr("menu.close"), 6);

        menu.Position = (Vector2I)(GetViewport().GetMousePosition());
        menu.Size = new Vector2I(180, 0);

        menu.IdPressed += (long id) =>
        {
            switch (id)
            {
                case 1: // 从团队移除
                    if (fromTeam != null)
                    {
                        _teamMgr.RemoveFromTeam(fromTeam, emp);
                        _refreshEmployeeList?.Invoke();
                        ShowToast("已移除", $"{Loc.DisplayName(emp.Name)} 已离开{fromTeam.Name}", new Color(0.7f, 0.5f, 0.2f));
                    }
                    break;
                case 2: // 分配团队
                    ShowAssignTeamPopup(emp);
                    break;
                case 3: // 设为队长
                    if (fromTeam != null)
                    {
                        if (fromTeam.Captain != null) fromTeam.Captain.IsCaptain = false;
                        emp.IsCaptain = true;
                        fromTeam.Captain = emp;
                        _refreshEmployeeList?.Invoke();
                        ShowToast(Loc.Tr("toast.captain_set"), Loc.TrF("toast.captain_msg", Loc.DisplayName(emp.Name), fromTeam.Name), new Color(0.3f, 0.7f, 0.4f));
                    }
                    break;
                case 4: // 培训
                    if (_res.SpendMoney(30000, "salary"))
                    {
                        emp.TrainingLeaveMonths = 1;
                        emp.LastTrainAbsoluteMonth = GameMonth + GameYear * 12;
                        foreach (var sk in emp.Skills.Keys.ToList())
                            emp.AddExp(sk, 30, true);
                        _refreshEmployeeList?.Invoke();
                        ShowToast(Loc.Tr("toast.training_start"), Loc.TrF("toast.training_msg", Loc.DisplayName(emp.Name)), new Color(0.3f, 0.7f, 0.5f));
                    }
                    else ShowToast(Loc.Tr("toast.funds_low"), Loc.TrF("toast.funds_need", 30000), new Color(0.9f, 0.3f, 0.2f));
                    break;
                case 5: // 解雇
                    _empMgr.FireEmployee(emp);
                    _roomMgr.RefreshEmployees();
                    _refreshEmployeeList?.Invoke();
                    ShowToast("已解雇", $"{Loc.DisplayName(emp.Name)} 已离开公司", new Color(0.8f, 0.2f, 0.2f));
                    break;
            }
            _ctxMenuOpen = false; menu.QueueFree();
        };
        menu.PopupHide += () => { _ctxMenuOpen = false; menu.QueueFree(); };

        _uiLayer.AddChild(menu);
        _ctxMenuOpen = true;
        menu.PopupHide += () => _ctxMenuOpen = false;
        menu.Popup();
    }

    /// <summary>分配团队弹窗</summary>
    private void ShowAssignTeamPopup(Employee emp)
    {
        var menu = new PopupMenu();
        menu.Name = "_assignMenu";

        menu.AddItem($"将 {Loc.DisplayName(emp.Name)} 分配到:");
        menu.SetItemDisabled(0, true);
        menu.AddSeparator();

        int id = 1;
        foreach (var team in _teamMgr.Teams)
        {
            menu.AddItem(Loc.TrF("ui.join_team_fmt", team.Name, team.Members.Count), id);
            id++;
        }
        menu.AddSeparator();
        menu.AddItem(Loc.Tr("ui.new_team_idle"), id);

        menu.Position = (Vector2I)(GetViewport().GetMousePosition());
        menu.Size = new Vector2I(220, 0);

        var teams = new List<Team>(_teamMgr.Teams);
        menu.IdPressed += (long sel) =>
        {
            int idx = (int)sel - 1;
            if (idx >= 0 && idx < teams.Count)
            {
                var targetTeam = teams[idx];
                _teamMgr.AddToTeam(targetTeam, emp);
                DlcManager.Log("Assign", $"assigned emp {emp.Id} to team, refresh={( _refreshEmployeeList == null ? "NULL" : "set")}");
                _refreshEmployeeList?.Invoke();
                ShowToast(Loc.Tr("toast.assign_ok"), Loc.TrF("toast.assign_msg", Loc.DisplayName(emp.Name), targetTeam.Name), new Color(0.3f, 0.7f, 0.4f));
            }
            else if (idx == teams.Count)
            {
                // 新建团队（仅从空闲员工中创建，不带走已在团队中的员工）
                var idle = _teamMgr.GetIdleEmployees();
                var members = idle.Take(Mathf.Min(4, idle.Count)).ToList();
                var captain = members.FirstOrDefault(e => e.CanMentor);
                var newTeam = _teamMgr.CreateTeam(Loc.TrF("ui.team_fmt", _teamMgr.Teams.Count + 1), members, captain);
                ShowToast(Loc.Tr("toast.team_created"), Loc.TrF("toast.team_created_msg", newTeam.Name, members.Count), new Color(0.3f, 0.7f, 0.5f));
            }
            menu.QueueFree();
        };
        menu.PopupHide += () => menu.QueueFree();

        _uiLayer.AddChild(menu);
        menu.Popup();
    }

    /// <summary>团队右键菜单</summary>
    private void ShowTeamContextMenu(Team team)
    {
        var menu = new PopupMenu();
        menu.Name = "_teamCtxMenu";

        menu.AddItem(Loc.TrF("menu.team_title", team.Name));
        menu.SetItemDisabled(0, true);
        menu.AddSeparator();

        menu.AddItem(Loc.Tr("menu.view_employees"), 1);
        menu.AddItem(Loc.Tr("menu.rename"), 2);
        menu.AddSeparator();

        int trainable = team.Members.Count(e => e.TrainingLeaveMonths <= 0 && (GameMonth + GameYear * 12 - e.LastTrainAbsoluteMonth >= 12));
        menu.AddItem(Loc.TrF("menu.mass_train", 30000 * trainable), 3);
        if (trainable == 0) menu.SetItemDisabled(3, true);
        menu.AddSeparator();

        menu.AddItem(Loc.Tr("menu.disband"), 4);
        menu.AddItem(Loc.Tr("menu.close"), 5);

        menu.Position = (Vector2I)(GetViewport().GetMousePosition());
        menu.Size = new Vector2I(200, 0);

        var tRef = team;
        menu.IdPressed += (long id) =>
        {
            switch (id)
            {
                case 1: ShowTeamEmployeeDetail(tRef); break;
                case 2: ShowRenameTeamDialog(tRef); break;
                case 3:
                    int cost = 0;
                    foreach (var e in tRef.Members.Where(e => e.TrainingLeaveMonths <= 0 && (GameMonth + GameYear * 12 - e.LastTrainAbsoluteMonth >= 12)).ToList())
                    {
                        if (_res.SpendMoney(30000, "salary"))
                        {
                            e.TrainingLeaveMonths = 1;
                            e.LastTrainAbsoluteMonth = GameMonth + GameYear * 12;
                            foreach (var sk in e.Skills.Keys.ToList()) e.AddExp(sk, 30, true);
                            cost += 30000;
                        }
                    }
                    ShowToast(Loc.Tr("toast.group_train"), Loc.TrF("toast.group_train_msg", cost, tRef.Name), new Color(0.3f, 0.7f, 0.5f));
                    CloseAll(); ShowTeamPanel();
                    break;
                case 4:
                    _teamMgr.DisbandTeam(tRef);
                    ShowToast(Loc.Tr("toast.team_disbanded"), tRef.Name, new Color(0.8f, 0.3f, 0.2f));
                    CloseAll(); ShowTeamPanel();
                    break;
            }
            menu.QueueFree();
        };
        menu.PopupHide += () => menu.QueueFree();
        _uiLayer.AddChild(menu);
        menu.Popup();
    }

    private void ShowRenameTeamDialog(Team team)
    {
        var vp = GetViewport().GetVisibleRect().Size;
        float pw = 360, ph = 160;
        var dlg = new DragPanel { Position = new((vp.X - pw) / 2, (vp.Y - ph) / 2), Size = new(pw, ph) };
        dlg.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f, 0.98f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.3f, 0.3f, 0.3f, 0.3f) });

        var tb = new LineEdit { PlaceholderText = Loc.Tr("dlg.rename_placeholder"), Text = team.Name, Position = new(20, 20), Size = new(pw - 40, 36) };
        dlg.AddChild(tb);

        var btnH = new HBoxContainer { Position = new(20, 70) };
        var okBtn = new Button { Text = Loc.Tr("dlg.confirm") };
        okBtn.Pressed += () => { if (!string.IsNullOrWhiteSpace(tb.Text)) { team.Name = tb.Text; ShowToast(Loc.Tr("toast.team_rename_ok"), Loc.TrF("toast.team_rename_msg", team.Name), new Color(0.3f, 0.7f, 0.4f)); dlg.QueueFree(); CloseAll(); ShowTeamPanel(); } };
        btnH.AddChild(okBtn);
        var cancelBtn = new Button { Text = Loc.Tr("dlg.cancel") };
        cancelBtn.Pressed += () => dlg.QueueFree();
        btnH.AddChild(cancelBtn);
        dlg.AddChild(btnH);
        _uiLayer.AddChild(dlg);
    }

    /// <summary>批量操作菜单（多选员工时）</summary>
    private void ShowBatchEmployeeMenu()
    {
        CloseExistingContextMenu();
        var employees = _empMgr.Employees.Where(e => _selectedEmployees.Contains(e.Id)).ToList();
        if (employees.Count == 0) return;

        var menu = new PopupMenu();
        menu.Name = "_batchMenu";
        menu.AddItem($"批量操作 ({employees.Count}人)");
        menu.SetItemDisabled(0, true);
        menu.AddSeparator();

        int trainable = employees.Count(e => e.TrainingLeaveMonths <= 0 && (GameMonth + GameYear * 12 - e.LastTrainAbsoluteMonth >= 12));
        menu.AddItem(Loc.TrF("menu.batch_train_item", trainable, trainable * 30000), 1);
        if (trainable == 0) menu.SetItemDisabled(1, true);
        menu.AddSeparator();

        menu.AddItem(Loc.Tr("menu.batch_assign_item"), 2);
        menu.AddItem(Loc.Tr("menu.batch_fire_item"), 3);
        menu.AddItem(Loc.Tr("menu.close"), 4);

        menu.Position = (Vector2I)(GetViewport().GetMousePosition());
        menu.Size = new Vector2I(240, 0);

        menu.IdPressed += (long id) =>
        {
            switch (id)
            {
                case 1: // 批量培训
                    int cost = 0, done = 0;
                    foreach (var e in employees.Where(e => e.TrainingLeaveMonths <= 0 && (GameMonth + GameYear * 12 - e.LastTrainAbsoluteMonth >= 12)))
                    {
                        if (_res.SpendMoney(30000, "salary"))
                        {
                            e.TrainingLeaveMonths = 1;
                            e.LastTrainAbsoluteMonth = GameMonth + GameYear * 12;
                            foreach (var sk in e.Skills.Keys.ToList()) e.AddExp(sk, 30, true);
                            done++;
                            cost += 30000;
                        }
                        else break;
                    }
                    ShowToast(Loc.Tr("toast.batch_train"), Loc.TrF("toast.batch_train_msg", done, cost), new Color(0.3f, 0.7f, 0.5f));
                    _selectedEmployees.Clear();
                    CloseAll(); ShowTeamPanel();
                    break;
                case 2: // 分配团队
                    ShowBatchAssignPopup(employees);
                    break;
                case 3: // 解雇全部
                    foreach (var e in employees)
                    {
                        _empMgr.FireEmployee(e);
                        _selectedEmployees.Remove(e.Id);
                    }
                    _roomMgr.RefreshEmployees();
                    ShowToast(Loc.Tr("toast.batch_fire"), Loc.TrF("toast.batch_fire_msg", employees.Count), new Color(0.8f, 0.2f, 0.2f));
                    CloseAll(); ShowTeamPanel();
                    break;
            }
            _ctxMenuOpen = false; menu.QueueFree();
        };
        _ctxMenuOpen = true;
        menu.PopupHide += () => { _ctxMenuOpen = false; menu.QueueFree(); };
        _uiLayer.AddChild(menu);
        menu.Popup();
    }

    private void ShowBatchAssignPopup(List<Employee> emps)
    {
        var menu = new PopupMenu();
        menu.AddItem(Loc.TrF("menu.assign_batch_to", emps.Count));
        menu.SetItemDisabled(0, true);
        menu.AddSeparator();

        int id = 1;
        foreach (var team in _teamMgr.Teams)
        {
            menu.AddItem(Loc.TrF("menu.join_team", team.Name, team.Members.Count), id++);
        }
        menu.AddSeparator();
        menu.AddItem(Loc.Tr("menu.new_team"), id);

        var teams = new List<Team>(_teamMgr.Teams);
        menu.Position = (Vector2I)(GetViewport().GetMousePosition());
        menu.Size = new Vector2I(220, 0);

        menu.IdPressed += (long sel) =>
        {
            int idx = (int)sel - 1;
            if (idx >= 0 && idx < teams.Count)
            {
                var t = teams[idx];
                foreach (var e in emps) _teamMgr.AddToTeam(t, e);
                _refreshEmployeeList?.Invoke();
                ShowToast(Loc.Tr("toast.batch_assign"), Loc.TrF("toast.batch_assign_msg", emps.Count, t.Name), new Color(0.3f, 0.7f, 0.4f));
            }
            else if (idx == teams.Count)
            {
                var captain = emps.FirstOrDefault(e => e.CanMentor);
                var newTeam = _teamMgr.CreateTeam(Loc.TrF("ui.team_fmt", _teamMgr.Teams.Count + 1), emps, captain);
                _refreshEmployeeList?.Invoke();
                ShowToast(Loc.Tr("toast.team_created"), Loc.TrF("toast.team_created_msg", newTeam.Name, emps.Count), new Color(0.3f, 0.7f, 0.5f));
            }
            _selectedEmployees.Clear();
            menu.QueueFree();
        };
        menu.PopupHide += () => menu.QueueFree();
        _uiLayer.AddChild(menu);
        menu.Popup();
    }

    /// <summary>查看所有员工面板</summary>
    private void ShowAllEmployeesPanel()
    {
        var p = MakePanel(Loc.Tr("panel.all_employees_title"));

        // 右键提示
        var hintLabel = MkPLabel(Loc.Tr("panel.all_emp_hint"), 10, new Color(0.55f, 0.58f, 0.6f));
        hintLabel.Position = new(UIScale * 22, UIScale * 30);
        p.AddChild(hintLabel);

        float y0 = UIScale * 50;
        var header = new HBoxContainer { Position = new(UIScale * 20, y0) };
        header.AddThemeConstantOverride("separation", (int)(UIScale * 4));
        void AddHCol(string label, float w)
        {
            var l = MkPLabel(label, 12, new Color(0.08f, 0.12f, 0.2f));
            l.CustomMinimumSize = new(w, 0);
            header.AddChild(l);
        }
        AddHCol(" "+Loc.Tr("panel.col_employee"), UIScale * 135);
        AddHCol(Loc.Tr("panel.col_level"), UIScale * 50);
        AddHCol(Loc.Tr("panel.col_status"), UIScale * 55);
        AddHCol(Loc.Tr("panel.col_team"), UIScale * 85);
        AddHCol(Loc.Tr("panel.col_salary"), UIScale * 80);
        AddHCol(Loc.Tr("panel.col_fatigue"), UIScale * 60);
        AddHCol(Loc.Tr("panel.col_satisfaction"), UIScale * 60);
        p.AddChild(header);

        var scroll = new ScrollContainer { Position = new(UIScale * 20, y0 + UIScale * 20), Size = new(p.Size.X - UIScale * 40, p.Size.Y - y0 - UIScale * 90) };
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        var list = new VBoxContainer();
        RegisterEmpList(list, _empMgr.Employees);
        list.AddThemeConstantOverride("separation", (int)(UIScale * 2));
        scroll.AddChild(list);
        p.AddChild(scroll);

        SetupEmpSelectAllKeys(p, _empMgr.Employees);
        _refreshEmployeeList = () =>
        {
            DlcManager.Log("Refresh", "queuing AllEmployeesPanel rebuild via timer");
            var t = new Timer { WaitTime = 0.01f, OneShot = true };
            AddChild(t);
            t.Timeout += () => { DlcManager.Log("Refresh", "timer fired, rebuilding AllEmployeesPanel"); _lastEmpClickIndex = -1; CloseAll(); ShowAllEmployeesPanel(); t.QueueFree(); };
            t.Start();
        };

        if (_empMgr.Employees.Count == 0)
        {
            list.AddChild(MkPLabel(Loc.Tr("panel.all_emp_hint"), 13, new Color(0.6f, 0.3f, 0.2f)));
        }
        else for (int ei = 0; ei < _empMgr.Employees.Count; ei++)
        {
            var emp = _empMgr.Employees[ei];
            int avgLv = emp.Skills.Count > 0 ? (int)emp.Skills.Values.Average(s => s.Level) : 0;
            Color fatigueC = emp.Fatigue > 70 ? new Color(0.9f, 0.3f, 0.2f) : emp.Fatigue > 40 ? new Color(0.9f, 0.6f, 0.2f) : new Color(0.3f, 0.7f, 0.3f);
            Color satC = emp.Satisfaction > 70 ? new Color(0.2f, 0.7f, 0.3f) : emp.Satisfaction > 40 ? new Color(0.9f, 0.6f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);
            string tags = emp.IsCaptain ? "👤" : emp.IsChiefArchitect ? "★" : "";
            string traitText = emp.Trait != EmployeeTrait.None ? $" {GetTraitName(emp.Trait)}" : "";
            string status = emp.TrainingLeaveMonths > 0 ? Loc.TrF("status.training", emp.TrainingLeaveMonths) : Loc.Tr("status.none");

            var (pc, hb) = MakeEmpRowContainer(emp);
            void AddL(string text, float w, int r, int g, int b)
            {
                var l = new Label { Text = text, CustomMinimumSize = new(w, UIScale * 30) };
                l.AutowrapMode = TextServer.AutowrapMode.Word;
                l.AddThemeFontSizeOverride("font_size", 12);
                l.AddThemeColorOverride("font_color", new Color(r / 255f, g / 255f, b / 255f));
                l.MouseFilter = Control.MouseFilterEnum.Ignore;
                hb.AddChild(l);
            }
            AddL($" {tags} {Loc.DisplayName(emp.Name)}{traitText}", UIScale * 135, 8, 17, 28);
            AddL(Loc.TrF("ui.lv", avgLv), UIScale * 50, 30, 50, 35);
            AddL(status, UIScale * 55, emp.TrainingLeaveMonths > 0 ? 200 : 120, emp.TrainingLeaveMonths > 0 ? 100 : 120, emp.TrainingLeaveMonths > 0 ? 40 : 120);
            AddL(emp.TeamName ?? "-", UIScale * 85, 18, 22, 30);
            AddL($"¥{emp.Salary:N0}", UIScale * 80, 15, 25, 30);
            AddL($"{emp.Fatigue:F0}%", UIScale * 60, (int)(fatigueC.R * 255), (int)(fatigueC.G * 255), (int)(fatigueC.B * 255));
            AddL($"{emp.Satisfaction:F0}%", UIScale * 60, (int)(satC.R * 255), (int)(satC.G * 255), (int)(satC.B * 255));

            AttachEmpClickHandler(pc, emp, ei, _empMgr.Employees, _teamMgr.Teams.Find(x => x.Name == emp.TeamName));
            list.AddChild(pc);
        }

        // 底部操作栏（含批量操作按钮）
        float botY = p.Size.Y - UIScale * 55;
        var backBtn = new Button { Text = "← 返回", Flat = true };
        backBtn.Position = new(UIScale * 20, botY);
        backBtn.AddThemeFontSizeOverride("font_size", 12);
        backBtn.AddThemeColorOverride("font_color", new Color(0f, 0f, 0f));
        backBtn.AddThemeColorOverride("font_hover_color", new Color(0.45f, 0.45f, 0.45f));
        backBtn.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.60f, 0.60f, 0.60f, 0.5f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        backBtn.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = new Color(0.18f, 0.18f, 0.18f, 0.75f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        backBtn.Pressed += () => ShowTeamPanel();
        p.AddChild(backBtn);

        if (_selectedEmployees.Count > 1)
        {
            var batchBtn = MkPButton(Loc.TrF("ui.batch_op_fmt", _selectedEmployees.Count), 150, 32);
            batchBtn.Position = new(UIScale * 155, botY);
            batchBtn.Pressed += () => ShowBatchEmployeeMenu();
            p.AddChild(batchBtn);
        }
        else
        {
            var recruitBtn = MkPButton(Loc.Tr("panel.recruit_btn"), 120, 32);
            recruitBtn.Position = new(UIScale * 155, botY);
            recruitBtn.Pressed += () =>
            {
                if (_empMgr.Employees.Count >= _roomMgr.TotalCapacity)
                {
                    ShowPopup(Loc.Tr("toast.room_full"), Loc.TrF("ui.employee_cap", _roomMgr.TotalCapacity, _empMgr.Employees.Count), new Color(0.9f, 0.3f, 0.2f));
                    return;
                }
                ShowRecruitPanel();
            };
            p.AddChild(recruitBtn);
        }

        // 汇总行在列表末尾
        int totalEmp = _empMgr.Employees.Count;
        float totalSalary = _empMgr.Employees.Sum(e => e.Salary);
        float avgFatigue = totalEmp > 0 ? _empMgr.Employees.Average(e => e.Fatigue) : 0;
        float avgSat = totalEmp > 0 ? _empMgr.Employees.Average(e => e.Satisfaction) : 0;
        var sumPc = new PanelContainer();
        sumPc.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        sumPc.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.92f, 0.90f, 0.86f, 0.6f), BorderWidthTop = 1, BorderColor = new Color(0.4f, 0.45f, 0.5f, 0.3f) });
        var sumHb = new HBoxContainer();
        sumHb.AddThemeConstantOverride("separation", (int)(UIScale * 4));
        void AddSumL(string text, float w, int r, int g, int b)
        {
            var l = new Label { Text = text, CustomMinimumSize = new(w, 0) };
            l.AddThemeFontSizeOverride("font_size", 12);
            l.AddThemeColorOverride("font_color", new Color(r / 255f, g / 255f, b / 255f));
            l.MouseFilter = Control.MouseFilterEnum.Ignore;
            sumHb.AddChild(l);
        }
        AddSumL(Loc.TrF("ui.summary_fmt", Loc.TrF("ui.people_count", totalEmp)), UIScale * 135, 20, 20, 25);
        AddSumL($"  {GetTraitName(EmployeeTrait.None)}", UIScale * 50, 20, 20, 25);
        AddSumL("", UIScale * 55, 20, 20, 25);
        AddSumL("", UIScale * 85, 20, 20, 25);
        AddSumL(Loc.TrF("ui.monthly", totalSalary), UIScale * 80, 15, 55, 20);
        AddSumL($"{avgFatigue:F0}%", UIScale * 60, 18, 18, 25);
        AddSumL($"{avgSat:F0}%", UIScale * 60, 18, 18, 25);
        sumPc.AddChild(sumHb);
        list.AddChild(sumPc);
    }

    /// <summary>招聘面板：多选雇佣，回车批量确认，不关闭面板</summary>
    private void ShowRecruitPanel()
    {
        // 每月刷新一次候选池
        if (_lastRecruitMonth != GameMonth + GameYear * 12)
        {
            _recruitCandidates.Clear();
            int count = 6;
            for (int i = 0; i < count; i++)
                _recruitCandidates.Add(_empMgr.GenerateRecruit());
            _lastRecruitMonth = GameMonth + GameYear * 12;
        }

        // 如果池子空了（全被雇佣了），重新生成
        if (_recruitCandidates.Count == 0)
        {
            int count = 6;
            for (int i = 0; i < count; i++)
                _recruitCandidates.Add(_empMgr.GenerateRecruit());
        }

        float S(float v) => v * UIScale;
        var p = MakePanel(Loc.Tr("panel.recruit_title"));

        int capacity = _roomMgr.TotalCapacity;
        int occupied = _empMgr.Employees.Count;
        int free = capacity - occupied;

        var topBar = new HBoxContainer { Position = new(UIScale * 20, UIScale * 8) };
        string capText = free > 0 ? Loc.TrF("panel.recruit_hint", free, capacity, _recruitCandidates.Count)
                                  : Loc.TrF("panel.recruit_full", occupied, capacity);
        topBar.AddChild(MkPLabel(capText, 11, free > 0 ? new Color(0.5f, 0.55f, 0.6f) : new Color(0.9f, 0.3f, 0.2f)));
        p.AddChild(topBar);

        // 表头
        float y0 = UIScale * 40;
        var header = new HBoxContainer { Position = new(UIScale * 20, y0) };
        header.AddThemeConstantOverride("separation", (int)(UIScale * 4));
        void AddH(string text, float w) { var l = MkPLabel(text, 12, new Color(0.08f, 0.12f, 0.2f)); l.CustomMinimumSize = new(w, 0); header.AddChild(l); }
        AddH(" "+Loc.Tr("panel.col_name"), S(130));
        AddH(Loc.Tr("panel.col_skills"), S(170));
        AddH(Loc.Tr("panel.col_trait"), S(100));
        AddH(Loc.Tr("panel.col_salary"), S(70));
        AddH(Loc.Tr("panel.col_hire_fee"), S(70));
        p.AddChild(header);

        // 滚动列表
        var scroll = new ScrollContainer { Position = new(UIScale * 20, y0 + UIScale * 20), Size = new(p.Size.X - UIScale * 40, p.Size.Y - y0 - UIScale * 95) };
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        RegisterEmpList(list, _recruitCandidates);
        list.AddThemeConstantOverride("separation", (int)(UIScale * 2));
        scroll.AddChild(list);
        p.AddChild(scroll);

        // 底部操作栏
        float botY = p.Size.Y - UIScale * 58;
        var botBar = new HBoxContainer { Position = new(UIScale * 20, botY) };
        botBar.AddThemeConstantOverride("separation", (int)(UIScale * 12));

        var backBtn = new Button { Text = Loc.Tr("panel.btn_back"), Flat = true };
        backBtn.AddThemeFontSizeOverride("font_size", 12);
        backBtn.AddThemeColorOverride("font_color", new Color(0f, 0f, 0f));
        backBtn.AddThemeColorOverride("font_hover_color", new Color(0.45f, 0.45f, 0.45f));
        backBtn.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.60f, 0.60f, 0.60f, 0.5f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        backBtn.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = new Color(0.18f, 0.18f, 0.18f, 0.75f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        backBtn.Pressed += () => ShowTeamPanel();
        botBar.AddChild(backBtn);

        // 批量雇佣按钮（提前声明以便UpdateBatchBtn和Ctrl+A使用）
        Button batchHireBtn = MkPButton(Loc.Tr("panel.btn_hire0"), 160, 32);
        void UpdateBatchBtn()
        {
            int cnt = _recruitCandidates.Count(c => _selectedEmployees.Contains(c.Id));
            float totalCost = _recruitCandidates.Where(c => _selectedEmployees.Contains(c.Id)).Sum(c => c.GetHighestLevel() switch { 1 => 5000, 2 => 20000, 3 => 80000, 4 => 300000, _ => 5000 });
            batchHireBtn.Text = cnt > 0 ? Loc.TrF("panel.btn_hire", cnt, totalCost) : Loc.Tr("panel.btn_hire0");
        }
        batchHireBtn.Pressed += () =>
        {
            int available = _roomMgr.TotalCapacity - _empMgr.Employees.Count;
            if (available <= 0)
            {
                ShowToast(Loc.Tr("toast.room_full"), Loc.Tr("toast.room_full_msg"), new Color(0.9f, 0.3f, 0.2f));
                return;
            }
            int hired = 0;
            float totalCost = 0;
            foreach (var e in _recruitCandidates.ToList())
            {
                if (!_selectedEmployees.Contains(e.Id)) continue;
                if (_empMgr.Employees.Count >= _roomMgr.TotalCapacity) break;
                float hc = e.GetHighestLevel() switch { 1 => 5000, 2 => 20000, 3 => 80000, 4 => 300000, _ => 5000 };
                if (_res.SpendMoney(hc, "salary"))
                {
                    e.TeamName = null;
                    _empMgr.Employees.Add(e);
                    _res.TotalEmployees = _empMgr.Employees.Count;
                    _recruitCandidates.Remove(e);
                    _selectedEmployees.Remove(e.Id);
                    _roomMgr.RefreshEmployees();
                    hired++;
                    totalCost += hc;
                }
                else break;
            }
            if (hired > 0) ShowToast(Loc.Tr("toast.batch_hire"), Loc.TrF("toast.batch_hire_msg", hired, totalCost), new Color(0.3f, 0.7f, 0.5f));
            UpdateBatchBtn();
            CloseAll();
            ShowAllEmployeesPanel();
        };
        botBar.AddChild(batchHireBtn);

        var refreshBtn = MkPButton(Loc.Tr("panel.btn_refresh"), 140, 32);
        refreshBtn.Pressed += () =>
        {
            if (_res.SpendMoney(2000, "salary"))
            {
                _recruitCandidates.Clear();
                _selectedEmployees.Clear();
                int cnt = 6;
                for (int i = 0; i < cnt; i++)
                    _recruitCandidates.Add(_empMgr.GenerateRecruit());
                _lastRecruitMonth = GameMonth + GameYear * 12;
                CloseAll();
                ShowRecruitPanel();
            }
            else ShowToast(Loc.Tr("toast.funds_low"), Loc.TrF("toast.funds_need", 2000), new Color(0.9f, 0.3f, 0.2f));
        };
        botBar.AddChild(refreshBtn);
        p.AddChild(botBar);

        // 注册面板快捷键：Ctrl+A 全选 + Enter 批量雇佣，Ctrl+A后更新按钮文字
        SetupEmpSelectAllKeys(p, _recruitCandidates, UpdateBatchBtn, () => batchHireBtn.EmitSignal("pressed"));

        // 生成行
        if (_recruitCandidates.Count == 0)
        {
            list.AddChild(MkPLabel(Loc.Tr("panel.recruit_empty"), 13, new Color(0.6f, 0.3f, 0.2f)));
        }
        else for (int ci = 0; ci < _recruitCandidates.Count; ci++)
        {
            var emp = _recruitCandidates[ci];
            string skills = string.Join("、", emp.Skills.OrderByDescending(s => s.Value.Level).Select(kv => $"{kv.Key.Name()}Lv{kv.Value.Level}"));
            if (string.IsNullOrEmpty(skills)) skills = Loc.Tr("status.none");
            string traitText = emp.Trait != EmployeeTrait.None ? GetTraitName(emp.Trait) : "-";
            string mentorTag = emp.CanMentor ? $" 🎓{Loc.Tr("status.can_mentor")}" : "";

            var (pc, hb) = MakeEmpRowContainer(emp);
            void AddL(string text, float w, int r, int g, int b)
            {
                var l = new Label { Text = text, CustomMinimumSize = new(w, UIScale * 30) };
                l.AutowrapMode = TextServer.AutowrapMode.Word;
                l.AddThemeFontSizeOverride("font_size", 12);
                l.AddThemeColorOverride("font_color", new Color(r / 255f, g / 255f, b / 255f));
                l.MouseFilter = Control.MouseFilterEnum.Ignore;
                hb.AddChild(l);
            }
            float hireCost = emp.GetHighestLevel() switch { 1 => 5000, 2 => 20000, 3 => 80000, 4 => 300000, _ => 5000 };
            AddL($"{Loc.DisplayName(emp.Name)}{mentorTag}", S(130), 8, 17, 28);
            AddL(skills, S(170), 76, 128, 153);
            AddL(traitText, S(100), 128, 100, 60);
            AddL(Loc.TrF("ui.monthly", emp.Salary), S(70), 18, 22, 30);
            AddL($"¥{hireCost:N0}", S(70), 200, 100, 20);

            // 点击多选（使用统一 helper）
            AttachEmpClickHandler(pc, emp, ci, _recruitCandidates);
            // 点击后更新批量按钮
            pc.GuiInput += (ie) =>
            {
                if (ie is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                    UpdateBatchBtn();
            };

            list.AddChild(pc);
        }
    }

    private void ShowEnginePanel()
    {
        var p = MakePanel(Loc.Tr("panel.engine_business"));
        var sc = AddScroll(p);
        var c = new VBoxContainer(); sc.AddChild(c);

        c.AddChild(MkPLabel(Loc.TrF("engine.current_model", _techMgr.EngineModel.Name()), 15, new Color(0.3f, 0.9f, 1.0f)));
        c.AddChild(MkPLabel(Loc.TrF("engine.license_info", _techMgr.EngineLicenseCount, _techMgr.EngineMarketShare, _techMgr.EngineReputation), 13, new Color(0.6f, 0.7f, 0.8f)));

        foreach (EngineBizModel m in Enum.GetValues(typeof(EngineBizModel)))
        {
            var model = (EngineBizModel)m;
            if (model == EngineBizModel.Closed) continue;
            var btn = MkPButton(Loc.TrF("ui.set_mode", model.Name()), 200, 32);
            btn.Pressed += () => { _techMgr.SetEngineModel(model); ShowEnginePanel(); };
            c.AddChild(btn);
        }
        var clsBtn = MkPButton(Loc.Tr("eng.close_exclusive"), 220, 32);
        clsBtn.Pressed += () => { _techMgr.SetEngineModel(EngineBizModel.Closed); ShowEnginePanel(); };
        c.AddChild(clsBtn);
    }

    private void ShowRoomPanel()
    {
        var p = MakePanel(Loc.Tr("panel.room_manage"));
        var sc = AddScroll(p);
        var c = new VBoxContainer(); sc.AddChild(c);

        var houseInfo = HouseData.Data[_roomMgr.CurrentTier];
        var curTierName = _roomMgr.CurrentTier.Name();
        c.AddChild(MkPLabel(Loc.TrF("ui.office_status", curTierName, houseInfo.Capacity, houseInfo.MonthlyRent), 15, new Color(0.3f, 0.8f, 1.0f)));
        c.AddChild(MkPLabel(Loc.TrF("ui.room_status", _roomMgr.TotalCapacity, _empMgr.Employees.Count, _roomMgr.TotalMonthlyRent), 13, new Color(0.6f, 0.7f, 0.8f)));

        c.AddChild(MkPLabel("", 6, Colors.White));
        c.AddChild(MkPLabel(Loc.Tr("panel.room_move"), 16, new Color(0.10f, 0.14f, 0.22f)));
        if (HouseData.CanUpgrade(_roomMgr.CurrentTier))
        {
            var nextTier = HouseData.NextTier(_roomMgr.CurrentTier);
            var nextInfo = HouseData.Data[nextTier];
            var moveBtn = MkPButton(Loc.TrF("house.move_fmt", nextTier.Name(), nextInfo.Capacity, nextInfo.MonthlyRent, nextInfo.MoveCost), 480, 30);
            moveBtn.Pressed += () =>
            {
                if (_roomMgr.MoveToBiggerHouse()) { ShowRoomPanel(); }
            };
            c.AddChild(moveBtn);
        }
        else
        {
            c.AddChild(MkPLabel(Loc.Tr("house.max_tier"), 13, new Color(0.5f, 0.7f, 0.5f)));
        }

        c.AddChild(MkPLabel("", 6, Colors.White));
        c.AddChild(MkPLabel(Loc.Tr("ui.buy_room"), 16, new Color(0.10f, 0.14f, 0.22f)));
        foreach (var kv in BonusRoomData.Data)
        {
            bool owned = _roomMgr.PurchasedBonusRooms.Contains(kv.Key);
            string btnText = owned
                ? Loc.TrF("room.owned_fmt", kv.Key.Name(), kv.Key.BonusDesc())
                : Loc.TrF("room.buy_fmt", kv.Key.Name(), kv.Key.BonusDesc(), kv.Value.Capacity, kv.Value.MonthlyRent, kv.Value.Cost);
            var btn = MkPButton(btnText, 480, 30);
            if (owned) btn.Disabled = true;
            else
            {
                var t = kv.Key;
                btn.Pressed += () => { if (_roomMgr.BuyBonusRoom(t)) { ShowRoomPanel(); } };
            }
            c.AddChild(btn);
        }
    }

    /// <summary>联机游戏比例（0~1，取决于已发售游戏中含联机组件的比例）</summary>
    public float GetOnlineGameRatio()
    {
        var devMgr = GetNode<GameDevManager>("GameDevManager");
        var active = devMgr.CompletedProjects.Where(p => p.IsReleased).ToList();
        if (active.Count == 0) return 0;
        int onlineCount = active.Count(p => p.NetworkScore > 30 || p.GameplayScore > 70);
        return Mathf.Clamp((float)onlineCount / active.Count, 0, 1);
    }

    // ═══════════════════════════════════════════════
    //  🎮 开发中心面板
    // ═══════════════════════════════════════════════
    private void ShowDevCenterPanel()
    {
        var p = MakePanel("🎮 " + Loc.Tr("hud.dev"));
        var sc = AddScroll(p);
        var c = new VBoxContainer(); sc.AddChild(c);

        // 操作按钮
        var btns = new HBoxContainer();
        var newGameBtn = MkPButton(Loc.Tr("dev.game_management"), 150, 32);
        newGameBtn.AddThemeColorOverride("font_color", new Color(0.3f, 0.9f, 0.3f));
        newGameBtn.Pressed += () => {
            CloseAll();
            ShowProjectPanel();
        };
        btns.AddChild(newGameBtn);

        var engineBtn = MkPButton(Loc.Tr("dev.engine_manage"), 150, 32);
        engineBtn.AddThemeColorOverride("font_color", new Color(0.5f, 0.7f, 1f));
        engineBtn.Pressed += () => {
            CloseAll();
            var menu = new DevMenu(this);
            _openPopup = menu;
            menu.ShowEnginePage();
        };
        btns.AddChild(engineBtn);
        c.AddChild(btns);
        c.AddChild(MkPLabel("", 8, Colors.White));

        // 正在开发的项目
        var devMgr2 = GetNode<GameDevManager>("GameDevManager");
        c.AddChild(MkPLabel(Loc.Tr("dev.dev_in_progress"), 15, new Color(0.3f, 0.8f, 0.4f)));
        var active = devMgr2?.Projects.FindAll(p => !p.IsReleased) ?? new();
        if (active.Count == 0)
            c.AddChild(MkPLabel($"  {Loc.Tr("dev.no_active")}", 12, new Color(0.5f, 0.5f, 0.5f)));
        else
            foreach (var proj in active)
            {
                var row = new HBoxContainer();
                row.AddChild(MkPLabel($"  🎯 {proj.Name}  {proj.Genre}×{proj.Theme}  {Loc.Tr("ui.progress")}: {proj.DevProgress * 100:F0}%", 12, proj.FinalScore > 0 ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.08f, 0.12f, 0.20f)));
                c.AddChild(row);
            }

        c.AddChild(MkPLabel("", 8, Colors.White));
        c.AddChild(MkPLabel(Loc.Tr("dev.completed_list"), 14, new Color(0.5f, 0.6f, 0.7f)));
        // 最近5个已完成
        var completed = devMgr2?.CompletedProjects ?? new();
        foreach (var proj in completed.TakeLast(5).Reverse())
            c.AddChild(MkPLabel($"  ✅ {proj.Name}  {Loc.Tr("ui.score")}: {proj.FinalScore:F0}  {Loc.Tr("ui.sales")}: {proj.Sales / 1000f:F0}K", 12, new Color(0.6f, 0.7f, 0.8f)));
    }

    // ═══════════════════════════════════════════════
    //  📄 合同/外包中心面板
    // ═══════════════════════════════════════════════
    private void ShowContractsPanel()
    {
        var p = MakePanel("📄 " + Loc.Tr("hud.contracts"));
        var sc = AddScroll(p);
        var c = new VBoxContainer(); sc.AddChild(c);

        c.AddChild(MkPLabel(Loc.Tr("hud.contracts"), 16, new Color(0.4f, 0.8f, 0.4f)));
        c.AddChild(MkPLabel("", 6, Colors.White));

        if (OutsourceTaskPool.Count == 0)
        {
            c.AddChild(MkPLabel($"  {Loc.Tr("dev.no_contracts")}", 13, new Color(0.5f, 0.5f, 0.5f)));
        }
        else
        {
            foreach (var task in OutsourceTaskPool)
            {
                var row = new HBoxContainer();
                row.AddChild(MkPLabel($"  💰 {task.Name}  {task.RequiredSkill}Lv{task.RequiredLevel}  {task.Duration}{Loc.Tr("ui.month_unit")}  ¥{task.Reward / 1000f:F0}K", 12, new Color(0.7f, 0.8f, 0.4f)));

            var takeBtn = MkPButton(Loc.Tr("dev.take_contract"), 70, 24);
            var capTask = task;
            takeBtn.Pressed += () => {
                var tm = GetNode<TeamManager>("TeamManager");
                var idle = tm.Teams.Where(t => t.Task == TeamTask.None && t.Members.Count > 0).ToList();
                if (idle.Count == 0)
                {
                    ShowToast(Loc.Tr("dev.no_idle_team"), Loc.Tr("dev.no_idle_team_msg"), new Color(0.9f, 0.5f, 0.2f));
                    return;
                }
                // 弹出团队选择（类似科技研发）
                var vp = GetViewport().GetVisibleRect().Size;
                float pw = 400f * UIScale, ph = Mathf.Min(vp.Y * 0.75f, (60f + idle.Count * 36f) * UIScale);
                var overlay = new ColorRect { Color = new Color(0, 0, 0, 0.5f), MouseFilter = Control.MouseFilterEnum.Stop };
                overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
                var pp = new Panel { Position = new((vp.X - pw) / 2, (vp.Y - ph) / 2), Size = new(pw, ph) };
                pp.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 });
                overlay.AddChild(pp);
                var title = new Label { Text = Loc.Tr("tech.select_team"), Position = new(12f * UIScale, 8f * UIScale), Size = new(pw - 24f * UIScale, 24f * UIScale) };
                title.AddThemeFontSizeOverride("font_size", 16); title.AddThemeColorOverride("font_color", new Color(0, 0, 0));
                pp.AddChild(title);
                bool[] sel = new bool[idle.Count];
                for (int i = 0; i < idle.Count; i++)
                {
                    int idx = i; var t = idle[i];
                    float y = (36f + i * 34f) * UIScale;
                    var row2 = new Panel { Position = new(8f * UIScale, y), Size = new(pw - 16f * UIScale, 30f * UIScale) };
                    row2.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.92f, 0.92f, 0.90f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
                    var check = new Label { Text = "☐", Position = new(4f * UIScale, 3f * UIScale), Size = new(20f * UIScale, 24f * UIScale) };
                    check.AddThemeFontSizeOverride("font_size", 14); check.AddThemeColorOverride("font_color", new Color(0, 0, 0));
                    row2.AddChild(check);
                    var nl = new Label { Text = $"{t.Name} ({t.Members.Count}{Loc.Tr("ui.people_suffix")})", Position = new(28f * UIScale, 3f * UIScale), Size = new(pw - 80f * UIScale, 24f * UIScale) };
                    nl.AddThemeFontSizeOverride("font_size", 12); nl.AddThemeColorOverride("font_color", new Color(0, 0, 0));
                    row2.AddChild(nl);
                    row2.MouseFilter = Control.MouseFilterEnum.Stop;
                    row2.GuiInput += (ie) => { if (ie is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left) { sel[idx] = !sel[idx]; check.Text = sel[idx] ? "☑" : "☐"; } };
                    pp.AddChild(row2);
                }
                float btnY = ph - 36f * UIScale;
                var startBtn = new Button { Text = Loc.Tr("dev.take_contract"), Position = new(8f * UIScale, btnY), Size = new(pw / 2 - 12f * UIScale, 28f * UIScale) };
                startBtn.Pressed += () =>
                {
                    int assigned = 0;
                    for (int i = 0; i < idle.Count; i++)
                    {
                        if (!sel[i]) continue;
                        var team = idle[i];
                        team.Task = TeamTask.Outsource;
                        team.CurrentContract = new OutsourceContract
                        {
                            Name = capTask.Name, Difficulty = capTask.Difficulty > 0.7f ? OutsourceDifficulty.Hard : capTask.Difficulty > 0.4f ? OutsourceDifficulty.Medium : OutsourceDifficulty.Easy,
                            RequiredMonths = capTask.Duration, Payment = capTask.Reward, PenaltyRate = 0.2f,
                            PrimarySkill = capTask.RequiredSkill, MinSkillLevel = capTask.RequiredLevel, ExpReward = 5
                        };
                        team.OutsourceMonthsRemaining = capTask.Duration;
                        assigned++;
                    }
                    if (assigned > 0) { OutsourceTaskPool.Remove(capTask); RebuildHUDTabs(); }
                    overlay.QueueFree(); ShowContractsPanel();
                };
                pp.AddChild(startBtn);
                var cancelBtn = new Button { Text = Loc.Tr("ui.cancel"), Position = new(pw / 2 + 4f * UIScale, btnY), Size = new(pw / 2 - 12f * UIScale, 28f * UIScale) };
                cancelBtn.Pressed += () => overlay.QueueFree();
                pp.AddChild(cancelBtn);
                _uiLayer.AddChild(overlay);
            };
            row.AddChild(takeBtn);
                c.AddChild(row);
            }
        }

        // 进行中的外包
        var teamsWithOutsource = GetNode<TeamManager>("TeamManager").Teams.Where(t => t.Task == TeamTask.Outsource && t.CurrentContract != null).ToList();
        if (teamsWithOutsource.Count > 0)
        {
            c.AddChild(MkPLabel("", 8, Colors.White));
            c.AddChild(MkPLabel(Loc.Tr("dev.outsource_in_progress"), 14, new Color(0.9f, 0.7f, 0.3f)));
            foreach (var team in teamsWithOutsource)
            {
                var task = team.CurrentContract;
                c.AddChild(MkPLabel($"  ⚡ {task?.Name ?? "?"} - {team.Name} {team.Members.Count}{Loc.Tr("ui.people_suffix")}", 12, new Color(0.8f, 0.7f, 0.4f)));
            }
        }
    }

    // ═══════════════════════════════════════════════
    //  👤 员工管理面板
    // ═══════════════════════════════════════════════
    private void ShowEmployeePanel()
    {
        var p = MakePanel("👤 " + Loc.Tr("hud.employees"));

        float y0 = UIScale * 30;
        var header = new HBoxContainer { Position = new(UIScale * 20, y0) };
        header.AddThemeConstantOverride("separation", (int)(UIScale * 4));
        void AddHCol(string label, float w)
        {
            var l = MkPLabel(label, 12, new Color(0.08f, 0.12f, 0.2f));
            l.CustomMinimumSize = new(w, 0);
            header.AddChild(l);
        }
        AddHCol(" " + Loc.Tr("panel.col_employee"), UIScale * 180);
        AddHCol(Loc.Tr("panel.col_level"), UIScale * 50);
        AddHCol(Loc.Tr("panel.col_team"), UIScale * 100);
        AddHCol(Loc.Tr("panel.col_satisfaction"), UIScale * 80);
        p.AddChild(header);

        var scroll = new ScrollContainer { Position = new(UIScale * 20, y0 + UIScale * 20), Size = new(p.Size.X - UIScale * 40, p.Size.Y - y0 - UIScale * 90) };
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        var list = new VBoxContainer();
        list.AddThemeConstantOverride("separation", (int)(UIScale * 2));
        scroll.AddChild(list);
        p.AddChild(scroll);

        SetupEmpSelectAllKeys(p, _empMgr.Employees);

        var sorted = _empMgr.Employees.OrderByDescending(e => e.GetHighestLevel()).ToList();
        RegisterEmpList(list, sorted);
        EmpRowBuilder simpleBuilder = (lst, e, ei, src) =>
        {
            int avgLv = e.Skills.Count > 0 ? (int)e.Skills.Values.Average(s => s.Level) : 0;
            string tags = e.IsCaptain ? "👤" : e.IsChiefArchitect ? "★" : "";
            string traitText = e.Trait != EmployeeTrait.None ? $" {GetTraitName(e.Trait)}" : "";
            Color satColor = e.Satisfaction > 70 ? new Color(0.3f, 0.9f, 0.3f) : e.Satisfaction > 40 ? new Color(0.9f, 0.8f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
            var (pc, hb) = MakeEmpRowContainer(e);
            void AddL(string text, float w, int r, int g, int b) { var l = new Label { Text = text, CustomMinimumSize = new(w, UIScale * 30) }; l.AutowrapMode = TextServer.AutowrapMode.Word; l.AddThemeFontSizeOverride("font_size", 12); l.AddThemeColorOverride("font_color", new Color(r / 255f, g / 255f, b / 255f)); l.MouseFilter = Control.MouseFilterEnum.Ignore; hb.AddChild(l); }
            AddL($" {tags} {Loc.DisplayName(e.Name)}{traitText}", UIScale * 180, 8, 17, 28);
            AddL(Loc.TrF("ui.lv", avgLv), UIScale * 50, 30, 50, 35);
            AddL(e.TeamName ?? "-", UIScale * 100, 18, 22, 30);
            AddL($"{e.Satisfaction:F0}%", UIScale * 80, (int)(satColor.R * 255), (int)(satColor.G * 255), (int)(satColor.B * 255));
            AttachEmpClickHandler(pc, e, ei, src);
            lst.AddChild(pc);
        };
        for (int ei = 0; ei < sorted.Count; ei++) simpleBuilder(list, sorted[ei], ei, sorted);
        _refreshEmployeeList = () => { _lastEmpClickIndex = -1; RebuildEmpListInPlace(list, _empMgr.Employees.OrderByDescending(e => e.GetHighestLevel()).ToList(), simpleBuilder); };

        float botY = p.Size.Y - UIScale * 55;
        var backBtn = new Button { Text = "← 返回", Flat = true };
        backBtn.Position = new(UIScale * 20, botY);
        backBtn.AddThemeFontSizeOverride("font_size", 12);
        backBtn.AddThemeColorOverride("font_color", new Color(0f, 0f, 0f));
        backBtn.AddThemeColorOverride("font_hover_color", new Color(0.45f, 0.45f, 0.45f));
        backBtn.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.60f, 0.60f, 0.60f, 0.5f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        backBtn.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = new Color(0.18f, 0.18f, 0.18f, 0.75f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        backBtn.Pressed += () => PopTopPanel();
        p.AddChild(backBtn);

        if (_selectedEmployees.Count > 1)
        {
            var batchBtn = MkPButton(Loc.TrF("ui.batch_op_fmt", _selectedEmployees.Count), 150, 32);
            batchBtn.Position = new(UIScale * 155, botY);
            batchBtn.Pressed += () => ShowBatchEmployeeMenu();
            p.AddChild(batchBtn);
        }
        else
        {
            var recruitBtn = MkPButton($"🎯 {Loc.Tr("panel.recruit_title")}", 140, 32);
            recruitBtn.Position = new(UIScale * 155, botY);
            recruitBtn.AddThemeColorOverride("font_color", new Color(0.3f, 0.9f, 0.4f));
            recruitBtn.Pressed += () => { CloseAll(); ShowRecruitPanel(); };
            p.AddChild(recruitBtn);
        }

        int totalEmp = _empMgr.Employees.Count;
        float avgSat = totalEmp > 0 ? _empMgr.Employees.Average(e => e.Satisfaction) : 0;
        var sumPc = new PanelContainer();
        sumPc.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        sumPc.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.92f, 0.90f, 0.86f, 0.6f), BorderWidthTop = 1, BorderColor = new Color(0.4f, 0.45f, 0.5f, 0.3f) });
        var sumHb = new HBoxContainer();
        sumHb.AddThemeConstantOverride("separation", (int)(UIScale * 4));
        void AddSumL(string text, float w, int r, int g, int b)
        {
            var l = new Label { Text = text, CustomMinimumSize = new(w, 0) };
            l.AddThemeFontSizeOverride("font_size", 12);
            l.AddThemeColorOverride("font_color", new Color(r / 255f, g / 255f, b / 255f));
            l.MouseFilter = Control.MouseFilterEnum.Ignore;
            sumHb.AddChild(l);
        }
        AddSumL(Loc.TrF("ui.summary_fmt", Loc.TrF("ui.people_count", totalEmp)), UIScale * 180, 20, 20, 25);
        AddSumL("", UIScale * 50, 20, 20, 25);
        AddSumL("", UIScale * 100, 20, 20, 25);
        AddSumL($"{avgSat:F0}%", UIScale * 80, 18, 18, 25);
        sumPc.AddChild(sumHb);
        list.AddChild(sumPc);
    }

    // ═══════════════════════════════════════════════
    //  ⚔ 商战攻击面板
    // ═══════════════════════════════════════════════
    private void ShowAttackPanel(AIStudio selectedTarget = null)
    {
        var p = MakePanel("⚔ " + Loc.Tr("hud.attack"));
        var sc = AddScroll(p);
        var c = new VBoxContainer(); sc.AddChild(c);

        // 目标选择
        c.AddChild(MkPLabel(Loc.Tr("corp.select_target"), 15, new Color(0.9f, 0.4f, 0.3f)));
        var studios = _competitor.Studios;

        foreach (var studio in studios)
        {
            var row = new HBoxContainer();
            row.AddChild(MkPLabel($"  {studio.Name}  {Loc.Tr("ui.reputation")}: {studio.Reputation}  {Loc.Tr("ui.employees")}: {studio.EmployeeCount}", 12, new Color(0.7f, 0.8f, 0.9f)));

            var targetBtn = MkPButton(Loc.Tr("corp.target_btn"), 60, 24);
            var capStudio = studio;
            targetBtn.Pressed += () => { CloseAll(); ShowAttackPanel(capStudio); };
            row.AddChild(targetBtn);
            c.AddChild(row);
        }

        if (studios.Count == 0)
            c.AddChild(MkPLabel($"  {Loc.Tr("corp.no_target")}", 12, new Color(0.5f, 0.5f, 0.5f)));

        // 如果选中了目标，显示行动按钮
        if (selectedTarget != null)
        {
            c.AddChild(MkPLabel("", 8, Colors.White));
            c.AddChild(MkPLabel($"🎯 {Loc.Tr("corp.target")}: {selectedTarget.Name}", 14, new Color(0.9f, 0.5f, 0.2f)));

            var actions = new[] { ActionType.Poach, ActionType.NegativePress, ActionType.Lawsuit, ActionType.HostileTakeover, ActionType.PriceWar, ActionType.ExclusiveDeal, ActionType.IPDispute };
            foreach (var action in actions)
            {
                int cd = CorpActions.GetCooldownRemaining(action);
                int cost = CorpActions.GetCost(action);
                float success = CorpActions.GetBaseSuccessRate(action, selectedTarget);
                string name = action switch
                {
                    ActionType.Poach => "🧨 " + Loc.Tr("corp.poach"),
                    ActionType.NegativePress => "📰 " + Loc.Tr("corp.press"),
                    ActionType.Lawsuit => "⚖️ " + Loc.Tr("corp.sue"),
                    ActionType.HostileTakeover => "🤝 " + Loc.Tr("corp.takeover"),
                    ActionType.PriceWar => "💣 " + Loc.Tr("corp.pricewar"),
                    ActionType.ExclusiveDeal => "📝 " + Loc.Tr("corp.exclusive"),
                    ActionType.IPDispute => "⚔️ " + Loc.Tr("corp.ip"),
                    _ => "?",
                };

                bool canUse = cd <= 0 && _res.Money >= cost;
                var btn = MkPButton($"{name}  ¥{cost / 1000f:F0}K  {Loc.Tr("ui.success")}:{success * 100:F0}%  {(cd > 0 ? Loc.TrF("ui.cd_months", cd) : "")}", 400, 28);
                if (!canUse) btn.Modulate = new Color(0.5f, 0.5f, 0.5f);
                else btn.AddThemeColorOverride("font_color", new Color(0.9f, 0.4f, 0.3f));
                btn.Disabled = !canUse;

                var capAction = action;
                var capTarget = selectedTarget;
                btn.Pressed += () => {
                    int nowCd = CorpActions.GetCooldownRemaining(capAction);
                    if (nowCd > 0) { ShowToast(Loc.Tr("corp.failed"), Loc.TrF("ui.cd_months", nowCd), new Color(0.9f, 0.5f, 0.2f)); return; }
                    long nowCost = CorpActions.GetCost(capAction);
                    if (_res.Money < nowCost) { ShowToast(Loc.Tr("ui.insufficient_funds"), Loc.TrF("ui.need_money", nowCost), new Color(0.9f, 0.3f, 0.2f)); return; }
                    CorpActions.Execute(capAction, capTarget); CloseAll(); ShowAttackPanel();
                };
                c.AddChild(btn);
            }
        }

        // 行动日志
        c.AddChild(MkPLabel("", 8, Colors.White));
        c.AddChild(MkPLabel(Loc.Tr("corp.log_title"), 14, new Color(0.6f, 0.7f, 0.8f)));
        foreach (var log in CorpActions.ActionLogs.TakeLast(10).Reverse())
        {
            string icon = log.Success ? "✅" : "❌";
            string date = $"{log.Month / 12 + 1}{Loc.Tr("ui.year")}{log.Month % 12 + 1}{Loc.Tr("ui.month_short")}";
            c.AddChild(MkPLabel($"{icon} {date} {log.Description}", 11, log.Success ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.3f, 0.3f)));
        }

        // 贷款信息
        c.AddChild(MkPLabel("", 8, Colors.White));
        c.AddChild(MkPLabel(Loc.Tr("loan.title"), 14, new Color(0.9f, 0.7f, 0.3f)));
        if (Loan.HasActiveLoan)
        {
            c.AddChild(MkPLabel($"  {Loc.Tr("loan.principal")}: ¥{Loan.Principal:N0}  {Loc.Tr("loan.rate")}: {Loan.InterestRate * 100:F1}%  {Loc.Tr("loan.months_left")}: {Loan.RemainingMonths}  {Loc.Tr("loan.monthly_pay")}: ¥{Loan.MonthlyPayment:N0}", 12, new Color(0.9f, 0.7f, 0.3f)));

            var payoffBtn = MkPButton(Loc.Tr("loan.payoff"), 100, 28);
            payoffBtn.Pressed += () => {
                if (_res.SpendMoney(Loan.GetEarlyPayoff(), "loan_payoff"))
                {
                    Loan.PayOff();
                    ShowToast(Loc.Tr("loan.paid"), Loc.Tr("loan.paid_msg"), new Color(0.3f, 0.9f, 0.3f));
                    CloseAll(); ShowAttackPanel();
                }
            };
            c.AddChild(payoffBtn);
        }
        else
        {
            var devMgrForLoanMax = GetNode<GameDevManager>("GameDevManager");
            float repVal = devMgrForLoanMax?.PublisherReputation * 100f ?? 50f;
            float maxLoan = Loan.GetMaxLoanAmount(_res.Money, _res.TotalRevenue, (int)repVal, devMgrForLoanMax?.IsListed ?? false);
            c.AddChild(MkPLabel(Loc.TrF("loan.max_amount", maxLoan / 10000f), 12, new Color(0.7f, 0.7f, 0.7f)));

            var loanBtns = new HBoxContainer();
            float[] amounts = { 50000, 100000, 200000, 500000, 1000000 };
            foreach (var amt in amounts)
            {
                if (amt > maxLoan) break;
                var lb = MkPButton($"¥{amt / 10000f:F0}{Loc.Tr("ui.million")}", 80, 26);
                float cap = amt;
                lb.Pressed += () => {
                    if (Loan.TakeLoan(cap, maxLoan))
                    {
                        _res.EarnMoney((long)cap, "loan");
                        ShowToast(Loc.Tr("loan.taken"), Loc.TrF("loan.taken_msg", cap, Loan.InterestRate * 100), new Color(0.3f, 0.9f, 0.3f));
                        CloseAll(); ShowAttackPanel();
                    }
                };
                loanBtns.AddChild(lb);
            }
            c.AddChild(loanBtns);
        }
    }

    private void ShowServerPanel()
    {
        var p = MakePanel(Loc.Tr("server.panel_title"));
        var sc = AddScroll(p);
        var c = new VBoxContainer(); sc.AddChild(c);

        var info = ServerData.Data[_serverMgr.CurrentTier];
        string tierName = ServerData.GetTierName(_serverMgr.CurrentTier);
        var cap = info.Capacity;
        var monthCost = info.MonthlyCost;
        var rel = info.Reliability;

        c.AddChild(MkPLabel(
            $"{Loc.Tr("server.current_server")}: {tierName}  {Loc.Tr("server.capacity")}: {cap:N0}{Loc.Tr("server.concurrent")}  {Loc.Tr("server.monthly_cost")}: ¥{monthCost:N0}  {Loc.Tr("server.reliability")}: {rel * 100:F1}%",
            14, info.Color));

        if (_serverMgr.HasNoServer)
            c.AddChild(MkPLabel(Loc.Tr("server.no_server_warn"), 13, new Color(0.9f, 0.3f, 0.2f)));
        else
            c.AddChild(MkPLabel(_serverMgr.GetOverloadHint(), 13,
                _serverMgr.IsOverloaded ? new Color(0.95f, 0.45f, 0.15f) : new Color(0.3f, 0.8f, 0.3f)));

        c.AddChild(MkPLabel("", 6, Colors.White));

        int demand = _serverMgr.CurrentDemand;
        c.AddChild(MkPLabel(
            $"{Loc.Tr("server.demand_label")}: {demand:N0} {Loc.Tr("server.concurrent_users")}  ({Loc.Tr("server.online_ratio")} {GetOnlineGameRatio() * 100:F0}%)",
            13, new Color(0.5f, 0.7f, 0.9f)));

        c.AddChild(MkPLabel("", 6, Colors.White));
        c.AddChild(MkPLabel(Loc.Tr("server.upgrade_title"), 16, new Color(0.10f, 0.14f, 0.22f)));

        if (_serverMgr.CurrentTier.CanUpgrade())
        {
            var next = _serverMgr.CurrentTier.NextInfo();
            string nextName = ServerData.GetTierName(ServerData.NextTier(_serverMgr.CurrentTier));
            var upgradeBtn = MkPButton(
                Loc.TrF("server.upgrade_btn", nextName, next.Capacity, next.MonthlyCost, next.PurchaseCost, next.Reliability * 100),
                540, 30);
            upgradeBtn.Pressed += () =>
            {
                if (_serverMgr.UpgradeServer())
                    ShowServerPanel();
            };
            c.AddChild(upgradeBtn);
        }
        else
        {
            c.AddChild(MkPLabel(Loc.Tr("server.max_tier"), 13, new Color(0.3f, 0.8f, 0.4f)));
        }

        c.AddChild(MkPLabel("", 8, Colors.White));
        c.AddChild(MkPLabel(Loc.Tr("server.all_tiers_title"), 14, new Color(0.10f, 0.14f, 0.22f)));

        // 表头
        float S(float v) => v * UIScale;
        var header = new HBoxContainer();
        header.AddChild(MkPLabel(Loc.Tr("server.current_server"), 11, new Color(0.4f, 0.45f, 0.5f), S(130)));
        header.AddChild(MkPLabel(Loc.Tr("server.capacity"), 11, new Color(0.4f, 0.45f, 0.5f), S(110), center: true));
        header.AddChild(MkPLabel(Loc.Tr("server.monthly_cost"), 11, new Color(0.4f, 0.45f, 0.5f), S(110), center: true));
        header.AddChild(MkPLabel(Loc.Tr("server.upgrade_btn"), 11, new Color(0.4f, 0.45f, 0.5f), S(110), center: true));
        header.AddChild(MkPLabel(Loc.Tr("server.reliability"), 11, new Color(0.4f, 0.45f, 0.5f), S(90), center: true));
        c.AddChild(header);

        foreach (var kv in ServerData.Data)
        {
            if (kv.Key == ServerTier.None) continue;
            var si = kv.Value;
            bool owned = kv.Key == _serverMgr.CurrentTier;
            Color rowCol = owned ? new Color(0.9f, 0.7f, 0.1f) : new Color(0.5f, 0.55f, 0.6f);
            string tierDisplayName = ServerData.GetTierName(kv.Key);
            if (owned) tierDisplayName += Loc.Tr("server.current_marker");

            var row = new HBoxContainer();
            row.AddChild(MkPLabel(tierDisplayName, 12, rowCol, S(130)));
            row.AddChild(MkPLabel($"{si.Capacity:N0}", 12, rowCol, S(110), center: true));
            row.AddChild(MkPLabel($"¥{si.MonthlyCost:N0}", 12, rowCol, S(110), center: true));
            row.AddChild(MkPLabel($"¥{si.PurchaseCost:N0}", 12, rowCol, S(110), center: true));
            row.AddChild(MkPLabel($"{si.Reliability * 100:F1}%", 12, rowCol, S(90), center: true));
            c.AddChild(row);
        }
    }

    private void ShowFinancePanel()
    {
        var p = MakePanel(Loc.Tr("panel.finance_fans"));
        var sc = AddScroll(p);
        var c = new VBoxContainer(); sc.AddChild(c);

        // 快捷操作按钮行
        var opsRow = new HBoxContainer();
        opsRow.AddThemeConstantOverride("separation", (int)(UIScale * 6));
        var pubBtn = MkPButton(Loc.Tr("fin.pub_center"), 100, 30);
        pubBtn.AddThemeFontSizeOverride("font_size", 12);
        pubBtn.Pressed += () => { CloseAll(); _openPopup = new DevMenu(this); _openPopup.RenderPublishingCenter(); };
        opsRow.AddChild(pubBtn);
        var finBtn = MkPButton(Loc.Tr("fin.finance_ops"), 80, 30);
        finBtn.AddThemeFontSizeOverride("font_size", 12);
        finBtn.Pressed += () => { CloseAll(); _openPopup = new DevMenu(this); _openPopup.RenderFinanceOps(); };
        opsRow.AddChild(finBtn);
        var predBtn = MkPButton(Loc.Tr("fin.market_pred"), 80, 30);
        predBtn.AddThemeFontSizeOverride("font_size", 12);
        predBtn.Pressed += () => { CloseAll(); _openPopup = new DevMenu(this); _openPopup.RenderMarketPrediction(); };
        opsRow.AddChild(predBtn);
        c.AddChild(opsRow);
        c.AddChild(MkPLabel("", 6, Colors.White));

        c.AddChild(MkPLabel(Loc.TrF("fin.cash_fmt", _res.Money), 16, new Color(0.2f, 0.9f, 0.3f)));
        c.AddChild(MkPLabel(Loc.TrF("fin.monthly_fmt", _res.MonthlyIncome, _res.MonthlyExpense), 13, new Color(0.6f, 0.7f, 0.8f)));
        c.AddChild(MkPLabel(Loc.TrF("fin.total_rev_fmt", _res.TotalRevenue), 13, new Color(0.6f, 0.7f, 0.8f)));
        c.AddChild(MkPLabel(Loc.TrF("fin.engine_inc_fmt", _res.EngineIncome), 13, new Color(0.6f, 0.7f, 0.8f)));
        c.AddChild(MkPLabel(Loc.TrF("fin.expense_fmt", _res.SalaryExpense, _roomMgr.TotalMonthlyRent), 13, new Color(0.6f, 0.7f, 0.8f)));

        c.AddChild(MkPLabel("", 8, Colors.White));
        c.AddChild(MkPLabel(Loc.TrF("fin.fan_fmt", _fanMgr.CasualFans, _fanMgr.DiehardFans, _fanMgr.GetGuaranteedSales()), 14, new Color(0.9f, 0.5f, 0.9f)));
        var evtBtn = MkPButton(Loc.Tr("fin.fan_event_btn"), 200, 32);
        evtBtn.Pressed += () => { if (_fanMgr.HoldFanEvent(50000)) { ShowFinancePanel(); } };
        c.AddChild(evtBtn);

        c.AddChild(MkPLabel("", 8, Colors.White));
        c.AddChild(MkPLabel(Loc.Tr("fin.rivals_title"), 15, new Color(0.6f, 0.8f, 1.0f)));
        foreach (var s in _competitor.Studios.Take(10))
            c.AddChild(MkPLabel(Loc.TrF("ui.rival_detail", s.Name, s.Reputation, s.Releases.Count, s.HasPlayerEngine ? Loc.Tr("ui.rival_has_engine") : ""), 12, new Color(0.5f, 0.6f, 0.7f)));

        c.AddChild(MkPLabel("", 8, Colors.White));
        c.AddChild(MkPLabel(Loc.Tr("ui.completed_projects"), 15, new Color(0.6f, 0.8f, 1.0f)));
        foreach (var proj in _devMgr.CompletedProjects)
            c.AddChild(MkPLabel(Loc.TrF("ui.project_done", proj.Name, proj.FinalScore, proj.Sales, proj.Revenue), 12, new Color(0.5f, 0.7f, 0.5f)));
    }

    private enum CompanySortBy { Name, MarketCap, Cash, Fans, GameCount, IPCount, ListedPct }
    private CompanySortBy _companySort = CompanySortBy.Name;
    private bool _companySortAsc = true;

    /// <summary>计算大盘指数（所有上市公司市值加权，基期1000）</summary>
    private float CalcMarketIndex()
    {
        float totalMC = 0;
        int count = 0;
        var devMgr = GetNode<GameDevManager>("GameDevManager");
        if (devMgr.IsListed) { totalMC += devMgr.SharePrice * devMgr.SharesOutstanding; count++; }
        foreach (var s in _competitor.Studios)
        {
            if (s.IsListed) { totalMC += s.MarketCap; count++; }
        }
        return count > 0 ? totalMC / count / 100f * 1000f : 1000f;
    }

    private void ShowCompanyPanel()
    {
        bool firstBuild = _companyPanel == null || !GodotObject.IsInstanceValid(_companyPanel) || !_companyPanel.IsInsideTree();

        if (firstBuild)
        {
            // 移除所有旧引用
            while (_openPanels.Count > 0) PopTopPanel();
            if (_openPopup != null) { _openPopup.Close(); _openPopup = null; }
            if (_openDevPopup != null) { _openDevPopup.Close(); _openDevPopup = null; }
            _selectedCompanies.Clear();
            _lastClickIndex = -1;

            var devMgr = GetNode<GameDevManager>("GameDevManager");
            var p = MakePanel(Loc.Tr("ui.company_list"));
            _companyPanel = p;

            // 顶栏：大盘指数 + IPO + 市场情绪
            var topBar = new HBoxContainer { Position = new(UIScale * 20, UIScale * 8) };
            // 大盘指数
            float indexVal = CalcMarketIndex();
            Color indexColor = indexVal > 1000 ? new Color(0.2f, 0.7f, 0.3f) : indexVal < 900 ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.3f, 0.3f, 0.3f);
            topBar.AddChild(MkPLabel(Loc.TrF("ui.market_index", indexVal), 12, indexColor));
            topBar.AddChild(MkPLabel($"  |  {_competitor.MarketPhase}", 12, _competitor.MarketSentiment > 1.05f ? new Color(0.8f, 0.15f, 0.08f) : _competitor.MarketSentiment < 0.95f ? new Color(0.08f, 0.15f, 0.6f) : new Color(0.3f, 0.3f, 0.3f)));
            if (!devMgr.IsListed && GameYear >= 5 && GetTotalEmployees() >= 30)
            {
                long yrProfit = devMgr.MonthlyProfitLog.TakeLast(12).Sum(l => (long)(l.revenue - l.expense));
                if (yrProfit >= 500000)
                {
                    var ipoBtn = MkButtonS($"🚀 {Loc.Tr("ui.ipo")}", 100, 28); ipoBtn.Modulate = new Color(0.3f, 0.9f, 0.4f);
                    ipoBtn.Pressed += () => { devMgr.IsListed = true; ModAPI.FireHooks(ModAPI.GameHook.OnCompanyIPO); devMgr.IPOProceeds = 3000000f + (float)new Random().NextDouble() * 3000000f; _res.EarnMoney(devMgr.IPOProceeds, "ipo"); devMgr.SharesOutstanding = 10000; float eps = yrProfit / 12f / devMgr.SharesOutstanding; float repScore = devMgr.PublisherReputation * 100f + _res.TotalRevenue / 10000000f; float industryPE = Mathf.Clamp(8 + repScore * 0.08f, 5, 40); devMgr.SharePrice = Mathf.Max(10f, eps * industryPE); devMgr.ExpectedProfit = yrProfit / 12f; devMgr.PriceHistory.Clear(); devMgr.PriceHistory.Add((GameMonth, devMgr.SharePrice)); _companyListNeedRebuild = true; ShowCompanyPanel(); };
                    topBar.AddChild(ipoBtn);
                }
            }
            else if (devMgr.IsListed)
            {
                topBar.AddChild(MkPLabel(Loc.TrF("ui.star_listed", devMgr.SharePrice, devMgr.SharePrice * devMgr.SharesOutstanding / 1e6f), 12, new Color(0.3f, 0.9f, 0.4f)));
            }
            p.AddChild(topBar);

            // 排序表头
            float y0 = UIScale * 40;
            var header = new HBoxContainer { Position = new(UIScale * 20, y0) };
            header.AddThemeConstantOverride("separation", (int)(UIScale * 4));
            void AddCol(string label, CompanySortBy sort, float w) {
                var l = MkPLabel(label, 12, _companySort == sort ? new Color(0.05f, 0.2f, 0.6f) : new Color(0.08f, 0.12f, 0.2f));
                l.CustomMinimumSize = new(w, 0);
                l.MouseFilter = Control.MouseFilterEnum.Stop;
                l.GuiInput += (ie) => { if (ie is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left) { if (_companySort == sort) _companySortAsc = !_companySortAsc; else { _companySort = sort; _companySortAsc = sort == CompanySortBy.Name; } _selectedCompanies.Clear(); _lastClickIndex = -1; _hoveredCompany = null; _companyListNeedRebuild = true; ShowCompanyPanel(); } };
                header.AddChild(l);
            }
            AddCol($" {Loc.Tr("ui.company_name")}", CompanySortBy.Name, UIScale * 155);
            AddCol(Loc.Tr("ui.market_cap"), CompanySortBy.MarketCap, UIScale * 92);
            AddCol(Loc.Tr("ui.cash"), CompanySortBy.Cash, UIScale * 98);
            AddCol(Loc.Tr("ui.fans"), CompanySortBy.Fans, UIScale * 82);
            AddCol(Loc.Tr("ui.games"), CompanySortBy.GameCount, UIScale * 58);
            AddCol(Loc.Tr("ui.ip"), CompanySortBy.IPCount, UIScale * 52);
            AddCol(Loc.Tr("ui.listed_pct"), CompanySortBy.ListedPct, UIScale * 62);
            p.AddChild(header);

            // 滚动容器
            _companyScroll = new ScrollContainer { Position = new(UIScale * 20, y0 + UIScale * 20), Size = new(p.Size.X - UIScale * 40, p.Size.Y - y0 - UIScale * 30) };
            _companyScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
            _companyList = new VBoxContainer();
            _companyList.AddThemeConstantOverride("separation", (int)(UIScale * 2));
            _companyScroll.AddChild(_companyList);
            p.AddChild(_companyScroll);
            _companyListNeedRebuild = true;

            // 注册公司面板的 Ctrl+A 全选
            _activePanelSelectAllAction = () =>
            {
                _selectedCompanies.Clear();
                var all = BuildCompanyList(GetNode<GameDevManager>("GameDevManager"));
                foreach (var c in all) _selectedCompanies.Add(c.name);
                ApplyAllRowHighlights();
                _lastClickIndex = -1;
            };
        }

        // ── 重建/刷新行 ──
        var devMgr2 = GetNode<GameDevManager>("GameDevManager");
        var allCompanies = BuildCompanyList(devMgr2);

        // 需要重建（排序变化/IPO等）→ 清空重来
        if (_companyListNeedRebuild || _companyList.GetChildCount() != allCompanies.Count)
        {
            foreach (var ch in _companyList.GetChildren()) ch.QueueFree();
            _companyList.Visible = false;
            int idx = 0;
            foreach (var (name, mktcap, cash, fans, games, ips, listedPct, isPlayer, studioRef) in allCompanies)
            {
                var row = MakeCompanyRow(name, mktcap, cash, fans, games, ips, listedPct, isPlayer, studioRef, devMgr2, idx);
                _companyList.AddChild(row);
                idx++;
            }
            _companyList.Visible = true;
            _companyListNeedRebuild = false;
        }
        else
        {
            // 增量刷新：只更新数据，不重置滚动位置
            var children = _companyList.GetChildren();
            int idx = 0;
            foreach (var (name, mktcap, cash, fans, games, ips, listedPct, isPlayer, studioRef) in allCompanies)
            {
                if (idx < children.Count && children[idx] is PanelContainer rowPc)
                    UpdateCompanyRow(rowPc, name, mktcap, cash, fans, games, ips, listedPct, isPlayer, studioRef, devMgr2, idx);
                else
                {
                    // 数量变化时追加
                    var newRow = MakeCompanyRow(name, mktcap, cash, fans, games, ips, listedPct, isPlayer, studioRef, devMgr2, idx);
                    _companyList.AddChild(newRow);
                }
                idx++;
            }
            // 移除多余行
            for (int i = children.Count - 1; i >= idx; i--)
                children[i].QueueFree();
        }

        // ── 列表末尾加汇总行 ──
        AddCompanySummaryRow(allCompanies);
    }

    /// <summary>在公司列表末尾添加/更新汇总统计行</summary>
    private void AddCompanySummaryRow(List<(string name, float mktcap, float cash, int fans, int games, int ips, float listedPct, bool isPlayer, AIStudio studio)> allCompanies)
    {
        if (_companyList == null) return;

        // 移除旧的汇总行
        if (GodotObject.IsInstanceValid(_companySummaryRow))
        {
            _companySummaryRow.QueueFree();
            _companySummaryRow = null;
        }

        int empMgrTotal = GetTotalEmployees();
        float totalSalary = _empMgr.Employees.Sum(e => e.Salary);
        float totalMktCap = allCompanies.Sum(c => c.mktcap);
        long totalCash = (long)allCompanies.Sum(c => c.cash);
        int totalFans = allCompanies.Sum(c => c.fans);
        int totalGames = allCompanies.Sum(c => c.games);
        int totalIPs = _competitor.Studios.Sum(s => s.IPCount) + (_devMgr?.CompletedProjects.Where(p => !string.IsNullOrEmpty(p.IPName)).Select(p => p.IPName).Distinct().Count() ?? 0);
        int totalListed = allCompanies.Count(c => c.listedPct > 0);

        var pc = new PanelContainer();
        pc.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        pc.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.92f, 0.90f, 0.86f, 0.6f), BorderWidthTop = 1, BorderColor = new Color(0.4f, 0.45f, 0.5f, 0.3f) });

        var hb = new HBoxContainer();
        hb.AddThemeConstantOverride("separation", (int)(UIScale * 4));
        pc.AddChild(hb);

        void AddSL(string text, float w, int r, int g, int b)
        {
            var l = new Label { Text = text, CustomMinimumSize = new(w, 0) };
            l.AddThemeFontSizeOverride("font_size", 12);
            l.AddThemeColorOverride("font_color", new Color(r / 255f, g / 255f, b / 255f));
            l.MouseFilter = Control.MouseFilterEnum.Ignore;
            hb.AddChild(l);
        }

        AddSL(Loc.TrF("comp.summary_fmt", allCompanies.Count), UIScale * 155, 20, 20, 25);
        AddSL(totalMktCap > 0 ? Loc.TrF("comp.market_cap_fmt", totalMktCap/1e6f) : "-", UIScale * 92, 20, 20, 25);
        AddSL(FormatMoney(totalCash), UIScale * 98, 20, 20, 25);
        AddSL($"{totalFans:N0}", UIScale * 82, 20, 20, 25);
        AddSL($"{totalGames}", UIScale * 58, 20, 20, 25);
        AddSL($"{totalIPs}", UIScale * 52, 20, 20, 25);
        AddSL(Loc.TrF("comp.listed_count", totalListed), UIScale * 62, 15, 55, 20);

        _companyList.AddChild(pc);
        _companySummaryRow = pc;
    }

    private List<(string name, float mktcap, float cash, int fans, int games, int ips, float listedPct, bool isPlayer, AIStudio studio)> BuildCompanyList(GameDevManager devMgr)
    {
        var list = new List<(string name, float mktcap, float cash, int fans, int games, int ips, float listedPct, bool isPlayer, AIStudio studio)>();
        // 玩家公司
        float pMkt = devMgr.IsListed ? devMgr.SharePrice * devMgr.SharesOutstanding : 0;
        int pGames = devMgr.CompletedProjects.Count;
        int pIPs = devMgr.CompletedProjects.Where(p => !string.IsNullOrEmpty(p.IPName)).Select(p => p.IPName).Distinct().Count();
        var fanMgr = GetNodeOrNull("FanManager");
        int pFans = fanMgr != null ? GetNode<FanManager>("FanManager").TotalFans : 0;
        list.Add((devMgr.IsListed ? Loc.Tr("ui.your_company") : Loc.Tr("ui.your_company_unlisted"), pMkt, _res.Money, pFans, pGames, pIPs, devMgr.IsListed ? 1f : 0, true, null));
        // AI公司
        foreach (var s in _competitor.Studios)
        {
            if (s.IsAcquired) continue;
            float listedPct = s.IsListed ? (float)(s.SharesOutstanding - s.Shareholders.GetValueOrDefault(Loc.Tr("compname.founder"), 0)) / s.SharesOutstanding : 0;
            list.Add((s.Name, s.IsListed ? s.MarketCap : 0f, s.Money, s.Fans, s.Releases.Count, s.IPCount, listedPct, false, s));
        }
        var ordered = _companySort switch
        {
            CompanySortBy.MarketCap => _companySortAsc ? list.OrderBy(x => x.mktcap) : list.OrderByDescending(x => x.mktcap),
            CompanySortBy.Cash => _companySortAsc ? list.OrderBy(x => x.cash) : list.OrderByDescending(x => x.cash),
            CompanySortBy.Fans => _companySortAsc ? list.OrderBy(x => x.fans) : list.OrderByDescending(x => x.fans),
            CompanySortBy.GameCount => _companySortAsc ? list.OrderBy(x => x.games) : list.OrderByDescending(x => x.games),
            CompanySortBy.IPCount => _companySortAsc ? list.OrderBy(x => x.ips) : list.OrderByDescending(x => x.ips),
            CompanySortBy.ListedPct => _companySortAsc ? list.OrderBy(x => x.listedPct) : list.OrderByDescending(x => x.listedPct),
            _ => _companySortAsc ? list.OrderBy(x => x.name) : list.OrderByDescending(x => x.name),
        };
        return ordered.ToList();
    }

    private PanelContainer MakeCompanyRow(string name, float mktcap, float cash, int fans, int games, int ips,
        float listedPct, bool isPlayer, AIStudio studioRef, GameDevManager devMgr, int idx)
    {
        var pc = new PanelContainer();
        pc.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        pc.MouseFilter = Control.MouseFilterEnum.Stop;
        pc.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(1, 1, 1, 0.01f) });

        var hb = new HBoxContainer();
        hb.AddThemeConstantOverride("separation", (int)(UIScale * 4));
        hb.MouseFilter = Control.MouseFilterEnum.Ignore;
        pc.AddChild(hb);

        void AddL(string text, float w, int r, int g, int b)
        {
            var l = new Label { Text = text };
            l.CustomMinimumSize = new Vector2(w, 0);
            l.AutowrapMode = TextServer.AutowrapMode.Word;
            l.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin | Control.SizeFlags.Expand;
            l.AddThemeFontSizeOverride("font_size", 12);
            l.AddThemeColorOverride("font_color", new Color(r / 255f, g / 255f, b / 255f));
            l.MouseFilter = Control.MouseFilterEnum.Ignore;
            hb.AddChild(l);
        }

        Color nc = isPlayer ? new Color(0.05f, 0.55f, 0.1f) : new Color(0.1f, 0.12f, 0.2f);
        AddL($" {(isPlayer ? "⭐" : " ")} {Loc.DisplayName(name)}", UIScale * 155, (int)(nc.R * 255), (int)(nc.G * 255), (int)(nc.B * 255));
        AddL(mktcap > 0 ? $"¥{mktcap/1e6f:F1}M" : "-", UIScale * 92, 13, 64, 20);
        AddL(FormatMoney((long)cash), UIScale * 98, 26, 31, 51);
        AddL($"{fans:N0}", UIScale * 82, 26, 31, 51);
        AddL($"{games}", UIScale * 58, 26, 31, 51);
        AddL($"{ips}", UIScale * 52, 26, 31, 51);
        AddL(listedPct > 0 ? $"{listedPct:P0}" : "-", UIScale * 62, listedPct > 0 ? 13 : 20, listedPct > 0 ? 64 : 26, listedPct > 0 ? 26 : 36);

        string captureName = name; bool capturePlayer = isPlayer;
        AIStudio captureStudio = studioRef;
        int captureIdx = idx;

        pc.MouseEntered += () => { _hoveredCompany = captureName; ApplyPCRowHighlight(pc, captureName); };
        pc.MouseExited += () => { _hoveredCompany = null; ApplyPCRowHighlight(pc, captureName); };

        pc.GuiInput += (ie) =>
        {
            if (!(ie is InputEventMouseButton mb) || !mb.Pressed || mb.ButtonIndex != MouseButton.Left)
                return;

            float now = (float)Time.GetTicksMsec() / 1000f;
            if (_lastCompanyClicked == captureName && now - _lastCompanyClickTime < 0.5f)
            {
                _lastCompanyClicked = "";
                ShowCompanyDetail(captureName, capturePlayer, captureStudio, devMgr);
                return;
            }
            _lastCompanyClickTime = now;
            _lastCompanyClicked = captureName;

            bool ctrl = mb.CtrlPressed || mb.MetaPressed;
            bool shift = mb.ShiftPressed;

            if (ctrl)
            {
                if (_selectedCompanies.Contains(captureName)) _selectedCompanies.Remove(captureName);
                else _selectedCompanies.Add(captureName);
                _lastClickIndex = captureIdx;
            }
            else if (shift && _lastClickIndex >= 0)
            {
                int start = Mathf.Min(_lastClickIndex, captureIdx);
                int end = Mathf.Max(_lastClickIndex, captureIdx);
                var all = BuildCompanyList(devMgr);
                for (int i = start; i <= end && i < all.Count; i++)
                    _selectedCompanies.Add(all[i].name);
            }
            else
            {
                _selectedCompanies.Clear();
                _selectedCompanies.Add(captureName);
                _lastClickIndex = captureIdx;
            }
            ApplyAllRowHighlights();
        };
        ApplyPCRowHighlight(pc, captureName);
        return pc;
    }

    private void UpdateCompanyRow(PanelContainer pc, string name, float mktcap, float cash, int fans, int games, int ips,
        float listedPct, bool isPlayer, object studioRef, GameDevManager devMgr, int idx)
    {
        var hb = pc.GetChild<HBoxContainer>(0);
        if (hb == null) return;
        var children = hb.GetChildren();
        if (children.Count < 7) return;
        ((Label)children[0]).Text = $" {(isPlayer ? "⭐" : " ")} {Loc.DisplayName(name)}";
        ((Label)children[1]).Text = mktcap > 0 ? Loc.TrF("comp.market_cap_fmt", mktcap/1e6f) : "-";
        ((Label)children[2]).Text = FormatMoney((long)cash);
        ((Label)children[3]).Text = $"{fans:N0}";
        ((Label)children[4]).Text = $"{games}";
        ((Label)children[5]).Text = $"{ips}";
        ((Label)children[6]).Text = listedPct > 0 ? $"{listedPct:P0}" : "-";
        ApplyPCRowHighlight(pc, name);
    }

    private void ApplyPCRowHighlight(PanelContainer pc, string name)
    {
        bool selected = _selectedCompanies.Contains(name);
        bool hovered = _hoveredCompany == name;

        // 所有状态边框宽度一致(Left=3, 其他=1)，只有颜色不同，避免高度跳动
        const int BW = 1; // 基础边框
        const int BL = 3; // 左边框

        if (selected)
        {
            pc.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color(0.15f, 0.4f, 0.85f, 0.15f),
                BorderWidthLeft = BL, BorderWidthTop = BW, BorderWidthRight = BW, BorderWidthBottom = BW,
                BorderColor = new Color(0.15f, 0.5f, 0.9f, 0.7f),
                ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 2, ContentMarginBottom = 2,
            });
        }
        else if (hovered)
        {
            pc.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color(1, 1, 1, 0.02f),
                BorderWidthLeft = BL, BorderWidthTop = BW, BorderWidthRight = BW, BorderWidthBottom = BW,
                BorderColor = new Color(0.6f, 0.6f, 0.6f, 0.35f),
                ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 2, ContentMarginBottom = 2,
            });
        }
        else
        {
            pc.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color(1, 1, 1, 0.01f),
                BorderWidthLeft = BL, BorderWidthTop = BW, BorderWidthRight = BW, BorderWidthBottom = BW,
                BorderColor = new Color(0, 0, 0, 0),
                ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 2, ContentMarginBottom = 2,
            });
        }
    }

    private void ApplyAllRowHighlights()
    {
        if (_companyList == null) return;
        var children = _companyList.GetChildren();
        int idx = 0;
        var devMgr = GetNode<GameDevManager>("GameDevManager");
        var allCompanies = BuildCompanyList(devMgr);
        foreach (var ch in children)
        {
            if (ch is PanelContainer rowPc && idx < allCompanies.Count)
            {
                ApplyPCRowHighlight(rowPc, allCompanies[idx].name);
                idx++;
            }
        }
    }

    // ══════════════════ 公司详情（新 UI） ══════════════════
    private void ShowCompanyDetail(string name, bool isPlayer, AIStudio studio, GameDevManager devMgr)
    {
        _lastCompanyDetailName = name;
        _lastCompanyDetailIsPlayer = isPlayer;
        _lastCompanyDetailStudio = studio;
        var vp = GetViewport().GetVisibleRect().Size;
        var S = (Func<float, float>)(v => v * UIScale);
        float pw = S(620), ph = vp.Y * 0.88f;
        var panel = new DragPanel { Position = new((vp.X - pw) / 2, (vp.Y - ph) / 2), Size = new(pw, ph), Protected = true };
        panel.SetScale(UIScale);
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f, 0.97f), BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, BorderColor = new Color(0.2f, 0.4f, 0.8f, 0.5f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 });

        // 标题+关闭按钮：RelativeLayout
        var topBar = new RelativeLayout(); topBar.SetPadding(0, 0, 0, 0);
        topBar.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        topBar.OffsetBottom = -(ph - S(44));
        panel.AddChild(topBar);

        var title = LUI.Label($"📋 {Loc.DisplayName(name)}", 18, new Color(0.10f, 0.14f, 0.22f));
        topBar.AddChild(title);
        RelativeLayout.AddRule(title, RelativeLayout.Rule.AlignParentLeft);
        RelativeLayout.AddRule(title, RelativeLayout.Rule.AlignParentTop);
        RelativeLayout.SetMargins(title, S(20), S(10), 0, 0);

        var closeBtn = new Button { Text = "✕", Flat = true };
        closeBtn.AddThemeFontSizeOverride("font_size", 14); closeBtn.AddThemeColorOverride("font_color", new Color(0.8f, 0.3f, 0.3f));
        closeBtn.Pressed += () => { panel.Protected = false; panel.QueueFree(); };
        topBar.AddChild(closeBtn);
        RelativeLayout.AddRule(closeBtn, RelativeLayout.Rule.AlignParentRight);
        RelativeLayout.AddRule(closeBtn, RelativeLayout.Rule.AlignParentTop);
        RelativeLayout.SetMargins(closeBtn, 0, S(8), S(10), 0);

        // 内容区
        float empCnt = isPlayer ? GetTotalEmployees() : studio?.EmployeeCount ?? 0;
        float fanCnt = isPlayer ? (_fanMgr?.TotalFans ?? 0) : studio?.Fans ?? 0;
        float rep = isPlayer ? 60 : studio?.Reputation ?? 50;
        float mon = isPlayer ? _res.Money : studio?.Money ?? 0;
        float mCap = isPlayer && devMgr.IsListed ? devMgr.SharePrice * devMgr.SharesOutstanding : studio is { IsListed: true } ? studio.MarketCap : 0;
        bool listed = isPlayer ? devMgr.IsListed : (studio?.IsListed ?? false);
        float sp = isPlayer ? devMgr.SharePrice : (studio?.SharePrice ?? 0);

        var content = new ScrollContainer();
        content.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        content.OffsetTop = S(44);
        panel.AddChild(content);
        var vc = new VBoxContainer(); vc.AddThemeConstantOverride("separation", (int)S(8));
        var cardStyle = new StyleBoxFlat { BgColor = new Color(1, 1, 1, 0.85f), CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.6f, 0.6f, 0.65f, 0.3f) };

        // 信息卡片行
        float cardW = (pw - S(92) - S(32)) / (listed ? 5 : 4);
        var infoRow = new HBoxContainer(); infoRow.AddThemeConstantOverride("separation", (int)S(8));
        Panel MkCard(string label, string val, Color c)
        {
            var p = new Panel { CustomMinimumSize = new(cardW, S(40)) }; p.AddThemeStyleboxOverride("panel", cardStyle);
            var rl = new RelativeLayout(); rl.SetPadding(S(4), S(3), S(4), S(3));
            rl.SetAnchorsPreset(Control.LayoutPreset.FullRect); p.AddChild(rl);
            var l = LUI.Label(label, 8, new Color(0.4f, 0.45f, 0.55f)); rl.AddChild(l);
            RelativeLayout.AddRule(l, RelativeLayout.Rule.AlignParentLeft); RelativeLayout.AddRule(l, RelativeLayout.Rule.AlignParentTop);
            var v = LUI.Label(val, 13, c); rl.AddChild(v);
            RelativeLayout.AddRule(v, RelativeLayout.Rule.AlignParentLeft); RelativeLayout.AddRule(v, RelativeLayout.Rule.AlignParentBottom);
            return p;
        }
        infoRow.AddChild(MkCard(Loc.Tr("ui.reputation"), $"{rep:F0}", rep > 70 ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.7f, 0.5f, 0.2f)));
        infoRow.AddChild(MkCard(Loc.Tr("ui.employees"), $"{empCnt:F0}", new Color(0.2f, 0.4f, 0.7f)));
        infoRow.AddChild(MkCard(Loc.Tr("ui.fans"), $"{fanCnt:N0}", new Color(0.8f, 0.3f, 0.5f)));
        infoRow.AddChild(MkCard(Loc.Tr("ui.cash"), $"¥{mon:N0}", mon > 0 ? new Color(0.2f, 0.6f, 0.3f) : new Color(0.9f, 0.2f, 0.2f)));
        if (listed) infoRow.AddChild(MkCard(Loc.Tr("comp.stock_price"), $"¥{sp:F1}", sp > 100 ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.7f, 0.3f, 0.2f)));
        vc.AddChild(infoRow);

        // 股价走势
        if (listed)
        {
            var hist = isPlayer ? devMgr.PriceHistory : studio?.PriceHistory ?? new();
            if (hist.Count > 1)
            {
                var chartBox = new Panel { CustomMinimumSize = new(0, S(130)) }; chartBox.AddThemeStyleboxOverride("panel", cardStyle);
                var chartRl = new RelativeLayout(); chartRl.SetPadding(S(6), S(4), S(6), S(4));
                chartRl.SetAnchorsPreset(Control.LayoutPreset.FullRect); chartBox.AddChild(chartRl);
                var chartLbl = LUI.Label(Loc.Tr("comp.stock_chart"), 10, new Color(0.3f, 0.5f, 0.8f));
                chartRl.AddChild(chartLbl); RelativeLayout.AddRule(chartLbl, RelativeLayout.Rule.AlignParentLeft); RelativeLayout.AddRule(chartLbl, RelativeLayout.Rule.AlignParentTop);
                var chartAr = new Control();
                chartRl.AddChild(chartAr); RelativeLayout.AddRule(chartAr, RelativeLayout.Rule.AlignParentLeft); RelativeLayout.AddRule(chartAr, RelativeLayout.Rule.Below, chartLbl);
                RelativeLayout.SetMargins(chartAr, 0, S(4), 0, 0);
                chartAr.Draw += () => DrawStockChart(chartAr, hist, sp);
                vc.AddChild(chartBox);
            }
        }

        // 游戏列表
        var rels = isPlayer ? devMgr.CompletedProjects.Select(p => (name: p.Name, score: p.FinalScore, sales: p.Sales)).ToList() :
                    studio?.Releases.Select(r => (name: r.Name, score: r.Score, sales: r.Sales)).ToList() ?? new();
        var gameBox = new Panel { CustomMinimumSize = new(0, S(130)) }; gameBox.AddThemeStyleboxOverride("panel", cardStyle);
        var gameRl = new RelativeLayout(); gameRl.SetPadding(S(6), S(4), S(6), S(4));
        gameRl.SetAnchorsPreset(Control.LayoutPreset.FullRect); gameBox.AddChild(gameRl);
        var gameLbl = LUI.Label(Loc.Tr("ui.released_games"), 10, new Color(0.3f, 0.5f, 0.8f));
        gameRl.AddChild(gameLbl); RelativeLayout.AddRule(gameLbl, RelativeLayout.Rule.AlignParentLeft); RelativeLayout.AddRule(gameLbl, RelativeLayout.Rule.AlignParentTop);
        var gSc = new ScrollContainer(); gameRl.AddChild(gSc);
        RelativeLayout.AddRule(gSc, RelativeLayout.Rule.Below, gameLbl); RelativeLayout.AddRule(gSc, RelativeLayout.Rule.AlignParentLeft);
        RelativeLayout.AddRule(gSc, RelativeLayout.Rule.AlignParentRight); RelativeLayout.AddRule(gSc, RelativeLayout.Rule.AlignParentBottom);
        RelativeLayout.SetMargins(gSc, 0, S(4), 0, 0);
        var gL = new VBoxContainer();
        foreach (var g in rels.TakeLast(8).Reverse())
        {
            Color gc = g.score >= 80 ? new Color(0.2f, 0.6f, 0.3f) : g.score >= 60 ? new Color(0.6f, 0.5f, 0.1f) : new Color(0.7f, 0.2f, 0.2f);
            string salesStr = g.sales >= 1000 ? $"{g.sales / 1000f:F1}K" : $"{g.sales}";
            gL.AddChild(MkPLabel(Loc.TrF("comp.game_fmt", g.name, g.score, salesStr), 10, gc));
        }
        if (rels.Count == 0) gL.AddChild(MkPLabel(Loc.Tr("comp.no_games"), 10, new Color(0.4f, 0.45f, 0.55f)));
        gSc.AddChild(gL); vc.AddChild(gameBox);

        // 股份分布
        var shareBox = new Panel { CustomMinimumSize = new(0, S(160)) }; shareBox.AddThemeStyleboxOverride("panel", cardStyle);
        var titleLbl = new Label { Text = Loc.Tr("comp.shares_title"), Position = new(S(6), S(4)), Size = new(pw - S(96), S(16)) };
        titleLbl.AddThemeFontSizeOverride("font_size", 10); titleLbl.AddThemeColorOverride("font_color", new Color(0.3f, 0.5f, 0.8f));
        shareBox.AddChild(titleLbl);

        if (!listed)
        {
            var nl = new Label { Text = Loc.Tr("comp.not_listed"), Position = new(S(10), S(24)), Size = new(pw - S(100), S(20)) };
            nl.AddThemeFontSizeOverride("font_size", 11); nl.AddThemeColorOverride("font_color", new Color(0.4f, 0.45f, 0.55f));
            shareBox.AddChild(nl);
        }
        else
        {
            var holders = isPlayer
                ? new Dictionary<string, int> { [Loc.Tr("ui.founder_you")] = devMgr.SharesOutstanding * 30 / 100, [Loc.Tr("ui.public")] = devMgr.SharesOutstanding * 70 / 100 }
                : studio?.Shareholders ?? new();
            if (holders.Count == 0)
            {
                var nl = new Label { Text = Loc.Tr("comp.no_shareholders"), Position = new(S(10), S(24)), Size = new(pw - S(100), S(20)) };
                nl.AddThemeFontSizeOverride("font_size", 11); nl.AddThemeColorOverride("font_color", new Color(0.4f, 0.45f, 0.55f));
                shareBox.AddChild(nl);
            }
            else
            {
                float total = holders.Values.Sum(); if (total <= 0) total = 1;
                var pieColors = new[] { new Color(0.05f, 0.2f, 0.55f), new Color(0.55f, 0.4f, 0.05f), new Color(0.55f, 0.08f, 0.08f), new Color(0.05f, 0.35f, 0.1f) };
                int ci = 0; string pk = Loc.Tr("comp.player_tag"); float yOff = S(24);
                float barMaxW = pw - S(100);
                foreach (var kv in holders)
                {
                    float pct = kv.Value / total;
                    var col = pieColors[ci % pieColors.Length]; ci++;
                    // 背景条
                    var bgBar = new ColorRect { Position = new(S(8), yOff), Size = new(barMaxW, S(16)), Color = new Color(0.85f, 0.85f, 0.88f, 0.5f) };
                    shareBox.AddChild(bgBar);
                    // 填充条
                    var fillBar = new ColorRect { Position = new(S(8), yOff), Size = new(barMaxW * pct, S(16)), Color = col };
                    shareBox.AddChild(fillBar);
                    // 标签：条够宽时在条内（白色/深色字），条窄时在条右侧（深色字）
                    var textColor = (col.R + col.G + col.B < 1.2f) ? Colors.White : new Color(0.05f, 0.08f, 0.15f);
                    float barPx = barMaxW * pct;
                    bool textInside = barPx > S(80);
                    var finalColor = textInside ? textColor : new Color(0.05f, 0.08f, 0.15f);
                    float lblX = textInside ? S(10) : barPx + S(8);
                    var lbl = new Label { Text = $"{kv.Key}  {pct:P0}", Position = new(lblX, yOff + 1), Size = new(barMaxW - lblX, S(14)) };
                    lbl.AddThemeFontSizeOverride("font_size", 10); lbl.AddThemeColorOverride("font_color", finalColor);
                    shareBox.AddChild(lbl);
                    // 按钮
                    if (!isPlayer && kv.Key != pk && kv.Key != Loc.Tr("compname.founder") && kv.Key != Loc.Tr("ui.founder_you") && kv.Value > 0)
                    {
                        int aa = kv.Value; string hn = kv.Key; float pp = studio!.SharePrice;
                        var by = new Button { Text = "+", Flat = true, Position = new(barMaxW + S(12), yOff), CustomMinimumSize = new(18, 18), TooltipText = Loc.Tr("comp.tip_buy") };
                        by.AddThemeFontSizeOverride("font_size", 14); by.AddThemeColorOverride("font_color", new Color(0.2f, 0.6f, 0.3f)); by.AddThemeColorOverride("font_hover_color", new Color(0.1f, 0.4f, 0.15f));
                        by.Pressed += () => ShowBuyStockPopup(studio, hn, aa, pp);
                        shareBox.AddChild(by);
                    }
                    if (!isPlayer && kv.Key == pk && kv.Value > 0)
                    {
                        int held = kv.Value; float pp = studio!.SharePrice;
                        var sl = new Button { Text = "−", Flat = true, Position = new(barMaxW + S(12), yOff), CustomMinimumSize = new(18, 18), TooltipText = Loc.Tr("comp.tip_sell") };
                        sl.AddThemeFontSizeOverride("font_size", 14); sl.AddThemeColorOverride("font_color", new Color(0.8f, 0.3f, 0.2f)); sl.AddThemeColorOverride("font_hover_color", new Color(0.5f, 0.1f, 0.1f));
                        sl.Pressed += () => ShowSellStockPopup(studio, held, pp);
                        shareBox.AddChild(sl);
                    }
                    yOff += S(20);
                }
            }
        }
        vc.AddChild(shareBox);
        content.AddChild(vc); panel.AddChild(content);
        _uiLayer.AddChild(panel); PushPanel(panel);
    }

    private void RefreshCompanyDetailDelayed()
    {
        if (string.IsNullOrEmpty(_lastCompanyDetailName)) return;
        var toRemove = new List<Panel>();
        while (_openPanels.Count > 0)
        {
            var p = _openPanels.Pop();
            if (p is DragPanel && p != null && GodotObject.IsInstanceValid(p))
            {
                if (p is DragPanel adp) adp.Protected = false;
                p.QueueFree();
                break;
            }
            toRemove.Add(p);
        }
        foreach (var p in toRemove) _openPanels.Push(p);
        var devMgr = GetNode<GameDevManager>("GameDevManager");
        ShowCompanyDetail(_lastCompanyDetailName, _lastCompanyDetailIsPlayer, _lastCompanyDetailStudio, devMgr);
    }

    /// <summary>绘制股价折线图</summary>
    private void DrawStockChart(Control area, List<(int month, float price)> history, float currentPrice)
    {
        if (history.Count < 2) return;
        float w = area.Size.X, h = area.Size.Y;
        if (w <= 0 || h <= 0) return;
        float padL = 40, padR = 8, padT = 8, padB = 20;
        float plotW = w - padL - padR, plotH = h - padT - padB;

        float minP = history.Min(p => p.price) * 0.9f;
        float maxP = history.Max(p => p.price) * 1.1f;
        if (maxP - minP < 1) { minP -= 5; maxP += 5; }

        // 网格线
        for (int i = 0; i <= 4; i++)
        {
            float y = padT + plotH * i / 4f;
            area.DrawLine(new Vector2(padL, y), new Vector2(padL + plotW, y), new Color(0.7f, 0.7f, 0.75f, 0.3f));
            float val = maxP - (maxP - minP) * i / 4f;
            area.DrawString(ThemeDB.FallbackFont, new Vector2(2, y + 4), val.ToString("F1"), HorizontalAlignment.Left, -1, 9, new Color(0.3f, 0.3f, 0.4f));
        }

        // 折线
        var points = new List<Vector2>();
        for (int i = 0; i < history.Count; i++)
        {
            float x = padL + plotW * i / (history.Count - 1);
            float y = padT + plotH * (1f - (history[i].price - minP) / (maxP - minP));
            points.Add(new(x, y));
        }

        // 画填充区域（渐变）
        var fillColor = new Color(0.2f, 0.5f, 0.9f, 0.15f);
        for (int i = 0; i < points.Count - 1; i++)
        {
            var p1 = points[i]; var p2 = points[i + 1];
            area.DrawRect(new Rect2(p1.X, p1.Y, p2.X - p1.X, padT + plotH - p1.Y), fillColor);
        }

        // 画线
        for (int i = 0; i < points.Count - 1; i++)
        {
            Color lineColor = history[i + 1].price >= history[i].price ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.9f, 0.2f, 0.2f);
            area.DrawLine(points[i], points[i + 1], lineColor, 2f);
        }

        // 终点标记
        var last = points[^1];
        area.DrawCircle(last, 4, new Color(0.2f, 0.5f, 0.9f));
        area.DrawString(ThemeDB.FallbackFont, new Vector2(last.X - 10, last.Y - 8), $"¥{currentPrice:F1}", HorizontalAlignment.Left, -1, 10, new Color(0.1f, 0.15f, 0.25f));
    }

    private void ShowBuyStockPopup(AIStudio studio, string holderName, int available, float pricePerShare)
    {
        var vp = GetViewport().GetVisibleRect().Size;
        var S = (Func<float, float>)(v => v * UIScale);
        int maxAfford = Mathf.Min(available, (int)(_res.Money / pricePerShare));
        if (maxAfford <= 0) return;
        float pw = S(400);
        float tPad = S(8), bPad = S(8), titleH = S(24), infoH = S(90), sliderH = S(24), costH = S(20), btnH = S(34), sep = S(6);
        float totalH = tPad + titleH + sep + infoH + sep + sliderH + sep + costH + sep + btnH + bPad;
        float px = Mathf.Clamp((vp.X - pw) / 2, 10, vp.X - pw - 10);
        float py = Mathf.Clamp((vp.Y - totalH) / 2, 10, vp.Y - totalH - 10);
        var panel = new DragPanel { Position = new(px, py), Size = new(pw, totalH) };
        panel.SetScale(UIScale);
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.95f, 0.94f, 0.92f, 0.98f), CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10 });

        var vbox = new VBoxContainer { Position = new(S(16), tPad), Size = new(pw - S(32), totalH - tPad - bPad) };
        vbox.AddThemeConstantOverride("separation", (int)sep);
        panel.AddChild(vbox);

        var title = new Label { Text = Loc.TrF("ui.buy_stock_title") + $" - {studio.Name}" };
        title.AddThemeFontSizeOverride("font_size", 16); title.AddThemeColorOverride("font_color", new Color(0.10f, 0.14f, 0.22f));
        vbox.AddChild(title);

        var info = new Label { Text = $"{Loc.Tr("ui.seller")}: {holderName}\n{Loc.Tr("ui.available")}: {available} {Loc.Tr("ui.shares")}\n{Loc.Tr("ui.price")}: ¥{pricePerShare:F2}/{Loc.Tr("ui.shares")}\n\n{Loc.Tr("ui.max_buy")}: {maxAfford} {Loc.Tr("ui.shares")} (¥{maxAfford * pricePerShare:N0})" };
        info.AddThemeFontSizeOverride("font_size", 12); info.AddThemeColorOverride("font_color", new Color(0.15f, 0.18f, 0.25f));
        info.AutowrapMode = TextServer.AutowrapMode.Word;
        info.CustomMinimumSize = new(0, infoH);
        vbox.AddChild(info);

        var slider = new HSlider { MinValue = 1, MaxValue = maxAfford, Step = 1, Value = Mathf.Max(1, maxAfford / 2), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        vbox.AddChild(slider);

        var costLabel = new Label { Text = $"{Loc.Tr("ui.total_price")}: ¥{slider.Value * pricePerShare:N0}" };
        costLabel.AddThemeFontSizeOverride("font_size", 14); costLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.3f, 0.2f));
        vbox.AddChild(costLabel);
        slider.ValueChanged += (v) => { costLabel.Text = $"{Loc.Tr("ui.total_price")}: ¥{v * pricePerShare:N0}"; };

        // 按钮行
        var btnRow = new HBoxContainer();
        btnRow.AddThemeConstantOverride("separation", (int)S(10));
        var buyBtn = new Button { Text = Loc.Tr("ui.confirm_buy"), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        buyBtn.AddThemeFontSizeOverride("font_size", 13);
        buyBtn.Pressed += () =>
        {
            int qty = (int)slider.Value;
            float cost = qty * pricePerShare;
            if (_res.Money < cost) return;
            _res.SpendMoney(cost, "buy_stock");
            studio.Shareholders[holderName] -= qty;
            if (studio.Shareholders[holderName] <= 0) studio.Shareholders.Remove(holderName);
            string playerKey = Loc.Tr("comp.player_tag");
            studio.Shareholders.TryGetValue(playerKey, out int have);
            studio.Shareholders[playerKey] = have + qty;
            panel.QueueFree();
            _needCompanyDetailRefresh = true;
        };
        btnRow.AddChild(buyBtn);

        var cancelBtn = new Button { Text = Loc.Tr("ui.cancel"), Flat = true };
        cancelBtn.AddThemeFontSizeOverride("font_size", 13); cancelBtn.AddThemeColorOverride("font_color", new Color(0.15f, 0.15f, 0.2f)); cancelBtn.AddThemeColorOverride("font_hover_color", new Color(0.4f, 0.4f, 0.4f));
        cancelBtn.Pressed += () => panel.QueueFree();
        btnRow.AddChild(cancelBtn);
        vbox.AddChild(btnRow);

        _uiLayer.AddChild(panel);
        PushPanel(panel);
    }

    private void ShowSellStockPopup(AIStudio studio, int heldShares, float pricePerShare)
    {
        var vp = GetViewport().GetVisibleRect().Size;
        var S = (Func<float, float>)(v => v * UIScale);
        float pw = S(400);
        float tPad = S(8), bPad = S(8), titleH = S(24), infoH = S(70), sliderH = S(24), costH = S(20), btnH = S(34), sep = S(6);
        float totalH = tPad + titleH + sep + infoH + sep + sliderH + sep + costH + sep + btnH + bPad;
        float px = Mathf.Clamp((vp.X - pw) / 2, 10, vp.X - pw - 10);
        float py = Mathf.Clamp((vp.Y - totalH) / 2, 10, vp.Y - totalH - 10);
        var panel = new DragPanel { Position = new(px, py), Size = new(pw, totalH) };
        panel.SetScale(UIScale);
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.95f, 0.94f, 0.92f, 0.98f), CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10 });
        _uiLayer.AddChild(panel);

        var vbox = new VBoxContainer { Position = new(S(16), tPad), Size = new(pw - S(32), totalH - tPad - bPad) };
        vbox.AddThemeConstantOverride("separation", (int)sep);
        panel.AddChild(vbox);

        var title = new Label { Text = Loc.Tr("comp.sell_title") };
        title.AddThemeFontSizeOverride("font_size", 16); title.AddThemeColorOverride("font_color", new Color(0.10f, 0.14f, 0.22f));
        vbox.AddChild(title);

        float totalValue = heldShares * pricePerShare;
        var info = new Label { Text = Loc.TrF("comp.sell_info", heldShares, pricePerShare, totalValue) };
        info.AddThemeFontSizeOverride("font_size", 12); info.AddThemeColorOverride("font_color", new Color(0.15f, 0.18f, 0.25f));
        info.AutowrapMode = TextServer.AutowrapMode.Word;
        info.CustomMinimumSize = new(0, infoH);
        vbox.AddChild(info);

        // 滑块 + 全部按钮行
        var sliderRow = new HBoxContainer();
        sliderRow.AddThemeConstantOverride("separation", (int)S(6));
        var slider = new HSlider { MinValue = 1, MaxValue = heldShares, Step = 1, Value = Mathf.Max(1, heldShares / 2), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        sliderRow.AddChild(slider);
        var allBtn = new Button { Text = Loc.Tr("comp.sell_all"), Flat = true, CustomMinimumSize = new(40, 0) };
        allBtn.AddThemeFontSizeOverride("font_size", 10); allBtn.AddThemeColorOverride("font_color", new Color(0.5f, 0.2f, 0.2f));
        allBtn.Pressed += () => { slider.Value = heldShares; };
        sliderRow.AddChild(allBtn);
        vbox.AddChild(sliderRow);

        var valueLabel = new Label { Text = Loc.TrF("comp.sell_value", slider.Value * pricePerShare) };
        valueLabel.AddThemeFontSizeOverride("font_size", 14); valueLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.7f, 0.3f));
        vbox.AddChild(valueLabel);
        slider.ValueChanged += (v) => { valueLabel.Text = Loc.TrF("comp.sell_value", v * pricePerShare); };

        // 按钮行
        var btnRow = new HBoxContainer();
        btnRow.AddThemeConstantOverride("separation", (int)S(10));
        var sellBtn = new Button { Text = Loc.Tr("comp.sell_confirm"), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        sellBtn.AddThemeColorOverride("font_color", new Color(0.8f, 0.3f, 0.2f));
        sellBtn.Pressed += () =>
        {
            int qty = (int)slider.Value;
            float revenue = qty * pricePerShare;
            _res.EarnMoney(revenue, "stock");
            string playerKey = Loc.Tr("comp.player_tag");
            if (studio.Shareholders.ContainsKey(playerKey))
            {
                studio.Shareholders[playerKey] -= qty;
                if (studio.Shareholders[playerKey] <= 0) studio.Shareholders.Remove(playerKey);
            }
            string publicKey = Loc.Tr("comp.shareholder_public");
            studio.Shareholders.TryGetValue(publicKey, out int pub);
            studio.Shareholders[publicKey] = pub + qty;
            float impact = qty / (float)studio.SharesOutstanding * 10f;
            studio.SharePrice = Mathf.Max(3f, studio.SharePrice * (1f - impact));
            panel.QueueFree();
            _needCompanyDetailRefresh = true;
        };
        btnRow.AddChild(sellBtn);

        var cancelBtn = new Button { Text = Loc.Tr("ui.cancel"), Flat = true };
        cancelBtn.AddThemeFontSizeOverride("font_size", 13); cancelBtn.AddThemeColorOverride("font_color", new Color(0.15f, 0.15f, 0.2f)); cancelBtn.AddThemeColorOverride("font_hover_color", new Color(0.4f, 0.4f, 0.4f));
        cancelBtn.Pressed += () => panel.QueueFree();
        btnRow.AddChild(cancelBtn);
        vbox.AddChild(btnRow);
    }

    private int _newsPage;       // 历史播报页码

    private void ShowNewsHistoryPanel()
    {
        // 如果已有旧面板，移除
        if (_openPanels.Count > 0)
        {
            var top = _openPanels.Peek();
            if (GodotObject.IsInstanceValid(top) && top.IsInsideTree())
                top.QueueFree();
        }

        var vp = GetViewport().GetVisibleRect().Size;
        var S = (Func<float, float>)(v => v * UIScale);
        float pw = S(580), ph = vp.Y * 0.8f;
        var panel = new DragPanel { Position = new((vp.X - pw) / 2, (vp.Y - ph) / 2), Size = new(pw, ph) };
        panel.SetScale(UIScale);
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.96f, 0.95f, 0.93f, 0.98f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8, BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, BorderColor = new Color(0.2f, 0.4f, 0.8f, 0.5f) });
        panel.MouseFilter = Control.MouseFilterEnum.Stop;

        var devMgr = GetNode<GameDevManager>("GameDevManager");
        var news = _competitor.NewsFeed;
        int pageSize = 50;
        int maxPage = Mathf.Max(1, (news.Count + pageSize - 1) / pageSize);
        _newsPage = Mathf.Clamp(_newsPage, 0, maxPage - 1);

        var title = new Label { Text = $"📰 {Loc.Tr("ui.history_log")} ({news.Count})  {_newsPage+1}/{maxPage}", Position = new(S(20), S(10)), Size = new(pw - S(80), S(28)) };
        title.AddThemeFontSizeOverride("font_size", 18); title.AddThemeColorOverride("font_color", new Color(0.10f, 0.14f, 0.22f));
        panel.AddChild(title);

        var closeBtn = new Button { Text = "✕", Position = new(pw - S(40), S(10)), Size = new(S(30), S(30)), Flat = true };
        closeBtn.AddThemeFontSizeOverride("font_size", 14);
        closeBtn.AddThemeColorOverride("font_color", new Color(0.8f, 0.3f, 0.3f));
        closeBtn.AddThemeColorOverride("font_hover_color", new Color(0.45f, 0.45f, 0.45f));
        closeBtn.AddThemeColorOverride("font_pressed_color", new Color(0.3f, 0.3f, 0.3f));
        closeBtn.Pressed += () => { CloseAll(); _newsPage = 0; };
        panel.AddChild(closeBtn);

        // 分页按钮
        if (maxPage > 1)
        {
            var pageBar = new HBoxContainer { Position = new(S(20), S(40)) };
            string prevLabel = _newsPage > 0 ? $"◀ {Loc.Tr("ui.prev_page")}" : "◀";
            string nextLabel = _newsPage < maxPage - 1 ? $"{Loc.Tr("ui.next_page")} ▶" : "▶";
            var prevBtn = new Button { Text = prevLabel, Flat = true, Disabled = _newsPage <= 0 };
            prevBtn.AddThemeFontSizeOverride("font_size", 11);
            prevBtn.AddThemeColorOverride("font_color", new Color(0.05f, 0.05f, 0.1f));
            prevBtn.AddThemeColorOverride("font_hover_color", new Color(0.05f, 0.05f, 0.1f));
            prevBtn.AddThemeColorOverride("font_disabled_color", new Color(0.5f, 0.5f, 0.55f));
            prevBtn.Pressed += () => { _newsPage--; ShowNewsHistoryPanel(); };
            pageBar.AddChild(prevBtn);
            pageBar.AddChild(MkPLabel($"  {_newsPage+1}/{maxPage}  ", 12, new Color(0.08f, 0.12f, 0.2f)));
            var nextBtn = new Button { Text = nextLabel, Flat = true, Disabled = _newsPage >= maxPage - 1 };
            nextBtn.AddThemeFontSizeOverride("font_size", 11);
            nextBtn.AddThemeColorOverride("font_color", new Color(0.05f, 0.05f, 0.1f));
            nextBtn.AddThemeColorOverride("font_hover_color", new Color(0.05f, 0.05f, 0.1f));
            nextBtn.AddThemeColorOverride("font_disabled_color", new Color(0.5f, 0.5f, 0.55f));
            nextBtn.Pressed += () => { _newsPage++; ShowNewsHistoryPanel(); };
            pageBar.AddChild(nextBtn);
            panel.AddChild(pageBar);
        }

        var sc = new ScrollContainer { Position = new(S(20), S(maxPage > 1 ? 72 : 44)), Size = new(pw - S(40), ph - S(maxPage > 1 ? 84 : 56)) };
        var list = new VBoxContainer();
        list.AddThemeConstantOverride("separation", (int)(UIScale * 3));

        int startIdx = news.Count - 1 - _newsPage * pageSize;
        int endIdx = Mathf.Max(0, startIdx - pageSize + 1);
        for (int i = startIdx; i >= endIdx; i--)
        {
            var n = news[i];
            int y = n.Month / 12 + 1;
            int m = n.Month % 12 + 1;

            var row = new HBoxContainer();

            var dateLbl = new Label { Text = $"{2025+y-1}.{m:D2}", CustomMinimumSize = new(S(60), 0) };
            dateLbl.AddThemeFontSizeOverride("font_size", 10); dateLbl.AddThemeColorOverride("font_color", new Color(0.3f, 0.3f, 0.35f));
            row.AddChild(dateLbl);

            string fullText = $"{n.Emoji} {n.Headline} — {n.Detail}";

            string foundCompany = null;
            foreach (var s in _competitor.Studios)
            {
                if (!s.IsAcquired && fullText.Contains(s.Name))
                {
                    foundCompany = s.Name;
                    break;
                }
            }

            if (foundCompany != null)
            {
                var btn = new Button { Text = fullText, Flat = true };
                btn.Alignment = HorizontalAlignment.Left;
                btn.AddThemeFontSizeOverride("font_size", 10);
                btn.AddThemeColorOverride("font_color", n.Color);
                btn.AddThemeColorOverride("font_hover_color", n.Color);
                btn.AddThemeColorOverride("font_pressed_color", n.Color);
                btn.SizeFlagsHorizontal = Control.SizeFlags.Fill;
                string captureCompany = foundCompany;
                btn.Pressed += () =>
                {
                    var studio = _competitor.Studios.FirstOrDefault(s => s.Name == captureCompany && !s.IsAcquired);
                    if (studio != null)
                        ShowCompanyDetail(captureCompany, false, studio, devMgr);
                };
                row.AddChild(btn);
            }
            else
            {
                var lbl = new Label { Text = fullText };
                lbl.AddThemeFontSizeOverride("font_size", 10); lbl.AddThemeColorOverride("font_color", n.Color);
                lbl.SizeFlagsHorizontal = Control.SizeFlags.Fill;
                row.AddChild(lbl);
            }

            list.AddChild(row);
        }
        sc.AddChild(list);
        panel.AddChild(sc);

        _uiLayer.AddChild(panel);
        PushPanel(panel);
    }

    private Button MkButtonS(string text, float w, float h)
    {
        var b = new Button { Text = text, CustomMinimumSize = new(UIScale * w, UIScale * h) };
        b.AddThemeFontSizeOverride("font_size", 11);
        return b;
    }

    private int GetTotalEmployees() => _empMgr.Employees.Count;

    public void MarkHUDDirty() { _hudNeedsFullRefresh = true; _predictionDirty = true; }

    private void UpdateHUDTimeSensitive()
    {
        if (_res == null) return;
        string t = $"{_res.Money:N0}";
        if (_lastMoneyTxt != t) { _lastMoneyTxt = t; if (_moneyLabel != null) _moneyLabel.Text = t; }

        bool ps = Paused;
        if (_lastPaused != ps) { _lastPaused = ps; if (_pauseBtn != null) _pauseBtn.Text = ps ? "▶" : "⏸"; }

        int spd = _gameSpeed;
        if (_lastSpeed != spd) { _lastSpeed = spd; if (_speedOpt?.Selected != spd - 1) _speedOpt.Selected = spd - 1; }
    }

    private void UpdateHUDFull()
    {
        // 防御：服务未就绪时跳过
        if (_empMgr == null || _roomMgr == null || _res == null) return;

        string t;
        t = $"{_res.Inspiration:F0}/{_res.MaxInspiration:F0}";
        if (_lastInspTxt != t) { _lastInspTxt = t; _inspirationLabel.Text = t; }

        // ── 债务警示灯 ──
        float totalDebt = Services.TechDebtManager.ComputeTotalDebt();
        string debtTxt = "";
        Color debtColor = Colors.Transparent;
        if (totalDebt > 80) { debtTxt = "🔴"; debtColor = new Color(0.95f, 0.2f, 0.2f); }
        else if (totalDebt > 50) { debtTxt = "🟡"; debtColor = new Color(0.95f, 0.7f, 0.15f); }
        else if (totalDebt > 25) { debtTxt = "⚪"; debtColor = new Color(0.6f, 0.6f, 0.5f); }
        if (_lastDebtTxt != debtTxt) { _lastDebtTxt = debtTxt; _debtWarnLabel.Text = debtTxt; _debtWarnLabel.SelfModulate = debtColor; }

        // 历史最佳评审分
        float bestHist = Services.GameDevManager.CompletedProjects.Count > 0 ? Services.GameDevManager.CompletedProjects.Max(p => p.FinalScore) : 0;
        string bestText = bestHist > 0 ? $"🏆 {bestHist:F0}" : "";
        if (_lastBestScore != bestText) { _lastBestScore = bestText; _bestScoreLabel.Text = bestText; }

        t = Loc.TrF("fan.topbar", _fanMgr.TotalFans, _fanMgr.DiehardFans);
        if (_lastFanTxt != t) { _lastFanTxt = t; _fanLabel.Text = t; }

        t = Loc.TrF("date.year_month_fmt", 2025 + GameYear - 1, MonthInYear);
        if (_lastDateTxt != t) { _lastDateTxt = t; _dateLabel.Text = t; }

        t = Loc.TrF("emp.topbar", _empMgr.Employees.Count, _roomMgr.TotalCapacity);
        if (_lastEmpTxt != t) { _lastEmpTxt = t; _empLabel.Text = t; }

        float trust = _devMgr.PlayerTrust;
        string trustTxt = trust >= 70 ? $"💚 {trust:F0}" : trust >= 40 ? $"💛 {trust:F0}" : $"❤️ {trust:F0}";
        _trustLabel.Text = Loc.TrF("hud.trust", trustTxt);

        // ── 经济周期指示器 ──
        var ecoLabel = _uiLayer.GetNodeOrNull<Label>("EcoPhaseLabel");
        if (ecoLabel != null && _longTermSys != null)
        {
            ecoLabel.Text = _longTermSys.CurrentPhase switch
            {
                EconomyPhase.Boom => "📈 繁荣",
                EconomyPhase.Recession => "📉 衰退",
                EconomyPhase.Depression => "🔻 萧条",
                EconomyPhase.Recovery => "📊 复苏",
                _ => ""
            };
        }
    }

    private void UpdatePredictionPanel()
    {
        if (_predictionLabel?.GetParent() == null) return;
        var devTeam = _teamMgr.Teams.Find(t => t.CurrentProject != null);
        if (devTeam == null || devTeam.CurrentProject == null)
        {
            _predictionLabel.GetParent<Panel>().Visible = false;
            return;
        }
        var proj = devTeam.CurrentProject;
        var parent = _predictionLabel.GetParent<Panel>();
        parent.Visible = true;

        if (proj.Phase == DevPhase.Polishing)
        {
            _predictionLabel.Text = Loc.TrF("pred.polish_fmt", proj.Name, proj.PolishMonths, proj.BugCount, proj.ScoreTierIcon + proj.ScoreTier, proj.BaseQualityScore);
            _predictionLabel.AddThemeColorOverride("font_color", proj.BugCount < 5 ? new Color(0.3f, 1f, 0.4f) : new Color(0.9f, 0.5f, 0.1f));
            return;
        }

        float remainingMonths = Mathf.Max(0, proj.EstimatedMonths - proj.MonthsSpent);
        float estScore = proj.FinalScore > 0 ? proj.FinalScore : Mathf.Clamp((proj.GameplayScore + proj.GraphicsScore + proj.AudioScore + proj.StoryScore + proj.NetworkScore + proj.StabilityScore) / 6f * 0.85f - proj.BugCount * 0.3f, 10, 95);

        string phase = proj.Phase.Name();

        _predictionLabel.Text = Loc.TrF("pred.dev_fmt", proj.Name, phase, remainingMonths, proj.ScoreTierIcon + proj.ScoreTier, proj.BaseProgramScore, proj.BaseArtScore, proj.BugCount);
        _predictionLabel.AddThemeColorOverride("font_color", estScore >= 80
            ? new Color(0.2f, 0.7f, 0.3f)
            : estScore >= 60 ? new Color(0.9f, 0.5f, 0.1f) : new Color(1f, 0.3f, 0.3f));
    }

    public void RefreshHUD() { _lastMoneyTxt = null; MarkHUDDirty(); }

    /// <summary>根据房子尺寸自动调整相机距离，保证覆盖整栋房子</summary>
    public void AdjustCameraToHouse(Vector3 houseSize)
    {
        float diag = Mathf.Sqrt(houseSize.X * houseSize.X + houseSize.Z * houseSize.Z);
        // 需要距离保证房子对角线 + 边距在视野内
        _camDist = Mathf.Clamp(diag * 0.85f + 2f, 8f, _camMaxDist);
        _camTarget = new Vector3(0, houseSize.Y * 0.5f, 0);
    }

    // ══════════════════ 暂停菜单 (ESC) ══════════════════

    private void TogglePauseMenu()
    {
        if (_pausePanel != null) { ClosePauseMenu(); return; }

        Paused = true;
        _soundMgr?.PlayMenuBgm();
        _pausePanel = new Panel();
        _pausePanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        var vp = GetViewport().GetVisibleRect().Size;
        var S = (Func<float, float>)(v => v * UIScale);

        // 暗色遮罩 - 覆盖整个 UI 层（包括顶部和底部导航栏）
        var overlay = new ColorRect { Position = Vector2.Zero, Size = vp, Color = new Color(0.97f, 0.96f, 0.94f, 0.6f) };
        overlay.GuiInput += (ev) => { }; // 吞掉点击（防止穿透）
        _pauseOverlay = overlay;
        _pausePanel.AddChild(overlay);

        float pw = vp.X * 0.40f;
        float infoY = S(56);
        float btnH = S(38), gap = S(10);
        int btnCount = 6;
        float contentH = infoY + S(34) + (btnH + gap) * btnCount - gap + S(20);
        float ph = contentH;
        float px = (vp.X - pw) / 2, py = (vp.Y - ph) / 2;
        var card = new DragPanel { Position = new(px, py), Size = new(pw, ph) };
        card.SetScale(UIScale);
        card.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.97f, 0.96f, 0.94f, 0.97f),
            BorderWidthLeft = 2, BorderWidthTop = 2,
            BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.3f, 0.5f, 0.8f, 0.6f),
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12
        });
        _pausePanel.AddChild(card);

        var title = new Label { Text = Loc.Tr("proj.pause_title"), Position = new(S(24), S(20)), Size = new(pw - S(48), S(36)) };
        title.AddThemeFontSizeOverride("font_size", 26);
        title.AddThemeColorOverride("font_color", new Color(0.15f, 0.4f, 0.7f));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        card.AddChild(title);

        // ── 元信息行 ──
        var info = new Label
        {
            Text = Loc.TrF("proj.pause_header_fmt", GameYear, MonthInYear, _res.Money, _res.TechDebt),
            Position = new(S(24), infoY), Size = new(pw - S(48), S(24))
        };
        info.AddThemeFontSizeOverride("font_size", 13);
        info.AddThemeColorOverride("font_color", new Color(0.55f, 0.6f, 0.7f));
        info.HorizontalAlignment = HorizontalAlignment.Center;
        card.AddChild(info);

        // ── 菜单按钮 ──
        float btnW = pw - S(80), startY = infoY + S(44);
        string[] labels = { Loc.Tr("proj.resume_btn"), Loc.Tr("ui.save_game"), Loc.Tr("ui.load_game"), Loc.Tr("ui.settings"), Loc.Tr("ui.back_menu"), Loc.Tr("ui.quit_game") };
        Action[] actions = { ClosePauseMenu, ShowSavePanel, ShowLoadPanel, ShowSettingsPanel, BackToMainMenu, QuitGame };

        for (int i = 0; i < labels.Length; i++)
        {
            int idx = i;
            float btnY = startY + (btnH + gap) * i;
            var btn = new Button { Text = labels[i], Position = new(S(40), btnY), Size = new(btnW, btnH) };
            btn.AddThemeFontSizeOverride("font_size", 17);
            btn.Pressed += () => actions[idx]();
            card.AddChild(btn);
        }

        _uiLayer.AddChild(_pausePanel);
    }

    private Panel _simplePausePanel;
    private ColorRect _simplePauseOverlay;

    /// <summary>显示简单暂停指示（屏幕中间上方，白圆角底黑字）</summary>
    private CanvasLayer _logOverlay;
    private void ShowModLog()
    {
        if (_logOverlay != null) { _logOverlay.QueueFree(); _logOverlay = null; return; }
        string logText = DlcManager.ReadLog();
        if (string.IsNullOrEmpty(logText)) logText = "[DlcManager] No log entries yet.\n[DlcManager] Mod load status will appear here after restart.";

        _logOverlay = new CanvasLayer { Layer = 128 };
        AddChild(_logOverlay);

        var vp = GetViewport().GetVisibleRect().Size;
        var panel = new Panel { Position = new(40, 40), Size = new(vp.X - 80, vp.Y - 80) };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.08f, 0.08f, 0.12f, 0.95f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 });
        _logOverlay.AddChild(panel);

        var title = new Label { Text = "📋 Mod Log  [F9]", Position = new(20, 12), Size = new(300, 30) };
        title.AddThemeFontSizeOverride("font_size", 16);
        title.AddThemeColorOverride("font_color", new Color(0.8f, 0.9f, 1f));
        panel.AddChild(title);

        float scrollW = vp.X - 100;
        var scroll = new ScrollContainer { Position = new(10, 50), Size = new(scrollW, panel.Size.Y - 70) };
        panel.AddChild(scroll);

        var label = new Label();
        label.Text = logText;
        label.AutowrapMode = TextServer.AutowrapMode.Arbitrary;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.CustomMinimumSize = new(scrollW - 20, 0);
        label.AddThemeFontSizeOverride("font_size", 11);
        label.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 0.9f));
        scroll.AddChild(label);

        var close = new Button { Text = "✕ Close", Position = new(panel.Size.X - 100, 10), Size = new(80, 28), FocusMode = Control.FocusModeEnum.None };
        close.AddThemeFontSizeOverride("font_size", 12);
        close.Pressed += () => { _logOverlay?.QueueFree(); _logOverlay = null; };
        panel.AddChild(close);
    }

    private void ShowSimplePause()
    {
        if (_simplePausePanel != null) return;
        var vp = GetViewport().GetVisibleRect().Size;
        // 半透明遮罩
        _simplePauseOverlay = new ColorRect
        {
            Position = Vector2.Zero,
            Size = vp,
            Color = new Color(0, 0, 0, 0.25f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _uiLayer.AddChild(_simplePauseOverlay);
        float w = 200, h = 60;
        _simplePausePanel = new Panel
        {
            Position = new((int)((vp.X - w) / 2), Math.Max(0, (int)(vp.Y * 0.25f - h / 2))),
            Size = new(w, h)
        };
        _simplePausePanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(1, 1, 1, 0.95f),
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12
        });
        var label = new Label
        {
            Text = Loc.Tr("proj.pause_title"),
            Position = new(0, 0), Size = new(w, h),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeColorOverride("font_color", new Color(0, 0, 0));
        _simplePausePanel.AddChild(label);
        _simplePausePanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        _uiLayer.AddChild(_simplePausePanel);
    }

    private void CloseSimplePause()
    {
        if (_simplePauseOverlay != null)
        {
            _simplePauseOverlay.QueueFree();
            _simplePauseOverlay = null;
        }
        if (_simplePausePanel != null)
        {
            _simplePausePanel.QueueFree();
            _simplePausePanel = null;
        }
    }

    private void ClosePauseMenu()
    {
        _pausePanel?.QueueFree();
        _pausePanel = null;
        Paused = false;
        _soundMgr?.PlayGameBgm();
    }

    /// <summary>显示冲刺规划面板</summary>
    private void ShowSprintPlanning(Team team)
    {
        var proj = team.CurrentProject;
        if (proj == null) return;

        var vp = GetViewport().GetVisibleRect().Size;
        float S(float v) => v * UIScale;
        var panel = new Panel { Position = new(S(40), S(40)), Size = new(vp.X - S(80), vp.Y - S(80)) };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f, 0.97f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.3f, 0.5f, 0.8f, 0.5f), CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10 });
        _uiLayer.AddChild(panel);
        IsAnyModalOpen = true;
        panel.TreeExiting += () => IsAnyModalOpen = false;

        float y = 15;
        var titleL = new Label { Text = Loc.TrF("sprint.title", proj.Name), Position = new(S(20), y), Size = new(panel.Size.X - S(40), S(30)) };
        titleL.AddThemeFontSizeOverride("font_size", 20); titleL.AddThemeColorOverride("font_color", new Color(0.15f, 0.3f, 0.6f));
        panel.AddChild(titleL); y += S(40);

        int available = _sprintSys.GetAvailablePoints(team);
        var infoL = new Label { Text = Loc.TrF("sprint.points", available, Mathf.RoundToInt(proj.DevProgress * 100)), Position = new(S(20), y), Size = new(panel.Size.X - S(40), S(20)) };
        infoL.AddThemeFontSizeOverride("font_size", 12); infoL.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
        panel.AddChild(infoL); y += S(26);
        var remainL = new Label { Text = "", Position = new(S(20), y), Size = new(panel.Size.X - S(40), S(16)) };
        remainL.AddThemeFontSizeOverride("font_size", 10); remainL.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        panel.AddChild(remainL); y += S(20);

        var selected = new List<SprintTask>();
        int totalPts = 0;
        var listContainer = new VBoxContainer { Position = new(S(20), y), Size = new(panel.Size.X - S(40), panel.Size.Y - y - S(70)) };
        panel.AddChild(listContainer);

        var scroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        listContainer.AddChild(scroll);
        var taskList = new VBoxContainer();
        scroll.AddChild(taskList);

        foreach (var task in SprintDefinitions.AllTasks)
        {
            var row = new HBoxContainer();
            var cb = new CheckBox();
            cb.AddThemeColorOverride("font_color", new Color(0.08f, 0.12f, 0.20f));
            var label = new Label { Text = Loc.Tr(task.NameKey), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            label.AddThemeFontSizeOverride("font_size", 13);
            label.AddThemeColorOverride("font_color", new Color(0.08f, 0.12f, 0.20f));
            var descL = new Label { Text = Loc.Tr(task.DescKey), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, AutowrapMode = TextServer.AutowrapMode.Word };
            descL.AddThemeFontSizeOverride("font_size", 9);
            descL.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.6f));
            var ptsL = new Label { Text = $"{task.StoryPoints}{Loc.Tr("sprint.pts")}", CustomMinimumSize = new(S(40), 0) };
            ptsL.AddThemeFontSizeOverride("font_size", 11); ptsL.AddThemeColorOverride("font_color", new Color(0.6f, 0.5f, 0.2f));

            cb.Toggled += (on) =>
            {
                if (on) { selected.Add(task); totalPts += task.StoryPoints; }
                else { selected.Remove(task); totalPts -= task.StoryPoints; }
                int remain = available - totalPts;
                remainL.Text = remain >= 0 ? $"剩余 {remain}/{available} 点" : $"超出 {Math.Abs(remain)} 点！";
                remainL.AddThemeColorOverride("font_color", remain >= 0 ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.9f, 0.3f, 0.3f));
            };
            var taskVbox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(0, 34) };
            taskVbox.AddThemeConstantOverride("separation", 2);
            taskVbox.AddChild(label);
            taskVbox.AddChild(descL);
            row.AddChild(cb); row.AddChild(taskVbox); row.AddChild(ptsL);
            taskList.AddChild(row);
        }

        var confirmBtn = new Button { Text = Loc.Tr("sprint.confirm"), Position = new(S(20), panel.Size.Y - S(60)), Size = new(panel.Size.X - S(40), S(36)) };
        confirmBtn.AddThemeFontSizeOverride("font_size", 15);
        confirmBtn.Pressed += () =>
        {
            if (selected.Count == 0) return;
            _sprintSys.ExecuteSprint(team, selected);
            panel.QueueFree();
        };
        panel.AddChild(confirmBtn);


    }

    private void CloseSaveLoad()
    {
        _saveLoadPanel?.QueueFree();
        _saveLoadPanel = null;
    }

    // ── 合并项目面板：新项目按钮 + 项目列表在一页 ──
    private void ShowProjectPanel()
    {
        var vp = GetViewport().GetVisibleRect().Size;
        float S(float v) => v * UIScale;

        var overlayPanel = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore };
        var overlay = new ColorRect { Position = Vector2.Zero, Size = vp, Color = new Color(0.97f, 0.96f, 0.94f, 0.6f) };
        overlay.GuiInput += (_) => { };
        overlayPanel.AddChild(overlay);

        float pw = vp.X * 0.55f;
        var projects = _devMgr.Projects.FindAll(p => !p.IsReleased);
        var completed = _devMgr.CompletedProjects;
        int totalRows = Mathf.Max(projects.Count + completed.Count, 1);
        float rowH = S(44), gap = S(8);
        float listH = Mathf.Min(totalRows * (rowH + gap), vp.Y * 0.45f);
        float ph = S(100) + listH + S(40);

        var card = new DragPanel { Position = new((vp.X - pw) / 2, (vp.Y - ph) / 2), Size = new(pw, ph) };
        card.SetScale(UIScale);
        card.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.97f, 0.96f, 0.94f, 0.97f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.3f, 0.5f, 0.8f, 0.6f),
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12
        });
        
        // 标题行 + 新项目按钮
        var headerRow = new HBoxContainer { Position = new(S(20), S(14)), Size = new(pw - S(40), S(36)) };
        var title = new Label { Text = $"{Loc.Tr("ui.project")}", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeColorOverride("font_color", new Color(0.15f, 0.4f, 0.7f));
        headerRow.AddChild(title);
        var newBtn = new Button { Text = Loc.Tr("proj.new_project"), CustomMinimumSize = new(S(80), S(30)), Size = new(S(80), S(30)) };
        newBtn.AddThemeFontSizeOverride("font_size", 12);
        newBtn.Pressed += () => { CloseAll(); _openPopup = new DevMenu(this); _openPopup.Show(); };
        headerRow.AddChild(newBtn);
        var postBtn = new Button { Text = Loc.Tr("proj.post_release"), CustomMinimumSize = new(S(70), S(30)), Size = new(S(70), S(30)), TooltipText = Loc.Tr("proj.post_release_desc") };
        postBtn.AddThemeFontSizeOverride("font_size", 12);
        postBtn.Pressed += () => { CloseAll(); _openPopup = new DevMenu(this); _openPopup.RenderPostRelease(); };
        headerRow.AddChild(postBtn);
        card.AddChild(headerRow);

        var scroll = new ScrollContainer { Position = new(S(20), S(60)), Size = new(pw - S(40), listH) };
        var list = new VBoxContainer();

        if (projects.Count == 0 && completed.Count == 0)
        {
            var emptyLabel = new Label { Text = Loc.Tr("proj.no_projects"), Size = new(pw - S(60), S(30)) };
            emptyLabel.AddThemeFontSizeOverride("font_size", 13);
            emptyLabel.AddThemeColorOverride("font_color", new Color(0.25f, 0.28f, 0.32f));
            emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
            list.AddChild(emptyLabel);
        }

        foreach (var proj in projects)
        {
            string phase = proj.Phase switch
            {
                DevPhase.Planning => Loc.Tr("proj.phase_planning"),
                DevPhase.Developing => Loc.TrF("proj.phase_dev_fmt", proj.DevProgress * 100),
                DevPhase.Polishing => Loc.TrF("proj.phase_polish_fmt", proj.BugCount),
                _ => proj.Phase.Name()
            };
            var row = new HBoxContainer { CustomMinimumSize = new(pw - S(70), rowH) };
            var rowLabel = new Label { Text = Loc.TrF("proj.dev_row_fmt", proj.Name, proj.Genre.Name(), proj.Theme.Name(), phase), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            rowLabel.AddThemeFontSizeOverride("font_size", 12);
            rowLabel.AddThemeColorOverride("font_color", new Color(0.15f, 0.45f, 0.15f));
            rowLabel.VerticalAlignment = VerticalAlignment.Center;
            row.AddChild(rowLabel);

            if (proj.Phase == DevPhase.Polishing)
            {
                var releaseBtn = new Button { Text = Loc.Tr("proj.btn_release"), CustomMinimumSize = new(S(70), S(28)), Size = new(S(70), S(28)) };
                releaseBtn.AddThemeFontSizeOverride("font_size", 11);
                releaseBtn.Pressed += () =>
                {
                    CloseAll();
                    var team = _teamMgr.Teams.Find(t => t.CurrentProject == proj);
                    if (team != null) { _devMgr.ReleaseGame(team); }
                    else { _devMgr.ReleaseGameDirect(proj); }
                };
                row.AddChild(releaseBtn);

                // 延期按钮
                if (proj.DelayCount < 3)
                {
                    var delayBtn = new Button { Text = Loc.Tr("proj.btn_delay"), CustomMinimumSize = new(S(70), S(28)), Size = new(S(70), S(28)) };
                    delayBtn.AddThemeFontSizeOverride("font_size", 11);
                    delayBtn.AddThemeColorOverride("font_color", new Color(0.9f, 0.5f, 0.1f));
                    delayBtn.Pressed += () =>
                    {
                        CloseAll();
                        string[] reasons = delayReasons;
                        var dialog = new AcceptDialog();
                        dialog.Title = Loc.Tr("proj.delay_title");
                        dialog.DialogText = Loc.TrF("proj.delay_desc", proj.Name) + "\n" + Loc.Tr("proj.delay_trust_warn");
                        dialog.AddCancelButton(Loc.Tr("dlg.cancel"));
                        dialog.Confirmed += () =>
                        {
                            var team = _teamMgr.Teams.Find(t => t.CurrentProject == proj);
                            if (team != null) _devMgr.DelayRelease(proj, 1, 0); // 默认延期1月+第一个理由
                        };
                        AddChild(dialog);
                        dialog.PopupCentered();
                    };
                    row.AddChild(delayBtn);
                }
            }
            if (proj.Phase == DevPhase.Planning)
            {
                // 立项中：可分配团队或删除
                var assignBtn = new Button { Text = Loc.Tr("dev.assign_dev"), CustomMinimumSize = new(S(70), S(28)), Size = new(S(70), S(28)) };
                assignBtn.AddThemeFontSizeOverride("font_size", 11);
                assignBtn.Pressed += () =>
                {
                    CloseAll();
                    var popup = new GameDevPopup(this);
                    _openDevPopup = popup;
                    popup.ShowAssignTeam(proj);
                };
                row.AddChild(assignBtn);

                var delBtn = new Button { Text = "🗑", CustomMinimumSize = new(S(30), S(28)), Size = new(S(30), S(28)) };
                delBtn.AddThemeFontSizeOverride("font_size", 11);
                delBtn.AddThemeColorOverride("font_color", new Color(0.8f, 0.2f, 0.2f));
                delBtn.Pressed += () =>
                {
                    _devMgr.Projects.Remove(proj);
                    CloseAll();
                    ShowProjectPanel();
                };
                row.AddChild(delBtn);
            }
            else
            {
                var detailBtn = new Button { Text = Loc.Tr("proj.btn_detail"), CustomMinimumSize = new(S(70), S(28)), Size = new(S(70), S(28)) };
                detailBtn.AddThemeFontSizeOverride("font_size", 11);
                detailBtn.Pressed += () =>
                {
                    var popup = new GameDevPopup(this);
                    _openDevPopup = popup;
                    popup.ShowProjectStatus(proj);
                };
                row.AddChild(detailBtn);
            }
            list.AddChild(row);
        }

        if (completed.Count > 0)
        {
            var sep = new Label { Text = $"── {Loc.Tr("dev.completed_list")} ──", Size = new(pw - S(70), S(20)) };
            sep.AddThemeFontSizeOverride("font_size", 10);
            sep.AddThemeColorOverride("font_color", new Color(0.3f, 0.6f, 0.3f));
            list.AddChild(sep);
        }
        foreach (var proj in completed)
        {
            var row = new Panel { CustomMinimumSize = new(pw - S(70), rowH) };
            row.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.90f, 0.93f, 0.88f, 0.5f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
            var rowLabel = new Label { Text = Loc.TrF("proj.done_row_fmt", proj.Name, proj.Genre.Name(), proj.Theme.Name(), proj.FinalScore, proj.Sales), Position = new(S(8), S(6)), Size = new(pw - S(86), rowH - S(12)) };
            rowLabel.AddThemeFontSizeOverride("font_size", 12);
            rowLabel.AddThemeColorOverride("font_color", new Color(0.25f, 0.28f, 0.32f));
            row.AddChild(rowLabel);
            list.AddChild(row);
        }

        scroll.AddChild(list);
        card.AddChild(scroll);

        var closeBtn = new Button { Text = Loc.Tr("proj.close_btn"), Position = new(S(40), ph - S(44)), Size = new(pw - S(80), S(36)) };
        closeBtn.AddThemeFontSizeOverride("font_size", 15);
        closeBtn.Pressed += () => CloseAll();
        card.AddChild(closeBtn);

        overlayPanel.AddChild(card);
        PushPanel(overlayPanel);
    }

    private void BackToMainMenu()
    {
        Paused = false;
        _pausePanel?.QueueFree();
        _saveLoadPanel?.QueueFree();
        GetTree().ChangeSceneToFile("res://scenes/menu.tscn");
    }

    /// <summary>DevMenu 调用设置面板（不隐藏 pause overlay）</summary>
    public void ShowSettingsPanelFromDevMenu()
    {
        CloseSaveLoad();
        if (_pauseOverlay != null) _pauseOverlay.Visible = false;
        _saveLoadPanel = new Panel();
        RenderSettingsContent(_saveLoadPanel);
        _uiLayer.AddChild(_saveLoadPanel);
    }

    // ══════════════════ 设置面板 ══════════════════
    private void ShowSettingsPanel()
    {
        CloseSaveLoad();
        if (_pauseOverlay != null) _pauseOverlay.Visible = false;
        _saveLoadPanel = new Panel();
        RenderSettingsContent(_saveLoadPanel);
        _uiLayer.AddChild(_saveLoadPanel);
    }

    private void RenderSettingsContent(Panel hostPanel)
    {
        var vp = GetViewport().GetVisibleRect().Size;
        float S(float v) => v * UIScale;
        float pw = Mathf.Min(500, vp.X - S(40));
        float ph = Mathf.Min(600, vp.Y - S(40));
        float px = (vp.X - pw) / 2, py = (vp.Y - ph) / 2;

        var card = new Panel { Position = new(px, py), Size = new(pw, ph), MouseFilter = Control.MouseFilterEnum.Stop };
        card.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = Colors.White,
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.3f, 0.5f, 0.8f, 0.6f),
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10
        });
        hostPanel.AddChild(card);

        // 标题
        var tl = new Label { Text = Loc.Tr("set.title"), Position = new(S(20), S(12)), Size = new(pw - S(80), S(28)) };
        tl.AddThemeFontSizeOverride("font_size", 20);
        tl.AddThemeColorOverride("font_color", new Color(0.15f, 0.4f, 0.7f));
        card.AddChild(tl);

        // 关闭按钮（固定在底部中间）
        float bw = 120, bh = 34;
        var closeBtn = new Button { Text = Loc.Tr("set.cancel"), Flat = true };
        closeBtn.AddThemeFontSizeOverride("font_size", 13);
        closeBtn.AddThemeColorOverride("font_color", Colors.Black);
        closeBtn.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.90f, 0.89f, 0.86f), CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.45f, 0.5f, 0.55f, 0.3f) });
        closeBtn.Size = new(bw, bh);
        closeBtn.Position = new((pw - bw) / 2, ph - bh - 12);
        closeBtn.Pressed += () => { if (_pauseOverlay != null) _pauseOverlay.Visible = true; CloseSaveLoad(); };
        card.AddChild(closeBtn);

        // 可滚动内容区域
        var scroll = new ScrollContainer { Position = new(S(20), S(46)), Size = new(pw - S(40), ph - S(46) - bh - S(24)) };
        card.AddChild(scroll);

        // 内容根布局
        var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        root.AddThemeConstantOverride("separation", 12);
        scroll.AddChild(root);
        root.AddChild(new Control { CustomMinimumSize = new(0, 6) });

        const float rowH = 30;
        float rowW = pw - S(80);
        OptionButton MkOpt(string[] names, int sel)
        {
            var opt = new OptionButton();
            foreach (var n in names) opt.AddItem(n);
            opt.Selected = sel;
            opt.AddThemeFontSizeOverride("font_size", 11);
            opt.AddThemeColorOverride("font_color", new Color(0.15f, 0.18f, 0.22f));
            opt.AddThemeColorOverride("font_hover_color", new Color(0.15f, 0.18f, 0.22f));
            return opt;
        }
        void AddRow(string label, Control widget)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("separation", 8);
            var lbl = new Label { Text = label, CustomMinimumSize = new(130, rowH), VerticalAlignment = VerticalAlignment.Center };
            lbl.AddThemeFontSizeOverride("font_size", 11);
            lbl.AddThemeColorOverride("font_color", new Color(0.15f, 0.18f, 0.22f));
            row.AddChild(lbl);
            widget.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            widget.CustomMinimumSize = new(0, rowH);
            row.AddChild(widget);
            root.AddChild(row);
        }
        {
            var opt = new OptionButton();
            opt.AddItem(Loc.Tr("set.window"));
            opt.AddItem(Loc.Tr("set.borderless"));
            opt.AddItem(Loc.Tr("set.fullscreen"));
            opt.Selected = GlobalSettings.DisplayMode;
            opt.AddThemeFontSizeOverride("font_size", 11);
            opt.AddThemeColorOverride("font_color", new Color(0.15f, 0.18f, 0.22f));
            opt.AddThemeColorOverride("font_hover_color", new Color(0.15f, 0.18f, 0.22f));
            opt.ItemSelected += (long i) => { GlobalSettings.DisplayMode = (int)i; GlobalSettings.ApplyAll(); };
            AddRow(Loc.Tr("set.display"), opt);
        }

        // 分辨率
        {
            var opt = MkOpt(GlobalSettings.ResNames, GlobalSettings.Resolution);
            opt.ItemSelected += (long i) => { GlobalSettings.Resolution = (int)i; GlobalSettings.ApplyAll(); };
            AddRow(Loc.Tr("set.resolution"), opt);
        }
        {
            string[] fpsLabels = new string[GlobalSettings.FpsOptions.Length];
            for (int fi = 0; fi < fpsLabels.Length; fi++)
                fpsLabels[fi] = GlobalSettings.FpsOptions[fi] == 0 ? Loc.Tr("set.fps_off") : $"{GlobalSettings.FpsOptions[fi]} FPS";
            var fpsOpt = MkOpt(fpsLabels, System.Array.IndexOf(GlobalSettings.FpsOptions, GlobalSettings.FpsCap));
            fpsOpt.ItemSelected += (long i) => { GlobalSettings.FpsCap = GlobalSettings.FpsOptions[i]; Engine.MaxFps = GlobalSettings.FpsCap; };
            AddRow(Loc.Tr("set.fps"), fpsOpt);
        }
        {
            var chk = new CheckBox { Text = Loc.Tr("set.vsync"), ButtonPressed = GlobalSettings.VSync };
            chk.AddThemeFontSizeOverride("font_size", 11);
            chk.AddThemeColorOverride("font_color", Colors.Black);
            chk.AddThemeColorOverride("font_hover_color", new Color(0.4f, 0.4f, 0.4f));
            chk.Toggled += (b) => { GlobalSettings.VSync = b; DisplayServer.WindowSetVsyncMode(b ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled); };
            AddRow("", chk);
        }
        {
            float[] scales = { 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f, 1.1f, 1.2f, 1.3f, 1.5f, 1.7f, 2.0f };
            string[] sn = { "0.5x", "0.6x", "0.7x", "0.8x", "0.9x", "1.0x", "1.1x", "1.2x", "1.3x", "1.5x", "1.7x", "2.0x" };
            int si = 5;
            for (int j = 0; j < scales.Length; j++) if (Mathf.Abs(scales[j] - UIScale) < 0.05f) { si = j; break; }
            var opt = MkOpt(sn, si);
            opt.ItemSelected += (long i) => { UIScale = scales[i]; GlobalSettings.UIScale = scales[i]; GetTree().ReloadCurrentScene(); };
            AddRow(Loc.Tr("set.ui_scale"), opt);
        }
        {
            var opt = MkOpt(Loc.LangLabels, Loc.CurrentLang);
            opt.ItemSelected += (long i) => { Loc.SetLang((int)i); GlobalSettings.Language = (int)i; GlobalSettings.Save(); GetTree().ReloadCurrentScene(); };
            AddRow(Loc.Tr("set.lang"), opt);
        }
        // 音效开关
        {
            var chk = new CheckBox { Text = Loc.Tr("set.sound_enable"), ButtonPressed = GlobalSettings.SoundEnabled };
            chk.AddThemeFontSizeOverride("font_size", 11);
            chk.AddThemeColorOverride("font_color", Colors.Black);
            chk.AddThemeColorOverride("font_hover_color", new Color(0.4f, 0.4f, 0.4f));
            chk.AddThemeColorOverride("font_pressed_color", Colors.Black);
            chk.AddThemeColorOverride("font_focus_color", Colors.Black);
            chk.Toggled += (on) => { GlobalSettings.SoundEnabled = on; GlobalSettings.Save(); };
            AddRow(Loc.Tr("set.sound"), chk);
        }
        // 音效音量
        {
            var slider = new HSlider { MinValue = 0, MaxValue = 100, Value = GlobalSettings.SoundVolume, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(0, rowH) };
            slider.ValueChanged += (v) => { GlobalSettings.SoundVolume = (float)v; _soundMgr?.RefreshVolume(); GlobalSettings.Save(); };
            AddRow(Loc.Tr("set.sound_volume"), slider);
        }
        // 音乐开关
        {
            var chk = new CheckBox { Text = Loc.Tr("set.music_enable"), ButtonPressed = GlobalSettings.MusicEnabled };
            chk.AddThemeFontSizeOverride("font_size", 11);
            chk.AddThemeColorOverride("font_color", Colors.Black);
            chk.AddThemeColorOverride("font_hover_color", new Color(0.4f, 0.4f, 0.4f));
            chk.AddThemeColorOverride("font_pressed_color", Colors.Black);
            chk.AddThemeColorOverride("font_focus_color", Colors.Black);
            chk.Toggled += (on) => { GlobalSettings.MusicEnabled = on; _soundMgr?.RefreshVolume(); GlobalSettings.Save(); };
            AddRow(Loc.Tr("set.music"), chk);
        }
        // 音乐音量
        {
            var slider = new HSlider { MinValue = 0, MaxValue = 100, Value = GlobalSettings.MusicVolume, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(0, rowH) };
            slider.ValueChanged += (v) => { GlobalSettings.MusicVolume = (float)v; _soundMgr?.RefreshVolume(); GlobalSettings.Save(); };
            AddRow(Loc.Tr("set.music_volume"), slider);
        }
        // 名称缩写
        {
            var chk = new CheckBox { Text = Loc.Tr("set.name_abbr"), ButtonPressed = GlobalSettings.ArabicNameAbbr };
            chk.AddThemeFontSizeOverride("font_size", 11);
            chk.AddThemeColorOverride("font_color", Colors.Black);
            chk.AddThemeColorOverride("font_hover_color", new Color(0.4f, 0.4f, 0.4f));
            if (Loc.CurrentLang != 10) chk.Visible = false;
            chk.Toggled += (b) => { GlobalSettings.ArabicNameAbbr = b; GlobalSettings.Save(); };
            AddRow("", chk);
        }
        // ── 键位映射 ──
        {
            var sep = new HSeparator { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(0, 6) };
            root.AddChild(sep);
            var kbTitle = new Label { Text = "⌨️ 键位映射", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            kbTitle.AddThemeFontSizeOverride("font_size", 14);
            kbTitle.AddThemeColorOverride("font_color", new Color(0.15f, 0.4f, 0.7f));
            root.AddChild(kbTitle);
            string[] keys = {
                "Space — 暂停/继续",
                "1~8 — 速度切换",
                "W/A/S/D — 平移3D视角",
                "鼠标右键拖拽 — 旋转视角",
                "滚轮 — 缩放视角",
                "C — 卡牌商店",
                "F1 — 游戏百科",
                "F2 — 成就馆",
                "Esc — 关闭面板/暂停菜单",
            };
            foreach (var k in keys)
            {
                var lbl = new Label { Text = k, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                lbl.AddThemeFontSizeOverride("font_size", 11);
                lbl.AddThemeColorOverride("font_color", new Color(0.2f, 0.25f, 0.3f));
                root.AddChild(lbl);
            }
        }
        root.AddChild(new Control { CustomMinimumSize = new(0, 8) });
    }

    private void QuitGame()
    {
        GetTree().Quit();
    }

    private void RebuildHUDTabs()
    {
        string[] tabs = { Loc.Tr("ui.project"), Loc.Tr("ui.tech"), Loc.Tr("ui.team"), Loc.Tr("ui.engine"), Loc.Tr("ui.room"), Loc.Tr("ui.finance"), Loc.Tr("ui.company"), Loc.Tr("ui.server") };
        for (int i = 0; i < _tabButtons.Count && i < tabs.Length; i++)
            _tabButtons[i].Text = tabs[i];
    }

    // ── 存档面板 ──

    private void ShowSavePanel()
    {
        CloseSaveLoad();
        _saveLoadPanel = BuildSlotPanel(Loc.Tr("ui.save_title"), Loc.Tr("ui.save_to"), true);
    }

    private void ShowLoadPanel()
    {
        CloseSaveLoad();
        _saveLoadPanel = BuildSlotPanel(Loc.Tr("pause.load_title"), Loc.Tr("pause.load_from"), false);
    }

    private Panel BuildSlotPanel(string title, string actionLabel, bool isSave)
    {
        var panel = new Panel();
        var vp = GetViewport().GetVisibleRect().Size;
        var S = (Func<float, float>)(v => v * UIScale);

        float pw = vp.X * 0.55f, ph = vp.Y * 0.55f;
        float px = (vp.X - pw) / 2, py = (vp.Y - ph) / 2;
        var card = new DragPanel { Position = new(px, py), Size = new(pw, ph) };
        card.SetScale(UIScale);
        card.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = Colors.White,
            BorderWidthLeft = 2, BorderWidthTop = 2,
            BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.3f, 0.5f, 0.8f, 0.6f),
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10
        });
        panel.AddChild(card);

        var tl = new Label { Text = title, Position = new(S(20), S(14)), Size = new(pw - S(80), S(30)) };
        tl.AddThemeFontSizeOverride("font_size", 20);
        tl.AddThemeColorOverride("font_color", new Color(0.15f, 0.4f, 0.7f));
        card.AddChild(tl);

        var closeBtn = new Button { Text = "✕", Position = new(pw - S(50), S(10)), Size = new(S(36), S(30)), Flat = true };
        closeBtn.AddThemeFontSizeOverride("font_size", 16);
        closeBtn.Pressed += CloseSaveLoad;
        card.AddChild(closeBtn);

        // ── 自动存档位（最上方） ──
        float rowH = S(52), startY = S(58), colW = pw - S(40);
        {
            float ry = startY;
            var autoBg = new ColorRect { Position = new(S(20), ry), Size = new(colW, rowH - S(6)), Color = new Color(0.85f, 0.90f, 0.95f, 0.55f) };
            card.AddChild(autoBg);
            var autoLbl = new Label { Text = Loc.Tr("save.autosave"), Position = new(S(30), ry + S(10)), Size = new(S(80), S(24)) };
            autoLbl.AddThemeFontSizeOverride("font_size", 11);
            autoLbl.AddThemeColorOverride("font_color", new Color(0.3f, 0.7f, 1.0f));
            autoLbl.ClipText = true;
            card.AddChild(autoLbl);
            string autoMeta = GetSlotMeta(0);
            var autoMetaLbl = new Label { Text = autoMeta, Position = new(S(130), ry + S(10)), Size = new(S(260), S(24)) };
            autoMetaLbl.AddThemeFontSizeOverride("font_size", 11);
            autoMetaLbl.AddThemeColorOverride("font_color", string.IsNullOrEmpty(autoMeta) ? new Color(0.6f, 0.58f, 0.55f) : new Color(0.25f, 0.28f, 0.32f));
            card.AddChild(autoMetaLbl);
            var autoBtn = new Button { Text = isSave ? Loc.Tr("ui.save_btn") : Loc.Tr("ui.load_btn"), Position = new(pw - S(110), ry + S(6)), Size = new(S(90), rowH - S(12)) };
            autoBtn.AddThemeFontSizeOverride("font_size", 12);
            autoBtn.Pressed += () =>
            {
                if (isSave) SaveGame(GlobalSettings.GetAutoSavePath());
                else { FounderOverlayCleanup(); ClosePauseMenu(); CloseSaveLoad(); LoadGame(GlobalSettings.GetAutoSavePath()); ClearHUD(); BuildHUD(); Paused = false; }
                if (isSave) CloseSaveLoad();
            };
            card.AddChild(autoBtn);
            startY += rowH;
        }

        // 5 个手动存档位
        for (int slot = 1; slot <= 5; slot++)
        {
            float ry = startY + (slot - 1) * rowH;
            // 背景条
            var bg = new ColorRect { Position = new(S(20), ry), Size = new(colW, rowH - S(6)), Color = new Color(0.90f, 0.93f, 0.91f, 0.55f) };
            card.AddChild(bg);

            // 存档位标签（支持自定义名称）
            string slotLabel = GlobalSettings.GetSaveSlotName(slot);
            var lbl = new Label { Text = slotLabel, Position = new(S(30), ry + S(10)), Size = new(S(75), S(24)) };
            lbl.AddThemeFontSizeOverride("font_size", 11);
            lbl.AddThemeColorOverride("font_color", new Color(0.45f, 0.6f, 0.8f));
            lbl.ClipText = true;
            card.AddChild(lbl);

            // 重命名按钮（放在标签右侧）
            var renameBtn = new Button { Text = "✎", Position = new(S(112), ry + S(6)), Size = new(S(22), S(24)), Flat = true };
            renameBtn.AddThemeFontSizeOverride("font_size", 10);
            renameBtn.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            int sc = slot; Label sl = lbl;
            renameBtn.Pressed += () =>
            {
                ShowSlotRenamePopup(sc, sl, card);
            };
            card.AddChild(renameBtn);

            // 元信息（放在重命名按钮右侧）
            string meta = GetSlotMeta(slot);
            var metaLbl = new Label { Text = meta, Position = new(S(140), ry + S(10)), Size = new(S(240), S(24)) };
            metaLbl.AddThemeFontSizeOverride("font_size", 11);
            metaLbl.AddThemeColorOverride("font_color", string.IsNullOrEmpty(meta) ? new Color(0.6f, 0.58f, 0.55f) : new Color(0.25f, 0.28f, 0.32f));
            metaLbl.ClipText = true;
            card.AddChild(metaLbl);

            // 操作按钮
            var actBtn = new Button
            {
                Text = isSave ? Loc.Tr("ui.save_btn") : Loc.Tr("ui.load_btn"),
                Position = new(pw - S(110), ry + S(6)), Size = new(S(90), rowH - S(12))
            };
            actBtn.AddThemeFontSizeOverride("font_size", 12);
            int captured = slot;
            actBtn.Pressed += () =>
            {
                int oldSlot = GlobalSettings.SaveSlot;
                GlobalSettings.SaveSlot = captured;
                if (isSave) SaveGame();
                else { FounderOverlayCleanup(); ClosePauseMenu(); CloseSaveLoad(); LoadGame(); ClearHUD(); BuildHUD(); Paused = false; }
                GlobalSettings.SaveSlot = oldSlot;
                if (isSave) CloseSaveLoad();
            };
            card.AddChild(actBtn);
        }

        _uiLayer.AddChild(panel);
        return panel;
    }

    /// <summary>载入存档前清理创始人画面</summary>
    private void FounderOverlayCleanup()
    {
        if (_founderOverlay != null) { _founderOverlay.QueueFree(); _founderOverlay = null; }
        _modalLock = 0;
    }

    /// <summary>读档前清理旧 HUD，重置弹窗锁，避免控件叠加</summary>
    private void ClearHUD()
    {
        _modalLock = 0;
        _moneyLabel = null; _inspirationLabel = null; _fanLabel = null;
        _debtWarnLabel = null; _dateLabel = null; _empLabel = null;
        _trustLabel = null; _bestScoreLabel = null; _pauseBtn = null;
        _speedOpt = null; _bottomNav = null; _revHoverLabel = null;
        _hoverTooltipLabel = null; _hoverTooltipPanel = null;
        _tooltipPanel = null; _predictionLabel = null;
        _simplePausePanel = null;
        // 清除 _uiLayer 所有非弹窗控件（读档前弹窗已全部关闭）
        for (int i = _uiLayer.GetChildCount() - 1; i >= 0; i--)
        {
            var child = _uiLayer.GetChild(i);
            if (child is Control) child.QueueFree();
        }
    }

    /// <summary>读取存档位元信息（不加载） slot=0 表示自动存档</summary>
    private string GetSlotMeta(int slot)
    {
        string path = slot == 0 ? GlobalSettings.GetAutoSavePath() : GlobalSettings.GetSlotPath(slot);
        if (!FileAccess.FileExists(path)) return "";
        try
        {
            string json;
            using (var f = FileAccess.Open(path, FileAccess.ModeFlags.Read))
            { if (f == null) return ""; json = f.GetAsText(); }
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            if (r.TryGetProperty("meta", out var meta))
            {
                int month = meta.GetProperty("Month").GetInt32();
                float money = meta.GetProperty("Money").GetSingle();
                float debt = meta.GetProperty("TechDebt").GetSingle();
                int y = month / 12, m = month % 12 + 1;
                return Loc.TrF("save.entry_fmt", y, m, money, debt);
            }
            int mo = r.GetProperty("Month").GetInt32();
            float mn = r.GetProperty("Money").GetSingle();
            float db = r.GetProperty("TechDebt").GetSingle();
            int yy = mo / 12, mm = mo % 12 + 1;
            return Loc.TrF("save.entry_fmt", yy, mm, mn, db);
        }
        catch { return Loc.Tr("save.corrupt"); }
    }

    /// <summary>弹窗重命名存档位</summary>
    private void ShowSlotRenamePopup(int slot, Label targetLabel, Panel parent)
    {
        var vp = GetViewport().GetVisibleRect().Size;
        float pw = vp.X * 0.3f, ph = vp.Y * 0.15f;
        var dlg = new DragPanel { Position = new((vp.X - pw) / 2, (vp.Y - ph) / 2), Size = new(pw, ph) };
        dlg.SetScale(UIScale);
        dlg.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.98f, 0.98f, 0.98f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.3f, 0.5f, 0.8f, 0.6f),
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8
        });

        float S(float v) => v * UIScale;
        dlg.AddChild(new Label { Text = $"重命名存档位 {slot}", Position = new(S(14), S(8)), Size = new(pw - S(28), S(22)) });

        var edit = new LineEdit { Text = GlobalSettings.SaveSlotNames[slot] ?? "", Position = new(S(14), S(34)), Size = new(pw - S(28), S(28)), PlaceholderText = $"存档位 {slot}", Editable = true };
        dlg.AddChild(edit);

        var okBtn = new Button { Text = "确定", Position = new(pw - S(110), ph - S(34)), Size = new(S(50), S(26)), Flat = true };
        okBtn.Pressed += () =>
        {
            GlobalSettings.SetSaveSlotName(slot, edit.Text);
            targetLabel.Text = GlobalSettings.GetSaveSlotName(slot);
            GlobalSettings.Save();
            dlg.QueueFree();
        };
        dlg.AddChild(okBtn);

        var cancelBtn = new Button { Text = Loc.Tr("ui.cancel"), Position = new(pw - S(58), ph - S(34)), Size = new(S(50), S(26)), Flat = true };
        cancelBtn.Pressed += () => dlg.QueueFree();
        dlg.AddChild(cancelBtn);

        _uiLayer.AddChild(dlg);
    }

    private void SaveGame() => SaveGame(SavePath);
    private void SaveGame(string path)
    {
        ModAPI.FireHooks(ModAPI.GameHook.OnSaveGame);
        string json = BuildSaveJson();
        GD.Print($"[SAVE] Writing {json.Length} bytes to {path}... first 100 chars: {json.Substring(0, Math.Min(100, json.Length))}");
        using var f = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (f != null) { f.StoreString(json); GD.Print("[SAVE] OK"); }
        else GD.PrintErr($"[SAVE] FAILED - cannot open {path}");
    }

    private string BuildSaveJson()
    {
        // 强制不变文化确保浮点数序列化为小数点，不随系统语言变逗号
        var oldCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        var sb = new System.Text.StringBuilder();
        sb.Append("{");

        // schema version for backward compat
        sb.Append("\"__version\":2,");

        // meta
        sb.Append("\"meta\":{");
        float curDebt = GetNode<TechDebtManager>("TechDebtManager")?.ComputeTotalDebt() ?? 0;
        sb.Append($"\"Month\":{GameMonth},\"Money\":{_res.Money},\"Inspiration\":{_res.Inspiration},\"TechDebt\":{curDebt}");
        sb.Append("},");

        // tutorial
        sb.Append("\"tutorial\":{");
        sb.Append($"\"Completed\":{JsonBool(_tutorialMgr.TutorialCompleted)},");
        sb.Append($"\"CurrentStep\":{_tutorialMgr.CurrentStepIndex}");
        sb.Append("},");

        // debt
        sb.Append("\"debt\":{");
        sb.Append($"\"HasCrashed\":{JsonBool(_debtMgr.HasCrashed)},");
        sb.Append($"\"CrashRecoveryMonths\":{_debtMgr.CrashRecoveryMonths},");
        sb.Append($"\"CrunchMode\":{JsonBool(_debtMgr.CrunchMode)}");
        sb.Append("},");

        // fans
        sb.Append("\"fans\":{");
        sb.Append($"\"Casual\":{_fanMgr.CasualFans},\"Diehard\":{_fanMgr.DiehardFans},\"Cooldown\":{_fanMgr.FanEventCooldown}");
        sb.Append("},");

        // room
        sb.Append("\"room\":{");
        sb.Append($"\"Tier\":{(int)_roomMgr.CurrentTier},\"BonusRooms\":[");
        sb.Append(string.Join(",", _roomMgr.PurchasedBonusRooms.Select(b => $"{(int)b}")));
        sb.Append("]},");

        // server
        sb.Append("\"server\":{");
        sb.Append($"\"Tier\":{(int)_serverMgr.CurrentTier}");
        sb.Append("},");

        // employees
        sb.Append("\"employees\":[");
        for (int i = 0; i < _empMgr.Employees.Count; i++)
        { if (i > 0) sb.Append(","); sb.Append(SerializeEmp(_empMgr.Employees[i])); }
        sb.Append("],");

        // teams
        sb.Append("\"teams\":[");
        for (int i = 0; i < _teamMgr.Teams.Count; i++)
        { if (i > 0) sb.Append(","); sb.Append(SerializeTeam(_teamMgr.Teams[i])); }
        sb.Append("],");

        // projects
        sb.Append("\"projects\":[");
        for (int i = 0; i < _devMgr.Projects.Count; i++)
        { if (i > 0) sb.Append(","); sb.Append(SerializeProj(_devMgr.Projects[i])); }
        sb.Append("],");

        // completed
        sb.Append("\"completed\":[");
        for (int i = 0; i < _devMgr.CompletedProjects.Count; i++)
        { if (i > 0) sb.Append(","); sb.Append(SerializeProj(_devMgr.CompletedProjects[i])); }
        sb.Append("],");

        // techs
        sb.Append(SerializeTechs());
        sb.Append(",");

        // engines
        sb.Append("\"engines\":[");
        for (int i = 0; i < Engines.Count; i++)
        { if (i > 0) sb.Append(","); sb.Append(SerializeEngine(Engines[i])); }
        sb.Append("],");

        // competitors
        sb.Append("\"competitors\":[");
        for (int i = 0; i < _competitor.Studios.Count; i++)
        { if (i > 0) sb.Append(","); sb.Append(SerializeStudio(_competitor.Studios[i])); }
        sb.Append("],");

        // triggered events (反重复触发保护)
        var storyEvt = GetNode<StoryEvents>("StoryEvents");
        var triggered = storyEvt.GetTriggeredEvents();
        sb.Append("\"triggeredEvents\":[");
        for (int i = 0; i < triggered.Count; i++)
        { if (i > 0) sb.Append(","); sb.Append(JsonStr(triggered[i])); }
        sb.Append("],");

        // founder
        sb.Append("\"founder\":{");
        sb.Append($"\"name\":{JsonStr(Founder.Name)},");
        sb.Append($"\"company\":{JsonStr(Founder.CompanyName)},");
        sb.Append($"\"prog\":{Founder.Programming},\"art\":{Founder.Art},\"audio\":{Founder.Audio},");
        sb.Append($"\"net\":{Founder.Network},\"ai\":{Founder.AI},\"mgmt\":{Founder.Management},");
        sb.Append($"\"trait\":{JsonStr(Founder.Trait.ToString())},\"unused\":{Founder.UnusedPoints},\"created\":{JsonBool(Founder.HasCreated)}");
        sb.Append("},");

        // loan
        sb.Append("\"loan\":{");
        sb.Append($"\"principal\":{Loan.Principal},\"rate\":{Loan.InterestRate},\"months\":{Loan.RemainingMonths},");
        sb.Append($"\"payment\":{Loan.MonthlyPayment},\"overdue\":{Loan.OverdueMonths}");
        sb.Append("},");

        // corporate actions
        sb.Append("\"corpActions\":{");
        sb.Append("\"logs\":[");
        for (int i = 0; i < CorpActions.ActionLogs.Count; i++)
        {
            if (i > 0) sb.Append(",");
            var l = CorpActions.ActionLogs[i];
            sb.Append($"{{\"month\":{l.Month},\"type\":{JsonStr(l.Type.ToString())},\"target\":{JsonStr(l.TargetName)},\"success\":{JsonBool(l.Success)},\"desc\":{JsonStr(l.Description)}}}");
        }
        sb.Append("],\"cooldowns\":{");
        bool firstCd = true;
        foreach (var kv in CorpActions.Cooldowns)
        {
            if (!firstCd) sb.Append(","); firstCd = false;
            sb.Append($"{JsonStr(kv.Key.ToString())}:{kv.Value}");
        }
        sb.Append("}}");

        // Mod 存档数据
        string modData = ModAPI.BuildSaveData();
        if (!string.IsNullOrEmpty(modData))
            sb.Append($",\"modData\":{{{modData}}}");

        sb.Append("}");

        System.Threading.Thread.CurrentThread.CurrentCulture = oldCulture;
        return sb.ToString();
    }

    private static string JsonBool(bool v) => v ? "true" : "false";
    private static int TryGetPropInt(System.Text.Json.JsonElement je, string prop, int fallback = 0)
    {
        if (je.TryGetProperty(prop, out var val)) return val.GetInt32();
        return fallback;
    }
    private static float TryGetPropFloat(System.Text.Json.JsonElement je, string prop, float fallback = 0)
    {
        if (je.TryGetProperty(prop, out var val)) return val.GetSingle();
        return fallback;
    }
    private static string TryGetPropStr(System.Text.Json.JsonElement je, string prop, string fallback = "")
    {
        if (je.TryGetProperty(prop, out var val)) return val.GetString() ?? fallback;
        return fallback;
    }
    private static string JsonStr(string s) => $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n")}\"";
    private static string JsonNum(float v) => v.ToString("F2");

    private void LoadGame() => LoadGame(SavePath);
    private void LoadGame(string path)
    {
        GD.Print($"[LOAD] Reading from {path}");
        if (!FileAccess.FileExists(path)) { GD.Print("[LOAD] File not found"); return; }

        string json;
        using (var f = FileAccess.Open(path, FileAccess.ModeFlags.Read))
        {
            if (f == null) { GD.Print("[LOAD] Cannot open file"); return; }
            json = f.GetAsText();
        }
        if (string.IsNullOrEmpty(json)) { GD.Print("[LOAD] Empty file"); return; }
        GD.Print($"[LOAD] Read {json.Length} bytes");

        ModAPI.FireHooks(ModAPI.GameHook.OnLoadGame);
        using var doc = JsonDocument.Parse(json);
        var d = doc.RootElement;
        GD.Print("[LOAD] JSON parsed OK");

        try
        {
            // schema version check for future compat
            int saveVersion = d.TryGetProperty("__version", out var verEl) ? verEl.GetInt32() : 1;

            // meta
            if (d.TryGetProperty("meta", out var meta))
            {
                GameMonth = TryGetPropInt(meta, "Month", 0);
                _res.Money = TryGetPropFloat(meta, "Money", 500000);
                _res.Inspiration = TryGetPropFloat(meta, "Inspiration", 30);
                float techDebtVal = TryGetPropFloat(meta, "TechDebt", 0);
                if (_res != null) _res.TechDebt = techDebtVal;
            }
            // tutorial
            if (d.TryGetProperty("tutorial", out var tutorial))
            {
                bool tutCompleted = tutorial.TryGetProperty("Completed", out var tc) && tc.GetBoolean();
                int tutStep = tutorial.TryGetProperty("CurrentStep", out var ts) ? ts.GetInt32() : -1;
                _tutorialMgr.LoadProgress(tutStep, tutCompleted);
            }
            // debt
            if (d.TryGetProperty("debt", out var debt))
            {
                if (debt.TryGetProperty("HasCrashed", out var hc)) _debtMgr.HasCrashed = hc.GetBoolean();
                if (debt.TryGetProperty("CrashRecoveryMonths", out var crm)) _debtMgr.CrashRecoveryMonths = crm.GetInt32();
                if (debt.TryGetProperty("CrunchMode", out var cm)) _debtMgr.CrunchMode = cm.GetBoolean();
            }
            // fans
            if (d.TryGetProperty("fans", out var fans))
            {
                _fanMgr.CasualFans = fans.GetProperty("Casual").GetInt32();
                _fanMgr.DiehardFans = fans.GetProperty("Diehard").GetInt32();
                _fanMgr.FanEventCooldown = fans.GetProperty("Cooldown").GetSingle();
            }
            // room
            if (d.TryGetProperty("room", out var room))
            {
                _roomMgr.CurrentTier = (HouseTier)room.GetProperty("Tier").GetInt32();
                _roomMgr.PurchasedBonusRooms.Clear();
                foreach (var b in room.GetProperty("BonusRooms").EnumerateArray())
                    _roomMgr.PurchasedBonusRooms.Add((BonusRoom)b.GetInt32());
            }
            // server
            if (d.TryGetProperty("server", out var server))
            {
                _serverMgr.CurrentTier = (ServerTier)server.GetProperty("Tier").GetInt32();
            }
            // employees
            _empMgr.Employees.Clear();
            var loadedEmpIds = new HashSet<int>();
            if (d.TryGetProperty("employees", out var emps))
                foreach (var e in emps.EnumerateArray())
                {
                    var emp = DeserializeEmp(e);
                    if (loadedEmpIds.Add(emp.Id))
                        _empMgr.Employees.Add(emp);
                }

            // projects & completed
            _devMgr.Projects.Clear();
            if (d.TryGetProperty("projects", out var projs))
                foreach (var p in projs.EnumerateArray())
                    _devMgr.Projects.Add(DeserializeProj(p));
            _devMgr.CompletedProjects.Clear();
            if (d.TryGetProperty("completed", out var comps))
                foreach (var p in comps.EnumerateArray())
                    _devMgr.CompletedProjects.Add(DeserializeProj(p));

            // teams (after employees)
            _teamMgr.Teams.Clear();
            if (d.TryGetProperty("teams", out var teams))
                foreach (var t in teams.EnumerateArray())
                    _teamMgr.Teams.Add(DeserializeTeam(t));

            // techs
            if (d.TryGetProperty("techs", out var techs))
                DeserializeTechs(techs);

            // engines
            Engines.Clear();
            if (d.TryGetProperty("engines", out var engArr))
                foreach (var eng in engArr.EnumerateArray())
                    Engines.Add(DeserializeEngine(eng));
            if (Engines.Count == 0)
            {
                // 兼容旧存档：从 techs 中读取引擎商业数据回填到默认引擎
                var starter = new GameEngine
                {
                    Name = Loc.Tr("devmenu.starter_engine"),
                    AppliedTechs = _techMgr.ResearchedTech.Where(kv => kv.Value).Select(kv => kv.Key).ToList()
                };
                starter.UpdateCapabilities();
                starter.DerivePerks();
                if (techs.TryGetProperty("engineModel", out var em)) starter.BizModel = (EngineBizModel)em.GetInt32();
                if (techs.TryGetProperty("royalty", out var ry)) starter.RoyaltyRate = ry.GetSingle();
                if (techs.TryGetProperty("buyout", out var bo)) starter.BuyoutPrice = bo.GetSingle();
                if (techs.TryGetProperty("sub", out var sb)) starter.SubscriptionPrice = sb.GetSingle();
                if (techs.TryGetProperty("licCount", out var lc)) starter.LicenseCount = lc.GetInt32();
                if (techs.TryGetProperty("mktShare", out var ms)) starter.MarketShare = ms.GetSingle();
                if (techs.TryGetProperty("reputation", out var rp)) starter.Reputation = rp.GetSingle();
                Engines.Add(starter);
            }

            // competitors
            _competitor.Studios.Clear();
            if (d.TryGetProperty("competitors", out var comp))
                foreach (var s in comp.EnumerateArray())
                    _competitor.Studios.Add(DeserializeStudio(s));

            // triggered events
            if (d.TryGetProperty("triggeredEvents", out var trigArr))
            {
                var events = new List<string>();
                foreach (var t in trigArr.EnumerateArray()) events.Add(t.GetString());
                GetNode<StoryEvents>("StoryEvents").SetTriggeredEvents(events);
            }

            // founder
            if (d.TryGetProperty("founder", out var founderEl))
            {
                Founder.Name = TryGetPropStr(founderEl, "name", "创始人");
                Founder.CompanyName = TryGetPropStr(founderEl, "company", "独立游戏工作室");
                Founder.Programming = TryGetPropInt(founderEl, "prog", 3);
                Founder.Art = TryGetPropInt(founderEl, "art", 2);
                Founder.Audio = TryGetPropInt(founderEl, "audio", 1);
                Founder.Network = TryGetPropInt(founderEl, "net", 1);
                Founder.AI = TryGetPropInt(founderEl, "ai", 1);
                Founder.Management = TryGetPropInt(founderEl, "mgmt", 2);
                Founder.Trait = Enum.TryParse<FounderTrait>(TryGetPropStr(founderEl, "trait", "Balanced"), out var ft) ? ft : FounderTrait.Balanced;
                Founder.UnusedPoints = TryGetPropInt(founderEl, "unused", 0);
                Founder.HasCreated = founderEl.TryGetProperty("created", out var cr) && cr.GetBoolean();
            }

            // loan
            if (d.TryGetProperty("loan", out var loanEl))
            {
                Loan.Principal = TryGetPropFloat(loanEl, "principal", 0);
                Loan.InterestRate = TryGetPropFloat(loanEl, "rate", 0.01f);
                Loan.RemainingMonths = TryGetPropInt(loanEl, "months", 0);
                Loan.MonthlyPayment = TryGetPropFloat(loanEl, "payment", 0);
                Loan.OverdueMonths = TryGetPropInt(loanEl, "overdue", 0);
            }

            // corporate actions
            if (d.TryGetProperty("corpActions", out var corpEl))
            {
                if (corpEl.TryGetProperty("logs", out var logsArr))
                {
                    CorpActions.ActionLogs.Clear();
                    foreach (var l in logsArr.EnumerateArray())
                        CorpActions.ActionLogs.Add(new CorporateActionLog
                        {
                            Month = TryGetPropInt(l, "month"),
                            Type = Enum.TryParse<ActionType>(TryGetPropStr(l, "type", "Poach"), out var at) ? at : ActionType.Poach,
                            TargetName = TryGetPropStr(l, "target", ""),
                            Success = l.TryGetProperty("success", out var sc) && sc.GetBoolean(),
                            Description = TryGetPropStr(l, "desc", "")
                        });
                }
                if (corpEl.TryGetProperty("cooldowns", out var cdObj))
                {
                    CorpActions.Cooldowns.Clear();
                    foreach (var prop in cdObj.EnumerateObject())
                        if (Enum.TryParse<ActionType>(prop.Name, out var at))
                            CorpActions.Cooldowns[at] = prop.Value.GetInt32();
                }
            }
            CorpActions.Init(this, _competitor);

            // Rebuild house & employees
            GD.Print($"[LOAD] Replaying house tier={_roomMgr.CurrentTier}");
            _roomMgr.ReplayHouse(_roomMgr.CurrentTier);

            // Mod 存档数据加载
            if (d.TryGetProperty("modData", out var modDataEl))
            {
                var modDict = new Dictionary<string, string>();
                foreach (var kv in modDataEl.EnumerateObject())
                    modDict[kv.Name] = kv.Value.GetRawText();
                ModAPI.LoadSaveData(modDict);
            }

            GD.Print($"[LOAD] Complete - Month={GameMonth} Money={_res.Money} Debt={_res.TechDebt}");
        }
        catch (Exception ex) { GD.PrintErr($"LoadGame failed: {ex.Message}"); }
    }

    // ══════════════════ JSON 读写 ══════════════════
    private static void WriteJson(string path, object data)
    {
        using var f = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (f != null) f.StoreString(JsonSerializer.Serialize(data));
    }

    private static JsonElement? ReadJson(string path)
    {
        if (!FileAccess.FileExists(path)) return null;
        using var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (f == null) return null;
        try { return JsonDocument.Parse(f.GetAsText()).RootElement; }
        catch { return null; }
    }

    // ══════════════════ 员工 ══════════════════
    private static string SerializeEmp(Employee e)
    {
        var s = new System.Text.StringBuilder();
        s.Append("{");
        s.Append($"\"name\":{JsonStr(e.Name)},\"id\":{e.Id},\"salary\":{e.Salary}");
        s.Append($",\"months\":{e.MonthsEmployed},\"projDone\":{e.ProjectsCompleted},\"highScore\":{e.HighScoreProjects}");
        s.Append($",\"hw\":{JsonBool(e.IsHardwareEngineer)},\"architect\":{JsonBool(e.IsChiefArchitect)},\"captain\":{JsonBool(e.IsCaptain)}");
        s.Append($",\"teamName\":{JsonStr(e.TeamName ?? "")},\"fatigue\":{e.Fatigue},\"satisfaction\":{e.Satisfaction}");
        s.Append($",\"trained\":{JsonBool(e.TrainingThisYear)},\"mentored\":{JsonBool(e.HadMentorEvent)},\"lttm\":{e.LastTrainAbsoluteMonth}");
        s.Append($",\"trait\":{JsonStr(e.Trait.ToString())},\"trainingLeave\":{e.TrainingLeaveMonths}");
        s.Append(",\"skills\":[");
        int ki = 0;
        foreach (var kv in e.Skills)
        {
            if (ki++ > 0) s.Append(",");
            s.Append("{");
            s.Append($"\"type\":{(int)kv.Key},\"lv\":{kv.Value.Level},\"exp\":{kv.Value.Exp},\"eff\":{kv.Value.Efficiency}");
            s.Append("}");
        }
        s.Append("],\"friends\":["); s.Append(string.Join(",", e.Friends.Select(f => f.ToString()))); s.Append("]");
        s.Append(",\"rivals\":["); s.Append(string.Join(",", e.Rivals.Select(r => r.ToString()))); s.Append("]");
        s.Append("}");
        return s.ToString();
    }

    private static Employee DeserializeEmp(JsonElement e)
    {
        var emp = new Employee
        {
            Name = e.GetProperty("name").GetString(), Id = e.GetProperty("id").GetInt32(),
            Salary = e.GetProperty("salary").GetSingle(), MonthsEmployed = e.GetProperty("months").GetInt32(),
            ProjectsCompleted = e.GetProperty("projDone").GetInt32(), HighScoreProjects = e.GetProperty("highScore").GetInt32(),
            IsHardwareEngineer = e.GetProperty("hw").GetBoolean(), IsChiefArchitect = e.GetProperty("architect").GetBoolean(),
            IsCaptain = e.GetProperty("captain").GetBoolean(), TeamName = e.GetProperty("teamName").GetString(),
            Fatigue = e.GetProperty("fatigue").GetSingle(), Satisfaction = e.GetProperty("satisfaction").GetSingle(),
            TrainingThisYear = e.GetProperty("trained").GetBoolean(), HadMentorEvent = e.GetProperty("mentored").GetBoolean(),
            LastTrainAbsoluteMonth = TryGetPropInt(e, "lttm"),
            TrainingLeaveMonths = e.GetProperty("trainingLeave").GetInt32()
        };
        string traitName = e.GetProperty("trait").GetString();
        if (!string.IsNullOrEmpty(traitName)) emp.Trait = Enum.Parse<EmployeeTrait>(traitName);
        foreach (var s in e.GetProperty("skills").EnumerateArray())
            emp.Skills[(SkillType)s.GetProperty("type").GetInt32()] = new SkillLevelInfo
            {
                Level = s.GetProperty("lv").GetInt32(), Exp = s.GetProperty("exp").GetInt32(),
                Efficiency = s.GetProperty("eff").GetSingle()
            };
        if (e.TryGetProperty("friends", out var fr)) foreach (var f in fr.EnumerateArray()) emp.Friends.Add(f.GetInt32());
        if (e.TryGetProperty("rivals", out var rv)) foreach (var r in rv.EnumerateArray()) emp.Rivals.Add(r.GetInt32());
        return emp;
    }

    // ══════════════════ 团队 ══════════════════
    private static string SerializeTeam(Team t)
    {
        var s = new System.Text.StringBuilder();
        s.Append("{");
        s.Append($"\"name\":{JsonStr(t.Name)},\"task\":{(int)t.Task},\"prodSlider\":{t.ProdSlider}");
        s.Append($",\"captainId\":{(t.Captain != null ? t.Captain.Id : -1)}");
        s.Append(",\"memberIds\":[");
        s.Append(string.Join(",", t.Members.Select(m => m.Id.ToString())));
        s.Append("],\"chemistry\":[");
        int ci = 0;
        foreach (var kv in t.Chemistry)
        { if (ci++ > 0) s.Append(","); s.Append($"{{\"k\":{kv.Key},\"v\":{kv.Value}}}"); }
        s.Append($"],\"targetTech\":{JsonStr(t.TargetTech?.Id ?? "")}");
        s.Append($",\"outsourceRemaining\":{t.OutsourceMonthsRemaining}");
        s.Append(",\"contract\":");
        if (t.CurrentContract.HasValue)
        {
            var c = t.CurrentContract.Value;
            s.Append($"{{\"name\":{JsonStr(c.Name)},\"diff\":{(int)c.Difficulty},\"months\":{c.RequiredMonths},\"pay\":{c.Payment},\"penalty\":{c.PenaltyRate},\"skill\":{(int)c.PrimarySkill},\"minLv\":{c.MinSkillLevel},\"exp\":{c.ExpReward}}}");
        }
        else s.Append("null");
        s.Append(",\"project\":");
        if (t.CurrentProject != null) s.Append(SerializeProj(t.CurrentProject));
        else s.Append("null");
        s.Append("}");
        return s.ToString();
    }

    private Team DeserializeTeam(JsonElement e)
    {
        var t = new Team
        {
            Name = e.GetProperty("name").GetString(), Task = (TeamTask)e.GetProperty("task").GetInt32(),
            ProdSlider = e.GetProperty("prodSlider").GetSingle(), OutsourceMonthsRemaining = e.GetProperty("outsourceRemaining").GetInt32()
        };
        int capId = e.GetProperty("captainId").GetInt32();
        foreach (var id in e.GetProperty("memberIds").EnumerateArray())
        {
            int mid = id.GetInt32();
            var emp = _empMgr.Employees.Find(x => x.Id == mid);
            if (emp != null) { t.Members.Add(emp); if (mid == capId) { t.Captain = emp; emp.IsCaptain = true; } }
        }
        foreach (var c in e.GetProperty("chemistry").EnumerateArray())
            t.Chemistry[c.GetProperty("k").GetInt32()] = c.GetProperty("v").GetSingle();
        string techId = e.GetProperty("targetTech").GetString();
        if (!string.IsNullOrEmpty(techId) && TechTreeData.AllTech.ContainsKey(techId))
            t.TargetTech = TechTreeData.AllTech[techId];
        if (e.TryGetProperty("contract", out var ct) && ct.ValueKind != JsonValueKind.Null)
        {
            t.CurrentContract = new OutsourceContract
            {
                Name = ct.GetProperty("name").GetString(),
                Difficulty = (OutsourceDifficulty)ct.GetProperty("diff").GetInt32(),
                RequiredMonths = ct.GetProperty("months").GetInt32(),
                Payment = ct.GetProperty("pay").GetSingle(),
                PenaltyRate = ct.GetProperty("penalty").GetSingle(),
                PrimarySkill = (SkillType)ct.GetProperty("skill").GetInt32(),
                MinSkillLevel = ct.GetProperty("minLv").GetInt32(),
                ExpReward = ct.GetProperty("exp").GetInt32()
            };
        }
        if (e.TryGetProperty("project", out var pj) && pj.ValueKind != JsonValueKind.Null)
            t.CurrentProject = DeserializeProj(pj);
        return t;
    }

    // ══════════════════ 项目 ══════════════════
    private static string SerializeProj(GameProject p) =>
        JsonSerializer.Serialize(new
        {
            name = p.Name, genre = (int)p.Genre, theme = (int)p.Theme, platform = (int)p.Platform, phase = (int)p.Phase,
            graphics = p.GraphicsScore, gameplay = p.GameplayScore, audio = p.AudioScore,
            network = p.NetworkScore, ai = p.AIScore, stability = p.StabilityScore,
            progress = p.DevProgress, estimated = p.EstimatedMonths, spent = p.MonthsSpent, bugs = p.BugCount, lastSprint = p.LastSprintMonth,
            marketing = (int)p.Marketing, mBudget = p.MarketingBudget, scale = p.Scale,
            priceModel = (int)p.PriceModel, adIntensity = p.AdIntensity,
            story = p.StoryScore, price = p.SuggestedPrice,
            originMonth = p.OriginalReleaseMonth, monthsOnMarket = p.MonthsOnMarket,
            brandPower = p.BrandPower, hasModKit = p.HasModKit, isLongTail = p.IsLongTail,
            postRelease = (int)p.PostRelease, postReleaseCount = p.PostReleaseCount, fanSatisfaction = p.FanSatisfaction,
            expected = p.ExpectedScore, final = p.FinalScore, sales = p.Sales, revenue = p.Revenue,
            released = p.IsReleased, compat = p.GenreThemeCompatibility,
            modProgCore = p.ModuleProgressCore, modProgVisual = p.ModuleProgressVisual, modProgAudio = p.ModuleProgressAudio,
            modProgStory = p.ModuleProgressStory, modProgStability = p.ModuleProgressStability, modProgOnline = p.ModuleProgressOnline,
            budgetGraphics = p.BudgetGraphics, budgetAudio = p.BudgetAudio, budgetGameplay = p.BudgetGameplay,
            engine = p.EngineName ?? "", predecessorScore = p.PredecessorScore, ipName = p.IPName ?? "",
            log = p.DevLog
        });

    private static GameProject DeserializeProj(JsonElement e)
    {
        var jn = JsonNode.Parse(e.GetRawText())!;
        return new()
        {
            Name = (string)jn["name"]!, Genre = (GameGenre)(int)jn["genre"]!, Theme = (GameTheme)(int)jn["theme"]!,
            Platform = (Platform)(int)jn["platform"]!, Phase = (DevPhase)(int)jn["phase"]!,
            GraphicsScore = (float)jn["graphics"]!, GameplayScore = (float)jn["gameplay"]!,
            AudioScore = (float)jn["audio"]!, StoryScore = (float)jn["story"]!,
            NetworkScore = (float)jn["network"]!, AIScore = (float)jn["ai"]!,
            StabilityScore = (float)jn["stability"]!,
            DevProgress = (float)jn["progress"]!, EstimatedMonths = (float)jn["estimated"]!,
            MonthsSpent = (float)jn["spent"]!, LastSprintMonth = jn["lastSprint"] != null ? (float)jn["lastSprint"]! : -3f, BugCount = (int)jn["bugs"]!,
            Marketing = (MarketingStrategy)(int)jn["marketing"]!, MarketingBudget = (float)jn["mBudget"]!,
            Scale = (float)jn["scale"]!, PriceModel = (PriceModel)(int)jn["priceModel"]!,
            AdIntensity = (float)jn["adIntensity"]!, SuggestedPrice = (float)jn["price"]!,
            OriginalReleaseMonth = (int)jn["originMonth"]!, MonthsOnMarket = (int)(float)jn["monthsOnMarket"]!,
            BrandPower = (float)jn["brandPower"]!, HasModKit = (bool)jn["hasModKit"]!, IsLongTail = (bool)jn["isLongTail"]!,
            PostRelease = (PostReleaseType)(int)jn["postRelease"]!, PostReleaseCount = (int)jn["postReleaseCount"]!,
            FanSatisfaction = (float)jn["fanSatisfaction"]!,
            ExpectedScore = (float)jn["expected"]!, FinalScore = (float)jn["final"]!,
            Sales = (int)jn["sales"]!, Revenue = (float)jn["revenue"]!,
            IsReleased = (bool)jn["released"]!, GenreThemeCompatibility = (float)jn["compat"]!,
            ModuleProgressCore = (float)jn["modProgCore"]!, ModuleProgressVisual = (float)jn["modProgVisual"]!,
            ModuleProgressAudio = (float)jn["modProgAudio"]!, ModuleProgressStory = (float)jn["modProgStory"]!,
            ModuleProgressStability = (float)jn["modProgStability"]!, ModuleProgressOnline = (float)jn["modProgOnline"]!,
            BudgetGraphics = (float)jn["budgetGraphics"]!, BudgetAudio = (float)jn["budgetAudio"]!,
            BudgetGameplay = (float)jn["budgetGameplay"]!,
            EngineName = (string)jn["engine"]!, PredecessorScore = (float)jn["predecessorScore"]!,
            IPName = (string)jn["ipName"]!,
            DevLog = jn["log"]!.AsArray().Select(x => (string)x!).ToList()
        };
    }

    // ══════════════════ 科技 ══════════════════
    private string SerializeTechs()
    {
        var s = new System.Text.StringBuilder();
        s.Append("\"techs\":{");
        s.Append($"\"engineOpen\":{JsonBool(_techMgr.EngineOpenForLicense)},");
        s.Append("\"researched\":[");
        int ri = 0;
        foreach (var t in _techMgr.ResearchedTech)
        { if (ri++ > 0) s.Append(","); s.Append(JsonStr(t.Key)); }
        s.Append("],\"progress\":[");
        ri = 0;
        foreach (var kv in _techMgr.ResearchProgress)
        { if (ri++ > 0) s.Append(","); s.Append($"{{\"k\":{JsonStr(kv.Key)},\"v\":{kv.Value}}}"); }
        s.Append("]}");
        return s.ToString();
    }

    private void DeserializeTechs(JsonElement e)
    {
        _techMgr.ResearchedTech.Clear();
        foreach (var t in e.GetProperty("researched").EnumerateArray())
            _techMgr.ResearchedTech[t.GetString()] = true;
        _techMgr.ResearchProgress.Clear();
        foreach (var p in e.GetProperty("progress").EnumerateArray())
            _techMgr.ResearchProgress[p.GetProperty("k").GetString()] = p.GetProperty("v").GetSingle();
        // engineOpenForLicense
        if (e.TryGetProperty("engineOpen", out var eo))
            _techMgr.EngineOpenForLicense = eo.GetBoolean();
    }

    // ══════════════════ 引擎 ══════════════════
    private static string SerializeEngine(GameEngine e)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("{");
        sb.Append($"\"name\":{JsonStr(e.Name)},\"gen\":{e.Generation},\"ver\":{JsonStr(e.VersionTag)}");
        sb.Append($",\"debt\":{e.TechDebt},\"stab\":{e.Stability},\"perf\":{e.Performance},\"eff\":{e.DevEfficiency}");
        sb.Append($",\"position\":{(int)e.Position}");
        sb.Append($",\"biz\":{(int)e.BizModel},\"buyout\":{e.BuyoutPrice},\"sub\":{e.SubscriptionPrice},\"royalty\":{e.RoyaltyRate}");
        sb.Append($",\"licCount\":{e.LicenseCount},\"share\":{e.MarketShare},\"rep\":{e.Reputation}");
        sb.Append($",\"monRev\":{e.MonthlyRevenue},\"totalRev\":{e.TotalRevenue}");
        sb.Append($",\"deprecated\":{JsonBool(e.IsDeprecated)},\"isDev\":{JsonBool(e.IsDeveloping)},\"devRemaining\":{e.DevMonthsRemaining}");
        sb.Append(",\"techs\":[");
        for (int i = 0; i < e.AppliedTechs.Count; i++)
        { if (i > 0) sb.Append(","); sb.Append(JsonStr(e.AppliedTechs[i])); }
        sb.Append("]");
        sb.Append("}");
        return sb.ToString();
    }

    private static GameEngine DeserializeEngine(JsonElement e)
    {
        var eng = new GameEngine
        {
            Name = e.GetProperty("name").GetString(),
            Generation = e.GetProperty("gen").GetInt32(),
            VersionTag = e.GetProperty("ver").GetString(),
            TechDebt = e.GetProperty("debt").GetSingle(),
            Stability = e.GetProperty("stab").GetSingle(),
            Performance = e.GetProperty("perf").GetSingle(),
            DevEfficiency = e.GetProperty("eff").GetSingle(),
            Position = (EnginePosition)e.GetProperty("position").GetInt32(),
            BizModel = (EngineBizModel)e.GetProperty("biz").GetInt32(),
            BuyoutPrice = e.GetProperty("buyout").GetSingle(),
            SubscriptionPrice = e.GetProperty("sub").GetSingle(),
            RoyaltyRate = e.GetProperty("royalty").GetSingle(),
            LicenseCount = e.GetProperty("licCount").GetInt32(),
            MarketShare = e.GetProperty("share").GetSingle(),
            Reputation = e.GetProperty("rep").GetSingle(),
            MonthlyRevenue = e.GetProperty("monRev").GetSingle(),
            TotalRevenue = e.GetProperty("totalRev").GetSingle(),
            IsDeprecated = e.GetProperty("deprecated").GetBoolean(),
            IsDeveloping = e.GetProperty("isDev").GetBoolean(),
            DevMonthsRemaining = e.GetProperty("devRemaining").GetInt32()
        };
        foreach (var t in e.GetProperty("techs").EnumerateArray())
            eng.AppliedTechs.Add(t.GetString());
        eng.UpdateCapabilities();
        eng.DerivePerks();
        return eng;
    }

    // ══════════════════ 对手 ══════════════════
    private static string SerializeStudio(AIStudio s)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("{");
        sb.Append($"\"name\":{JsonStr(s.Name)},\"founded\":{s.FoundedMonth},\"rep\":{s.Reputation}");
        sb.Append($",\"money\":{s.Money},\"empCount\":{s.EmployeeCount},\"hasEngine\":{JsonBool(s.HasPlayerEngine)}");
        sb.Append($",\"listed\":{JsonBool(s.IsListed)},\"sPrice\":{s.SharePrice},\"sOut\":{s.SharesOutstanding}");
        sb.Append($",\"expProfit\":{s.ExpectedProfit},\"divRate\":{s.DividendRate},\"bkCounter\":{s.BankruptcyCounter}");
        sb.Append($",\"trVol\":{s.TradingVolume},\"lastQRev\":{s.LastQuarterRevenue},\"lastQExp\":{s.LastQuarterExpense},\"lastQProfit\":{s.LastQuarterProfit}");
        // Shareholders
        sb.Append(",\"sh\":{");
        bool firstSh = true;
        foreach (var kv in s.Shareholders)
        {
            if (!firstSh) sb.Append(","); firstSh = false;
            sb.Append($"{JsonStr(kv.Key)}:{kv.Value}");
        }
        sb.Append("}");
        // PriceHistory
        sb.Append(",\"ph\":[");
        for (int i = 0; i < s.PriceHistory.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append($"{{\"m\":{s.PriceHistory[i].month},\"p\":{s.PriceHistory[i].price}}}");
        }
        sb.Append("]");
        // Releases
        sb.Append(",\"releases\":[");
        for (int i = 0; i < s.Releases.Count; i++)
        {
            if (i > 0) sb.Append(",");
            var r = s.Releases[i];
            sb.Append($"{{\"n\":{JsonStr(r.Name)},\"sc\":{r.Score},\"sa\":{r.Sales},\"m\":{r.ReleaseMonth}}}");
        }
        sb.Append("]}");
        return sb.ToString();
    }

    private static AIStudio DeserializeStudio(JsonElement e)
    {
        var s = new AIStudio
        {
            Name = e.GetProperty("name").GetString(), FoundedMonth = e.GetProperty("founded").GetInt32(),
            Reputation = e.GetProperty("rep").GetInt32(), Money = e.GetProperty("money").GetSingle(),
            EmployeeCount = e.GetProperty("empCount").GetInt32(), HasPlayerEngine = e.GetProperty("hasEngine").GetBoolean()
        };
        // Stock data (with fallback for old saves)
        if (e.TryGetProperty("listed", out var listed)) s.IsListed = listed.GetBoolean();
        if (e.TryGetProperty("sPrice", out var sp)) s.SharePrice = sp.GetSingle();
        if (e.TryGetProperty("sOut", out var so)) s.SharesOutstanding = so.GetInt32();
        if (e.TryGetProperty("expProfit", out var ep)) s.ExpectedProfit = ep.GetSingle();
        if (e.TryGetProperty("divRate", out var dr)) s.DividendRate = dr.GetSingle();
        if (e.TryGetProperty("bkCounter", out var bk)) s.BankruptcyCounter = bk.GetInt32();
        if (e.TryGetProperty("trVol", out var tv)) s.TradingVolume = tv.GetSingle();
        if (e.TryGetProperty("lastQRev", out var lqr)) s.LastQuarterRevenue = lqr.GetSingle();
        if (e.TryGetProperty("lastQExp", out var lqe)) s.LastQuarterExpense = lqe.GetSingle();
        if (e.TryGetProperty("lastQProfit", out var lqp)) s.LastQuarterProfit = lqp.GetSingle();
        // Shareholders
        if (e.TryGetProperty("sh", out var sh))
        {
            foreach (var kv in sh.EnumerateObject())
                s.Shareholders[kv.Name] = kv.Value.GetInt32();
        }
        // PriceHistory
        if (e.TryGetProperty("ph", out var ph))
        {
            foreach (var p in ph.EnumerateArray())
                s.PriceHistory.Add((p.GetProperty("m").GetInt32(), p.GetProperty("p").GetSingle()));
        }
        // Releases
        foreach (var r in e.GetProperty("releases").EnumerateArray())
            s.Releases.Add(new AIStudio.AIGameRelease
            {
                Name = r.GetProperty("n").GetString(), Score = r.GetProperty("sc").GetSingle(),
                Sales = r.GetProperty("sa").GetInt32(), ReleaseMonth = r.GetProperty("m").GetInt32()
            });
        return s;
    }

    // ══════════════════ 主机生命周期 ══════════════════
    private int _currentConsoleGen = 1;
    private int _consoleKitCost = 8000;
    public int ConsoleKitCost => _consoleKitCost;
    private bool _hasDevKitCurrent = true;
    private bool _hasDevKitNext = false;
    private int _monthsUntilNextConsole = 72;
    private int _nextConsoleGen = 2;
    public int ConsoleSwitchMonth { get; private set; } // 最近一次主机换代的月份

    public int CurrentConsoleGen => _currentConsoleGen;
    public bool HasDevKitForPlatform(Platform p)
    {
        if (p == Platform.PC) return true;
        return _hasDevKitCurrent; // 主机需要开发套件
    }

    private void CheckConsoleLifecycle()
    {
        _monthsUntilNextConsole--;
        if (_monthsUntilNextConsole <= 0)
        {
            _currentConsoleGen = _nextConsoleGen;
            _nextConsoleGen++;
            _hasDevKitCurrent = _hasDevKitNext;
            _hasDevKitNext = false;
            _consoleKitCost = (int)(_consoleKitCost * 1.3f);
            _monthsUntilNextConsole = 60 + new Random().Next(24);
            ConsoleSwitchMonth = GameMonth;

            ShowPopup(Loc.Tr("comp.console_gen_title"),
                Loc.TrF("comp.console_gen_body", _currentConsoleGen, _consoleKitCost),
                new Color(0.2f, 0.7f, 1f));
        }

        // 旧主机退市提醒（最后一年）
        if (_monthsUntilNextConsole < 12 && _monthsUntilNextConsole == 11)
        {
            ShowToast(Loc.Tr("comp.console_gen_warn"),
                    Loc.TrF("comp.console_gen_warn_body", _nextConsoleGen),
                    new Color(1f, 0.7f, 0.2f));
        }
    }

    public bool BuyDevKitNext()
    {
        if (_hasDevKitNext) return true;
        if (_res.SpendMoney(_consoleKitCost, "dev_kit"))
        {
            _hasDevKitNext = true;
            ShowToast(Loc.Tr("comp.kit_purchased_title"), Loc.TrF("comp.kit_purchased", _nextConsoleGen), new Color(0.3f, 0.8f, 0.3f));
            return true;
        }
        return false;
    }

    /// <summary>开发成本追踪</summary>
    public void TrackDevCost(long cost) { /* tracked in ResourceManager.SpendMoney */ }

    // ══════════════════ 员工留人弹窗 ══════════════════
    private void ShowEmployeeRetentionDialog(Employee emp)
    {
        if (emp == null || !emp.ConsideringOffer) return;

        float counterCost = emp.OfferAmount * 1.2f;
        string msg = Loc.TrF("story.poach_offer_msg", emp.Name, emp.TargetCompanyName, emp.OfferAmount);

        if (_res.Money >= counterCost)
        {
            bool autoKeep = emp.IsLegendary || emp.Loyalty > 30;
            if (autoKeep)
            {
                _res.SpendMoney(counterCost, "counter_offer");
                emp.ConsideringOffer = false;
                emp.Loyalty = Mathf.Min(100, emp.Loyalty + 20f);
                emp.Salary *= 1.2f;
                ShowToast(Loc.Tr("toast.counter_ok"), Loc.TrF("toast.counter_ok_msg", emp.Name, emp.Salary), new Color(0.3f, 0.8f, 0.3f));
            }
            else
            {
                LetEmployeeGo(emp);
            }
        }
        else
        {
            ShowPopup(Loc.Tr("story.poach_offer_title"),
                msg + $"\n\n{Loc.Tr("story.poach_letgo_no_money")}",
                new Color(0.9f, 0.3f, 0.3f));
            LetEmployeeGo(emp);
        }
    }

    private void LetEmployeeGo(Employee emp)
    {
        if (emp == null) return;
        emp.ConsideringOffer = false;

        // 王牌离职触发事件
        if (emp.IsLegendary)
        {
            var compAI = GetNodeOrNull<CompetitorAI>("CompetitorAI");
            if (compAI != null)
            {
                compAI.HandleDefection(emp, emp.TargetCompanyName);
            }
            ShowToast(Loc.Tr("toast.legendary_left"), Loc.TrF("toast.legendary_left_msg", emp.Name, emp.TargetCompanyName), new Color(0.9f, 0.2f, 0.2f));
        }

        _empMgr.RemoveEmployee(emp);
        _teamMgr.RemoveFromAllTeams(emp);
        ShowToast(Loc.Tr("toast.employee_left"), Loc.TrF("toast.employee_left_msg", emp.Name, emp.TargetCompanyName), new Color(0.8f, 0.4f, 0.2f));
    }
}
