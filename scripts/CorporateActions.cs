using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 商战攻击系统 — 玩家可主动对AI工作室采取攻击行动
/// </summary>
public class CorporateActions
{
    private CompetitorAI _compAI;
    private GameManager _gm;

    public List<CorporateActionLog> ActionLogs { get; set; } = new();
    public Dictionary<ActionType, int> Cooldowns { get; set; } = new();  // ActionType → last used month

    public void Init(GameManager gm, CompetitorAI compAI)
    {
        _gm = gm;
        _compAI = compAI;
    }

    /// <summary>获取某行动的冷却剩余月数</summary>
    public int GetCooldownRemaining(ActionType type)
    {
        if (!Cooldowns.TryGetValue(type, out int lastUsed)) return 0;
        int cd = GetCooldownMonths(type);
        int elapsed = _gm.GameMonth - lastUsed;
        return Math.Max(0, cd - elapsed);
    }

    public int GetCooldownMonths(ActionType type) => type switch
    {
        ActionType.Poach => 3,
        ActionType.NegativePress => 2,
        ActionType.Lawsuit => 6,
        ActionType.HostileTakeover => 12,
        ActionType.PriceWar => 4,
        ActionType.ExclusiveDeal => 8,
        ActionType.IPDispute => 5,
        _ => 1
    };

    public int GetCost(ActionType type) => type switch
    {
        ActionType.Poach => 50_000,
        ActionType.NegativePress => 30_000,
        ActionType.Lawsuit => 200_000,
        ActionType.HostileTakeover => 1_000_000,
        ActionType.PriceWar => 100_000,
        ActionType.ExclusiveDeal => 300_000,
        ActionType.IPDispute => 80_000,
        _ => 10_000
    };

    public float GetBaseSuccessRate(ActionType type, AIStudio target)
    {
        float baseRate = type switch
        {
            ActionType.Poach => 0.55f,
            ActionType.NegativePress => 0.65f,
            ActionType.Lawsuit => 0.35f,
            ActionType.HostileTakeover => 0.15f,
            ActionType.PriceWar => 0.50f,
            ActionType.ExclusiveDeal => 0.40f,
            ActionType.IPDispute => 0.45f,
            _ => 0.5f
        };
        // 目标声誉越高越难
        baseRate -= target.Reputation / 200f;
        // 创始人赌博性格加成
        if (_gm.Founder?.Trait == FounderTrait.RiskTaker)
            baseRate += 0.15f;
        return Math.Clamp(baseRate, 0.05f, 0.95f);
    }

