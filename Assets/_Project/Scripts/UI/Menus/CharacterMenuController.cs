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
        [SerializeField] private Image[] equipSlotIcons = new Image[7];
        [SerializeField] private TMP_Text[] equipSlotNames = new TMP_Text[7];
        [SerializeField] private TMP_Text[] equipSlotStats = new TMP_Text[7];
        [SerializeField] private Button[] equipSlotButtons = new Button[7];

        [Header("Inventory")]
        [SerializeField] private TMP_Text inventoryCountText;
        [SerializeField] private Image[] inventoryCellIcons;
        [SerializeField] private Image[] inventoryCellBorders;
        [SerializeField] private Button[] inventoryCellButtons;

        [Header("Compare Panel")]
        [SerializeField] private GameObject comparePanel;
        [SerializeField] private TMP_Text compareItemName;
        [SerializeField] private TMP_Text compareStatDeltas;

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

        // Drag state
        private int dragSourceIndex = -1;
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
            if (isOpen) { RefreshEquipment(); RefreshCharacterPreview(); }
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
            HideComparePanel();

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
                levelText.text = $"Lv {levelSystem.CurrentLevel}";

            if (hpText != null && healthSystem != null)
                hpText.text = $"HP: {healthSystem.CurrentHealth:F0}/{healthSystem.MaxHealth:F0}";

            if (mpText != null && manaSystem != null)
                mpText.text = $"MP: {manaSystem.CurrentMana:F0}/{manaSystem.MaxMana:F0}";
        }

        private void RefreshStats()
        {
            if (statSystem == null) return;

            if (availablePointsText != null)
                availablePointsText.text = $"Pts: {statSystem.AvailableStatPoints}";

            if (strengthText != null)
                strengthText.text = $"STR: {statSystem.Strength}";
            if (intelligenceText != null)
                intelligenceText.text = $"INT: {statSystem.Intelligence}";
            if (agilityText != null)
                agilityText.text = $"AGI: {statSystem.Agility}";

            if (bonusHPText != null)
                bonusHPText.text = $"HP+{statSystem.BonusMaxHP:F0}";
            if (meleeDamageText != null)
                meleeDamageText.text = $"Melee{statSystem.MeleeDamageMultiplier:F2}";
            if (bonusManaText != null)
                bonusManaText.text = $"MP+{statSystem.BonusMaxMana:F0}";
            if (skillDamageText != null)
                skillDamageText.text = $"Skill{statSystem.SkillDamageMultiplier:F2}";
            if (speedText != null)
                speedText.text = $"Spd{(1f / statSystem.AttackSpeedMultiplier):F2}";
            if (critChanceText != null)
                critChanceText.text = $"Crit{statSystem.CritChance * 100f:F0}%";

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

            for (int i = 0; i < SlotDisplayOrder.Length && i < 7; i++)
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

        private void AllocateStat(string statName)
        {
            if (statSystem == null) return;

            if (statSystem.AllocateStat(statName))
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

            // Right-click / normal click: unequip to inventory
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

            var item = inventoryManager.GetItem(cellIndex);
            if (item == null) return;

            // Double-click / quick-equip: equip to matching slot
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
                HideComparePanel();
                return;
            }

            // Show compare panel
            var equipped = equipmentManager.GetEquipped(item.slotType);
            ShowComparePanel(item, equipped);

            // Highlight border
            if (cellIndex < inventoryCellBorders.Length && inventoryCellBorders[cellIndex] != null)
                inventoryCellBorders[cellIndex].color = AgedGold;
        }

        private void OnInventoryCellHoverExit(int cellIndex)
        {
            HideComparePanel();

            if (cellIndex < inventoryCellBorders.Length && inventoryCellBorders[cellIndex] != null)
                inventoryCellBorders[cellIndex].color = CellBorderNormal;
        }

        private void ShowComparePanel(EquipmentData candidate, EquipmentData currentEquipped)
        {
            if (comparePanel == null) return;

            comparePanel.SetActive(true);

            if (compareItemName != null)
                compareItemName.text = candidate.displayName;

            if (compareStatDeltas != null)
                compareStatDeltas.text = StatComparisonHelper.BuildCompareText(currentEquipped, candidate);
        }

        private void HideComparePanel()
        {
            if (comparePanel != null)
                comparePanel.SetActive(false);
        }

        #endregion

        #region Drag and Drop

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

            // Restore source cell visual
            if (dragSourceIndex >= 0)
            {
                if (dragSourceIndex < inventoryCellBorders.Length && inventoryCellBorders[dragSourceIndex] != null)
                    inventoryCellBorders[dragSourceIndex].color = CellBorderNormal;

                // Refresh icon
                RefreshInventory();
            }

            dragSourceIndex = -1;
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
        private static readonly Color DeepCrimson = new Color(0.545f, 0f, 0f, 1f);
        private static readonly Color DarkBlue = new Color(0.1f, 0.2f, 0.6f, 1f);
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

            // --- Center panel (960x680) ---
            var panelGo = MakeRect("Panel", canvasGo.transform);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(960f, 680f);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.sprite = WhiteSprite;
            panelImg.color = PanelBg;

            // --- Title row (top 50px) ---
            var titleRow = MakeRect("TitleRow", panelGo.transform);
            var titleRowRect = titleRow.GetComponent<RectTransform>();
            titleRowRect.anchorMin = new Vector2(0, 1);
            titleRowRect.anchorMax = new Vector2(1, 1);
            titleRowRect.pivot = new Vector2(0.5f, 1);
            titleRowRect.anchoredPosition = Vector2.zero;
            titleRowRect.sizeDelta = new Vector2(0, 50);

            var titleTextGo = MakeRect("TitleText", titleRow.transform);
            Stretch(titleTextGo);
            var titleTmp = titleTextGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "CHARACTER";
            titleTmp.fontSize = 28;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = AgedGold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(titleTmp);

            // Close [X] button
            var closeBtn = BuildCloseButton(titleRow.transform);

            // --- Divider below title ---
            BuildHorizontalDivider(panelGo.transform, 50f);

            // =================================================================
            // TOP SECTION: anchorMin(0, 0.37) to anchorMax(1, 1) minus title
            // =================================================================
            var topSection = MakeRect("TopSection", panelGo.transform);
            var topRect = topSection.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 0.37f);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.offsetMin = new Vector2(20, 10);
            topRect.offsetMax = new Vector2(-20, -54);

            // --- LEFT COLUMN: Preview + Player Info + Stats (0-48%) ---
            var leftCol = MakeRect("LeftColumn", topSection.transform);
            var leftColRect = leftCol.GetComponent<RectTransform>();
            leftColRect.anchorMin = new Vector2(0, 0);
            leftColRect.anchorMax = new Vector2(0.48f, 1);
            leftColRect.offsetMin = Vector2.zero;
            leftColRect.offsetMax = Vector2.zero;

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

            var charNameGo = BuildInfoText(leftCol.transform, "CharName", "Hero", 20, AgedGold, infoX, infoY);
            infoY -= lineH + 2f;
            var classGo = BuildInfoText(leftCol.transform, "Class", "(Adventurer)", 16, SubtleText, infoX, infoY);
            infoY -= lineH + 2f;
            var lvlGo = BuildInfoText(leftCol.transform, "Level", "Lv 1", 16, BoneWhite, infoX, infoY);
            infoY -= lineH + 2f;
            var hpGoTxt = BuildInfoText(leftCol.transform, "HP", "HP: 100/100", 16, DeepCrimson, infoX, infoY);
            infoY -= lineH + 2f;
            var mpGoTxt = BuildInfoText(leftCol.transform, "MP", "MP: 50/50", 16, DarkBlue, infoX, infoY);

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
            ptsTmp.text = "Pts: 0";
            ptsTmp.fontSize = 16;
            ptsTmp.fontStyle = FontStyles.Bold;
            ptsTmp.color = AgedGold;
            ptsTmp.alignment = TextAlignmentOptions.Left;
            FontManager.EnsureFont(ptsTmp);

            statsBaseY -= 28f;

            // Stat rows: STR [+]  HP+60 Melee1.24
            var (strTmp, strBtn, bHpTmp, meleeTmp) =
                BuildStatRow(leftCol.transform, "STR", statsBaseY);
            statsBaseY -= 26f;

            var (intTmp, intBtn, bManaTmp, skillDmgTmp) =
                BuildStatRow(leftCol.transform, "INT", statsBaseY);
            statsBaseY -= 26f;

            var (agiTmp, agiBtn, spdTmp, critTmp) =
                BuildStatRow(leftCol.transform, "AGI", statsBaseY);

            // --- GOLD VERTICAL DIVIDER at 49% ---
            BuildVerticalDivider(topSection.transform, 0.49f);

            // --- RIGHT COLUMN: Equipment Slots (50-100%) ---
            var rightCol = MakeRect("RightColumn", topSection.transform);
            var rightColRect = rightCol.GetComponent<RectTransform>();
            rightColRect.anchorMin = new Vector2(0.51f, 0);
            rightColRect.anchorMax = new Vector2(1, 1);
            rightColRect.offsetMin = Vector2.zero;
            rightColRect.offsetMax = Vector2.zero;

            // Build 7 equipment slot rows
            var slotIcons = new Image[7];
            var slotNames = new TMP_Text[7];
            var slotStatTexts = new TMP_Text[7];
            var slotRowBtns = new Button[7];

            float slotY = 0f;
            float slotRowHeight = 50f;
            float slotSpacing = 6f;

            for (int i = 0; i < 7; i++)
            {
                var (rowBtn, icon, nameTxt, statsTxt) =
                    BuildEquipmentSlotRow(rightCol.transform, SlotLabels[i], slotY);
                slotIcons[i] = icon;
                slotNames[i] = nameTxt;
                slotStatTexts[i] = statsTxt;
                slotRowBtns[i] = rowBtn;

                int capturedIndex = i;
                rowBtn.onClick.AddListener(() => { /* wired after controller created */ });

                slotY -= slotRowHeight + slotSpacing;
            }

            // =================================================================
            // GOLD HORIZONTAL DIVIDER at y=37%
            // =================================================================
            BuildHorizontalDivider(panelGo.transform, 680f * 0.63f); // ~428px from top

            // =================================================================
            // BOTTOM STRIP: Inventory + Compare Panel
            // =================================================================
            var bottomSection = MakeRect("BottomSection", panelGo.transform);
            var bottomRect = bottomSection.GetComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0, 0);
            bottomRect.anchorMax = new Vector2(1, 0.35f);
            bottomRect.offsetMin = new Vector2(20, 16);
            bottomRect.offsetMax = new Vector2(-20, -6);

            // Inventory label
            var invLabelGo = MakeRect("InvLabel", bottomSection.transform);
            var invLabelRect = invLabelGo.GetComponent<RectTransform>();
            invLabelRect.anchorMin = new Vector2(0, 1);
            invLabelRect.anchorMax = new Vector2(0.6f, 1);
            invLabelRect.pivot = new Vector2(0, 1);
            invLabelRect.anchoredPosition = Vector2.zero;
            invLabelRect.sizeDelta = new Vector2(0, 22);
            var invLabelTmp = invLabelGo.AddComponent<TextMeshProUGUI>();
            invLabelTmp.text = "INVENTORY (0/24)";
            invLabelTmp.fontSize = 14;
            invLabelTmp.fontStyle = FontStyles.Bold;
            invLabelTmp.color = AgedGold;
            invLabelTmp.alignment = TextAlignmentOptions.Left;
            FontManager.EnsureFont(invLabelTmp);

            // Inventory grid (left 60%): 8 cols x 3 rows
            var gridContainer = MakeRect("InventoryGrid", bottomSection.transform);
            var gridRect = gridContainer.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0, 0);
            gridRect.anchorMax = new Vector2(0.6f, 1);
            gridRect.offsetMin = new Vector2(0, 0);
            gridRect.offsetMax = new Vector2(0, -26);

            int cols = 8;
            int rows = 3;
            int totalCells = cols * rows;
            float cellSize = 52f;
            float cellSpacing = 4f;

            var cellIcons = new Image[totalCells];
            var cellBorders = new Image[totalCells];
            var cellButtons = new Button[totalCells];

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int idx = row * cols + col;
                    float x = col * (cellSize + cellSpacing);
                    float y = -(row * (cellSize + cellSpacing));

                    var cellGo = MakeRect($"Cell_{idx}", gridContainer.transform);
                    var cellRect = cellGo.GetComponent<RectTransform>();
                    cellRect.anchorMin = new Vector2(0, 1);
                    cellRect.anchorMax = new Vector2(0, 1);
                    cellRect.pivot = new Vector2(0, 1);
                    cellRect.anchoredPosition = new Vector2(x, y);
                    cellRect.sizeDelta = new Vector2(cellSize, cellSize);

                    // Cell background
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
                    // Make it an outline by adding a child that covers the interior
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
                    iconRect.anchorMin = new Vector2(0.1f, 0.1f);
                    iconRect.anchorMax = new Vector2(0.9f, 0.9f);
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

                    // EventTrigger for hover
                    var trigger = cellGo.AddComponent<EventTrigger>();

                    cellIcons[idx] = iconImg;
                    cellBorders[idx] = borderImg;
                    cellButtons[idx] = cellBtn;
                }
            }

            // Compare panel (right 38%)
            var compPanel = MakeRect("ComparePanel", bottomSection.transform);
            var compPanelRect = compPanel.GetComponent<RectTransform>();
            compPanelRect.anchorMin = new Vector2(0.62f, 0);
            compPanelRect.anchorMax = new Vector2(1, 1);
            compPanelRect.offsetMin = Vector2.zero;
            compPanelRect.offsetMax = new Vector2(0, -26);

            var compBg = compPanel.AddComponent<Image>();
            compBg.sprite = WhiteSprite;
            compBg.color = CompareBg;

            // Compare title "COMPARE"
            var compTitleGo = MakeRect("CompTitle", compPanel.transform);
            var compTitleRect = compTitleGo.GetComponent<RectTransform>();
            compTitleRect.anchorMin = new Vector2(0, 1);
            compTitleRect.anchorMax = new Vector2(1, 1);
            compTitleRect.pivot = new Vector2(0.5f, 1);
            compTitleRect.anchoredPosition = new Vector2(0, -4);
            compTitleRect.sizeDelta = new Vector2(-16, 24);
            var compTitleTmp = compTitleGo.AddComponent<TextMeshProUGUI>();
            compTitleTmp.text = "";
            compTitleTmp.fontSize = 16;
            compTitleTmp.fontStyle = FontStyles.Bold;
            compTitleTmp.color = AgedGold;
            compTitleTmp.alignment = TextAlignmentOptions.Left;
            FontManager.EnsureFont(compTitleTmp);

            // Compare stat deltas
            var compDeltaGo = MakeRect("CompDeltas", compPanel.transform);
            var compDeltaRect = compDeltaGo.GetComponent<RectTransform>();
            compDeltaRect.anchorMin = new Vector2(0, 0);
            compDeltaRect.anchorMax = new Vector2(1, 1);
            compDeltaRect.offsetMin = new Vector2(8, 8);
            compDeltaRect.offsetMax = new Vector2(-8, -30);
            var compDeltaTmp = compDeltaGo.AddComponent<TextMeshProUGUI>();
            compDeltaTmp.text = "";
            compDeltaTmp.fontSize = 14;
            compDeltaTmp.color = BoneWhite;
            compDeltaTmp.alignment = TextAlignmentOptions.TopLeft;
            compDeltaTmp.richText = true;
            FontManager.EnsureFont(compDeltaTmp);

            compPanel.SetActive(false);

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

            // Derived stats
            controller.bonusHPText = bHpTmp;
            controller.meleeDamageText = meleeTmp;
            controller.bonusManaText = bManaTmp;
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

            // Compare
            controller.comparePanel = compPanel;
            controller.compareItemName = compTitleTmp;
            controller.compareStatDeltas = compDeltaTmp;

            // Wire equipment slot click handlers
            for (int i = 0; i < 7; i++)
            {
                // Remove the placeholder listener and add real one
                slotRowBtns[i].onClick.RemoveAllListeners();
                int capturedSlot = i;
                slotRowBtns[i].onClick.AddListener(() => controller.OnEquipSlotClicked(capturedSlot));
            }

            // Wire inventory cell handlers
            for (int i = 0; i < totalCells; i++)
            {
                int capturedIdx = i;

                // Click = quick-equip
                cellButtons[i].onClick.AddListener(() => controller.OnInventoryCellClicked(capturedIdx));

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
                }
            }

            // Wire drop targets on equipment slot rows
            for (int i = 0; i < 7; i++)
            {
                int capturedSlot = i;
                var slotTrigger = slotRowBtns[i].gameObject.AddComponent<EventTrigger>();

                var dropEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drop };
                dropEntry.callback.AddListener(_ => controller.OnEquipSlotDrop(capturedSlot));
                slotTrigger.triggers.Add(dropEntry);
            }

            // Wire button listeners (close, allocate)
            controller.WireButtonListeners();

            Debug.Log("[CharacterMenuController] Runtime UI created.");
            return controller;
        }

        /// <summary>
        /// Called when a drag ends (released anywhere).
        /// </summary>
        private void OnDragEnd()
        {
            // If not dropped on a valid target, cancel
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
            tmp.fontSize = 20;
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

        private static (TMP_Text statText, Button allocBtn, TMP_Text derived1, TMP_Text derived2)
            BuildStatRow(Transform parent, string label, float yOffset)
        {
            var rowGo = MakeRect(label + "Row", parent);
            var rowRect = rowGo.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.pivot = new Vector2(0, 1);
            rowRect.anchoredPosition = new Vector2(0, yOffset);
            rowRect.sizeDelta = new Vector2(0, 24);

            // Stat value text (left)
            var statGo = MakeRect("StatVal", rowGo.transform);
            var statRect = statGo.GetComponent<RectTransform>();
            statRect.anchorMin = new Vector2(0, 0);
            statRect.anchorMax = new Vector2(0.22f, 1);
            statRect.offsetMin = Vector2.zero;
            statRect.offsetMax = Vector2.zero;
            var statTmp = statGo.AddComponent<TextMeshProUGUI>();
            statTmp.text = $"{label}: 1";
            statTmp.fontSize = 15;
            statTmp.color = BoneWhite;
            statTmp.alignment = TextAlignmentOptions.Left;
            FontManager.EnsureFont(statTmp);

            // Allocate [+] button
            var btnGo = MakeRect("AllocBtn", rowGo.transform);
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.22f, 0.05f);
            btnRect.anchorMax = new Vector2(0.30f, 0.95f);
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
            btnTmp.fontSize = 16;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.color = AgedGold;
            btnTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(btnTmp);

            // Derived stat 1
            var d1Go = MakeRect("Derived1", rowGo.transform);
            var d1Rect = d1Go.GetComponent<RectTransform>();
            d1Rect.anchorMin = new Vector2(0.32f, 0);
            d1Rect.anchorMax = new Vector2(0.60f, 1);
            d1Rect.offsetMin = Vector2.zero;
            d1Rect.offsetMax = Vector2.zero;
            var d1Tmp = d1Go.AddComponent<TextMeshProUGUI>();
            d1Tmp.text = "";
            d1Tmp.fontSize = 13;
            d1Tmp.color = SubtleText;
            d1Tmp.alignment = TextAlignmentOptions.Left;
            FontManager.EnsureFont(d1Tmp);

            // Derived stat 2
            var d2Go = MakeRect("Derived2", rowGo.transform);
            var d2Rect = d2Go.GetComponent<RectTransform>();
            d2Rect.anchorMin = new Vector2(0.62f, 0);
            d2Rect.anchorMax = new Vector2(1, 1);
            d2Rect.offsetMin = Vector2.zero;
            d2Rect.offsetMax = Vector2.zero;
            var d2Tmp = d2Go.AddComponent<TextMeshProUGUI>();
            d2Tmp.text = "";
            d2Tmp.fontSize = 13;
            d2Tmp.color = SubtleText;
            d2Tmp.alignment = TextAlignmentOptions.Left;
            FontManager.EnsureFont(d2Tmp);

            return (statTmp, btn, d1Tmp, d2Tmp);
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
            iconContainerRect.anchoredPosition = new Vector2(4, -2);
            iconContainerRect.sizeDelta = new Vector2(42, -12);
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
            labelTmp.fontSize = 11;
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
            nameTmp.fontSize = 14;
            nameTmp.color = BoneWhite;
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.raycastTarget = false;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            nameTmp.enableAutoSizing = true;
            nameTmp.fontSizeMin = 10f;
            nameTmp.fontSizeMax = 14f;
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
            statsTmp.fontSize = 12;
            statsTmp.color = AgedGold;
            statsTmp.alignment = TextAlignmentOptions.Left;
            statsTmp.raycastTarget = false;
            statsTmp.overflowMode = TextOverflowModes.Ellipsis;
            statsTmp.textWrappingMode = TextWrappingModes.NoWrap;
            statsTmp.enableAutoSizing = true;
            statsTmp.fontSizeMin = 9f;
            statsTmp.fontSizeMax = 12f;
            FontManager.EnsureFont(statsTmp);

            return (rowBtn, iconImg, nameTmp, statsTmp);
        }

        private static void BuildHorizontalDivider(Transform parent, float yOffset)
        {
            var divider = MakeRect("HDivider", parent);
            var divRect = divider.GetComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0, 1);
            divRect.anchorMax = new Vector2(1, 1);
            divRect.pivot = new Vector2(0.5f, 1);
            divRect.anchoredPosition = new Vector2(0, -yOffset);
            divRect.sizeDelta = new Vector2(-20, 2);
            var divImg = divider.AddComponent<Image>();
            divImg.sprite = WhiteSprite;
            divImg.color = DividerCol;
        }

        private static void BuildVerticalDivider(Transform parent, float xAnchor)
        {
            var divider = MakeRect("VDivider", parent);
            var divRect = divider.GetComponent<RectTransform>();
            divRect.anchorMin = new Vector2(xAnchor, 0.05f);
            divRect.anchorMax = new Vector2(xAnchor, 0.95f);
            divRect.pivot = new Vector2(0.5f, 0.5f);
            divRect.sizeDelta = new Vector2(2, 0);
            var divImg = divider.AddComponent<Image>();
            divImg.sprite = WhiteSprite;
            divImg.color = DividerCol;
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
    }
}
