using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class RoomManager : Node
{
    public HouseTier CurrentTier { get; set; } = HouseTier.Garage;
    public HashSet<BonusRoom> PurchasedBonusRooms { get; private set; } = new();

    public float TotalMonthlyRent { get; private set; }
    public int TotalCapacity => HouseData.Data[CurrentTier].Capacity + PurchasedBonusRooms.Sum(r => BonusRoomData.Data[r].Capacity);

    private Node3D _building;
    private GameManager _gm;
    private ResourceManager _res;
    private EmployeeManager _empMgr;
    private StudioRoom _house;

    private List<EmployeeCharacter> _characters = new();

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        _res = GetNode<ResourceManager>("../ResourceManager");
        _empMgr = GetNode<EmployeeManager>("../EmployeeManager");
    }

    public void InitStartingHouse()
    {
        BuildHouse(CurrentTier);
    }

    private void BuildHouse(HouseTier tier)
    {
        // 直接从场景树获取 Building（避免 _Ready 时序问题）
        _building = _gm.BuildingNode;
        if (_building == null) { GD.PrintErr("RoomManager: BuildingNode is null!"); return; }

        // 清除旧房子
        if (_house != null)
        {
            _house.QueueFree();
            _house = null;
        }

        _house = new StudioRoom();
        _building.AddChild(_house);
        _house.Position = Vector3.Zero;
        _house.Setup(tier);
        SpawnEmployees();
        UpdateTotalRent();
        _gm.AdjustCameraToHouse(HouseData.Data[tier].Size);
    }

    /// <summary>
    /// 花钱搬家到更大的房子
    /// </summary>
    public bool MoveToBiggerHouse()
    {
        if (!HouseData.CanUpgrade(CurrentTier)) return false;
        ModAPI.FireHooks(ModAPI.GameHook.BeforeOfficeUpgrade);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeOfficeUpgrade)) return false;
        var nextTier = HouseData.NextTier(CurrentTier);
        var info = HouseData.Data[nextTier];

        if (!_res.SpendMoney(info.MoveCost, "rent")) return false;

        CurrentTier = nextTier;
        BuildHouse(CurrentTier);
        _gm.GetNodeOrNull<TutorialManager>("TutorialManager")?.NotifyAction("office_upgraded");
        ModAPI.FireHooks(ModAPI.GameHook.AfterOfficeUpgrade);
        return true;
    }

    /// <summary>
    /// 购买额外房间
    /// </summary>
    public bool BuyBonusRoom(BonusRoom type)
    {
        if (PurchasedBonusRooms.Contains(type)) return false;
        ModAPI.FireHooks(ModAPI.GameHook.BeforeOfficeUpgrade);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeOfficeUpgrade)) return false;
        var info = BonusRoomData.Data[type];
        if (!_res.SpendMoney(info.Cost, "rent")) return false;

        PurchasedBonusRooms.Add(type);
        UpdateTotalRent();
        ModAPI.FireHooks(ModAPI.GameHook.AfterOfficeUpgrade);
        return true;
    }

    public void UpdateTotalRent()
    {
        TotalMonthlyRent = HouseData.Data[CurrentTier].MonthlyRent
            + PurchasedBonusRooms.Sum(r => BonusRoomData.Data[r].MonthlyRent);
    }

    public void PayMonthlyRent()
    {
        _res.SpendMoney(TotalMonthlyRent, "rent");
    }

    public float GetBonusForSkill(SkillType skillType)
    {
        float bonus = 0;
        if (PurchasedBonusRooms.Contains(BonusRoom.ArtStudio) && skillType == SkillType.Art)
            bonus += 0.15f;
        if (PurchasedBonusRooms.Contains(BonusRoom.AudioLab) && skillType == SkillType.Audio)
            bonus += 0.15f;
        return bonus;
    }

    public float GetResearchSpeedBonus()
    {
        return PurchasedBonusRooms.Contains(BonusRoom.ServerRoom) ? 0.1f : 0;
    }

    public float GetChemistryBonus()
    {
        return PurchasedBonusRooms.Contains(BonusRoom.MeetingRoom) ? 0.1f : 0;
    }

    public float GetFatigueRecoveryBonus()
    {
        return PurchasedBonusRooms.Contains(BonusRoom.Lounge) ? 0.2f : 0;
    }

    /// <summary>读档后重建房屋+角色</summary>
    public void ReplayHouse(HouseTier tier)
    {
        _building = _gm.BuildingNode;
        if (_building == null) { GD.PrintErr("ReplayHouse: BuildingNode null!"); return; }

        // 清除旧子节点
        foreach (var c in _building.GetChildren()) c.QueueFree();
        _house = new StudioRoom();
        _building.AddChild(_house);
        _house.Setup(tier);
        RefreshEmployees();
    }

    // ==================== 员工角色生成 ====================

    /// <summary>建房后刷新所有员工角色</summary>
    public void SpawnEmployees()
    {
        if (_house == null || _empMgr == null || _building == null) return;

        // 清除旧角色
        foreach (var ch in _characters)
            ch.QueueFree();
        _characters.Clear();

        var positions = _house.WorkstationPositions;
        var rotations = _house.WorkstationRotationY;
        var employees = _empMgr.Employees;
        int count = Mathf.Min(employees.Count, positions.Count);

        for (int i = 0; i < count; i++)
        {
            var pos = positions[i];
            pos.Y = 0.38f; // 椅子座面高度
            var ch = new EmployeeCharacter();
            ch.Build(employees[i]);
            ch.Position = pos;
            ch.RotationDegrees = new Vector3(0, rotations[i], 0);
            ch.Scale = new Vector3(1.3f, 1.3f, 1.3f);
            _building.AddChild(ch);
            _characters.Add(ch);
        }
    }

    /// <summary>招聘/解雇后刷新角色</summary>
    public void RefreshEmployees()
    {
        SpawnEmployees();
    }
}
