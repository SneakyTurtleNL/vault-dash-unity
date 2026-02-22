using UnityEngine;
using System.Collections;

namespace VaultDash.UI.Utilities
{
    /// <summary>
    /// UI ANIMATION HELPER
    /// Reusable coroutines for common UI polish animations
    /// Standardized easing and timing across all UI
    /// </summary>
    public static class UIAnimationHelper
    {
        #region Scale Animations

        public static IEnumerator ScaleTo(Transform target, Vector3 endScale, float duration, EasingType easing = EasingType.EaseOutCubic)
        {
            Vector3 startScale = target.localScale;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Ease(t, easing);
                target.localScale = Vector3.Lerp(startScale, endScale, eased);
                yield return null;
            }

            target.localScale = endScale;
        }

        public static IEnumerator ScaleBounce(Transform target, float maxScale, float duration)
        {
            Vector3 startScale = target.localScale;
            Vector3 bounceScale = startScale * maxScale;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float bounce = EaseOutBounce(t);
                target.localScale = Vector3.Lerp(Vector3.zero, bounceScale, bounce);
                yield return null;
            }

            target.localScale = bounceScale;
        }

        #endregion

        #region Fade Animations

        public static IEnumerator FadeTo(CanvasGroup group, float endAlpha, float duration, EasingType easing = EasingType.Linear)
        {
            float startAlpha = group.alpha;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Ease(t, easing);
                group.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);
                yield return null;
            }

            group.alpha = endAlpha;
        }

        public static IEnumerator FadeInAndScale(CanvasGroup group, Transform transform, float duration)
        {
            float elapsed = 0;
            Vector3 startScale = Vector3.zero;
            Vector3 endScale = Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutBack(t);

                group.alpha = t;
                transform.localScale = Vector3.Lerp(startScale, endScale, eased);
                yield return null;
            }

            group.alpha = 1f;
            transform.localScale = endScale;
        }

        #endregion

        #region Slide Animations

        public static IEnumerator SlideIn(RectTransform target, Vector2 fromPos, Vector2 toPos, float duration, EasingType easing = EasingType.EaseOutCubic)
        {
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Ease(t, easing);
                target.anchoredPosition = Vector2.Lerp(fromPos, toPos, eased);
                yield return null;
            }

            target.anchoredPosition = toPos;
        }

        #endregion

        #region Number Counter

        public static IEnumerator CountUpText(TMPro.TextMeshProUGUI textComponent, int startValue, int endValue, float duration)
        {
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                int currentValue = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, t));
                textComponent.text = currentValue.ToString();
                yield return null;
            }

            textComponent.text = endValue.ToString();
        }

        #endregion

        #region Easing Functions

        public enum EasingType
        {
            Linear,
            EaseInQuad,
            EaseOutQuad,
            EaseInOutQuad,
            EaseInCubic,
            EaseOutCubic,
            EaseInOutCubic,
            EaseInBack,
            EaseOutBack,
            EaseInOutBack,
            EaseOutBounce
        }

        public static float Ease(float t, EasingType easing) =>
            easing switch
            {
                EasingType.Linear => t,
                EasingType.EaseInQuad => t * t,
                EasingType.EaseOutQuad => 1 - (1 - t) * (1 - t),
                EasingType.EaseInOutQuad => t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t,
                EasingType.EaseInCubic => t * t * t,
                EasingType.EaseOutCubic => 1 - (1 - t) * (1 - t) * (1 - t),
                EasingType.EaseInOutCubic => t < 0.5f ? 4 * t * t * t : 1 + (t - 1) * (2 * (t - 2)) * (2 * (t - 2)),
                EasingType.EaseInBack => BackEaseIn(t),
                EasingType.EaseOutBack => EaseOutBack(t),
                EasingType.EaseInOutBack => t < 0.5f ? BackEaseIn(t * 2) / 2 : 1 - BackEaseIn((1 - t) * 2) / 2,
                EasingType.EaseOutBounce => EaseOutBounce(t),
                _ => t
            };

        private static float BackEaseIn(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;
            return c3 * t * t * t - c1 * t * t;
        }

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
        }

        public static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1 / d1)
                return n1 * t * t;
            else if (t < 2 / d1)
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            else if (t < 2.5 / d1)
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            else
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }

        #endregion
    }
}
