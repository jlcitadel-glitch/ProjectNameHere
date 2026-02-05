using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// HUD component that displays the player's mana bar.
    /// Auto-finds the player and subscribes to ManaSystem events.
    /// </summary>
    public class ManaDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text manaLabel;

        [Header("Style")]
        [SerializeField] private UIStyleGuide styleGuide;
        [SerializeField] private bool useGradient = true;

        [Header("Animation")]
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private float lowManaPulseThreshold = 0.25f;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseIntensity = 0.3f;

        [Header("Audio")]
        [SerializeField] private UISoundBank soundBank;
        [SerializeField] private AudioSource audioSource;

        private ManaSystem manaSystem;
        private float displayedMana;
        private float targetMana;
        private Gradient manaGradient;
        private bool isPulsing;
        private float pulseTimer;

        private void Start()
        {
            FindPlayerManaSystem();
            InitializeStyle();
            InitializeAudio();
        }

        private void OnDestroy()
        {
            if (manaSystem != null)
            {
                manaSystem.OnManaChanged -= HandleManaChanged;
            }
        }

        private void Update()
        {
            UpdateBarSmooth();
            UpdatePulse();
        }

        private void FindPlayerManaSystem()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                manaSystem = player.GetComponent<ManaSystem>();
                if (manaSystem != null)
                {
                    manaSystem.OnManaChanged += HandleManaChanged;
                    displayedMana = manaSystem.ManaPercent;
                    targetMana = displayedMana;
                    UpdateBar(displayedMana);
                    Debug.Log("[ManaDisplay] Connected to ManaSystem");
                }
                else
                {
                    Debug.LogWarning("[ManaDisplay] Player found but ManaSystem component missing");
                }
            }
            else
            {
                Debug.LogWarning("[ManaDisplay] Player not found. ManaDisplay will not update.");
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
                manaGradient = styleGuide.GetSoulMeterGradient();

                if (backgroundImage != null)
                {
                    backgroundImage.color = styleGuide.charcoal;
                }

                if (fillImage != null && !useGradient)
                {
                    fillImage.color = styleGuide.spectralCyan;
                }
            }
            else
            {
                CreateDefaultGradient();
            }
        }

        private void CreateDefaultGradient()
        {
            manaGradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(new Color(0.098f, 0.098f, 0.439f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0f, 0.808f, 0.820f), 0.7f);
            colorKeys[2] = new GradientColorKey(Color.white, 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(0.8f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            manaGradient.SetKeys(colorKeys, alphaKeys);
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

        private void HandleManaChanged(float current, float max)
        {
            float previousTarget = targetMana;
            targetMana = max > 0f ? current / max : 0f;

            if (targetMana > previousTarget && soundBank != null && soundBank.soulFill != null)
            {
                soundBank.PlaySound(soundBank.soulFill, audioSource, 0.5f);
            }

            isPulsing = targetMana <= lowManaPulseThreshold && targetMana > 0f;

            if (manaLabel != null)
            {
                manaLabel.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
            }
        }

        private void UpdateBarSmooth()
        {
            if (Mathf.Approximately(displayedMana, targetMana))
                return;

            displayedMana = Mathf.Lerp(displayedMana, targetMana, smoothSpeed * Time.deltaTime);

            if (Mathf.Abs(displayedMana - targetMana) < 0.001f)
            {
                displayedMana = targetMana;
            }

            UpdateBar(displayedMana);
        }

        private void UpdateBar(float percent)
        {
            if (fillImage == null)
                return;

            fillImage.fillAmount = percent;

            if (useGradient && manaGradient != null)
            {
                fillImage.color = manaGradient.Evaluate(percent);
            }
        }

        private void UpdatePulse()
        {
            if (!isPulsing || fillImage == null)
                return;

            pulseTimer += Time.deltaTime * pulseSpeed;
            float pulse = Mathf.Sin(pulseTimer * Mathf.PI * 2f) * pulseIntensity;

            Color baseColor = useGradient && manaGradient != null
                ? manaGradient.Evaluate(displayedMana)
                : (styleGuide != null ? styleGuide.spectralCyan : Color.cyan);

            fillImage.color = new Color(
                Mathf.Clamp01(baseColor.r + pulse),
                Mathf.Clamp01(baseColor.g + pulse),
                Mathf.Clamp01(baseColor.b + pulse),
                baseColor.a
            );
        }

        /// <summary>
        /// Manually connects to a ManaSystem (for runtime spawned players).
        /// </summary>
        public void SetManaSystem(ManaSystem system)
        {
            if (manaSystem != null)
            {
                manaSystem.OnManaChanged -= HandleManaChanged;
            }

            manaSystem = system;

            if (manaSystem != null)
            {
                manaSystem.OnManaChanged += HandleManaChanged;
                displayedMana = manaSystem.ManaPercent;
                targetMana = displayedMana;
                UpdateBar(displayedMana);
            }
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
