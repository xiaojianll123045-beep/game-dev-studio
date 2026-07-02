using Godot;
using System.Text.Json;

public static class GlobalSettings
{
    public static float UIScale = 1.0f;
    public static bool NewGame;
    public static bool LoadGame;
    public static int DisplayMode = 1;
    public static int Resolution = 4;
    public static int FpsCap = 0;
    public static bool VSync = true;

    public enum Difficulty { Easy, Normal, Hard }
    public static Difficulty GameDifficulty = Difficulty.Normal;

    public static float StartingMoney = 500000f;
    public static float StartingInspiration = 30f;
    public static float MaxInspiration = 100f;

    // ── 存档设置 ──
    public static int SaveSlot { get; set; } = 1;           // 1~5 存档位
    public static bool AutoSaveEnabled { get; set; } = true;
    public static int AutoSaveIntervalMonths { get; set; } = 3; // 0=关, 1/3/6/12 月
    public static string CustomSavePath { get; set; } = "";     // 空=默认user://
    public static int Language { get; set; } = -1;               // -1=未设置(自动检测)
    public static bool ArabicNameAbbr { get; set; } = false;     // 阿拉伯语名字缩写

    // ── 音效 ──
    public static bool SoundEnabled { get; set; } = true;
    public static bool MusicEnabled { get; set; } = true;
    public static float SoundVolume { get; set; } = 80f;   // 0~100
    public static float MusicVolume { get; set; } = 80f;   // 0~100

    // ── 教程进度 ──
    public static bool TutorialCompleted { get; set; } = false;
    public static int TutorialCurrentStep { get; set; } = -1;

