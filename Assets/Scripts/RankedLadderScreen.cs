using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// RankedLadderScreen — Trophy rank + top 100 leaderboard display.
///
/// Sections:
///   RANK CARD    — Current rank icon + trophy count + progress bar to next rank
///   LEADERBOARD  — Scrollable top 100 (fetched from Nakama leaderboard)
///   RANKED INFO  — How ranked mode works
///
/// Real data source: Nakama leaderboard API (when NAKAMA_AVAILABLE defined).
/// Fallback: simulated leaderboard from PlayerPrefs.
/// </summary>
public class RankedLadderScreen : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Rank Card")]
    public TMP_Text     rankNameText;           // "GOLD"
    public TMP_Text     trophyCountText;        // "1,240" (trophy icon via Image)
    public TMP_Text     rankProgressText;       // "240 / 500 to Diamond"
    public Slider       rankProgressBar;
    public Image        rankIconImage;
    public TMP_Text     playerPositionText;     // "#47 globally"

    [Header("Prestige in Rank Card")]
    [Tooltip("Shows prestige level + stars on the rank card")]
    public PrestigeBadge rankCardPrestigeBadge;
    public TMP_Text     rankPrestigeLevelText;  // "Prestige 3"
    public TMP_Text     rankPrestigeStarsText;  // "★★★"
    public GameObject   rankPrestigeSection;    // hidden if prestige == 0

    public Color[]      rankColors = new Color[]
    {
        new Color(0.68f, 0.68f, 0.68f), // Rookie  (grey)
        new Color(0.80f, 0.85f, 0.90f), // Silver  (silver)
        new Color(0.90f, 0.75f, 0.10f), // Gold    (gold)
        new Color(0.40f, 0.70f, 1.00f), // Diamond (blue)
        new Color(0.80f, 0.40f, 1.00f), // Master  (purple)
        new Color(1.00f, 0.84f, 0.00f), // Legend  (gold/yellow)
    };

    [Header("Leaderboard")]
    public Transform    leaderboardContainer;   // vertical layout content
    public GameObject   leaderboardRowPrefab;   // row prefab: rank# | name | trophies | rank icon
    public ScrollRect   leaderboardScrollRect;
    public TMP_Text     loadingText;
    public Button       refreshButton;
    public int          displayCount = 100;

    [Header("Rank Info")]
    public GameObject   rankInfoPanel;
    public Button       toggleRankInfoButton;
    public TMP_Text     rankInfoText;

    [Header("Season Selector")]
    [Tooltip("Dropdown listing 'Current Season', 'Season 2', 'Season 1'…")]
    public TMP_Dropdown seasonDropdown;
    public TMP_Text     seasonDropdownLabel;   // e.g. "Season 1 — Neon Vault"
    public GameObject   pastSeasonBanner;      // shown when viewing a past season
    public TMP_Text     pastSeasonBannerText;  // "Viewing archived Season 1"
    public Button       returnToCurrentButton; // "Back to Current Season"
    public TMP_Text     currentSeasonLabel;    // "Season 1 — Neon Vault | Ends in 3d"

    [Header("Navigation")]
    public Button       playRankedButton;
    public Button       backButton;

    // ─── Rank Tiers — delegate to RankedProgressionManager ──────────────────
    // NOTE: RankTier struct kept for leaderboard rows (local usage only).
    //       Source of truth is RankedProgressionManager.TIERS.
    [System.Serializable]
    public struct RankTier
    {
        public string name;
        public int    minTrophies;
        public int    maxTrophies;
        public Color  color;
        /// <summary>Icon key for GameIconSystem.GetSprite(iconKey). Replaces legacy emoji field.</summary>
        public string iconKey;
    }

    // Mirrors RankedProgressionManager.TIERS for backwards-compatible local use
    private static readonly RankTier[] RANKS = new RankTier[]
    {
        new RankTier { name = "Rookie",  iconKey = "tier_rookie",  minTrophies = 0,    maxTrophies = 499,   color = new Color(0.68f, 0.68f, 0.68f) },
        new RankTier { name = "Silver",  iconKey = "tier_silver",  minTrophies = 500,  maxTrophies = 999,   color = new Color(0.80f, 0.85f, 0.90f) },
        new RankTier { name = "Gold",    iconKey = "tier_gold",    minTrophies = 1000, maxTrophies = 1999,  color = new Color(0.90f, 0.75f, 0.10f) },
        new RankTier { name = "Diamond", iconKey = "tier_diamond", minTrophies = 2000, maxTrophies = 3499,  color = new Color(0.40f, 0.70f, 1.00f) },
        new RankTier { name = "Master",  iconKey = "tier_master",  minTrophies = 3500, maxTrophies = 4499,  color = new Color(0.80f, 0.40f, 1.00f) },
        new RankTier { name = "Legend",  iconKey = "tier_legend",  minTrophies = 4500, maxTrophies = 99999, color = new Color(1.00f, 0.84f, 0.00f) },
    };

    // ─── Private ──────────────────────────────────────────────────────────────
    private bool _loading = false;
    private bool _rankInfoVisible = false;
    private List<LeaderboardEntry> _entries = new List<LeaderboardEntry>();

    // Season selector
    private string _viewingSeasonId = null;     // null = current season
    private List<string> _seasonIds = new List<string>();
    private bool _viewingPastSeason = false;

    [System.Serializable]
    public struct LeaderboardEntry
    {
        public int    rank;
        public string playerName;
        public int    trophies;
        public string rankName;
        public int    prestigeLevel;   // 0 = no prestige
        public bool   isLocalPlayer;

        /// <summary>Display string: tier name (+ stars if prestige). Icon shown via Image component.</summary>
        public string DisplayRank =>
            prestigeLevel > 0
                ? $"{rankName} {RankedProgressionManager.GetPrestigeStars(prestigeLevel)}"
                : rankName;
    }

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Start()
    {
        if (backButton              != null) backButton.onClick.AddListener(OnBack);
        if (playRankedButton        != null) playRankedButton.onClick.AddListener(OnPlayRanked);
        if (refreshButton           != null) refreshButton.onClick.AddListener(() => StartCoroutine(LoadLeaderboard()));
        if (toggleRankInfoButton    != null) toggleRankInfoButton.onClick.AddListener(ToggleRankInfo);
        if (returnToCurrentButton   != null) returnToCurrentButton.onClick.AddListener(ReturnToCurrentSeason);
        if (seasonDropdown          != null) seasonDropdown.onValueChanged.AddListener(OnSeasonDropdownChanged);

        if (rankInfoPanel       != null) rankInfoPanel.SetActive(false);
        if (rankInfoText        != null) rankInfoText.text = GetRankInfoText();
        if (pastSeasonBanner    != null) pastSeasonBanner.SetActive(false);
    }

    // ─── Activation ───────────────────────────────────────────────────────────
    public void OnActivate()
    {
        // Subscribe to live prestige + season updates
        if (RankedProgressionManager.Instance != null)
        {
            RankedProgressionManager.Instance.OnProgressionChanged -= OnProgressionChanged;
            RankedProgressionManager.Instance.OnProgressionChanged += OnProgressionChanged;
        }
        if (SeasonManager.Instance != null)
        {
            SeasonManager.Instance.OnSeasonChanged -= OnSeasonChanged;
            SeasonManager.Instance.OnSeasonChanged += OnSeasonChanged;
        }

        // Reset to current season
        _viewingSeasonId    = null;
        _viewingPastSeason  = false;

        RefreshRankCard();
        RefreshCurrentSeasonLabel();
        StartCoroutine(PopulateSeasonDropdown());
        StartCoroutine(LoadLeaderboard());
    }

    void OnSeasonChanged(SeasonInfo newSeason)
    {
        RefreshCurrentSeasonLabel();
        StartCoroutine(PopulateSeasonDropdown());
    }

    void RefreshCurrentSeasonLabel()
    {
        var season = SeasonManager.Instance?.CurrentSeason;
        if (currentSeasonLabel != null && season != null)
        {
            currentSeasonLabel.text = season.TimeRemaining.TotalSeconds > 0
                ? $"Season {season.seasonNumber} — {season.name}  |  Ends in {season.TimeRemainingFormatted}"
                : $"Season {season.seasonNumber} — {season.name}  (ended)";
        }
    }

    void OnDisable()
    {
        if (RankedProgressionManager.Instance != null)
            RankedProgressionManager.Instance.OnProgressionChanged -= OnProgressionChanged;
        if (SeasonManager.Instance != null)
            SeasonManager.Instance.OnSeasonChanged -= OnSeasonChanged;
    }

    void OnProgressionChanged(RankedProgressionManager.ProgressionState _)
    {
        RefreshRankCard();
    }

    // ─── Rank Card ────────────────────────────────────────────────────────────
    void RefreshRankCard()
    {
        // Prefer live RankedProgressionManager data; fall back to PlayerPrefs
        int trophies = RankedProgressionManager.Instance?.State.trophies
                       ?? PlayerPrefs.GetInt("VaultDash_Trophies", 0);
        int prestige = RankedProgressionManager.Instance?.State.prestigeLevel
                       ?? PlayerPrefs.GetInt("VaultDash_PrestigeLevel", 0);

        RankTier tier = GetRankTier(trophies);

        // Rank name (include prestige marker)
        if (rankNameText != null)
        {
            // Tier icon shown via rankIconImage (use GameIconSystem.ApplyIcon in Inspector setup)
            rankNameText.text  = prestige > 0
                ? $"{tier.name.ToUpper()}  P{prestige}"
                : tier.name.ToUpper();
            rankNameText.color = tier.color;
        }

        // Tier icon via GameIconSystem
        if (rankIconImage != null)
            GameIconSystem.ApplyIcon(rankIconImage, tier.iconKey);

        if (trophyCountText != null) trophyCountText.text = $"{trophies:N0}";

        // Progress to next rank
        if (tier.name == "Legend")
        {
            if (rankProgressText != null)
            {
                rankProgressText.text = prestige > 0
                    ? $"LEGEND MAX  |  Prestige {prestige} Active"
                    : "MAX RANK — LEGEND  |  Prestige available!";
            }
            if (rankProgressBar != null) rankProgressBar.value = 1f;
        }
        else
        {
            int toNext    = tier.maxTrophies - trophies + 1;
            string nextRk = GetNextRank(tier);
            int tierRange = tier.maxTrophies - tier.minTrophies;
            int progress  = trophies - tier.minTrophies;

            if (rankProgressText != null)
                rankProgressText.text = $"{toNext} trophies to {nextRk}";
            if (rankProgressBar != null)
                rankProgressBar.value = tierRange > 0 ? (float)progress / tierRange : 0f;
        }

        // Bar tint
        if (rankProgressBar != null)
        {
            var fill = rankProgressBar.fillRect?.GetComponent<Image>();
            if (fill != null) fill.color = tier.color;
        }

        // Simulated leaderboard position
        if (playerPositionText != null)
        {
            int fakePosition = Mathf.Max(1, 1000 - trophies / 4);
            playerPositionText.text = $"#{fakePosition:N0} globally";
        }

        // ─── Prestige section ─────────────────────────────────────────────────
        if (rankCardPrestigeBadge != null)
        {
            rankCardPrestigeBadge.SetPrestige(prestige, trophies);
        }
        else
        {
            // Standalone fallback fields
            if (rankPrestigeSection != null)
                rankPrestigeSection.SetActive(prestige > 0);

            if (prestige > 0)
            {
                if (rankPrestigeLevelText != null)
                    rankPrestigeLevelText.text = RankedProgressionManager.GetPrestigeLabel(prestige);
                if (rankPrestigeStarsText != null)
                    rankPrestigeStarsText.text = RankedProgressionManager.GetPrestigeStars(prestige);
            }
        }
    }

    // ─── Leaderboard ─────────────────────────────────────────────────────────
    IEnumerator LoadLeaderboard()
    {
        if (_loading) yield break;
        _loading = true;

        if (loadingText != null) loadingText.gameObject.SetActive(true);
        if (refreshButton != null) refreshButton.interactable = false;

        // Clear existing rows
        if (leaderboardContainer != null)
            foreach (Transform child in leaderboardContainer)
                Destroy(child.gameObject);

        _entries.Clear();

        if (_viewingPastSeason && !string.IsNullOrEmpty(_viewingSeasonId) &&
            SeasonManager.Instance != null)
        {
            // Load archived season leaderboard
            yield return StartCoroutine(LoadPastSeasonLeaderboard(_viewingSeasonId));
        }
        else
        {
#if NAKAMA_AVAILABLE
            yield return StartCoroutine(FetchNakamaLeaderboard());
#else
            yield return StartCoroutine(GenerateSimulatedLeaderboard());
#endif
        }

        BuildLeaderboardRows();

        if (loadingText != null) loadingText.gameObject.SetActive(false);
        if (refreshButton != null) refreshButton.interactable = true;
        _loading = false;
    }

    IEnumerator GenerateSimulatedLeaderboard()
    {
        yield return new WaitForSecondsRealtime(0.5f);

        string[] fakeNames = {
            "VaultKing","ShadowRunner","NeonDash","TrophyHunter","ZeroGravity",
            "BlazeFast","KnoxBeast","JadeViper","VectorAce","CipherGhost",
            "NovaFlare","RyzeArc","TitanSmash","EchoMirror","GoldSprint",
            "DiamondDash","LegendaryRun","VaultElite","RankedGod","TopRunner"
        };

        int localTrophies  = RankedProgressionManager.Instance?.State.trophies
                             ?? PlayerPrefs.GetInt("VaultDash_Trophies", 0);
        int localPrestige  = RankedProgressionManager.Instance?.State.prestigeLevel
                             ?? PlayerPrefs.GetInt("VaultDash_PrestigeLevel", 0);
        string localName   = PlayerPrefs.GetString("VaultDash_PlayerName", "You");
        bool placed        = false;

        for (int i = 1; i <= displayCount; i++)
        {
            // Top players: high trophies + possible prestige
            int fakeTrophies  = Mathf.Max(0, 6000 - (i * 55) + UnityEngine.Random.Range(-30, 30));
            // Top 20 players might have prestige
            int fakePrestige  = (i <= 20 && fakeTrophies >= RankedProgressionManager.PRESTIGE_THRESHOLD)
                ? UnityEngine.Random.Range(1, Mathf.Max(2, 6 - i / 4))
                : 0;

            if (!placed && fakeTrophies <= localTrophies)
            {
                _entries.Add(new LeaderboardEntry
                {
                    rank          = _entries.Count + 1,
                    playerName    = localName,
                    trophies      = localTrophies,
                    rankName      = GetRankTier(localTrophies).name,
                    prestigeLevel = localPrestige,
                    isLocalPlayer = true
                });
                placed = true;
            }

            _entries.Add(new LeaderboardEntry
            {
                rank          = _entries.Count + 1,
                playerName    = fakeNames[UnityEngine.Random.Range(0, fakeNames.Length)] + UnityEngine.Random.Range(10, 9999),
                trophies      = fakeTrophies,
                rankName      = GetRankTier(fakeTrophies).name,
                prestigeLevel = fakePrestige,
                isLocalPlayer = false
            });

            if (_entries.Count >= displayCount + 1) break;
        }

        if (!placed)
        {
            _entries.Add(new LeaderboardEntry
            {
                rank          = _entries.Count + 1,
                playerName    = localName,
                trophies      = localTrophies,
                rankName      = GetRankTier(localTrophies).name,
                prestigeLevel = localPrestige,
                isLocalPlayer = true
            });
        }
    }

