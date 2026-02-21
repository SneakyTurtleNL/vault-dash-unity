using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ProfileScreen — Player stats, mastery progression, skins inventory, settings.
///
/// Sections:
///   STATS     — Total wins, avg score, best run, trophies, win rate
///   MASTERY   — Per-character mastery XP bars
///   INVENTORY — Owned skins grid
///   SETTINGS  — Audio, graphics, language toggles
///
/// Wire all references in Inspector.
/// </summary>
public class ProfileScreen : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Player Identity")]
    public TMP_Text  playerNameText;
    public TMP_Text  playerLevelText;
    public TMP_Text  playerTitleText;
    public Image     playerAvatarImage;
    public TMP_Text  gemBalanceText;

    [Header("Prestige Badge")]
    [Tooltip("Attach a PrestigeBadge component here — auto-refreshed on activate")]
    public PrestigeBadge prestigeBadge;
    public TMP_Text  prestigeTierText;    // e.g. "⭐⭐ DIAMOND" (standalone fallback)
    public TMP_Text  prestigeStarsText;   // standalone star row
    public Image     prestigeGlowRing;   // paarse ring rond avatar (optional)

    [Header("Section Tabs")]
    public Button    tabStatsButton;
    public Button    tabMasteryButton;
    public Button    tabInventoryButton;
    public Button    tabSettingsButton;
    public GameObject statsPanel;
    public GameObject masteryPanel;
    public GameObject inventoryPanel;
    public GameObject settingsPanel;
    public Color tabActiveColor   = new Color(0.9f, 0.75f, 0.1f);
    public Color tabInactiveColor = new Color(0.3f, 0.3f, 0.35f);

    [Header("Stats Panel")]
    public TMP_Text  totalWinsText;
    public TMP_Text  totalMatchesText;
    public TMP_Text  winRateText;
    public TMP_Text  bestScoreText;
    public TMP_Text  bestDistanceText;
    public TMP_Text  totalTrophiesText;
    public TMP_Text  currentRankText;

    [Header("Mastery Panel")]
    public Transform      masteryContainer;
    public GameObject     masteryRowPrefab;

    [Header("Inventory Panel")]
    public Transform      inventoryContainer;
    public GameObject     inventoryItemPrefab;
    public TMP_Text       noSkinsText;

    [Header("Settings Panel")]
    public Slider    masterVolumeSlider;
    public Slider    sfxVolumeSlider;
    public Slider    musicVolumeSlider;
    public TMP_Text  masterVolumeLabel;
    public TMP_Text  sfxVolumeLabel;
    public TMP_Text  musicVolumeLabel;
    public Toggle    vibrationToggle;
    public Toggle    notificationsToggle;
    public TMP_Dropdown graphicsDropdown;
    public TMP_Dropdown languageDropdown;
    public Button    signOutButton;
    public Button    privacyPolicyButton;

    [Header("Season Info (Stats Panel)")]
    [Tooltip("e.g. 'Season 1 — Neon Vault'")]
    public TMP_Text  currentSeasonText;     // "Season 1 — Neon Vault"
    public TMP_Text  peakTrophiesText;      // "Peak this season: 2,340 🏆"
    public TMP_Text  seasonRewardText;      // "Season reward: 23 💎"
    public TMP_Text  seasonEndText;         // "Ends in 3d 4h"
    public Image     seasonAccentBar;       // colored by season theme
    public GameObject seasonInfoSection;   // show/hide the whole block

    [Header("Back")]
    public Button backButton;

    // ─── State ────────────────────────────────────────────────────────────────
    private enum Tab { Stats, Mastery, Inventory, Settings }
    private Tab _activeTab = Tab.Stats;
    private bool _initialized = false;

    // PlayerPrefs keys
    private const string WINS_KEY         = "VaultDash_TotalWins";
    private const string MATCHES_KEY      = "VaultDash_TotalMatches";
    private const string BEST_SCORE_KEY   = "VaultDash_HighScore";
    private const string BEST_DIST_KEY    = "VaultDash_BestDistance";
    private const string TROPHIES_KEY     = "VaultDash_Trophies";
    private const string RANK_KEY         = "VaultDash_Rank";
    private const string GEM_KEY          = "VaultDash_Gems";
    private const string LEVEL_KEY        = "VaultDash_PlayerLevel";
    private const string PLAYER_NAME_KEY  = "VaultDash_PlayerName";
    private const string PLAYER_TITLE_KEY = "VaultDash_PlayerTitle";

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Start()
    {
        if (backButton         != null) backButton.onClick.AddListener(OnBack);
        if (tabStatsButton     != null) tabStatsButton.onClick.AddListener(() => SwitchTab(Tab.Stats));
        if (tabMasteryButton   != null) tabMasteryButton.onClick.AddListener(() => SwitchTab(Tab.Mastery));
        if (tabInventoryButton != null) tabInventoryButton.onClick.AddListener(() => SwitchTab(Tab.Inventory));
        if (tabSettingsButton  != null) tabSettingsButton.onClick.AddListener(() => SwitchTab(Tab.Settings));

        SetupSettingsSliders();

        if (signOutButton       != null) signOutButton.onClick.AddListener(OnSignOut);
        if (privacyPolicyButton != null) privacyPolicyButton.onClick.AddListener(OnPrivacyPolicy);
    }

    // ─── Activation ───────────────────────────────────────────────────────────
    public void OnActivate()
    {
        // Subscribe to season events for live season info updates
        if (SeasonManager.Instance != null)
        {
            SeasonManager.Instance.OnSeasonChanged -= OnSeasonChanged;
            SeasonManager.Instance.OnSeasonChanged += OnSeasonChanged;
        }

        RefreshPlayerIdentity();
        SwitchTab(_activeTab);
    }

    void OnDisable()
    {
        if (SeasonManager.Instance != null)
            SeasonManager.Instance.OnSeasonChanged -= OnSeasonChanged;
    }

    void OnSeasonChanged(SeasonInfo _) => RefreshSeasonInfo();

    // ─── Player Identity ──────────────────────────────────────────────────────
    void RefreshPlayerIdentity()
    {
        string name  = PlayerPrefs.GetString(PLAYER_NAME_KEY,  "Vault Runner");
        int    level = PlayerPrefs.GetInt(LEVEL_KEY, 1);
        int    gems  = PlayerPrefs.GetInt(GEM_KEY, 0);

        // Derive title from tier (prefer live tier over stored string)
        int    trophies  = PlayerPrefs.GetInt(TROPHIES_KEY, 0);
        int    prestige  = PlayerPrefs.GetInt("VaultDash_PrestigeLevel", 0);
        var    tier      = RankedProgressionManager.GetTierForTrophies(trophies);
        string title     = prestige > 0
            ? $"Prestige {prestige} · {tier.name}"
            : tier.name;

        if (playerNameText  != null) playerNameText.text  = name;
        if (playerLevelText != null) playerLevelText.text = $"Level {level}";
        if (playerTitleText != null)
        {
            playerTitleText.text  = title;
            playerTitleText.color = tier.color;
        }
        if (gemBalanceText  != null) gemBalanceText.text  = $"💎 {gems}";

        // ─── Prestige Badge ───────────────────────────────────────────────────
        if (prestigeBadge != null)
        {
            prestigeBadge.SetPrestige(prestige, trophies);
        }
        else
        {
            // Fallback standalone fields
            if (prestigeTierText != null)
            {
                string stars = RankedProgressionManager.GetPrestigeStars(prestige);
                prestigeTierText.text  = prestige > 0
                    ? $"{stars} {tier.name.ToUpper()}"
                    : tier.name.ToUpper();
                prestigeTierText.color = tier.color;
            }
            if (prestigeStarsText != null)
            {
                prestigeStarsText.text         = RankedProgressionManager.GetPrestigeStars(prestige);
                prestigeStarsText.gameObject.SetActive(prestige > 0);
            }
        }

        // Purple glow ring on avatar
        if (prestigeGlowRing != null)
        {
            if (prestige > 0)
            {
                prestigeGlowRing.gameObject.SetActive(true);
                prestigeGlowRing.color = RankedProgressionManager.GetPrestigeGlowColor(prestige);
            }
            else
            {
                prestigeGlowRing.gameObject.SetActive(false);
            }
        }
    }

    // ─── Tab Switching ────────────────────────────────────────────────────────
    void SwitchTab(Tab tab)
    {
        _activeTab = tab;

        if (statsPanel     != null) statsPanel.SetActive(tab == Tab.Stats);
        if (masteryPanel   != null) masteryPanel.SetActive(tab == Tab.Mastery);
        if (inventoryPanel != null) inventoryPanel.SetActive(tab == Tab.Inventory);
        if (settingsPanel  != null) settingsPanel.SetActive(tab == Tab.Settings);

        SetTabTint(tabStatsButton,     tab == Tab.Stats);
        SetTabTint(tabMasteryButton,   tab == Tab.Mastery);
        SetTabTint(tabInventoryButton, tab == Tab.Inventory);
        SetTabTint(tabSettingsButton,  tab == Tab.Settings);

        switch (tab)
        {
            case Tab.Stats:     PopulateStats();     break;
            case Tab.Mastery:   PopulateMastery();   break;
            case Tab.Inventory: PopulateInventory(); break;
            case Tab.Settings:  RefreshSettings();   break;
        }
    }

    void SetTabTint(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? tabActiveColor : tabInactiveColor;
    }

    // ─── Stats ────────────────────────────────────────────────────────────────
    void PopulateStats()
    {
        int wins    = PlayerPrefs.GetInt(WINS_KEY,       0);
        int matches = PlayerPrefs.GetInt(MATCHES_KEY,    0);
        int score   = PlayerPrefs.GetInt(BEST_SCORE_KEY, 0);
        float dist  = PlayerPrefs.GetFloat(BEST_DIST_KEY, 0f);
        int trophies= PlayerPrefs.GetInt(TROPHIES_KEY,   0);
        int prestige= PlayerPrefs.GetInt("VaultDash_PrestigeLevel", 0);

        var tier = RankedProgressionManager.GetTierForTrophies(trophies);
        float winRate = matches > 0 ? (float)wins / matches * 100f : 0f;

        if (totalWinsText    != null) totalWinsText.text    = $"Wins  {wins}";
        if (totalMatchesText != null) totalMatchesText.text = $"Matches  {matches}";
        if (winRateText      != null) winRateText.text      = $"Win Rate  {winRate:F0}%";
        if (bestScoreText    != null) bestScoreText.text    = $"Best Score  {score:N0}";
        if (bestDistanceText != null) bestDistanceText.text = $"Best Run  {dist:F0}m";
        if (totalTrophiesText!= null) totalTrophiesText.text= $"Trophies 🏆 {trophies:N0}";

        if (currentRankText  != null)
        {
            string stars = RankedProgressionManager.GetPrestigeStars(prestige);
            string rankDisplay = prestige > 0
                ? $"{tier.emoji} {tier.name}  {stars}  Prestige {prestige}"
                : $"{tier.emoji} {tier.name}";
            currentRankText.text  = rankDisplay;
            currentRankText.color = tier.color;
        }

        // Season info block
        RefreshSeasonInfo();
    }

    // ─── Season Info ──────────────────────────────────────────────────────────

    void RefreshSeasonInfo()
    {
        if (seasonInfoSection != null)
            seasonInfoSection.SetActive(SeasonManager.Instance != null && SeasonManager.Instance.IsInitialized);

        var season = SeasonManager.Instance?.CurrentSeason;
        if (season == null) return;

        // Season label
        if (currentSeasonText != null)
            currentSeasonText.text = $"Season {season.seasonNumber} — {season.name}";

        // Peak trophies this season (from SeasonManager live cache)
        int peakTrophies = SeasonManager.Instance?.PeakTrophiesThisSeason ?? 0;
        if (peakTrophiesText != null)
            peakTrophiesText.text = $"Peak this season: {peakTrophies:N0} 🏆";

        // Estimated season reward
        int estimatedGems = PlayerSeasonRecord.CalculateGemReward(peakTrophies);
        if (seasonRewardText != null)
            seasonRewardText.text = $"Est. season reward: {estimatedGems} 💎";

        // Time remaining
        if (seasonEndText != null)
            seasonEndText.text = season.TimeRemaining.TotalSeconds > 0
                ? $"Ends in {season.TimeRemainingFormatted}"
                : "Season ended";

        // Theme accent
        if (seasonAccentBar != null && season.cosmetic != null)
            seasonAccentBar.color = season.cosmetic.ThemeColorUnity;
        else if (seasonAccentBar != null && season.arenaOverlay != null)
            seasonAccentBar.color = season.arenaOverlay.PrimaryColor;
    }

    // ─── Mastery ──────────────────────────────────────────────────────────────
    void PopulateMastery()
    {
        if (masteryContainer == null) return;

        // Clear
        foreach (Transform child in masteryContainer)
            Destroy(child.gameObject);

        string[] charNames = { "Agent Zero","Blaze","Knox","Jade","Vector","Cipher","Nova","Ryze","Titan","Echo" };

        for (int i = 0; i < charNames.Length; i++)
        {
            int mastery   = PlayerPrefs.GetInt($"Char_{i}_Mastery", 0);
            int level     = PlayerPrefs.GetInt($"Char_{i}_Level",   1);
            int threshold = 500 + level * 250;

            if (masteryRowPrefab != null)
            {
                GameObject row = Instantiate(masteryRowPrefab, masteryContainer);
                // Assuming prefab has: TMP_Text "CharName", TMP_Text "XP", Slider "Bar"
                var nameLabel = row.transform.Find("CharName")?.GetComponent<TMP_Text>();
                var xpLabel   = row.transform.Find("XP")?.GetComponent<TMP_Text>();
                var bar       = row.transform.Find("Bar")?.GetComponent<Slider>();

                if (nameLabel != null) nameLabel.text = $"{charNames[i]}  Lv{level}";
                if (xpLabel   != null) xpLabel.text   = $"{mastery:N0} / {threshold:N0} XP";
                if (bar       != null) bar.value       = Mathf.Clamp01((float)mastery / threshold);
            }
            else
            {
                // Fallback: text row
                GameObject go = new GameObject($"MasteryRow_{i}");
                go.transform.SetParent(masteryContainer, false);
                var text = go.AddComponent<TMP_Text>();
                text.text = $"{charNames[i].PadRight(12)} Lv{level}   {mastery}/{threshold} XP";
                text.fontSize = 14;
                text.color = Color.white;
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(600f, 30f);
            }
        }
    }

    // ─── Inventory ────────────────────────────────────────────────────────────
    void PopulateInventory()
    {
        if (inventoryContainer == null) return;

        foreach (Transform child in inventoryContainer)
            Destroy(child.gameObject);

        // Check which skins are owned
        string[] skinIds = {
            "agz_gold","agz_shadow","blz_fire","blz_ice","knx_mech",
            "jde_neon","vec_chrome","cph_glitch","nova_void","ryze_arc"
        };
        string[] skinNames = {
            "Gold Operative","Shadow Protocol","Inferno Blaze","Cryo Blaze","Mechknox 3000",
            "Neon Jade","Chrome Vector","Glitch Cipher","Void Nova","Arc Ryze"
        };

        int ownedCount = 0;

        for (int i = 0; i < skinIds.Length; i++)
        {
            if (PlayerPrefs.GetInt($"Skin_{skinIds[i]}", 0) == 0) continue;

            ownedCount++;

            if (inventoryItemPrefab != null)
            {
                GameObject item = Instantiate(inventoryItemPrefab, inventoryContainer);
                var label = item.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = skinNames[i];
            }
            else
            {
                GameObject go = new GameObject($"SkinItem_{i}");
                go.transform.SetParent(inventoryContainer, false);
                var text = go.AddComponent<TMP_Text>();
                text.text = $"✓ {skinNames[i]}";
                text.fontSize = 13;
                text.color = new Color(0.9f, 0.75f, 0.1f);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200f, 40f);
            }
        }

        if (noSkinsText != null)
            noSkinsText.gameObject.SetActive(ownedCount == 0);
    }

    // ─── Settings ─────────────────────────────────────────────────────────────
    void SetupSettingsSliders()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.value = PlayerPrefs.GetFloat("Audio_Master", 1f);
            masterVolumeSlider.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetMasterVolume(v);
                FMODAudioManager.Instance?.SetMasterVolume(v);
                if (masterVolumeLabel != null) masterVolumeLabel.text = $"Master  {v * 100f:F0}%";
            });
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("Audio_SFX", 0.8f);
            sfxVolumeSlider.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetSFXVolume(v);
                FMODAudioManager.Instance?.SetSFXVolume(v);
                if (sfxVolumeLabel != null) sfxVolumeLabel.text = $"SFX  {v * 100f:F0}%";
            });
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.value = PlayerPrefs.GetFloat("Audio_Music", 0.45f);
            musicVolumeSlider.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetMusicVolume(v);
                FMODAudioManager.Instance?.SetMusicVolume(v);
                if (musicVolumeLabel != null) musicVolumeLabel.text = $"Music  {v * 100f:F0}%";
            });
        }

        if (vibrationToggle != null)
        {
            vibrationToggle.isOn = PlayerPrefs.GetInt("Vibration", 1) == 1;
            vibrationToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt("Vibration", v ? 1 : 0));
        }

        if (notificationsToggle != null)
        {
            notificationsToggle.isOn = PlayerPrefs.GetInt("Notifications", 1) == 1;
            notificationsToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt("Notifications", v ? 1 : 0));
        }
    }

    void RefreshSettings()
    {
        if (masterVolumeSlider != null)
        {
            float v = PlayerPrefs.GetFloat("Audio_Master", 1f);
            masterVolumeSlider.SetValueWithoutNotify(v);
            if (masterVolumeLabel != null) masterVolumeLabel.text = $"Master  {v * 100f:F0}%";
        }

        if (sfxVolumeSlider != null)
        {
            float v = PlayerPrefs.GetFloat("Audio_SFX", 0.8f);
            sfxVolumeSlider.SetValueWithoutNotify(v);
            if (sfxVolumeLabel != null) sfxVolumeLabel.text = $"SFX  {v * 100f:F0}%";
        }

        if (musicVolumeSlider != null)
        {
            float v = PlayerPrefs.GetFloat("Audio_Music", 0.45f);
            musicVolumeSlider.SetValueWithoutNotify(v);
            if (musicVolumeLabel != null) musicVolumeLabel.text = $"Music  {v * 100f:F0}%";
        }
    }

    void OnSignOut()
    {
        // Clear player session (keep stats)
        PlayerPrefs.DeleteKey("VaultDash_AuthToken");
        Debug.Log("[ProfileScreen] Signed out.");
    }

    void OnPrivacyPolicy()
    {
        Application.OpenURL("https://vaultdash.gg/privacy");
    }

    // ─── Back ─────────────────────────────────────────────────────────────────
    void OnBack() => UIManager.Instance?.GoBack();
}
