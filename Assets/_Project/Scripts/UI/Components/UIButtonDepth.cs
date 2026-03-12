using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectName.UI
{
    /// <summary>
    /// Adds scale/depth feedback to UI buttons, slots, and interactive elements.
    /// Hover/select: scale up. Press: push-in. Release: settle back.
    /// Uses coroutines with unscaled time for pause-safe animation.
    /// </summary>
    public class UIButtonDepth : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float pressScale = 0.95f;
        [SerializeField] private float hoverDuration = 0.1f;
        [SerializeField] private float pressDuration = 0.05f;

        private Vector3 originalScale;
        private Coroutine scaleCoroutine;
        private bool isHovered;
        private bool isSelected;
        private bool isPressed;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            if (!isPressed) AnimateTo(hoverScale, hoverDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            if (!isPressed && !isSelected) AnimateTo(1f, hoverDuration);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            AnimateTo(pressScale, pressDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            float target = (isHovered || isSelected) ? hoverScale : 1f;
            AnimateTo(target, hoverDuration);
        }

        public void OnSelect(BaseEventData eventData)
        {
            isSelected = true;
            if (!isPressed) AnimateTo(hoverScale, hoverDuration);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            if (!isHovered && !isPressed) AnimateTo(1f, hoverDuration);
        }

        private void AnimateTo(float targetMultiplier, float duration)
        {
            if (scaleCoroutine != null)
                StopCoroutine(scaleCoroutine);

            if (!gameObject.activeInHierarchy) return;
            scaleCoroutine = StartCoroutine(ScaleCoroutine(originalScale * targetMultiplier, duration));
        }

        private IEnumerator ScaleCoroutine(Vector3 target, float duration)
        {
            Vector3 start = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // EaseOutBack for satisfying overshoot
                t = EaseOutBack(t);
                transform.localScale = Vector3.LerpUnclamped(start, target, t);
                yield return null;
            }

            transform.localScale = target;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
