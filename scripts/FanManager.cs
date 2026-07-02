using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 粉丝系统管理器
/// </summary>
public partial class FanManager : Node
{
    public int CasualFans { get; set; }                     // 路人粉数量
    public int DiehardFans { get; set; }                    // 死忠粉数量
    public int TotalFans => CasualFans + DiehardFans;

    public float FanEventCooldown { get; set; }             // 众筹/请愿冷却

    private ResourceManager _res;

    public override void _Ready()
    {
        _res = GetNode<ResourceManager>("../ResourceManager");
        if (GlobalSettings.NewGame)
        {
            CasualFans = 0;
            DiehardFans = 0;
        }
    }

    /// <summary>
    /// 游戏发售后粉丝变化
    /// </summary>
    public void OnGameReleased(GameProject proj)
    {
        float score = proj.FinalScore;

        // 路人粉变化
        int casualGain = (int)(proj.Sales * 0.01f);                    // 销量1%转为路人粉
        int casualLoss = (int)(CasualFans * (score < 60 ? 0.2f : score < 75 ? 0.05f : 0));
        casualLoss += (int)(casualGain * 0.3f);                        // 新路人30%是跟风会流失

        // 创始人性格：独立魂 +20% 粉丝增长
        float fanMult = 1f + (GetNode<GameManager>("/root/Main")?.Founder.GetFanGrowthBonus() ?? 0f);
        CasualFans += (int)((casualGain - casualLoss) * fanMult);
        CasualFans = Mathf.Max(0, CasualFans);

        // 死忠粉变化
        int diehardGain = score >= 85 ? (int)(proj.Sales * 0.003f) : (int)(proj.Sales * 0.001f);
        DiehardFans += (int)(diehardGain * fanMult);

        // ── 风口保险：额外路人粉加成 ──
        var gm = GetNode<GameManager>("/root/Main");
        int windBuffCount = gm?.WindInsurances?.Count(w => w.MonthsLeft > 0
            && (w.GenreOrTheme == proj.Genre.ToString() || w.GenreOrTheme == proj.Theme.ToString())) ?? 0;
        if (windBuffCount > 0)
            CasualFans += windBuffCount * 500;
        DiehardFans = Mathf.Max(0, DiehardFans);

        // 引擎开源吸引开发者粉丝
        var techMgr = GetNode<TechManager>("../TechManager");
        if (techMgr.EngineModel == EngineBizModel.OpenSource)
        {
            CasualFans += (int)(proj.Sales * 0.005f);
        }

        string msg = $"[粉丝] 路人粉 {casualGain:+0;-0} / {casualLoss:+0;-0}，死忠粉 +{diehardGain}";
        GD.Print(msg);
    }

    /// <summary>
    /// 计算销量保底（死忠粉无条件购买）
    /// </summary>
    public int GetGuaranteedSales()
    {
        return DiehardFans;
    }

    /// <summary>
    /// 信仰加成：评分低于预期时，死忠粉占比高可缓冲
    /// </summary>
    public float GetFaithBonus()
    {
        if (TotalFans == 0) return 0;
        float ratio = (float)DiehardFans / TotalFans;
        if (ratio > 0.1f)
            return ratio * 0.5f; // 最多50%惩罚减免
        return 0;
    }

    /// <summary>
    /// 粉丝见面会/嘉年华
    /// </summary>
    public bool HoldFanEvent(float cost)
    {
        if (cost > _res.Money) return false;
        ModAPI.FireHooks(ModAPI.GameHook.BeforeFanEvent);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeFanEvent)) return false;
        _res.SpendMoney(cost, "marketing");

        int converted = (int)(CasualFans * 0.1f);
        CasualFans -= converted;
        DiehardFans += converted;

        FanEventCooldown = 12;
        ModAPI.FireHooks(ModAPI.GameHook.AfterFanEvent);
        return true;
    }

    /// <summary>
    /// 每月更新（自然流失）
    /// </summary>
    public void MonthlyUpdate()
    {
        ModAPI.FireHooks(ModAPI.GameHook.BeforeFanMonthlyUpdate);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeFanMonthlyUpdate)) return;
        FanEventCooldown = Mathf.Max(0, FanEventCooldown - 1);
        // 自然流失少量路人粉
        CasualFans = (int)(CasualFans * 0.998f);
        ModAPI.FireHooks(ModAPI.GameHook.AfterFanMonthlyUpdate);
    }
}
