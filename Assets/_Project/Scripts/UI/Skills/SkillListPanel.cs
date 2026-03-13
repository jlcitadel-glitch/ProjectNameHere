using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Vertical skill list panel for the Skills tab of the character menu.
    /// Displays skills sorted by dependency tier in a scrollable list with
    /// a detail panel for the selected skill.
    /// </summary>
    public class SkillListPanel : MonoBehaviour
    {
        // Header
        private TMP_Text spDisplayText;
        private TMP_Text levelDisplayText;

        // Stats summary
        private TMP_Text hpStatText;
        private TMP_Text mpStatText;
        private TMP_Text strStatText;
        private TMP_Text intStatText;
        private TMP_Text agiStatText;

        // Detail panel
        private GameObject detailPanel;
        private Image detailIcon;
        private TMP_Text detailName;
        private TMP_Text detailDesc;
        private TMP_Text detailStats;
        private TMP_Text detailRequirements;
        private Button learnButton;
        private TMP_Text learnButtonText;
        private Button assignHotbarButton;
        private TMP_Text assignHotbarButtonText;

        // Scroll area
        private ScrollRect scrollRect;
        private Transform rowContainer;

        // Runtime state
        private SkillTreeData currentTree;
        private readonly Dictionary<string, SkillRowRef> rowsBySkillId = new();
        private string selectedSkillId;

        // Color palette
        private static readonly Color AgedGold = new(0.812f, 0.710f, 0.231f, 1f);
        private static readonly Color BoneWhite = new(0.961f, 0.961f, 0.863f, 1f);
        private static readonly Color SubtleText = new(0.7f, 0.65f, 0.55f, 1f);
        private static readonly Color RowNormal = new(0.12f, 0.12f, 0.15f, 0.8f);
        private static readonly Color RowHover = new(0.18f, 0.18f, 0.22f, 0.9f);
        private static readonly Color RowPress = new(0.25f, 0.22f, 0.12f, 0.9f);
        private static readonly Color RowSelected = new(0.2f, 0.18f, 0.1f, 0.9f);
        private static readonly Color LockedColor = new(0.3f, 0.3f, 0.3f, 1f);
        private static readonly Color AvailableColor = new(0.8f, 0.7f, 0.2f, 1f);
        private static readonly Color LearnedColor = new(0.4f, 0.6f, 0.9f, 1f);
        private static readonly Color MaxedColor = new(0.9f, 0.8f, 0.2f, 1f);

        private static Sprite _whiteSprite;
        private static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
                }
                return _whiteSprite;
            }
        }

        private struct SkillRowRef
        {
            public SkillData skill;
            public GameObject row;
            public Image stateIndicator;
            public Image icon;
            public TMP_Text nameText;
            public TMP_Text levelText;
            public TMP_Text spText;
            public Image rowBg;
        }

        /// <summary>
        /// Assigns all UI references from the runtime builder.
        /// </summary>
        public void SetRuntimeReferences(
            TMP_Text spDisplay, TMP_Text levelDisplay,
            TMP_Text hpStat, TMP_Text mpStat,
            TMP_Text strStat, TMP_Text intStat, TMP_Text agiStat,
            GameObject detailPanel, Image detailIcon,
            TMP_Text detailName, TMP_Text detailDesc,
            TMP_Text detailStats, TMP_Text detailRequirements,
            Button learnBtn, TMP_Text learnBtnText,
            Button assignBtn, TMP_Text assignBtnText,
            ScrollRect scroll, Transform rowContainer)
        {
            spDisplayText = spDisplay;
            levelDisplayText = levelDisplay;
            hpStatText = hpStat;
            mpStatText = mpStat;
            strStatText = strStat;
            intStatText = intStat;
            agiStatText = agiStat;
            this.detailPanel = detailPanel;
            this.detailIcon = detailIcon;
            this.detailName = detailName;
            this.detailDesc = detailDesc;
            this.detailStats = detailStats;
            this.detailRequirements = detailRequirements;
            learnButton = learnBtn;
            learnButtonText = learnBtnText;
            assignHotbarButton = assignBtn;
            assignHotbarButtonText = assignBtnText;
            this.scrollRect = scroll;
            this.rowContainer = rowContainer;

            if (learnButton != null)
                learnButton.onClick.AddListener(OnLearnButtonClicked);
            if (assignHotbarButton != null)
                assignHotbarButton.onClick.AddListener(OnAssignHotbarClicked);

            if (this.detailPanel != null)
                this.detailPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.OnSkillLearned += HandleSkillLearned;
                SkillManager.Instance.OnSkillLevelChanged += HandleSkillLevelChanged;
                SkillManager.Instance.OnSPChanged += HandleSPChanged;
                SkillManager.Instance.OnJobChanged += HandleJobChanged;
            }

            RefreshHeader();
            RefreshAllRows();
            RefreshStatsSummary();
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

        #region Load & Create

        /// <summary>
        /// Loads and displays skills from a tree data asset.
        /// </summary>
        public void LoadTree(SkillTreeData treeData)
        {
            if (treeData == null) return;

            if (treeData == currentTree)
            {
                RefreshHeader();
                RefreshAllRows();
                RefreshStatsSummary();
                return;
            }

            ClearRows();
            currentTree = treeData;
            CreateRows();
            RefreshHeader();
            RefreshAllRows();
            RefreshStatsSummary();
        }

        private void ClearRows()
        {
            foreach (var kvp in rowsBySkillId)
            {
                if (kvp.Value.row != null)
                    Destroy(kvp.Value.row);
            }
            rowsBySkillId.Clear();
            selectedSkillId = null;
            currentTree = null;

            if (detailPanel != null)
                detailPanel.SetActive(false);
        }

        private void CreateRows()
        {
            if (currentTree?.nodes == null || rowContainer == null) return;

            var sorted = SortByDependencyTier(currentTree);

            foreach (var skill in sorted)
            {
                if (skill == null) continue;
                CreateSkillRow(skill);
            }
        }

        private void CreateSkillRow(SkillData skill)
        {
            var rowGo = new GameObject(skill.skillId, typeof(RectTransform));
            rowGo.transform.SetParent(rowContainer, false);

            // Row background + button
            var rowBg = rowGo.AddComponent<Image>();
            rowBg.sprite = WhiteSprite;
            rowBg.color = RowNormal;

            var btn = rowGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = RowNormal;
            colors.highlightedColor = RowHover;
            colors.pressedColor = RowPress;
            colors.selectedColor = RowHover;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 6, 6);
            hlg.spacing = 8;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var rowLE = rowGo.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 60;

            // State indicator (12x12 dot)
            var stateGo = new GameObject("State", typeof(RectTransform));
            stateGo.transform.SetParent(rowGo.transform, false);
            var stateImg = stateGo.AddComponent<Image>();
            stateImg.sprite = WhiteSprite;
            stateImg.color = LockedColor;
            var stateLE = stateGo.AddComponent<LayoutElement>();
            stateLE.preferredWidth = 12;
            stateLE.preferredHeight = 12;

            // Icon (48x48)
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(rowGo.transform, false);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            var resolvedIcon = SkillIconHelper.ResolveIcon(skill);
            if (resolvedIcon != null)
            {
                iconImg.sprite = resolvedIcon;
                iconImg.color = SkillIconHelper.ResolveTint(skill);
            }
            else
            {
                iconImg.sprite = WhiteSprite;
                iconImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            }
            var iconLE = iconGo.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 48;
            iconLE.preferredHeight = 48;

            // Name + type (flexible)
            var nameGo = new GameObject("Name", typeof(RectTransform));
            nameGo.transform.SetParent(rowGo.transform, false);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            string typeColorHex = ColorUtility.ToHtmlStringRGB(SubtleText);
            nameTmp.text = $"{skill.skillName}\n<size=11><color=#{typeColorHex}>{skill.skillType}</color></size>";
            nameTmp.fontSize = 16;
            nameTmp.color = BoneWhite;
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.richText = true;
            nameTmp.raycastTarget = false;
            FontManager.EnsureFont(nameTmp);
            var nameLE = nameGo.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1;

            // Level text
            var lvlGo = new GameObject("Level", typeof(RectTransform));
            lvlGo.transform.SetParent(rowGo.transform, false);
            var lvlTmp = lvlGo.AddComponent<TextMeshProUGUI>();
            lvlTmp.text = $"0/{skill.maxSkillLevel}";
            lvlTmp.fontSize = 14;
            lvlTmp.color = BoneWhite;
            lvlTmp.alignment = TextAlignmentOptions.Center;
            lvlTmp.raycastTarget = false;
            FontManager.EnsureFont(lvlTmp);
            var lvlLE = lvlGo.AddComponent<LayoutElement>();
            lvlLE.preferredWidth = 50;

            // SP cost
            var spGo = new GameObject("SP", typeof(RectTransform));
            spGo.transform.SetParent(rowGo.transform, false);
            var spTmp = spGo.AddComponent<TextMeshProUGUI>();
            spTmp.text = $"{skill.spCost} SP";
            spTmp.fontSize = 13;
            spTmp.color = AgedGold;
            spTmp.alignment = TextAlignmentOptions.Center;
            spTmp.raycastTarget = false;
            FontManager.EnsureFont(spTmp);
            var spLE = spGo.AddComponent<LayoutElement>();
            spLE.preferredWidth = 50;

            // Wire click
            string capturedId = skill.skillId;
            btn.onClick.AddListener(() => SelectSkill(capturedId));

            rowsBySkillId[skill.skillId] = new SkillRowRef
            {
                skill = skill,
                row = rowGo,
                stateIndicator = stateImg,
                icon = iconImg,
                nameText = nameTmp,
                levelText = lvlTmp,
                spText = spTmp,
                rowBg = rowBg,
            };
        }

        #endregion

        #region Sorting

        /// <summary>
        /// Topological sort: skills with no prerequisites first, then dependents.
        /// Within the same tier, preserves original row order from the tree data.
        /// </summary>
        private static List<SkillData> SortByDependencyTier(SkillTreeData tree)
        {
            if (tree.nodes == null || tree.nodes.Length == 0)
                return new List<SkillData>();

            // Build skill → tier map via BFS
            var tierMap = new Dictionary<string, int>();
            var allSkills = new List<SkillData>();

            foreach (var node in tree.nodes)
            {
                if (node.skill == null) continue;
                allSkills.Add(node.skill);
                tierMap[node.skill.skillId] = -1; // unvisited
            }

            // Assign tiers: skills with no prereqs = tier 0
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var skill in allSkills)
                {
                    if (tierMap[skill.skillId] >= 0) continue;

                    if (skill.prerequisiteSkills == null || skill.prerequisiteSkills.Length == 0)
                    {
                        tierMap[skill.skillId] = 0;
                        changed = true;
                        continue;
                    }

                    int maxPrereqTier = -1;
                    bool allResolved = true;
                    foreach (var prereq in skill.prerequisiteSkills)
                    {
                        if (prereq == null) continue;
                        if (!tierMap.TryGetValue(prereq.skillId, out int prereqTier) || prereqTier < 0)
                        {
                            allResolved = false;
                            break;
                        }
                        if (prereqTier > maxPrereqTier)
                            maxPrereqTier = prereqTier;
                    }

                    if (allResolved)
                    {
                        tierMap[skill.skillId] = maxPrereqTier + 1;
                        changed = true;
                    }
                }
            }

            // Any unresolved (circular deps) get max tier
            foreach (var skill in allSkills)
            {
                if (tierMap[skill.skillId] < 0)
                    tierMap[skill.skillId] = 999;
            }

            // Sort by tier, then by original order in the nodes array
            allSkills.Sort((a, b) =>
            {
                int tierCmp = tierMap[a.skillId].CompareTo(tierMap[b.skillId]);
                return tierCmp;
            });

            return allSkills;
        }

        #endregion

        #region Refresh

        private void RefreshHeader()
        {
            var sm = SkillManager.Instance;
            if (sm == null) return;

            if (spDisplayText != null)
                spDisplayText.text = $"SP: {sm.AvailableSP}";
            if (levelDisplayText != null)
                levelDisplayText.text = $"Lv. {sm.PlayerLevel}";
        }

        public void RefreshAllRows()
        {
            var sm = SkillManager.Instance;
            if (sm == null) return;

            foreach (var kvp in rowsBySkillId)
            {
                var r = kvp.Value;
                var instance = sm.GetLearnedSkill(r.skill.skillId);

                // Update level text
                int currentLvl = instance?.currentLevel ?? 0;
                if (r.levelText != null)
                    r.levelText.text = $"{currentLvl}/{r.skill.maxSkillLevel}";

                // Update state indicator color
                Color stateColor;
                if (instance != null && instance.IsMaxLevel)
                    stateColor = MaxedColor;
                else if (instance != null)
                    stateColor = LearnedColor;
                else if (sm.CanLearnSkill(r.skill))
                    stateColor = AvailableColor;
                else
                    stateColor = LockedColor;

                if (r.stateIndicator != null)
                    r.stateIndicator.color = stateColor;

                // Dim locked rows
                float rowAlpha = (instance != null || sm.CanLearnSkill(r.skill)) ? 1f : 0.5f;
                if (r.nameText != null)
                {
                    var c = r.nameText.color;
                    c.a = rowAlpha;
                    r.nameText.color = c;
                }
                if (r.icon != null)
                {
                    var c = r.icon.color;
                    c.a = rowAlpha;
                    r.icon.color = c;
                }
            }

            // Refresh selected skill detail
            if (selectedSkillId != null)
                UpdateDetailPanel(selectedSkillId);
        }

        public void RefreshStatsSummary()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            var health = player.GetComponent<HealthSystem>();
            var mana = player.GetComponent<ManaSystem>();
            var stats = player.GetComponent<StatSystem>();

            if (hpStatText != null && health != null)
                hpStatText.text = $"HP: {health.CurrentHealth:F0}/{health.MaxHealth:F0}";
            if (mpStatText != null && mana != null)
                mpStatText.text = $"MP: {mana.CurrentMana:F0}/{mana.MaxMana:F0}";
            if (strStatText != null && stats != null)
                strStatText.text = $"STR: {stats.Strength}";
            if (intStatText != null && stats != null)
                intStatText.text = $"INT: {stats.Intelligence}";
            if (agiStatText != null && stats != null)
                agiStatText.text = $"AGI: {stats.Agility}";
        }

        #endregion

        #region Selection & Detail

        private void SelectSkill(string skillId)
        {
            selectedSkillId = skillId;

            // Highlight selected row
            foreach (var kvp in rowsBySkillId)
            {
                if (kvp.Value.rowBg != null)
                    kvp.Value.rowBg.color = kvp.Key == skillId ? RowSelected : RowNormal;
            }

            UpdateDetailPanel(skillId);
            UIManager.Instance?.PlayNavigateSound();
        }

        private void UpdateDetailPanel(string skillId)
        {
            if (detailPanel == null) return;
            if (!rowsBySkillId.TryGetValue(skillId, out var rowRef)) return;

            detailPanel.SetActive(true);
            var skill = rowRef.skill;
            var sm = SkillManager.Instance;
            var instance = sm?.GetLearnedSkill(skillId);

            if (detailIcon != null)
            {
                var resolved = SkillIconHelper.ResolveIcon(skill);
                if (resolved != null)
                {
                    detailIcon.sprite = resolved;
                    detailIcon.color = SkillIconHelper.ResolveTint(skill);
                    detailIcon.enabled = true;
                }
                else
                {
                    detailIcon.enabled = false;
                }
            }

            if (detailName != null)
                detailName.text = skill.skillName;

            if (detailDesc != null)
            {
                int level = instance?.currentLevel ?? 1;
                detailDesc.text = skill.GetFormattedDescription(level);
            }

            if (detailStats != null)
            {
                int level = instance?.currentLevel ?? 1;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Type: {skill.skillType}");
                if (skill.GetDamage(level) > 0)
                    sb.AppendLine($"Damage: {skill.GetDamage(level):F0}");
                if (skill.GetManaCost(level) > 0)
                    sb.AppendLine($"Mana Cost: {skill.GetManaCost(level):F0}");
                if (skill.GetCooldown(level) > 0)
                    sb.AppendLine($"Cooldown: {skill.GetCooldown(level):F1}s");
                if (skill.GetDuration(level) > 0)
                    sb.AppendLine($"Duration: {skill.GetDuration(level):F1}s");
                sb.AppendLine($"SP Cost: {skill.spCost}");
                if (instance != null)
                    sb.AppendLine($"Level: {instance.currentLevel} / {skill.maxSkillLevel}");
                else
                    sb.AppendLine($"Max Level: {skill.maxSkillLevel}");
                detailStats.text = sb.ToString();
            }

            if (detailRequirements != null)
            {
                var sb = new System.Text.StringBuilder();
                if (skill.requiredPlayerLevel > 1)
                {
                    bool met = sm != null && sm.PlayerLevel >= skill.requiredPlayerLevel;
                    sb.AppendLine(met ? $"<color=#88CC88>Lv. {skill.requiredPlayerLevel} Required</color>"
                                     : $"<color=#CC8888>Lv. {skill.requiredPlayerLevel} Required</color>");
                }
                if (skill.prerequisiteSkills != null)
                {
                    foreach (var prereq in skill.prerequisiteSkills)
                    {
                        if (prereq == null) continue;
                        bool met = sm?.GetLearnedSkill(prereq.skillId) != null;
                        sb.AppendLine(met ? $"<color=#88CC88>Requires: {prereq.skillName}</color>"
                                         : $"<color=#CC8888>Requires: {prereq.skillName}</color>");
                    }
                }
                if (sm != null && instance == null && !sm.CanLearnSkill(skill))
                {
                    if (sm.AvailableSP < skill.spCost)
                        sb.AppendLine($"<color=#CC8888>Need {skill.spCost} SP (have {sm.AvailableSP})</color>");
                }
                detailRequirements.text = sb.ToString();
            }

            UpdateLearnButton(skill, instance);
            UpdateAssignHotbarButton(skill, instance);
        }

        #endregion

        #region Learn & Hotbar

        private void UpdateLearnButton(SkillData skill, SkillInstance instance)
        {
            if (learnButton == null) return;
            var sm = SkillManager.Instance;
            if (sm == null) return;

            if (instance != null && instance.IsMaxLevel)
            {
                learnButton.interactable = false;
                if (learnButtonText != null)
                    learnButtonText.text = "MAX LEVEL";
            }
            else if (instance != null)
            {
                bool canUpgrade = sm.CanUpgradeSkill(skill.skillId);
                learnButton.interactable = canUpgrade;
                if (learnButtonText != null)
                    learnButtonText.text = $"Upgrade (SP: {skill.spCost})";
            }
            else if (sm.CanLearnSkill(skill))
            {
                learnButton.interactable = true;
                if (learnButtonText != null)
                    learnButtonText.text = $"Learn (SP: {skill.spCost})";
            }
            else
            {
                learnButton.interactable = false;
                if (learnButtonText != null)
                    learnButtonText.text = "Locked";
            }
        }

        private void UpdateAssignHotbarButton(SkillData skill, SkillInstance instance)
        {
            if (assignHotbarButton == null) return;

            bool show = instance != null && instance.SkillType != SkillType.Passive;
            assignHotbarButton.gameObject.SetActive(show);

            if (show && assignHotbarButtonText != null)
            {
                var controller = Object.FindAnyObjectByType<PlayerSkillController>();
                bool assigned = controller != null && controller.IsSkillOnHotbar(instance.SkillId);
                assignHotbarButtonText.text = assigned ? "Reassign Hotbar" : "Assign to Hotbar";
            }
        }

        private void OnLearnButtonClicked()
        {
            if (selectedSkillId == null) return;
            var sm = SkillManager.Instance;
            if (sm == null) return;

            if (!rowsBySkillId.TryGetValue(selectedSkillId, out var rowRef)) return;

            var instance = sm.GetLearnedSkill(selectedSkillId);
            bool success;

            if (instance != null)
                success = sm.UpgradeSkill(selectedSkillId);
            else
                success = sm.LearnSkill(rowRef.skill);

            if (success)
                UIManager.Instance?.PlayConfirmSound();
            else
                UIManager.Instance?.PlayErrorSound();
        }

        private void OnAssignHotbarClicked()
        {
            if (selectedSkillId == null) return;
            var sm = SkillManager.Instance;
            if (sm == null) return;

            var instance = sm.GetLearnedSkill(selectedSkillId);
            if (instance == null || instance.SkillType == SkillType.Passive) return;

            var popup = SkillHotbarAssignPopup.Instance;
            if (popup == null)
                popup = SkillHotbarAssignPopup.CreateRuntimeUI(transform.root);

            popup.Show(selectedSkillId);
        }

        #endregion

        #region Event Handlers

        private void HandleSkillLearned(SkillInstance instance)
        {
            RefreshAllRows();
        }

        private void HandleSkillLevelChanged(SkillInstance instance, int oldLevel, int newLevel)
        {
            RefreshAllRows();
        }

        private void HandleSPChanged(int oldSP, int newSP)
        {
            RefreshHeader();
            RefreshAllRows();
        }

        private void HandleJobChanged(JobClassData oldJob, JobClassData newJob)
        {
            RefreshHeader();
            if (newJob?.skillTree != null)
                LoadTree(newJob.skillTree);
        }

        #endregion
    }
}
