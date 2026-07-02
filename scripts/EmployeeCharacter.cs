using Godot;

/// <summary>
/// 3D 员工角色模型 — 纯原始体搭建的小人
/// 坐在办公桌前工作的员工形象
/// 单击打开员工详情菜单
/// </summary>
public partial class EmployeeCharacter : Node3D
{
    public Employee Employee { get; set; }

    private MeshInstance3D _handL;
    private MeshInstance3D _handR;
    private Vector3 _handBaseL, _handBaseR;
    private float _animPhase;

    // 头顶贡献球
    private MeshInstance3D _progBall;   // 蓝色程序球
    private MeshInstance3D _artBall;    // 橙色美术球
    private float _progBallTimer;       // 球显示计时器
    private float _artBallTimer;
    private Color _progColor = new(0.25f, 0.55f, 1.0f);
    private Color _artColor = new(1.0f, 0.55f, 0.15f);

    private static readonly Color[] ShirtColors = {
        new(0.2f, 0.45f, 0.75f),  // 蓝
        new(0.75f, 0.25f, 0.25f),  // 红
        new(0.2f, 0.65f, 0.3f),   // 绿
        new(0.7f, 0.5f, 0.15f),   // 橙
        new(0.55f, 0.25f, 0.65f),  // 紫
        new(0.15f, 0.55f, 0.6f),   // 青
        new(0.7f, 0.65f, 0.15f),   // 黄
        new(0.5f, 0.35f, 0.6f),    // 粉紫
    };

