using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 发售日事件 + 内部冲突 + 灵感碎片 + 竞争对手交互 — 统一的"故事发生器"
/// </summary>
public partial class StoryEvents : Node
{
    private GameManager _gm;
    private ResourceManager _res;
    private EmployeeManager _empMgr;
    private TeamManager _teamMgr;
    private CompetitorAI _compAI;

    // 已触发的事件ID（避免重复）
    private HashSet<string> _triggeredEvents = new();
    /// <summary>存档用：获取已触发事件列表</summary>
    public List<string> GetTriggeredEvents() => _triggeredEvents.ToList();
    /// <summary>存档用：恢复已触发事件列表</summary>
    public void SetTriggeredEvents(List<string> events) { _triggeredEvents = new HashSet<string>(events); }
    // 灵感碎片库存
    public List<InspirationFragment> Fragments { get; private set; } = new();
    // 行业动态记录
    public List<string> IndustryFeed { get; private set; } = new();

    public override void _Ready()
    {
        _gm = Services.GameManager;
        _res = Services.ResourceManager;
        _empMgr = Services.EmployeeManager;
        _teamMgr = Services.TeamManager;
        _compAI = Services.CompetitorAI;
    }

    // ══════════════════ P0: 发售日事件池 ══════════════════

    public string PickReleaseEvent(GameProject proj, GameDevManager devMgr)
    {
        var pool = new List<(string id, string title, string desc, Action onA, Action onB, Action onC,
            string optA, string optB, string optC)>();

        // 1. 服务器崩溃
        if (proj.ModuleProgressOnline > 0.3f && proj.Sales > 50000 && !_triggeredEvents.Contains("server_crash"))
            pool.Add(("server_crash", "🔥 服务器崩溃！", $"《{proj.Name}》首日销量火爆，服务器扛不住了！",
                () => { _res.SpendMoney(50000, "emergency"); proj.Sales = (int)(proj.Sales * 1.05f); proj.DevLog.Add("紧急扩容服务器，花费5万，销量+5%"); },
                () => { proj.DevLog.Add("发道歉信+送补偿"); proj.Sales = (int)(proj.Sales * 0.95f); },
                () => { proj.Sales = (int)(proj.Sales * 0.85f); proj.DevLog.Add("装死冷漠应对，销量-15%"); },
                "紧急扩容(5万)", "道歉送补偿", "装死(销量-15%)"));

        // 2. 媒体评分争议
        if (proj.FinalScore >= 70 && proj.FinalScore <= 85 && !_triggeredEvents.Contains("review_war"))
            pool.Add(("review_war", "📰 媒体评分争议！", $"几家媒体给《{proj.Name}》打了低分，但玩家评分高得多。",
                () => { if (_res.SpendMoney(30000, "pr")) { proj.FinalScore = Mathf.Min(100, proj.FinalScore + 3); proj.DevLog.Add("买通媒体+3分"); } },
                () => { proj.DevLog.Add("发长文自辩"); proj.FinalScore = Mathf.Min(100, proj.FinalScore + 1); },
                null, "买通媒体(3万)", "发长文自辩", "沉默"));

        // 3. 首日补丁危机
        if (proj.BugCount > 30 && !_triggeredEvents.Contains("day1_patch"))
            pool.Add(("day1_patch", "🐛 首日补丁危机！", $"玩家发现《{proj.Name}》有{proj.BugCount}个BUG在论坛刷屏！",
                () => { proj.BugCount = Math.Max(0, proj.BugCount - 20); proj.DevLog.Add("全员加班修BUG"); },
                () => { proj.DevLog.Add("发道歉信承诺一周内修"); proj.Sales = (int)(proj.Sales * 0.92f); },
                () => { proj.Sales = (int)(proj.Sales * 0.80f); proj.DevLog.Add("装死不理，销量暴跌-20%"); },
                "全员加班修", "道歉+承诺修", "不管"));

        // 4. 盗版泄露
        if (!_triggeredEvents.Contains("piracy"))
            pool.Add(("piracy", "🏴 盗版泄露！", $"《{proj.Name}》的破解版在S平台出现！",
                () => { proj.DevLog.Add("联合平台打击盗版"); },
                () => { proj.DevLog.Add("发公开信呼吁正版"); proj.Sales = (int)(proj.Sales * 0.92f); },
                () => { proj.Sales = (int)(proj.Sales * 0.90f); proj.DevLog.Add("无视盗版，销量-10%"); },
                "联合打击", "呼吁正版", "无视(销量-10%)"));

        // 5. 年度游戏提名
        if (proj.FinalScore >= 85 && !_triggeredEvents.Contains("goty_nom"))
            pool.Add(("goty_nom", "🏆 年度游戏提名！", $"《{proj.Name}》获得了年度游戏提名！",
                () => { proj.DevLog.Add("参加颁奖典礼+高调宣传"); proj.Sales = (int)(proj.Sales * 1.1f); },
                () => { proj.DevLog.Add("参加颁奖典礼"); proj.Sales = (int)(proj.Sales * 1.04f); },
                () => { proj.DevLog.Add("婉拒邀请"); },
                "高调宣传(销量+10%)", "参加典礼", "不去"));

        // 6. 玩家退款潮
        if (proj.FinalScore < 65 && !_triggeredEvents.Contains("refund_wave"))
            pool.Add(("refund_wave", "💰 退款潮！", $"评分太低，玩家纷纷申请退款！",
                () => { proj.Revenue = (int)(proj.Revenue * 0.7f); proj.DevLog.Add("全额退款-30%收入"); },
                () => { proj.Revenue = (int)(proj.Revenue * 0.85f); proj.DevLog.Add("部分退款"); },
                () => { proj.Revenue = (int)(proj.Revenue * 0.55f); proj.Sales = (int)(proj.Sales * 0.85f); proj.DevLog.Add("拒绝退款，口碑崩塌！收入-45%、销量-15%"); },
                "全额退款", "部分退款", "拒绝退款"));

        // 7. 主播合作
        if (proj.FinalScore >= 75 && !_triggeredEvents.Contains("streamer"))
            pool.Add(("streamer", "🎥 大主播想播你的游戏！", $"某百万粉丝主播想免费拿Key推广《{proj.Name}》。",
                () => { proj.Sales = (int)(proj.Sales * 1.1f); proj.DevLog.Add("免费送Key+10%销量"); },
                () => { _res.SpendMoney(20000, "marketing"); proj.Sales = (int)(proj.Sales * 1.15f); proj.DevLog.Add("付费推广+15%销量"); },
                () => { proj.DevLog.Add("拒绝合作"); },
                "免费送Key", "付费推广(2万)", "无视"));

        // 8. 跨平台移植需求
        if (proj.Platform != Platform.All && proj.Sales > 20000 && !_triggeredEvents.Contains("port_req"))
            pool.Add(("port_req", "🔄 玩家呼吁移植！", $"大量玩家要求《{proj.Name}》上其他平台。",
                () => { _res.SpendMoney(80000, "port"); proj.Sales = (int)(proj.Sales * 1.3f); proj.DevLog.Add("移植+30%销量"); },
                () => { proj.DevLog.Add("拒绝移植请求"); },
                () => { proj.DevLog.Add("发起众筹移植，粉丝投票"); },
                "移植(8万)", "拒绝", "众筹移植"));

        // 9. 社区模组
        if (!_triggeredEvents.Contains("modding"))
            pool.Add(("modding", "🔧 玩家想做MOD！", $"社区有人问能不能给《{proj.Name}》做MOD。",
                () => { proj.DevLog.Add("官方支持MOD"); proj.Sales = (int)(proj.Sales * 1.08f); },
                () => { proj.DevLog.Add("不支持MOD"); proj.Sales = (int)(proj.Sales * 0.95f); },
                () => { proj.DevLog.Add("开放收费MOD工坊"); if (_res != null) _res.EarnMoney(30000, "mod_workshop"); },
                "支持MOD(销量+8%)", "不支持(销量-5%)", "收费MOD工坊(+¥3万)"));

        // 10. DLC路线图
        if (proj.FinalScore >= 80 && !_triggeredEvents.Contains("dlc_plan"))
            pool.Add(("dlc_plan", "📋 DLC计划？", $"《{proj.Name}》成功了，粉丝在问有没有DLC。",
                () => { proj.DevLog.Add("发布路线图"); proj.Sales = (int)(proj.Sales * 1.05f); },
                () => { proj.DevLog.Add("宣布免费DLC"); },
                () => { proj.DevLog.Add("暂不回应"); },
                "公布路线图(销量+5%)", "宣布免费DLC", "暂不回应"));

        // 11. 引擎反噬
        if (_compAI != null && proj.DevLog.Any(l => l.Contains("组件联动")))
        {
            var buyer = _compAI.Studios.FirstOrDefault(s => s.HasPlayerEngine);
            if (buyer != null && !_triggeredEvents.Contains("engine_backlash"))
                pool.Add(("engine_backlash", "🗡️ 养虎为患！", $"{buyer.Name}用你的引擎做出了高分游戏！媒体报道铺天盖地...",
                    () => { proj.DevLog.Add("引擎反噬：被自己的技术超越"); proj.Sales = (int)(proj.Sales * 0.90f); },
                    null, null, "接受现实(销量-10%)", null, null));
        }

        // 12. 加班抗议
        if (!_triggeredEvents.Contains("crunch_protest"))
            pool.Add(("crunch_protest", "✊ 员工抗议加班！", "连续高强度开发让团队不堪重负。",
                () => { proj.DevLog.Add("给团队放假休息"); proj.MarketingHype = Mathf.Max(0, proj.MarketingHype - 5); },
                () => { proj.DevLog.Add("无视抗议"); proj.BugCount = Math.Max(0, proj.BugCount + 5); },
                () => { proj.DevLog.Add("给全员加薪安抚"); if (_res != null) _res.SpendMoney(20000, "salary"); proj.Sales = (int)(proj.Sales * 1.03f); },
                "放假休整(热度-5)", "无视(BUG+5)", "加薪安抚(-¥2万,销量+3%)"));

        // Mod 自定义发售事件
        var rng = new Random();
        foreach (var dict in EventModDB.CustomEvents)
        {
            if (dict.TryGetValue("id", out var idEl) && dict.TryGetValue("title", out var titleEl)
                && dict.TryGetValue("description", out var descEl) && !_triggeredEvents.Contains($"mod_release_{idEl}"))
            {
                string evtId = idEl.GetString();
                string evtTitle = titleEl.GetString();
                string evtDesc = descEl.GetString();
                pool.Add((evtId, evtTitle, evtDesc,
                    () => { ModAPI.ProcessEventCallbacks(evtId); },
                    () => { ModAPI.ProcessEventCallbacks(evtId); },
                    () => { ModAPI.ProcessEventCallbacks(evtId); },
                    "确认", "忽略", null));
            }
        }

        if (pool.Count == 0) return null;

        var evt = pool[new Random().Next(pool.Count)];
        _triggeredEvents.Add(evt.id);

        if (_gm != null && GodotObject.IsInstanceValid(_gm))
        {
            if (evt.optC != null)
                _gm.ShowTriChoicePopup(evt.title, evt.desc, evt.optA, evt.optB, evt.optC, evt.onA, evt.onB, evt.onC, new Color(0.9f, 0.5f, 0.2f));
            else
                _gm.ShowChoicePopup(evt.title, evt.desc, evt.optA, evt.optB, evt.onA, evt.onB, new Color(0.9f, 0.5f, 0.2f));
        }

        return evt.title;
    }

