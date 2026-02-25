using System;
using UnityEngine;

#if FIREBASE_CRASHLYTICS
using Firebase.Crashlytics;
#endif

/// <summary>
/// CrashlyticsManager — Global error tracking for Vault Dash.
///
/// Captures:
///  • All Unity LogException / LogError messages
///  • AppDomain unhandled exceptions
///  • Manual error reports via CrashlyticsManager.ReportError()
///
/// SETUP:
///  1. Download Firebase Unity SDK from https://firebase.google.com/download/unity
///  2. Import FirebaseCrashlytics.unitypackage into the project.
///  3. Add FIREBASE_CRASHLYTICS to Project Settings → Player → Scripting Define Symbols.
///  4. Attach this script to the same GameObject as FirebaseManager (or its own persistent GO).
///
/// DASHBOARD:
///  Firebase Console → Crashlytics → vault-dash project
///
/// CUSTOM KEYS attached to every report:
///  • player_level, arena, trophies, scene_name
/// </summary>
public class CrashlyticsManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static CrashlyticsManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Disable in editor to avoid polluting the dashboard with dev crashes")]
    public bool enableInEditor = false;

    private bool _isActive = false;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
#if UNITY_EDITOR
        if (!enableInEditor)
        {
            Debug.Log("[Crashlytics] Disabled in editor (enableInEditor = false)");
            return;
        }
#endif

#if FIREBASE_CRASHLYTICS
        // Wait for Firebase to be ready
        if (FirebaseManager.Instance != null)
        {
            StartCoroutine(WaitForFirebaseAndEnable());
        }
        else
        {
            EnableCrashlytics();
        }
#else
        Debug.Log("[Crashlytics] Firebase Crashlytics SDK not installed — running in stub mode.");
        // Still attach the log handler so we can at least catch errors in the console
        Application.logMessageReceived += OnLogMessageReceived;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        _isActive = true;
#endif
    }

    System.Collections.IEnumerator WaitForFirebaseAndEnable()
    {
        yield return new UnityEngine.WaitUntil(() => FirebaseManager.Instance.IsInitialized);
        EnableCrashlytics();
    }

    void EnableCrashlytics()
    {
#if FIREBASE_CRASHLYTICS
        // Report ALL uncaught exceptions (including native) as fatal
        Crashlytics.ReportUncaughtExceptionsAsFatal = true;

        // Set initial custom keys
        SetContextKeys();
#endif
        // Attach global log listener
        Application.logMessageReceived += OnLogMessageReceived;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        _isActive = true;
        Debug.Log("[Crashlytics] ✅ Enabled — tracking errors globally");
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= OnLogMessageReceived;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
    }

    // ─── Error Handlers ───────────────────────────────────────────────────────

    void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (!_isActive) return;
        if (type != LogType.Exception && type != LogType.Error) return;

#if FIREBASE_CRASHLYTICS
        // Refresh context keys before reporting
        SetContextKeys();

        var ex = new Exception($"[Unity:{type}] {condition}\n{stackTrace}");
        Crashlytics.LogException(ex);
#else
        // Fallback: in non-Crashlytics builds, critical errors are already shown in console
        Debug.LogWarning($"[Crashlytics STUB] Error captured: {condition}");
#endif
    }

    void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (!_isActive) return;

#if FIREBASE_CRASHLYTICS
        SetContextKeys();
        if (e.ExceptionObject is Exception ex)
        {
            Crashlytics.LogException(ex);
        }
        else
        {
            Crashlytics.LogException(new Exception($"Unhandled exception: {e.ExceptionObject}"));
        }
#endif
    }

    // ─── Context Keys ─────────────────────────────────────────────────────────

    /// <summary>
    /// Updates Crashlytics custom keys with current player state.
    /// Called before each error report so the dashboard shows relevant context.
    /// </summary>
    void SetContextKeys()
    {
#if FIREBASE_CRASHLYTICS
        Crashlytics.SetCustomKey("player_level",  PlayerPrefs.GetInt("VaultDash_PlayerLevel", 1));
        Crashlytics.SetCustomKey("trophies",      PlayerPrefs.GetInt("VaultDash_RankedTrophies", 0));
        Crashlytics.SetCustomKey("arena",         PlayerPrefs.GetString("SelectedArena", "Rookie"));
        Crashlytics.SetCustomKey("scene_name",    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        Crashlytics.SetCustomKey("prestige",      PlayerPrefs.GetInt("VaultDash_PrestigeLevel", 0));
        Crashlytics.SetCustomKey("platform",      Application.platform.ToString());
        Crashlytics.SetCustomKey("app_version",   Application.version);
#endif
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Manually report an error or exception to Crashlytics.
    /// Use for caught exceptions that you want to track in the dashboard.
    /// </summary>
    /// <param name="message">Human-readable description of the error</param>
    /// <param name="exception">Optional exception object (preferred over message-only)</param>
    public static void ReportError(string message, Exception exception = null)
    {
        if (Instance != null && !Instance._isActive) return;

#if FIREBASE_CRASHLYTICS
        Instance?.SetContextKeys();
        if (exception != null)
        {
            Crashlytics.LogException(exception);
        }
        else
        {
            Crashlytics.LogException(new Exception(message));
        }
#endif
        Debug.LogError($"[Crashlytics] Reported: {message}");
    }

    /// <summary>
    /// Set a key-value pair on the current Crashlytics session.
    /// Useful for custom state (e.g., "current_screen", "match_id").
    /// </summary>
    public static void SetKey(string key, string value)
    {
#if FIREBASE_CRASHLYTICS
        Crashlytics.SetCustomKey(key, value);
#endif
    }

    /// <summary>
    /// Set the user identifier for the current Crashlytics session.
    /// Call this after Firebase Auth signs the user in.
    /// </summary>
    public static void SetUserId(string uid)
    {
#if FIREBASE_CRASHLYTICS
        Crashlytics.SetUserId(uid);
#endif
        Debug.Log($"[Crashlytics] User ID set: {uid}");
    }

    /// <summary>Log a non-fatal breadcrumb message visible in the Crashlytics report.</summary>
    public static void Log(string message)
    {
#if FIREBASE_CRASHLYTICS
        Crashlytics.Log(message);
#endif
    }
}
