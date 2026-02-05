using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Experience bar display for the bottom frame HUD.
    /// Shows current/required XP with smooth fill animation and flash effects on XP gain.
    /// </summary>
    public class ExpBarDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text expLabel;

        [Header("Style")]
        [SerializeField] private UIStyleGuide styleGuide;
        [SerializeField] private string labelFormat = "{0}/{1} XP";

        [Header("Animation")]
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private float flashDuration = 0.4f;
        [SerializeField] private float flashIntensity = 0.6f;

        private LevelSystem levelSystem;
        private float displayedProgress;
        private float targetProgress;
        private Coroutine flashCoroutine;

        private void Start()
        {
            FindPlayerLevelSystem();
            InitializeStyle();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            UpdateBarSmooth();
        }

        private void FindPlayerLevelSystem()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                levelSystem = player.GetComponent<LevelSystem>();
                if (levelSystem != null)
                {
                    SubscribeToEvents();
                    UpdateDisplay();
                }
            }
        }

        private void SubscribeToEvents()
        {
            if (levelSystem != null)
            {
                levelSystem.OnXPChanged += HandleXPChanged;
                levelSystem.OnXPGained += HandleXPGained;
                levelSystem.OnLevelUp += HandleLevelUp;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (levelSystem != null)
            {
                levelSystem.OnXPChanged -= HandleXPChanged;
                levelSystem.OnXPGained -= HandleXPGained;
                levelSystem.OnLevelUp -= HandleLevelUp;
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
                if (backgroundImage != null)
                {
                    backgroundImage.color = styleGuide.charcoal;
                }

                if (fillImage != null)
                {
                    fillImage.color = styleGuide.agedGold;
                }

                if (expLabel != null)
                {
                    expLabel.color = styleGuide.boneWhite;
                }
            }
        }

        private void HandleXPChanged(int currentXP, int xpForNextLevel)
        {
            UpdateDisplay();
        }

        private void HandleXPGained(int amount, int totalXP)
        {
            PlayFlashEffect();
        }

        private void HandleLevelUp(int newLevel)
        {
            // Reset bar display on level up
            if (levelSystem != null)
            {
                displayedProgress = 0f;
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (levelSystem == null)
                return;

            targetProgress = levelSystem.LevelProgress;

            // Update label
            if (expLabel != null)
            {
                if (levelSystem.IsMaxLevel)
                {
                    expLabel.text = "MAX LEVEL";
                }
                else
                {
                    int currentLevelXP = levelSystem.XPForCurrentLevel;
                    int nextLevelXP = levelSystem.XPForNextLevel;
                    int xpIntoLevel = levelSystem.CurrentXP - currentLevelXP;
                    int xpRequired = nextLevelXP - currentLevelXP;

                    expLabel.text = string.Format(labelFormat, xpIntoLevel, xpRequired);
                }
            }
        }

        private void UpdateBarSmooth()
        {
            if (Mathf.Approximately(displayedProgress, targetProgress))
                return;

            displayedProgress = Mathf.Lerp(displayedProgress, targetProgress, smoothSpeed * Time.deltaTime);

            if (Mathf.Abs(displayedProgress - targetProgress) < 0.001f)
            {
                displayedProgress = targetProgress;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = displayedProgress;
            }
        }

        private void PlayFlashEffect()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }

            flashCoroutine = StartCoroutine(FlashCoroutine());
        }

        private System.Collections.IEnumerator FlashCoroutine()
        {
            if (fillImage == null)
                yield break;

            Color originalColor = styleGuide != null ? styleGuide.agedGold : fillImage.color;
            float elapsed = 0f;

            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flashDuration;
                float flash = Mathf.Sin(t * Mathf.PI) * flashIntensity;

                fillImage.color = new Color(
                    Mathf.Clamp01(originalColor.r + flash),
                    Mathf.Clamp01(originalColor.g + flash),
                    Mathf.Clamp01(originalColor.b + flash),
                    originalColor.a
                );

                yield return null;
            }

            fillImage.color = originalColor;
            flashCoroutine = null;
        }

        /// <summary>
        /// Manually sets the XP display values.
        /// </summary>
        public void SetValues(int currentXP, int maxXP, float progress)
        {
            targetProgress = progress;

            if (expLabel != null)
            {
                expLabel.text = string.Format(labelFormat, currentXP, maxXP);
            }
        }

        /// <summary>
        /// Sets the progress immediately without animation.
        /// </summary>
        public void SetProgressImmediate(float progress)
        {
            targetProgress = progress;
            displayedProgress = progress;

            if (fillImage != null)
            {
                fillImage.fillAmount = progress;
            }
        }

        /// <summary>
        /// Manually connects to a LevelSystem.
        /// </summary>
        public void SetLevelSystem(LevelSystem system)
        {
            UnsubscribeFromEvents();
            levelSystem = system;

            if (levelSystem != null)
            {
                SubscribeToEvents();
                UpdateDisplay();
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
