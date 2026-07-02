using Godot;

/// <summary>
/// 温馨家居办公室 — 程序化建造的3D房子
/// 车库创业起步，逐步升级到更大办公楼
/// </summary>
public partial class StudioRoom : Node3D
{
    public HouseTier Tier { get; private set; }
    public HouseInfo Info { get; private set; }

    private const float WallThickness = 0.15f;
    private const float DoorWidth = 1.2f;
    private const float DoorHeight = 2.0f;

    /// <summary>工位坐标列表，供外部放置角色模型</summary>
    public System.Collections.Generic.List<Vector3> WorkstationPositions { get; private set; } = new();
    /// <summary>对应每个工位角色的 Y 轴旋转角度（度）</summary>
    public System.Collections.Generic.List<float> WorkstationRotationY { get; private set; } = new();
    private const float WindowWidth = 1.0f;
    private const float WindowHeight = 1.2f;
    private const float WindowBottom = 0.85f;

    public void Setup(HouseTier tier)
    {
        Tier = tier;
        Info = HouseData.Data[tier];

        float w = Info.Size.X, d = Info.Size.Z, h = Info.Size.Y;
        float hw = w / 2, hd = d / 2;

        // ── 平台/地基 ──
        Color platColor = new Color(0.7f, 0.68f, 0.62f);
        AddBox(V3(0, -0.12f, 0), V3(w + 0.4f, 0.15f, d + 0.4f), platColor, 0.8f);
        // ── 木地板 ──
        Color woodColor = new Color(0.65f, 0.55f, 0.4f);
        AddBox(V3(0, 0.05f, 0), V3(w, 0.08f, d), woodColor, 0.85f);
        AddRug(w, d, hw, hd);

        // ── 四面墙壁（无屋顶，上帝视角直接看到室内） ──
        Color wallColor = new Color(0.96f, 0.94f, 0.90f);
        float wh = DoorHeight; // 墙高与门齐平
        BuildWallWithWindows(V3(0, 0, -hd), true, wh);   // 后墙
        BuildWallWithWindows(V3(0, 0, +hd), true, wh);   // 前墙
        BuildWallPlain(V3(-hw, 0, 0), false, wh);         // 左墙
        BuildWallWithDoor(V3(+hw, 0, 0), false, wh);      // 右墙（门）

        // 四角柱——遮盖墙体交接处 D3D12 的 z-fighting 微小缝隙
        AddCornerColumns(hw, hd, wh);

        // ── 室内装饰 ──
        if (Tier == HouseTier.Garage)
            PlaceBedCorner(w, d, hw, hd);
        PlaceWorkstations(w, d, hw, hd);
        PlacePlants(w, d, hw, hd);
        PlaceShelf(w, d, hw, hd);
        PlaceLamp(w, d, hw, hd);
        PlacePoster(w, d, h, hw, hd);
    }

    // ==================== 墙壁建造 ====================

    private void AddCornerColumns(float hw, float hd, float wh)
    {
        Color col = new Color(0.96f, 0.94f, 0.90f);
        float cw = WallThickness;
        float y = wh / 2;
        // 四个内墙角，柱体恰好覆盖两面墙重叠区
        AddBox(V3(-hw, y, -hd), V3(cw, wh, cw), col, 0.7f);
        AddBox(V3(+hw, y, -hd), V3(cw, wh, cw), col, 0.7f);
        AddBox(V3(-hw, y, +hd), V3(cw, wh, cw), col, 0.7f);
        AddBox(V3(+hw, y, +hd), V3(cw, wh, cw), col, 0.7f);
    }

