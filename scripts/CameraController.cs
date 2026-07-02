using Godot;

/// <summary>3D 相机控制器 — 旋转/平移/缩放</summary>
public partial class CameraController : Node
{
    private GameManager _gm;
    private Camera3D _camera;

    private Vector3 _camTarget = new(0, 1, 0);
    private float _camYaw, _camPitch = Mathf.DegToRad(45), _camDist = 12;
    private float _camRotSensitivity = 0.3f;
    private float _camPanSpeed = 12f;
    private float _camZoomSpeed = 2f;
    private float _camMinDist = 5, _camMaxDist = 40;
    private float _camMinPitch = Mathf.DegToRad(15), _camMaxPitch = Mathf.DegToRad(80);

    private bool _isRotating;
    private Vector2 _lastMouseGlobal;

    public void Init(GameManager gm, Camera3D cam)
    {
        _gm = gm;
        _camera = cam;
    }

    public void HandleInput(InputEvent @event, bool uiOpen)
    {
        var vp = _gm.GetViewport();

        if (uiOpen) return;

        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Right)
            {
                _isRotating = mb.Pressed;
                if (mb.Pressed) _lastMouseGlobal = vp.GetMousePosition();
            }
            if (mb.ButtonIndex == MouseButton.WheelUp)
                _camDist = Mathf.Clamp(_camDist - _camZoomSpeed, _camMinDist, _camMaxDist);
            if (mb.ButtonIndex == MouseButton.WheelDown)
                _camDist = Mathf.Clamp(_camDist + _camZoomSpeed, _camMinDist, _camMaxDist);
        }

        if (@event is InputEventMouseMotion && _isRotating)
        {
            var m = vp.GetMousePosition();
            var d = m - _lastMouseGlobal;
            _lastMouseGlobal = m;
            _camYaw -= d.X * _camRotSensitivity * 0.01f;
            _camPitch += d.Y * _camRotSensitivity * 0.01f;
            _camPitch = Mathf.Clamp(_camPitch, _camMinPitch, _camMaxPitch);
        }
    }

    public void UpdatePosition()
    {
        float y = Mathf.Sin(_camPitch) * _camDist;
        float xz = Mathf.Cos(_camPitch) * _camDist;
        _camera.Position = _camTarget + new Vector3(Mathf.Sin(_camYaw) * xz, y, Mathf.Cos(_camYaw) * xz);
        _camera.LookAt(_camTarget);
    }

    public void HandleKeys(float delta)
    {
        var focus = _gm.GetViewport().GuiGetFocusOwner();
        if (focus is LineEdit || focus is TextEdit) return;
        float s = _camPanSpeed * delta;
        if (Input.IsKeyPressed(Key.W)) { var f = _camera.GlobalBasis.Z; f.Y = 0; _camTarget -= f * s; }
        if (Input.IsKeyPressed(Key.S)) { var f = _camera.GlobalBasis.Z; f.Y = 0; _camTarget += f * s; }
        if (Input.IsKeyPressed(Key.A)) { var r = _camera.GlobalBasis.X; r.Y = 0; _camTarget -= r * s; }
        if (Input.IsKeyPressed(Key.D)) { var r = _camera.GlobalBasis.X; r.Y = 0; _camTarget += r * s; }
    }
}
