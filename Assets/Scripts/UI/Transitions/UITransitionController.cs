using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VaultDash.UI.Transitions
{
    /// <summary>
    /// UI TRANSITION CONTROLLER
    /// Premium screen transitions (fade, slide, scale)
    /// Coordinates multiple screens for smooth navigation
    /// </summary>
    public class UITransitionController : MonoBehaviour
    {
        public static UITransitionController Instance { get; private set; }

        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private float defaultTransitionDuration = 0.4f;

        private Dictionary<string, CanvasGroup> screenRegistry = new();
        private Coroutine activeTransition;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            if (fadeOverlay == null)
            {
                var overlayObj = new GameObject("FadeOverlay");
                overlayObj.transform.SetParent(transform);
                fadeOverlay = overlayObj.AddComponent<CanvasGroup>();
                var image = overlayObj.AddComponent<Image>();
                image.color = Color.black;
                fadeOverlay.alpha = 0f;
            }
        }

        public void RegisterScreen(string screenName, CanvasGroup canvasGroup)
        {
            screenRegistry[screenName] = canvasGroup;
        }

        public void FadeTransition(string fromScreen, string toScreen, float duration = -1f)
        {
            if (activeTransition != null)
                StopCoroutine(activeTransition);

            activeTransition = StartCoroutine(FadeTransitionCoroutine(
                fromScreen, toScreen, duration <= 0 ? defaultTransitionDuration : duration
            ));
        }

        public void SlideTransition(string fromScreen, string toScreen, Vector2 slideDirection, float duration = -1f)
        {
            if (activeTransition != null)
                StopCoroutine(activeTransition);

            activeTransition = StartCoroutine(SlideTransitionCoroutine(
                fromScreen, toScreen, slideDirection, duration <= 0 ? defaultTransitionDuration : duration
            ));
        }

        private IEnumerator FadeTransitionCoroutine(string fromScreen, string toScreen, float duration)
        {
            var from = screenRegistry.ContainsKey(fromScreen) ? screenRegistry[fromScreen] : null;
            var to = screenRegistry.ContainsKey(toScreen) ? screenRegistry[toScreen] : null;

            if (to == null) yield break;

            // Fade out current screen
            if (from != null)
            {
                float elapsed = 0f;
                while (elapsed < duration / 2)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (duration / 2);
                    from.alpha = Mathf.Lerp(1f, 0f, t);
                    yield return null;
                }
                from.alpha = 0f;
                from.gameObject.SetActive(false);
            }

            // Show new screen
            to.gameObject.SetActive(true);
            to.alpha = 0f;

            // Fade in new screen
            float elapsedIn = 0f;
            while (elapsedIn < duration / 2)
            {
                elapsedIn += Time.deltaTime;
                float t = elapsedIn / (duration / 2);
                to.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }
            to.alpha = 1f;
        }

        private IEnumerator SlideTransitionCoroutine(string fromScreen, string toScreen, Vector2 slideDirection, float duration)
        {
            var from = screenRegistry.ContainsKey(fromScreen) ? screenRegistry[fromScreen] : null;
            var to = screenRegistry.ContainsKey(toScreen) ? screenRegistry[toScreen] : null;

            if (to == null) yield break;

            // Position new screen off-screen
            var toRect = to.GetComponent<RectTransform>();
            var startPos = toRect.anchoredPosition;
            var offScreenPos = startPos + slideDirection * 1000f;
            toRect.anchoredPosition = offScreenPos;

            // Show new screen
            to.gameObject.SetActive(true);
            to.alpha = 1f;

            // Slide in
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutCubic(t);
                toRect.anchoredPosition = Vector2.Lerp(offScreenPos, startPos, eased);
                
                if (from != null)
                    from.alpha = Mathf.Lerp(1f, 0f, eased);
                
                yield return null;
            }

            toRect.anchoredPosition = startPos;
            if (from != null)
            {
                from.alpha = 0f;
                from.gameObject.SetActive(false);
            }
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    }
}
