using Godot;
using System.Collections.Generic;

// ==================== 流派融合 ====================
public static class GenreFusion
{
    public static float GetFusionBonus(System.Enum primary, System.Enum secondary)
    {
        var key = (primary, secondary);
        return FusionTable.ContainsKey(key) ? FusionTable[key] : 0f;
    }

    private static readonly Dictionary<(System.Enum, System.Enum), float> FusionTable = new();
}

// ==================== 创意火花大会 ====================
public class CreativeProposal
{
    public int ProposerId;
    public string Title;
    public string Description;
    public float EstimatedMonths;
    public float ScoreImpact;
    public float RiskLevel;
    public bool IsApproved;
    public bool IsCompleted;
}

// ==================== 员工梦想 ====================
public enum EmployeeDream
{
    None, MakeMasterpiece, StartOwnStudio, TravelWorld, BuyHome,
    LearnMastery, WriteBook, WinAward, SaveMoney, MentorNextGen, MakeIndieGame
}

// ==================== 心理健康 ====================
public enum BurnoutStage { None, Mild, Moderate, Severe, Crisis }

// ==================== 自发项目 ====================
public enum SideProjectType { Tool, MiniGame, TechPrototype, EfficiencyHack, PersonalArt }

// ==================== E3/展会系统 ====================
public enum GameShow { E3, TGS, Gamescom, SteamNextFest, IndieCade }

// ==================== 众筹系统 ====================
public class CrowdfundingCampaign
{
    public long GoalAmount;
    public long RaisedAmount;
    public int BackerCount;
    public int MonthsToDeliver;
    public bool IsSuccessful;
    public List<StretchGoal> StretchGoals = new();
}

public class StretchGoal
{
    public string Description;
    public long FundingThreshold;
    public bool IsDelivered;
}

// ==================== Pivot/设计回溯 ====================
public class PivotInfo
{
    public int PivotCount;
    public float CodeReusability = 1f;
}

// ==================== 董事会系统 ====================
public enum BoardMood { Supportive, Neutral, Pressuring, Angry }

// ==================== 现金流 ====================
public enum CashFlowAlert { Green, Yellow, Orange, Red, Black }

// ==================== 投资者关系 ====================
public enum DividendPolicy { None, Moderate, Generous }

// ==================== 信用评级 ====================
public class CreditRating
{
    public string Rating = "BBB";
    public int Score = 650;
    public int LatePayments;
    public int PaymentHistory;
}

// ==================== 赛季系统 ====================
public class SeasonDefinition
{
    public string GameName;
    public int SeasonNumber;
    public string ThemeKey;
    public int DurationMonths;
    public float InvestAmount;
    public float ContentQuality;
    public bool IsActive;
    public int ActiveMonths;
}

// ==================== 社区帖子 ====================
public struct CommunityPost
{
    public string AuthorName;
    public string Platform;
    public string Content;
    public int Upvotes;
    public string PostType;
    public string RelatedGame;
}

// ==================== 媒体评测 ====================
public struct MediaReview
{
    public string OutletName;
    public float Score;
    public string Excerpt;
    public string Verdict;
}

// ==================== CEO人格 ====================
public enum CEOArchetype { Visionary, Hustler, Craftsman, EmpireBuilder, Underdog, CorpRaider, Artist, Accountant, Balanced }

// ==================== 专利系统 ====================
public class PatentInfo
{
    public string HolderName;
    public string TechId;
    public int GrantedMonth;
    public float RoyaltyRate = 0.05f;
}

// ==================== 衍生品 ====================
public class MerchandiseProduct
{
    public string ProductId;
    public string NameKey;
    public float DevCost;
    public float UnitPrice;
    public float Quality;
    public int MonthsOnShelf;
    public bool IsActive;
}

// ==================== 跨媒体改编 ====================
public class MediaAdaptation
{
    public string MediaType;
    public string Title;
    public int ReleaseMonth;
    public float Quality;
    public float Revenue;
    public int FanBoost;
    public bool IsHit;
}

// ==================== OKR目标系统 ====================
public class OkrGoal
{
    public string Id;
    public string Name;
    public string Desc;
    public float Progress;
    public bool IsCompleted;
    public string RewardDesc;
}

// ==================== 公司文化 ====================
public enum CompanyMission
{
    ArtForArt, ProfitFirst, TechInnovation, PeopleMatter, GamerFirst
}

// ==================== 宏观经济 ====================
public enum EconomyPhase { Boom, Recession, Depression, Recovery }

// ==================== 组织架构 ====================
public enum OrgStage { Flat, Departmental, Division, Matrix, Conglomerate }

// ==================== 创始人生活 ====================
public class FounderLife
{
    public float Health = 100f;
    public float FamilyRelation = 70f;
    public float SocialNetwork = 30f;
    public float Happiness = 60f;
    public bool IsMarried;
    public int Children;
}

// ==================== 行业流言 ====================
public class Rumor
{
    public string Content;
    public float Credibility;
    public int SpawnMonth;
    public int ExpireMonth;
}

// ==================== 线下实体 ====================
public enum VenueType { MerchShop, ExperienceHall, ThemePark }

public class PhysicalVenue
{
    public string Name;
    public VenueType Type;
    public string IPBinding;
    public float BuildCost;
    public float MonthlyRevenue;
    public int BuiltMonth;
}

// ==================== 经济周期 ====================
public class MarketInvestment
{
    public string Name;
    public float Amount;
    public float CurrentValue;
    public int InvestedMonth;
    public float Volatility;
}
