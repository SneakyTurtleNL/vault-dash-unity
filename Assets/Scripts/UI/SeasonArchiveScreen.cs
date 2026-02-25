using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SeasonArchiveScreen — "Season Archive" shop.
///
/// Shows all past season exclusive skins, organized by season.
/// Current season skin is highlighted at the top.
/// Past season skins are available for purchase (archiveCost = gemCost × 1.5).
/// Players who earned the skin (prestige 5 during that season) see "OWNED" badge.
///
/// Layout:
///   • Current Season section — skin card + buy/earned badge
///   • Past Seasons section  — grid of archive skin cards
///
/// Wire all references in Inspector.
/// </summary>
public class SeasonArchiveScreen : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Header")]
    public TMP_Text  headerText;             // "Season Archive"
    public TMP_Text  gemBalanceText;         // "340" (gem icon via Image)

    [Header("Current Season")]
    public GameObject currentSeasonSection;
    public TMP_Text   currentSeasonLabel;    // "Season 1 — Neon Vault"
    public TMP_Text   currentSkinName;       // "Neon Vault Operative"
    public TMP_Text   currentSkinDesc;       // description
    public TMP_Text   currentSkinCost;       // "500 gems" or "FREE at Prestige 5"
    public Image      currentSkinPreview;
    public Button     buyCurrentButton;
    public TMP_Text   buyCurrentButtonText;  // "Buy 500 gems" / "Earned" / "Owned"
    public Image      currentSeasonAccent;   // season theme color bar

    [Header("Archive Grid")]
    public Transform  archiveContainer;      // ScrollRect content
    public GameObject archiveCardPrefab;     // instantiated per past season

    [Header("Loading")]
    public GameObject loadingIndicator;
    public TMP_Text   emptyText;             // "No past seasons yet"

    [Header("Navigation")]
    public Button     backButton;

    // ─── State ────────────────────────────────────────────────────────────────
    private List<SeasonArchiveEntry> _pastSeasons = new List<SeasonArchiveEntry>();
    private bool                     _loading     = false;
    private int                      _playerGems  = 0;

    [Serializable]
    struct SeasonArchiveEntry
    {
        public string   seasonId;
        public int      seasonNumber;
        public string   seasonName;
        public SeasonCosmetic cosmetic;
        public bool     owned;
        public bool     isCurrent;
    }

    // ─── Init ─────────────────────────────────────────────────────────────────

    void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBack);
        if (buyCurrentButton != null) buyCurrentButton.onClick.AddListener(OnBuyCurrent);
    }

    // ─── Activation ───────────────────────────────────────────────────────────

    public void OnActivate()
    {
        _playerGems = PlayerPrefs.GetInt("VaultDash_Gems", 0);
        if (gemBalanceText != null) gemBalanceText.text = $"{_playerGems:N0}";

        StartCoroutine(LoadSeasons());
    }

    // ─── Load ─────────────────────────────────────────────────────────────────

    IEnumerator LoadSeasons()
    {
        if (_loading) yield break;
        _loading = true;

        if (loadingIndicator != null) loadingIndicator.SetActive(true);
        if (emptyText        != null) emptyText.gameObject.SetActive(false);

        _pastSeasons.Clear();

        // Get list of past seasons
        yield return StartCoroutine(FetchSeasonList());

        BuildCurrentSeasonUI();
        BuildArchiveGrid();

        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        _loading = false;
    }

    IEnumerator FetchSeasonList()
    {
#if FIREBASE_FIRESTORE
        // Fetch via getSeasonList callable
        // var func = FirebaseFunctions.DefaultInstance.GetHttpsCallable("getSeasonList");
        // var task = func.CallAsync();
        // yield return new WaitUntil(() => task.IsCompleted);
        // Parse task.Result…
        yield return GenerateStubSeasons();
#else
        yield return GenerateStubSeasons();
#endif
    }

    IEnumerator GenerateStubSeasons()
    {
        yield return new WaitForSecondsRealtime(0.3f);

        var currentSeason = SeasonManager.Instance?.CurrentSeason;

        // Add current season
        if (currentSeason != null)
        {
            _pastSeasons.Add(new SeasonArchiveEntry
            {
                seasonId     = currentSeason.seasonId,
                seasonNumber = currentSeason.seasonNumber,
                seasonName   = currentSeason.name,
                cosmetic     = currentSeason.cosmetic,
                isCurrent    = true,
                owned        = SeasonManager.Instance?.PlayerEarnedSeasonSkin() ?? false,
            });
        }

        // Stub past seasons
        _pastSeasons.Add(new SeasonArchiveEntry
        {
            seasonId     = "season_0",
            seasonNumber = 0,
            seasonName   = "Founder's Vault",
            cosmetic     = new SeasonCosmetic
            {
                skinId         = "founders_skin",
                skinName       = "Vault Founder",
                description    = "Exclusive Season 0 skin for early players.",
                gemCost        = 500,
                archiveGemCost = 750,
                themeColor     = "#FFD700",
                prestigeFree   = 5,
            },
            isCurrent = false,
            owned     = false,
        });
    }

    // ─── Build UI ─────────────────────────────────────────────────────────────

    void BuildCurrentSeasonUI()
    {
        var currentEntry = _pastSeasons.Find(e => e.isCurrent);

        if (currentSeasonSection != null)
            currentSeasonSection.SetActive(currentEntry.seasonId != null);

        if (currentEntry.seasonId == null) return;

        var cosmetic = currentEntry.cosmetic;

        if (currentSeasonLabel  != null)
            currentSeasonLabel.text  = $"Season {currentEntry.seasonNumber} — {currentEntry.seasonName}";
        if (currentSkinName     != null)
            currentSkinName.text     = cosmetic?.skinName  ?? "Season Skin";
        if (currentSkinDesc     != null)
            currentSkinDesc.text     = cosmetic?.description ?? "";

        // Cost / earned logic
        bool owned   = currentEntry.owned;
        int  cost    = cosmetic?.gemCost ?? 500;
        int  prestige= cosmetic?.prestigeFree ?? 5;

        if (currentSkinCost != null)
            currentSkinCost.text = owned
                ? $"Earned at Prestige {prestige}"
                : $"Buy for {cost} gems";

        if (buyCurrentButton != null)
        {
            buyCurrentButton.interactable = !owned;
            if (buyCurrentButtonText != null)
                buyCurrentButtonText.text = owned ? "Owned" : $"Buy {cost} gems";
        }

        // Theme color
        if (cosmetic != null && currentSeasonAccent != null)
            currentSeasonAccent.color = cosmetic.ThemeColorUnity;
    }

    void BuildArchiveGrid()
    {
        if (archiveContainer == null) return;

        // Clear existing
        foreach (Transform child in archiveContainer)
            Destroy(child.gameObject);

        var pastOnly = _pastSeasons.FindAll(e => !e.isCurrent);

        if (emptyText != null)
            emptyText.gameObject.SetActive(pastOnly.Count == 0);

        foreach (var entry in pastOnly)
            BuildArchiveCard(entry);
    }

    void BuildArchiveCard(SeasonArchiveEntry entry)
    {
        if (archiveCardPrefab == null || archiveContainer == null) return;

        GameObject card = Instantiate(archiveCardPrefab, archiveContainer);
        var cosmetic = entry.cosmetic;

        // Populate standard TMP fields by child name convention
        SetChildText(card, "SeasonLabel",   $"Season {entry.seasonNumber}");
        SetChildText(card, "SeasonName",    entry.seasonName);
        SetChildText(card, "SkinName",      cosmetic?.skinName   ?? "Archive Skin");
        SetChildText(card, "SkinDesc",      cosmetic?.description ?? "");

        int archiveCost = cosmetic?.ArchiveCostDerived ?? 750;
        SetChildText(card, "CostText",      entry.owned ? "Owned" : $"{archiveCost} gems");

        // Buy button
        var btn = card.GetComponentInChildren<Button>();
        if (btn != null)
        {
            btn.interactable = !entry.owned && _playerGems >= archiveCost;
            var capturedEntry = entry;
            btn.onClick.AddListener(() => OnBuyArchiveSkin(capturedEntry, archiveCost));
        }

        // Theme accent
        var accent = card.transform.Find("ThemeAccent")?.GetComponent<Image>();
        if (accent != null && cosmetic != null)
            accent.color = cosmetic.ThemeColorUnity;
    }

    // ─── Buy Logic ────────────────────────────────────────────────────────────

    void OnBuyCurrent()
    {
        var currentEntry = _pastSeasons.Find(e => e.isCurrent);
        if (currentEntry.seasonId == null) return;
        var cosmetic = currentEntry.cosmetic;
        if (cosmetic == null) return;

        int cost = cosmetic.gemCost;
        if (_playerGems < cost)
        {
            Debug.Log("[SeasonArchive] Not enough gems.");
            return;
        }

        StartCoroutine(PurchaseSkin(currentEntry.skinId, cost, false));
    }

    void OnBuyArchiveSkin(SeasonArchiveEntry entry, int cost)
    {
        if (_playerGems < cost) return;
        StartCoroutine(PurchaseSkin(entry.skinId, cost, true));
    }

    IEnumerator PurchaseSkin(string skinId, int cost, bool isArchive)
    {
#if FIREBASE_FIRESTORE
        // Deduct gems + add skin via Firestore transaction
        yield return null;
        Debug.Log($"[SeasonArchive] Purchasing skin {skinId} for {cost} gems (archive={isArchive})");
#else
        // Stub
        yield return new WaitForSecondsRealtime(0.3f);
        _playerGems -= cost;
        PlayerPrefs.SetInt("VaultDash_Gems", _playerGems);
        if (gemBalanceText != null) gemBalanceText.text = $"{_playerGems:N0}";
        Debug.Log($"[SeasonArchive] (stub) Purchased skin {skinId}");

        // Reload UI
        yield return LoadSeasons();
#endif
    }

    string GetSkinId(SeasonArchiveEntry entry) => entry.cosmetic?.skinId ?? "";
    string SkinId(SeasonArchiveEntry e) => e.cosmetic?.skinId ?? e.seasonId;

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static void SetChildText(GameObject root, string childName, string text)
    {
        var t = root.transform.Find(childName)?.GetComponent<TMP_Text>();
        if (t != null) t.text = text;
    }

    // ─── Navigation ───────────────────────────────────────────────────────────
    void OnBack() => UIManager.Instance?.GoBack();
}

// Extension to get skinId from entry (helper since struct doesn't allow properties on fields)
internal static class SeasonArchiveExtensions
{
    internal static string GetSkinId(this SeasonCosmetic c) => c?.skinId ?? "";
}
