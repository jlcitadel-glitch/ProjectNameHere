using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Controller for opening/closing the skill tree UI.
    /// Handles input binding and integration with game state.
    /// </summary>
    public class SkillTreeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas skillTreeCanvas;
        [SerializeField] private CanvasGroup skillTreeCanvasGroup;
        [SerializeField] private SkillTreePanel skillTreePanel;
        [SerializeField] private Button closeButton;

        [Header("Input")]
        [SerializeField] private InputActionReference openSkillTreeAction;

        [Header("Settings")]
        [SerializeField] private bool pauseGameWhenOpen = true;
        [SerializeField] private int canvasSortOrder = 150;

        private bool isOpen;
        private PlayerInput playerInput;
        private int lastToggleFrame = -1;

        // Fallback input actions created at runtime
        private InputAction fallbackOpenAction;
        private InputAction escapeAction;

        public bool IsOpen => isOpen;

        public event System.Action OnOpened;
        public event System.Action OnClosed;

        private void Awake()
        {
            // Auto-find references
            if (skillTreeCanvas == null)
            {
                skillTreeCanvas = GetComponent<Canvas>();
            }

            if (skillTreeCanvasGroup == null && skillTreeCanvas != null)
            {
                skillTreeCanvasGroup = skillTreeCanvas.GetComponent<CanvasGroup>();
                if (skillTreeCanvasGroup == null)
                {
                    skillTreeCanvasGroup = skillTreeCanvas.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (skillTreePanel == null)
            {
                skillTreePanel = GetComponentInChildren<SkillTreePanel>();
            }

            // Configure canvas
            if (skillTreeCanvas != null)
            {
                skillTreeCanvas.sortingOrder = canvasSortOrder;
            }

            // Find player input
            playerInput = FindAnyObjectByType<PlayerInput>();

            // Create fallback input actions using new Input System
            fallbackOpenAction = new InputAction("OpenSkillTree", InputActionType.Button, "<Keyboard>/k");
            escapeAction = new InputAction("CloseSkillTree", InputActionType.Button, "<Keyboard>/escape");

            // Wire close button
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            // Start closed
            Close();
        }

        private void OnEnable()
        {
            if (openSkillTreeAction?.action != null)
            {
                openSkillTreeAction.action.Enable();
                openSkillTreeAction.action.performed += OnOpenSkillTreeInput;
            }

            // Always enable fallback K key — ensures input works even if
            // the InputActionReference is assigned but its action map is disabled
            if (fallbackOpenAction != null)
            {
                fallbackOpenAction.Enable();
                fallbackOpenAction.performed += OnOpenSkillTreeInput;
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
        }

        private void OnDisable()
        {
            if (openSkillTreeAction?.action != null)
            {
                openSkillTreeAction.action.performed -= OnOpenSkillTreeInput;
            }

            if (fallbackOpenAction != null)
            {
                fallbackOpenAction.performed -= OnOpenSkillTreeInput;
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
        }

        private void OnDestroy()
        {
            // Dispose dynamically created InputActions
            fallbackOpenAction?.Dispose();
            escapeAction?.Dispose();
        }

        private void OnOpenSkillTreeInput(InputAction.CallbackContext context)
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
            // Close skill tree if game state changes to something incompatible
            if (isOpen && newState != GameManager.GameState.Paused && newState != GameManager.GameState.Playing)
            {
                Close();
            }
        }

        /// <summary>
        /// Toggles the skill tree open/closed.
        /// </summary>
        public void Toggle()
        {
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        /// <summary>
        /// Opens the skill tree UI.
        /// </summary>
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

            isOpen = true;

            // Show canvas using CanvasGroup (don't use SetActive - keeps controller running)
            if (skillTreeCanvasGroup != null)
            {
                skillTreeCanvasGroup.alpha = 1f;
                skillTreeCanvasGroup.interactable = true;
                skillTreeCanvasGroup.blocksRaycasts = true;
            }

            // Load current job's skill tree
            if (skillTreePanel != null && SkillManager.Instance?.CurrentJob?.skillTree != null)
            {
                skillTreePanel.LoadTree(SkillManager.Instance.CurrentJob.skillTree);
            }

            // Freeze time if configured (without triggering pause menu)
            if (pauseGameWhenOpen)
            {
                Time.timeScale = 0f;
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

            UIManager.Instance?.PlaySelectSound();
            OnOpened?.Invoke();

            Debug.Log("[SkillTreeController] Skill tree opened");
        }

        /// <summary>
        /// Closes the skill tree UI.
        /// </summary>
        public void Close()
        {
            if (!isOpen && skillTreeCanvasGroup != null && skillTreeCanvasGroup.alpha == 0f)
            {
                // Already closed
                return;
            }

            isOpen = false;

            // Hide canvas using CanvasGroup (don't use SetActive - keeps controller running for input)
            if (skillTreeCanvasGroup != null)
            {
                skillTreeCanvasGroup.alpha = 0f;
                skillTreeCanvasGroup.interactable = false;
                skillTreeCanvasGroup.blocksRaycasts = false;
            }

            // Restore time if we froze it
            if (pauseGameWhenOpen)
            {
                Time.timeScale = 1f;
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

            UIManager.Instance?.PlayCancelSound();
            OnClosed?.Invoke();

            Debug.Log("[SkillTreeController] Skill tree closed");
        }

        #region Runtime UI Builder

        private static readonly Color RTBPanelBg = new Color(0.08f, 0.08f, 0.1f, 0.97f);
        private static readonly Color RTBAgedGold = new Color(0.812f, 0.710f, 0.231f, 1f);
        private static readonly Color RTBBoneWhite = new Color(0.961f, 0.961f, 0.863f, 1f);
        private static readonly Color RTBCharcoal = new Color(0.102f, 0.102f, 0.102f, 0.9f);
        private static readonly Color RTBDividerCol = new Color(0.812f, 0.710f, 0.231f, 0.3f);
        private static readonly Color RTBBtnNormal = new Color(0.2f, 0.2f, 0.25f, 1f);
        private static readonly Color RTBBtnHover = new Color(0.3f, 0.3f, 0.35f, 1f);
        private static readonly Color RTBBtnPress = new Color(0.15f, 0.15f, 0.2f, 1f);
        private static readonly Color RTBSubtleText = new Color(0.7f, 0.65f, 0.55f, 1f);

        private static Sprite _rtbWhiteSprite;
        private static Sprite RTBWhiteSprite
        {
            get
            {
                if (_rtbWhiteSprite == null)
                {
                    var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    _rtbWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
                }
                return _rtbWhiteSprite;
            }
        }

        /// <summary>
        /// Builds the entire skill tree UI at runtime. No scene setup required.
        /// Call from UIManager.EnsureSkillTree() or similar.
        /// </summary>
        public static SkillTreeController CreateRuntimeUI()
        {
            // --- Canvas ---
            var canvasGo = new GameObject("SkillTree_Canvas");
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
            overlayImg.sprite = RTBWhiteSprite;
            overlayImg.color = new Color(0f, 0f, 0f, 0.6f);

            // --- Main panel (900x600) ---
            var panelGo = MakeRect("Panel", canvasGo.transform);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(900f, 600f);

            var panelImg = panelGo.AddComponent<Image>();
            panelImg.sprite = RTBWhiteSprite;
            panelImg.color = RTBPanelBg;

            // --- Header (50px) ---
            var headerGo = MakeRect("Header", panelGo.transform);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 50);

            // Job icon
            var iconGo = MakeRect("JobIcon", headerGo.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(15, 0);
            iconRect.sizeDelta = new Vector2(36, 36);
            var jobIconImg = iconGo.AddComponent<Image>();
            jobIconImg.sprite = RTBWhiteSprite;
            jobIconImg.color = RTBCharcoal;
            jobIconImg.preserveAspect = true;

            // Job title
            var titleGo = MakeRect("JobTitle", headerGo.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.offsetMin = new Vector2(60, 0);
            titleRect.offsetMax = Vector2.zero;
            var jobTitleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            jobTitleTmp.text = "Skill Tree";
            jobTitleTmp.fontSize = 26;
            jobTitleTmp.fontStyle = FontStyles.Bold;
            jobTitleTmp.color = RTBAgedGold;
            jobTitleTmp.alignment = TextAlignmentOptions.Left;
            FontManager.EnsureFont(jobTitleTmp);

            // SP display
            var spGo = MakeRect("SPDisplay", headerGo.transform);
            var spRect = spGo.GetComponent<RectTransform>();
            spRect.anchorMin = new Vector2(1, 0);
            spRect.anchorMax = new Vector2(1, 1);
            spRect.pivot = new Vector2(1, 0.5f);
            spRect.anchoredPosition = new Vector2(-160, 0);
            spRect.sizeDelta = new Vector2(100, 0);
            var spTmp = spGo.AddComponent<TextMeshProUGUI>();
            spTmp.text = "SP: 0";
            spTmp.fontSize = 20;
            spTmp.color = RTBAgedGold;
            spTmp.alignment = TextAlignmentOptions.Right;
            FontManager.EnsureFont(spTmp);

            // Level display
            var lvlGo = MakeRect("LevelDisplay", headerGo.transform);
            var lvlRect = lvlGo.GetComponent<RectTransform>();
            lvlRect.anchorMin = new Vector2(1, 0);
            lvlRect.anchorMax = new Vector2(1, 1);
            lvlRect.pivot = new Vector2(1, 0.5f);
            lvlRect.anchoredPosition = new Vector2(-60, 0);
            lvlRect.sizeDelta = new Vector2(80, 0);
            var lvlTmp = lvlGo.AddComponent<TextMeshProUGUI>();
            lvlTmp.text = "Lv. 1";
            lvlTmp.fontSize = 20;
            lvlTmp.color = RTBBoneWhite;
            lvlTmp.alignment = TextAlignmentOptions.Right;
            FontManager.EnsureFont(lvlTmp);

            // Close button
            var closeBtnGo = MakeRect("CloseButton", headerGo.transform);
            var closeBtnRect = closeBtnGo.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1, 0.5f);
            closeBtnRect.anchorMax = new Vector2(1, 0.5f);
            closeBtnRect.pivot = new Vector2(1, 0.5f);
            closeBtnRect.anchoredPosition = new Vector2(-10, 0);
            closeBtnRect.sizeDelta = new Vector2(36, 36);

            var closeBtnImg = closeBtnGo.AddComponent<Image>();
            closeBtnImg.sprite = RTBWhiteSprite;
            closeBtnImg.color = RTBBtnNormal;

            var closeBtn = closeBtnGo.AddComponent<Button>();
            var closeBtnColors = closeBtn.colors;
            closeBtnColors.normalColor = RTBBtnNormal;
            closeBtnColors.highlightedColor = RTBBtnHover;
            closeBtnColors.pressedColor = RTBBtnPress;
            closeBtnColors.selectedColor = RTBBtnHover;
            closeBtnColors.fadeDuration = 0.1f;
            closeBtn.colors = closeBtnColors;

            var closeBtnTextGo = MakeRect("Text", closeBtnGo.transform);
            Stretch(closeBtnTextGo);
            var closeBtnTmp = closeBtnTextGo.AddComponent<TextMeshProUGUI>();
            closeBtnTmp.text = "X";
            closeBtnTmp.fontSize = 20;
            closeBtnTmp.fontStyle = FontStyles.Bold;
            closeBtnTmp.color = RTBBoneWhite;
            closeBtnTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(closeBtnTmp);

            // --- Divider (below header) ---
            BuildDivider(panelGo.transform, 50f);

            // --- Body area (below header divider, split into tree view + info panel) ---
            var bodyGo = MakeRect("Body", panelGo.transform);
            var bodyRect = bodyGo.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0, 0);
            bodyRect.anchorMax = new Vector2(1, 1);
            bodyRect.offsetMin = new Vector2(0, 0);
            bodyRect.offsetMax = new Vector2(0, -54);

            // --- Left side: Scroll view for skill tree (65%) ---
            var scrollGo = MakeRect("ScrollView", bodyGo.transform);
            var scrollRectComp = scrollGo.AddComponent<ScrollRect>();
            var scrollGoRect = scrollGo.GetComponent<RectTransform>();
            scrollGoRect.anchorMin = new Vector2(0, 0);
            scrollGoRect.anchorMax = new Vector2(0.65f, 1);
            scrollGoRect.offsetMin = Vector2.zero;
            scrollGoRect.offsetMax = Vector2.zero;

            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.sprite = RTBWhiteSprite;
            scrollImg.color = new Color(0.05f, 0.05f, 0.07f, 1f);

            scrollGo.AddComponent<Mask>().showMaskGraphic = true;

            // Content inside scroll view
            var contentGo = MakeRect("Content", scrollGo.transform);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.sizeDelta = new Vector2(800, 800);
            contentRect.anchoredPosition = Vector2.zero;

            // Connections container (behind nodes)
            var connectionsGo = MakeRect("Connections", contentGo.transform);
            Stretch(connectionsGo);

            // Nodes container (on top)
            var nodesGo = MakeRect("Nodes", contentGo.transform);
            Stretch(nodesGo);

            scrollRectComp.content = contentRect;
            scrollRectComp.horizontal = true;
            scrollRectComp.vertical = true;
            scrollRectComp.movementType = ScrollRect.MovementType.Elastic;
            scrollRectComp.scrollSensitivity = 20f;

            // --- Vertical divider ---
            var vDivGo = MakeRect("VerticalDivider", bodyGo.transform);
            var vDivRect = vDivGo.GetComponent<RectTransform>();
            vDivRect.anchorMin = new Vector2(0.65f, 0.02f);
            vDivRect.anchorMax = new Vector2(0.65f, 0.98f);
            vDivRect.pivot = new Vector2(0.5f, 0.5f);
            vDivRect.sizeDelta = new Vector2(2, 0);
            var vDivImg = vDivGo.AddComponent<Image>();
            vDivImg.sprite = RTBWhiteSprite;
            vDivImg.color = RTBDividerCol;

            // --- Right side: Skill info panel (35%) ---
            var infoPanelGo = MakeRect("SkillInfoPanel", bodyGo.transform);
            var infoPanelRect = infoPanelGo.GetComponent<RectTransform>();
            infoPanelRect.anchorMin = new Vector2(0.65f, 0);
            infoPanelRect.anchorMax = new Vector2(1, 1);
            infoPanelRect.offsetMin = new Vector2(10, 15);
            infoPanelRect.offsetMax = new Vector2(-15, -10);
            infoPanelGo.SetActive(false);

            float infoY = 0f;

            // Skill icon
            var skillIconGo = MakeRect("SkillIcon", infoPanelGo.transform);
            var skillIconRect = skillIconGo.GetComponent<RectTransform>();
            skillIconRect.anchorMin = new Vector2(0.5f, 1);
            skillIconRect.anchorMax = new Vector2(0.5f, 1);
            skillIconRect.pivot = new Vector2(0.5f, 1);
            skillIconRect.anchoredPosition = new Vector2(0, infoY);
            skillIconRect.sizeDelta = new Vector2(64, 64);
            var skillIconImg = skillIconGo.AddComponent<Image>();
            skillIconImg.sprite = RTBWhiteSprite;
            skillIconImg.color = RTBCharcoal;
            skillIconImg.preserveAspect = true;
            infoY -= 70;

            // Skill name
            var skillNameGo = MakeRect("SkillName", infoPanelGo.transform);
            var skillNameRect = skillNameGo.GetComponent<RectTransform>();
            skillNameRect.anchorMin = new Vector2(0, 1);
            skillNameRect.anchorMax = new Vector2(1, 1);
            skillNameRect.pivot = new Vector2(0.5f, 1);
            skillNameRect.anchoredPosition = new Vector2(0, infoY);
            skillNameRect.sizeDelta = new Vector2(0, 30);
            var skillNameTmp = skillNameGo.AddComponent<TextMeshProUGUI>();
            skillNameTmp.text = "";
            skillNameTmp.fontSize = 22;
            skillNameTmp.fontStyle = FontStyles.Bold;
            skillNameTmp.color = RTBAgedGold;
            skillNameTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(skillNameTmp);
            infoY -= 35;

            // Skill description
            var skillDescGo = MakeRect("SkillDescription", infoPanelGo.transform);
            var skillDescRect = skillDescGo.GetComponent<RectTransform>();
            skillDescRect.anchorMin = new Vector2(0, 1);
            skillDescRect.anchorMax = new Vector2(1, 1);
            skillDescRect.pivot = new Vector2(0.5f, 1);
            skillDescRect.anchoredPosition = new Vector2(0, infoY);
            skillDescRect.sizeDelta = new Vector2(0, 80);
            var skillDescTmp = skillDescGo.AddComponent<TextMeshProUGUI>();
            skillDescTmp.text = "";
            skillDescTmp.fontSize = 16;
            skillDescTmp.color = RTBBoneWhite;
            skillDescTmp.alignment = TextAlignmentOptions.TopLeft;
            skillDescTmp.textWrappingMode = TextWrappingModes.Normal;
            FontManager.EnsureFont(skillDescTmp);
            infoY -= 85;

            // Skill stats
            var skillStatsGo = MakeRect("SkillStats", infoPanelGo.transform);
            var skillStatsRect = skillStatsGo.GetComponent<RectTransform>();
            skillStatsRect.anchorMin = new Vector2(0, 1);
            skillStatsRect.anchorMax = new Vector2(1, 1);
            skillStatsRect.pivot = new Vector2(0.5f, 1);
            skillStatsRect.anchoredPosition = new Vector2(0, infoY);
            skillStatsRect.sizeDelta = new Vector2(0, 80);
            var skillStatsTmp = skillStatsGo.AddComponent<TextMeshProUGUI>();
            skillStatsTmp.text = "";
            skillStatsTmp.fontSize = 15;
            skillStatsTmp.color = RTBSubtleText;
            skillStatsTmp.alignment = TextAlignmentOptions.TopLeft;
            skillStatsTmp.textWrappingMode = TextWrappingModes.Normal;
            FontManager.EnsureFont(skillStatsTmp);
            infoY -= 85;

            // Skill requirements
            var skillReqGo = MakeRect("SkillRequirements", infoPanelGo.transform);
            var skillReqRect = skillReqGo.GetComponent<RectTransform>();
            skillReqRect.anchorMin = new Vector2(0, 1);
            skillReqRect.anchorMax = new Vector2(1, 1);
            skillReqRect.pivot = new Vector2(0.5f, 1);
            skillReqRect.anchoredPosition = new Vector2(0, infoY);
            skillReqRect.sizeDelta = new Vector2(0, 40);
            var skillReqTmp = skillReqGo.AddComponent<TextMeshProUGUI>();
            skillReqTmp.text = "";
            skillReqTmp.fontSize = 14;
            skillReqTmp.color = RTBSubtleText;
            skillReqTmp.alignment = TextAlignmentOptions.TopLeft;
            skillReqTmp.textWrappingMode = TextWrappingModes.Normal;
            FontManager.EnsureFont(skillReqTmp);
            infoY -= 50;

            // Learn button (anchored to bottom of info panel)
            var learnBtnGo = MakeRect("LearnButton", infoPanelGo.transform);
            var learnBtnRect = learnBtnGo.GetComponent<RectTransform>();
            learnBtnRect.anchorMin = new Vector2(0.1f, 0);
            learnBtnRect.anchorMax = new Vector2(0.9f, 0);
            learnBtnRect.pivot = new Vector2(0.5f, 0);
            learnBtnRect.anchoredPosition = new Vector2(0, 10);
            learnBtnRect.sizeDelta = new Vector2(0, 40);

            var learnBtnImg = learnBtnGo.AddComponent<Image>();
            learnBtnImg.sprite = RTBWhiteSprite;
            learnBtnImg.color = RTBBtnNormal;

            var learnBtn = learnBtnGo.AddComponent<Button>();
            var learnBtnColors = learnBtn.colors;
            learnBtnColors.normalColor = RTBBtnNormal;
            learnBtnColors.highlightedColor = RTBBtnHover;
            learnBtnColors.pressedColor = RTBBtnPress;
            learnBtnColors.selectedColor = RTBBtnHover;
            learnBtnColors.fadeDuration = 0.1f;
            learnBtn.colors = learnBtnColors;

            var learnBtnTextGo = MakeRect("Text", learnBtnGo.transform);
            Stretch(learnBtnTextGo);
            var learnBtnTmp = learnBtnTextGo.AddComponent<TextMeshProUGUI>();
            learnBtnTmp.text = "Learn";
            learnBtnTmp.fontSize = 18;
            learnBtnTmp.color = RTBBoneWhite;
            learnBtnTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(learnBtnTmp);

            // --- Wire SkillTreePanel ---
            var treePanel = panelGo.AddComponent<SkillTreePanel>();
            treePanel.SetRuntimeReferences(
                scrollRectComp, contentRect, nodesGo.GetComponent<RectTransform>(),
                connectionsGo.GetComponent<RectTransform>(),
                jobTitleTmp, jobIconImg, spTmp, lvlTmp,
                infoPanelGo, skillNameTmp, skillIconImg,
                skillDescTmp, skillStatsTmp, skillReqTmp,
                learnBtn, learnBtnTmp
            );

            // --- Wire SkillTreeController ---
            // Note: Awake() fires during AddComponent and auto-finds canvas/canvasGroup/panel,
            // but closeButton is null at that point, so we wire it manually afterward.
            var controller = canvasGo.AddComponent<SkillTreeController>();
            controller.skillTreeCanvas = canvas;
            controller.skillTreeCanvasGroup = cg;
            controller.skillTreePanel = treePanel;
            controller.closeButton = closeBtn;
            closeBtn.onClick.AddListener(controller.Close);

            Debug.Log("[SkillTreeController] Runtime UI created.");
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
            divImg.sprite = RTBWhiteSprite;
            divImg.color = RTBDividerCol;
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
