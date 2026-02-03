using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectName.UI
{
    /// <summary>
    /// Handles animated menu transitions with gothic visual effects.
    /// Uses coroutines for smooth animations (DOTween-free).
    /// </summary>
    public class MenuTransitions : MonoBehaviour
    {
        [Header("Transition Settings")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float slideDuration = 0.4f;
        [SerializeField] private float slideDistance = 50f;

        [Header("Gothic Effects")]
        [SerializeField] private Image vignetteOverlay;
        [SerializeField] private float vignetteIntensity = 0.4f;

        [Header("Spectral Effects")]
        [SerializeField] private Color spectralGlowColor = new Color(0.25f, 0.88f, 0.82f, 0.8f);
        [SerializeField] private float spectralPulseSpeed = 0.5f;

        private Coroutine currentSequence;

        private void OnDestroy()
        {
            if (currentSequence != null)
                StopCoroutine(currentSequence);
        }

        /// <summary>
        /// Opens a menu panel with slide and fade animation.
        /// </summary>
        public void OpenMenu(CanvasGroup menuGroup, RectTransform panel, Action onComplete = null)
        {
            if (menuGroup == null)
                return;

            if (currentSequence != null)
                StopCoroutine(currentSequence);

            currentSequence = StartCoroutine(OpenMenuCoroutine(menuGroup, panel, onComplete));
        }

        private IEnumerator OpenMenuCoroutine(CanvasGroup menuGroup, RectTransform panel, Action onComplete)
        {
            menuGroup.gameObject.SetActive(true);
            menuGroup.alpha = 0f;
            menuGroup.interactable = false;
            menuGroup.blocksRaycasts = false;

            Vector2 startPos = Vector2.zero;
            if (panel != null)
            {
                startPos = panel.anchoredPosition;
                panel.anchoredPosition = new Vector2(startPos.x, -slideDistance);
            }

            if (vignetteOverlay != null)
            {
                Color startColor = vignetteOverlay.color;
                startColor.a = 0f;
                vignetteOverlay.color = startColor;
                vignetteOverlay.gameObject.SetActive(true);
            }

            float elapsed = 0f;
            float duration = Mathf.Max(fadeDuration, slideDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutQuart(t);

                // Fade
                float fadeT = Mathf.Clamp01(elapsed / fadeDuration);
                menuGroup.alpha = EaseOutQuart(fadeT);

                // Slide
                if (panel != null)
                {
                    float slideT = Mathf.Clamp01(elapsed / slideDuration);
                    float slideEased = EaseOutQuart(slideT);
                    panel.anchoredPosition = new Vector2(startPos.x, Mathf.Lerp(-slideDistance, 0f, slideEased));
                }

                // Vignette
                if (vignetteOverlay != null)
                {
                    float vignetteT = Mathf.Clamp01(elapsed / fadeDuration);
                    Color c = vignetteOverlay.color;
                    c.a = Mathf.Lerp(0f, vignetteIntensity, EaseOutQuart(vignetteT));
                    vignetteOverlay.color = c;
                }

                yield return null;
            }

            menuGroup.alpha = 1f;
            if (panel != null)
                panel.anchoredPosition = startPos;

            menuGroup.interactable = true;
            menuGroup.blocksRaycasts = true;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Closes a menu panel with slide and fade animation.
        /// </summary>
        public void CloseMenu(CanvasGroup menuGroup, RectTransform panel, Action onComplete = null)
        {
            if (menuGroup == null)
                return;

            if (currentSequence != null)
                StopCoroutine(currentSequence);

            currentSequence = StartCoroutine(CloseMenuCoroutine(menuGroup, panel, onComplete));
        }

        private IEnumerator CloseMenuCoroutine(CanvasGroup menuGroup, RectTransform panel, Action onComplete)
        {
            menuGroup.interactable = false;
            menuGroup.blocksRaycasts = false;

            Vector2 startPos = panel != null ? panel.anchoredPosition : Vector2.zero;

            float elapsed = 0f;
            float duration = Mathf.Max(fadeDuration, slideDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                // Fade
                float fadeT = Mathf.Clamp01(elapsed / fadeDuration);
                menuGroup.alpha = 1f - EaseInQuart(fadeT);

                // Slide
                if (panel != null)
                {
                    float slideT = Mathf.Clamp01(elapsed / slideDuration);
                    float slideEased = EaseInQuart(slideT);
                    panel.anchoredPosition = new Vector2(startPos.x, Mathf.Lerp(0f, -slideDistance, slideEased));
                }

                // Vignette
                if (vignetteOverlay != null)
                {
                    float vignetteT = Mathf.Clamp01(elapsed / fadeDuration);
                    Color c = vignetteOverlay.color;
                    c.a = Mathf.Lerp(vignetteIntensity, 0f, EaseInQuart(vignetteT));
                    vignetteOverlay.color = c;
                }

                yield return null;
            }

            menuGroup.alpha = 0f;
            menuGroup.gameObject.SetActive(false);

            if (vignetteOverlay != null)
                vignetteOverlay.gameObject.SetActive(false);

            onComplete?.Invoke();
        }

        /// <summary>
        /// Cross-fades between two menu panels.
        /// </summary>
        public void CrossFade(CanvasGroup fromGroup, CanvasGroup toGroup, Action onComplete = null)
        {
            if (currentSequence != null)
                StopCoroutine(currentSequence);

            currentSequence = StartCoroutine(CrossFadeCoroutine(fromGroup, toGroup, onComplete));
        }

        private IEnumerator CrossFadeCoroutine(CanvasGroup fromGroup, CanvasGroup toGroup, Action onComplete)
        {
            if (fromGroup != null)
            {
                fromGroup.interactable = false;
                fromGroup.blocksRaycasts = false;
            }

            if (toGroup != null)
            {
                toGroup.gameObject.SetActive(true);
                toGroup.alpha = 0f;
                toGroup.interactable = false;
                toGroup.blocksRaycasts = false;
            }

            // Fade out
            if (fromGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeDuration);
                    fromGroup.alpha = 1f - EaseInQuart(t);
                    yield return null;
                }
                fromGroup.alpha = 0f;
                fromGroup.gameObject.SetActive(false);
            }

            // Fade in
            if (toGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeDuration);
                    toGroup.alpha = EaseOutQuart(t);
                    yield return null;
                }
                toGroup.alpha = 1f;
                toGroup.interactable = true;
                toGroup.blocksRaycasts = true;
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Applies a Soul Reaver-style spectral pulse effect to an image.
        /// </summary>
        public Coroutine SpectralPulse(Image element, bool loop = true)
        {
            if (element == null)
                return null;

            return StartCoroutine(SpectralPulseCoroutine(element, loop));
        }

        private IEnumerator SpectralPulseCoroutine(Image element, bool loop)
        {
            Color originalColor = element.color;

            do
            {
                // Pulse to spectral color
                float elapsed = 0f;
                while (elapsed < spectralPulseSpeed)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / spectralPulseSpeed);
                    t = EaseInOutSine(t);
                    element.color = Color.Lerp(originalColor, spectralGlowColor, t);
                    yield return null;
                }

                // Pulse back
                elapsed = 0f;
                while (elapsed < spectralPulseSpeed)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / spectralPulseSpeed);
                    t = EaseInOutSine(t);
                    element.color = Color.Lerp(spectralGlowColor, originalColor, t);
                    yield return null;
                }
            } while (loop);
        }

        /// <summary>
        /// Stops the spectral pulse and resets the element color.
        /// </summary>
        public void StopSpectralPulse(Image element, Color originalColor)
        {
            if (element == null)
                return;

            StopAllCoroutines();
            element.color = originalColor;
        }

        /// <summary>
        /// Creates a scale punch effect for button feedback.
        /// </summary>
        public void PunchScale(Transform target, float intensity = 0.1f, float duration = 0.2f)
        {
            if (target == null)
                return;

            StartCoroutine(PunchScaleCoroutine(target, intensity, duration));
        }

        private IEnumerator PunchScaleCoroutine(Transform target, float intensity, float duration)
        {
            Vector3 originalScale = target.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                // Damped oscillation
                float damping = 1f - t;
                float oscillation = Mathf.Sin(t * Mathf.PI * 4f) * damping;

                target.localScale = originalScale + Vector3.one * (oscillation * intensity);
                yield return null;
            }

            target.localScale = originalScale;
        }

        /// <summary>
        /// Creates a shake effect for error feedback.
        /// </summary>
        public void ShakePosition(RectTransform target, float intensity = 10f, float duration = 0.3f)
        {
            if (target == null)
                return;

            StartCoroutine(ShakePositionCoroutine(target, intensity, duration));
        }

        private IEnumerator ShakePositionCoroutine(RectTransform target, float intensity, float duration)
        {
            Vector2 originalPos = target.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float damping = 1f - t;

                float offsetX = UnityEngine.Random.Range(-1f, 1f) * intensity * damping;
                float offsetY = UnityEngine.Random.Range(-1f, 1f) * intensity * damping * 0.5f;

                target.anchoredPosition = originalPos + new Vector2(offsetX, offsetY);
                yield return null;
            }

            target.anchoredPosition = originalPos;
        }

        /// <summary>
        /// Animates an element appearing with scale and fade.
        /// </summary>
        public void PopIn(CanvasGroup group, Transform target, float duration = 0.2f)
        {
            if (group == null || target == null)
                return;

            StartCoroutine(PopInCoroutine(group, target, duration));
        }

        private IEnumerator PopInCoroutine(CanvasGroup group, Transform target, float duration)
        {
            group.alpha = 0f;
            target.localScale = Vector3.one * 0.8f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                group.alpha = EaseOutQuad(t);
                target.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, EaseOutBack(t));

                yield return null;
            }

            group.alpha = 1f;
            target.localScale = Vector3.one;
        }

        /// <summary>
        /// Animates an element disappearing with scale and fade.
        /// </summary>
        public void PopOut(CanvasGroup group, Transform target, float duration = 0.15f, Action onComplete = null)
        {
            if (group == null || target == null)
                return;

            StartCoroutine(PopOutCoroutine(group, target, duration, onComplete));
        }

        private IEnumerator PopOutCoroutine(CanvasGroup group, Transform target, float duration, Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                group.alpha = 1f - EaseInQuad(t);
                target.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.8f, EaseInBack(t));

                yield return null;
            }

            group.alpha = 0f;
            target.localScale = Vector3.one * 0.8f;
            onComplete?.Invoke();
        }

        #region Easing Functions

        private float EaseOutQuart(float t) => 1f - Mathf.Pow(1f - t, 4f);
        private float EaseInQuart(float t) => t * t * t * t;
        private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        private float EaseInQuad(float t) => t * t;
        private float EaseInOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private float EaseInBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }

        #endregion
    }
}