    // ══════════════════ P1: 内部冲突事件 ══════════════════

    public string PickInternalEvent()
    {
        if (_empMgr == null || !GodotObject.IsInstanceValid(_gm)) return null;
        var emps = _empMgr.Employees;
        if (emps.Count < 2) return null;
        var rng = new Random();

        // 办公室恋情
        if (emps.Count >= 2 && !_triggeredEvents.Contains($"romance_{_gm.GameMonth}"))
        {
            var a = emps[rng.Next(emps.Count)];
            var b = emps[rng.Next(emps.Count)];
            if (a.Id != b.Id)
            {
                _triggeredEvents.Add($"romance_{_gm.GameMonth}");
                _gm.ShowChoicePopup("办公室恋情",
                    $"{a.Name}和{b.Name}似乎在谈恋爱。",
                    "祝福", "禁止",
                    () => { a.Satisfaction += 10; b.Satisfaction += 10; },
                    () => { a.Satisfaction -= 15; b.Satisfaction -= 15; },
                    new Color(0.9f, 0.5f, 0.7f));
                return $"办公室绯闻: {a.Name}和{b.Name}";
            }
        }

        // 代码风格之争（恩怨系统）
        var techClean = emps.FirstOrDefault(e => e.Trait == EmployeeTrait.TechClean);
        var chill = emps.FirstOrDefault(e => e.Trait == EmployeeTrait.Chill);
        if (techClean != null && chill != null && !_triggeredEvents.Contains($"code_war_{_gm.GameMonth}"))
        {
            _triggeredEvents.Add($"code_war_{_gm.GameMonth}");
            _gm.ShowTriChoicePopup(Loc.Tr("story.code_war_title"), Loc.TrF("story.code_war_desc", techClean.Name, chill.Name),
                Loc.TrF("story.side_back", techClean.Name), Loc.TrF("story.side_back", chill.Name), Loc.Tr("story.side_neutral"),
                () => {
                    techClean.Satisfaction += 10; chill.Satisfaction -= 15;
                    chill.HoldingGrudge = true; chill.GrudgeTargetId = techClean.Id;
                    techClean.IsGrudgeTarget = true;
                    chill.RecordMemory(Loc.TrF("story.mem_grudge", techClean.Name));
                },
                () => {
                    chill.Satisfaction += 10; techClean.Satisfaction -= 15;
                    techClean.HoldingGrudge = true; techClean.GrudgeTargetId = chill.Id;
                    chill.IsGrudgeTarget = true;
                    techClean.RecordMemory(Loc.TrF("story.mem_grudge", chill.Name));
                },
                () => {
                    techClean.Satisfaction += 3; chill.Satisfaction += 3;
                    _gm.ShowPopup(Loc.Tr("story.mediate_ok"), Loc.Tr("story.mediate_ok_msg"), new Color(0.5f, 0.5f, 0.9f));
                },
                new Color(0.7f, 0.5f, 0.3f));
            return "代码大战";
        }

        // ── 事件链 B：怀恨员工消极怠工 ──
        foreach (var e in emps)
        {
            if (!e.HoldingGrudge || e.Satisfaction > 25) continue;
            var target = emps.FirstOrDefault(x => x.Id == e.GrudgeTargetId);
            if (target == null) continue;
            if (_triggeredEvents.Contains($"grudge_slack_{e.Id}")) continue;
            _triggeredEvents.Add($"grudge_slack_{e.Id}");
            e.HoldingGrudge = false; // 怨恨已释放
            target.IsGrudgeTarget = false;
            string title = Loc.Tr("story.grudge_slack_title");
            string desc = Loc.TrF("story.grudge_slack_desc", e.Name, target.Name);
            _gm.ShowChoicePopup(title, desc,
                Loc.Tr("story.grudge_keep"), Loc.Tr("story.grudge_fire"),
                () => {
                    target.Satisfaction -= 10;
                    e.Satisfaction -= 5;
                    e.RecordMemory(Loc.Tr("story.mem_slack"));
                },
                () => {
                    _empMgr.FireEmployee(target);
                    e.Satisfaction += 15;
                    e.RecordMemory(Loc.Tr("story.mem_fire_grudge"));
                    _gm.ShowPopup(Loc.Tr("story.fire_done"), Loc.TrF("story.fire_done_msg", target.Name), new Color(0.9f, 0.3f, 0.3f));
                },
                new Color(0.85f, 0.2f, 0.2f));
            return $"怀恨消极怠工: {e.Name} vs {target.Name}";
        }

        // ── 事件链 C：竞品公司挖走了你的痛处 ──
        for (int i = emps.Count - 1; i >= 0; i--)
        {
            var e = emps[i];
            if (!e.HoldingGrudge) continue;
            if (_triggeredEvents.Contains($"grudge_poach_{e.Id}")) continue;
            // 50% 概率触发（怨恨积累后）
            if (rng.Next(100) < 50) continue;
            _triggeredEvents.Add($"grudge_poach_{e.Id}");
            var target = emps.FirstOrDefault(x => x.Id == e.GrudgeTargetId);
            string poacher = _compAI.Studios.OrderBy(_ => rng.Next()).FirstOrDefault()?.Name ?? "某竞品公司";
            string title = Loc.Tr("story.grudge_poach_title");
            string desc = Loc.TrF("story.grudge_poach_desc", e.Name, poacher);
            _gm.ShowChoicePopup(title, desc,
                Loc.Tr("story.grudge_keep"), Loc.Tr("story.grudge_letgo"),
                () => {
                    e.Satisfaction += 20;
                    e.HoldingGrudge = false;
                    if (target != null) target.IsGrudgeTarget = false;
                    e.RecordMemory(Loc.TrF("story.mem_stay", poacher));
                },
                () => {
                    // 转到竞品公司，带来加成
                    if (target != null) target.IsGrudgeTarget = false;
                    var studio = _compAI.Studios.FirstOrDefault(s => s.Name == poacher);
                    if (studio != null) {
                        studio.EmployeeCount += 1;
                        studio.Reputation = Math.Min(100, studio.Reputation + 5);
                    }
                    _empMgr.FireEmployee(e);
                    _gm.ShowPopup(Loc.Tr("story.poach_done"), Loc.TrF("story.poach_done_msg", e.Name, poacher), new Color(0.8f, 0.5f, 0.1f));
                },
                new Color(0.9f, 0.6f, 0.1f));
            return $"被挖角: {e.Name} → {poacher}";
        }

        // ── 事件链 D：战场兄弟 — 恩怨者共同完成高分项目 → 和解 ──
        var devMgr = _gm.GetNodeOrNull<GameDevManager>("GameDevManager");
        foreach (var e in emps)
        {
            if (!e.HoldingGrudge) continue;
            var target = emps.FirstOrDefault(x => x.Id == e.GrudgeTargetId);
            if (target == null || _triggeredEvents.Contains($"reconcile_{e.Id}_{target.Id}")) continue;
            // 两人必须在同一团队，且该团队当前无项目(说明刚完成一个)
            var team = _teamMgr.Teams.FirstOrDefault(t =>
                t.Members.Contains(e) && t.Members.Contains(target));
            if (team == null || team.CurrentProject != null) continue;
            // 最近完成的高分项目
            var highProj = devMgr?.CompletedProjects?.LastOrDefault(p => p.FinalScore >= 85);
            if (highProj == null) continue;
            _triggeredEvents.Add($"reconcile_{e.Id}_{target.Id}");
            e.HoldingGrudge = false; target.IsGrudgeTarget = false;
            _gm.ShowPopup(
                Loc.Tr("story.reconcile_title"),
                Loc.TrF("story.reconcile_desc", e.Name, target.Name, highProj.Name),
                new Color(0.9f, 0.7f, 0.2f));
            e.Satisfaction += 15; target.Satisfaction += 15;
            e.RecordMemory(Loc.TrF("story.mem_reconcile", target.Name));
            target.RecordMemory(Loc.TrF("story.mem_reconcile", e.Name));
            e.Friends.Add(target.Id);
            return $"战场兄弟: {e.Name} & {target.Name}";
        }

        // ── Mod 自定义事件 ──
        if (EventModDB.CustomEvents.Count > 0 && rng.Next(100) < 30)
        {
            var customEvt = EventModDB.CustomEvents[rng.Next(EventModDB.CustomEvents.Count)];
            if (customEvt.TryGetValue("id", out var evtId))
            {
                string title = customEvt.TryGetValue("title", out var t) ? t.GetString() ?? "Mod Event" : "Mod Event";
                string desc = customEvt.TryGetValue("description", out var d) ? d.GetString() ?? "" : "";
                if (!string.IsNullOrEmpty(desc))
                {
                    _gm.ShowPopup(title, desc, new Color(0.5f, 0.3f, 0.8f));
                    _triggeredEvents.Add($"mod_evt_{evtId}_{_gm.GameMonth}");
                    ModAPI.ProcessEventCallbacks(evtId.GetString());
                    return $"[Mod] {title}";
                }
            }
        }

        return null;
    }

