using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

/// <summary>
/// 完整科技树数据（6大分支，48项科技）
/// </summary>
public static class TechTreeData
{
    public static readonly Dictionary<string, TechInfo> AllTech = new()
    {
        // ============ 分支一：程序基础 ============
        ["save_system"] = new TechInfo("save_system", "存档机制", TechBranch.ProgramBase, 0, 5,
            SkillType.Program, 1, null, 0, "", "基础存档/读档", "RPG/AVG必备，无则评分-20"),

        ["memory_v1"] = new TechInfo("memory_v1", "内存管理 V1", TechBranch.ProgramBase, 1, 8,
            SkillType.Program, 1, null, 0, "", "基础内存管理", "稳定性+10%，BUG率-5%"),
        ["memory_v2"] = new TechInfo("memory_v2", "内存管理 V2", TechBranch.ProgramBase, 2, 20,
            SkillType.Program, 2, null, 0, "memory_v1", "高级内存调度", "稳定性+15%，支持开放世界"),

        ["physics_v1"] = new TechInfo("physics_v1", "物理引擎 V1", TechBranch.ProgramBase, 1, 15,
            SkillType.Program, 2, null, 0, "", "刚体碰撞检测", "ACT/赛车基础"),
        ["physics_v2"] = new TechInfo("physics_v2", "物理引擎 V2", TechBranch.ProgramBase, 2, 40,
            SkillType.Program, 3, null, 0, "physics_v1", "布料/流体模拟", "画面真实感+10"),

        ["multithread"] = new TechInfo("multithread", "多线程优化", TechBranch.ProgramBase, 1, 25,
            SkillType.Program, 3, null, 0, "memory_v2", "多核CPU利用", "开发速度+20%"),

        // ============ 分支二：2D渲染 ============
        ["2d_v1"] = new TechInfo("2d_v1", "2D渲染 V1", TechBranch.Render2D, 1, 0,
            SkillType.Art, 1, null, 0, "", "像素风格（开局自带）", "支持像素风"),
        ["2d_v2"] = new TechInfo("2d_v2", "2D渲染 V2", TechBranch.Render2D, 2, 8,
            SkillType.Art, 2, SkillType.Program, 1, "2d_v1", "高清立绘渲染", "2D画面属性上限+15"),
        ["2d_v3"] = new TechInfo("2d_v3", "2D渲染 V3", TechBranch.Render2D, 3, 20,
            SkillType.Art, 3, SkillType.Program, 2, "2d_v2", "骨骼动画系统", "2D开发速度+25%，动态角色"),
        ["2d_v4"] = new TechInfo("2d_v4", "2D渲染 V4", TechBranch.Render2D, 4, 45,
            SkillType.Art, 4, SkillType.Program, 3, "2d_v3", "4K/粒子特效", "画面上限+15，速度+30%"),
        ["2d_v5"] = new TechInfo("2d_v5", "2D渲染 V5", TechBranch.Render2D, 5, 100,
            SkillType.Art, 5, SkillType.Program, 3, "2d_v4", "极致手绘模拟", "2D封神，速度+100%，画面可满分"),

        // ============ 分支三：3D渲染 ============
        ["3d_v1"] = new TechInfo("3d_v1", "3D渲染 V1", TechBranch.Render3D, 1, 20,
            SkillType.Program, 2, SkillType.Art, 2, "2d_v2", "低模/无光照3D", "支持低多边形3D"),
        ["3d_v2"] = new TechInfo("3d_v2", "3D渲染 V2", TechBranch.Render3D, 2, 50,
            SkillType.Program, 3, SkillType.Art, 3, "3d_v1", "标准/光栅化", "PS2~PS3级别3D"),
        ["3d_v3"] = new TechInfo("3d_v3", "3D渲染 V3", TechBranch.Render3D, 3, 120,
            SkillType.Program, 4, SkillType.Art, 4, "3d_v2", "PBR/全局光照", "光追，次世代画面"),
        ["3d_v4"] = new TechInfo("3d_v4", "3D渲染 V4", TechBranch.Render3D, 4, 240,
            SkillType.Program, 5, SkillType.Art, 5, "3d_v3", "实时路径追踪", "电影级渲染，画面可冲100分"),

        ["daynight"] = new TechInfo("daynight", "昼夜更替", TechBranch.Render3D, 1, 15,
            SkillType.Program, 2, SkillType.Art, 1, "3d_v1", "昼夜循环系统", "开放世界沉浸感+15"),
        ["volumetric"] = new TechInfo("volumetric", "体积云/雾", TechBranch.Render3D, 1, 25,
            SkillType.Art, 3, null, 0, "3d_v2", "体积雾渲染", "场景氛围+10"),

        // ============ 分支四：音频 ============
        ["audio_v1"] = new TechInfo("audio_v1", "音效库 V1", TechBranch.Audio, 1, 4,
            SkillType.Audio, 1, null, 0, "", "基础音效", "有声音，不出戏"),
        ["audio_v2"] = new TechInfo("audio_v2", "音效库 V2", TechBranch.Audio, 2, 12,
            SkillType.Audio, 2, null, 0, "audio_v1", "拟真音效", "音效品质+20"),
        ["dynamic_music"] = new TechInfo("dynamic_music", "动态配乐", TechBranch.Audio, 1, 18,
            SkillType.Audio, 3, SkillType.Program, 1, "audio_v2", "战斗/探索无缝配乐", "音乐评分+15"),
        ["spatial_audio"] = new TechInfo("spatial_audio", "3D空间音效", TechBranch.Audio, 1, 22,
            SkillType.Audio, 3, SkillType.Program, 2, "3d_v1", "3D定位音效", "FPS/恐怖沉浸感+20"),

        // ============ 分支五：网络 ============
        ["net_v1"] = new TechInfo("net_v1", "网络联机 V1", TechBranch.Network, 1, 25,
            SkillType.Network, 2, SkillType.Program, 1, "save_system", "P2P双人联机", "支持双人联机"),
        ["net_v2"] = new TechInfo("net_v2", "网络联机 V2", TechBranch.Network, 2, 60,
            SkillType.Network, 3, SkillType.Program, 2, "net_v1", "C/S 32人对战", "小型服务器对战"),
        ["net_v3"] = new TechInfo("net_v3", "网络联机 V3", TechBranch.Network, 3, 150,
            SkillType.Network, 4, SkillType.Program, 4, "net_v2", "MMO千人同服", "大型多人在线，运营前提"),
        ["frame_sync"] = new TechInfo("frame_sync", "断线重连/帧同步", TechBranch.Network, 1, 30,
            SkillType.Network, 3, null, 0, "net_v2", "帧同步系统", "格斗/MOBA必备，否则差评"),
        ["cloud_save"] = new TechInfo("cloud_save", "云存档", TechBranch.Network, 1, 15,
            SkillType.Network, 2, SkillType.Program, 1, "net_v1", "云端存档", "跨平台存档，粘性+10%"),

        // ============ 分支六：AI ============
        ["ai_v1"] = new TechInfo("ai_v1", "AI行为树 V1", TechBranch.AI, 1, 30,
            SkillType.AI, 2, SkillType.Program, 2, "", "基础巡逻/攻击", "NPC有简单逻辑"),
        ["ai_v2"] = new TechInfo("ai_v2", "AI行为树 V2", TechBranch.AI, 2, 70,
            SkillType.AI, 3, SkillType.Program, 3, "ai_v1", "复杂决策AI", "NPC战术配合，SLG/RPG+20"),
        ["pathfinding"] = new TechInfo("pathfinding", "寻路算法", TechBranch.AI, 1, 15,
            SkillType.AI, 1, SkillType.Program, 1, "", "自动寻路", "RTS/开放世界必备"),
        ["ml_ai"] = new TechInfo("ml_ai", "机器学习AI", TechBranch.AI, 1, 200,
            SkillType.AI, 5, SkillType.Network, 3, "ai_v2|multithread", "NPC学习玩家习惯", "动态难度，封神级体验"),
        ["gen_story"] = new TechInfo("gen_story", "生成式剧情", TechBranch.AI, 1, 120,
            SkillType.AI, 4, SkillType.Program, 3, "save_system", "自动生成支线", "开放世界独创性+25"),

        // ============ 分支七：平台与硬件 ============
        ["cross_v1"] = new TechInfo("cross_v1", "跨平台移植 V1", TechBranch.Platform, 1, 30,
            SkillType.Program, 3, null, 0, "", "PC→主机移植", "可多平台发售，销量×1.2"),
        ["cross_v2"] = new TechInfo("cross_v2", "跨平台移植 V2", TechBranch.Platform, 2, 80,
            SkillType.Program, 4, SkillType.Network, 2, "cross_v1", "全平台同步", "PC/主机/手机同步，销量×1.5"),
        ["hardware_design"] = new TechInfo("hardware_design", "自有主机硬件设计", TechBranch.Platform, 1, 200,
            SkillType.Program, 5, SkillType.Art, 3, "", "主机硬件设计", "解锁终极项目（需硬件工程师员工）"),
        ["console_os"] = new TechInfo("console_os", "主机操作系统", TechBranch.Platform, 1, 100,
            SkillType.Program, 4, SkillType.Network, 3, "hardware_design", "主机系统", "商城/好友系统"),
        ["sdk"] = new TechInfo("sdk", "开发套件SDK", TechBranch.Platform, 1, 80,
            SkillType.Program, 4, null, 0, "console_os", "第三方SDK", "可向第三方售卖开发机"),

        // ============ 分支八：游戏类型解锁 ============
        ["genre_rac"] = new TechInfo("genre_rac", "赛车游戏类型", TechBranch.GenreUnlock, 1, 8,
            SkillType.Program, 1, SkillType.Art, 1, "", "解锁赛车游戏", "解锁赛车类型"),
        ["genre_sim"] = new TechInfo("genre_sim", "模拟游戏类型", TechBranch.GenreUnlock, 1, 12,
            SkillType.Program, 2, SkillType.AI, 1, "", "解锁模拟游戏", "解锁模拟类型"),
        ["genre_spo"] = new TechInfo("genre_spo", "体育游戏类型", TechBranch.GenreUnlock, 1, 10,
            SkillType.Program, 2, SkillType.Art, 1, "", "解锁体育游戏", "解锁体育类型"),
        ["genre_mus"] = new TechInfo("genre_mus", "音乐游戏类型", TechBranch.GenreUnlock, 1, 8,
            SkillType.Audio, 2, SkillType.Program, 1, "", "解锁音乐游戏", "解锁音乐类型"),
        ["genre_ftg"] = new TechInfo("genre_ftg", "格斗游戏类型", TechBranch.GenreUnlock, 1, 12,
            SkillType.Program, 2, null, 0, "", "解锁格斗游戏", "解锁格斗类型"),
        ["genre_moba"] = new TechInfo("genre_moba", "MOBA游戏类型", TechBranch.GenreUnlock, 1, 25,
            SkillType.Network, 2, SkillType.Program, 2, "net_v1", "解锁MOBA", "解锁多人在线类型"),
        ["genre_mmo"] = new TechInfo("genre_mmo", "MMO游戏类型", TechBranch.GenreUnlock, 1, 30,
            SkillType.Network, 2, SkillType.Program, 2, "net_v2", "解锁MMO", "解锁大型MMO类型"),
        ["genre_rts"] = new TechInfo("genre_rts", "RTS游戏类型", TechBranch.GenreUnlock, 1, 15,
            SkillType.AI, 2, SkillType.Program, 1, "ai_v1", "解锁即时战略", "解锁即时战略类型"),
        ["genre_hor"] = new TechInfo("genre_hor", "恐怖游戏类型", TechBranch.GenreUnlock, 1, 10,
            SkillType.Audio, 2, SkillType.Art, 1, "", "解锁恐怖游戏", "解锁恐怖类型"),
        ["genre_san"] = new TechInfo("genre_san", "沙盒游戏类型", TechBranch.GenreUnlock, 1, 20,
            SkillType.Program, 3, SkillType.AI, 2, "memory_v2", "解锁沙盒游戏", "解锁沙盒类型"),
        ["genre_rog"] = new TechInfo("genre_rog", "Roguelike类型", TechBranch.GenreUnlock, 1, 15,
            SkillType.Program, 2, SkillType.AI, 1, "pathfinding", "解锁Roguelike", "解锁Roguelike类型"),
        ["genre_vis"] = new TechInfo("genre_vis", "视觉小说类型", TechBranch.GenreUnlock, 1, 5,
            SkillType.Art, 1, null, 0, "", "解锁视觉小说", "解锁视觉小说类型"),
        ["genre_pzl"] = new TechInfo("genre_pzl", "解谜游戏类型", TechBranch.GenreUnlock, 1, 8,
            SkillType.Program, 1, SkillType.Art, 1, "", "解锁解谜游戏", "解锁解谜类型"),

        // ============ 分支九：游戏主题解锁 ============
        ["theme_cyber"] = new TechInfo("theme_cyber", "赛博朋克主题", TechBranch.ThemeUnlock, 1, 10,
            SkillType.Art, 2, SkillType.Program, 1, "", "解锁赛博朋克", "解锁赛博朋克主题"),
        ["theme_steam"] = new TechInfo("theme_steam", "蒸汽朋克主题", TechBranch.ThemeUnlock, 1, 10,
            SkillType.Art, 2, SkillType.Program, 1, "", "解锁蒸汽朋克", "解锁蒸汽朋克主题"),
        ["theme_horror"] = new TechInfo("theme_horror", "恐怖主题", TechBranch.ThemeUnlock, 1, 12,
            SkillType.Audio, 2, SkillType.Art, 1, "", "解锁恐怖主题", "解锁恐怖主题"),
        ["theme_comedy"] = new TechInfo("theme_comedy", "喜剧主题", TechBranch.ThemeUnlock, 1, 8,
            SkillType.Art, 1, null, 0, "", "解锁喜剧主题", "解锁喜剧主题"),
        ["theme_romance"] = new TechInfo("theme_romance", "恋爱主题", TechBranch.ThemeUnlock, 1, 8,
            SkillType.Art, 1, null, 0, "", "解锁恋爱主题", "解锁恋爱主题"),
        ["theme_war"] = new TechInfo("theme_war", "战争主题", TechBranch.ThemeUnlock, 1, 15,
            SkillType.Program, 2, SkillType.AI, 2, "", "解锁战争主题", "解锁战争主题"),
        ["theme_mystery"] = new TechInfo("theme_mystery", "悬疑主题", TechBranch.ThemeUnlock, 1, 12,
            SkillType.Art, 2, SkillType.Audio, 1, "", "解锁悬疑主题", "解锁悬疑主题"),
        ["theme_school"] = new TechInfo("theme_school", "校园主题", TechBranch.ThemeUnlock, 1, 8,
            SkillType.Art, 1, null, 0, "", "解锁校园主题", "解锁校园主题"),
        ["theme_myth"] = new TechInfo("theme_myth", "神话主题", TechBranch.ThemeUnlock, 1, 12,
            SkillType.Art, 2, null, 0, "", "解锁神话主题", "解锁神话主题"),
        ["theme_western"] = new TechInfo("theme_western", "西部主题", TechBranch.ThemeUnlock, 1, 10,
            SkillType.Art, 2, SkillType.Audio, 1, "", "解锁西部主题", "解锁西部主题"),
        ["theme_space"] = new TechInfo("theme_space", "太空主题", TechBranch.ThemeUnlock, 1, 15,
            SkillType.Art, 2, SkillType.Program, 2, "3d_v1", "解锁太空主题", "解锁太空主题"),
    };

