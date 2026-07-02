using Godot;

/// <summary>
/// 全局服务定位器 —— 替代 GetNode("../xxx") 硬编码路径。
/// GameManager._Ready() 中统一注册，其他脚本通过 Services.Get<T>() 获取引用。
/// </summary>
public static class Services
{
    public static GameManager GameManager { get; private set; } = null!;
    public static ResourceManager ResourceManager { get; private set; } = null!;
    public static EmployeeManager EmployeeManager { get; private set; } = null!;
    public static TeamManager TeamManager { get; private set; } = null!;
    public static TechManager TechManager { get; private set; } = null!;
    public static TechDebtManager TechDebtManager { get; private set; } = null!;
    public static GameDevManager GameDevManager { get; private set; } = null!;
    public static FanManager FanManager { get; private set; } = null!;
    public static AchievementManager AchievementManager { get; private set; } = null!;
    public static CompetitorAI CompetitorAI { get; private set; } = null!;
    public static StoryEvents StoryEvents { get; private set; } = null!;
    public static MarketTrendManager MarketTrendManager { get; private set; } = null!;
    public static ServerManager ServerManager { get; private set; } = null!;
    public static RoomManager RoomManager { get; private set; } = null!;

    // ── 新系统 ──
    public static SprintSystemEx SprintSystemEx { get; private set; } = null!;
    public static EmployeeSystemEx EmployeeSystemEx { get; private set; } = null!;
    public static EconomySystemEx EconomySystemEx { get; private set; } = null!;
    public static MarketSystemEx MarketSystemEx { get; private set; } = null!;
    public static CommunitySystemEx CommunitySystemEx { get; private set; } = null!;
    public static IPSystemEx IPSystemEx { get; private set; } = null!;
    public static UISystemEx UISystemEx { get; private set; } = null!;
    public static LongTermSystemEx LongTermSystemEx { get; private set; } = null!;

    /// <summary>GameManager._Ready() 调用，注册所有子节点。</summary>
    public static void Initialize(GameManager gm)
    {
        GameManager = gm;
        ResourceManager = gm.GetNode<ResourceManager>("ResourceManager");
        EmployeeManager = gm.GetNode<EmployeeManager>("EmployeeManager");
        TeamManager = gm.GetNode<TeamManager>("TeamManager");
        TechManager = gm.GetNode<TechManager>("TechManager");
        TechDebtManager = gm.GetNode<TechDebtManager>("TechDebtManager");
        GameDevManager = gm.GetNode<GameDevManager>("GameDevManager");
        FanManager = gm.GetNode<FanManager>("FanManager");
        AchievementManager = gm.GetNode<AchievementManager>("AchievementManager");
        CompetitorAI = gm.GetNode<CompetitorAI>("CompetitorAI");
        StoryEvents = gm.GetNode<StoryEvents>("StoryEvents");
        MarketTrendManager = gm.GetNode<MarketTrendManager>("MarketTrendManager");
        ServerManager = gm.GetNode<ServerManager>("ServerManager");
        RoomManager = gm.GetNode<RoomManager>("RoomManager");

        // ── 新系统 ──
        SprintSystemEx = gm.GetNode<SprintSystemEx>("SprintSystemEx");
        EmployeeSystemEx = gm.GetNode<EmployeeSystemEx>("EmployeeSystemEx");
        EconomySystemEx = gm.GetNode<EconomySystemEx>("EconomySystemEx");
        MarketSystemEx = gm.GetNode<MarketSystemEx>("MarketSystemEx");
        CommunitySystemEx = gm.GetNode<CommunitySystemEx>("CommunitySystemEx");
        IPSystemEx = gm.GetNode<IPSystemEx>("IPSystemEx");
        UISystemEx = gm.GetNode<UISystemEx>("UISystemEx");
        LongTermSystemEx = gm.GetNode<LongTermSystemEx>("LongTermSystemEx");
    }

    /// <summary>按类型获取服务（泛型版，编译期类型安全）。</summary>
    public static T Get<T>() where T : class
    {
        var t = typeof(T);
        if (t == typeof(GameManager)) return (T)(object)GameManager;
        if (t == typeof(ResourceManager)) return (T)(object)ResourceManager;
        if (t == typeof(EmployeeManager)) return (T)(object)EmployeeManager;
        if (t == typeof(TeamManager)) return (T)(object)TeamManager;
        if (t == typeof(TechManager)) return (T)(object)TechManager;
        if (t == typeof(TechDebtManager)) return (T)(object)TechDebtManager;
        if (t == typeof(GameDevManager)) return (T)(object)GameDevManager;
        if (t == typeof(FanManager)) return (T)(object)FanManager;
        if (t == typeof(AchievementManager)) return (T)(object)AchievementManager;
        if (t == typeof(CompetitorAI)) return (T)(object)CompetitorAI;
        if (t == typeof(StoryEvents)) return (T)(object)StoryEvents;
        if (t == typeof(MarketTrendManager)) return (T)(object)MarketTrendManager;
        if (t == typeof(ServerManager)) return (T)(object)ServerManager;
        if (t == typeof(RoomManager)) return (T)(object)RoomManager;
        if (t == typeof(SprintSystemEx)) return (T)(object)SprintSystemEx;
        if (t == typeof(EmployeeSystemEx)) return (T)(object)EmployeeSystemEx;
        if (t == typeof(EconomySystemEx)) return (T)(object)EconomySystemEx;
        if (t == typeof(MarketSystemEx)) return (T)(object)MarketSystemEx;
        if (t == typeof(CommunitySystemEx)) return (T)(object)CommunitySystemEx;
        if (t == typeof(IPSystemEx)) return (T)(object)IPSystemEx;
        if (t == typeof(UISystemEx)) return (T)(object)UISystemEx;
        if (t == typeof(LongTermSystemEx)) return (T)(object)LongTermSystemEx;
        GD.PushError($"[Services] Unknown service type: {t.Name}");
        return null!;
    }
}
