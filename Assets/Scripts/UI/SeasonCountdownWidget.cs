using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SeasonCountdownWidget — Shows "Season X ends in 3d 4h" or
/// "Season X starts in 2 days" on the main menu.
///
/// Refreshes every minute. Subscribes to SeasonManager.OnSeasonChanged.
///
/// Wire in Inspector — attach to the main menu countdown container.
/// </summary>
public class SeasonCountdownWidget : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Text Fields")]
    public TMP_Text  countdownLabelText;    // "SEASON 1 — NEON VAULT"
    public TMP_Text  countdownTimeText;     // "Ends in 3d 4h" or "2 days left"
    public TMP_Text  seasonNumberText;      // "S1"  (small badge)

    [Header("Visuals")]
    public Image     seasonAccentBar;       // colored bar (season theme color)
    public Image     backgroundPanel;       // panel tinted by theme
    public GameObject urgentPulse;          // activated when < 24h remain

    [Header("Tap to Open")]
    [Tooltip("Set this to navigate to the Season / Ranked screen on tap")]
    public Button    widgetButton;

    [Header("Refresh Rate")]
    public float     refreshIntervalSeconds = 60f;  // every minute

    // ─── State ────────────────────────────────────────────────────────────────
    private SeasonInfo _season;
    private Coroutine  _refreshRoutine;

    // ─── Init ─────────────────────────────────────────────────────────────────

    void OnEnable()
    {
        if (SeasonManager.Instance != null)
        {
            SeasonManager.Instance.OnSeasonChanged -= OnSeasonChanged;
            SeasonManager.Instance.OnSeasonChanged += OnSeasonChanged;

            _season = SeasonManager.Instance.CurrentSeason;
        }

        Refresh();
        _refreshRoutine = StartCoroutine(RefreshLoop());

        if (widgetButton != null)
            widgetButton.onClick.AddListener(OnWidgetTapped);
    }

    void OnDisable()
    {
        if (SeasonManager.Instance != null)
            SeasonManager.Instance.OnSeasonChanged -= OnSeasonChanged;

        if (_refreshRoutine != null) StopCoroutine(_refreshRoutine);

        if (widgetButton != null)
            widgetButton.onClick.RemoveListener(OnWidgetTapped);
    }

    // ─── Season Change ────────────────────────────────────────────────────────

    void OnSeasonChanged(SeasonInfo newSeason)
    {
        _season = newSeason;
        Refresh();
    }

    // ─── Refresh Loop ─────────────────────────────────────────────────────────

    IEnumerator RefreshLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(refreshIntervalSeconds);
            Refresh();
        }
    }

    // ─── UI Update ────────────────────────────────────────────────────────────

    public void Refresh()
    {
        if (_season == null && SeasonManager.Instance != null)
            _season = SeasonManager.Instance.CurrentSeason;

        if (_season == null)
        {
            SetNoSeasonState();
            return;
        }

        var now       = DateTime.UtcNow;
        var remaining = _season.TimeRemaining;
        bool isActive = _season.IsActive;

        // ── Season number badge ───────────────────────────────────────────────
        if (seasonNumberText != null)
            seasonNumberText.text = $"S{_season.seasonNumber}";

        // ── Label ─────────────────────────────────────────────────────────────
        if (countdownLabelText != null)
            countdownLabelText.text = $"Season {_season.seasonNumber} — {_season.name}";

        // ── Time remaining ────────────────────────────────────────────────────
        if (countdownTimeText != null)
        {
            if (!isActive && now < _season.startDate)
            {
                // Season hasn't started yet
                var untilStart = _season.startDate - now;
                countdownTimeText.text = FormatCountdown(untilStart, prefix: "Starts in");
            }
            else if (isActive && remaining.TotalSeconds > 0)
            {
                countdownTimeText.text = FormatCountdown(remaining, prefix: "Ends in");
            }
            else
            {
                countdownTimeText.text = "Season ended";
            }
        }

        // ── Urgency pulse (< 24h) ─────────────────────────────────────────────
        bool urgent = isActive && remaining.TotalHours < 24 && remaining.TotalSeconds > 0;
        if (urgentPulse != null) urgentPulse.SetActive(urgent);

        if (countdownTimeText != null)
        {
            countdownTimeText.color = urgent
                ? new Color(1f, 0.4f, 0.3f)   // red-orange
                : Color.white;
        }

        // ── Theme color ───────────────────────────────────────────────────────
        Color themeColor = Color.cyan;
        if (_season.cosmetic != null)
            themeColor = _season.cosmetic.ThemeColorUnity;
        else if (_season.arenaOverlay != null)
            themeColor = _season.arenaOverlay.PrimaryColor;

        if (seasonAccentBar   != null) seasonAccentBar.color   = themeColor;
        if (backgroundPanel   != null) backgroundPanel.color   = themeColor * 0.15f;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    void SetNoSeasonState()
    {
        if (countdownLabelText != null) countdownLabelText.text = "Loading season…";
        if (countdownTimeText  != null) countdownTimeText.text  = "";
        if (seasonNumberText   != null) seasonNumberText.text   = "—";
        if (urgentPulse        != null) urgentPulse.SetActive(false);
    }

    static string FormatCountdown(TimeSpan t, string prefix = "")
    {
        string time;
        if (t.TotalDays >= 1)
            time = $"{(int)t.TotalDays}d {t.Hours}h";
        else if (t.TotalHours >= 1)
            time = $"{(int)t.TotalHours}h {t.Minutes}m";
        else
            time = $"{(int)t.TotalMinutes}m";

        return string.IsNullOrEmpty(prefix) ? time : $"{prefix} {time}";
    }

    // ─── Navigation ───────────────────────────────────────────────────────────

    void OnWidgetTapped()
    {
        UIManager.Instance?.ShowRankedLadder();  // or ShowSeasonScreen() if defined
    }
}
