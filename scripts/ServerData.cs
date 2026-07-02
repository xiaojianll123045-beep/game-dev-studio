using System.Collections.Generic;
using Godot;

/// <summary>
/// 服务器档位枚举
/// </summary>
public enum ServerTier
{
    None,       // 无服务器
    Basic,      // 基础服务器
    Standard,   // 标准服务器
    Pro,        // 专业服务器
    Enterprise, // 企业级
    Cloud,      // 云端集群
}

/// <summary>
/// 服务器数据类
/// </summary>
public static class ServerData
{
    public class ServerInfo
    {
        public string Name;
        public int Capacity;          // 最大并发用户数
        public int PurchaseCost;
        public int MonthlyCost;
        public float Reliability;     // 稳定性系数 (0~1，影响联机评分)
        public Color Color;
    }

    public static readonly Dictionary<ServerTier, ServerInfo> Data = new()
    {
        [ServerTier.None] = new ServerInfo
        {
            Name = "无服务器",
            Capacity = 0,
            PurchaseCost = 0,
            MonthlyCost = 0,
            Reliability = 0f,
            Color = new Color(0.4f, 0.4f, 0.4f),
        },
        [ServerTier.Basic] = new ServerInfo
        {
            Name = "基础服务器",
            Capacity = 5000,
            PurchaseCost = 20000,
            MonthlyCost = 3000,
            Reliability = 0.7f,
            Color = new Color(0.3f, 0.7f, 0.3f),
        },
        [ServerTier.Standard] = new ServerInfo
        {
            Name = "标准服务器",
            Capacity = 20000,
            PurchaseCost = 60000,
            MonthlyCost = 8000,
            Reliability = 0.85f,
            Color = new Color(0.3f, 0.5f, 0.9f),
        },
        [ServerTier.Pro] = new ServerInfo
        {
            Name = "专业服务器",
            Capacity = 80000,
            PurchaseCost = 200000,
            MonthlyCost = 25000,
            Reliability = 0.93f,
            Color = new Color(0.7f, 0.3f, 0.9f),
        },
        [ServerTier.Enterprise] = new ServerInfo
        {
            Name = "企业级服务器",
            Capacity = 300000,
            PurchaseCost = 600000,
            MonthlyCost = 80000,
            Reliability = 0.97f,
            Color = new Color(0.9f, 0.6f, 0.15f),
        },
        [ServerTier.Cloud] = new ServerInfo
        {
            Name = "云端集群",
            Capacity = 1500000,
            PurchaseCost = 2000000,
            MonthlyCost = 250000,
            Reliability = 0.995f,
            Color = new Color(0.15f, 0.8f, 0.9f),
        },
    };

    public static ServerTier NextTier(ServerTier current)
    {
        return current switch
        {
            ServerTier.None => ServerTier.Basic,
            ServerTier.Basic => ServerTier.Standard,
            ServerTier.Standard => ServerTier.Pro,
            ServerTier.Pro => ServerTier.Enterprise,
            ServerTier.Enterprise => ServerTier.Cloud,
            _ => ServerTier.Cloud,
        };
    }

    /// <summary>返回当前档位的本地化名称</summary>
    public static string GetTierName(ServerTier tier) => tier switch
    {
        ServerTier.None => Loc.Tr("server.tier_none"),
        ServerTier.Basic => Loc.Tr("server.tier_basic"),
        ServerTier.Standard => Loc.Tr("server.tier_standard"),
        ServerTier.Pro => Loc.Tr("server.tier_pro"),
        ServerTier.Enterprise => Loc.Tr("server.tier_enterprise"),
        ServerTier.Cloud => Loc.Tr("server.tier_cloud"),
        _ => "",
    };

    /// <summary>是否可以升级</summary>
    public static bool CanUpgrade(this ServerTier t) => t < ServerTier.Cloud;

    /// <summary>返回下一档数据（含null guard）</summary>
    public static ServerInfo NextInfo(this ServerTier t) => Data[NextTier(t)];

    /// <summary>计算实际在线玩家需求数（并发 <= 总销量 * 月活系数）</summary>
    public static int ComputeDemand(int totalActiveSales, float onlineRatio)
    {
        // 活跃销量：过去12个月发售的游戏总销量
        // onlineRatio：用户指定的联机依赖度（0~1）
        return (int)(totalActiveSales * 0.15f * onlineRatio);
    }
}
