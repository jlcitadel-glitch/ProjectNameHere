using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Controller for opening/closing the stat allocation menu.
    /// Handles input binding (S key) and integration with game state.
    /// Can build its own UI at runtime via CreateRuntimeUI().
    /// </summary>
    public class StatMenuController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas statMenuCanvas;
        [SerializeField] private CanvasGroup statMenuCanvasGroup;
        [SerializeField] private Button closeButton;

        [Header("Player Info Display")]
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text classText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text mpText;
        [SerializeField] private Image playerImage;
        [SerializeField] private UILayeredSpritePreview playerLayeredPreview;

        [Header("Stat Display")]
        [SerializeField] private TMP_Text availablePointsText;
        [SerializeField] private TMP_Text strengthText;
        [SerializeField] private TMP_Text intelligenceText;
        [SerializeField] private TMP_Text agilityText;

        [Header("Derived Stats")]
        [SerializeField] private TMP_Text bonusHPText;
        [SerializeField] private TMP_Text meleeDamageText;
        [SerializeField] private TMP_Text bonusManaText;
        [SerializeField] private TMP_Text skillDamageText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text critChanceText;

        [Header("Allocate Buttons")]
        [SerializeField] private Button allocateStrButton;
        [SerializeField] private Button allocateIntButton;
        [SerializeField] private Button allocateAgiButton;

        [Header("Input")]
        [SerializeField] private InputActionReference openStatMenuAction;

        [Header("Settings")]
        [SerializeField] private bool pauseGameWhenOpen = true;
        [SerializeField] private int canvasSortOrder = 150;

        private bool isOpen;
        private PlayerInput playerInput;
        private int lastToggleFrame = -1;

        // Fallback input actions created at runtime
        private InputAction fallbackOpenAction;
        private InputAction escapeAction;

        private StatSystem statSystem;
        private HealthSystem healthSystem;
        private ManaSystem manaSystem;
        private LevelSystem levelSystem;
        private SpriteRenderer playerSpriteRenderer;
        private PlayerAppearance playerAppearance;

        public bool IsOpen => isOpen;

        public event System.Action OnOpened;
        public event System.Action OnClosed;

        private void Awake()
        {
            // Auto-find references
            if (statMenuCanvas == null)
            {
                statMenuCanvas = GetComponent<Canvas>();
            }

            if (statMenuCanvasGroup == null && statMenuCanvas != null)
            {
                statMenuCanvasGroup = statMenuCanvas.GetComponent<CanvasGroup>();
                if (statMenuCanvasGroup == null)
                {
                    statMenuCanvasGroup = statMenuCanvas.gameObject.AddComponent<CanvasGroup>();
                }
            }

            // Configure canvas
            if (statMenuCanvas != null)
            {
                statMenuCanvas.sortingOrder = canvasSortOrder;
            }

            // Find player input
            playerInput = FindAnyObjectByType<PlayerInput>();

            // Create fallback input actions
            fallbackOpenAction = new InputAction("OpenStatMenu", InputActionType.Button, "<Keyboard>/s");
            escapeAction = new InputAction("CloseStatMenu", InputActionType.Button, "<Keyboard>/escape");

            // Wire buttons (works for scene-placed UI; runtime UI calls WireButtonListeners() after field assignment)
            WireButtonListeners();

            // Start closed
            Close();

            // Disable canvas rendering to prevent overlay flash on load
            if (statMenuCanvas != null)
                statMenuCanvas.enabled = false;
        }

        private void OnEnable()
        {
            if (openStatMenuAction?.action != null)
            {
                openStatMenuAction.action.Enable();
                openStatMenuAction.action.performed += OnOpenStatMenuInput;
            }

            // Always enable fallback S key
            if (fallbackOpenAction != null)
            {
                fallbackOpenAction.Enable();
                fallbackOpenAction.performed += OnOpenStatMenuInput;
            }

            // Always enable escape to close
            if (escapeAction != null)
            {
                escapeAction.Enable();
                escapeAction.performed += OnEscapeInput;
            }

            // Subscribe to game state changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }

            // Find stat system on the player
            FindPlayerSystems();
        }

        private void OnDisable()
        {
            if (openStatMenuAction?.action != null)
            {
                openStatMenuAction.action.performed -= OnOpenStatMenuInput;
            }

            if (fallbackOpenAction != null)
            {
                fallbackOpenAction.performed -= OnOpenStatMenuInput;
                fallbackOpenAction.Disable();
            }

            if (escapeAction != null)
            {
                escapeAction.performed -= OnEscapeInput;
                escapeAction.Disable();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }

            UnsubscribeFromPlayerSystems();
        }

        private void OnDestroy()
        {
            fallbackOpenAction?.Dispose();
            escapeAction?.Dispose();
        }

        /// <summary>
        /// Wires onClick listeners to all buttons. Safe to call multiple times
        /// (buttons are only wired if non-null and not already connected).
        /// Called from Awake() for scene-placed UI, and from CreateRuntimeUI() for runtime UI.
        /// </summary>
        private void WireButtonListeners()
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

        private void FindPlayerSystems()
        {
            if (statSystem != null) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            statSystem = player.GetComponent<StatSystem>();
            if (statSystem != null)
            {
                statSystem.OnStatsChanged += RefreshDisplay;
                statSystem.OnStatPointsChanged += HandleStatPointsChanged;
            }

            healthSystem = player.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += HandleHealthChanged;
            }

            manaSystem = player.GetComponent<ManaSystem>();
            if (manaSystem != null)
            {
                manaSystem.OnManaChanged += HandleManaChanged;
            }

            levelSystem = player.GetComponent<LevelSystem>();
            if (levelSystem != null)
            {
                levelSystem.OnLevelUp += HandleLevelUp;
            }

            playerSpriteRenderer = player.GetComponent<SpriteRenderer>();
            playerAppearance = player.GetComponent<PlayerAppearance>();
        }

        private void UnsubscribeFromPlayerSystems()
        {
            if (statSystem != null)
            {
                statSystem.OnStatsChanged -= RefreshDisplay;
                statSystem.OnStatPointsChanged -= HandleStatPointsChanged;
            }

            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged -= HandleHealthChanged;
            }

            if (manaSystem != null)
            {
                manaSystem.OnManaChanged -= HandleManaChanged;
            }

            if (levelSystem != null)
            {
                levelSystem.OnLevelUp -= HandleLevelUp;
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (isOpen) RefreshPlayerInfo();
        }

        private void HandleManaChanged(float current, float max)
        {
            if (isOpen) RefreshPlayerInfo();
        }

        private void HandleLevelUp(int newLevel)
        {
            if (isOpen) RefreshPlayerInfo();
        }

        private void OnOpenStatMenuInput(InputAction.CallbackContext context)
        {
            // Prevent double-toggle when both InputActionReference and fallback fire on the same frame
            if (Time.frameCount == lastToggleFrame) return;
            lastToggleFrame = Time.frameCount;
            Toggle();
        }

        private void OnEscapeInput(InputAction.CallbackContext context)
        {
            if (isOpen)
            {
                Close();
            }
        }

        private void HandleGameStateChanged(GameManager.GameState previousState, GameManager.GameState newState)
        {
            if (isOpen && newState != GameManager.GameState.Paused && newState != GameManager.GameState.Playing)
            {
                Close();
            }
        }

        private void HandleStatPointsChanged(int points)
        {
            RefreshDisplay();
        }

        public void Toggle()
        {
            if (isOpen)
                Close();
            else
                Open();
        }

        public void Open()
        {
            if (isOpen) return;

            // Don't open during certain game states
            if (GameManager.Instance != null)
            {
                var state = GameManager.Instance.CurrentState;
                if (state == GameManager.GameState.MainMenu ||
                    state == GameManager.GameState.Loading ||
                    state == GameManager.GameState.GameOver ||
                    state == GameManager.GameState.Cutscene)
                {
                    return;
                }
            }

            // Re-acquire systems if needed (player may have spawned after us)
            FindPlayerSystems();

            isOpen = true;

            // Enable canvas rendering
            if (statMenuCanvas != null)
                statMenuCanvas.enabled = true;

            if (statMenuCanvasGroup != null)
            {
                statMenuCanvasGroup.alpha = 1f;
                statMenuCanvasGroup.interactable = true;
                statMenuCanvasGroup.blocksRaycasts = true;
            }

            RefreshDisplay();

            // Register with UIManager so Escape closes this menu, not pause
            if (UIManager.Instance != null)
            {
                UIManager.Instance.RegisterOverlayMenu();
            }

            if (pauseGameWhenOpen && GameManager.Instance != null)
            {
                GameManager.Instance.RequestMenuPause();
            }

            // Switch to UI input
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SwitchToUIInput();
            }
            else if (playerInput != null)
            {
                playerInput.SwitchCurrentActionMap("UI");
            }

            OnOpened?.Invoke();

            Debug.Log("[StatMenuController] Stat menu opened");
        }

        public void Close()
        {
            if (!isOpen && statMenuCanvasGroup != null && statMenuCanvasGroup.alpha == 0f)
                return;

            isOpen = false;

            if (statMenuCanvasGroup != null)
            {
                statMenuCanvasGroup.alpha = 0f;
                statMenuCanvasGroup.interactable = false;
                statMenuCanvasGroup.blocksRaycasts = false;
            }

            // Disable canvas rendering when closed
            if (statMenuCanvas != null)
                statMenuCanvas.enabled = false;

            if (pauseGameWhenOpen && GameManager.Instance != null)
            {
                GameManager.Instance.ReleaseMenuPause();
            }

            // Unregister overlay before switching input so UIManager knows we're done
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UnregisterOverlayMenu();
            }

            // Switch back to gameplay input
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SwitchToGameplayInput();
            }
            else if (playerInput != null)
            {
                playerInput.SwitchCurrentActionMap("Player");
            }

            OnClosed?.Invoke();

            Debug.Log("[StatMenuController] Stat menu closed");
        }

        private void AllocateStat(string statName)
        {
            if (statSystem == null) return;

            var kb = UnityEngine.InputSystem.Keyboard.current;
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
                RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (statSystem == null) return;

            // Available points
            if (availablePointsText != null)
                availablePointsText.text = $"Available Points: {statSystem.AvailableStatPoints}";

            // Core stats — show base stat with [total] in brackets when equipment modifies it
            if (strengthText != null)
                strengthText.text = FormatCoreStat("Strength", statSystem.BaseStrength, statSystem.Strength);
            if (intelligenceText != null)
                intelligenceText.text = FormatCoreStat("Intelligence", statSystem.BaseIntelligence, statSystem.Intelligence);
            if (agilityText != null)
                agilityText.text = FormatCoreStat("Agility", statSystem.BaseAgility, statSystem.Agility);

            // Derived stats
            if (bonusHPText != null)
                bonusHPText.text = $"Bonus HP: +{statSystem.BonusMaxHP:F0}";
            if (meleeDamageText != null)
                meleeDamageText.text = $"Melee Damage: x{statSystem.MeleeDamageMultiplier:F2}";
            if (bonusManaText != null)
                bonusManaText.text = $"Bonus Mana: +{statSystem.BonusMaxMana:F0}";
            if (skillDamageText != null)
                skillDamageText.text = $"Skill Damage: x{statSystem.SkillDamageMultiplier:F2}";
            if (speedText != null)
                speedText.text = $"Attack Speed: x{(1f / statSystem.AttackSpeedMultiplier):F2}";
            if (critChanceText != null)
                critChanceText.text = $"Critical: {statSystem.CritChance * 100f:F1}% (x{statSystem.CritDamageMultiplier:F2})";

            // Enable/disable allocate buttons based on available points
            bool hasPoints = statSystem.AvailableStatPoints > 0;
            if (allocateStrButton != null) allocateStrButton.interactable = hasPoints;
            if (allocateIntButton != null) allocateIntButton.interactable = hasPoints;
            if (allocateAgiButton != null) allocateAgiButton.interactable = hasPoints;

            // Player info
            RefreshPlayerInfo();
        }

        /// <summary>
        /// Formats a core stat line. Shows "Name: base [total]" when equipment modifies
        /// the stat, or just "Name: value" when base equals total.
        /// </summary>
        private static string FormatCoreStat(string name, int baseStat, int total)
        {
            if (total != baseStat)
                return $"{name}: {baseStat} [{total}]";
            return $"{name}: {baseStat}";
        }

        private void RefreshPlayerInfo()
        {
            if (hpText != null && healthSystem != null)
                hpText.text = $"HP: {healthSystem.CurrentHealth:F0}/{healthSystem.MaxHealth:F0}";

            if (mpText != null && manaSystem != null)
                mpText.text = $"MP: {manaSystem.CurrentMana:F0}/{manaSystem.MaxMana:F0}";

            if (levelText != null && levelSystem != null)
                levelText.text = $"Level: {levelSystem.CurrentLevel}";

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

            // Prefer layered preview; fall back to static sprite
            if (playerLayeredPreview != null && playerAppearance != null && playerAppearance.CurrentConfig != null)
            {
                playerLayeredPreview.ApplyConfig(playerAppearance.CurrentConfig);
                if (playerImage != null)
                    playerImage.enabled = false;
            }
            else if (playerImage != null && playerSpriteRenderer != null && playerSpriteRenderer.sprite != null)
            {
                playerImage.sprite = playerSpriteRenderer.sprite;
            }
        }

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
        private static readonly Color DeepCrimson = new Color(0.545f, 0f, 0f, 1f);
        private static readonly Color DarkBlue = new Color(0.1f, 0.2f, 0.6f, 1f);
        private static readonly Color SubtleText = new Color(0.7f, 0.65f, 0.55f, 1f);

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
        /// Adds a LayoutElement to the given GameObject with optional size constraints.
        /// Negative values are ignored (left at Unity defaults).
        /// </summary>
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

        /// <summary>
        /// Builds a thin divider line as a child of the given parent LayoutGroup.
        /// </summary>
        private static void BuildLayoutDivider(Transform parent, bool horizontal)
        {
            var go = new GameObject("Divider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.812f, 0.710f, 0.231f, 0.3f);
            img.raycastTarget = false;
            var le = go.AddComponent<LayoutElement>();
            if (horizontal) { le.preferredHeight = 2; le.flexibleWidth = 1; }
            else { le.preferredWidth = 2; le.flexibleHeight = 1; }
        }

        /// <summary>
        /// Builds the entire stat menu UI at runtime. No scene setup required.
        /// Call from UIManager.EnsureStatMenu() or similar.
        /// </summary>
        public static StatMenuController CreateRuntimeUI()
        {
            // --- Canvas ---
            var canvasGo = new GameObject("StatMenu_Canvas");
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

            // --- Dark semi-transparent full-screen overlay ---
            var overlayGo = MakeRect("Overlay", canvasGo.transform);
            Stretch(overlayGo);
            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.sprite = WhiteSprite;
            overlayImg.color = new Color(0f, 0f, 0f, 0.6f);

            // --- Center panel with VLG + ContentSizeFitter ---
            var panelGo = MakeRect("Panel", canvasGo.transform);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 520f);

            var panelImg = panelGo.AddComponent<Image>();
            panelImg.sprite = WhiteSprite;
            panelImg.color = PanelBg;

            var panelVLG = panelGo.AddComponent<VerticalLayoutGroup>();
            panelVLG.padding = new RectOffset(20, 20, 0, 10);
            panelVLG.spacing = 4f;
            panelVLG.childAlignment = TextAnchor.UpperCenter;
            panelVLG.childControlWidth = true;
            panelVLG.childControlHeight = false;
            panelVLG.childForceExpandWidth = true;
            panelVLG.childForceExpandHeight = false;

            var panelFitter = panelGo.AddComponent<ContentSizeFitter>();
            panelFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Keep a minimum height so the panel doesn't collapse when empty
            AddLayout(panelGo, minH: 520);

            // --- Title row ---
            var titleRow = MakeRect("TitleRow", panelGo.transform);
            AddLayout(titleRow, prefH: 50);

            var titleTextGo = MakeRect("TitleText", titleRow.transform);
            Stretch(titleTextGo);
            var titleTmp = titleTextGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "Character Stats";
            titleTmp.fontSize = 28;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = AgedGold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(titleTmp);

            // Close [X] button (top-right)
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

            // --- Divider 1 (below title) ---
            BuildLayoutDivider(panelGo.transform, true);

            // --- Top section: player image + info (horizontal) ---
            var topSection = MakeRect("TopSection", panelGo.transform);
            AddLayout(topSection, prefH: 140);

            // Player image container (left side)
            var imgContainer = MakeRect("PlayerImageContainer", topSection.transform);
            var imgContainerRect = imgContainer.GetComponent<RectTransform>();
            imgContainerRect.anchorMin = new Vector2(0, 0.5f);
            imgContainerRect.anchorMax = new Vector2(0, 0.5f);
            imgContainerRect.pivot = new Vector2(0, 0.5f);
            imgContainerRect.anchoredPosition = Vector2.zero;
            imgContainerRect.sizeDelta = new Vector2(128, 128);

            var imgContainerBg = imgContainer.AddComponent<Image>();
            imgContainerBg.sprite = WhiteSprite;
            imgContainerBg.color = Charcoal;

            var playerImgGo = MakeRect("PlayerImage", imgContainer.transform);
            var playerImgRect = playerImgGo.GetComponent<RectTransform>();
            playerImgRect.anchorMin = new Vector2(0.1f, 0.1f);
            playerImgRect.anchorMax = new Vector2(0.9f, 0.9f);
            playerImgRect.offsetMin = Vector2.zero;
            playerImgRect.offsetMax = Vector2.zero;
            var playerImg = playerImgGo.AddComponent<Image>();
            playerImg.preserveAspect = true;
            playerImg.color = Color.white;

            // Layered character preview (overlays the static image)
            var layeredPreviewGo = MakeRect("LayeredPreview", imgContainer.transform);
            var layeredPreviewRect = layeredPreviewGo.GetComponent<RectTransform>();
            layeredPreviewRect.anchorMin = new Vector2(0.05f, 0.05f);
            layeredPreviewRect.anchorMax = new Vector2(0.95f, 0.95f);
            layeredPreviewRect.offsetMin = Vector2.zero;
            layeredPreviewRect.offsetMax = Vector2.zero;
            var layeredPreview = layeredPreviewGo.AddComponent<UILayeredSpritePreview>();

            // Info column (right of image)
            var infoCol = MakeRect("InfoColumn", topSection.transform);
            var infoColRect = infoCol.GetComponent<RectTransform>();
            infoColRect.anchorMin = new Vector2(0, 0);
            infoColRect.anchorMax = new Vector2(1, 1);
            infoColRect.offsetMin = new Vector2(140, 0);
            infoColRect.offsetMax = Vector2.zero;

            var infoVLG = infoCol.AddComponent<VerticalLayoutGroup>();
            infoVLG.padding = new RectOffset(0, 0, 5, 0);
            infoVLG.spacing = 2f;
            infoVLG.childAlignment = TextAnchor.UpperLeft;
            infoVLG.childControlWidth = true;
            infoVLG.childControlHeight = false;
            infoVLG.childForceExpandWidth = true;
            infoVLG.childForceExpandHeight = false;

            var charNameGo = MakeRect("CharacterName", infoCol.transform);
            var charNameTmp = BuildInfoLabel(charNameGo, "Hero", 22, AgedGold);
            AddLayout(charNameGo, prefH: 24);

            var classGo = MakeRect("Class", infoCol.transform);
            var classTmp = BuildInfoLabel(classGo, "Adventurer", 18, SubtleText);
            AddLayout(classGo, prefH: 24);

            var levelGo = MakeRect("Level", infoCol.transform);
            var lvlTmp = BuildInfoLabel(levelGo, "Level: 1", 18, BoneWhite);
            AddLayout(levelGo, prefH: 24);

            var hpGo = MakeRect("HP", infoCol.transform);
            var hpTmpRef = BuildInfoLabel(hpGo, "HP: 100/100", 18, DeepCrimson);
            AddLayout(hpGo, prefH: 24);

            var mpGo = MakeRect("MP", infoCol.transform);
            var mpTmpRef = BuildInfoLabel(mpGo, "MP: 50/50", 18, DarkBlue);
            AddLayout(mpGo, prefH: 24);

            // --- Divider 2 (below top section) ---
            BuildLayoutDivider(panelGo.transform, true);

            // --- Available Points label ---
            var pointsGo = MakeRect("AvailablePoints", panelGo.transform);
            AddLayout(pointsGo, prefH: 30);

            var pointsTmp = pointsGo.AddComponent<TextMeshProUGUI>();
            pointsTmp.text = "Available Points: 0";
            pointsTmp.fontSize = 20;
            pointsTmp.fontStyle = FontStyles.Bold;
            pointsTmp.color = AgedGold;
            pointsTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(pointsTmp);

            // --- Scrollable stats area ---
            var scrollGo = MakeRect("StatsScroll", panelGo.transform);
            AddLayout(scrollGo, prefH: 160, flexH: 1);

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20f;

            // Mask for scroll clipping
            var scrollMask = scrollGo.AddComponent<RectMask2D>();
            // RectMask2D handles clipping without needing an Image

            // Scroll content container (VLG)
            var contentGo = MakeRect("Content", scrollGo.transform);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            var contentVLG = contentGo.AddComponent<VerticalLayoutGroup>();
            contentVLG.spacing = 6f;
            contentVLG.childAlignment = TextAnchor.UpperCenter;
            contentVLG.childControlWidth = true;
            contentVLG.childControlHeight = false;
            contentVLG.childForceExpandWidth = true;
            contentVLG.childForceExpandHeight = false;

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            scrollRect.viewport = scrollGo.GetComponent<RectTransform>();

            // --- Stat definition array for data-driven row creation ---
            var statDefs = new[]
            {
                new { Name = "StrRow", StatLabel = "Strength: 1", Derived1Name = "BonusHP",
                      Derived1Label = "Bonus HP: +5", Derived2Name = "MeleeDmg",
                      Derived2Label = "Melee Damage: x1.02" },
                new { Name = "IntRow", StatLabel = "Intelligence: 1", Derived1Name = "BonusMana",
                      Derived1Label = "Bonus Mana: +3", Derived2Name = "SkillDmg",
                      Derived2Label = "Skill Damage: x1.02" },
                new { Name = "AgiRow", StatLabel = "Agility: 1", Derived1Name = "AtkSpd",
                      Derived1Label = "Attack Speed: x1.01", Derived2Name = "Crit",
                      Derived2Label = "Critical: 0.5% (x2.01)" },
            };

            // Arrays to collect references for wiring
            var statTexts = new TMP_Text[statDefs.Length];
            var allocButtons = new Button[statDefs.Length];
            var derived1Texts = new TMP_Text[statDefs.Length];
            var derived2Texts = new TMP_Text[statDefs.Length];

            for (int i = 0; i < statDefs.Length; i++)
            {
                var def = statDefs[i];

                var row = MakeRect(def.Name, contentGo.transform);
                AddLayout(row, prefH: 40);

                // Internal layout of each row uses anchor-based positioning (unchanged)
                var rowRect = row.GetComponent<RectTransform>();
                // Row height is driven by LayoutElement; width by parent VLG

                statTexts[i] = BuildStatCell(row.transform, "StatVal", def.StatLabel, 0f, 0.26f);
                allocButtons[i] = BuildAllocateButton(row.transform, "+", 0.26f, 0.32f);
                derived1Texts[i] = BuildStatCell(row.transform, def.Derived1Name, def.Derived1Label, 0.34f, 0.62f);
                derived2Texts[i] = BuildStatCell(row.transform, def.Derived2Name, def.Derived2Label, 0.64f, 1f);
            }

            // --- Wire up StatMenuController component ---
            var controller = canvasGo.AddComponent<StatMenuController>();
            controller.statMenuCanvas = canvas;
            controller.statMenuCanvasGroup = cg;
            controller.closeButton = closeBtn;

            // Player info
            controller.characterNameText = charNameTmp;
            controller.classText = classTmp;
            controller.levelText = lvlTmp;
            controller.hpText = hpTmpRef;
            controller.mpText = mpTmpRef;
            controller.playerImage = playerImg;
            controller.playerLayeredPreview = layeredPreview;

            // Stat display
            controller.availablePointsText = pointsTmp;
            controller.strengthText = statTexts[0];
            controller.intelligenceText = statTexts[1];
            controller.agilityText = statTexts[2];

            // Derived stats
            controller.bonusHPText = derived1Texts[0];
            controller.meleeDamageText = derived2Texts[0];
            controller.bonusManaText = derived1Texts[1];
            controller.skillDamageText = derived2Texts[1];
            controller.speedText = derived1Texts[2];
            controller.critChanceText = derived2Texts[2];

            // Buttons
            controller.allocateStrButton = allocButtons[0];
            controller.allocateIntButton = allocButtons[1];
            controller.allocateAgiButton = allocButtons[2];

            // Wire button listeners now that fields are assigned
            // (Awake() already ran before fields were set, so listeners were not attached)
            controller.WireButtonListeners();

            Debug.Log("[StatMenuController] Runtime UI created.");
            return controller;
        }

        private static TMP_Text BuildInfoLabel(GameObject go, string text, float fontSize, Color color)
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Left;
            FontManager.EnsureFont(tmp);
            return tmp;
        }

        private static TMP_Text BuildStatCell(Transform parent, string name, string text,
            float anchorMinX, float anchorMaxX)
        {
            var go = MakeRect(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorMinX, 0);
            rt.anchorMax = new Vector2(anchorMaxX, 1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16;
            tmp.color = BoneWhite;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 12f;
            tmp.fontSizeMax = 16f;
            FontManager.EnsureFont(tmp);
            return tmp;
        }

        private static Button BuildAllocateButton(Transform parent, string label,
            float anchorMinX, float anchorMaxX)
        {
            var go = MakeRect("AllocateBtn", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorMinX, 0.1f);
            rt.anchorMax = new Vector2(anchorMaxX, 0.9f);
            rt.offsetMin = new Vector2(2, 0);
            rt.offsetMax = new Vector2(-2, 0);

            var img = go.AddComponent<Image>();
            img.sprite = WhiteSprite;
            img.color = BtnNormal;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = BtnNormal;
            colors.highlightedColor = BtnHover;
            colors.pressedColor = BtnPress;
            colors.selectedColor = BtnHover;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            var textGo = MakeRect("Text", go.transform);
            Stretch(textGo);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = AgedGold;
            tmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(tmp);

            return btn;
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
