using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Generic reusable bar component for HP/MP/XP displays.
    /// Supports gradient colors, smooth animation, and low-resource pulse effects.
    /// </summary>
    public class ResourceBarDisplay : MonoBehaviour
    {
        public enum ResourceType
        {
            Health,
            Mana,
            Experience,
            Custom
        }

        [Header("References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text valueLabel;

        [Header("Configuration")]
        [SerializeField] private ResourceType resourceType = ResourceType.Health;
        [SerializeField] private string labelFormat = "{0}/{1}";
        [SerializeField] private bool showAsPercent = false;

        [Header("Style")]
        [SerializeField] private UIStyleGuide styleGuide;
        [SerializeField] private bool useGradient = true;
        [SerializeField] private Color customFillColor = Color.white;
        [SerializeField] private Color customBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        [Header("Animation")]
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private float lowResourceThreshold = 0.25f;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseIntensity = 0.3f;
        [SerializeField] private bool enableLowPulse = true;

        private float displayedValue;
        private float targetValue;
        private float currentMax;
        private Gradient resourceGradient;
        private bool isPulsing;
        private float pulseTimer;

        public float DisplayedValue => displayedValue;
        public float TargetValue => targetValue;
        public float CurrentMax => currentMax;

        /// <summary>
        /// Event fired when the bar value changes.
        /// </summary>
        public event Action<float, float> OnValueChanged;

        private void Start()
        {
            InitializeStyle();
        }

        private void Update()
        {
            UpdateBarSmooth();
            UpdatePulse();
        }

        private void InitializeStyle()
        {
            if (styleGuide == null)
            {
                styleGuide = UIManager.Instance?.StyleGuide;
            }

            if (styleGuide != null)
            {
                switch (resourceType)
                {
                    case ResourceType.Health:
                        resourceGradient = styleGuide.GetHealthGradient();
                        if (backgroundImage != null)
                            backgroundImage.color = styleGuide.charcoal;
                        break;

                    case ResourceType.Mana:
                        resourceGradient = styleGuide.GetSoulMeterGradient();
                        if (backgroundImage != null)
                            backgroundImage.color = styleGuide.charcoal;
                        break;

                    case ResourceType.Experience:
                        resourceGradient = CreateExpGradient();
                        if (backgroundImage != null)
                            backgroundImage.color = styleGuide.charcoal;
                        break;

                    case ResourceType.Custom:
                        resourceGradient = null;
                        if (backgroundImage != null)
                            backgroundImage.color = customBackgroundColor;
                        if (fillImage != null)
                            fillImage.color = customFillColor;
                        break;
                }
            }
            else
            {
                CreateDefaultGradient();
            }
        }

        private void CreateDefaultGradient()
        {
            resourceGradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(customFillColor * 0.5f, 0f);
            colorKeys[1] = new GradientColorKey(customFillColor, 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            resourceGradient.SetKeys(colorKeys, alphaKeys);
        }

        private Gradient CreateExpGradient()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(styleGuide.agedGold * 0.6f, 0f);
            colorKeys[1] = new GradientColorKey(styleGuide.agedGold, 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        /// <summary>
        /// Sets the bar value with current and max amounts.
        /// </summary>
        public void SetValue(float current, float max)
        {
            currentMax = max;
            float previousTarget = targetValue;
            targetValue = max > 0f ? current / max : 0f;

            if (enableLowPulse)
            {
                isPulsing = targetValue <= lowResourceThreshold && targetValue > 0f;
            }

            UpdateLabel(current, max);
            OnValueChanged?.Invoke(current, max);
        }

        /// <summary>
        /// Sets the bar value as a percentage (0-1).
        /// </summary>
        public void SetPercent(float percent)
        {
            targetValue = Mathf.Clamp01(percent);

            if (enableLowPulse)
            {
                isPulsing = targetValue <= lowResourceThreshold && targetValue > 0f;
            }

            if (showAsPercent && valueLabel != null)
            {
                valueLabel.text = $"{Mathf.RoundToInt(percent * 100)}%";
            }
        }

        /// <summary>
        /// Immediately sets the bar without animation.
        /// </summary>
        public void SetValueImmediate(float current, float max)
        {
            currentMax = max;
            targetValue = max > 0f ? current / max : 0f;
            displayedValue = targetValue;

            if (fillImage != null)
            {
                fillImage.fillAmount = displayedValue;
                UpdateFillColor(displayedValue);
            }

            UpdateLabel(current, max);
        }

        private void UpdateLabel(float current, float max)
        {
            if (valueLabel == null)
                return;

            if (showAsPercent)
            {
                valueLabel.text = $"{Mathf.RoundToInt((current / max) * 100)}%";
            }
            else
            {
                valueLabel.text = string.Format(labelFormat, Mathf.CeilToInt(current), Mathf.CeilToInt(max));
            }
        }

        private void UpdateBarSmooth()
        {
            if (Mathf.Approximately(displayedValue, targetValue))
                return;

            // Use unscaledDeltaTime so bar animates even when time is paused (e.g. stat menu open)
            displayedValue = Mathf.Lerp(displayedValue, targetValue, smoothSpeed * Time.unscaledDeltaTime);

            if (Mathf.Abs(displayedValue - targetValue) < 0.001f)
            {
                displayedValue = targetValue;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = displayedValue;
                UpdateFillColor(displayedValue);
            }
        }

        private void UpdateFillColor(float percent)
        {
            if (fillImage == null)
                return;

            if (useGradient && resourceGradient != null)
            {
                fillImage.color = resourceGradient.Evaluate(percent);
            }
        }

        private void UpdatePulse()
        {
            if (!isPulsing || fillImage == null)
                return;

            pulseTimer += Time.unscaledDeltaTime * pulseSpeed;
            float pulse = Mathf.Sin(pulseTimer * Mathf.PI * 2f) * pulseIntensity;

            Color baseColor = useGradient && resourceGradient != null
                ? resourceGradient.Evaluate(displayedValue)
                : customFillColor;

            fillImage.color = new Color(
                Mathf.Clamp01(baseColor.r + pulse),
                Mathf.Clamp01(baseColor.g + pulse * 0.5f),
                Mathf.Clamp01(baseColor.b + pulse * 0.5f),
                baseColor.a
            );
        }

        /// <summary>
        /// Plays a flash effect on the bar (e.g., for XP gain).
        /// </summary>
        public void PlayFlashEffect()
        {
            StartCoroutine(FlashCoroutine());
        }

        private System.Collections.IEnumerator FlashCoroutine()
        {
            if (fillImage == null)
                yield break;

            Color originalColor = fillImage.color;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float brightness = 1f + Mathf.Sin(t * Mathf.PI) * 0.5f;
                fillImage.color = originalColor * brightness;
                yield return null;
            }

            UpdateFillColor(displayedValue);
        }

        /// <summary>
        /// Wires internal references for runtime-created bars.
        /// Call before Start() runs or before SetValue/SetValueImmediate.
        /// </summary>
        public void SetReferences(Image fill, Image background, TMP_Text label)
        {
            fillImage = fill;
            backgroundImage = background;
            valueLabel = label;

            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
            }
        }

        /// <summary>
        /// Configures the bar for runtime creation, preventing InitializeStyle from overriding colors.
        /// Must be called before Start() runs.
        /// </summary>
        public void ConfigureForRuntime(Color fillColor, Color bgColor)
        {
            resourceType = ResourceType.Custom;
            useGradient = false;
            customFillColor = fillColor;
            customBackgroundColor = bgColor;

            if (fillImage != null)
                fillImage.color = fillColor;
            if (backgroundImage != null)
                backgroundImage.color = bgColor;
        }

        /// <summary>
        /// Sets the label format string. Use {0} for current and {1} for max.
        /// Example: "HP {0}/{1}" displays as "HP 100/100".
        /// </summary>
        public void SetLabelFormat(string format)
        {
            labelFormat = format;
        }

        /// <summary>
        /// Sets custom colors for the bar.
        /// </summary>
        public void SetCustomColors(Color fill, Color background)
        {
            customFillColor = fill;
            customBackgroundColor = background;

            if (fillImage != null)
                fillImage.color = fill;
            if (backgroundImage != null)
                backgroundImage.color = background;
        }

        /// <summary>
        /// Sets a custom gradient for the fill.
        /// </summary>
        public void SetGradient(Gradient gradient)
        {
            resourceGradient = gradient;
            useGradient = true;
            UpdateFillColor(displayedValue);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (fillImage != null && !Application.isPlaying)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
            }
        }
#endif
    }
}