    private void BuildWallWithWindows(Vector3 facePos, bool isZ, float wh)
    {
        float w = Info.Size.X, d = Info.Size.Z;
        float len = isZ ? w : d;
        Color wallC = new Color(0.96f, 0.94f, 0.90f);
        float hLen = len / 2;

        int wc = Mathf.Max(1, Info.WindowCount / 2);
        float winH = Mathf.Min(WindowHeight, wh - WindowBottom - 0.04f);
        float winW = Mathf.Min(WindowWidth * 0.75f, len * 0.15f);
        float winBot = WindowBottom;
        float midY = winBot + winH / 2;
        float f = 0.03f;                                            // 窗框厚度
        float slotW = winW + 0.12f;                                 // 窗户总宽=窗台宽，玻璃/框均以此为准
        float spacing = (len - slotW * wc) / (wc + 1);              // 柱宽

        // 内墙面 Z（所有窗户部件挂在这个面上）
        float zSign = isZ ? Mathf.Sign(facePos.Z) : Mathf.Sign(facePos.X);
        float innerFace = isZ ? facePos.Z - zSign * WallThickness / 2 : facePos.X - zSign * WallThickness / 2;
        float winZ = innerFace + zSign * WallThickness * 0.5f;     // 窗台/玻璃: 内墙面往外移半个墙厚 = 墙中心

        // ===== 第一层：窗户下方整条墙 =====
        if (winBot > 0.03f)
            AddBox(facePos + V3(0, winBot / 2, 0),
                isZ ? V3(len, winBot, WallThickness) : V3(WallThickness, winBot, len), wallC, 0.7f);

        // ===== 第二层：窗户高度区域 =====
        float posX = -hLen;
        Color fc = new Color(0.3f, 0.26f, 0.22f);
        float hw2 = slotW / 2, hh2 = winH / 2;
        for (int i = 0; i < wc; i++)
        {
            // 墙段（柱）
            float cx = posX + spacing / 2;
            AddBox(isZ ? V3(cx, midY, facePos.Z) : V3(facePos.X, midY, cx),
                isZ ? V3(spacing, winH, WallThickness) : V3(WallThickness, winH, spacing), wallC, 0.7f);
            posX += spacing;

            // 窗户
            cx = posX + slotW / 2;
            float wx = isZ ? cx : winZ;
            float wz = isZ ? winZ : cx;

            // 窗台（木头色）
            AddBox(V3(wx, winBot + 0.04f, wz),
                isZ ? V3(slotW, 0.08f, WallThickness) : V3(WallThickness, 0.08f, slotW),
                new Color(0.8f, 0.75f, 0.65f), 0.5f);

            // 窗框（细框围在玻璃外）
            AddBox(V3(wx, winBot + winH + f / 2, wz), isZ ? V3(slotW, f, 0.02f) : V3(0.02f, f, slotW), fc, 0.4f);
            AddBox(V3(wx, winBot - f / 2, wz), isZ ? V3(slotW, f, 0.02f) : V3(0.02f, f, slotW), fc, 0.4f);
            AddBox(V3(wx - hw2 + f / 2, midY, wz), isZ ? V3(f, winH, 0.02f) : V3(0.02f, winH, f), fc, 0.4f);
            AddBox(V3(wx + hw2 - f / 2, midY, wz), isZ ? V3(f, winH, 0.02f) : V3(0.02f, winH, f), fc, 0.4f);

            // 玻璃（和窗台同宽，框包边）
            AddGlassPane(V3(wx, midY, wz), isZ ? V3(slotW - f, winH, 0.01f) : V3(0.01f, winH, slotW - f));
            posX += slotW;
        }
        // 最后一根柱
        float lastW = hLen - posX;
        if (lastW > 0.03f)
        {
            float cx = posX + lastW / 2;
            AddBox(isZ ? V3(cx, midY, facePos.Z) : V3(facePos.X, midY, cx),
                isZ ? V3(lastW, winH, WallThickness) : V3(WallThickness, winH, lastW), wallC, 0.7f);
        }

        // ===== 第三层：窗户上方整条墙 =====
        float topY = winBot + winH;
        float topH = wh - topY;
        if (topH > 0.03f)
            AddBox(facePos + V3(0, topY + topH / 2, 0),
                isZ ? V3(len, topH, WallThickness) : V3(WallThickness, topH, len), wallC, 0.7f);
    }

