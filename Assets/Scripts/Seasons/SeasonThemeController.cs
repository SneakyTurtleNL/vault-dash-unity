using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

/// <summary>
/// SeasonThemeController — Applies seasonal visual theme to the game world.
///
/// Manages:
///   1. Arena overlay — seasonal color palette (tunnelGlow, fogColor, accentColor)
///      applied to lighting, post-processing, and shader global properties.
///   2. Character skin theming — reads current season cosmetic and applies
///      tint / material swap on the active character.
///   3. Power-up name overrides — consults SeasonManager.CurrentSeason.powerupTheme
///      to return seasonal names ("Freeze" → "Winter Freeze" in December).
///
/// SETUP:
///   • Add FIREBASE_FIRESTORE (and optionally UNITY_POST_PROCESSING_STACK_V2)
///     to Scripting Define Symbols.
///   • Place on a persistent GameObject (DontDestroyOnLoad).
///   • Wire shader globals if using custom shaders (see SHADER_GLOBALS).
///   • Wire postProcessVolume if using URP/HDRP Volume override.
///
/// SHADER GLOBALS (set each season transition):
///   _SeasonPrimaryColor   — Color
///   _SeasonGlowColor      — Color
///   _SeasonFogColor       — Color
///   _SeasonIntensity      — float (0–1, for gradual lerp)
/// </summary>
public class SeasonThemeController : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static SeasonThemeController Instance { get; private set; }

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Transition")]
    [Tooltip("Time in seconds to lerp from old season theme to new (on game start)")]
    public float themeTransitionDuration = 2f;
    public bool  applyOnStart = true;

    [Header("Lighting")]
    public Light directionalLight;         // main directional; tinted by accent
    public Light ambientFillLight;         // fill light; tinted by primary

    [Header("Fog")]
    public bool   controlFog = true;
    public float  fogDensityBase = 0.02f;

    [Header("Post Processing")]
#if UNITY_POST_PROCESSING_STACK_V2
    public PostProcessVolume postProcessVolume;
#else
    public MonoBehaviour     postProcessVolume;   // placeholder when PP not installed
