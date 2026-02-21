using System.Collections;
using UnityEngine;

#if FIREBASE_ANALYTICS
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
#endif

/// <summary>
/// FirebaseManager — Analytics initialization and event tracking for Vault Dash.
///
/// TRACKED EVENTS:
///  • game_start      (arena, character)
///  • game_over       (score, distance, winner)
///  • opponent_collision (distance)
///  • loot_collected  (type: coin/gem, count)
///  • power_up_used   (type)
///  • purchase_initiated (item_id, price)
///
/// USER PROPERTIES:
///  • player_level, selected_character, total_matches
///
/// SETUP:
///  1. Download Firebase Unity SDK from https://firebase.google.com/download/unity
///  2. Import FirebaseAnalytics.unitypackage into the project.
///  3. Place google-services.json in Assets/ (Android).
///  4. Add FIREBASE_ANALYTICS to Project Settings → Player → Scripting Define Symbols.
///
/// ⚠️  Without the SDK, all Firebase code is inside #if FIREBASE_ANALYTICS guards.
///     The class will silently log to Unity console instead (safe fallback).
///
/// Project ID: vault-dash-unity (from DESIGN.md)
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static FirebaseManager Instance { get; private set; }

    // ─── State ────────────────────────────────────────────────────────────────
    public bool IsInitialized { get; private set; } = false;

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Configuration")]
    [Tooltip("Log events to Unity console even in release builds (disable for ship)")]
    public bool verboseLogging = true;

    [Tooltip("Enable analytics collection (set false for COPPA / privacy opt-out)")]
    public bool analyticsEnabled = true;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartCoroutine(InitializeFirebase());
    }

    IEnumerator InitializeFirebase()
    {
#if FIREBASE_ANALYTICS
        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();

        // Wait for async task to complete
        yield return new WaitUntil(() => dependencyTask.IsCompleted);

        if (dependencyTask.Result == DependencyStatus.Available)
        {
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(analyticsEnabled);
            FirebaseAnalytics.SetSessionTimeoutDuration(System.TimeSpan.FromMinutes(30));

            IsInitialized = true;
            Log("[FirebaseManager] Firebase Analytics initialized ✅");
        }
        else
        {
            Debug.LogError($"[FirebaseManager] Firebase dependency error: {dependencyTask.Result}");
        }
#else
        Log("[FirebaseManager] Firebase SDK not installed — running in stub mode.");
        IsInitialized = true;  // Allow game to proceed; events are no-ops
        yield return null;
#endif
    }

    // ─── User Properties ──────────────────────────────────────────────────────

    public void SetPlayerLevel(int level)
    {
#if FIREBASE_ANALYTICS
        if (!IsInitialized) return;
        FirebaseAnalytics.SetUserProperty("player_level", level.ToString());
#endif
        Log($"[Firebase] UserProp: player_level = {level}");
    }

    public void SetSelectedCharacter(string characterName)
    {
#if FIREBASE_ANALYTICS
        if (!IsInitialized) return;
        FirebaseAnalytics.SetUserProperty("selected_character", characterName);
#endif
        Log($"[Firebase] UserProp: selected_character = {characterName}");
    }

    public void SetTotalMatches(int matches)
    {
#if FIREBASE_ANALYTICS
        if (!IsInitialized) return;
        FirebaseAnalytics.SetUserProperty("total_matches", matches.ToString());
#endif
        Log($"[Firebase] UserProp: total_matches = {matches}");
    }

    // ─── Events ───────────────────────────────────────────────────────────────

    /// <summary>Log when a match begins.</summary>
    /// <param name="arena">Arena name (Rookie/Silver/Gold/Diamond/Legend)</param>
    /// <param name="character">Character display name</param>
    public void LogGameStart(string arena, string character)
    {
#if FIREBASE_ANALYTICS
        if (!IsInitialized) return;
        FirebaseAnalytics.LogEvent("game_start",
            new Parameter("arena",     arena),
            new Parameter("character", character)
        );
#endif
        Log($"[Firebase] Event: game_start  arena={arena} character={character}");
    }

    /// <summary>Log when a match ends.</summary>
    /// <param name="score">Final score</param>
    /// <param name="distance">Distance traveled in metres</param>
    /// <param name="isWinner">True if local player won</param>
    public void LogGameOver(int score, float distance, bool isWinner)
    {
#if FIREBASE_ANALYTICS
        if (!IsInitialized) return;
        FirebaseAnalytics.LogEvent("game_over",
            new Parameter("score",    score),
            new Parameter("distance", Mathf.RoundToInt(distance)),
            new Parameter("winner",   isWinner ? "local" : "opponent")
        );
#endif
        Log($"[Firebase] Event: game_over  score={score} distance={distance:F0}m winner={isWinner}");
    }

    /// <summary>Log when the opponent's marker reaches 0 m / collision triggers.</summary>
    public void LogOpponentCollision(float distanceAtImpact)
    {
#if FIREBASE_ANALYTICS
        if (!IsInitialized) return;
        FirebaseAnalytics.LogEvent("opponent_collision",
            new Parameter("distance_at_impact", Mathf.RoundToInt(distanceAtImpact))
        );
#endif
        Log($"[Firebase] Event: opponent_collision  distance={distanceAtImpact:F0}m");
    }

    /// <summary>Log loot collected (coin or gem).</summary>
    /// <param name="lootType">"coin" or "gem"</param>
    /// <param name="count">Number collected in one session (batch logging)</param>
    public void LogLootCollected(string lootType, int count = 1)
    {
#if FIREBASE_ANALYTICS
        if (!IsInitialized) return;
        FirebaseAnalytics.LogEvent("loot_collected",
            new Parameter("loot_type", lootType),
            new Parameter("count",     count)
        );
#endif
        Log($"[Firebase] Event: loot_collected  type={lootType} count={count}");
    }

    /// <summary>Log when a power-up is activated.</summary>
    public void LogPowerUpUsed(string powerUpType)
    {
#if FIREBASE_ANALYTICS
        if (!IsInitialized) return;
        FirebaseAnalytics.LogEvent("power_up_used",
            new Parameter("power_up_type", powerUpType)
        );
#endif
        Log($"[Firebase] Event: power_up_used  type={powerUpType}");
    }

    /// <summary>Log when the player taps a purchase button (before payment).</summary>
    /// <param name="itemId">Product ID (e.g. gems_80, gems_500)</param>
    /// <param name="price">Display price in USD</param>
    public void LogPurchaseInitiated(string itemId, float price)
    {
#if FIREBASE_ANALYTICS
        if (!IsInitialized) return;
        FirebaseAnalytics.LogEvent("purchase_initiated",
            new Parameter("item_id", itemId),
            new Parameter("price",   price)
        );
#endif
        Log($"[Firebase] Event: purchase_initiated  item={itemId} price=${price:F2}");
    }

    /// <summary>Log successful in-app purchase completion.</summary>
    public void LogPurchaseComplete(string itemId, float price, string currency = "USD")
    {
#if FIREBASE_ANALYTICS
        if (!IsInitialized) return;
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventPurchase,
            new Parameter(FirebaseAnalytics.ParameterItemId,    itemId),
            new Parameter(FirebaseAnalytics.ParameterValue,     price),
            new Parameter(FirebaseAnalytics.ParameterCurrency,  currency)
        );
#endif
        Log($"[Firebase] Event: purchase  item={itemId} price={price} {currency}");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    void Log(string msg)
    {
        if (verboseLogging) Debug.Log(msg);
    }
}
