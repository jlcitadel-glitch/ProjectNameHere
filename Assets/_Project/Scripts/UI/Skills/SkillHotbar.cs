using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// UI component for the in-game skill hotbar.
    /// Displays equipped skills and cooldown states.
    /// </summary>
    public class SkillHotbar : MonoBehaviour
    {
        [Serializable]
        public class HotbarSlot
        {
            public RectTransform slotRect;
            public Image iconImage;
            public Image cooldownOverlay;
            public TMP_Text cooldownText;
            public TMP_Text keyBindText;
            public Image frameImage;
            public CanvasGroup canvasGroup;
        }

        [Header("Slots")]
        [SerializeField] private HotbarSlot[] slots;

        [Header("Style")]
        [SerializeField] private Color readyColor = Color.white;
        [SerializeField] private Color cooldownColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color noManaColor = new Color(0.3f, 0.3f, 0.8f, 1f);
        [SerializeField] private Sprite emptySlotSprite;

        [Header("Key Bindings Display")]
        [SerializeField] private string[] keyBindLabels = { "1", "2", "3", "4", "5", "6" };

        // Runtime
        private PlayerSkillController skillController;
        private ManaSystem manaSystem;
        private SkillCooldownTracker cooldownTracker;

        private void Start()
        {
            FindReferences();
            InitializeSlots();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            UpdateCooldowns();
        }

        private void FindReferences()
        {
            // Find player and components
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                skillController = player.GetComponent<PlayerSkillController>();
                manaSystem = player.GetComponent<ManaSystem>();

                if (skillController != null)
                {
                    cooldownTracker = skillController.GetCooldownTracker();
                }
            }
        }

        private void SubscribeToEvents()
        {
            if (skillController != null)
            {
                skillController.OnHotbarChanged += HandleHotbarChanged;
                skillController.OnSkillUsed += HandleSkillUsed;
                skillController.OnSkillReady += HandleSkillReady;
            }

            if (manaSystem != null)
            {
                manaSystem.OnManaChanged += HandleManaChanged;
            }

            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.OnSkillLevelChanged += HandleSkillLevelChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (skillController != null)
            {
                skillController.OnHotbarChanged -= HandleHotbarChanged;
                skillController.OnSkillUsed -= HandleSkillUsed;
                skillController.OnSkillReady -= HandleSkillReady;
            }

            if (manaSystem != null)
            {
                manaSystem.OnManaChanged -= HandleManaChanged;
            }

            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.OnSkillLevelChanged -= HandleSkillLevelChanged;
            }
        }

        private void InitializeSlots()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;

                // Set key bind label
                if (slot.keyBindText != null && i < keyBindLabels.Length)
                {
                    slot.keyBindText.text = keyBindLabels[i];
                }

                // Initialize as empty
                RefreshSlot(i);
            }
        }

        /// <summary>
        /// Refreshes the display of a specific slot.
        /// </summary>
        public void RefreshSlot(int index)
        {
            if (index < 0 || index >= slots.Length) return;

            var slot = slots[index];
            if (slot == null) return;

            string skillId = skillController?.GetHotbarSkill(index);
            SkillInstance skillInstance = null;
            SkillData skillData = null;

            if (!string.IsNullOrEmpty(skillId))
            {
                skillInstance = SkillManager.Instance?.GetLearnedSkill(skillId);
                skillData = skillInstance?.skillData ?? SkillManager.Instance?.GetSkillData(skillId);
            }

            // Update icon
            if (slot.iconImage != null)
            {
                if (skillData?.icon != null)
                {
                    slot.iconImage.sprite = skillData.icon;
                    slot.iconImage.color = Color.white;
                    slot.iconImage.enabled = true;
                }
                else if (emptySlotSprite != null)
                {
                    slot.iconImage.sprite = emptySlotSprite;
                    slot.iconImage.color = new Color(1f, 1f, 1f, 0.3f);
                    slot.iconImage.enabled = true;
                }
                else
                {
                    slot.iconImage.enabled = false;
                }
            }

            // Update state
            UpdateSlotState(index);
        }

        /// <summary>
        /// Refreshes all slots.
        /// </summary>
        public void RefreshAllSlots()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                RefreshSlot(i);
            }
        }

        private void UpdateSlotState(int index)
        {
            if (index < 0 || index >= slots.Length) return;

            var slot = slots[index];
            if (slot == null) return;

            string skillId = skillController?.GetHotbarSkill(index);

            if (string.IsNullOrEmpty(skillId))
            {
                // Empty slot
                if (slot.cooldownOverlay != null)
                    slot.cooldownOverlay.fillAmount = 0f;
                if (slot.cooldownText != null)
                    slot.cooldownText.gameObject.SetActive(false);
                if (slot.frameImage != null)
                    slot.frameImage.color = readyColor * 0.5f;
                return;
            }

            var skillInstance = SkillManager.Instance?.GetLearnedSkill(skillId);
            if (skillInstance == null)
            {
                // Skill not learned
                if (slot.canvasGroup != null)
                    slot.canvasGroup.alpha = 0.5f;
                return;
            }

            // Check cooldown
            float remaining = cooldownTracker?.GetRemainingCooldown(skillId) ?? 0f;
            float progress = cooldownTracker?.GetCooldownProgress(skillId) ?? 1f;

            if (remaining > 0f)
            {
                // On cooldown
                if (slot.cooldownOverlay != null)
                {
                    slot.cooldownOverlay.fillAmount = 1f - progress;
                    slot.cooldownOverlay.color = cooldownColor;
                }

                if (slot.cooldownText != null)
                {
                    slot.cooldownText.gameObject.SetActive(true);
                    slot.cooldownText.text = remaining > 1f ? Mathf.CeilToInt(remaining).ToString() : remaining.ToString("F1");
                }

                if (slot.iconImage != null)
                    slot.iconImage.color = cooldownColor;
            }
            else
            {
                // Ready
                if (slot.cooldownOverlay != null)
                    slot.cooldownOverlay.fillAmount = 0f;

                if (slot.cooldownText != null)
                    slot.cooldownText.gameObject.SetActive(false);

                // Check mana
                float manaCost = skillInstance.GetManaCost();
                bool canAfford = manaSystem?.CanAfford(manaCost) ?? true;

                if (slot.iconImage != null)
                    slot.iconImage.color = canAfford ? readyColor : noManaColor;

                if (slot.frameImage != null)
                    slot.frameImage.color = canAfford ? readyColor : noManaColor;
            }
        }

        private void UpdateCooldowns()
        {
            if (cooldownTracker == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                string skillId = skillController?.GetHotbarSkill(i);
                if (string.IsNullOrEmpty(skillId)) continue;

                if (cooldownTracker.IsOnCooldown(skillId))
                {
                    UpdateSlotState(i);
                }
            }
        }

        private void HandleHotbarChanged(int index, string skillId)
        {
            RefreshSlot(index);
        }

        private void HandleSkillUsed(string skillId, SkillInstance instance)
        {
            // Find slot with this skill
            for (int i = 0; i < slots.Length; i++)
            {
                if (skillController?.GetHotbarSkill(i) == skillId)
                {
                    UpdateSlotState(i);
                    PlayUseAnimation(i);
                    break;
                }
            }
        }

        private void HandleSkillReady(string skillId)
        {
            // Find slot with this skill
            for (int i = 0; i < slots.Length; i++)
            {
                if (skillController?.GetHotbarSkill(i) == skillId)
                {
                    UpdateSlotState(i);
                    PlayReadyAnimation(i);
                    break;
                }
            }
        }

        private void HandleManaChanged(float current, float max)
        {
            // Update all slots to reflect mana availability
            for (int i = 0; i < slots.Length; i++)
            {
                UpdateSlotState(i);
            }
        }

        private void HandleSkillLevelChanged(SkillInstance instance, int oldLevel, int newLevel)
        {
            // Refresh slots that contain this skill
            for (int i = 0; i < slots.Length; i++)
            {
                if (skillController?.GetHotbarSkill(i) == instance.SkillId)
                {
                    RefreshSlot(i);
                }
            }
        }

        private void PlayUseAnimation(int index)
        {
            if (index < 0 || index >= slots.Length) return;
            var slot = slots[index];
            if (slot?.slotRect == null) return;

            // Simple scale punch
            StartCoroutine(ScalePunchCoroutine(slot.slotRect));
        }

        private void PlayReadyAnimation(int index)
        {
            if (index < 0 || index >= slots.Length) return;
            var slot = slots[index];
            if (slot?.frameImage == null) return;

            // Flash effect
            StartCoroutine(FlashCoroutine(slot.frameImage));
        }

        private System.Collections.IEnumerator ScalePunchCoroutine(RectTransform rect)
        {
            Vector3 originalScale = rect.localScale;
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
                rect.localScale = originalScale * scale;
                yield return null;
            }

            rect.localScale = originalScale;
        }

        private System.Collections.IEnumerator FlashCoroutine(Image image)
        {
            Color originalColor = image.color;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float brightness = 1f + Mathf.Sin(t * Mathf.PI) * 0.5f;
                image.color = originalColor * brightness;
                yield return null;
            }

            image.color = originalColor;
        }

        /// <summary>
        /// Sets a skill in a hotbar slot.
        /// </summary>
        public void SetSlotSkill(int index, string skillId)
        {
            if (skillController != null)
            {
                skillController.SetHotbarSkill(index, skillId);
            }
        }

        /// <summary>
        /// Clears a hotbar slot.
        /// </summary>
        public void ClearSlot(int index)
        {
            SetSlotSkill(index, null);
        }
    }
}
