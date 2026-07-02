using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Mod 方法覆盖注册表 — Mod 可注册任意命名方法的替代/前置/后置逻辑。
/// 游戏代码通过 ModMethodOverride.Call(name, defaultImpl) 调用，
/// Mod 通过 RegisterOverrideRegister(name, handler) 注册覆盖。
/// 
/// 比 Harmony 更安全、更可控，无需 IL 操作。
/// </summary>
public static class ModMethodOverride
{
    /// <summary>覆盖处理器：输入 (方法名, 参数字典, 默认实现) → 输出值</summary>
    public delegate object OverrideHandler(string method, Dictionary<string, object> args, Func<object> original);

    private static readonly Dictionary<string, List<OverrideHandler>> _overrides = new();
    private static readonly Dictionary<string, List<Action<Dictionary<string, object>>>> _beforeHooks = new();
    private static readonly Dictionary<string, List<Action<Dictionary<string, object>, object>>> _afterHooks = new();

    /// <summary>注册方法覆盖（完全替代原逻辑）</summary>
    public static void RegisterOverride(string method, OverrideHandler handler)
    {
        if (!_overrides.ContainsKey(method))
            _overrides[method] = new List<OverrideHandler>();
        _overrides[method].Add(handler);
    }

    /// <summary>注册前置钩子（在方法前执行）</summary>
    public static void RegisterBefore(string method, Action<Dictionary<string, object>> handler)
    {
        if (!_beforeHooks.ContainsKey(method))
            _beforeHooks[method] = new List<Action<Dictionary<string, object>>>();
        _beforeHooks[method].Add(handler);
    }

    /// <summary>注册后置钩子（在方法后执行，可看到返回值）</summary>
    public static void RegisterAfter(string method, Action<Dictionary<string, object>, object> handler)
    {
        if (!_afterHooks.ContainsKey(method))
            _afterHooks[method] = new List<Action<Dictionary<string, object>, object>>();
        _afterHooks[method].Add(handler);
    }

    /// <summary>
    /// 调用可覆盖方法。游戏代码应使用此方式调用关键逻辑。
    /// 如果有 Mod 注册了覆盖，则执行覆盖；否则执行默认实现。
    /// 前置/后置钩子总会执行。
    /// </summary>
    public static T Call<T>(string method, Dictionary<string, object> args, Func<T> original)
    {
        // 前置钩子
        if (_beforeHooks.TryGetValue(method, out var befores))
            foreach (var h in befores)
                try { h(args); } catch (Exception e) { GD.PrintErr($"[ModOverride] Before error [{method}]: {e.Message}"); }

        // 覆盖
        object result;
        if (_overrides.TryGetValue(method, out var handlers) && handlers.Count > 0)
        {
            // 链式调用：最后一个注册的 handler 最先执行
            Func<object> chain = () => original();
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                var handler = handlers[i];
                var next = chain;
                chain = () => handler(method, args, next);
            }
            result = chain();
        }
        else
        {
            result = original();
        }

        // 后置钩子
        if (_afterHooks.TryGetValue(method, out var afters))
            foreach (var h in afters)
                try { h(args, result); } catch (Exception e) { GD.PrintErr($"[ModOverride] After error [{method}]: {e.Message}"); }

        return (T)result;
    }

    /// <summary>无返回值版本</summary>
    public static void CallVoid(string method, Dictionary<string, object> args, Action original)
    {
        if (_beforeHooks.TryGetValue(method, out var befores))
            foreach (var h in befores)
                try { h(args); } catch (Exception e) { GD.PrintErr($"[ModOverride] Before error [{method}]: {e.Message}"); }

        bool overridden = false;
        if (_overrides.TryGetValue(method, out var handlers) && handlers.Count > 0)
        {
            overridden = true;
            Action chain = () => original();
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                var handler = handlers[i];
                var next = chain;
                chain = () => handler(method, args, () => { next(); return null; });
            }
            chain();
        }

        if (!overridden)
            original();

        if (_afterHooks.TryGetValue(method, out var afters))
            foreach (var h in afters)
                try { h(args, null); } catch (Exception e) { GD.PrintErr($"[ModOverride] After error [{method}]: {e.Message}"); }
    }

    /// <summary>构造参数字典</summary>
    public static Dictionary<string, object> Args(params (string, object)[] pairs)
    {
        var d = new Dictionary<string, object>();
        foreach (var (k, v) in pairs) d[k] = v;
        return d;
    }
}
