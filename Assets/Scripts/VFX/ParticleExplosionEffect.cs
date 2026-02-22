using UnityEngine;
using System.Collections;

namespace VaultDash.VFX
{
    /// <summary>
    /// PARTICLE EXPLOSION EFFECT
    /// Premium VFX for critical hits, power-up activations, level-ups
    /// Multi-layer particle burst with screen shake
    /// </summary>
    public class ParticleExplosionEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem coreExplosion;
        [SerializeField] private ParticleSystem outerRing;
        [SerializeField] private ParticleSystem sparkles;
        
        [SerializeField] private float screenShakeIntensity = 0.15f;
        [SerializeField] private float screenShakeDuration = 0.3f;
        [SerializeField] private AudioClip explosionSFX;

        private ScreenShakeController shakeController;
        private AudioSource audioSource;

        private void Awake()
        {
            shakeController = FindObjectOfType<ScreenShakeController>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        public void PlayExplosion(Vector3 position, Color tintColor, float scale = 1f)
        {
            transform.position = position;
            
            // Scale all particles
            if (coreExplosion != null)
            {
                coreExplosion.transform.localScale = Vector3.one * scale;
                coreExplosion.Play();
                TintParticles(coreExplosion, tintColor);
            }

            if (outerRing != null)
            {
                outerRing.transform.localScale = Vector3.one * scale * 1.5f;
                outerRing.Play();
                TintParticles(outerRing, tintColor * 0.7f);
            }

            if (sparkles != null)
            {
                sparkles.transform.localScale = Vector3.one * scale;
                sparkles.Play();
                TintParticles(sparkles, Color.yellow);
            }

            // Screen shake
            if (shakeController != null)
                shakeController.Shake(screenShakeIntensity, screenShakeDuration);

            // Play sound
            if (explosionSFX != null && audioSource != null)
                audioSource.PlayOneShot(explosionSFX);

            // Auto-cleanup
            StartCoroutine(DestroyAfterDuration(5f));
        }

        private void TintParticles(ParticleSystem ps, Color color)
        {
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                var mat = renderer.material;
                mat.color = color;
            }
        }

        private IEnumerator DestroyAfterDuration(float duration)
        {
            yield return new WaitForSeconds(duration);
            Destroy(gameObject);
        }
    }
}
