using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Main container that manages the fixed HUD frame layout.
    /// Contains the top-left stat group (HP/MP/Level) and bottom bar (XP/Skills).
    /// Uses GothicFrameStyle for consistent borders.
    /// </summary>
    public class GameFrameHUD : MonoBehaviour
    {
        [Header("Frame References")]
        [SerializeField] private RectTransform topLeftGroup;
        [SerializeField] private RectTransform topRightGroup;
        [SerializeField] private RectTransform bottomBar;

        [Header("Top Left Components")]
        [SerializeField] private LevelDisplay levelDisplay;
        [SerializeField] private ResourceBarDisplay healthBar;
        [SerializeField] private ResourceBarDisplay manaBar;
        [SerializeField] private Image topLeftFrame;

        [Header("Bottom Bar Components")]
        [SerializeField] private ExpBarDisplay expBar;
        [SerializeField] private SkillHotbar skillHotbar;
        [SerializeField] private Image bottomFrame;

        [Header("Style")]
        [SerializeField] private UIStyleGuide styleGuide;
        [SerializeField] private GothicFrameStyle frameStyle;

        [Header("Auto-Wire Settings")]
        [SerializeField] private bool autoFindPlayer = true;
        [SerializeField] private bool autoWireComponents = true;

        private HealthSystem healthSystem;
        private ManaSystem manaSystem;
        private LevelSystem levelSystem;

        private void Start()
        {
            InitializeStyle();

            if (autoFindPlayer)
            {
                FindPlayerSystems();
            }

            if (autoWireComponents)
            {
                WireComponents();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeStyle()
        {
            if (styleGuide == null)
            {
                styleGuide = UIManager.Instance?.StyleGuide;
            }

            ApplyFrameStyle();
        }

        private void ApplyFrameStyle()
        {
            if (frameStyle == null)
                return;

            if (topLeftFrame != null)
            {
                topLeftFrame.sprite = frameStyle.frameSprite;
                topLeftFrame.color = frameStyle.frameColor;
                topLeftFrame.type = Image.Type.Sliced;
            }

            if (bottomFrame != null)
            {
                bottomFrame.sprite = frameStyle.frameSprite;
                bottomFrame.color = frameStyle.frameColor;
                bottomFrame.type = Image.Type.Sliced;
            }
        }

        private void FindPlayerSystems()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[GameFrameHUD] Player not found. HUD will not update.");
                return;
            }

            healthSystem = player.GetComponent<HealthSystem>();
            manaSystem = player.GetComponent<ManaSystem>();
            levelSystem = player.GetComponent<LevelSystem>();

            SubscribeToEvents();
            InitializeValues();
        }

        private void SubscribeToEvents()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += HandleHealthChanged;
            }

            if (manaSystem != null)
            {
                manaSystem.OnManaChanged += HandleManaChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged -= HandleHealthChanged;
            }

            if (manaSystem != null)
            {
                manaSystem.OnManaChanged -= HandleManaChanged;
            }
        }

        private void InitializeValues()
        {
            if (healthSystem != null && healthBar != null)
            {
                healthBar.SetValueImmediate(healthSystem.CurrentHealth, healthSystem.MaxHealth);
            }

            if (manaSystem != null && manaBar != null)
            {
                manaBar.SetValueImmediate(manaSystem.CurrentMana, manaSystem.MaxMana);
            }

            if (levelSystem != null && levelDisplay != null)
            {
                levelDisplay.SetLevelSystem(levelSystem);
            }

            if (levelSystem != null && expBar != null)
            {
                expBar.SetLevelSystem(levelSystem);
            }
        }

        private void WireComponents()
        {
            // Auto-find child components if not assigned
            if (levelDisplay == null && topLeftGroup != null)
            {
                levelDisplay = topLeftGroup.GetComponentInChildren<LevelDisplay>();
            }

            if (healthBar == null && topLeftGroup != null)
            {
                var bars = topLeftGroup.GetComponentsInChildren<ResourceBarDisplay>();
                foreach (var bar in bars)
                {
                    if (bar.name.Contains("Health"))
                    {
                        healthBar = bar;
                        break;
                    }
                }
            }

            if (manaBar == null && topLeftGroup != null)
            {
                var bars = topLeftGroup.GetComponentsInChildren<ResourceBarDisplay>();
                foreach (var bar in bars)
                {
                    if (bar.name.Contains("Mana"))
                    {
                        manaBar = bar;
                        break;
                    }
                }
            }

            if (expBar == null && bottomBar != null)
            {
                expBar = bottomBar.GetComponentInChildren<ExpBarDisplay>();
            }

            if (skillHotbar == null && bottomBar != null)
            {
                skillHotbar = bottomBar.GetComponentInChildren<SkillHotbar>();
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.SetValue(current, max);
            }
        }

        private void HandleManaChanged(float current, float max)
        {
            if (manaBar != null)
            {
                manaBar.SetValue(current, max);
            }
        }

        /// <summary>
        /// Manually connects to player systems.
        /// </summary>
        public void SetPlayerSystems(HealthSystem health, ManaSystem mana, LevelSystem level)
        {
            UnsubscribeFromEvents();

            healthSystem = health;
            manaSystem = mana;
            levelSystem = level;

            SubscribeToEvents();
            InitializeValues();
        }

        /// <summary>
        /// Shows or hides the top-left group.
        /// </summary>
        public void SetTopLeftVisible(bool visible)
        {
            if (topLeftGroup != null)
            {
                topLeftGroup.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Shows or hides the bottom bar.
        /// </summary>
        public void SetBottomBarVisible(bool visible)
        {
            if (bottomBar != null)
            {
                bottomBar.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Shows or hides the entire HUD frame.
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// Gets the health bar component.
        /// </summary>
        public ResourceBarDisplay GetHealthBar() => healthBar;

        /// <summary>
        /// Gets the mana bar component.
        /// </summary>
        public ResourceBarDisplay GetManaBar() => manaBar;

        /// <summary>
        /// Gets the level display component.
        /// </summary>
        public LevelDisplay GetLevelDisplay() => levelDisplay;

        /// <summary>
        /// Gets the exp bar component.
        /// </summary>
        public ExpBarDisplay GetExpBar() => expBar;

        /// <summary>
        /// Gets the skill hotbar component.
        /// </summary>
        public SkillHotbar GetSkillHotbar() => skillHotbar;
    }
}
