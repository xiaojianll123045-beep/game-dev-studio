using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Mod 控制台 — 游戏内命令行终端。
/// 按 ~ 或 F12 打开，支持自动补全和 Mod 注册自定义命令。
/// </summary>
public partial class ModConsole : Control
{
    private static ModConsole _instance;
    private static GameManager _gm;
    private static bool _isOpen = false;

    // 控制台 UI 元素
    private Panel _bg;
    private RichTextLabel _output;
    private LineEdit _input;
    private VBoxContainer _historyBox;

    // 命令注册
    private static Dictionary<string, ConsoleCommand> _commands = new();
    private static List<string> _commandHistory = new();
    private static int _historyIndex = -1;

    public class ConsoleCommand
    {
        public string Name;
        public string Description;
        public string Usage;
        public Action<string[]> Handler;
    }

    /// <summary>初始化控制台（由 GameManager 调用）</summary>
    public static void Init(GameManager gm)
    {
        _gm = gm;
        RegisterBuiltinCommands();
    }

    /// <summary>注册命令</summary>
    public static void RegisterCommand(string name, string desc, string usage, Action<string[]> handler)
    {
        _commands[name.ToLower()] = new ConsoleCommand
        {
            Name = name, Description = desc, Usage = usage, Handler = handler
        };
    }

    /// <summary>切换控制台显示</summary>
    /// <summary>预创建控制台节点（让 _Input 能接收事件）</summary>
    public static void CreateNow()
    {
        if (_instance == null)
        {
            CreateConsole();
            if (_instance != null) { _instance.Visible = false; _isOpen = false; }
        }
    }

    public static void Toggle()
    {
        if (_instance == null)
            CreateConsole();
        _isOpen = !_isOpen;
        _instance.Visible = _isOpen;
        if (_isOpen)
        {
            _instance._input.GrabFocus();
            _instance._input.Text = "";
        }
    }

    public static bool IsOpen => _isOpen;

    /// <summary>输出文本到控制台</summary>
    public static void Print(string msg)
    {
        if (_instance != null && _isOpen)
            _instance._output.AppendText(msg + "\n");
    }

    /// <summary>清除控制台</summary>
    public static void Clear()
    {
        if (_instance != null)
            _instance._output.Clear();
    }

    // ── 内置命令 ──