    // ══════════════════ P3: 灵感碎片 ══════════════════

    public struct InspirationFragment
    {
        public string Name;
        public string Source;
        public string EffectDesc;
        public float Bonus;
    }

    public void GainInspirationFragment(int amount)
    {
        var sourcePool = new[]
        {
            ("🌙 梦中灵感", "张三昨晚梦到一个完整的关卡设计"),
            ("🚿 洗澡顿悟", "程序员老王洗澡时突然想到一个绝妙的跳跃机制"),
            ("💬 玩家建议", "一条热评写道：'如果这个游戏能……就好了'"),
            ("🎪 展会启发", "在GDC上看到新技术的演示，灵感迸发"),
            ("📦 外包惊喜", "外包方交来的成品超出预期，带来新的思路"),
            ("👂 偶然听到", "在咖啡店听到两个玩家讨论游戏设计"),
            ("📖 读书心得", "从一本设计书里得到了新的启发"),
            ("🎮 玩到好游戏", "最近玩的一款独立游戏给了团队很大触动"),
        };

        var r = new Random();
        var src = sourcePool[r.Next(sourcePool.Length)];
        var names = new[] { "子弹时间", "动态天气", "非线性叙事", "水彩画风", "物理破坏",
            "潜行机制", "角色成长树", "环境叙事", "分支对话", "随机地图" };
        var frag = new InspirationFragment
        {
            Name = names[r.Next(names.Length)],
            Source = src.Item2,
            EffectDesc = $"可为游戏增加独特特性，属性+{3 + r.Next(3)}" ,
            Bonus = 3 + r.Next(3)
        };
        Fragments.Add(frag);
        _res?.GainInspiration((int)frag.Bonus);
        // 不弹窗，静默增加 — 避免与灵感赌局重复
    }

