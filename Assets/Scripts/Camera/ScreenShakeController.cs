using UnityEngine;
using System.Collections;

namespace VaultDash.Camera
{
    /// <summary>
    /// SCREEN SHAKE CONTROLLER
    /// Professional camera shake for impacts, explosions, critical events
    /// Perlin noise-based smooth shaking
    /// </summary>
    public class ScreenShakeController : MonoBehaviour
    {
        private Vector3 originalPosition;
        private UnityEngine.Camera cameraComponent;
        private Coroutine shakeCoroutine;

        private void Awake()
        {
            cameraComponent = GetComponent<UnityEngine.Camera>();
            if (cameraComponent == null)
                cameraComponent = Camera.main;
            originalPosition = transform.localPosition;
        }

        public void Shake(float intensity, float duration)
        {
            if (shakeCoroutine != null)
                StopCoroutine(shakeCoroutine);
            
            shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
        }

        private IEnumerator ShakeCoroutine(float intensity, float duration)
        {
            float elapsed = 0f;
            float noiseOffset = Random.Range(0f, 100f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // Perlin noise for smooth shaking
                float x = Mathf.PerlinNoise(noiseOffset + elapsed * 2f, 0f) - 0.5f;
                float y = Mathf.PerlinNoise(noiseOffset, elapsed * 2f) - 0.5f;

                // Fade out intensity
                float currentIntensity = intensity * (1f - progress);

                transform.localPosition = originalPosition + new Vector3(x * currentIntensity, y * currentIntensity, 0f);

                yield return null;
            }

            transform.localPosition = originalPosition;
        }
    }
}
