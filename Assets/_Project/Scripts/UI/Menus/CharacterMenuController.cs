using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Merged character menu combining stats, equipment, and inventory.
    /// Opens with E key, 960x680 layout with stat allocation, equipment slots,
    /// inventory grid, and stat comparison panel.
    /// </summary>
    public class CharacterMenuController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas menuCanvas;
        [SerializeField] private CanvasGroup menuCanvasGroup;
        [SerializeField] private Button closeButton;

        [Header("Character Preview")]
        [SerializeField] private UILayeredSpritePreview characterPreview;

        [Header("Player Info")]
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text classText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text mpText;

        [Header("Stats")]
        [SerializeField] private TMP_Text availablePointsText;
        [SerializeField] private TMP_Text strengthText;
        [SerializeField] private TMP_Text intelligenceText;
        [SerializeField] private TMP_Text agilityText;
        [SerializeField] private Button allocateStrButton;
        [SerializeField] private Button allocateIntButton;
        [SerializeField] private Button allocateAgiButton;

        [Header("Derived Stats")]
        [SerializeField] private TMP_Text bonusHPText;
        [SerializeField] private TMP_Text meleeDamageText;
        [SerializeField] private TMP_Text bonusManaText;
        [SerializeField] private TMP_Text skillDamageText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text critChanceText;

        [Header("Equipment Slots")]
        [SerializeField] private Image[] equipSlotIcons;
        [SerializeField] private TMP_Text[] equipSlotNames;
        [SerializeField] private TMP_Text[] equipSlotStats;
        [SerializeField] private Button[] equipSlotButtons;

        [Header("Inventory")]
        [SerializeField] private TMP_Text inventoryCountText;
        [SerializeField] private Image[] inventoryCellIcons;
        [SerializeField] private Image[] inventoryCellBorders;
        [SerializeField] private Button[] inventoryCellButtons;

        // Floating tooltip (replaces old compare panel)
        private GameObject tooltipPanel;
        private TMP_Text tooltipTitle;
        private TMP_Text tooltipStats;
        private TMP_Text tooltipDesc;
        private RectTransform tooltipRect;
        private RectTransform canvasRect;

        [Header("Settings")]
        [SerializeField] private bool pauseGameWhenOpen = true;
        [SerializeField] private int canvasSortOrder = 150;

        private bool isOpen;
        private PlayerInput playerInput;
        private int lastToggleFrame = -1;

        private InputAction fallbackOpenAction;
        private InputAction escapeAction;

        // Player systems
        private StatSystem statSystem;
        private HealthSystem healthSystem;
        private ManaSystem manaSystem;
        private LevelSystem levelSystem;
        private EquipmentManager equipmentManager;
        private InventoryManager inventoryManager;
        private PlayerAppearance playerAppearance;

        // Drag state (-1 = no drag; for inventory drags use dragSourceIndex,
        // for equipment drags use dragEquipSlotIndex)
        private int dragSourceIndex = -1;
        private int dragEquipSlotIndex = -1;
        private GameObject dragGhost;
        private Canvas dragCanvas;

        // Equipment slot order matching EquipmentSlotType layout
        private static readonly EquipmentSlotType[] SlotDisplayOrder = new[]
        {
            EquipmentSlotType.Weapon,
            EquipmentSlotType.Head,
            EquipmentSlotType.Armor,
            EquipmentSlotType.Hands,
            EquipmentSlotType.Legs,
            EquipmentSlotType.Feet,
            EquipmentSlotType.Accessory,
        };

        private static readonly string[] SlotLabels = new[]
        {
            "Weapon", "Head", "Armor", "Hands", "Legs", "Feet", "Accessory"
        };

        public bool IsOpen => isOpen;
        public event System.Action OnOpened;
        public event System.Action OnClosed;

        #region Lifecycle

        private void Awake()
        {
            if (menuCanvas == null)
                menuCanvas = GetComponent<Canvas>();

            if (menuCanvasGroup == null && menuCanvas != null)
            {
                menuCanvasGroup = menuCanvas.GetComponent<CanvasGroup>();
                if (menuCanvasGroup == null)
                    menuCanvasGroup = menuCanvas.gameObject.AddComponent<CanvasGroup>();
            }

            if (menuCanvas != null)
                menuCanvas.sortingOrder = canvasSortOrder;

            playerInput = FindAnyObjectByType<PlayerInput>();

            fallbackOpenAction = new InputAction("OpenCharMenu", InputActionType.Button, "<Keyboard>/e");
            escapeAction = new InputAction("CloseCharMenu", InputActionType.Button, "<Keyboard>/escape");

            WireButtonListeners();

            Close();

            if (menuCanvas != null)
                menuCanvas.enabled = false;
        }

        private void OnEnable()
        {
            if (fallbackOpenAction != null)
            {
                fallbackOpenAction.Enable();
                fallbackOpenAction.performed += OnOpenInput;
            }

            if (escapeAction != null)
            {
                escapeAction.Enable();
                escapeAction.performed += OnEscapeInput;
            }

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

            FindPlayerSystems();
        }

        private void OnDisable()
        {
            if (fallbackOpenAction != null)
            {
                fallbackOpenAction.performed -= OnOpenInput;
                fallbackOpenAction.Disable();
            }

            if (escapeAction != null)
            {
                escapeAction.performed -= OnEscapeInput;
                escapeAction.Disable();
            }

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;

            UnsubscribeFromPlayerSystems();
        }

        private void OnDestroy()
        {
            fallbackOpenAction?.Dispose();
            escapeAction?.Dispose();

            if (dragGhost != null)
                Destroy(dragGhost);
        }

        internal void WireButtonListeners()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (allocateStrButton != null)
                allocateStrButton.onClick.AddListener(() => AllocateStat("str"));
            if (allocateIntButton != null)
                allocateIntButton.onClick.AddListener(() => AllocateStat("int"));
            if (allocateAgiButton != null)
                allocateAgiButton.onClick.AddListener(() => AllocateStat("agi"));
        }

        #endregion

        #region Player System Binding

        private void FindPlayerSystems()
        {
            // All systems live on the same Player — if we have all of them, skip
            if (statSystem != null && equipmentManager != null && inventoryManager != null)
                return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            if (statSystem == null)
            {
                statSystem = player.GetComponent<StatSystem>();
                if (statSystem != null)
                {
                    statSystem.OnStatsChanged += RefreshStats;
                    statSystem.OnStatPointsChanged += HandleStatPointsChanged;
                }
            }

            if (healthSystem == null)
            {
                healthSystem = player.GetComponent<HealthSystem>();
                if (healthSystem != null)
                    healthSystem.OnHealthChanged += HandleHealthChanged;
            }

            if (manaSystem == null)
            {
                manaSystem = player.GetComponent<ManaSystem>();
                if (manaSystem != null)
                    manaSystem.OnManaChanged += HandleManaChanged;
            }

            if (levelSystem == null)
            {
                levelSystem = player.GetComponent<LevelSystem>();
                if (levelSystem != null)
                    levelSystem.OnLevelUp += HandleLevelUp;
            }

            if (equipmentManager == null)
            {
                equipmentManager = player.GetComponent<EquipmentManager>();
                if (equipmentManager != null)
                    equipmentManager.OnEquipmentChanged += HandleEquipmentChanged;
            }

            if (inventoryManager == null)
            {
                inventoryManager = player.GetComponent<InventoryManager>();
                if (inventoryManager == null)
                    inventoryManager = player.AddComponent<InventoryManager>();
                inventoryManager.OnInventoryChanged += RefreshInventory;
            }

            if (playerAppearance == null)
                playerAppearance = player.GetComponent<PlayerAppearance>();
        }

        private void UnsubscribeFromPlayerSystems()
        {
            if (statSystem != null)
            {
                statSystem.OnStatsChanged -= RefreshStats;
                statSystem.OnStatPointsChanged -= HandleStatPointsChanged;
            }
            if (healthSystem != null)
                healthSystem.OnHealthChanged -= HandleHealthChanged;
            if (manaSystem != null)
                manaSystem.OnManaChanged -= HandleManaChanged;
            if (levelSystem != null)
                levelSystem.OnLevelUp -= HandleLevelUp;
            if (equipmentManager != null)
                equipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;
            if (inventoryManager != null)
                inventoryManager.OnInventoryChanged -= RefreshInventory;
        }

        private void HandleHealthChanged(float current, float max) { if (isOpen) RefreshPlayerInfo(); }
        private void HandleManaChanged(float current, float max) { if (isOpen) RefreshPlayerInfo(); }
        private void HandleLevelUp(int newLevel) { if (isOpen) RefreshAll(); }
        private void HandleStatPointsChanged(int points) { if (isOpen) RefreshStats(); }
        private void HandleEquipmentChanged(EquipmentSlotType slot, EquipmentData item)
        {
            if (isOpen) { RefreshEquipment(); RefreshStats(); RefreshPlayerInfo(); RefreshCharacterPreview(); }
        }

        #endregion

        #region Input

        private void OnOpenInput(InputAction.CallbackContext context)
        {
            if (Time.frameCount == lastToggleFrame) return;
            lastToggleFrame = Time.frameCount;
            Toggle();
        }

        private void OnEscapeInput(InputAction.CallbackContext context)
        {
            if (dragSourceIndex >= 0)
            {
                CancelDrag();
                return;
            }
            if (isOpen) Close();
        }

        private void HandleGameStateChanged(GameManager.GameState previousState, GameManager.GameState newState)
        {
            if (isOpen && newState != GameManager.GameState.Paused && newState != GameManager.GameState.Playing)
                Close();
        }

        #endregion

        #region Open/Close

        public void Toggle()
        {
            if (isOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (isOpen) return;

            if (GameManager.Instance != null)
            {
                var state = GameManager.Instance.CurrentState;
                if (state == GameManager.GameState.MainMenu ||
                    state == GameManager.GameState.Loading ||
                    state == GameManager.GameState.GameOver ||
                    state == GameManager.GameState.Cutscene)
                    return;
            }

            FindPlayerSystems();

            isOpen = true;

            if (menuCanvas != null)
                menuCanvas.enabled = true;

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = 1f;
                menuCanvasGroup.interactable = true;
                menuCanvasGroup.blocksRaycasts = true;
            }

            RefreshAll();
            HideTooltip();

            if (UIManager.Instance != null)
                UIManager.Instance.RegisterOverlayMenu();

            if (pauseGameWhenOpen && GameManager.Instance != null)
                GameManager.Instance.RequestMenuPause();

            if (UIManager.Instance != null)
                UIManager.Instance.SwitchToUIInput();
            else if (playerInput != null)
                playerInput.SwitchCurrentActionMap("UI");

            OnOpened?.Invoke();
            Debug.Log("[CharacterMenuController] Character menu opened");
        }

        public void Close()
        {
            if (!isOpen && menuCanvasGroup != null && menuCanvasGroup.alpha == 0f)
                return;

            isOpen = false;
            CancelDrag();

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = 0f;
                menuCanvasGroup.interactable = false;
                menuCanvasGroup.blocksRaycasts = false;
            }

            if (menuCanvas != null)
                menuCanvas.enabled = false;

            if (pauseGameWhenOpen && GameManager.Instance != null)
                GameManager.Instance.ReleaseMenuPause();

            if (UIManager.Instance != null)
                UIManager.Instance.UnregisterOverlayMenu();

            if (UIManager.Instance != null)
                UIManager.Instance.SwitchToGameplayInput();
            else if (playerInput != null)
                playerInput.SwitchCurrentActionMap("Player");

            OnClosed?.Invoke();
            Debug.Log("[CharacterMenuController] Character menu closed");
        }

        #endregion

        #region Refresh Display

        private void RefreshAll()
        {
            RefreshPlayerInfo();
            RefreshStats();
            RefreshEquipment();
            RefreshInventory();
            RefreshCharacterPreview();
        }

        private void RefreshPlayerInfo()
        {
            if (characterNameText != null)
            {
                string name = SaveManager.Instance?.CurrentSave?.characterName;
                characterNameText.text = !string.IsNullOrEmpty(name) ? name : "Hero";
            }

            if (classText != null)
            {
                string jobName = SkillManager.Instance?.CurrentJob?.jobName;
                classText.text = !string.IsNullOrEmpty(jobName) ? $"({jobName})" : "(Adventurer)";
            }

            if (levelText != null && levelSystem != null)
                levelText.text = $"Level {levelSystem.CurrentLevel}";

            // HP with stat bonus shown inline
            if (hpText != null && healthSystem != null)
            {
                float bonusHP = statSystem != null ? statSystem.BonusMaxHP : 0f;
                string hpBonus = bonusHP > 0 ? $" (+{bonusHP:F0})" : "";
                hpText.text = $"HP: {healthSystem.CurrentHealth:F0}/{healthSystem.MaxHealth:F0}{hpBonus}";
            }

            // MP with stat bonus shown inline
            if (mpText != null && manaSystem != null)
            {
                float bonusMP = statSystem != null ? statSystem.BonusMaxMana : 0f;
                string mpBonus = bonusMP > 0 ? $" (+{bonusMP:F0})" : "";
                mpText.text = $"MP: {manaSystem.CurrentMana:F0}/{manaSystem.MaxMana:F0}{mpBonus}";
            }
        }

        private void RefreshStats()
        {
            if (statSystem == null) return;

            if (availablePointsText != null)
                availablePointsText.text = $"Stat Points: {statSystem.AvailableStatPoints}";

            if (strengthText != null)
                strengthText.text = FormatCoreStat("Strength", statSystem.BaseStrength, statSystem.Strength);
            if (intelligenceText != null)
                intelligenceText.text = FormatCoreStat("Intelligence", statSystem.BaseIntelligence, statSystem.Intelligence);
            if (agilityText != null)
                agilityText.text = FormatCoreStat("Agility", statSystem.BaseAgility, statSystem.Agility);

            // Derived combat stats (Bonus HP/Mana now shown inline with HP/MP above)
            if (meleeDamageText != null)
                meleeDamageText.text = $"Melee Damage: x{statSystem.MeleeDamageMultiplier:F2}";
            if (skillDamageText != null)
                skillDamageText.text = $"Skill Damage: x{statSystem.SkillDamageMultiplier:F2}";
            if (speedText != null)
                speedText.text = $"Attack Speed: x{(1f / statSystem.AttackSpeedMultiplier):F2}";
            if (critChanceText != null)
                critChanceText.text = $"Critical: {statSystem.CritChance * 100f:F1}% (x{statSystem.CritDamageMultiplier:F2})";

            bool hasPoints = statSystem.AvailableStatPoints > 0;
            if (allocateStrButton != null) allocateStrButton.interactable = hasPoints;
            if (allocateIntButton != null) allocateIntButton.interactable = hasPoints;
            if (allocateAgiButton != null) allocateAgiButton.interactable = hasPoints;
        }

        private void RefreshEquipment()
        {
            if (equipmentManager == null)
            {
                // Re-try finding systems in case player spawned after us
                FindPlayerSystems();
                if (equipmentManager == null) return;
            }

            for (int i = 0; i < SlotDisplayOrder.Length; i++)
            {
                var slot = SlotDisplayOrder[i];
                var item = equipmentManager.GetEquipped(slot);

                if (i < equipSlotIcons.Length && equipSlotIcons[i] != null)
                {
                    if (item != null)
                    {
                        var sprite = ResolveEquipmentSprite(item, slot);
                        equipSlotIcons[i].sprite = sprite;
                        equipSlotIcons[i].enabled = sprite != null;
                        equipSlotIcons[i].color = Color.white;
                    }
                    else
                    {
                        equipSlotIcons[i].sprite = null;
                        equipSlotIcons[i].enabled = false;
                    }
                }

                if (i < equipSlotNames.Length && equipSlotNames[i] != null)
                    equipSlotNames[i].text = item != null ? item.displayName : "Empty";

                if (i < equipSlotStats.Length && equipSlotStats[i] != null)
                {
                    if (item != null)
                    {
                        string stats = item.GetStatSummary();
                        equipSlotStats[i].text = !string.IsNullOrEmpty(stats) ? stats : "No bonuses";
                    }
                    else
                    {
                        equipSlotStats[i].text = "";
                    }
                }
            }
        }

        private void RefreshInventory()
        {
            int count = inventoryManager != null ? inventoryManager.Count : 0;
            if (inventoryCountText != null)
                inventoryCountText.text = $"INVENTORY ({count}/{InventoryManager.MAX_CAPACITY})";

            if (inventoryCellIcons == null) return;

            for (int i = 0; i < inventoryCellIcons.Length; i++)
            {
                var item = inventoryManager != null ? inventoryManager.GetItem(i) : null;

                if (inventoryCellIcons[i] != null)
                {
                    if (item != null)
                    {
                        var sprite = ResolveEquipmentSprite(item, item.slotType);
                        inventoryCellIcons[i].sprite = sprite;
                        inventoryCellIcons[i].enabled = sprite != null;
                        inventoryCellIcons[i].color = Color.white;
                    }
                    else
                    {
                        inventoryCellIcons[i].sprite = null;
                        inventoryCellIcons[i].enabled = false;
                    }
                }
            }
        }

        private void RefreshCharacterPreview()
        {
            if (characterPreview == null || playerAppearance == null) return;
            if (playerAppearance.CurrentConfig == null) return;

            characterPreview.ApplyConfig(playerAppearance.CurrentConfig);

            // Hat hides hair — replicate PlayerAppearance stash logic for preview.
            // If a hat is equipped, the config still contains hair data but
            // PlayerAppearance visually hides it. Mirror that here.
            if (equipmentManager != null &&
                equipmentManager.GetEquipped(EquipmentSlotType.Head) != null)
            {
                characterPreview.SetPart(BodyPartSlot.Hair, null);
            }
        }

        #endregion

        #region Stat Allocation

        private static string FormatCoreStat(string name, int baseStat, int total)
        {
            if (total != baseStat)
                return $"{name}: {baseStat} [{total}]";
            return $"{name}: {baseStat}";
        }

        private void AllocateStat(string statName)
        {
            if (statSystem == null) return;

            var kb = Keyboard.current;
            bool shiftHeld = kb != null
                && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
            int amount = shiftHeld ? 5 : 1;

            bool anyAllocated = false;
            for (int i = 0; i < amount; i++)
            {
                if (!statSystem.AllocateStat(statName))
                    break;
                anyAllocated = true;
            }

            if (anyAllocated)
            {
                RefreshStats();
                RefreshPlayerInfo();

                if (UIManager.Instance != null)
                    UIManager.Instance.PlayConfirmSound();
            }
        }

        #endregion

        #region Equipment Interaction

        private void OnEquipSlotClicked(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotDisplayOrder.Length) return;
            if (equipmentManager == null || inventoryManager == null) return;

            var slotType = SlotDisplayOrder[slotIndex];

            // If we have a dragged item from inventory, try equipping it
            if (dragSourceIndex >= 0)
            {
                var dragItem = inventoryManager.GetItem(dragSourceIndex);
                if (dragItem != null && dragItem.slotType == slotType)
                {
                    inventoryManager.EquipFromInventory(dragSourceIndex, equipmentManager);
                    CancelDrag();
                    return;
                }
                else
                {
                    CancelDrag();
                    return;
                }
            }

            // Left-click with no active drag: no action (drag is handled by EventTrigger)
            // Right-click handles unequip via OnEquipSlotRightClicked
        }

        private void OnEquipSlotRightClicked(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotDisplayOrder.Length) return;
            if (equipmentManager == null || inventoryManager == null) return;

            var slotType = SlotDisplayOrder[slotIndex];
            var equipped = equipmentManager.GetEquipped(slotType);
            if (equipped != null)
            {
                if (!inventoryManager.UnequipToInventory(slotType, equipmentManager))
                {
                    Debug.Log("[CharacterMenuController] Inventory full, cannot unequip");
                    if (UIManager.Instance != null)
                        UIManager.Instance.PlayErrorSound();
                }
            }
        }

        #endregion

        #region Inventory Interaction

        private void OnInventoryCellClicked(int cellIndex)
        {
            if (inventoryManager == null || equipmentManager == null) return;

            // If dragging equipment, drop it into this inventory cell (unequip)
            if (dragEquipSlotIndex >= 0)
            {
                var slotType = SlotDisplayOrder[dragEquipSlotIndex];
                inventoryManager.UnequipToInventory(slotType, equipmentManager);
                CancelDrag();
                return;
            }

            // Left-click does nothing (drag only). Right-click equips via InventoryRightClickHandler.
        }

        private void OnInventoryCellRightClicked(int cellIndex)
        {
            if (inventoryManager == null || equipmentManager == null) return;

            var item = inventoryManager.GetItem(cellIndex);
            if (item == null) return;

            inventoryManager.EquipFromInventory(cellIndex, equipmentManager);

            if (UIManager.Instance != null)
                UIManager.Instance.PlaySelectSound();
        }

        private void OnInventoryCellHoverEnter(int cellIndex)
        {
            if (inventoryManager == null || equipmentManager == null) return;

            var item = inventoryManager.GetItem(cellIndex);
            if (item == null)
            {
                HideTooltip();
                return;
            }

            var equipped = equipmentManager.GetEquipped(item.slotType);
            ShowTooltip(item, equipped);

            if (cellIndex < inventoryCellBorders.Length && inventoryCellBorders[cellIndex] != null)
                inventoryCellBorders[cellIndex].color = AgedGold;
        }

        private void OnInventoryCellHoverExit(int cellIndex)
        {
            HideTooltip();

            if (cellIndex < inventoryCellBorders.Length && inventoryCellBorders[cellIndex] != null)
                inventoryCellBorders[cellIndex].color = CellBorderNormal;
        }

        private void OnEquipSlotHoverEnter(int slotIndex)
        {
            if (equipmentManager == null) return;
            if (slotIndex < 0 || slotIndex >= SlotDisplayOrder.Length) return;

            var item = equipmentManager.GetEquipped(SlotDisplayOrder[slotIndex]);
            if (item == null)
            {
                HideTooltip();
                return;
            }

            ShowTooltip(item, null);
        }

        private void OnEquipSlotHoverExit(int slotIndex)
        {
            HideTooltip();
        }

        private void ShowTooltip(EquipmentData item, EquipmentData compareAgainst)
        {
            if (tooltipPanel == null || item == null) return;

            tooltipPanel.SetActive(true);

            if (tooltipTitle != null)
                tooltipTitle.text = item.displayName;

            if (tooltipStats != null)
            {
                string statLine = item.GetStatSummary();
                if (compareAgainst != null)
                    statLine += "\n" + BuildCompareText(compareAgainst, item);
                tooltipStats.text = !string.IsNullOrEmpty(statLine) ? statLine : "No bonuses";
            }

            if (tooltipDesc != null)
                tooltipDesc.text = !string.IsNullOrEmpty(item.description) ? item.description : "";

            PositionTooltipAtCursor();
        }

        private void HideTooltip()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }

        private void PositionTooltipAtCursor()
        {
            if (tooltipRect == null || canvasRect == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screenPos = mouse.position.ReadValue();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos,
                menuCanvas != null && menuCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? menuCanvas.worldCamera : null,
                out var localPoint);

            // Offset so tooltip doesn't sit right under cursor
            localPoint += new Vector2(16f, -16f);

            // Clamp to canvas bounds
            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 tooltipSize = tooltipRect.sizeDelta;
            Vector2 pivot = tooltipRect.pivot;

            float minX = -canvasSize.x * canvasRect.pivot.x + tooltipSize.x * pivot.x;
            float maxX = canvasSize.x * (1f - canvasRect.pivot.x) - tooltipSize.x * (1f - pivot.x);
            float minY = -canvasSize.y * canvasRect.pivot.y + tooltipSize.y * pivot.y;
            float maxY = canvasSize.y * (1f - canvasRect.pivot.y) - tooltipSize.y * (1f - pivot.y);

            localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
            localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

            tooltipRect.anchoredPosition = localPoint;
        }

        private static string BuildCompareText(EquipmentData current, EquipmentData candidate)
        {
            var sb = new System.Text.StringBuilder();
            AppendDelta(sb, "STR", candidate.bonusSTR - current.bonusSTR);
            AppendDelta(sb, "INT", candidate.bonusINT - current.bonusINT);
            AppendDelta(sb, "AGI", candidate.bonusAGI - current.bonusAGI);
            return sb.ToString().TrimEnd();
        }

        private static void AppendDelta(System.Text.StringBuilder sb, string stat, int delta)
        {
            if (delta == 0) return;
            string color = delta > 0 ? "#4CAF50" : "#8B0000";
            sb.Append($"<color={color}>{stat} {delta:+#;-#}</color>   ");
        }

        private void LateUpdate()
        {
            if (tooltipPanel != null && tooltipPanel.activeSelf)
                PositionTooltipAtCursor();
        }

        #endregion

        #region Drag and Drop

        private void StartEquipDrag(int slotIndex, EquipmentSlotType slotType)
        {
            var item = equipmentManager.GetEquipped(slotType);
            if (item == null) return;

            dragEquipSlotIndex = slotIndex;

            if (dragGhost != null)
                Destroy(dragGhost);

            dragCanvas = menuCanvas;
            dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup));
            dragGhost.transform.SetParent(menuCanvas.transform, false);

            var ghostCg = dragGhost.GetComponent<CanvasGroup>();
            ghostCg.alpha = 0.6f;
            ghostCg.blocksRaycasts = false;

            var ghostImg = dragGhost.AddComponent<Image>();
            ghostImg.sprite = ResolveEquipmentSprite(item, slotType);
            ghostImg.preserveAspect = true;
            ghostImg.raycastTarget = false;

            var ghostRect = dragGhost.GetComponent<RectTransform>();
            ghostRect.sizeDelta = new Vector2(48, 48);

            // Dim the source slot icon
            if (slotIndex < equipSlotIcons.Length && equipSlotIcons[slotIndex] != null)
                equipSlotIcons[slotIndex].color = new Color(1f, 1f, 1f, 0.3f);
        }

        private void StartDrag(int inventoryIndex)
        {
            if (inventoryManager == null) return;
            var item = inventoryManager.GetItem(inventoryIndex);
            if (item == null) return;

            dragSourceIndex = inventoryIndex;

            // Create ghost icon
            if (dragGhost != null)
                Destroy(dragGhost);

            dragCanvas = menuCanvas;
            dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup));
            dragGhost.transform.SetParent(menuCanvas.transform, false);

            var ghostCg = dragGhost.GetComponent<CanvasGroup>();
            ghostCg.alpha = 0.6f;
            ghostCg.blocksRaycasts = false;

            var ghostImg = dragGhost.AddComponent<Image>();
            ghostImg.sprite = ResolveEquipmentSprite(item, item.slotType);
            ghostImg.preserveAspect = true;
            ghostImg.raycastTarget = false;

            var ghostRect = dragGhost.GetComponent<RectTransform>();
            ghostRect.sizeDelta = new Vector2(48, 48);

            // Dim the source cell
            if (inventoryIndex < inventoryCellIcons.Length && inventoryCellIcons[inventoryIndex] != null)
                inventoryCellIcons[inventoryIndex].color = new Color(1f, 1f, 1f, 0.3f);

            // Pulse source border
            if (inventoryIndex < inventoryCellBorders.Length && inventoryCellBorders[inventoryIndex] != null)
                inventoryCellBorders[inventoryIndex].color = AgedGold;
        }

        private void UpdateDragPosition(Vector2 screenPos)
        {
            if (dragGhost == null || dragCanvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragCanvas.GetComponent<RectTransform>(),
                screenPos,
                dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : dragCanvas.worldCamera,
                out var localPoint);

            dragGhost.GetComponent<RectTransform>().anchoredPosition = localPoint;
        }

        private void CancelDrag()
        {
            if (dragGhost != null)
            {
                Destroy(dragGhost);
                dragGhost = null;
            }

            // Restore source cell visual (inventory drag)
            if (dragSourceIndex >= 0)
            {
                if (dragSourceIndex < inventoryCellBorders.Length && inventoryCellBorders[dragSourceIndex] != null)
                    inventoryCellBorders[dragSourceIndex].color = CellBorderNormal;

                RefreshInventory();
            }

            // Restore source slot visual (equipment drag)
            if (dragEquipSlotIndex >= 0)
            {
                RefreshEquipment();
            }

            dragSourceIndex = -1;
            dragEquipSlotIndex = -1;
        }

        #endregion

        #region Sprite Resolution

        /// <summary>
        /// Resolves display sprite for equipment item. Mirrors EquipmentMenuController logic.
        /// </summary>
        private static Sprite ResolveEquipmentSprite(EquipmentData item, EquipmentSlotType slot)
        {
            if (item == null) return null;
            if (item.icon != null) return item.icon;
            if (item.visualPart == null) return null;

            Sprite result = null;

            if (slot == EquipmentSlotType.Weapon)
                result = FindBestWeaponFrame(item.visualPart);

            if (result == null && item.visualPart.previewSprite != null)
                result = item.visualPart.previewSprite;

            if (result == null && item.visualPart.frames != null)
            {
                foreach (var f in item.visualPart.frames)
                {
                    if (f != null) { result = f; break; }
                }
            }

            return result;
        }

        private static Sprite FindBestWeaponFrame(BodyPartData part)
        {
            if (part?.frames == null) return null;
            int[][] ranges = { new[] { 26, 33 }, new[] { 20, 25 }, new[] { 34, 40 } };
            foreach (var range in ranges)
            {
                for (int i = range[1]; i >= range[0] && i < part.frames.Length; i--)
                {
                    if (part.frames[i] != null) return part.frames[i];
                }
            }
            return null;
        }

        #endregion

        #region Runtime UI Builder

        // Typography — 4 tiers only
        private const float FontHeader = 24f;    // panel title, character name
        private const float FontPrimary = 16f;   // stat names, stat values, equipment names, HP/MP
        private const float FontSecondary = 13f;  // slot labels, inventory count, derived stats, class/level
        private const float FontFlavor = 11f;     // descriptions, lore, tooltips

        // Gothic color palette
        private static readonly Color PanelBg = new Color(0.08f, 0.08f, 0.1f, 0.97f);
        private static readonly Color AgedGold = new Color(0.812f, 0.710f, 0.231f, 1f);
        private static readonly Color BoneWhite = new Color(0.961f, 0.961f, 0.863f, 1f);
        private static readonly Color Charcoal = new Color(0.102f, 0.102f, 0.102f, 0.9f);
        private static readonly Color DividerCol = new Color(0.812f, 0.710f, 0.231f, 0.3f);
        private static readonly Color BtnNormal = new Color(0.2f, 0.2f, 0.25f, 1f);
        private static readonly Color BtnHover = new Color(0.3f, 0.3f, 0.35f, 1f);
        private static readonly Color BtnPress = new Color(0.15f, 0.15f, 0.2f, 1f);
        private static readonly Color SubtleText = new Color(0.7f, 0.65f, 0.55f, 1f);
        private static readonly Color CellBg = new Color(0.06f, 0.06f, 0.08f, 0.8f);
        private static readonly Color CellBorderNormal = new Color(0.3f, 0.3f, 0.3f, 0.3f);
        private static readonly Color CompareBg = new Color(0.06f, 0.06f, 0.08f, 0.95f);
        private static readonly Color SlotRowBg = new Color(0.12f, 0.12f, 0.15f, 0.8f);
        private static readonly Color SlotRowHover = new Color(0.18f, 0.18f, 0.22f, 0.9f);
        private static readonly Color SlotRowPress = new Color(0.25f, 0.22f, 0.12f, 0.9f);

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

        /// <summary>
        /// Builds the entire merged character menu UI at runtime.
        /// </summary>
        public static CharacterMenuController CreateRuntimeUI()
        {
            int slotCount = SlotDisplayOrder.Length;
            int totalCells = InventoryManager.MAX_CAPACITY;
            int invCols = 8;
            int invRows = Mathf.CeilToInt((float)totalCells / invCols);
            float cellSize = 64f;
            float cellSpacing = 4f;
            float invGridHeight = invRows * cellSize + Mathf.Max(0, invRows - 1) * cellSpacing;

            // --- Canvas ---
            var canvasGo = new GameObject("CharacterMenu_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var cg = canvasGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            // --- Dark overlay ---
            var overlayGo = MakeRect("Overlay", canvasGo.transform);
            Stretch(overlayGo);
            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.sprite = WhiteSprite;
            overlayImg.color = new Color(0f, 0f, 0f, 0.6f);

            // --- Center panel (960 wide, height driven by content, min 680) ---
            var panelGo = MakeRect("Panel", canvasGo.transform);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(960f, 0f);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.sprite = WhiteSprite;
            panelImg.color = PanelBg;

            var panelVLG = panelGo.AddComponent<VerticalLayoutGroup>();
            panelVLG.padding = new RectOffset(0, 0, 0, 0);
            panelVLG.spacing = 0;
            panelVLG.childControlWidth = true;
            panelVLG.childControlHeight = true;
            panelVLG.childForceExpandWidth = true;
            panelVLG.childForceExpandHeight = false;

            var panelCSF = panelGo.AddComponent<ContentSizeFitter>();
            panelCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            panelCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddLayout(panelGo, minH: 680);

            // --- Title row (50px) ---
            var titleRow = MakeRect("TitleRow", panelGo.transform);
            AddLayout(titleRow, prefH: 50);

            var titleTextGo = MakeRect("TitleText", titleRow.transform);
            Stretch(titleTextGo);
            var titleTmp = titleTextGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "CHARACTER";
            titleTmp.fontSize = FontHeader;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = AgedGold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(titleTmp);

            // Close [X] button
            var closeBtn = BuildCloseButton(titleRow.transform);

            // --- Divider below title ---
            BuildLayoutDivider(panelGo.transform, true);

            // =================================================================
            // TOP SECTION: HLG with left column, vertical divider, right column
            // Height is driven by the taller column (equipment slots).
            // =================================================================
            var topSection = MakeRect("TopSection", panelGo.transform);
            AddLayout(topSection, flexH: 1, minH: 200);
            var topHLG = topSection.AddComponent<HorizontalLayoutGroup>();
            topHLG.padding = new RectOffset(20, 20, 10, 10);
            topHLG.spacing = 4;
            topHLG.childControlWidth = true;
            topHLG.childControlHeight = true;
            topHLG.childForceExpandWidth = false;
            topHLG.childForceExpandHeight = true;

            // --- LEFT COLUMN: Preview + Player Info + Stats ---
            var leftCol = MakeRect("LeftColumn", topSection.transform);
            AddLayout(leftCol, flexW: 0.48f);

            // Character preview (200x250, top-left)
            var previewContainer = MakeRect("PreviewContainer", leftCol.transform);
            var previewContainerRect = previewContainer.GetComponent<RectTransform>();
            previewContainerRect.anchorMin = new Vector2(0, 1);
            previewContainerRect.anchorMax = new Vector2(0, 1);
            previewContainerRect.pivot = new Vector2(0, 1);
            previewContainerRect.anchoredPosition = Vector2.zero;
            previewContainerRect.sizeDelta = new Vector2(160, 200);
            var previewBg = previewContainer.AddComponent<Image>();
            previewBg.sprite = WhiteSprite;
            previewBg.color = Charcoal;

            var previewGo = MakeRect("CharPreview", previewContainer.transform);
            var previewGoRect = previewGo.GetComponent<RectTransform>();
            previewGoRect.anchorMin = new Vector2(0.05f, 0.05f);
            previewGoRect.anchorMax = new Vector2(0.95f, 0.95f);
            previewGoRect.offsetMin = Vector2.zero;
            previewGoRect.offsetMax = Vector2.zero;
            var charPreview = previewGo.AddComponent<UILayeredSpritePreview>();

            // Player info (right of preview)
            float infoX = 170f;
            float infoY = 0f;
            float lineH = 22f;

            var charNameGo = BuildInfoText(leftCol.transform, "CharName", "Hero", FontHeader, AgedGold, infoX, infoY);
            infoY -= lineH + 2f;
            var classGo = BuildInfoText(leftCol.transform, "Class", "(Adventurer)", FontSecondary, SubtleText, infoX, infoY);
            infoY -= lineH + 2f;
            var lvlGo = BuildInfoText(leftCol.transform, "Level", "Level 1", FontSecondary, BoneWhite, infoX, infoY);
            infoY -= lineH + 2f;
            var hpGoTxt = BuildInfoText(leftCol.transform, "HP", "HP: 100/100", FontPrimary, BoneWhite, infoX, infoY);
            infoY -= lineH + 2f;
            var mpGoTxt = BuildInfoText(leftCol.transform, "MP", "MP: 50/50", FontPrimary, BoneWhite, infoX, infoY);

            // Stats + Derived (below preview area)
            float statsBaseY = -210f;

            // Available Points
            var ptsGo = MakeRect("AvailablePoints", leftCol.transform);
            var ptsRect = ptsGo.GetComponent<RectTransform>();
            ptsRect.anchorMin = new Vector2(0, 1);
            ptsRect.anchorMax = new Vector2(1, 1);
            ptsRect.pivot = new Vector2(0, 1);
            ptsRect.anchoredPosition = new Vector2(0, statsBaseY);
            ptsRect.sizeDelta = new Vector2(0, 22);
            var ptsTmp = ptsGo.AddComponent<TextMeshProUGUI>();
            ptsTmp.text = "Stat Points: 0";
            ptsTmp.fontSize = FontPrimary;
            ptsTmp.fontStyle = FontStyles.Bold;
            ptsTmp.color = AgedGold;
            ptsTmp.alignment = TextAlignmentOptions.Left;
            FontManager.EnsureFont(ptsTmp);

            statsBaseY -= 28f;

            // Two-column layout: left = core stats with [+], right = derived stats
            // Left sub-column (0–55%)
            var statLeftCol = MakeRect("StatLeftCol", leftCol.transform);
            var statLeftRect = statLeftCol.GetComponent<RectTransform>();
            statLeftRect.anchorMin = new Vector2(0, 1);
            statLeftRect.anchorMax = new Vector2(0.55f, 1);
            statLeftRect.pivot = new Vector2(0, 1);
            statLeftRect.anchoredPosition = new Vector2(0, statsBaseY);
            statLeftRect.sizeDelta = new Vector2(0, 100);

            float leftY = 0f;
            var (strTmp, strBtn) = BuildStatRow(statLeftCol.transform, "Strength", leftY);
            leftY -= 26f;
            var (intTmp, intBtn) = BuildStatRow(statLeftCol.transform, "Intelligence", leftY);
            leftY -= 26f;
            var (agiTmp, agiBtn) = BuildStatRow(statLeftCol.transform, "Agility", leftY);

            // Right sub-column (58–100%) — derived stats stacked
            var statRightCol = MakeRect("StatRightCol", leftCol.transform);
            var statRightRect = statRightCol.GetComponent<RectTransform>();
            statRightRect.anchorMin = new Vector2(0.58f, 1);
            statRightRect.anchorMax = new Vector2(1, 1);
            statRightRect.pivot = new Vector2(0, 1);
            statRightRect.anchoredPosition = new Vector2(0, statsBaseY);
            statRightRect.sizeDelta = new Vector2(0, 100);

            float rightY = 0f;
            var meleeTmp = BuildDerivedStatLine(statRightCol.transform, "Melee Damage: x1.00", rightY);
            rightY -= 22f;
            var skillDmgTmp = BuildDerivedStatLine(statRightCol.transform, "Skill Damage: x1.00", rightY);
            rightY -= 22f;
            var spdTmp = BuildDerivedStatLine(statRightCol.transform, "Attack Speed: x1.00", rightY);
            rightY -= 22f;
            var critTmp = BuildDerivedStatLine(statRightCol.transform, "Critical: 0.0% (x2.00)", rightY);

            // --- GOLD VERTICAL DIVIDER ---
            BuildLayoutDivider(topSection.transform, false);

            // --- RIGHT COLUMN: Equipment Slots (VLG — slot count drives height) ---
            var rightCol = MakeRect("RightColumn", topSection.transform);
            AddLayout(rightCol, flexW: 0.52f);
            rightCol.AddComponent<RectMask2D>();
            var rightVLG = rightCol.AddComponent<VerticalLayoutGroup>();
            rightVLG.spacing = 6;
            rightVLG.childControlWidth = true;
            rightVLG.childControlHeight = true;
            rightVLG.childForceExpandWidth = true;
            rightVLG.childForceExpandHeight = false;

            // Build equipment slot rows (count driven by SlotDisplayOrder)
            var slotIcons = new Image[slotCount];
            var slotNames = new TMP_Text[slotCount];
            var slotStatTexts = new TMP_Text[slotCount];
            var slotRowBtns = new Button[slotCount];

            for (int i = 0; i < slotCount; i++)
            {
                var (rowBtn, icon, nameTxt, statsTxt) =
                    BuildEquipmentSlotRow(rightCol.transform, SlotLabels[i], 0f);
                AddLayout(rowBtn.gameObject, prefH: SLOT_ROW_HEIGHT);
                slotIcons[i] = icon;
                slotNames[i] = nameTxt;
                slotStatTexts[i] = statsTxt;
                slotRowBtns[i] = rowBtn;
            }

            // =================================================================
            // GOLD HORIZONTAL DIVIDER (position driven by VLG)
            // =================================================================
            BuildLayoutDivider(panelGo.transform, true);

            // =================================================================
            // BOTTOM STRIP: Inventory (height driven by cell count and rows)
            // =================================================================
            var bottomSection = MakeRect("BottomSection", panelGo.transform);
            var bottomVLG = bottomSection.AddComponent<VerticalLayoutGroup>();
            bottomVLG.padding = new RectOffset(20, 20, 6, 16);
            bottomVLG.spacing = 4;
            bottomVLG.childControlWidth = true;
            bottomVLG.childControlHeight = true;
            bottomVLG.childForceExpandWidth = true;
            bottomVLG.childForceExpandHeight = false;

            // Inventory label
            var invLabelGo = MakeRect("InvLabel", bottomSection.transform);
            AddLayout(invLabelGo, prefH: 22);
            var invLabelTmp = invLabelGo.AddComponent<TextMeshProUGUI>();
            invLabelTmp.text = $"INVENTORY (0/{totalCells})";
            invLabelTmp.fontSize = FontSecondary;
            invLabelTmp.fontStyle = FontStyles.Bold;
            invLabelTmp.color = AgedGold;
            invLabelTmp.alignment = TextAlignmentOptions.Left;
            FontManager.EnsureFont(invLabelTmp);

            // Inventory grid (GridLayoutGroup — rows/cols derived from MAX_CAPACITY)
            var gridContainer = MakeRect("InventoryGrid", bottomSection.transform);
            AddLayout(gridContainer, prefH: invGridHeight, flexW: 1);
            var gridLayout = gridContainer.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(cellSize, cellSize);
            gridLayout.spacing = new Vector2(cellSpacing, cellSpacing);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = invCols;
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;

            var cellIcons = new Image[totalCells];
            var cellBorders = new Image[totalCells];
            var cellButtons = new Button[totalCells];

            for (int i = 0; i < totalCells; i++)
            {
                // Size/position managed by GridLayoutGroup
                var cellGo = MakeRect($"Cell_{i}", gridContainer.transform);

                var cellBg = cellGo.AddComponent<Image>();
                cellBg.sprite = WhiteSprite;
                cellBg.color = CellBg;

                // Border (child overlay)
                var borderGo = MakeRect("Border", cellGo.transform);
                Stretch(borderGo);
                var borderImg = borderGo.AddComponent<Image>();
                borderImg.sprite = WhiteSprite;
                borderImg.color = CellBorderNormal;
                borderImg.raycastTarget = false;
                var innerGo = MakeRect("Inner", borderGo.transform);
                var innerRect = innerGo.GetComponent<RectTransform>();
                innerRect.anchorMin = Vector2.zero;
                innerRect.anchorMax = Vector2.one;
                innerRect.offsetMin = new Vector2(1, 1);
                innerRect.offsetMax = new Vector2(-1, -1);
                var innerImg = innerGo.AddComponent<Image>();
                innerImg.sprite = WhiteSprite;
                innerImg.color = CellBg;
                innerImg.raycastTarget = false;

                // Item icon
                var iconGo = MakeRect("Icon", cellGo.transform);
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.05f, 0.05f);
                iconRect.anchorMax = new Vector2(0.95f, 0.95f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                iconImg.enabled = false;

                // Button for interaction
                var cellBtn = cellGo.AddComponent<Button>();
                var btnColors = cellBtn.colors;
                btnColors.normalColor = CellBg;
                btnColors.highlightedColor = new Color(0.12f, 0.12f, 0.15f, 0.9f);
                btnColors.pressedColor = new Color(0.2f, 0.18f, 0.1f, 0.9f);
                btnColors.selectedColor = btnColors.highlightedColor;
                btnColors.fadeDuration = 0.08f;
                cellBtn.colors = btnColors;
                cellBtn.targetGraphic = cellBg;

                cellGo.AddComponent<EventTrigger>();

                cellIcons[i] = iconImg;
                cellBorders[i] = borderImg;
                cellButtons[i] = cellBtn;
            }

            // Floating tooltip (parented to canvas root, not panel, so it renders on top)
            var ttGo = MakeRect("Tooltip", canvasGo.transform);
            var ttRect = ttGo.GetComponent<RectTransform>();
            ttRect.pivot = new Vector2(0, 1);
            ttRect.sizeDelta = new Vector2(250, 0); // width fixed, height auto
            var ttCg = ttGo.AddComponent<CanvasGroup>();
            ttCg.blocksRaycasts = false;
            ttCg.interactable = false;

            var ttBg = ttGo.AddComponent<Image>();
            ttBg.sprite = WhiteSprite;
            ttBg.color = CompareBg;

            // 1px gold border via child overlay
            var ttBorder = MakeRect("Border", ttGo.transform);
            Stretch(ttBorder);
            var ttBorderImg = ttBorder.AddComponent<Image>();
            ttBorderImg.sprite = WhiteSprite;
            ttBorderImg.color = AgedGold;
            ttBorderImg.raycastTarget = false;
            var ttInner = MakeRect("Inner", ttBorder.transform);
            var ttInnerRect = ttInner.GetComponent<RectTransform>();
            ttInnerRect.anchorMin = Vector2.zero;
            ttInnerRect.anchorMax = Vector2.one;
            ttInnerRect.offsetMin = new Vector2(1, 1);
            ttInnerRect.offsetMax = new Vector2(-1, -1);
            var ttInnerImg = ttInner.AddComponent<Image>();
            ttInnerImg.sprite = WhiteSprite;
            ttInnerImg.color = CompareBg;
            ttInnerImg.raycastTarget = false;

            // Vertical layout for auto-sizing
            var ttLayout = ttGo.AddComponent<VerticalLayoutGroup>();
            ttLayout.padding = new RectOffset(8, 8, 6, 6);
            ttLayout.spacing = 2f;
            ttLayout.childForceExpandWidth = true;
            ttLayout.childForceExpandHeight = false;
            ttLayout.childControlWidth = true;
            ttLayout.childControlHeight = true;

            var ttFitter = ttGo.AddComponent<ContentSizeFitter>();
            ttFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ttFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Title
            var ttTitleGo = MakeRect("Title", ttGo.transform);
            var ttTitleTmp = ttTitleGo.AddComponent<TextMeshProUGUI>();
            ttTitleTmp.text = "";
            ttTitleTmp.fontSize = FontPrimary;
            ttTitleTmp.fontStyle = FontStyles.Bold;
            ttTitleTmp.color = AgedGold;
            ttTitleTmp.alignment = TextAlignmentOptions.TopLeft;
            FontManager.EnsureFont(ttTitleTmp);

            // Stats
            var ttStatsGo = MakeRect("Stats", ttGo.transform);
            var ttStatsTmp = ttStatsGo.AddComponent<TextMeshProUGUI>();
            ttStatsTmp.text = "";
            ttStatsTmp.fontSize = FontSecondary;
            ttStatsTmp.color = BoneWhite;
            ttStatsTmp.alignment = TextAlignmentOptions.TopLeft;
            ttStatsTmp.richText = true;
            FontManager.EnsureFont(ttStatsTmp);

            // Description
            var ttDescGo = MakeRect("Desc", ttGo.transform);
            var ttDescTmp = ttDescGo.AddComponent<TextMeshProUGUI>();
            ttDescTmp.text = "";
            ttDescTmp.fontSize = FontFlavor;
            ttDescTmp.color = SubtleText;
            ttDescTmp.alignment = TextAlignmentOptions.TopLeft;
            FontManager.EnsureFont(ttDescTmp);

            ttGo.SetActive(false);

            // =================================================================
            // Wire Controller
            // =================================================================
            var controller = canvasGo.AddComponent<CharacterMenuController>();
            controller.menuCanvas = canvas;
            controller.menuCanvasGroup = cg;
            controller.closeButton = closeBtn;
            controller.characterPreview = charPreview;

            // Player info
            controller.characterNameText = charNameGo;
            controller.classText = classGo;
            controller.levelText = lvlGo;
            controller.hpText = hpGoTxt;
            controller.mpText = mpGoTxt;

            // Stats
            controller.availablePointsText = ptsTmp;
            controller.strengthText = strTmp;
            controller.intelligenceText = intTmp;
            controller.agilityText = agiTmp;
            controller.allocateStrButton = strBtn;
            controller.allocateIntButton = intBtn;
            controller.allocateAgiButton = agiBtn;

            // Derived combat stats (Bonus HP/Mana shown inline with HP/MP, not here)
            controller.meleeDamageText = meleeTmp;
            controller.skillDamageText = skillDmgTmp;
            controller.speedText = spdTmp;
            controller.critChanceText = critTmp;

            // Equipment slots
            controller.equipSlotIcons = slotIcons;
            controller.equipSlotNames = slotNames;
            controller.equipSlotStats = slotStatTexts;
            controller.equipSlotButtons = slotRowBtns;

            // Inventory
            controller.inventoryCountText = invLabelTmp;
            controller.inventoryCellIcons = cellIcons;
            controller.inventoryCellBorders = cellBorders;
            controller.inventoryCellButtons = cellButtons;

            // Tooltip
            controller.tooltipPanel = ttGo;
            controller.tooltipTitle = ttTitleTmp;
            controller.tooltipStats = ttStatsTmp;
            controller.tooltipDesc = ttDescTmp;
            controller.tooltipRect = ttRect;
            controller.canvasRect = canvasGo.GetComponent<RectTransform>();

            // Wire equipment slot click handlers
            for (int i = 0; i < slotCount; i++)
            {
                // Left-click: start drag of equipped item
                slotRowBtns[i].onClick.RemoveAllListeners();
                int capturedSlot = i;
                slotRowBtns[i].onClick.AddListener(() => controller.OnEquipSlotClicked(capturedSlot));

                // Right-click: unequip to inventory
                var rightClick = slotRowBtns[i].gameObject.AddComponent<SlotRightClickHandler>();
                rightClick.onRightClick = () => controller.OnEquipSlotRightClicked(capturedSlot);
            }

            // Wire inventory cell handlers
            for (int i = 0; i < totalCells; i++)
            {
                int capturedIdx = i;

                // Left-click: only handles equip-drag-to-inventory drop
                cellButtons[i].onClick.AddListener(() => controller.OnInventoryCellClicked(capturedIdx));

                // Right-click: quick-equip from inventory
                var invRightClick = cellButtons[i].gameObject.AddComponent<InventoryRightClickHandler>();
                invRightClick.onRightClick = () => controller.OnInventoryCellRightClicked(capturedIdx);

                // Hover events via EventTrigger
                var trigger = cellButtons[i].GetComponent<EventTrigger>();
                if (trigger != null)
                {
                    var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                    enterEntry.callback.AddListener(_ => controller.OnInventoryCellHoverEnter(capturedIdx));
                    trigger.triggers.Add(enterEntry);

                    var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                    exitEntry.callback.AddListener(_ => controller.OnInventoryCellHoverExit(capturedIdx));
                    trigger.triggers.Add(exitEntry);

                    // Drag support
                    var beginDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
                    beginDragEntry.callback.AddListener(_ => controller.StartDrag(capturedIdx));
                    trigger.triggers.Add(beginDragEntry);

                    var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
                    dragEntry.callback.AddListener(data =>
                    {
                        var pointerData = data as PointerEventData;
                        if (pointerData != null)
                            controller.UpdateDragPosition(pointerData.position);
                    });
                    trigger.triggers.Add(dragEntry);

                    var endDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
                    endDragEntry.callback.AddListener(_ => controller.OnDragEnd());
                    trigger.triggers.Add(endDragEntry);

                    // Drop target (for equipment → inventory drags)
                    var dropEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drop };
                    dropEntry.callback.AddListener(_ => controller.OnInventoryCellDrop(capturedIdx));
                    trigger.triggers.Add(dropEntry);
                }
            }

            // Wire drag + drop + hover on equipment slot rows
            for (int i = 0; i < slotCount; i++)
            {
                int capturedSlot = i;
                var slotTrigger = slotRowBtns[i].gameObject.AddComponent<EventTrigger>();

                // Drop target (for inventory → equip drags)
                var dropEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drop };
                dropEntry.callback.AddListener(_ => controller.OnEquipSlotDrop(capturedSlot));
                slotTrigger.triggers.Add(dropEntry);

                // Drag source (for equip → inventory drags)
                var beginDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
                beginDragEntry.callback.AddListener(_ => controller.StartEquipDrag(capturedSlot, SlotDisplayOrder[capturedSlot]));
                slotTrigger.triggers.Add(beginDragEntry);

                var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
                dragEntry.callback.AddListener(data =>
                {
                    var pointerData = data as PointerEventData;
                    if (pointerData != null)
                        controller.UpdateDragPosition(pointerData.position);
                });
                slotTrigger.triggers.Add(dragEntry);

                var endDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
                endDragEntry.callback.AddListener(_ => controller.OnDragEnd());
                slotTrigger.triggers.Add(endDragEntry);

                // Hover tooltip on equipment slots
                var slotEnterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                slotEnterEntry.callback.AddListener(_ => controller.OnEquipSlotHoverEnter(capturedSlot));
                slotTrigger.triggers.Add(slotEnterEntry);

                var slotExitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                slotExitEntry.callback.AddListener(_ => controller.OnEquipSlotHoverExit(capturedSlot));
                slotTrigger.triggers.Add(slotExitEntry);
            }

            // Wire button listeners (close, allocate)
            controller.WireButtonListeners();

            Debug.Log("[CharacterMenuController] Runtime UI created.");
            return controller;
        }

        /// <summary>
        /// Called when a drag ends (released anywhere without hitting a drop target).
        /// </summary>
        private void OnDragEnd()
        {
            CancelDrag();
        }

        /// <summary>
        /// Called when an equipment drag is dropped on an inventory cell.
        /// Unequips the item to inventory.
        /// </summary>
        private void OnInventoryCellDrop(int cellIndex)
        {
            if (dragEquipSlotIndex < 0) return;
            if (equipmentManager == null || inventoryManager == null) return;

            var slotType = SlotDisplayOrder[dragEquipSlotIndex];
            if (inventoryManager.UnequipToInventory(slotType, equipmentManager))
            {
                if (UIManager.Instance != null)
                    UIManager.Instance.PlaySelectSound();
            }
            else
            {
                Debug.Log("[CharacterMenuController] Inventory full, cannot unequip");
                if (UIManager.Instance != null)
                    UIManager.Instance.PlayErrorSound();
            }

            CancelDrag();
        }

        /// <summary>
        /// Called when an item is dropped on an equipment slot.
        /// </summary>
        private void OnEquipSlotDrop(int slotIndex)
        {
            if (dragSourceIndex < 0) return;
            if (inventoryManager == null || equipmentManager == null) return;
            if (slotIndex < 0 || slotIndex >= SlotDisplayOrder.Length) return;

            var item = inventoryManager.GetItem(dragSourceIndex);
            if (item == null)
            {
                CancelDrag();
                return;
            }

            var targetSlot = SlotDisplayOrder[slotIndex];
            if (item.slotType == targetSlot)
            {
                inventoryManager.EquipFromInventory(dragSourceIndex, equipmentManager);
                if (UIManager.Instance != null)
                    UIManager.Instance.PlaySelectSound();
            }

            CancelDrag();
        }

        #region UI Build Helpers

        private static Button BuildCloseButton(Transform parent)
        {
            var closeBtnGo = MakeRect("CloseButton", parent);
            var closeBtnRect = closeBtnGo.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1, 0.5f);
            closeBtnRect.anchorMax = new Vector2(1, 0.5f);
            closeBtnRect.pivot = new Vector2(1, 0.5f);
            closeBtnRect.anchoredPosition = new Vector2(-10, 0);
            closeBtnRect.sizeDelta = new Vector2(36, 36);

            var closeBtnImg = closeBtnGo.AddComponent<Image>();
            closeBtnImg.sprite = WhiteSprite;
            closeBtnImg.color = BtnNormal;

            var closeBtn = closeBtnGo.AddComponent<Button>();
            var colors = closeBtn.colors;
            colors.normalColor = BtnNormal;
            colors.highlightedColor = BtnHover;
            colors.pressedColor = BtnPress;
            colors.selectedColor = BtnHover;
            colors.fadeDuration = 0.1f;
            closeBtn.colors = colors;

            var textGo = MakeRect("Text", closeBtnGo.transform);
            Stretch(textGo);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "X";
            tmp.fontSize = FontPrimary;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = BoneWhite;
            tmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(tmp);

            return closeBtn;
        }

        private static TMP_Text BuildInfoText(Transform parent, string name, string text,
            float fontSize, Color color, float xOffset, float yOffset)
        {
            var go = MakeRect(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(xOffset, yOffset);
            rt.sizeDelta = new Vector2(-xOffset, 22);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Left;
            FontManager.EnsureFont(tmp);
            return tmp;
        }

        private static (TMP_Text statText, Button allocBtn)
            BuildStatRow(Transform parent, string label, float yOffset)
        {
            var rowGo = MakeRect(label + "Row", parent);
            var rowRect = rowGo.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.pivot = new Vector2(0, 1);
            rowRect.anchoredPosition = new Vector2(0, yOffset);
            rowRect.sizeDelta = new Vector2(0, 24);

            // Stat value text
            var statGo = MakeRect("StatVal", rowGo.transform);
            var statRect = statGo.GetComponent<RectTransform>();
            statRect.anchorMin = new Vector2(0, 0);
            statRect.anchorMax = new Vector2(0.78f, 1);
            statRect.offsetMin = Vector2.zero;
            statRect.offsetMax = Vector2.zero;
            var statTmp = statGo.AddComponent<TextMeshProUGUI>();
            statTmp.text = $"{label}: 1";
            statTmp.fontSize = FontPrimary;
            statTmp.color = BoneWhite;
            statTmp.alignment = TextAlignmentOptions.Left;
            statTmp.overflowMode = TextOverflowModes.Ellipsis;
            statTmp.textWrappingMode = TextWrappingModes.NoWrap;
            FontManager.EnsureFont(statTmp);

            // Allocate [+] button
            var btnGo = MakeRect("AllocBtn", rowGo.transform);
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.80f, 0.05f);
            btnRect.anchorMax = new Vector2(0.96f, 0.95f);
            btnRect.offsetMin = new Vector2(2, 0);
            btnRect.offsetMax = new Vector2(-2, 0);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.sprite = WhiteSprite;
            btnImg.color = BtnNormal;
            var btn = btnGo.AddComponent<Button>();
            var btnCols = btn.colors;
            btnCols.normalColor = BtnNormal;
            btnCols.highlightedColor = BtnHover;
            btnCols.pressedColor = BtnPress;
            btnCols.selectedColor = BtnHover;
            btnCols.fadeDuration = 0.1f;
            btn.colors = btnCols;
            var btnTextGo = MakeRect("Text", btnGo.transform);
            Stretch(btnTextGo);
            var btnTmp = btnTextGo.AddComponent<TextMeshProUGUI>();
            btnTmp.text = "+";
            btnTmp.fontSize = FontPrimary;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.color = AgedGold;
            btnTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(btnTmp);

            return (statTmp, btn);
        }

        /// <summary>
        /// Builds a single derived stat text line (secondary font, subtle color).
        /// </summary>
        private static TMP_Text BuildDerivedStatLine(Transform parent, string defaultText, float yOffset)
        {
            var go = MakeRect("DerivedStat", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(0, yOffset);
            rt.sizeDelta = new Vector2(0, 20);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = defaultText;
            tmp.fontSize = FontSecondary;
            tmp.color = SubtleText;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            FontManager.EnsureFont(tmp);
            return tmp;
        }

        private const float SLOT_ROW_HEIGHT = 50f;

        private static (Button rowBtn, Image icon, TMP_Text name, TMP_Text stats) BuildEquipmentSlotRow(
            Transform parent, string slotLabel, float yOffset)
        {
            var rowGo = MakeRect(slotLabel + "Row", parent);
            var rowRect = rowGo.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.pivot = new Vector2(0, 1);
            rowRect.anchoredPosition = new Vector2(0, yOffset);
            rowRect.sizeDelta = new Vector2(0, SLOT_ROW_HEIGHT);

            var rowBg = rowGo.AddComponent<Image>();
            rowBg.sprite = WhiteSprite;
            rowBg.color = SlotRowBg;

            var rowBtn = rowGo.AddComponent<Button>();
            var btnColors = rowBtn.colors;
            btnColors.normalColor = SlotRowBg;
            btnColors.highlightedColor = SlotRowHover;
            btnColors.pressedColor = SlotRowPress;
            btnColors.selectedColor = SlotRowHover;
            btnColors.fadeDuration = 0.1f;
            rowBtn.colors = btnColors;
            rowBtn.targetGraphic = rowBg;

            // Icon container
            var iconContainerGo = MakeRect("IconContainer", rowGo.transform);
            var iconContainerRect = iconContainerGo.GetComponent<RectTransform>();
            iconContainerRect.anchorMin = new Vector2(0, 0);
            iconContainerRect.anchorMax = new Vector2(0, 1);
            iconContainerRect.pivot = new Vector2(0, 0.5f);
            iconContainerRect.anchoredPosition = new Vector2(4, 0);
            iconContainerRect.sizeDelta = new Vector2(46, -6);
            var iconContainerBg = iconContainerGo.AddComponent<Image>();
            iconContainerBg.sprite = WhiteSprite;
            iconContainerBg.color = CellBg;
            iconContainerBg.raycastTarget = false;
            iconContainerGo.AddComponent<RectMask2D>();

            var iconGo = MakeRect("Icon", iconContainerGo.transform);
            Stretch(iconGo);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.color = new Color(1f, 1f, 1f, 0f);

            // Slot label
            var labelGo = MakeRect("SlotLabel", rowGo.transform);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(0, 1);
            labelRect.anchoredPosition = new Vector2(50, -2);
            labelRect.sizeDelta = new Vector2(-58, 14);
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = slotLabel;
            labelTmp.fontSize = FontFlavor;
            labelTmp.color = SubtleText;
            labelTmp.alignment = TextAlignmentOptions.TopLeft;
            labelTmp.raycastTarget = false;
            FontManager.EnsureFont(labelTmp);

            // Item name (middle)
            var nameGo = MakeRect("ItemName", rowGo.transform);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.28f);
            nameRect.anchorMax = new Vector2(1, 0.72f);
            nameRect.offsetMin = new Vector2(50, 0);
            nameRect.offsetMax = new Vector2(-6, 0);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "Empty";
            nameTmp.fontSize = FontPrimary;
            nameTmp.color = BoneWhite;
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.raycastTarget = false;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            nameTmp.enableAutoSizing = true;
            nameTmp.fontSizeMin = FontSecondary;
            nameTmp.fontSizeMax = FontPrimary;
            FontManager.EnsureFont(nameTmp);

            // Stats (bottom)
            var statsGo = MakeRect("Stats", rowGo.transform);
            var statsRect = statsGo.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 0);
            statsRect.anchorMax = new Vector2(1, 0.28f);
            statsRect.offsetMin = new Vector2(50, 2);
            statsRect.offsetMax = new Vector2(-6, 0);
            var statsTmp = statsGo.AddComponent<TextMeshProUGUI>();
            statsTmp.text = "";
            statsTmp.fontSize = FontSecondary;
            statsTmp.color = AgedGold;
            statsTmp.alignment = TextAlignmentOptions.Left;
            statsTmp.raycastTarget = false;
            statsTmp.overflowMode = TextOverflowModes.Ellipsis;
            statsTmp.textWrappingMode = TextWrappingModes.NoWrap;
            statsTmp.enableAutoSizing = true;
            statsTmp.fontSizeMin = FontFlavor;
            statsTmp.fontSizeMax = FontSecondary;
            FontManager.EnsureFont(statsTmp);

            return (rowBtn, iconImg, nameTmp, statsTmp);
        }

        private static void BuildLayoutDivider(Transform parent, bool horizontal)
        {
            var go = MakeRect("Divider", parent);
            var img = go.AddComponent<Image>();
            img.sprite = WhiteSprite;
            img.color = DividerCol;
            img.raycastTarget = false;
            var le = go.AddComponent<LayoutElement>();
            if (horizontal)
            {
                le.preferredHeight = 2;
                le.flexibleWidth = 1;
            }
            else
            {
                le.preferredWidth = 2;
                le.flexibleHeight = 1;
            }
        }

        private static LayoutElement AddLayout(GameObject go,
            float prefH = -1, float prefW = -1,
            float flexH = -1, float flexW = -1,
            float minH = -1, float minW = -1)
        {
            var le = go.AddComponent<LayoutElement>();
            if (prefH >= 0) le.preferredHeight = prefH;
            if (prefW >= 0) le.preferredWidth = prefW;
            if (flexH >= 0) le.flexibleHeight = flexH;
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (minH >= 0) le.minHeight = minH;
            if (minW >= 0) le.minWidth = minW;
            return le;
        }

        private static GameObject MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        #endregion

        #endregion

        /// <summary>
        /// Helper component that detects right-clicks on a UI element.
        /// Used on equipment slot rows for right-click-to-unequip.
        /// </summary>
        private class SlotRightClickHandler : MonoBehaviour, IPointerClickHandler
        {
            public System.Action onRightClick;

            public void OnPointerClick(PointerEventData eventData)
            {
                if (eventData.button == PointerEventData.InputButton.Right)
                    onRightClick?.Invoke();
            }
        }

        private class InventoryRightClickHandler : MonoBehaviour, IPointerClickHandler
        {
            public System.Action onRightClick;

            public void OnPointerClick(PointerEventData eventData)
            {
                if (eventData.button == PointerEventData.InputButton.Right)
                    onRightClick?.Invoke();
            }
        }
    }
}
