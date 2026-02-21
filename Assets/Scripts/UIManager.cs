using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UIManager — Central screen navigation hub for Vault Dash.
///
/// Manages all UI panels with smooth fade transitions:
///   • MainMenuScreen      — Hero entry point
///   • CharacterSelection  — Pick + preview character
///   • ShopScreen          — Gem packs + skins + battle pass
///   • ProfileScreen       — Stats, mastery, settings
///   • RankedLadderScreen  — Trophy rank + top 100
///   • GameHUD             — In-game overlay (TopBar)
///
/// Singleton — persists across scenes.
/// Wire all Screen panels in Inspector.
///
/// Usage:
///   UIManager.Instance.Show(UIManager.Screen.Shop);
///   UIManager.Instance.GoBack();
/// </summary>
public class UIManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static UIManager Instance { get; private set; }

    // ─── Screen Enum ──────────────────────────────────────────────────────────
    public enum Screen
    {
        None,
        MainMenu,
        CharacterSelection,
        Shop,
        Profile,
        RankedLadder,
        GameHUD,
        Victory,
        // Card System (Phase 1)
        CardDeck,          // Character card collection grid
        SkillDeck,         // Skill library + active deck selector
    }

    // ─── Panel References ─────────────────────────────────────────────────────
    [Header("Screens — assign each root CanvasGroup")]
    public CanvasGroup mainMenuPanel;
    public CanvasGroup characterSelectionPanel;
    public CanvasGroup shopPanel;
    public CanvasGroup profilePanel;
    public CanvasGroup rankedLadderPanel;
    public CanvasGroup gameHUDPanel;
    public CanvasGroup victoryPanel;

    [Header("Card System Screens")]
    public CanvasGroup cardDeckPanel;      // CardDeckScreen root
    public CanvasGroup skillDeckPanel;     // SkillDeckScreen root

    [Header("Transition")]
    [Tooltip("Duration of cross-fade between screens")]
    public float transitionDuration = 0.25f;

    // ─── State ────────────────────────────────────────────────────────────────
    private Screen _currentScreen = Screen.None;
    private Screen _previousScreen = Screen.None;
    private Coroutine _transitionRoutine;

    // ─── Screen Components (cached) ───────────────────────────────────────────
    private CharacterSelectionScreen _charSelection;
    private ShopScreen               _shopScreen;
    private ProfileScreen            _profileScreen;
    private RankedLadderScreen       _rankedLadder;
    private CardDeckScreen           _cardDeck;
    private SkillDeckScreen          _skillDeck;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Cache screen components
        if (characterSelectionPanel) _charSelection = characterSelectionPanel.GetComponent<CharacterSelectionScreen>();
        if (shopPanel)               _shopScreen    = shopPanel.GetComponent<ShopScreen>();
        if (profilePanel)            _profileScreen = profilePanel.GetComponent<ProfileScreen>();
        if (rankedLadderPanel)       _rankedLadder  = rankedLadderPanel.GetComponent<RankedLadderScreen>();
        if (cardDeckPanel)           _cardDeck      = cardDeckPanel.GetComponent<CardDeckScreen>();
        if (skillDeckPanel)          _skillDeck     = skillDeckPanel.GetComponent<SkillDeckScreen>();

        // Hide all panels at start
        HideAllImmediate();
    }

    void Start()
    {
        // Open main menu on boot
        Show(Screen.MainMenu, immediate: true);
    }

    // ─── Public Navigation API ────────────────────────────────────────────────
    public void ShowMainMenu()           => Show(Screen.MainMenu);
    public void ShowCharacterSelection() => Show(Screen.CharacterSelection);
    public void ShowShop()               => Show(Screen.Shop);
    public void ShowProfile()            => Show(Screen.Profile);
    public void ShowRankedLadder()       => Show(Screen.RankedLadder);
    public void ShowGameHUD()            => Show(Screen.GameHUD);
    public void ShowVictory()            => Show(Screen.Victory);
    public void ShowCardDeckScreen()     => Show(Screen.CardDeck);
    public void ShowSkillDeckScreen()    => Show(Screen.SkillDeck);

    public void GoBack()
    {
        if (_previousScreen != Screen.None)
            Show(_previousScreen);
    }

    public Screen CurrentScreen => _currentScreen;

    // ─── Core Transition ──────────────────────────────────────────────────────
    public void Show(Screen screen, bool immediate = false)
    {
        if (screen == _currentScreen) return;

        _previousScreen = _currentScreen;
        _currentScreen  = screen;

        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);

        if (immediate)
            TransitionImmediate(screen);
        else
            _transitionRoutine = StartCoroutine(TransitionRoutine(screen));

        // Notify screen component
        OnScreenActivated(screen);

        Debug.Log($"[UIManager] → {screen}");
    }

    void HideAllImmediate()
    {
        SetPanel(mainMenuPanel,          false, immediate: true);
        SetPanel(characterSelectionPanel, false, immediate: true);
        SetPanel(shopPanel,              false, immediate: true);
        SetPanel(profilePanel,           false, immediate: true);
        SetPanel(rankedLadderPanel,      false, immediate: true);
        SetPanel(gameHUDPanel,           false, immediate: true);
        SetPanel(victoryPanel,           false, immediate: true);
        SetPanel(cardDeckPanel,          false, immediate: true);
        SetPanel(skillDeckPanel,         false, immediate: true);
    }

    void TransitionImmediate(Screen screen)
    {
        HideAllImmediate();
        SetPanel(GetPanel(screen), true, immediate: true);
    }

    IEnumerator TransitionRoutine(Screen screen)
    {
        // Fade OUT current
        CanvasGroup prev = GetPanel(_previousScreen);
        if (prev != null)
        {
            float elapsed = 0f;
            float startAlpha = prev.alpha;
            while (elapsed < transitionDuration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                prev.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / (transitionDuration * 0.5f));
                yield return null;
            }
            SetPanel(prev, false, immediate: true);
        }

        // Fade IN new screen
        CanvasGroup next = GetPanel(screen);
        if (next != null)
        {
            next.alpha          = 0f;
            next.interactable   = false;
            next.blocksRaycasts = true;
            next.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < transitionDuration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                next.alpha = Mathf.Lerp(0f, 1f, elapsed / (transitionDuration * 0.5f));
                yield return null;
            }
            next.alpha        = 1f;
            next.interactable = true;
        }
    }

    void SetPanel(CanvasGroup panel, bool visible, bool immediate = false)
    {
        if (panel == null) return;
        panel.alpha          = visible ? 1f : 0f;
        panel.interactable   = visible;
        panel.blocksRaycasts = visible;
        panel.gameObject.SetActive(visible);
    }

    CanvasGroup GetPanel(Screen screen)
    {
        switch (screen)
        {
            case Screen.MainMenu:           return mainMenuPanel;
            case Screen.CharacterSelection: return characterSelectionPanel;
            case Screen.Shop:               return shopPanel;
            case Screen.Profile:            return profilePanel;
            case Screen.RankedLadder:       return rankedLadderPanel;
            case Screen.GameHUD:            return gameHUDPanel;
            case Screen.Victory:            return victoryPanel;
            case Screen.CardDeck:           return cardDeckPanel;
            case Screen.SkillDeck:          return skillDeckPanel;
            default:                        return null;
        }
    }

    void OnScreenActivated(Screen screen)
    {
        switch (screen)
        {
            case Screen.CharacterSelection: _charSelection?.OnActivate(); break;
            case Screen.Shop:               _shopScreen?.OnActivate();    break;
            case Screen.Profile:            _profileScreen?.OnActivate(); break;
            case Screen.RankedLadder:       _rankedLadder?.OnActivate();  break;
            // Card screens activate via OnEnable (MonoBehaviour lifecycle)
            case Screen.CardDeck:
            case Screen.SkillDeck:
                break;
            case Screen.MainMenu:
                AudioManager.Instance?.PlayMenuMusic();
                FMODAudioManager.Instance?.PlayMenuMusic();
                break;
        }
    }
}
