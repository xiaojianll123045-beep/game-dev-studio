using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>完整 Android RelativeLayout 实现，支持全部 12 种布局规则。</summary>
public partial class RelativeLayout : Container
{
    public enum Rule
    {
        AlignParentLeft, AlignParentTop, AlignParentRight, AlignParentBottom,
        CenterInParent, CenterHorizontal, CenterVertical,
        LeftOf, RightOf, Above, Below,
        AlignLeft, AlignRight, AlignTop, AlignBottom
    }

    private static readonly Dictionary<Control, List<(Rule rule, Control target)>> _rules = new();

    public static void AddRule(Control c, Rule rule, Control target = null)
    {
        if (!_rules.ContainsKey(c)) _rules[c] = new();
        _rules[c].Add((rule, target));
        if (c.GetParent() is RelativeLayout rl) rl.QueueSort();
    }

    public static void RemoveAllRules(Control c)
    {
        _rules.Remove(c);
    }

    private static readonly Dictionary<Control, Margins> _margins = new();

    public class Margins { public float L, T, R, B; }

    public static void SetMargins(Control c, float l, float t, float r, float b)
    {
        if (!_margins.ContainsKey(c)) _margins[c] = new();
        var m = _margins[c]; m.L = l; m.T = t; m.R = r; m.B = b;
        if (c.GetParent() is RelativeLayout rl) rl.QueueSort();
    }

    private static Margins GetMargins(Control c) =>
        _margins.TryGetValue(c, out var m) ? m : new();

    private float _padL, _padT, _padR, _padB;
    public void SetPadding(float l, float t, float r, float b)
    { _padL = l; _padT = t; _padR = r; _padB = b; QueueSort(); }

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == NotificationSortChildren) DoLayout();
    }

    private void DoLayout()
    {
        var rect = GetRect();
        float pw = rect.Size.X - _padL - _padR;
        float ph = rect.Size.Y - _padT - _padB;
        if (pw <= 0 || ph <= 0) return;

        var children = new List<Control>();
        foreach (var node in GetChildren())
            if (node is Control c && c.Visible) children.Add(c);

        var placed = new HashSet<Control>();

        // 第一遍：父容器对齐规则（AlignParent / Center）
        foreach (var c in children)
        {
            if (!_rules.TryGetValue(c, out var rules)) continue;
            var m = GetMargins(c);
            float w = c.GetCombinedMinimumSize().X;
            float h = c.GetCombinedMinimumSize().Y;
            if (w <= 0) w = pw - m.L - m.R;
            if (h <= 0) h = ph - m.T - m.B;

            float x = _padL + m.L, y = _padT + m.T;
            bool hasParentRule = false;

            foreach (var (rule, _) in rules)
            {
                switch (rule)
                {
                    case Rule.AlignParentLeft:      x = _padL + m.L; hasParentRule = true; break;
                    case Rule.AlignParentTop:       y = _padT + m.T; hasParentRule = true; break;
                    case Rule.AlignParentRight:     x = _padL + pw - w - m.R; hasParentRule = true; break;
                    case Rule.AlignParentBottom:    y = _padT + ph - h - m.B; hasParentRule = true; break;
                    case Rule.CenterInParent:       x = _padL + (pw - w) / 2; y = _padT + (ph - h) / 2; hasParentRule = true; break;
                    case Rule.CenterHorizontal:     x = _padL + (pw - w) / 2; hasParentRule = true; break;
                    case Rule.CenterVertical:       y = _padT + (ph - h) / 2; hasParentRule = true; break;
                }
            }

            if (hasParentRule)
            {
                c.Position = new(x, y);
                c.Size = new(w, h);
                placed.Add(c);
            }
        }

        // 第二遍：相对兄弟规则（LeftOf/RightOf/Above/Below + Align）
        for (int pass = 0; pass < 3; pass++)
        {
            foreach (var c in children)
            {
                if (placed.Contains(c)) continue;
                if (!_rules.TryGetValue(c, out var rules)) { placed.Add(c); continue; }

                var m = GetMargins(c);
                float w = c.GetCombinedMinimumSize().X;
                float h = c.GetCombinedMinimumSize().Y;
                if (w <= 0) w = pw - m.L - m.R;
                if (h <= 0) h = ph - m.T - m.B;

                float x = _padL + m.L, y = _padT + m.T;
                bool canPlace = true;
                bool hasRelative = false;

                foreach (var (rule, target) in rules)
                {
                    if (target == null) continue;
                    if (!placed.Contains(target)) { canPlace = false; break; }
                    hasRelative = true;

                    switch (rule)
                    {
                        case Rule.LeftOf:      x = target.Position.X - w - m.R; break;
                        case Rule.RightOf:     x = target.Position.X + target.Size.X + m.L; break;
                        case Rule.Above:       y = target.Position.Y - h - m.B; break;
                        case Rule.Below:       y = target.Position.Y + target.Size.Y + m.T; break;
                        case Rule.AlignLeft:   x = target.Position.X + m.L; break;
                        case Rule.AlignRight:  x = target.Position.X + target.Size.X - w - m.R; break;
                        case Rule.AlignTop:    y = target.Position.Y + m.T; break;
                        case Rule.AlignBottom: y = target.Position.Y + target.Size.Y - h - m.B; break;
                    }
                }

                if (canPlace && hasRelative)
                {
                    c.Position = new(x, y);
                    c.Size = new(w, h);
                    placed.Add(c);
                }
            }
        }

        // 剩余未布局的：简单纵向排列
        float oy = _padT;
        foreach (var c in children)
        {
            if (placed.Contains(c)) continue;
            var m = GetMargins(c);
            float w = c.GetCombinedMinimumSize().X; if (w <= 0) w = pw - m.L - m.R;
            float h = c.GetCombinedMinimumSize().Y; if (h <= 0) h = 30;
            c.Position = new(_padL + m.L, oy + m.T);
            c.Size = new(w, h);
            oy += h + m.T + m.B + 4;
        }
    }
}