    private void AddGlassPane(Vector3 pos, Vector3 size)
    {
        var mi = new MeshInstance3D();
        mi.Mesh = new BoxMesh();
        ((BoxMesh)mi.Mesh).Size = size;
        mi.Position = pos;
        var mat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.7f, 0.85f, 1f, 0.15f),
            Roughness = 0.05f,
            Metallic = 0.1f,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha
        };
        mi.MaterialOverride = mat;
        AddChild(mi);
    }

    private void BuildWallPlain(Vector3 facePos, bool isZ, float wh)
    {
        float w = Info.Size.X, d = Info.Size.Z;
        float len = isZ ? w : d;
        Vector3 s = isZ ? V3(len, wh, WallThickness) : V3(WallThickness, wh, len);
        AddBox(facePos + V3(0, wh / 2, 0), s, new Color(0.96f, 0.94f, 0.90f), 0.7f);
    }

    private void BuildWallWithDoor(Vector3 facePos, bool isZ, float wh)
    {
        float w = Info.Size.X, d = Info.Size.Z;
        float len = isZ ? w : d;
        float hLen = len / 2;
        Color wallC = new Color(0.96f, 0.94f, 0.90f);

        // 门上段
        float topH = wh - DoorHeight;
        if (topH > 0.03f)
        {
            Vector3 ts = isZ ? V3(len, topH, WallThickness) : V3(WallThickness, topH, len);
            AddBox(facePos + V3(0, DoorHeight + topH / 2, 0), ts, wallC, 0.7f);
        }

        // 门两侧
        float sideW = hLen - DoorWidth / 2;
        if (sideW > 0.03f)
        {
            Vector3 ss = isZ ? V3(sideW, DoorHeight, WallThickness) : V3(WallThickness, DoorHeight, sideW);
            Vector3 leftP = facePos + V3(0, DoorHeight / 2, 0);
            Vector3 rightP = facePos + V3(0, DoorHeight / 2, 0);
            if (isZ) { leftP.X = -hLen + sideW / 2; rightP.X = hLen - sideW / 2; }
            else { leftP.Z = -hLen + sideW / 2; rightP.Z = hLen - sideW / 2; }
            AddBox(leftP, ss, wallC, 0.7f);
            AddBox(rightP, ss, wallC, 0.7f);
        }

        // 门板 — 与墙面齐平（WallThickness/2 是外墙面，+0.03 使 0.06 厚的门内面恰好贴墙）
        Vector3 ds = isZ ? V3(DoorWidth, DoorHeight, 0.06f) : V3(0.06f, DoorHeight, DoorWidth);
        AddBox(facePos + (isZ ? V3(0, DoorHeight / 2, WallThickness / 2 + 0.03f) : V3(WallThickness / 2 + 0.03f, DoorHeight / 2, 0)), ds, new Color(0.55f, 0.4f, 0.28f), 0.55f);

        // 门把手
        Vector3 knobS = V3(0.07f, 0.07f, 0.07f);
        AddBox(facePos + (isZ ? V3(DoorWidth * 0.3f, DoorHeight * 0.5f, WallThickness / 2 + 0.07f) : V3(WallThickness / 2 + 0.07f, DoorHeight * 0.5f, DoorWidth * 0.3f)), knobS, new Color(0.75f, 0.55f, 0.15f), 0.25f);
    }

    // ==================== 室内照明 ====================

    private void AddIndoorLights(float w, float d)
    {
        float hw = w / 2, hd = d / 2;
        float lightY = 1.2f;

        // 天花中央主灯（白天）
        AddOmniLight(V3(0, lightY, 0), 4f, new Color(1f, 0.99f, 0.96f), w * 0.85f);
        // 四角辅助灯
        float cx = hw * 0.6f, cz = hd * 0.6f;
        float cornerRange = w * 0.5f;
        AddOmniLight(V3(+cx, lightY * 0.7f, +cz), 1.5f, new Color(1f, 0.98f, 0.95f), cornerRange);
        AddOmniLight(V3(-cx, lightY * 0.7f, +cz), 1.5f, new Color(1f, 0.98f, 0.95f), cornerRange);
        AddOmniLight(V3(+cx, lightY * 0.7f, -cz), 1.5f, new Color(1f, 0.98f, 0.95f), cornerRange);
        AddOmniLight(V3(-cx, lightY * 0.7f, -cz), 1.5f, new Color(1f, 0.98f, 0.95f), cornerRange);
    }

    private void AddOmniLight(Vector3 pos, float energy, Color color, float range)
    {
        var light = new OmniLight3D
        {
            Position = pos,
            LightEnergy = energy,
            LightColor = color,
            OmniRange = range,
            LightSize = 0.2f,
            ShadowEnabled = false
        };
        AddChild(light);
    }

    // ==================== 室内装饰 ====================

    private void AddRug(float w, float d, float hw, float hd)
    {
        float rw = w * 0.6f, rd = d * 0.45f;
        AddBox(V3(0, 0.12f, 0), V3(rw, 0.04f, rd), new Color(0.75f, 0.45f, 0.35f), 0.95f);
        // 地毯镶边
        AddBox(V3(0, 0.15f, -rd / 2 + 0.05f), V3(rw - 0.2f, 0.06f, 0.1f), new Color(0.9f, 0.75f, 0.5f), 0.8f);
        AddBox(V3(0, 0.15f, +rd / 2 - 0.05f), V3(rw - 0.2f, 0.06f, 0.1f), new Color(0.9f, 0.75f, 0.5f), 0.8f);
    }

    private void PlaceBedCorner(float w, float d, float hw, float hd)
    {
        // 床放在左后角，贴左墙+后墙
        float bw = 1.6f, bd = 2.0f, bh = 0.35f;
        float bx = -hw + bw / 2 + 0.1f;   // 左边缘贴左墙
        float bz = -hd + bd / 2 + 0.1f;   // 床头贴后墙

        // 床架
        AddBox(V3(bx, bh / 2, bz), V3(bw, bh, bd), new Color(0.55f, 0.42f, 0.3f), 0.6f);
        // 床垫
        AddBox(V3(bx, bh + 0.1f, bz), V3(bw - 0.1f, 0.2f, bd - 0.1f), new Color(0.25f, 0.4f, 0.55f), 0.5f);
        // 枕头（床头侧 = Z-）
        AddBox(V3(bx, bh + 0.25f, bz - bd / 2 + 0.25f), V3(bw - 0.25f, 0.1f, 0.4f), new Color(0.95f, 0.93f, 0.88f), 0.3f);
        // 床头板
        AddBox(V3(bx, bh + 0.35f, bz - bd / 2 - 0.06f), V3(bw, 0.7f, 0.08f), new Color(0.5f, 0.38f, 0.28f), 0.55f);

        // 床头柜 + 台灯（床右侧，靠后墙）
        float ntW = 0.35f, ntD = 0.35f, ntH = 0.5f;
        float ntX = bx + bw / 2 + ntW / 2 + 0.08f;   // 床右边紧挨
        float ntZ = bz - bd / 2 + ntD / 2 + 0.1f;    // 贴后墙
        AddBox(V3(ntX, ntH / 2, ntZ), V3(ntW, ntH, ntD), new Color(0.55f, 0.45f, 0.32f), 0.5f);
        AddBox(V3(ntX, ntH + 0.05f, ntZ), V3(0.12f, 0.35f, 0.12f), new Color(0.9f, 0.85f, 0.78f), 0.2f);
        AddBox(V3(ntX, ntH + 0.22f, ntZ), V3(0.08f, 0.06f, 0.08f), new Color(1f, 0.95f, 0.8f), 0.1f);
    }

    // ==================== 工位 ====================

    private void PlaceWorkstations(float w, float d, float hw, float hd)
    {
        WorkstationPositions.Clear();
        WorkstationRotationY.Clear();
        int cap = Info.Capacity;
        float margin = WallThickness / 2 + 0.25f;

        if (cap <= 2)
        {
            // 小车库：背墙一排 2 个工位
            float deskW = Mathf.Min(1.3f, (w - 1.5f) / 2f);
            float deskD = 0.65f;
            float deskH = 0.72f;
            for (int i = 0; i < cap; i++)
            {
                float dx = -hw * 0.3f + i * hw * 0.6f;
                float dz = -hd + deskD / 2 + 0.12f;
                float chZ = dz + deskD / 2 + 0.2f;  // 椅子中心 Z
                PlaceSingleDesk(dx, deskH, dz, deskW, deskD, i);
                WorkstationPositions.Add(new Vector3(dx, 0, chZ));
                WorkstationRotationY.Add(180f);
            }
        }
        else if (cap <= 5)
        {
            // 小办公室：背墙 3 个 + 右墙 2 个
            int backCount = Mathf.Min(cap, 3);
            int sideCount = cap - backCount;
            float deskW = Mathf.Min(1.2f, (w - 1f) / backCount);
            float deskD = 0.65f;
            float deskH = 0.72f;
            float spacing = (w - deskW * backCount) / (backCount + 1);

            // 背墙（Z-）
            for (int i = 0; i < backCount; i++)
            {
                float dx = -hw + spacing + deskW / 2 + i * (deskW + spacing);
                float dz = -hd + deskD / 2 + 0.12f;
                float chZ = dz + deskD / 2 + 0.2f;  // 椅子中心 Z
                PlaceSingleDesk(dx, deskH, dz, deskW, deskD, i);
                WorkstationPositions.Add(new Vector3(dx, 0, chZ));
                WorkstationRotationY.Add(180f);
            }

            // 右墙（X+）, 每位置 Z 从前到后
            float sdW = 0.85f, sdD = 0.55f, sdH = 0.72f;
            float sSpacing = (d - sdD * sideCount) / (sideCount + 1);
            for (int i = 0; i < sideCount; i++)
            {
                float dz = -hd + sSpacing + sdD / 2 + i * (sdD + sSpacing);
                float dx = hw - sdW / 2 - margin;
                float chX = dx - sdW / 2 - 0.2f;  // 椅子中心 X
                PlaceSingleDeskSide(dx, sdH, dz, sdW, sdD, backCount + i);
                WorkstationPositions.Add(new Vector3(chX, 0, dz));
                WorkstationRotationY.Add(90f); // 面朝 +X（桌子在右）
            }
        }
        else
        {
            // 中大型：背墙一排 + 前排一排（留出过道）
            int rows = Mathf.Min(2 + cap / 12, 4);
            int perRow = Mathf.CeilToInt((float)cap / rows);
            float deskW = Mathf.Min(1.15f, (w - 1.2f) / perRow);
            float deskD = 0.6f;
            float deskH = 0.72f;
            float rowSpacing = Mathf.Min(2.0f, (d - 1.2f) / rows);
            float rowStartZ = -hd + deskD / 2 + 0.15f;
            int placed = 0;

            for (int r = 0; r < rows && placed < cap; r++)
            {
                int inRow = Mathf.Min(perRow, cap - placed);
                float spacing = (w - deskW * inRow) / (inRow + 1);
                float rowDz = rowStartZ + r * rowSpacing;
                for (int i = 0; i < inRow; i++)
                {
                    float dx = -hw + spacing + deskW / 2 + i * (deskW + spacing);
                    float chZ = rowDz + deskD / 2 + 0.2f;  // 椅子中心 Z
                    PlaceSingleDesk(dx, deskH, rowDz, deskW, deskD, placed);
                    WorkstationPositions.Add(new Vector3(dx, 0, chZ));
                    WorkstationRotationY.Add(180f);
                    placed++;
                }
            }
        }
    }

    private void PlaceSingleDesk(float dx, float dH, float dz, float dW, float dD, int idx)
    {
        Color deskColor = new Color(0.6f + idx * 0.06f, 0.48f + idx * 0.04f, 0.35f + idx * 0.03f);
        // 桌面
        AddBox(new Vector3(dx, dH, dz), new Vector3(dW, 0.05f, dD), deskColor, 0.45f);
        // 桌腿 x4
        float legS = 0.05f;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                AddBox(new Vector3(dx + sx * (dW / 2 - 0.06f), dH / 2, dz + sz * (dD / 2 - 0.06f)),
                    new Vector3(legS, dH - 0.05f, legS), new Color(0.3f, 0.25f, 0.2f), 0.4f);

        // 显示器（靠后墙，Z-侧）
        float monZ = dz - dD / 2 + 0.06f;
        AddBox(new Vector3(dx, dH + 0.28f, monZ), new Vector3(dW * 0.55f, 0.42f, 0.05f), new Color(0.15f, 0.15f, 0.18f), 0.25f);
        AddBox(new Vector3(dx, dH + 0.28f, monZ + 0.035f), new Vector3(dW * 0.5f, 0.37f, 0.015f), new Color(0.2f, 0.3f, 0.4f), 0.1f);
        // 键盘
        AddBox(new Vector3(dx, dH + 0.03f, dz + 0.12f), new Vector3(dW * 0.3f, 0.02f, 0.13f), new Color(0.25f, 0.25f, 0.28f), 0.3f);

        // 咖啡杯
        AddBox(new Vector3(dx + dW * 0.22f, dH + 0.05f, dz + 0.18f), new Vector3(0.07f, 0.07f, 0.07f), new Color(0.7f, 0.55f, 0.3f), 0.2f);

        // 办公椅
        float chZ = dz + dD / 2 + 0.2f;
        AddBox(new Vector3(dx, 0.38f, chZ), new Vector3(0.52f, 0.07f, 0.45f), new Color(0.25f, 0.3f, 0.38f), 0.55f);
        AddBox(new Vector3(dx, 0.58f, chZ + 0.2f), new Vector3(0.42f, 0.42f, 0.05f), new Color(0.2f, 0.25f, 0.35f), 0.55f);
        AddBox(new Vector3(dx, 0.22f, chZ), new Vector3(0.07f, 0.42f, 0.07f), new Color(0.3f, 0.32f, 0.35f), 0.35f);
    }

    private void PlaceSingleDeskSide(float dx, float dH, float dz, float dW, float dD, int idx)
    {
        Color deskColor = new Color(0.6f + idx * 0.06f, 0.48f + idx * 0.04f, 0.35f + idx * 0.03f);
        // 桌面
        AddBox(new Vector3(dx, dH, dz), new Vector3(dW, 0.05f, dD), deskColor, 0.45f);
        // 桌腿
        float legS = 0.05f;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                AddBox(new Vector3(dx + sx * (dW / 2 - 0.06f), dH / 2, dz + sz * (dD / 2 - 0.06f)),
                    new Vector3(legS, dH - 0.05f, legS), new Color(0.3f, 0.25f, 0.2f), 0.4f);

        float monX = dx - dW / 2 + 0.06f;
        AddBox(new Vector3(monX, dH + 0.28f, dz), new Vector3(0.05f, 0.42f, dD * 0.55f), new Color(0.15f, 0.15f, 0.18f), 0.25f);
        AddBox(new Vector3(monX + 0.035f, dH + 0.28f, dz), new Vector3(0.015f, 0.37f, dD * 0.5f), new Color(0.2f, 0.3f, 0.4f), 0.1f);

        // 键盘（靠前缘 X+ 侧，靠近椅子）
        float kbX = dx - dW / 4;
        AddBox(new Vector3(kbX, dH + 0.03f, dz + 0.1f), new Vector3(dD * 0.28f, 0.02f, 0.13f), new Color(0.25f, 0.25f, 0.28f), 0.3f);

        float chX = dx - dW / 2 - 0.2f;
        AddBox(new Vector3(chX, 0.38f, dz), new Vector3(0.45f, 0.07f, 0.52f), new Color(0.25f, 0.3f, 0.38f), 0.55f);
        AddBox(new Vector3(chX - 0.2f, 0.58f, dz), new Vector3(0.05f, 0.42f, 0.42f), new Color(0.2f, 0.25f, 0.35f), 0.55f);
        AddBox(new Vector3(chX, 0.22f, dz), new Vector3(0.07f, 0.42f, 0.07f), new Color(0.3f, 0.32f, 0.35f), 0.35f);
    }

    private void PlacePlants(float w, float d, float hw, float hd)
    {
        float margin = WallThickness / 2 + 0.35f; // 保证叶子球不穿墙

        // 右后角盆栽
        float px = hw - margin, pz = -hd + margin;
        AddBox(V3(px, 0.2f, pz), V3(0.25f, 0.4f, 0.25f), new Color(0.65f, 0.45f, 0.3f), 0.5f);
        AddBox(V3(px, 0.45f, pz), V3(0.45f, 0.5f, 0.45f), new Color(0.25f, 0.6f, 0.22f), 0.7f);

        // 右前角盆栽（门附近，往里挪）
        float px2 = hw - margin, pz2 = hd - margin;
        AddBox(V3(px2, 0.15f, pz2), V3(0.18f, 0.3f, 0.18f), new Color(0.6f, 0.4f, 0.25f), 0.5f);
        AddBox(V3(px2, 0.35f, pz2), V3(0.28f, 0.35f, 0.28f), new Color(0.2f, 0.55f, 0.2f), 0.7f);
    }

    private void PlaceShelf(float w, float d, float hw, float hd)
    {
        // 左墙书架
        float sx = -hw + 0.1f, sz = hd - 0.8f;
        float sW = 0.9f, sD = 0.3f;

        for (int i = 0; i < 3; i++)
        {
            float sy = 0.35f + i * 0.45f;
            AddBox(V3(sx, sy, sz), V3(sD, 0.04f, sW), new Color(0.45f, 0.32f, 0.2f), 0.5f);
            // 几本书
            if (i == 1)
            {
                AddBox(V3(sx + 0.1f, sy + 0.07f, sz - 0.1f), V3(0.05f, 0.13f, 0.08f), new Color(0.2f, 0.3f, 0.7f), 0.3f);
                AddBox(V3(sx + 0.16f, sy + 0.09f, sz - 0.08f), V3(0.04f, 0.1f, 0.07f), new Color(0.8f, 0.2f, 0.2f), 0.3f);
            }
            if (i == 2)
            {
                AddBox(V3(sx + 0.1f, sy + 0.06f, sz), V3(0.1f, 0.1f, 0.1f), new Color(0.5f, 0.6f, 0.2f), 0.3f);
            }
        }
    }

    private void PlaceLamp(float w, float d, float hw, float hd)
    {
        // 角落落地灯
        float lx = -hw + 0.3f, lz = hd - 0.3f;
        AddBox(V3(lx, 0.7f, lz), V3(0.08f, 1.4f, 0.08f), new Color(0.35f, 0.3f, 0.25f), 0.4f); // 灯杆
        AddBox(V3(lx, 1.4f, lz), V3(0.35f, 0.4f, 0.35f), new Color(0.92f, 0.88f, 0.78f), 0.2f); // 灯罩
        AddBox(V3(lx, 0.1f, lz), V3(0.25f, 0.2f, 0.25f), new Color(0.4f, 0.35f, 0.28f), 0.5f); // 底座
    }

    private void PlacePoster(float w, float d, float h, float hw, float hd)
    {
        // 左墙内侧贴海报（内嵌墙体，不突出）
        float px = -hw + WallThickness / 2 - 0.005f;   // 墙内表面
        float pz = 0f;                                  // 房间中心Z
        float py = 1.2f;                                // 海报高度
        AddBox(V3(px, py, pz), V3(0.015f, 0.5f, 0.35f), new Color(0.2f, 0.3f, 0.45f), 0.1f);
        // 画框（细木框）
        AddBox(V3(px, py + 0.27f, pz), V3(0.018f, 0.03f, 0.38f), new Color(0.5f, 0.38f, 0.28f), 0.4f);
        AddBox(V3(px, py - 0.27f, pz), V3(0.018f, 0.03f, 0.38f), new Color(0.5f, 0.38f, 0.28f), 0.4f);
        AddBox(V3(px, py, pz + 0.19f), V3(0.018f, 0.5f, 0.03f), new Color(0.5f, 0.38f, 0.28f), 0.4f);
        AddBox(V3(px, py, pz - 0.19f), V3(0.018f, 0.5f, 0.03f), new Color(0.5f, 0.38f, 0.28f), 0.4f);
    }

    // ==================== 工具方法 ====================

    private static Vector3 V3(float x, float y, float z) => new(x, y, z);

    private void AddBox(Vector3 pos, Vector3 size, Color color, float roughness = 0.65f)
    {
        var mi = new MeshInstance3D();
        mi.Mesh = new BoxMesh();
        ((BoxMesh)mi.Mesh).Size = size;
        mi.Position = pos;
        mi.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        // 标准光照材质：roughness 提供磨砂感，metallic=0 无反光
        mi.MaterialOverride = new StandardMaterial3D { AlbedoColor = color, Roughness = roughness, Metallic = 0f };
        AddChild(mi);
    }
}
