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
        [SerializeField] private Button resetButton;

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
            if (resetButton != null)
                resetButton.onClick.AddListener(ResetAllocations);
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

            if (statMenuCanvasGroup != null)
            {
                statMenuCanvasGroup.alpha = 1f;
                statMenuCanvasGroup.interactable = true;
                statMenuCanvasGroup.blocksRaycasts = true;
            }

            RefreshDisplay();

            if (pauseGameWhenOpen)
            {
                Time.timeScale = 0f;
            }

            // Switch to UI input
            if (playerInput != null)
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

            if (pauseGameWhenOpen)
            {
                Time.timeScale = 1f;
            }

            // Switch back to gameplay input
            if (playerInput != null)
            {
                playerInput.SwitchCurrentActionMap("Player");
            }

            OnClosed?.Invoke();

            Debug.Log("[StatMenuController] Stat menu closed");
        }

        private void AllocateStat(string statName)
        {
            if (statSystem == null) return;

            if (statSystem.AllocateStat(statName))
            {
                RefreshDisplay();
            }
        }

        private void ResetAllocations()
        {
            if (statSystem == null) return;

            statSystem.ResetAllocations();
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (statSystem == null) return;

            // Available points
            if (availablePointsText != null)
                availablePointsText.text = $"Available Points: {statSystem.AvailableStatPoints}";

            // Core stats
            if (strengthText != null)
                strengthText.text = $"STR: {statSystem.Strength}";
            if (intelligenceText != null)
                intelligenceText.text = $"INT: {statSystem.Intelligence}";
            if (agilityText != null)
                agilityText.text = $"AGI: {statSystem.Agility}";

            // Derived stats
            if (bonusHPText != null)
                bonusHPText.text = $"Bonus HP: +{statSystem.BonusMaxHP:F0}";
            if (meleeDamageText != null)
                meleeDamageText.text = $"Melee Dmg: x{statSystem.MeleeDamageMultiplier:F2}";
            if (bonusManaText != null)
                bonusManaText.text = $"Bonus Mana: +{statSystem.BonusMaxMana:F0}";
            if (skillDamageText != null)
                skillDamageText.text = $"Skill Dmg: x{statSystem.SkillDamageMultiplier:F2}";
            if (speedText != null)
                speedText.text = $"Speed: x{statSystem.SpeedMultiplier:F2}";
            if (critChanceText != null)
                critChanceText.text = $"Crit: {statSystem.CritChance * 100f:F1}%";

            // Enable/disable allocate buttons based on available points
            bool hasPoints = statSystem.AvailableStatPoints > 0;
            if (allocateStrButton != null) allocateStrButton.interactable = hasPoints;
            if (allocateIntButton != null) allocateIntButton.interactable = hasPoints;
            if (allocateAgiButton != null) allocateAgiButton.interactable = hasPoints;

            // Player info
            RefreshPlayerInfo();
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

            if (playerImage != null && playerSpriteRenderer != null && playerSpriteRenderer.sprite != null)
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

            // --- Center panel (700x500) ---
            var panelGo = MakeRect("Panel", canvasGo.transform);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(700f, 520f);

            var panelImg = panelGo.AddComponent<Image>();
            panelImg.sprite = WhiteSprite;
            panelImg.color = PanelBg;

            // --- Title row ---
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
            BuildDivider(panelGo.transform, 50f);

            // --- Top section: player image + info (horizontal) ---
            var topSection = MakeRect("TopSection", panelGo.transform);
            var topRect = topSection.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.anchoredPosition = new Vector2(0, -54);
            topRect.sizeDelta = new Vector2(-40, 140);

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

            // Info column (right of image)
            var infoCol = MakeRect("InfoColumn", topSection.transform);
            var infoColRect = infoCol.GetComponent<RectTransform>();
            infoColRect.anchorMin = new Vector2(0, 0);
            infoColRect.anchorMax = new Vector2(1, 1);
            infoColRect.offsetMin = new Vector2(140, 0);
            infoColRect.offsetMax = Vector2.zero;

            float infoY = -5f;
            float infoLineH = 24f;

            var charNameGo = MakeRect("CharacterName", infoCol.transform);
            var charNameTmp = BuildInfoLabel(charNameGo, "Hero", 22, AgedGold);
            PositionInfoLine(charNameGo, infoY);
            infoY -= infoLineH + 2f;

            var classGo = MakeRect("Class", infoCol.transform);
            var classTmp = BuildInfoLabel(classGo, "Adventurer", 18, SubtleText);
            PositionInfoLine(classGo, infoY);
            infoY -= infoLineH + 2f;

            var levelGo = MakeRect("Level", infoCol.transform);
            var lvlTmp = BuildInfoLabel(levelGo, "Level: 1", 18, BoneWhite);
            PositionInfoLine(levelGo, infoY);
            infoY -= infoLineH + 2f;

            var hpGo = MakeRect("HP", infoCol.transform);
            var hpTmpRef = BuildInfoLabel(hpGo, "HP: 100/100", 18, DeepCrimson);
            PositionInfoLine(hpGo, infoY);
            infoY -= infoLineH + 2f;

            var mpGo = MakeRect("MP", infoCol.transform);
            var mpTmpRef = BuildInfoLabel(mpGo, "MP: 50/50", 18, DarkBlue);
            PositionInfoLine(mpGo, infoY);

            // --- Divider 2 (below top section) ---
            BuildDivider(panelGo.transform, 198f);

            // --- Available Points label ---
            var pointsGo = MakeRect("AvailablePoints", panelGo.transform);
            var pointsRect = pointsGo.GetComponent<RectTransform>();
            pointsRect.anchorMin = new Vector2(0, 1);
            pointsRect.anchorMax = new Vector2(1, 1);
            pointsRect.pivot = new Vector2(0.5f, 1);
            pointsRect.anchoredPosition = new Vector2(0, -206);
            pointsRect.sizeDelta = new Vector2(-40, 30);

            var pointsTmp = pointsGo.AddComponent<TextMeshProUGUI>();
            pointsTmp.text = "Available Points: 0";
            pointsTmp.fontSize = 20;
            pointsTmp.fontStyle = FontStyles.Bold;
            pointsTmp.color = AgedGold;
            pointsTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(pointsTmp);

            // --- Stats Grid (3 rows) ---
            float gridY = -240f;
            float rowH = 40f;

            // STR row
            var strRow = MakeRect("StrRow", panelGo.transform);
            PositionGridRow(strRow, gridY);
            var strTmp = BuildStatCell(strRow.transform, "StatVal", "STR: 1", 0f, 0.18f);
            var strBtn = BuildAllocateButton(strRow.transform, "+", 0.18f, 0.25f);
            var bonusHpTmp = BuildStatCell(strRow.transform, "BonusHP", "Bonus HP: +5", 0.28f, 0.56f);
            var meleeTmp = BuildStatCell(strRow.transform, "MeleeDmg", "Melee Dmg: x1.02", 0.58f, 0.95f);
            gridY -= rowH + 6f;

            // INT row
            var intRow = MakeRect("IntRow", panelGo.transform);
            PositionGridRow(intRow, gridY);
            var intTmp = BuildStatCell(intRow.transform, "StatVal", "INT: 1", 0f, 0.18f);
            var intBtn = BuildAllocateButton(intRow.transform, "+", 0.18f, 0.25f);
            var bonusManaTmp = BuildStatCell(intRow.transform, "BonusMana", "Bonus Mana: +3", 0.28f, 0.56f);
            var skillDmgTmp = BuildStatCell(intRow.transform, "SkillDmg", "Skill Dmg: x1.02", 0.58f, 0.95f);
            gridY -= rowH + 6f;

            // AGI row
            var agiRow = MakeRect("AgiRow", panelGo.transform);
            PositionGridRow(agiRow, gridY);
            var agiTmp = BuildStatCell(agiRow.transform, "StatVal", "AGI: 1", 0f, 0.18f);
            var agiBtn = BuildAllocateButton(agiRow.transform, "+", 0.18f, 0.25f);
            var spdTmp = BuildStatCell(agiRow.transform, "Speed", "Speed: x1.01", 0.28f, 0.56f);
            var critTmp = BuildStatCell(agiRow.transform, "Crit", "Crit: 0.5%", 0.58f, 0.95f);

            // --- Reset button (centered at bottom) ---
            var resetGo = MakeRect("ResetButton", panelGo.transform);
            var resetRect = resetGo.GetComponent<RectTransform>();
            resetRect.anchorMin = new Vector2(0.5f, 0);
            resetRect.anchorMax = new Vector2(0.5f, 0);
            resetRect.pivot = new Vector2(0.5f, 0);
            resetRect.anchoredPosition = new Vector2(0, 20);
            resetRect.sizeDelta = new Vector2(200, 40);

            var resetImg = resetGo.AddComponent<Image>();
            resetImg.sprite = WhiteSprite;
            resetImg.color = BtnNormal;

            var resetBtn = resetGo.AddComponent<Button>();
            var resetColors = resetBtn.colors;
            resetColors.normalColor = BtnNormal;
            resetColors.highlightedColor = BtnHover;
            resetColors.pressedColor = BtnPress;
            resetColors.selectedColor = BtnHover;
            resetColors.fadeDuration = 0.1f;
            resetBtn.colors = resetColors;

            var resetTextGo = MakeRect("Text", resetGo.transform);
            Stretch(resetTextGo);
            var resetTmp = resetTextGo.AddComponent<TextMeshProUGUI>();
            resetTmp.text = "Reset Points";
            resetTmp.fontSize = 18;
            resetTmp.color = BoneWhite;
            resetTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(resetTmp);

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

            // Stat display
            controller.availablePointsText = pointsTmp;
            controller.strengthText = strTmp;
            controller.intelligenceText = intTmp;
            controller.agilityText = agiTmp;

            // Derived stats
            controller.bonusHPText = bonusHpTmp;
            controller.meleeDamageText = meleeTmp;
            controller.bonusManaText = bonusManaTmp;
            controller.skillDamageText = skillDmgTmp;
            controller.speedText = spdTmp;
            controller.critChanceText = critTmp;

            // Buttons
            controller.allocateStrButton = strBtn;
            controller.allocateIntButton = intBtn;
            controller.allocateAgiButton = agiBtn;
            controller.resetButton = resetBtn;

            // Wire button listeners now that fields are assigned
            // (Awake() already ran before fields were set, so listeners were not attached)
            controller.WireButtonListeners();

            Debug.Log("[StatMenuController] Runtime UI created.");
            return controller;
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

        private static void PositionInfoLine(GameObject go, float yOffset)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(0, yOffset);
            rt.sizeDelta = new Vector2(0, 24);
        }

        private static void PositionGridRow(GameObject go, float yOffset)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, yOffset);
            rt.sizeDelta = new Vector2(-40, 40);
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
