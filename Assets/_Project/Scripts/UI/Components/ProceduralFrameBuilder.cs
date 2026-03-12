using UnityEngine;
using UnityEngine.UI;

namespace ProjectName.UI
{
    /// <summary>
    /// Builds gothic ornate borders from UGUI Image primitives.
    /// Creates aged gold edge bars, corner accents, and inner glow — all procedural.
    /// Designed to be replaceable with real 9-slice sprites via GothicFrameStyle later.
    /// </summary>
    public static class ProceduralFrameBuilder
    {
        private static readonly Color DefaultBorderColor = new Color(0.812f, 0.710f, 0.231f, 1f); // Aged gold
        private static readonly Color DefaultGlowColor = new Color(0f, 0.808f, 0.820f, 0.07f);    // Spectral cyan, faint
        private static readonly Color DefaultCornerColor = new Color(0.9f, 0.8f, 0.3f, 1f);        // Brighter gold

        private static Sprite _pixel;
        private static Sprite Pixel
        {
            get
            {
                if (_pixel == null)
                {
                    var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    _pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
                }
                return _pixel;
            }
        }

        /// <summary>
        /// Applies a procedural gothic frame to the target RectTransform.
        /// Creates edge bars, corners, and inner glow as child Images.
        /// </summary>
        public static void ApplyFrame(RectTransform target, Color? borderColor = null, Color? glowColor = null)
        {
            if (target == null) return;

            Color border = borderColor ?? DefaultBorderColor;
            Color glow = glowColor ?? DefaultGlowColor;
            Color corner = borderColor.HasValue
                ? new Color(
                    Mathf.Min(border.r * 1.15f, 1f),
                    Mathf.Min(border.g * 1.15f, 1f),
                    Mathf.Min(border.b * 1.15f, 1f), border.a)
                : DefaultCornerColor;

            var frameRoot = new GameObject("GothicFrame", typeof(RectTransform));
            frameRoot.transform.SetParent(target, false);
            Stretch(frameRoot);
            // Push behind content but above background
            frameRoot.transform.SetAsFirstSibling();

            float barThickness = 2f;
            float glowThickness = 1f;
            float glowInset = barThickness + 1f;
            float cornerSize = 8f;

            // --- Gold edge bars ---
            CreateEdge(frameRoot.transform, "TopBar", border, barThickness,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -barThickness));
            CreateEdge(frameRoot.transform, "BottomBar", border, barThickness,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, barThickness));
            CreateEdge(frameRoot.transform, "LeftBar", border, barThickness,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(barThickness, 0));
            CreateEdge(frameRoot.transform, "RightBar", border, barThickness,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(-barThickness, 0));

            // --- Inner glow bars (spectral cyan, faint, inset from gold) ---
            CreateInnerGlow(frameRoot.transform, "TopGlow", glow, glowThickness, glowInset, true);
            CreateInnerGlow(frameRoot.transform, "BottomGlow", glow, glowThickness, glowInset, true, bottom: true);
            CreateInnerGlow(frameRoot.transform, "LeftGlow", glow, glowThickness, glowInset, false);
            CreateInnerGlow(frameRoot.transform, "RightGlow", glow, glowThickness, glowInset, false, right: true);

