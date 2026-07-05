# ModAPI 参考

> **重要：此 API 仅适用于 C# 模组 / 程序集模组。**  
> GDScript 模组必须通过 `bridge.xxx()` 访问（参见《ModBridge_API_参考.md》）。  
> GDScript 模组的唯一有效入口点是 `OnLoad(gm, bridge)`，不能使用 `_ready()`。

---

## 概述

`ModAPI` 是一个 C# 静态类（`public static class ModAPI`），由游戏在启动时初始化。它提供对游戏核心系统的直接访问，包括资金、员工、粉丝、项目、科技、办公室、服务器、债务、品牌、IP宇宙、经济系统等。

**初始化**：`ModAPI.Init(GameManager gm)` 由游戏引擎调用，模组无需手动调用。

---

## 核心访问（属性）

这些属性返回对应管理器的引用，可用于深度操作。

| 属性 | 类型 | 说明 |
|------|------|------|
| `ModAPI.GameManager` | `GameManager` | 游戏主管理器 |
| `ModAPI.Resource` | `ResourceManager` | 资源管理（资金、灵感） |
| `ModAPI.Employees` | `EmployeeManager` | 员工管理 |
| `ModAPI.Development` | `GameDevManager` | 游戏开发管理 |
| `ModAPI.Fans` | `FanManager` | 粉丝管理 |
| `ModAPI.Tech` | `TechManager` | 科技管理 |
| `ModAPI.Room` | `RoomManager` | 办公室/房间管理 |
| `ModAPI.Teams` | `TeamManager` | 团队管理 |
| `ModAPI.Market` | `MarketTrendManager` | 市场趋势管理 |
| `ModAPI.Competitors` | `CompetitorAI` | 竞争对手 AI |
| `ModAPI.Debt` | `TechDebtManager` | 技术债务管理 |
| `ModAPI.Server` | `ServerManager` | 服务器管理 |
| `ModAPI.Story` | `StoryEvents` | 故事事件 |

---

## 资金

```csharp
float ModAPI.GetMoney();
void  ModAPI.SetMoney(float v);
void  ModAPI.AddMoney(float v);
bool  ModAPI.SpendMoney(float v, string reason);
float ModAPI.GetMonthlyIncome();
float ModAPI.GetMonthlyExpense();
```

---

## 灵感

```csharp
float ModAPI.GetInspiration();
float ModAPI.GetMaxInspiration();
void  ModAPI.AddInspiration(float v);
void  ModAPI.SetInspiration(float v);
bool  ModAPI.SpendInspiration(float v);
```

---

## 员工

```csharp
int               ModAPI.GetEmployeeCount();
List<Employee>    ModAPI.GetAllEmployees();
Employee          ModAPI.GetEmployee(int id);
void              ModAPI.AddEmployee(Employee e);
void              ModAPI.RemoveEmployee(Employee e);
void              ModAPI.FireEmployee(Employee e);
```

---

## 粉丝

```csharp
int   ModAPI.GetCasualFans();
int   ModAPI.GetDiehardFans();
int   ModAPI.GetTotalFans();
void  ModAPI.AddCasualFans(int v);
void  ModAPI.AddDiehardFans(int v);
bool  ModAPI.HoldFanEvent(float cost);
int   ModAPI.GetGuaranteedSales();
```

---

## 公司

```csharp
string ModAPI.GetCompanyName();
float  ModAPI.GetPlayerTrust();
void   ModAPI.SetPlayerTrust(float v);
float  ModAPI.GetReputation();
float  ModAPI.GetSharePrice();
bool   ModAPI.IsListed();
```

---

## 时间

```csharp
int   ModAPI.GetMonth();
int   ModAPI.GetYear();
int   ModAPI.GetMonthInYear();
int   ModAPI.GetTotalMonths();
bool  ModAPI.IsPaused();
void  ModAPI.SetPaused(bool v);
void  ModAPI.SetSpeed(int speed); // 1-8
```

---

## 项目

