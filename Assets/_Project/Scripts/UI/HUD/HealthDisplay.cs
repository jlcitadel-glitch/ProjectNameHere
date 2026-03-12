using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// HUD component that displays the player's health bar.
    /// Auto-finds the player and subscribes to HealthSystem events.
    /// </summary>
    public class HealthDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text healthLabel;

        [Header("Style")]
        [SerializeField] private UIStyleGuide styleGuide;
        [SerializeField] private bool useGradient = true;

        [Header("Animation")]
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private float lowHealthPulseThreshold = 0.25f;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseIntensity = 0.4f;

        [Header("Audio")]
        [SerializeField] private UISoundBank soundBank;
        [SerializeField] private AudioSource audioSource;

        private HealthSystem healthSystem;
        private float displayedHealth;
        private float targetHealth;
        private Gradient healthGradient;
        private bool isPulsing;
        private float pulseTimer;

        private bool visualsInitialized;

        [Header("Glow Flash")]
        [SerializeField] private float glowFlashDuration = 0.2f;
        [SerializeField] private float glowFlashAlpha = 0.3f;
        private Image glowFlashImage;
        private Coroutine glowFlashCoroutine;

        private void Start()
        {
            InitializeVisuals();
        }

        private void OnEnable()
        {
            EnsureReferences();
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void InitializeVisuals()
        {
            if (visualsInitialized) return;
            visualsInitialized = true;
            InitializeStyle();
            InitializeAudio();
            CreateGlowFlash();
        }

        private void EnsureReferences()
        {
            InitializeVisuals();
            if (healthSystem == null)
            {
                FindPlayerHealthSystem();
            }
        }

        private void SubscribeEvents()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += HandleHealthChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged -= HandleHealthChanged;
            }
        }

        private void Update()
        {
            UpdateBarSmooth();
            UpdatePulse();
        }

        private void FindPlayerHealthSystem()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                healthSystem = player.GetComponent<HealthSystem>();
                if (healthSystem != null)
                {
                    displayedHealth = healthSystem.HealthPercent;
                    targetHealth = displayedHealth;
                    UpdateBar(displayedHealth);
                    Debug.Log("[HealthDisplay] Connected to HealthSystem");
                }
                else
                {
                    Debug.LogWarning("[HealthDisplay] Player found but HealthSystem component missing");
                }
            }
            else
            {
                Debug.LogWarning("[HealthDisplay] Player not found. HealthDisplay will not update.");
            }
        }

        private void InitializeStyle()
        {
            if (styleGuide == null)
            {
                styleGuide = UIManager.Instance?.StyleGuide;
            }

            if (styleGuide != null)
            {
                healthGradient = styleGuide.GetHealthGradient();

                if (backgroundImage != null)
                {
                    backgroundImage.color = styleGuide.charcoal;
                }

                if (fillImage != null && !useGradient)
                {
                    fillImage.color = styleGuide.deepCrimson;
                }
            }
            else
            {
                CreateDefaultGradient();
            }
        }

        private void CreateDefaultGradient()
        {
            healthGradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(new Color(0.863f, 0.078f, 0.235f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.812f, 0.710f, 0.231f), 0.5f);
            colorKeys[2] = new GradientColorKey(new Color(0.545f, 0f, 0f), 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            healthGradient.SetKeys(colorKeys, alphaKeys);
        }

        private void InitializeAudio()
        {
            if (soundBank == null && UIManager.Instance != null)
            {
                soundBank = UIManager.Instance.SoundBank;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f;
                }
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            float previousTarget = targetHealth;
            targetHealth = max > 0f ? current / max : 0f;

            if (targetHealth > previousTarget && soundBank != null && soundBank.healthGain != null)
            {
                soundBank.PlaySound(soundBank.healthGain, audioSource, 0.5f);
            }
            else if (targetHealth < previousTarget && soundBank != null && soundBank.healthLoss != null)
            {
                soundBank.PlaySound(soundBank.healthLoss, audioSource, 0.5f);
            }

            isPulsing = targetHealth <= lowHealthPulseThreshold && targetHealth > 0f;

            // Flash glow on value change
            if (!Mathf.Approximately(targetHealth, previousTarget))
            {
                Color flashCol = targetHealth < previousTarget
                    ? (styleGuide != null ? styleGuide.bloodRed : Color.red)
                    : (styleGuide != null ? styleGuide.etherealGreen : Color.green);
                FlashGlow(flashCol);
            }

            if (healthLabel != null)
            {
                healthLabel.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
            }
        }

        private void UpdateBarSmooth()
        {
            if (Mathf.Approximately(displayedHealth, targetHealth))
                return;

            // Use unscaledDeltaTime so bar animates even when time is paused (e.g. stat menu open)
            displayedHealth = Mathf.Lerp(displayedHealth, targetHealth, smoothSpeed * Time.unscaledDeltaTime);

            if (Mathf.Abs(displayedHealth - targetHealth) < 0.001f)
            {
                displayedHealth = targetHealth;
            }

            UpdateBar(displayedHealth);
        }

        private void UpdateBar(float percent)
        {
            if (fillImage == null)
                return;

            fillImage.fillAmount = percent;

            if (useGradient && healthGradient != null)
            {
                fillImage.color = healthGradient.Evaluate(percent);
            }
        }

        private void UpdatePulse()
        {
            if (!isPulsing || fillImage == null)
                return;

            pulseTimer += Time.unscaledDeltaTime * pulseSpeed;
            float pulse = Mathf.Sin(pulseTimer * Mathf.PI * 2f) * pulseIntensity;

            Color baseColor = useGradient && healthGradient != null
                ? healthGradient.Evaluate(displayedHealth)
                : (styleGuide != null ? styleGuide.bloodRed : Color.red);

            fillImage.color = new Color(
                Mathf.Clamp01(baseColor.r + pulse),
                Mathf.Clamp01(baseColor.g + pulse * 0.5f),
                Mathf.Clamp01(baseColor.b + pulse * 0.5f),
                baseColor.a
            );
        }

        /// <summary>
        /// Manually connects to a HealthSystem (for runtime spawned players).
        /// </summary>
        public void SetHealthSystem(HealthSystem system)
        {
            UnsubscribeEvents();

            healthSystem = system;

            if (healthSystem != null)
            {
                SubscribeEvents();
                displayedHealth = healthSystem.HealthPercent;
                targetHealth = displayedHealth;
                UpdateBar(displayedHealth);
            }
        }

        private void CreateGlowFlash()
        {
            if (fillImage == null) return;

            var go = new GameObject("GlowFlash", typeof(RectTransform));
            go.transform.SetParent(fillImage.transform.parent, false);

            var rt = go.GetComponent<RectTransform>();
            // Copy fill image anchors but slightly wider
            rt.anchorMin = fillImage.rectTransform.anchorMin;
            rt.anchorMax = fillImage.rectTransform.anchorMax;
            rt.offsetMin = fillImage.rectTransform.offsetMin + new Vector2(-2f, -2f);
            rt.offsetMax = fillImage.rectTransform.offsetMax + new Vector2(2f, 2f);

            glowFlashImage = go.AddComponent<Image>();
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            glowFlashImage.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
            glowFlashImage.color = new Color(1f, 0f, 0f, 0f);
            glowFlashImage.raycastTarget = false;

            // Push behind the fill but above the background
            go.transform.SetSiblingIndex(fillImage.transform.GetSiblingIndex());
        }

        private void FlashGlow(Color flashColor)
        {
            if (glowFlashImage == null) return;
            if (glowFlashCoroutine != null) StopCoroutine(glowFlashCoroutine);
            glowFlashCoroutine = StartCoroutine(GlowFlashCoroutine(flashColor));
        }

        private IEnumerator GlowFlashCoroutine(Color flashColor)
        {
            float elapsed = 0f;
            Color startColor = new Color(flashColor.r, flashColor.g, flashColor.b, glowFlashAlpha);

            while (elapsed < glowFlashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / glowFlashDuration);
                float alpha = Mathf.Lerp(glowFlashAlpha, 0f, t * t);
                glowFlashImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }

            glowFlashImage.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
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
