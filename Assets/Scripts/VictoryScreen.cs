using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VictoryScreen — Post-match celebration + defeat screen with Revenge Queue.
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
/// REVENGE QUEUE (Bart's design — always available):
///   • Win or lose → [REMATCH] always shown
///   • Click REMATCH → same opponent, fresh tunnel, streaks preserved
///   • Best-of-3 mode toggle: winner takes enhanced prize pool
///   • If opponent unavailable → fallback to random matchmaking
///   • Match history (W/L streaks per opponent) tracked in PlayerPrefs
///
/// Wire all UI references in the Inspector.
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
    public TMP_Text trophiesText;       // "+15" or "-8" (trophy icon shown via Image)
    public TMP_Text rankText;           // "Rookie → Silver" or "Still Rookie"
    public TMP_Text distanceText;       // "500m cleared!"
    public TMP_Text rewardText;         // "Reward: 250 coins"

    [Header("Tier Progression Bar (Post-Match)")]
    [Tooltip("Attach a TierProgressionBar component on a Slider. Shows tier progress after match.")]
    public TierProgressionBar tierProgressionBar;
    [Tooltip("Optional: prestige popup panel shown when player is eligible")]
    public GameObject prestigePromptPanel;
    public TMP_Text   prestigePromptText;
    public UnityEngine.UI.Button prestigeConfirmButton;
    public UnityEngine.UI.Button prestigeCancelButton;

    [Header("Confetti")]
    public ParticleSystem confettiParticles;

    [Header("Buttons")]
    public Button rematchButton;
    public Button menuButton;

    // ─── Revenge Queue ────────────────────────────────────────────────────────
    [Header("Revenge Queue")]
    public TMP_Text  rematchButtonText;     // "REMATCH" or "REVENGE"
    public TMP_Text  streakText;            // "You lead 2-1 this session!"
    public TMP_Text  opponentStatusText;    // "Opponent available" / "Searching…"
    public Toggle    bestOf3Toggle;         // Toggle best-of-3 mode
    public GameObject bestOf3Panel;         // Shows prize pool info
    public TMP_Text  bestOf3PrizeText;      // "Best-of-3 Prize: 750 coins"
    public TMP_Text  bestOf3ScoreText;      // "Session: You 1 — Opponent 1"

    [Header("Timing")]
    public float fadeInDuration      = 0.5f;
    public float showcaseDelay       = 0.3f;
    public float showcaseDuration    = 2.5f;
    public float statsRevealDelay    = 0.8f;

    [Header("Win Character Animation")]
    public float winSpinSpeed        = 180f;
    public float winScaleTarget      = 1.25f;
    public float winScaleSpeed       = 2.0f;

    [Header("Lose Character Animation")]
    public float loseScaleTarget     = 0.75f;
    public Color loseTint            = new Color(0.5f, 0.5f, 0.55f, 1f);

    // ─── Runtime ──────────────────────────────────────────────────────────────
    private bool   _showing   = false;
    private bool   _playerWon;
    private string _opponentId = "";

    // Streak tracking keys (per opponent — keyed by opponentId)
    private const string STREAK_WIN_KEY  = "Streak_Win_";
    private const string STREAK_LOSS_KEY = "Streak_Loss_";
    private const string BO3_WIN_KEY     = "BO3_Win_";
    private const string BO3_MODE_KEY    = "BO3_Mode";

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Start()
    {
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

        if (rematchButton != null) rematchButton.onClick.AddListener(OnRematch);
        if (menuButton    != null) menuButton.onClick.AddListener(OnMenu);
        if (bestOf3Toggle != null) bestOf3Toggle.onValueChanged.AddListener(OnBestOf3Toggle);

        IAPManager.OnGemsGranted   += OnGemsGrantedCallback;
        IAPManager.OnPurchaseError += OnPurchaseErrorCallback;
    }

    // ─── Public API ───────────────────────────────────────────────────────────
    /// <summary>
    /// Trigger the victory/defeat sequence.
    /// Called from MatchManager via OpponentVisualizer.
    /// </summary>
    public void Show(bool playerWon)
    {
        if (_showing) return;
        _showing    = true;
        _playerWon  = playerWon;
        _opponentId = MatchManager.Instance?.OpponentId ?? "offline";

        // Record match result
        RecordMatchResult(playerWon);

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
        SetButtonsInteractable(false);

        // Audio
        if (playerWon) AudioManager.Instance?.PlayVictory();
        else           AudioManager.Instance?.PlayDefeat();

        // FMOD variant
        if (playerWon) FMODAudioManager.Instance?.PlayVictory();
        else           FMODAudioManager.Instance?.PlayDefeat();

        // 1. Fade in
        yield return StartCoroutine(FadeIn(fadeInDuration));

        // 2. Result header
        ShowResultHeader(playerWon);
        yield return new WaitForSecondsRealtime(showcaseDelay);

        // 3. Character showcase
        yield return StartCoroutine(CharacterShowcase(playerWon));

        // 4. Stats panel
        yield return StartCoroutine(RevealStats(playerWon));

        // 5. Revenge Queue UI
        SetupRevengeQueueUI(playerWon);

        // 6. Enable buttons
        SetButtonsInteractable(true);

        Debug.Log($"[VictoryScreen] Sequence complete. Won: {playerWon}");
    }

    // ─── Fade In ──────────────────────────────────────────────────────────────
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

    // ─── Result Header ────────────────────────────────────────────────────────
    void ShowResultHeader(bool playerWon)
    {
        if (winHeader  != null) winHeader.SetActive(playerWon);
        if (loseHeader != null) loseHeader.SetActive(!playerWon);

        if (resultSubText != null)
        {
            resultSubText.text = playerWon
                ? "You outran your opponent!"
                : "Your opponent was faster this time...";
        }
    }

    // ─── Character Showcase ───────────────────────────────────────────────────
    IEnumerator CharacterShowcase(bool playerWon)
    {
        if (characterContainer == null) { yield return new WaitForSecondsRealtime(showcaseDuration); yield break; }

        // Set portrait
        var profile = GetCurrentPlayerProfile();
        if (profile != null && characterPortrait != null)
        {
            if (playerWon && profile.victorySprite != null)
                characterPortrait.sprite = profile.victorySprite;
            else if (profile.portraitSprite != null)
                characterPortrait.sprite = profile.portraitSprite;
        }

        if (!playerWon && characterPortrait != null)
            characterPortrait.color = loseTint;

        if (playerWon && confettiParticles != null) confettiParticles.Play();
        if (characterGlowEffect != null) characterGlowEffect.SetActive(playerWon);

        float elapsed = 0f;

        if (playerWon)
        {
            while (elapsed < showcaseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                characterContainer.Rotate(0f, 0f, winSpinSpeed * Time.unscaledDeltaTime);
                float sc = Mathf.MoveTowards(characterContainer.localScale.x, winScaleTarget, winScaleSpeed * Time.unscaledDeltaTime);
                characterContainer.localScale = Vector3.one * sc;
                yield return null;
            }
        }
        else
        {
            while (elapsed < showcaseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float sc = Mathf.MoveTowards(characterContainer.localScale.x, loseScaleTarget, 0.8f * Time.unscaledDeltaTime);
                characterContainer.localScale = Vector3.one * sc;
                yield return null;
            }
        }
    }

    // ─── Stats Reveal ─────────────────────────────────────────────────────────
    IEnumerator RevealStats(bool playerWon)
    {
        yield return new WaitForSecondsRealtime(statsRevealDelay);

        int   score = GameManager.Instance?.Score    ?? 0;
        float dist  = GameManager.Instance?.Distance ?? 0f;

        if (finalScoreText != null) finalScoreText.text = $"Score: {score}";
        if (distanceText   != null) distanceText.text   = $"{Mathf.RoundToInt(dist)}m cleared";

        int trophyChange = playerWon ? CalculateTrophyGain(score) : -CalculateTrophyLoss(score);

        // ─── Trophy + Tier update via RankedProgressionManager ────────────────
        int oldTrophies = RankedProgressionManager.Instance != null
            ? RankedProgressionManager.Instance.State.trophies
            : PlayerPrefs.GetInt("VaultDash_Trophies", 0);

        UpdateTrophies(trophyChange);   // writes to PlayerPrefs + RankedProgressionManager

        int newTrophies = RankedProgressionManager.Instance != null
            ? RankedProgressionManager.Instance.State.trophies
            : PlayerPrefs.GetInt("VaultDash_Trophies", 0);

        // ─── Trophies text ────────────────────────────────────────────────────
        if (trophiesText != null)
        {
            // Trophy icon shown via Image component next to this label
            trophiesText.text  = trophyChange >= 0 ? $"+{trophyChange}" : $"{trophyChange}";
            trophiesText.color = trophyChange >= 0
                ? new Color(0.9f, 0.75f, 0.1f)
                : new Color(0.7f, 0.3f, 0.3f);
        }

        // ─── Tier progression bar ─────────────────────────────────────────────
        if (tierProgressionBar != null)
            tierProgressionBar.Show(oldTrophies, newTrophies);

        // ─── Rank text ────────────────────────────────────────────────────────
        if (rankText != null)
        {
            var oldTier = RankedProgressionManager.GetTierForTrophies(oldTrophies);
            var newTier = RankedProgressionManager.GetTierForTrophies(newTrophies);

            if (oldTier.tier != newTier.tier && trophyChange > 0)
            {
                // Tier icon shown via Image (use GameIconSystem.ApplyIcon on a sibling Image)
                rankText.text  = $"Promoted to {newTier.name}!";
                rankText.color = newTier.color;
            }
            else if (oldTier.tier != newTier.tier && trophyChange < 0)
            {
                rankText.text  = $"Dropped to {newTier.name}";
                rankText.color = new Color(0.7f, 0.3f, 0.3f);
            }
            else
            {
                rankText.text  = playerWon ? "Keep climbing!" : "Better luck next time!";
                rankText.color = Color.white;
            }
        }

        // ─── Rewards ──────────────────────────────────────────────────────────
        if (rewardText != null)
        {
            int coins = playerWon ? 250 + (score / 10) : 50;
            // Coin icon shown via Image component next to this label
            rewardText.text = $"Reward: {coins} coins";
            GrantCoins(coins);
        }

        yield return StartCoroutine(AnimateStatText(finalScoreText));
        yield return StartCoroutine(AnimateStatText(trophiesText));
        yield return new WaitForSecondsRealtime(0.1f);
        yield return StartCoroutine(AnimateStatText(rewardText));

        // ─── Prestige prompt (if eligible) ────────────────────────────────────
        if (RankedProgressionManager.Instance != null
            && RankedProgressionManager.Instance.State.prestigeAvailable)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            ShowPrestigePrompt();
        }
    }

    IEnumerator AnimateStatText(TMP_Text label)
    {
        if (label == null) yield break;
        float elapsed = 0f;
        float duration = 0.2f;
        Vector3 originalScale = label.transform.localScale;
        Vector3 punchScale    = originalScale * 1.35f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            label.transform.localScale = Vector3.Lerp(punchScale, originalScale, elapsed / duration);
            yield return null;
        }
        label.transform.localScale = originalScale;
    }

    // ─── Revenge Queue ────────────────────────────────────────────────────────
    void SetupRevengeQueueUI(bool playerWon)
    {
        // Rematch button label
        if (rematchButtonText != null)
        {
            rematchButtonText.text = playerWon ? "REMATCH" : "REVENGE";
        }

        // Session streak display
        if (streakText != null)
        {
            string opp    = MatchManager.Instance?.OpponentName ?? "opponent";
            int wins      = PlayerPrefs.GetInt($"{STREAK_WIN_KEY}{_opponentId}",  0);
            int losses    = PlayerPrefs.GetInt($"{STREAK_LOSS_KEY}{_opponentId}", 0);
            int total     = wins + losses;

            if (total > 0)
            {
                streakText.gameObject.SetActive(true);
                if (wins > losses)
                    streakText.text = $"You lead {wins}–{losses} vs {opp} this session!";
                else if (losses > wins)
                    streakText.text = $"{opp} leads {losses}–{wins} this session";
                else
                    streakText.text = $"Tied {wins}–{losses} vs {opp}!";
            }
            else
            {
                streakText.gameObject.SetActive(false);
            }
        }

        // Opponent availability
        if (opponentStatusText != null)
        {
            bool available = MatchmakingService.IsPlayerAvailable(_opponentId);
            opponentStatusText.text = available
                ? $"{MatchManager.Instance?.OpponentName ?? "Opponent"} is ready"
                : "Searching for opponent...";
        }

        // Best-of-3 toggle
        if (bestOf3Toggle != null)
        {
            bestOf3Toggle.isOn = PlayerPrefs.GetInt(BO3_MODE_KEY, 0) == 1;
            UpdateBestOf3Display(bestOf3Toggle.isOn);
        }
    }

    void OnBestOf3Toggle(bool enabled)
    {
        PlayerPrefs.SetInt(BO3_MODE_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
        UpdateBestOf3Display(enabled);
    }

    void UpdateBestOf3Display(bool enabled)
    {
        if (bestOf3Panel != null) bestOf3Panel.SetActive(enabled);

        if (enabled && bestOf3PrizeText != null)
        {
            int basePrize = 750;
            bestOf3PrizeText.text = $"Best-of-3 Prize: {basePrize} coins + bonus gems";
        }

        if (enabled && bestOf3ScoreText != null)
        {
            int myWins  = PlayerPrefs.GetInt($"{BO3_WIN_KEY}Me",  0);
            int oppWins = PlayerPrefs.GetInt($"{BO3_WIN_KEY}Opp", 0);
            bestOf3ScoreText.text = $"BO3 Score: You {myWins} — {oppWins} Opponent";
        }
    }

    // ─── Match History ────────────────────────────────────────────────────────
    void RecordMatchResult(bool won)
    {
        if (string.IsNullOrEmpty(_opponentId)) return;

        if (won)
            PlayerPrefs.SetInt($"{STREAK_WIN_KEY}{_opponentId}",
                PlayerPrefs.GetInt($"{STREAK_WIN_KEY}{_opponentId}", 0) + 1);
        else
            PlayerPrefs.SetInt($"{STREAK_LOSS_KEY}{_opponentId}",
                PlayerPrefs.GetInt($"{STREAK_LOSS_KEY}{_opponentId}", 0) + 1);

        // Update global stats
        PlayerPrefs.SetInt("VaultDash_TotalMatches",
            PlayerPrefs.GetInt("VaultDash_TotalMatches", 0) + 1);
        if (won)
            PlayerPrefs.SetInt("VaultDash_TotalWins",
                PlayerPrefs.GetInt("VaultDash_TotalWins", 0) + 1);

        PlayerPrefs.Save();
    }

    // ─── Trophy + Coin Logic ──────────────────────────────────────────────────
    void UpdateTrophies(int delta)
    {
        if (RankedProgressionManager.Instance != null)
        {
            // Authoritative path — manager handles Firestore + tier events
            int before = RankedProgressionManager.Instance.State.trophies;
            RankedProgressionManager.Instance.AddTrophies(delta);
            int after  = RankedProgressionManager.Instance.State.trophies;
            Debug.Log($"[VictoryScreen] Trophies (via Manager): {before} → {after} (Δ{delta})");
        }
        else
        {
            // Fallback — manager not in scene
            int current = PlayerPrefs.GetInt("VaultDash_Trophies", 0);
            int updated = Mathf.Max(0, current + delta);
            PlayerPrefs.SetInt("VaultDash_Trophies", updated);
            PlayerPrefs.Save();
            Debug.Log($"[VictoryScreen] Trophies (local): {current} → {updated} (Δ{delta})");
        }
    }

    // ─── Prestige Prompt ──────────────────────────────────────────────────────
    void ShowPrestigePrompt()
    {
        if (prestigePromptPanel == null) return;

        int prestige = RankedProgressionManager.Instance?.State.prestigeLevel ?? 0;
        int nextPrestige = prestige + 1;

        if (prestigePromptText != null)
        {
            prestigePromptText.text =
                $"PRESTIGE AVAILABLE!\n\n" +
                $"You've conquered Legend.\n" +
                $"Reset to Rookie and earn Prestige {nextPrestige}.\n\n" +
                $"{RankedProgressionManager.GetPrestigeStars(nextPrestige)} ({nextPrestige} star{(nextPrestige > 1 ? "s" : "")})\n" +
                $"Purple glow unlocks on your character!\n\n" +
                $"Ready to prove yourself again?";
        }

        if (prestigeConfirmButton != null)
        {
            prestigeConfirmButton.onClick.RemoveAllListeners();
            prestigeConfirmButton.onClick.AddListener(() =>
            {
                RankedProgressionManager.Instance?.ExecutePrestige();
                if (prestigePromptPanel != null) prestigePromptPanel.SetActive(false);
            });
        }

        if (prestigeCancelButton != null)
        {
            prestigeCancelButton.onClick.RemoveAllListeners();
            prestigeCancelButton.onClick.AddListener(() =>
            {
                if (prestigePromptPanel != null) prestigePromptPanel.SetActive(false);
            });
        }

        prestigePromptPanel.SetActive(true);
    }

    void GrantCoins(int coins)
    {
        int current = PlayerPrefs.GetInt("VaultDash_Coins", 0);
        PlayerPrefs.SetInt("VaultDash_Coins", current + coins);
        PlayerPrefs.Save();
    }

    int CalculateTrophyGain(int score) => Mathf.Clamp(10 + score / 100, 10, 35);
    int CalculateTrophyLoss(int score) => Mathf.Clamp(5 + score / 200, 5, 15);

    // ─── Helpers ──────────────────────────────────────────────────────────────
    CharacterAnimationProfile GetCurrentPlayerProfile()
    {
        if (CharacterDatabase.Instance == null) return null;
        int selectedChar = PlayerPrefs.GetInt("SelectedCharacter", 0);
        return CharacterDatabase.Instance.GetProfile(selectedChar);
    }

    void SetButtonsInteractable(bool value)
    {
        if (rematchButton   != null) rematchButton.interactable = value;
        if (menuButton      != null) menuButton.interactable    = value;
        if (rootCanvasGroup != null) rootCanvasGroup.interactable = value;
    }

    // ─── Button Callbacks ─────────────────────────────────────────────────────
    /// <summary>
    /// REVENGE QUEUE: Always available — same opponent or random fallback.
    /// </summary>
    void OnRematch()
    {
        Hide();

        bool bestOf3 = PlayerPrefs.GetInt(BO3_MODE_KEY, 0) == 1;

        if (!string.IsNullOrEmpty(_opponentId) && _opponentId != "offline")
        {
            MatchManager.Instance?.RequestRematch(_opponentId, bestOf3: bestOf3);
        }
        else
        {
            // Offline or no opponent ID — start fresh
            MatchManager.Instance?.FindMatch();
        }
    }

    void OnMenu()
    {
        Hide();
        GameManager.Instance?.ReturnToMenu();
        AudioManager.Instance?.PlayMenuMusic();
        FMODAudioManager.Instance?.PlayMenuMusic();
        PostProcessingManager.Instance?.ResetEffects();
        UIManager.Instance?.ShowMainMenu();
    }

    void OnDestroy()
    {
        IAPManager.OnGemsGranted   -= OnGemsGrantedCallback;
        IAPManager.OnPurchaseError -= OnPurchaseErrorCallback;
    }

    void OnGemsGrantedCallback(string productId, int amount)
    {
        Debug.Log($"[VictoryScreen] Gems granted: {amount} from {productId}");
    }

    void OnPurchaseErrorCallback(string reason)
    {
        Debug.LogWarning($"[VictoryScreen] Purchase error: {reason}");
    }
}

// ─── Matchmaking Service (opponent availability) ───────────────────────────────
/// <summary>
/// Stub for real-time opponent availability check.
/// In production: query Nakama presence/status.
/// </summary>
public static class MatchmakingService
{
    /// <summary>
    /// Returns true if opponent is available for a rematch.
    /// Offline: always returns false (trigger random matchmaking fallback).
    /// </summary>
    public static bool IsPlayerAvailable(string playerId)
    {
        if (string.IsNullOrEmpty(playerId) || playerId == "offline") return false;

#if NAKAMA_AVAILABLE
        // TODO: query Nakama status API
        // var status = await _socket.FollowUsersAsync(new[] { playerId });
        // return status.Presences.Any(p => p.Status == "in_menu");
        return false; // placeholder until Nakama connected
#else
        // Simulate: 60% chance opponent is available in offline/demo mode
        return UnityEngine.Random.value > 0.4f;
#endif
    }
}