    // ── 存档命名 ──
    public static string[] SaveSlotNames { get; set; } = new string[6]; // [1]~[5] 自定义名称，null=默认
    public static string GetSaveSlotName(int slot)
    {
        if (slot >= 1 && slot <= 5 && !string.IsNullOrWhiteSpace(SaveSlotNames[slot]))
            return SaveSlotNames[slot];
        return Loc.TrF("set.save_slot_fmt", slot);
    }
    public static void SetSaveSlotName(int slot, string name)
    {
        if (slot >= 1 && slot <= 5)
            SaveSlotNames[slot] = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    public static string GetSavePath() =>
        string.IsNullOrEmpty(CustomSavePath)
            ? $"user://savegame_slot{SaveSlot}.json"
            : CustomSavePath;

    public static string GetAutoSavePath() =>
        string.IsNullOrEmpty(CustomSavePath)
            ? "user://savegame_autosave.json"
            : CustomSavePath.Replace(".json", "_autosave.json");

    /// <summary>获取特定存档位的路径</summary>
    public static string GetSlotPath(int slot) =>
        string.IsNullOrEmpty(CustomSavePath)
            ? $"user://savegame_slot{slot}.json"
            : CustomSavePath.Replace(".json", $"_slot{slot}.json");

    public static string SaveSlotLabel(int slot) => Loc.TrF("set.save_slot_fmt", slot);
    public static readonly string[] AutoSaveNames = { "set.autosave_off", "set.autosave_monthly", "set.autosave_3month", "set.autosave_6month", "set.autosave_yearly" };
    public static string AutoSaveDisplayName(string key) => Loc.Tr(key);
    public static readonly int[] AutoSaveValues = { 0, 1, 3, 6, 12 };

    public static readonly Vector2I[] Resolutions = {
        new(800, 600), new(1024, 576), new(1024, 768), new(1176, 664),
        new(1280, 720), new(1280, 800), new(1360, 768), new(1366, 768),
        new(1440, 900), new(1600, 900), new(1680, 1050), new(1920, 1080),
        new(1920, 1200), new(2048, 1080), new(2560, 1080),
        new(2560, 1440), new(2560, 1600), new(2880, 1800),
        new(3200, 1800), new(3440, 1440), new(3840, 2160),
        new(4096, 2160), new(5120, 1440), new(5120, 2880),
        new(7680, 4320),
    };
    public static readonly string[] ResNames = {
        "800x600 (SVGA)", "1024x576 (WSVGA)", "1024x768 (XGA)", "1176x664 (Deck)",
        "1280x720 (HD)", "1280x800 (WXGA)", "1360x768 (WXGA+)", "1366x768",
        "1440x900 (WXGA+)", "1600x900 (HD+)", "1680x1050 (WSXGA+)", "1920x1080 (FHD)",
        "1920x1200 (WUXGA)", "2048x1080 (2K)", "2560x1080 (Ultrawide)",
        "2560x1440 (2K)", "2560x1600 (WQXGA)", "2880x1800",
        "3200x1800 (QHD+)", "3440x1440 (UWQHD)", "3840x2160 (4K)",
        "4096x2160 (DCI 4K)", "5120x1440 (32:9)", "5120x2880 (5K)",
        "7680x4320 (8K)",
    };
    public static readonly string[] ModeNames = { "set.window", "set.borderless", "set.fullscreen" };
    public static string ModeDisplayName(string key) => Loc.Tr(key);
    public static readonly int[] FpsOptions = { 0, 30, 60, 90, 120, 144, 240 };

    private const string CfgPath = "user://settings_dev.json";

    public static void Save()
    {
        var data = new { DisplayMode, Resolution, FpsCap, VSync, UIScale, SaveSlot, AutoSaveEnabled, AutoSaveIntervalMonths, CustomSavePath, Language, ArabicNameAbbr, SoundEnabled, MusicEnabled, SoundVolume, MusicVolume, SaveSlotNames, TutorialCompleted, TutorialCurrentStep };
        using var f = FileAccess.Open(CfgPath, FileAccess.ModeFlags.Write);
        if (f != null) f.StoreString(JsonSerializer.Serialize(data));
    }

    public static void Load()
    {
        if (!FileAccess.FileExists(CfgPath)) return;
        using var f = FileAccess.Open(CfgPath, FileAccess.ModeFlags.Read);
        if (f == null) return;
        try
        {
            using var doc = JsonDocument.Parse(f.GetAsText());
            var r = doc.RootElement;
            if (r.TryGetProperty("DisplayMode", out var dm)) DisplayMode = dm.GetInt32();
            if (r.TryGetProperty("Resolution", out var res)) Resolution = res.GetInt32();
            if (r.TryGetProperty("FpsCap", out var fps)) FpsCap = fps.GetInt32();
            if (r.TryGetProperty("VSync", out var vs)) VSync = vs.GetBoolean();
            if (r.TryGetProperty("UIScale", out var us)) UIScale = us.GetSingle();
            if (r.TryGetProperty("SaveSlot", out var ss)) SaveSlot = ss.GetInt32();
            if (r.TryGetProperty("AutoSaveEnabled", out var ae)) AutoSaveEnabled = ae.GetBoolean();
            if (r.TryGetProperty("AutoSaveIntervalMonths", out var ai)) AutoSaveIntervalMonths = ai.GetInt32();
            if (r.TryGetProperty("CustomSavePath", out var cp)) CustomSavePath = cp.GetString();
            if (r.TryGetProperty("Language", out var lang)) Language = lang.GetInt32();
            if (r.TryGetProperty("ArabicNameAbbr", out var ana)) ArabicNameAbbr = ana.GetBoolean();
            if (r.TryGetProperty("SoundEnabled", out var se)) SoundEnabled = se.GetBoolean();
            if (r.TryGetProperty("MusicEnabled", out var me)) MusicEnabled = me.GetBoolean();
            if (r.TryGetProperty("SoundVolume", out var sv)) SoundVolume = sv.GetSingle();
            if (r.TryGetProperty("MusicVolume", out var mv)) MusicVolume = mv.GetSingle();
            if (r.TryGetProperty("TutorialCompleted", out var tc)) TutorialCompleted = tc.GetBoolean();
            if (r.TryGetProperty("TutorialCurrentStep", out var ts)) TutorialCurrentStep = ts.GetInt32();
            if (r.TryGetProperty("SaveSlotNames", out var snames))
            {
                SaveSlotNames = new string[6];
                int i = 0;
                foreach (var item in snames.EnumerateArray())
                {
                    if (i >= 6) break;
                    SaveSlotNames[i++] = item.GetString();
                }
            }
        }
        catch { }
    }

    public static void ApplyAll()
    {
        // 显示模式
        if (DisplayMode == 2)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        }
        else if (DisplayMode == 1)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
        }
        else
        {
            DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        }
        // 分辨率
        var r = Resolutions[Resolution];
        DisplayServer.WindowSetSize(r);
        // 居中
        var screen = DisplayServer.ScreenGetSize();
        DisplayServer.WindowSetPosition(new Vector2I((screen.X - r.X) / 2, (screen.Y - r.Y) / 2));
        // FPS
        Engine.MaxFps = FpsCap;
        // VSync
        DisplayServer.WindowSetVsyncMode(VSync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
    }
}