    /// <summary>
    /// 消耗一个灵感碎片并应用到项目
    /// </summary>
    public string ConsumeFragment(GameProject proj, int index)
    {
        if (index < 0 || index >= Fragments.Count) return null;
        var frag = Fragments[index];
        proj.GameplayScore += frag.Bonus;
        proj.DevLog.Add($"灵感碎片「{frag.Name}」: {frag.Source}，趣味性+{frag.Bonus:F0}");
        Fragments.RemoveAt(index);
        return frag.Name;
    }

    // ══════════════════ 意见①: 灵感赌局 ══════════════════

    /// <summary>开发中随机触发高风险创意提案</summary>
    public void PickInspirationGamble(GameProject proj, Team team)
    {
        if (_gm == null || proj == null || proj.Phase != DevPhase.Developing) return;
        if (_triggeredEvents.Contains($"gamble_{_gm.GameMonth}")) return;
        if (new Random().Next(100) > 20) return;

        var proposals = new (string name, string desc, string warnA, string okB, Action onB, Color color)[]
        {
            ("🎲 激进重构", "团队提议：彻底重写核心玩法模块", "拒绝（谨慎）", "接受！（延期2月，趣味+15，灵感+30）",
                () => { proj.EstimatedMonths += 2; proj.GameplayScore += 15; _res.GainInspiration(30); proj.DevLog.Add("接受激进重构：趣味+15，灵感+30，延期2月"); },
                new Color(0.9f, 0.4f, 0.3f)),
            ("🎲 艺术革命", "美术总监说：我们得换一种全新的视觉风格", "拒绝（谨慎）", "接受！（延期2月，画面+12，灵感+25）",
                () => { proj.EstimatedMonths += 2; proj.GraphicsScore += 12; _res.GainInspiration(25); proj.DevLog.Add("接受艺术革命：画面+12，灵感+25，延期2月"); },
                new Color(0.3f, 0.5f, 0.9f)),
            ("🎲 AI辅助开发", "冒险用未测试的AI工具加速开发", "拒绝（谨慎）", "接受！（进度+15%，BUG+20）",
                () => { proj.DevProgress += 0.15f; proj.BugCount += 20; proj.DevLog.Add("接受AI辅助开发：进度+15%，BUG+20"); },
                new Color(0.2f, 0.7f, 0.7f)),
            ("🎲 极限玩法实验", "加入一个没人做过的机制！", "拒绝（谨慎）", "接受！（延期3月，独创性+20，灵感+35）",
                () => { proj.EstimatedMonths += 3; proj.GameplayScore += 20; _res.GainInspiration(35); proj.DevLog.Add("接受极限玩法实验：趣味+20，灵感+35，延期3月"); },
                new Color(0.8f, 0.3f, 0.8f)),
        };

        var p = proposals[new Random().Next(proposals.Length)];
        _triggeredEvents.Add($"gamble_{_gm.GameMonth}");
        _gm.ShowChoicePopup(p.name, p.desc, p.warnA, p.okB, null, p.onB, p.color);
    }

    // ══════════════════ 意见②: 行业动态墙 ══════════════════

