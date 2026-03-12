using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectName.UI
{
    /// <summary>
    /// Adds a focus-driven spectral glow effect behind a UI element.
    /// Creates a child Image that pulses when the element is selected/hovered.
    /// </summary>
    public class UISpectralGlow : MonoBehaviour,
        ISelectHandler, IDeselectHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Color glowColor = new Color(0f, 0.808f, 0.820f, 0.15f);
        [SerializeField] private float glowPadding = 6f;
        [SerializeField] private float pulseMinAlpha = 0.05f;
        [SerializeField] private float pulseMaxAlpha = 0.15f;
        [SerializeField] private float pulsePeriod = 1f;
        [SerializeField] private float fadeOutDuration = 0.15f;

        private Image glowImage;
        private Coroutine activeCoroutine;
        private bool isFocused;

        private void Awake()
        {
            CreateGlowImage();
        }

        private void CreateGlowImage()
        {
            var go = new GameObject("SpectralGlow", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            // Push behind all other children
            go.transform.SetAsFirstSibling();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-glowPadding, -glowPadding);
            rt.offsetMax = new Vector2(glowPadding, glowPadding);

            glowImage = go.AddComponent<Image>();

            // Use a simple white sprite
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            glowImage.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);

            glowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
            glowImage.raycastTarget = false;
        }

        public void OnSelect(BaseEventData eventData) => SetFocused(true);
        public void OnDeselect(BaseEventData eventData) => SetFocused(false);
        public void OnPointerEnter(PointerEventData eventData) => SetFocused(true);
        public void OnPointerExit(PointerEventData eventData) => SetFocused(false);

        /// <summary>
        /// Programmatically activate/deactivate the glow (for non-EventSystem use).
        /// </summary>
        public void SetFocused(bool focused)
        {
            if (focused == isFocused) return;
            isFocused = focused;

            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);

            if (!gameObject.activeInHierarchy) return;

            if (isFocused)
                activeCoroutine = StartCoroutine(PulseCoroutine());
            else
                activeCoroutine = StartCoroutine(FadeOutCoroutine());
        }

        private IEnumerator PulseCoroutine()
        {
            float t = 0f;
            while (isFocused)
            {
                t += Time.unscaledDeltaTime / pulsePeriod;
                float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f);
                SetGlowAlpha(alpha);
                yield return null;
            }
        }

        private IEnumerator FadeOutCoroutine()
        {
            float startAlpha = glowImage != null ? glowImage.color.a : 0f;
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                SetGlowAlpha(Mathf.Lerp(startAlpha, 0f, t));
                yield return null;
            }

            SetGlowAlpha(0f);
        }

        private void SetGlowAlpha(float alpha)
        {
            if (glowImage == null) return;
            glowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
        }
    }
}
