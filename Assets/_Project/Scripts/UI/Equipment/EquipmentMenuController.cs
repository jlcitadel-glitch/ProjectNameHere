using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Controller for the equipment menu. Opens with E key, shows equipment slots
    /// with character preview. Clicking a slot opens a selection panel to swap gear.
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
        [SerializeField] private Image legsIcon;
        [SerializeField] private TMP_Text legsNameText;
        [SerializeField] private TMP_Text legsStatsText;
        [SerializeField] private Image feetIcon;
        [SerializeField] private TMP_Text feetNameText;
        [SerializeField] private TMP_Text feetStatsText;
        [SerializeField] private Image accessoryIcon;
        [SerializeField] private TMP_Text accessoryNameText;
        [SerializeField] private TMP_Text accessoryStatsText;
        [SerializeField] private Image headIcon;
        [SerializeField] private TMP_Text headNameText;
        [SerializeField] private TMP_Text headStatsText;
        [SerializeField] private Image handsIcon;
        [SerializeField] private TMP_Text handsNameText;
        [SerializeField] private TMP_Text handsStatsText;

        [Header("Settings")]
        [SerializeField] private bool pauseGameWhenOpen = true;
        [SerializeField] private int canvasSortOrder = 150;

        private bool isOpen;
        private PlayerInput playerInput;
        private int lastToggleFrame = -1;

        private InputAction fallbackOpenAction;
        private InputAction escapeAction;

        private EquipmentManager equipmentManager;

        // Selection panel for swapping gear
        private GameObject selectionPanel;
        private TMP_Text selectionTitleText;
        private Transform selectionListContent;
        private EquipmentSlotType selectedSlotType;
        private readonly List<GameObject> selectionRows = new List<GameObject>();

        public bool IsOpen => isOpen;

        public event System.Action OnOpened;
        public event System.Action OnClosed;

        private static readonly (EquipmentSlotType slot, string label)[] SlotDisplayOrder =
        {
            (EquipmentSlotType.Weapon, "Weapon"),
            (EquipmentSlotType.Head, "Head"),
            (EquipmentSlotType.Armor, "Armor"),
            (EquipmentSlotType.Hands, "Hands"),
            (EquipmentSlotType.Legs, "Legs"),
            (EquipmentSlotType.Feet, "Feet"),
            (EquipmentSlotType.Accessory, "Accessory"),
        };

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
            if (isOpen)
            {
                RefreshDisplay();
                if (selectionPanel != null && selectionPanel.activeSelf && selectedSlotType == slot)
                    PopulateSelectionList(slot);
            }
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
            if (selectionPanel != null && selectionPanel.activeSelf)
            {
                HideSelectionPanel();
                return;
            }
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

            HideSelectionPanel();
            RefreshDisplay();

            if (UIManager.Instance != null)
                UIManager.Instance.RegisterOverlayMenu();

            if (pauseGameWhenOpen && GameManager.Instance != null)
                GameManager.Instance.RequestMenuPause();

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

            HideSelectionPanel();

            if (equipmentCanvasGroup != null)
            {
                equipmentCanvasGroup.alpha = 0f;
                equipmentCanvasGroup.interactable = false;
                equipmentCanvasGroup.blocksRaycasts = false;
            }

            if (equipmentCanvas != null)
                equipmentCanvas.enabled = false;

            if (pauseGameWhenOpen && GameManager.Instance != null)
                GameManager.Instance.ReleaseMenuPause();

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
            RefreshSlot(EquipmentSlotType.Head, headIcon, headNameText, headStatsText);
            RefreshSlot(EquipmentSlotType.Armor, armorIcon, armorNameText, armorStatsText);
            RefreshSlot(EquipmentSlotType.Hands, handsIcon, handsNameText, handsStatsText);
            RefreshSlot(EquipmentSlotType.Legs, legsIcon, legsNameText, legsStatsText);
            RefreshSlot(EquipmentSlotType.Feet, feetIcon, feetNameText, feetStatsText);
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
                    var displaySprite = ResolveEquipmentSprite(item, slot);
                    icon.sprite = displaySprite;
                    icon.enabled = true;
                    icon.color = displaySprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
                    icon.rectTransform.localScale = Vector3.one;

                    if (displaySprite == null)
                        Debug.LogWarning($"[EquipmentMenu] No sprite for {item.displayName} in {slot}: " +
                            $"icon={item.icon != null}, visualPart={item.visualPart != null}, " +
                            $"previewSprite={item.visualPart?.previewSprite != null}");
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
                    icon.enabled = true;
                    icon.color = new Color(1f, 1f, 1f, 0f);
                    icon.rectTransform.localScale = Vector3.one;
                }
                if (nameText != null)
                    nameText.text = "Empty";
                if (statsText != null)
                    statsText.text = "Click to equip";
            }
        }

        /// <summary>
        /// Resolves the best display sprite for a piece of equipment.
        /// Priority: dedicated icon > weapon attack frame > visualPart preview > first frame.
        /// </summary>
        private static Sprite ResolveEquipmentSprite(EquipmentData item, EquipmentSlotType slot)
        {
            if (item.icon != null)
                return item.icon;

            if (item.visualPart == null)
                return null;

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

            if (result != null && slot == EquipmentSlotType.Weapon)
                result = CropToVisiblePixels(result);

            return result;
        }

        /// <summary>
        /// Returns a visible frame from a weapon's BodyPartData.
        /// Weapon idle frames are tiny (just a handle), so we search multiple
        /// animation ranges: thrust (26-33), slash (20-25), spellcast (34-40).
        /// </summary>
        private static Sprite FindBestWeaponFrame(BodyPartData part)
        {
            if (part?.frames == null) return null;
            int[][] ranges = { new[]{26,33}, new[]{20,25}, new[]{34,40} };
            foreach (var range in ranges)
            {
                for (int i = range[1]; i >= range[0] && i < part.frames.Length; i--)
                {
                    if (part.frames[i] != null) return part.frames[i];
                }
            }
            return null;
        }

        private static readonly Dictionary<int, Sprite> _croppedSpriteCache = new Dictionary<int, Sprite>();

        /// <summary>
        /// Creates a tightly-cropped sprite containing only the visible (non-transparent) pixels.
        /// LPC overlay sprites (especially weapons) are tiny pixels on large transparent 64x64 frames.
        /// Cropping lets them fill icon areas properly without needing fragile scale hacks.
        /// </summary>
        private static Sprite CropToVisiblePixels(Sprite source)
        {
            if (source == null) return null;

            int key = source.GetInstanceID();
            if (_croppedSpriteCache.TryGetValue(key, out var cached)) return cached;

            try
            {
                var tex = source.texture;
                if (!tex.isReadable)
                {
                    _croppedSpriteCache[key] = source;
                    return source;
                }

                Rect spriteRect;
                try { spriteRect = source.textureRect; }
                catch { _croppedSpriteCache[key] = source; return source; }

                int sx = (int)spriteRect.x;
                int sy = (int)spriteRect.y;
                int sw = (int)spriteRect.width;
                int sh = (int)spriteRect.height;

                var pixels = tex.GetPixels32(0);
                int texW = tex.width;

                int minX = sw, maxX = 0, minY = sh, maxY = 0;
                for (int y = 0; y < sh; y++)
                {
                    for (int x = 0; x < sw; x++)
                    {
                        if (pixels[(sy + y) * texW + (sx + x)].a > 10)
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }

                if (maxX < minX)
                {
                    _croppedSpriteCache[key] = source;
                    return source;
                }

                float coverage = (float)(maxX - minX + 1) * (maxY - minY + 1) / (sw * sh);
                if (coverage > 0.6f)
                {
                    _croppedSpriteCache[key] = source;
                    return source;
                }

                int pad = 1;
                minX = Mathf.Max(0, minX - pad);
                minY = Mathf.Max(0, minY - pad);
                maxX = Mathf.Min(sw - 1, maxX + pad);
                maxY = Mathf.Min(sh - 1, maxY + pad);

                var tightRect = new Rect(sx + minX, sy + minY, maxX - minX + 1, maxY - minY + 1);
                var cropped = Sprite.Create(tex, tightRect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
                cropped.name = source.name + "_cropped";

                _croppedSpriteCache[key] = cropped;
                return cropped;
            }
            catch (System.Exception)
            {
                _croppedSpriteCache[key] = source;
                return source;
            }
        }

        private void RefreshCharacterPreview()
        {
            if (characterPreview == null) return;

            if (equipmentManager == null) return;

            var playerAppearance = equipmentManager.GetComponent<PlayerAppearance>();
            if (playerAppearance != null && playerAppearance.CurrentConfig != null)
                characterPreview.ApplyConfig(playerAppearance.CurrentConfig);
        }

        private void OnSlotRightClicked(EquipmentSlotType slot)
        {
            FindEquipmentManager();
            if (equipmentManager == null) return;

            var item = equipmentManager.GetEquipped(slot);
            if (item != null)
            {
                equipmentManager.Unequip(slot);
                HideSelectionPanel();
            }
        }

        #region Selection Panel

        private void OnSlotClicked(EquipmentSlotType slot)
        {
            FindEquipmentManager();
            selectedSlotType = slot;
            ShowSelectionPanel(slot);
        }

        private void ShowSelectionPanel(EquipmentSlotType slot)
        {
            if (selectionPanel == null) return;

            selectionPanel.SetActive(true);

            if (selectionTitleText != null)
                selectionTitleText.text = $"Select {slot}";

            PopulateSelectionList(slot);
        }

        private void HideSelectionPanel()
        {
            if (selectionPanel != null)
                selectionPanel.SetActive(false);
        }

        private void PopulateSelectionList(EquipmentSlotType slot)
        {
            foreach (var row in selectionRows)
            {
                if (row != null) Destroy(row);
            }
            selectionRows.Clear();

            if (selectionListContent == null) return;

            var currentItem = equipmentManager != null ? equipmentManager.GetEquipped(slot) : null;

            var currentJob = SkillManager.Instance?.CurrentJob;
            var starterGear = currentJob?.starterEquipment;

            List<EquipmentData> matchingItems;
            if (starterGear != null && starterGear.Length > 0)
            {
                matchingItems = starterGear
                    .Where(e => e != null && e.slotType == slot)
                    .ToList();
            }
            else
            {
                var allEquipment = Resources.LoadAll<EquipmentData>("Equipment");
                matchingItems = allEquipment.Where(e => e.slotType == slot).ToList();
            }

            float yPos = 0f;
            float rowHeight = 70f;
            float spacing = 6f;

            if (currentItem != null)
            {
                var unequipRow = BuildSelectionRow(selectionListContent, "-- Unequip --", "",
                    null, yPos, false);
                var unequipBtn = unequipRow.GetComponent<Button>();
                unequipBtn.onClick.AddListener(() =>
                {
                    equipmentManager.Unequip(slot);
                    HideSelectionPanel();
                });
                selectionRows.Add(unequipRow);
                yPos -= rowHeight + spacing;
            }

            foreach (var item in matchingItems)
            {
                bool isCurrentlyEquipped = currentItem != null && currentItem.equipmentId == item.equipmentId;
                var sprite = ResolveEquipmentSprite(item, slot);
                string stats = item.GetStatSummary();
                string label = isCurrentlyEquipped ? $"{item.displayName} (Equipped)" : item.displayName;

                var row = BuildSelectionRow(selectionListContent, label, stats, sprite, yPos, isCurrentlyEquipped);

                if (!isCurrentlyEquipped)
                {
                    var capturedItem = item;
                    var btn = row.GetComponent<Button>();
                    btn.onClick.AddListener(() =>
                    {
                        equipmentManager.Equip(capturedItem);
                        HideSelectionPanel();
                    });
                }

                selectionRows.Add(row);
                yPos -= rowHeight + spacing;
            }

            if (matchingItems.Count == 0 && currentItem == null)
            {
                var emptyRow = BuildSelectionRow(selectionListContent, "No equipment available", "",
                    null, yPos, false);
                var emptyBtn = emptyRow.GetComponent<Button>();
                emptyBtn.interactable = false;
                selectionRows.Add(emptyRow);
                yPos -= rowHeight + spacing;
            }

            var contentRect = selectionListContent.GetComponent<RectTransform>();
            if (contentRect != null)
                contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, Mathf.Abs(yPos));
        }

        #endregion

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
        private static readonly Color SelectedRowBg = new Color(0.25f, 0.22f, 0.12f, 0.9f);
        private static readonly Color SelectionRowBg = new Color(0.14f, 0.14f, 0.17f, 0.9f);

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

            // Center panel (VLG + ContentSizeFitter)
            var panelGo = MakeRect("Panel", canvasGo.transform);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1000f, 0f);

            var panelImg = panelGo.AddComponent<Image>();
            panelImg.sprite = WhiteSprite;
            panelImg.color = PanelBg;

            var panelVLG = panelGo.AddComponent<VerticalLayoutGroup>();
            panelVLG.childAlignment = TextAnchor.UpperCenter;
            panelVLG.childControlWidth = true;
            panelVLG.childControlHeight = true;
            panelVLG.childForceExpandWidth = true;
            panelVLG.childForceExpandHeight = false;
            panelVLG.padding = new RectOffset(0, 0, 0, 0);
            panelVLG.spacing = 0;

            var panelCSF = panelGo.AddComponent<ContentSizeFitter>();
            panelCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            panelCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddLayout(panelGo, minH: 580);

            // Title row
            var titleRow = MakeRect("TitleRow", panelGo.transform);
            AddLayout(titleRow, prefH: 50);

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
            BuildLayoutDivider(panelGo.transform, true);

            // Body (HLG)
            var bodyGo = MakeRect("Body", panelGo.transform);
            AddLayout(bodyGo, flexH: 1, minH: 510);

            var bodyHLG = bodyGo.AddComponent<HorizontalLayoutGroup>();
            bodyHLG.childAlignment = TextAnchor.UpperLeft;
            bodyHLG.childControlWidth = true;
            bodyHLG.childControlHeight = true;
            bodyHLG.childForceExpandWidth = false;
            bodyHLG.childForceExpandHeight = true;
            bodyHLG.padding = new RectOffset(20, 20, 10, 20);
            bodyHLG.spacing = 0;

            // Left: character preview
            var previewCol = MakeRect("PreviewCol", bodyGo.transform);
            AddLayout(previewCol, flexW: 0.22f);

            var previewBg = previewCol.AddComponent<Image>();
            previewBg.sprite = WhiteSprite;
            previewBg.color = Charcoal;

            var previewGo = MakeRect("CharacterPreview", previewCol.transform);
            var previewGoRect = previewGo.GetComponent<RectTransform>();
            previewGoRect.anchorMin = new Vector2(0.05f, 0.05f);
            previewGoRect.anchorMax = new Vector2(0.95f, 0.95f);
            previewGoRect.offsetMin = Vector2.zero;
            previewGoRect.offsetMax = Vector2.zero;
            var charPreview = previewGo.AddComponent<UILayeredSpritePreview>();

            // Vertical divider (preview | slots)
            BuildLayoutDivider(bodyGo.transform, false);

            // Center: equipment slots (VLG)
            var slotsCol = MakeRect("SlotsCol", bodyGo.transform);
            AddLayout(slotsCol, flexW: 0.34f);

            var slotsVLG = slotsCol.AddComponent<VerticalLayoutGroup>();
            slotsVLG.childAlignment = TextAnchor.UpperCenter;
            slotsVLG.childControlWidth = true;
            slotsVLG.childControlHeight = false;
            slotsVLG.childForceExpandWidth = true;
            slotsVLG.childForceExpandHeight = false;
            slotsVLG.padding = new RectOffset(0, 0, 0, 0);
            slotsVLG.spacing = 8;

            // Build slot rows from data-driven array
            var slotButtons = new Button[SlotDisplayOrder.Length];
            var slotIcons = new Image[SlotDisplayOrder.Length];
            var slotNames = new TMP_Text[SlotDisplayOrder.Length];
            var slotStats = new TMP_Text[SlotDisplayOrder.Length];

            for (int i = 0; i < SlotDisplayOrder.Length; i++)
            {
                var (rowBtn, icon, nameT, statsT) = BuildEquipmentSlotRow(slotsCol.transform, SlotDisplayOrder[i].label);
                AddLayout(rowBtn.gameObject, prefH: SLOT_ROW_HEIGHT);
                slotButtons[i] = rowBtn;
                slotIcons[i] = icon;
                slotNames[i] = nameT;
                slotStats[i] = statsT;
            }

            // Vertical divider (slots | selection)
            BuildLayoutDivider(bodyGo.transform, false);

            // Right: selection panel
            var selPanel = BuildSelectionPanel(bodyGo.transform);

            // Wire controller
            var controller = canvasGo.AddComponent<EquipmentMenuController>();
            controller.equipmentCanvas = canvas;
            controller.equipmentCanvasGroup = cg;
            controller.closeButton = closeBtn;
            controller.characterPreview = charPreview;

            // Wire slot fields by matching SlotDisplayOrder
            for (int i = 0; i < SlotDisplayOrder.Length; i++)
            {
                switch (SlotDisplayOrder[i].slot)
                {
                    case EquipmentSlotType.Weapon:
                        controller.weaponIcon = slotIcons[i];
                        controller.weaponNameText = slotNames[i];
                        controller.weaponStatsText = slotStats[i];
                        break;
                    case EquipmentSlotType.Head:
                        controller.headIcon = slotIcons[i];
                        controller.headNameText = slotNames[i];
                        controller.headStatsText = slotStats[i];
                        break;
                    case EquipmentSlotType.Armor:
                        controller.armorIcon = slotIcons[i];
                        controller.armorNameText = slotNames[i];
                        controller.armorStatsText = slotStats[i];
                        break;
                    case EquipmentSlotType.Hands:
                        controller.handsIcon = slotIcons[i];
                        controller.handsNameText = slotNames[i];
                        controller.handsStatsText = slotStats[i];
                        break;
                    case EquipmentSlotType.Legs:
                        controller.legsIcon = slotIcons[i];
                        controller.legsNameText = slotNames[i];
                        controller.legsStatsText = slotStats[i];
                        break;
                    case EquipmentSlotType.Feet:
                        controller.feetIcon = slotIcons[i];
                        controller.feetNameText = slotNames[i];
                        controller.feetStatsText = slotStats[i];
                        break;
                    case EquipmentSlotType.Accessory:
                        controller.accessoryIcon = slotIcons[i];
                        controller.accessoryNameText = slotNames[i];
                        controller.accessoryStatsText = slotStats[i];
                        break;
                }
            }

            controller.selectionPanel = selPanel.panel;
            controller.selectionTitleText = selPanel.title;
            controller.selectionListContent = selPanel.content;

            // Wire slot click and right-click handlers via loop
            for (int i = 0; i < SlotDisplayOrder.Length; i++)
            {
                var slotType = SlotDisplayOrder[i].slot;
                slotButtons[i].onClick.AddListener(() => controller.OnSlotClicked(slotType));
                WireRightClick(slotButtons[i].gameObject, () => controller.OnSlotRightClicked(slotType));
            }

            controller.WireButtonListeners();

            Debug.Log("[EquipmentMenuController] Runtime UI created.");
            return controller;
        }

        private const float SLOT_ROW_HEIGHT = 58f;

        private static (Button rowBtn, Image icon, TMP_Text name, TMP_Text stats) BuildEquipmentSlotRow(
            Transform parent, string slotLabel)
        {
            var rowGo = MakeRect(slotLabel + "Row", parent);

            var rowBg = rowGo.AddComponent<Image>();
            rowBg.sprite = WhiteSprite;
            rowBg.color = new Color(0.12f, 0.12f, 0.15f, 0.8f);

            var rowBtn = rowGo.AddComponent<Button>();
            var btnColors = rowBtn.colors;
            btnColors.normalColor = new Color(0.12f, 0.12f, 0.15f, 0.8f);
            btnColors.highlightedColor = new Color(0.18f, 0.18f, 0.22f, 0.9f);
            btnColors.pressedColor = new Color(0.25f, 0.22f, 0.12f, 0.9f);
            btnColors.selectedColor = new Color(0.18f, 0.18f, 0.22f, 0.9f);
            btnColors.fadeDuration = 0.1f;
            rowBtn.colors = btnColors;
            rowBtn.targetGraphic = rowBg;

            // Icon container (left side, with RectMask2D to clip scaled weapon sprites)
            var iconContainerGo = MakeRect("IconContainer", rowGo.transform);
            var iconContainerRect = iconContainerGo.GetComponent<RectTransform>();
            iconContainerRect.anchorMin = new Vector2(0, 0);
            iconContainerRect.anchorMax = new Vector2(0, 1);
            iconContainerRect.pivot = new Vector2(0, 0.5f);
            iconContainerRect.anchoredPosition = new Vector2(6, -2);
            iconContainerRect.sizeDelta = new Vector2(46, -20);
            var iconContainerBg = iconContainerGo.AddComponent<Image>();
            iconContainerBg.sprite = WhiteSprite;
            iconContainerBg.color = new Color(0.06f, 0.06f, 0.08f, 0.8f);
            iconContainerBg.raycastTarget = false;
            iconContainerGo.AddComponent<RectMask2D>();

            // Icon
            var iconGo = MakeRect("Icon", iconContainerGo.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0);
            iconRect.anchorMax = new Vector2(1, 1);
            iconRect.offsetMin = new Vector2(2, 2);
            iconRect.offsetMax = new Vector2(-2, -2);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = null;
            iconImg.color = new Color(1f, 1f, 1f, 0f);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.type = Image.Type.Simple;

            // Slot label
            var labelGo = MakeRect("SlotLabel", rowGo.transform);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(0, 1);
            labelRect.anchoredPosition = new Vector2(58, -2);
            labelRect.sizeDelta = new Vector2(-66, 16);
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = slotLabel;
            labelTmp.fontSize = 11;
            labelTmp.color = SubtleText;
            labelTmp.alignment = TextAlignmentOptions.TopLeft;
            labelTmp.raycastTarget = false;
            FontManager.EnsureFont(labelTmp);

            // Item name
            var nameGo = MakeRect("ItemName", rowGo.transform);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.28f);
            nameRect.anchorMax = new Vector2(1, 0.72f);
            nameRect.offsetMin = new Vector2(58, 0);
            nameRect.offsetMax = new Vector2(-8, 0);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "Empty";
            nameTmp.fontSize = 15;
            nameTmp.color = BoneWhite;
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.raycastTarget = false;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            nameTmp.enableAutoSizing = true;
            nameTmp.fontSizeMin = 11f;
            nameTmp.fontSizeMax = 15f;
            FontManager.EnsureFont(nameTmp);

            // Stats
            var statsGo = MakeRect("Stats", rowGo.transform);
            var statsRect = statsGo.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 0);
            statsRect.anchorMax = new Vector2(1, 0.28f);
            statsRect.offsetMin = new Vector2(58, 2);
            statsRect.offsetMax = new Vector2(-8, 0);
            var statsTmp = statsGo.AddComponent<TextMeshProUGUI>();
            statsTmp.text = "";
            statsTmp.fontSize = 14;
            statsTmp.color = AgedGold;
            statsTmp.alignment = TextAlignmentOptions.Left;
            statsTmp.raycastTarget = false;
            statsTmp.overflowMode = TextOverflowModes.Ellipsis;
            statsTmp.textWrappingMode = TextWrappingModes.NoWrap;
            statsTmp.enableAutoSizing = true;
            statsTmp.fontSizeMin = 10f;
            statsTmp.fontSizeMax = 14f;
            FontManager.EnsureFont(statsTmp);

            return (rowBtn, iconImg, nameTmp, statsTmp);
        }

        private static (GameObject panel, TMP_Text title, Transform content) BuildSelectionPanel(Transform parent)
        {
            var panelGo = MakeRect("SelectionPanel", parent);
            AddLayout(panelGo, flexW: 0.40f);

            var panelBg = panelGo.AddComponent<Image>();
            panelBg.sprite = WhiteSprite;
            panelBg.color = new Color(0.06f, 0.06f, 0.08f, 0.95f);

            // Title
            var titleGo = MakeRect("SelectionTitle", panelGo.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(0, 36);

            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "Select Equipment";
            titleTmp.fontSize = 20;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = AgedGold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(titleTmp);

            // Divider below title
            var divGo = MakeRect("SelDivider", panelGo.transform);
            var divRect = divGo.GetComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0.05f, 1);
            divRect.anchorMax = new Vector2(0.95f, 1);
            divRect.pivot = new Vector2(0.5f, 1);
            divRect.anchoredPosition = new Vector2(0, -36);
            divRect.sizeDelta = new Vector2(0, 1);
            var divImg = divGo.AddComponent<Image>();
            divImg.sprite = WhiteSprite;
            divImg.color = DividerCol;

            // Scroll view for equipment list
            var scrollGo = MakeRect("ScrollView", panelGo.transform);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(4, 4);
            scrollRect.offsetMax = new Vector2(-4, -40);

            var scrollView = scrollGo.AddComponent<ScrollRect>();
            scrollView.horizontal = false;
            scrollView.vertical = true;
            scrollView.movementType = ScrollRect.MovementType.Clamped;
            scrollView.scrollSensitivity = 30f;

            var maskImg = scrollGo.AddComponent<Image>();
            maskImg.sprite = WhiteSprite;
            maskImg.color = new Color(0, 0, 0, 0.01f);
            scrollGo.AddComponent<Mask>().showMaskGraphic = false;

            // Content container
            var contentGo = MakeRect("Content", scrollGo.transform);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            scrollView.content = contentRect;

            panelGo.SetActive(false);

            return (panelGo, titleTmp, contentGo.transform);
        }

        private static GameObject BuildSelectionRow(Transform parent, string itemName, string stats,
            Sprite itemSprite, float yPos, bool isSelected)
        {
            var rowGo = MakeRect("SelRow", parent);
            var rowRect = rowGo.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.pivot = new Vector2(0, 1);
            rowRect.anchoredPosition = new Vector2(0, yPos);
            rowRect.sizeDelta = new Vector2(0, 70);

            var rowBg = rowGo.AddComponent<Image>();
            rowBg.sprite = WhiteSprite;
            rowBg.color = isSelected ? SelectedRowBg : SelectionRowBg;

            var rowBtn = rowGo.AddComponent<Button>();
            var btnColors = rowBtn.colors;
            btnColors.normalColor = isSelected ? SelectedRowBg : SelectionRowBg;
            btnColors.highlightedColor = new Color(0.22f, 0.20f, 0.14f, 0.95f);
            btnColors.pressedColor = new Color(0.30f, 0.26f, 0.14f, 1f);
            btnColors.selectedColor = btnColors.highlightedColor;
            btnColors.disabledColor = new Color(0.10f, 0.10f, 0.12f, 0.6f);
            btnColors.fadeDuration = 0.1f;
            rowBtn.colors = btnColors;
            rowBtn.targetGraphic = rowBg;

            // Icon (with RectMask2D container for weapon scaling)
            if (itemSprite != null)
            {
                var iconContainerGo = MakeRect("SelIconContainer", rowGo.transform);
                var iconContainerRect = iconContainerGo.GetComponent<RectTransform>();
                iconContainerRect.anchorMin = new Vector2(0, 0.1f);
                iconContainerRect.anchorMax = new Vector2(0, 0.9f);
                iconContainerRect.pivot = new Vector2(0, 0.5f);
                iconContainerRect.anchoredPosition = new Vector2(6, 0);
                iconContainerRect.sizeDelta = new Vector2(50, 0);
                iconContainerGo.AddComponent<RectMask2D>();

                var iconGo = MakeRect("SelIcon", iconContainerGo.transform);
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;

                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = itemSprite;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                iconImg.type = Image.Type.Simple;
                iconImg.enabled = true;
            }

            float textLeft = itemSprite != null ? 62f : 8f;

            // Name
            var nameGo = MakeRect("SelName", rowGo.transform);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = new Vector2(textLeft, 0);
            nameRect.offsetMax = new Vector2(-4, -4);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = itemName;
            nameTmp.fontSize = 16;
            nameTmp.color = isSelected ? AgedGold : BoneWhite;
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.raycastTarget = false;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            nameTmp.enableAutoSizing = true;
            nameTmp.fontSizeMin = 11f;
            nameTmp.fontSizeMax = 16f;
            FontManager.EnsureFont(nameTmp);

            // Stats
            if (!string.IsNullOrEmpty(stats))
            {
                var statsGo = MakeRect("SelStats", rowGo.transform);
                var statsRect = statsGo.GetComponent<RectTransform>();
                statsRect.anchorMin = new Vector2(0, 0);
                statsRect.anchorMax = new Vector2(1, 0.5f);
                statsRect.offsetMin = new Vector2(textLeft, 4);
                statsRect.offsetMax = new Vector2(-4, 0);
                var statsTmp = statsGo.AddComponent<TextMeshProUGUI>();
                statsTmp.text = stats;
                statsTmp.fontSize = 13;
                statsTmp.color = SubtleText;
                statsTmp.alignment = TextAlignmentOptions.Left;
                statsTmp.raycastTarget = false;
                statsTmp.overflowMode = TextOverflowModes.Ellipsis;
                statsTmp.textWrappingMode = TextWrappingModes.NoWrap;
                statsTmp.enableAutoSizing = true;
                statsTmp.fontSizeMin = 10f;
                statsTmp.fontSizeMax = 13f;
                FontManager.EnsureFont(statsTmp);
            }

            return rowGo;
        }

        private static void WireRightClick(GameObject go, System.Action callback)
        {
            var handler = go.AddComponent<SlotRightClickHandler>();
            handler.onRightClick = callback;
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

        private static void BuildLayoutDivider(Transform parent, bool horizontal)
        {
            var go = new GameObject("Divider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = DividerCol;
            img.raycastTarget = false;
            var le = go.AddComponent<LayoutElement>();
            if (horizontal) { le.preferredHeight = 2; le.flexibleWidth = 1; }
            else { le.preferredWidth = 2; le.flexibleHeight = 1; }
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

        /// <summary>
        /// Helper component that detects right-clicks on a UI element.
        /// Used on equipment slot rows to allow right-click unequip.
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
    }
}