    private static void RegisterBuiltinCommands()
    {
        RegisterCommand("help", "显示所有可用命令", "help [命令名]", (args) => {
            if (args.Length > 1 && _commands.TryGetValue(args[1].ToLower(), out var cmd))
                Print($"  {cmd.Name} - {cmd.Description}\n    用法: {cmd.Usage}");
            else
                foreach (var kv in _commands.OrderBy(k => k.Key))
                    Print($"  {kv.Key,-20} {kv.Value.Description}");
        });

        RegisterCommand("clear", "清除控制台输出", "clear", (args) => Clear());
        RegisterCommand("cls", "清除控制台输出", "cls", (args) => Clear());

        // ── 信息类 ──
        RegisterCommand("status", "查看游戏全局状态", "status", (args) => {
            Print($"━━━ 游戏状态 ━━━");
            Print($"  资金: {ModAPI.GetMoney():N0}");
            Print($"  灵感: {ModAPI.GetInspiration():F0}/{ModAPI.GetMaxInspiration():F0}");
            Print($"  月份: {ModAPI.GetMonth()} (第{ModAPI.GetYear()}年)");
            Print($"  员工: {ModAPI.GetEmployeeCount()} 人");
            Print($"  信任: {ModAPI.GetPlayerTrust():F0}/100");
            Print($"  粉丝: {ModAPI.GetTotalFans()} (死忠 {ModAPI.GetDiehardFans()})");
            Print($"  办公室: {ModAPI.GetOfficeTier()} 级");
            Print($"  暂停: {(ModAPI.IsPaused() ? "是" : "否")}");
        });

        RegisterCommand("list_mods", "列出所有已加载 Mod", "list_mods", (args) => {
            Print($"  Mod: {ModManager.LoadedMods.Count} 已加载");
            foreach (var m in ModManager.LoadedMods)
                Print($"    [{ModManager.ModStatus.GetValueOrDefault(m.Id, "?")}] [{m.Type}] {m.Name} v{m.Version}");
            Print($"  程序集 Mod: {ModAssemblyLoader.LoadedAssemblies.Count}");
            foreach (var a in ModAssemblyLoader.LoadedAssemblies)
                Print($"    {a.Name} v{a.Version}");
        });

        RegisterCommand("projects", "查看所有项目", "projects", (args) => {
            var dev = _gm.GetNodeOrNull<GameDevManager>("GameDevManager");
            if (dev == null) { Print("  GameDevManager 不可用"); return; }
            int n = 0;
            foreach (var p in dev.Projects)
            {
                n++;
                Print($"  #{n} [{p.Phase}] {p.Name} — 进度{p.DevProgress*100:F0}% Bug:{p.BugCount} 评分:{p.GameplayScore:F0}/{p.GraphicsScore:F0}/{p.AudioScore:F0}");
            }
            if (n == 0) Print("  (无活跃项目)");
        });

        // ── 作弊类 ──
        RegisterCommand("money", "设置/查看资金", "money [金额]", (args) => {
            if (args.Length > 1 && float.TryParse(args[1], out var v)) { ModAPI.SetMoney(v); Print($"  资金已设为 {v:N0}"); }
            else Print($"  当前资金: {ModAPI.GetMoney():N0}");
        });

        RegisterCommand("inspiration", "设置/查看灵感", "inspiration [值]", (args) => {
            if (args.Length > 1 && float.TryParse(args[1], out var v)) { ModAPI.SetInspiration(v); Print($"  灵感已设为 {v}"); }
            else Print($"  灵感: {ModAPI.GetInspiration():F0}/{ModAPI.GetMaxInspiration():F0}");
        });

        RegisterCommand("unlock_tech", "解锁科技", "unlock_tech <ID> [all]", (args) => {
            if (args.Length > 1 && args[1] == "all") {
                foreach (var tid in ModAPI.GetAllTechIds()) ModAPI.UnlockTech(tid);
                Print("  已解锁全部科技");
            }
            else if (args.Length > 1) { ModAPI.UnlockTech(args[1]); Print($"  已解锁: {args[1]}"); }
            else Print("  用法: unlock_tech <ID> 或 unlock_tech all");
        });

        RegisterCommand("add_fans", "增加粉丝", "add_fans <数量>", (args) => {
            if (args.Length > 1 && int.TryParse(args[1], out var v)) { ModAPI.AddCasualFans(v); Print($"  已增加 {v} 粉丝"); }
        });

        RegisterCommand("god", "无敌模式（开关）", "god", (args) => {
            ModAPI.SetMoney(99999999);
            ModAPI.SetInspiration(999);
            ModAPI.SetPlayerTrust(100);
            foreach (var e in _gm.GetNodeOrNull<EmployeeManager>("EmployeeManager")?.Employees ?? new())
                e.Fatigue = 0;
            Print("  🦸 无敌模式已激活：金钱/灵感/信任拉满，疲劳清零");
        });

        RegisterCommand("set_speed", "设置游戏速度", "set_speed <1-8>", (args) => {
            if (args.Length > 1 && int.TryParse(args[1], out var s)) { ModAPI.SetSpeed(Mathf.Clamp(s, 1, 8)); Print($"  速度已设为 {s}x"); }
            else Print($"  当前速度: {_gm.GameSpeed}x");
        });

        // ── 系统类 ──
        RegisterCommand("save", "手动保存游戏", "save [槽位号1-5]", (args) => {
            int slot = args.Length > 1 && int.TryParse(args[1], out var sv) ? sv : GlobalSettings.SaveSlot;
            GlobalSettings.SaveSlot = Mathf.Clamp(slot, 1, 5);
            string path = GlobalSettings.GetSlotPath(GlobalSettings.SaveSlot);
            var gm = _gm;
            gm.CallDeferred("SaveGame", path);
            Print($"  存档到槽位 {GlobalSettings.SaveSlot}...");
        });

        RegisterCommand("load", "读取存档", "load <槽位号1-5>", (args) => {
            if (args.Length > 1 && int.TryParse(args[1], out var sv))
            {
                GlobalSettings.SaveSlot = Mathf.Clamp(sv, 1, 5);
                string path = GlobalSettings.GetSlotPath(GlobalSettings.SaveSlot);
                var gm = _gm;
                gm.CallDeferred("LoadGame", path);
                Print($"  读取槽位 {GlobalSettings.SaveSlot}...");
            }
        });

        RegisterCommand("test", "运行自动化测试", "test", (args) => {
            Print("  运行自动化测试...");
            ModTestRunner.Run(_gm);
            Print("  完成，查看 Godot 输出台获取详细结果");
        });

        RegisterCommand("reset_mods", "重置所有 Mod 状态", "reset_mods", (args) => {
            ModManager.ModStatus.Clear();
            ModManager.LoadErrors.Clear();
            DlcManager.ClearLog();
            Print("  Mod 状态已重置");
        });

        // ── 日志类 ──
        RegisterCommand("mod_log", "显示完整 Mod 日志", "mod_log", (args) => {
            var log = DlcManager.ReadLog();
            if (log.Length > 3000) log = log[^3000..];
            Print($"━━━ Mod 日志 (最近3000字) ━━━");
            foreach (var line in log.Split('\n'))
                if (!string.IsNullOrEmpty(line))
                    Print($"  {line}");
        });
        RegisterCommand("save_log", "保存日志到 user://mod_log.txt", "save_log", (args) => {
            Print($"  📁 日志已保存到: user://mod_log.txt");
            Print("  可在文件管理器中找到此文件并分享给 Mod 作者");
        });
    }

