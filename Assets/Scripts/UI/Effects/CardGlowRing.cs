using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace VaultDash.UI.Effects
{
    /// <summary>
    /// CARD GLOW RING
    /// Premium rarity-based glow animation for card UI
    /// Clash Royale style pulsing glow
    /// </summary>
    public class CardGlowRing : MonoBehaviour
    {
        [SerializeField] private Image glowRingImage;
        [SerializeField] private float pulseSpeed = 1.5f;
        [SerializeField] private float minAlpha = 0.4f;
        [SerializeField] private float maxAlpha = 0.9f;

        private CanvasGroup canvasGroup;
        private Coroutine pulseCoroutine;

        private void OnEnable()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            StartPulse();
        }

        private void OnDisable()
        {
            if (pulseCoroutine != null)
                StopCoroutine(pulseCoroutine);
        }

        public void SetRarityColor(CardRarity rarity)
        {
            if (glowRingImage == null) return;

            glowRingImage.color = GetRarityGlowColor(rarity);
            glowRingImage.outlineWidth = GetRarityOutlineWidth(rarity);
        }

        private Color GetRarityGlowColor(CardRarity rarity) =>
            rarity switch
            {
                CardRarity.Common => new Color(1f, 0.84f, 0f, 1f),      // Gold
                CardRarity.Rare => new Color(0.6f, 0.2f, 0.8f, 1f),     // Purple
                CardRarity.Epic => new Color(0f, 0.76f, 0.82f, 1f),     // Cyan
                CardRarity.Legendary => new Color(1f, 0.34f, 0.14f, 1f), // Red/Orange
                _ => Color.white
            };

        private float GetRarityOutlineWidth(CardRarity rarity) =>
            rarity switch
            {
                CardRarity.Common => 2f,
                CardRarity.Rare => 3f,
                CardRarity.Epic => 4f,
                CardRarity.Legendary => 5f,
                _ => 1f
            };

        private void StartPulse()
        {
            pulseCoroutine = StartCoroutine(PulseCoroutine());
        }

        private IEnumerator PulseCoroutine()
        {
            while (true)
            {
                // Pulse in
                float elapsed = 0;
                while (elapsed < pulseSpeed / 2)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (pulseSpeed / 2);
                    canvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
                    yield return null;
                }

                // Pulse out
                elapsed = 0;
                while (elapsed < pulseSpeed / 2)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (pulseSpeed / 2);
                    canvasGroup.alpha = Mathf.Lerp(maxAlpha, minAlpha, t);
                    yield return null;
                }
            }
        }

        public void AnimateScale(float duration = 0.3f)
        {
            StartCoroutine(ScaleAnimCoroutine(duration));
        }

        private IEnumerator ScaleAnimCoroutine(float duration)
        {
            Vector3 originalScale = transform.localScale;
            float elapsed = 0;

            // Scale in with bounce
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float bounce = EaseOutBounce(t);
                transform.localScale = originalScale * bounce;
                yield return null;
            }

            transform.localScale = originalScale;
        }

        private static float EaseOutBounce(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }

    public enum CardRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }
}
