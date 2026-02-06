using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Large health bar displayed at top of screen during boss fights.
    /// Subscribes to BossController events for phase changes.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject bossBarRoot;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image healthFillBackground;
        [SerializeField] private TMP_Text bossNameText;
        [SerializeField] private TMP_Text healthText;

        [Header("Phase Indicators")]
        [SerializeField] private Image[] phaseIndicators;
        [SerializeField] private Color activePhaseColor = new Color(0.545f, 0f, 0f, 1f);
        [SerializeField] private Color inactivePhaseColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        [Header("Animation")]
        [SerializeField] private float fillLerpSpeed = 3f;
        [SerializeField] private float damageFlashDuration = 0.1f;
        [SerializeField] private Color damageFlashColor = Color.white;

        private BossController activeBoss;
        private HealthSystem bossHealth;
        private float targetFillAmount = 1f;
        private float currentFillAmount = 1f;
        private float flashTimer;
        private Color normalFillColor;

        private void Awake()
        {
            if (bossBarRoot != null)
                bossBarRoot.SetActive(false);

            if (healthFillImage != null)
                normalFillColor = healthFillImage.color;
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;

            UnsubscribeFromBoss();
        }

        private void Update()
        {
            if (activeBoss == null || bossBarRoot == null || !bossBarRoot.activeSelf)
                return;

            // Smooth fill lerp (unscaled so UI animates even when timeScale=0)
            if (healthFillImage != null)
            {
                currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, fillLerpSpeed * Time.unscaledDeltaTime);
                healthFillImage.fillAmount = currentFillAmount;
            }

            // Damage flash
            if (flashTimer > 0f)
            {
                flashTimer -= Time.unscaledDeltaTime;
                if (flashTimer <= 0f && healthFillImage != null)
                {
                    healthFillImage.color = normalFillColor;
                }
            }
        }

        private void HandleGameStateChanged(GameManager.GameState previous, GameManager.GameState current)
        {
            if (current == GameManager.GameState.BossFight)
            {
                FindAndShowBoss();
            }
            else if (previous == GameManager.GameState.BossFight)
            {
                HideBossBar();
            }
        }

        private void FindAndShowBoss()
        {
            // Find the active boss in the scene
            activeBoss = FindAnyObjectByType<BossController>();

            if (activeBoss == null)
            {
                Debug.LogWarning("[BossHealthBar] BossFight state entered but no BossController found.");
                return;
            }

            bossHealth = activeBoss.BossHealth;

            // Subscribe to events
            if (bossHealth != null)
            {
                bossHealth.OnHealthChanged += HandleHealthChanged;
                bossHealth.OnDeath += HandleBossDeath;
            }

            activeBoss.OnPhaseChanged += HandlePhaseChanged;

            // Initialize display
            if (bossNameText != null)
            {
                bossNameText.text = activeBoss.BossName;
                FontManager.EnsureFont(bossNameText);
            }

            if (healthText != null)
            {
                FontManager.EnsureFont(healthText);
            }

            targetFillAmount = 1f;
            currentFillAmount = 1f;

            if (healthFillImage != null)
                healthFillImage.fillAmount = 1f;

            UpdatePhaseIndicators(activeBoss.CurrentPhase);

            if (bossBarRoot != null)
                bossBarRoot.SetActive(true);
        }

        private void HideBossBar()
        {
            UnsubscribeFromBoss();

            if (bossBarRoot != null)
                bossBarRoot.SetActive(false);
        }

        private void UnsubscribeFromBoss()
        {
            if (bossHealth != null)
            {
                bossHealth.OnHealthChanged -= HandleHealthChanged;
                bossHealth.OnDeath -= HandleBossDeath;
            }

            if (activeBoss != null)
            {
                activeBoss.OnPhaseChanged -= HandlePhaseChanged;
            }

            activeBoss = null;
            bossHealth = null;
        }

        private void HandleHealthChanged(float currentHP, float maxHP)
        {
            targetFillAmount = maxHP > 0f ? currentHP / maxHP : 0f;

            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)}";
            }

            // Flash on damage
            if (healthFillImage != null && targetFillAmount < currentFillAmount)
            {
                healthFillImage.color = damageFlashColor;
                flashTimer = damageFlashDuration;
            }
        }

        private void HandlePhaseChanged(BossController.BossPhase newPhase)
        {
            UpdatePhaseIndicators(newPhase);
        }

        private void HandleBossDeath()
        {
            targetFillAmount = 0f;
        }

        private void UpdatePhaseIndicators(BossController.BossPhase phase)
        {
            if (phaseIndicators == null)
                return;

            int phaseIndex = (int)phase;

            for (int i = 0; i < phaseIndicators.Length; i++)
            {
                if (phaseIndicators[i] != null)
                {
                    phaseIndicators[i].color = i <= phaseIndex ? activePhaseColor : inactivePhaseColor;
                }
            }
        }
    }
}