    public void Build(Employee emp)
    {
        Employee = emp;
        var shirt = ShirtColors[emp.Id % ShirtColors.Length];
        var skin = new Color(1f, 0.85f, 0.7f);
        var pants = new Color(0.15f, 0.15f, 0.25f);
        var shoes = new Color(0.1f, 0.08f, 0.08f);
        var hair = new Color(0.1f, 0.08f, 0.06f);

        // 坐姿坐标约定：
        // Y=0 = 椅面（世界 Y=0.38）
        // +Z = 正面，朝桌子（角色最终会旋转 180° 使 +Z → 世界 -Z）
        // 骨骼从下往上搭，prevent clipping
        //   骨盆 → 躯干 → 肩 → 头
        //   大腿(贴椅面) → 小腿(下垂) → 脚
        //   上臂(下垂) → 前臂(前伸) → 手

        // ═══════════ 大腿 — 贴椅面，水平前伸 ═══════════
        float thighCY = 0.03f;         // 大腿中心 Y（略高于椅面）
        float thighCZ = 0.13f;         // 大腿中心 Z
        float thighSY = 0.06f;         // 大腿高（扁）
        float thighSZ = 0.22f;         // 大腿长（前后）
        float thighSX = 0.08f;         // 大腿宽
        AddBox(V3(0.06f, thighCY, thighCZ), V3(thighSX, thighSY, thighSZ), pants);
        AddBox(V3(-0.06f, thighCY, thighCZ), V3(thighSX, thighSY, thighSZ), pants);

        // ═══════════ 小腿 — 从大腿前缘垂下 ═══════════
        float calfCZ = thighCZ + thighSZ / 2 - 0.03f;  // 大腿前沿
        float calfCY = -0.13f;          // 小腿中心 Y
        float calfSY = 0.16f;           // 小腿长
        float calfSX = 0.07f;
        float calfSZ = 0.07f;
        AddBox(V3(0.06f, calfCY, calfCZ), V3(calfSX, calfSY, calfSZ), pants);
        AddBox(V3(-0.06f, calfCY, calfCZ), V3(calfSX, calfSY, calfSZ), pants);

        // ═══════════ 鞋 ═══════════
        float shoeCY = calfCY - calfSY / 2 - 0.02f;  // -0.13 - 0.08 - 0.02 = -0.23
        float shoeCZ = calfCZ + 0.04f;
        AddBox(V3(0.06f, shoeCY, shoeCZ), V3(0.08f, 0.04f, 0.11f), shoes);
        AddBox(V3(-0.06f, shoeCY, shoeCZ), V3(0.08f, 0.04f, 0.11f), shoes);

        // ═══════════ 躯干 — 骨盆之上 ═══════════
        float torsoCY = 0.21f;           // 躯干中心 Y
        float torsoSY = 0.32f;           // 躯干高
        float torsoSX = 0.26f;
        float torsoSZ = 0.14f;
        AddBox(V3(0, torsoCY, 0), V3(torsoSX, torsoSY, torsoSZ), shirt);

        // ═══════════ 头 ═══════════
        float headR = 0.11f;
        float headCY = torsoCY + torsoSY / 2 + headR - 0.01f;  // 0.21 + 0.16 + 0.11 - 0.01 = 0.47
        AddSphere(V3(0, headCY, 0.01f), headR, skin);

        // 眼睛 (在头的前表面 Z+)
        float eyeY = headCY + 0.02f;
        float eyeZ = headR - 0.01f;
        AddBox(V3(-0.035f, eyeY, eyeZ), V3(0.022f, 0.022f, 0.015f), Colors.Black);
        AddBox(V3(0.035f, eyeY, eyeZ), V3(0.022f, 0.022f, 0.015f), Colors.Black);

        // 头发
        float hairCY = headCY + headR - 0.01f;
        AddBox(V3(0, hairCY, -0.02f), V3(0.22f, 0.05f, 0.14f), hair);

        // ═══════════ 上臂 — 肩→肘，紧贴躯干侧面 ═══════════
        // 肩: (±0.17, 0.35, 0.05)  肘: (±0.17, 0.22, 0.05)
        float uaCX = 0.17f;
        float uaLen = 0.14f;          // Y方向长
        float uaCY = 0.35f - uaLen / 2; // 上臂中心 = 0.28
        float uaCZ = 0.05f;
        // Y范围: 0.21~0.35   Z范围: 0.015~0.085
        AddBox(V3(-uaCX, uaCY, uaCZ), V3(0.07f, uaLen, 0.07f), shirt);
        AddBox(V3(uaCX, uaCY, uaCZ), V3(0.07f, uaLen, 0.07f), shirt);

        // ═══════════ 肘关节小球 ═══════════
        AddSphere(V3(-uaCX, 0.22f, 0.05f), 0.035f, shirt);
        AddSphere(V3(uaCX, 0.22f, 0.05f), 0.035f, shirt);

        // ═══════════ 前臂 — 肘→手，斜前伸搭键盘 ═══════════
        // 肘: (±0.17, 0.22, 0.05)  手: (±0.17, 0.35, 0.30)
        // 前臂中心 Y=(0.22+0.35)/2=0.285  Z=(0.05+0.30)/2=0.175
        float faCY = 0.28f;
        float faCZ = 0.16f;
        float faLen = 0.26f;          // Z方向长
        // Y范围: 0.22~0.34   Z范围: 0.03~0.29
        // 与上臂重叠: Y 0.22~0.35 ∩ 0.22~0.34 = 0.22~0.34 ✓
        //              Z 0.015~0.085 ∩ 0.03~0.29 = 0.03~0.085 ✓
        AddBox(V3(-uaCX, faCY, faCZ), V3(0.06f, 0.12f, faLen), shirt);
        AddBox(V3(uaCX, faCY, faCZ), V3(0.06f, 0.12f, faLen), shirt);

        // ═══════════ 手 — 搭键盘 ═══════════
        float handY = 0.35f;          // 键盘面 Y≈0.37
        float handZ = 0.32f;          // 键盘 Z≈0.40, 手球在前缘略后
        _handL = AddSphereRet(V3(-uaCX + 0.01f, handY, handZ), 0.035f, skin);
        _handR = AddSphereRet(V3(uaCX - 0.01f, handY, handZ), 0.035f, skin);
        _handBaseL = _handL.Position;
        _handBaseR = _handR.Position;
        _animPhase = Employee == null ? 0f : (float)(Employee.Id * 1.7);

        // 创始人标识
        if (emp.IsCaptain && emp.GetHighestLevel() >= 5)
        {
            AddBox(V3(0.11f, headCY + 0.06f, 0.06f), V3(0.03f, 0.03f, 0.03f), new Color(1f, 0.85f, 0.2f));
        }

        // 头顶贡献球（程序蓝/美术橙）
        float ballY = headCY + headR + 0.06f;
        _progBall = AddSphereRet(V3(-0.07f, ballY, 0.01f), 0.04f, _progColor);
        _artBall = AddSphereRet(V3(0.07f, ballY, 0.01f), 0.04f, _artColor);
        _progBall.Visible = false;
        _artBall.Visible = false;

        // ── 可点击区域：包围盒覆盖整个小人 ──
        var area = new Area3D();
        area.Name = "_clickArea";
        area.CollisionLayer = 1;
        area.CollisionMask = 0;
        var col = new CollisionShape3D();
        col.Shape = new BoxShape3D { Size = new Vector3(0.5f, 0.7f, 0.5f) };
        col.Position = new Vector3(0, 0.25f, 0.1f);
        area.AddChild(col);
        area.InputEvent += (cam, evt, pos, normal, shapeIdx) =>
        {
            if (evt is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
            {
                var gm = GetNodeOrNull<GameManager>("/root/Main");
                gm?.OnCharacterClicked(Employee);
            }
        };
        AddChild(area);
    }

    private Vector3 V3(float x, float y, float z) => new(x, y, z);

    public override void _Process(double delta)
    {
        var args = ModMethodOverride.Args(("delta", delta));
        ModMethodOverride.CallVoid("employeechar_process", args, () =>
        {
            if (_handL == null || _handR == null) return;
            float t = (float)Time.GetTicksMsec() / 1000f + _animPhase;

            float dl = Mathf.Sin(t * 6.0f) * 0.012f;
            float dr = Mathf.Sin(t * 6.0f + Mathf.Pi) * 0.012f;
            _handL.Position = _handBaseL + new Vector3(0, dl, 0);
            _handR.Position = _handBaseR + new Vector3(0, dr, 0);

            float dt = (float)delta;
            float ballBaseY = 0.60f;

            if (Employee?.LastProgContrib > 0.01f)
                _progBallTimer = 2.5f;
            _progBallTimer -= dt;
            if (_progBallTimer > 0 && Employee != null)
            {
                float fade = Mathf.Min(1f, _progBallTimer / 0.5f);
                float scale = Mathf.Clamp(Employee.LastProgContrib * 0.03f, 0.03f, 0.10f);
                float bob = Mathf.Sin(t * 4f) * 0.015f;
                _progBall.Visible = true;
                _progBall.Scale = Vector3.One * scale * fade;
                _progBall.Position = new Vector3(-0.07f, ballBaseY + bob, 0.01f);
            }
            else
            {
                _progBall.Visible = false;
            }

            if (Employee?.LastArtContrib > 0.01f)
                _artBallTimer = 2.5f;
            _artBallTimer -= dt;
            if (_artBallTimer > 0 && Employee != null)
            {
                float fade = Mathf.Min(1f, _artBallTimer / 0.5f);
                float scale = Mathf.Clamp(Employee.LastArtContrib * 0.03f, 0.03f, 0.10f);
                float bob = Mathf.Sin(t * 4f + Mathf.Pi) * 0.015f;
                _artBall.Visible = true;
                _artBall.Scale = Vector3.One * scale * fade;
                _artBall.Position = new Vector3(0.07f, ballBaseY + bob, 0.01f);
            }
            else
            {
                _artBall.Visible = false;
            }
        });
    }

    private void AddBox(Vector3 pos, Vector3 size, Color color)
    {
        var mi = new MeshInstance3D();
        mi.Mesh = new BoxMesh();
        ((BoxMesh)mi.Mesh).Size = size;
        mi.Position = pos;
        mi.MaterialOverride = new StandardMaterial3D { AlbedoColor = color, Roughness = 0.6f };
        AddChild(mi);
    }

    private void AddSphere(Vector3 pos, float radius, Color color)
    {
        _ = AddSphereRet(pos, radius, color);
    }

    private MeshInstance3D AddSphereRet(Vector3 pos, float radius, Color color)
    {
        var mi = new MeshInstance3D();
        mi.Mesh = new SphereMesh();
        ((SphereMesh)mi.Mesh).Radius = radius;
        ((SphereMesh)mi.Mesh).Height = radius * 2;
        ((SphereMesh)mi.Mesh).RadialSegments = 12;
        ((SphereMesh)mi.Mesh).Rings = 8;
        mi.Position = pos;
        mi.MaterialOverride = new StandardMaterial3D { AlbedoColor = color, Roughness = 0.4f };
        AddChild(mi);
        return mi;
    }
}
