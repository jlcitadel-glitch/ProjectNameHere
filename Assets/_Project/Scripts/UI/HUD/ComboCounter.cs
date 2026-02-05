using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Displays hit combo counter during combat.
    /// Subscribes to CombatController.OnAttackHit.
    /// </summary>
    public class ComboCounter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text comboCountText;
        [SerializeField] private TMP_Text comboLabelText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image backgroundImage;

        [Header("Timing")]
        [SerializeField] private float comboTimeout = 2f;
        [SerializeField] private float displayDuration = 1.5f;

        [Header("Style")]
        [SerializeField] private UIStyleGuide styleGuide;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color goodComboColor = new Color(1f, 0.843f, 0f, 1f);
        [SerializeField] private Color greatComboColor = new Color(1f, 0.5f, 0f, 1f);
        [SerializeField] private Color amazingComboColor = new Color(1f, 0.2f, 0.2f, 1f);

        [Header("Thresholds")]
        [SerializeField] private int goodComboThreshold = 5;
        [SerializeField] private int greatComboThreshold = 10;
        [SerializeField] private int amazingComboThreshold = 20;

        [Header("Animation")]
        [SerializeField] private float punchScale = 1.3f;
        [SerializeField] private float punchDuration = 0.15f;
        [SerializeField] private float fadeSpeed = 3f;
        [SerializeField] private float shakeIntensity = 5f;

        [Header("Audio")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip milestoneSound;

        private CombatController combatController;
        private AudioSource audioSource;

        private int currentCombo;
        private float lastHitTime;
        private float displayTimer;
        private float punchTimer;
        private Vector3 originalScale;
        private Vector3 originalPosition;
        private bool isVisible;

        private void Awake()
        {
            originalScale = transform.localScale;
            originalPosition = transform.localPosition;
        }

        private void Start()
        {
            FindCombatController();
            InitializeStyle();
            InitializeAudio();
            Hide();
        }

        private void OnDestroy()
        {
            if (combatController != null)
            {
                combatController.OnAttackHit -= HandleAttackHit;
            }
        }

        private void Update()
        {
            UpdateComboTimeout();
            UpdateAnimation();
            UpdateVisibility();
        }

        private void FindCombatController()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                combatController = player.GetComponent<CombatController>();
                if (combatController != null)
                {
                    combatController.OnAttackHit += HandleAttackHit;
                    Debug.Log("[ComboCounter] Connected to CombatController");
                }
            }
        }

        private void InitializeStyle()
        {
            if (styleGuide == null && UIManager.Instance != null)
            {
                styleGuide = UIManager.Instance.StyleGuide;
            }

            if (styleGuide != null)
            {
                goodComboColor = styleGuide.agedGold;
                amazingComboColor = styleGuide.bloodRed;

                if (backgroundImage != null)
                {
                    backgroundImage.color = new Color(
                        styleGuide.charcoal.r,
                        styleGuide.charcoal.g,
                        styleGuide.charcoal.b,
                        0.7f
                    );
                }
            }

            if (comboLabelText != null)
            {
                comboLabelText.text = "COMBO";
            }
        }

        private void InitializeAudio()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }
        }

        private void HandleAttackHit(AttackData attack)
        {
            // Increment combo
            currentCombo++;
            lastHitTime = Time.time;
            displayTimer = displayDuration;

            // Update display
            UpdateComboDisplay();

            // Play animations
            PlayPunchAnimation();

            // Check for milestone
            bool isMilestone = currentCombo == goodComboThreshold ||
                               currentCombo == greatComboThreshold ||
                               currentCombo == amazingComboThreshold;

            PlaySound(isMilestone ? milestoneSound : hitSound);

            // Show if hidden
            if (!isVisible)
            {
                Show();
            }
        }

        private void UpdateComboDisplay()
        {
            if (comboCountText != null)
            {
                comboCountText.text = currentCombo.ToString();
                comboCountText.color = GetComboColor();
            }

            if (comboLabelText != null)
            {
                comboLabelText.text = GetComboLabel();
                comboLabelText.color = GetComboColor();
            }
        }

        private Color GetComboColor()
        {
            if (currentCombo >= amazingComboThreshold)
                return amazingComboColor;
            if (currentCombo >= greatComboThreshold)
                return greatComboColor;
            if (currentCombo >= goodComboThreshold)
                return goodComboColor;
            return normalColor;
        }

        private string GetComboLabel()
        {
            if (currentCombo >= amazingComboThreshold)
                return "AMAZING!";
            if (currentCombo >= greatComboThreshold)
                return "GREAT!";
            if (currentCombo >= goodComboThreshold)
                return "GOOD!";
            return "COMBO";
        }

        private void UpdateComboTimeout()
        {
            if (currentCombo == 0)
                return;

            if (Time.time - lastHitTime > comboTimeout)
            {
                ResetCombo();
            }
        }

        private void ResetCombo()
        {
            currentCombo = 0;
            displayTimer = 0f;
        }

        private void UpdateAnimation()
        {
            // Punch scale animation
            if (punchTimer > 0f)
            {
                punchTimer -= Time.deltaTime;
                float progress = 1f - (punchTimer / punchDuration);
                float scale = Mathf.Lerp(punchScale, 1f, EaseOutBack(progress));
                transform.localScale = originalScale * scale;

                // Shake for high combos
                if (currentCombo >= greatComboThreshold)
                {
                    float shake = shakeIntensity * (1f - progress);
                    Vector3 offset = new Vector3(
                        Random.Range(-shake, shake),
                        Random.Range(-shake, shake),
                        0f
                    );
                    transform.localPosition = originalPosition + offset;
                }
            }
            else
            {
                transform.localScale = originalScale;
                transform.localPosition = originalPosition;
            }
        }

        private void UpdateVisibility()
        {
            if (displayTimer > 0f)
            {
                displayTimer -= Time.deltaTime;
            }
            else if (isVisible && currentCombo == 0)
            {
                Hide();
            }

            // Fade based on display timer
            if (canvasGroup != null && isVisible)
            {
                float targetAlpha = displayTimer > 0.5f ? 1f : displayTimer / 0.5f;
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
            }
        }

        private void PlayPunchAnimation()
        {
            punchTimer = punchDuration;
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private void Show()
        {
            isVisible = true;
            gameObject.SetActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        private void Hide()
        {
            isVisible = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Manually adds a hit to the combo (for external systems).
        /// </summary>
        public void AddHit()
        {
            HandleAttackHit(null);
        }

        /// <summary>
        /// Gets the current combo count.
        /// </summary>
        public int GetComboCount()
        {
            return currentCombo;
        }

        /// <summary>
        /// Manually sets the combat controller reference.
        /// </summary>
        public void SetCombatController(CombatController controller)
        {
            if (combatController != null)
            {
                combatController.OnAttackHit -= HandleAttackHit;
            }

            combatController = controller;

            if (combatController != null)
            {
                combatController.OnAttackHit += HandleAttackHit;
            }
        }
    }
}
