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
        [SerializeField] private Button assignHotbarButton;
        [SerializeField] private TMP_Text assignHotbarButtonText;

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

            if (assignHotbarButton != null)
            {
                assignHotbarButton.onClick.AddListener(OnAssignHotbarClicked);
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
            Button learnButton, TMP_Text learnButtonText,
            Button assignHotbarButton = null, TMP_Text assignHotbarButtonText = null)
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
            this.assignHotbarButton = assignHotbarButton;
            this.assignHotbarButtonText = assignHotbarButtonText;

            // Wire button listeners here because Awake() runs before references
            // are set when using AddComponent + SetRuntimeReferences pattern.
            if (this.learnButton != null)
                this.learnButton.onClick.AddListener(OnLearnButtonClicked);
            if (this.assignHotbarButton != null)
                this.assignHotbarButton.onClick.AddListener(OnAssignHotbarClicked);
        }

        /// <summary>
        /// Sets the skill node prefab for runtime-built UIs.
        /// </summary>
        public void SetRuntimeNodePrefab(GameObject prefab)
        {
            this.skillNodePrefab = prefab;
        }

        /// <summary>
        /// Loads and displays a skill tree.
        /// </summary>
        public void LoadTree(SkillTreeData treeData)
        {
            if (treeData == null) return;

            ClearTree();
            currentTree = treeData;

            CalculateLayout();
            CreateNodes();
            CreateConnections();
            RefreshHeader();
            RefreshAllNodes();
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
                nodeGO.SetActive(true);
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
                nodeUI.OnNodeRightClicked += HandleNodeRightClicked;
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

        private void HandleNodeRightClicked(SkillNodeUI node)
        {
            // Right-click opens hotbar assignment if skill is learned and usable
            if (node.SkillInstance == null) return;
            if (node.SkillInstance.SkillType == SkillType.Passive) return;

            selectedNode = node;
            UpdateSkillInfoPanel(node);
            ShowHotbarAssignPopup(node.SkillData.skillId);
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

            if (skillIconImage != null)
            {
                var resolvedIcon = SkillIconHelper.ResolveIcon(skill);
                if (resolvedIcon != null)
                {
                    skillIconImage.sprite = resolvedIcon;
                    skillIconImage.color = SkillIconHelper.ResolveTint(skill);
                    skillIconImage.enabled = true;
                }
                else
                {
                    skillIconImage.enabled = false;
                }
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
                stats.AppendLine($"Type: {skill.skillType}");
                if (skill.GetDamage(level) > 0)
                    stats.AppendLine($"Damage: {skill.GetDamage(level):F0}");
                if (skill.GetManaCost(level) > 0)
                    stats.AppendLine($"Mana Cost: {skill.GetManaCost(level):F0}");
                if (skill.GetCooldown(level) > 0)
                    stats.AppendLine($"Cooldown: {skill.GetCooldown(level):F1}s");
                if (skill.GetDuration(level) > 0)
                    stats.AppendLine($"Duration: {skill.GetDuration(level):F1}s");

                stats.AppendLine($"SP Cost: {skill.spCost}");
                if (instance != null)
                    stats.AppendLine($"Level: {instance.currentLevel} / {skill.maxSkillLevel}");
                else
                    stats.AppendLine($"Max Level: {skill.maxSkillLevel}");

                skillStatsText.text = stats.ToString();
            }

            if (skillRequirementsText != null)
            {
                var reqs = new System.Text.StringBuilder();
                var skillManager = SkillManager.Instance;

                if (skill.requiredPlayerLevel > 1)
                {
                    bool met = skillManager != null && skillManager.PlayerLevel >= skill.requiredPlayerLevel;
                    reqs.AppendLine(met ? $"<color=#88CC88>Lv. {skill.requiredPlayerLevel} Required</color>"
                                       : $"<color=#CC8888>Lv. {skill.requiredPlayerLevel} Required</color>");
                }

                if (skill.prerequisiteSkills != null)
                {
                    foreach (var prereq in skill.prerequisiteSkills)
                    {
                        if (prereq == null) continue;
                        bool met = skillManager?.GetLearnedSkill(prereq.skillId) != null;
                        reqs.AppendLine(met ? $"<color=#88CC88>Requires: {prereq.skillName}</color>"
                                           : $"<color=#CC8888>Requires: {prereq.skillName}</color>");
                    }
                }

                if (skillManager != null && instance == null && !skillManager.CanLearnSkill(skill))
                {
                    if (skillManager.AvailableSP < skill.spCost)
                        reqs.AppendLine($"<color=#CC8888>Need {skill.spCost} SP (have {skillManager.AvailableSP})</color>");
                }

                skillRequirementsText.text = reqs.ToString();
            }

            UpdateLearnButton(node);
            UpdateAssignHotbarButton(node);
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

        private void UpdateAssignHotbarButton(SkillNodeUI node)
        {
            if (assignHotbarButton == null) return;

            var instance = node.SkillInstance;
            // Show assign button only for learned Active/Buff/Toggle skills
            bool show = instance != null && instance.SkillType != SkillType.Passive;
            assignHotbarButton.gameObject.SetActive(show);

            if (show && assignHotbarButtonText != null)
            {
                var controller = UnityEngine.Object.FindAnyObjectByType<PlayerSkillController>();
                bool alreadyAssigned = controller != null && controller.IsSkillOnHotbar(instance.SkillId);
                assignHotbarButtonText.text = alreadyAssigned ? "Reassign Hotbar" : "Assign to Hotbar";
            }
        }

        private void OnAssignHotbarClicked()
        {
            if (selectedNode?.SkillInstance == null) return;
            if (selectedNode.SkillInstance.SkillType == SkillType.Passive) return;

            ShowHotbarAssignPopup(selectedNode.SkillData.skillId);
        }

        /// <summary>
        /// Opens the hotbar slot picker for the given skill.
        /// Called by both the assign button and right-click on nodes.
        /// </summary>
        public void ShowHotbarAssignPopup(string skillId)
        {
            var popup = SkillHotbarAssignPopup.Instance;
            if (popup == null)
                popup = SkillHotbarAssignPopup.CreateRuntimeUI(transform.root);

            popup.Show(skillId);
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

        /// <summary>
        /// Calculates treeOffset and content size so nodes are centered
        /// horizontally and laid out top-to-bottom in a scrollable list.
        /// Must be called before CreateNodes().
        /// </summary>
        private void CalculateLayout()
        {
            if (currentTree?.nodes == null || contentContainer == null || scrollRect == null)
                return;

            int maxRow = 0, maxCol = 0;
            foreach (var node in currentTree.nodes)
            {
                if (node.skill == null) continue;
                if (node.row > maxRow) maxRow = node.row;
                if (node.column > maxCol) maxCol = node.column;
            }

            float hSpacing = currentTree.horizontalSpacing;
            float vSpacing = currentTree.verticalSpacing;
            float nodeSize = 70f;
            float topPadding = 30f;
            float bottomPadding = 50f;

            // Center the grid horizontally.
            // Nodes are anchored at (0.5, 0.5) of nodesContainer (= center of content).
            // Grid spans from col 0 to maxCol, so center of grid = maxCol * hSpacing / 2.
            treeOffset.x = -(maxCol * hSpacing) / 2f;

            // Position row 0 near the top of the content.
            // Content height = topPadding + nodeSize + maxRow * vSpacing + bottomPadding
            float contentHeight = topPadding + nodeSize + maxRow * vSpacing + bottomPadding;

            // treeOffset.y places row-0 nodes so their top edge is topPadding below
            // the content top. Content center y = contentHeight / 2 from top.
            treeOffset.y = (contentHeight / 2f) - topPadding - (nodeSize / 2f);

            // Resize content — width matches viewport (sizeDelta.x = 0 with stretch anchors),
            // height is the calculated value.
            contentContainer.sizeDelta = new Vector2(contentContainer.sizeDelta.x, contentHeight);

            // Reset scroll to top
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
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
