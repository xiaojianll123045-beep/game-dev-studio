using Godot;
using System.Collections.Generic;

public enum CardRarity { Common, Rare, Epic, Legendary }
public enum CardEffectType
{
    ProjectProgress, Fun, Graphics, Audio, Story, Stability, Network, AI,
    AllAttributes, TechDebt, Memory, Sales, Research,
    Score, Fatigue, Legendary
}

public class CardDefinition
{
    public string Key;
    public string NameKey;
    public string DescKey;
    public string Icon;
    public CardRarity Rarity;
    public CardEffectType EffectType;
    public float EffectValue;
    public int PriceMoney;
    public int PriceInspiration;
    public bool IsFreeCard;
    public int MaxStock;
    public int Stock;

    public CardDefinition Clone() => new()
    {
        Key = Key, NameKey = NameKey, DescKey = DescKey, Icon = Icon,
        Rarity = Rarity, EffectType = EffectType, EffectValue = EffectValue,
        PriceMoney = PriceMoney, PriceInspiration = PriceInspiration,
        IsFreeCard = IsFreeCard, MaxStock = MaxStock, Stock = Stock
    };
}
