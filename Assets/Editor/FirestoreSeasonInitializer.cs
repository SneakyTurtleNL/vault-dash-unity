using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary>
/// FirestoreSeasonInitializer: One-click setup of Firestore season_1 for device testing.
/// 
/// Usage: Tools → Initialize Firestore Season
/// 
/// Creates:
/// - config/seasons/season_1 document with:
///   - LoadingScreenTheme (colors, character, event text)
///   - Rewards tiers (gem amounts per level)
///   - Season metadata (active, startDate, endDate)
/// </summary>

public class FirestoreSeasonInitializer
{
    [MenuItem("Tools/Initialize Firestore Season")]
    public static void InitializeSeason()
    {
        EditorUtility.DisplayProgressBar("Firestore Setup", "Initializing season_1...", 0.5f);
        
        InitializeSeasonAsync();
    }

    private static async void InitializeSeasonAsync()
    {
        try
        {
            var db = FirebaseFirestore.DefaultInstance;
            var seasonDoc = db.Collection("config").Document("seasons").Collection("season_1").Document("season_1");

            var seasonData = new System.Collections.Generic.Dictionary<string, object>
            {
                // LoadingScreenTheme
                { "loadingScreenTheme", new System.Collections.Generic.Dictionary<string, object>
                {
                    { "character", "agent_zero" },
                    { "eventText", "Season 1: Rise of the Vault" },
                    { "primaryColor", "#2E7FD9" }, // Blue
                    { "accentColor", "#FFD700" },  // Gold
                    { "backgroundName", "rookie_bg" }
                }},

                // Rewards tiers (level → gems)
                { "rewards", new System.Collections.Generic.Dictionary<string, object>
                {
                    { "tier1", 50 },   // Level 5
                    { "tier2", 100 },  // Level 10
                    { "tier3", 200 },  // Level 20
                    { "tier4", 500 },  // Level 40
                    { "tier5", 1000 }  // Level 60
                }},

                // Season metadata
                { "active", true },
                { "startDate", Firebase.Firestore.Timestamp.GetCurrentTimestamp() },
                { "endDate", Firebase.Firestore.Timestamp.FromDateTime(System.DateTime.UtcNow.AddDays(30)) },
                { "prestigeResets", false },
                { "trophyResetEnabled", true }
            };

            await seasonDoc.SetAsync(seasonData);

            UnityEngine.Debug.Log("✅ Season_1 initialized in Firestore!");
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Success", "Firestore season_1 ready!\n\nPlayers can now login and see LoadingScreen themes.", "OK");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"❌ Failed to initialize season: {e.Message}");
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Error", $"Firestore init failed:\n{e.Message}", "OK");
        }
    }
}
