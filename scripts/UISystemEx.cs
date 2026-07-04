using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class UISystemEx : Node
{
    private GameManager _gm => Services.GameManager;
    private Control _uiLayer;
    private Panel _tooltipPanel;
    private Label _tooltipLabel;
    private ColorRect _encyOverlay; // 百科缓存，避免每次重建

    public void InitUI(Control uiLayer)
    {
        _uiLayer = uiLayer;
        CreateToolbar();
    }

    public void ShowEncyclopedia()
    {
        if (_encyOverlay != null && GodotObject.IsInstanceValid(_encyOverlay))
        {
            _encyOverlay.Visible = !_encyOverlay.Visible;
            _gm.IsAnyModalOpen = _encyOverlay.Visible;
            return;
        }
        var encyMgr = Services.GameManager?.GetNodeOrNull<EncyclopediaManager>("EncyclopediaManager");
        if (encyMgr == null) { _gm.ShowPopup("提示", "百科数据未加载，请先构建项目", Colors.Gray); return; }

        _gm.IsAnyModalOpen = true;
        _encyOverlay = new ColorRect { Color = new Color(0, 0, 0, 0.6f) };
        _encyOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _encyOverlay.MouseFilter = Control.MouseFilterEnum.Stop;
        _encyOverlay.TreeExiting += () => { _gm.IsAnyModalOpen = false; _encyOverlay = null; };

        var panel = new Panel();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        panel.Size = new Vector2(800, 580);
        panel.Position -= panel.Size / 2;

        var title = new Label { Text = "\U0001f4d6 " + Loc.Tr("encyclopedia.title"), Position = new Vector2(16, 10), Size = new Vector2(400, 30) };
        title.AddThemeFontSizeOverride("font_size", 20);
        panel.AddChild(title);

        var closeBtn = new Button { Text = "\u2715", Position = new Vector2(770, 4), Size = new Vector2(24, 24) };
        closeBtn.Pressed += () => { _encyOverlay.Visible = false; _gm.IsAnyModalOpen = false; };
        panel.AddChild(closeBtn);

        var searchBox = new LineEdit { PlaceholderText = Loc.Tr("encyclopedia.search"), Position = new Vector2(440, 10), Size = new Vector2(330, 28) };
        panel.AddChild(searchBox);

        var leftList = new ItemList { Position = new Vector2(10, 46), Size = new Vector2(190, 490), SelectMode = ItemList.SelectModeEnum.Single };
        var backBtn = new Button { Text = Loc.Tr("encyclopedia.back"), Position = new Vector2(10, 540), Size = new Vector2(190, 28), Visible = false };
        panel.AddChild(backBtn);
        panel.AddChild(leftList);

        var rightBg = new Panel { Position = new Vector2(210, 46), Size = new Vector2(580, 490) };
        panel.AddChild(rightBg);

        var rightTitle = new Label { Position = new Vector2(218, 52), Size = new Vector2(564, 26) };
        rightTitle.AddThemeFontSizeOverride("font_size", 16);
        panel.AddChild(rightTitle);

        var rightContent = new RichTextLabel { Position = new Vector2(218, 80), Size = new Vector2(564, 432), BbcodeEnabled = true };
        rightContent.ScrollActive = true;
        panel.AddChild(rightContent);

        var statusLabel = new Label { Position = new Vector2(218, 514), Size = new Vector2(564, 18) };
        statusLabel.AddThemeFontSizeOverride("font_size", 10);
        panel.AddChild(statusLabel);

        var categories = new List<(string key, string label)>
        {
            ("genre", Loc.Tr("encyclopedia.genre")),
            ("theme", Loc.Tr("encyclopedia.theme")),
            ("tech", Loc.Tr("encyclopedia.tech")),
            ("mechanics", Loc.Tr("encyclopedia.mechanics"))
        };

        string currentCategory = "";
        List<(string id, string name)> currentItemList = new();

        void ShowMainCategories()
        {
            backBtn.Visible = false;
            rightTitle.Text = "";
            rightContent.Text = "";
            statusLabel.Text = "";
            string text = "[center][b]" + Loc.Tr("encyclopedia.welcome") + "[/b][/center]\n\n";
            text += Loc.Tr("encyclopedia.welcome_desc") + "\n\n";
            foreach (var cat in categories)
                text += "[b]" + cat.label + "[/b]\n  " + GetCategoryDesc(cat.key) + "\n\n";
            rightContent.Text = text;
            currentCategory = "";
        }

        void ShowGenreList()
        {
            currentCategory = "genre";
            backBtn.Visible = true;
            leftList.Clear();
            var genres = encyMgr.GetAllGenres();
            currentItemList = genres.Select(g => (g.id, g.name)).ToList();
            foreach (var g in genres)
                leftList.AddItem(g.name);
            rightTitle.Text = "";
            rightContent.Text = Loc.Tr("encyclopedia.select_item");
            statusLabel.Text = "";
        }

        void ShowThemeList()
        {
            currentCategory = "theme";
            backBtn.Visible = true;
            leftList.Clear();
            var themes = encyMgr.GetAllThemes();
            currentItemList = themes.Select(t => (t.id, t.name)).ToList();
            foreach (var t in themes)
                leftList.AddItem(t.name);
            rightTitle.Text = "";
            rightContent.Text = Loc.Tr("encyclopedia.select_item");
            statusLabel.Text = "";
        }

        void ShowTechList()
        {
            currentCategory = "tech";
            backBtn.Visible = true;
            leftList.Clear();
            currentItemList = new();
            foreach (var branchKey in new[] { "ProgramBase", "Render2D", "Render3D", "Audio", "Network", "AI", "Platform", "GenreUnlock", "ThemeUnlock" })
            {
                var techs = encyMgr.GetTechByBranch(branchKey);
                if (techs.Count > 0)
                {
                    string branchName = encyMgr.GetBranchName(branchKey);
                    leftList.AddItem("[ " + branchName + " ]");
                    leftList.SetItemDisabled(leftList.ItemCount - 1, true);
                    currentItemList.Add(("__branch__", branchName));
                    foreach (var t in techs)
                    {
                        leftList.AddItem("  Lv" + t.level + " " + t.name);
                        currentItemList.Add((t.id, t.name));
                    }
                }
            }
            rightTitle.Text = "";
            rightContent.Text = Loc.Tr("encyclopedia.select_item");
            statusLabel.Text = "";
        }

        void ShowMechanicsList()
        {
            currentCategory = "mechanics";
            backBtn.Visible = true;
            leftList.Clear();
            var mech = encyMgr.GetMechanics();
            currentItemList = new();
            if (mech?.Items != null)
            {
                foreach (var kv in mech.Items)
                {
                    leftList.AddItem(kv.Value.title);
                    currentItemList.Add((kv.Key, kv.Value.title));
                }
            }
            rightTitle.Text = "";
            rightContent.Text = Loc.Tr("encyclopedia.select_item");
            statusLabel.Text = "";
        }

        void ShowGenreDetail(string genreId)
        {
            var info = encyMgr.GetGenreInfo(genreId);
            if (info == null) return;
            rightTitle.Text = info.name;
            string text = "";
            text += "[b]" + Loc.Tr("encyclopedia.desc") + "[/b]\n" + info.description + "\n\n";
            if (info.base_score_bonus != 0)
                text += "[b]" + Loc.Tr("encyclopedia.score_bonus") + "[/b] +" + info.base_score_bonus + "\n\n";
            text += "[b]" + Loc.Tr("encyclopedia.tips") + "[/b]\n" + info.tips;
            rightContent.Text = text;
            statusLabel.Text = "ID: " + info.id;
        }

        void ShowThemeDetail(string themeId)
        {
            var info = encyMgr.GetThemeInfo(themeId);
            if (info == null) return;
            rightTitle.Text = info.name;
            string text = "";
            text += "[b]" + Loc.Tr("encyclopedia.desc") + "[/b]\n" + info.description + "\n\n";
            text += "[b]" + Loc.Tr("encyclopedia.compat_genres") + "[/b]\n";
            var allGenres = encyMgr.GetAllGenres();
            var compatNames = info.compatible_genres.Select(gid =>
            {
                var g = allGenres.FirstOrDefault(gr => gr.id == gid);
                return g != null ? g.name : gid;
            });
            text += string.Join("\u3001", compatNames);
            rightContent.Text = text;
            statusLabel.Text = "ID: " + info.id;
        }

        void ShowTechDetail(string techId)
        {
            var info = encyMgr.GetTechInfo(techId);
            if (info == null) return;
            rightTitle.Text = info.name;
            string text = "";
            text += "[b]" + Loc.Tr("encyclopedia.branch") + "[/b] " + encyMgr.GetBranchName(info.branch) + "\n";
            text += "[b]" + Loc.Tr("encyclopedia.level") + "[/b] " + info.level + "\n";
            text += "[b]" + Loc.Tr("encyclopedia.required_months") + "[/b] " + info.required_months + " " + Loc.Tr("encyclopedia.months") + "\n";
            if (!string.IsNullOrEmpty(info.prerequisites))
                text += "[b]" + Loc.Tr("encyclopedia.prereq") + "[/b] " + info.prerequisites + "\n";
            text += "[b]" + Loc.Tr("encyclopedia.primary_skill") + "[/b] " + info.primary_skill + " Lv" + info.primary_skill_level + "\n";
            if (!string.IsNullOrEmpty(info.secondary_skill))
                text += "[b]" + Loc.Tr("encyclopedia.secondary_skill") + "[/b] " + info.secondary_skill + " Lv" + info.secondary_skill_level + "\n";
            text += "\n[b]" + Loc.Tr("encyclopedia.desc") + "[/b]\n" + info.description + "\n\n";
            text += "[b]" + Loc.Tr("encyclopedia.effect") + "[/b]\n" + info.effect;
            rightContent.Text = text;
            statusLabel.Text = "ID: " + info.id;
        }

        void ShowMechanicsDetail(string mechId)
        {
            var mech = encyMgr.GetMechanics();
            if (mech?.Items == null) return;
            if (!mech.Items.TryGetValue(mechId, out var cat)) return;
            rightTitle.Text = cat.title;
            string text = "";
            foreach (var sec in cat.sections)
            {
                text += "[b]" + sec.heading + "[/b]\n" + sec.content + "\n\n";
            }
            rightContent.Text = text;
            statusLabel.Text = "";
        }

        void OnLeftItemSelected(int idx)
        {
            if (idx < 0) return;

            if (string.IsNullOrEmpty(currentCategory))
            {
                if (idx >= 0 && idx < categories.Count)
                {
                    string key = categories[idx].key;
                    switch (key)
                    {
                        case "genre": ShowGenreList(); break;
                        case "theme": ShowThemeList(); break;
                        case "tech": ShowTechList(); break;
                        case "mechanics": ShowMechanicsList(); break;
                    }
                }
                return;
            }

            switch (currentCategory)
            {
                case "genre":
                    if (idx >= 0 && idx < currentItemList.Count)
                        ShowGenreDetail(currentItemList[idx].id);
                    break;
                case "theme":
                    if (idx >= 0 && idx < currentItemList.Count)
                        ShowThemeDetail(currentItemList[idx].id);
                    break;
                case "tech":
                    if (idx >= 0 && idx < currentItemList.Count)
                    {
                        var item = currentItemList[idx];
                        if (!item.id.StartsWith("__"))
                            ShowTechDetail(item.id);
                    }
                    break;
                case "mechanics":
                    if (idx >= 0 && idx < currentItemList.Count)
                        ShowMechanicsDetail(currentItemList[idx].id);
                    break;
                case "search":
                    if (idx >= 0 && idx < currentItemList.Count)
                        ShowSearchResult(currentItemList[idx].id);
                    break;
            }
        }

        void ShowSearchResult(string itemId)
        {
            var genre = encyMgr.GetGenreInfo(itemId);
            if (genre != null) { ShowGenreDetail(itemId); return; }
            var theme = encyMgr.GetThemeInfo(itemId);
            if (theme != null) { ShowThemeDetail(itemId); return; }
            var tech = encyMgr.GetTechInfo(itemId);
            if (tech != null) { ShowTechDetail(itemId); return; }
            // Show mechanics category directly
            currentCategory = "mechanics";
            ShowMechanicsDetail(itemId);
        }

        searchBox.TextChanged += (text) =>
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                ShowMainCategories();
                leftList.Clear();
                foreach (var cat in categories)
                    leftList.AddItem(cat.label);
                currentCategory = "";
                return;
            }
            var results = encyMgr.Search(text);
            leftList.Clear();
            currentItemList.Clear();
            currentCategory = "search";
            if (results.Count == 0)
            {
                leftList.AddItem(Loc.Tr("encyclopedia.no_results"));
                rightTitle.Text = "";
                rightContent.Text = "";
                statusLabel.Text = "";
                return;
            }
            foreach (var r in results)
            {
                var dict = (System.Collections.IDictionary)r;
                string type = dict["type"]?.ToString() ?? "";
                string id = dict["id"]?.ToString() ?? "";
                string name = dict["name"]?.ToString() ?? "";
                string prefix = type switch { "genre" => "", "theme" => "", "tech" => "", _ => "" };
                leftList.AddItem(prefix + name);
                currentItemList.Add((id, name));
            }
            leftList.Select(0);
            rightTitle.Text = "";
            rightContent.Text = Loc.Tr("encyclopedia.select_item");
            statusLabel.Text = "";
        };

        backBtn.Pressed += () =>
        {
            ShowMainCategories();
            leftList.Clear();
            foreach (var cat in categories)
                leftList.AddItem(cat.label);
            currentCategory = "";
        };

        leftList.ItemSelected += (idx) => OnLeftItemSelected((int)idx);

        foreach (var cat in categories)
            leftList.AddItem(cat.label);
        ShowMainCategories();

        _encyOverlay.AddChild(panel);
        _uiLayer.AddChild(_encyOverlay);
    }

    private string GetCategoryDesc(string key)
    {
        return key switch
        {
            "genre" => Loc.Tr("encyclopedia.genre_desc"),
            "theme" => Loc.Tr("encyclopedia.theme_desc"),
            "tech" => Loc.Tr("encyclopedia.tech_desc"),
            "mechanics" => Loc.Tr("encyclopedia.mechanics_desc"),
            _ => ""
        };
    }

    // ── 成就馆 ──
    public void ShowAchievementGallery()
    {
        var achMgr = Services.AchievementManager;
        var canvas = new CanvasLayer { Layer = 128 };
        var overlay = new ColorRect { Color = new Color(0, 0, 0, 0.6f) };
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        overlay.MouseFilter = Control.MouseFilterEnum.Stop;
        _gm.IsAnyModalOpen = true;
        overlay.TreeExiting += () => _gm.IsAnyModalOpen = false;
        canvas.AddChild(overlay);

        var panel = new Panel();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        panel.Size = new Vector2(850, 560);
        panel.Position -= panel.Size / 2;

        var title = new Label { Text = "\U0001f3c6 \u6210\u5c31\u9986", Position = new Vector2(20, 12), Size = new Vector2(400, 32) };
        title.AddThemeFontSizeOverride("font_size", 22);
        panel.AddChild(title);

        var (unlocked, total) = achMgr.GetProgress();
        var info = new Label { Text = $"\u5df2\u89e3\u9501: {unlocked}/{total}", Position = new Vector2(20, 48), Size = new Vector2(200, 20) };
        info.AddThemeFontSizeOverride("font_size", 13);
        info.AddThemeColorOverride("font_color", new Color(0.8f, 0.9f, 1f));
        panel.AddChild(info);

        // Legend
        string[] legendLabels = { "\u25a0 \u666e\u901a", "\u25a0 \u7a00\u6709", "\u25a0 \u5f69\u86cb", "\u25a0 \u5f69\u86cb\u7a00\u6709" };
        Color[] legendColors = { new Color(1,1,1), new Color(0.7f,0.4f,1f), new Color(0.3f,0.9f,0.3f), new Color(1f,0.85f,0f) };
        float lx = 420;
        for (int li = 0; li < legendLabels.Length; li++)
        {
            var leg = new Label { Text = legendLabels[li], Position = new Vector2(lx, 48), Size = new Vector2(100, 20) };
            leg.AddThemeFontSizeOverride("font_size", 11);
            leg.AddThemeColorOverride("font_color", legendColors[li]);
            panel.AddChild(leg);
            lx += 100;
        }

        var scroll = new ScrollContainer { Position = new Vector2(12, 72), Size = new Vector2(826, 448) };
        panel.AddChild(scroll);

        var grid = new GridContainer { Columns = 2, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(grid);

        foreach (var ach in achMgr.AllAchievements)
        {
            bool isUnlocked = achMgr.Unlocked.Contains(ach.Id);
            bool isEaster = ach.Rarity >= 2;

            var card = new Panel();
            card.CustomMinimumSize = new Vector2(0, 70);
            card.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            card.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = isUnlocked ? new Color(0.12f, 0.12f, 0.16f) : new Color(0.07f, 0.07f, 0.1f),
                BorderWidthLeft = 3,
                BorderWidthTop = 0,
                BorderWidthRight = 0,
                BorderWidthBottom = 0,
                BorderColor = !isUnlocked ? new Color(0.25f, 0.25f, 0.25f)
                    : ach.Rarity == 3 ? new Color(1f, 0.85f, 0f)
                    : ach.Rarity == 2 ? new Color(0.3f, 0.9f, 0.3f)
                    : ach.Rarity == 1 ? new Color(0.7f, 0.4f, 1f)
                    : new Color(0.5f, 0.6f, 0.8f),
                CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
                CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            });

            var vc = new VBoxContainer { Position = new Vector2(10, 6), Size = new Vector2(380, 58) };
            card.AddChild(vc);

            var nameLabel = new Label
            {
                Text = ach.Name,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            nameLabel.AddThemeFontSizeOverride("font_size", 13);
            if (!isUnlocked)
                nameLabel.AddThemeColorOverride("font_color", new Color(0.35f, 0.35f, 0.35f));
            else if (ach.Rarity == 3)
                nameLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0f));
            else if (ach.Rarity == 2)
                nameLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.9f, 0.3f));
            else if (ach.Rarity == 1)
                nameLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.4f, 1f));
            else
                nameLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
            vc.AddChild(nameLabel);

            if (!string.IsNullOrEmpty(ach.Desc))
            {
                var descLabel = new Label
                {
                    Text = (!isUnlocked && isEaster) ? "? ? ? ? ? ? ? ?" : ach.Desc,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                };
                descLabel.AddThemeFontSizeOverride("font_size", 10);
                descLabel.AddThemeColorOverride("font_color", isUnlocked ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.3f, 0.3f, 0.3f));
                vc.AddChild(descLabel);
            }

            grid.AddChild(card);
        }

        var closeBtn = new Button { Text = "\u5173\u95ed", Position = new Vector2(760, 524) };
        closeBtn.Pressed += () => canvas.QueueFree();
        panel.AddChild(closeBtn);
        overlay.AddChild(panel);
        _uiLayer.AddChild(canvas);
    }

    // ── 快捷工具栏（由 GameManager.BuildHUD 直接创建按钮） ──
    public void CreateToolbar() { }

    private void ShowTooltip(Control target, string text)
    {
        if (_tooltipPanel == null)
        {
            _tooltipPanel = new Panel();
            _tooltipPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
            _tooltipLabel = new Label { Position = new Vector2(6, 3) };
            _tooltipLabel.AddThemeFontSizeOverride("font_size", 11);
            _tooltipLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
            _tooltipPanel.AddChild(_tooltipLabel);
            _uiLayer.AddChild(_tooltipPanel);
        }
        _tooltipLabel.Text = text;
        var size = new Vector2(_tooltipLabel.GetMinimumSize().X + 14, _tooltipLabel.GetMinimumSize().Y + 8);
        _tooltipPanel.Size = size;
        var tp = target.GetGlobalMousePosition();
        _tooltipPanel.Position = new Vector2(tp.X + 20, tp.Y - size.Y - 8);
        _tooltipPanel.Visible = true;
    }

    private void HideTooltip()
    {
        if (_tooltipPanel != null) _tooltipPanel.Visible = false;
    }
}

public static class GameSettingsEx
{
    public static bool AmbientEnabled { get; set; } = true;
    public static int NgPlusLevel { get; set; }
}
