using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ParticleEffects — Centralized visual FX spawner for Vault Dash.
///
/// Provides fire-and-forget methods for:
///  • Loot collect burst (coins / gems)
///  • Obstacle hit burst
///  • Power-up activation glow
///  • Lane-change swoosh
///  • Jump arc trail
///  • Score popup (floating damage number)
///  • Screen flash (on collision / victory)
///
/// All effects use Unity's built-in ParticleSystem via procedural creation
/// when no prefabs are assigned — so the game looks great out of the box.
///
/// Singleton — attach to a persistent GameObject.
/// </summary>
public class ParticleEffects : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static ParticleEffects Instance { get; private set; }

    // ─── Optional Prefabs (assign in Inspector for custom art) ────────────────
    [Header("Optional Prefabs")]
    public GameObject coinBurstPrefab;
    public GameObject gemBurstPrefab;
    public GameObject obstacleBurstPrefab;
    public GameObject powerUpGlowPrefab;
    public GameObject swooshPrefab;

    [Header("Score Popup")]
    public GameObject scorePopupPrefab;  // TextMeshPro popup
    public Canvas     worldCanvas;       // World-space canvas for score popups

    [Header("Screen Flash")]
    public Image      screenFlashImage;  // Full-screen semi-transparent Image

    // ─── Pool ─────────────────────────────────────────────────────────────────
    // Procedural particle objects reuse a small pool
    private Queue<ParticleSystem> _particlePool = new Queue<ParticleSystem>();
    private const int POOL_SIZE = 12;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        PrewarmPool();
    }

    void PrewarmPool()
    {
        for (int i = 0; i < POOL_SIZE; i++)
        {
            var ps = CreateProceduralBurst(Color.white, 0);
            ps.gameObject.SetActive(false);
            _particlePool.Enqueue(ps);
        }
    }

    // ─── Public FX API ────────────────────────────────────────────────────────

    /// <summary>Coin collect burst — small golden explosion.</summary>
    public void CoinBurst(Vector3 worldPos)
    {
        if (coinBurstPrefab != null)
            SpawnPrefab(coinBurstPrefab, worldPos);
        else
            FirePooledBurst(worldPos, new Color(1f, 0.85f, 0.1f), 12, 2f);
    }

    /// <summary>Gem collect burst — sparkling purple/blue explosion.</summary>
    public void GemBurst(Vector3 worldPos)
    {
        if (gemBurstPrefab != null)
            SpawnPrefab(gemBurstPrefab, worldPos);
        else
            FirePooledBurst(worldPos, new Color(0.4f, 0.1f, 1.0f), 20, 3f);
    }

    /// <summary>Obstacle collision burst — red impact splatter.</summary>
    public void ObstacleBurst(Vector3 worldPos)
    {
        if (obstacleBurstPrefab != null)
            SpawnPrefab(obstacleBurstPrefab, worldPos);
        else
            FirePooledBurst(worldPos, new Color(1f, 0.2f, 0.05f), 25, 4f);

        // Screen flash red briefly
        StartCoroutine(ScreenFlash(new Color(1f, 0f, 0f, 0.35f), 0.15f));
    }

    /// <summary>Power-up activation — radial glow ring.</summary>
    public void PowerUpActivate(Vector3 worldPos, Color color)
    {
        if (powerUpGlowPrefab != null)
            SpawnPrefab(powerUpGlowPrefab, worldPos);
        else
            FirePooledBurst(worldPos, color, 30, 5f, radial: true);

        StartCoroutine(ScreenFlash(new Color(color.r, color.g, color.b, 0.2f), 0.1f));
    }

    /// <summary>Lane-change swoosh — quick directional streak.</summary>
    public void LaneSwoosh(Vector3 startPos, int direction)
    {
        if (swooshPrefab != null)
        {
            var go = SpawnPrefab(swooshPrefab, startPos);
            if (go != null) go.transform.localScale = new Vector3(direction, 1f, 1f);
            return;
        }

        // Procedural line trail
        StartCoroutine(SwooshTrail(startPos, direction));
    }

    /// <summary>Jump arc trail — breadcrumb dots along the jump path.</summary>
    public void JumpTrail(Vector3 position, float normalizedTime)
    {
        // Spawn a small bright dot particle at the current jump position
        var ps = GetPooledParticle();
        if (ps == null) return;

        ps.gameObject.SetActive(true);
        ps.transform.position = position;

        var main       = ps.main;
        main.startSize = 0.12f;
        main.startLifetime = 0.25f;
        main.startColor    = new ParticleSystem.MinMaxGradient(
            new Color(0.5f, 0.9f, 1.0f, 0.8f));
        main.maxParticles  = 3;

        var em       = ps.emission;
        em.rateOverTime = 0;

        var burst = new ParticleSystem.Burst(0f, 3);
        em.SetBursts(new[] { burst });

        ps.Play();
        StartCoroutine(ReturnToPool(ps, 0.4f));
    }

    /// <summary>Score popup — floating "+10" text that rises and fades.</summary>
    public void ScorePopup(Vector3 worldPos, int amount, bool isCombo = false)
    {
        if (scorePopupPrefab != null && worldCanvas != null)
        {
            StartCoroutine(FloatingScoreRoutine(worldPos, amount, isCombo));
            return;
        }

        // Fallback: just burst
        CoinBurst(worldPos);
    }

    // ─── Internal ─────────────────────────────────────────────────────────────
    void FirePooledBurst(Vector3 pos, Color color, int count, float speed,
                         bool radial = false)
    {
        var ps = GetPooledParticle();
        if (ps == null)
        {
            // Fallback: create a temporary one
            ps = CreateProceduralBurst(color, count);
            ps.transform.position = pos;
            ps.Play();
            Destroy(ps.gameObject, 2f);
            return;
        }

        ps.gameObject.SetActive(true);
        ps.transform.position = pos;

        ConfigureBurst(ps, color, count, speed, radial);
        ps.Play();

        StartCoroutine(ReturnToPool(ps, 1.5f));
    }

    void ConfigureBurst(ParticleSystem ps, Color color, int count, float speed, bool radial)
    {
        var main = ps.main;
        main.startColor    = new ParticleSystem.MinMaxGradient(color);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        main.maxParticles  = count * 2;
        main.gravityModifier = 1.2f;

        var em = ps.emission;
        em.rateOverTime = 0;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

        var shape = ps.shape;
        shape.shapeType = radial
            ? ParticleSystemShapeType.Circle
            : ParticleSystemShapeType.Sphere;
        shape.radius    = 0.1f;
    }

    ParticleSystem CreateProceduralBurst(Color color, int count)
    {
        var go = new GameObject("FX_Burst");
        go.transform.parent = transform;
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startColor    = new ParticleSystem.MinMaxGradient(color);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(1f, 4f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        main.maxParticles  = Mathf.Max(count * 2, 20);
        main.loop          = false;
        main.playOnAwake   = false;
        main.gravityModifier = 1.2f;

        var em = ps.emission;
        em.rateOverTime = 0;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.1f;

        // Color over lifetime: fade out
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        return ps;
    }

    ParticleSystem GetPooledParticle()
    {
        while (_particlePool.Count > 0)
        {
            var ps = _particlePool.Dequeue();
            if (ps != null) return ps;
        }
        return null;  // pool exhausted — caller handles fallback
    }

    IEnumerator ReturnToPool(ParticleSystem ps, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ps != null)
        {
            ps.Stop();
            ps.Clear();
            ps.gameObject.SetActive(false);
            _particlePool.Enqueue(ps);
        }
    }

    GameObject SpawnPrefab(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) return null;
        var go = Instantiate(prefab, pos, Quaternion.identity);
        Destroy(go, 3f);
        return go;
    }

    // ─── Screen Flash ─────────────────────────────────────────────────────────
    IEnumerator ScreenFlash(Color flashColor, float duration)
    {
        if (screenFlashImage == null) yield break;

        screenFlashImage.color = flashColor;
        yield return new WaitForSecondsRealtime(duration * 0.5f);

        float elapsed = 0f;
        Color start   = flashColor;
        Color end     = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);

        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            screenFlashImage.color = Color.Lerp(start, end, elapsed / (duration * 0.5f));
            yield return null;
        }
        screenFlashImage.color = Color.clear;
    }

    // ─── Swoosh Trail ─────────────────────────────────────────────────────────
    IEnumerator SwooshTrail(Vector3 startPos, int direction)
    {
        // Create a trail of 5 tiny particles across 0.5s
        for (int i = 0; i < 5; i++)
        {
            float offset = i * 0.2f * direction;
            Vector3 pos  = startPos + new Vector3(offset, 0f, 0f);
            FirePooledBurst(pos, new Color(0.7f, 0.9f, 1f, 0.6f), 3, 1f);
            yield return new WaitForSeconds(0.05f);
        }
    }

    // ─── Floating Score Popup ─────────────────────────────────────────────────
    IEnumerator FloatingScoreRoutine(Vector3 worldPos, int amount, bool isCombo)
    {
        var go = Instantiate(scorePopupPrefab, worldCanvas.transform);
        var tmp = go.GetComponent<TMPro.TMP_Text>();
        if (tmp == null) tmp = go.GetComponentInChildren<TMPro.TMP_Text>();

        if (tmp != null)
        {
            tmp.text  = isCombo ? $"+{amount} COMBO! 🔥" : $"+{amount}";
            tmp.color = isCombo ? new Color(1f, 0.7f, 0f) : Color.white;
        }

        // Position: convert world pos to screen/canvas pos
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null && Camera.main != null)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            rt.position = screenPos;
        }

        // Float up and fade out over 1.2s
        float elapsed  = 0f;
        float duration = 1.2f;
        Vector3 basePos = go.transform.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (rt != null)
                rt.localPosition = basePos + Vector3.up * (t * 60f);

            if (tmp != null)
            {
                Color c = tmp.color;
                c.a = 1f - t;
                tmp.color = c;
            }

            yield return null;
        }

        Destroy(go);
    }
}