    /// <summary>每月可能产生行业动态</summary>
    public void GenerateIndustryNews()
    {
        if (!GodotObject.IsInstanceValid(_gm) || _compAI == null || _compAI.Studios.Count == 0) return;

        var r = new Random();
        if (r.Next(100) > 22) return;

        var rival = _compAI.Studios[r.Next(_compAI.Studios.Count)];
        var devTeams = _teamMgr.Teams.Where(t => t.CurrentProject != null).ToList();
        bool playerIsDev = devTeams.Count > 0;

        var newsPool = new List<string>();
        if (rival.HasPlayerEngine)
        {
            newsPool.Add($"@{rival.Name}：感谢《游戏开发商》提供的引擎授权，我们的新项目跑得飞快！");
            newsPool.Add($"@{rival.Name}：用你们引擎做的游戏刚拿了85分，不好意思啊老板～");
        }
        if (playerIsDev)
        {
            var proj = devTeams[0].CurrentProject;
            newsPool.Add($"@{rival.Name}：听说有独立工作室在做{proj.Genre.Name()}游戏？祝你好运，这赛道可不是谁都能跑的。");
            newsPool.Add($"@{rival.Name}：这年头做{proj.Theme.Name()}题材的除了我们，还有别人？笑死。");
        }
        newsPool.Add($"@{rival.Name}：刚发售新作！首周销量突破50万！什么是实力？");
        newsPool.Add($"@{rival.Name}：又拿了年度游戏提名，这就是垄断。");
        newsPool.Add($"@{rival.Name}：招人！缺一个Lv4程序员，薪资面议。");

        string news = newsPool[r.Next(newsPool.Count)];
        IndustryFeed.Add($"[{_gm.GameMonth / 12 + 1}年{_gm.GameMonth % 12 + 1}月] {news}");
        if (IndustryFeed.Count > 20) IndustryFeed.RemoveAt(0);

        if (r.Next(100) < 30) // 30%概率弹窗提醒
            _gm.ShowPopup(Loc.Tr("story.industry_news"), news, new Color(0.3f, 0.5f, 0.8f));
    }

    // ══════════════════ 意见④: 粉丝社区互动 ══════════════════

    /// <summary>粉丝社区定期事件</summary>
    public void PickFanCommunityEvent()
    {
        if (!GodotObject.IsInstanceValid(_gm)) return;
        if (new Random().Next(100) > 20) return;
        var fanMgr = Services.FanManager;
        var res = Services.ResourceManager;

        var posts = new (string title, string desc, string optA, string optB, Action onA, Action onB, Color color)[]
        {
            ("📝 高赞帖: 配音需求", "粉丝强烈要求加入日语配音！", "采纳（¥3万，死忠+10%）", "无视",
                () => { if (res.SpendMoney(30000, "voice")) fanMgr.DiehardFans += Mathf.Max(5, (int)(fanMgr.DiehardFans * 0.1f)); },
                () => { },
                new Color(0.3f, 0.5f, 0.9f)),
            ("📝 开发者问答", "社区管理员提议办一次AMA", "举办（¥1万，死忠+5%）", "没空",
                () => { if (res.SpendMoney(10000, "ama")) fanMgr.DiehardFans += Mathf.Max(3, (int)(fanMgr.DiehardFans * 0.05f)); },
                () => { },
                new Color(0.4f, 0.6f, 0.8f)),
            ("📝 差评风暴", "有个差评被顶到首页了！", "诚恳回复（声誉+3）", "删帖（声誉-5）",
                () => { _gm.Engines.ForEach(e => e.Reputation += 3); },
                () => { _gm.Engines.ForEach(e => e.Reputation = Mathf.Max(0, e.Reputation - 5)); },
                new Color(0.9f, 0.3f, 0.2f)),
            ("📝 粉丝创作", "有粉丝画了超棒的FanArt！", "官方转发（粉丝+5%）", "不管",
                () => { fanMgr.CasualFans += Mathf.Max(3, (int)(fanMgr.CasualFans * 0.05f)); },
                () => { },
                new Color(0.9f, 0.6f, 0.2f)),
            ("📝 社区请愿", "2000名玩家联名请求做手游版", "承诺考虑（死忠+8%）", "明确拒绝",
                () => { fanMgr.DiehardFans += Mathf.Max(5, (int)(fanMgr.DiehardFans * 0.08f)); },
                () => { },
                new Color(0.5f, 0.5f, 0.9f)),
        };

        var p = posts[new Random().Next(posts.Length)];
        if (_gm != null) _gm.ShowChoicePopup(p.title, p.desc, p.optA, p.optB, p.onA, p.onB, p.color);
    }

    // ══════════════════ 人格驱动事件 ══════════════════

