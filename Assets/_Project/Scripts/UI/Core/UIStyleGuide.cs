using UnityEngine;
using TMPro;
#if DOTWEEN
using DG.Tweening;
#endif

namespace ProjectName.UI
{
    /// <summary>
    /// Central source of truth for gothic UI styling.
    /// Defines colors, typography, spacing, and animation settings used throughout the UI.
    /// Inspired by Castlevania: SOTN and Legacy of Kain: Soul Reaver aesthetics.
    /// </summary>
    [CreateAssetMenu(fileName = "UIStyleGuide", menuName = "UI/Style Guide")]
    public class UIStyleGuide : ScriptableObject
    {
        [Header("Primary Colors")]
        [Tooltip("Deep crimson - primary accent color")]
        public Color deepCrimson = new Color(0.545f, 0f, 0f, 1f);           // #8B0000

        [Tooltip("Midnight blue - primary background accent")]
        public Color midnightBlue = new Color(0.098f, 0.098f, 0.439f, 1f);  // #191970

        [Header("Secondary Colors")]
        [Tooltip("Aged gold - frame borders, highlights")]
        public Color agedGold = new Color(0.812f, 0.710f, 0.231f, 1f);      // #CFB53B

        [Tooltip("Spectral cyan - Soul Reaver ethereal glow")]
        public Color spectralCyan = new Color(0f, 0.808f, 0.820f, 1f);      // #00CED1

        [Header("Background Colors")]
        [Tooltip("Charcoal - lighter backgrounds")]
        public Color charcoal = new Color(0.102f, 0.102f, 0.102f, 1f);      // #1a1a1a

        [Tooltip("Obsidian - darkest backgrounds")]
        public Color obsidian = new Color(0.051f, 0.051f, 0.051f, 1f);      // #0d0d0d

        [Header("Text Colors")]
        [Tooltip("Bone white - primary text")]
        public Color boneWhite = new Color(0.961f, 0.961f, 0.863f, 1f);     // #F5F5DC

        [Tooltip("Faded parchment - secondary text")]
        public Color fadedParchment = new Color(0.831f, 0.769f, 0.659f, 1f); // #D4C4A8

        [Header("Accent Colors")]
        [Tooltip("Blood red - warnings, damage")]
        public Color bloodRed = new Color(0.863f, 0.078f, 0.235f, 1f);      // #DC143C

        [Tooltip("Soul blue - magic, special abilities")]
        public Color soulBlue = new Color(0.255f, 0.412f, 0.882f, 1f);      // #4169E1

        [Header("Warning/Special Colors")]
        [Tooltip("Poisoned purple - status effects")]
        public Color poisonedPurple = new Color(0.600f, 0.196f, 0.800f, 1f); // #9932CC

        [Tooltip("Ethereal green - healing, positive effects")]
        public Color etherealGreen = new Color(0f, 1f, 0.498f, 1f);          // #00FF7F

        [Header("Typography - Fonts")]
        [Tooltip("Ornate serif for headers (Cinzel, Cormorant Garamond style)")]
        public TMP_FontAsset headerFont;

        [Tooltip("Clean serif for body text (Crimson Text, EB Garamond style)")]
        public TMP_FontAsset bodyFont;

        [Tooltip("Monospace for stats and numbers")]
        public TMP_FontAsset numbersFont;

        [Header("Typography - Sizes")]
        [Tooltip("Large header size")]
        public float headerSizeLarge = 48f;

        [Tooltip("Standard header size")]
        public float headerSize = 36f;

        [Tooltip("Body text size")]
        public float bodySize = 24f;

        [Tooltip("Small/caption text size")]
        public float smallSize = 18f;

        [Tooltip("Character spacing for elegant look")]
        public float characterSpacing = 2f;

        [Header("Spacing")]
        [Tooltip("Small padding (buttons, icons)")]
        public float paddingSmall = 8f;

        [Tooltip("Medium padding (panels, sections)")]
        public float paddingMedium = 16f;

        [Tooltip("Large padding (screens, major divisions)")]
        public float paddingLarge = 32f;

        [Tooltip("Spacing between UI elements")]
        public float elementSpacing = 12f;

        [Header("Animation Settings")]
        [Tooltip("Standard transition duration")]
        public float transitionDuration = 0.3f;

        [Tooltip("Fast transition (button hover, etc)")]
        public float transitionFast = 0.15f;

        [Tooltip("Slow transition (menu open/close)")]
        public float transitionSlow = 0.5f;

#if DOTWEEN
        [Tooltip("Default easing for UI animations")]
        public Ease defaultEase = Ease.OutQuart;

        [Tooltip("Easing for closing/exit animations")]
        public Ease exitEase = Ease.InQuart;
#endif

        [Header("Effects")]
        [Tooltip("Hover scale multiplier for buttons")]
        public float hoverScale = 1.05f;

        [Tooltip("Pulse animation speed for spectral effects")]
        public float pulseSpeed = 1f;

        [Tooltip("Pulse intensity for glowing elements")]
        public float pulseIntensity = 0.2f;

        [Tooltip("Vignette intensity when menus are open")]
        public float menuVignetteIntensity = 0.4f;

        /// <summary>
        /// Gets a gradient from empty (dark) to full (spectral cyan) for soul/magic meters.
        /// </summary>
        public Gradient GetSoulMeterGradient()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(midnightBlue, 0f);
            colorKeys[1] = new GradientColorKey(spectralCyan, 0.7f);
            colorKeys[2] = new GradientColorKey(Color.white, 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(0.8f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        /// <summary>
        /// Gets a gradient for health display (full to critical).
        /// </summary>
        public Gradient GetHealthGradient()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(bloodRed, 0f);        // Critical
            colorKeys[1] = new GradientColorKey(agedGold, 0.5f);      // Mid
            colorKeys[2] = new GradientColorKey(deepCrimson, 1f);     // Full

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        /// <summary>
        /// Configures a TextMeshPro component with header styling.
        /// </summary>
        public void ApplyHeaderStyle(TMP_Text text)
        {
            if (headerFont != null)
                text.font = headerFont;
            text.fontSize = headerSize;
            text.color = boneWhite;
            text.characterSpacing = characterSpacing;
        }

        /// <summary>
        /// Configures a TextMeshPro component with body text styling.
        /// </summary>
        public void ApplyBodyStyle(TMP_Text text)
        {
            if (bodyFont != null)
                text.font = bodyFont;
            text.fontSize = bodySize;
            text.color = fadedParchment;
        }

        /// <summary>
        /// Configures a TextMeshPro component with number/stat styling.
        /// </summary>
        public void ApplyNumberStyle(TMP_Text text)
        {
            if (numbersFont != null)
                text.font = numbersFont;
            text.fontSize = bodySize;
            text.color = boneWhite;
        }
    }
}
