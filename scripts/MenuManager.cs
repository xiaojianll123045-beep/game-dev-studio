using System;
using System.Collections.Generic;
using Godot;

public partial class MenuManager : Node
{
	private CanvasLayer _canvas;
	private Panel _settingsPanel, _aboutPanel, _loadPanel;
	private Control _ui;
	private OptionButton _modeBtn, _resBtn;
	private CheckBox _vsyncCb;
	private AudioStreamPlayer _menuMusic;

	private int _resIdx = 4;
	private int _modeIdx = 1;
	private int _fpsCap = 0;
	private bool _vsync = true;
	private float _uiScale = 1.0f;

	private int _musicFrames;

	// ── UI 缩放：Control.Scale + PivotOffset，所有子控件自动等比缩放 ──

	public override void _Ready()
	{
		_canvas = new CanvasLayer();
		AddChild(_canvas);
		_ui = new Control();
		_ui.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_canvas.AddChild(_ui);
		GlobalSettings.Load();

		Loc.Init();
		if (GlobalSettings.Language >= 0)
			Loc.CurrentLang = GlobalSettings.Language;

		_uiScale = GlobalSettings.UIScale;
		_resIdx = GlobalSettings.Resolution;
		_modeIdx = GlobalSettings.DisplayMode;
		_fpsCap = GlobalSettings.FpsCap;
		_vsync = GlobalSettings.VSync;
		GlobalSettings.ApplyAll();
		BuildUI();

		// 主菜单 BGM
		_menuMusic = new AudioStreamPlayer { Name = "MenuMusic", VolumeDb = -6f, Bus = "Master" };
		AddChild(_menuMusic);
		_menuMusic.Finished += () => _menuMusic.Play();
		var stream = ResourceLoader.Load<AudioStream>("res://assets/sounds/bgm_加载.wav");
		if (stream != null) {
			_menuMusic.Stream = stream;
		} else GD.PrintErr("MenuMusic: load failed");
	}

	public override void _Process(double delta)
	{
		if (_menuMusic == null || _menuMusic.Stream == null) return;
		_musicFrames++;
		if (_musicFrames == 3)
		{
			// Enable autoplay via engine and force play
			_menuMusic.Play();
			GD.Print("MenuMusic: play at frame 3");
		}
		if (_musicFrames == 10)
		{
			if (!_menuMusic.Playing)
			{
				_menuMusic.Play();
				GD.Print("MenuMusic: retry at frame 10");
			}
			SetProcess(false);
		}
	}

	private void ApplyMusicVol()
	{
		if (_menuMusic == null) return;
		if (GlobalSettings.MusicEnabled && GlobalSettings.SoundEnabled)
		{
			float mv = GlobalSettings.MusicVolume / 100f * 80f - 80f;
			_menuMusic.VolumeDb = mv;
			if (!_menuMusic.Playing) _menuMusic.Play();
		}
		else { _menuMusic.Stop(); }
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		var args = ModMethodOverride.Args(("event", @event));
		// ESC 关闭日志覆盖层
		if (@event is InputEventKey esk && esk.Pressed && !esk.Echo && esk.Keycode == Key.Escape)
		{
			if (_menuLogOverlay != null) { _menuLogOverlay.QueueFree(); _menuLogOverlay = null; return; }
		}
		ModMethodOverride.CallVoid("menumanager_unhandled_input", args, () =>
		{
			if (@event is InputEventKey ke && ke.Pressed)
			{
				if (ke.Keycode == Key.Escape)
					CloseAllPanels();
				else if (ke.Keycode == Key.F9)
					ShowMenuModLog();
			}
		});
	}

	private void CloseAllPanels()
	{
		if (_aboutPanel != null && _aboutPanel.Visible) { _aboutPanel.Visible = false; return; }
		if (_loadPanel != null && _loadPanel.Visible) { _loadPanel.Visible = false; return; }
		if (_settingsPanel != null && _settingsPanel.Visible) { _settingsPanel.Visible = false; GlobalSettings.Save(); return; }
	}

	private Label MkLabel(string text, float x, float y, float w, float h, float fs, Color color, HorizontalAlignment ha = HorizontalAlignment.Left)
	{
		var l = new Label { Text = text, Position = new(x, y), Size = new(w, h), HorizontalAlignment = ha };
		l.AddThemeFontSizeOverride("font_size", (int)fs);
		l.AddThemeColorOverride("font_color", color);
		return l;
	}

