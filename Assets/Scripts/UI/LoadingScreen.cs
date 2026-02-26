using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// LoadingScreen — Splash art and async scene loading UI.
///
/// Asset references (wired via GameIconSystem):
///   Splash/splash_art_main       — main splash artwork (placeholder: 256x256 dark navy)
///   Splash/loading_screen_bg     — loading background (placeholder: 256x256 darker navy)
///
/// TODO post-launch:
///   1. Replace placeholder PNGs with final splash/loading art.
///   2. Resource paths stay the same — Unity hot-swaps the textures automatically.
///   3. Consider adding logo animation and studio ident before gameplay.
///
/// USAGE:
///   // Async load:
///   LoadingScreen.Instance.LoadScene("GameScene");
///
///   // Manual control:
///   LoadingScreen.Instance.Show("Loading...");
///   LoadingScreen.Instance.Hide();
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static LoadingScreen Instance { get; private set; }

    // ─── Inspector References ─────────────────────────────────────────────────
    [Header("Background Art")]
    [Tooltip("Full-screen background image. GameIconSystem applies 'splash_art_main'. " +
             "Placeholder: 256x256 dark navy PNG. Swap post-launch.")]
    public Image backgroundImage;

    [Tooltip("Overlay image drawn on top of background. GameIconSystem applies 'loading_screen_bg'. " +
             "Placeholder: 256x256 darker navy PNG. Swap post-launch.")]
    public Image overlayImage;

    [Header("Logo")]
    [Tooltip("Studio / game logo (not driven by GameIconSystem — assign directly).")]
    public Image logoImage;

    [Header("Progress")]
    public Slider     progressBar;
    public TMP_Text   statusLabel;
    public TMP_Text   tipLabel;

    [Header("Animation")]
    public CanvasGroup canvasGroup;
    public float       fadeDuration = 0.4f;

    // ─── Tips Pool ────────────────────────────────────────────────────────────
    private static readonly string[] LOADING_TIPS =
    {
        "Tip: Collect gems to unlock exclusive cosmetics!",
        "Tip: Reach Legend tier to unlock prestige mode.",
        "Tip: Cards level up when you collect enough duplicates.",
        "Tip: Power-ups stack — combine Freeze + Obstacle!",
        "Tip: Daily quests reset every 24h. Check back often.",
        "Tip: Seasonal rewards are based on your peak trophies.",
    };

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Start hidden
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    void Start()
    {
        ApplySplashArt();
    }

    // ─── Asset Wiring ─────────────────────────────────────────────────────────

    /// <summary>
    /// Applies splash art from GameIconSystem.
    /// Placeholder PNGs at Splash/splash_art_main and Splash/loading_screen_bg.
    /// Swap with final art post-launch (same resource paths).
    /// </summary>
    void ApplySplashArt()
    {
        // Main splash background
        if (backgroundImage != null)
        {
            GameIconSystem.ApplyIcon(backgroundImage, "splash_art_main");
            // Stretch to fill screen
            backgroundImage.preserveAspect = false;
        }

        // Loading overlay
        if (overlayImage != null)
        {
            GameIconSystem.ApplyIcon(overlayImage, "loading_screen_bg");
            overlayImage.preserveAspect = false;

            // Semi-transparent overlay
            var c = overlayImage.color;
            c.a = 0.7f;
            overlayImage.color = c;
        }

        // Random tip
        if (tipLabel != null)
            tipLabel.text = LOADING_TIPS[Random.Range(0, LOADING_TIPS.Length)];
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Show loading screen with optional status text, then async-load scene.</summary>
    public void LoadScene(string sceneName, string status = "Loading...")
    {
        gameObject.SetActive(true);
        ApplySplashArt();
        StartCoroutine(LoadSceneAsync(sceneName, status));
    }

    /// <summary>Show loading screen manually.</summary>
    public void Show(string status = "")
    {
        gameObject.SetActive(true);
        ApplySplashArt();
        if (statusLabel != null) statusLabel.text = status;
        StartCoroutine(FadeIn());
    }

    /// <summary>Hide loading screen.</summary>
    public void Hide()
    {
        StartCoroutine(FadeOutAndHide());
    }

    // ─── Internal ─────────────────────────────────────────────────────────────

    IEnumerator LoadSceneAsync(string sceneName, string status)
    {
        yield return StartCoroutine(FadeIn());

        if (statusLabel != null) statusLabel.text = status;
        if (progressBar != null) progressBar.value = 0f;

        var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        if (op == null) yield break;

        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            if (progressBar != null) progressBar.value = progress;

            if (op.progress >= 0.9f)
            {
                if (statusLabel != null) statusLabel.text = "Ready!";
                yield return new WaitForSecondsRealtime(0.3f);
                op.allowSceneActivation = true;
            }

            yield return null;
        }

        yield return StartCoroutine(FadeOutAndHide());
    }

    IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOutAndHide()
    {
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
        gameObject.SetActive(false);
    }
}
