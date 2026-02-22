using UnityEngine;
using System.Collections;

namespace VaultDash.VFX
{
    /// <summary>
    /// LOOT BURST EFFECT
    /// Premium coin/gem burst animation for game-over rewards
    /// Vault opening screen visual polish
    /// </summary>
    public class LootBurstEffect : MonoBehaviour
    {
        [SerializeField] private Vector2 burstForce = new Vector2(100f, 150f);
        [SerializeField] private float lifetime = 1.2f;
        [SerializeField] private float burstDelay = 0.1f;
        [SerializeField] private ParticleSystem burstParticles;
        [SerializeField] private ParticleSystem sparkleParticles;

        private CanvasGroup canvasGroup;
        private Vector3 startPos;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            startPos = transform.position;
        }

        public void PlayBurst(Vector3 fromPos, bool isGem = false)
        {
            StartCoroutine(BurstCoroutine(fromPos, isGem));
        }

        private IEnumerator BurstCoroutine(Vector3 fromPos, bool isGem)
        {
            transform.position = fromPos;
            canvasGroup.alpha = 1f;

            // Random burst direction (radial)
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0).normalized;

            // Random force variation
            float force = Random.Range(burstForce.x, burstForce.y);
            Vector3 velocity = direction * force;

            // Emit particles
            if (burstParticles != null)
                burstParticles.Play();
            if (sparkleParticles != null)
                sparkleParticles.Play();

            float elapsed = 0;
            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lifetime;

                // Float upward with velocity decay
                transform.position += velocity * Time.deltaTime * (1 - t);

                // Add sine wave drift for charm
                float drift = Mathf.Sin(t * Mathf.PI * 2) * 10f * (1 - t);
                transform.position += Vector3.right * drift * Time.deltaTime;

                // Fade out
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

                // Scale in then settle
                float scale = Mathf.Lerp(1.2f, 0.8f, t);
                transform.localScale = Vector3.one * scale;

                yield return null;
            }

            // Cleanup
            if (burstParticles != null)
                burstParticles.Stop();
            if (sparkleParticles != null)
                sparkleParticles.Stop();

            Destroy(gameObject);
        }
    }
}
