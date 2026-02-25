using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// ObstacleTextureImporter — Auto-imports all 18 arena obstacle textures.
///
/// Sets per-PNG import settings:
///   TextureType  = Sprite (2D and UI)
///   FilterMode   = Point  (pixel-perfect, no blurring)
///   MaxSize      = 512
///   Alpha        = Transparency enabled
///
/// Also validates Materials and Prefabs under Assets/Materials/Obstacles/
/// and Assets/Resources/Prefabs/Obstacles/ are present.
///
/// Usage: Tools → Import Obstacle Textures
/// </summary>
public static class ObstacleTextureImporter
{
    private static readonly string[] Arenas =
        { "Rookie", "Silver", "Gold", "Diamond", "Master", "Legend" };

    private static readonly string[] ObstacleTypes =
        { "low_obstacle", "full_obstacle", "under_obstacle" };

    // ─── Menu Entry ───────────────────────────────────────────────────────────
    [MenuItem("Tools/Import Obstacle Textures")]
    public static void ImportAllObstacleTextures()
    {
        int total     = 0;
        int reimported = 0;

        EditorUtility.DisplayProgressBar("Obstacle Importer", "Starting...", 0f);

        try
        {
            for (int a = 0; a < Arenas.Length; a++)
            {
                string arena = Arenas[a];
                float  arenaProgress = (float)a / Arenas.Length;

                for (int t = 0; t < ObstacleTypes.Length; t++)
                {
                    string type = ObstacleTypes[t];
                    float  progress = arenaProgress + (float)t / ObstacleTypes.Length / Arenas.Length;

                    EditorUtility.DisplayProgressBar(
                        "Obstacle Importer",
                        $"Importing {arena}/{type}...",
                        progress
                    );

                    string assetPath = $"Assets/Textures/Obstacles/{arena}/{type}.png";

                    if (!File.Exists(Path.Combine(Application.dataPath, $"Textures/Obstacles/{arena}/{type}.png")))
                    {
                        Debug.LogWarning($"[ObstacleImporter] Missing texture: {assetPath}");
                        total++;
                        continue;
                    }

                    bool changed = ApplyTextureSettings(assetPath);
                    if (changed) reimported++;
                    total++;
                }
            }

            // Refresh database after all settings applied
            AssetDatabase.Refresh();

            Debug.Log($"[ObstacleImporter] Done — {total} textures checked, {reimported} reimported.");
            EditorUtility.DisplayDialog(
                "Obstacle Importer",
                $"✅ {total} obstacle textures processed.\n{reimported} had settings updated.\n\nAll set to:\n• TextureType = Sprite\n• FilterMode = Point\n• MaxSize = 512",
                "OK"
            );
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ─── Validate Assets ──────────────────────────────────────────────────────
    [MenuItem("Tools/Validate Obstacle Assets")]
    public static void ValidateObstacleAssets()
    {
        int texOk = 0, matOk = 0, prefabOk = 0;
        int texMissing = 0, matMissing = 0, prefabMissing = 0;

        foreach (string arena in Arenas)
        {
            foreach (string type in ObstacleTypes)
            {
                string aLow = arena.ToLower();

                // Texture
                string texPath = $"Assets/Textures/Obstacles/{arena}/{type}.png";
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) != null)
                    texOk++;
                else
                {
                    Debug.LogError($"[ObstacleImporter] MISSING texture: {texPath}");
                    texMissing++;
                }

                // Material
                string matPath = $"Assets/Materials/Obstacles/{arena}/{aLow}_{type}.mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null)
                    matOk++;
                else
                {
                    Debug.LogError($"[ObstacleImporter] MISSING material: {matPath}");
                    matMissing++;
                }

                // Prefab
                string prefabPath = $"Assets/Resources/Prefabs/Obstacles/{arena}/{aLow}_{type}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                    prefabOk++;
                else
                {
                    Debug.LogError($"[ObstacleImporter] MISSING prefab: {prefabPath}");
                    prefabMissing++;
                }
            }
        }

        string msg = $"Textures: {texOk}/18 ({texMissing} missing)\n" +
                     $"Materials: {matOk}/18 ({matMissing} missing)\n" +
                     $"Prefabs: {prefabOk}/18 ({prefabMissing} missing)";

        Debug.Log($"[ObstacleImporter] Validation — {msg}");
        EditorUtility.DisplayDialog("Obstacle Asset Validation", msg, "OK");
    }

    // ─── Internal ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Applies Sprite/Point/512 import settings to a texture asset.
    /// Returns true if any setting was changed.
    /// </summary>
    static bool ApplyTextureSettings(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[ObstacleImporter] No importer for {assetPath}");
            return false;
        }

        bool dirty = false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            dirty = true;
        }

        if (importer.filterMode != FilterMode.Point)
        {
            importer.filterMode = FilterMode.Point;
            dirty = true;
        }

        if (importer.maxTextureSize != 512)
        {
            importer.maxTextureSize = 512;
            dirty = true;
        }

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            dirty = true;
        }

        if (dirty)
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        return dirty;
    }
}
