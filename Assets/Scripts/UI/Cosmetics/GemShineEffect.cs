using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace VaultDash.UI.Cosmetics
{
    /// <summary>
    /// GEM SHINE EFFECT
    /// Sparkling gem animation for gem counter UI
    /// Shows when gems are earned/spent
    /// </summary>
    public class GemShineEffect : MonoBehaviour
    {
        [SerializeField] private Image gemIcon;
        [SerializeField] private ParticleSystem shineParticles;
        [SerializeField] private Text gemCountText;

        [SerializeField] private Color gemGlowColor = new Color(0.62f, 0.12f, 0.94f, 1f);  // Purple
        [SerializeField] private float shineScale = 1.2f;
        [SerializeField] private float shineDuration = 0.8f;

        private Coroutine shineCoroutine;

        public void PlayGemEarned(int gemAmount)
        {
            if (shineCoroutine != null)
                StopCoroutine(shineCoroutine);

            shineCoroutine = StartCoroutine(ShineCoroutine(true, gemAmount));
        }

        public void PlayGemSpent(int gemAmount)
        {
            if (shineCoroutine != null)
                StopCoroutine(shineCoroutine);

            shineCoroutine = StartCoroutine(ShineCoroutine(false, gemAmount));
        }

        private IEnumerator ShineCoroutine(bool earned, int amount)
        {
            // Emit particles
            if (shineParticles != null)
            {
                var emission = shineParticles.emission;
                var burst = new ParticleSystem.Burst(0, Mathf.Clamp(amount / 10, 5, 30));
                emission.SetBursts(new[] { burst });
                
                if (earned)
                {
                    shineParticles.Play();
                }
                else
                {
                    // Red sparkle for spent (optional)
                    var renderer = shineParticles.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null)
                        renderer.material.color = new Color(1f, 0.2f, 0.2f);
                    shineParticles.Play();
                }
            }

            // Icon animation
            float elapsed = 0f;
            while (elapsed < shineDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shineDuration;

                // Scale bounce
                float scale = Mathf.Lerp(shineScale, 1f, EaseOutCubic(t));
                gemIcon.transform.localScale = Vector3.one * scale;

                // Glow pulse
                var color = gemIcon.color;
                color = Color.Lerp(gemGlowColor, Color.white, t);
                gemIcon.color = color;

                yield return null;
            }

            gemIcon.transform.localScale = Vector3.one;
            gemIcon.color = Color.white;
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    }
}
