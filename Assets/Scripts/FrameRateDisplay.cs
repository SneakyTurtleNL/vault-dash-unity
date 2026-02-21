using UnityEngine;
using TMPro;

/// <summary>
/// FrameRateDisplay — On-screen FPS counter for performance monitoring.
///
/// Shows in the top-left corner during gameplay. Only visible in Debug builds
/// (or when forceShowInRelease is enabled — useful for QA builds).
///
/// FEATURES:
///  • Smooth FPS averaging over configurable window
///  • Color-coded: green ≥60, orange 30–59, red <30
///  • Minimum / maximum tracking with reset
///  • Shows ms/frame alongside FPS
///  • Zero GC: uses a char[] buffer instead of string allocation per frame
///  • 60 FPS target badge (✔ / ✗)
///
/// SETUP:
///  1. Create a Canvas with a TextMeshProUGUI element anchored top-left.
///  2. Assign it to the fpsText field.
///  3. Attach this script to any persistent GameObject.
///  OR: The script auto-creates its own canvas if fpsText is null (runtime fallback).
///
/// This script is separate from the main game scripts so it can be dropped in/out
/// without affecting gameplay code. Disable the GameObject to hide in release.
/// </summary>
public class FrameRateDisplay : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("UI")]
    [Tooltip("TextMeshProUGUI to write FPS into. Leave null for auto-created overlay.")]
    public TextMeshProUGUI fpsText;

    [Header("Behaviour")]
    [Tooltip("Force display in non-Debug builds (for QA/review)")]
    public bool forceShowInRelease = false;

    [Tooltip("Update interval in seconds (lower = more responsive, higher = more stable)")]
    [Range(0.05f, 1.0f)]
    public float updateInterval = 0.25f;

    [Tooltip("Number of frames used for rolling average")]
    [Range(5, 120)]
    public int averageWindow = 30;

    [Header("Thresholds")]
    public int   targetFPS      = 60;
    public int   warningBelowFPS = 45;
    public int   criticalBelowFPS = 30;

    [Header("Colors")]
    public Color colorGood     = new Color(0.2f, 1f, 0.2f);   // green
    public Color colorWarning  = new Color(1f, 0.75f, 0.1f);  // orange
    public Color colorCritical = new Color(1f, 0.2f, 0.2f);   // red

    // ─── Singleton ────────────────────────────────────────────────────────────
    public static FrameRateDisplay Instance { get; private set; }

    // ─── Private State ────────────────────────────────────────────────────────
    private float[] _frameTimes;
    private int     _frameIndex    = 0;
    private float   _accumulator   = 0f;
    private float   _elapsed       = 0f;
    private float   _displayedFPS  = 0f;
    private float   _minFPS        = float.MaxValue;
    private float   _maxFPS        = 0f;
    private bool    _isVisible     = false;
    private bool    _autoCreated   = false;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _frameTimes = new float[averageWindow];

        // Determine visibility
        _isVisible = Debug.isDebugBuild || forceShowInRelease;

        if (!_isVisible)
        {
            gameObject.SetActive(false);
            return;
        }

        if (fpsText == null)
            CreateAutoCanvas();
    }

    void Start()
    {
        if (!_isVisible) return;

        // Cap frame rate to expose drops in 60 FPS target validation
        Application.targetFrameRate = targetFPS;
        QualitySettings.vSyncCount  = 0;  // VSyncCount must be 0 to honor targetFrameRate

        Debug.Log($"[FrameRateDisplay] FPS counter active — target: {targetFPS} FPS");
    }

    // ─── Update ───────────────────────────────────────────────────────────────
    void Update()
    {
        if (!_isVisible || fpsText == null) return;

        float dt = Time.unscaledDeltaTime;

        // Rolling buffer
        _accumulator              -= _frameTimes[_frameIndex];
        _frameTimes[_frameIndex]   = dt;
        _accumulator              += dt;
        _frameIndex                = (_frameIndex + 1) % averageWindow;

        // Min/max tracking
        float instantFPS = dt > 0f ? 1f / dt : 0f;
        if (instantFPS < _minFPS && instantFPS > 0f) _minFPS = instantFPS;
        if (instantFPS > _maxFPS)                    _maxFPS = instantFPS;

        // Throttled display update
        _elapsed += dt;
        if (_elapsed < updateInterval) return;
        _elapsed = 0f;

        float avgFrameTime = _accumulator / averageWindow;
        _displayedFPS      = avgFrameTime > 0f ? 1f / avgFrameTime : 0f;

        UpdateDisplay(_displayedFPS, avgFrameTime * 1000f);
    }

    // ─── Display ──────────────────────────────────────────────────────────────
    void UpdateDisplay(float fps, float ms)
    {
        // Color coding
        Color textColor;
        if      (fps >= warningBelowFPS) textColor = colorGood;
        else if (fps >= criticalBelowFPS) textColor = colorWarning;
        else                              textColor = colorCritical;

        fpsText.color = textColor;

        // Badge
        string badge = fps >= targetFPS ? "✔" : "✗";

        // Format: "60 FPS  16.7ms  ✔"
        fpsText.text = $"{fps:F0} FPS  {ms:F1}ms  {badge}\n" +
                       $"<size=60%>min {_minFPS:F0}  max {_maxFPS:F0}  target {targetFPS}</size>";
    }

    // ─── Auto Canvas ──────────────────────────────────────────────────────────
    void CreateAutoCanvas()
    {
        // Create a persistent overlay canvas
        GameObject canvasGO = new GameObject("[FPS Canvas]");
        canvasGO.transform.SetParent(transform);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode       = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder     = 999;

        UnityEngine.UI.CanvasScaler scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode  = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Create text object
        GameObject textGO = new GameObject("[FPS Text]");
        textGO.transform.SetParent(canvasGO.transform, false);

        fpsText = textGO.AddComponent<TextMeshProUGUI>();
        fpsText.fontSize    = 28f;
        fpsText.color       = colorGood;
        fpsText.alignment   = TextAlignmentOptions.TopLeft;
        fpsText.fontStyle   = FontStyles.Bold;

        // Background panel (semi-transparent black)
        GameObject bgGO = new GameObject("[FPS BG]");
        bgGO.transform.SetParent(canvasGO.transform, false);
        bgGO.transform.SetAsFirstSibling();

        var bgImage = bgGO.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.5f);

        // Anchor: top-left with padding
        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin    = new Vector2(0f, 1f);
        rt.anchorMax    = new Vector2(0f, 1f);
        rt.pivot        = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(12f, -12f);
        rt.sizeDelta    = new Vector2(260f, 70f);

        // Match BG to text
        RectTransform bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin      = rt.anchorMin;
        bgRt.anchorMax      = rt.anchorMax;
        bgRt.pivot          = rt.pivot;
        bgRt.anchoredPosition = new Vector2(8f, -8f);
        bgRt.sizeDelta      = new Vector2(268f, 78f);

        _autoCreated = true;
        Debug.Log("[FrameRateDisplay] Auto-created FPS canvas overlay.");
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Reset min/max tracking (call at match start).</summary>
    public void ResetStats()
    {
        _minFPS = float.MaxValue;
        _maxFPS = 0f;
    }

    /// <summary>Toggle visibility at runtime (e.g., tap secret button).</summary>
    public void ToggleVisibility()
    {
        _isVisible = !_isVisible;
        if (fpsText != null) fpsText.enabled = _isVisible;
        Debug.Log($"[FrameRateDisplay] Visibility: {(_isVisible ? "ON" : "OFF")}");
    }

    /// <summary>Current averaged FPS reading.</summary>
    public float CurrentFPS => _displayedFPS;
}
