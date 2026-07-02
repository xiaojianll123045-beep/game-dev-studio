using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public static class ModCommAPI
{
    private static readonly ConcurrentDictionary<string, Dictionary<string, Delegate>> _endpoints = new();
    private static readonly ConcurrentDictionary<string, Dictionary<string, Callable>> _gdEndpoints = new();

    public static void RegisterEndpoint(string modId, string endpoint, Delegate handler)
    {
        var eps = _endpoints.GetOrAdd(modId.ToLower(), _ => new Dictionary<string, Delegate>());
        eps[endpoint.ToLower()] = handler;
    }

    public static void RegisterGDEndpoint(string modId, string endpoint, Callable handler)
    {
        var eps = _gdEndpoints.GetOrAdd(modId.ToLower(), _ => new Dictionary<string, Callable>());
        eps[endpoint.ToLower()] = handler;
    }

    public static object SendMessage(string targetModId, string endpoint, Godot.Collections.Array args)
    {
        endpoint = endpoint.ToLower();
        targetModId = targetModId.ToLower();

        if (_endpoints.TryGetValue(targetModId, out var eps) && eps.TryGetValue(endpoint, out var del))
        {
            var objArgs = new object[args.Count];
            for (int i = 0; i < args.Count; i++) objArgs[i] = args[i];
            return del.DynamicInvoke(objArgs);
        }

        if (_gdEndpoints.TryGetValue(targetModId, out var gdEps) && gdEps.TryGetValue(endpoint, out var call))
            return call.Call(args);

        return null;
    }

    public static Dictionary<string, object> BroadcastMessage(string endpoint, Godot.Collections.Array args)
    {
        var results = new Dictionary<string, object>();
        endpoint = endpoint.ToLower();

        foreach (var kv in _endpoints)
        {
            if (kv.Value.TryGetValue(endpoint, out var del))
            {
                var objArgs = new object[args.Count];
                for (int i = 0; i < args.Count; i++) objArgs[i] = args[i];
                results[kv.Key] = del.DynamicInvoke(objArgs);
            }
        }
        foreach (var kv in _gdEndpoints)
        {
            if (!results.ContainsKey(kv.Key) && kv.Value.TryGetValue(endpoint, out var call))
                results[kv.Key] = call.Call(args);
        }

        return results;
    }

    public static bool HasEndpoint(string modId, string endpoint)
    {
        endpoint = endpoint.ToLower();
        modId = modId.ToLower();
        return (_endpoints.TryGetValue(modId, out var eps) && eps.ContainsKey(endpoint))
            || (_gdEndpoints.TryGetValue(modId, out var gdEps) && gdEps.ContainsKey(endpoint));
    }

    public static List<string> GetModsWithEndpoint(string endpoint)
    {
        endpoint = endpoint.ToLower();
        var result = new List<string>();
        foreach (var kv in _endpoints)
            if (kv.Value.ContainsKey(endpoint)) result.Add(kv.Key);
        foreach (var kv in _gdEndpoints)
            if (kv.Value.ContainsKey(endpoint) && !result.Contains(kv.Key)) result.Add(kv.Key);
        return result;
    }

    public static void UnregisterMod(string modId)
    {
        modId = modId.ToLower();
        _endpoints.TryRemove(modId, out _);
        _gdEndpoints.TryRemove(modId, out _);
    }
}
