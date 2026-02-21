using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CardDeckScreen — Character card collection grid (4×4 = up to 10 characters).
///
/// Layout:
///   • Header: "My Characters" + coin balance
///   • ScrollView: 4-column grid of CardUI prefabs
///   • Tapping a card opens CardDetailModal
///
/// Wire in Inspector:
///   cardContainer   → ScrollView Content (GridLayoutGroup, 4 cols)
///   cardPrefab      → CardUI prefab
///   coinBalanceText → coin display
///   detailModal     → CardDetailModal reference
///   backButton      → back nav
/// </summary>
public class CardDeckScreen : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Grid")]
    public Transform  cardContainer;   // GridLayoutGroup content
    public GameObject cardPrefab;      // CardUI prefab

    [Header("Header")]
    public TMP_Text   coinBalanceText;
    public TMP_Text   headerLabel;

    [Header("Modals")]
    public CardDetailModal detailModal;

    [Header("Navigation")]
    public Button     backButton;
    public Button     goToSkillsButton;

    // ─── Private ──────────────────────────────────────────────────────────────
    private List<CardUI> _cards = new List<CardUI>();
    private bool _initialized   = false;

    // ─── Lifecycle ────────────────────────────────────────────────────────────
    void Start()
    {
        if (backButton      != null) backButton.onClick.AddListener(OnBack);
        if (goToSkillsButton!= null) goToSkillsButton.onClick.AddListener(OnGoToSkills);
    }

    void OnEnable()
    {
        if (CardManager.Instance != null)
        {
            CardManager.Instance.OnCardsLoaded      += OnCardsReady;
            CardManager.Instance.OnCharacterUpgraded += OnCharacterUpgraded;
        }

        if (!_initialized && CardManager.Instance != null && CardManager.Instance.IsLoaded)
            BuildGrid();
        else if (!_initialized && CardManager.Instance == null)
            BuildGrid(); // editor fallback

        RefreshCoinDisplay();
    }

    void OnDisable()
    {
        if (CardManager.Instance != null)
        {
            CardManager.Instance.OnCardsLoaded       -= OnCardsReady;
            CardManager.Instance.OnCharacterUpgraded -= OnCharacterUpgraded;
        }
    }

    // ─── Build ────────────────────────────────────────────────────────────────
    void OnCardsReady() => BuildGrid();

    void BuildGrid()
    {
        if (cardPrefab == null || cardContainer == null) return;

        // Clear old cards
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);
        _cards.Clear();

        foreach (var def in CardDefinitions.Characters)
        {
            var go   = Instantiate(cardPrefab, cardContainer);
            var ui   = go.GetComponent<CardUI>() ?? go.AddComponent<CardUI>();
            string id = def.id;   // capture

            // Get or create card data
            CharacterCardData data = GetOrDefaultCharacter(id);
            ui.SetupCharacter(data, def);

            ui.OnClick        = () => OpenDetail(id);
            ui.OnUpgradeClick = () => OpenDetail(id, autoUpgrade: true);

            _cards.Add(ui);
        }

        if (headerLabel != null)
            headerLabel.text = $"Characters  ({CardDefinitions.Characters.Length})";

        _initialized = true;
        Debug.Log("[CardDeckScreen] Grid built.");
    }

    CharacterCardData GetOrDefaultCharacter(string id)
    {
        if (CardManager.Instance != null && CardManager.Instance.CharacterCards.TryGetValue(id, out var data))
            return data;
        return new CharacterCardData { characterId = id };
    }

    // ─── Detail ───────────────────────────────────────────────────────────────
    void OpenDetail(string characterId, bool autoUpgrade = false)
    {
        if (detailModal == null)
        {
            Debug.LogWarning("[CardDeckScreen] No CardDetailModal assigned.");
            return;
        }
        detailModal.ShowCharacter(characterId, autoUpgrade);
    }

    // ─── Refresh ──────────────────────────────────────────────────────────────
    void OnCharacterUpgraded(string id) => RefreshCard(id);

    void RefreshCard(string characterId)
    {
        CharacterCardData data = GetOrDefaultCharacter(characterId);
        foreach (var ui in _cards)
        {
            if (ui == null) continue;
            // Check card by name label (simple match)
            var def = CardDefinitions.FindCharacter(characterId);
            if (def.HasValue)
            {
                ui.Refresh();
                break;
            }
        }
        RefreshCoinDisplay();
    }

    void RefreshCoinDisplay()
    {
        if (coinBalanceText == null) return;
        int coins = CardManager.Instance != null
            ? CardManager.Instance.GetPlayerCoins()
            : PlayerPrefs.GetInt("VaultDash_Coins", 0);
        coinBalanceText.text = $"🪙 {coins:N0}";
    }

    // ─── Navigation ───────────────────────────────────────────────────────────
    void OnBack()       => UIManager.Instance?.GoBack();
    void OnGoToSkills() => UIManager.Instance?.ShowSkillDeckScreen();
}
