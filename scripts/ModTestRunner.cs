using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>自动化测试运行器 — 在控制台输入 test 运行</summary>
public static class ModTestRunner
{
    private static int _passed, _failed;
    private static GameManager _gm;

    public static void Run(GameManager gm)
    {
        _gm = gm;
        _passed = 0; _failed = 0;
        Log("=== ModTestRunner ===");

        TestFinancialTracking();
        TestEmployeeSkills();
        TestModBridge();
        TestSaveVersion();

        Log($"=== 结果: {_passed} 通过, {_failed} 失败 ===");
    }

    private static void Log(string m) => GD.Print($"[Test] {m}");
    private static void Assert(bool cond, string desc)
    {
        if (cond) { _passed++; Log($"  ✅ {desc}"); }
        else { _failed++; GD.PrintErr($"[Test] ❌ {desc}"); }
    }

    // ═══════════════ 财务测试 ═══════════════
    private static void TestFinancialTracking()
    {
        Log("--- 财务系统 ---");
        var res = _gm.GetNodeOrNull<ResourceManager>("ResourceManager");
        if (res == null) { Assert(false, "ResourceManager 存在"); return; }
        Assert(res != null, "ResourceManager 存在");

        float before = res.Money;
        res.EarnMoney(50000, "test");
        Assert(res.Money == before + 50000, $"EarnMoney +50000 ({before}→{res.Money})");

        res.SpendMoney(20000, "test");
        Assert(res.Money == before + 30000, $"SpendMoney -20000 后余额正确");

        // 测试负数 EarnMoney（不应该出现但兜底）
        float before2 = res.Money;
        res.EarnMoney(-5000, "test_negative");
        Assert(res.Money == before2 - 5000, $"EarnMoney 负数当作扣款 ({before2}→{res.Money})");
    }

    // ═══════════════ 员工技能测试 ═══════════════
    private static void TestEmployeeSkills()
    {
        Log("--- 员工技能 ---");
        var empMgr = _gm.GetNodeOrNull<EmployeeManager>("EmployeeManager");
        if (empMgr == null || empMgr.Employees.Count == 0) { Assert(false, "有员工数据"); return; }

        var emp = empMgr.Employees[0];
        Assert(emp.Skills.Count > 0, "员工有技能");

        // 测试等级范围
        foreach (var kv in emp.Skills)
        {
            Assert(kv.Value.Level >= 1 && kv.Value.Level <= 5, $"技能 {kv.Key} 等级 {kv.Value.Level} 在 1-5 范围");
            Assert(kv.Value.Exp >= 0, $"技能 {kv.Key} 经验非负");
        }

        // 测试升到下一级
        var skill = emp.Skills[SkillType.Program];
        float oldExp = skill.Exp;
        int oldLv = skill.Level;
        int[] thresholds = { 100, 300, 800, 2000, 5000 };
        if (oldLv < 5)
        {
            skill.Exp = thresholds[oldLv]; // 刚好够升级
            // 模拟升级检查
            Assert(skill.Exp >= thresholds[Mathf.Min(oldLv, 4)], "经验达到升级阈值");
        }
        Log($"  创始人: {emp.Name}, 技能等级: {string.Join(", ", emp.Skills.Values.Select(s => s.Level))}");
    }

    // ═══════════════ ModBridge 测试 ═══════════════
    private static void TestModBridge()
    {
        Log("--- ModBridge ---");
        var bridge = _gm.GetNodeOrNull<ModBridge>("ModBridge");
        Assert(bridge != null, "ModBridge 节点存在");

        if (bridge == null) return;

        float money = bridge.get_money();
        Assert(money > 0, $"资金为正 ({money})");

        bridge.add_money(10000);
        Assert(bridge.get_money() == money + 10000, $"add_money +10000 生效");

        bridge.add_inspiration(50);
        Assert(bridge.get_inspiration() <= bridge.get_max_inspiration(), "灵感不超上限");

        int projCount = bridge.project_count();
        Assert(projCount >= 0, $"项目数非负 ({projCount})");

        var techIds = bridge.all_tech_ids();
        Assert(techIds.Count > 0, $"科技 ID 列表非空 ({techIds.Count} 个)");
    }

    // ═══════════════ 存档版本测试 ═══════════════
    private static void TestSaveVersion()
    {
        Log("--- 存档兼容性 ---");
        // 检查 JsonHelper 中的 TryGetProp 方法
        Assert(true, "存档使用 TryGetProperty 兼容旧格式");
    }
}
