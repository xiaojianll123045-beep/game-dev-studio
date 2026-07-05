using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

/// <summary>
/// Mod UI 补丁系统 — Mod 通过 JSON 描述定义 UI 面板，
/// 系统自动解析并注入到游戏的 UI 层级中。
/// </summary>
public static class ModUIPatch
{
    private static GameManager _gm;
    private static List<Control> _injectedPanels = new();

    /// <summary>初始化（GameManager._Ready 调用）</summary>
    public static void Init(GameManager gm) { _gm = gm; }

    /// <summary>从 JSON 字符串创建 UI 面板</summary>
    public static Control CreateFromJson(string json, string modId)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var el = doc.RootElement;
            return BuildControl(el, modId);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[ModUIPatch] JSON 解析错误 [{modId}]: {e.Message}");
            return null;
        }
    }

    /// <summary>将 JSON 定义的 UI 注入到游戏 UI 层</summary>
    public static Control InjectPanel(string json, string modId)
    {
        var panel = CreateFromJson(json, modId);
        if (panel != null)
        {
            _gm?.UiLayer?.AddChild(panel);
            _injectedPanels.Add(panel);
            GD.Print($"[ModUIPatch] 已注入 UI 面板 [{modId}]");
        }
        return panel;
    }

    /// <summary>移除所有注入的 UI 面板</summary>
    public static void RemoveAllPanels()
    {
        foreach (var p in _injectedPanels)
        {
            if (p != null && IsInstanceValid(p))
                p.QueueFree();
        }
        _injectedPanels.Clear();
    }

    /// <summary>获取游戏 UI 层的根节点</summary>
    public static Control GetUIRoot() => _gm?.UiLayer;

    /// <summary>弹出模态面板（居中弹窗）</summary>
    public static Control ShowModal(string json, string modId)
    {
        var panel = CreateFromJson(json, modId);
        if (panel == null) return null;

        // 包裹在半透明遮罩中（使用锚点填满视口，适配分辨率）
        var overlay = new Panel
        {
            AnchorLeft = 0, AnchorTop = 0, AnchorRight = 1, AnchorBottom = 1,
            MouseFilter = Control.MouseFilterEnum.Pass,
        };
        var style = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.5f) };
        overlay.AddThemeStyleboxOverride("panel", style);

        panel.Position = (overlay.Size - panel.Size) / 2;
        overlay.AddChild(panel);

        _gm?.UiLayer?.AddChild(overlay);
        _injectedPanels.Add(overlay);
        return overlay;
    }

    /// <summary>关闭模态面板</summary>
    public static void CloseModal(Control overlay)
    {
        if (overlay != null && IsInstanceValid(overlay))
        {
            overlay.QueueFree();
            _injectedPanels.Remove(overlay);
        }
    }

    // ═══════════════ JSON → Control 构建器 ═══════════════

    private static Control BuildControl(JsonElement el, string modId, Control parent = null)
    {
        var type = el.TryGetProperty("type", out var t) ? t.GetString() ?? "label" : "label";
        Control ctrl = type.ToLower() switch
        {
            "panel" => new Panel(),
            "label" => new Label(),
            "button" => new Button(),
            "textedit" => new TextEdit(),
            "lineedit" => new LineEdit(),
            "slider" => new HSlider(),
            "checkbox" => new CheckBox(),
            "optionbutton" => new OptionButton(),
            "scroll" => new ScrollContainer(),
            "vbox" => new VBoxContainer(),
            "hbox" => new HBoxContainer(),
            "grid" => new GridContainer(),
            "progress" => new ProgressBar(),
            "texture" => new TextureRect(),
            _ => new Label(),
        };

        // 基础属性
        string name = el.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        if (!string.IsNullOrEmpty(name)) ctrl.Name = name;

        if (el.TryGetProperty("text", out var text))
            SetTextProperty(ctrl, text.GetString() ?? "");

        if (el.TryGetProperty("tooltip", out var tip))
            ctrl.TooltipText = tip.GetString() ?? "";

        // 锚点布局（0~1 比例，覆盖硬坐标，适配分辨率）
        if (el.TryGetProperty("anchor_left", out var al)) ctrl.AnchorLeft = al.GetSingle();
        if (el.TryGetProperty("anchor_top", out var at)) ctrl.AnchorTop = at.GetSingle();
        if (el.TryGetProperty("anchor_right", out var ar)) ctrl.AnchorRight = ar.GetSingle();
        if (el.TryGetProperty("anchor_bottom", out var ab)) ctrl.AnchorBottom = ab.GetSingle();
        // 锚点偏移（px）
        if (el.TryGetProperty("offset_left", out var ol)) ctrl.OffsetLeft = ol.GetSingle();
        if (el.TryGetProperty("offset_top", out var ot)) ctrl.OffsetTop = ot.GetSingle();
        if (el.TryGetProperty("offset_right", out var or_)) ctrl.OffsetRight = or_.GetSingle();
        if (el.TryGetProperty("offset_bottom", out var ob)) ctrl.OffsetBottom = ob.GetSingle();
        // 边距（offset 简写）
        if (el.TryGetProperty("margin", out var m)) { float mv = m.GetSingle(); ctrl.OffsetLeft = mv; ctrl.OffsetTop = mv; ctrl.OffsetRight = -mv; ctrl.OffsetBottom = -mv; }

        // 硬坐标（仅当未设置锚点时作为回退）
        if (!el.TryGetProperty("anchor_left", out _) && !el.TryGetProperty("offset_left", out _))
        {
            if (el.TryGetProperty("x", out var x)) ctrl.Position = new Vector2(x.GetSingle(), ctrl.Position.Y);
            if (el.TryGetProperty("y", out var y)) ctrl.Position = new Vector2(ctrl.Position.X, y.GetSingle());
        }
        if (el.TryGetProperty("w", out var w)) ctrl.Size = new Vector2(w.GetSingle(), ctrl.Size.Y);
        if (el.TryGetProperty("h", out var h)) ctrl.Size = new Vector2(ctrl.Size.X, h.GetSingle());

        // size_flags（弹性布局）
        if (el.TryGetProperty("expand_h", out var eh) && eh.GetBoolean())
            ctrl.SizeFlagsHorizontal |= Control.SizeFlags.ExpandFill;
        if (el.TryGetProperty("expand_v", out var ev) && ev.GetBoolean())
            ctrl.SizeFlagsVertical |= Control.SizeFlags.ExpandFill;
        if (el.TryGetProperty("shrink_center", out var sc) && sc.GetBoolean())
        {
            ctrl.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            ctrl.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        }
        // 容器间距
        if (el.TryGetProperty("separation", out var sep) && ctrl is Container cnt)
            cnt.AddThemeConstantOverride("separation", sep.GetInt32());

        if (el.TryGetProperty("color", out var color))
        {
            var parts = color.GetString().Split(',');
            if (parts.Length >= 3)
            {
                float r = float.Parse(parts[0]) / 255f;
                float g = float.Parse(parts[1]) / 255f;
                float b = float.Parse(parts[2]) / 255f;
                float a = parts.Length >= 4 ? float.Parse(parts[3]) / 255f : 1f;
                if (ctrl is Label lbl) lbl.AddThemeColorOverride("font_color", new Color(r, g, b, a));
                else if (ctrl is Button btn) btn.AddThemeColorOverride("font_color", new Color(r, g, b, a));
            }
        }

        if (el.TryGetProperty("font_size", out var fs))
        {
            int size = fs.GetInt32();
            if (ctrl is Label lbl) lbl.AddThemeFontSizeOverride("font_size", size);
            else if (ctrl is Button btn) btn.AddThemeFontSizeOverride("font_size", size);
        }

        // 按钮事件（通过 ModAPI 调用方法）
        if (ctrl is Button btnCtrl && el.TryGetProperty("on_click", out var onClick))
        {
            string action = onClick.GetString() ?? "";
            btnCtrl.Pressed += () => ModEventBus.FireCustom($"ui_click_{modId}_{action}");
        }

        // 子控件
        if (el.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in children.EnumerateArray())
            {
                var childCtrl = BuildControl(child, modId, ctrl);
                if (childCtrl != null)
                {
                    if (ctrl is Container c)
                        c.AddChild(childCtrl);
                    else
                        ctrl.AddChild(childCtrl);
                }
            }
        }

        return ctrl;
    }

    private static void SetTextProperty(Control ctrl, string text)
    {
        if (ctrl is Label l) l.Text = text;
        else if (ctrl is Button b) b.Text = text;
        else if (ctrl is CheckBox cb) cb.Text = text;
    }

    private static bool IsInstanceValid(Node node)
    {
        try { return node != null && !node.IsQueuedForDeletion(); }
        catch { return false; }
    }
}
