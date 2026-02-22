using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace VaultDash.UI.Effects
{
    /// <summary>
    /// BUTTON POLISH EFFECT
    /// Premium button state animations (hover, press, disabled)
    /// Supercell-grade feedback
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonPolishEffect : MonoBehaviour
    {
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float pressScale = 0.95f;
        [SerializeField] private float transitionDuration = 0.15f;
        [SerializeField] private bool playClickFeedback = true;
        [SerializeField] private Image glowOverlay;

        private Button button;
        private Vector3 originalScale;
        private CanvasGroup canvasGroup;
        private Coroutine scaleCoroutine;

        private void Awake()
        {
            button = GetComponent<Button>();
            originalScale = transform.localScale;
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onSelect.AddListener(OnSelected);
                button.onDeselect.AddListener(OnDeselected);
                button.onClick.AddListener(OnClicked);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onSelect.RemoveListener(OnSelected);
                button.onDeselect.RemoveListener(OnDeselected);
                button.onClick.RemoveListener(OnClicked);
            }
        }

        private void OnSelected(BaseEventData data)
        {
            AnimateScale(hoverScale, "Hover");
            if (glowOverlay != null)
                StartCoroutine(FadeGlow(0.3f, true));
        }

        private void OnDeselected(BaseEventData data)
        {
            AnimateScale(1f, "Normal");
            if (glowOverlay != null)
                StartCoroutine(FadeGlow(0.2f, false));
        }

        private void OnClicked()
        {
            StartCoroutine(ClickFeedback());
            if (playClickFeedback)
                AudioManager.Instance?.PlaySFX("click");
        }

        private void AnimateScale(float targetScale, string state)
        {
            if (scaleCoroutine != null)
                StopCoroutine(scaleCoroutine);

            scaleCoroutine = StartCoroutine(ScaleAnimCoroutine(targetScale));
        }

        private IEnumerator ScaleAnimCoroutine(float targetScale)
        {
            Vector3 startScale = transform.localScale;
            Vector3 endScale = originalScale * targetScale;
            float elapsed = 0;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            transform.localScale = endScale;
        }

        private IEnumerator ClickFeedback()
        {
            // Quick scale pulse
            Vector3 startScale = transform.localScale;
            float elapsed = 0;
            float duration = 0.3f;

            // Down phase
            while (elapsed < duration / 3)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 3);
                transform.localScale = Vector3.Lerp(startScale, startScale * 0.9f, t);
                yield return null;
            }

            // Up phase
            elapsed = 0;
            Vector3 downScale = transform.localScale;
            while (elapsed < duration / 3 * 2)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 3 * 2);
                transform.localScale = Vector3.Lerp(downScale, startScale * 1.05f, t);
                yield return null;
            }

            // Settle
            elapsed = 0;
            Vector3 upScale = transform.localScale;
            while (elapsed < duration / 3)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 3);
                transform.localScale = Vector3.Lerp(upScale, startScale, t);
                yield return null;
            }

            transform.localScale = startScale;
        }

        private IEnumerator FadeGlow(float duration, bool fadeIn)
        {
            if (glowOverlay == null) yield break;

            float startAlpha = glowOverlay.color.a;
            float endAlpha = fadeIn ? 0.6f : 0f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Color c = glowOverlay.color;
                c.a = Mathf.Lerp(startAlpha, endAlpha, t);
                glowOverlay.color = c;
                yield return null;
            }

            Color finalColor = glowOverlay.color;
            finalColor.a = endAlpha;
            glowOverlay.color = finalColor;
        }

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
            canvasGroup.alpha = interactable ? 1f : 0.5f;
        }
    }
}