    /// <summary>根据员工性格触发随机个人事件</summary>
    public void PickPersonalityEvent(Employee emp)
    {
        var rng = new Random();

        switch (emp.Trait)
        {
            case EmployeeTrait.Workaholic:
                if (rng.Next(100) < 40)
                {
                    emp.Fatigue = Mathf.Min(100, emp.Fatigue - 8);
                    emp.Satisfaction += 5;
                    _gm.ShowPopup(Loc.Tr("story.workaholic"), Loc.TrF("story.workaholic_msg", emp.Name), new Color(1f, 0.6f, 0.2f));
                }
                break;

            case EmployeeTrait.Genius:
                if (rng.Next(100) < 25)
                {
                    _res.GainInspiration(5);
                    emp.Satisfaction += 3;
                    _gm.ShowPopup(Loc.Tr("story.genius_idea"), Loc.TrF("story.genius_idea_msg", emp.Name), new Color(0.2f, 0.7f, 0.9f));
                }
                if (rng.Next(100) < 8 && emp.MonthsEmployed > 12)
                {
                    // 天才跳槽
                    _gm.ShowPopup(Loc.Tr("story.genius_leave"), Loc.TrF("story.genius_leave_msg", emp.Name, emp.Salary * 0.5f), new Color(0.9f, 0.3f, 0.3f));
                }
                break;

            case EmployeeTrait.Mentor:
                if (rng.Next(100) < 30 && _empMgr.Employees.Any(e => e.GetHighestLevel() < emp.GetHighestLevel()))
                {
                    var mentee = _empMgr.Employees.Where(e => e.GetHighestLevel() < emp.GetHighestLevel()).OrderBy(_ => rng.Next()).First();
                    // 给被指导者加经验
                    var topSkill = emp.Skills.OrderByDescending(kv => kv.Value.Level).First();
                    mentee.AddExp(topSkill.Key, 30, true);
                    emp.Satisfaction += 2;
                    _gm.ShowPopup(Loc.Tr("story.mentor"), Loc.TrF("story.mentor_msg", emp.Name, mentee.Name, SkillName(topSkill.Key)), new Color(0.3f, 0.7f, 0.4f));
                }
                break;

            case EmployeeTrait.Sensitive:
                if (emp.Satisfaction < 50 && rng.Next(100) < 35)
                {
                    emp.Satisfaction -= 15;
                    _gm.ShowPopup(Loc.Tr("story.crash"), Loc.TrF("story.crash_msg", emp.Name), new Color(0.8f, 0.3f, 0.5f));
                }
                break;

            case EmployeeTrait.Social:
                if (rng.Next(100) < 35 && _empMgr.Employees.Count >= 2)
                {
                    emp.Satisfaction += 8;
                    _gm.ShowPopup("🎉 团队凝聚", $"{emp.Name}（社交达人）组织了一次团建活动，大家都很开心！\n全体满意度+3", new Color(0.5f, 0.5f, 0.9f));
                    foreach (var e in _empMgr.Employees)
                        e.Satisfaction = Mathf.Min(100, e.Satisfaction + 3);
                }
                break;

            case EmployeeTrait.Perfectionist:
                if (rng.Next(100) < 30)
                {
                    emp.Fatigue += 10;
                    emp.Satisfaction -= 3;
                    _gm.ShowPopup("✨ 偏执狂发作", $"{emp.Name}（完美主义）为了一个像素的颜色反复调试了3天。\n疲劳+10", new Color(0.7f, 0.4f, 0.7f));
                }
                break;

            case EmployeeTrait.Lucky:
                if (rng.Next(100) < 20)
                {
                    _res.GainInspiration(8);
                    _res.EarnMoney(20000, "story");
                    // 附加：增加随机小钱/灵感
                    _gm.ShowPopup("🍀 好运降临", $"{emp.Name}（幸运星）在咖啡店偶遇投资人，拿到了赞助！\n资金+2万 灵感+8", new Color(0.2f, 0.9f, 0.2f));
                }
                break;

            case EmployeeTrait.Ambitious:
                if (emp.Satisfaction > 60 && emp.CompanyYears >= 2 && rng.Next(100) < 12)
                {
                    _gm.ShowPopup(Loc.Tr("story.ambitious"), Loc.TrF("story.ambitious_msg", emp.Name), new Color(0.8f, 0.6f, 0.1f));
                }
                break;

            case EmployeeTrait.LoneWolf:
                if (rng.Next(100) < 25)
                {
                    emp.Fatigue -= 5;
                    emp.Satisfaction += 5;
                    _gm.ShowPopup(Loc.Tr("story.lonewolf"), Loc.TrF("story.lonewolf_msg", emp.Name), new Color(0.4f, 0.5f, 0.6f));
                }
                break;

            case EmployeeTrait.TechClean:
                var debtMgr = Services.TechDebtManager;
                if (debtMgr != null && debtMgr.ComputeTotalDebt() > 40 && rng.Next(100) < 30)
                {
                    _gm.ShowPopup("🧹 代码洁癖爆发", $"{emp.Name}（技术洁癖）无法忍受屎山代码，主动承担了一些重构工作。\n债务-3", new Color(0.3f, 0.8f, 0.3f));
                    foreach (var eng in _gm.Engines)
                        eng.TechDebt = Mathf.Max(0, eng.TechDebt - 3);
                }
                break;

            case EmployeeTrait.Nostalgic:
                if (emp.CompanyYears >= 3 && rng.Next(100) < 20)
                {
                    emp.Satisfaction += 10;
                    _gm.ShowPopup(Loc.Tr("story.nostalgic"), Loc.TrF("story.nostalgic_msg", emp.Name), new Color(0.7f, 0.6f, 0.4f));
                }
                break;

            case EmployeeTrait.Chill:
                if (rng.Next(100) < 25)
                {
                    emp.Fatigue = Mathf.Max(0, emp.Fatigue - 10);
                    _gm.ShowPopup("🧘 冥想时间", $"{emp.Name}（佛系）在工位上冥想，完全不在意截止日期。\n疲劳-10", new Color(0.5f, 0.7f, 0.5f));
                }
                break;
        }
    }

    private static string SkillName(SkillType s) => s.Name();

    // ══════════════════ 趣味事件池 ══════════════════
    // 每月5%概率触发，提供"哇塞/我靠/哈哈哈"时刻