    /// <summary>执行商战行动</summary>
    public bool Execute(ActionType type, AIStudio target)
    {
        int cd = GetCooldownRemaining(type);
        if (cd > 0) return false;
        int cost = GetCost(type);
        if (!Services.ResourceManager.SpendMoney(cost, $"corp_action_{type}")) return false;

        var rng = new Random();
        float rate = GetBaseSuccessRate(type, target);
        bool success = rng.NextDouble() < rate;

        Cooldowns[type] = _gm.GameMonth;
        string log;
        Color color;

        switch (type)
        {
            case ActionType.Poach:
                if (success)
                {
                    target.EmployeeCount = Math.Max(1, target.EmployeeCount - 1);
                    target.Reputation = Math.Max(0, target.Reputation - 10);
                    log = Loc.TrF("corp.poach_ok", target.Name);
                    color = new Color(0.3f, 0.8f, 0.3f);
                }
                else
                {
                    target.Reputation = Math.Max(0, target.Reputation - 3);
                    log = Loc.TrF("corp.poach_fail", target.Name);
                    color = new Color(0.9f, 0.5f, 0.2f);
                }
                break;

            case ActionType.NegativePress:
                if (success)
                {
                    target.Reputation = Math.Max(0, target.Reputation - 8);
                    log = Loc.TrF("corp.press_ok", target.Name);
                    color = new Color(0.3f, 0.8f, 0.3f);
                }
                else
                {
                    // 被识破反噬
                    var devMgr = _gm.GetNode<GameDevManager>("GameDevManager");
                    if (devMgr != null) devMgr.PublisherReputation = Math.Max(0, devMgr.PublisherReputation - 0.1f);
                    log = Loc.TrF("corp.press_fail", target.Name);
                    color = new Color(0.9f, 0.3f, 0.3f);
                }
                break;

            case ActionType.Lawsuit:
                if (success)
                {
                    target.EmployeeCount = Math.Max(1, target.EmployeeCount - 2);
                    target.Reputation = Math.Max(0, target.Reputation - 15);
                    log = Loc.TrF("corp.sue_ok", target.Name);
                    color = new Color(0.3f, 0.8f, 0.3f);
                }
                else
                {
                    var res = Services.ResourceManager;
                    res.SpendMoney(cost / 2, "lawsuit_lost");
                    log = Loc.TrF("corp.sue_fail", target.Name);
                    color = new Color(0.9f, 0.3f, 0.3f);
                }
                break;

            case ActionType.HostileTakeover:
                if (success)
                {
                    // 吞并：消灭对手，获得其市场份额
                    var devMgr = _gm.GetNode<GameDevManager>("GameDevManager");
                    if (devMgr != null) devMgr.PublisherReputation = Math.Min(1f, devMgr.PublisherReputation + 0.1f);
                    _compAI.Studios.Remove(target);
                    log = Loc.TrF("corp.takeover_ok", target.Name);
                    color = new Color(1f, 0.85f, 0.1f);
                }
                else
                {
                    log = Loc.TrF("corp.takeover_fail", target.Name);
                    color = new Color(0.9f, 0.3f, 0.3f);
                }
                break;

            case ActionType.PriceWar:
                if (success)
                {
                    // 发起价格战：目标未来3款游戏销量-20%，我方+10%
                    target.Reputation = Math.Max(0, target.Reputation - 12);
                    log = Loc.TrF("corp.pricewar_ok", target.Name);
                    color = new Color(0.3f, 0.8f, 0.3f);
                }
                else
                {
                    // 失败：我方声誉受损
                    _gm.GetNode<GameDevManager>("GameDevManager").PublisherReputation = Math.Max(0, _gm.GetNode<GameDevManager>("GameDevManager").PublisherReputation - 0.08f);
                    log = Loc.TrF("corp.pricewar_fail", target.Name);
                    color = new Color(0.9f, 0.3f, 0.3f);
                }
                break;

            case ActionType.ExclusiveDeal:
                if (success)
                {
                    // 签下独占协议：目标少一个平台可选，我方多一个
                    target.EmployeeCount = Math.Max(1, target.EmployeeCount - 1);
                    target.Reputation = Math.Max(0, target.Reputation - 10);
                    var edm = _gm.GetNode<GameDevManager>("GameDevManager");
                    if (edm != null) edm.PublisherReputation = Math.Min(1f, edm.PublisherReputation + 0.05f);
                    log = Loc.TrF("corp.exclusive_ok", target.Name);
                    color = new Color(0.3f, 0.8f, 0.3f);
                }
                else
                {
                    var res = Services.ResourceManager;
                    res.SpendMoney(cost / 2, "exclusive_failed");
                    log = Loc.TrF("corp.exclusive_fail", target.Name);
                    color = new Color(0.9f, 0.3f, 0.3f);
                }
                break;

            case ActionType.IPDispute:
                if (success)
                {
                    // IP纠纷胜诉：目标被迫下架一款游戏
                    if (target.Releases.Count > 0)
                    {
                        target.Releases.RemoveAt(target.Releases.Count - 1);
                        target.Reputation = Math.Max(0, target.Reputation - 15);
                    }
                    log = Loc.TrF("corp.ip_ok", target.Name);
                    color = new Color(0.3f, 0.8f, 0.3f);
                }
                else
                {
                    // 败诉：我方赔偿
                    _gm.GetNode<GameDevManager>("GameDevManager").PublisherReputation = Math.Max(0, _gm.GetNode<GameDevManager>("GameDevManager").PublisherReputation - 0.1f);
                    log = Loc.TrF("corp.ip_fail", target.Name);
                    color = new Color(0.9f, 0.3f, 0.3f);
                }
                break;

            default:
                return false;
        }

        ActionLogs.Add(new CorporateActionLog
        {
            Month = _gm.GameMonth,
            Type = type,
            TargetName = target.Name,
            Success = success,
            Description = log
        });

        _gm.ShowToast(success ? Loc.Tr("corp.success") : Loc.Tr("corp.failed"), log, color);
        return true;
    }

    public Dictionary<string, object> Serialize()
    {
        return new()
        {
            ["logs"] = ActionLogs.Select(l => (object)new Dictionary<string, object>
            {
                ["month"] = l.Month,
                ["type"] = l.Type.ToString(),
                ["target"] = l.TargetName,
                ["success"] = l.Success,
                ["desc"] = l.Description
            }).ToList(),
            ["cooldowns"] = Cooldowns.ToDictionary(kv => kv.Key.ToString(), kv => (object)kv.Value)
        };
    }

    public static CorporateActions Deserialize(Dictionary<string, object> d)
    {
        var actions = new CorporateActions();
        if (d.TryGetValue("logs", out var logsObj) && logsObj is System.Text.Json.JsonElement logsArr)
        {
            foreach (var item in logsArr.EnumerateArray())
            {
                actions.ActionLogs.Add(new CorporateActionLog
                {
                    Month = item.GetProperty("month").GetInt32(),
                    Type = Enum.TryParse<ActionType>(item.GetProperty("type").GetString(), out var t) ? t : ActionType.Poach,
                    TargetName = item.GetProperty("target").GetString(),
                    Success = item.GetProperty("success").GetBoolean(),
                    Description = item.GetProperty("desc").GetString()
                });
            }
        }
        if (d.TryGetValue("cooldowns", out var cdObj) && cdObj is System.Text.Json.JsonElement cdDict)
        {
            foreach (var prop in cdDict.EnumerateObject())
            {
                if (Enum.TryParse<ActionType>(prop.Name, out var t))
                    actions.Cooldowns[t] = prop.Value.GetInt32();
            }
        }
        return actions;
    }
}

public enum ActionType
{
    Poach,          // 挖角
    NegativePress,  // 负面新闻
    Lawsuit,        // 专利诉讼
    HostileTakeover,// 恶意收购
    PriceWar,       // 价格战
    ExclusiveDeal,  // 独占协议
    IPDispute       // IP授权纠纷
}

public class CorporateActionLog
{
    public int Month { get; set; }
    public ActionType Type { get; set; }
    public string TargetName { get; set; } = "";
    public bool Success { get; set; }
    public string Description { get; set; } = "";
}
