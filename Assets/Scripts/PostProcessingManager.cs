using System.Collections;
using UnityEngine;

#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

/// <summary>
/// PostProcessingManager — Controls bloom, vignette and color grading at runtime.
///
/// FEATURES:
///  • Bloom pulse on power-up / loot collection
///  • Vignette flash on obstacle collision
///  • Color grading shift: warm (victory) / cool (defeat)
///  • All effects are additive — no permanent profile mutation
///
/// SETUP (Unity Editor):
///  1. Add "Post-Process Volume" component to Main Camera (or a dedicated volume object).
///  2. Create a Post-Processing Profile in Assets/PostProcessingProfiles/.
///  3. Add Bloom, Vignette, Color Grading overrides to the profile.
///  4. Assign the PostProcessVolume to this script's `volume` field.
///  5. This script lives on any persistent GameObject (e.g., GameManager).
///
/// PACKAGE: com.unity.postprocessing (added to manifest.json).
///
/// ⚠️  If package not yet installed, all post-processing code is inside
///     #if UNITY_POST_PROCESSING_STACK_V2 guards — compiles cleanly either way.
/// </summary>
public class PostProcessingManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static PostProcessingManager Instance { get; private set; }

    // ─── Inspector ────────────────────────────────────────────────────────────
#if UNITY_POST_PROCESSING_STACK_V2
    [Header("Volume")]
    [Tooltip("Assign the Post-Process Volume on the Main Camera")]
    public PostProcessVolume volume;
#endif

    [Header("Bloom Settings")]
    public float bloomBaseline    = 1f;     // intensity during normal gameplay
    public float bloomPowerUp     = 4f;     // intensity burst on power-up
    public float bloomPulseDuration = 0.4f; // seconds for one pulse

    [Header("Vignette Settings")]
    public float vignetteBaseline = 0.2f;   // subtle vignette during play
    public float vignetteHit      = 0.6f;   // strong darkening on collision
    public float vignetteFlashDuration = 0.5f;

    [Header("Color Grading")]
    [Tooltip("Color temperature shift for Victory result (+values = warm)")]
    public float colorTempVictory = 30f;    // warm orange/yellow
    [Tooltip("Color temperature shift for Defeat result (-values = cool)")]
    public float colorTempDefeat  = -30f;   // cool blue
    public float colorGradingLerpSpeed = 2f;

    // ─── Private State ────────────────────────────────────────────────────────
    private float _targetColorTemp = 0f;
    private bool  _colorGradingActive = false;

#if UNITY_POST_PROCESSING_STACK_V2
    private Bloom       _bloom;
    private Vignette    _vignette;
    private ColorGrading _colorGrading;
    private bool _profileLoaded = false;
#endif

    private Coroutine _bloomRoutine;
    private Coroutine _vignetteRoutine;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
#if UNITY_POST_PROCESSING_STACK_V2
        LoadProfileEffects();
#else
        Debug.Log("[PostProcessingManager] Post-Processing package not installed — effects disabled.");
#endif
    }

#if UNITY_POST_PROCESSING_STACK_V2

    void LoadProfileEffects()
    {
        if (volume == null)
        {
            // Try to auto-find
            volume = FindObjectOfType<PostProcessVolume>();
        }

        if (volume == null || volume.profile == null)
        {
            Debug.LogWarning("[PostProcessingManager] No PostProcessVolume found. Assign it in the Inspector.");
            return;
        }

        bool gotBloom  = volume.profile.TryGetSettings(out _bloom);
        bool gotVig    = volume.profile.TryGetSettings(out _vignette);
        bool gotCG     = volume.profile.TryGetSettings(out _colorGrading);

        if (!gotBloom)    Debug.LogWarning("[PostProcessingManager] Profile is missing a Bloom override.");
        if (!gotVig)      Debug.LogWarning("[PostProcessingManager] Profile is missing a Vignette override.");
        if (!gotCG)       Debug.LogWarning("[PostProcessingManager] Profile is missing a ColorGrading override.");

        // Set baselines
        if (_bloom != null)
        {
            _bloom.enabled.Override(true);
            _bloom.intensity.Override(bloomBaseline);
        }
        if (_vignette != null)
        {
            _vignette.enabled.Override(true);
            _vignette.intensity.Override(vignetteBaseline);
        }
        if (_colorGrading != null)
        {
            _colorGrading.enabled.Override(true);
            _colorGrading.temperature.Override(0f);
        }

        _profileLoaded = true;
        Debug.Log("[PostProcessingManager] Post-Processing profile loaded.");
    }

    void Update()
    {
        if (!_profileLoaded || !_colorGradingActive) return;
        if (_colorGrading == null) return;

        // Smoothly lerp color temperature toward target
        float current = _colorGrading.temperature.value;
        float next    = Mathf.Lerp(current, _targetColorTemp, colorGradingLerpSpeed * Time.deltaTime);
        _colorGrading.temperature.Override(next);
    }

