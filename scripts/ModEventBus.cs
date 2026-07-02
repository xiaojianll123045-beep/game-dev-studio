using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 全局 Mod 事件总线 — 游戏任意行为触发事件，Mod 可监听/拦截/
/// 覆盖任何游戏数据变更和状态切换。
/// </summary>
public static class ModEventBus
{
    // ═══════════════ 事件类型 ═══════════════
    public enum EventType
    {
        // 资金
        MoneyChanged, InspirationChanged,
        // 员工
        EmployeeHired, EmployeeFired, EmployeeSatisfactionChanged, EmployeeFatigueChanged, EmployeeSkillUp,
        // 项目
        ProjectCreated, ProjectScoreChanged, ProjectProgressChanged, ProjectBugChanged, ProjectDebtChanged,
        ProjectPhaseChanged, ProjectReleased, ProjectDelayed,
        // 科技
        TechResearched, TechProgressChanged,
        // 游戏时间
        MonthEnd, YearEnd, GameSpeedChanged,
        // 市场
        MarketTrendChanged, FanCountChanged,
        // 公司
        CompanyRenamed, TrustChanged, ReputationChanged, IPOCompleted,
        // 办公室
        OfficeUpgraded, BonusRoomPurchased,
        // 服务器
        ServerUpgraded,
        // 引擎
        EngineAdded, EngineUpgraded, EngineBizModelChanged,
        // Mod
        ModLoaded, ModUnloaded,
        // 自定义（由脚本 Mod 触发）
        Custom,
    }

    /// <summary>事件参数</summary>
    public class EventArgs
    {
        public EventType Type { get; set; }
        public string Key { get; set; }        // 变化的属性名
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public object Source { get; set; }      // 触发源（对象实例）
        public Dictionary<string, object> Extra { get; set; } = new();

        public T Get<T>(string key, T fallback = default) =>
            Extra.TryGetValue(key, out var v) && v is T tv ? tv : fallback;
    }

    // 监听器
    public delegate void EventHandler(EventArgs args);
    public delegate bool CancellableHandler(EventArgs args);

    private static Dictionary<EventType, List<EventHandler>> _listeners = new();
    private static Dictionary<EventType, List<CancellableHandler>> _cancellableListeners = new();

    static ModEventBus()
    {
        foreach (EventType t in Enum.GetValues(typeof(EventType)))
        {
            _listeners[t] = new List<EventHandler>();
            _cancellableListeners[t] = new List<CancellableHandler>();
        }
    }

    /// <summary>注册事件监听</summary>
    public static void Listen(EventType type, EventHandler handler)
    {
        if (!_listeners[type].Contains(handler))
            _listeners[type].Add(handler);
    }

    /// <summary>注册可取消事件监听（返回 true 则阻止操作）</summary>
    public static void ListenCancellable(EventType type, CancellableHandler handler)
    {
        if (!_cancellableListeners[type].Contains(handler))
            _cancellableListeners[type].Add(handler);
    }

    /// <summary>触发事件（返回是否被取消）</summary>
    public static bool Fire(EventType type, string key = null, object oldVal = null, object newVal = null,
        object source = null, Dictionary<string, object> extra = null)
    {
        var args = new EventArgs
        {
            Type = type, Key = key, OldValue = oldVal, NewValue = newVal,
            Source = source, Extra = extra ?? new Dictionary<string, object>(),
        };

        // 先检查取消
        foreach (var h in _cancellableListeners[type])
        {
            try { if (h(args)) return true; }
            catch (Exception e) { GD.PrintErr($"[ModEventBus] Cancel handler error: {e.Message}"); }
        }

        // 再触发监听
        foreach (var h in _listeners[type])
        {
            try { h(args); }
            catch (Exception e) { GD.PrintErr($"[ModEventBus] Listener error: {e.Message}"); }
        }

        return false;
    }

    /// <summary>取消注册监听</summary>
    public static void Unlisten(EventType type, EventHandler handler)
    {
        _listeners[type].Remove(handler);
    }

    public static void UnlistenCancellable(EventType type, CancellableHandler handler)
    {
        _cancellableListeners[type].Remove(handler);
    }

    // ═══════════════ 便捷触发方法 ═══════════════
    public static void FireMoneyChanged(float oldVal, float newVal, object source = null) =>
        Fire(EventType.MoneyChanged, "money", oldVal, newVal, source);

    public static void FireInspirationChanged(float oldVal, float newVal, object source = null) =>
        Fire(EventType.InspirationChanged, "inspiration", oldVal, newVal, source);

    public static bool FireEmployeeHired(Employee emp) =>
        Fire(EventType.EmployeeHired, "employee", null, emp?.Id, emp);

    public static bool FireEmployeeFired(Employee emp) =>
        Fire(EventType.EmployeeFired, "employee", emp?.Id, null, emp);

    public static void FireProjectScoreChanged(GameProject proj, string field, float oldVal, float newVal) =>
        Fire(EventType.ProjectScoreChanged, field, oldVal, newVal, proj);

    public static void FireProjectProgressChanged(GameProject proj, float oldVal, float newVal) =>
        Fire(EventType.ProjectProgressChanged, "progress", oldVal, newVal, proj);

    public static bool FireProjectReleased(GameProject proj) =>
        Fire(EventType.ProjectReleased, "project", null, proj?.Name, proj);

    public static void FireTechResearched(string techId) =>
        Fire(EventType.TechResearched, "tech", null, techId);

    public static void FireMonthEnd(int month) =>
        Fire(EventType.MonthEnd, "month", month - 1, month);

    public static void FireFanCountChanged(int oldVal, int newVal, object source = null) =>
        Fire(EventType.FanCountChanged, "fans", oldVal, newVal, source);

    public static void FireCustom(string eventKey, object data = null)
    {
        var extra = new Dictionary<string, object>();
        if (data != null) extra["data"] = data;
        Fire(EventType.Custom, eventKey, null, null, null, extra);
    }
}
