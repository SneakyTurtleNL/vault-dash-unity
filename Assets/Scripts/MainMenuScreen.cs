using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MainMenuScreen — Hero entry point for Vault Dash.
///
/// Features:
///   • Animated character showcase (big portrait, cycles through 3 characters)
///   • Quick play button → CharacterSelection → Game
///   • Navigation: Shop, Profile, Ranked Ladder buttons
///   • Gem + coin balance display
///   • Animated title + tagline
///   • Network status indicator (online/offline)
///
/// Wire all references in Inspector.
/// Relies on UIManager for screen navigation.
/// </summary>
public class MainMenuScreen : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Title")]
    public TMP_Text titleText;
    public TMP_Text taglineText;

    [Header("Character Showcase")]
    public Image        showcasePortrait;
    public TMP_Text     showcaseCharName;
    public TMP_Text     showcaseTagline;
    public float        showcaseRotationInterval = 4f;

    [Header("Currency Bar")]
    public TMP_Text gemCountText;
    public TMP_Text coinCountText;

    [Header("Navigation Buttons")]
    public Button quickPlayButton;
    public Button characterSelectButton;
    public Button shopButton;
    public Button profileButton;
    public Button rankedButton;

    [Header("Network Status")]
    public TMP_Text  networkStatusText;
    public Image     networkStatusIcon;
    public Color     onlineColor  = new Color(0.2f, 0.8f, 0.3f);
    public Color     offlineColor = new Color(0.8f, 0.3f, 0.2f);

    [Header("Version")]
    public TMP_Text  versionText;

    [Header("Season Countdown")]
    [Tooltip("Attach a SeasonCountdownWidget component; it refreshes itself")]
    public SeasonCountdownWidget seasonCountdownWidget;
    [Tooltip("Fallback text if SeasonCountdownWidget not used")]
    public TMP_Text              seasonCountdownText;   // "Season 1 ends in 3d 4h"
    public TMP_Text              seasonNameBadgeText;   // "🔥 NEON VAULT"
    public Image                 seasonThemeAccent;     // colored line (theme color)
    public Button                seasonBannerButton;    // tap → ranked screen

    [Header("Animated Elements")]
    public RectTransform titleContainer;  // bounces on entry
    public CanvasGroup   buttonGroup;     // fades in staggered

    // ─── Private ──────────────────────────────────────────────────────────────
    private int       _showcaseIndex = 0;
    private Coroutine _showcaseRoutine;
    private Coroutine _entryRoutine;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Start()
    {
        // Wire buttons
        if (quickPlayButton      != null) quickPlayButton.onClick.AddListener(OnQuickPlay);
        if (characterSelectButton!= null) characterSelectButton.onClick.AddListener(OnCharacterSelect);
        if (shopButton           != null) shopButton.onClick.AddListener(OnShop);
        if (profileButton        != null) profileButton.onClick.AddListener(OnProfile);
        if (rankedButton         != null) rankedButton.onClick.AddListener(OnRanked);

        // Version
        if (versionText != null)
            versionText.text = $"v{Application.version}  Alpha";
    }

    void OnEnable()
    {
        RefreshCurrency();
        RefreshNetworkStatus();
        RefreshSeasonBanner();

        // Subscribe to live season changes
        if (SeasonManager.Instance != null)
        {
            SeasonManager.Instance.OnSeasonChanged   -= OnSeasonChanged;
            SeasonManager.Instance.OnSeasonChanged   += OnSeasonChanged;
            SeasonManager.Instance.OnSeasonEndingSoon-= OnSeasonEndingSoon;
            SeasonManager.Instance.OnSeasonEndingSoon+= OnSeasonEndingSoon;
        }

        // Wire season banner button
        if (seasonBannerButton != null)
            seasonBannerButton.onClick.AddListener(OnSeasonBannerTapped);
        StartShowcase();
        PlayEntryAnimation();
    }

    void OnDisable()
    {
        if (_showcaseRoutine != null) StopCoroutine(_showcaseRoutine);
        if (_entryRoutine    != null) StopCoroutine(_entryRoutine);

        if (SeasonManager.Instance != null)
        {
            SeasonManager.Instance.OnSeasonChanged    -= OnSeasonChanged;
            SeasonManager.Instance.OnSeasonEndingSoon -= OnSeasonEndingSoon;
        }

        if (seasonBannerButton != null)
            seasonBannerButton.onClick.RemoveListener(OnSeasonBannerTapped);
    }

    // ─── Season Banner ────────────────────────────────────────────────────────

    void OnSeasonChanged(SeasonInfo newSeason)
    {
        RefreshSeasonBanner();
    }

    void OnSeasonEndingSoon(SeasonInfo season, System.TimeSpan remaining)
    {
        // Optionally show a notification or pulse animation
        if (seasonCountdownText != null)
            seasonCountdownText.color = new Color(1f, 0.4f, 0.3f); // urgent red
    }

    void RefreshSeasonBanner()
    {
        // SeasonCountdownWidget handles its own refresh — just make sure it's active
        if (seasonCountdownWidget != null)
        {
            seasonCountdownWidget.gameObject.SetActive(true);
            seasonCountdownWidget.Refresh();
            return;
        }

        // Fallback: manual text update
        var season = SeasonManager.Instance?.CurrentSeason;
        if (season == null)
        {
            if (seasonCountdownText != null) seasonCountdownText.text = "";
            return;
        }

        if (seasonCountdownText != null)
        {
            var remaining = season.TimeRemaining;
            if (remaining.TotalSeconds > 0)
                seasonCountdownText.text = $"Season {season.seasonNumber} ends in {season.TimeRemainingFormatted}";
            else
                seasonCountdownText.text = "Season ended";
        }

        if (seasonNameBadgeText != null)
            seasonNameBadgeText.text = $"🏆 {season.name.ToUpper()}";

        if (seasonThemeAccent != null && season.cosmetic != null)
            seasonThemeAccent.color = season.cosmetic.ThemeColorUnity;
    }

    void OnSeasonBannerTapped() => UIManager.Instance?.ShowRankedLadder();

    // ─── Currency ─────────────────────────────────────────────────────────────
    void RefreshCurrency()
    {
        if (gemCountText  != null) gemCountText.text  = $"💎 {PlayerPrefs.GetInt("VaultDash_Gems", 0)}";
        if (coinCountText != null) coinCountText.text = $"🪙 {PlayerPrefs.GetInt("VaultDash_Coins", 0)}";
    }

    // ─── Network Status ───────────────────────────────────────────────────────
    void RefreshNetworkStatus()
    {
        bool online = Application.internetReachability != NetworkReachability.NotReachable;

        if (networkStatusText != null)
            networkStatusText.text = online ? "● Online" : "○ Offline";

        if (networkStatusIcon != null)
            networkStatusIcon.color = online ? onlineColor : offlineColor;
    }

    // ─── Character Showcase ───────────────────────────────────────────────────
    void StartShowcase()
    {
        if (_showcaseRoutine != null) StopCoroutine(_showcaseRoutine);
        _showcaseRoutine = StartCoroutine(ShowcaseRotation());
    }

    IEnumerator ShowcaseRotation()
    {
        // Show featured characters (first 3 unlocked)
        while (true)
        {
            UpdateShowcase(_showcaseIndex);
            yield return new WaitForSeconds(showcaseRotationInterval);
            _showcaseIndex = (_showcaseIndex + 1) % 3; // cycle top 3
        }
    }

    void UpdateShowcase(int index)
    {
        if (CharacterDatabase.Instance == null) return;

        var profile = CharacterDatabase.Instance.GetProfile(index);
        if (profile == null) return;

        if (showcasePortrait != null && profile.portraitSprite != null)
        {
            StartCoroutine(CrossFadePortrait(profile.portraitSprite));
        }

        if (showcaseCharName != null) showcaseCharName.text = profile.displayName;
        if (showcaseTagline  != null) showcaseTagline.text  = profile.description;
    }

    IEnumerator CrossFadePortrait(Sprite newSprite)
    {
        if (showcasePortrait == null) yield break;

        // Fade out
        float elapsed = 0f;
        Color c = showcasePortrait.color;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            showcasePortrait.color = new Color(c.r, c.g, c.b, 1f - elapsed / 0.2f);
            yield return null;
        }

        showcasePortrait.sprite = newSprite;

        // Fade in
        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            showcasePortrait.color = new Color(c.r, c.g, c.b, elapsed / 0.3f);
            yield return null;
        }
        showcasePortrait.color = c;
    }

    // ─── Entry Animation ──────────────────────────────────────────────────────
    void PlayEntryAnimation()
    {
        if (_entryRoutine != null) StopCoroutine(_entryRoutine);
        _entryRoutine = StartCoroutine(EntryRoutine());
    }

    IEnumerator EntryRoutine()
    {
        // Title bounce
        if (titleContainer != null)
        {
            titleContainer.localScale = Vector3.one * 0.7f;
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / 0.4f;
                // Overshoot spring curve
                float scale = Mathf.LerpUnclamped(0.7f, 1f, t < 0.7f
                    ? t / 0.7f * 1.15f
                    : 1f + (1f - (t - 0.7f) / 0.3f) * 0.15f);
                titleContainer.localScale = Vector3.one * Mathf.Max(0.7f, scale);
                yield return null;
            }
            titleContainer.localScale = Vector3.one;
        }

        // Buttons fade-in staggered
        if (buttonGroup != null)
        {
            buttonGroup.alpha = 0f;
            yield return new WaitForSecondsRealtime(0.1f);

            float elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.unscaledDeltaTime;
                buttonGroup.alpha = elapsed / 0.35f;
                yield return null;
            }
            buttonGroup.alpha = 1f;
        }
    }

    // ─── Navigation ───────────────────────────────────────────────────────────
    void OnQuickPlay()
    {
        // Quick play: use last selected character, skip selection screen
        int lastChar = PlayerPrefs.GetInt("SelectedCharacter", 0);
        Debug.Log($"[MainMenu] Quick Play — character {lastChar}");
        MatchManager.Instance?.FindMatch();
        UIManager.Instance?.ShowGameHUD();
    }

    void OnCharacterSelect()
    {
        UIManager.Instance?.ShowCharacterSelection();
    }

    void OnShop()
    {
        UIManager.Instance?.ShowShop();
    }

    void OnProfile()
    {
        UIManager.Instance?.ShowProfile();
    }

    void OnRanked()
    {
        UIManager.Instance?.ShowRankedLadder();
    }

    // ─── Update ───────────────────────────────────────────────────────────────
    void Update()
    {
        // Refresh network status occasionally
        if (Time.frameCount % 300 == 0) RefreshNetworkStatus();
    }
}
