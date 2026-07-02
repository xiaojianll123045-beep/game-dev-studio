using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>新手指引 — 开局弹一个简介，其余靠玩家自己探索</summary>
public partial class TutorialManager : Node
{
    private GameManager _gm;
    public bool TutorialCompleted { get; private set; }

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        if (GlobalSettings.TutorialCompleted)
            TutorialCompleted = true;
    }

    public void StartTutorial()
    {
        if (TutorialCompleted) return;
        _gm.ShowPopup(Loc.Tr("tut.welcome"),
            Loc.Tr("tut.welcome_msg"),
            new Color(0.2f, 0.6f, 1f));
        TutorialCompleted = true;
        GlobalSettings.TutorialCompleted = true;
    }

    public void StartIfNewGame() { }
    public void MonthlyTick() { }
    public void SkipAll() { TutorialCompleted = true; GlobalSettings.TutorialCompleted = true; }
    public void ResetTutorial()
    {
        TutorialCompleted = false;
        GlobalSettings.TutorialCompleted = false;
        _gm.ShowPopup(Loc.Tr("tut.skipped_title"), Loc.Tr("tut.skipped_desc"), new Color(0.9f, 0.5f, 0.2f));
    }
    public void LoadProgress(int stepIndex, bool completed)
    {
        if (completed) { TutorialCompleted = true; GlobalSettings.TutorialCompleted = true; }
    }

    // ── 旧接口兼容 ──
    public int CurrentStepIndex { get; set; } = -1;
    public bool CurrentStepCompleted { get; set; }
    public Node ActivePopup { get; set; }
    public void ShowCurrentStep() { }
    public void NotifyAction(string n) { }
    public void DismissPopup() { }
    public void AdvanceStep() { }
    public void RecheckCondition() { }
    public void RefreshPopup() { }
    public List<GameGenre> GetAvailableGenres() => Enum.GetValues<GameGenre>().ToList();
    public List<GameTheme> GetAvailableThemes() => Enum.GetValues<GameTheme>().ToList();
    public bool CanResearchTech => true;
    public bool CanCommercializeEngine => true;
}

// TutorialPopup 引用的旧数据结构
public class TutorialStep
{
    public string Id;
    public string Title;
    public string Description;
    public string[] ActionsToWatch;
    public Func<bool> Condition;
    public string TipHighlight;
}
