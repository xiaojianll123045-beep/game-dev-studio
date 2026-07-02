using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CommunitySystemEx : Node
{
    private GameManager _gm => Services.GameManager;
    private FanManager _fanMgr => Services.FanManager;
    private LiveOpsSystem _liveOps => Services.GameManager?.GetNodeOrNull<LiveOpsSystem>("LiveOpsSystem");

    public List<CommunityPost> Feed { get; private set; } = new();
    public List<MediaReview> PendingReviews { get; private set; } = new();
    public List<SeasonDefinition> Seasons { get; private set; } = new();
    public float CommunityToxicity { get; set; }

    public void MonthlyTick()
    {
        GeneratePosts();
        ProcessSeasons();
        ProcessCommunityToxicity();
    }

    // ── 社区帖子 ──
    private void GeneratePosts()
    {
        if (new Random().Next(100) > 40) return;

        var recentProj = Services.GameDevManager.CompletedProjects.LastOrDefault();
        if (recentProj == null) return;

        string[] templates = recentProj.FinalScore switch
        {
            >= 85 => new[] {
                $"{recentProj.Name} 是我今年玩过最爽的游戏！#神作",
                $"二周目通关，{recentProj.Name} 的世界观太沉浸了",
                $"{recentProj.Name} 的 {recentProj.Genre} + {recentProj.Theme} 组合绝了！" },
            >= 60 => new[] {
                $"{recentProj.Name} 还不错，但还可以更好",
                $"有料！{recentProj.Name} 值得一试",
                $"{recentProj.Name} 的评分有点低了，我觉得挺好玩的" },
            _ => new[] {
                $"{recentProj.Name} 有点失望…希望下作能改进",
                $"这Bug也太多了吧 {recentProj.Name}",
                $"退款了，{recentProj.Name} 优化太差" }
        };

        Feed.Add(new CommunityPost
        {
            AuthorName = GetRandomName(),
            Platform = new[] { "贴吧", "Reddit", "Steam评测", "B站", "Discord" }[new Random().Next(5)],
            Content = templates[new Random().Next(templates.Length)],
            Upvotes = new Random().Next(10, 5000),
            PostType = recentProj.FinalScore >= 70 ? "praise" : "complaint",
            RelatedGame = recentProj.Name
        });

        if (Feed.Count > 100) Feed.RemoveRange(0, Feed.Count - 100);
    }

    private string GetRandomName()
    {
        string[] names = { "游戏发烧友88", "熬夜打工人", "独立游戏爱好者", "画面党", "剧情控", "Steam收集者", "老玩家" };
        return names[new Random().Next(names.Length)];
    }

    // ── 赛季系统 ──
    public void StartSeason(string gameName, SeasonDefinition def)
    {
        def.IsActive = true;
        Seasons.Add(def);
        _gm.ShowToast("🎮", $"《{gameName}》第{def.SeasonNumber}赛季开启！", Colors.Cyan);
    }

    private void ProcessSeasons()
    {
        for (int i = Seasons.Count - 1; i >= 0; i--)
        {
            var s = Seasons[i];
            s.ActiveMonths++;
            if (s.ActiveMonths >= s.DurationMonths)
            {
                s.IsActive = false;
                _gm.ShowToast("🏁", $"《{s.GameName}》赛季结束", Colors.Gray);
            }
        }
    }

    // ── 社区毒性 ──
    private void ProcessCommunityToxicity()
    {
        CommunityToxicity = Mathf.Max(0, CommunityToxicity - 1f);
        var recentProj = Services.GameDevManager.CompletedProjects.LastOrDefault();
        if (recentProj != null && recentProj.FinalScore < 50)
            CommunityToxicity = Mathf.Min(100, CommunityToxicity + 3f);
    }

    // ── 媒体评测 ──
    public void GenerateMediaReview()
    {
        var proj = Services.GameDevManager.CompletedProjects.LastOrDefault();
        if (proj == null) return;

        string[] outlets = { "IGN", "Gamespot", "游民星空", "Famitsu", "PC Gamer" };
        foreach (var outlet in outlets)
        {
            float score = Mathf.Clamp(proj.FinalScore + new Random().Next(-10, 10), 20, 100);
            PendingReviews.Add(new MediaReview
            {
                OutletName = outlet,
                Score = score,
                Excerpt = score switch { >= 85 => "必玩神作！", >= 70 => "值得一试", >= 50 => "中规中矩", _ => "令人失望" },
                Verdict = score >= 70 ? "推荐" : "不推荐"
            });
        }
    }
}
