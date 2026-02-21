using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CharacterSelectionScreen — Full 10-character selection + preview UI.
///
/// Layout:
///   • Top: large character portrait + name + tagline
///   • Middle: scrollable horizontal card strip (10 cards)
///   • Bottom: Stats panel (Level, Mastery, Skins owned)
///   • CTA: [SELECT & PLAY] button
///
/// Wire all references in Inspector.
/// CharacterDatabase.Instance must be present in scene.
/// </summary>
public class CharacterSelectionScreen : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Portrait Preview")]
    public Image        portraitImage;
    public TMP_Text     characterNameText;
    public TMP_Text     characterTaglineText;
    public Image        characterGlowImage;     // tinted ring behind portrait

    [Header("Character Cards")]
    public Transform    cardContainer;           // horizontal scroll content parent
    public GameObject   characterCardPrefab;     // card prefab with portrait + name + lock icon
    public ScrollRect   cardScrollRect;

    [Header("Stats Panel")]
    public TMP_Text     levelText;
    public TMP_Text     masteryText;             // "Mastery: 1,240 XP"
    public TMP_Text     skinsOwnedText;          // "Skins: 2 / 7"
    public Slider       masteryProgressBar;

    [Header("Locked State")]
    public GameObject   lockedOverlay;           // shown when character is locked
    public TMP_Text     unlockRequirementText;   // "Reach Silver rank to unlock"
    public Button       unlockButton;            // redirect to shop/rank

    [Header("CTA")]
    public Button       playButton;
    public TMP_Text     playButtonText;

    [Header("Back")]
    public Button       backButton;

    // ─── Private ──────────────────────────────────────────────────────────────
    private int _selectedIndex = 0;
    private List<CharacterCard> _cards = new List<CharacterCard>();
    private bool _initialized = false;

    // PlayerPrefs key for selected character
    private const string SELECTED_CHAR_KEY = "SelectedCharacter";

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Start()
    {
        _selectedIndex = PlayerPrefs.GetInt(SELECTED_CHAR_KEY, 0);

        if (backButton  != null) backButton.onClick.AddListener(OnBack);
        if (playButton  != null) playButton.onClick.AddListener(OnPlay);
        if (unlockButton!= null) unlockButton.onClick.AddListener(OnUnlock);
    }

    // ─── Activation (called by UIManager when screen opens) ──────────────────
    public void OnActivate()
    {
        if (!_initialized)
        {
            BuildCards();
            _initialized = true;
        }

        _selectedIndex = PlayerPrefs.GetInt(SELECTED_CHAR_KEY, 0);
        SelectCharacter(_selectedIndex, skipScroll: false);
    }

    // ─── Card Grid ────────────────────────────────────────────────────────────
    void BuildCards()
    {
        if (CharacterDatabase.Instance == null)
        {
            Debug.LogWarning("[CharacterSelectionScreen] No CharacterDatabase found.");
            BuildFallbackCards();
            return;
        }

        if (characterCardPrefab == null || cardContainer == null) return;

        // Clear existing
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);
        _cards.Clear();

        int count = CharacterDatabase.Instance.ProfileCount;
        for (int i = 0; i < count; i++)
        {
            int idx = i; // capture for lambda
            var profile = CharacterDatabase.Instance.GetProfile(i);

            GameObject cardGO = Instantiate(characterCardPrefab, cardContainer);
            CharacterCard card = cardGO.GetComponent<CharacterCard>();

            if (card == null) card = cardGO.AddComponent<CharacterCard>();

            card.Setup(profile, i, isLocked: IsCharacterLocked(i), onClick: () => SelectCharacter(idx));
            _cards.Add(card);
        }
    }

    void BuildFallbackCards()
    {
        // Build text-only cards when no CharacterDatabase
        string[] names = { "Agent Zero","Blaze","Knox","Jade","Vector","Cipher","Nova","Ryze","Titan","Echo" };
        if (cardContainer == null) return;

        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);
        _cards.Clear();

        for (int i = 0; i < names.Length; i++)
        {
            int idx = i;
            // Create a simple button-based card
            GameObject cardGO = new GameObject($"Card_{names[i]}");
            cardGO.transform.SetParent(cardContainer, false);

            var rt = cardGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120f, 160f);

            var btn = cardGO.AddComponent<Button>();
            var img = cardGO.AddComponent<Image>();
            img.color = IsCharacterLocked(i)
                ? new Color(0.3f, 0.3f, 0.3f)
                : new Color(0.15f, 0.15f, 0.2f);

            // Name label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(cardGO.transform, false);
            var label = labelGO.AddComponent<TMP_Text>();
            label.text = names[i];
            label.fontSize = 12;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            var lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            btn.onClick.AddListener(() => SelectCharacter(idx));

            CharacterCard card = cardGO.AddComponent<CharacterCard>();
            _cards.Add(card);
        }
    }

    // ─── Selection ────────────────────────────────────────────────────────────
    void SelectCharacter(int index, bool skipScroll = false)
    {
        _selectedIndex = Mathf.Clamp(index, 0, Mathf.Max(0, _cards.Count - 1));

        // Highlight selected card
        for (int i = 0; i < _cards.Count; i++)
            _cards[i]?.SetSelected(i == _selectedIndex);

        // Scroll selected card into view
        if (!skipScroll && cardScrollRect != null && _cards.Count > 0)
        {
            float t = (float)_selectedIndex / Mathf.Max(1, _cards.Count - 1);
            cardScrollRect.horizontalNormalizedPosition = t;
        }

        // Update portrait + stats
        UpdatePreview(_selectedIndex);
    }

    void UpdatePreview(int index)
    {
        bool locked = IsCharacterLocked(index);

        // Locked overlay
        if (lockedOverlay != null) lockedOverlay.SetActive(locked);
        if (playButton    != null) playButton.interactable = !locked;

        // Portrait
        var profile = CharacterDatabase.Instance?.GetProfile(index);
        if (profile != null)
        {
            if (portraitImage != null && profile.portraitSprite != null)
                portraitImage.sprite = profile.portraitSprite;

            if (characterNameText    != null) characterNameText.text    = profile.displayName;
            if (characterTaglineText != null) characterTaglineText.text = profile.description;

            // Glow tint (match character colour)
            if (characterGlowImage != null) characterGlowImage.color = profile.accentColor;

            // Stats
            int level   = PlayerPrefs.GetInt($"Char_{index}_Level",   1);
            int mastery = PlayerPrefs.GetInt($"Char_{index}_Mastery", 0);
            int skins   = PlayerPrefs.GetInt($"Char_{index}_Skins",   0);
            int nextXP  = GetMasteryThreshold(level);

            if (levelText         != null) levelText.text        = $"Lv {level}";
            if (masteryText       != null) masteryText.text      = $"Mastery  {mastery:N0} XP";
            if (skinsOwnedText    != null) skinsOwnedText.text   = $"Skins  {skins} / 7";
            if (masteryProgressBar!= null) masteryProgressBar.value = Mathf.Clamp01((float)mastery / nextXP);

            if (unlockRequirementText != null && locked)
                unlockRequirementText.text = GetUnlockRequirement(index);
        }
        else
        {
            // Fallback text
            string[] names = { "Agent Zero","Blaze","Knox","Jade","Vector","Cipher","Nova","Ryze","Titan","Echo" };
            if (characterNameText != null && index < names.Length)
                characterNameText.text = names[index];
        }

        if (playButtonText != null)
            playButtonText.text = locked ? "LOCKED 🔒" : "PLAY ▶";
    }

    // ─── Lock Logic ───────────────────────────────────────────────────────────
    bool IsCharacterLocked(int index)
    {
        // First two characters always unlocked
        if (index < 2) return false;
        return PlayerPrefs.GetInt($"Char_{index}_Unlocked", 0) == 0;
    }

    string GetUnlockRequirement(int index)
    {
        switch (index)
        {
            case 2: return "Reach Silver rank";
            case 3: return "Reach Gold rank";
            case 4: return "Reach Diamond rank";
            case 5: return "Win 50 matches";
            case 6: return "Reach Legend rank";
            case 7: return "Available in Shop";
            case 8: return "Season 2 Battle Pass";
            case 9: return "Available in Shop";
            default: return "Coming soon";
        }
    }

    int GetMasteryThreshold(int level)
    {
        return 500 + (level * 250);
    }

    // ─── Button Callbacks ─────────────────────────────────────────────────────
    void OnPlay()
    {
        // Save selection + start game
        PlayerPrefs.SetInt(SELECTED_CHAR_KEY, _selectedIndex);
        PlayerPrefs.Save();

        GameManager.Instance?.StartGame();
        UIManager.Instance?.ShowGameHUD();

        Debug.Log($"[CharacterSelection] Playing as character {_selectedIndex}");
    }

    void OnBack()
    {
        UIManager.Instance?.GoBack();
    }

    void OnUnlock()
    {
        UIManager.Instance?.ShowShop();
    }
}