```csharp
List<GameProject> ModAPI.GetCompletedProjects();
List<GameProject> ModAPI.GetActiveProjects();
GameProject       ModAPI.GetCurrentProject(Team team);
List<GameProject> ModAPI.GetAllProjects();
```

**项目字段读写（字符串键，大小写不敏感）：**  
`graphics`, `gameplay`, `audio`, `story`, `network`, `stability`, `final`（只读）, `progress`（0-100）, `bug`, `debt`, `sales`, `revenue`（只读）, `months`（只读）, `hype`

```csharp
float ModAPI.GetProjectScore(GameProject p, string field);
void  ModAPI.SetProjectScore(GameProject p, string field, float value);
```

**项目创建与发布：**

```csharp
GameProject ModAPI.CreateProject(string name, GameGenre genre, GameTheme theme,
    Platform platform, float estimatedMonths, MarketingStrategy marketing,
    float marketingBudget, float scale = 0.5f,
    PriceModel priceModel = PriceModel.BuyToPlay);
bool ModAPI.StartDevelopment(GameProject proj, Team team);
void ModAPI.ReleaseGame(Team team);
void ModAPI.DelayRelease(GameProject proj, int months);
```

---

## 科技

```csharp
bool         ModAPI.IsTechResearched(string techId);
void         ModAPI.UnlockTech(string techId);
List<string> ModAPI.GetAllTechIds();
```

---

## 引擎

```csharp
List<GameEngine> ModAPI.GetEngines();
void             ModAPI.AddEngine(GameEngine e);
int              ModAPI.GetEngineGeneration();
```

---

## 办公室

```csharp
int               ModAPI.GetOfficeTier();
void              ModAPI.SetOfficeTier(int tier);
List<BonusRoom>   ModAPI.GetBonusRooms();
void              ModAPI.BuyBonusRoom(BonusRoom room);
```

---

## 技术债务

```csharp
float ModAPI.GetTotalDebt();
bool  ModAPI.IsCrunchMode();
void  ModAPI.SetCrunchMode(bool v);
bool  ModAPI.HasDebtCrashed();
float ModAPI.GetBugRateMultiplier();
float ModAPI.GetDevSpeedPenalty();
float ModAPI.GetFatiguePerMonth();
bool  ModAPI.IsCrashRecovery();
```

---

## 服务器

```csharp
int   ModAPI.GetServerTier();
bool  ModAPI.UpgradeServer();
int   ModAPI.GetServerDemand();
int   ModAPI.GetServerCapacity();
bool  ModAPI.IsServerOverloaded();
float ModAPI.GetServerMonthlyCost();
```

---

## 工作室 DNA

```csharp
StudioDNA ModAPI.GetStudioDNA();
float     ModAPI.GetGenreProficiency(string genre);   // GameGenre 枚举名
float     ModAPI.GetThemeProficiency(string theme);   // GameTheme 枚举名
string    ModAPI.GetStudioLabel();
List<string> ModAPI.GetUnlockedTags();
```

---

## IP 宇宙

```csharp
List<string> ModAPI.GetAllIPIds();
IPUniverse   ModAPI.GetIP(string id);
IPUniverse   ModAPI.CreateIP(string id, string name);
int          ModAPI.GetIPFanCount(string ipId);
int          ModAPI.GetIPHeatLevel(string ipId);
float        ModAPI.GetIPSalesBonus(string ipId);
void         ModAPI.AddGameToIP(string ipId, string title, float score,
               string genre, string theme, bool isSequel);
```

---

## 品牌系统

```csharp
BrandSystem ModAPI.GetBrandSystem();
float       ModAPI.GetBrandCoherence();
```

---

## 经济系统

```csharp
float ModAPI.GetIndustryReputation();
float ModAPI.GetCreativePotential();
float ModAPI.GetMarketHeat();
float ModAPI.GetCostIndex();
```

---

## 社区系统

```csharp
float ModAPI.GetCommunityToxicity();
int   ModAPI.GetPendingReviewCount();
```

