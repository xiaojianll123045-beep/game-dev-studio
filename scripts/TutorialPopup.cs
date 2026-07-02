using System;
using Godot;

/// <summary>
/// 可拖动的新手引导弹窗。
/// 高度自适应：Label 入树完成自动换行后，用 GetContentHeight 读取真实高度。
/// </summary>
public partial class TutorialPopup : Panel
{
    private TutorialManager _tutorialMgr;
    private TutorialStep _step;
    private int _stepNum;
    private int _totalSteps;
    private bool _stepCompleted;

    private Button _nextBtn;
    private Button _dismissBtn;
    private Label _titleLabel;
    private Label _descLabel;
    private Label _progressLabel;
    private Label _hintLabel;

    private bool _dragging;
    private Vector2 _dragOffset;
    private Control _titleBar;
    private Vector2 _vpSize;
    private float _uiScale;
    private bool _sized;

    private float _descW, _hintH, _pad, _gap, _btnH, _titleH, _progressH;

    public TutorialPopup(TutorialManager mgr, TutorialStep step, int stepNum, int totalSteps, bool completed)
    {
        _tutorialMgr = mgr;
        _step = step;
        _stepNum = stepNum;
        _totalSteps = totalSteps;
        _stepCompleted = completed;

        _vpSize = mgr.GetViewport().GetVisibleRect().Size;
        _uiScale = GlobalSettings.UIScale;

        MouseFilter = MouseFilterEnum.Ignore;
        SetupUI();
        RefreshState(_stepCompleted);
    }

    private int _pendingFrames = 3; // 1帧入树 + 1帧布局 + 1帧确保

    public override void _Ready()
    {
        // Timer 0 秒 = 下一空闲帧，但 Label 的 line count 可能还没更新
        // 再多等一帧确保测量准确
    }

    private void ResizeToContent()
    {
        if (_sized) return;
        float S(float v) => v * _uiScale;

        // 直接读 Label 入树换行后的实际最小高度
        float descH = _descLabel.GetCombinedMinimumSize().Y + S(8);
        descH = Mathf.Max(descH, S(40));
        descH = Mathf.Min(descH, _vpSize.Y * 0.55f);

        float ph = _titleH + _pad + _gap + _progressH + _gap + descH + _gap + S(6) + _hintH + _gap + _btnH + _pad;
        ph = Mathf.Min(ph, _vpSize.Y * 0.75f);
        Position = new((_vpSize.X - S(420)) / 2, (_vpSize.Y - ph) / 2);
        Size = new(S(420), ph);

        // 修正描述 Label 高度
        _descLabel.Size = new(_descW, descH);

        // 重新定位底部元素
        float y = _titleH + _pad + _gap + _progressH + _gap + descH + _gap + S(6);
        _hintLabel.Position = new(_pad, y);
        _dismissBtn.Position = new(_pad, y + _hintH + _gap);
        _nextBtn.Position = new(S(420) - _pad - S(120), y + _hintH + _gap);
        _sized = true;
    }

