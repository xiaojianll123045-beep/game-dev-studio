using Godot;

/// <summary>
/// 可拖动、可缩放的 Panel。
/// 拖拽区域：顶部 36px（避开按钮等交互控件）。
/// 缩放：拖拽边缘和四角。
/// </summary>
public partial class DragPanel : Panel
{
    /// <summary>设为 true 后，任何外部 CloseAll/PopTopPanel 都不会关闭此面板</summary>
    public bool Protected { get; set; }

    private bool _dragging;
    private Vector2 _dragOffset;
    private bool _resizing;
    private Vector2 _resizeStartSize;
    private Vector2 _resizeStartPos;
    private Vector2 _resizeStartMouse;
    private int _resizeEdge;

    private const float ResizeMargin = 6f;
    private const float DragBarHeight = 36f;
    private static readonly Vector2 MinSize = new(300, 200);

    private float _scale = 1f;
    private bool _ready;

    public void SetScale(float s) => _scale = s;

    /// <summary>自定义拖拽条高度（默认36）</summary>
    public float DragZoneHeight { get; set; } = 36f;

    public override void _Ready()
    {
        _ready = true;
        MouseFilter = MouseFilterEnum.Stop;
        GuiInput += OnPanelInput;
    }

    private void BringToFront()
    {
        var p = GetParent();
        if (p != null && p.GetChildCount() > 0 && p.GetChild(p.GetChildCount() - 1) != this)
            p.MoveChild(this, p.GetChildCount() - 1);
    }

    private void OnPanelInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                var localPos = GetLocalMousePosition();

                // 边缘缩放优先
                _resizeEdge = GetResizeEdge(localPos);
                if (_resizeEdge != 0)
                {
                    _resizing = true;
                    _resizeStartSize = Size;
                    _resizeStartPos = GlobalPosition;
                    _resizeStartMouse = GetGlobalMousePosition();
                    BringToFront();
                    AcceptEvent();
                    return;
                }

                // 顶部区域拖拽
                float barH = DragZoneHeight * _scale;
                if (localPos.Y >= 0 && localPos.Y <= barH)
                {
                    _dragging = true;
                    _resizing = false;
                    _dragOffset = GetGlobalMousePosition() - GlobalPosition;
                    BringToFront();
                    AcceptEvent();
                }
            }
            else
            {
                _dragging = false;
                _resizing = false;
                _resizeEdge = 0;
            }
        }
    }

    private int GetResizeEdge(Vector2 localPos)
    {
        var s = Size / _scale;
        if (localPos.X < 0 || localPos.X > s.X || localPos.Y < 0 || localPos.Y > s.Y)
            return 0;
        int edge = 0;
        if (localPos.X <= ResizeMargin) edge |= 1;
        if (localPos.X >= s.X - ResizeMargin) edge |= 2;
        if (localPos.Y <= ResizeMargin) edge |= 4;
        if (localPos.Y >= s.Y - ResizeMargin) edge |= 8;
        return edge;
    }

    private static CursorShape GetEdgeCursor(int edge)
    {
        return edge switch
        {
            1 | 4 or 2 | 8 => CursorShape.Fdiagsize,
            2 | 4 or 1 | 8 => CursorShape.Bdiagsize,
            1 or 2 => CursorShape.Hsize,
            4 or 8 => CursorShape.Vsize,
            _ => CursorShape.Arrow
        };
    }

    public override void _Process(double delta)
    {
        var args = ModMethodOverride.Args(("delta", delta));
        ModMethodOverride.CallVoid("dragpanel_process", args, () =>
        {
            if (!_ready) return;

            var vpSize = GetViewport().GetVisibleRect().Size;
            var mousePos = GetGlobalMousePosition();

            if (!_resizing && !_dragging)
            {
                var localPos = GetLocalMousePosition();
                float sX = Size.X / _scale, sY = Size.Y / _scale;
                bool mouseInside = localPos.X >= 0 && localPos.X <= sX && localPos.Y >= 0 && localPos.Y <= sY;
                MouseDefaultCursorShape = mouseInside ? GetEdgeCursor(GetResizeEdge(localPos)) : CursorShape.Arrow;
            }

            if (_dragging)
            {
                var newPos = mousePos - _dragOffset;
                newPos.X = Mathf.Clamp(newPos.X, 0, vpSize.X - Size.X);
                newPos.Y = Mathf.Clamp(newPos.Y, 0, vpSize.Y - Size.Y);
                GlobalPosition = newPos;
            }

            if (_resizing)
            {
                var mouseDelta = mousePos - _resizeStartMouse;
                var newSize = _resizeStartSize;
                var newPos = _resizeStartPos;

                if ((_resizeEdge & 2) != 0)
                    newSize.X = Mathf.Max(_resizeStartSize.X + mouseDelta.X, MinSize.X);
                if ((_resizeEdge & 8) != 0)
                    newSize.Y = Mathf.Max(_resizeStartSize.Y + mouseDelta.Y, MinSize.Y);
                if ((_resizeEdge & 1) != 0)
                {
                    float dx = Mathf.Min(mouseDelta.X, _resizeStartSize.X - MinSize.X);
                    newSize.X = _resizeStartSize.X - dx;
                    newPos.X = _resizeStartPos.X + dx;
                }
                if ((_resizeEdge & 4) != 0)
                {
                    float dy = Mathf.Min(mouseDelta.Y, _resizeStartSize.Y - MinSize.Y);
                    newSize.Y = _resizeStartSize.Y - dy;
                    newPos.Y = _resizeStartPos.Y + dy;
                }

                Size = newSize;
                GlobalPosition = newPos;
            }
        });
    }

    public override void _ExitTree()
    {
        if (_ready)
        {
            GuiInput -= OnPanelInput;
        }
    }
}
