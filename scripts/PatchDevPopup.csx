using System.Text;
using System.Text.RegularExpressions;

string filePath = @"E:\编程\Ai\打飞机\游戏开发商\scripts\GameDevPopup.cs";
string text = File.ReadAllText(filePath, Encoding.UTF8);

// Find the boundary: from "六大模块进度条" to the _content.AddChild(releaseBtn);
string startMarker = "// ── 六大模块进度条 ──";
string endMarker = "                _content.AddChild(releaseBtn);\n            }\n        }\n\n        if (_devMgr.Projects";

int startIdx = text.IndexOf(startMarker);
int endIdx = text.IndexOf(endMarker);

if (startIdx < 0 || endIdx < 0)
{
    Console.WriteLine($"start={startIdx}, end={endIdx}");
    // Try finding the end differently
    int relBtnIdx = text.LastIndexOf("_content.AddChild(releaseBtn);");
    Console.WriteLine($"relBtnIdx={relBtnIdx}");
    if (relBtnIdx > 0)
    {
        // Find the matching closing }
        int pos = relBtnIdx;
        while (pos < text.Length && text[pos] != '}') pos++;
        pos++; // skip first }
        while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
        if (pos < text.Length && text[pos] == '}') pos++; // skip second }
        endIdx = pos;
        Console.WriteLine($"Found endIdx={endIdx}, char={text[endIdx]}");
    }
    return;
}

string before = text[..startIdx];
string after = text[endIdx..];

