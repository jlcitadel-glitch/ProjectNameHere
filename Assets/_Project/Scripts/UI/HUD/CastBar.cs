using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Displays skill casting progress bar.
    /// Subscribes to PlayerSkillController cast events.
    /// </summary>
    public class CastBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text skillNameText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Style")]
        [SerializeField] private UIStyleGuide styleGuide;
        [SerializeField] private Color castingColor = new Color(0.812f, 0.710f, 0.231f, 1f);
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        [Header("Animation")]
        [SerializeField] private float fadeSpeed = 5f;
        [SerializeField] private float showDelay = 0.1f;

        [Header("Audio")]
        [SerializeField] private AudioClip castStartSound;
        [SerializeField] private AudioClip castCompleteSound;
        [SerializeField] private AudioClip castCancelSound;

        private PlayerSkillController skillController;
        private SkillManager skillManager;
        private AudioSource audioSource;

        private bool isCasting;
        private float castStartTime;
        private float castDuration;
        private string currentSkillId;
        private float targetAlpha;
        private float showDelayTimer;

        private void Start()
        {
            FindPlayerSkillController();
            InitializeStyle();
            InitializeAudio();
            Hide();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            UpdateCastProgress();
            UpdateVisibility();
        }

        private void FindPlayerSkillController()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                skillController = player.GetComponent<PlayerSkillController>();
                if (skillController != null)
                {
                    SubscribeToEvents();
                    Debug.Log("[CastBar] Connected to PlayerSkillController");
                }
            }

            skillManager = SkillManager.Instance;
        }

        private void SubscribeToEvents()
        {
            if (skillController == null) return;

            skillController.OnCastStarted += HandleCastStarted;
            skillController.OnCastCompleted += HandleCastCompleted;
            skillController.OnCastCancelled += HandleCastCancelled;
            skillController.OnSkillUsed += HandleSkillUsed;
        }

        private void UnsubscribeFromEvents()
        {
            if (skillController == null) return;

            skillController.OnCastStarted -= HandleCastStarted;
            skillController.OnCastCompleted -= HandleCastCompleted;
            skillController.OnCastCancelled -= HandleCastCancelled;
            skillController.OnSkillUsed -= HandleSkillUsed;
        }

        private void InitializeStyle()
        {
            if (styleGuide == null && UIManager.Instance != null)
            {
                styleGuide = UIManager.Instance.StyleGuide;
            }

            if (styleGuide != null)
            {
                castingColor = styleGuide.agedGold;
                backgroundColor = styleGuide.charcoal;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundColor;
            }

            if (fillImage != null)
            {
                fillImage.color = castingColor;
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

        private void HandleCastStarted()
        {
            // Get current casting skill info from skill controller
            // We need to find the skill being cast - check pending skill via reflection or track it
            isCasting = true;
            castStartTime = Time.time;
            showDelayTimer = showDelay;

            // Try to get skill info
            if (skillManager != null && !string.IsNullOrEmpty(currentSkillId))
            {
                var skill = skillManager.GetLearnedSkill(currentSkillId);
                if (skill != null)
                {
                    castDuration = skill.skillData?.castTime ?? 1f;

                    if (skillNameText != null)
                    {
                        skillNameText.text = skill.SkillName;
                    }
                }
            }

            PlaySound(castStartSound);
        }

        private void HandleSkillUsed(string skillId, SkillInstance instance)
        {
            // Track current skill for cast bar info
            currentSkillId = skillId;

            if (instance?.skillData != null && instance.skillData.castTime > 0f)
            {
                castDuration = instance.skillData.castTime;

                if (skillNameText != null)
                {
                    skillNameText.text = instance.SkillName;
                }
            }
        }

        private void HandleCastCompleted()
        {
            isCasting = false;
            targetAlpha = 0f;
            PlaySound(castCompleteSound);
        }

        private void HandleCastCancelled()
        {
            isCasting = false;
            targetAlpha = 0f;
            PlaySound(castCancelSound);
        }

        private void UpdateCastProgress()
        {
            if (!isCasting || fillImage == null)
                return;

            if (showDelayTimer > 0f)
            {
                showDelayTimer -= Time.deltaTime;
                if (showDelayTimer <= 0f)
                {
                    targetAlpha = 1f;
                }
                return;
            }

            float elapsed = Time.time - castStartTime;
            float progress = castDuration > 0f ? Mathf.Clamp01(elapsed / castDuration) : 1f;

            fillImage.fillAmount = progress;
        }

        private void UpdateVisibility()
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

            if (canvasGroup.alpha <= 0.01f && !isCasting)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private void Show()
        {
            targetAlpha = 1f;
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }
        }

        private void Hide()
        {
            targetAlpha = 0f;
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
        /// Manually sets the skill controller reference.
        /// </summary>
        public void SetSkillController(PlayerSkillController controller)
        {
            UnsubscribeFromEvents();
            skillController = controller;
            SubscribeToEvents();
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
