using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PrestigeBadge — Reusable UI component that displays:
///   • Tier name + color (e.g. "DIAMOND" in blue)
///   • Prestige level number (e.g. "Prestige 3")
///   • Star row (⭐ per prestige level, up to MaxStarsShown before grouping)
///   • Optional: purple glow on a target Image (for in-game character tint)
///
/// Usage:
///   // In code
///   badge.SetPrestige(3, 2450);          // prestige level 3, 2450 trophies
///   badge.SetPrestige(0, 120);           // no prestige, Rookie
///   badge.ApplyGlowToCharacter(charImg); // tint a character image with purple
///
///   // In Inspector
///   Drag the component onto a UI panel. Wire tier/prestige/stars text fields.
///   Set glowTarget to the character Image for in-game 1v1 use.
///
/// All fields are optional — the component degrades gracefully.
/// </summary>
public class PrestigeBadge : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Tier Display")]
    [Tooltip("Shows tier name, e.g. 'GOLD'")]
    public TMP_Text tierNameText;

    [Tooltip("Background image tinted with tier color")]
    public Image    tierBadgeBackground;

    [Tooltip("Trophy count text, e.g. '🏆 1,240'")]
    public TMP_Text trophyCountText;

    [Header("Prestige Display")]
    [Tooltip("Shows 'Prestige N' — hidden if prestige == 0")]
    public TMP_Text prestigeLevelText;

    [Tooltip("Shows ⭐ stars — hidden if prestige == 0")]
    public TMP_Text prestigeStarsText;

    [Tooltip("Root GameObject to hide entirely when prestige == 0")]
    public GameObject prestigeContainer;

    [Header("Prestige Glow")]
    [Tooltip("Image to apply purple glow tint (character portrait or icon)")]
    public Image    glowTarget;

    [Tooltip("If true, glow tint applies even at prestige 0 (dim). Default: false.")]
    public bool     showGlowWithoutPrestige = false;

    [Header("Style")]
    [Tooltip("Base font size for stars. Clamps on mobile.")]
    public float    starFontSize = 14f;

    [Tooltip("Max individual stars before grouping into batches of 5.")]
    public int      maxRawStars = 10;

    // ─── State ────────────────────────────────────────────────────────────────
    private int _prestige;
    private int _trophies;

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Refresh badge to reflect given prestige level and trophy count.
    /// </summary>
    public void SetPrestige(int prestigeLevel, int trophies)
    {
        _prestige = Mathf.Max(0, prestigeLevel);
        _trophies = Mathf.Max(0, trophies);
        Refresh();
    }

    /// <summary>
    /// Shortcut: read from RankedProgressionManager.Instance.
    /// Safe to call even before RankedProgressionManager is ready (shows defaults).
    /// </summary>
    public void RefreshFromManager()
    {
        if (RankedProgressionManager.Instance == null) return;
        var state = RankedProgressionManager.Instance.State;
        SetPrestige(state.prestigeLevel, state.trophies);
    }

    /// <summary>
    /// Apply prestige purple glow to an external character Image.
    /// Pass null to remove glow.
    /// </summary>
    public void ApplyGlowToCharacter(Image characterImage)
    {
        if (characterImage == null) return;
        if (_prestige > 0 || showGlowWithoutPrestige)
            characterImage.color = RankedProgressionManager.GetPrestigeGlowColor(_prestige);
        else
            characterImage.color = Color.white; // reset to normal
    }

    // ─── Refresh ──────────────────────────────────────────────────────────────

    void Refresh()
    {
        var tier = RankedProgressionManager.GetTierForTrophies(_trophies);

        // Tier name
        if (tierNameText != null)
        {
            tierNameText.text  = tier.name.ToUpper();
            tierNameText.color = tier.color;
        }

        // Badge background
        if (tierBadgeBackground != null)
            tierBadgeBackground.color = new Color(tier.color.r, tier.color.g, tier.color.b, 0.25f);

        // Trophy count
        if (trophyCountText != null)
            trophyCountText.text = $"🏆 {_trophies:N0}";

        // Prestige block
        bool hasPrestige = _prestige > 0;
        if (prestigeContainer != null)
            prestigeContainer.SetActive(hasPrestige);

        if (hasPrestige)
        {
            if (prestigeLevelText != null)
                prestigeLevelText.text = RankedProgressionManager.GetPrestigeLabel(_prestige);

            if (prestigeStarsText != null)
            {
                prestigeStarsText.text     = BuildStarString(_prestige);
                prestigeStarsText.fontSize = starFontSize;
            }
        }

        // Glow on wired glowTarget
        if (glowTarget != null)
        {
            if (hasPrestige)
                glowTarget.color = RankedProgressionManager.GetPrestigeGlowColor(_prestige);
            else if (!showGlowWithoutPrestige)
                glowTarget.color = Color.white;
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static string BuildStarString(int prestigeLevel)
    {
        if (prestigeLevel <= 0) return "";

        // For small values show raw stars; above maxRawStars group into ×N notation
        if (prestigeLevel <= 10)
        {
            string s = "";
            for (int i = 0; i < prestigeLevel; i++) s += "⭐";
            return s;
        }
        else
        {
            // e.g. "⭐×23"
            return $"⭐×{prestigeLevel}";
        }
    }

    // ─── Auto-refresh ─────────────────────────────────────────────────────────

    void OnEnable()
    {
        if (RankedProgressionManager.Instance != null)
        {
            RankedProgressionManager.Instance.OnProgressionChanged += OnProgressionChanged;
            RefreshFromManager();
        }
    }

    void OnDisable()
    {
        if (RankedProgressionManager.Instance != null)
            RankedProgressionManager.Instance.OnProgressionChanged -= OnProgressionChanged;
    }

    void OnProgressionChanged(RankedProgressionManager.ProgressionState state)
    {
        _prestige = state.prestigeLevel;
        _trophies = state.trophies;
        Refresh();
    }
}

/// <summary>
/// TierProgressionBar — Standalone horizontal bar showing progress within current tier.
/// Attach to a Slider GameObject.
/// Useful in GameOverScreen / VictoryScreen to show post-match tier progress.
/// </summary>
public class TierProgressionBar : MonoBehaviour
{
    [Header("References")]
    public Slider   progressSlider;
    public TMP_Text fromTierText;     // "GOLD"
    public TMP_Text toTierText;       // "DIAMOND" (or "MAX" at Legend)
    public TMP_Text progressLabel;    // "240 / 500 to Diamond"
    public TMP_Text trophyChangeText; // "+15 🏆" or "-8 🏆"
    public Image    fillImage;        // Slider fill — tinted tier color

    [Header("Animation")]
    public float    animDuration = 0.8f;

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Show progression bar after a match.
    /// oldTrophies = trophies before match; newTrophies = after.
    /// </summary>
    public void Show(int oldTrophies, int newTrophies)
    {
        var oldTier = RankedProgressionManager.GetTierForTrophies(oldTrophies);
        var newTier = RankedProgressionManager.GetTierForTrophies(newTrophies);
        int delta   = newTrophies - oldTrophies;

        // Tier labels
        if (fromTierText != null)
        {
            fromTierText.text  = newTier.name.ToUpper();
            fromTierText.color = newTier.color;
        }

        if (toTierText != null)
        {
            // Show the next tier name (or MAX)
            var nextTierIndex = (int)newTier.tier + 1;
            if (nextTierIndex < RankedProgressionManager.TIERS.Length)
            {
                var nextTier = RankedProgressionManager.TIERS[nextTierIndex];
                toTierText.text  = nextTier.name.ToUpper();
                toTierText.color = nextTier.color;
            }
            else
            {
                toTierText.text  = "MAX";
                toTierText.color = RankedProgressionManager.TIERS[^1].color;
            }
        }

        // Trophy change label
        if (trophyChangeText != null)
        {
            trophyChangeText.text  = delta >= 0 ? $"+{delta} 🏆" : $"{delta} 🏆";
            trophyChangeText.color = delta >= 0
                ? new Color(0.9f, 0.75f, 0.1f)
                : new Color(0.8f, 0.3f, 0.3f);
        }

        // Progress label
        if (progressLabel != null)
        {
            if (newTier.IsLegend)
                progressLabel.text = "👑 LEGEND MAX";
            else
            {
                int nextMin     = newTier.maxTrophies + 1;
                string nextName = GetNextTierName(newTier);
                progressLabel.text = $"{newTrophies} / {nextMin} to {nextName}";
            }
        }

        // Fill color
        if (fillImage != null) fillImage.color = newTier.color;

        // Animate bar
        float targetFill = newTier.NormalizedProgress(newTrophies);
        float startFill  = newTier.NormalizedProgress(Mathf.Clamp(oldTrophies, newTier.minTrophies, newTier.ProgressionCap));

        if (progressSlider != null) StartCoroutine(AnimateBar(startFill, targetFill));

        // Tier-up flash
        if (oldTier.tier != newTier.tier)
            StartCoroutine(FlashTierUp(newTier));
    }

    System.Collections.IEnumerator AnimateBar(float from, float to)
    {
        if (progressSlider == null) yield break;
        float elapsed = 0f;
        progressSlider.value = from;
        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            progressSlider.value = Mathf.Lerp(from, to, elapsed / animDuration);
            yield return null;
        }
        progressSlider.value = to;
    }

    System.Collections.IEnumerator FlashTierUp(RankedProgressionManager.TierInfo tier)
    {
        if (fromTierText == null) yield break;
        float elapsed = 0f;
        float duration = 0.5f;
        Vector3 baseScale = fromTierText.transform.localScale;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            fromTierText.transform.localScale = Vector3.Lerp(baseScale * 1.4f, baseScale, t);
            yield return null;
        }
        fromTierText.transform.localScale = baseScale;
    }

    static string GetNextTierName(RankedProgressionManager.TierInfo tier)
    {
        int next = (int)tier.tier + 1;
        if (next < RankedProgressionManager.TIERS.Length)
            return RankedProgressionManager.TIERS[next].name;
        return "MAX";
    }
}
