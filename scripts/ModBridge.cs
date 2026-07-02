using Godot;
using System;
using System.Linq;

[GlobalClass]
public partial class ModBridge : Node
{
    private GameManager _gm;
    private ResourceManager _res;
    private GameDevManager _dev;
    private EmployeeManager _emp;
    private TechManager _tech;
    private FanManager _fan;
    private StoryEvents _storyEvt;
    private RoomManager _room;

    public void Init(GameManager gm)
    {
        _gm = gm;
        _res = gm.GetNodeOrNull<ResourceManager>("ResourceManager");
        _dev = gm.GetNodeOrNull<GameDevManager>("GameDevManager");
        _emp = gm.GetNodeOrNull<EmployeeManager>("EmployeeManager");
        _tech = gm.GetNodeOrNull<TechManager>("TechManager");
        _fan = gm.GetNodeOrNull<FanManager>("FanManager");
        _storyEvt = gm.GetNodeOrNull<StoryEvents>("StoryEvents");
        _room = gm.GetNodeOrNull<RoomManager>("RoomManager");
    }

    // ═══════════════ 资金 ═══════════════
    public float get_money() => _res?.Money ?? 0;
    public void set_money(float v) { if (_res != null) _res.Money = v; }
    public void add_money(float v) { if (_res != null) _res.Money += v; }
    public bool spend_money(float v, string cat = "") => _res?.SpendMoney(v, cat) ?? false;

    // ═══════════════ 灵感 ═══════════════
    public float get_inspiration() => _res?.Inspiration ?? 0;
    public float get_max_inspiration() => _res?.MaxInspiration ?? 100;
    public void add_inspiration(float v) { if (_res != null) _res.GainInspiration(v); }
    public bool spend_inspiration(float v) => _res?.SpendInspiration(v) ?? false;

    // ═══════════════ 科技 ═══════════════
    public Godot.Collections.Array all_tech_ids()
    {
        var a = new Godot.Collections.Array();
        foreach (var k in TechTreeData.AllTech.Keys) a.Add(k);
        return a;
    }
    public bool is_tech_researched(string id) => _tech?.IsResearched(id) ?? false;
    public void unlock_tech(string id) { if (_tech != null) _tech.ResearchedTech[id] = true; }

    // ═══════════════ 项目 ═══════════════
    public int project_count() => _dev?.Projects.Count ?? 0;
    public void clear_bugs() { if (_dev != null) foreach (var p in _dev.Projects) p.BugCount = 0; }
    public void max_scores()
    {
        if (_dev == null) return;
        foreach (var p in _dev.Projects)
        {
            p.GameplayScore = 100; p.GraphicsScore = 100; p.AudioScore = 100;
            p.StoryScore = 100; p.NetworkScore = 100; p.StabilityScore = 100;
            p.BugCount = 0;
        }
    }

    // ═══════════════ 员工 ═══════════════
    public int employee_count() => _emp?.Employees.Count ?? 0;
    public void zero_fatigue() { if (_emp != null) foreach (var e in _emp.Employees) e.Fatigue = 0; }
    public void max_skills()
    {
        if (_emp == null) return;
        foreach (var e in _emp.Employees)
            foreach (var key in e.Skills.Keys.ToArray())
                { var s = e.Skills[key]; s.Level = 5; s.Exp = 9999; e.Skills[key] = s; }
    }

    // ═══════════════ 粉丝 ═══════════════
    public void add_fans(int v) { if (_fan != null) _fan.CasualFans = Mathf.Max(0, _fan.CasualFans + v); }
    public void add_diehard_fans(int v) { if (_fan != null) _fan.DiehardFans = Mathf.Max(0, _fan.DiehardFans + v); }

    // ═══════════════ 信任/声誉 ═══════════════
    public float get_trust() => _dev?.PlayerTrust ?? 0;
    public void set_trust(float v) { if (_dev != null) _dev.PlayerTrust = Mathf.Clamp(v, 0, 100); }

    // ═══════════════ 时间 ═══════════════
    public int get_month() => _gm?.GameMonth ?? 0;
    public int get_year() => _gm?.GameYear ?? 0;
    public void set_speed(int s) { if (_gm != null) _gm.SetGameSpeed(Mathf.Clamp(s, 1, 8)); }
    public bool is_paused() => _gm?.Paused ?? false;
    public void set_paused(bool v) { if (_gm != null) _gm.Paused = v; }

    // ═══════════════ 弹窗 ═══════════════
    public void toast(string title, string msg, Color? c = null)
    {
        if (_gm != null) _gm.ShowToast(title ?? "", msg, c ?? new Color(0.3f, 0.8f, 1f));
    }
    public void popup(string title, string msg, Color? c = null)
    {
        if (_gm != null) _gm.ShowPopup(title ?? "", msg, c ?? new Color(0.3f, 0.8f, 1f));
    }

    // ═══════════════ 游戏对象访问 ═══════════════
    public GameManager get_game_manager() => _gm;
    public ResourceManager get_resource_manager() => _res;
    public GameDevManager get_dev_manager() => _dev;
    public EmployeeManager get_employee_manager() => _emp;
    public TechManager get_tech_manager() => _tech;

    // ═══════════════ Mod 设置 ═══════════════
    public string get_setting(string mod_id, string key, string fallback = "")
        => ModManager.GetModSetting(mod_id, key, fallback);
    public void set_setting(string mod_id, string key, string value)
        => ModManager.SetModSetting(mod_id, key, value);

    // ═══════════════ 日志 ═══════════════
    public void log(string msg) => GD.Print($"[Mod] {msg}");
    public void log_err(string msg) => GD.PrintErr($"[Mod] {msg}");
}
