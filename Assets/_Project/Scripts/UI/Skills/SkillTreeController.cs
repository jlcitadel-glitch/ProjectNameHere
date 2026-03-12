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

            // Disable canvas rendering to prevent overlay flash on load
            if (skillTreeCanvas != null)
                skillTreeCanvas.enabled = false;
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

            // Enable canvas rendering
            if (skillTreeCanvas != null)
                skillTreeCanvas.enabled = true;

            if (skillTreeCanvasGroup != null)
            {
                var transitions = UIManager.Instance?.Transitions;
                if (transitions != null)
                {
                    skillTreeCanvasGroup.alpha = 0f;
                    skillTreeCanvasGroup.interactable = false;
                    skillTreeCanvasGroup.blocksRaycasts = false;
                    transitions.OpenMenu(skillTreeCanvasGroup, skillTreeCanvasGroup.GetComponent<RectTransform>(), () =>
                    {
                        skillTreeCanvasGroup.interactable = true;
                        skillTreeCanvasGroup.blocksRaycasts = true;
                    });
                }
                else
                {
                    skillTreeCanvasGroup.alpha = 1f;
                    skillTreeCanvasGroup.interactable = true;
                    skillTreeCanvasGroup.blocksRaycasts = true;
                }
            }

            // Load current job's skill tree
            if (skillTreePanel != null && SkillManager.Instance?.CurrentJob?.skillTree != null)
            {
                skillTreePanel.LoadTree(SkillManager.Instance.CurrentJob.skillTree);
            }

            // Register with UIManager so Escape closes this menu, not pause
            if (UIManager.Instance != null)
            {
                UIManager.Instance.RegisterOverlayMenu();
            }

            // Freeze time if configured (without triggering pause menu)
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
                var transitions = UIManager.Instance?.Transitions;
                if (transitions != null)
                {
                    transitions.CloseMenu(skillTreeCanvasGroup, skillTreeCanvasGroup.GetComponent<RectTransform>(), () =>
                    {
                        if (skillTreeCanvas != null)
                            skillTreeCanvas.enabled = false;
                    });
                }
                else
                {
                    skillTreeCanvasGroup.alpha = 0f;
                    skillTreeCanvasGroup.interactable = false;
                    skillTreeCanvasGroup.blocksRaycasts = false;
                    if (skillTreeCanvas != null)
                        skillTreeCanvas.enabled = false;
                }
            }
            else if (skillTreeCanvas != null)
            {
                skillTreeCanvas.enabled = false;
            }

            // Restore time if we froze it
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

            // --- Main panel (VLG + ContentSizeFitter, minHeight=600) ---
            var panelGo = MakeRect("Panel", canvasGo.transform);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(900f, 600f);

            var panelImg = panelGo.AddComponent<Image>();
            panelImg.sprite = RTBWhiteSprite;
            panelImg.color = RTBPanelBg;

            var panelVlg = panelGo.AddComponent<VerticalLayoutGroup>();
            panelVlg.childForceExpandWidth = true;
            panelVlg.childForceExpandHeight = false;
            panelVlg.childControlWidth = true;
            panelVlg.childControlHeight = true;
            panelVlg.childScaleWidth = false;
            panelVlg.childScaleHeight = false;
            panelVlg.spacing = 0;
            panelVlg.padding = new RectOffset(0, 0, 0, 0);

            var panelFitter = panelGo.AddComponent<ContentSizeFitter>();
            panelFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            AddLayout(panelGo, minH: 600);

            // --- Header (HLG, prefH=50) ---
            var headerGo = MakeRect("Header", panelGo.transform);
            var headerHlg = headerGo.AddComponent<HorizontalLayoutGroup>();
            headerHlg.childForceExpandWidth = false;
            headerHlg.childForceExpandHeight = false;
            headerHlg.childControlWidth = true;
            headerHlg.childControlHeight = true;
            headerHlg.childScaleWidth = false;
            headerHlg.childScaleHeight = false;
            headerHlg.childAlignment = TextAnchor.MiddleLeft;
            headerHlg.spacing = 8;
            headerHlg.padding = new RectOffset(15, 10, 0, 0);
            AddLayout(headerGo, prefH: 50);

            // Job icon
            var iconGo = MakeRect("JobIcon", headerGo.transform);
            var jobIconImg = iconGo.AddComponent<Image>();
            jobIconImg.sprite = RTBWhiteSprite;
            jobIconImg.color = RTBCharcoal;
            jobIconImg.preserveAspect = true;
            AddLayout(iconGo, prefW: 36, prefH: 36);

            // Job title (flexible width fills remaining space)
            var titleGo = MakeRect("JobTitle", headerGo.transform);
            var jobTitleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            jobTitleTmp.text = "Skill Tree";
            jobTitleTmp.fontSize = 26;
            jobTitleTmp.fontStyle = FontStyles.Bold;
            jobTitleTmp.color = RTBAgedGold;
            jobTitleTmp.alignment = TextAlignmentOptions.Left;
            FontManager.EnsureFont(jobTitleTmp);
            AddLayout(titleGo, flexW: 1);

            // SP display
            var spGo = MakeRect("SPDisplay", headerGo.transform);
            var spTmp = spGo.AddComponent<TextMeshProUGUI>();
            spTmp.text = "SP: 0";
            spTmp.fontSize = 20;
            spTmp.color = RTBAgedGold;
            spTmp.alignment = TextAlignmentOptions.Right;
            FontManager.EnsureFont(spTmp);
            AddLayout(spGo, prefW: 100);

            // Level display
            var lvlGo = MakeRect("LevelDisplay", headerGo.transform);
            var lvlTmp = lvlGo.AddComponent<TextMeshProUGUI>();
            lvlTmp.text = "Lv. 1";
            lvlTmp.fontSize = 20;
            lvlTmp.color = RTBBoneWhite;
            lvlTmp.alignment = TextAlignmentOptions.Right;
            FontManager.EnsureFont(lvlTmp);
            AddLayout(lvlGo, prefW: 80);

            // Close button
            var closeBtnGo = MakeRect("CloseButton", headerGo.transform);
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
            AddLayout(closeBtnGo, prefW: 36, prefH: 36);

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
            BuildLayoutDivider(panelGo.transform, true);

            // --- Body (HLG, fills remaining space) ---
            var bodyGo = MakeRect("Body", panelGo.transform);
            var bodyHlg = bodyGo.AddComponent<HorizontalLayoutGroup>();
            bodyHlg.childForceExpandWidth = false;
            bodyHlg.childForceExpandHeight = true;
            bodyHlg.childControlWidth = true;
            bodyHlg.childControlHeight = true;
            bodyHlg.childScaleWidth = false;
            bodyHlg.childScaleHeight = false;
            bodyHlg.spacing = 0;
            bodyHlg.padding = new RectOffset(0, 0, 0, 0);
            AddLayout(bodyGo, flexH: 1);

            // --- Left side: Scroll view for skill tree (flexW=0.65) ---
            var scrollGo = MakeRect("ScrollView", bodyGo.transform);
            var scrollRectComp = scrollGo.AddComponent<ScrollRect>();

            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.sprite = RTBWhiteSprite;
            scrollImg.color = new Color(0.05f, 0.05f, 0.07f, 1f);

            scrollGo.AddComponent<Mask>().showMaskGraphic = true;
            AddLayout(scrollGo, prefW: 0, flexW: 0.65f);

            // Content inside scroll view — anchored at top, stretches horizontally,
            // height is set by SkillTreePanel.CalculateLayout() based on actual node count.
            var contentGo = MakeRect("Content", scrollGo.transform);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 400);
            contentRect.anchoredPosition = Vector2.zero;

            // Connections container (behind nodes)
            var connectionsGo = MakeRect("Connections", contentGo.transform);
            Stretch(connectionsGo);

            // Nodes container (on top)
            var nodesGo = MakeRect("Nodes", contentGo.transform);
            Stretch(nodesGo);

            scrollRectComp.content = contentRect;
            scrollRectComp.horizontal = false;
            scrollRectComp.vertical = true;
            scrollRectComp.movementType = ScrollRect.MovementType.Clamped;
            scrollRectComp.scrollSensitivity = 20f;

            // --- Vertical divider ---
            BuildLayoutDivider(bodyGo.transform, false);

            // --- Right side: Skill info panel (VLG, flexW=0.35) ---
            var infoPanelGo = MakeRect("SkillInfoPanel", bodyGo.transform);
            var infoPanelVlg = infoPanelGo.AddComponent<VerticalLayoutGroup>();
            infoPanelVlg.childForceExpandWidth = true;
            infoPanelVlg.childForceExpandHeight = false;
            infoPanelVlg.childControlWidth = true;
            infoPanelVlg.childControlHeight = true;
            infoPanelVlg.childScaleWidth = false;
            infoPanelVlg.childScaleHeight = false;
            infoPanelVlg.childAlignment = TextAnchor.UpperCenter;
            infoPanelVlg.spacing = 5;
            infoPanelVlg.padding = new RectOffset(10, 15, 10, 15);
            AddLayout(infoPanelGo, prefW: 0, flexW: 0.35f);
            infoPanelGo.SetActive(false);

            // Skill icon
            var skillIconGo = MakeRect("SkillIcon", infoPanelGo.transform);
            var skillIconImg = skillIconGo.AddComponent<Image>();
            skillIconImg.sprite = RTBWhiteSprite;
            skillIconImg.color = RTBCharcoal;
            skillIconImg.preserveAspect = true;
            AddLayout(skillIconGo, prefH: 64, prefW: 64);

            // Skill name
            var skillNameGo = MakeRect("SkillName", infoPanelGo.transform);
            var skillNameTmp = skillNameGo.AddComponent<TextMeshProUGUI>();
            skillNameTmp.text = "";
            skillNameTmp.fontSize = 22;
            skillNameTmp.fontStyle = FontStyles.Bold;
            skillNameTmp.color = RTBAgedGold;
            skillNameTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(skillNameTmp);
            AddLayout(skillNameGo, prefH: 30);

            // Skill description
            var skillDescGo = MakeRect("SkillDescription", infoPanelGo.transform);
            var skillDescTmp = skillDescGo.AddComponent<TextMeshProUGUI>();
            skillDescTmp.text = "";
            skillDescTmp.fontSize = 16;
            skillDescTmp.color = RTBBoneWhite;
            skillDescTmp.alignment = TextAlignmentOptions.TopLeft;
            skillDescTmp.textWrappingMode = TextWrappingModes.Normal;
            FontManager.EnsureFont(skillDescTmp);
            AddLayout(skillDescGo, prefH: 80);

            // Skill stats
            var skillStatsGo = MakeRect("SkillStats", infoPanelGo.transform);
            var skillStatsTmp = skillStatsGo.AddComponent<TextMeshProUGUI>();
            skillStatsTmp.text = "";
            skillStatsTmp.fontSize = 15;
            skillStatsTmp.color = RTBSubtleText;
            skillStatsTmp.alignment = TextAlignmentOptions.TopLeft;
            skillStatsTmp.textWrappingMode = TextWrappingModes.Normal;
            FontManager.EnsureFont(skillStatsTmp);
            AddLayout(skillStatsGo, prefH: 120);

            // Skill requirements
            var skillReqGo = MakeRect("SkillRequirements", infoPanelGo.transform);
            var skillReqTmp = skillReqGo.AddComponent<TextMeshProUGUI>();
            skillReqTmp.text = "";
            skillReqTmp.fontSize = 14;
            skillReqTmp.color = RTBSubtleText;
            skillReqTmp.alignment = TextAlignmentOptions.TopLeft;
            skillReqTmp.textWrappingMode = TextWrappingModes.Normal;
            skillReqTmp.richText = true;
            FontManager.EnsureFont(skillReqTmp);
            AddLayout(skillReqGo, prefH: 80);

            // Flexible spacer pushes buttons to the bottom
            var spacerGo = MakeRect("Spacer", infoPanelGo.transform);
            AddLayout(spacerGo, flexH: 1);

            // Assign to Hotbar button
            var assignBtnGo = MakeRect("AssignHotbarButton", infoPanelGo.transform);
            var assignBtnImg = assignBtnGo.AddComponent<Image>();
            assignBtnImg.sprite = RTBWhiteSprite;
            assignBtnImg.color = RTBBtnNormal;

            var assignBtn = assignBtnGo.AddComponent<Button>();
            var assignBtnColors = assignBtn.colors;
            assignBtnColors.normalColor = RTBBtnNormal;
            assignBtnColors.highlightedColor = RTBBtnHover;
            assignBtnColors.pressedColor = RTBBtnPress;
            assignBtnColors.selectedColor = RTBBtnHover;
            assignBtnColors.fadeDuration = 0.1f;
            assignBtn.colors = assignBtnColors;
            AddLayout(assignBtnGo, prefH: 36);

            var assignBtnTextGo = MakeRect("Text", assignBtnGo.transform);
            Stretch(assignBtnTextGo);
            var assignBtnTmp = assignBtnTextGo.AddComponent<TextMeshProUGUI>();
            assignBtnTmp.text = "Assign to Hotbar";
            assignBtnTmp.fontSize = 16;
            assignBtnTmp.color = RTBBoneWhite;
            assignBtnTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(assignBtnTmp);

            assignBtnGo.SetActive(false); // Hidden until a learned skill is selected

            // Learn button
            var learnBtnGo = MakeRect("LearnButton", infoPanelGo.transform);
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
            AddLayout(learnBtnGo, prefH: 40);

            var learnBtnTextGo = MakeRect("Text", learnBtnGo.transform);
            Stretch(learnBtnTextGo);
            var learnBtnTmp = learnBtnTextGo.AddComponent<TextMeshProUGUI>();
            learnBtnTmp.text = "Learn";
            learnBtnTmp.fontSize = 18;
            learnBtnTmp.color = RTBBoneWhite;
            learnBtnTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(learnBtnTmp);

            // --- Build skill node prefab template ---
            var nodePrefab = BuildSkillNodePrefab();

            // --- Visual depth layers ---
            panelGo.AddComponent<UIDepthLayer>();
            ProceduralFrameBuilder.ApplyFrame(panelRect);

            // --- Wire SkillTreePanel ---
            var treePanel = panelGo.AddComponent<SkillTreePanel>();
            treePanel.SetRuntimeReferences(
                scrollRectComp, contentRect, nodesGo.GetComponent<RectTransform>(),
                connectionsGo.GetComponent<RectTransform>(),
                jobTitleTmp, jobIconImg, spTmp, lvlTmp,
                infoPanelGo, skillNameTmp, skillIconImg,
                skillDescTmp, skillStatsTmp, skillReqTmp,
                learnBtn, learnBtnTmp,
                assignBtn, assignBtnTmp
            );
            treePanel.SetRuntimeNodePrefab(nodePrefab);

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

        /// <summary>
        /// Builds a runtime skill node prefab (disabled template for Instantiate).
        /// Layout: 70x70 square with background, frame, icon, lock overlay, level text, SP badge.
        /// </summary>
        private static GameObject BuildSkillNodePrefab()
        {
            float nodeSize = 70f;

            // Root node object
            var nodeGo = new GameObject("SkillNodePrefab", typeof(RectTransform));
            var nodeRect = nodeGo.GetComponent<RectTransform>();
            nodeRect.sizeDelta = new Vector2(nodeSize, nodeSize);

            // Background (dark fill)
            var bgGo = MakeChildRect("Background", nodeGo.transform, Vector2.zero, Vector2.one);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = RTBWhiteSprite;
            bgImg.color = new Color(0.06f, 0.06f, 0.08f, 0.95f);

            // Frame (colored border — state-dependent color set by SkillNodeUI)
            var frameGo = MakeChildRect("Frame", nodeGo.transform, Vector2.zero, Vector2.one);
            var frameRect = frameGo.GetComponent<RectTransform>();
            frameRect.offsetMin = new Vector2(-2, -2);
            frameRect.offsetMax = new Vector2(2, 2);
            var frameImg = frameGo.AddComponent<Image>();
            frameImg.sprite = RTBWhiteSprite;
            frameImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            // Push behind background by reordering
            frameGo.transform.SetAsFirstSibling();

            // Icon (centered, slightly inset)
            var iconGo = MakeChildRect("Icon", nodeGo.transform, Vector2.zero, Vector2.one);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.offsetMin = new Vector2(6, 6);
            iconRect.offsetMax = new Vector2(-6, -6);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = RTBWhiteSprite;
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Lock overlay (semi-transparent dark, covers entire node)
            var lockGo = MakeChildRect("LockOverlay", nodeGo.transform, Vector2.zero, Vector2.one);
            var lockImg = lockGo.AddComponent<Image>();
            lockImg.sprite = RTBWhiteSprite;
            lockImg.color = new Color(0f, 0f, 0f, 0.6f);
            lockImg.raycastTarget = false;

            // Lock icon text
            var lockTextGo = MakeChildRect("LockText", lockGo.transform, Vector2.zero, Vector2.one);
            var lockTmp = lockTextGo.AddComponent<TextMeshProUGUI>();
            lockTmp.text = "X";
            lockTmp.fontSize = 24;
            lockTmp.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            lockTmp.alignment = TextAlignmentOptions.Center;
            lockTmp.raycastTarget = false;
            FontManager.EnsureFont(lockTmp);

            // Level text (bottom center, outside node)
            var levelGo = new GameObject("LevelText", typeof(RectTransform));
            levelGo.transform.SetParent(nodeGo.transform, false);
            var levelRect = levelGo.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0, 0);
            levelRect.anchorMax = new Vector2(1, 0);
            levelRect.pivot = new Vector2(0.5f, 1);
            levelRect.anchoredPosition = new Vector2(0, -2);
            levelRect.sizeDelta = new Vector2(0, 16);
            var levelTmp = levelGo.AddComponent<TextMeshProUGUI>();
            levelTmp.text = "0/3";
            levelTmp.fontSize = 12;
            levelTmp.color = RTBBoneWhite;
            levelTmp.alignment = TextAlignmentOptions.Center;
            levelTmp.raycastTarget = false;
            FontManager.EnsureFont(levelTmp);

            // SP cost badge (top-right corner)
            var badgeGo = new GameObject("SPBadge", typeof(RectTransform));
            badgeGo.transform.SetParent(nodeGo.transform, false);
            var badgeRect = badgeGo.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1, 1);
            badgeRect.anchorMax = new Vector2(1, 1);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(4, 4);
            badgeRect.sizeDelta = new Vector2(24, 18);

            var badgeBg = badgeGo.AddComponent<Image>();
            badgeBg.sprite = RTBWhiteSprite;
            badgeBg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            var badgeTextGo = MakeChildRect("Text", badgeGo.transform, Vector2.zero, Vector2.one);
            var badgeTmp = badgeTextGo.AddComponent<TextMeshProUGUI>();
            badgeTmp.text = "1";
            badgeTmp.fontSize = 11;
            badgeTmp.color = RTBAgedGold;
            badgeTmp.alignment = TextAlignmentOptions.Center;
            badgeTmp.raycastTarget = false;
            FontManager.EnsureFont(badgeTmp);

            // Add SkillNodeUI and wire references
            var nodeUI = nodeGo.AddComponent<SkillNodeUI>();
            nodeUI.SetRuntimeReferences(iconImg, frameImg, bgImg, lockImg, levelTmp, badgeGo, badgeTmp);

            // Disable the template — Instantiate will clone it, then it gets activated
            nodeGo.SetActive(false);

            return nodeGo;
        }

        /// <summary>
        /// Creates a stretched child RectTransform.
        /// </summary>
        private static GameObject MakeChildRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
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
            img.sprite = RTBWhiteSprite;
            img.color = RTBDividerCol;
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
    }
}
