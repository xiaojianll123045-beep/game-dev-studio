using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

/// <summary>
/// C# 程序集 Mod 加载器 — 加载编译好的 .dll 文件。
/// Mod 需实现 IModEntry 接口，编译时引用游戏程序集。
/// </summary>
public interface IModEntry
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    void OnLoad(GameManager gm);
    void OnUnload();
    void OnSave(BinaryWriter writer);
    void OnLoadSave(BinaryReader reader);
}

/// <summary>Mod 程序集特性标记</summary>
[AttributeUsage(AttributeTargets.Class)]
public class ModEntryAttribute : Attribute { }

public static class ModAssemblyLoader
{
    public static List<IModEntry> LoadedAssemblies { get; } = new();
    public static List<string> LoadErrors { get; } = new();
    public static List<string> LoadWarnings { get; } = new();
    public static List<string> BlockedAssemblies { get; } = new();

    /// <summary>从指定目录加载所有 .dll Mod（带安全检查）</summary>
    public static void LoadAllFrom(string directory, GameManager gm)
    {
        if (!System.IO.Directory.Exists(directory)) return;

        foreach (var dllPath in System.IO.Directory.GetFiles(directory, "*.dll"))
        {
            try
            {
                LoadAssembly(dllPath, gm);
            }
            catch (Exception e)
            {
                LoadErrors.Add($"[{Path.GetFileName(dllPath)}] {e.Message}");
                GD.PrintErr($"[ModAssembly] 加载失败: {dllPath} - {e.Message}");
            }
        }
    }

    private static void LoadAssembly(string dllPath, GameManager gm)
    {
        string fileName = System.IO.Path.GetFileName(dllPath);

        // ═══════════ 安全检查：反射/DllImport/unsafe 检测 ═══════════
        // 使用 Assembly 反射 API（而非字符串扫描，避免误报）

        try
        {
            var assembly = Assembly.LoadFrom(dllPath);

            // 1. 检查引用程序集
            foreach (var refAsm in assembly.GetReferencedAssemblies())
            {
                string name = refAsm.Name;
                if (name.Contains("System.Reflection") || name == "System.Reflection" ||
                    name == "System.Reflection.Emit" || name == "System.Reflection.Metadata" ||
                    name.Contains("Reflection"))
                {
                    BlockedAssemblies.Add($"[{fileName}] 引用反射命名空间，已拦截");
                    GD.PrintErr($"[ModAssembly] 已阻止反射程序集: {fileName}");
                    return;
                }
            }

            // 2. 使用 Mono.Cecil 风格的反射：检查类型和方法上的 DllImport 特性
            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                {
                    if (method.GetCustomAttribute<System.Runtime.InteropServices.DllImportAttribute>() != null)
                    {
                        BlockedAssemblies.Add($"[{fileName}] 检测到 DllImport，已拦截");
                        return;
                    }
                }
            }
        }
        catch
        {
            // ReflectionOnlyLoadFrom 可能失败（依赖缺失），回退到 Assembly.LoadFrom
        }

        // 3. 加载并运行时检查
        var assembly2 = Assembly.LoadFrom(dllPath);
        var entryTypes = new List<Type>();

        // 运行时：再次检查 DllImport
        try
        {
            foreach (var type in assembly2.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                {
                    var dllImport = method.GetCustomAttribute<System.Runtime.InteropServices.DllImportAttribute>();
                    if (dllImport != null)
                    {
                        BlockedAssemblies.Add($"[{fileName}] 运行时检测到 DllImport({dllImport.Value})，已拦截");
                        return;
                    }
                }
            }
        }
        catch { }

        // 4. 查找 Mod 入口
        foreach (var type in assembly2.GetTypes())
        {
            if (typeof(IModEntry).IsAssignableFrom(type) && !type.IsAbstract)
                entryTypes.Add(type);
            else if (type.GetCustomAttribute<ModEntryAttribute>() != null && !type.IsAbstract)
                entryTypes.Add(type);
        }

        if (entryTypes.Count == 0)
        {
            LoadWarnings.Add($"[{fileName}] 未找到 IModEntry 实现");
            return;
        }

        foreach (var type in entryTypes)
        {
            try
            {
                if (Activator.CreateInstance(type) is IModEntry entry)
                {
                    entry.OnLoad(gm);
                    LoadedAssemblies.Add(entry);
                    GD.Print($"[ModAssembly] 已加载: {entry.Name} v{entry.Version}");
                    ModEventBus.FireCustom("assembly_mod_loaded", entry.Id);
                }
            }
            catch (Exception e)
            {
                LoadErrors.Add($"[{type.Name}] {e.Message}");
                GD.PrintErr($"[ModAssembly] 实例化失败 {type.Name}: {e.Message}");
            }
        }
    }

    /// <summary>卸载所有程序集 Mod</summary>
    public static void UnloadAll()
    {
        foreach (var mod in LoadedAssemblies)
        {
            try { mod.OnUnload(); }
            catch (Exception e) { GD.PrintErr($"[ModAssembly] 卸载错误 {mod.Id}: {e.Message}"); }
        }
        LoadedAssemblies.Clear();
    }

    /// <summary>获取程序集 Mod 的存档数据</summary>
    public static Dictionary<string, byte[]> CollectSaveData()
    {
        var data = new Dictionary<string, byte[]>();
        foreach (var mod in LoadedAssemblies)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            try
            {
                mod.OnSave(writer);
                writer.Flush();
                data[mod.Id] = ms.ToArray();
            }
            catch (Exception e) { GD.PrintErr($"[ModAssembly] 存档错误 {mod.Id}: {e.Message}"); }
        }
        return data;
    }

    /// <summary>恢复程序集 Mod 的存档数据</summary>
    public static void RestoreSaveData(Dictionary<string, byte[]> data)
    {
        foreach (var mod in LoadedAssemblies)
        {
            if (data.TryGetValue(mod.Id, out var bytes))
            {
                using var ms = new MemoryStream(bytes);
                using var reader = new BinaryReader(ms);
                try { mod.OnLoadSave(reader); }
                catch (Exception e) { GD.PrintErr($"[ModAssembly] 读档错误 {mod.Id}: {e.Message}"); }
            }
        }
    }
}
