using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Controller for the equipment menu. Opens with E key, shows 4 equipment slots
    /// with character preview. Follows the StatMenuController/SkillTreeController pattern.
    /// </summary>
    public class EquipmentMenuController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas equipmentCanvas;
        [SerializeField] private CanvasGroup equipmentCanvasGroup;
        [SerializeField] private Button closeButton;

        [Header("Character Preview")]
        [SerializeField] private UILayeredSpritePreview characterPreview;

        [Header("Slot Displays")]
        [SerializeField] private Image weaponIcon;
        [SerializeField] private TMP_Text weaponNameText;
        [SerializeField] private TMP_Text weaponStatsText;
        [SerializeField] private Image armorIcon;
        [SerializeField] private TMP_Text armorNameText;
        [SerializeField] private TMP_Text armorStatsText;
        [SerializeField] private Image bootsIcon;
        [SerializeField] private TMP_Text bootsNameText;
        [SerializeField] private TMP_Text bootsStatsText;
        [SerializeField] private Image accessoryIcon;
        [SerializeField] private TMP_Text accessoryNameText;
        [SerializeField] private TMP_Text accessoryStatsText;

        [Header("Settings")]
        [SerializeField] private bool pauseGameWhenOpen = true;
        [SerializeField] private int canvasSortOrder = 150;

        private bool isOpen;
        private PlayerInput playerInput;
        private int lastToggleFrame = -1;

        private InputAction fallbackOpenAction;
        private InputAction escapeAction;

        private EquipmentManager equipmentManager;

        public bool IsOpen => isOpen;

        public event System.Action OnOpened;
        public event System.Action OnClosed;

        private void Awake()
        {
            if (equipmentCanvas == null)
                equipmentCanvas = GetComponent<Canvas>();

            if (equipmentCanvasGroup == null && equipmentCanvas != null)
            {
                equipmentCanvasGroup = equipmentCanvas.GetComponent<CanvasGroup>();
                if (equipmentCanvasGroup == null)
                    equipmentCanvasGroup = equipmentCanvas.gameObject.AddComponent<CanvasGroup>();
            }

            if (equipmentCanvas != null)
                equipmentCanvas.sortingOrder = canvasSortOrder;

            playerInput = FindAnyObjectByType<PlayerInput>();

            fallbackOpenAction = new InputAction("OpenEquipment", InputActionType.Button, "<Keyboard>/e");
            escapeAction = new InputAction("CloseEquipment", InputActionType.Button, "<Keyboard>/escape");

            WireButtonListeners();

            Close();

            if (equipmentCanvas != null)
                equipmentCanvas.enabled = false;
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

            FindEquipmentManager();
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

            if (equipmentManager != null)
                equipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;
        }

        private void OnDestroy()
        {
            fallbackOpenAction?.Dispose();
            escapeAction?.Dispose();
        }

        private void WireButtonListeners()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void FindEquipmentManager()
        {
            if (equipmentManager != null) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            equipmentManager = player.GetComponent<EquipmentManager>();
            if (equipmentManager != null)
                equipmentManager.OnEquipmentChanged += HandleEquipmentChanged;
        }

        private void HandleEquipmentChanged(EquipmentSlotType slot, EquipmentData item)
        {
            if (isOpen) RefreshDisplay();
        }

        private void HandleGameStateChanged(GameManager.GameState previousState, GameManager.GameState newState)
        {
            if (isOpen && newState != GameManager.GameState.Paused && newState != GameManager.GameState.Playing)
                Close();
        }

        private void OnOpenInput(InputAction.CallbackContext context)
        {
            if (Time.frameCount == lastToggleFrame) return;
            lastToggleFrame = Time.frameCount;
            Toggle();
        }

        private void OnEscapeInput(InputAction.CallbackContext context)
        {
            if (isOpen) Close();
        }

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

            FindEquipmentManager();

            isOpen = true;

            if (equipmentCanvas != null)
                equipmentCanvas.enabled = true;

            if (equipmentCanvasGroup != null)
            {
                equipmentCanvasGroup.alpha = 1f;
                equipmentCanvasGroup.interactable = true;
                equipmentCanvasGroup.blocksRaycasts = true;
            }

            RefreshDisplay();

            if (UIManager.Instance != null)
                UIManager.Instance.RegisterOverlayMenu();

            if (pauseGameWhenOpen)
                Time.timeScale = 0f;

            if (UIManager.Instance != null)
                UIManager.Instance.SwitchToUIInput();
            else if (playerInput != null)
                playerInput.SwitchCurrentActionMap("UI");

            OnOpened?.Invoke();
            Debug.Log("[EquipmentMenuController] Equipment menu opened");
        }

        public void Close()
        {
            if (!isOpen && equipmentCanvasGroup != null && equipmentCanvasGroup.alpha == 0f)
                return;

            isOpen = false;

            if (equipmentCanvasGroup != null)
            {
                equipmentCanvasGroup.alpha = 0f;
                equipmentCanvasGroup.interactable = false;
                equipmentCanvasGroup.blocksRaycasts = false;
            }

            if (equipmentCanvas != null)
                equipmentCanvas.enabled = false;

            if (pauseGameWhenOpen)
                Time.timeScale = 1f;

            if (UIManager.Instance != null)
                UIManager.Instance.UnregisterOverlayMenu();

            if (UIManager.Instance != null)
                UIManager.Instance.SwitchToGameplayInput();
            else if (playerInput != null)
                playerInput.SwitchCurrentActionMap("Player");

            OnClosed?.Invoke();
            Debug.Log("[EquipmentMenuController] Equipment menu closed");
        }

        private void RefreshDisplay()
        {
            RefreshSlot(EquipmentSlotType.Weapon, weaponIcon, weaponNameText, weaponStatsText);
            RefreshSlot(EquipmentSlotType.Armor, armorIcon, armorNameText, armorStatsText);
            RefreshSlot(EquipmentSlotType.Boots, bootsIcon, bootsNameText, bootsStatsText);
            RefreshSlot(EquipmentSlotType.Accessory, accessoryIcon, accessoryNameText, accessoryStatsText);
            RefreshCharacterPreview();
        }

        private void RefreshSlot(EquipmentSlotType slot, Image icon, TMP_Text nameText, TMP_Text statsText)
        {
            var item = equipmentManager != null ? equipmentManager.GetEquipped(slot) : null;

            if (item != null)
            {
                if (icon != null)
                {
                    // Use dedicated icon if set, otherwise fall back to the visual part's preview sprite
                    var displaySprite = item.icon;
                    if (displaySprite == null && item.visualPart != null)
                        displaySprite = item.visualPart.previewSprite;

                    icon.sprite = displaySprite;
                    icon.color = displaySprite != null ? Color.white : EmptySlotColor;
                }
                if (nameText != null)
                    nameText.text = item.displayName;
                if (statsText != null)
                {
                    string stats = item.GetStatSummary();
                    statsText.text = !string.IsNullOrEmpty(stats) ? stats : "No bonuses";
                }
            }
            else
            {
                if (icon != null)
                {
                    icon.sprite = null;
                    icon.color = EmptySlotColor;
                }
                if (nameText != null)
                    nameText.text = "Empty";
                if (statsText != null)
                    statsText.text = "";
            }
        }

        private void RefreshCharacterPreview()
        {
            if (characterPreview == null) return;

            // Try to get the player's current appearance
            if (equipmentManager == null) return;

            var playerAppearance = equipmentManager.GetComponent<PlayerAppearance>();
            if (playerAppearance != null && playerAppearance.CurrentConfig != null)
                characterPreview.ApplyConfig(playerAppearance.CurrentConfig);
        }

        #region Runtime UI Builder

        private static readonly Color PanelBg = new Color(0.08f, 0.08f, 0.1f, 0.97f);
        private static readonly Color AgedGold = new Color(0.812f, 0.710f, 0.231f, 1f);
        private static readonly Color BoneWhite = new Color(0.961f, 0.961f, 0.863f, 1f);
        private static readonly Color Charcoal = new Color(0.102f, 0.102f, 0.102f, 0.9f);
        private static readonly Color DividerCol = new Color(0.812f, 0.710f, 0.231f, 0.3f);
        private static readonly Color BtnNormal = new Color(0.2f, 0.2f, 0.25f, 1f);
        private static readonly Color BtnHover = new Color(0.3f, 0.3f, 0.35f, 1f);
        private static readonly Color BtnPress = new Color(0.15f, 0.15f, 0.2f, 1f);
        private static readonly Color SubtleText = new Color(0.7f, 0.65f, 0.55f, 1f);
        private static readonly Color EmptySlotColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

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
        /// Builds the entire equipment menu UI at runtime.
        /// </summary>
        public static EquipmentMenuController CreateRuntimeUI()
        {
            // Canvas
            var canvasGo = new GameObject("EquipmentMenu_Canvas");
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

            // Dark overlay
            var overlayGo = MakeRect("Overlay", canvasGo.transform);
            Stretch(overlayGo);
            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.sprite = WhiteSprite;
            overlayImg.color = new Color(0f, 0f, 0f, 0.6f);

            // Center panel
            var panelGo = MakeRect("Panel", canvasGo.transform);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(700f, 520f);

            var panelImg = panelGo.AddComponent<Image>();
            panelImg.sprite = WhiteSprite;
            panelImg.color = PanelBg;

            // Title row
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
            titleTmp.text = "Equipment";
            titleTmp.fontSize = 28;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = AgedGold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(titleTmp);

            // Close button
            var closeBtnGo = MakeRect("CloseButton", titleRow.transform);
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
            var closeBtnColors = closeBtn.colors;
            closeBtnColors.normalColor = BtnNormal;
            closeBtnColors.highlightedColor = BtnHover;
            closeBtnColors.pressedColor = BtnPress;
            closeBtnColors.selectedColor = BtnHover;
            closeBtnColors.fadeDuration = 0.1f;
            closeBtn.colors = closeBtnColors;

            var closeBtnTextGo = MakeRect("Text", closeBtnGo.transform);
            Stretch(closeBtnTextGo);
            var closeBtnTmp = closeBtnTextGo.AddComponent<TextMeshProUGUI>();
            closeBtnTmp.text = "X";
            closeBtnTmp.fontSize = 20;
            closeBtnTmp.fontStyle = FontStyles.Bold;
            closeBtnTmp.color = BoneWhite;
            closeBtnTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(closeBtnTmp);

            // Divider
            BuildDivider(panelGo.transform, 50f);

            // Body: left = character preview, right = equipment slots
            var bodyGo = MakeRect("Body", panelGo.transform);
            var bodyRect = bodyGo.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0, 0);
            bodyRect.anchorMax = new Vector2(1, 1);
            bodyRect.offsetMin = new Vector2(20, 20);
            bodyRect.offsetMax = new Vector2(-20, -54);

            // Left side: character preview
            var previewContainer = MakeRect("PreviewContainer", bodyGo.transform);
            var previewContainerRect = previewContainer.GetComponent<RectTransform>();
            previewContainerRect.anchorMin = new Vector2(0, 0);
            previewContainerRect.anchorMax = new Vector2(0.35f, 1);
            previewContainerRect.offsetMin = Vector2.zero;
            previewContainerRect.offsetMax = Vector2.zero;

            var previewBg = previewContainer.AddComponent<Image>();
            previewBg.sprite = WhiteSprite;
            previewBg.color = Charcoal;

            // Add layered sprite preview
            var previewGo = MakeRect("CharacterPreview", previewContainer.transform);
            var previewGoRect = previewGo.GetComponent<RectTransform>();
            previewGoRect.anchorMin = new Vector2(0.05f, 0.05f);
            previewGoRect.anchorMax = new Vector2(0.95f, 0.95f);
            previewGoRect.offsetMin = Vector2.zero;
            previewGoRect.offsetMax = Vector2.zero;
            var charPreview = previewGo.AddComponent<UILayeredSpritePreview>();

            // Vertical divider
            var vDivGo = MakeRect("VerticalDivider", bodyGo.transform);
            var vDivRect = vDivGo.GetComponent<RectTransform>();
            vDivRect.anchorMin = new Vector2(0.37f, 0.02f);
            vDivRect.anchorMax = new Vector2(0.37f, 0.98f);
            vDivRect.pivot = new Vector2(0.5f, 0.5f);
            vDivRect.sizeDelta = new Vector2(2, 0);
            var vDivImg = vDivGo.AddComponent<Image>();
            vDivImg.sprite = WhiteSprite;
            vDivImg.color = DividerCol;

            // Right side: equipment slots
            var slotsContainer = MakeRect("SlotsContainer", bodyGo.transform);
            var slotsRect = slotsContainer.GetComponent<RectTransform>();
            slotsRect.anchorMin = new Vector2(0.39f, 0);
            slotsRect.anchorMax = new Vector2(1, 1);
            slotsRect.offsetMin = Vector2.zero;
            slotsRect.offsetMax = Vector2.zero;

            float slotY = 0f;
            float slotHeight = 90f;
            float slotSpacing = 15f;

            var (wIcon, wName, wStats) = BuildEquipmentSlotRow(slotsContainer.transform, "Weapon", slotY);
            slotY -= slotHeight + slotSpacing;
            var (aIcon, aName, aStats) = BuildEquipmentSlotRow(slotsContainer.transform, "Armor", slotY);
            slotY -= slotHeight + slotSpacing;
            var (bIcon, bName, bStats) = BuildEquipmentSlotRow(slotsContainer.transform, "Boots", slotY);
            slotY -= slotHeight + slotSpacing;
            var (accIcon, accName, accStats) = BuildEquipmentSlotRow(slotsContainer.transform, "Accessory", slotY);

            // Wire controller
            var controller = canvasGo.AddComponent<EquipmentMenuController>();
            controller.equipmentCanvas = canvas;
            controller.equipmentCanvasGroup = cg;
            controller.closeButton = closeBtn;
            controller.characterPreview = charPreview;

            controller.weaponIcon = wIcon;
            controller.weaponNameText = wName;
            controller.weaponStatsText = wStats;
            controller.armorIcon = aIcon;
            controller.armorNameText = aName;
            controller.armorStatsText = aStats;
            controller.bootsIcon = bIcon;
            controller.bootsNameText = bName;
            controller.bootsStatsText = bStats;
            controller.accessoryIcon = accIcon;
            controller.accessoryNameText = accName;
            controller.accessoryStatsText = accStats;

            controller.WireButtonListeners();

            Debug.Log("[EquipmentMenuController] Runtime UI created.");
            return controller;
        }

        private static (Image icon, TMP_Text name, TMP_Text stats) BuildEquipmentSlotRow(
            Transform parent, string slotLabel, float yOffset)
        {
            var rowGo = MakeRect(slotLabel + "Row", parent);
            var rowRect = rowGo.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.pivot = new Vector2(0, 1);
            rowRect.anchoredPosition = new Vector2(0, yOffset);
            rowRect.sizeDelta = new Vector2(0, 90);

            var rowBg = rowGo.AddComponent<Image>();
            rowBg.sprite = WhiteSprite;
            rowBg.color = new Color(0.12f, 0.12f, 0.15f, 0.8f);

            // Slot label (top-left corner)
            var labelGo = MakeRect("SlotLabel", rowGo.transform);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(0, 1);
            labelRect.anchoredPosition = new Vector2(8, -4);
            labelRect.sizeDelta = new Vector2(0, 18);
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = slotLabel;
            labelTmp.fontSize = 14;
            labelTmp.color = SubtleText;
            labelTmp.alignment = TextAlignmentOptions.TopLeft;
            FontManager.EnsureFont(labelTmp);

            // Icon (left side)
            var iconGo = MakeRect("Icon", rowGo.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0);
            iconRect.anchorMax = new Vector2(0, 1);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(8, -10);
            iconRect.sizeDelta = new Vector2(56, -28);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = WhiteSprite;
            iconImg.color = EmptySlotColor;
            iconImg.preserveAspect = true;

            // Item name (center)
            var nameGo = MakeRect("ItemName", rowGo.transform);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = new Vector2(72, -6);
            nameRect.offsetMax = new Vector2(-8, -4);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "Empty";
            nameTmp.fontSize = 20;
            nameTmp.color = BoneWhite;
            nameTmp.alignment = TextAlignmentOptions.Left;
            FontManager.EnsureFont(nameTmp);

            // Stats (below name)
            var statsGo = MakeRect("Stats", rowGo.transform);
            var statsRect = statsGo.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 0);
            statsRect.anchorMax = new Vector2(1, 0.5f);
            statsRect.offsetMin = new Vector2(72, 4);
            statsRect.offsetMax = new Vector2(-8, 6);
            var statsTmp = statsGo.AddComponent<TextMeshProUGUI>();
            statsTmp.text = "";
            statsTmp.fontSize = 16;
            statsTmp.color = AgedGold;
            statsTmp.alignment = TextAlignmentOptions.Left;
            FontManager.EnsureFont(statsTmp);

            return (iconImg, nameTmp, statsTmp);
        }

        private static void BuildDivider(Transform parent, float yOffset)
        {
            var divider = MakeRect("Divider", parent);
            var divRect = divider.GetComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0, 1);
            divRect.anchorMax = new Vector2(1, 1);
            divRect.pivot = new Vector2(0.5f, 1);
            divRect.anchoredPosition = new Vector2(0, -yOffset);
            divRect.sizeDelta = new Vector2(-30, 2);

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
    }
}
