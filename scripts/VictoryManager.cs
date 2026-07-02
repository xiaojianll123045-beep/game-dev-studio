using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public enum VictoryType
{
    HallOfFame, IndustryGiant, TechPioneer, CultClassic, LegendaryStudio, Eternal
}

public class VictoryCondition
{
    public VictoryType Type;
    public string Name;
    public string Description;
    public Func<GameManager, bool> Check;
    public bool Unlocked;
}

public partial class VictoryManager : Node
{
    private GameManager _gm;
    private List<VictoryCondition> _conditions = new();

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        _conditions = new List<VictoryCondition>
        {
            new() { Type = VictoryType.HallOfFame, Name = Loc.Tr("victory.hall_of_fame"), Description = Loc.Tr("victory.hall_of_fame_desc"), Check = (gm) => gm.GetNode<GameDevManager>("GameDevManager").CompletedProjects.Count(p => p.FinalScore >= 90) >= 5 },
            new() { Type = VictoryType.IndustryGiant, Name = Loc.Tr("victory.industry_giant"), Description = Loc.Tr("victory.industry_giant_desc"), Check = (gm) => { var dev = gm.GetNode<GameDevManager>("GameDevManager"); if (!dev.IsListed) return false; var comp = gm.GetNode<CompetitorAI>("CompetitorAI"); float myMC = dev.SharePrice * dev.SharesOutstanding; float totalAI = comp.Studios.Where(s => s.IsListed).Sum(s => s.MarketCap); return myMC > totalAI && totalAI > 0; } },
            new() { Type = VictoryType.TechPioneer, Name = Loc.Tr("victory.tech_pioneer"), Description = Loc.Tr("victory.tech_pioneer_desc"), Check = (gm) => { var tech = gm.GetNode<TechManager>("TechManager"); return TechTreeData.AllTech.Keys.All(id => tech.IsResearched(id)); } },
            new() { Type = VictoryType.CultClassic, Name = Loc.Tr("victory.cult_classic"), Description = Loc.Tr("victory.cult_classic_desc"), Check = (gm) => gm.GetNode<GameDevManager>("GameDevManager").CompletedProjects.Any(p => p.Sales >= 100_000_000) },
            new() { Type = VictoryType.LegendaryStudio, Name = Loc.Tr("victory.legendary_studio"), Description = Loc.Tr("victory.legendary_studio_desc"), Check = (gm) => { var dev = gm.GetNode<GameDevManager>("GameDevManager"); for (int y = gm.GameYear - 9; y <= gm.GameYear; y++) { int s = (y-1)*12, e = y*12; if (!dev.CompletedProjects.Any(p => p.OriginalReleaseMonth >= s && p.OriginalReleaseMonth < e && p.FinalScore >= 85)) return false; } return gm.GameYear >= 10; } },
            new() { Type = VictoryType.Eternal, Name = Loc.Tr("victory.eternal"), Description = Loc.Tr("victory.eternal_desc"), Check = (gm) => gm.GameYear >= 30 },
        };
    }

    public void MonthlyCheck()
    {
        foreach (var cond in _conditions)
        {
            if (!cond.Unlocked && cond.Check(_gm))
            {
                cond.Unlocked = true;
                _gm.ShowPopup(Loc.TrF("victory.unlocked", cond.Name), cond.Description, new Color(1f, 0.85f, 0.1f));
                if (_conditions.All(c => c.Unlocked)) ShowCompleteVictory();
            }
        }
    }

    private void ShowCompleteVictory()
    {
        _gm.Paused = true;
        var vp = _gm.GetViewport().GetVisibleRect().Size;
        var panel = new Panel { Position = new(vp.X/2-300, vp.Y/2-200), Size = new(600, 400) };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.1f, 0.08f, 0.15f, 0.98f), CornerRadiusTopLeft = 16, CornerRadiusTopRight = 16, CornerRadiusBottomLeft = 16, CornerRadiusBottomRight = 16, BorderWidthLeft = 3, BorderWidthTop = 3, BorderWidthRight = 3, BorderWidthBottom = 3, BorderColor = new Color(1f, 0.85f, 0.1f, 0.8f) });
        var title = new Label { Text = Loc.Tr("victory.complete"), Position = new(20, 30), Size = new(560, 50), HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 32); title.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.1f)); panel.AddChild(title);
        var dev = _gm.GetNode<GameDevManager>("GameDevManager");
        int total = dev.CompletedProjects.Count;
        float avg = total > 0 ? dev.CompletedProjects.Average(p => p.FinalScore) : 0;
        var stats = new Label { Text = Loc.TrF("victory.stats", _gm.Founder.CompanyName, _gm.Founder.Name, _gm.GameYear, total, avg, _gm.GetNode<ResourceManager>("ResourceManager").Money), Position = new(40, 100), Size = new(520, 180) };
        stats.AddThemeFontSizeOverride("font_size", 16); stats.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.95f)); panel.AddChild(stats);
        var cont = new Button { Text = Loc.Tr("victory.continue"), Position = new(80, 310), Size = new(200, 40) };
        cont.AddThemeFontSizeOverride("font_size", 16); cont.Pressed += () => { panel.QueueFree(); _gm.Paused = false; }; panel.AddChild(cont);
        var menu = new Button { Text = Loc.Tr("victory.back_to_menu"), Position = new(320, 310), Size = new(200, 40) };
        menu.AddThemeFontSizeOverride("font_size", 16); menu.Pressed += () => _gm.GetTree().ChangeSceneToFile("res://scenes/menu.tscn"); panel.AddChild(menu);
        _gm.UiLayer.AddChild(panel);
    }

    public int Progress => _conditions.Count(c => c.Unlocked);
    public int Total => _conditions.Count;
}