#endif

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Pulse bloom — call when player collects a power-up or gem.</summary>
    public void BloomPulse(float intensity = -1f)
    {
        if (intensity < 0f) intensity = bloomPowerUp;
        if (_bloomRoutine != null) StopCoroutine(_bloomRoutine);
        _bloomRoutine = StartCoroutine(BloomPulseRoutine(intensity));
    }

    /// <summary>Flash vignette — call on obstacle collision.</summary>
    public void VignetteFlash()
    {
        if (_vignetteRoutine != null) StopCoroutine(_vignetteRoutine);
        _vignetteRoutine = StartCoroutine(VignetteFlashRoutine());
    }

    /// <summary>Shift to warm color grading (Victory screen).</summary>
    public void SetVictoryColors()
    {
        _targetColorTemp    = colorTempVictory;
        _colorGradingActive = true;
        Debug.Log("[PostProcessingManager] Color grading → WARM (victory)");
    }

    /// <summary>Shift to cool color grading (Defeat screen).</summary>
    public void SetDefeatColors()
    {
        _targetColorTemp    = colorTempDefeat;
        _colorGradingActive = true;
        Debug.Log("[PostProcessingManager] Color grading → COOL (defeat)");
    }

    /// <summary>Reset all effects to neutral baselines.</summary>
    public void ResetEffects()
    {
        _targetColorTemp    = 0f;
        _colorGradingActive = true;  // let it lerp back to 0

#if UNITY_POST_PROCESSING_STACK_V2
        if (_bloom != null)    _bloom.intensity.Override(bloomBaseline);
        if (_vignette != null) _vignette.intensity.Override(vignetteBaseline);
#endif

        Debug.Log("[PostProcessingManager] Effects reset to baseline.");
    }

    // ─── Coroutines ───────────────────────────────────────────────────────────

    IEnumerator BloomPulseRoutine(float peakIntensity)
    {
        float elapsed   = 0f;
        float halfTime  = bloomPulseDuration * 0.5f;

#if UNITY_POST_PROCESSING_STACK_V2
        while (elapsed < bloomPulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bloomPulseDuration;

            // Ramp up then ramp down (triangle curve)
            float triangleT = t < 0.5f ? t * 2f : 2f - t * 2f;
            float intensity = Mathf.Lerp(bloomBaseline, peakIntensity, triangleT);

            if (_bloom != null) _bloom.intensity.Override(intensity);

            yield return null;
        }

        if (_bloom != null) _bloom.intensity.Override(bloomBaseline);
#else
        yield return null;
#endif
    }

    IEnumerator VignetteFlashRoutine()
    {
        float elapsed = 0f;

#if UNITY_POST_PROCESSING_STACK_V2
        // Quick ramp to dark vignette, then slow recovery
        while (elapsed < vignetteFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / vignetteFlashDuration;

            // Sharp in, slow out
            float curve = 1f - Mathf.Pow(t, 0.3f);
            float intensity = Mathf.Lerp(vignetteBaseline, vignetteHit, curve);

            if (_vignette != null) _vignette.intensity.Override(intensity);

            yield return null;
        }

        if (_vignette != null) _vignette.intensity.Override(vignetteBaseline);
#else
        yield return null;
#endif
    }
}
