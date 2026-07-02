using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CardSystem : Node
{
    public const int FreeSlotCount = 3;
    public const int PaidSlotCount = 5;
    public const int ShopRefreshMonths = 3;

    public List<CardDefinition> FreeSlots { get; private set; } = new();
    public List<CardDefinition> PaidSlots { get; private set; } = new();
    public List<CardDefinition> ShopInventory { get; private set; } = new();
    public int MonthsUntilShopRefresh { get; set; } = 0;
    public int LegendaryBoughtThisQuarter { get; set; } = 0;

    private static List<CardDefinition> _freeCardPool;
    private static List<CardDefinition> _paidCardPool;
    private GameManager _gm;
    public ResourceManager ResMgr => _gm?.ResMgr;

    public override void _Ready()
    {
        _gm = GetNode<GameManager>("..");
        InitCardPools();
        RefreshFreeSlots();
        RefreshShop();
    }

    private void InitCardPools()
    {
        _freeCardPool = new List<CardDefinition>
        {
            Free("speed","🏃",CardEffectType.ProjectProgress,3f,CardRarity.Common),
            Free("spark","✨",CardEffectType.Fun,2f,CardRarity.Common),
            Free("clean","🧹",CardEffectType.TechDebt,-5f,CardRarity.Common),
            Free("coffee","☕",CardEffectType.Fatigue,-8f,CardRarity.Common),
            Free("brainstorm","🧠",CardEffectType.Fun,3f,CardRarity.Common),
            Free("detail","🔍",CardEffectType.Stability,3f,CardRarity.Common),
            Free("feedback","💬",CardEffectType.Score,1f,CardRarity.Rare),
            Free("report","📊",CardEffectType.ProjectProgress,4f,CardRarity.Common),
            Free("nap","😴",CardEffectType.Fatigue,-10f,CardRarity.Common),
        };

        _paidCardPool = new List<CardDefinition>
        {
            Paid("tweak","🔧",CardEffectType.AllAttributes,1f,CardRarity.Common,5000,0,3),
            Paid("read_doc","📖",CardEffectType.Research,5f,CardRarity.Common,5000,0,3),
            Paid("art_out","🎨",CardEffectType.Graphics,2f,CardRarity.Common,8000,0,3),
            Paid("audio_out","🔊",CardEffectType.Audio,2f,CardRarity.Common,8000,0,3),
            Paid("story_lab","✍️",CardEffectType.Story,2f,CardRarity.Common,8000,0,3),
            Paid("test","✅",CardEffectType.Stability,3f,CardRarity.Common,6000,0,3),
            Paid("community","📢",CardEffectType.Sales,3f,CardRarity.Common,10000,0,3),
            Paid("hotfix","🩹",CardEffectType.Memory,-5f,CardRarity.Common,6000,0,3),
            Paid("overtime","⏰",CardEffectType.ProjectProgress,2f,CardRarity.Common,4000,0,3),
            Paid("redesign","🔄",CardEffectType.AllAttributes,1f,CardRarity.Rare,12000,0,3),
            Paid("sprint","🚀",CardEffectType.ProjectProgress,12f,CardRarity.Rare,25000,0,2),
            Paid("big_fun","💡",CardEffectType.Fun,6f,CardRarity.Rare,20000,0,2),
            Paid("big_clean","🔧",CardEffectType.TechDebt,-20f,CardRarity.Rare,30000,0,2),
            Paid("big_art","🎨",CardEffectType.Graphics,6f,CardRarity.Rare,25000,0,2),
            Paid("big_audio","🔊",CardEffectType.Audio,6f,CardRarity.Rare,25000,0,2),
            Paid("big_story","📖",CardEffectType.Story,6f,CardRarity.Rare,25000,0,2),
            Paid("big_stab","🛡️",CardEffectType.Stability,8f,CardRarity.Rare,20000,0,2),
            Paid("big_sales","📢",CardEffectType.Sales,12f,CardRarity.Rare,35000,0,2),
            Paid("mem_opt","💾",CardEffectType.Memory,-20f,CardRarity.Rare,25000,0,2),
            Paid("teambuild","🤝",CardEffectType.Fatigue,-25f,CardRarity.Rare,20000,0,2),
            Paid("rapid_iter","⚡",CardEffectType.ProjectProgress,15f,CardRarity.Rare,30000,0,2),
            Paid("tech_boost","🔬",CardEffectType.Research,40f,CardRarity.Epic,60000,0,1),
            Paid("art_revo","🎭",CardEffectType.AllAttributes,12f,CardRarity.Epic,70000,0,1),
            Paid("fun_big","🎯",CardEffectType.Fun,12f,CardRarity.Epic,60000,0,1),
            Paid("perfect","✨",CardEffectType.AllAttributes,5f,CardRarity.Epic,80000,20,1),
            Paid("sales_storm","📈",CardEffectType.Sales,25f,CardRarity.Epic,75000,0,1),
            Paid("eng_refactor","⚙️",CardEffectType.Memory,-40f,CardRarity.Epic,70000,0,1),
            Paid("summit","🏛️",CardEffectType.Score,3f,CardRarity.Rare,50000,0,1),
            Paid("coop","🤝",CardEffectType.AllAttributes,4f,CardRarity.Epic,80000,0,1),
            Paid("ace_prod","👑",CardEffectType.Legendary,10f,CardRarity.Legendary,200000,0,1),
            Paid("viral","🌟",CardEffectType.Sales,50f,CardRarity.Legendary,180000,0,1),
            Paid("tech_down","🚀",CardEffectType.Research,100f,CardRarity.Legendary,250000,0,1),
            Paid("legend_ip","🏆",CardEffectType.Score,15f,CardRarity.Legendary,0,50,1),
            Paid("monopoly","💎",CardEffectType.AllAttributes,-20f,CardRarity.Legendary,300000,0,1),
        };
    }

    private static CardDefinition Free(string key, string icon, CardEffectType eff, float val, CardRarity r) =>
        new() { Key="free_"+key, NameKey="card."+key, DescKey="card."+key+"_desc", Icon=icon, Rarity=r, EffectType=eff, EffectValue=val, IsFreeCard=true, MaxStock=1, Stock=1 };

    private static CardDefinition Paid(string key, string icon, CardEffectType eff, float val, CardRarity r, int money, int insp, int stock) =>
        new() { Key="paid_"+key, NameKey="card."+key, DescKey="card."+key+"_desc", Icon=icon, Rarity=r, EffectType=eff, EffectValue=val, IsFreeCard=false, PriceMoney=money, PriceInspiration=insp, MaxStock=stock, Stock=stock };

    public void RefreshFreeSlots()
    {
        FreeSlots.Clear();
        foreach (var c in _freeCardPool.OrderBy(_ => Random.Shared.Next()).Take(FreeSlotCount))
        {
            var clone = c.Clone(); clone.Stock = 1; FreeSlots.Add(clone);
        }
    }

    public void RefreshShop()
    {
        ShopInventory.Clear();
        LegendaryBoughtThisQuarter = 0;
        MonthsUntilShopRefresh = ShopRefreshMonths;

        int common = Random.Shared.Next(4, 7);
        int rare = Random.Shared.Next(2, 4);
        int epic = Random.Shared.Next(1, 3);
        bool hasLeg = Random.Shared.NextDouble() < 0.3;

        AddToShop(CardRarity.Common, common);
        AddToShop(CardRarity.Rare, rare);
        AddToShop(CardRarity.Epic, epic);
        if (hasLeg) AddToShop(CardRarity.Legendary, 1);
    }

    private void AddToShop(CardRarity rarity, int count)
    {
        foreach (var c in _paidCardPool.Where(x => x.Rarity == rarity).OrderBy(_ => Random.Shared.Next()).Take(count))
        {
            var clone = c.Clone();
            clone.Stock = Mathf.Clamp(clone.MaxStock, 1, (int)(1 + Random.Shared.NextDouble() * 2));
            ShopInventory.Add(clone);
        }
    }

    public int PaidSlotUsed => PaidSlots.Count;
    public bool CanBuy => PaidSlotUsed < PaidSlotCount;

    public bool BuyFromShop(CardDefinition card)
    {
        if (PaidSlotUsed >= PaidSlotCount) return false;
        if (card.Rarity == CardRarity.Legendary && LegendaryBoughtThisQuarter >= 1) return false;

        if (card.PriceMoney > 0 && (ResMgr == null || !ResMgr.SpendMoney(card.PriceMoney, "cards"))) return false;
        if (card.PriceInspiration > 0 && (ResMgr == null || !ResMgr.SpendInspiration(card.PriceInspiration))) return false;

        PaidSlots.Add(card.Clone());
        card.Stock--;
        if (card.Rarity == CardRarity.Legendary) LegendaryBoughtThisQuarter++;
        if (card.Stock <= 0) ShopInventory.Remove(card);
        return true;
    }

    public void DiscardCard(CardDefinition card)
    {
        if (card.IsFreeCard) FreeSlots.Remove(card);
        else PaidSlots.Remove(card);
    }

    public void OnNewYear() => RefreshFreeSlots();
    public void OnQuarterEnd()
    {
        MonthsUntilShopRefresh--;
        if (MonthsUntilShopRefresh <= 0) RefreshShop();
    }
}
