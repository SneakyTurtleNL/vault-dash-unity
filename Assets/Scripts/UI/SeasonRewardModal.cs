using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SeasonRewardModal — "Season X ended! Claim your rewards" pop-up.
///
/// Shows:
///   - Season name + number
///   - Player's peak tier reached (emoji + name)
///   - Gem reward amount
///   - Season cosmetic unlock (if earned via prestige)
///   - Claim button (calls SeasonManager.ClaimSeasonReward — Firestore transaction)
///
/// Usage:
///   SeasonRewardModal.Show(seasonInfo, playerRecord);
///
/// Wire all references in Inspector.
/// Place on a DontDestroyOnLoad canvas or on each scene's UI canvas.
/// </summary>
public class SeasonRewardModal : MonoBehaviour
{
    // ─── Singleton (optional — can also be instantiated per-scene) ────────────
    public static SeasonRewardModal Instance { get; private set; }

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Panel")]
    public GameObject   panelRoot;           // root to activate/deactivate
    public CanvasGroup  canvasGroup;         // for fade in/out

    [Header("Header")]
    public TMP_Text  seasonEndedText;        // "🏁 Season 1 Ended!"
    public TMP_Text  seasonNameText;         // "Neon Vault"
    public Image     seasonThemeBg;          // tinted by season theme color

    [Header("Tier Result")]
    public TMP_Text  tierEmojiText;          // "👑"
    public TMP_Text  tierNameText;           // "LEGEND"
    public TMP_Text  peakTrophiesText;       // "Peak: 4,820 🏆"
    public Image     tierColorBar;           // colored bar matching tier

    [Header("Gem Reward")]
    public TMP_Text  gemAmountText;          // "💎 150 gems"
    public TMP_Text  gemFormulaText;         // "4820 ÷ 100 + Legend bonus"
    public GameObject gemBonusRow;           // hide if no bonus
    public TMP_Text  gemBonusText;           // "+50 Legend bonus"

    [Header("Season Cosmetic")]
    public GameObject cosmeticSection;      // hide if no cosmetic earned
    public TMP_Text  cosmeticNameText;       // "Neon Vault Operative"
    public TMP_Text  cosmeticDescText;       // "Earned free — Prestige 5"
    public Image     cosmeticPreviewImage;   // skin preview (optional)

    [Header("Buttons")]
    public Button    claimButton;
    public TMP_Text  claimButtonText;        // "Claim 150 Gems"
    public Button    closeButton;            // available after claim

    [Header("Claim FX")]
    public GameObject claimFXObject;         // particle / shine effect
    public TMP_Text  alreadyClaimedText;     // "Already claimed ✓"

    // ─── State ────────────────────────────────────────────────────────────────
    private SeasonInfo         _season;
    private PlayerSeasonRecord _record;
    private bool               _claiming;
    private bool               _claimed;

    // ─── Init ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (claimButton  != null) claimButton.onClick.AddListener(OnClaimClicked);
        if (closeButton  != null) closeButton.onClick.AddListener(Hide);

