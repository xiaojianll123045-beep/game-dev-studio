using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>成就截图卡片生成器 — 可分享的社交货币</summary>
public static class AchievementCard
{
    /// <summary>已触发的成就 ID，防止重复</summary>
    public static HashSet<string> Triggered { get; } = new();

    /// <summary>检查并触发成就卡片</summary>
    public static void Check(GameManager gm)
    {
        var devMgr = gm.GetNode<GameDevManager>("GameDevManager");
        var res = gm.GetNode<ResourceManager>("ResourceManager");
        int year = gm.GameYear;
        var released = devMgr.CompletedProjects.Where(p => p.IsReleased).ToList();

        // 第一个百万销量
        if (!Triggered.Contains("first_million") && released.Any(p => p.Sales >= 1_000_000))
        {
            Triggered.Add("first_million");
            var best = released.Where(p => p.Sales >= 1_000_000).OrderByDescending(p => p.Sales).First();
            Show(gm, "🏆 首款百万销量！", $"《{best.Name}》销量突破100万！",
                $"评分: {best.FinalScore:F0}  |  销量: {best.Sales:N0}",
                best.Genre.Name(), year);
        }

        // 五连高分
        if (!Triggered.Contains("five_high") && released.Count >= 5)
        {
            var last5 = released.TakeLast(5).ToList();
            if (last5.All(p => p.FinalScore >= 85))
            {
                Triggered.Add("five_high");
                Show(gm, "🔥 五连高分神话！", "连续5款游戏评分85+！",
                    $"最高: {last5.Max(p => p.FinalScore):F0}  |  平均: {last5.Average(p => p.FinalScore):F0}",
                    "", year);
            }
        }

        // 十年老店
        if (!Triggered.Contains("decade") && year >= 10)
        {
            Triggered.Add("decade");
            Show(gm, "🎂 十年老店！", "公司经营满10年！",
                $"累计游戏: {released.Count}  |  总粉丝: {gm.GetNode<FanManager>("FanManager")?.TotalFans ?? 0}",
                "", year);
        }

        // 击败所有竞品
        if (!Triggered.Contains("dominate") && gm.GameMonth > 12)
        {
            var competitor = gm.GetNode<CompetitorAI>("CompetitorAI");
            if (competitor != null && competitor.Studios.Count == 0)
            {
                Triggered.Add("dominate");
                Show(gm, "👑 行业霸主！", "所有AI工作室已被击败！",
                    $"登顶市场！成为唯一的游戏公司",
                    "", year);
            }
        }

        // 首款满分游戏
        if (!Triggered.Contains("perfect") && released.Any(p => p.FinalScore >= 95))
        {
            Triggered.Add("perfect");
            var perfect = released.Where(p => p.FinalScore >= 95).OrderByDescending(p => p.FinalScore).First();
            Show(gm, "💎 传世神作！", $"《{perfect.Name}》获得95+评分！",
                $"评分: {perfect.FinalScore:F0}  |  销量: {perfect.Sales:N0}",
                perfect.Genre.Name(), year);
        }

        // 资产过亿
        if (!Triggered.Contains("billionaire") && res.Money >= 100_000_000)
        {
            Triggered.Add("billionaire");
            Show(gm, "💰 身价过亿！", "公司资金突破1亿！",
                $"总资产: ¥{res.Money:N0}  |  累计收入: ¥{res.TotalRevenue:N0}",
                "", year);
        }
    }

    private static readonly string[] Quotes = {
        "成功不是偶然，是每一个加班的夜晚。",
        "做游戏，我们是认真的。",
        "像素之间，皆是热爱。",
        "不是因为有了希望才坚持，而是坚持了才有希望。",
        "每一行代码，都在改变世界。",
        "Game Over? 不，这是新的开始。",
        "玩家笑了，我们就赢了。",
        "用创意改变世界，用代码实现梦想。",
    };

    private static void Show(GameManager gm, string title, string message, string data, string genre, int year)
    {
        // 卡片内容准备
        var rng = new Random(year + message.Length);
        string quote = Quotes[rng.Next(Quotes.Length)];
        string cardText = $"{title}\n\n{message}\n\n{data}\n\n“{quote}”\n\nGame Developer Tycoon";

        // 弹窗展示（未来可以扩展为保存截图）
        gm.ShowToast("📸 成就解锁！", cardText, new Color(0.9f, 0.7f, 0.2f));

        // 实际截图保存到 user://screenshots/
        SaveCardImage(title, message, data, quote, year, gm.Founder?.CompanyName ?? "");
    }

    private static void SaveCardImage(string title, string message, string data, string quote, int year, string companyName)
    {
        try
        {
            var dir = DirAccess.Open("user://");
            if (dir == null) DirAccess.MakeDirRecursiveAbsolute("user://");
            if (!DirAccess.DirExistsAbsolute("user://screenshots"))
                DirAccess.MakeDirRecursiveAbsolute("user://screenshots");

            int w = 600, h = 400;
            var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
            var colors = new Dictionary<Color, int>();

            // 绘制简单卡片
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    // 渐变背景
                    float nx = x / (float)w, ny = y / (float)h;
                    byte r = (byte)(20 + nx * 30);
                    byte g = (byte)(25 + ny * 20);
                    byte b = (byte)(40 + (1 - ny) * 30);
                    img.SetPixel(x, y, new Color(r / 255f, g / 255f, b / 255f));
                }

            // 绘制顶部装饰条
            for (int x = 0; x < w; x++)
                for (int y = 0; y < 4; y++)
                    img.SetPixel(x, y, new Color(0.9f, 0.6f, 0.1f));

            // 文件名
            string safeName = title.Replace(" ", "_").Replace("/", "_").Replace(":", "");
            string path = $"user://screenshots/{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            img.SavePng(path);
            GD.Print($"[成就] 截图已保存: {path}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[成就] 截图保存失败: {ex.Message}");
        }
    }
}
