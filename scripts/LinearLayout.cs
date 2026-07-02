using Godot;
using System.Collections.Generic;

/// <summary>Android LinearLayout 实现。子控件通过 SetXxx 方法配置布局参数。</summary>
public partial class LinearLayout : Container
{
    public enum Orient { Vertical, Horizontal }
    public enum Grav { Left, Center, Right, Top, Bottom }

    private Orient _orientation = Orient.Vertical;
    public Orient Orientation { get => _orientation; set { _orientation = value; QueueSort(); } }

    private float _spacing = 0;
    public float Spacing { get => _spacing; set { _spacing = value; QueueSort(); } }

    private float _padL, _padT, _padR, _padB;
    public void SetPadding(float l, float t, float r, float b)
    { _padL = l; _padT = t; _padR = r; _padB = b; QueueSort(); }

    private Grav _gravity = Grav.Left;
    public Grav Gravity { get => _gravity; set { _gravity = value; QueueSort(); } }

    // ── 子控件布局参数 ──
    private static readonly Dictionary<Control, LayoutParams> _params = new();

    public class LayoutParams
    {
        public float Weight;
        public float MarginL, MarginT, MarginR, MarginB;
        public Grav Gravity = Grav.Left;
        public float MinW, MinH;
    }

    public static void SetWeight(Control c, float w) => GetOrCreate(c).Weight = w;
    public static void SetMargins(Control c, float l, float t, float r, float b)
    { var p = GetOrCreate(c); p.MarginL = l; p.MarginT = t; p.MarginR = r; p.MarginB = b; }
    public static void SetGravity(Control c, Grav g) => GetOrCreate(c).Gravity = g;
    public static void SetMinSize(Control c, float w, float h)
    { var p = GetOrCreate(c); p.MinW = w; p.MinH = h; }

    private static LayoutParams GetOrCreate(Control c)
    {
        if (!_params.TryGetValue(c, out var lp))
        { lp = new(); _params[c] = lp; }
        return lp;
    }

    private static LayoutParams Get(Control c) =>
        _params.TryGetValue(c, out var lp) ? lp : new();

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == NotificationSortChildren) DoLayout();
    }

    private void DoLayout()
    {
        var rect = GetRect();
        float availW = rect.Size.X - _padL - _padR;
        float availH = rect.Size.Y - _padT - _padB;
        if (availW <= 0 || availH <= 0) return;

        // 收集可见子控件
        var children = new List<Control>();
        float totalWeight = 0;
        float usedMain = 0;
        float maxCross = 0;

        foreach (var node in GetChildren())
        {
            if (node is Control c && c.Visible)
            {
                children.Add(c);
                var lp = Get(c);
                totalWeight += lp.Weight;

                if (_orientation == Orient.Vertical)
                {
                    float h = lp.MinH > 0 ? lp.MinH : c.GetCombinedMinimumSize().Y;
                    usedMain += h + lp.MarginT + lp.MarginB + (children.Count > 1 ? _spacing : 0);
                    float childW = lp.MinW > 0 ? lp.MinW : c.GetCombinedMinimumSize().X;
                    if (childW + lp.MarginL + lp.MarginR > maxCross)
                        maxCross = childW + lp.MarginL + lp.MarginR;
                }
                else
                {
                    float w = lp.MinW > 0 ? lp.MinW : c.GetCombinedMinimumSize().X;
                    usedMain += w + lp.MarginL + lp.MarginR + (children.Count > 1 ? _spacing : 0);
                    float childH = lp.MinH > 0 ? lp.MinH : c.GetCombinedMinimumSize().Y;
                    if (childH + lp.MarginT + lp.MarginB > maxCross)
                        maxCross = childH + lp.MarginT + lp.MarginB;
                }
            }
        }

        if (children.Count == 0) return;

        // 主轴剩余空间
        float mainAvail = _orientation == Orient.Vertical ? availH : availW;
        float crossAvail = _orientation == Orient.Vertical ? availW : availH;
        float remaining = mainAvail - usedMain;
        if (remaining < 0) remaining = 0;

        // 交叉轴内容尺寸（不超过可用空间）
        float crossSize = Mathf.Min(maxCross, crossAvail);

        float offset = 0;
        foreach (var c in children)
        {
            var lp = Get(c);
            float mainSize, crossChild;
            float mOff;

            if (_orientation == Orient.Vertical)
            {
                mainSize = lp.Weight > 0 ? remaining * (lp.Weight / totalWeight) :
                    (lp.MinH > 0 ? lp.MinH : c.GetCombinedMinimumSize().Y);
                mainSize = Mathf.Max(mainSize, lp.MinH);
                crossChild = crossSize - lp.MarginL - lp.MarginR;
                mOff = lp.MarginT;

                float gOff = 0;
                var g = lp.Gravity != Grav.Left ? lp.Gravity : _gravity;
                if (g == Grav.Center) gOff = (crossAvail - crossSize) / 2;
                else if (g == Grav.Right) gOff = crossAvail - crossSize;
                gOff += (crossSize - crossChild - lp.MarginL - lp.MarginR) / 2;

                c.Position = new(_padL + lp.MarginL + gOff, _padT + offset + mOff);
                c.Size = new(Mathf.Max(0, crossChild), Mathf.Max(0, mainSize));
                offset += mainSize + lp.MarginT + lp.MarginB + _spacing;
            }
            else
            {
                mainSize = lp.Weight > 0 ? remaining * (lp.Weight / totalWeight) :
                    (lp.MinW > 0 ? lp.MinW : c.GetCombinedMinimumSize().X);
                mainSize = Mathf.Max(mainSize, lp.MinW);
                crossChild = crossSize - lp.MarginT - lp.MarginB;
                mOff = lp.MarginL;

                float gOff = 0;
                var g = lp.Gravity != Grav.Left ? lp.Gravity : _gravity;
                if (g == Grav.Center) gOff = (crossAvail - crossSize) / 2;
                else if (g == Grav.Bottom) gOff = crossAvail - crossSize;
                gOff += (crossSize - crossChild - lp.MarginT - lp.MarginB) / 2;

                c.Position = new(_padL + offset + mOff, _padT + lp.MarginT + gOff);
                c.Size = new(Mathf.Max(0, mainSize), Mathf.Max(0, crossChild));
                offset += mainSize + lp.MarginL + lp.MarginR + _spacing;
            }

            c.Owner = this;
        }
    }
}
