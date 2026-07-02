using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static class ModAPI
{
    private static GameManager _gm;
    private static ResourceManager _res => _gm?.ResMgr;
    private static EmployeeManager _emp => Services.EmployeeManager;
    private static GameDevManager _dev => Services.GameDevManager;
    private static FanManager _fan => Services.FanManager;
    private static TechManager _tech => Services.TechManager;
    private static RoomManager _room => Services.RoomManager;
    private static TeamManager _team => Services.TeamManager;
    private static MarketTrendManager _trend => Services.MarketTrendManager;
    private static CompetitorAI _comp => Services.CompetitorAI;
    private static TechDebtManager _debt => Services.TechDebtManager;
    private static ServerManager _server => Services.ServerManager;
    private static StoryEvents _story => Services.StoryEvents;

    public static void Init(GameManager gm) { _gm = gm; }

    // ═══════════════ 核心访问 ═══════════════
    public static GameManager GameManager => _gm;
    public static ResourceManager Resource => _res;
    public static EmployeeManager Employees => _emp;
    public static GameDevManager Development => _dev;
    public static FanManager Fans => _fan;
    public static TechManager Tech => _tech;
    public static RoomManager Room => _room;
    public static TeamManager Teams => _team;
    public static MarketTrendManager Market => _trend;
    public static CompetitorAI Competitors => _comp;
    public static TechDebtManager Debt => _debt;
    public static ServerManager Server => _server;
    public static StoryEvents Story => _story;

    public static void Log(string msg) => GD.Print($"[Mod] {msg}");

    // ═══════════════ 钩子上下文 ═══════════════
    public class HookContext
    {
        public Dictionary<string, object> Args { get; set; } = new();
        public object ReturnValue { get; set; }
        public bool ReturnValueSet { get; set; }

        public T Get<T>(string key, T fallback = default)
        {
            if (Args.TryGetValue(key, out var v) && v is T tv) return tv;
            return fallback;
        }
        public void Set(string key, object val) => Args[key] = val;
    }

    // ═══════════════ 资金 ═══════════════
    public static float GetMoney() => _res?.Money ?? 0;
    public static void SetMoney(float v) { if (_res != null) _res.Money = v; }
    public static void AddMoney(float v) { if (_res != null) _res.Money += v; }
    public static bool SpendMoney(float v, string reason) => _res?.SpendMoney(v, reason) ?? false;
    public static float GetMonthlyIncome() => _res?.MonthlyIncome ?? 0;
    public static float GetMonthlyExpense() => _res?.MonthlyExpense ?? 0;

    // ═══════════════ 灵感 ═══════════════
    public static float GetInspiration() => _res?.Inspiration ?? 0;
    public static float GetMaxInspiration() => _res?.MaxInspiration ?? 100;
    public static void AddInspiration(float v) { if (_res != null) _res.GainInspiration(v); }
    public static void SetInspiration(float v) { if (_res != null) _res.Inspiration = Mathf.Min(v, _res.MaxInspiration); }
    public static bool SpendInspiration(float v) => _res?.SpendInspiration(v) ?? false;

    // ═══════════════ 员工 ═══════════════
    public static int GetEmployeeCount() => _emp?.Employees.Count ?? 0;
    public static List<Employee> GetAllEmployees() => _emp?.Employees ?? new();
    public static Employee GetEmployee(int id) => _emp?.Employees.Find(e => e.Id == id);
    public static void AddEmployee(Employee e) { if (_emp != null) _emp.Employees.Add(e); }
    public static void RemoveEmployee(Employee e) { if (_emp != null) _emp.Employees.Remove(e); }
    public static void FireEmployee(Employee e) { if (_emp != null) _emp.FireEmployee(e); }

    // ═══════════════ 粉丝 ═══════════════
    public static int GetCasualFans() => _fan?.CasualFans ?? 0;
    public static int GetDiehardFans() => _fan?.DiehardFans ?? 0;
    public static int GetTotalFans() => _fan?.TotalFans ?? 0;
    public static void AddCasualFans(int v) { if (_fan != null) _fan.CasualFans = Mathf.Max(0, _fan.CasualFans + v); }
    public static void AddDiehardFans(int v) { if (_fan != null) _fan.DiehardFans = Mathf.Max(0, _fan.DiehardFans + v); }
    public static bool HoldFanEvent(float cost) => _fan?.HoldFanEvent(cost) ?? false;
    public static int GetGuaranteedSales() => _fan?.GetGuaranteedSales() ?? 0;

    // ═══════════════ 公司 ═══════════════
    public static string GetCompanyName() => _gm?.Founder?.CompanyName ?? "";
    public static float GetPlayerTrust() => _dev?.PlayerTrust ?? 0;
    public static void SetPlayerTrust(float v) { if (_dev != null) _dev.PlayerTrust = Mathf.Clamp(v, 0, 100); }
    public static float GetReputation() => _dev?.PublisherReputation ?? 0;
    public static float GetSharePrice() => _dev?.SharePrice ?? 0;
    public static bool IsListed() => _dev?.IsListed ?? false;

    // ═══════════════ 时间 ═══════════════
    public static int GetMonth() => _gm?.GameMonth ?? 0;
    public static int GetYear() => _gm?.GameYear ?? 0;
    public static int GetMonthInYear() => _gm?.MonthInYear ?? 1;
    public static int GetTotalMonths() => _gm?.GameMonth ?? 0;
    public static bool IsPaused() => _gm?.Paused ?? false;
    public static void SetPaused(bool v) { if (_gm != null) _gm.Paused = v; }
    public static void SetSpeed(int s) { _gm?.SetGameSpeed(Mathf.Clamp(s, 1, 8)); }

    // ═══════════════ 项目 ═══════════════
    public static List<GameProject> GetCompletedProjects() => _dev?.CompletedProjects ?? new();
    public static List<GameProject> GetActiveProjects() => _dev?.Projects.Where(p => !p.IsReleased).ToList() ?? new();
    public static GameProject GetCurrentProject(Team team) => team?.CurrentProject;
    public static List<GameProject> GetAllProjects() => _dev?.Projects ?? new();

    /// <summary>读取项目字段</summary>
    public static float GetProjectScore(GameProject p, string field) => field.ToLower() switch
    {
        "graphics" => p?.GraphicsScore ?? 0,
        "gameplay" => p?.GameplayScore ?? 0,
        "audio" => p?.AudioScore ?? 0,
        "story" => p?.StoryScore ?? 0,
        "network" => p?.NetworkScore ?? 0,
        "stability" => p?.StabilityScore ?? 0,
        "final" => p?.FinalScore ?? 0,
        "progress" => (p?.DevProgress ?? 0) * 100,
        "bug" => p?.BugCount ?? 0,
        "debt" => p?.TechDebt ?? 0,
        "sales" => p?.Sales ?? 0,
        "revenue" => p?.Revenue ?? 0,
        "months" => p?.MonthsSpent ?? 0,
        "hype" => p?.MarketingHype ?? 0,
        _ => 0
    };

    /// <summary>修改项目字段</summary>
    public static void SetProjectScore(GameProject p, string field, float value)
    {
        if (p == null) return;
        switch (field.ToLower())
        {
            case "graphics": p.GraphicsScore = Mathf.Clamp(value, 0, 100); break;
            case "gameplay": p.GameplayScore = Mathf.Clamp(value, 0, 100); break;
            case "audio": p.AudioScore = Mathf.Clamp(value, 0, 100); break;
            case "story": p.StoryScore = Mathf.Clamp(value, 0, 100); break;
            case "network": p.NetworkScore = Mathf.Clamp(value, 0, 100); break;
            case "stability": p.StabilityScore = Mathf.Clamp(value, 0, 100); break;
            case "bug": p.BugCount = Mathf.Max(0, (int)value); break;
            case "debt": p.TechDebt = Mathf.Clamp(value, 0, 100); break;
            case "progress": p.DevProgress = Mathf.Clamp(value / 100f, 0, 1); break;
            case "sales": p.Sales = Mathf.Max(0, (int)value); break;
            case "hype": p.MarketingHype = Mathf.Max(0, value); break;
        }
    }

    // ═══════════════ 科技 ═══════════════
    public static bool IsTechResearched(string techId) => _tech?.IsResearched(techId) ?? false;
    public static void UnlockTech(string techId) { if (_tech != null) _tech.ResearchedTech[techId] = true; }
    public static List<string> GetAllTechIds() => _tech?.ResearchedTech.Keys.ToList() ?? new();


    // ═══════════════ 引擎 ═══════════════
    public static List<GameEngine> GetEngines() => _gm?.Engines ?? new();
    public static void AddEngine(GameEngine e) { if (_gm != null) _gm.Engines.Add(e); }
    public static int GetEngineGeneration() => _tech?.EngineGeneration ?? 1;

    // ═══════════════ 办公室 ═══════════════
    public static int GetOfficeTier() => _room != null ? (int)_room.CurrentTier : 0;
    public static void SetOfficeTier(int tier) { if (_room != null) _room.CurrentTier = (HouseTier)tier; }
    public static List<BonusRoom> GetBonusRooms() => _room?.PurchasedBonusRooms.ToList() ?? new();
    public static void BuyBonusRoom(BonusRoom room) { _room?.BuyBonusRoom(room); }

    // ═══════════════ 技术债务 ═══════════════
    public static float GetTotalDebt() => _debt?.ComputeTotalDebt() ?? 0;
    public static bool IsCrunchMode() => _debt?.CrunchMode ?? false;
    public static void SetCrunchMode(bool v) { if (_debt != null) _debt.CrunchMode = v; }
    public static bool HasDebtCrashed() => _debt?.HasCrashed ?? false;
    public static float GetBugRateMultiplier() => _debt?.BugRateMultiplier ?? 1f;
    public static float GetDevSpeedPenalty() => _debt?.DevSpeedPenalty ?? 0;
    public static float GetFatiguePerMonth() => _debt?.FatiguePerMonth ?? 0;
    public static bool IsCrashRecovery() => _debt?.CrashRecoveryMonths > 0;

    // ═══════════════ 服务器 ═══════════════
    public static int GetServerTier() => _server != null ? (int)_server.CurrentTier : 0;
    public static bool UpgradeServer() => _server?.UpgradeServer() ?? false;
    public static int GetServerDemand() => _server?.CurrentDemand ?? 0;
    public static int GetServerCapacity() => _server?.TotalCapacity ?? 0;
    public static bool IsServerOverloaded() => _server?.IsOverloaded ?? false;
    public static float GetServerMonthlyCost() => _server?.MonthlyCost ?? 0;

    // ═══════════════ 工作室 DNA ═══════════════
    public static StudioDNA GetStudioDNA() => _gm?.GetNodeOrNull<StudioDNA>("StudioDNA");
    public static float GetGenreProficiency(string genre)
    {
        var dna = GetStudioDNA();
        if (dna == null || !Enum.TryParse<GameGenre>(genre, true, out var g)) return 0;
        return dna.GenreProficiency.GetValueOrDefault(g, 0);
    }
    public static float GetThemeProficiency(string theme)
    {
        var dna = GetStudioDNA();
        if (dna == null || !Enum.TryParse<GameTheme>(theme, true, out var t)) return 0;
        return dna.ThemeProficiency.GetValueOrDefault(t, 0);
    }
    public static string GetStudioLabel() => GetStudioDNA()?.StudioLabel ?? "";
    public static List<string> GetUnlockedTags() => GetStudioDNA()?.UnlockedTags.ToList() ?? new();

    // ═══════════════ IP 宇宙 ═══════════════
    public static List<string> GetAllIPIds() => IPManager.AllIPs.Keys.ToList();
    public static IPUniverse GetIP(string id) => IPManager.AllIPs.GetValueOrDefault(id);
    public static IPUniverse CreateIP(string id, string name) => IPManager.GetOrCreate(id, name);
    public static int GetIPFanCount(string ipId) => IPManager.AllIPs.GetValueOrDefault(ipId)?.FanCount ?? 0;
    public static int GetIPHeatLevel(string ipId) => IPManager.AllIPs.GetValueOrDefault(ipId)?.HeatLevel ?? 0;
    public static float GetIPSalesBonus(string ipId) => IPManager.AllIPs.GetValueOrDefault(ipId)?.SalesBonus ?? 1f;
    public static void AddGameToIP(string ipId, string title, float score, string genre, string theme, bool isSequel)
    {
        IPManager.AllIPs.GetValueOrDefault(ipId)?.AddGame(title, score, genre, theme, isSequel);
    }

    // ═══════════════ 品牌系统 ═══════════════
    public static BrandSystem GetBrandSystem() => _gm?.GetNodeOrNull<BrandSystem>("BrandSystem");
    public static float GetBrandCoherence()
    {
        var brand = GetBrandSystem();
        if (brand == null) return 0;
        var prop = brand.GetType().GetProperty("BrandCoherence");
        return prop != null ? (float)prop.GetValue(brand) : 0;
    }

    // ═══════════════ 经济系统 ═══════════════
    public static float GetIndustryReputation() => Services.EconomySystemEx?.IndustryReputation ?? 0;
    public static float GetCreativePotential() => Services.EconomySystemEx?.CreativePotential ?? 0;
    public static float GetMarketHeat() => Services.EconomySystemEx?.MarketHeatMultiplier ?? 1f;
    public static float GetCostIndex() => Services.EconomySystemEx?.CostIndex ?? 1f;

    // ═══════════════ 社区系统 ═══════════════
    public static float GetCommunityToxicity() => Services.CommunitySystemEx?.CommunityToxicity ?? 0;
    public static int GetPendingReviewCount() => Services.CommunitySystemEx?.PendingReviews?.Count ?? 0;

    // ═══════════════ 贷款系统 ═══════════════
    public static bool HasActiveLoan() => _gm?.Loan?.HasActiveLoan ?? false;
    public static bool TakeLoan(float amount, float maxLoan) => _gm?.Loan?.TakeLoan(amount, maxLoan) ?? false;
    public static float GetLoanPrincipal() => _gm?.Loan?.Principal ?? 0;
    public static int GetLoanOverdueMonths() => _gm?.Loan?.OverdueMonths ?? 0;

    // ═══════════════ 活动项目 ═══════════════
    public static GameProject CreateProject(string name, GameGenre genre, GameTheme theme,
        Platform platform, float estimatedMonths, MarketingStrategy marketing,
        float marketingBudget, float scale = 0.5f, PriceModel priceModel = PriceModel.BuyToPlay) =>
        _dev?.CreateProject(name, genre, theme, platform, estimatedMonths, marketing, marketingBudget, scale, priceModel);
    public static bool StartDevelopment(GameProject proj, Team team) => _dev?.StartDevelopment(proj, team) ?? false;
    public static void ReleaseGame(Team team) => _dev?.ReleaseGame(team);
    public static void DelayRelease(GameProject proj, int months) { _dev?.DelayRelease(proj, months, 0); }

    // ═══════════════ 弹窗 ═══════════════
    public static void ShowToast(string title, string msg, Color? color = null) =>
        _gm?.ShowToast(title, msg, color ?? Colors.Gold);
    public static void ShowPopup(string title, string msg, Color? color = null) =>
        _gm?.ShowPopup(title, msg, color ?? Colors.White);
    public static void ShowChoicePopup(string title, string desc, string optA, string optB,
        Action onA, Action onB, Color? color = null) =>
        _gm?.ShowChoicePopup(title, desc, optA, optB, onA, onB, color ?? Colors.White);
    public static void ShowTriChoicePopup(string title, string desc, string optA, string optB, string optC,
        Action onA, Action onB, Action onC, Color? color = null) =>
        _gm?.ShowTriChoicePopup(title, desc, optA, optB, optC, onA, onB, onC, color ?? Colors.White);

    // ═══════════════ 事件系统（钩子系统）═══════════════
    public enum GameHook
    {
        BeforeMonthEnd, AfterMonthEnd,
        BeforeProjectCreate, AfterProjectCreate,
        BeforeGameRelease, AfterGameRelease,
        BeforeEmployeeHire, AfterEmployeeHire,
        BeforeResearchComplete, AfterResearchComplete,
        BeforeSprint, AfterSprint,
        BeforeMarketing, AfterMarketing,
        OnGameStart, OnGameLoad,
        BeforeScoreCalc, AfterScoreCalc,
        BeforeMonthlySalary, AfterMonthlySalary,
        BeforeOfficeUpgrade, AfterOfficeUpgrade,
        BeforeServerUpgrade, AfterServerUpgrade,
        BeforeFanEvent, AfterFanEvent,
        BeforeLoanTaken, AfterLoanRepaid,
        BeforeEmployeeLeave, AfterEmployeeLeave,
        BeforeCrisisTrigger, AfterCrisisChoice,
        BeforeBlackSwan, AfterBlackSwanResponse,
        BeforeEngineLicense, AfterEngineLicense,
        OnCompanyIPO, OnCompanyBankruptcy,
        OnQuarterlyReport, OnYearlyReport,
        OnSaveGame, OnLoadGame,
        BeforeCompetitorUpdate, AfterCompetitorUpdate,
        BeforeMarketTrendTick, AfterMarketTrendTick,
        BeforeFanMonthlyUpdate, AfterFanMonthlyUpdate,
        BeforeConsoleLifecycle, AfterConsoleLifecycle,
        BeforeTutorialTick, AfterTutorialTick,
        BeforeMonthlySales, AfterMonthlySales,
        BeforeMonthlyExpSettle, AfterMonthlyExpSettle,
        BeforeAnnualAwards, AfterAnnualAwards,
        BeforeEmployeeSatisfaction, AfterEmployeeSatisfaction,
    }

    private static Dictionary<GameHook, List<Func<bool>>> _cancelHooks = new();
    private static Dictionary<GameHook, List<Action>> _actionHooks = new();
    private static Dictionary<GameHook, List<Func<HookContext, bool>>> _cancelHooksEx = new();
    private static Dictionary<GameHook, List<Action<HookContext>>> _actionHooksEx = new();

    public static void RegisterCancelHook(GameHook hook, Func<bool> handler)
    {
        if (!_cancelHooks[hook].Contains(handler))
            _cancelHooks[hook].Add(handler);
    }

    public static void RegisterActionHook(GameHook hook, Action handler)
    {
        if (!_actionHooks[hook].Contains(handler))
            _actionHooks[hook].Add(handler);
    }

    static ModAPI()
    {
        foreach (GameHook h in Enum.GetValues(typeof(GameHook)))
        {
            _cancelHooks[h] = new List<Func<bool>>();
            _actionHooks[h] = new List<Action>();
            _cancelHooksEx[h] = new List<Func<HookContext, bool>>();
            _actionHooksEx[h] = new List<Action<HookContext>>();
        }
    }

    public static void RegisterCancelHookEx(GameHook hook, Func<HookContext, bool> handler)
    {
        if (!_cancelHooksEx[hook].Contains(handler))
            _cancelHooksEx[hook].Add(handler);
    }

    public static void RegisterActionHookEx(GameHook hook, Action<HookContext> handler)
    {
        if (!_actionHooksEx[hook].Contains(handler))
            _actionHooksEx[hook].Add(handler);
    }

    public static bool IsCancelled(GameHook hook)
    {
        foreach (var h in _cancelHooks[hook])
            try { if (h()) return true; } catch (Exception e) { GD.PrintErr($"[Mod] Cancel hook error: {e.Message}"); }
        return false;
    }

    public static bool IsCancelled(GameHook hook, Dictionary<string, object> args)
    {
        var ctx = new HookContext { Args = args ?? new() };
        foreach (var h in _cancelHooks[hook])
            try { if (h()) return true; } catch (Exception e) { GD.PrintErr($"[Mod] Cancel hook error: {e.Message}"); }
        foreach (var h in _cancelHooksEx[hook])
            try { if (h(ctx)) return true; } catch (Exception e) { GD.PrintErr($"[Mod] Cancel hook ex error: {e.Message}"); }
        return false;
    }

    public static void FireHooks(GameHook hook)
    {
        foreach (var h in _actionHooks[hook])
            try { h(); } catch (Exception e) { GD.PrintErr($"[Mod] Action hook error: {e.Message}"); }
    }

    public static void FireHooks(GameHook hook, Dictionary<string, object> args)
    {
        var ctx = new HookContext { Args = args ?? new() };
        foreach (var h in _actionHooks[hook])
            try { h(); } catch (Exception e) { GD.PrintErr($"[Mod] Action hook error: {e.Message}"); }
        foreach (var h in _actionHooksEx[hook])
            try { h(ctx); } catch (Exception e) { GD.PrintErr($"[Mod] Action hook ex error: {e.Message}"); }
    }

    // ═══════════════ 月度阶段（可覆写的游戏循环片段）═══════════════
    public enum MonthlyPhase
    {
        IPUniverseTick,
        BlackSwanTick,
        AchievementCardCheck,
        DebtCrashRecovery,
        PaySalaries,
        PayRent,
        StoryEventsTick,
        QuarterlyReport,
        IndustryNews,
        WindInsuranceDecay,
        TeamDevelopment,
        TeamChemistry,
        EmployeeExpSettle,
        MonthlySales,
        EmployeeFatigueTick,
        TechDebtTick,
        MarketTrendTick,
        NewSystemsTick,
        LoanProcessing,
        FanUpdate,
        PlayerTrustDecay,
        EmployeePoaching,
        CompetitorTick,
        AudienceTick,
        LiveOpsTick,
        FounderLegacyTick,
        ResourceMonthEnd,
        EngineMaintenance,
        ProfitLogging,
        AnnualAwards,
        EmployeeSatisfaction,
        OutsourceTick,
        PublishingMonthly,
        EngineMonthlyTick,
        ContractRefresh,
        GameOverCheck,
        FanPetition,
        ConsoleLifecycle,
        EraMilestone,
        AchievementCheck,
        ServerMonthlyTick,
        TutorialTick,
        VictoryCheck,
        AutoSave,
    }

    // ═══════════════ 注册回调 ═══════════════
    private static List<Action> _monthlyCallbacks = new();
    private static List<Action<string>> _eventCallbacks = new();
    private static List<Func<GameProject, float>> _scoreModifiers = new();

    public static void RegisterMonthlyCallback(Action cb)
    {
        if (!_monthlyCallbacks.Contains(cb)) _monthlyCallbacks.Add(cb);
    }
    public static void RegisterEventCallback(Action<string> cb)
    {
        if (!_eventCallbacks.Contains(cb)) _eventCallbacks.Add(cb);
    }
    public static void RegisterScoreModifier(Func<GameProject, float> mod)
    {
        if (!_scoreModifiers.Contains(mod)) _scoreModifiers.Add(mod);
    }

    public static void ProcessMonthlyCallbacks()
    {
        foreach (var cb in _monthlyCallbacks)
            try { cb(); } catch (Exception e) { GD.PrintErr($"[Mod] Monthly error: {e.Message}"); }
    }
    public static void ProcessEventCallbacks(string eventId)
    {
        foreach (var cb in _eventCallbacks)
            try { cb(eventId); } catch (Exception e) { GD.PrintErr($"[Mod] Event error: {e.Message}"); }
    }
    public static float ApplyScoreModifiers(GameProject proj, float baseScore)
    {
        float modified = baseScore;
        foreach (var mod in _scoreModifiers)
            try { modified += mod(proj); } catch (Exception e) { GD.PrintErr($"[Mod] Score mod error: {e.Message}"); }
        return modified;
    }

    // ═══════════════ 完全重写开关 ═══════════════
    public static class Features
    {
        public const string EmployeeSystem = "feature.employee";
        public const string FanSystem = "feature.fan";
        public const string TechSystem = "feature.tech";
        public const string MarketTrendSystem = "feature.market_trend";
        public const string CompetitorSystem = "feature.competitor";
        public const string LoanSystem = "feature.loan";
        public const string StoryEventSystem = "feature.story_event";
        public const string OutsourceSystem = "feature.outsource";
        public const string PublishingSystem = "feature.publishing";
        public const string ConsoleLifecycle = "feature.console";
        public const string ServerSystem = "feature.server";
        public const string TutorialSystem = "feature.tutorial";
        public const string AchievementSystem = "feature.achievement";
        public const string EmployeeSatisfaction = "feature.emp_satisfaction";
        public const string LiveOpsSystem = "feature.liveops";
        public const string AudienceSystem = "feature.audience";
        public const string EngineSystem = "feature.engine";
        public const string CrisisSystem = "feature.crisis";
    }

    private static Dictionary<string, bool> _features = new();
    public static void OverrideFeature(string featureId, bool enabled) => _features[featureId] = enabled;
    public static bool IsFeatureOverridden(string featureId) => _features.ContainsKey(featureId);
    public static bool IsFeatureEnabled(string featureId) => _features.GetValueOrDefault(featureId, true);

    // ═══════════════ 成就系统 ═══════════════
    public static void UnlockAchievement(string achievementId) =>
        _gm?.GetNodeOrNull<AchievementManager>("AchievementManager")?.TryUnlock(achievementId, true);
    public static AchievementManager GetAchievementManager() =>
        _gm?.GetNodeOrNull<AchievementManager>("AchievementManager");

    // ═══════════════ Mod 存档数据持久化 ═══════════════
    private static Dictionary<string, Func<string>> _saveSerializers = new();
    private static Dictionary<string, Action<string>> _saveDeserializers = new();

    /// <summary>注册 Mod 存档序列化回调（返回 JSON 字符串）</summary>
    public static void RegisterSaveHandler(string modId, Func<string> serializer, Action<string> deserializer)
    {
        _saveSerializers[modId] = serializer;
        _saveDeserializers[modId] = deserializer;
    }

    /// <summary>构建存档中的 modData 段</summary>
    public static string BuildSaveData()
    {
        var parts = new List<string>();
        foreach (var kv in _saveSerializers)
        {
            try
            {
                string data = kv.Value();
                if (!string.IsNullOrEmpty(data))
                    parts.Add($"\"{kv.Key}\":{data}");
            }
            catch (Exception e) { GD.PrintErr($"[Mod] Save error [{kv.Key}]: {e.Message}"); }
        }
        return string.Join(",", parts);
    }

    /// <summary>从存档中加载 modData</summary>
    public static void LoadSaveData(Dictionary<string, string> modData)
    {
        foreach (var kv in modData)
        {
            if (_saveDeserializers.TryGetValue(kv.Key, out var deserializer))
            {
                try { deserializer(kv.Value); }
                catch (Exception e) { GD.PrintErr($"[Mod] Load error [{kv.Key}]: {e.Message}"); }
            }
        }
    }

    // ═══════════════ Mod 黑名单/冲突管理 ═══════════════
    public static HashSet<string> DisabledModFeatures { get; } = new();
    public static void DisableFeature(string featureId) => DisabledModFeatures.Add(featureId);
    public static void EnableFeature(string featureId) => DisabledModFeatures.Remove(featureId);
    public static bool IsFeatureDisabled(string featureId) => DisabledModFeatures.Contains(featureId);

    // ═══════════════ 自定义 Mod 事件触发 ═══════════════
    public static void TriggerModEvent(string eventId)
    {
        ProcessEventCallbacks(eventId);
        Log($"触发 Mod 事件: {eventId}");
    }
}
