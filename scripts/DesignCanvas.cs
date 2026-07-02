using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>设计决策节点</summary>
public class DesignNode
{
    public string Id;
    public string NameKey;
    public string DescKey;
    public string Category;              // "core", "combat", "视角", "美术", "叙事"
    public List<DesignOption> Options;
    public Dictionary<string, float> BaseEffects; // 基础效果
}

public class DesignOption
{
    public string LabelKey;
    public string DescKey;
    public Dictionary<string, float> Effects;
    public string UnlockGenre;           // 解锁的游戏类型
    public string UnlockTheme;           // 解锁的主题
    public int InnovationScore;          // 创新度
    public int AudienceAppeal;           // 受众吸引力
    public string ExclusiveTag;          // 独占标签
}

/// <summary>设计决策树——替代简单的类型/主题选择</summary>
public static class DesignCanvasData
{
    public static List<DesignNode> GetTree()
    {
        return new List<DesignNode>
        {
            new DesignNode
            {
                Id = "core_loop", Category = "core",
                NameKey = "design.core_loop", DescKey = "design.core_loop_desc",
                Options = new List<DesignOption>
                {
                    new DesignOption { LabelKey="design.loop_explore_combat", DescKey="design.loop_explore_combat_desc",
                        Effects=new Dictionary<string,float>{{"gameplay",10},{"story",5}}, InnovationScore=3, AudienceAppeal=70, UnlockGenre="RPG" },
                    new DesignOption { LabelKey="design.loop_fast_paced", DescKey="design.loop_fast_paced_desc",
                        Effects=new Dictionary<string,float>{{"gameplay",12},{"graphics",3}}, InnovationScore=2, AudienceAppeal=80, UnlockGenre="ACT" },
                    new DesignOption { LabelKey="design.loop_strategic", DescKey="design.loop_strategic_desc",
                        Effects=new Dictionary<string,float>{{"gameplay",8},{"story",8}}, InnovationScore=5, AudienceAppeal=50, UnlockGenre="SLG" },
                    new DesignOption { LabelKey="design.loop_narrative", DescKey="design.loop_narrative_desc",
                        Effects=new Dictionary<string,float>{{"story",15}}, InnovationScore=4, AudienceAppeal=60, UnlockGenre="AVG" },
                    new DesignOption { LabelKey="design.loop_creative", DescKey="design.loop_creative_desc",
                        Effects=new Dictionary<string,float>{{"gameplay",6},{"graphics",6}}, InnovationScore=6, AudienceAppeal=55, UnlockGenre="SIM" },
                }
            },
            new DesignNode
            {
                Id = "combat_system", Category = "combat",
                NameKey = "design.combat_system", DescKey = "design.combat_system_desc",
                Options = new List<DesignOption>
                {
                    new DesignOption { LabelKey="design.combat_real_time", DescKey="design.combat_real_time_desc",
                        Effects=new Dictionary<string,float>{{"gameplay",8}}, InnovationScore=2, AudienceAppeal=75 },
                    new DesignOption { LabelKey="design.combat_turn_based", DescKey="design.combat_turn_based_desc",
                        Effects=new Dictionary<string,float>{{"gameplay",6},{"story",4}}, InnovationScore=4, AudienceAppeal=50 },
                    new DesignOption { LabelKey="design.combat_stealth", DescKey="design.combat_stealth_desc",
                        Effects=new Dictionary<string,float>{{"story",6},{"gameplay",5}}, InnovationScore=7, AudienceAppeal=45, ExclusiveTag="stealth" },
                    new DesignOption { LabelKey="design.combat_no_combat", DescKey="design.combat_no_combat_desc",
                        Effects=new Dictionary<string,float>{{"story",10},{"gameplay",-5}}, InnovationScore=3, AudienceAppeal=40 },
                }
            },
            new DesignNode
            {
                Id = "perspective", Category = "perspective",
                NameKey = "design.perspective", DescKey = "design.perspective_desc",
                Options = new List<DesignOption>
                {
                    new DesignOption { LabelKey="design.persp_first_person", DescKey="design.persp_first_person_desc",
                        Effects=new Dictionary<string,float>{{"graphics",8}}, InnovationScore=1, AudienceAppeal=70 },
                    new DesignOption { LabelKey="design.persp_third_person", DescKey="design.persp_third_person_desc",
                        Effects=new Dictionary<string,float>{{"graphics",6},{"story",3}}, InnovationScore=2, AudienceAppeal=75 },
                    new DesignOption { LabelKey="design.persp_top_down", DescKey="design.persp_top_down_desc",
                        Effects=new Dictionary<string,float>{{"gameplay",5}}, InnovationScore=3, AudienceAppeal=45 },
                    new DesignOption { LabelKey="design.persp_side_scroll", DescKey="design.persp_side_scroll_desc",
                        Effects=new Dictionary<string,float>{{"gameplay",4},{"graphics",4}}, InnovationScore=4, AudienceAppeal=40 },
                }
            },
            new DesignNode
            {
                Id = "art_style", Category = "art",
                NameKey = "design.art_style", DescKey = "design.art_style_desc",
                Options = new List<DesignOption>
                {
                    new DesignOption { LabelKey="design.art_realistic", DescKey="design.art_realistic_desc",
                        Effects=new Dictionary<string,float>{{"graphics",12}}, InnovationScore=1, AudienceAppeal=80, UnlockTheme="Modern" },
                    new DesignOption { LabelKey="design.art_pixel", DescKey="design.art_pixel_desc",
                        Effects=new Dictionary<string,float>{{"audio",5}}, InnovationScore=5, AudienceAppeal=50, UnlockTheme="Retro" },
                    new DesignOption { LabelKey="design.art_cartoon", DescKey="design.art_cartoon_desc",
                        Effects=new Dictionary<string,float>{{"graphics",4},{"gameplay",4}}, InnovationScore=3, AudienceAppeal=65, UnlockTheme="Fantasy" },
                    new DesignOption { LabelKey="design.art_low_poly", DescKey="design.art_low_poly_desc",
                        Effects=new Dictionary<string,float>{{"stability",5}}, InnovationScore=4, AudienceAppeal=55 },
                }
            },
            new DesignNode
            {
                Id = "narrative_style", Category = "narrative",
                NameKey = "design.narrative_style", DescKey = "design.narrative_style_desc",
                Options = new List<DesignOption>
                {
                    new DesignOption { LabelKey="design.narr_linear", DescKey="design.narr_linear_desc",
                        Effects=new Dictionary<string,float>{{"story",8}}, InnovationScore=1, AudienceAppeal=60 },
                    new DesignOption { LabelKey="design.narr_branching", DescKey="design.narr_branching_desc",
                        Effects=new Dictionary<string,float>{{"story",12},{"gameplay",-3}}, InnovationScore=6, AudienceAppeal=55 },
                    new DesignOption { LabelKey="design.narr_open", DescKey="design.narr_open_desc",
                        Effects=new Dictionary<string,float>{{"story",4},{"gameplay",6}}, InnovationScore=5, AudienceAppeal=65 },
                    new DesignOption { LabelKey="design.narr_minimal", DescKey="design.narr_minimal_desc",
                        Effects=new Dictionary<string,float>{{"gameplay",5}}, InnovationScore=3, AudienceAppeal=40 },
                }
            },
        };
    }