#if NAKAMA_AVAILABLE
    IEnumerator FetchNakamaLeaderboard()
    {
        // Real Nakama leaderboard fetch
        // var task = MatchManager.Instance._client.ListLeaderboardRecordsAsync(
        //     session, "trophies", null, displayCount);
        // yield return new WaitUntil(() => task.IsCompleted);
        // ... parse + populate _entries
        yield return GenerateSimulatedLeaderboard(); // fallback for now
    }
#endif

    void BuildLeaderboardRows()
    {
        if (leaderboardContainer == null) return;

        foreach (var entry in _entries)
        {
            var localRankTier = GetRankTier(entry.trophies);

            if (leaderboardRowPrefab != null)
            {
                GameObject row = Instantiate(leaderboardRowPrefab, leaderboardContainer);
                var rankLabel   = row.transform.Find("Rank")?.GetComponent<TMP_Text>();
                var nameLabel   = row.transform.Find("Name")?.GetComponent<TMP_Text>();
                var trophyLabel = row.transform.Find("Trophies")?.GetComponent<TMP_Text>();
                var rankBadge   = row.transform.Find("RankBadge")?.GetComponent<TMP_Text>();
                var starsLabel  = row.transform.Find("Stars")?.GetComponent<TMP_Text>();  // optional
                var rowImg      = row.GetComponent<Image>();

                if (rankLabel   != null) rankLabel.text   = $"#{entry.rank}";
                if (nameLabel   != null)
                {
                    // Star prefix (ASCII) for prestige players
                    nameLabel.text = entry.prestigeLevel > 0
                        ? $"★ {entry.playerName}"
                        : entry.playerName;
                }
                if (trophyLabel != null) trophyLabel.text = $"{entry.trophies:N0}";
                if (rankBadge   != null)
                {
                    rankBadge.text  = entry.DisplayRank;
                    rankBadge.color = localRankTier.color;
                }
                if (starsLabel != null)
                {
                    starsLabel.text = RankedProgressionManager.GetPrestigeStars(entry.prestigeLevel);
                    starsLabel.gameObject.SetActive(entry.prestigeLevel > 0);
                }

                // Highlight local player row
                if (rowImg != null && entry.isLocalPlayer)
                    rowImg.color = new Color(0.9f, 0.75f, 0.1f, 0.25f);

                // Top 3: use icon key to load medal image (apply to a dedicated Image child)
                if (entry.rank <= 3)
                {
                    string medalKey = entry.rank == 1 ? "tier_gold" : entry.rank == 2 ? "tier_silver" : "tier_rookie";
                    var medalImage = row.transform.Find("MedalIcon")?.GetComponent<Image>();
                    if (medalImage != null)
                        GameIconSystem.ApplyIcon(medalImage, medalKey);
                    // Keep rank label as number if no dedicated medal image slot
                    if (rankLabel != null && row.transform.Find("MedalIcon") == null)
                        rankLabel.text = $"#{entry.rank}";
                }
            }
            else
            {
                // Fallback: text row
                string stars = RankedProgressionManager.GetPrestigeStars(entry.prestigeLevel);
                string prestigeTag = entry.prestigeLevel > 0 ? $" [{stars}P{entry.prestigeLevel}]" : "";
                GameObject go = new GameObject($"Row_{entry.rank}");
                go.transform.SetParent(leaderboardContainer, false);
                var text = go.AddComponent<TMP_Text>();
                text.text = $"#{entry.rank,-4}  {entry.playerName,-20}  {entry.trophies,6} trophies  {entry.rankName}{prestigeTag}";
                text.fontSize = 13;
                text.color = entry.isLocalPlayer
                    ? new Color(0.9f, 0.75f, 0.1f)
                    : (entry.prestigeLevel > 0 ? new Color(0.8f, 0.5f, 1.0f) : Color.white);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(700f, 35f);
            }
        }

        // Scroll to local player position
        if (leaderboardScrollRect != null)
        {
            int localIdx = _entries.FindIndex(e => e.isLocalPlayer);
            if (localIdx >= 0 && _entries.Count > 1)
            {
                float t = 1f - ((float)localIdx / (_entries.Count - 1));
                leaderboardScrollRect.verticalNormalizedPosition = t;
            }
        }
    }

    // ─── Season Dropdown ──────────────────────────────────────────────────────

    IEnumerator PopulateSeasonDropdown()
    {
        if (seasonDropdown == null) yield break;

        _seasonIds.Clear();
        var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();

        // Option 0: Current season
        var currentSeason = SeasonManager.Instance?.CurrentSeason;
        string currentLabel = currentSeason != null
            ? $"Season {currentSeason.seasonNumber} — {currentSeason.name} (Current)"
            : "Current Season";
        options.Add(new TMP_Dropdown.OptionData(currentLabel));
        _seasonIds.Add(null);   // null = current

        // Load past seasons from Firestore/stub
        var pastSeasons = new System.Collections.Generic.List<(string id, int num, string name)>();
        yield return StartCoroutine(FetchPastSeasons(list => pastSeasons = list));

        foreach (var s in pastSeasons)
        {
            options.Add(new TMP_Dropdown.OptionData($"Season {s.num} — {s.name}"));
            _seasonIds.Add(s.id);
        }

        seasonDropdown.ClearOptions();
        seasonDropdown.AddOptions(options);
        seasonDropdown.SetValueWithoutNotify(0);
    }

    IEnumerator FetchPastSeasons(
        System.Action<System.Collections.Generic.List<(string, int, string)>> callback)
    {
        var result = new System.Collections.Generic.List<(string, int, string)>();

#if FIREBASE_FIRESTORE
        // Call getSeasonList HTTPS callable
        // var func = FirebaseFunctions.DefaultInstance.GetHttpsCallable("getSeasonList");
        // ... parse response
        yield return null;
        result.Add(("season_0", 0, "Founder's Vault"));
#else
        yield return null;
        // Stub: add fictional past seasons for testing
        if (SeasonManager.Instance?.CurrentSeason?.seasonNumber > 1)
        {
            for (int i = (SeasonManager.Instance?.CurrentSeason?.seasonNumber ?? 1) - 1; i >= 0; i--)
                result.Add(($"season_{i}", i, $"Past Season {i}"));
        }
#endif
        callback(result);
    }

    void OnSeasonDropdownChanged(int index)
    {
        if (index < 0 || index >= _seasonIds.Count) return;
        string selectedId = _seasonIds[index];
        _viewingSeasonId   = selectedId;
        _viewingPastSeason = selectedId != null;

        // Update past season banner visibility
        if (pastSeasonBanner != null)
            pastSeasonBanner.SetActive(_viewingPastSeason);

        if (pastSeasonBannerText != null && _viewingPastSeason)
        {
            var option = seasonDropdown.options[index];
            pastSeasonBannerText.text = $"Archived — {option.text}";
        }

        // Ranked play button only for current season
        if (playRankedButton != null)
            playRankedButton.interactable = !_viewingPastSeason;

        StartCoroutine(LoadLeaderboard());
    }

    void ReturnToCurrentSeason()
    {
        _viewingSeasonId   = null;
        _viewingPastSeason = false;
        if (seasonDropdown != null) seasonDropdown.SetValueWithoutNotify(0);
        if (pastSeasonBanner != null) pastSeasonBanner.SetActive(false);
        if (playRankedButton != null) playRankedButton.interactable = true;
        StartCoroutine(LoadLeaderboard());
    }

    // ─── Leaderboard — past season support ───────────────────────────────────

    IEnumerator LoadPastSeasonLeaderboard(string seasonId)
    {
        _entries.Clear();
        string uid = PlayerPrefs.GetString("VaultDash_UID", "");

        var seasonEntries = new System.Collections.Generic.List<SeasonLeaderboardEntry>();
        yield return StartCoroutine(
            SeasonManager.Instance.LoadSeasonLeaderboard(seasonId, 100,
                list => seasonEntries = list));

        foreach (var e in seasonEntries)
        {
            var rankTier = GetRankTier(e.trophies);
            _entries.Add(new LeaderboardEntry
            {
                rank          = e.rank,
                playerName    = e.username,
                trophies      = e.trophies,
                rankName      = rankTier.name,
                prestigeLevel = e.prestigeLevel,
                isLocalPlayer = e.isLocalPlayer || e.uid == uid,
            });
        }

        if (_entries.Count == 0)
        {
            // Fallback to stub if archive is empty
            yield return GenerateSimulatedLeaderboard();
        }
    }

    // ─── Rank Info ────────────────────────────────────────────────────────────
    void ToggleRankInfo()
    {
        _rankInfoVisible = !_rankInfoVisible;
        if (rankInfoPanel != null) rankInfoPanel.SetActive(_rankInfoVisible);
    }

    string GetRankInfoText()
    {
        return
            "RANKED MODE — HOW IT WORKS\n\n" +
            "Win → +10 to +35 trophies (based on opponent rank)\n" +
            "Lose → -5 to -15 trophies\n\n" +
            "TIERS:\n" +
            "Rookie         0 – 499 trophies\n" +
            "Silver       500 – 999 trophies\n" +
            "Gold      1,000 – 1,999 trophies\n" +
            "Diamond   2,000 – 3,499 trophies\n" +
            "Master    3,500 – 4,499 trophies\n" +
            "Legend      4,500+ trophies\n\n" +
            "PRESTIGE SYSTEM\n" +
            "Reach Legend (4,500+) and prestige!\n" +
            "• Reset to Rookie with a Prestige badge\n" +
            "• Earn stars (★) for each prestige level\n" +
            "• Purple glow on your character in 1v1\n" +
            "• Prestige is permanent — reset as many times as you want!\n\n" +
            "Season resets at the start of each season.\n" +
            "Top 100 players earn exclusive season rewards!";
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    RankTier GetRankTier(int trophies)
    {
        for (int i = RANKS.Length - 1; i >= 0; i--)
            if (trophies >= RANKS[i].minTrophies) return RANKS[i];
        return RANKS[0];
    }

    string GetNextRank(RankTier current)
    {
        for (int i = 0; i < RANKS.Length - 1; i++)
            if (RANKS[i].name == current.name) return RANKS[i + 1].name;
        return current.name == "Legend" ? "PRESTIGE" : "MAX";
    }

    // ─── Navigation ───────────────────────────────────────────────────────────
    void OnPlayRanked()
    {
        UIManager.Instance?.ShowCharacterSelection();
    }

    void OnBack() => UIManager.Instance?.GoBack();
}