    private void SetupUI()
    {
        float S(float v) => v * _uiScale;

        float pw = S(420);
        _pad = S(18);
        _titleH = S(30);
        _gap = S(10);
        _btnH = S(34);
        float closeBtnW = S(24);
        _descW = pw - _pad * 2;
        _hintH = S(22);
        _progressH = S(18);

        // 初始估算高度（布局后会修正）
        float initPh = S(300);
        Size = new(pw, initPh);
        Position = new((_vpSize.X - pw) / 2, (_vpSize.Y - initPh) / 2);

        AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.96f, 0.95f, 0.92f, 0.98f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.2f, 0.55f, 0.9f, 0.6f),
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10
        });
        ClipContents = false;

        // ── 标题栏 ──
        _titleBar = new Panel
        {
            Position = new(0, 0),
            Size = new(pw, _titleH + _pad),
            MouseFilter = MouseFilterEnum.Stop
        };
        _titleBar.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.18f, 0.42f, 0.75f, 0.9f),
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 0, CornerRadiusBottomRight = 0
        });
        AddChild(_titleBar);

        _titleLabel = new Label
        {
            Text = _step.Title ?? "",
            Position = new(_pad, S(5)),
            Size = new(pw - _pad - closeBtnW - S(16), _titleH)
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 16);
        _titleLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        _titleLabel.ClipText = true;
        _titleBar.AddChild(_titleLabel);

        var closeBtn = new Button
        {
            Text = "\u00d7",
            Position = new(pw - closeBtnW - S(10), S(5)),
            Size = new(closeBtnW, closeBtnW),
            Flat = true,
        };
        closeBtn.AddThemeFontSizeOverride("font_size", 18);
        closeBtn.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.85f));
        closeBtn.AddThemeColorOverride("font_hover_color", new Color(1f, 0.4f, 0.4f));
        closeBtn.Pressed += () => _tutorialMgr.DismissPopup();
        _titleBar.AddChild(closeBtn);

        float y = _titleH + _pad + _gap;

        // ── 进度 ──
        _progressLabel = new Label
        {
            Text = $"{Loc.Tr("tut.step_prefix")} {_stepNum}/{_totalSteps}",
            Position = new(_pad, y),
            Size = new(pw - _pad * 2, _progressH)
        };
        _progressLabel.AddThemeFontSizeOverride("font_size", 12);
        _progressLabel.AddThemeColorOverride("font_color", new Color(0.18f, 0.42f, 0.75f));
        AddChild(_progressLabel);

        y += _progressH + _gap;

        // ── 描述文本（初始给足高度让它完成换行渲染，后续 ResizeToContent 精确修正）──
        float initDescH = S(500);
        _descLabel = new Label
        {
            Text = _step.Description ?? "",
            Position = new(_pad, y),
            Size = new(_descW, initDescH),
        };
        _descLabel.AddThemeFontSizeOverride("font_size", 13);
        _descLabel.AddThemeColorOverride("font_color", new Color(0.10f, 0.14f, 0.22f));
        _descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _descLabel.ClipContents = false;
        AddChild(_descLabel);

        y += initDescH + _gap; // 提示和按钮放远一点避免初始重叠

        // ── 提示文字 ──
        _hintLabel = new Label
        {
            Position = new(_pad, y),
            Size = new(_descW, _hintH)
        };
        _hintLabel.AddThemeFontSizeOverride("font_size", 11);
        _hintLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.5f, 0.1f));
        AddChild(_hintLabel);

        y += _hintH + _gap;

        // ── 按钮 ──
        _dismissBtn = new Button
        {
            Text = Loc.Tr("tut.btn_skip"),
            Position = new(_pad, y),
            Size = new(S(80), _btnH),
            Flat = true,
        };
        _dismissBtn.AddThemeFontSizeOverride("font_size", 12);
        _dismissBtn.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
        _dismissBtn.AddThemeColorOverride("font_hover_color", new Color(0.7f, 0.3f, 0.3f));
        _dismissBtn.Pressed += () => _tutorialMgr.SkipAll();
        AddChild(_dismissBtn);

        _nextBtn = new Button
        {
            Text = Loc.Tr("tut.btn_next"),
            Position = new(pw - _pad - S(120), y),
            Size = new(S(120), _btnH),
        };
        _nextBtn.AddThemeFontSizeOverride("font_size", 14);
        _nextBtn.Pressed += () => _tutorialMgr.AdvanceStep();
        AddChild(_nextBtn);

        _titleBar.GuiInput += OnTitleBarInput;
    }

    public void RefreshState(bool completed)
    {
        _stepCompleted = completed;
        _nextBtn.Disabled = !completed;

        if (completed)
        {
            _nextBtn.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
            _nextBtn.AddThemeStyleboxOverride("normal", new StyleBoxFlat
            {
                BgColor = new Color(0.2f, 0.65f, 0.35f),
                CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
                CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
            });
            _nextBtn.AddThemeStyleboxOverride("hover", new StyleBoxFlat
            {
                BgColor = new Color(0.25f, 0.75f, 0.4f),
                CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
                CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
            });
            _hintLabel.Text = Loc.Tr("tut.step_done");
            _hintLabel.AddThemeColorOverride("font_color", new Color(0.13f, 0.55f, 0.13f));
        }
        else
        {
            _nextBtn.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
            _nextBtn.AddThemeStyleboxOverride("normal", new StyleBoxFlat
            {
                BgColor = new Color(0.75f, 0.75f, 0.75f),
                CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
                CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
            });
            _nextBtn.AddThemeStyleboxOverride("hover", new StyleBoxFlat
            {
                BgColor = new Color(0.75f, 0.75f, 0.75f),
                CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
                CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
            });
            _hintLabel.Text = Loc.Tr("tut.step_waiting");
            _hintLabel.AddThemeColorOverride("font_color", new Color(0.72f, 0.53f, 0.04f));
        }
    }

    private void OnTitleBarInput(InputEvent ie)
    {
        if (ie is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed) { _dragging = true; _dragOffset = GetGlobalMousePosition() - GlobalPosition; }
                else _dragging = false;
            }
        }
    }

    public override void _Process(double delta)
    {
        var args = ModMethodOverride.Args(("delta", delta));
        ModMethodOverride.CallVoid("tutorialpopup_process", args, () =>
        {
            if (_dragging)
                GlobalPosition = GetGlobalMousePosition() - _dragOffset;

            if (_pendingFrames > 0)
            {
                _pendingFrames--;
                if (_pendingFrames == 0)
                    ResizeToContent();
            }

            var p = GetParentOrNull<CanvasItem>();
            if (p != null && p.GetChildCount() > 0 && p.GetChild(p.GetChildCount() - 1) != this)
                p.MoveChild(this, p.GetChildCount() - 1);

            if (!_stepCompleted && _tutorialMgr.CurrentStepCompleted)
            {
                _stepCompleted = true;
                RefreshState(true);
            }
        });
    }
}