string replacement = @"                // ═══════ 四象限仪表盘 ═══════
                float quadW = (_panel.Size.X - Sf(50)) / 2;
                float quadH = Sf(175);

                // 行1: 内容 | 技术
                var topRow = new HBoxContainer();
                topRow.AddThemeConstantOverride(""separation"", (int)Sf(8));
                var qContent = new Panel { CustomMinimumSize = new(quadW, quadH) };
                qContent.AddThemeStyleboxOverride(""panel"", new StyleBoxFlat { BgColor = new Color(0.92f, 0.96f, 0.98f, 0.9f), BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.3f, 0.5f, 0.8f, 0.3f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
                var vQ1 = new VBoxContainer { Position = new(Sf(6), Sf(4)) };
                vQ1.AddChild(MakeLabel(Loc.TrF(""dev.quad_content"", """", 11, new Color(0.15f, 0.3f, 0.65f)));
                AddModuleBar(Loc.Tr(""dev.core_gameplay""), proj.ModuleProgressCore, SkillType.Program);
                AddModuleBar(Loc.Tr(""dev.visual""), proj.ModuleProgressVisual, SkillType.Art);
                AddModuleBar(Loc.Tr(""dev.audio_design""), proj.ModuleProgressAudio, SkillType.Audio);
                AddModuleBar(Loc.Tr(""dev.story_text""), proj.ModuleProgressStory, SkillType.Program);
                AddModuleBar(Loc.Tr(""dev.program_stable""), proj.ModuleProgressStability, SkillType.Program);
                AddModuleBar(Loc.Tr(""dev.online_service""), proj.ModuleProgressOnline, SkillType.Network);
                float estRaw = proj.GraphicsScore * 0.2f + proj.GameplayScore * 0.3f + proj.AudioScore * 0.1f + proj.StoryScore * 0.15f + proj.NetworkScore * 0.1f + proj.StabilityScore * 0.15f;
                float est = estRaw * (0.9f + proj.Scale * 0.2f) - proj.BugCount * 0.3f;
                string estGrade = est >= 85 ? ""A"" : est >= 70 ? ""B"" : est >= 50 ? ""C"" : ""D"";
                vQ1.AddChild(MakeLabel(Loc.TrF(""dev.est_score"", Mathf.Clamp(est, 0, 100), estGrade), 10, new Color(0.4f, 0.55f, 0.7f)));
                qContent.AddChild(vQ1);
                topRow.AddChild(qContent);

                // ── 右上：技术象限 ──
                var qTech = new Panel { CustomMinimumSize = new(quadW, quadH) };
                qTech.AddThemeStyleboxOverride(""panel"", new StyleBoxFlat { BgColor = new Color(0.98f, 0.94f, 0.92f, 0.9f), BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.8f, 0.3f, 0.2f, 0.35f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
                var vQ2 = new VBoxContainer { Position = new(Sf(6), Sf(4)) };
                bool memDanger = proj.MemoryOverLimit;
                bool fpsDanger = proj.FpsBelowTarget;
                Color techTitleColor = (memDanger || fpsDanger) ? new Color(0.85f, 0.15f, 0.1f) : new Color(0.55f, 0.25f, 0.15f);
                vQ2.AddChild(MakeLabel(Loc.TrF(""dev.quad_tech"", """", 11, techTitleColor)));
                float memPct = Mathf.Clamp(proj.MemoryUsage / proj.PlatformMemoryLimit, 0, 1.5f);
                Color memColor = memPct > 1f ? new Color(0.9f, 0.1f, 0.1f) : memPct > 0.8f ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.2f, 0.7f, 0.3f);
                vQ2.AddChild(MakeLabel(Loc.TrF(""dev.mem_gauge"", proj.MemoryUsage, proj.PlatformMemoryLimit), 11, memColor));
                var memRow = new HBoxContainer();
                var memBar = new ColorRect { CustomMinimumSize = new(quadW - Sf(80), 14), Size = new(quadW - Sf(80), 14), Color = new Color(0.2f, 0.2f, 0.2f) };
                var memFill = new ColorRect { Size = new(Mathf.Clamp(memPct * (quadW - Sf(80)), 0, quadW - Sf(80)), 14), Color = memColor };
                memBar.AddChild(memFill);
                memRow.AddChild(memBar);
                if (memDanger) memRow.AddChild(MakeLabel(""\u26a0"", 12, new Color(0.9f, 0.2f, 0.2f)));
                vQ2.AddChild(memRow);
                Color fpsColor = fpsDanger ? new Color(0.9f, 0.1f, 0.1f) : proj.FpsEstimate < 45 ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.2f, 0.65f, 0.3f);
                vQ2.AddChild(MakeLabel(Loc.TrF(""dev.fps_label"", proj.FpsEstimate, proj.PlatformFpsTarget), 11, fpsColor));
                float crashPct = proj.CrashRate * 100;
                Color crashColor = crashPct > 30 ? new Color(0.9f, 0.1f, 0.1f) : crashPct > 15 ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.2f, 0.65f, 0.3f);
                vQ2.AddChild(MakeLabel(Loc.TrF(""dev.crash_label"", crashPct), 11, crashColor));
                string platStatus = proj.PlatformStressLevel == ""danger"" ? Loc.Tr(""dev.plat_fail"") : proj.PlatformStressLevel == ""warn"" ? Loc.Tr(""dev.plat_warn"") : Loc.Tr(""dev.plat_ok"");
                Color platColor = proj.PlatformStressLevel == ""danger"" ? new Color(0.9f, 0.2f, 0.2f) : proj.PlatformStressLevel == ""warn"" ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.2f, 0.65f, 0.3f);
                vQ2.AddChild(MakeLabel(Loc.TrF(""dev.plat_status"", proj.Platform.Name(), platStatus), 11, platColor));
                qTech.AddChild(vQ2);
                topRow.AddChild(qTech);
                _content.AddChild(topRow);

                // 行2: 资源 | 风险
                var botRow = new HBoxContainer();
                botRow.AddThemeConstantOverride(""separation"", (int)Sf(8));
                var qRes = new Panel { CustomMinimumSize = new(quadW, quadH) };
                qRes.AddThemeStyleboxOverride(""panel"", new StyleBoxFlat { BgColor = new Color(0.94f, 0.98f, 0.92f, 0.9f), BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.3f, 0.7f, 0.3f, 0.3f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
                var vQ3 = new VBoxContainer { Position = new(Sf(6), Sf(4)) };
                vQ3.AddChild(MakeLabel(Loc.TrF(""dev.quad_res"", """", 11, new Color(0.15f, 0.5f, 0.15f)));
                string[] modNames = Loc.ParseModNames();
                var inspRow = new HBoxContainer();
                for (int i = 0; i < 6; i++)
                {
                    int idx = i; string sn = modNames[i]; string sl = sn.Length > 2 ? sn[..2] : sn;
                    var btn = MakeBtn(sl, 48, 20, 9);
                    btn.TooltipText = Loc.TrF(""dev.inspire_btn"", sn);
                    btn.Pressed += () => { InjectInspiration(proj, idx); RenderStep3_Status(); };
                    inspRow.AddChild(btn);
                }
                vQ3.AddChild(inspRow);
                vQ3.AddChild(MakeLabel(Loc.TrF(""dev.inspire_cost"", _res.Inspiration), 10, new Color(0.3f, 0.35f, 0.4f)));
                var storyEvt = _gm.GetNode<StoryEvents>(""StoryEvents"");
                if (storyEvt != null && storyEvt.Fragments.Count > 0)
                {
                    vQ3.AddChild(MakeLabel(Loc.TrF(""dev.frag_count"", storyEvt.Fragments.Count, storyEvt.Fragments[0].Bonus), 10, new Color(0.7f, 0.3f, 0.6f)));
                    var fragBtn = MakeBtn(Loc.Tr(""dev.use_fragment""), 70, 20, 9);
                    var p = proj;
                    fragBtn.Pressed += () => { var f = storyEvt.UseFragment(0); if (f != null) { p.GameplayScore += f.Bonus; p.OriginScore += f.Bonus * 0.5f; p.DevLog.Add(Loc.TrF(""dev.frag_used"", f.Bonus)); } RenderStep3_Status(); };
                    vQ3.AddChild(fragBtn);
                }
                qRes.AddChild(vQ3);
                botRow.AddChild(qRes);

                // ── 右下：风险象限 ──
                var qRisk = new Panel { CustomMinimumSize = new(quadW, quadH) };
                Color riskBg = proj.TechDebt > 50 ? new Color(0.98f, 0.90f, 0.82f, 0.95f) : new Color(0.95f, 0.95f, 0.90f, 0.9f);
                qRisk.AddThemeStyleboxOverride(""panel"", new StyleBoxFlat { BgColor = riskBg, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1, BorderColor = new Color(0.8f, 0.6f, 0.1f, 0.35f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
                var vQ4 = new VBoxContainer { Position = new(Sf(6), Sf(4)) };
                Color riskTitle = proj.TechDebt > 50 ? new Color(0.75f, 0.15f, 0.05f) : new Color(0.45f, 0.35f, 0.15f);
                vQ4.AddChild(MakeLabel(Loc.TrF(""dev.quad_risk"", """", 11, riskTitle)));
                vQ4.AddChild(MakeLabel(Loc.TrF(""dev.debt_val"", proj.TechDebt), 11, riskTitle));
                if (proj.TechDebt > 30)
                    vQ4.AddChild(MakeLabel(Loc.TrF(""dev.debt_interest"", proj.NextMonthBugFromDebt, proj.NextMonthSlowFromDebt * 100), 10, new Color(0.8f, 0.3f, 0.1f)));
                if (proj.TechDebt >= 20)
                {
                    float refCost = 1f + proj.TechDebt * 0.015f;
                    string refLabel = proj.TechDebt > 50 ? Loc.TrF(""dev.refactor_urgent"", proj.TechDebt * 0.5f, refCost) : Loc.TrF(""dev.refactor_btn"", proj.TechDebt * 0.5f, refCost);
                    Color refColor = proj.TechDebt > 50 ? new Color(0.9f, 0.2f, 0.1f) : new Color(0.7f, 0.4f, 0.1f);
                    var refBtn = MakeBtn(refLabel, 200, 22, 9);
                    var rproj = proj;
                    refBtn.Pressed += () => { if (_devMgr.PartialRefactor(rproj)) RenderStep3_Status(); };
                    vQ4.AddChild(refBtn);
                    if (proj.TechDebt > 60) vQ4.AddChild(MakeLabel(Loc.Tr(""dev.spaghetti"", 10, new Color(0.9f, 0.15f, 0.1f))));
                }
                vQ4.AddChild(MakeLabel("""", 3, Colors.White));
                if (proj.BugCount > 0)
                {
                    var polishBtn = MakeBtn(Loc.TrF(""dev.polish_btn"", proj.BugCount), 150, 26, 10);
                    var p_proj = proj;
                    polishBtn.Pressed += () => { _devMgr.Polish(p_proj); RenderStep3_Status(); };
                    vQ4.AddChild(polishBtn);
                }
                if (proj.Phase == DevPhase.ReadyToRelease || proj.Phase == DevPhase.Developing)
                {
                    string relLabel = proj.Phase == DevPhase.ReadyToRelease ? Loc.Tr(""dev.ship_tested"") : Loc.Tr(""dev.ship_now"");
                    var releaseBtn = MakeBtn(relLabel, 150, 26, 10);
                    var r_proj = proj;
                    releaseBtn.Pressed += () =>
                    {
                        var team = _teamMgr.Teams.Find(t => t.CurrentProject == proj);
                        if (team != null) { if (!string.IsNullOrEmpty(proj.QATestReport) && proj.BugCount < 15) proj.DevLog.Add(""QA passed, quality bonus""); _devMgr.ReleaseGame(team); ShowReleaseResult(proj); }
                    };
                    vQ4.AddChild(releaseBtn);
                }
                qRisk.AddChild(vQ4);
                botRow.AddChild(qRisk);
                _content.AddChild(botRow);

                // 开发日志（最近3条）
                if (proj.DevLog.Count > 0)
                {
                    _content.AddChild(MakeLabel(Loc.Tr(""dev.log_title""), 10, new Color(0.35f, 0.38f, 0.42f)));
                    foreach (var line in proj.DevLog.TakeLast(3))
                        if (!string.IsNullOrWhiteSpace(line)) _content.AddChild(MakeLabel(line, 9, new Color(0.25f, 0.22f, 0.45f)));
                }
";

// Fix escaped quotes in the code
replacement = replacement.Replace("\"\"", "\"");
// Fix the empty Loc.TrF calls
replacement = replacement.Replace("Loc.TrF(\"dev.quad_content\", \"\", ", "Loc.Tr(\"dev.quad_content\"), ");
replacement = replacement.Replace("Loc.TrF(\"dev.quad_tech\", \"\", ", "Loc.Tr(\"dev.quad_tech\"), ");
replacement = replacement.Replace("Loc.TrF(\"dev.quad_res\", \"\", ", "Loc.Tr(\"dev.quad_res\"), ");
replacement = replacement.Replace("Loc.TrF(\"dev.quad_risk\", \"\", ", "Loc.Tr(\"dev.quad_risk\"), ");
// Fix the spaghetti warn line
replacement = replacement.Replace("Loc.Tr(\"dev.spaghetti\", 10,", "Loc.Tr(\"dev.spaghetti\"), 10,");

string newText = before + replacement + after;
File.WriteAllText(filePath, newText, new UTF8Encoding(false));
Console.WriteLine("Done! File updated.");
