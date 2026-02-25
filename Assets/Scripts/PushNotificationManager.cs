using System.Collections;
using UnityEngine;

#if FIREBASE_MESSAGING
using Firebase.Messaging;
using Firebase.Extensions;
#endif

#if FIREBASE_FIRESTORE
using Firebase.Firestore;
#endif

/// <summary>
/// PushNotificationManager — Firebase Cloud Messaging (FCM) integration for Vault Dash.
///
/// Responsibilities:
///  • Retrieves FCM device token on startup
///  • Stores/refreshes token in Firestore (players/{uid}.fcmToken)
///  • Dispatches incoming notifications to in-game handlers
///
/// SUPPORTED NOTIFICATION TYPES (notification_type in data payload):
///  • chest_ready          — chest unlocked, prompt to open
///  • season_ending_soon   — season closing in N hours
///  • match_invitation     — rematch request from an opponent
///  • seasonal_reward      — reward ready to claim
///
/// SETUP:
///  1. Import FirebaseMessaging.unitypackage from Firebase Unity SDK.
///  2. Add FIREBASE_MESSAGING to Project Settings → Player → Scripting Define Symbols.
///  3. Add FIREBASE_FIRESTORE if Firestore SDK is also present (for token storage).
///  4. In AndroidManifest.xml, declare notification channels:
///       chest_ready  (default, high importance)
///       season_events (high importance)
///
/// TESTING:
///  Firebase Console → Cloud Messaging → Send test message → target this device's token
///  (token is logged to Unity console on startup)
/// </summary>
public class PushNotificationManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static PushNotificationManager Instance { get; private set; }

    [Header("Configuration")]
    public bool enableNotifications = true;

    [Header("Debug")]
    public bool verboseLogging = true;

    // ─── State ────────────────────────────────────────────────────────────────
    public string CachedFcmToken { get; private set; }
    public bool IsInitialized { get; private set; } = false;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (!enableNotifications)
        {
            Log("[FCM] Push notifications disabled by config");
            return;
        }

        StartCoroutine(InitializeFCM());
    }

    // ─── Initialization ───────────────────────────────────────────────────────

    IEnumerator InitializeFCM()
    {
#if FIREBASE_MESSAGING
        // Wait for FirebaseManager to be ready first
        if (FirebaseManager.Instance != null)
        {
            yield return new WaitUntil(() => FirebaseManager.Instance.IsInitialized);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        // Request FCM token
        var tokenTask = FirebaseMessaging.GetTokenAsync();
        yield return new WaitUntil(() => tokenTask.IsCompleted);

        if (tokenTask.IsFaulted || tokenTask.Exception != null)
        {
            Debug.LogError($"[FCM] Token retrieval failed: {tokenTask.Exception?.Message}");
            CrashlyticsManager.ReportError("FCM token retrieval failed", tokenTask.Exception);
            yield break;
        }

        CachedFcmToken = tokenTask.Result;
        Log($"[FCM] Token obtained: {CachedFcmToken.Substring(0, Mathf.Min(20, CachedFcmToken.Length))}...");

        // Store in Firestore
        yield return StartCoroutine(StoreFCMToken(CachedFcmToken));

        // Subscribe to message events
        FirebaseMessaging.MessageReceived += OnMessageReceived;
        FirebaseMessaging.TokenReceived   += OnTokenRefresh;

        IsInitialized = true;
        Log("[FCM] ✅ Ready — listening for push notifications");

#else
        Log("[FCM] Firebase Messaging SDK not installed — running in stub mode.");
        IsInitialized = true;
        yield return null;
#endif
    }

    // ─── Token Storage ────────────────────────────────────────────────────────

    IEnumerator StoreFCMToken(string token)
    {
        string uid = GetCurrentUID();
        if (string.IsNullOrEmpty(uid))
        {
            Log("[FCM] No authenticated user — token will be stored after login");
            yield break;
        }

#if FIREBASE_FIRESTORE
        var db = FirebaseFirestore.DefaultInstance;
        var playerRef = db.Collection("players").Document(uid);

        var task = playerRef.UpdateAsync("fcmToken", token);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogWarning($"[FCM] Failed to store token in Firestore: {task.Exception?.Message}");
        }
        else
        {
            Log("[FCM] Token stored in Firestore ✅");
        }
#else
        Log("[FCM] Firestore SDK not available — token not persisted");
        yield return null;
#endif
    }

    /// <summary>
    /// Call this after the user logs in (Firebase Auth sign-in).
    /// Ensures the FCM token is stored even if login happened after FCM init.
    /// </summary>
    public void OnUserLoggedIn(string uid)
    {
        if (!string.IsNullOrEmpty(CachedFcmToken))
        {
            StartCoroutine(StoreFCMToken(CachedFcmToken));
        }
    }

    // ─── Message Handlers ─────────────────────────────────────────────────────

