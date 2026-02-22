using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace VaultDash.UI.Cosmetics
{
    /// <summary>
    /// BUTTON HOVER GLOW
    /// Premium glow effect on button hover
    /// Adds visual depth and interactivity feedback
    /// </summary>
    public class ButtonHoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image glowImage;
        [SerializeField] private Color glowColor = new Color(1f, 0.84f, 0f);  // Gold
        [SerializeField] private float maxGlowAlpha = 0.6f;
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float transitionDuration = 0.2f;

        private Button button;
        private CanvasGroup glowCanvasGroup;
        private Vector3 originalScale;
        private bool isHovering = false;

        private void Awake()
        {
            button = GetComponent<Button>();
            originalScale = transform.localScale;

            // Create glow image if not assigned
            if (glowImage == null)
            {
                var glowObj = new GameObject("GlowOverlay");
                glowObj.transform.SetParent(transform);
                glowObj.transform.SetAsFirstSibling();
                glowImage = glowObj.AddComponent<Image>();
                glowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
                
                var rectTransform = glowObj.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }

            glowCanvasGroup = glowImage.GetComponent<CanvasGroup>();
            if (glowCanvasGroup == null)
                glowCanvasGroup = glowImage.gameObject.AddComponent<CanvasGroup>();

            glowCanvasGroup.alpha = 0f;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            StopAllCoroutines();
            StartCoroutine(TransitionGlow(true));
            StartCoroutine(TransitionScale(true));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            StopAllCoroutines();
            StartCoroutine(TransitionGlow(false));
            StartCoroutine(TransitionScale(false));
        }

        private System.Collections.IEnumerator TransitionGlow(bool showGlow)
        {
            float elapsed = 0f;
            float startAlpha = glowCanvasGroup.alpha;
            float endAlpha = showGlow ? maxGlowAlpha : 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                glowCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }

            glowCanvasGroup.alpha = endAlpha;
        }

        private System.Collections.IEnumerator TransitionScale(bool hoverIn)
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 endScale = hoverIn ? originalScale * hoverScale : originalScale;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            transform.localScale = endScale;
        }

        public void SetGlowColor(Color newColor)
        {
            glowColor = newColor;
            if (glowImage != null)
            {
                var c = glowImage.color;
                c.r = newColor.r;
                c.g = newColor.g;
                c.b = newColor.b;
                glowImage.color = c;
            }
        }
    }
}
