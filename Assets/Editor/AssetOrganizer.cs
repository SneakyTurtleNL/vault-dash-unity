using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// AssetOrganizer: Automatically organize assets into proper folder structure.
/// 
/// After Scenario.gg generation, assets end up scattered.
/// This script organizes them:
/// 
/// Assets/Resources/
/// ├── Characters/
/// │   ├── agent_zero.png
/// │   ├── cipher.png
/// │   ... (10 total)
/// ├── Icons/
/// │   ├── trophy.png
/// │   ... (16 total)
/// └── Backgrounds/
///     ├── rookie_bg.png
///     ... (5 total)
/// 
/// Usage: Tools → Organize Assets
/// </summary>

public class AssetOrganizer
{
    [MenuItem("Tools/Organize Assets")]
    public static void OrganizeAssets()
    {
        EditorUtility.DisplayProgressBar("Asset Organization", "Organizing assets...", 0f);
        
        try
        {
            string assetsRoot = Path.Combine(Application.dataPath, "Resources");
            
            // Ensure directories exist
            CreateDirectoryIfNotExists(Path.Combine(assetsRoot, "Characters"));
            CreateDirectoryIfNotExists(Path.Combine(assetsRoot, "Icons"));
            CreateDirectoryIfNotExists(Path.Combine(assetsRoot, "Backgrounds"));

            EditorUtility.DisplayProgressBar("Asset Organization", "Organizing characters...", 0.3f);
            OrganizeCharacters(assetsRoot);

            EditorUtility.DisplayProgressBar("Asset Organization", "Organizing icons...", 0.6f);
            OrganizeIcons(assetsRoot);

            EditorUtility.DisplayProgressBar("Asset Organization", "Organizing backgrounds...", 0.9f);
            OrganizeBackgrounds(assetsRoot);

            // Refresh asset database
            AssetDatabase.Refresh();

            UnityEngine.Debug.Log("✅ Assets organized!");
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Success", "Assets organized into proper folders!\n\nCharacters/Icons/Backgrounds", "OK");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"❌ Asset organization failed: {e.Message}");
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Error", $"Organization failed:\n{e.Message}", "OK");
        }
    }

    private static void OrganizeCharacters(string assetsRoot)
    {
        string[] characterNames = new[]
        {
            "agent_zero", "cipher", "blaze", "tank", "ghost",
            "viper", "nova", "pulse", "eclipse", "phoenix"
        };

        foreach (var charName in characterNames)
        {
            MoveAssetIfExists(assetsRoot, $"{charName}.png", "Characters");
            UnityEngine.Debug.Log($"✓ Organized {charName}");
        }
    }

    private static void OrganizeIcons(string assetsRoot)
    {
        string[] iconNames = new[]
        {
            "trophy", "gem", "coin", "sword", "shield", "lightning",
            "skull", "dice", "clover", "card", "star",
            "medal_gold", "medal_silver", "medal_bronze", "clock", "crown"
        };

        foreach (var iconName in iconNames)
        {
            MoveAssetIfExists(assetsRoot, $"{iconName}.png", "Icons");
        }
        
        UnityEngine.Debug.Log($"✓ Organized {iconNames.Length} icons");
    }

    private static void OrganizeBackgrounds(string assetsRoot)
    {
        string[] bgNames = new[]
        {
            "rookie_bg", "silver_bg", "gold_bg", "diamond_bg", "legend_bg"
        };

        foreach (var bgName in bgNames)
        {
            MoveAssetIfExists(assetsRoot, $"{bgName}.png", "Backgrounds");
        }
        
        UnityEngine.Debug.Log($"✓ Organized {bgNames.Length} backgrounds");
    }

    private static void MoveAssetIfExists(string assetsRoot, string filename, string targetFolder)
    {
        string sourcePath = Path.Combine(assetsRoot, filename);
        string targetDir = Path.Combine(assetsRoot, targetFolder);
        string targetPath = Path.Combine(targetDir, filename);

        // Check if source exists
        if (!File.Exists(sourcePath))
        {
            UnityEngine.Debug.LogWarning($"File not found: {filename}");
            return;
        }

        // Don't overwrite if already in correct location
        if (File.Exists(targetPath))
        {
            UnityEngine.Debug.Log($"Already organized: {filename}");
            File.Delete(sourcePath); // Remove duplicate
            return;
        }

        // Move file
        File.Move(sourcePath, targetPath);
        UnityEngine.Debug.Log($"Moved {filename} → {targetFolder}/");
    }

    private static void CreateDirectoryIfNotExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
