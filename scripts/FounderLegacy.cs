using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public enum HeirType
{
    ProductVisionary, BusinessTycoon, TechArchitect, CreativeDirector, PeoplePerson
}

public partial class FounderLegacy : Node
{
    private GameManager _gm => Services.GameManager;
    private GameDevManager _devMgr => Services.GameDevManager;

    public int SuccessionCount { get; private set; }
    public HeirType? CurrentHeir { get; private set; }
    public int FoundedYear { get; set; }
    public List<string> Milestones { get; private set; } = new();

    public void CheckSuccession()
    {
        int year = _gm.GameYear;
        if (year - FoundedYear >= 10 * (SuccessionCount + 1) && SuccessionCount < 3)
            TriggerSuccession();
    }

    private void TriggerSuccession()
    {
        SuccessionCount++;
        _gm.IsAnyModalOpen = true;
        var vp = _gm.GetViewport().GetVisibleRect().Size;
        var panel = new Panel { Position = new(vp.X * 0.12f, vp.Y * 0.1f), Size = new(vp.X * 0.76f, vp.Y * 0.8f) };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.97f, 0.96f, 0.94f, 0.97f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.5f, 0.3f, 0.8f, 0.6f), CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12 });
        _gm.UiLayer.AddChild(panel);
        panel.TreeExited += () => _gm.IsAnyModalOpen = false;

        var scroll = new ScrollContainer { Position = new Vector2(10, 10), Size = new Vector2(panel.Size.X - 20, panel.Size.Y - 20) };
        panel.AddChild(scroll);

        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        content.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(content);

        content.AddChild(MakeTitle(Loc.Tr("legacy.title"), 22, new Color(0.4f, 0.2f, 0.7f)));
        content.AddChild(MakeText(Loc.TrF("legacy.desc", SuccessionCount), 14, new Color(0.2f, 0.2f, 0.25f), true));

        foreach (HeirType heir in Enum.GetValues(typeof(HeirType)))
        {
            var btn = new Button { Text = Loc.Tr($"legacy.heir_{heir}"), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            btn.AddThemeFontSizeOverride("font_size", 14);
            var capturedHeir = heir;
            btn.Pressed += () => { ApplyHeir(capturedHeir); panel.QueueFree(); };
            content.AddChild(btn);

            var hint = MakeText(Loc.Tr($"legacy.heir_{heir}_desc"), 11, new Color(0.5f, 0.5f, 0.55f), true);
            content.AddChild(hint);
        }
    }

    private Label MakeTitle(string text, int fontSize, Color color)
    {
        var l = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        l.AddThemeFontSizeOverride("font_size", fontSize);
        l.AddThemeColorOverride("font_color", color);
        return l;
    }

    private Label MakeText(string text, int fontSize, Color color, bool wrap)
    {
        var l = new Label { Text = text, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, AutowrapMode = wrap ? TextServer.AutowrapMode.Word : TextServer.AutowrapMode.Off };
        l.AddThemeFontSizeOverride("font_size", fontSize);
        l.AddThemeColorOverride("font_color", color);
        return l;
    }

    private void ApplyHeir(HeirType heir)
    {
        CurrentHeir = heir;
        Milestones.Add($"[年{_gm.GameYear}] 传承{SuccessionCount}: {heir}");
        switch (heir)
        {
            case HeirType.ProductVisionary:
                _devMgr.CompletedProjects.ForEach(p => p.FinalScore = Mathf.Min(p.FinalScore + 5, 115));
                break;
            case HeirType.BusinessTycoon:
                _devMgr.IsListed = true;
                _devMgr.SharePrice = Mathf.Max(_devMgr.SharePrice, 50f);
                Services.ResourceManager?.EarnMoney(300000, "legacy");
                break;
        }
        _gm.ShowToast("👑", Loc.TrF("legacy.applied", Loc.Tr($"legacy.heir_{heir}")), new Color(0.5f, 0.3f, 0.8f));
    }
}