#endif

    [Header("Character Skin Tinting")]
    [Tooltip("Renderer on the player character to tint with season accent")]
    public Renderer characterRenderer;
    [Tooltip("Material property block is used — does NOT dirty the material asset")]
    public string   tintPropertyName = "_Color";
    public bool     applyCharacterTint = false;    // opt-in (only for season that wants it)

    // ─── Shader global IDs ────────────────────────────────────────────────────
    static readonly int SHADER_PRIMARY    = Shader.PropertyToID("_SeasonPrimaryColor");
    static readonly int SHADER_GLOW       = Shader.PropertyToID("_SeasonGlowColor");
    static readonly int SHADER_FOG        = Shader.PropertyToID("_SeasonFogColor");
    static readonly int SHADER_INTENSITY  = Shader.PropertyToID("_SeasonIntensity");

    // ─── State ────────────────────────────────────────────────────────────────
    private Coroutine _transitionRoutine;
    private SeasonInfo _appliedSeason;

    // ─── Init ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (SeasonManager.Instance != null)
        {
            SeasonManager.Instance.OnSeasonChanged += OnSeasonChanged;
        }

        if (applyOnStart)
            StartCoroutine(ApplyCurrentTheme(instant: false));
    }

    void OnDestroy()
    {
        if (SeasonManager.Instance != null)
            SeasonManager.Instance.OnSeasonChanged -= OnSeasonChanged;
    }

    // ─── Season Change ────────────────────────────────────────────────────────

    void OnSeasonChanged(SeasonInfo newSeason)
    {
        Debug.Log($"[SeasonTheme] 🎨 Season theme change: {newSeason?.name}");

        if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
        _transitionRoutine = StartCoroutine(TransitionToSeason(newSeason));
    }

    // ─── Apply current theme ──────────────────────────────────────────────────

    IEnumerator ApplyCurrentTheme(bool instant = false)
    {
        // Wait for SeasonManager to initialize
        if (SeasonManager.Instance == null || !SeasonManager.Instance.IsInitialized)
            yield return new WaitUntil(() =>
                SeasonManager.Instance != null && SeasonManager.Instance.IsInitialized);

        var season = SeasonManager.Instance?.CurrentSeason;
        if (season == null) yield break;

        if (instant)
            ApplyThemeImmediate(season);
        else
        {
            _transitionRoutine = StartCoroutine(TransitionToSeason(season));
            yield return _transitionRoutine;
        }
    }

    // ─── Transition ───────────────────────────────────────────────────────────

    IEnumerator TransitionToSeason(SeasonInfo season)
    {
        if (season?.arenaOverlay == null)
        {
            _appliedSeason = season;
            yield break;
        }

        var overlay = season.arenaOverlay;
        Color targetPrimary = overlay.PrimaryColor;
        Color targetGlow    = overlay.GlowColor;
        Color targetFog     = overlay.FogColor;

        // Capture start colors
        Color startPrimary = Shader.GetGlobalColor(SHADER_PRIMARY);
        Color startGlow    = Shader.GetGlobalColor(SHADER_GLOW);
        Color startFog     = Shader.GetGlobalColor(SHADER_FOG);

        float elapsed = 0f;
        while (elapsed < themeTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / themeTransitionDuration);

            Color primary = Color.Lerp(startPrimary, targetPrimary, t);
            Color glow    = Color.Lerp(startGlow,    targetGlow,    t);
            Color fogCol  = Color.Lerp(startFog,     targetFog,     t);

            ApplyColors(primary, glow, fogCol, t);
            yield return null;
        }

        ApplyColors(targetPrimary, targetGlow, targetFog, 1f);
        _appliedSeason = season;
        Debug.Log($"[SeasonTheme] ✅ Theme applied: {season.name}");
    }

    void ApplyThemeImmediate(SeasonInfo season)
    {
        if (season?.arenaOverlay == null) return;
        var o = season.arenaOverlay;
        ApplyColors(o.PrimaryColor, o.GlowColor, o.FogColor, 1f);
        _appliedSeason = season;
    }

    void ApplyColors(Color primary, Color glow, Color fog, float intensity)
    {
        // ── Shader globals ────────────────────────────────────────────────────
        Shader.SetGlobalColor(SHADER_PRIMARY,   primary);
        Shader.SetGlobalColor(SHADER_GLOW,      glow);
        Shader.SetGlobalColor(SHADER_FOG,       fog);
        Shader.SetGlobalFloat(SHADER_INTENSITY, intensity);

        // ── Unity fog ─────────────────────────────────────────────────────────
        if (controlFog && RenderSettings.fog)
        {
            RenderSettings.fogColor   = fog;
            RenderSettings.fogDensity = fogDensityBase * (0.5f + intensity * 0.5f);
        }

        // ── Lighting tints ────────────────────────────────────────────────────
        if (directionalLight != null)
            directionalLight.color  = Color.Lerp(Color.white, glow, intensity * 0.3f);
        if (ambientFillLight != null)
            ambientFillLight.color  = Color.Lerp(Color.white, primary, intensity * 0.2f);

        // ── Character tint (opt-in) ───────────────────────────────────────────
        if (applyCharacterTint && characterRenderer != null && intensity >= 1f)
        {
            var mpb = new MaterialPropertyBlock();
            characterRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(tintPropertyName, Color.Lerp(Color.white, primary, 0.15f));
            characterRenderer.SetPropertyBlock(mpb);
        }
    }

    // ─── Power-up Name Overrides ──────────────────────────────────────────────

    /// <summary>
    /// Returns the seasonal display name for a power-up.
    /// Checks SeasonManager.CurrentSeason.powerupTheme for overrides.
    ///
    /// Example: GetPowerUpName("FREEZE") → "Winter Freeze" in winter season.
    /// Falls back to the canonical name if no override exists.
    /// </summary>
    public static string GetPowerUpName(string canonicalId)
    {
        var theme = SeasonManager.Instance?.CurrentSeason?.powerupTheme;
        if (theme != null)
            return theme.GetName(canonicalId);
        return FormatPowerUpName(canonicalId);
    }

    /// <summary>Returns display name from canonical ID (FREEZE → Freeze).</summary>
    public static string FormatPowerUpName(string id)
    {
        if (string.IsNullOrEmpty(id)) return id;
        // Convert "FREEZE" → "Freeze", "MULTIPLIER" → "Multiplier"
        return char.ToUpper(id[0]) + id.Substring(1).ToLower();
    }

    // ─── Season Skin Resolution ───────────────────────────────────────────────

    /// <summary>
    /// Returns the skin ID the player should use, considering:
    ///   1. The player's explicitly selected skin (from PlayerPrefs/Firestore)
    ///   2. If the selected skin is the current season skin and the player
    ///      earned/purchased it — use it; otherwise fall back to default.
    /// </summary>
    public static string ResolveActiveSkinId(string selectedSkinId, string defaultSkinId = "default")
    {
        if (string.IsNullOrEmpty(selectedSkinId)) return defaultSkinId;

        // If this is the current season skin, verify ownership
        var cosmetic = SeasonManager.Instance?.CurrentSeason?.cosmetic;
        if (cosmetic != null && selectedSkinId == cosmetic.skinId)
        {
            bool owned = PlayerPrefs.GetInt($"Skin_{selectedSkinId}", 0) == 1;
            if (!owned && !(SeasonManager.Instance?.PlayerEarnedSeasonSkin() ?? false))
                return defaultSkinId;
        }

        return selectedSkinId;
    }

    // ─── Debug ────────────────────────────────────────────────────────────────

    [ContextMenu("Debug: Apply Current Season Theme")]
    void DbgApply()
    {
        if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
        StartCoroutine(ApplyCurrentTheme(instant: false));
    }

    [ContextMenu("Debug: Apply Instantly")]
    void DbgApplyInstant()
    {
        if (SeasonManager.Instance?.CurrentSeason != null)
            ApplyThemeImmediate(SeasonManager.Instance.CurrentSeason);
    }

    [ContextMenu("Debug: Print PowerUp Names")]
    void DbgPowerUpNames()
    {
        string[] ids = { "FREEZE", "MAGNET", "SHIELD", "ROCKET", "MULTIPLIER" };
        foreach (var id in ids)
            Debug.Log($"[SeasonTheme] {id} → '{GetPowerUpName(id)}'");
    }
}
