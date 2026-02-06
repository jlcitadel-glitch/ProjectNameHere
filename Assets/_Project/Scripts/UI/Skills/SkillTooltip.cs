using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Tooltip panel that displays skill information on hover.
    /// Follows the cursor or controller selection.
    /// </summary>
    public class SkillTooltip : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform tooltipRect;

        [Header("Content")]
        [SerializeField] private TMP_Text skillNameText;
        [SerializeField] private TMP_Text skillTypeText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Image skillIcon;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Stats")]
        [SerializeField] private GameObject statsContainer;
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text manaCostText;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private TMP_Text durationText;

        [Header("Next Level")]
        [SerializeField] private GameObject nextLevelContainer;
        [SerializeField] private TMP_Text nextLevelText;

        [Header("Requirements")]
        [SerializeField] private GameObject requirementsContainer;
        [SerializeField] private TMP_Text requirementsText;

        [Header("SP Cost")]
        [SerializeField] private TMP_Text spCostText;

        [Header("Positioning")]
        [SerializeField] private Vector2 offset = new Vector2(20, -20);
        [SerializeField] private float padding = 10f;

        [Header("Animation")]
        [SerializeField] private float fadeSpeed = 10f;

        private Canvas parentCanvas;
        private RectTransform canvasRect;
        private bool isShowing;
        private float targetAlpha;
        private SkillData currentSkill;
        private SkillInstance currentInstance;

        private void Awake()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                canvasRect = parentCanvas.GetComponent<RectTransform>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (tooltipRect == null)
            {
                tooltipRect = GetComponent<RectTransform>();
            }

            Hide();
        }

        private void Update()
        {
            // Fade animation
            if (canvasGroup != null && canvasGroup.alpha != targetAlpha)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
            }

            // Follow cursor when visible
            if (isShowing)
            {
                UpdatePosition();
            }
        }

        /// <summary>
        /// Shows the tooltip for a skill.
        /// </summary>
        public void Show(SkillData skill, SkillInstance instance = null)
        {
            if (skill == null)
            {
                Hide();
                return;
            }

            currentSkill = skill;
            currentInstance = instance;

            UpdateContent();
            UpdatePosition();

            isShowing = true;
            targetAlpha = 1f;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the tooltip.
        /// </summary>
        public void Hide()
        {
            isShowing = false;
            targetAlpha = 0f;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            gameObject.SetActive(false);
        }

        private void UpdateContent()
        {
            if (currentSkill == null) return;

            int level = currentInstance?.currentLevel ?? 0;
            bool isLearned = currentInstance != null;
            bool isMaxLevel = currentInstance?.IsMaxLevel ?? false;

            // Name
            if (skillNameText != null)
            {
                skillNameText.text = currentSkill.skillName;
            }

            // Type
            if (skillTypeText != null)
            {
                string typeStr = currentSkill.skillType.ToString();
                if (currentSkill.damageType != DamageType.Physical)
                {
                    typeStr += $" - {currentSkill.damageType}";
                }
                skillTypeText.text = typeStr;
            }

            // Level
            if (levelText != null)
            {
                if (isLearned)
                {
                    levelText.text = $"Level {level}/{currentSkill.maxSkillLevel}";
                    levelText.color = isMaxLevel ? Color.yellow : Color.white;
                }
                else
                {
                    levelText.text = "Not Learned";
                    levelText.color = Color.gray;
                }
            }

            // Icon
            if (skillIcon != null && currentSkill.icon != null)
            {
                skillIcon.sprite = currentSkill.icon;
            }

            // Description
            if (descriptionText != null)
            {
                int displayLevel = isLearned ? level : 1;
                descriptionText.text = currentSkill.GetFormattedDescription(displayLevel);
            }

            // Stats
            UpdateStats(isLearned ? level : 1);

            // Next level preview
            if (nextLevelContainer != null)
            {
                if (isLearned && !isMaxLevel)
                {
                    nextLevelContainer.SetActive(true);
                    if (nextLevelText != null)
                    {
                        nextLevelText.text = currentSkill.GetNextLevelDescription(level);
                    }
                }
                else
                {
                    nextLevelContainer.SetActive(false);
                }
            }

            // Requirements
            UpdateRequirements();

            // SP Cost
            if (spCostText != null)
            {
                if (isMaxLevel)
                {
                    spCostText.text = "MAX LEVEL";
                }
                else
                {
                    spCostText.text = $"SP Cost: {currentSkill.spCost}";
                }
            }

            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        }

        private void UpdateStats(int level)
        {
            if (statsContainer == null) return;

            if (damageText != null)
            {
                float damage = currentSkill.GetDamage(level);
                if (damage > 0)
                {
                    damageText.text = $"Damage: {damage:F0}";
                    damageText.gameObject.SetActive(true);
                }
                else
                {
                    damageText.gameObject.SetActive(false);
                }
            }

            if (manaCostText != null)
            {
                float manaCost = currentSkill.GetManaCost(level);
                manaCostText.text = $"Mana: {manaCost:F0}";
            }

            if (cooldownText != null)
            {
                float cooldown = currentSkill.GetCooldown(level);
                cooldownText.text = $"Cooldown: {cooldown:F1}s";
            }

            if (durationText != null)
            {
                float duration = currentSkill.GetDuration(level);
                if (duration > 0)
                {
                    durationText.text = $"Duration: {duration:F1}s";
                    durationText.gameObject.SetActive(true);
                }
                else
                {
                    durationText.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateRequirements()
        {
            if (requirementsContainer == null) return;

            var skillManager = SkillManager.Instance;
            bool canLearn = skillManager?.CanLearnSkill(currentSkill) ?? false;
            bool alreadyLearned = currentInstance != null;

            if (alreadyLearned || canLearn)
            {
                requirementsContainer.SetActive(false);
                return;
            }

            requirementsContainer.SetActive(true);

            if (requirementsText == null) return;

            var requirements = new System.Text.StringBuilder();

            // Level requirement
            if (skillManager != null && skillManager.PlayerLevel < currentSkill.requiredPlayerLevel)
            {
                requirements.AppendLine($"<color=red>Requires Level {currentSkill.requiredPlayerLevel}</color>");
            }

            // SP requirement
            if (skillManager != null && skillManager.AvailableSP < currentSkill.spCost)
            {
                requirements.AppendLine($"<color=red>Not enough SP ({skillManager.AvailableSP}/{currentSkill.spCost})</color>");
            }

            // Prerequisite skills
            if (currentSkill.prerequisiteSkills != null)
            {
                for (int i = 0; i < currentSkill.prerequisiteSkills.Length; i++)
                {
                    var prereq = currentSkill.prerequisiteSkills[i];
                    if (prereq == null) continue;

                    int requiredLevel = i < currentSkill.prerequisiteLevels.Length ? currentSkill.prerequisiteLevels[i] : 1;
                    int currentLevel = skillManager?.GetSkillLevel(prereq.skillId) ?? 0;

                    if (currentLevel < requiredLevel)
                    {
                        requirements.AppendLine($"<color=red>Requires {prereq.skillName} Lv.{requiredLevel}</color>");
                    }
                }
            }

            requirementsText.text = requirements.ToString().TrimEnd();
        }

        private void UpdatePosition()
        {
            if (tooltipRect == null || canvasRect == null) return;

            // Get mouse position via Input System
            Vector2 screenPos = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : Vector2.zero;

            Vector2 mousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                parentCanvas.worldCamera,
                out mousePos
            );

            // Apply offset
            Vector2 tooltipPos = mousePos + offset;

            // Clamp to canvas bounds
            Vector2 tooltipSize = tooltipRect.sizeDelta;
            Vector2 canvasSize = canvasRect.sizeDelta;

            float halfWidth = canvasSize.x / 2f;
            float halfHeight = canvasSize.y / 2f;

            // Clamp X
            if (tooltipPos.x + tooltipSize.x > halfWidth - padding)
            {
                tooltipPos.x = mousePos.x - tooltipSize.x - offset.x;
            }
            if (tooltipPos.x < -halfWidth + padding)
            {
                tooltipPos.x = -halfWidth + padding;
            }

            // Clamp Y
            if (tooltipPos.y - tooltipSize.y < -halfHeight + padding)
            {
                tooltipPos.y = mousePos.y + tooltipSize.y + Mathf.Abs(offset.y);
            }
            if (tooltipPos.y > halfHeight - padding)
            {
                tooltipPos.y = halfHeight - padding;
            }

            tooltipRect.anchoredPosition = tooltipPos;
        }
    }
}