        if (panelRoot    != null) panelRoot.SetActive(false);
    }

    void OnEnable()
    {
        // Hook into SeasonManager so we auto-show when a season ends
        if (SeasonManager.Instance != null)
            SeasonManager.Instance.OnSeasonRewardCalculated += OnRewardReady;
    }

    void OnDisable()
    {
        if (SeasonManager.Instance != null)
            SeasonManager.Instance.OnSeasonRewardCalculated -= OnRewardReady;
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Open the modal for the given season and player record.</summary>
    public static void Show(SeasonInfo season, PlayerSeasonRecord record)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[SeasonRewardModal] No instance found in scene.");
            return;
        }
        Instance.ShowInternal(season, record);
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // ─── Internal ─────────────────────────────────────────────────────────────

    void OnRewardReady(int gems, SeasonInfo endedSeason)
    {
        // Build a minimal record from the event
        var record = new PlayerSeasonRecord
        {
            seasonId     = endedSeason.seasonId,
            peakTrophies = SeasonManager.Instance?.PeakTrophiesThisSeason ?? 0,
            gemReward    = gems,
            finalTier    = RankedProgressionManager.Instance?.State.currentTier.name ?? "Rookie",
            finalPrestige= RankedProgressionManager.Instance?.State.prestigeLevel ?? 0,
        };
        ShowInternal(endedSeason, record);
    }

    void ShowInternal(SeasonInfo season, PlayerSeasonRecord record)
    {
        _season  = season;
        _record  = record;
        _claiming= false;
        _claimed = record.claimedSeasonReward;

        PopulateUI();

        if (panelRoot != null) panelRoot.SetActive(true);
        if (claimFXObject != null) claimFXObject.SetActive(false);

        // Fade in
        StartCoroutine(FadeIn());
    }

    void PopulateUI()
    {
        if (_season == null || _record == null) return;

        // ── Header ────────────────────────────────────────────────────────────
        if (seasonEndedText != null)
            seasonEndedText.text = $"🏁 Season {_season.seasonNumber} Ended!";
        if (seasonNameText != null)
            seasonNameText.text = _season.name;

        // Theme tint
        if (seasonThemeBg != null && _season.cosmetic != null)
            seasonThemeBg.color = _season.cosmetic.ThemeColorUnity * 0.4f;

        // ── Tier result ───────────────────────────────────────────────────────
        var tier = RankedProgressionManager.GetTierForTrophies(_record.peakTrophies);
        if (tierEmojiText  != null) tierEmojiText.text  = tier.emoji;
        if (tierNameText   != null)
        {
            tierNameText.text  = tier.name.ToUpper();
            tierNameText.color = tier.color;
        }
        if (peakTrophiesText != null)
            peakTrophiesText.text = $"Peak: {_record.peakTrophies:N0} 🏆";
        if (tierColorBar != null) tierColorBar.color = tier.color;

        // ── Gem reward ────────────────────────────────────────────────────────
        int gems  = _record.gemReward;
        int bonus = GetTierBonus(_record.peakTrophies);

        if (gemAmountText  != null) gemAmountText.text  = $"💎 {gems} gems";
        if (gemFormulaText != null)
            gemFormulaText.text = $"{_record.peakTrophies} ÷ 100 = {gems - bonus}";

        if (gemBonusRow != null) gemBonusRow.SetActive(bonus > 0);
        if (gemBonusText != null && bonus > 0)
            gemBonusText.text = $"+{bonus} {tier.name} bonus";

        // ── Cosmetic section ──────────────────────────────────────────────────
        bool skinEarned = _season.cosmetic != null &&
                          SeasonManager.Instance != null &&
                          SeasonManager.Instance.PlayerEarnedSeasonSkin();

        if (cosmeticSection != null) cosmeticSection.SetActive(skinEarned);
        if (skinEarned && _season.cosmetic != null)
        {
            if (cosmeticNameText != null)
                cosmeticNameText.text = _season.cosmetic.skinName;
            if (cosmeticDescText != null)
                cosmeticDescText.text = $"Earned free — Prestige {_season.cosmetic.prestigeFree} ✅";
        }

        // ── Claim button ──────────────────────────────────────────────────────
        if (claimButton != null) claimButton.gameObject.SetActive(!_claimed);
        if (alreadyClaimedText != null) alreadyClaimedText.gameObject.SetActive(_claimed);
        if (closeButton  != null) closeButton.gameObject.SetActive(_claimed);

        if (claimButtonText != null)
            claimButtonText.text = $"Claim {gems} 💎";
    }

    // ─── Claim ────────────────────────────────────────────────────────────────

    void OnClaimClicked()
    {
        if (_claiming || _claimed || _season == null) return;
        _claiming = true;
        if (claimButton != null) claimButton.interactable = false;
        StartCoroutine(DoClaim());
    }

    IEnumerator DoClaim()
    {
        int awarded = 0;

        if (SeasonManager.Instance != null)
        {
            yield return StartCoroutine(
                SeasonManager.Instance.ClaimSeasonReward(_season.seasonId, gems => awarded = gems));
        }
        else
        {
            // Stub
            awarded = _record.gemReward;
            yield return new WaitForSecondsRealtime(0.5f);
        }

        _claimed  = true;
        _claiming = false;

        // FX
        if (claimFXObject != null) claimFXObject.SetActive(true);
        if (claimButton   != null) claimButton.gameObject.SetActive(false);
        if (alreadyClaimedText != null)
        {
            alreadyClaimedText.gameObject.SetActive(true);
            alreadyClaimedText.text = $"✅ {awarded} gems claimed!";
        }
        if (closeButton != null) closeButton.gameObject.SetActive(true);

        Debug.Log($"[SeasonRewardModal] ✅ Claimed {awarded} gems for {_season.seasonId}");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static int GetTierBonus(int peakTrophies)
    {
        if (peakTrophies >= 4500) return 50;
        if (peakTrophies >= 3500) return 25;
        if (peakTrophies >= 2000) return 10;
        return 0;
    }

    IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;
        canvasGroup.alpha = 0f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 4f;
            canvasGroup.alpha = Mathf.Clamp01(t);
            yield return null;
        }
    }

    // ─── Debug ────────────────────────────────────────────────────────────────

    [ContextMenu("Debug: Show Test Modal")]
    void DbgShow()
    {
        var stubSeason = new SeasonInfo
        {
            seasonId     = "season_1",
            seasonNumber = 1,
            name         = "Neon Vault",
            theme        = "neon",
            cosmetic     = new SeasonCosmetic
            {
                skinName     = "Neon Vault Operative",
                themeColor   = "#4444FF",
                prestigeFree = 5,
            },
        };
        var stubRecord = new PlayerSeasonRecord
        {
            seasonId     = "season_1",
            peakTrophies = 4820,
            finalTier    = "Legend",
            finalPrestige= 2,
            gemReward    = PlayerSeasonRecord.CalculateGemReward(4820),
        };
        ShowInternal(stubSeason, stubRecord);
    }
}
