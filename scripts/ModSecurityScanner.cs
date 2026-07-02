using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public enum ModRiskLevel
{
    Low,
    Medium,
    High,
    Critical,
    Dangerous
}

public struct DangerPattern
{
    public string File;
    public int Line;
    public string Code;
    public string Reason;
}

public class ScanResult
{
    public string ModId;
    public ModRiskLevel RiskLevel;
    public List<DangerPattern> Patterns = new();
    public List<string> DllFiles = new();
    public List<string> ScriptFiles = new();
    public bool HasGDScript;
    public int GDScriptCount;
}

public static class ModSecurityScanner
{
    private static readonly Dictionary<string, string> DangerPatterns = new()
    {
        { @"OS\.execute\s*\(", "执行外部命令" },
        { @"OS\.shell_open\s*\(", "打开外部程序" },
        { @"FileAccess\.open\s*\(", "文件操作" },
        { @"FileAccess\.remove\s*\(", "删除文件" },
        { @"DirAccess\.remove\s*\(", "删除文件" },
        { @"DirAccess\.rename\s*\(", "重命名文件" },
        { @"DirAccess\.copy\s*\(", "复制文件" },
        { @"\.dll\b", "加载 DLL" },
        { @"CSharpScript", "加载 C# 脚本" },
        { @"GD\.Load\s*\(", "加载外部代码" },
        { @"load\s*\(\s*""res", "加载外部资源" },
        { @"preload\s*\(\s*""res", "预加载外部资源" },
        { @"HTTPRequest", "网络请求" },
        { @"\.exe\b", "执行可执行文件" },
        { @"\.bat\b", "执行批处理文件" },
        { @"\.ps1\b", "执行 PowerShell 脚本" },
        { @"System\.Diagnostics\.Process", ".NET 进程调用" },
        { @"System\.IO\.File", ".NET 文件操作" },
        { @"System\.Net\.WebClient", ".NET 网络请求" },
        { @"System\.Reflection\.Assembly", ".NET 反射加载" },
    };

    private static readonly string[] DangerousExtensions = { ".dll", ".exe", ".ps1", ".bat", ".cmd", ".vbs", ".js", ".py" };

    public static ScanResult ScanMod(string folderPath, string modId)
    {
        var result = new ScanResult { ModId = modId, RiskLevel = ModRiskLevel.Low };

        if (!Godot.DirAccess.DirExistsAbsolute(folderPath))
            return result;

        var dir = Godot.DirAccess.Open(folderPath);
        if (dir == null) return result;

        ScanDirectory(dir, folderPath, result, "");

        ClassifyRisk(result);
        return result;
    }

    private static void ScanDirectory(Godot.DirAccess dir, string basePath, ScanResult result, string subPath)
    {
        if (dir.ListDirBegin() != Godot.Error.Ok) return;

        while (true)
        {
            var f = dir.GetNext();
            if (string.IsNullOrEmpty(f)) break;
            if (f.StartsWith(".")) continue;

            string fullPath = subPath.Length > 0 ? $"{subPath}/{f}" : f;

            if (dir.CurrentIsDir())
            {
                var subDir = Godot.DirAccess.Open($"{basePath}/{fullPath}");
                if (subDir != null)
                    ScanDirectory(subDir, basePath, result, fullPath);
                continue;
            }

            string ext = System.IO.Path.GetExtension(f).ToLowerInvariant();

            if (ext == ".gd")
            {
                result.HasGDScript = true;
                result.GDScriptCount++;
                ScanGDScript($"{basePath}/{fullPath}", result);
            }
            else if (ext == ".dll")
            {
                result.DllFiles.Add(fullPath);
            }
            else if (DangerousExtensions.Contains(ext))
            {
                result.ScriptFiles.Add(fullPath);
            }
        }

        dir.ListDirEnd();
    }

    private static void ScanGDScript(string filePath, ScanResult result)
    {
        if (!Godot.FileAccess.FileExists(filePath)) return;

        using var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read);
        if (file == null) return;

        string content = file.GetAsText();
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

            foreach (var kv in DangerPatterns)
            {
                if (Regex.IsMatch(line, kv.Key, RegexOptions.IgnoreCase))
                {
                    result.Patterns.Add(new DangerPattern
                    {
                        File = System.IO.Path.GetFileName(filePath),
                        Line = i + 1,
                        Code = line.Truncate(80),
                        Reason = kv.Value
                    });
                }
            }
        }
    }

    private static void ClassifyRisk(ScanResult result)
    {
        bool hasDll = result.DllFiles.Count > 0;
        bool hasScriptFile = result.ScriptFiles.Count > 0;
        bool hasCriticalPattern = result.Patterns.Any(p =>
            p.Reason.Contains("执行") || p.Reason.Contains("删除") ||
            p.Reason.Contains(".NET 进程") || p.Reason.Contains(".NET 反射"));
        bool hasFileOp = result.Patterns.Any(p =>
            p.Reason.Contains("文件操作") || p.Reason.Contains("重命名") || p.Reason.Contains("复制"));
        bool hasAnyPattern = result.Patterns.Count > 0;

        if (hasScriptFile)
            result.RiskLevel = ModRiskLevel.Dangerous;
        else if (hasDll)
            result.RiskLevel = ModRiskLevel.Critical;
        else if (hasCriticalPattern)
            result.RiskLevel = ModRiskLevel.High;
        else if (hasFileOp)
            result.RiskLevel = ModRiskLevel.Medium;
        else if (hasAnyPattern || result.HasGDScript)
            result.RiskLevel = ModRiskLevel.Low;
        else
            result.RiskLevel = ModRiskLevel.Low;
    }

    public static string GetRiskLevelLabel(ModRiskLevel level)
    {
        return level switch
        {
            ModRiskLevel.Low => Loc.Tr("mod_risk.low"),
            ModRiskLevel.Medium => Loc.Tr("mod_risk.medium"),
            ModRiskLevel.High => Loc.Tr("mod_risk.high"),
            ModRiskLevel.Critical => Loc.Tr("mod_risk.critical"),
            ModRiskLevel.Dangerous => Loc.Tr("mod_risk.dangerous"),
            _ => Loc.Tr("mod_risk.unknown"),
        };
    }
}

public static class StringExt
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }
}
