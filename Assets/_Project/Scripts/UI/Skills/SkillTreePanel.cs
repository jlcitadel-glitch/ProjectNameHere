using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Main container for the skill tree UI.
    /// Manages node creation, connections, and interactions.
    /// </summary>
    public class SkillTreePanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentContainer;
        [SerializeField] private RectTransform nodesContainer;
        [SerializeField] private RectTransform connectionsContainer;

        [Header("Header")]
        [SerializeField] private TMP_Text jobTitleText;
        [SerializeField] private Image jobIconImage;
        [SerializeField] private TMP_Text spDisplayText;
        [SerializeField] private TMP_Text levelDisplayText;

        [Header("Skill Info Panel")]
        [SerializeField] private GameObject skillInfoPanel;
        [SerializeField] private TMP_Text skillNameText;
        [SerializeField] private Image skillIconImage;
        [SerializeField] private TMP_Text skillDescriptionText;
        [SerializeField] private TMP_Text skillStatsText;
        [SerializeField] private TMP_Text skillRequirementsText;
        [SerializeField] private Button learnButton;
        [SerializeField] private TMP_Text learnButtonText;

        [Header("Prefabs")]
        [SerializeField] private GameObject skillNodePrefab;

        [Header("Tooltip")]
        [SerializeField] private SkillTooltip tooltip;

        [Header("Job Advancement")]
        [SerializeField] private JobAdvancementPopup advancementPopup;

        [Header("Layout")]
        [SerializeField] private Vector2 nodeSpacing = new Vector2(150f, 120f);
        [SerializeField] private Vector2 treeOffset = new Vector2(0f, -100f);

        // Runtime
        private SkillTreeData currentTree;
        private Dictionary<string, SkillNodeUI> nodesBySkillId = new Dictionary<string, SkillNodeUI>();
        private List<SkillConnectionLine> connections = new List<SkillConnectionLine>();
        private SkillNodeUI selectedNode;
        private CanvasGroup canvasGroup;

        public event System.Action<SkillData> OnSkillSelected;
        public event System.Action<SkillData> OnSkillLearned;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            if (learnButton != null)
            {
                learnButton.onClick.AddListener(OnLearnButtonClicked);
            }

            // Hide skill info panel initially
            if (skillInfoPanel != null)
            {
                skillInfoPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // Subscribe to skill manager events
            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.OnSkillLearned += HandleSkillLearned;
                SkillManager.Instance.OnSkillLevelChanged += HandleSkillLevelChanged;
                SkillManager.Instance.OnSPChanged += HandleSPChanged;
                SkillManager.Instance.OnJobChanged += HandleJobChanged;
            }

            RefreshHeader();
            RefreshAllNodes();
        }

        private void OnDisable()
        {
            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.OnSkillLearned -= HandleSkillLearned;
                SkillManager.Instance.OnSkillLevelChanged -= HandleSkillLevelChanged;
                SkillManager.Instance.OnSPChanged -= HandleSPChanged;
                SkillManager.Instance.OnJobChanged -= HandleJobChanged;
            }
        }

        /// <summary>
        /// Assigns serialized field references from the runtime UI builder.
        /// </summary>
        public void SetRuntimeReferences(
            ScrollRect scrollRect, RectTransform contentContainer,
            RectTransform nodesContainer, RectTransform connectionsContainer,
            TMP_Text jobTitleText, Image jobIconImage,
            TMP_Text spDisplayText, TMP_Text levelDisplayText,
            GameObject skillInfoPanel, TMP_Text skillNameText,
            Image skillIconImage, TMP_Text skillDescriptionText,
            TMP_Text skillStatsText, TMP_Text skillRequirementsText,
            Button learnButton, TMP_Text learnButtonText)
        {
            this.scrollRect = scrollRect;
            this.contentContainer = contentContainer;
            this.nodesContainer = nodesContainer;
            this.connectionsContainer = connectionsContainer;
            this.jobTitleText = jobTitleText;
            this.jobIconImage = jobIconImage;
            this.spDisplayText = spDisplayText;
            this.levelDisplayText = levelDisplayText;
            this.skillInfoPanel = skillInfoPanel;
            this.skillNameText = skillNameText;
            this.skillIconImage = skillIconImage;
            this.skillDescriptionText = skillDescriptionText;
            this.skillStatsText = skillStatsText;
            this.skillRequirementsText = skillRequirementsText;
            this.learnButton = learnButton;
            this.learnButtonText = learnButtonText;
        }

        /// <summary>
        /// Loads and displays a skill tree.
        /// </summary>
        public void LoadTree(SkillTreeData treeData)
        {
            if (treeData == null) return;

            ClearTree();
            currentTree = treeData;

            CreateNodes();
            CreateConnections();
            RefreshHeader();
            RefreshAllNodes();

            // Center view on root nodes
            CenterOnRootNodes();
        }

        /// <summary>
        /// Clears the current tree display.
        /// </summary>
        public void ClearTree()
        {
            // Destroy existing nodes
            foreach (var node in nodesBySkillId.Values)
            {
                if (node != null)
                    Destroy(node.gameObject);
            }
            nodesBySkillId.Clear();

            // Destroy connections
            foreach (var connection in connections)
            {
                if (connection != null)
                    Destroy(connection.gameObject);
            }
            connections.Clear();

            selectedNode = null;
            currentTree = null;
        }

        private void CreateNodes()
        {
            if (currentTree?.nodes == null || skillNodePrefab == null || nodesContainer == null)
                return;

            foreach (var nodeData in currentTree.nodes)
            {
                if (nodeData.skill == null) continue;

                // Instantiate node
                var nodeGO = Instantiate(skillNodePrefab, nodesContainer);
                var nodeUI = nodeGO.GetComponent<SkillNodeUI>();

                if (nodeUI == null)
                {
                    Debug.LogError("[SkillTreePanel] SkillNodePrefab missing SkillNodeUI component");
                    Destroy(nodeGO);
                    continue;
                }

                // Initialize
                nodeUI.Initialize(nodeData.skill);

                // Position
                Vector2 position = currentTree.GetNodeWorldPosition(nodeData) + treeOffset;
                nodeUI.SetPosition(position);

                // Subscribe to events
                nodeUI.OnNodeClicked += HandleNodeClicked;
                nodeUI.OnNodeHovered += HandleNodeHovered;
                nodeUI.OnNodeUnhovered += HandleNodeUnhovered;

                nodesBySkillId[nodeData.skill.skillId] = nodeUI;
            }
        }

        private void CreateConnections()
        {
            if (currentTree?.connections == null || connectionsContainer == null)
                return;

            foreach (var connection in currentTree.connections)
            {
                if (connection.fromNodeIndex < 0 || connection.fromNodeIndex >= currentTree.nodes.Length)
                    continue;
                if (connection.toNodeIndex < 0 || connection.toNodeIndex >= currentTree.nodes.Length)
                    continue;

                var fromNode = currentTree.nodes[connection.fromNodeIndex];
                var toNode = currentTree.nodes[connection.toNodeIndex];

                if (fromNode.skill == null || toNode.skill == null)
                    continue;

                if (!nodesBySkillId.TryGetValue(fromNode.skill.skillId, out var fromUI))
                    continue;
                if (!nodesBySkillId.TryGetValue(toNode.skill.skillId, out var toUI))
                    continue;

                var line = SkillConnectionLine.Create(connectionsContainer, fromUI, toUI);
                connections.Add(line);
            }
        }

        private void RefreshHeader()
        {
            var skillManager = SkillManager.Instance;
            if (skillManager == null) return;

            var currentJob = skillManager.CurrentJob;

            if (jobTitleText != null)
            {
                jobTitleText.text = currentJob?.jobName ?? "No Job";
            }

            if (jobIconImage != null && currentJob?.jobIcon != null)
            {
                jobIconImage.sprite = currentJob.jobIcon;
            }

            if (spDisplayText != null)
            {
                spDisplayText.text = $"SP: {skillManager.AvailableSP}";
            }

            if (levelDisplayText != null)
            {
                levelDisplayText.text = $"Lv. {skillManager.PlayerLevel}";
            }
        }

        private void RefreshAllNodes()
        {
            foreach (var node in nodesBySkillId.Values)
            {
                node.RefreshState();
            }

            foreach (var connection in connections)
            {
                connection.RefreshState();
            }

            // Refresh selected skill info
            if (selectedNode != null)
            {
                UpdateSkillInfoPanel(selectedNode);
            }
        }

        private void HandleNodeClicked(SkillNodeUI node)
        {
            selectedNode = node;
            UpdateSkillInfoPanel(node);
            OnSkillSelected?.Invoke(node.SkillData);
        }

        private void HandleNodeHovered(SkillNodeUI node)
        {
            if (tooltip != null)
            {
                tooltip.Show(node.SkillData, node.SkillInstance);
            }
        }

        private void HandleNodeUnhovered(SkillNodeUI node)
        {
            if (tooltip != null)
            {
                tooltip.Hide();
            }
        }

        private void UpdateSkillInfoPanel(SkillNodeUI node)
        {
            if (skillInfoPanel == null) return;

            skillInfoPanel.SetActive(true);

            var skill = node.SkillData;
            var instance = node.SkillInstance;

            if (skillNameText != null)
            {
                skillNameText.text = skill.skillName;
            }

            if (skillIconImage != null && skill.icon != null)
            {
                skillIconImage.sprite = skill.icon;
            }

            if (skillDescriptionText != null)
            {
                int level = instance?.currentLevel ?? 1;
                skillDescriptionText.text = skill.GetFormattedDescription(level);
            }

            if (skillStatsText != null)
            {
                int level = instance?.currentLevel ?? 1;
                var stats = new System.Text.StringBuilder();
                stats.AppendLine($"Damage: {skill.GetDamage(level):F0}");
                stats.AppendLine($"Mana Cost: {skill.GetManaCost(level):F0}");
                stats.AppendLine($"Cooldown: {skill.GetCooldown(level):F1}s");
                if (skill.GetDuration(level) > 0)
                    stats.AppendLine($"Duration: {skill.GetDuration(level):F1}s");
                skillStatsText.text = stats.ToString();
            }

            UpdateLearnButton(node);
        }

        private void UpdateLearnButton(SkillNodeUI node)
        {
            if (learnButton == null) return;

            var skillManager = SkillManager.Instance;
            if (skillManager == null) return;

            var skill = node.SkillData;
            var instance = node.SkillInstance;

            if (instance != null && instance.IsMaxLevel)
            {
                // Maxed
                learnButton.interactable = false;
                if (learnButtonText != null)
                    learnButtonText.text = "MAX LEVEL";
            }
            else if (instance != null)
            {
                // Upgrade
                bool canUpgrade = skillManager.CanUpgradeSkill(skill.skillId);
                learnButton.interactable = canUpgrade;
                if (learnButtonText != null)
                    learnButtonText.text = $"Upgrade (SP: {skill.spCost})";
            }
            else if (skillManager.CanLearnSkill(skill))
            {
                // Learn
                learnButton.interactable = true;
                if (learnButtonText != null)
                    learnButtonText.text = $"Learn (SP: {skill.spCost})";
            }
            else
            {
                // Locked
                learnButton.interactable = false;
                if (learnButtonText != null)
                    learnButtonText.text = "Locked";
            }
        }

        private void OnLearnButtonClicked()
        {
            if (selectedNode == null) return;

            var skillManager = SkillManager.Instance;
            if (skillManager == null) return;

            var skill = selectedNode.SkillData;
            bool success = false;

            if (selectedNode.SkillInstance != null)
            {
                // Upgrade
                success = skillManager.UpgradeSkill(skill.skillId);
            }
            else
            {
                // Learn
                success = skillManager.LearnSkill(skill);
            }

            if (success)
            {
                selectedNode.PlayLearnAnimation();
                OnSkillLearned?.Invoke(skill);

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.PlayConfirmSound();
                }
            }
            else
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.PlayErrorSound();
                }
            }
        }

        private void HandleSkillLearned(SkillInstance instance)
        {
            RefreshAllNodes();
        }

        private void HandleSkillLevelChanged(SkillInstance instance, int oldLevel, int newLevel)
        {
            RefreshAllNodes();
        }

        private void HandleSPChanged(int oldSP, int newSP)
        {
            RefreshHeader();
            RefreshAllNodes();
        }

        private void HandleJobChanged(JobClassData oldJob, JobClassData newJob)
        {
            RefreshHeader();

            // Load new job's skill tree
            if (newJob?.skillTree != null)
            {
                LoadTree(newJob.skillTree);
            }
        }

        private void CenterOnRootNodes()
        {
            if (scrollRect == null || contentContainer == null) return;

            // Find the topmost node (row 0)
            Vector2 centerPos = Vector2.zero;
            int count = 0;

            if (currentTree?.nodes != null)
            {
                foreach (var node in currentTree.nodes)
                {
                    if (node.row == 0)
                    {
                        centerPos += currentTree.GetNodeWorldPosition(node);
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                centerPos /= count;
                // Adjust scroll position to center on these nodes
                // This is a simplified approach - a full implementation would calculate proper normalized position
            }
        }

        /// <summary>
        /// Shows the job advancement popup if advancements are available.
        /// </summary>
        public void ShowAdvancementPopup()
        {
            if (advancementPopup != null)
            {
                advancementPopup.Show();
            }
        }
    }
}