---

## 贷款

```csharp
bool  ModAPI.HasActiveLoan();
bool  ModAPI.TakeLoan(float amount, float maxLoan);
float ModAPI.GetLoanPrincipal();
int   ModAPI.GetLoanOverdueMonths();
```

---

## UI 弹窗

```csharp
void ModAPI.ShowToast(string title, string msg, Color? color = null);
void ModAPI.ShowPopup(string title, string msg, Color? color = null);
void ModAPI.ShowChoicePopup(string title, string desc,
    string optA, string optB, Action onA, Action onB, Color? color = null);
void ModAPI.ShowTriChoicePopup(string title, string desc,
    string optA, string optB, string optC,
    Action onA, Action onB, Action onC, Color? color = null);
```

---

## 成就

```csharp
void                ModAPI.UnlockAchievement(string achievementId);
AchievementManager  ModAPI.GetAchievementManager();
```

---

## 日志

```csharp
void ModAPI.Log(string msg);  // 输出 [Mod] 前缀的日志
```

---

## 钩子系统（GameHook）

`ModAPI.GameHook` 枚举定义了 **68 个**钩子点，分为 Before/After 对以及独立事件。

### 钩子枚举值

| 分类 | 钩子点 |
|------|--------|
| 月份 | `BeforeMonthEnd`, `AfterMonthEnd` |
| 项目 | `BeforeProjectCreate`, `AfterProjectCreate` |
| 发布 | `BeforeGameRelease`, `AfterGameRelease` |
| 雇佣 | `BeforeEmployeeHire`, `AfterEmployeeHire` |
| 研究 | `BeforeResearchComplete`, `AfterResearchComplete` |
| 冲刺 | `BeforeSprint`, `AfterSprint` |
| 营销 | `BeforeMarketing`, `AfterMarketing` |
| 游戏 | `OnGameStart`, `OnGameLoad` |
| 评分 | `BeforeScoreCalc`, `AfterScoreCalc` |
| 工资 | `BeforeMonthlySalary`, `AfterMonthlySalary` |
| 办公室 | `BeforeOfficeUpgrade`, `AfterOfficeUpgrade` |
| 服务器 | `BeforeServerUpgrade`, `AfterServerUpgrade` |
| 粉丝活动 | `BeforeFanEvent`, `AfterFanEvent` |
| 贷款 | `BeforeLoanTaken`, `AfterLoanRepaid` |
| 员工离职 | `BeforeEmployeeLeave`, `AfterEmployeeLeave` |
| 危机 | `BeforeCrisisTrigger`, `AfterCrisisChoice` |
| 黑天鹅 | `BeforeBlackSwan`, `AfterBlackSwanResponse` |
| 引擎授权 | `BeforeEngineLicense`, `AfterEngineLicense` |
| 公司事件 | `OnCompanyIPO`, `OnCompanyBankruptcy` |
| 报告 | `OnQuarterlyReport`, `OnYearlyReport` |
| 存档 | `OnSaveGame`, `OnLoadGame` |
| 竞争对手 | `BeforeCompetitorUpdate`, `AfterCompetitorUpdate` |
| 市场趋势 | `BeforeMarketTrendTick`, `AfterMarketTrendTick` |
| 粉丝月更新 | `BeforeFanMonthlyUpdate`, `AfterFanMonthlyUpdate` |
| 主机生命周期 | `BeforeConsoleLifecycle`, `AfterConsoleLifecycle` |
| 教程 | `BeforeTutorialTick`, `AfterTutorialTick` |
| 月销售 | `BeforeMonthlySales`, `AfterMonthlySales` |
| 月支出 | `BeforeMonthlyExpSettle`, `AfterMonthlyExpSettle` |
| 年度奖项 | `BeforeAnnualAwards`, `AfterAnnualAwards` |
| 员工满意度 | `BeforeEmployeeSatisfaction`, `AfterEmployeeSatisfaction` |

### 注册钩子

