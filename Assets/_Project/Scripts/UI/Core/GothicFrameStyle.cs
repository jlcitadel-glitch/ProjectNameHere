using UnityEngine;

namespace ProjectName.UI
{
    /// <summary>
    /// Reusable style asset for 9-slice gothic borders.
    /// Use this to maintain consistent frame aesthetics across all UI panels.
    /// </summary>
    [CreateAssetMenu(fileName = "GothicFrameStyle", menuName = "UI/Gothic Frame Style")]
    public class GothicFrameStyle : ScriptableObject
    {
        [Header("Sprites")]
        [Tooltip("Main 9-sliced ornate border sprite")]
        public Sprite frameSprite;

        [Tooltip("Decorative corner accent pieces (optional)")]
        public Sprite cornerAccent;

        [Tooltip("Ornate horizontal divider line")]
        public Sprite dividerLine;

        [Tooltip("Inner glow/shadow sprite for depth")]
        public Sprite innerShadow;

        [Header("9-Slice Border Settings")]
        [Tooltip("Border values for 9-slice: Left, Bottom, Right, Top")]
        public Vector4 border = new Vector4(32, 32, 32, 32);

        [Tooltip("Pixels per unit for proper scaling")]
        public float pixelsPerUnit = 100f;

        [Header("Colors")]
        [Tooltip("Frame tint color (aged gold default)")]
        public Color frameColor = new Color(0.812f, 0.710f, 0.231f, 1f);

        [Tooltip("Inner shadow/glow color")]
        public Color shadowColor = new Color(0f, 0f, 0f, 0.5f);

        [Tooltip("Highlight color for selected state")]
        public Color selectedColor = new Color(0.25f, 0.88f, 0.82f, 0.8f);

        [Header("Animation")]
        [Tooltip("Enable subtle pulse animation on frame")]
        public bool enablePulse = false;

        [Tooltip("Speed of pulse animation")]
        public float pulseSpeed = 1f;

        [Tooltip("Intensity of pulse (0-1)")]
        [Range(0f, 1f)]
        public float pulseIntensity = 0.1f;

        [Header("Corner Decorations")]
        [Tooltip("Show decorative corner accents")]
        public bool showCornerAccents = true;

        [Tooltip("Corner accent offset from frame edges")]
        public Vector2 cornerOffset = new Vector2(4f, 4f);

        [Tooltip("Corner accent scale")]
        public float cornerScale = 1f;

        /// <summary>
        /// Gets the pulsed frame color based on time.
        /// </summary>
        public Color GetPulsedColor(float time)
        {
            if (!enablePulse)
                return frameColor;

            float pulse = Mathf.Sin(time * pulseSpeed * Mathf.PI * 2f) * 0.5f + 0.5f;
            float intensity = 1f + (pulse * pulseIntensity);

            return new Color(
                Mathf.Clamp01(frameColor.r * intensity),
                Mathf.Clamp01(frameColor.g * intensity),
                Mathf.Clamp01(frameColor.b * intensity),
                frameColor.a
            );
        }

        /// <summary>
        /// Creates a lerped color between normal and selected states.
        /// </summary>
        public Color GetSelectionColor(float t)
        {
            return Color.Lerp(frameColor, selectedColor, t);
        }
    }
}
