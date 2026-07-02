using Godot;
using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Mod 资源覆盖系统 — Mod 文件夹中的同名文件覆盖游戏内置资源。
/// Mod 资源放在 user://mods/{modId}/assets/ 下，
/// 路径与游戏资源路径保持一致。
/// </summary>
public static class ModAssetOverride
{
    private static Dictionary<string, string> _overrideMap = new(); // res://path → user://mods/{id}/assets/path
    private static bool _initialized = false;

    /// <summary>初始化：扫描所有已启用 Mod 的资源文件夹</summary>
    public static void Initialize()
    {
        if (_initialized) return;
        _overrideMap.Clear();

        foreach (var mod in ModManager.LoadedMods)
        {
            if (!ModManager.IsEnabled(mod)) continue;
            string assetsDir = mod.Folder + "/assets";
            if (!DirAccess.DirExistsAbsolute(assetsDir)) continue;

            var dir = DirAccess.Open(assetsDir);
            if (dir == null) continue;

            IndexDirectory(dir, assetsDir, "", mod.Id);
        }

        _initialized = true;
        GD.Print($"[ModAssetOverride] 已索引 {_overrideMap.Count} 个资源覆盖");
    }

    private static void IndexDirectory(DirAccess dir, string basePath, string relPath, string modId)
    {
        dir.ListDirBegin();
        while (true)
        {
            var name = dir.GetNext();
            if (string.IsNullOrEmpty(name)) break;
            if (name.StartsWith(".")) continue;

            string fullPath = basePath + "/" + name;
            string gameRelPath = (relPath + "/" + name).TrimStart('/');

            if (dir.CurrentIsDir())
            {
                var subDir = DirAccess.Open(fullPath);
                if (subDir != null) IndexDirectory(subDir, fullPath, gameRelPath, modId);
            }
            else
            {
                // res:// 路径格式，替换反斜杠
                string resPath = "res://" + gameRelPath.Replace("\\", "/");
                // 后面的 Mod 覆盖前面的（优先级按加载顺序）
                _overrideMap[resPath] = fullPath;
            }
        }
        dir.ListDirEnd();
    }

    /// <summary>检查指定资源路径是否有 Mod 覆盖</summary>
    public static bool HasOverride(string resPath)
    {
        return _overrideMap.ContainsKey(resPath);
    }

    /// <summary>获取覆盖资源的实际文件路径</summary>
    public static string GetOverridePath(string resPath)
    {
        return _overrideMap.GetValueOrDefault(resPath);
    }

    /// <summary>加载可能被覆盖的资源（推荐使用此方法替代直接 ResourceLoader.Load）</summary>
    public static T Load<T>(string resPath) where T : class
    {
        if (_overrideMap.TryGetValue(resPath, out var overridePath))
        {
            // 尝试从覆盖路径加载
            var resource = ResourceLoader.Load(overridePath);
            if (resource is T result) return result;
        }
        return ResourceLoader.Load<T>(resPath);
    }

    /// <summary>重新索引（游戏运行时刷新）</summary>
    public static void Refresh()
    {
        _initialized = false;
        Initialize();
    }
}
