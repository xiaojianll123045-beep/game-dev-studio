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

        // ═══════════ 安全检查：扫描 DLL 元数据和内容 ═══════════
        byte[] rawBytes = null;
        try { rawBytes = File.ReadAllBytes(dllPath); } catch { }

        // 1. 检查 unsafe 代码（通过 PE CorFlags）
        if (rawBytes != null && HasUnsafeFlag(rawBytes))
        {
            BlockedAssemblies.Add($"[{fileName}] 包含 unsafe 非托管代码，已被安全系统拦截");
            GD.PrintErr($"[ModAssembly] 已阻止 unsafe 程序集: {fileName}");
            return;
        }

        // 2. 检查 DllImport（扫描 DLL 字符串 + 反射检查）
        if (rawBytes != null && HasDllImport(rawBytes))
        {
            BlockedAssemblies.Add($"[{fileName}] 包含 DllImport 原生调用，已被安全系统拦截");
            GD.PrintErr($"[ModAssembly] 已阻止 DllImport 程序集: {fileName}");
            return;
        }

        // 3. 加载程序集
        var assembly = Assembly.LoadFrom(dllPath);

        // 4. 运行时扫描：检查 Reflection 和 Unsafe 引用
        bool hasReflection = false;
        bool hasUnsafeRuntime = false;
        try
        {
            foreach (var refAsm in assembly.GetReferencedAssemblies())
            {
                string name = refAsm.Name;
                if (name.Contains("System.Reflection") || name == "System.Reflection" ||
                    name == "System.Reflection.Emit" || name == "System.Reflection.Metadata")
                    hasReflection = true;
                if (name.Contains("Unsafe") || name.Contains("unsafe"))
                    hasUnsafeRuntime = true;
            }

            // 检查类型中是否有 DllImport 特性
            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                {
                    if (method.GetCustomAttribute<System.Runtime.InteropServices.DllImportAttribute>() != null)
                    {
                        BlockedAssemblies.Add($"[{fileName}] 检测到 DllImport 方法, 已被拦截");
                        return;
                    }
                }
            }
        }
        catch { /* 某些类型无法遍历，不阻止 */ }

        if (hasReflection)
        {
            BlockedAssemblies.Add($"[{fileName}] 引用了 System.Reflection，已被安全系统拦截");
            GD.PrintErr($"[ModAssembly] 已阻止反射程序集: {fileName}");
            return;
        }
        if (hasUnsafeRuntime)
        {
            BlockedAssemblies.Add($"[{fileName}] 引用了 Unsafe 运行时，已被安全系统拦截");
            GD.PrintErr($"[ModAssembly] 已阻止 unsafe 程序集: {fileName}");
            return;
        }

        // 5. 通过检查，加载 Mod 入口
        var entryTypes = new List<Type>();

        foreach (var type in assembly.GetTypes())
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

    // ═══════════ PE 元数据检查 ═══════════

    /// <summary>检查 DLL 是否包含 unsafe 代码</summary>
    private static bool HasUnsafeFlag(byte[] rawBytes)
    {
        try
        {
            if (rawBytes.Length < 256) return false;

            // PE 签名的偏移（在 DOS 头偏移 0x3C 处）
            int peOffset = BitConverter.ToInt32(rawBytes, 0x3C);
            if (peOffset < 0x40 || peOffset + 4 > rawBytes.Length) return false;

            // PE 签名 "PE\0\0"
            if (rawBytes[peOffset] != 0x50 || rawBytes[peOffset + 1] != 0x45) return false;
            peOffset += 4;

            // COFF 头 20 bytes，之后是 PE 可选头
            // .NET IL Only 标志在 CLR 头的 CorFlags 中
            // CLR 头 RVA 在可选头第 0xE8 字节（PE32+）
            // 简化：搜索 .NET 目录（IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR = 14）
            int peOptionalStart = peOffset + 20;
            if (peOptionalStart + 232 > rawBytes.Length) return false;

            // COM descriptor (entry 14) at optional header offset 0xE8 (PE32+)
            int magic = BitConverter.ToUInt16(rawBytes, peOptionalStart);
            int clrOffset;
            if (magic == 0x20B) // PE32+
                clrOffset = BitConverter.ToInt32(rawBytes, peOptionalStart + 0xE8);
            else // PE32
                clrOffset = BitConverter.ToInt32(rawBytes, peOptionalStart + 0xE0);

            if (clrOffset <= 0 || clrOffset + 8 > rawBytes.Length) return false;

            // CLR header: cb(4) + version(4) + MetaData RVA(4) + MetaData Size(4) + Flags(4)
            int flagsOffset = clrOffset + 16;
            if (flagsOffset + 4 > rawBytes.Length) return false;

            uint flags = BitConverter.ToUInt32(rawBytes, flagsOffset);
            // COMIMAGE_FLAGS_ILONLY = 0x01
            // 如果 ILONLY 未设置，说明包含原生代码或 unsafe block
            bool ilOnly = (flags & 0x01) != 0;
            return !ilOnly;
        }
        catch { return false; }
    }

    /// <summary>检查 DLL 是否引用了 DllImport</summary>
    private static bool HasDllImport(byte[] rawBytes)
    {
        try
        {
            // 在 DLL 原始字节中搜索 DllImport 特征字符串
            string ascii = System.Text.Encoding.ASCII.GetString(rawBytes);
            string unicode = System.Text.Encoding.Unicode.GetString(rawBytes);

            foreach (var pattern in new[]
            {
                "DllImportAttribute",
                "System.Runtime.InteropServices.DllImport",
                "[DllImport",
            })
            {
                if (ascii.Contains(pattern, StringComparison.Ordinal))
                    return true;
                if (unicode.Contains(pattern, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
        catch { return false; }
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
