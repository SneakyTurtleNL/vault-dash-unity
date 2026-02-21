using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Vault Opening Animation — post-run loot reveal
/// 
/// Hierarchy verwacht:
/// VaultOpeningPanel (Canvas/Panel)
///   ├── VaultDoor (Image) — de kluis sprite
///   │     └── WheelHandle (Image) — het draaiwiel (child, draait onafhankelijk)
///   ├── LightBurst (Image) — witte gloed, schaal 0 → groot bij opening
///   ├── LootContainer (RectTransform) — houdt loot items
///   │     ├── CoinReward (Image + TMP_Text)
///   │     └── GemReward (Image + TMP_Text)
///   └── TapToContinue (TMP_Text) — fade in na animatie
/// </summary>
public class VaultOpeningAnimation : MonoBehaviour
{
    [Header("Vault Parts")]
    public RectTransform vaultDoor;        // De hele kluisdeur
    public RectTransform wheelHandle;      // Het draaiwiel (child van vaultDoor)
    public Image lightBurst;              // Witte lichtflits bij opening

    [Header("Loot")]
    public RectTransform lootContainer;   // Parent van alle loot items
    public TMP_Text coinAmountText;
    public TMP_Text gemAmountText;

    [Header("UI")]
    public TMP_Text tapToContinueText;
    public CanvasGroup panelGroup;        // Voor fade in/out van het hele panel

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip wheelTurnClip;
    public AudioClip lockClickClip;
    public AudioClip doorOpenClip;
    public AudioClip lootRevealClip;

    [Header("Timing")]
    public float introDelay = 0.3f;
    public float wheelSpinDuration = 1.8f;
    public float doorSwingDuration = 0.6f;
    public float lootPopDelay = 0.15f;

    // Loot data
    private int _coins;
    private int _gems;

    // ──────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Start de vault opening animatie met de opgegeven loot.
    /// </summary>
    public void PlayAnimation(int coins, int gems)
    {
        _coins = coins;
        _gems  = gems;
        gameObject.SetActive(true);
        StartCoroutine(AnimationSequence());
    }

    // ──────────────────────────────────────────────
    // Animatie Sequence
    // ──────────────────────────────────────────────

    private IEnumerator AnimationSequence()
    {
        // Reset state
        ResetState();

        // 1. Panel fade in
        yield return StartCoroutine(FadePanel(0f, 1f, 0.25f));

        // 2. Kluis bounce in (schaal 0 → 1 met overshoot)
        yield return StartCoroutine(BounceIn(vaultDoor, introDelay));

        // 3. Wiel draait (3 rondes, versnellen dan afremmen)
        PlaySound(wheelTurnClip);
        yield return StartCoroutine(SpinWheel(wheelSpinDuration));

        // 4. Klik — vergrendeling gaat open
        PlaySound(lockClickClip);
        yield return StartCoroutine(ShakeVault(0.15f));
        yield return new WaitForSeconds(0.1f);

        // 5. Deur zwaait open (schaal X → 0, dan lichtburst)
        PlaySound(doorOpenClip);
        yield return StartCoroutine(SwingDoorOpen(doorSwingDuration));

        // 6. Lichtburst
        yield return StartCoroutine(LightBurstEffect(0.3f));

        // 7. Loot items poppen één voor één omhoog
        PlaySound(lootRevealClip);
        yield return StartCoroutine(RevealLoot());

        // 8. "Tik om door te gaan" fade in
        yield return StartCoroutine(FadeText(tapToContinueText, 0f, 1f, 0.5f));

        // 9. Wacht op tik
        yield return WaitForTap();

        // 10. Panel fade out
        yield return StartCoroutine(FadePanel(1f, 0f, 0.3f));
        gameObject.SetActive(false);
    }

    // ──────────────────────────────────────────────
    // Animatie Helpers
    // ──────────────────────────────────────────────

    private void ResetState()
    {
        if (panelGroup) panelGroup.alpha = 0f;

        vaultDoor.localScale        = Vector3.zero;
        wheelHandle.localEulerAngles = Vector3.zero;

        if (lightBurst)
        {
            var c = lightBurst.color;
            lightBurst.color = new Color(c.r, c.g, c.b, 0f);
            lightBurst.transform.localScale = Vector3.zero;
        }

        if (lootContainer)
            foreach (Transform child in lootContainer)
                child.localScale = Vector3.zero;

        if (tapToContinueText)
        {
            var c = tapToContinueText.color;
            tapToContinueText.color = new Color(c.r, c.g, c.b, 0f);
        }

        if (coinAmountText) coinAmountText.text = $"+{_coins}";
        if (gemAmountText)  gemAmountText.text  = $"+{_gems}";
    }

