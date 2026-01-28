using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ProjectName.UI
{
    /// <summary>
    /// Handles animated menu transitions with gothic visual effects.
    /// Uses DOTween for smooth, customizable animations.
    /// </summary>
    public class MenuTransitions : MonoBehaviour
    {
        [Header("Transition Settings")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float slideDuration = 0.4f;
        [SerializeField] private float slideDistance = 50f;
        [SerializeField] private Ease openEase = Ease.OutQuart;
        [SerializeField] private Ease closeEase = Ease.InQuart;

        [Header("Gothic Effects")]
        [SerializeField] private Image vignetteOverlay;
        [SerializeField] private float vignetteIntensity = 0.4f;

        [Header("Spectral Effects")]
        [SerializeField] private Color spectralGlowColor = new Color(0.25f, 0.88f, 0.82f, 0.8f);
        [SerializeField] private float spectralPulseSpeed = 0.5f;

        private Sequence currentSequence;

        private void OnDestroy()
        {
            currentSequence?.Kill();
        }

        /// <summary>
        /// Opens a menu panel with slide and fade animation.
        /// </summary>
        public void OpenMenu(CanvasGroup menuGroup, RectTransform panel, Action onComplete = null)
        {
            if (menuGroup == null)
                return;

            currentSequence?.Kill();
            menuGroup.gameObject.SetActive(true);
            menuGroup.alpha = 0f;
            menuGroup.interactable = false;
            menuGroup.blocksRaycasts = false;

            if (panel != null)
            {
                panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, -slideDistance);
            }

            currentSequence = DOTween.Sequence();

            // Fade in
            currentSequence.Append(menuGroup.DOFade(1f, fadeDuration).SetEase(openEase));

            // Slide up
            if (panel != null)
            {
                currentSequence.Join(
                    panel.DOAnchorPosY(0f, slideDuration).SetEase(openEase)
                );
            }

            // Vignette effect
            if (vignetteOverlay != null)
            {
                Color startColor = vignetteOverlay.color;
                startColor.a = 0f;
                vignetteOverlay.color = startColor;
                vignetteOverlay.gameObject.SetActive(true);

                currentSequence.Join(
                    vignetteOverlay.DOFade(vignetteIntensity, fadeDuration).SetEase(openEase)
                );
            }

            currentSequence.OnComplete(() =>
            {
                menuGroup.interactable = true;
                menuGroup.blocksRaycasts = true;
                onComplete?.Invoke();
            });

            currentSequence.SetUpdate(true); // Ignore time scale
        }

        /// <summary>
        /// Closes a menu panel with slide and fade animation.
        /// </summary>
        public void CloseMenu(CanvasGroup menuGroup, RectTransform panel, Action onComplete = null)
        {
            if (menuGroup == null)
                return;

            currentSequence?.Kill();
            menuGroup.interactable = false;
            menuGroup.blocksRaycasts = false;

            currentSequence = DOTween.Sequence();

            // Fade out
            currentSequence.Append(menuGroup.DOFade(0f, fadeDuration).SetEase(closeEase));

            // Slide down
            if (panel != null)
            {
                currentSequence.Join(
                    panel.DOAnchorPosY(-slideDistance, slideDuration).SetEase(closeEase)
                );
            }

            // Vignette effect
            if (vignetteOverlay != null)
            {
                currentSequence.Join(
                    vignetteOverlay.DOFade(0f, fadeDuration).SetEase(closeEase)
                );
            }

            currentSequence.OnComplete(() =>
            {
                menuGroup.gameObject.SetActive(false);
                if (vignetteOverlay != null)
                {
                    vignetteOverlay.gameObject.SetActive(false);
                }
                onComplete?.Invoke();
            });

            currentSequence.SetUpdate(true); // Ignore time scale
        }

        /// <summary>
        /// Cross-fades between two menu panels.
        /// </summary>
        public void CrossFade(CanvasGroup fromGroup, CanvasGroup toGroup, Action onComplete = null)
        {
            currentSequence?.Kill();

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

            currentSequence = DOTween.Sequence();

            if (fromGroup != null)
            {
                currentSequence.Append(fromGroup.DOFade(0f, fadeDuration).SetEase(closeEase));
            }

            if (toGroup != null)
            {
                currentSequence.Append(toGroup.DOFade(1f, fadeDuration).SetEase(openEase));
            }

            currentSequence.OnComplete(() =>
            {
                if (fromGroup != null)
                {
                    fromGroup.gameObject.SetActive(false);
                }
                if (toGroup != null)
                {
                    toGroup.interactable = true;
                    toGroup.blocksRaycasts = true;
                }
                onComplete?.Invoke();
            });

            currentSequence.SetUpdate(true);
        }

        /// <summary>
        /// Applies a Soul Reaver-style spectral pulse effect to an image.
        /// </summary>
        public Tween SpectralPulse(Image element, bool loop = true)
        {
            if (element == null)
                return null;

            Color originalColor = element.color;

            var tween = element.DOColor(spectralGlowColor, spectralPulseSpeed)
                .SetEase(Ease.InOutSine);

            if (loop)
            {
                tween.SetLoops(-1, LoopType.Yoyo);
            }

            return tween;
        }

        /// <summary>
        /// Stops the spectral pulse and resets the element color.
        /// </summary>
        public void StopSpectralPulse(Image element, Color originalColor)
        {
            if (element == null)
                return;

            element.DOKill();
            element.color = originalColor;
        }

        /// <summary>
        /// Creates a scale punch effect for button feedback.
        /// </summary>
        public void PunchScale(Transform target, float intensity = 0.1f, float duration = 0.2f)
        {
            if (target == null)
                return;

            target.DOPunchScale(Vector3.one * intensity, duration, 2, 0.5f)
                .SetUpdate(true);
        }

        /// <summary>
        /// Creates a shake effect for error feedback.
        /// </summary>
        public void ShakePosition(RectTransform target, float intensity = 10f, float duration = 0.3f)
        {
            if (target == null)
                return;

            target.DOShakeAnchorPos(duration, intensity, 10, 90f, false, true)
                .SetUpdate(true);
        }

        /// <summary>
        /// Animates an element appearing with scale and fade.
        /// </summary>
        public void PopIn(CanvasGroup group, Transform target, float duration = 0.2f)
        {
            if (group == null || target == null)
                return;

            group.alpha = 0f;
            target.localScale = Vector3.one * 0.8f;

            DOTween.Sequence()
                .Append(group.DOFade(1f, duration).SetEase(Ease.OutQuad))
                .Join(target.DOScale(Vector3.one, duration).SetEase(Ease.OutBack))
                .SetUpdate(true);
        }

        /// <summary>
        /// Animates an element disappearing with scale and fade.
        /// </summary>
        public void PopOut(CanvasGroup group, Transform target, float duration = 0.15f, Action onComplete = null)
        {
            if (group == null || target == null)
                return;

            DOTween.Sequence()
                .Append(group.DOFade(0f, duration).SetEase(Ease.InQuad))
                .Join(target.DOScale(Vector3.one * 0.8f, duration).SetEase(Ease.InBack))
                .OnComplete(() => onComplete?.Invoke())
                .SetUpdate(true);
        }
    }
}