    public void PickJuicyEvent()
    {
        if (_teamMgr == null || !GodotObject.IsInstanceValid(_gm)) return;
        var rng = new Random();
        if (rng.Next(100) > 3) return; // 每月3%概率

        var pool = new List<(string id, string title, string desc, string optA, string optB, Action onA, Action onB, Color color)>();

        var devTeams = _teamMgr.Teams.Where(t => t.CurrentProject != null).ToList();
        var hasDev = devTeams.Count > 0;

        // ── 开发期间事件 ──
        if (hasDev)
        {
            var proj = devTeams[0].CurrentProject;

            // 1. 员工疯狂创意
            var creativeEmp = _empMgr.Employees.FirstOrDefault(e =>
                e.Trait == EmployeeTrait.Genius && !_triggeredEvents.Contains($"crazy_{_gm.GameMonth}"));
            if (creativeEmp != null && proj.Phase == DevPhase.Developing)
            {
                var proposals = new (string title, string desc, string optA, string optB, Action onA, Action onB)[]
                {
                    ("🎸 我要做音乐游戏！", $"{creativeEmp.Name}（天才）说想加入节奏玩法",
                        "支持他做原型", "让他专心干正事",
                        () => { proj.EstimatedMonths += 2; proj.GameplayScore += 15; },
                        () => creativeEmp.Satisfaction -= 20),
                    ("🐱 加猫！", $"{creativeEmp.Name}说：每个NPC都应该是猫！",
                        "加猫！", "不加",
                        () => { proj.EstimatedMonths += 1; },
                        () => {}),
                    ("🧀 奶酪机制", $"{creativeEmp.Name}认为加入模拟奶酪发酵更真实",
                        "试试看", "驳回",
                        () => { proj.EstimatedMonths += 1; proj.GameplayScore += new Random().Next(-5, 13); },
                        () => creativeEmp.Satisfaction -= 10),
                };
                var p = proposals[rng.Next(proposals.Length)];
                _triggeredEvents.Add($"crazy_{_gm.GameMonth}");
                _gm.ShowChoicePopup(p.title, p.desc, p.optA, p.optB, p.onA, p.onB, new Color(0.8f, 0.4f, 0.9f));
                return;
            }

            // 2. 彩蛋事件
            var nostalgicEmp = _empMgr.Employees.FirstOrDefault(e =>
                e.Trait == EmployeeTrait.Nostalgic && !_triggeredEvents.Contains($"easter_{_gm.GameMonth}"));
            if (nostalgicEmp != null && proj.Phase == DevPhase.Developing)
            {
                _triggeredEvents.Add($"easter_{_gm.GameMonth}");
                _gm.ShowChoicePopup("🥚 有人在代码里藏了彩蛋",
                    $"{nostalgicEmp.Name}（恋旧）偷偷藏了一个致敬彩蛋",
                    "保留彩蛋", "要求删除",
                    () => proj.DevLog.Add("彩蛋被保留！发售后玩家疯狂猜测"),
                    () => nostalgicEmp.Satisfaction -= 10,
                    new Color(0.9f, 0.7f, 0.2f));
                return;
            }
        }

        // ── 行业/对手事件 ──
        if (_compAI != null && _compAI.Studios.Count > 0)
        {
            var rival = _compAI.Studios[rng.Next(_compAI.Studios.Count)];

            // 3. 公开嘲讽
            if (!_triggeredEvents.Contains($"taunt_{_gm.GameMonth}"))
            {
                _triggeredEvents.Add($"taunt_{_gm.GameMonth}");
                _gm.ShowChoicePopup($"💢 {rival.Name}公开嘲讽你们！",
                    $"{rival.Name}的CEO说：\"某些小工作室做的都是幼儿园游戏。\"",
                    "发推回怼", "沉默",
                    () => {
                        var fm = Services.FanManager;
                        fm.CasualFans += Mathf.Max(8, (int)(fm.CasualFans * 0.08f));
                        _gm.Engines.ForEach(e => e.Reputation += 5);
                        _gm.ShowPopup(Loc.Tr("story.roast_war"), Loc.TrF("story.roast_war_msg", rival.Name), new Color(1f, 0.5f, 0.2f));
                    },
                    () => {},
                    new Color(1f, 0.3f, 0.2f));
                return;
            }
        }

        // ── 玩家/社区事件 ──
        var fanMgr = Services.FanManager;

        if (fanMgr.DiehardFans > 10 && !_triggeredEvents.Contains($"fanletter_{_gm.GameMonth}"))
        {
            _triggeredEvents.Add($"fanletter_{_gm.GameMonth}");
            _gm.ShowChoicePopup("💌 一封手写信",
                "有位10岁小粉丝寄来了手写感谢信，说你陪他度过了化疗。",
                "公开回信", "私信感谢",
                () => fanMgr.CasualFans += Mathf.Max(10, (int)(fanMgr.CasualFans * 0.2f)),
                () => fanMgr.DiehardFans += Mathf.Max(5, (int)(fanMgr.DiehardFans * 0.1f)),
                new Color(0.9f, 0.6f, 0.7f));
            return;
        }

        if (!_triggeredEvents.Contains($"catkey_{_gm.GameMonth}") && _gm.GameMonth > 6)
        {
            _triggeredEvents.Add($"catkey_{_gm.GameMonth}");
            var catFanMgr = _gm.GetNodeOrNull<FanManager>("FanManager");
            var catEmpMgr = _gm.GetNodeOrNull<EmployeeManager>("EmployeeManager");
            _gm.ShowChoicePopup("🐱 办公室来了一只猫",
                "一只橘猫不知从哪溜进来，跳上服务器踩了几脚。",
                "收养它(员工开心+粉丝涨)", "赶走(无事发生)",
                () => {
                    if (catFanMgr != null) { catFanMgr.CasualFans += 500; catFanMgr.DiehardFans += 50; }
                    if (catEmpMgr != null) foreach (var e in catEmpMgr.Employees) e.Satisfaction = Mathf.Min(100, e.Satisfaction + 5);
                },
                () => {},
                new Color(1f, 0.6f, 0.2f));
            return;
        }

        var highScoreProj = Services.GameDevManager.CompletedProjects
            .FirstOrDefault(p => p.FinalScore >= 85);
        if (highScoreProj != null && !_triggeredEvents.Contains($"textbook_{_gm.GameMonth}"))
        {
            _triggeredEvents.Add($"textbook_{_gm.GameMonth}");
            var res = Services.ResourceManager;
            _gm.ShowChoicePopup("📖 你的游戏被大学选为教材！",
                $"《{highScoreProj.Name}》被某大学游戏设计专业选为教材案例！",
                "免费授权", "收费授权",
                () => _gm.Engines.ForEach(e => e.Reputation += 30),
                () => res.EarnMoney(100000, "textbook"),
                new Color(0.3f, 0.7f, 0.9f));
            return;
        }

        if (_gm.GameMonth > 12 && !_triggeredEvents.Contains($"pcbreak_{_gm.GameMonth}"))
        {
            _triggeredEvents.Add($"pcbreak_{_gm.GameMonth}");
            var res2 = Services.ResourceManager;
            _gm.ShowChoicePopup("💻 主程序员电脑冒烟了",
                "核心开发机的显卡烧了！\"我代码还没Push啊！\"",
                "紧急换新(¥3万)", "尝试修复(¥5千)",
                () => res2.SpendMoney(30000, "pc"),
                () => { if (rng.Next(2) == 0) { res2.SpendMoney(5000, "pcfix"); _gm.ShowPopup(Loc.Tr("story.pcfix_ok"), Loc.Tr("story.pcfix_ok_msg"), new Color(0.3f, 0.8f, 0.3f)); } else { res2.SpendMoney(5000, "pcfix"); _gm.ShowPopup(Loc.Tr("story.pcfix_fail"), Loc.Tr("story.pcfix_fail_msg"), new Color(0.9f, 0.3f, 0.3f)); } },
                new Color(0.6f, 0.3f, 0.3f));
            return;
        }

        if (!_triggeredEvents.Contains($"holiday_{_gm.GameMonth}") && _gm.GameMonth % 12 == 0)
        {
            _triggeredEvents.Add($"holiday_{_gm.GameMonth}");
            _gm.ShowChoicePopup("🎄 圣诞加班",
                "年底了，对手都放假了，这正是赶工的好时机！",
                "全员放假", "自愿加班",
                () => _empMgr.Employees.ForEach(e => e.Satisfaction = Mathf.Min(100, e.Satisfaction + 20)),
                () => _empMgr.Employees.ForEach(e => e.Fatigue = Mathf.Min(100, e.Fatigue + 20)),
                new Color(0.2f, 0.8f, 0.2f));
            return;
        }
    }

    // ══════════════════ 季度审查 ══════════════════
    // 每3个月弹出趋势报告

