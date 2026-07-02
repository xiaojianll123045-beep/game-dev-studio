using Godot;
using System;
using System.Collections.Generic;

/// <summary>Android 风格 UI 构建辅助。</summary>
public static class LUI
{
    // ═══════════════════════ 创建布局 ═══════════════════════

    public static LinearLayout VBox(float spacing = 0)
    {
        var ll = new LinearLayout { Orientation = LinearLayout.Orient.Vertical, Spacing = spacing };
        return ll;
    }

    public static LinearLayout HBox(float spacing = 0)
    {
        var ll = new LinearLayout { Orientation = LinearLayout.Orient.Horizontal, Spacing = spacing };
        return ll;
    }

    public static RelativeLayout Relative()
    {
        return new RelativeLayout();
    }

    public static ScrollContainer Scroll(Control content)
    {
        var sc = new ScrollContainer();
        sc.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        sc.AddChild(content);
        return sc;
    }

    // ═══════════════════════ 基础组件 ═══════════════════════

    public static Label Label(string text, int fontSize = 14, Color? color = null, float minH = 0)
    {
        var l = new Label { Text = text ?? "" };
        l.AddThemeFontSizeOverride("font_size", fontSize);
        l.AddThemeColorOverride("font_color", color ?? new Color(0.10f, 0.14f, 0.22f));
        if (minH > 0) l.CustomMinimumSize = new(0, minH);
        return l;
    }

    public static Button Button(string text, Action onClick = null, bool flat = false)
    {
        var b = new Button { Text = text ?? "", Flat = flat };
        b.AddThemeFontSizeOverride("font_size", 13);
        if (onClick != null) b.Pressed += onClick;
        return b;
    }

    public static LineEdit Input(string text = "", string placeholder = "", float width = 180)
    {
        return new LineEdit { Text = text, PlaceholderText = placeholder, CustomMinimumSize = new(width, 0) };
    }

    public static Panel Card(Color? bg = null)
    {
        var p = new Panel();
        p.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = bg ?? new Color(0.97f, 0.96f, 0.94f, 0.85f),
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        });
        return p;
    }

    // ═══════════════════════ 边距简写 ═══════════════════════

    public static void Margins(Control c, float all) => LinearLayout.SetMargins(c, all, all, all, all);
    public static void Margins(Control c, float l, float t, float r, float b) => LinearLayout.SetMargins(c, l, t, r, b);
    public static void Weight(Control c, float w) => LinearLayout.SetWeight(c, w);

    // ═══════════════════════ 添加到布局 ═══════════════════════

    /// <summary>添加到 LinearLayout 并设 weight</summary>
    public static T Add<T>(this LinearLayout ll, T child, float weight = 0) where T : Control
    {
        ll.AddChild(child);
        if (weight > 0) LinearLayout.SetWeight(child, weight);
        return child;
    }

    /// <summary>添加到 LinearLayout 并设 margins</summary>
    public static T Add<T>(this LinearLayout ll, T child, float mL, float mT, float mR, float mB) where T : Control
    {
        ll.AddChild(child);
        LinearLayout.SetMargins(child, mL, mT, mR, mB);
        return child;
    }

    public static T AddTo<T>(this T child, LinearLayout ll, float weight = 0) where T : Control
    {
        ll.AddChild(child);
        if (weight > 0) LinearLayout.SetWeight(child, weight);
        return child;
    }

    // ═══════════════════════ RelativeLayout 规则 ═══════════════════════

    public static T Align<T>(this T c, RelativeLayout.Rule rule, Control target = null) where T : Control
    {
        RelativeLayout.AddRule(c, rule, target);
        return c;
    }
}