// ─── Character Card Component ─────────────────────────────────────────────────
/// <summary>Small card in the character grid.</summary>
public class CharacterCard : MonoBehaviour
{
    [HideInInspector] public int characterIndex;

    private Image    _portrait;
    private TMP_Text _nameLabel;
    private Image    _selectionBorder;
    private GameObject _lockIcon;
    private Button   _button;

    public void Setup(CharacterAnimationProfile profile, int index, bool isLocked, System.Action onClick)
    {
        characterIndex = index;

        _portrait        = GetComponentInChildren<Image>();
        _nameLabel       = GetComponentInChildren<TMP_Text>();
        _button          = GetComponent<Button>();

        if (_portrait != null && profile?.portraitSprite != null)
            _portrait.sprite = profile.portraitSprite;

        if (_nameLabel != null)
            _nameLabel.text = profile?.displayName ?? $"Char {index}";

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick?.Invoke());
        }

        // Dim locked characters
        CanvasGroup cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        cg.alpha = isLocked ? 0.5f : 1f;
    }

    public void SetSelected(bool selected)
    {
        // Find or create selection border image
        if (_selectionBorder == null)
        {
            Transform border = transform.Find("SelectionBorder");
            if (border) _selectionBorder = border.GetComponent<Image>();
        }

        if (_selectionBorder != null)
            _selectionBorder.enabled = selected;

        // Scale punch
        transform.localScale = selected ? Vector3.one * 1.08f : Vector3.one;
    }
}
