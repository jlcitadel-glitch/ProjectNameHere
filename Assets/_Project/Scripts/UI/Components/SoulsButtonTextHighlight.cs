using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Changes button text to aged gold when highlighted or selected,
    /// and back to dim white when not. Souls-like text-only button effect.
    /// Enhanced with scale punch and optional background glow for depth.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SoulsButtonTextHighlight : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private static readonly Color NormalColor = new Color(0.93f, 0.89f, 0.82f, 1f);
        private static readonly Color HighlightColor = new Color(0.81f, 0.71f, 0.23f, 1f);
        private static readonly Color GlowColor = new Color(0.81f, 0.71f, 0.23f, 0.12f);

        [SerializeField] private bool enableScalePunch = true;
        [SerializeField] private bool enableGlow = true;

        private TMP_Text label;
        private Image backgroundGlow;
        private Vector3 originalScale;
        private bool isHighlighted;
        private bool isSelected;
        private Coroutine scaleCoroutine;
        private Coroutine glowCoroutine;

        private void Awake()
        {
            label = GetComponentInChildren<TMP_Text>();
            originalScale = transform.localScale;

            // Find sibling/child Image for background glow (not the button's own image)
            if (enableGlow)
            {
                var images = GetComponentsInChildren<Image>();
                foreach (var img in images)
                {
                    if (img.gameObject != gameObject && img.GetComponent<Button>() == null)
                    {
                        backgroundGlow = img;
                        break;
                    }
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHighlighted = true;
            UpdateVisuals(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHighlighted = false;
            UpdateVisuals(isSelected);
        }

        public void OnSelect(BaseEventData eventData)
        {
            isSelected = true;
            UpdateVisuals(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            UpdateVisuals(isHighlighted);
        }

        private void UpdateVisuals(bool active)
        {
            if (label != null)
                label.color = active ? HighlightColor : NormalColor;

            if (enableScalePunch)
            {
                if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
                if (gameObject.activeInHierarchy)
                {
                    float targetScale = active ? 1.05f : 1f;
                    scaleCoroutine = StartCoroutine(LerpScale(targetScale, 0.1f));
                }
            }

            if (enableGlow && backgroundGlow != null)
            {
                if (glowCoroutine != null) StopCoroutine(glowCoroutine);
                if (gameObject.activeInHierarchy)
                {
                    float targetAlpha = active ? GlowColor.a : 0f;
                    glowCoroutine = StartCoroutine(LerpGlow(targetAlpha, 0.1f));
                }
            }
        }

        private IEnumerator LerpScale(float targetMultiplier, float duration)
        {
            Vector3 start = transform.localScale;
            Vector3 target = originalScale * targetMultiplier;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // EaseOutBack for satisfying overshoot on scale-up
                if (targetMultiplier > 1f)
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    t = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                }
                transform.localScale = Vector3.LerpUnclamped(start, target, t);
                yield return null;
            }
            transform.localScale = target;
        }

        private IEnumerator LerpGlow(float targetAlpha, float duration)
        {
            Color c = backgroundGlow.color;
            float startAlpha = c.a;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                c = new Color(GlowColor.r, GlowColor.g, GlowColor.b, Mathf.Lerp(startAlpha, targetAlpha, t));
                backgroundGlow.color = c;
                yield return null;
            }
            backgroundGlow.color = new Color(GlowColor.r, GlowColor.g, GlowColor.b, targetAlpha);
        }
    }
}
