using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// UI component for a single skill node in the skill tree.
    /// Handles visual states, hover, and click interactions.
    /// </summary>
    public class SkillNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler
    {
        public enum NodeState
        {
            Locked,      // Prerequisites not met
            Available,   // Can be learned
            Learned,     // Learned but not maxed
            Maxed        // At maximum level
        }

        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image lockOverlay;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private GameObject spCostBadge;
        [SerializeField] private TMP_Text spCostText;

        [Header("State Colors")]
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color availableColor = new Color(0.8f, 0.7f, 0.2f, 1f);
        [SerializeField] private Color learnedColor = new Color(0.4f, 0.6f, 0.9f, 1f);
        [SerializeField] private Color maxedColor = new Color(0.9f, 0.8f, 0.2f, 1f);

        [Header("Animation")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float animationSpeed = 8f;

        // Runtime
        private SkillData skillData;
        private SkillInstance skillInstance;
        private NodeState currentState;
        private RectTransform rectTransform;
        private Vector3 originalScale;
        private Vector3 targetScale;
        private bool isHovered;
        private bool isSelected;

        public SkillData SkillData => skillData;
        public SkillInstance SkillInstance => skillInstance;
        public NodeState State => currentState;

        public event System.Action<SkillNodeUI> OnNodeClicked;
        public event System.Action<SkillNodeUI> OnNodeHovered;
        public event System.Action<SkillNodeUI> OnNodeUnhovered;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            originalScale = transform.localScale;
            targetScale = originalScale;
        }

        private void Update()
        {
            // Smooth scale animation
            if (transform.localScale != targetScale)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);
            }
        }

        /// <summary>
        /// Initializes the node with skill data.
        /// </summary>
        public void Initialize(SkillData skill)
        {
            skillData = skill;

            if (skill == null)
            {
                gameObject.SetActive(false);
                return;
            }

            // Set icon
            if (iconImage != null && skill.icon != null)
            {
                iconImage.sprite = skill.icon;
            }

            // Update state
            RefreshState();
        }

        /// <summary>
        /// Refreshes the node's visual state based on current skill state.
        /// </summary>
        public void RefreshState()
        {
            if (skillData == null) return;

            var skillManager = SkillManager.Instance;
            if (skillManager == null) return;

            // Get skill instance if learned
            skillInstance = skillManager.GetLearnedSkill(skillData.skillId);

            // Determine state
            if (skillInstance != null)
            {
                currentState = skillInstance.IsMaxLevel ? NodeState.Maxed : NodeState.Learned;
            }
            else if (skillManager.CanLearnSkill(skillData))
            {
                currentState = NodeState.Available;
            }
            else
            {
                currentState = NodeState.Locked;
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            Color frameColor = GetStateColor();

            // Frame color
            if (frameImage != null)
            {
                frameImage.color = frameColor;
            }

            // Lock overlay
            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(currentState == NodeState.Locked);
            }

            // Icon saturation
            if (iconImage != null)
            {
                iconImage.color = currentState == NodeState.Locked
                    ? new Color(0.5f, 0.5f, 0.5f, 1f)
                    : Color.white;
            }

            // Level text
            if (levelText != null)
            {
                if (skillInstance != null)
                {
                    levelText.text = $"{skillInstance.currentLevel}/{skillData.maxSkillLevel}";
                    levelText.gameObject.SetActive(true);
                }
                else
                {
                    levelText.text = $"0/{skillData.maxSkillLevel}";
                    levelText.gameObject.SetActive(currentState != NodeState.Locked);
                }
            }

            // SP cost badge
            if (spCostBadge != null)
            {
                bool showCost = currentState == NodeState.Available ||
                               (currentState == NodeState.Learned && !skillInstance.IsMaxLevel);
                spCostBadge.SetActive(showCost);

                if (showCost && spCostText != null)
                {
                    spCostText.text = skillData.spCost.ToString();
                }
            }
        }

        private Color GetStateColor()
        {
            return currentState switch
            {
                NodeState.Locked => lockedColor,
                NodeState.Available => availableColor,
                NodeState.Learned => learnedColor,
                NodeState.Maxed => maxedColor,
                _ => lockedColor
            };
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            targetScale = originalScale * hoverScale;
            OnNodeHovered?.Invoke(this);

            // Play hover sound
            if (UIManager.Instance != null)
            {
                UIManager.Instance.PlayNavigateSound();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            if (!isSelected)
            {
                targetScale = originalScale;
            }
            OnNodeUnhovered?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnNodeClicked?.Invoke(this);

            // Play click sound
            if (UIManager.Instance != null)
            {
                UIManager.Instance.PlaySelectSound();
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            isSelected = true;
            targetScale = originalScale * hoverScale;
            OnNodeHovered?.Invoke(this);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            if (!isHovered)
            {
                targetScale = originalScale;
            }
            OnNodeUnhovered?.Invoke(this);
        }

        /// <summary>
        /// Sets the node position in the tree.
        /// </summary>
        public void SetPosition(Vector2 position)
        {
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = position;
            }
        }

        /// <summary>
        /// Gets the center position for connection lines.
        /// </summary>
        public Vector2 GetCenterPosition()
        {
            if (rectTransform == null) return Vector2.zero;
            return rectTransform.anchoredPosition;
        }

        /// <summary>
        /// Plays a skill learned animation.
        /// </summary>
        public void PlayLearnAnimation()
        {
            // Simple scale punch effect
            StartCoroutine(LearnAnimationCoroutine());
        }

        private System.Collections.IEnumerator LearnAnimationCoroutine()
        {
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                // Pulse effect
                float pulse = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
                transform.localScale = originalScale * pulse;

                yield return null;
            }

            transform.localScale = originalScale;
            targetScale = originalScale;
        }
    }
}