    public void ShowQuarterlyReview()
    {
        if (_teamMgr == null || !GodotObject.IsInstanceValid(_gm)) return;
        var teams = _teamMgr.Teams.Where(t => t.CurrentProject != null).ToList();
        if (teams.Count == 0) return;

        var debtMgr = _gm.GetNodeOrNull<TechDebtManager>("TechDebtManager");
        float debt = debtMgr?.ComputeTotalDebt() ?? 0;
        var debtTrend = debt > 50 ? "🔴" : debt > 25 ? "🟡" : "🟢";

        var empMgr = Services.EmployeeManager;
        float avgSat = empMgr.Employees.Where(e => e.Name != Loc.Tr("person.founder_name")).Select(e => e.Satisfaction).DefaultIfEmpty(70).Average();
        var satTrend = avgSat < 40 ? "🔴" : avgSat < 60 ? "🟡" : "🟢";

        var projLines = new System.Text.StringBuilder();
        foreach (var team in teams.Take(2))
        {
            var proj = team.CurrentProject;
            string phase = proj.Phase switch
            {
                DevPhase.Developing => $"{proj.DevProgress*100:F0}%",
                DevPhase.Polishing => $"打磨BUG:{proj.BugCount}",
                _ => proj.Phase.Name()
            };
            projLines.AppendLine($"《{proj.Name}》{phase}");
        }

        string report = $"技术债务 {debtTrend} {debt:F0}/100\n" +
                        $"员工满意度 {satTrend} {avgSat:F0}/100\n" +
                        $"资金 ¥{Services.ResourceManager.Money:N0}\n\n" +
                        $"{projLines}";

        // 竞争对手发行
        var compAI = Services.CompetitorAI;
        var recent = compAI.UpcomingReleases.Where(r => r.monthsUntil <= 1).Take(2).ToList();
        if (recent.Count > 0)
        {
            report += "\n";
            foreach (var r in recent)
                report += $"🏢 {r.studio.Name}发售《{r.game.Name}》({r.game.Score:F0}分)\n";
        }

        _gm.ShowToast($"📊 Q{_gm.GameMonth / 3 % 4 + 1} · 第{_gm.GameYear}年", report, new Color(0.4f, 0.7f, 0.9f), 8f);
    }

    // ══════════════════ 叙事增强：开发里程碑事件 ══════════════════

    /// <summary>项目开发达到里程碑时触发叙事</summary>
    public void CheckDevMilestones(GameProject proj, Team team)
    {
        if (proj == null || proj.IsReleased || !GodotObject.IsInstanceValid(_gm)) return;
        var rng = new Random();

        // 首次达到50%进度
        if (proj.DevProgress >= 0.5f && proj.DevProgress < 0.55f && !_triggeredEvents.Contains($"milestone_half_{proj.Name}"))
        {
            _triggeredEvents.Add($"milestone_half_{proj.Name}");
            var messages = new[]
            {
                $"📐 开发过半！《{proj.Name}》的核心框架已经成型，团队士气高涨。",
                $"🎯 进度过半！{team.Members.FirstOrDefault()?.Name ?? "团队"}在走廊里兴奋地讨论已经实现的功能。",
                $"⚡ 过半里程碑！游戏的雏形已经可以试玩，办公室弥漫着期待的气氛。",
            };
            _gm.ShowToast("开发里程碑", messages[rng.Next(messages.Length)], new Color(0.3f, 0.7f, 0.5f), 5f);
        }

        // 发售前最后一个月（打磨阶段第2月起）
        if (proj.Phase == DevPhase.Polishing && proj.PolishMonths == 1 && !_triggeredEvents.Contains($"final_stretch_{proj.Name}"))
        {
            _triggeredEvents.Add($"final_stretch_{proj.Name}");
            var messages = new[]
            {
                $"🏁 进入最后冲刺！《{proj.Name}》即将交付，团队正在进行最后的打磨。",
                $"🔥 终局冲刺！咖啡机和加班餐成了办公室的主角。",
                $"⏰ 倒计时！《{proj.Name}》距离发售只剩最后的打磨。",
            };
            _gm.ShowToast("最后冲刺", messages[rng.Next(messages.Length)], new Color(0.9f, 0.6f, 0.2f), 5f);
        }
    }

    /// <summary>生成氛围开发日志条目</summary>
    public string GenerateFlavorLog(GameProject proj)
    {
        var rng = new Random();
        var logs = new[]
        {
            $"💡 有人提出了一个关于{proj.Genre.Name()}玩法的有趣想法。",
            $"🎨 美术组为新角色设计了{3 + rng.Next(8)}版草图。",
            $"☕ 今天的咖啡消耗量创了工作室新纪录。",
            $"🔊 音频组为关键场景编写了令人印象深刻的配乐。",
            $"📝 策划写了长达{10 + rng.Next(30)}页的设计文档。",
            $"🐛 修复了一个诡异的BUG——原来是个多余的空格。",
            $"🎮 内部试玩会上，一个测试者沉迷了2小时。",
            $"🏆 {proj.Name}的预告片在公司内部引起热烈讨论。",
            $"📊 市场调研显示{proj.Theme.Name()}题材热度正在上升。",
            $"💬 论坛上出现了讨论{proj.Name}的帖子，虽然游戏还没发布。",
        };
        return logs[rng.Next(logs.Length)];
    }

    // ══════════════════ 跳票事件 ══════════════════
    public void TriggerDelayEvent(GameProject proj, int months, int reasonIndex)
    {
        string[] reasons = { Loc.Tr("delay.quality"), Loc.Tr("delay.tech"), Loc.Tr("delay.content"), Loc.Tr("delay.staff") };
        string reason = reasonIndex >= 0 && reasonIndex < reasons.Length ? reasons[reasonIndex] : reasons[0];

        var pool = new List<(string title, string desc)>
        {
            ($"《{proj.Name}》宣布延期", $"开发团队表示需要更多时间来{reason}，新发售日定在{months}个月后。"),
            ($"跳票！{proj.Name}推迟发售", $"为了{reason}，{proj.Name}将延后{months}个月发布。玩家社区反应不一。"),
            ($"延期公告：{proj.Name}", $"官方公告：游戏将延期{months}个月。{reason}是主要原因。"),
        };
        var pick = pool[_rng.Next(pool.Count)];
        _gm.ShowToast("📅", $"{pick.title}\n{pick.desc}", new Color(0.9f, 0.6f, 0.1f));
        IndustryFeed.Add($"[延{_gm.GameMonth}] {pick.title}");
    }

    // ══════════════════ 差评轰炸事件 ══════════════════
    public void TriggerReviewBomb()
    {
        var devMgr = _gm.GetNodeOrNull<GameDevManager>("GameDevManager");
        if (devMgr == null) return;

        string title = Loc.Tr("evt.review_bomb_title");
        string desc = Loc.TrF("evt.review_bomb_desc", devMgr.CompletedProjects.Count > 0
            ? devMgr.CompletedProjects[^1].Name : "");
        _gm.ShowPopup($"🔥 {title}", desc, new Color(0.9f, 0.2f, 0.2f));
        IndustryFeed.Add($"[{_gm.GameMonth}] {title}");
    }

    private Random _rng = new();
}