            // --- Corner accents (L-shape squares at each corner) ---
            CreateCorner(frameRoot.transform, "TopLeftCorner", corner, cornerSize, 0, 1);
            CreateCorner(frameRoot.transform, "TopRightCorner", corner, cornerSize, 1, 1);
            CreateCorner(frameRoot.transform, "BottomLeftCorner", corner, cornerSize, 0, 0);
            CreateCorner(frameRoot.transform, "BottomRightCorner", corner, cornerSize, 1, 0);
        }

        /// <summary>
        /// Creates an enhanced double-line divider: gold line + 1px gap + faint cyan line.
        /// </summary>
        public static GameObject CreateDivider(Transform parent, bool horizontal, Color? goldColor = null, Color? cyanColor = null)
        {
            Color gold = goldColor ?? new Color(0.812f, 0.710f, 0.231f, 0.3f);
            Color cyan = cyanColor ?? new Color(0f, 0.808f, 0.820f, 0.08f);

            var container = new GameObject("GothicDivider", typeof(RectTransform));
            container.transform.SetParent(parent, false);

            var le = container.AddComponent<LayoutElement>();
            if (horizontal)
            {
                le.preferredHeight = 4;
                le.flexibleWidth = 1;
            }
            else
            {
                le.preferredWidth = 4;
                le.flexibleHeight = 1;
            }

            // Gold line
            var line1 = new GameObject("GoldLine", typeof(RectTransform));
            line1.transform.SetParent(container.transform, false);
            var img1 = line1.AddComponent<Image>();
            img1.sprite = Pixel;
            img1.color = gold;
            img1.raycastTarget = false;
            var rt1 = line1.GetComponent<RectTransform>();

            // Cyan line
            var line2 = new GameObject("CyanLine", typeof(RectTransform));
            line2.transform.SetParent(container.transform, false);
            var img2 = line2.AddComponent<Image>();
            img2.sprite = Pixel;
            img2.color = cyan;
            img2.raycastTarget = false;
            var rt2 = line2.GetComponent<RectTransform>();

            if (horizontal)
            {
                // Gold line at top, cyan below with 1px gap
                rt1.anchorMin = new Vector2(0, 1);
                rt1.anchorMax = new Vector2(1, 1);
                rt1.pivot = new Vector2(0.5f, 1);
                rt1.sizeDelta = new Vector2(0, 1);
                rt1.anchoredPosition = Vector2.zero;

                rt2.anchorMin = new Vector2(0, 1);
                rt2.anchorMax = new Vector2(1, 1);
                rt2.pivot = new Vector2(0.5f, 1);
                rt2.sizeDelta = new Vector2(0, 1);
                rt2.anchoredPosition = new Vector2(0, -2);
            }
            else
            {
                // Gold line at left, cyan to the right with 1px gap
                rt1.anchorMin = new Vector2(0, 0);
                rt1.anchorMax = new Vector2(0, 1);
                rt1.pivot = new Vector2(0, 0.5f);
                rt1.sizeDelta = new Vector2(1, 0);
                rt1.anchoredPosition = Vector2.zero;

                rt2.anchorMin = new Vector2(0, 0);
                rt2.anchorMax = new Vector2(0, 1);
                rt2.pivot = new Vector2(0, 0.5f);
                rt2.sizeDelta = new Vector2(1, 0);
                rt2.anchoredPosition = new Vector2(2, 0);
            }

            return container;
        }

        private static void CreateEdge(Transform parent, string name, Color color, float thickness,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = Pixel;
            img.color = color;
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void CreateInnerGlow(Transform parent, string name, Color color,
            float thickness, float inset, bool horizontal, bool bottom = false, bool right = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = Pixel;
            img.color = color;
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();

            if (horizontal)
            {
                float yAnchor = bottom ? 0f : 1f;
                rt.anchorMin = new Vector2(0, yAnchor);
                rt.anchorMax = new Vector2(1, yAnchor);
                rt.pivot = new Vector2(0.5f, bottom ? 0f : 1f);
                rt.sizeDelta = new Vector2(-inset * 2f, thickness);
                rt.anchoredPosition = new Vector2(0, bottom ? inset : -inset);
            }
            else
            {
                float xAnchor = right ? 1f : 0f;
                rt.anchorMin = new Vector2(xAnchor, 0);
                rt.anchorMax = new Vector2(xAnchor, 1);
                rt.pivot = new Vector2(right ? 1f : 0f, 0.5f);
                rt.sizeDelta = new Vector2(thickness, -inset * 2f);
                rt.anchoredPosition = new Vector2(right ? -inset : inset, 0);
            }
        }

        private static void CreateCorner(Transform parent, string name, Color color, float size, float xAnchor, float yAnchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = Pixel;
            img.color = color;
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xAnchor, yAnchor);
            rt.anchorMax = new Vector2(xAnchor, yAnchor);
            rt.pivot = new Vector2(xAnchor, yAnchor);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
