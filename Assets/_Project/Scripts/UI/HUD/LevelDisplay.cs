using UnityEngine;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Simple level number display for the fixed HUD frame.
    /// Subscribes to LevelSystem events and displays "Lv. X".
    /// </summary>
    public class LevelDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text levelText;

        [Header("Style")]
        [SerializeField] private UIStyleGuide styleGuide;
        [SerializeField] private string levelFormat = "Lv. {0}";

        [Header("Animation")]
        [SerializeField] private bool enableLevelUpAnimation = true;
        [SerializeField] private float scalePunchAmount = 1.3f;
        [SerializeField] private float animationDuration = 0.3f;

        private LevelSystem levelSystem;
        private Vector3 originalScale;
        private Coroutine animationCoroutine;

        private void Start()
        {
            if (levelText != null)
            {
                originalScale = levelText.transform.localScale;
            }

            FindPlayerLevelSystem();
            InitializeStyle();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
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
                    UpdateDisplay(levelSystem.CurrentLevel);
                }
            }

            // Also try SkillManager for player level
            if (levelSystem == null && SkillManager.Instance != null)
            {
                SkillManager.Instance.OnPlayerLevelChanged += HandlePlayerLevelChanged;
                UpdateDisplay(SkillManager.Instance.PlayerLevel);
            }
        }

        private void SubscribeToEvents()
        {
            if (levelSystem != null)
            {
                levelSystem.OnLevelUp += HandleLevelUp;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (levelSystem != null)
            {
                levelSystem.OnLevelUp -= HandleLevelUp;
            }

            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.OnPlayerLevelChanged -= HandlePlayerLevelChanged;
            }
        }

        private void InitializeStyle()
        {
            if (styleGuide == null)
            {
                styleGuide = UIManager.Instance?.StyleGuide;
            }

            if (styleGuide != null && levelText != null)
            {
                levelText.color = styleGuide.agedGold;
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            UpdateDisplay(newLevel);

            if (enableLevelUpAnimation)
            {
                PlayLevelUpAnimation();
            }
        }

        private void HandlePlayerLevelChanged(int oldLevel, int newLevel)
        {
            UpdateDisplay(newLevel);

            if (enableLevelUpAnimation && newLevel > oldLevel)
            {
                PlayLevelUpAnimation();
            }
        }

        private void UpdateDisplay(int level)
        {
            if (levelText != null)
            {
                levelText.text = string.Format(levelFormat, level);
            }
        }

        private void PlayLevelUpAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            animationCoroutine = StartCoroutine(LevelUpAnimationCoroutine());
        }

        private System.Collections.IEnumerator LevelUpAnimationCoroutine()
        {
            if (levelText == null)
                yield break;

            Transform textTransform = levelText.transform;
            Color originalColor = levelText.color;
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / animationDuration;

                // Scale punch
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * (scalePunchAmount - 1f);
                textTransform.localScale = originalScale * scale;

                // Color flash to white and back
                if (styleGuide != null)
                {
                    levelText.color = Color.Lerp(Color.white, styleGuide.agedGold, t);
                }
                else
                {
                    levelText.color = Color.Lerp(Color.white, originalColor, t);
                }

                yield return null;
            }

            textTransform.localScale = originalScale;
            levelText.color = styleGuide != null ? styleGuide.agedGold : originalColor;
            animationCoroutine = null;
        }

        /// <summary>
        /// Wires internal references for runtime-created displays.
        /// </summary>
        public void SetReferences(TMP_Text text)
        {
            levelText = text;
            if (levelText != null)
            {
                originalScale = levelText.transform.localScale;
            }
        }

        /// <summary>
        /// Manually sets the displayed level.
        /// </summary>
        public void SetLevel(int level)
        {
            UpdateDisplay(level);
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
                UpdateDisplay(levelSystem.CurrentLevel);
            }
        }
    }
}
