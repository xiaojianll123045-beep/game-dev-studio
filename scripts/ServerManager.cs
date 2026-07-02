using System.Linq;
using Godot;

/// <summary>
/// 服务器管理器 — 购买/升级/月费/需求分析
/// </summary>
public partial class ServerManager : Node
{
    private GameManager _gm;
    private ResourceManager _res;

    /// <summary>当前拥有的服务器档位</summary>
    public ServerTier CurrentTier { get; set; } = ServerTier.None;

    /// <summary>服务器总容量（并发）</summary>
    public int TotalCapacity => ServerData.Data[CurrentTier].Capacity;

    /// <summary>月费</summary>
    public int MonthlyCost => ServerData.Data[CurrentTier].MonthlyCost;

    /// <summary>可靠性</summary>
    public float Reliability => ServerData.Data[CurrentTier].Reliability;

    /// <summary>当前需求（由游戏销量+在线比决定）</summary>
    public int CurrentDemand { get; private set; }

    /// <summary>是否过载（需求>容量）</summary>
    public bool IsOverloaded => CurrentDemand > TotalCapacity && CurrentTier != ServerTier.None;

    /// <summary>是否无服务器</summary>
    public bool HasNoServer => CurrentTier == ServerTier.None;

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        _res = GetNode<ResourceManager>("../ResourceManager");
    }

    /// <summary>购买/升级服务器</summary>
    public bool UpgradeServer()
    {
        if (!CurrentTier.CanUpgrade()) return false;
        ModAPI.FireHooks(ModAPI.GameHook.BeforeServerUpgrade);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeServerUpgrade)) return false;
        var next = CurrentTier.NextInfo();
        if (!_res.SpendMoney(next.PurchaseCost, "server"))
        {
            _gm.ShowToast(Loc.Tr("server.need_money"),
                Loc.TrF("server.need_money_msg", ServerData.GetTierName(ServerData.NextTier(CurrentTier)), next.PurchaseCost),
                new Color(0.9f, 0.3f, 0.2f));
            return false;
        }
        CurrentTier = ServerData.NextTier(CurrentTier);
        _gm.ShowToast(Loc.Tr("server.upgrade_ok"),
            Loc.TrF("server.upgrade_ok_msg", ServerData.GetTierName(CurrentTier), ServerData.Data[CurrentTier].Capacity),
            new Color(0.3f, 0.8f, 0.4f));
        ModAPI.FireHooks(ModAPI.GameHook.AfterServerUpgrade);
        return true;
    }

    /// <summary>每月刷新需求（OnMonthEnd中调用）</summary>
    public void MonthlyTick()
    {
        var devMgr = _gm.GetNode<GameDevManager>("GameDevManager");
        var totalActiveSales = devMgr.CompletedProjects
            .Where(p => p.IsReleased && p.OriginalReleaseMonth > 0)
            .Sum(p => p.TotalLifetimeSales);
        float onlineRatio = _gm.GetOnlineGameRatio();
        CurrentDemand = ServerData.ComputeDemand((int)totalActiveSales, onlineRatio);

        // 扣月费
        if (CurrentTier != ServerTier.None)
            _res.SpendMoney(MonthlyCost, "server");
    }

    /// <summary>过载惩罚语（供UI显示）</summary>
    public string GetOverloadHint()
    {
        if (HasNoServer) return Loc.Tr("server.no_capacity");
        if (IsOverloaded)
        {
            int overload = CurrentDemand - TotalCapacity;
            return Loc.TrF("server.overload", overload);
        }
        return Loc.TrF("server.capacity_ok", (float)CurrentDemand / TotalCapacity * 100);
    }
}