	private Panel MkPanel(float x, float y, float w, float h)
	{
		var p = new Panel { Position = new(x, y), Size = new(w, h) };
		p.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = Colors.White });
		return p;
	}

	private static StyleBoxFlat MakeStyle(Color bg, Color border) => new()
	{
		BgColor = new(bg, 0.75f),
		BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
		BorderColor = new(border, 0.5f),
		CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
		CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
	};

	private static StyleBoxFlat MkOptBg() => new()
	{
		BgColor = new Color(0.97f, 0.96f, 0.94f, 0.95f),
		BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
		BorderColor = new Color(0.55f, 0.55f, 0.55f, 0.4f),
		CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
		CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3
	};

	private void BuildUI()
	{
		var vp = GetViewport().GetVisibleRect().Size;
		float cx = vp.X / 2, cy = vp.Y / 2;

		// 背景（不缩放）
		_ui.AddChild(new ColorRect { Position = new(0, 0), Size = new(vp.X, vp.Y), Color = new Color(0.97f, 0.96f, 0.94f), MouseFilter = Control.MouseFilterEnum.Ignore });

		for (int i = 0; i < 8; i++)
		{
			float y = vp.Y * 0.1f + i * vp.Y * 0.05f;
			_ui.AddChild(new ColorRect { Position = new(0, y), Size = new(vp.X, 2), Color = new Color(0.70f, 0.72f, 0.75f, 0.25f), MouseFilter = Control.MouseFilterEnum.Ignore });
		}
		_ui.AddChild(new ColorRect { Position = new(0, 0), Size = new(vp.X, 2), Color = new Color(0.3f, 0.5f, 0.9f, 0.5f), MouseFilter = Control.MouseFilterEnum.Ignore });
		_ui.AddChild(new ColorRect { Position = new(0, vp.Y - 2), Size = new(vp.X, 2), Color = new Color(0.3f, 0.5f, 0.9f, 0.5f), MouseFilter = Control.MouseFilterEnum.Ignore });

		// 标题
		_ui.AddChild(new ColorRect { Position = new(cx - 280, 40), Size = new(560, 100), Color = new Color(0.90f, 0.93f, 0.92f, 0.35f), MouseFilter = Control.MouseFilterEnum.Ignore });
		_ui.AddChild(MkLabel(Loc.Tr("menu.title"), cx - 200, 50, 400, 45, 36, new Color(0.10f, 0.14f, 0.22f), HorizontalAlignment.Center));
		_ui.AddChild(MkLabel(Loc.Tr("menu.subtitle"), cx - 260, 95, 520, 20, 11, new Color(0.5f, 0.7f, 0.95f), HorizontalAlignment.Center));
		_ui.AddChild(new ColorRect { Position = new(cx - 180, 155), Size = new(360, 1), Color = new Color(0.25f, 0.4f, 0.7f, 0.4f), MouseFilter = Control.MouseFilterEnum.Ignore });

		// 按钮
		var btnContainer = new VBoxContainer { Position = new(cx - 130, 175), Alignment = BoxContainer.AlignmentMode.Center };
		btnContainer.AddThemeConstantOverride("separation", 10);
		_ui.AddChild(btnContainer);

		string[] labels = { Loc.Tr("menu.new_game"), Loc.Tr("menu.continue"), Loc.Tr("menu.load_game"), Loc.Tr("menu.settings"), Loc.Tr("menu.about"), Loc.Tr("mod.title"), Loc.Tr("menu.exit") };
		System.Action[] actions = { OnNewGame, OnContinue, OnLoadGame, OnSettings, OnAbout, ShowModList, () => GetTree().Quit() };

		var buttonRefs = new List<Button>();
		for (int i = 0; i < labels.Length; i++)
		{
			int idx = i;
			var btn = new Button { Text = labels[i], CustomMinimumSize = new(260, 40), Size = new(260, 40), Flat = true };
			btn.AddThemeFontSizeOverride("font_size", 13);
			btn.AddThemeColorOverride("font_color", new Color(0.15f, 0.18f, 0.22f));
			btn.AddThemeColorOverride("font_hover_color", new Color(0.25f, 0.30f, 0.35f));
			btn.AddThemeColorOverride("font_pressed_color", new Color(0.10f, 0.14f, 0.22f));
			btn.AddThemeStyleboxOverride("normal", MakeStyle(new(0.97f, 0.96f, 0.94f), new(0.3f, 0.4f, 0.55f)));
			btn.AddThemeStyleboxOverride("hover", MakeStyle(new(0.88f, 0.87f, 0.84f), new(0.2f, 0.35f, 0.6f)));
			btn.AddThemeStyleboxOverride("pressed", MakeStyle(new(0.80f, 0.79f, 0.76f), new(0.3f, 0.45f, 0.7f)));
			btn.AddThemeStyleboxOverride("disabled", MakeStyle(new(0.95f, 0.94f, 0.92f), new(0.6f, 0.6f, 0.6f)));
			btn.Pressed += actions[idx];
			btnContainer.AddChild(btn);

			buttonRefs.Add(btn);
		}

		// 禁用「继续游戏」如果没有存档
		{
			bool hasSave = false;
			for (int s = 1; s <= 5; s++)
			{
				if (FileAccess.FileExists(GlobalSettings.GetSlotPath(s))) { hasSave = true; break; }
			}
			if (!hasSave && FileAccess.FileExists(GlobalSettings.GetAutoSavePath())) hasSave = true;
			var continueBtn = buttonRefs[1];
			continueBtn.Disabled = !hasSave;
			if (continueBtn.Disabled) continueBtn.Modulate = new Color(1, 1, 1, 0.35f);
		}
		// 禁用「读取存档」如果没有存档
		{
			bool hasSave = false;
			for (int s = 1; s <= 5; s++)
			{
				if (FileAccess.FileExists(GlobalSettings.GetSlotPath(s))) { hasSave = true; break; }
			}
			if (!hasSave && FileAccess.FileExists(GlobalSettings.GetAutoSavePath())) hasSave = true;
			var loadBtn = buttonRefs[2];
			loadBtn.Disabled = !hasSave;
			if (loadBtn.Disabled) loadBtn.Modulate = new Color(1, 1, 1, 0.35f);
		}

		_ui.AddChild(MkLabel(Loc.Tr("menu.footer"), cx - 180, vp.Y - 28, 360, 20, 9, new Color(0.35f, 0.4f, 0.5f), HorizontalAlignment.Center));

		// 四角
		for (int r = 0; r < 2; r++)
			for (int c = 0; c < 2; c++)
			{
				float dx = c == 0 ? 15 : vp.X - 25, dy = r == 0 ? 15 : vp.Y - 25;
				_ui.AddChild(new ColorRect { Position = new(dx, dy), Size = new(8, 2), Color = new Color(0.3f, 0.55f, 1f, 0.5f), MouseFilter = Control.MouseFilterEnum.Ignore });
				_ui.AddChild(new ColorRect { Position = new(dx, dy), Size = new(2, 8), Color = new Color(0.3f, 0.55f, 1f, 0.5f), MouseFilter = Control.MouseFilterEnum.Ignore });
			}

		BuildSettingsPanel(cx, cy);
		BuildAboutPanel(cx, cy);
		BuildLoadPanel(cx, cy);

		// UI 缩放：以屏幕中心为 pivot，Scale 自动等比缩放所有子控件
		// Position=(0,0), PivotOffset=屏幕中心 → 缩放时中心不动，四周均匀扩展/收缩
		_ui.PivotOffset = new Vector2(cx, cy);
		_ui.Scale = new Vector2(_uiScale, _uiScale);
	}

	private void AddSettingRow(Panel parent, string label, float x, float y)
	{
		var l = new Label { Text = label, Position = new(x, y + 1), Size = new(90, 20) };
		l.AddThemeFontSizeOverride("font_size", 11);
		l.AddThemeColorOverride("font_color", new Color(0.15f, 0.18f, 0.22f));
		parent.AddChild(l);
	}

	private void BuildSettingsPanel(float cx, float cy)
	{
		float pw = 540, ph = 660;
		var dp = new DragPanel { Position = new(cx - pw / 2, cy - ph / 2), Size = new(pw, ph) };
		dp.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = Colors.White, CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.3f, 0.3f, 0.3f, 0.3f) });
		_settingsPanel = dp;
		_settingsPanel.Visible = false;
		_ui.AddChild(_settingsPanel);

		// 标题栏
		var titleLbl = LUI.Label(Loc.Tr("set.title"), 16, new Color(0.10f, 0.14f, 0.22f));
		titleLbl.Position = new(20, 8);
		_settingsPanel.AddChild(titleLbl);

		// 可滚动内容区域（标题与关闭按钮之间）
		var scroll = new ScrollContainer { Position = new(20, 42), Size = new(pw - 40, ph - 42 - 56) };
		_settingsPanel.AddChild(scroll);

		// 内容根布局
		var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		root.AddThemeConstantOverride("separation", 12);
		scroll.AddChild(root);
		// 上下边距（用空控件撑开）
		root.AddChild(new Control { CustomMinimumSize = new(0, 8) });

		// 每行高度固定 28px（OptionButton 等控件的最小合理高度）
		const float rowH = 34;

		// ── 显示设置 ──
		_modeBtn = MkOpt(new[] { Loc.Tr("set.window"), Loc.Tr("set.borderless"), Loc.Tr("set.fullscreen") }, _modeIdx);
		_modeBtn.ItemSelected += (long i) => ApplyDisplayMode((int)i);
		AddSettingRowLayout(root, Loc.Tr("set.display"), _modeBtn, rowH);

		_resBtn = MkOpt(GlobalSettings.ResNames, _resIdx);
		_resBtn.ItemSelected += (long i) => ApplyResolution((int)i);
		AddSettingRowLayout(root, Loc.Tr("set.resolution"), _resBtn, rowH);

		// 帧率
		var fpsSlider = new HSlider { MinValue = 0, MaxValue = GlobalSettings.FpsOptions.Length - 1, Step = 1, CustomMinimumSize = new(0, rowH) };
		int initFpsIdx = System.Array.IndexOf(GlobalSettings.FpsOptions, _fpsCap);
		if (initFpsIdx < 0) initFpsIdx = 0;
		fpsSlider.Value = initFpsIdx;
		var fpsLabel = LUI.Label(_fpsCap == 0 ? Loc.Tr("set.fps_off") : $"{_fpsCap} FPS", 11, Colors.Black, rowH);
		fpsSlider.ValueChanged += (v) =>
		{
			int i = (int)v; _fpsCap = GlobalSettings.FpsOptions[i];
			fpsLabel.Text = _fpsCap == 0 ? Loc.Tr("set.fps_unlimited") : Loc.TrF("set.fps_fmt", _fpsCap);
			Engine.MaxFps = _fpsCap; GlobalSettings.FpsCap = _fpsCap;
		};
		{ var hb = RowHBox(rowH); hb.Add(LUI.Label(Loc.Tr("set.fps"), 11, Colors.Black, rowH)); hb.Add(fpsSlider, 1); hb.Add(fpsLabel); root.AddChild(hb); }

		// 垂直同步
		_vsyncCb = new CheckBox { Text = Loc.Tr("set.vsync_enable"), ButtonPressed = _vsync };
		_vsyncCb.AddThemeFontSizeOverride("font_size", 11);
		_vsyncCb.AddThemeColorOverride("font_color", new Color(0.10f, 0.14f, 0.22f));
		_vsyncCb.Toggled += (on) => { _vsync = on; DisplayServer.WindowSetVsyncMode(on ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled); GlobalSettings.VSync = on; };
		{ var hb = RowHBox(rowH); hb.Add(LUI.Label(Loc.Tr("set.vsync"), 11, Colors.Black, rowH)); hb.Add(_vsyncCb); hb.Add(new Control(), 1); root.AddChild(hb); }

		// 分隔线
		root.AddChild(new ColorRect { Color = new Color(0.70f, 0.72f, 0.75f, 0.25f), CustomMinimumSize = new(0, 1) });

		// ── UI 缩放 ──
		float[] scales = { 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f, 1.1f, 1.2f, 1.3f, 1.5f, 1.7f, 2.0f };
		string[] scaleNames = { "0.5x", "0.6x", "0.7x", "0.8x", "0.9x", "1.0x", "1.1x", "1.2x", "1.3x", "1.5x", "1.7x", "2.0x" };
		int selScale = 5;
		for (int si = 0; si < scales.Length; si++)
			if (Mathf.Abs(scales[si] - _uiScale) < 0.05f) selScale = si;
		var scaleOpt = MkOpt(scaleNames, selScale);
		scaleOpt.ItemSelected += (long i) => { _uiScale = scales[i]; GlobalSettings.UIScale = _uiScale; _ui.Scale = new Vector2(_uiScale, _uiScale); GlobalSettings.Save(); };
		AddSettingRowLayout(root, Loc.Tr("set.ui_scale"), scaleOpt, rowH);

		// 语言
		var langOpt = new OptionButton();
		foreach (var n in Loc.LangLabels) langOpt.AddItem(n);
		langOpt.Selected = Loc.CurrentLang;
		langOpt.AddThemeFontSizeOverride("font_size", 11);
		langOpt.CustomMinimumSize = new(200, rowH);
		langOpt.ItemSelected += (long i) => { Loc.SetLang((int)i); GlobalSettings.Language = (int)i; _settingsPanel.Visible = false; GlobalSettings.Save(); GetTree().ReloadCurrentScene(); };
		AddSettingRowLayout(root, Loc.Tr("set.lang"), langOpt, rowH);

		// ── 音效开关 ──
		var soundCb = new CheckBox { Text = Loc.Tr("set.sound_enable"), ButtonPressed = GlobalSettings.SoundEnabled };
		soundCb.AddThemeFontSizeOverride("font_size", 11);
		soundCb.AddThemeColorOverride("font_color", Colors.Black);
		soundCb.AddThemeColorOverride("font_hover_color", new Color(0.4f, 0.4f, 0.4f));
		soundCb.AddThemeColorOverride("font_pressed_color", Colors.Black);
		soundCb.AddThemeColorOverride("font_focus_color", Colors.Black);
		soundCb.Toggled += (on) => { GlobalSettings.SoundEnabled = on; GlobalSettings.Save(); };
		{ var hb = RowHBox(rowH); hb.Add(LUI.Label(Loc.Tr("set.sound"), 11, Colors.Black, rowH)); hb.Add(soundCb); hb.Add(new Control(), 1); root.AddChild(hb); }

		// 音效音量
		{
			var slider = new HSlider { MinValue = 0, MaxValue = 100, Value = GlobalSettings.SoundVolume, CustomMinimumSize = new(150, 0) };
			slider.ValueChanged += (v) => { GlobalSettings.SoundVolume = (float)v; GlobalSettings.Save(); };
			{ var hb = RowHBox(rowH); hb.Add(LUI.Label(Loc.Tr("set.sound_volume"), 11, Colors.Black, rowH)); hb.Add(slider); hb.Add(new Control(), 1); root.AddChild(hb); }
		}

		// 音乐开关
		{
			var musicCb = new CheckBox { Text = Loc.Tr("set.music_enable"), ButtonPressed = GlobalSettings.MusicEnabled };
			musicCb.AddThemeFontSizeOverride("font_size", 11);
			musicCb.AddThemeColorOverride("font_color", Colors.Black);
			musicCb.AddThemeColorOverride("font_hover_color", new Color(0.4f, 0.4f, 0.4f));
			musicCb.AddThemeColorOverride("font_pressed_color", Colors.Black);
			musicCb.AddThemeColorOverride("font_focus_color", Colors.Black);
			musicCb.Toggled += (on) => { GlobalSettings.MusicEnabled = on; ApplyMusicVol(); GlobalSettings.Save(); };
			{ var hb = RowHBox(rowH); hb.Add(LUI.Label(Loc.Tr("set.music"), 11, Colors.Black, rowH)); hb.Add(musicCb); hb.Add(new Control(), 1); root.AddChild(hb); }
		}

		// 音乐音量
		{
			var slider = new HSlider { MinValue = 0, MaxValue = 100, Value = GlobalSettings.MusicVolume, CustomMinimumSize = new(150, 0) };
			slider.ValueChanged += (v) => { GlobalSettings.MusicVolume = (float)v; ApplyMusicVol(); GlobalSettings.Save(); };
			{ var hb = RowHBox(rowH); hb.Add(LUI.Label(Loc.Tr("set.music_volume"), 11, Colors.Black, rowH)); hb.Add(slider); hb.Add(new Control(), 1); root.AddChild(hb); }
		}

		// 名称缩写（仅阿拉伯语）
		if (Loc.CurrentLang == 10)
		{
			var abbrCb = new CheckBox { Text = Loc.Tr("set.name_abbr_desc"), ButtonPressed = GlobalSettings.ArabicNameAbbr };
			abbrCb.AddThemeFontSizeOverride("font_size", 10); abbrCb.AddThemeColorOverride("font_color", Colors.Black);
			abbrCb.CustomMinimumSize = new(0, rowH);
			abbrCb.Toggled += (b) => { GlobalSettings.ArabicNameAbbr = b; GlobalSettings.Save(); };
			{ var hb = RowHBox(rowH); hb.Add(LUI.Label(Loc.Tr("set.name_abbr"), 11, Colors.Black, rowH)); hb.Add(abbrCb); hb.Add(new Control(), 1); root.AddChild(hb); }
		}

		// 分隔线 + 存档设置标题
		root.AddChild(new ColorRect { Color = new Color(0.70f, 0.72f, 0.75f, 0.25f), CustomMinimumSize = new(0, 1) });
		root.AddChild(LUI.Label(Loc.Tr("set.save_section"), 12, new Color(0.10f, 0.14f, 0.22f), rowH + 4));

		// 存档位
		var slotOpt = new OptionButton();
		for (int i = 1; i <= 5; i++) slotOpt.AddItem(GlobalSettings.SaveSlotLabel(i));
		slotOpt.Selected = GlobalSettings.SaveSlot - 1;
		slotOpt.AddThemeFontSizeOverride("font_size", 11); slotOpt.CustomMinimumSize = new(200, rowH);
		slotOpt.ItemSelected += (long i) => GlobalSettings.SaveSlot = (int)i + 1;
		AddSettingRowLayout(root, Loc.Tr("set.save_slot"), slotOpt, rowH);

		// 自动存档
		var autoOpt = new OptionButton();
		foreach (var n in GlobalSettings.AutoSaveNames) autoOpt.AddItem(GlobalSettings.AutoSaveDisplayName(n));
		int autoIdx = System.Array.IndexOf(GlobalSettings.AutoSaveValues, GlobalSettings.AutoSaveIntervalMonths);
		autoOpt.Selected = autoIdx < 0 ? 0 : autoIdx;
		autoOpt.AddThemeFontSizeOverride("font_size", 11); autoOpt.CustomMinimumSize = new(200, rowH);
		autoOpt.ItemSelected += (long i) => { GlobalSettings.AutoSaveIntervalMonths = GlobalSettings.AutoSaveValues[i]; GlobalSettings.AutoSaveEnabled = i > 0; };
		AddSettingRowLayout(root, Loc.Tr("set.autosave"), autoOpt, rowH);

		// 存档路径
		var pathEdit = new LineEdit { Text = GlobalSettings.CustomSavePath, PlaceholderText = Loc.Tr("set.save_path_hint"), CustomMinimumSize = new(0, rowH) };
		pathEdit.AddThemeFontSizeOverride("font_size", 10);
		pathEdit.TextChanged += (txt) => GlobalSettings.CustomSavePath = txt.Trim();
		{ var hb = RowHBox(rowH); hb.Add(LUI.Label(Loc.Tr("set.save_path"), 11, Colors.Black, rowH)); hb.Add(pathEdit, 1); root.AddChild(hb); }
		root.AddChild(LUI.Label(Loc.Tr("set.path_hint"), 8, new Color(0.4f, 0.45f, 0.55f), 16));
		// 底部撑开
		root.AddChild(new Control { CustomMinimumSize = new(0, 12) });

		// 关闭按钮（固定在面板底部，在 ScrollContainer 之外）
		float bw = 120, bh = 34;
		var closeBtn = new Button { Text = Loc.Tr("set.cancel"), Flat = true };
		closeBtn.AddThemeFontSizeOverride("font_size", 13);
		closeBtn.AddThemeColorOverride("font_color", Colors.Black);
		closeBtn.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.90f, 0.89f, 0.86f), CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.45f, 0.5f, 0.55f, 0.3f) });
		closeBtn.Size = new(bw, bh);
		closeBtn.Position = new((pw - bw) / 2, ph - bh - 12);
		closeBtn.Pressed += () => _settingsPanel.Visible = false;
		_settingsPanel.AddChild(closeBtn);
	}

	private static LinearLayout RowHBox(float h)
	{
		var hb = LUI.HBox(8);
		hb.CustomMinimumSize = new(0, h);
		return hb;
	}

	private void AddSettingRowLayout(Control parent, string labelText, Control widget, float rowH)
	{
		var row = LUI.HBox(8);
		row.CustomMinimumSize = new(0, rowH);
		row.Add(LUI.Label(labelText, 11, Colors.Black, rowH));
		widget.CustomMinimumSize = new(widget.CustomMinimumSize.X > 0 ? widget.CustomMinimumSize.X : 200, rowH);
		row.Add(widget);
		row.Add(new Control(), 1);
		parent.AddChild(row);
	}

	private OptionButton MkOpt(string[] names, int selected)
	{
		var opt = new OptionButton();
		foreach (var n in names) opt.AddItem(n);
		opt.Selected = selected;
		opt.AddThemeFontSizeOverride("font_size", 11);
		opt.AddThemeColorOverride("font_color", new Color(0.15f, 0.18f, 0.22f));
		opt.CustomMinimumSize = new(200, 26);
		return opt;
	}

	private void BuildAboutPanel(float cx, float cy)
	{
		float pw = 600, ph = 520;
		_aboutPanel = MkPanel(cx - pw / 2, cy - ph / 2, pw, ph);
		_aboutPanel.Visible = false;
		_ui.AddChild(_aboutPanel);

		_aboutPanel.AddChild(MkLabel(Loc.Tr("menu.about"), pw / 2 - 50, 10, 100, 24, 16, new Color(0.10f, 0.14f, 0.22f), HorizontalAlignment.Center));
		_aboutPanel.AddChild(new ColorRect { Position = new(20, 38), Size = new(pw - 40, 1), Color = new Color(0.70f, 0.72f, 0.75f, 0.25f), MouseFilter = Control.MouseFilterEnum.Ignore });

		string[] lines = {
			Loc.Tr("about.dev"),
			Loc.Tr("about.qq"),
			Loc.Tr("about.email"),
			Loc.Tr("about.group"),
			Loc.Tr("about.github"),
			Loc.Tr("about.ai_credit"),
		};
		float ly = 44;
		float editW = pw - 80;
		float editX = (pw - editW) / 2;
		foreach (var line in lines)
		{
			var lbl = new RichTextLabel { Position = new(editX, ly), Size = new(editW, 24), BbcodeEnabled = true };
			lbl.Text = "[center]" + line + "[/center]";
			lbl.AddThemeFontSizeOverride("font_size", 14);
			lbl.AddThemeColorOverride("default_color", new Color(0, 0, 0));
			lbl.AddThemeColorOverride("selection_color", new Color(0.6f, 0.7f, 0.9f, 0.3f));
			lbl.SelectionEnabled = true;
			lbl.FocusMode = Control.FocusModeEnum.Click;
			lbl.MouseFilter = Control.MouseFilterEnum.Stop;
			var focusStyle = new StyleBoxFlat { BgColor = new Color(1, 1, 1, 0.05f), BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0, 0, 0, 0.3f) };
			lbl.AddThemeStyleboxOverride("focus", focusStyle);
			_aboutPanel.AddChild(lbl);
			ly += 34;
		}

		ly += 12;
		var tip = new Label { Text = Loc.Tr("about.copy_tip"), Position = new(pw / 2 - 130, ly), Size = new(260, 18) };
		tip.AddThemeFontSizeOverride("font_size", 10);
		tip.AddThemeColorOverride("font_color", new Color(0.4f, 0.5f, 0.6f));
		tip.HorizontalAlignment = HorizontalAlignment.Center;
		_aboutPanel.AddChild(tip);

		var devotion = new Label { Text = Loc.Tr("about.devotion"), Position = new(20, ly + 24), Size = new(pw - 40, 130) };
		devotion.AddThemeFontSizeOverride("font_size", 9);
		devotion.AddThemeColorOverride("font_color", new Color(0.40f, 0.45f, 0.50f));
		devotion.HorizontalAlignment = HorizontalAlignment.Center;
		devotion.AutowrapMode = TextServer.AutowrapMode.Word;
		_aboutPanel.AddChild(devotion);

		_aboutPanel.AddChild(MkLabel("Godot 4.7 Mono  |  C# .NET 8.0", pw / 2 - 150, ph - 78, 300, 18, 10, new Color(0.20f, 0.25f, 0.32f), HorizontalAlignment.Center));
		_aboutPanel.AddChild(MkLabel(Loc.Tr("menu.esc_close"), pw / 2 - 100, ph - 56, 200, 16, 9, new Color(0.25f, 0.30f, 0.36f), HorizontalAlignment.Center));

		var closeBtn = new Button { Text = Loc.Tr("menu.close"), Position = new(pw / 2 - 50, ph - 36), Size = new(100, 28), CustomMinimumSize = new(100, 28), Flat = true };
		closeBtn.AddThemeFontSizeOverride("font_size", 12);
		closeBtn.AddThemeColorOverride("font_color", new Color(0.15f, 0.18f, 0.22f));
		closeBtn.AddThemeColorOverride("font_hover_color", new Color(0.40f, 0.40f, 0.40f));
		closeBtn.AddThemeStyleboxOverride("normal", MakeStyle(new(0.90f, 0.89f, 0.86f), new(0.45f, 0.5f, 0.55f)));
		closeBtn.AddThemeStyleboxOverride("hover", MakeStyle(new(0.80f, 0.79f, 0.76f), new(0.3f, 0.4f, 0.55f)));
		closeBtn.Pressed += () => _aboutPanel.Visible = false;
		_aboutPanel.AddChild(closeBtn);
	}

	private void ApplyDisplayMode(int idx)
	{
		_modeIdx = idx;
		GlobalSettings.DisplayMode = idx;
		if (idx == 2)
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		else if (idx == 1)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
			DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
		}
		else
		{
			DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		}
		ApplyResolution(_resIdx);
	}

	private void ApplyResolution(int idx)
	{
		_resIdx = idx;
		GlobalSettings.Resolution = idx;
		var res = GlobalSettings.Resolutions[idx];
		DisplayServer.WindowSetSize(res);
		var screen = DisplayServer.ScreenGetSize();
		DisplayServer.WindowSetPosition(new Vector2I((screen.X - res.X) / 2, (screen.Y - res.Y) / 2));
	}

	private void OnNewGame() { GlobalSettings.NewGame = true; GlobalSettings.LoadGame = false; GetTree().ChangeSceneToFile("res://scenes/main.tscn"); }

	/// <summary>找到最近修改的存档文件（含自动存档），返回路径</summary>
	private static string FindLatestSave()
	{
		string bestPath = null;
		ulong bestTime = 0;

		void Check(string path)
		{
			if (!FileAccess.FileExists(path)) return;
			ulong t = FileAccess.GetModifiedTime(path);
			if (t > bestTime) { bestTime = t; bestPath = path; }
		}

		for (int s = 1; s <= 5; s++) Check(GlobalSettings.GetSlotPath(s));
		Check(GlobalSettings.GetAutoSavePath());
		return bestPath;
	}

	private void OnContinue()
	{
		string latest = FindLatestSave();
		if (latest == null) { OnNewGame(); return; }
		// 判断是哪个槽位
		if (latest == GlobalSettings.GetAutoSavePath())
			_ = 0; // 自动存档 —— SaveSlot 不重要，LoadGame 会直接读 latest
		else for (int s = 1; s <= 5; s++)
			if (latest == GlobalSettings.GetSlotPath(s)) { GlobalSettings.SaveSlot = s; break; }
		// 通过 CustomSavePath 传路径给 GameManager
		GlobalSettings.LoadGame = true; GlobalSettings.NewGame = false;
		// 把路径存到临时变量让 GameManager 知道读哪个文件
		_loadPathOverride = latest;
		GetTree().ChangeSceneToFile("res://scenes/main.tscn");
	}

	private static string _loadPathOverride;

	/// <summary>GameManager 调用此方法获取「继续」或「读取」指定的存档路径</summary>
	public static string GetLoadPathOverride()
	{
		var p = _loadPathOverride;
		_loadPathOverride = null;
		return p;
	}

	private void OnLoadGame()
	{
		if (_loadPanel != null) _loadPanel.Visible = true;
	}

	private void BuildLoadPanel(float cx, float cy)
	{
		float pw = 480, ph = 480;
		_loadPanel = MkPanel(cx - pw / 2, cy - ph / 2 - 10, pw, ph);
		_loadPanel.Visible = false;
		_ui.AddChild(_loadPanel);

		_loadPanel.AddChild(MkLabel(Loc.Tr("ui.load_title"), pw / 2 - 50, 8, 100, 24, 16, new Color(0.10f, 0.14f, 0.22f), HorizontalAlignment.Center));
		_loadPanel.AddChild(new ColorRect { Position = new(20, 36), Size = new(pw - 40, 1), Color = new Color(0.70f, 0.72f, 0.75f, 0.25f), MouseFilter = Control.MouseFilterEnum.Ignore });

		float rowY = 48;
		// 自动存档槽位
		BuildSlotRow(Loc.Tr("set.autosave_slot"), GlobalSettings.GetAutoSavePath(), ref rowY);

		// 手动存档槽位 1~5
		for (int s = 1; s <= 5; s++)
		{
			string spath = GlobalSettings.GetSlotPath(s);
			string customName = GlobalSettings.GetSaveSlotName(s);
			string label = customName != Loc.TrF("save.slot_fmt", s) ? customName : Loc.TrF("save.slot_fmt", s);
			BuildSlotRow(label, spath, ref rowY);
		}

		// 关闭按钮
		var closeBtn = new Button { Text = Loc.Tr("menu.close"), Position = new(pw / 2 - 50, rowY + 8), Size = new(100, 32), CustomMinimumSize = new(100, 32), Flat = true };
		closeBtn.AddThemeFontSizeOverride("font_size", 12);
		closeBtn.AddThemeColorOverride("font_color", new Color(0.15f, 0.18f, 0.22f));
		closeBtn.AddThemeColorOverride("font_hover_color", new Color(0.40f, 0.40f, 0.40f));
		closeBtn.AddThemeStyleboxOverride("normal", MakeStyle(new(0.90f, 0.89f, 0.86f), new(0.45f, 0.5f, 0.55f)));
		closeBtn.AddThemeStyleboxOverride("hover", MakeStyle(new(0.80f, 0.79f, 0.76f), new(0.3f, 0.4f, 0.55f)));
		closeBtn.Pressed += () => _loadPanel.Visible = false;
		_loadPanel.AddChild(closeBtn);
	}

	private void BuildSlotRow(string label, string path, ref float rowY)
	{
		const float leftX = 40;
		bool exists = FileAccess.FileExists(path);

		var lbl = new Label { Position = new(leftX, rowY + 2), Size = new(100, 22) };
		lbl.AddThemeFontSizeOverride("font_size", 11);
		lbl.AddThemeColorOverride("font_color", exists ? new Color(0.08f, 0.12f, 0.18f) : new Color(0.55f, 0.55f, 0.55f));
		lbl.ClipText = true;
		lbl.Text = exists ? label : label + "  " + Loc.Tr("save.empty");
		_loadPanel.AddChild(lbl);

		if (exists)
		{
			// 摘要信息
			string summary = ReadSaveSlotSummary(path);
			var sumLabel = new Label { Text = summary, Position = new(leftX + 140, rowY + 2), Size = new(240, 22) };
			sumLabel.AddThemeFontSizeOverride("font_size", 10);
			sumLabel.AddThemeColorOverride("font_color", new Color(0.25f, 0.3f, 0.4f));
			sumLabel.ClipText = true;
			_loadPanel.AddChild(sumLabel);

			// 重命名按钮
				var renameBtn = new Button { Text = "✎", Position = new(leftX + 105, rowY + 1), Size = new(20, 22), Flat = true };
			renameBtn.AddThemeFontSizeOverride("font_size", 10);
			renameBtn.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
			renameBtn.AddThemeColorOverride("font_hover_color", new Color(0.35f, 0.35f, 0.35f));
			string capPath = path;
			renameBtn.Pressed += () => { ShowRenameSlotDialog(capPath); RebuildLoadPanel(); };
			_loadPanel.AddChild(renameBtn);

			var loadBtn = new Button { Text = Loc.Tr("ui.load_btn"), Position = new(leftX + 390, rowY), Size = new(55, 24), Flat = true };
			loadBtn.AddThemeFontSizeOverride("font_size", 10);
			loadBtn.AddThemeColorOverride("font_color", new Color(0.12f, 0.16f, 0.22f));
			loadBtn.AddThemeColorOverride("font_hover_color", new Color(0.35f, 0.35f, 0.35f));
			loadBtn.AddThemeStyleboxOverride("normal", MakeStyle(new(0.88f, 0.87f, 0.84f), new(0.4f, 0.5f, 0.55f)));
			loadBtn.AddThemeStyleboxOverride("hover", MakeStyle(new(0.78f, 0.77f, 0.74f), new(0.25f, 0.4f, 0.55f)));
			string capturedPath = path;
			loadBtn.Pressed += () =>
			{
				_loadPathOverride = capturedPath;
				// 判断槽位
				if (capturedPath == GlobalSettings.GetAutoSavePath()) { }
				else for (int s = 1; s <= 5; s++)
					if (capturedPath == GlobalSettings.GetSlotPath(s)) { GlobalSettings.SaveSlot = s; break; }
				GlobalSettings.LoadGame = true; GlobalSettings.NewGame = false;
				GetTree().ChangeSceneToFile("res://scenes/main.tscn");
			};
			_loadPanel.AddChild(loadBtn);
		}

		_loadPanel.AddChild(new ColorRect { Position = new(20, rowY + 28), Size = new(pw() - 40, 1), Color = new Color(0.70f, 0.72f, 0.75f, 0.15f), MouseFilter = Control.MouseFilterEnum.Ignore });
		rowY += 32;
		float pw() => _loadPanel.Size.X;
	}

	private void ShowRenameSlotDialog(string slotPath)
	{
		int slot = 0;
		if (slotPath == GlobalSettings.GetAutoSavePath()) { return; }
		for (int s = 1; s <= 5; s++) { if (slotPath == GlobalSettings.GetSlotPath(s)) { slot = s; break; } }
		if (slot == 0) return;
		var vp = GetViewport().GetVisibleRect().Size;
		float pw = 300, ph = 140;
		var dlg = new Panel { Position = new((vp.X - pw) / 2, (vp.Y - ph) / 2), Size = new(pw, ph) };
		dlg.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = Colors.White, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.3f, 0.3f, 0.3f, 0.3f) });
		dlg.MouseFilter = Control.MouseFilterEnum.Stop;
		var overlay = new ColorRect { Color = new Color(0, 0, 0, 0) };
		overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		overlay.MouseFilter = Control.MouseFilterEnum.Stop;
		_ui.AddChild(overlay);
		_ui.AddChild(dlg);
		var inp = new LineEdit { Text = GlobalSettings.GetSaveSlotName(slot), Position = new(20, 30), Size = new(260, 28) };
		inp.AddThemeFontSizeOverride("font_size", 12);
		dlg.AddChild(inp);
		var okBtn = new Button { Text = "确定", Position = new(150, 75), Size = new(60, 30) };
		var cancelBtn = new Button { Text = "取消", Position = new(85, 75), Size = new(60, 30) };
		okBtn.AddThemeFontSizeOverride("font_size", 12);
		cancelBtn.AddThemeFontSizeOverride("font_size", 12);
		System.Action close = () => { dlg.QueueFree(); overlay.QueueFree(); };
		okBtn.Pressed += () => { GlobalSettings.SetSaveSlotName(slot, inp.Text); GlobalSettings.Save(); close(); RebuildLoadPanel(); };
		cancelBtn.Pressed += () => close();
		dlg.AddChild(okBtn);
		dlg.AddChild(cancelBtn);
	}

	private void RebuildLoadPanel()
	{
		_loadPanel.Visible = false;
		_loadPanel.QueueFree();
		var cx = GetViewport().GetVisibleRect().Size.X / 2;
		var cy = GetViewport().GetVisibleRect().Size.Y / 2;
		BuildLoadPanel(cx, cy);
		_loadPanel.Visible = true;
	}

	/// <summary>读取存档文件的简要摘要（年/月/资金/债务）</summary>
	private static string ReadSaveSlotSummary(string path)
	{
		try
		{
			using var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
			if (f == null) return "?";
			string json = f.GetAsText();
			if (string.IsNullOrEmpty(json)) return "?";
			using var doc = System.Text.Json.JsonDocument.Parse(json);
			var meta = doc.RootElement.GetProperty("meta");
			int month = meta.GetProperty("Month").GetInt32();
			float money = meta.GetProperty("Money").GetSingle();
			float debt = meta.GetProperty("TechDebt").GetSingle();
			int y = month / 12 + 1;
			int m = month % 12 + 1;
			return Loc.TrF("save.entry_fmt", y, m, money, debt);
		}
		catch { return Loc.Tr("save.corrupt"); }
	}

	private void OnSettings() { if (_settingsPanel != null) _settingsPanel.Visible = true; }
	private void OnAbout() { if (_aboutPanel != null) _aboutPanel.Visible = true; }

	// ══════════════════ Mod 列表 ══════════════════

	private void ShowModList()
	{
		float pw = 480, ph = 500;
		var dp = new DragPanel { Position = new((GetViewport().GetVisibleRect().Size.X - pw) / 2, (GetViewport().GetVisibleRect().Size.Y - ph) / 2), Size = new(pw, ph) };
		dp.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = Colors.White, CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 });
		_ui.AddChild(dp);

		var title = LUI.Label(Loc.Tr("mod.title"), 16, new Color(0.10f, 0.14f, 0.22f));
		title.Position = new(20, 10);
		dp.AddChild(title);

		var closeBtn = new Button { Text = "✕", Position = new(pw - 40, 8), Size = new(30, 28), Flat = true };
		closeBtn.AddThemeFontSizeOverride("font_size", 16);
		closeBtn.AddThemeColorOverride("font_color", new Color(0.8f, 0.3f, 0.3f));
		closeBtn.Pressed += () => dp.QueueFree();
		dp.AddChild(closeBtn);

		// 主菜单也加载 Mod 列表
		if (ModManager.LoadedMods.Count == 0) ModManager.Init();
		float y = 50;
		var mods = ModManager.LoadedMods;
		if (mods.Count == 0)
		{
			var empty = LUI.Label(Loc.Tr("mod.no_mods"), 12, new Color(0.4f, 0.45f, 0.55f));
			empty.Position = new(20, y);
			empty.Size = new(pw - 40, 30);
			dp.AddChild(empty);
		}
		else
		{
			foreach (var m in mods)
			{
				float rowH = 36;
				var typeStr = m.IsLanguage ? Loc.Tr("mod.lang") : m.HasScripts ? Loc.Tr("mod.script") : Loc.Tr("mod.data");
				bool enabled = ModManager.IsEnabled(m);

				float nameH = rowH;
				var nameLbl = LUI.Label($"{m.Name} ({typeStr})", 12, enabled ? new Color(0.10f, 0.14f, 0.22f) : new Color(0.5f, 0.5f, 0.5f));
				nameLbl.Position = new(20, y + 2);
				nameLbl.Size = new(300, rowH);
				dp.AddChild(nameLbl);

				// 依赖/冲突摘要
				var infoParts = new System.Collections.Generic.List<string>();
				if (m.Dependencies.Count > 0) infoParts.Add($"↓{string.Join(",", m.Dependencies)}");
				if (m.OptionalDependencies.Count > 0) infoParts.Add($"?{string.Join(",", m.OptionalDependencies)}");
				if (m.Conflicts.Count > 0) infoParts.Add($"✗{string.Join(",", m.Conflicts)}");
				if (infoParts.Count > 0)
				{
					var depLbl = LUI.Label(string.Join(" ", infoParts), 8, new Color(0.4f, 0.45f, 0.55f));
					depLbl.Position = new(20, y + 16);
					depLbl.Size = new(380, 14);
					dp.AddChild(depLbl);
					nameH = 50;
				}

				// 加载错误提示
				var modErrors = ModManager.LoadErrors.FindAll(e => e.Contains($"[{m.Name}]"));
				if (modErrors.Count > 0)
				{
					var errLbl = LUI.Label(modErrors[0], 8, new Color(0.9f, 0.3f, 0.2f));
					errLbl.Position = new(20, y + (infoParts.Count > 0 ? 30 : 16));
					errLbl.Size = new(380, 14);
					dp.AddChild(errLbl);
					nameH = 64;
				}

				var toggle = new CheckBox { ButtonPressed = enabled, Position = new(pw - 60, y + 4) };
				var mRef = m;
				toggle.Toggled += (b) =>
				{
					if (b && mRef.HasScripts && ModManager.NeedsRiskConfirm(mRef))
					{
						// 风险确认
						ShowModRiskConfirm(mRef, () => { ModManager.SetEnabled(mRef, true); nameLbl.AddThemeColorOverride("font_color", new Color(0.10f, 0.14f, 0.22f)); }, () => toggle.ButtonPressed = false);
						return;
					}
					ModManager.SetEnabled(mRef, b);
					if (b) nameLbl.AddThemeColorOverride("font_color", new Color(0.10f, 0.14f, 0.22f));
					else nameLbl.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
				};
				dp.AddChild(toggle);

				if (!string.IsNullOrEmpty(m.Description))
				{
					var desc = LUI.Label(m.Description, 9, new Color(0.4f, 0.45f, 0.55f));
					desc.Position = new(20, y + (nameH > 36 ? nameH - 14 : 18));
					desc.Size = new(pw - 80, 14);
					dp.AddChild(desc);
					nameH = Mathf.Max(nameH, 48);
				}

				y += nameH + 4;
			}
		}

		// 关闭按钮底部
		var botClose = new Button { Text = Loc.Tr("set.cancel"), Flat = true, Position = new(pw / 2 - 50, Mathf.Max(y + 20, ph - 50)), Size = new(100, 32) };
		botClose.AddThemeFontSizeOverride("font_size", 12);
		botClose.AddThemeColorOverride("font_color", Colors.Black);
		botClose.Pressed += () => dp.QueueFree();
		dp.AddChild(botClose);
	}

	private void ShowModRiskConfirm(ModManifest mod, Action onAccept, Action onDecline)
	{
		var vp = GetViewport().GetVisibleRect().Size;
		var dp = new DragPanel { Position = new(vp.X / 2 - 220, vp.Y / 2 - 100), Size = new(440, 200) };
		dp.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(1f, 0.95f, 0.90f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8, BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, BorderColor = new Color(0.9f, 0.3f, 0.2f, 0.6f) });
		_ui.AddChild(dp);

		var title = LUI.Label(Loc.Tr("mod.risk_title"), 16, new Color(0.8f, 0.2f, 0.2f));
		title.Position = new(20, 12);
		dp.AddChild(title);

		var desc = LUI.Label(Loc.TrF("mod.risk_desc", mod.Name, mod.Author), 11, new Color(0.2f, 0.2f, 0.2f));
		desc.Position = new(20, 42);
		desc.Size = new(400, 80);
		desc.AutowrapMode = TextServer.AutowrapMode.Word;
		dp.AddChild(desc);

		var acceptBtn = new Button { Text = Loc.Tr("mod.risk_accept"), Position = new(20, 150), Size = new(200, 34) };
		acceptBtn.AddThemeFontSizeOverride("font_size", 12);
		acceptBtn.AddThemeColorOverride("font_color", new Color(0.9f, 0.2f, 0.2f));
		acceptBtn.Pressed += () => { ModManager.ConfirmedRiskyMods.Add(mod.Id); dp.QueueFree(); onAccept(); };
		dp.AddChild(acceptBtn);

		var declineBtn = new Button { Text = Loc.Tr("mod.risk_decline"), Position = new(240, 150), Size = new(140, 34), Flat = true };
		declineBtn.AddThemeFontSizeOverride("font_size", 12);
		declineBtn.AddThemeColorOverride("font_color", Colors.Black);
		declineBtn.Pressed += () => { dp.QueueFree(); onDecline(); };
		dp.AddChild(declineBtn);
	}

	private static AudioStream LoadWavFallback(string path)
	{
		if (!FileAccess.FileExists(path)) return null;
		var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (f == null) return null;
		int len = (int)f.GetLength();
		var buf = f.GetBuffer(len);
		f.Close();
		if (len < 44) return null;

		int sr = buf[24] | (buf[25] << 8) | (buf[26] << 16) | (buf[27] << 24);
		int ch = buf[22] | (buf[23] << 8);
		int bits = buf[34] | (buf[35] << 8);
		int pos = 12, dSize = 0;
		while (pos < len - 8)
		{
			int csz = buf[pos+4] | (buf[pos+5] << 8) | (buf[pos+6] << 16) | (buf[pos+7] << 24);
			if (buf[pos]=='d' && buf[pos+1]=='a' && buf[pos+2]=='t' && buf[pos+3]=='a') { dSize = csz; break; }
			pos += 8 + csz;
		}
		pos += 8;
		if (dSize <= 0 || pos >= len) return null;
		int pcmLen = Mathf.Min(dSize, len - pos);
		var pcm = new byte[pcmLen];
		System.Buffer.BlockCopy(buf, pos, pcm, 0, pcmLen);

		return new AudioStreamWav
		{
			Data = pcm, LoopMode = AudioStreamWav.LoopModeEnum.Disabled,
			Format = bits == 8 ? AudioStreamWav.FormatEnum.Format8Bits : AudioStreamWav.FormatEnum.Format16Bits,
			MixRate = sr, Stereo = ch >= 2
		};
	}

	private CanvasLayer _menuLogOverlay;
	private void ShowMenuModLog()
	{
		if (_menuLogOverlay != null) { _menuLogOverlay.QueueFree(); _menuLogOverlay = null; return; }
		string text = DlcManager.ReadLog();
		if (string.IsNullOrEmpty(text)) text = "[DlcManager] No log entries yet.";

		_menuLogOverlay = new CanvasLayer { Layer = 128 };
		AddChild(_menuLogOverlay);
		var vp = GetViewport().GetVisibleRect().Size;
		var panel = new Panel { Position = new(40, 40), Size = new(vp.X - 80, vp.Y - 80) };
		panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.08f, 0.08f, 0.12f, 0.95f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 });
		_menuLogOverlay.AddChild(panel);

		var title = new Label { Text = "📋 Mod Log  [F9]", Position = new(20, 12), Size = new(300, 30) };
		title.AddThemeFontSizeOverride("font_size", 16);
		title.AddThemeColorOverride("font_color", new Color(0.8f, 0.9f, 1f));
		panel.AddChild(title);

		float sw = vp.X - 100;
		var scroll = new ScrollContainer { Position = new(10, 50), Size = new(sw, panel.Size.Y - 70) };
		panel.AddChild(scroll);
		var label = new Label();
		label.Text = text;
		label.AutowrapMode = TextServer.AutowrapMode.Arbitrary;
		label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		label.CustomMinimumSize = new(sw - 20, 0);
		label.AddThemeFontSizeOverride("font_size", 11);
		label.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 0.9f));
		scroll.AddChild(label);

		var close = new Button { Text = "✕ Close", Position = new(panel.Size.X - 100, 10), Size = new(80, 28), FocusMode = Control.FocusModeEnum.None };
		close.AddThemeFontSizeOverride("font_size", 12);
		close.Pressed += () => { _menuLogOverlay?.QueueFree(); _menuLogOverlay = null; };
		panel.AddChild(close);
	}
}
