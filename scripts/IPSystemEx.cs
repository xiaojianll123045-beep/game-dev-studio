using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class IPSystemEx : Node
{
    private GameManager _gm => Services.GameManager;
    private IPUniverse _ipMgr => Services.GameManager?.GetNodeOrNull<IPUniverse>("IPManager");

    public void MonthlyTick()
    {
        if (_ipMgr == null) return;
        foreach (var kv in IPManager.AllIPs)
        {
            var ip = kv.Value;
            ProcessMerch(ip);
            ProcessAdaptations(ip);
        }
    }

    // ── 衍生品商店 ──
    private void ProcessMerch(IPUniverse ip)
    {
        foreach (var m in ip.MerchProducts.Where(m => m.IsActive))
        {
            float baseSales = ip.FanCount * 0.001f;
            float revenue = baseSales * m.UnitPrice * (1 + (ip.HeatLevel - 1) * 0.1f);
            _gm.ResMgr.Money += revenue;
            m.MonthsOnShelf++;
        }
    }

    public void CreateMerchProduct(IPUniverse ip, string productId)
    {
        var product = new MerchandiseProduct
        {
            ProductId = productId,
            NameKey = productId,
            DevCost = productId switch { "t_shirt" => 5000f, "figure" => 30000f, "artbook" => 15000f, "soundtrack" => 8000f, _ => 10000f },
            UnitPrice = productId switch { "t_shirt" => 25f, "figure" => 120f, "artbook" => 45f, "soundtrack" => 15f, _ => 30f },
            Quality = 0.5f,
            IsActive = true
        };
        ip.MerchProducts.Add(product);
        _gm.ResMgr.SpendMoney(product.DevCost, "merch_dev");
    }

    // ── 跨媒体改编 ──
    private void ProcessAdaptations(IPUniverse ip)
    {
        if (ip.HeatLevel < 5 || ip.LastAdaptationMonth > 0 && _gm.GameMonth - ip.LastAdaptationMonth < 24) return;

        string[] mediaTypes = { "anime", "manga", "novel" };
        if (ip.HeatLevel >= 8) mediaTypes = new[] { "anime", "manga", "novel", "film", "tv_series" };

        if (new Random().NextDouble() < 0.02f)
        {
            var type = mediaTypes[new Random().Next(mediaTypes.Length)];
            float quality = 40f + ip.HeatLevel * 5f + (float)new Random().NextDouble() * 20f;
            float revenue = type switch { "film" => 500000f, "tv_series" => 300000f, "anime" => 100000f, _ => 50000f };
            revenue *= quality / 100f;

            ip.Adaptations.Add(new MediaAdaptation
            {
                MediaType = type,
                Title = $"{ip.Name} {type}版",
                Quality = quality,
                Revenue = revenue,
                FanBoost = quality > 75 ? (int)(ip.FanCount * 0.3f) : (int)(ip.FanCount * 0.05f),
                IsHit = quality > 75,
                ReleaseMonth = _gm.GameMonth
            });

            ip.FanCount += ip.Adaptations.Last().FanBoost;
            _gm.ResMgr.Money += revenue;
            ip.LastAdaptationMonth = _gm.GameMonth;
            _gm.ShowToast("🎬", $"{ip.Name} 改编{type}大获成功！收入¥{revenue:N0}", Colors.Gold);
        }
    }

    // ── 玩家共创 ──
    private Dictionary<string, List<string>> _communityContent = new();

    public void GenerateFanContent(string ipName)
    {
        if (!_communityContent.ContainsKey(ipName))
            _communityContent[ipName] = new List<string>();

        string[] types = { "同人图", "同人文", "MMD", "攻略", "速通视频" };
        string content = $"{types[new Random().Next(types.Length)]} - {GetRandomTitle()}";
        _communityContent[ipName].Add(content);
    }

    private string GetRandomTitle()
    {
        string[] titles = { "粉丝创作热潮", "社区活动火爆", "角色人气投票", "同人作品展" };
        return titles[new Random().Next(titles.Length)];
    }
}

// ── IPUniverse 扩展 ──
public partial class IPUniverse
{
    public List<MerchandiseProduct> MerchProducts { get; set; } = new();
    public float MerchRevenue { get; set; }
    public List<MediaAdaptation> Adaptations { get; set; } = new();
    public int LastAdaptationMonth { get; set; }
    public float CommunityVitality { get; set; }
    public int FanArtCount { get; set; }
    public float FanSentiment { get; set; } = 0.5f;
    public float LoreDepth { get; set; }
    public int ConnectedEntries { get; set; }
}
