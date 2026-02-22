using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace VaultDash.UI.Cosmetics
{
    /// <summary>
    /// TROPHY SPARKLE EFFECT
    /// Animated sparkle particles around trophy icon
    /// Triggers on trophy gain/update
    /// </summary>
    public class TrophySparkleEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem sparkleParticles;
        [SerializeField] private Image trophyIcon;
        [SerializeField] private float sparkleScale = 1f;
        [SerializeField] private float sparkleIntensity = 0.8f;

        private CanvasGroup canvasGroup;
        private Coroutine glowCoroutine;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void PlaySparkle(int trophyGain)
        {
            // Emit particles based on trophy gain
            if (sparkleParticles != null)
            {
                var emission = sparkleParticles.emission;
                int burstCount = Mathf.Clamp(trophyGain / 5, 5, 50);
                emission.SetBursts(new[] { new ParticleSystem.Burst(0, burstCount) });
                sparkleParticles.Play();
            }

            // Glow animation
            if (glowCoroutine != null)
                StopCoroutine(glowCoroutine);
            glowCoroutine = StartCoroutine(GlowCoroutine());
        }

        private IEnumerator GlowCoroutine()
        {
            float elapsed = 0f;
            float duration = 0.6f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Scale pulse
                float scale = Mathf.Lerp(1f, 1.2f, Mathf.Sin(t * Mathf.PI));
                trophyIcon.transform.localScale = Vector3.one * scale;

                // Icon glow
                var color = trophyIcon.color;
                color.a = Mathf.Lerp(1f, 0.8f, t);
                trophyIcon.color = color;

                yield return null;
            }

            trophyIcon.transform.localScale = Vector3.one;
            var finalColor = trophyIcon.color;
            finalColor.a = 1f;
            trophyIcon.color = finalColor;
        }
    }
}