    /// <summary>
    /// 获取某分支所有科技（按等级排序）
    /// </summary>
    public static List<TechInfo> GetBranchTech(TechBranch branch)
    {
        var list = new List<TechInfo>();
        foreach (var kv in AllTech)
            if (kv.Value.Branch == branch)
                list.Add(kv.Value);
        list.Sort((a, b) => a.Level.CompareTo(b.Level));
        return list;
    }

    /// <summary>
    /// 检查前置科技是否满足
    /// </summary>
    public static bool PrerequisiteMet(string prereqId, Dictionary<string, bool> researched)
    {
        if (string.IsNullOrEmpty(prereqId)) return true;
        // 支持多前置（用|分隔）
        foreach (var p in prereqId.Split('|'))
        {
            if (!researched.TryGetValue(p.Trim(), out var done) || !done)
                return false;
        }
        return true;
    }

    /// <summary>Mod 数据合并入口（接受 JSON 字符串，按 ID 合并/覆盖科技）</summary>
    public static void MergeFromJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var id = el.GetProperty("id").GetString();
                var tech = new TechInfo(
                    id,
                    el.GetProperty("name").GetString(),
                    Enum.Parse<TechBranch>(el.GetProperty("branch").GetString()),
                    el.GetProperty("level").GetInt32(),
                    el.GetProperty("man_months").GetInt32(),
                    Enum.Parse<SkillType>(el.GetProperty("primary_skill").GetString()),
                    el.GetProperty("primary_skill_level").GetInt32(),
                    el.TryGetProperty("secondary_skill", out var ss) ? Enum.Parse<SkillType>(ss.GetString()) : (SkillType?)null,
                    el.GetProperty("secondary_skill_level").GetInt32(),
                    el.TryGetProperty("prerequisite", out var prereq) ? prereq.GetString() ?? "" : "",
                    el.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                    el.TryGetProperty("effect", out var eff) ? eff.GetString() ?? "" : ""
                );
                AllTech[id] = tech;
            }
            GD.Print($"[Mod][TechTreeData] 已合并 {doc.RootElement.GetArrayLength()} 个科技");
        }
        catch (Exception e) { GD.PrintErr($"[Mod] techs.json parse error: {e.Message}"); }
    }
}