    /// <summary>根据设计决策计算游戏参数</summary>
    public static void ApplyDesignChoices(GameProject proj, Dictionary<string, string> choices)
    {
        var tree = GetTree();
        string detectedGenre = "ETC";
        string detectedTheme = "Modern";
        float totalInnovation = 0;
        int choiceCount = 0;

        foreach (var node in tree)
        {
            if (!choices.ContainsKey(node.Id)) continue;
            var opt = node.Options.Find(o => o.LabelKey == choices[node.Id]);
            if (opt == null) continue;

            // 应用效果
            foreach (var kv in opt.Effects)
            {
                switch (kv.Key)
                {
                    case "gameplay": proj.GameplayScore += kv.Value; break;
                    case "graphics": proj.GraphicsScore += kv.Value; break;
                    case "audio": proj.AudioScore += kv.Value; break;
                    case "story": proj.StoryScore += kv.Value; break;
                    case "stability": proj.StabilityScore += kv.Value; break;
                }
            }

            totalInnovation += opt.InnovationScore;
            choiceCount++;
            if (!string.IsNullOrEmpty(opt.UnlockGenre)) detectedGenre = opt.UnlockGenre;
            if (!string.IsNullOrEmpty(opt.UnlockTheme)) detectedTheme = opt.UnlockTheme;
        }

        // 根据创新度设定项目属性
        float avgInnovation = choiceCount > 0 ? totalInnovation / choiceCount : 3;
        proj.Scale = Mathf.Clamp(0.3f + avgInnovation * 0.1f, 0.3f, 1f);
        proj.EstimatedMonths = Mathf.Clamp(6 + avgInnovation * 2 - proj.GameplayScore * 0.05f, 3, 24);

        // 设定类型和主题
        if (Enum.TryParse(detectedGenre, out GameGenre genre)) proj.Genre = genre;
        if (Enum.TryParse(detectedTheme, out GameTheme theme)) proj.Theme = theme;
    }
}
