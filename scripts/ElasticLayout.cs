using Godot;
using System.Collections.Generic;

/// <summary>
/// 弹性布局 — 网格排列子控件，自动缩放大小和字体。
/// 类似 Android FlexboxLayout / GridLayout 自适应版。
/// </summary>
public partial class ElasticLayout : Container
{
    /// <summary>期望列数（水平方向格子数）</summary>
    public int Columns { get; set; } = 3;

    /// <summary>格子之间的间距</summary>
    public float Spacing { get; set; } = 6;

    /// <summary>内边距</summary>
    public float Padding { get; set; } = 8;

    /// <summary>字体缩放基数（相对于格子高度的比例，0=不缩放）</summary>
    public float FontScale { get; set; } = 0.018f;

    /// <summary>最小/最大字体</summary>
    public int FontMin { get; set; } = 8;
    public int FontMax { get; set; } = 28;

    /// <summary>格子最小宽高</summary>
    public float CellMinW { get; set; } = 80;
    public float CellMinH { get; set; } = 40;

    /// <summary>每个子控件的弹性权重（默认1）</summary>
    private static readonly Dictionary<Control, float> _weights = new();

    public static void SetWeight(Control c, float w)
    { _weights[c] = w; if (c.GetParent() is ElasticLayout el) el.QueueSort(); }

    /// <summary>是否启用字体缩放</summary>
    public bool EnableFontScaling { get; set; } = true;

    /// <summary>子控件是否等宽（否则按权重）</summary>
    public bool EqualSize { get; set; } = true;

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == NotificationSortChildren) DoLayout();
    }

    private void DoLayout()
    {
        var rect = GetRect();
        float pw = rect.Size.X - Padding * 2;
        float ph = rect.Size.Y - Padding * 2;
        if (pw <= 0 || ph <= 0) return;

        // 收集可见子控件
        var children = new List<Control>();
        foreach (var node in GetChildren())
            if (node is Control c && c.Visible) children.Add(c);

        int count = children.Count;
        if (count == 0) return;

        int cols = Mathf.Max(1, Columns);
        int rows = Mathf.CeilToInt((float)count / cols);

        float cellW = (pw - Spacing * (cols - 1)) / cols;
        float cellH = (ph - Spacing * (rows - 1)) / rows;

        // 计算字体大小
        float baseFontSize = Mathf.Min(cellH, cellW) * FontScale;
        int fontSize = Mathf.Clamp((int)baseFontSize, FontMin, FontMax);

        int idx = 0;
        for (int r = 0; r < rows && idx < count; r++)
        {
            int colsInRow = Mathf.Min(cols, count - r * cols);
            float rowH = cellH;

            for (int c = 0; c < colsInRow && idx < count; c++, idx++)
            {
                float w = cellW;
                float h = rowH;
                float x = Padding + c * (cellW + Spacing);
                float y = Padding + r * (cellH + Spacing);

                // 最后一行可能不完整，居中
                if (c == colsInRow - 1 && colsInRow < cols)
                {
                    float extra = (cols - colsInRow) * (cellW + Spacing);
                    x += extra / 2;
                }

                var child = children[idx];

                // 权重调整
                if (_weights.TryGetValue(child, out var weight) && weight > 0 && !EqualSize)
                {
                    w = (pw - Spacing * (cols - 1)) * weight;
                }

                // 最小尺寸
                w = Mathf.Max(w, CellMinW);
                h = Mathf.Max(h, CellMinH);

                child.Position = new(x, y);
                child.Size = new(w, h);

                // 字体缩放
                if (EnableFontScaling)
                {
                    ScaleChildFont(child, fontSize);
                }
            }
        }
    }

    private void ScaleChildFont(Control child, int targetSize)
    {
        // 递归查找所有 Label 并缩放
        ApplyFontRecursive(child, targetSize);
    }

    private static void ApplyFontRecursive(Control c, int size)
    {
        if (c is Label label)
        {
            label.AddThemeFontSizeOverride("font_size", size);
        }
        if (c is Button btn)
        {
            btn.AddThemeFontSizeOverride("font_size", Mathf.Max(size - 2, 8));
        }
        if (c is CheckBox cb)
        {
            cb.AddThemeFontSizeOverride("font_size", Mathf.Max(size - 2, 8));
        }
        if (c is LineEdit le)
        {
            le.AddThemeFontSizeOverride("font_size", Mathf.Max(size - 2, 8));
        }
        if (c is OptionButton ob)
        {
            ob.AddThemeFontSizeOverride("font_size", Mathf.Max(size - 2, 8));
        }

        foreach (var node in c.GetChildren())
        {
            if (node is Control child)
                ApplyFontRecursive(child, size);
        }
    }
}
