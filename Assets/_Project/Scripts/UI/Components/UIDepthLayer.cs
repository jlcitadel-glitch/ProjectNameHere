using UnityEngine;
using UnityEngine.UI;

namespace ProjectName.UI
{
    /// <summary>
    /// Adds visual depth to UI panels via inner shadow and gradient overlays.
    /// Creates two child Images behind content: rectangular inner shadow (dark edges)
    /// and a subtle vertical gradient (top darker).
    /// </summary>
    public class UIDepthLayer : MonoBehaviour
    {
        [SerializeField] private float innerShadowIntensity = 0.3f;
        [SerializeField] private float gradientIntensity = 0.15f;
        [SerializeField] private int shadowBorderWidth = 24;

        private Image shadowImage;
        private Image gradientImage;

        private void Awake()
        {
            CreateLayers();
        }

        private void CreateLayers()
        {
            // Inner shadow overlay (dark edges, transparent center)
            var shadowGo = new GameObject("InnerShadow", typeof(RectTransform));
            shadowGo.transform.SetParent(transform, false);
            shadowGo.transform.SetAsFirstSibling();
            StretchRect(shadowGo);

            shadowImage = shadowGo.AddComponent<Image>();
            var shadowTex = CreateInnerShadowTexture(128, 128, shadowBorderWidth);
            shadowImage.sprite = Sprite.Create(shadowTex,
                new Rect(0, 0, shadowTex.width, shadowTex.height),
                new Vector2(0.5f, 0.5f), 100f);
            shadowImage.type = Image.Type.Sliced;
            shadowImage.color = new Color(0f, 0f, 0f, innerShadowIntensity);
            shadowImage.raycastTarget = false;

            // Vertical gradient (top slightly darker)
            var gradGo = new GameObject("GradientOverlay", typeof(RectTransform));
            gradGo.transform.SetParent(transform, false);
            gradGo.transform.SetSiblingIndex(1);
            StretchRect(gradGo);

            gradientImage = gradGo.AddComponent<Image>();
            var gradTex = CreateVerticalGradientTexture(
                new Color(0f, 0f, 0f, 1f),
                new Color(0f, 0f, 0f, 0f));
            gradientImage.sprite = Sprite.Create(gradTex,
                new Rect(0, 0, gradTex.width, gradTex.height),
                new Vector2(0.5f, 0.5f), 100f);
            gradientImage.color = new Color(1f, 1f, 1f, gradientIntensity);
            gradientImage.raycastTarget = false;
        }

        /// <summary>
        /// Creates a rectangular vignette texture (dark edges, transparent center).
        /// </summary>
        public static Texture2D CreateInnerShadowTexture(int w, int h, int border)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float bw = border;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Distance from each edge, normalized to 0..1
                    float dLeft = x / bw;
                    float dRight = (w - 1 - x) / bw;
                    float dBottom = y / bw;
                    float dTop = (h - 1 - y) / bw;

                    float dMin = Mathf.Clamp01(Mathf.Min(dLeft, dRight, dBottom, dTop));
                    // Smooth falloff
                    float alpha = 1f - (dMin * dMin);
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
            }

            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        /// <summary>
        /// Creates a vertical gradient texture from top color to bottom color.
        /// </summary>
        public static Texture2D CreateVerticalGradientTexture(Color top, Color bottom)
        {
            var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
            {
                float t = y / 63f; // 0 at bottom, 1 at top
                tex.SetPixel(0, y, Color.Lerp(bottom, top, t));
            }
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        private static void StretchRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
