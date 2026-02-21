using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VictoryScreen — Post-match celebration + defeat screen.
///
/// Flow:
///   1. Fade in (0.5s)
///   2. Show WIN / LOSE header (with glow pulse)
///   3. Character skin showcase animation (2-3s)
///      - WIN:  Spin → glow → enlarged character
///      - LOSE: Shrink + grey-out
///   4. Confetti particles (WIN only)
///   5. Stats panel: score, trophies earned/lost, new rank
///   6. Buttons: [REMATCH] [MENU]
///
/// Wire up all UI references in the Inspector.
/// This GO should be in the scene but with Canvas alpha = 0 (hidden).
/// </summary>
public class VictoryScreen : MonoBehaviour
{
    // ─── UI References ────────────────────────────────────────────────────────
    [Header("Root Canvas Group (set alpha=0 initially)")]
    public CanvasGroup rootCanvasGroup;

    [Header("Result Header")]
    public GameObject winHeader;      // "VICTORY!" text object
    public GameObject loseHeader;     // "DEFEATED" text object
    public TMP_Text   resultSubText;  // "You outran your opponent!" etc.

    [Header("Character Showcase")]
    public Image       characterPortrait;    // Winner's big skin portrait
    public GameObject  characterGlowEffect;  // Glow ring/particles around portrait
    public RectTransform characterContainer; // Parent RectTransform to scale/spin

    [Header("Stats Panel")]
    public TMP_Text finalScoreText;
    public TMP_Text trophiesText;       // "+15 🏆" or "-8 🏆"
    public TMP_Text rankText;           // "Rookie → Silver" or "Still Rookie"
    public TMP_Text distanceText;       // "500m cleared!"
    public TMP_Text rewardText;         // "Reward: 250 coins"

    [Header("Confetti")]
    public ParticleSystem confettiParticles;

    [Header("Buttons")]
    public Button rematchButton;
    public Button menuButton;

    [Header("Timing")]
    public float fadeInDuration      = 0.5f;
    public float showcaseDelay       = 0.3f;    // delay before character anim starts
    public float showcaseDuration    = 2.5f;    // total character celebration
    public float statsRevealDelay    = 0.8f;    // stats fade-in after showcase

    [Header("Win Character Animation")]
    public float winSpinSpeed        = 180f;    // deg/s
    public float winScaleTarget      = 1.25f;
    public float winScaleSpeed       = 2.0f;

    [Header("Lose Character Animation")]
    public float loseScaleTarget     = 0.75f;
    public Color loseTint            = new Color(0.5f, 0.5f, 0.55f, 1f);

    // ─── Runtime ──────────────────────────────────────────────────────────────
    private bool _showing = false;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Start()
    {
        // Make sure screen is hidden at start
        if (rootCanvasGroup == null)
            rootCanvasGroup = GetComponent<CanvasGroup>();
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha          = 0f;
            rootCanvasGroup.interactable   = false;
            rootCanvasGroup.blocksRaycasts = false;
        }

        if (winHeader  != null) winHeader.SetActive(false);
        if (loseHeader != null) loseHeader.SetActive(false);

