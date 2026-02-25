using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SkillDeckScreen — Skill card library + active deck selector.
///
/// Layout:
///   ┌─────────────────────────────────────────────────────────┐
///   │  ACTIVE DECK (4 slots)                                  │
///   │  [ Slot1 ]  [ Slot2 ]  [ Slot3 ]  [ Slot4 ]           │
///   ├─────────────────────────────────────────────────────────┤
///   │  ALL SKILLS (12 cards in a scrollable 4-col grid)       │
///   │  [Freeze] [Reverse] [Shrink] [Obstacle]                │
///   │  [Shield] [Ghost]   [Deflect] [SlowMo]                 │
///   │  [Magnet] [DoubleLoot] [Steal] [VaultKey]              │
///   └─────────────────────────────────────────────────────────┘
///
/// Tap a skill in the library:
///   • If NOT in deck → add it (if slot available); else show "deck full"
///   • If IN deck → remove it
///   • Always opens CardDetailModal on second tap (long hold in future)
///
/// Wire in Inspector:
///   deckSlots[]         → 4 CardUI slot references (top area)
///   libraryContainer    → ScrollView Content (GridLayoutGroup)
///   libraryCardPrefab   → CardUI prefab (smaller version)
///   detailModal         → CardDetailModal
///   coinBalanceText     → shared coin display
///   deckFullLabel       → shown briefly when deck is full
/// </summary>
public class SkillDeckScreen : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Active Deck Slots (4)")]
    public CardUI[] deckSlots = new CardUI[4];     // pre-placed slot UIs in scene
    public TMP_Text deckTitleLabel;

    [Header("Skill Library")]
    public Transform  libraryContainer;
    public GameObject libraryCardPrefab;

    [Header("Modals + Feedback")]
    public CardDetailModal detailModal;
    public TMP_Text        deckFullLabel;           // "Deck full! Remove a skill first."
    public TMP_Text        coinBalanceText;

    [Header("Navigation")]
    public Button backButton;
    public Button goToCharactersButton;

    // ─── Private ──────────────────────────────────────────────────────────────
    private List<CardUI> _libraryCards = new List<CardUI>();
    private bool         _initialized  = false;
    private string       _lastTappedSkill = "";    // detect double-tap for detail
    private float        _lastTapTime    = 0f;
    private const float  DOUBLE_TAP_WINDOW = 0.5f;

    // ─── Lifecycle ────────────────────────────────────────────────────────────
    void Start()
    {
        if (backButton            != null) backButton.onClick.AddListener(OnBack);
        if (goToCharactersButton  != null) goToCharactersButton.onClick.AddListener(OnGoToCharacters);
    }

    void OnEnable()
    {
        if (CardManager.Instance != null)
        {
            CardManager.Instance.OnCardsLoaded      += OnCardsReady;
            CardManager.Instance.OnActiveDeckChanged += RefreshAll;
            CardManager.Instance.OnSkillUpgraded     += OnSkillUpgraded;
        }

        if (!_initialized)
        {
            if (CardManager.Instance != null && CardManager.Instance.IsLoaded)
                BuildScreen();
            else if (CardManager.Instance == null)
                BuildScreen(); // editor fallback
        }
        else
        {
            RefreshAll();
        }

        RefreshCoinDisplay();

        if (deckFullLabel != null) deckFullLabel.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        if (CardManager.Instance != null)
        {
            CardManager.Instance.OnCardsLoaded       -= OnCardsReady;
            CardManager.Instance.OnActiveDeckChanged -= RefreshAll;
            CardManager.Instance.OnSkillUpgraded     -= OnSkillUpgraded;
        }
    }

    // ─── Build ────────────────────────────────────────────────────────────────
    void OnCardsReady() => BuildScreen();

    void BuildScreen()
    {
        BuildLibrary();
        RefreshDeckSlots();
        _initialized = true;

        if (deckTitleLabel != null) deckTitleLabel.text = "Active Deck (pick 4)";
    }

    void BuildLibrary()
    {
        if (libraryCardPrefab == null || libraryContainer == null) return;

        foreach (Transform child in libraryContainer)
            Destroy(child.gameObject);
        _libraryCards.Clear();

        foreach (var def in CardDefinitions.Skills)
        {
            string id = def.id; // capture

            var go = Instantiate(libraryCardPrefab, libraryContainer);
            var ui = go.GetComponent<CardUI>() ?? go.AddComponent<CardUI>();

            SkillCardData data = GetOrDefaultSkill(id);
            ui.SetupSkill(data, def);

            bool inDeck = CardManager.Instance != null
                ? CardManager.Instance.IsInActiveDeck(id)
                : false;
            ui.SetSelected(inDeck);

            ui.OnClick = () => OnLibraryCardTapped(id, ui);

            _libraryCards.Add(ui);
        }
    }

    void RefreshDeckSlots()
    {
        var activeDeck = CardManager.Instance?.ActiveDeck ?? new System.Collections.Generic.List<string>();

        for (int i = 0; i < deckSlots.Length; i++)
        {
            var slot = deckSlots[i];
            if (slot == null) continue;

            if (i < activeDeck.Count && !string.IsNullOrEmpty(activeDeck[i]))
            {
                string skillId = activeDeck[i];
                SkillCardData data = GetOrDefaultSkill(skillId);
                var def = CardDefinitions.FindSkill(skillId);
                if (def.HasValue)
                {
                    slot.gameObject.SetActive(true);
                    slot.SetupSkill(data, def.Value);
                    slot.SetSelected(true);

                    int capturedI = i;
                    string capturedId = skillId;
                    slot.OnClick = () => RemoveFromDeckSlot(capturedI, capturedId);
                }
            }
            else
            {
                // Empty slot
                slot.gameObject.SetActive(true);
                ClearSlotUI(slot, i);
            }
        }
    }

    void ClearSlotUI(CardUI slot, int slotIndex)
    {
        // Reset slot to "empty" visual state
        slot.SetSelected(false);
        slot.OnClick = null;

        // Name label shows empty hint
        if (slot.nameLabel != null) slot.nameLabel.text = $"— empty —";
        if (slot.levelLabel != null) slot.levelLabel.text = "";
        if (slot.portraitImage != null) slot.portraitImage.sprite = null;
        if (slot.glowRingImage != null) slot.glowRingImage.color = new Color(0.3f, 0.3f, 0.3f);
        if (slot.frameImage    != null) slot.frameImage.color    = new Color(0.15f, 0.15f, 0.15f);
        if (slot.progressBar   != null) slot.progressBar.gameObject.SetActive(false);
        if (slot.upgradeButton != null) slot.upgradeButton.gameObject.SetActive(false);
    }

    // ─── Interaction ──────────────────────────────────────────────────────────

    void OnLibraryCardTapped(string skillId, CardUI ui)
    {
        float now = Time.unscaledTime;
        bool doubleTap = (skillId == _lastTappedSkill) && (now - _lastTapTime < DOUBLE_TAP_WINDOW);
        _lastTappedSkill = skillId;
        _lastTapTime     = now;

        if (doubleTap)
        {
            // Double-tap → open detail modal
            if (detailModal != null)
                detailModal.ShowSkill(skillId);
            return;
        }

        // Single tap → toggle deck membership
        if (CardManager.Instance == null) return;

        bool wasInDeck = CardManager.Instance.IsInActiveDeck(skillId);
        if (!wasInDeck && CardManager.Instance.ActiveDeck.Count >= CardManager.Instance.activeDeckSize)
        {
            // Deck full
            ShowDeckFullMessage();
            return;
        }

        CardManager.Instance.ToggleSkillInDeck(skillId);
        // UI refresh happens via OnActiveDeckChanged event
    }

    void RemoveFromDeckSlot(int slotIndex, string skillId)
    {
        CardManager.Instance?.ToggleSkillInDeck(skillId);
    }

    void ShowDeckFullMessage()
    {
        if (deckFullLabel == null) return;
        deckFullLabel.gameObject.SetActive(true);
        CancelInvoke(nameof(HideDeckFullMessage));
        Invoke(nameof(HideDeckFullMessage), 2f);
    }

    void HideDeckFullMessage()
    {
        if (deckFullLabel != null) deckFullLabel.gameObject.SetActive(false);
    }

    // ─── Refresh ──────────────────────────────────────────────────────────────
    void RefreshAll()
    {
        RefreshDeckSlots();
        RefreshLibrarySelectionState();
        RefreshCoinDisplay();
    }

    void RefreshLibrarySelectionState()
    {
        int idx = 0;
        foreach (var def in CardDefinitions.Skills)
        {
            if (idx >= _libraryCards.Count) break;
            var ui = _libraryCards[idx];
            if (ui != null)
            {
                bool inDeck = CardManager.Instance != null
                    ? CardManager.Instance.IsInActiveDeck(def.id)
                    : false;
                ui.SetSelected(inDeck);

                // Re-apply data in case copies changed
                SkillCardData data = GetOrDefaultSkill(def.id);
                ui.SetupSkill(data, def);
                ui.SetSelected(inDeck); // re-apply after setup resets it
            }
            idx++;
        }
    }

    void OnSkillUpgraded(string skillId) => RefreshAll();

    void RefreshCoinDisplay()
    {
        if (coinBalanceText == null) return;
        int coins = CardManager.Instance?.GetPlayerCoins()
                    ?? PlayerPrefs.GetInt("VaultDash_Coins", 0);
        coinBalanceText.text = $" coins {coins:N0}";
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    SkillCardData GetOrDefaultSkill(string id)
    {
        if (CardManager.Instance != null && CardManager.Instance.SkillCards.TryGetValue(id, out var data))
            return data;
        return new SkillCardData { skillId = id };
    }

    // ─── Navigation ───────────────────────────────────────────────────────────
    void OnBack()             => UIManager.Instance?.GoBack();
    void OnGoToCharacters()   => UIManager.Instance?.ShowCardDeckScreen();
}