    // ── 控制台 UI 构建 ──

    private static CanvasLayer _consoleLayer;
    private static void CreateConsole()
    {
        if (_gm?.UiLayer == null) return;

        _consoleLayer = new CanvasLayer { Layer = 128 };
        _gm.AddChild(_consoleLayer);

        _instance = new ModConsole();
        _instance.Visible = false;
        _instance.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _consoleLayer.AddChild(_instance);
    }

    public override void _Ready()
    {
        Name = "ModConsole";
        MouseFilter = MouseFilterEnum.Ignore;

        _bg = new Panel();
        _bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var bgStyle = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.85f) };
        _bg.AddThemeStyleboxOverride("panel", bgStyle);
        _bg.MouseFilter = MouseFilterEnum.Pass;
        AddChild(_bg);

        // 输出区域
        _output = new RichTextLayout();
        _output.Size = new Vector2(GetViewportRect().Size.X - 20, GetViewportRect().Size.Y - 80);
        _output.Position = new Vector2(10, 10);
        _output.ScrollActive = true;
        _output.BbcodeEnabled = true;
        _output.AddThemeColorOverride("default_color", new Color(0.2f, 0.9f, 0.2f));
        _output.AddThemeFontSizeOverride("font_size", 14);
        _output.MouseFilter = MouseFilterEnum.Stop;
        _bg.AddChild(_output);

        // 输入区域
        _input = new LineEdit();
        _input.Position = new Vector2(10, GetViewportRect().Size.Y - 60);
        _input.Size = new Vector2(GetViewportRect().Size.X - 20, 40);
        _input.PlaceholderText = "输入命令 (help 查看帮助)";
        _input.AddThemeFontSizeOverride("font_size", 16);
        _input.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        _input.AddThemeColorOverride("placeholder_color", new Color(0.5f, 0.5f, 0.5f));
        _input.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.1f, 0.8f) });
        _input.TextSubmitted += OnInput;
        _input.MouseFilter = MouseFilterEnum.Stop;
        _bg.AddChild(_input);
    }

    private void OnInput(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        _commandHistory.Add(text);
        _historyIndex = _commandHistory.Count;
        _input.Text = "";

        _output.AppendText($">>> {text}\n");
        ExecuteCommand(text);
    }

    private void ExecuteCommand(string text)
    {
        var parts = ParseCommand(text);
        if (parts.Count == 0) return;

        string cmdName = parts[0].ToLower();
        var args = parts.ToArray();

        if (_commands.TryGetValue(cmdName, out var cmd))
        {
            try { cmd.Handler(args); }
            catch (Exception e) { Print($"  ❌ 命令执行错误: {e.Message}"); }
        }
        else
        {
            Print($"  ❌ 未知命令: {cmdName}\n  输入 help 查看所有命令");
        }
    }

    private static List<string> ParseCommand(string text)
    {
        var parts = new List<string>();
        bool inQuote = false;
        string current = "";

        foreach (char c in text)
        {
            if (c == '"') { inQuote = !inQuote; continue; }
            if (c == ' ' && !inQuote)
            {
                if (!string.IsNullOrEmpty(current)) { parts.Add(current); current = ""; }
            }
            else current += c;
        }
        if (!string.IsNullOrEmpty(current)) parts.Add(current);
        return parts;
    }

    public override void _Input(InputEvent @event)
    {
        var args = ModMethodOverride.Args(("event", @event));
        ModMethodOverride.CallVoid("modconsole_input", args, () =>
        {
            if (@event.IsActionPressed("toggle_console") ||
                (@event is InputEventKey key && key.Keycode == Key.F12 && key.Pressed) ||
                (@event is InputEventKey key2 && key2.Keycode == Key.Quoteleft && key2.Pressed))
            {
                Toggle();
                AcceptEvent();
            }

            if (_isOpen && @event is InputEventKey ev && ev.Pressed && ev.Keycode == Key.Up)
            {
                if (_commandHistory.Count > 0)
                {
                    _historyIndex = Mathf.Max(0, _historyIndex - 1);
                    _input.Text = _commandHistory[_historyIndex];
                    _input.SetCaretColumn(_input.Text.Length);
                }
                AcceptEvent();
            }
            if (_isOpen && @event is InputEventKey ev2 && ev2.Pressed && ev2.Keycode == Key.Down)
            {
                if (_commandHistory.Count > 0)
                {
                    _historyIndex = Mathf.Min(_commandHistory.Count - 1, _historyIndex + 1);
                    _input.Text = _commandHistory[_historyIndex];
                    _input.SetCaretColumn(_input.Text.Length);
                }
                AcceptEvent();
            }
            // Tab 自动补全
            if (_isOpen && @event is InputEventKey tab && tab.Pressed && tab.Keycode == Key.Tab)
            {
                string partial = _input.Text.Trim().ToLower();
                var matches = _commands.Keys.Where(k => k.StartsWith(partial)).OrderBy(k => k).ToList();
                if (matches.Count == 1)
                    _input.Text = matches[0] + " ";
                else if (matches.Count > 1)
                    Print($"  {string.Join(" ", matches)}");
                _input.SetCaretColumn(_input.Text.Length);
                AcceptEvent();
            }
        });
    }
}

/// <summary>带滚动支持的 RichTextLabel</summary>
public partial class RichTextLayout : RichTextLabel
{
    public new bool ScrollActive { get; set; } = true;
    public override void _Process(double delta)
    {
        var args = ModMethodOverride.Args(("delta", delta));
        ModMethodOverride.CallVoid("richtextlayout_process", args, () =>
        {
            if (ScrollActive)
                ScrollToLine(GetLineCount() - 1);
        });
    }
}