        // Button listeners
        if (rematchButton != null) rematchButton.onClick.AddListener(OnRematch);
        if (menuButton    != null) menuButton.onClick.AddListener(OnMenu);
    }

    // ─── Public API ───────────────────────────────────────────────────────────
    /// <summary>
    /// Trigger the victory/defeat sequence.
    /// Call from OpponentVisualizer.CollisionSequence().
    /// </summary>
    public void Show(bool playerWon)
    {
        if (_showing) return;
        _showing = true;
        StartCoroutine(VictorySequence(playerWon));
    }

    public void Hide()
    {
        _showing = false;
        StopAllCoroutines();
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha          = 0f;
            rootCanvasGroup.interactable   = false;
            rootCanvasGroup.blocksRaycasts = false;
        }
        if (winHeader  != null) winHeader.SetActive(false);
        if (loseHeader != null) loseHeader.SetActive(false);
    }

    // ─── Victory Sequence ─────────────────────────────────────────────────────
    IEnumerator VictorySequence(bool playerWon)
    {
        // Make buttons non-interactable until sequence finishes
        SetButtonsInteractable(false);

        // 1. Fade in canvas
        yield return StartCoroutine(FadeIn(fadeInDuration));

        // 2. Show result header
        ShowResultHeader(playerWon);
        yield return new WaitForSecondsRealtime(showcaseDelay);

        // 3. Character showcase
        yield return StartCoroutine(CharacterShowcase(playerWon));

        // 4. Stats panel (staggered reveal)
        yield return StartCoroutine(RevealStats(playerWon));

        // 5. Enable buttons
        SetButtonsInteractable(true);

        Debug.Log($"[VictoryScreen] Sequence complete. Won: {playerWon}");
    }

    // ─── Step 1: Fade In ──────────────────────────────────────────────────────
    IEnumerator FadeIn(float duration)
    {
        if (rootCanvasGroup == null) yield break;

        rootCanvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            rootCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        rootCanvasGroup.alpha = 1f;
    }

    // ─── Step 2: Result Header ────────────────────────────────────────────────
    void ShowResultHeader(bool playerWon)
    {
        if (winHeader  != null) winHeader.SetActive(playerWon);
        if (loseHeader != null) loseHeader.SetActive(!playerWon);

        if (resultSubText != null)
        {
            resultSubText.text = playerWon
                ? "You outran your opponent! 🏅"
                : "Your opponent was faster this time...";
        }
    }

    // ─── Step 3: Character Showcase ───────────────────────────────────────────
    IEnumerator CharacterShowcase(bool playerWon)
    {
        if (characterContainer == null) yield return new WaitForSecondsRealtime(showcaseDuration);

        // Set portrait sprite from selected character
        CharacterAnimationProfile profile = GetCurrentPlayerProfile();
        if (profile != null && characterPortrait != null && profile.victorySprite != null)
            characterPortrait.sprite = profile.victorySprite;
        else if (profile != null && characterPortrait != null && profile.portraitSprite != null)
            characterPortrait.sprite = profile.portraitSprite;

        // Tint for defeat
        if (!playerWon && characterPortrait != null)
            characterPortrait.color = loseTint;

        // Confetti
        if (playerWon && confettiParticles != null)
            confettiParticles.Play();

        // Glow effect
        if (characterGlowEffect != null)
            characterGlowEffect.SetActive(playerWon);

        float elapsed = 0f;

        if (playerWon)
        {
            // WIN: spin + scale up
            Vector3 startScale = characterContainer.localScale;
            float   targetSc   = winScaleTarget;

            while (elapsed < showcaseDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                // Spin
                characterContainer.Rotate(0f, 0f, winSpinSpeed * Time.unscaledDeltaTime);

                // Scale up
                float sc = Mathf.MoveTowards(
                    characterContainer.localScale.x,
                    targetSc,
                    winScaleSpeed * Time.unscaledDeltaTime);
                characterContainer.localScale = Vector3.one * sc;

                yield return null;
            }

            // Settle — stop spin
            // (rotation stays at current angle — looks natural)
        }
        else
        {
            // LOSE: shrink + grey
            while (elapsed < showcaseDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float sc = Mathf.MoveTowards(
                    characterContainer.localScale.x,
                    loseScaleTarget,
                    0.8f * Time.unscaledDeltaTime);
                characterContainer.localScale = Vector3.one * sc;

                yield return null;
            }
        }
    }

    // ─── Step 4: Stats Reveal ─────────────────────────────────────────────────
    IEnumerator RevealStats(bool playerWon)
    {
        yield return new WaitForSecondsRealtime(statsRevealDelay);

        int score    = GameManager.Instance?.Score    ?? 0;
        float dist   = GameManager.Instance?.Distance ?? 0f;

        // Score
        if (finalScoreText != null)
            finalScoreText.text = $"Score: {score}";

        // Distance
        if (distanceText != null)
            distanceText.text = $"{Mathf.RoundToInt(dist)}m cleared";

        // Trophies (simulated — replace with real Nakama data)
        int trophyChange = playerWon
            ? CalculateTrophyGain(score)
            : -CalculateTrophyLoss(score);

        if (trophiesText != null)
        {
            trophiesText.text  = trophyChange >= 0
                ? $"+{trophyChange} 🏆"
                : $"{trophyChange} 🏆";
            trophiesText.color = trophyChange >= 0
                ? new Color(0.9f, 0.75f, 0.1f)   // gold
                : new Color(0.7f, 0.3f, 0.3f);    // red-ish
        }

        // Reward (flat for now)
        if (rewardText != null)
        {
            int coins = playerWon ? 250 + (score / 10) : 50;
            rewardText.text = $"Reward: {coins} coins 🪙";
        }

        // Rank text (simple placeholder)
        if (rankText != null)
            rankText.text = playerWon ? "Keep climbing! 🚀" : "Better luck next time!";

        // Staggered number pop-in for each stat
        yield return StartCoroutine(AnimateStatText(finalScoreText));
        yield return StartCoroutine(AnimateStatText(trophiesText));
        yield return new WaitForSecondsRealtime(0.1f);
        yield return StartCoroutine(AnimateStatText(rewardText));
    }

    IEnumerator AnimateStatText(TMP_Text label)
    {
        if (label == null) yield break;

        // Quick punch scale
        float elapsed = 0f;
        float duration = 0.2f;
        Vector3 originalScale = label.transform.localScale;
        Vector3 punchScale    = originalScale * 1.35f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = elapsed / duration;
            label.transform.localScale = Vector3.Lerp(punchScale, originalScale, t);
            yield return null;
        }

        label.transform.localScale = originalScale;
    }

    // ─── Trophy Calculation (placeholder logic) ────────────────────────────────
    int CalculateTrophyGain(int score)
    {
        // Base 10, +1 per 100 score
        return Mathf.Clamp(10 + score / 100, 10, 35);
    }

    int CalculateTrophyLoss(int score)
    {
        return Mathf.Clamp(5 + score / 200, 5, 15);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    CharacterAnimationProfile GetCurrentPlayerProfile()
    {
        // Try to get from CharacterDatabase
        if (CharacterDatabase.Instance == null) return null;

        // Default to index 0 (AgentZero) — in a full game, read from PlayerPrefs
        int selectedChar = PlayerPrefs.GetInt("SelectedCharacter", 0);
        return CharacterDatabase.Instance.GetProfile(selectedChar);
    }

    void SetButtonsInteractable(bool value)
    {
        if (rematchButton != null)
        {
            rematchButton.interactable = value;
            // Also set canvas group interactable
            rootCanvasGroup.interactable = value;
        }
        if (menuButton != null)
            menuButton.interactable = value;
    }

    // ─── Button Callbacks ─────────────────────────────────────────────────────
    void OnRematch()
    {
        Hide();
        MatchManager.Instance?.FindMatch();
    }

    void OnMenu()
    {
        Hide();
        GameManager.Instance?.ReturnToMenu();
        AudioManager.Instance?.PlayMenuMusic();
    }
}