```csharp
// 简单动作钩子（无上下文）
void ModAPI.RegisterActionHook(GameHook hook, Action handler);

// 可取消钩子（返回 true 取消事件）
void ModAPI.RegisterCancelHook(GameHook hook, Func<bool> handler);

// 带 HookContext 的动作钩子
void ModAPI.RegisterActionHookEx(GameHook hook, Action<HookContext> handler);

// 带 HookContext 的可取消钩子
void ModAPI.RegisterCancelHookEx(GameHook hook, Func<HookContext, bool> handler);
```

### HookContext

`ModAPI.HookContext` 用于在钩子之间传递数据。

```csharp
public class HookContext
{
    public Dictionary<string, object> Args { get; set; }
    public object ReturnValue { get; set; }
    public bool ReturnValueSet { get; set; }

    public T Get<T>(string key, T fallback = default);
    public void Set(string key, object val);
}
```

---

## 月度阶段（MonthlyPhase）

`ModAPI.MonthlyPhase` 枚举定义了游戏每月循环中的各个阶段，可用于精确控制执行时机：

```
IPUniverseTick, BlackSwanTick, AchievementCardCheck,
DebtCrashRecovery, PaySalaries, PayRent, StoryEventsTick,
QuarterlyReport, IndustryNews, WindInsuranceDecay,
TeamDevelopment, TeamChemistry, EmployeeExpSettle,
MonthlySales, EmployeeFatigueTick, TechDebtTick,
MarketTrendTick, NewSystemsTick, LoanProcessing,
FanUpdate, PlayerTrustDecay, EmployeePoaching,
CompetitorTick, AudienceTick, LiveOpsTick,
FounderLegacyTick, ResourceMonthEnd, EngineMaintenance,
ProfitLogging, AnnualAwards, EmployeeSatisfaction,
OutsourceTick, PublishingMonthly, EngineMonthlyTick,
ContractRefresh, GameOverCheck, FanPetition,
ConsoleLifecycle, EraMilestone, AchievementCheck,
ServerMonthlyTick, TutorialTick, VictoryCheck, AutoSave
```

---

## 回调注册

```csharp
// 每月调用一次
void ModAPI.RegisterMonthlyCallback(Action cb);

// 事件触发时调用（eventId 为事件标识）
void ModAPI.RegisterEventCallback(Action<string> cb);

// 修改游戏项目最终评分（返回增量值）
void ModAPI.RegisterScoreModifier(Func<GameProject, float> mod);
```

---

## 存档持久化

```csharp
// 注册模组存档序列化/反序列化回调
// serializer 返回 JSON 字符串，deserializer 接收 JSON 字符串
void ModAPI.RegisterSaveHandler(string modId,
    Func<string> serializer, Action<string> deserializer);

// 构建存档数据（内部调用，模组一般无需调用）
string ModAPI.BuildSaveData();

// 加载存档数据（内部调用，模组一般无需调用）
void ModAPI.LoadSaveData(Dictionary<string, string> modData);
```

---

## 功能开关（Features）

`ModAPI.Features` 提供预定义的功能标识符常量，可通过 `OverrideFeature` 强制启用/禁用。

### 常量列表

| 常量 | 值 |
|------|-----|
| `Features.EmployeeSystem` | `"feature.employee"` |
| `Features.FanSystem` | `"feature.fan"` |
| `Features.TechSystem` | `"feature.tech"` |
| `Features.MarketTrendSystem` | `"feature.market_trend"` |
| `Features.CompetitorSystem` | `"feature.competitor"` |
| `Features.LoanSystem` | `"feature.loan"` |
| `Features.StoryEventSystem` | `"feature.story_event"` |
| `Features.OutsourceSystem` | `"feature.outsource"` |
| `Features.PublishingSystem` | `"feature.publishing"` |
| `Features.ConsoleLifecycle` | `"feature.console"` |
| `Features.ServerSystem` | `"feature.server"` |
| `Features.TutorialSystem` | `"feature.tutorial"` |
| `Features.AchievementSystem` | `"feature.achievement"` |
| `Features.EmployeeSatisfaction` | `"feature.emp_satisfaction"` |
| `Features.LiveOpsSystem` | `"feature.liveops"` |
| `Features.AudienceSystem` | `"feature.audience"` |
| `Features.EngineSystem` | `"feature.engine"` |
| `Features.CrisisSystem` | `"feature.crisis"` |

