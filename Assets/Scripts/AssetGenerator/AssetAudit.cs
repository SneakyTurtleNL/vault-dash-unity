using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// AssetAudit — Verifies all 55 game assets are present and loadable.
///
/// ASSET MANIFEST v2 — Updated 2026-02-26 (55 assets):
///   Characters  (12): agent_zero, blaze, cipher, eclipse, ghost, jade,
///                     knox, nova, phoenix, pulse, tank, viper
///   Victory      (3): agent_zero_victory, blaze_victory, cipher_victory
///   Skills      (12): deflect, double_loot, freeze, ghost_skill, magnet,
///                     obstacle, reverse, shield, shrink, slowmo, steal, vault_key
///   Power-ups    (5): power_freeze, power_reverse, power_shrink,
///                     power_obstacle, power_electric
///   Chests       (3): chest_silver, chest_gold, chest_legendary
///   Currency     (3): coin, gem, trophy
///   Tiers        (6): tier_rookie/silver/gold/diamond/master/legend
///   Arenas       (6): rookie, silver, gold, diamond, legend, master_arena
///
/// STYLE CONSISTENCY CHECKS (code-level):
///   • Character portraits: 1024×1024 px (jade/knox: 512×512 RGBA)
///   • Skill icons:          512×512 px
///   • Power-up icons:       512×512 px (power_obstacle: 256×256)
///   • Arena backgrounds:   1024×512 px (master: 1024×1024)
///   • All textures must load via Resources.Load
///
/// USAGE:
///   // Runtime check (always compiled):
///   AssetAudit.RunRuntimeCheck();
///
///   // Editor check (context menu on this component):
///   Right-click component → "Run Asset Audit"
///
/// RESULT:
///   Console log: PASS ✅ or FAIL ❌ per asset + summary.
///   Visual verification required on device (Saturday test).
/// </summary>
public class AssetAudit : MonoBehaviour
{
    // ─── Asset Manifest ───────────────────────────────────────────────────────
    [Serializable]
    public struct AssetSpec
    {
        public string ResourcePath;   // relative to Resources/
        public int    ExpectedWidth;
        public int    ExpectedHeight;
        public string Category;
    }

