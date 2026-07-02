using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static class CardUI
{
    private static GameManager _gm;
    private static CardSystem _cs;
    public static System.Action OnClickSound;
    private static Control _drawer;          // 整个组件（把手+内容一体滑动）
    private static Control _handlePanel;     // 把手
    private static bool _drawerOpen;
    private static Panel _activeStorePanel;
    private static CardDefinition _selectedCard;
    private const float HANDLE_W = 48;

    public static void Init(GameManager gm)
    {
        _gm = gm;
        _cs = gm.CardSys;
    }

    /// <summary>创建关闭状态下的独立把手</summary>
    public static void EnsureHandle()
    {
        if (_handlePanel != null) return;
        var vp = _gm.GetViewport().GetVisibleRect().Size;
        float us = _gm.UIScale;
        float hw = HANDLE_W * us;
        float dh = vp.Y - 80 * us; // 和抽屉内容区同高
        float y0 = 40 * us;

        _handlePanel = new Control { Position = new(vp.X - hw, y0), Size = new(hw, dh) };
        var bg = new Panel();
        bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        bg.MouseFilter = Control.MouseFilterEnum.Ignore;
        var s = new StyleBoxFlat { BgColor = new Color(0.2f, 0.4f, 0.8f, 0.15f), CornerRadiusTopLeft = 6, CornerRadiusBottomLeft = 6, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthBottom = 1, BorderColor = new Color(0.2f, 0.4f, 0.8f, 0.3f) };
        bg.AddThemeStyleboxOverride("panel", s);
        _handlePanel.AddChild(bg);
        var il = new Label { Text = "🃏", Position = new(0, dh * 0.2f), Size = new(hw, 28 * us), HorizontalAlignment = HorizontalAlignment.Center };
        il.AddThemeFontSizeOverride("font_size", (int)(20 * us)); _handlePanel.AddChild(il);
        var tl = new Label { Text = Loc.Tr("card.drawer_title"), Position = new(0, dh * 0.2f + 30 * us), Size = new(hw, 40 * us), HorizontalAlignment = HorizontalAlignment.Center };
        tl.AddThemeFontSizeOverride("font_size", (int)(10 * us)); tl.AddThemeColorOverride("font_color", new Color(0.2f, 0.3f, 0.6f));
        _handlePanel.AddChild(tl);
        var hsS = new StyleBoxFlat { BgColor = new Color(0.3f, 0.5f, 0.8f, 0.25f), CornerRadiusTopLeft = 6, CornerRadiusBottomLeft = 6, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthBottom = 1, BorderColor = new Color(0.3f, 0.5f, 0.8f, 0.5f) };
        _handlePanel.MouseEntered += () => bg.AddThemeStyleboxOverride("panel", hsS);
        _handlePanel.MouseExited += () => bg.AddThemeStyleboxOverride("panel", s);
        _handlePanel.GuiInput += (ie) => { if (ie is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left) { OnClickSound?.Invoke(); ToggleDrawer(); } };
        _gm.UiLayer.AddChild(_handlePanel);
    }

    public static void ShowStore()
    {
        _activeStorePanel?.QueueFree();
        var p = _gm.MakePanel(Loc.Tr("card.store_title"));
        var sc = _gm.AddScroll(p);
        var vb = new VBoxContainer();
        sc.AddChild(vb);

        // Title with refresh info
        var header = new HBoxContainer();
        var titleL = new Label { Text = Loc.Tr("card.store_subtitle") };
        titleL.AddThemeFontSizeOverride("font_size", 12);
        titleL.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.5f));
        header.AddChild(titleL);
        var refreshL = new Label { Text = "  " + Loc.TrF("card.next_refresh", _cs.MonthsUntilShopRefresh) };
        refreshL.AddThemeFontSizeOverride("font_size", 11);
        refreshL.AddThemeColorOverride("font_color", new Color(0.6f, 0.5f, 0.3f));
        header.AddChild(refreshL);
        vb.AddChild(header);

        vb.AddChild(new HSeparator());

        var slotL = new Label { Text = Loc.TrF("card.paid_slots", _cs.PaidSlotUsed, CardSystem.PaidSlotCount) };
        slotL.AddThemeFontSizeOverride("font_size", 11);
        slotL.AddThemeColorOverride("font_color", new Color(0.5f, 0.3f, 0.2f));
        vb.AddChild(slotL);

        // Shop inventory
        foreach (var card in _cs.ShopInventory)
        {
            var row = new HBoxContainer();
            var cardBtn = new Button { Text = $"{card.Icon} {Loc.Tr(card.NameKey)}", Flat = false,
                CustomMinimumSize = new(0, 36), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            cardBtn.AddThemeFontSizeOverride("font_size", 13);
            cardBtn.AddThemeColorOverride("font_color", new Color(0.08f, 0.12f, 0.22f));

            string priceStr = "";
            if (card.PriceMoney > 0) priceStr += $"¥{card.PriceMoney:N0}";
            if (card.PriceInspiration > 0) priceStr += $" {(priceStr.Length > 0 ? "+" : "")}{card.PriceInspiration}💡";
            var priceL = new Label { Text = priceStr, CustomMinimumSize = new(120, 0) };
            priceL.AddThemeFontSizeOverride("font_size", 12);
            priceL.AddThemeColorOverride("font_color", new Color(0.8f, 0.6f, 0.1f));

            var stockL = new Label { Text = Loc.TrF("card.stock", card.Stock), CustomMinimumSize = new(60, 0) };
            stockL.AddThemeFontSizeOverride("font_size", 11);
            stockL.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));

            var descL = new Label { Text = Loc.Tr(card.DescKey), CustomMinimumSize = new(200, 0) };
            descL.AddThemeFontSizeOverride("font_size", 10);
            descL.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.45f));

            cardBtn.Pressed += () => {
                if (_cs.BuyFromShop(card)) {
                    _gm.ShowToast(Loc.Tr("card.bought"), Loc.Tr(card.NameKey), new Color(0.3f, 0.8f, 0.3f));
                    ShowStore(); // refresh
                } else {
                    _gm.ShowToast(Loc.Tr("card.cant_buy"), _cs.PaidSlotUsed >= CardSystem.PaidSlotCount ?
                        Loc.Tr("card.slots_full") : Loc.Tr("card.fail_money"), new Color(0.9f, 0.3f, 0.2f));
                }
            };

            row.AddChild(cardBtn); row.AddChild(priceL); row.AddChild(stockL);
            vb.AddChild(row);
            var descRow = new HBoxContainer();
            descRow.AddChild(descL);
            vb.AddChild(descRow);
            vb.AddChild(new HSeparator());
        }

        if (_cs.ShopInventory.Count == 0)
        {
            var empty = new Label { Text = Loc.Tr("card.shop_empty"), HorizontalAlignment = HorizontalAlignment.Center };
            empty.AddThemeFontSizeOverride("font_size", 14);
            empty.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
            vb.AddChild(empty);
        }

        _activeStorePanel = p;
    }

    public static void ToggleDrawer()
    {
        if (_drawerOpen) { CloseDrawer(); return; }
        OpenDrawer();
    }

    public static void OpenDrawer()
    {
        if (_drawerOpen) return;
        EnsureHandle();
        if (_drawer != null && GodotObject.IsInstanceValid(_drawer))
        {
            _drawer.Visible = true; _drawerOpen = true; return;
        }

        var vp = _gm.GetViewport().GetVisibleRect().Size;
        float us = _gm.UIScale;
        float hw = HANDLE_W * us;
        float cw = 220 * us;
        float dh = vp.Y - 80 * us;
        float y0 = 40 * us;

        // 隐藏独立把手
        if (_handlePanel != null) _handlePanel.Visible = false;

        // 创建整体抽屉 = [把手 | 内容]
        _drawer = new Control { Size = new(hw + cw, dh) };

        // ── 左侧：把手 ──
        var handle = new Panel { Position = new(0, 0), Size = new(hw, dh) };
        var hs = new StyleBoxFlat { BgColor = new Color(0.2f, 0.4f, 0.8f, 0.15f), CornerRadiusTopLeft = 6, CornerRadiusBottomLeft = 6, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthBottom = 1, BorderColor = new Color(0.2f, 0.4f, 0.8f, 0.3f) };
        handle.AddThemeStyleboxOverride("panel", hs);
        var il = new Label { Text = "🃏", Position = new(0, dh * 0.2f), Size = new(hw, 28 * us), HorizontalAlignment = HorizontalAlignment.Center };
        il.AddThemeFontSizeOverride("font_size", (int)(20 * us)); handle.AddChild(il);
        var hl = new Label { Text = Loc.Tr("card.drawer_title"), Position = new(0, dh * 0.2f + 30 * us), Size = new(hw, 40 * us), HorizontalAlignment = HorizontalAlignment.Center };
        hl.AddThemeFontSizeOverride("font_size", (int)(10 * us)); hl.AddThemeColorOverride("font_color", new Color(0.2f, 0.3f, 0.6f));
        handle.AddChild(hl);
        var hhs = new StyleBoxFlat { BgColor = new Color(0.3f, 0.5f, 0.8f, 0.25f), CornerRadiusTopLeft = 6, CornerRadiusBottomLeft = 6, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthBottom = 1, BorderColor = new Color(0.3f, 0.5f, 0.8f, 0.5f) };
        handle.MouseEntered += () => handle.AddThemeStyleboxOverride("panel", hhs);
        handle.MouseExited += () => handle.AddThemeStyleboxOverride("panel", hs);
        handle.GuiInput += (ie) => { if (ie is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left) CloseDrawer(); };
        _drawer.AddChild(handle);

        // ── 右侧：内容区 ──
        var content = new Control { Position = new(hw, 0), Size = new(cw, dh) };
        var bg = new Panel();
        bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        bg.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f, 0.95f), BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1, BorderColor = new Color(0.2f, 0.4f, 0.8f, 0.25f), CornerRadiusTopRight = 8, CornerRadiusBottomRight = 8 });
        content.AddChild(bg);

        float cy = 8;
        var tl = new Label { Text = Loc.Tr("card.drawer_title"), Position = new(10, cy), Size = new(cw - 20, 24) };
        tl.AddThemeFontSizeOverride("font_size", 14); tl.AddThemeColorOverride("font_color", new Color(0.1f, 0.14f, 0.22f));
        content.AddChild(tl); cy += 28;

        var ft = new Label { Text = Loc.TrF("card.free_slots", _cs.FreeSlots.Count, CardSystem.FreeSlotCount), Position = new(10, cy), Size = new(cw - 20, 18) };
        ft.AddThemeFontSizeOverride("font_size", 10); ft.AddThemeColorOverride("font_color", new Color(0.3f, 0.6f, 0.3f));
        content.AddChild(ft); cy += 20;
        foreach (var c in _cs.FreeSlots) cy = AddCardSlot(content, c, cw, cy);

        var pt = new Label { Text = Loc.TrF("card.paid_slots", _cs.PaidSlots.Count, CardSystem.PaidSlotCount), Position = new(10, cy), Size = new(cw - 20, 18) };
        pt.AddThemeFontSizeOverride("font_size", 10); pt.AddThemeColorOverride("font_color", new Color(0.6f, 0.3f, 0.1f));
        content.AddChild(pt); cy += 20;
        foreach (var c in _cs.PaidSlots) cy = AddCardSlot(content, c, cw, cy);

        var sb = new Button { Text = Loc.Tr("card.open_store"), Position = new(10, cy), Size = new(cw - 20, 30) };
        sb.AddThemeFontSizeOverride("font_size", 12); sb.Pressed += () => { ShowStore(); CloseDrawer(); };
        content.AddChild(sb); cy += 34;

        var cb = new Button { Text = Loc.Tr("card.close_drawer"), Position = new(10, cy), Size = new(cw - 20, 26), Flat = true };
        cb.AddThemeFontSizeOverride("font_size", 10); cb.AddThemeColorOverride("font_color", new Color(0.7f, 0.3f, 0.3f));
        cb.Pressed += () => CloseDrawer();
        content.AddChild(cb);

        _drawer.AddChild(content);

        // 定位：关闭状态只露把手，打开状态整体左移露出内容
        float closedX = vp.X - hw;
        float openX = vp.X - hw - cw;
        _drawer.Position = new(closedX, y0);
        _gm.UiLayer.AddChild(_drawer);

        var tween = _drawer.CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quint);
        tween.TweenProperty(_drawer, "position", new Vector2(openX, y0), 0.25f);
        _drawerOpen = true;
    }

    private static float AddCardSlot(Control parent, CardDefinition card, float dw, float y)
    {
        var bg = new Panel { Position = new(8, y), Size = new(dw - 16, 44) };
        bg.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = new Color(0.95f, 0.94f, 0.90f, 0.9f),
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            BorderColor = RarityColor(card.Rarity, 0.4f) });
        parent.AddChild(bg);

        var nameL = new Label { Text = $"{card.Icon} {Loc.Tr(card.NameKey)}", Position = new(6, 2), Size = new(dw - 28, 18) };
        nameL.AddThemeFontSizeOverride("font_size", 10);
        nameL.AddThemeColorOverride("font_color", new Color(0.08f, 0.12f, 0.22f));
        bg.AddChild(nameL);

        var descL = new Label { Text = Loc.Tr(card.DescKey), Position = new(6, 20), Size = new(dw - 28, 22) };
        descL.AddThemeFontSizeOverride("font_size", 8);
        descL.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
        bg.AddChild(descL);

        // Click to select
        bg.MouseFilter = Control.MouseFilterEnum.Stop;
        bg.GuiInput += (ie) => {
            if (ie is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                StartCardUsage(card);
        };

        return y + 48;
    }

    private static void StartCardUsage(CardDefinition card)
    {
        _selectedCard = card;

        // Find available projects to use card on
        var devMgr = _gm.GetNode<GameDevManager>("GameDevManager");
        var targets = devMgr.Projects.Where(p => !p.IsReleased && p.Phase != DevPhase.Idle).ToList();
        var techMgr = _gm.GetNode<TechManager>("TechManager");

        if (targets.Count == 0 && !HasResearchEffect(card.EffectType))
        {
            _gm.ShowToast(Loc.Tr("card.no_target"), "", new Color(0.9f, 0.6f, 0.2f));
            _selectedCard = null; return;
        }

        // Show target selection popup
        var vp = _gm.GetViewport().GetVisibleRect().Size;
        var popup = new Panel { Position = new(vp.X * 0.25f, vp.Y * 0.25f), Size = new(vp.X * 0.5f, vp.Y * 0.5f) };
        popup.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = new Color(0.97f, 0.96f, 0.94f, 0.97f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.2f, 0.4f, 0.8f, 0.5f),
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 });
        _gm.UiLayer.AddChild(popup);

        float pady = 10;
        var tt = new Label { Text = Loc.TrF("card.select_target", Loc.Tr(card.NameKey)), Position = new(15, pady), Size = new(vp.X * 0.5f - 30, 26) };
        tt.AddThemeFontSizeOverride("font_size", 15);
        tt.AddThemeColorOverride("font_color", new Color(0.1f, 0.14f, 0.22f));
        popup.AddChild(tt); pady += 32;

        var effL = new Label { Text = Loc.Tr(card.DescKey), Position = new(15, pady), Size = new(vp.X * 0.5f - 30, 20) };
        effL.AddThemeFontSizeOverride("font_size", 11);
        effL.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.5f));
        popup.AddChild(effL); pady += 26;

        foreach (var p in targets)
        {
            int idx = popup.GetChildCount();
            var btn = new Button { Text = $"🎮 {p.Name} ({DevPhaseName(p.Phase)}) {p.DevProgress:F0}%",
                Position = new(15, pady), Size = new(vp.X * 0.5f - 30, 28), Flat = false };
            btn.AddThemeFontSizeOverride("font_size", 12);
            btn.Pressed += () => {
                ApplyCard(card, p);
                popup.QueueFree();
                CloseDrawer();
            };
            popup.AddChild(btn); pady += 32;
        }

        // Research option
        if (HasResearchEffect(card.EffectType) && _gm.GetNode<TechManager>("TechManager").ResearchProgress.Any(kv => !kv.Value.Equals(0)))
        {
            var rbtn = new Button { Text = "🔬 " + Loc.Tr("card.research_target"),
                Position = new(15, pady), Size = new(vp.X * 0.5f - 30, 28), Flat = false };
            rbtn.AddThemeFontSizeOverride("font_size", 12);
            rbtn.Pressed += () => {
                ApplyResearchCard(card);
                popup.QueueFree();
                CloseDrawer();
            };
            popup.AddChild(rbtn); pady += 32;
        }

        // Cancel
        var cb = new Button { Text = Loc.Tr("dlg.cancel"), Position = new(15, pady), Size = new(vp.X * 0.5f - 30, 26), Flat = true };
        cb.AddThemeFontSizeOverride("font_size", 11);
        cb.AddThemeColorOverride("font_color", new Color(0.7f, 0.3f, 0.3f));
        cb.Pressed += () => { _selectedCard = null; popup.QueueFree(); };
        popup.AddChild(cb);
    }

    private static void ApplyCard(CardDefinition card, GameProject target)
    {
        float v = card.EffectValue;
        switch (card.EffectType)
        {
            case CardEffectType.ProjectProgress:
                target.DevProgress = Mathf.Min(1f, target.DevProgress + v / 100f); break;
            case CardEffectType.Fun: target.GameplayScore += v; break;
            case CardEffectType.Graphics: target.GraphicsScore += v; break;
            case CardEffectType.Audio: target.AudioScore += v; break;
            case CardEffectType.Story: target.StoryScore += v; break;
            case CardEffectType.Stability: target.StabilityScore += v; break;
            case CardEffectType.AllAttributes:
                target.GameplayScore += v; target.GraphicsScore += v; target.AudioScore += v;
                target.StoryScore += v; target.StabilityScore += v; target.NetworkScore += v; target.AIScore += v; break;
            case CardEffectType.TechDebt:
                float d = Mathf.Abs(v);
                if (v < 0) target.TechDebt = Mathf.Max(0, target.TechDebt - d);
                else target.TechDebt = Mathf.Min(100, target.TechDebt + d); break;
            case CardEffectType.Sales:
                target.MonthlySalesBonus *= (1f + v / 100f); break;
            case CardEffectType.Score:
                target.FinalScore += v; break;
            case CardEffectType.Fatigue:
                float fv = Mathf.Abs(v);
                var tm = _gm.GetNode<TeamManager>("TeamManager");
                foreach (var team in tm.Teams)
                    if (team.CurrentProject == target)
                        foreach (var emp in team.Members)
                            emp.Fatigue = v < 0 ? Mathf.Max(0, emp.Fatigue - fv) : Mathf.Min(100, emp.Fatigue + fv); break;
            case CardEffectType.Memory:
                target.MemoryUsage = Mathf.Max(0, target.MemoryUsage + v); break;
            case CardEffectType.Legendary:
                target.GameplayScore += v; target.GraphicsScore += v; target.AudioScore += v;
                target.StoryScore += v; target.StabilityScore += v; target.NetworkScore += v; target.AIScore += v;
                target.DevProgress = Mathf.Min(1f, target.DevProgress + 0.3f); break;
        }

        _cs.DiscardCard(card);
        _gm.ShowToast(Loc.Tr("card.used"), Loc.Tr(card.NameKey) + " → " + target.Name, new Color(0.3f, 0.8f, 0.3f));
    }

    private static void ApplyResearchCard(CardDefinition card)
    {
        float v = card.EffectValue;
        var techMgr = _gm.GetNode<TechManager>("TechManager");
        var toAdvance = techMgr.ResearchProgress.Where(kv => kv.Value > 0 && !techMgr.IsResearched(kv.Key)).ToList();
        if (toAdvance.Count > 0)
        {
            var kv = toAdvance[Random.Shared.Next(toAdvance.Count)];
            float add = TechTreeData.AllTech.TryGetValue(kv.Key, out var info) ? info.RequiredManMonths * v / 100f : 0;
            techMgr.ResearchProgress[kv.Key] += add;
        }

        _cs.DiscardCard(card);
        _gm.ShowToast(Loc.Tr("card.used"), Loc.Tr(card.NameKey), new Color(0.3f, 0.8f, 0.3f));
    }

    public static void CloseDrawer()
    {
        if (_drawer == null || !GodotObject.IsInstanceValid(_drawer))
        {
            _drawerOpen = false; _selectedCard = null; return;
        }
        float hw = HANDLE_W * _gm.UIScale;
        float targetX = _drawer.Position.X + 220 * _gm.UIScale; // 滑回只露把手的位置
        var tween = _drawer.CreateTween();
        tween.SetEase(Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Quint);
        tween.TweenProperty(_drawer, "position", new Vector2(targetX, _drawer.Position.Y), 0.2f);
        tween.TweenCallback(Callable.From(() => {
            if (_drawer != null && GodotObject.IsInstanceValid(_drawer))
            {
                _drawer.QueueFree(); _drawer = null;
            }
            if (_handlePanel != null) _handlePanel.Visible = true;
        }));
        _drawerOpen = false;
        _selectedCard = null;
    }

    private static bool HasResearchEffect(CardEffectType t) =>
        t == CardEffectType.Research;

    private static string DevPhaseName(DevPhase p) => p switch
    {
        DevPhase.Planning => Loc.Tr("phase.Planning"),
        DevPhase.Developing => Loc.Tr("phase.Developing"),
        DevPhase.Polishing => Loc.Tr("phase.Polishing"),
        DevPhase.Marketing => Loc.Tr("phase.Marketing"),
        DevPhase.Released => Loc.Tr("phase.Released"),
        _ => ""
    };

    private static Color RarityColor(CardRarity r, float alpha) => r switch
    {
        CardRarity.Common => new Color(0.6f, 0.6f, 0.6f, alpha),
        CardRarity.Rare => new Color(0.2f, 0.5f, 0.9f, alpha),
        CardRarity.Epic => new Color(0.7f, 0.3f, 0.8f, alpha),
        CardRarity.Legendary => new Color(0.9f, 0.7f, 0.1f, alpha),
        _ => new Color(0.6f, 0.6f, 0.6f, alpha)
    };
}