### 方法

```csharp
void  ModAPI.OverrideFeature(string featureId, bool enabled);
bool  ModAPI.IsFeatureOverridden(string featureId);
bool  ModAPI.IsFeatureEnabled(string featureId);   // 检查 OverrideFeature 设置
void  ModAPI.DisableFeature(string featureId);     // 加入黑名单
void  ModAPI.EnableFeature(string featureId);      // 从黑名单移除
bool  ModAPI.IsFeatureDisabled(string featureId);  // 检查黑名单
```

---

## 自定义 Mod 事件

```csharp
void ModAPI.TriggerModEvent(string eventId);
```
触发自定义事件会调用所有通过 `RegisterEventCallback` 注册的回调。

---

## 示例

### 示例 1：在游戏加载时修改资金

```csharp
public static void OnLoad(GameManager gm, ModBridge bridge)
{
    ModAPI.RegisterActionHook(ModAPI.GameHook.OnGameLoad, () =>
    {
        ModAPI.AddMoney(50000f);
        ModAPI.ShowToast("模组已加载", "已添加 ¥50,000 启动资金", Colors.Green);
    });
}
```

### 示例 2：拦截游戏发布并修改评分

```csharp
ModAPI.RegisterCancelHookEx(ModAPI.GameHook.BeforeGameRelease, ctx =>
{
    var project = ctx.Get<GameProject>("project");
    if (project != null && project.Name.Contains("测试"))
    {
        ModAPI.SetProjectScore(project, "stability", 95);
        ModAPI.SetProjectScore(project, "gameplay", 90);
    }
    return false; // 不取消事件
});
```

### 示例 3：月度回调 + 存档持久化

```csharp
private static string _myData;

public static void OnLoad(GameManager gm, ModBridge bridge)
{
    ModAPI.RegisterMonthlyCallback(() =>
    {
        float money = ModAPI.GetMoney();
        ModAPI.Log($"当前资金: {money}");
    });

    ModAPI.RegisterSaveHandler("my_mod",
        serializer: () => Json.Serialize(new { lastMoney = ModAPI.GetMoney() }),
        deserializer: (json) => {
            var data = Json.Deserialize<Dictionary<string, object>>(json);
            _myData = data?.GetValueOrDefault("lastMoney")?.ToString();
        });
}
```

### 示例 4：功能开关

```csharp
// 强制禁用竞争对手系统
ModAPI.OverrideFeature(ModAPI.Features.CompetitorSystem, false);

// 启用后检查
if (ModAPI.IsFeatureEnabled(ModAPI.Features.CompetitorSystem))
{
    // 竞争对手系统已启用
}
```

### 示例 5：HookContext 传递数据

```csharp
ModAPI.RegisterActionHookEx(ModAPI.GameHook.BeforeFanEvent, ctx =>
{
    ctx.Set("modifier", 1.5f);
    ModAPI.Log("粉丝事件收益倍率已设置为 1.5x");
});

ModAPI.RegisterActionHookEx(ModAPI.GameHook.AfterFanEvent, ctx =>
{
    float modifier = ctx.Get<float>("modifier", 1.0f);
    int gained = ctx.Get<int>("fansGained", 0);
    int bonus = (int)(gained * (modifier - 1));
    ModAPI.AddCasualFans(bonus);
    ModAPI.Log($"额外获得 {bonus} 名粉丝");
});
```

### 示例 6：评分修饰器

```csharp
ModAPI.RegisterScoreModifier(project =>
{
    if (project.Genre == GameGenre.RPG)
        return 5f; // RPG 项目最终评分 +5
    return 0f;
});
```