    // ASSET MANIFEST v2 — Updated 2026-02-26 to match actual file paths in Assets/Resources/
    // Characters: 12 actual portraits (no "portrait_" prefix, no "shadow"/"raven"/"titan"/"ember")
    // Icons: subdir structure (Currency/, Tiers/, Actions/, UI/, PowerUps/)
    // ArenaBackgrounds: correct folder name (was "Arenas/")
    // Skills + Chests added for Saturday playtest coverage
    private static readonly AssetSpec[] Manifest = new AssetSpec[]
    {
        // ── Characters (12 × various sizes) ──────────────────────────────
        new AssetSpec { ResourcePath = "Characters/agent_zero", ExpectedWidth = 1024, ExpectedHeight = 1024, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/blaze",      ExpectedWidth = 1024, ExpectedHeight = 1024, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/cipher",     ExpectedWidth = 1024, ExpectedHeight = 1024, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/eclipse",    ExpectedWidth = 1024, ExpectedHeight = 1024, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/ghost",      ExpectedWidth = 1024, ExpectedHeight = 1024, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/jade",       ExpectedWidth = 512,  ExpectedHeight = 512,  Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/knox",       ExpectedWidth = 512,  ExpectedHeight = 512,  Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/nova",       ExpectedWidth = 1024, ExpectedHeight = 1024, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/phoenix",    ExpectedWidth = 1024, ExpectedHeight = 1024, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/pulse",      ExpectedWidth = 1024, ExpectedHeight = 1024, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/tank",       ExpectedWidth = 1024, ExpectedHeight = 1024, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/viper",      ExpectedWidth = 1024, ExpectedHeight = 1024, Category = "Character" },

        // ── Victory Poses (3 × 512×512) ────────────────────────────────────
        new AssetSpec { ResourcePath = "Characters/Victory/agent_zero_victory", ExpectedWidth = 512, ExpectedHeight = 512, Category = "VictoryPose" },
        new AssetSpec { ResourcePath = "Characters/Victory/blaze_victory",      ExpectedWidth = 512, ExpectedHeight = 512, Category = "VictoryPose" },
        new AssetSpec { ResourcePath = "Characters/Victory/cipher_victory",     ExpectedWidth = 512, ExpectedHeight = 512, Category = "VictoryPose" },

        // ── Skills (12 × 512×512) ──────────────────────────────────────────
        new AssetSpec { ResourcePath = "Skills/deflect",    ExpectedWidth = 512, ExpectedHeight = 512, Category = "Skill" },
        new AssetSpec { ResourcePath = "Skills/double_loot",ExpectedWidth = 512, ExpectedHeight = 512, Category = "Skill" },
        new AssetSpec { ResourcePath = "Skills/freeze",     ExpectedWidth = 512, ExpectedHeight = 512, Category = "Skill" },
        new AssetSpec { ResourcePath = "Skills/ghost_skill",ExpectedWidth = 512, ExpectedHeight = 512, Category = "Skill" },
        new AssetSpec { ResourcePath = "Skills/magnet",     ExpectedWidth = 512, ExpectedHeight = 512, Category = "Skill" },
        new AssetSpec { ResourcePath = "Skills/obstacle",   ExpectedWidth = 512, ExpectedHeight = 512, Category = "Skill" },
        new AssetSpec { ResourcePath = "Skills/reverse",    ExpectedWidth = 512, ExpectedHeight = 512, Category = "Skill" },
        new AssetSpec { ResourcePath = "Skills/shield",     ExpectedWidth = 512, ExpectedHeight = 512, Category = "Skill" },
        new AssetSpec { ResourcePath = "Skills/shrink",     ExpectedWidth = 512, ExpectedHeight = 512, Category = "Skill" },
        new AssetSpec { ResourcePath = "Skills/slowmo",     ExpectedWidth = 512, ExpectedHeight = 512, Category = "Skill" },
        new AssetSpec { ResourcePath = "Skills/steal",      ExpectedWidth = 512, ExpectedHeight = 512, Category = "Skill" },
        new AssetSpec { ResourcePath = "Skills/vault_key",  ExpectedWidth = 512, ExpectedHeight = 512, Category = "Skill" },

        // ── Power-ups (5 × 512×512 or 256×256) ────────────────────────────
        new AssetSpec { ResourcePath = "Icons/PowerUps/power_freeze",   ExpectedWidth = 512, ExpectedHeight = 512, Category = "PowerUp" },
        new AssetSpec { ResourcePath = "Icons/PowerUps/power_reverse",  ExpectedWidth = 512, ExpectedHeight = 512, Category = "PowerUp" },
        new AssetSpec { ResourcePath = "Icons/PowerUps/power_shrink",   ExpectedWidth = 512, ExpectedHeight = 512, Category = "PowerUp" },
        new AssetSpec { ResourcePath = "Icons/PowerUps/power_obstacle", ExpectedWidth = 256, ExpectedHeight = 256, Category = "PowerUp" },
        new AssetSpec { ResourcePath = "Icons/PowerUps/power_electric", ExpectedWidth = 512, ExpectedHeight = 512, Category = "PowerUp" },

        // ── Chests (3 × 512×512) ───────────────────────────────────────────
        new AssetSpec { ResourcePath = "Rewards/chest_silver",    ExpectedWidth = 512, ExpectedHeight = 512, Category = "Chest" },
        new AssetSpec { ResourcePath = "Rewards/chest_gold",      ExpectedWidth = 512, ExpectedHeight = 512, Category = "Chest" },
        new AssetSpec { ResourcePath = "Rewards/chest_legendary", ExpectedWidth = 512, ExpectedHeight = 512, Category = "Chest" },

        // ── UI Icons — Currency (3 × 512×512) ─────────────────────────────
        new AssetSpec { ResourcePath = "Icons/Currency/coin",   ExpectedWidth = 512, ExpectedHeight = 512, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/Currency/gem",    ExpectedWidth = 512, ExpectedHeight = 512, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/Currency/trophy", ExpectedWidth = 512, ExpectedHeight = 512, Category = "Icon" },

        // ── UI Icons — Tiers (6 × 512×512) ────────────────────────────────
        new AssetSpec { ResourcePath = "Icons/Tiers/tier_rookie",   ExpectedWidth = 512, ExpectedHeight = 512, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/Tiers/tier_silver",   ExpectedWidth = 512, ExpectedHeight = 512, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/Tiers/tier_gold",     ExpectedWidth = 512, ExpectedHeight = 512, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/Tiers/tier_diamond",  ExpectedWidth = 512, ExpectedHeight = 512, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/Tiers/tier_master",   ExpectedWidth = 512, ExpectedHeight = 512, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/Tiers/tier_legend",   ExpectedWidth = 512, ExpectedHeight = 512, Category = "Icon" },

        // ── Arena Backgrounds (6 × 1024×512) ──────────────────────────────
        new AssetSpec { ResourcePath = "ArenaBackgrounds/rookie",       ExpectedWidth = 1024, ExpectedHeight = 512, Category = "Arena" },
        new AssetSpec { ResourcePath = "ArenaBackgrounds/silver",       ExpectedWidth = 1024, ExpectedHeight = 512, Category = "Arena" },
        new AssetSpec { ResourcePath = "ArenaBackgrounds/gold",         ExpectedWidth = 1024, ExpectedHeight = 512, Category = "Arena" },
        new AssetSpec { ResourcePath = "ArenaBackgrounds/diamond",      ExpectedWidth = 1024, ExpectedHeight = 512, Category = "Arena" },
        new AssetSpec { ResourcePath = "ArenaBackgrounds/legend",       ExpectedWidth = 1024, ExpectedHeight = 512, Category = "Arena" },
        new AssetSpec { ResourcePath = "ArenaBackgrounds/master_arena", ExpectedWidth = 1024, ExpectedHeight = 1024, Category = "Arena" },
    };

    // ─── Result ───────────────────────────────────────────────────────────────
    public struct AuditResult
    {
        public string  ResourcePath;
        public string  Category;
        public bool    Exists;
        public bool    SizeCorrect;
        public int     ActualWidth;
        public int     ActualHeight;
        public string  FailReason;

        public bool Pass => Exists && SizeCorrect;
    }

    // ─── Runtime Check ────────────────────────────────────────────────────────

    public static List<AuditResult> RunRuntimeCheck()
    {
        var results   = new List<AuditResult>();
        int pass = 0, fail = 0, missing = 0;

        Debug.Log($"[AssetAudit] ─── Starting Asset Audit ({Manifest.Length} assets) ───");

        foreach (var spec in Manifest)
        {
            var result = CheckAsset(spec);
            results.Add(result);

            if (!result.Exists)
            {
                missing++;
                fail++;
                Debug.LogWarning($"  ❌ MISSING  [{result.Category}] {result.ResourcePath}");
            }
            else if (!result.SizeCorrect)
            {
                fail++;
                Debug.LogWarning($"  ⚠️  SIZE     [{result.Category}] {result.ResourcePath} " +
                                 $"— expected {spec.ExpectedWidth}×{spec.ExpectedHeight}, " +
                                 $"got {result.ActualWidth}×{result.ActualHeight}");
            }
            else
            {
                pass++;
                Debug.Log($"  ✅ OK       [{result.Category}] {result.ResourcePath}");
            }
        }

        Debug.Log($"[AssetAudit] ─── SUMMARY ───");
        Debug.Log($"  Total:   {Manifest.Length}");
        Debug.Log($"  Pass:    {pass} ✅");
        Debug.Log($"  Fail:    {fail} ❌");
        Debug.Log($"  Missing: {missing}");
        Debug.Log($"  Result:  {(fail == 0 ? "ALL PASS ✅" : $"ISSUES FOUND ❌ ({fail} failed)")}");
        Debug.Log("[AssetAudit] ⚠️  Visual verification required on physical device (Saturday).");

        return results;
    }

    // ─── Single Asset Check ───────────────────────────────────────────────────

    private static AuditResult CheckAsset(AssetSpec spec)
    {
        var result = new AuditResult
        {
            ResourcePath = spec.ResourcePath,
            Category     = spec.Category
        };

        var tex = Resources.Load<Texture2D>(spec.ResourcePath);

        if (tex == null)
        {
            // Try as Sprite
            var sprite = Resources.Load<Sprite>(spec.ResourcePath);
            if (sprite != null) tex = sprite.texture;
        }

        if (tex == null)
        {
            result.Exists      = false;
            result.SizeCorrect = false;
            result.FailReason  = "Not found in Resources";
            return result;
        }

        result.Exists       = true;
        result.ActualWidth  = tex.width;
        result.ActualHeight = tex.height;
        result.SizeCorrect  = (tex.width  == spec.ExpectedWidth &&
                               tex.height == spec.ExpectedHeight);

        if (!result.SizeCorrect)
            result.FailReason = $"Size mismatch: {tex.width}×{tex.height} ≠ {spec.ExpectedWidth}×{spec.ExpectedHeight}";

        return result;
    }

    // ─── Auto-run on Start ────────────────────────────────────────────────────

    [Header("Run on Start")]
    public bool RunOnStart = false;

    void Start()
    {
        if (RunOnStart) RunRuntimeCheck();
    }

    // ─── Context Menu (Editor) ────────────────────────────────────────────────

#if UNITY_EDITOR
    [ContextMenu("Run Asset Audit")]
    private void EditorRunAudit()
    {
        RunRuntimeCheck();
    }

    [ContextMenu("Generate Asset Audit Report (File)")]
    private void EditorGenerateReport()
    {
        var results = RunRuntimeCheck();
        var sb      = new System.Text.StringBuilder();
        sb.AppendLine("# Asset Audit Report");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("| Asset | Category | Status | Size |");
        sb.AppendLine("|-------|----------|--------|------|");

        foreach (var r in results)
        {
            string status = r.Pass    ? "✅ PASS"
                          : !r.Exists ? "❌ MISSING"
                          :             "⚠️ SIZE";
            string size   = r.Exists ? $"{r.ActualWidth}×{r.ActualHeight}" : "N/A";
            sb.AppendLine($"| {r.ResourcePath} | {r.Category} | {status} | {size} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Notes");
        sb.AppendLine("- Visual verification still required on physical device (Saturday).");
        sb.AppendLine("- Style consistency (colors, fonts) requires in-game review.");

        string path = "Assets/ASSET_AUDIT_REPORT.md";
        System.IO.File.WriteAllText(path, sb.ToString());
        AssetDatabase.Refresh();
        Debug.Log($"[AssetAudit] Report saved to {path}");
    }
#endif
}
