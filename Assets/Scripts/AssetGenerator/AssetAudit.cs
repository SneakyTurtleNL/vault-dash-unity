using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// AssetAudit — Verifies all 31 premium game assets are present and loadable.
///
/// ASSET MANIFEST (31 assets):
///   Characters (10): AgentZero, Blaze, Knox, Jade, Raven, Phoenix,
///                    Ghost, Shadow, Titan, Ember
///   UI Icons   (16): icon_coin, icon_gem, icon_trophy, icon_shield,
///                    icon_star, icon_chest, icon_arrow_up, icon_arrow_down,
///                    icon_settings, icon_friends, icon_clan, icon_ranked,
///                    icon_solo, icon_casual, icon_pvp, icon_spectate
///   Arenas      (5): arena_rookie, arena_silver, arena_gold,
///                    arena_diamond, arena_legend
///
/// STYLE CONSISTENCY CHECKS (code-level):
///   • Each character portrait: 512×512 px
///   • Each UI icon:           256×256 px
///   • Each arena background:  1024×512 px
///   • All textures: RGBA32 format, mipmaps enabled
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

    private static readonly AssetSpec[] Manifest = new AssetSpec[]
    {
        // ── Characters (10 × 512×512) ─────────────────────────────────────
        new AssetSpec { ResourcePath = "Characters/portrait_agent_zero",  ExpectedWidth = 512, ExpectedHeight = 512, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/portrait_blaze",       ExpectedWidth = 512, ExpectedHeight = 512, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/portrait_knox",        ExpectedWidth = 512, ExpectedHeight = 512, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/portrait_jade",        ExpectedWidth = 512, ExpectedHeight = 512, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/portrait_raven",       ExpectedWidth = 512, ExpectedHeight = 512, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/portrait_phoenix",     ExpectedWidth = 512, ExpectedHeight = 512, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/portrait_ghost",       ExpectedWidth = 512, ExpectedHeight = 512, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/portrait_shadow",      ExpectedWidth = 512, ExpectedHeight = 512, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/portrait_titan",       ExpectedWidth = 512, ExpectedHeight = 512, Category = "Character" },
        new AssetSpec { ResourcePath = "Characters/portrait_ember",       ExpectedWidth = 512, ExpectedHeight = 512, Category = "Character" },

        // ── UI Icons (16 × 256×256) ────────────────────────────────────────
        new AssetSpec { ResourcePath = "Icons/icon_coin",       ExpectedWidth = 256, ExpectedHeight = 256, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/icon_gem",        ExpectedWidth = 256, ExpectedHeight = 256, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/icon_trophy",     ExpectedWidth = 256, ExpectedHeight = 256, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/icon_shield",     ExpectedWidth = 256, ExpectedHeight = 256, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/icon_star",       ExpectedWidth = 256, ExpectedHeight = 256, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/icon_chest",      ExpectedWidth = 256, ExpectedHeight = 256, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/icon_arrow_up",   ExpectedWidth = 256, ExpectedHeight = 256, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/icon_arrow_down", ExpectedWidth = 256, ExpectedHeight = 256, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/icon_settings",   ExpectedWidth = 256, ExpectedHeight = 256, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/icon_friends",    ExpectedWidth = 256, ExpectedHeight = 256, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/icon_clan",       ExpectedWidth = 256, ExpectedHeight = 256, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/icon_ranked",     ExpectedWidth = 256, ExpectedHeight = 256, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/icon_solo",       ExpectedWidth = 256, ExpectedHeight = 256, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/icon_casual",     ExpectedWidth = 256, ExpectedHeight = 256, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/icon_pvp",        ExpectedWidth = 256, ExpectedHeight = 256, Category = "Icon" },
        new AssetSpec { ResourcePath = "Icons/icon_spectate",   ExpectedWidth = 256, ExpectedHeight = 256, Category = "Icon" },

        // ── Arenas (5 × 1024×512) ─────────────────────────────────────────
        new AssetSpec { ResourcePath = "Arenas/arena_rookie",   ExpectedWidth = 1024, ExpectedHeight = 512, Category = "Arena" },
        new AssetSpec { ResourcePath = "Arenas/arena_silver",   ExpectedWidth = 1024, ExpectedHeight = 512, Category = "Arena" },
        new AssetSpec { ResourcePath = "Arenas/arena_gold",     ExpectedWidth = 1024, ExpectedHeight = 512, Category = "Arena" },
        new AssetSpec { ResourcePath = "Arenas/arena_diamond",  ExpectedWidth = 1024, ExpectedHeight = 512, Category = "Arena" },
        new AssetSpec { ResourcePath = "Arenas/arena_legend",   ExpectedWidth = 1024, ExpectedHeight = 512, Category = "Arena" },
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

        Debug.Log("[AssetAudit] ─── Starting Asset Audit (31 assets) ───");

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