#if FIREBASE_MESSAGING
    void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Log($"[FCM] Message received from: {e.Message.From}");

        // Log breadcrumb
        CrashlyticsManager.Log($"[FCM] Push received: {e.Message.From}");

        if (e.Message.Data != null && e.Message.Data.Count > 0)
        {
            if (e.Message.Data.TryGetValue("notification_type", out string notificationType))
            {
                HandleNotificationType(notificationType, e.Message.Data);
            }
            else
            {
                Log($"[FCM] Message has no notification_type — ignoring data payload");
            }
        }
    }

    void OnTokenRefresh(object sender, TokenReceivedEventArgs e)
    {
        Log($"[FCM] Token refreshed");
        CachedFcmToken = e.Token;
        StartCoroutine(StoreFCMToken(e.Token));
    }

    void HandleNotificationType(
        string type,
        System.Collections.Generic.IDictionary<string, string> data)
    {
        Log($"[FCM] Handling notification type: {type}");

        switch (type)
        {
            case "chest_ready":
                HandleChestReady(data);
                break;

            case "season_ending_soon":
                HandleSeasonEndingSoon(data);
                break;

            case "match_invitation":
                HandleMatchInvitation(data);
                break;

            case "seasonal_reward":
                HandleSeasonalReward(data);
                break;

            default:
                Log($"[FCM] Unknown notification_type: {type} — no handler");
                break;
        }
    }

    void HandleChestReady(System.Collections.Generic.IDictionary<string, string> data)
    {
        Log("[FCM] Chest ready notification received");
        // TODO: show in-game banner or set pending notification badge
        // If game is in foreground: show banner UI
        // If game was opened via notification: deep link to chest screen
        data.TryGetValue("chest_type", out string chestType);
        NotificationEvents.OnChestReadyReceived?.Invoke(chestType ?? "Silver");
    }

    void HandleSeasonEndingSoon(System.Collections.Generic.IDictionary<string, string> data)
    {
        data.TryGetValue("hours_remaining", out string hoursStr);
        data.TryGetValue("season_id", out string seasonId);
        int hours = int.TryParse(hoursStr, out int h) ? h : 24;
        Log($"[FCM] Season ending in {hours}h (season: {seasonId})");
        NotificationEvents.OnSeasonEndingSoonReceived?.Invoke(seasonId, hours);
    }

    void HandleMatchInvitation(System.Collections.Generic.IDictionary<string, string> data)
    {
        data.TryGetValue("opponent_name", out string opponentName);
        data.TryGetValue("match_id", out string matchId);
        Log($"[FCM] Rematch invitation from: {opponentName}");
        NotificationEvents.OnMatchInvitationReceived?.Invoke(opponentName ?? "Unknown", matchId ?? "");
    }

    void HandleSeasonalReward(System.Collections.Generic.IDictionary<string, string> data)
    {
        data.TryGetValue("gems", out string gemsStr);
        data.TryGetValue("season_id", out string seasonId);
        int gems = int.TryParse(gemsStr, out int g) ? g : 0;
        Log($"[FCM] Seasonal reward ready: {gems} gems");
        NotificationEvents.OnSeasonalRewardReceived?.Invoke(seasonId ?? "", gems);
    }
#endif

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static string GetCurrentUID()
    {
#if FIREBASE_AUTH
        return Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "";
#else
        return SystemInfo.deviceUniqueIdentifier;
#endif
    }

    void Log(string message)
    {
        if (verboseLogging) Debug.Log(message);
    }

    void OnDestroy()
    {
#if FIREBASE_MESSAGING
        FirebaseMessaging.MessageReceived -= OnMessageReceived;
        FirebaseMessaging.TokenReceived   -= OnTokenRefresh;
#endif
    }
}

/// <summary>
/// Static events dispatched by PushNotificationManager.
/// Subscribe from any screen or manager that needs to respond to push notifications.
///
/// Example:
///   void OnEnable()  => NotificationEvents.OnChestReadyReceived += ShowChestBanner;
///   void OnDisable() => NotificationEvents.OnChestReadyReceived -= ShowChestBanner;
/// </summary>
public static class NotificationEvents
{
    /// <summary>Fired when a chest_ready notification arrives. Param: chestType (e.g. "Silver")</summary>
    public static System.Action<string> OnChestReadyReceived;

    /// <summary>Fired when season is ending soon. Params: seasonId, hoursRemaining</summary>
    public static System.Action<string, int> OnSeasonEndingSoonReceived;

    /// <summary>Fired when a rematch invitation is received. Params: opponentName, matchId</summary>
    public static System.Action<string, string> OnMatchInvitationReceived;

    /// <summary>Fired when seasonal reward is ready to claim. Params: seasonId, gemsAmount</summary>
    public static System.Action<string, int> OnSeasonalRewardReceived;
}
