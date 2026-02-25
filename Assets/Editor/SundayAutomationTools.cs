using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// SundayAutomationTools — Batch automation for Sunday setup
/// Run via: Tools → Vault Dash → Sunday Automation
/// </summary>
public class SundayAutomationTools : Editor
{
    [MenuItem("Tools/Vault Dash/Sunday Automation/1. Generate All Particle Prefabs")]
    public static void GenerateAllParticles()
    {
        Debug.Log("[Sunday] Generating particle prefabs...");
        
        // This calls the existing ParticlePrefabGenerator
        ParticlePrefabGenerator.GenerateAll();
        
        Debug.Log("[Sunday] Particle prefabs complete ✅");
    }

    [MenuItem("Tools/Vault Dash/Sunday Automation/2. Setup Character Materials")]
    public static void SetupCharacterMaterials()
    {
        Debug.Log("[Sunday] Setting up character materials...");
        
        string characterDir = "Assets/Characters/Materials";
        Directory.CreateDirectory(characterDir);

        // Create base toon material if doesn't exist
        string toonMatPath = characterDir + "/ToonCelShaded.mat";
        if (!File.Exists(toonMatPath))
        {
            var material = new Material(Shader.Find("Custom/ToonCelShaded"));
            material.name = "ToonCelShaded";
            AssetDatabase.CreateAsset(material, toonMatPath);
            Debug.Log("[Sunday] Created base toon material");
        }

        // Create color variant folders
        string[] characters = { "AgentZero", "Blaze", "Ghost", "Cipher", "Tank" };
        string[] skins = { "Blue", "Red", "Gold" };

        foreach (var character in characters)
        {
            string charDir = characterDir + "/" + character;
            Directory.CreateDirectory(charDir);

            foreach (var skin in skins)
            {
                string skinMatPath = charDir + "/" + character + "_" + skin + ".mat";
                if (!File.Exists(skinMatPath))
                {
                    var material = new Material(Shader.Find("Custom/ToonCelShaded"));
                    material.name = character + "_" + skin;
                    // Color placeholder - you'll adjust in Editor
                    material.color = Random.ColorHSV();
                    AssetDatabase.CreateAsset(material, skinMatPath);
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Sunday] Character materials setup complete ✅");
    }

    [MenuItem("Tools/Vault Dash/Sunday Automation/3. Verify Firebase Setup")]
    public static void VerifyFirebaseSetup()
    {
        Debug.Log("[Sunday] Checking Firebase setup...");

        bool hasAnalytics = CheckScriptingDefine("FIREBASE_ANALYTICS");
        bool hasCrashlytics = CheckScriptingDefine("FIREBASE_CRASHLYTICS");
        bool hasMessaging = CheckScriptingDefine("FIREBASE_MESSAGING");

        Debug.Log($"[Sunday] Firebase Analytics: {(hasAnalytics ? "✅" : "❌")}");
        Debug.Log($"[Sunday] Firebase Crashlytics: {(hasCrashlytics ? "✅" : "❌")}");
        Debug.Log($"[Sunday] Firebase Messaging: {(hasMessaging ? "✅" : "❌")}");

        if (!hasAnalytics || !hasCrashlytics || !hasMessaging)
        {
            EditorUtility.DisplayDialog("Firebase Setup Incomplete",
                "Missing scripting defines. Add to Project Settings:\n" +
                "FIREBASE_ANALYTICS;FIREBASE_CRASHLYTICS;FIREBASE_MESSAGING",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Firebase Setup", "All defines present ✅", "OK");
        }
    }

    [MenuItem("Tools/Vault Dash/Sunday Automation/4. Organize Assets")]
    public static void OrganizeAssets()
    {
        Debug.Log("[Sunday] Organizing asset folders...");

        string[] requiredFolders = new string[]
        {
            "Assets/Characters",
            "Assets/Characters/Prefabs",
            "Assets/Characters/Materials",
            "Assets/Characters/Skins",
            "Assets/Resources/Particles",
            "Assets/Resources/Particles/Auras",
            "Assets/Resources/Particles/Trails",
            "Assets/Resources/Particles/Footsteps",
            "Assets/Resources/Particles/Spawn",
            "Assets/Resources/Particles/LevelUp",
            "Assets/Animations",
            "Assets/Documentation"
        };

        foreach (var folder in requiredFolders)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                Debug.Log($"[Sunday] Created: {folder}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("[Sunday] Asset organization complete ✅");
    }

    [MenuItem("Tools/Vault Dash/Sunday Automation/5. Batch Import Prefabs")]
    public static void BatchImportPrefabs()
    {
        Debug.Log("[Sunday] Scanning for prefabs to wire...");

        // Find all character prefabs in Sidekick pack
        var sidekickPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int count = 0;

        foreach (var guid in sidekickPrefabs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Sidekick") && path.Contains("Character"))
            {
                count++;
                Debug.Log($"[Sunday] Found prefab: {path}");
            }
        }

        Debug.Log($"[Sunday] Found {count} Sidekick character prefabs ✅");
    }

    [MenuItem("Tools/Vault Dash/Sunday Automation/6. Generate Playtest Report")]
    public static void GeneratePlaytestReport()
    {
        Debug.Log("[Sunday] Generating playtest report template...");

        string reportPath = "Assets/Documentation/SUNDAY_PLAYTEST_REPORT.md";
        string reportContent = @"# SUNDAY PLAYTEST REPORT

**Date:** " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + @"

## Startup (15m)
- [ ] Splash screen displays
- [ ] Login works
- [ ] Main menu loads
- [ ] No console errors

## Gameplay (30m)
- [ ] Characters load correctly
- [ ] Character switching works
- [ ] Movement responds to input
- [ ] Obstacles spawn properly
- [ ] Vault opening animation smooth
- [ ] No crashes during 5 minutes play

## Progression (40m)
- [ ] Trophies increment on win
- [ ] Prestige levels increase
- [ ] Cards appear in deck
- [ ] Skins unlock correctly

## Monetization (15m)
- [ ] First gem offer appears
- [ ] Purchase dialog works
- [ ] Analytics events log

## Particles & FX (10m)
- [ ] Particle cosmetics visible
- [ ] Skill animations trigger
- [ ] No visual glitches

## BUGS FOUND
(List any crashes, visual issues, or blockers below)

---
";

        File.WriteAllText(reportPath, reportContent);
        AssetDatabase.Refresh();
        Debug.Log("[Sunday] Playtest report template created ✅");
    }

    [MenuItem("Tools/Vault Dash/Sunday Automation/—/Run All Setup")]
    public static void RunAllSetup()
    {
        Debug.Log("[Sunday] ⚡ RUNNING FULL AUTOMATION SUITE...");
        
        GenerateAllParticles();
        SetupCharacterMaterials();
        VerifyFirebaseSetup();
        OrganizeAssets();
        BatchImportPrefabs();
        GeneratePlaytestReport();

        EditorUtility.DisplayDialog("Sunday Automation Complete",
            "✅ All setup tasks automated.\n\n" +
            "Next steps:\n" +
            "1. Open Sidekick Character Creator\n" +
            "2. Mix & create characters\n" +
            "3. Complete Backend Manual Setup\n" +
            "4. Playtest using generated report\n\n" +
            "See Assets/Documentation/ for all reports.",
            "Let's Go! 🚀");

        Debug.Log("[Sunday] Automation suite complete!");
    }

    // ─── Helper ────────────────────────────────────────────────────────
    
    static bool CheckScriptingDefine(string define)
    {
        var defines = PlayerSettings.GetScriptingDefineSymbols(EditorUserBuildSettings.selectedBuildTargetGroup);
        return defines.Contains(define);
    }
}