    /// Schaal bounce in: 0 → 1.15 → 0.95 → 1.0
    private IEnumerator BounceIn(RectTransform rt, float delay)
    {
        yield return new WaitForSeconds(delay);

        float duration = 0.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float scale = BounceOutCurve(Mathf.Clamp01(t));
            rt.localScale = Vector3.one * scale;
            yield return null;
        }

        rt.localScale = Vector3.one;
    }

    /// Wiel draaien: 3× 360° met ease-in start en ease-out einde
    private IEnumerator SpinWheel(float duration)
    {
        float totalDegrees = 360f * 3f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float eased    = EaseInOut(Mathf.Clamp01(t));
            float rotation = eased * totalDegrees;
            wheelHandle.localEulerAngles = new Vector3(0f, 0f, -rotation);
            yield return null;
        }
    }

    /// Kleine shake voor de klik
    private IEnumerator ShakeVault(float duration)
    {
        Vector3 origin = vaultDoor.anchoredPosition3D;
        float   t      = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float shake = Mathf.Sin(t * Mathf.PI * 8f) * (1f - t) * 12f;
            vaultDoor.anchoredPosition3D = origin + new Vector3(shake, 0f, 0f);
            yield return null;
        }

        vaultDoor.anchoredPosition3D = origin;
    }

    /// Deur zwaait open: scaleX → 0 (alsof hij wegdraait)
    private IEnumerator SwingDoorOpen(float duration)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float eased  = EaseIn(Mathf.Clamp01(t));
            float scaleX = Mathf.Lerp(1f, 0f, eased);
            vaultDoor.localScale = new Vector3(scaleX, 1f, 1f);
            yield return null;
        }
    }

    /// Witte lichtflits die opkomt en vervaagt
    private IEnumerator LightBurstEffect(float duration)
    {
        if (!lightBurst) yield break;

        // Opkomen
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / (duration * 0.3f);
            float a = Mathf.Clamp01(t);
            var c = lightBurst.color;
            lightBurst.color           = new Color(c.r, c.g, c.b, a);
            lightBurst.transform.localScale = Vector3.one * Mathf.Lerp(0f, 3f, EaseOut(a));
            yield return null;
        }

        // Vervagen
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / (duration * 0.7f);
            float a = Mathf.Clamp01(1f - t);
            var c = lightBurst.color;
            lightBurst.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
    }

    /// Loot items poppen één voor één omhoog
    private IEnumerator RevealLoot()
    {
        if (!lootContainer) yield break;

        foreach (Transform item in lootContainer)
        {
            yield return StartCoroutine(BounceIn(item.GetComponent<RectTransform>(), 0f));
            yield return new WaitForSeconds(lootPopDelay);
        }
    }

    private IEnumerator FadePanel(float from, float to, float duration)
    {
        if (!panelGroup) yield break;

        float t = 0f;
        while (t < 1f)
        {
            t             += Time.deltaTime / duration;
            panelGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t));
            yield return null;
        }

        panelGroup.alpha = to;
    }

    private IEnumerator FadeText(TMP_Text txt, float from, float to, float duration)
    {
        if (!txt) yield break;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            var c = txt.color;
            txt.color = new Color(c.r, c.g, c.b, Mathf.Lerp(from, to, Mathf.Clamp01(t)));
            yield return null;
        }
    }

    private IEnumerator WaitForTap()
    {
        yield return new WaitForSeconds(0.3f);

        bool tapped = false;
        while (!tapped)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                tapped = true;
            yield return null;
        }
    }

    // ──────────────────────────────────────────────
    // Easing Curves
    // ──────────────────────────────────────────────

    private float EaseInOut(float t) => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
    private float EaseIn(float t)    => t * t;
    private float EaseOut(float t)   => t * (2f - t);

    /// Bounce curve: 0 → 1.15 → 0.95 → 1.0
    private float BounceOutCurve(float t)
    {
        if (t < 0.6f)  return EaseOut(t / 0.6f) * 1.15f;
        if (t < 0.8f)  return 1.15f - (t - 0.6f) / 0.2f * 0.20f;
        return 0.95f + (t - 0.8f) / 0.2f * 0.05f;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
            audioSource.PlayOneShot(clip);
    }
}
