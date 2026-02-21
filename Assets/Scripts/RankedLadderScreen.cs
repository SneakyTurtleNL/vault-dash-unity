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
    public TMP_Text     trophyCountText;        // "🏆 1,240"
    public TMP_Text     rankProgressText;       // "240 / 500 to Diamond"
    public Slider       rankProgressBar;
    public Image        rankIconImage;
    public TMP_Text     playerPositionText;     // "#47 globally"
    public Color[]      rankColors = new Color[]
    {
        new Color(0.7f, 0.7f, 0.7f),  // Rookie (grey)
        new Color(0.8f, 0.85f, 0.9f), // Silver (silver)
        new Color(0.9f, 0.75f, 0.1f), // Gold   (gold)
        new Color(0.4f, 0.7f, 1.0f),  // Diamond (blue)
        new Color(0.85f, 0.5f, 1.0f), // Legend (purple)
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

    [Header("Navigation")]
    public Button       playRankedButton;
    public Button       backButton;

    // ─── Rank Tiers ──────────────────────────────────────────────────────────
    [System.Serializable]
    public struct RankTier
    {
        public string name;
        public int    minTrophies;
        public int    maxTrophies;
        public Color  color;
    }

    private static readonly RankTier[] RANKS = new RankTier[]
    {
        new RankTier { name = "Rookie",  minTrophies = 0,    maxTrophies = 499  },
        new RankTier { name = "Silver",  minTrophies = 500,  maxTrophies = 999  },
        new RankTier { name = "Gold",    minTrophies = 1000, maxTrophies = 1999 },
        new RankTier { name = "Diamond", minTrophies = 2000, maxTrophies = 3499 },
        new RankTier { name = "Legend",  minTrophies = 3500, maxTrophies = 99999},
    };

    // ─── Private ──────────────────────────────────────────────────────────────
    private bool _loading = false;
    private bool _rankInfoVisible = false;
    private List<LeaderboardEntry> _entries = new List<LeaderboardEntry>();

    [System.Serializable]
    public struct LeaderboardEntry
    {
        public int    rank;
        public string playerName;
        public int    trophies;
        public string rankName;
        public bool   isLocalPlayer;
    }

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Start()
    {
        if (backButton          != null) backButton.onClick.AddListener(OnBack);
        if (playRankedButton    != null) playRankedButton.onClick.AddListener(OnPlayRanked);
        if (refreshButton       != null) refreshButton.onClick.AddListener(() => StartCoroutine(LoadLeaderboard()));
        if (toggleRankInfoButton!= null) toggleRankInfoButton.onClick.AddListener(ToggleRankInfo);

        if (rankInfoPanel       != null) rankInfoPanel.SetActive(false);
        if (rankInfoText        != null) rankInfoText.text = GetRankInfoText();
    }

    // ─── Activation ───────────────────────────────────────────────────────────
    public void OnActivate()
    {
        RefreshRankCard();
        StartCoroutine(LoadLeaderboard());
    }

    // ─── Rank Card ────────────────────────────────────────────────────────────
    void RefreshRankCard()
    {
        int trophies = PlayerPrefs.GetInt("VaultDash_Trophies", 0);
        RankTier tier = GetRankTier(trophies);

        if (rankNameText    != null) rankNameText.text    = tier.name.ToUpper();
        if (trophyCountText != null) trophyCountText.text = $"🏆 {trophies:N0}";

        // Progress to next rank
        int toNext = tier.maxTrophies - trophies;
        if (tier.name == "Legend")
        {
            if (rankProgressText != null) rankProgressText.text = "MAX RANK — LEGEND 🌟";
            if (rankProgressBar  != null) rankProgressBar.value = 1f;
        }
        else
        {
            string nextRank = GetNextRank(tier);
            int tierRange   = tier.maxTrophies - tier.minTrophies;
            int progress    = trophies - tier.minTrophies;

            if (rankProgressText != null)
                rankProgressText.text = $"{toNext} 🏆 to {nextRank}";
            if (rankProgressBar != null)
                rankProgressBar.value = tierRange > 0 ? (float)progress / tierRange : 0f;
        }

        // Color rank bar
        if (rankProgressBar != null)
        {
            var fill = rankProgressBar.fillRect?.GetComponent<Image>();
            if (fill != null) fill.color = tier.color;
        }

        // Rank icon tint
        if (rankIconImage != null) rankIconImage.color = tier.color;

        // Simulated position
        if (playerPositionText != null)
        {
            int fakePosition = Mathf.Max(1, 1000 - trophies / 4);
            playerPositionText.text = $"#{fakePosition:N0} globally";
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

#if NAKAMA_AVAILABLE
        yield return StartCoroutine(FetchNakamaLeaderboard());
#else
        yield return StartCoroutine(GenerateSimulatedLeaderboard());
#endif

        BuildLeaderboardRows();

        if (loadingText != null) loadingText.gameObject.SetActive(false);
        if (refreshButton != null) refreshButton.interactable = true;
        _loading = false;
    }

    IEnumerator GenerateSimulatedLeaderboard()
    {
        // Simulate network delay
        yield return new WaitForSecondsRealtime(0.5f);

        string[] fakeNames = {
            "VaultKing","ShadowRunner","NeonDash","TrophyHunter","ZeroGravity",
            "BlazeFast","KnoxBeast","JadeViper","VectorAce","CipherGhost",
            "NovaFlare","RyzeArc","TitanSmash","EchoMirror","GoldSprint",
            "DiamondDash","LegendaryRun","VaultElite","RankedGod","TopRunner"
        };

        int localTrophies = PlayerPrefs.GetInt("VaultDash_Trophies", 0);
        string localName  = PlayerPrefs.GetString("VaultDash_PlayerName", "You");
        bool placed       = false;

        for (int i = 1; i <= displayCount; i++)
        {
            // Simulate trophy distribution: top players have lots of trophies
            int fakeTrophies = Mathf.Max(0, 5000 - (i * 45) + UnityEngine.Random.Range(-20, 20));

            // Insert local player at correct position
            if (!placed && fakeTrophies <= localTrophies)
            {
                _entries.Add(new LeaderboardEntry
                {
                    rank        = _entries.Count + 1,
                    playerName  = localName,
                    trophies    = localTrophies,
                    rankName    = GetRankTier(localTrophies).name,
                    isLocalPlayer = true
                });
                placed = true;
            }

            _entries.Add(new LeaderboardEntry
            {
                rank       = _entries.Count + 1,
                playerName = fakeNames[UnityEngine.Random.Range(0, fakeNames.Length)] + UnityEngine.Random.Range(10, 9999),
                trophies   = fakeTrophies,
                rankName   = GetRankTier(fakeTrophies).name,
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
            if (leaderboardRowPrefab != null)
            {
                GameObject row = Instantiate(leaderboardRowPrefab, leaderboardContainer);
                var rankLabel   = row.transform.Find("Rank")?.GetComponent<TMP_Text>();
                var nameLabel   = row.transform.Find("Name")?.GetComponent<TMP_Text>();
                var trophyLabel = row.transform.Find("Trophies")?.GetComponent<TMP_Text>();
                var rankBadge   = row.transform.Find("RankBadge")?.GetComponent<TMP_Text>();
                var rowImg      = row.GetComponent<Image>();

                if (rankLabel   != null) rankLabel.text   = $"#{entry.rank}";
                if (nameLabel   != null) nameLabel.text   = entry.playerName;
                if (trophyLabel != null) trophyLabel.text = $"🏆 {entry.trophies:N0}";
                if (rankBadge   != null) rankBadge.text   = entry.rankName;

                // Highlight local player row
                if (rowImg != null && entry.isLocalPlayer)
                    rowImg.color = new Color(0.9f, 0.75f, 0.1f, 0.25f);

                // Top 3 gold/silver/bronze highlights
                if (entry.rank <= 3 && rankLabel != null)
                {
                    string medal = entry.rank == 1 ? "🥇" : entry.rank == 2 ? "🥈" : "🥉";
                    rankLabel.text = medal;
                }
            }
            else
            {
                // Fallback: text row
                GameObject go = new GameObject($"Row_{entry.rank}");
                go.transform.SetParent(leaderboardContainer, false);
                var text = go.AddComponent<TMP_Text>();
                text.text = $"#{entry.rank,-4}  {entry.playerName,-20}  🏆{entry.trophies,6}  {entry.rankName}";
                text.fontSize = 13;
                text.color = entry.isLocalPlayer
                    ? new Color(0.9f, 0.75f, 0.1f)
                    : Color.white;
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

    // ─── Rank Info ────────────────────────────────────────────────────────────
    void ToggleRankInfo()
    {
        _rankInfoVisible = !_rankInfoVisible;
        if (rankInfoPanel != null) rankInfoPanel.SetActive(_rankInfoVisible);
    }

    string GetRankInfoText()
    {
        return
            "🏆 RANKED MODE — HOW IT WORKS\n\n" +
            "Win → +10 to +35 trophies (based on opponent rank)\n" +
            "Lose → -5 to -15 trophies\n\n" +
            "RANKS:\n" +
            "🥉 Rookie       0–499 trophies\n" +
            "🥈 Silver     500–999 trophies\n" +
            "🥇 Gold    1,000–1,999 trophies\n" +
            "💎 Diamond 2,000–3,499 trophies\n" +
            "🌟 Legend    3,500+ trophies\n\n" +
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
        return "MAX";
    }

    // ─── Navigation ───────────────────────────────────────────────────────────
    void OnPlayRanked()
    {
        UIManager.Instance?.ShowCharacterSelection();
    }

    void OnBack() => UIManager.Instance?.GoBack();
}
