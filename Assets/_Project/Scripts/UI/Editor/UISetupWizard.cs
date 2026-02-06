#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;
using System.IO;

namespace ProjectName.UI.Editor
{
    /// <summary>
    /// Editor wizard to quickly set up the UI system in a scene.
    /// Creates prefabs and instantiates them properly.
    /// </summary>
    public class UISetupWizard : EditorWindow
    {
        // Prefab paths
        private const string UI_PREFAB_ROOT = "Assets/_Project/Prefabs/UI";
        private const string SYSTEMS_PATH = "Assets/_Project/Prefabs/UI/Systems";
        private const string CANVASES_PATH = "Assets/_Project/Prefabs/UI/Canvases";
        private const string COMPONENTS_PATH = "Assets/_Project/Prefabs/UI/Components";
        private const string DEBUG_PATH = "Assets/_Project/Prefabs/UI/Debug";

        [MenuItem("Tools/ProjectName/UI Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow<UISetupWizard>("UI Setup Wizard");
        }

        private void OnGUI()
        {
            GUILayout.Label("UI System Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This wizard creates prefabs and instantiates them:\n\n" +
                "Prefab Structure:\n" +
                "• Prefabs/UI/Systems/ - UIManager\n" +
                "• Prefabs/UI/Canvases/ - HUD, Pause, World canvases\n" +
                "• Prefabs/UI/Components/ - Buttons, frames, etc.\n" +
                "• Prefabs/UI/Debug/ - Test tools",
                MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Full UI Setup", GUILayout.Height(40)))
            {
                CreateFullSetup();
            }

            EditorGUILayout.Space();

            // Prefab creation section
            GUILayout.Label("Prefab Management", EditorStyles.boldLabel);

            if (GUILayout.Button("Create/Update All UI Prefabs"))
            {
                CreateAllPrefabs();
            }

            if (GUILayout.Button("Instantiate UI From Prefabs"))
            {
                InstantiateFromPrefabs();
            }

            EditorGUILayout.Space();

            // Cleanup section
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("Clean Up Duplicate UI Objects", GUILayout.Height(30)))
            {
                CleanupDuplicates();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();
            GUILayout.Label("Individual Components", EditorStyles.boldLabel);

            if (GUILayout.Button("Create UIManager Only"))
            {
                CreateUIManager();
            }

            if (GUILayout.Button("Create HUD Canvas Only"))
            {
                CreateHUDCanvas();
            }

            if (GUILayout.Button("Create Pause Menu Only"))
            {
                CreatePauseMenu();
            }

            if (GUILayout.Button("Create Display Settings Only"))
            {
                CreateDisplaySettings();
            }

            if (GUILayout.Button("Create Skill Tree Canvas Only"))
            {
                CreateSkillTreeCanvas();
            }

            if (GUILayout.Button("Create Skill Manager"))
            {
                CreateSkillManager();
            }

            EditorGUILayout.Space();
            GUILayout.Label("Main Menu", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Main Menu Scene"))
            {
                CreateMainMenuScene();
            }

            if (GUILayout.Button("Create Main Menu Canvas Only"))
            {
                CreateMainMenuCanvas();
            }

            EditorGUILayout.Space();
            GUILayout.Label("Utilities", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Adaptive Scaler to Selected Canvas"))
            {
                AddAdaptiveScalerToSelection();
            }

            if (GUILayout.Button("Add Safe Area Handler to Selected"))
            {
                AddSafeAreaHandlerToSelection();
            }
        }

        #region Directory Setup

        private static void EnsureDirectoriesExist()
        {
            string[] directories = new string[]
            {
                UI_PREFAB_ROOT,
                SYSTEMS_PATH,
                CANVASES_PATH,
                COMPONENTS_PATH,
                DEBUG_PATH
            };

            foreach (string dir in directories)
            {
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    string parent = Path.GetDirectoryName(dir).Replace("\\", "/");
                    string folderName = Path.GetFileName(dir);
                    AssetDatabase.CreateFolder(parent, folderName);
                    Debug.Log($"[UISetupWizard] Created folder: {dir}");
                }
            }
        }

        #endregion

        #region Full Setup

        private static void CreateFullSetup()
        {
            EnsureDirectoriesExist();

            // Ensure EventSystem exists
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemGO, "Create EventSystem");
            }

            // Create systems
            CreateDisplaySettings();
            CreateSkillManager();

            // Create and wire up
            var uiManager = CreateUIManager();
            var hudCanvas = CreateHUDCanvas();
            var pauseCanvas = CreatePauseMenu();
            CreateSkillTreeCanvas();

            // Wire up references
            var manager = uiManager.GetComponent<UIManager>();
            if (manager != null)
            {
                SerializedObject so = new SerializedObject(manager);
                so.FindProperty("hudCanvas").objectReferenceValue = hudCanvas.GetComponent<Canvas>();
                so.FindProperty("hudGroup").objectReferenceValue = hudCanvas.GetComponent<CanvasGroup>();
                so.FindProperty("pauseCanvas").objectReferenceValue = pauseCanvas.GetComponent<Canvas>();
                so.FindProperty("pauseGroup").objectReferenceValue = pauseCanvas.GetComponent<CanvasGroup>();
                so.ApplyModifiedProperties();

                // Update the UIManager prefab with wired references
                SaveAsPrefab(uiManager, SYSTEMS_PATH, "UIManager");
            }

            Debug.Log("[UISetupWizard] Full UI setup complete!");
            Selection.activeGameObject = uiManager;
        }

        #endregion

        #region Prefab Creation

        private static void CreateAllPrefabs()
        {
            EnsureDirectoriesExist();

            // Find and save existing scene objects as prefabs
            SaveSceneObjectAsPrefab("UIManager", SYSTEMS_PATH);
            SaveSceneObjectAsPrefab("HUD_Canvas", CANVASES_PATH);
            SaveSceneObjectAsPrefab("PauseMenu_Canvas", CANVASES_PATH);
            SaveSceneObjectAsPrefab("SkillTree_Canvas", CANVASES_PATH);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[UISetupWizard] All UI prefabs created/updated.");
        }

        private static void SaveSceneObjectAsPrefab(string objectName, string folderPath)
        {
            // Find object including inactive
            GameObject sceneObject = FindSceneObject(objectName);

            if (sceneObject == null)
            {
                Debug.LogWarning($"[UISetupWizard] Could not find '{objectName}' in scene. Skipping prefab creation.");
                return;
            }

            SaveAsPrefab(sceneObject, folderPath, objectName);
        }

        private static void SaveAsPrefab(GameObject sceneObject, string folderPath, string prefabName)
        {
            string prefabPath = $"{folderPath}/{prefabName}.prefab";

            // Check if prefab already exists
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existingPrefab != null)
            {
                // Update existing prefab
                PrefabUtility.SaveAsPrefabAssetAndConnect(sceneObject, prefabPath, InteractionMode.UserAction);
                Debug.Log($"[UISetupWizard] Updated prefab: {prefabPath}");
            }
            else
            {
                // Create new prefab
                PrefabUtility.SaveAsPrefabAssetAndConnect(sceneObject, prefabPath, InteractionMode.UserAction);
                Debug.Log($"[UISetupWizard] Created prefab: {prefabPath}");
            }
        }

        private static void InstantiateFromPrefabs()
        {
            EnsureDirectoriesExist();

            InstantiatePrefabIfNotInScene($"{SYSTEMS_PATH}/UIManager.prefab", "UIManager");
            InstantiatePrefabIfNotInScene($"{CANVASES_PATH}/HUD_Canvas.prefab", "HUD_Canvas");
            InstantiatePrefabIfNotInScene($"{CANVASES_PATH}/PauseMenu_Canvas.prefab", "PauseMenu_Canvas");
            InstantiatePrefabIfNotInScene($"{CANVASES_PATH}/SkillTree_Canvas.prefab", "SkillTree_Canvas");

            // Ensure EventSystem
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemGO, "Create EventSystem");
            }

            Debug.Log("[UISetupWizard] UI instantiated from prefabs.");
        }

        private static void InstantiatePrefabIfNotInScene(string prefabPath, string objectName)
        {
            // Check if already in scene
            if (FindSceneObject(objectName) != null)
            {
                Debug.Log($"[UISetupWizard] '{objectName}' already in scene. Skipping.");
                return;
            }

            // Load and instantiate prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = objectName;
                Undo.RegisterCreatedObjectUndo(instance, $"Instantiate {objectName}");
                Debug.Log($"[UISetupWizard] Instantiated '{objectName}' from prefab.");
            }
            else
            {
                Debug.LogWarning($"[UISetupWizard] Prefab not found: {prefabPath}");
            }
        }

        #endregion

        #region Individual Creators

        private static GameObject CreateUIManager()
        {
            var existing = Object.FindAnyObjectByType<UIManager>();
            if (existing != null)
            {
                Debug.LogWarning("[UISetupWizard] UIManager already exists in scene.");
                return existing.gameObject;
            }

            // Try to instantiate from prefab first
            string prefabPath = $"{SYSTEMS_PATH}/UIManager.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            GameObject go;
            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = "UIManager";
            }
            else
            {
                // Create new
                go = new GameObject("UIManager");
                go.AddComponent<UIManager>();
                go.AddComponent<AudioSource>();

                // Save as prefab
                EnsureDirectoriesExist();
                SaveAsPrefab(go, SYSTEMS_PATH, "UIManager");
            }

            Undo.RegisterCreatedObjectUndo(go, "Create UIManager");
            Debug.Log("[UISetupWizard] Created UIManager");
            return go;
        }

        private static GameObject CreateHUDCanvas()
        {
            // Check for existing (including inactive)
            GameObject existing = FindSceneObject("HUD_Canvas");
            if (existing != null)
            {
                Debug.LogWarning("[UISetupWizard] HUD_Canvas already exists. Skipping.");
                return existing;
            }

            // Try to instantiate from prefab first
            string prefabPath = $"{CANVASES_PATH}/HUD_Canvas.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            GameObject go;
            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = "HUD_Canvas";
            }
            else
            {
                // Create new
                go = new GameObject("HUD_Canvas");

                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;

                // Use AdaptiveCanvasScaler for proper aspect ratio handling
                var scaler = go.AddComponent<CanvasScaler>();
                var adaptiveScaler = go.AddComponent<AdaptiveCanvasScaler>();
                // AdaptiveCanvasScaler will configure the CanvasScaler automatically

                go.AddComponent<GraphicRaycaster>();
                go.AddComponent<CanvasGroup>();

                // Add SafeArea container for content
                var safeArea = new GameObject("SafeArea");
                safeArea.transform.SetParent(go.transform, false);
                var safeAreaRect = safeArea.AddComponent<RectTransform>();
                safeAreaRect.anchorMin = Vector2.zero;
                safeAreaRect.anchorMax = Vector2.one;
                safeAreaRect.offsetMin = Vector2.zero;
                safeAreaRect.offsetMax = Vector2.zero;
                safeArea.AddComponent<SafeAreaHandler>();

                CreateHUDPlaceholder(safeArea.transform);

                // Save as prefab
                EnsureDirectoriesExist();
                SaveAsPrefab(go, CANVASES_PATH, "HUD_Canvas");
            }

            Undo.RegisterCreatedObjectUndo(go, "Create HUD Canvas");
            Debug.Log("[UISetupWizard] Created HUD Canvas");
            return go;
        }

        private static void CreateHUDPlaceholder(Transform parent)
        {
            // Add GameFrameHUD component to parent's canvas
            var canvas = parent.GetComponentInParent<Canvas>();
            GameFrameHUD frameHUD = null;
            if (canvas != null)
            {
                frameHUD = canvas.gameObject.GetComponent<GameFrameHUD>();
                if (frameHUD == null)
                    frameHUD = canvas.gameObject.AddComponent<GameFrameHUD>();
            }

            // === BOTTOM BAR (Level/HP/MP/XP/Skills — all in one bar) ===
            var bottomBar = CreateBottomBar(parent);

            // Wire up GameFrameHUD references
            if (frameHUD != null)
            {
                var so = new SerializedObject(frameHUD);
                so.FindProperty("bottomBar").objectReferenceValue = bottomBar.GetComponent<RectTransform>();

                // Wire child components from bottom bar
                var levelDisplay = bottomBar.GetComponentInChildren<LevelDisplay>();
                if (levelDisplay != null)
                    so.FindProperty("levelDisplay").objectReferenceValue = levelDisplay;

                var resourceBars = bottomBar.GetComponentsInChildren<ResourceBarDisplay>();
                foreach (var bar in resourceBars)
                {
                    if (bar.gameObject.name.Contains("Health"))
                        so.FindProperty("healthBar").objectReferenceValue = bar;
                    else if (bar.gameObject.name.Contains("Mana"))
                        so.FindProperty("manaBar").objectReferenceValue = bar;
                }

                var expBar = bottomBar.GetComponentInChildren<ExpBarDisplay>();
                if (expBar != null)
                    so.FindProperty("expBar").objectReferenceValue = expBar;

                var bottomFrame = bottomBar.transform.Find("FrameBorder")?.GetComponent<Image>();
                if (bottomFrame != null)
                    so.FindProperty("bottomFrame").objectReferenceValue = bottomFrame;

                so.ApplyModifiedProperties();
            }
        }

        private static GameObject CreateBottomBar(Transform parent)
        {
            var bottomBar = new GameObject("BottomBar");
            bottomBar.transform.SetParent(parent, false);

            var barRect = bottomBar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(1, 0);
            barRect.pivot = new Vector2(0.5f, 0);
            barRect.anchoredPosition = new Vector2(0, 20);
            barRect.sizeDelta = new Vector2(-40, 60);

            // Frame border background
            var frameBorder = new GameObject("FrameBorder");
            frameBorder.transform.SetParent(bottomBar.transform, false);
            var frameImage = frameBorder.AddComponent<Image>();
            frameImage.color = new Color(0.812f, 0.710f, 0.231f, 0.3f); // Aged gold, semi-transparent
            var frameRect = frameBorder.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;

            // XP Bar
            var expBarGroup = new GameObject("ExpBarGroup");
            expBarGroup.transform.SetParent(bottomBar.transform, false);
            var expBar = expBarGroup.AddComponent<ExpBarDisplay>();

            var expGroupRect = expBarGroup.GetComponent<RectTransform>();
            expGroupRect.anchorMin = new Vector2(0, 0.5f);
            expGroupRect.anchorMax = new Vector2(0.5f, 0.5f);
            expGroupRect.pivot = new Vector2(0, 0.5f);
            expGroupRect.anchoredPosition = new Vector2(12, 0);
            expGroupRect.sizeDelta = new Vector2(-24, 24);

            // XP Background
            var expBg = new GameObject("Background");
            expBg.transform.SetParent(expBarGroup.transform, false);
            var expBgImage = expBg.AddComponent<Image>();
            expBgImage.color = new Color(0.102f, 0.102f, 0.102f, 0.9f);
            var expBgRect = expBg.GetComponent<RectTransform>();
            expBgRect.anchorMin = Vector2.zero;
            expBgRect.anchorMax = Vector2.one;
            expBgRect.offsetMin = Vector2.zero;
            expBgRect.offsetMax = Vector2.zero;

            // XP Fill
            var expFill = new GameObject("Fill");
            expFill.transform.SetParent(expBarGroup.transform, false);
            var expFillImage = expFill.AddComponent<Image>();
            expFillImage.type = Image.Type.Filled;
            expFillImage.fillMethod = Image.FillMethod.Horizontal;
            expFillImage.fillAmount = 0f;
            expFillImage.color = new Color(0.812f, 0.710f, 0.231f, 1f); // Aged gold
            var expFillRect = expFill.GetComponent<RectTransform>();
            expFillRect.anchorMin = Vector2.zero;
            expFillRect.anchorMax = Vector2.one;
            expFillRect.offsetMin = new Vector2(2, 2);
            expFillRect.offsetMax = new Vector2(-2, -2);

            // XP Label
            var expLabel = new GameObject("Label");
            expLabel.transform.SetParent(expBarGroup.transform, false);
            var expLabelTMP = expLabel.AddComponent<TextMeshProUGUI>();
            expLabelTMP.text = "0/100 XP";
            expLabelTMP.fontSize = 12;
            expLabelTMP.alignment = TextAlignmentOptions.Center;
            expLabelTMP.color = new Color(0.961f, 0.961f, 0.863f, 1f);
            var expLabelRect = expLabel.GetComponent<RectTransform>();
            expLabelRect.anchorMin = Vector2.zero;
            expLabelRect.anchorMax = Vector2.one;
            expLabelRect.offsetMin = Vector2.zero;
            expLabelRect.offsetMax = Vector2.zero;

            // Wire ExpBarDisplay
            var expSO = new SerializedObject(expBar);
            expSO.FindProperty("fillImage").objectReferenceValue = expFillImage;
            expSO.FindProperty("backgroundImage").objectReferenceValue = expBgImage;
            expSO.FindProperty("expLabel").objectReferenceValue = expLabelTMP;
            expSO.ApplyModifiedProperties();

            // Quick Slots placeholder (right side of bottom bar)
            var quickSlots = new GameObject("QuickSlots");
            quickSlots.transform.SetParent(bottomBar.transform, false);
            var quickSlotsRect = quickSlots.AddComponent<RectTransform>();
            quickSlotsRect.anchorMin = new Vector2(0.5f, 0);
            quickSlotsRect.anchorMax = new Vector2(1, 1);
            quickSlotsRect.pivot = new Vector2(1, 0.5f);
            quickSlotsRect.offsetMin = new Vector2(12, 8);
            quickSlotsRect.offsetMax = new Vector2(-12, -8);

            // Placeholder text for quick slots
            var slotsText = new GameObject("PlaceholderText");
            slotsText.transform.SetParent(quickSlots.transform, false);
            var slotsTMP = slotsText.AddComponent<TextMeshProUGUI>();
            slotsTMP.text = "[1] [2] [3] [4] [5] [6]";
            slotsTMP.fontSize = 16;
            slotsTMP.alignment = TextAlignmentOptions.Right;
            slotsTMP.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            var slotsTextRect = slotsText.GetComponent<RectTransform>();
            slotsTextRect.anchorMin = Vector2.zero;
            slotsTextRect.anchorMax = Vector2.one;
            slotsTextRect.offsetMin = Vector2.zero;
            slotsTextRect.offsetMax = Vector2.zero;

            return bottomBar;
        }

        private static GameObject CreatePauseMenu()
        {
            // Check for existing (including inactive)
            GameObject existing = FindSceneObject("PauseMenu_Canvas");
            if (existing != null)
            {
                Debug.LogWarning("[UISetupWizard] PauseMenu_Canvas already exists. Skipping.");
                return existing;
            }

            // Try to instantiate from prefab first
            string prefabPath = $"{CANVASES_PATH}/PauseMenu_Canvas.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            GameObject go;
            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = "PauseMenu_Canvas";
            }
            else
            {
                // Create new
                go = new GameObject("PauseMenu_Canvas");

                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 200;

                // Use AdaptiveCanvasScaler for proper aspect ratio handling
                var scaler = go.AddComponent<CanvasScaler>();
                var adaptiveScaler = go.AddComponent<AdaptiveCanvasScaler>();

                go.AddComponent<GraphicRaycaster>();
                var group = go.AddComponent<CanvasGroup>();
                group.alpha = 1f;

                // Add SafeArea container for content
                var safeArea = new GameObject("SafeArea");
                safeArea.transform.SetParent(go.transform, false);
                var safeAreaRect = safeArea.AddComponent<RectTransform>();
                safeAreaRect.anchorMin = Vector2.zero;
                safeAreaRect.anchorMax = Vector2.one;
                safeAreaRect.offsetMin = Vector2.zero;
                safeAreaRect.offsetMax = Vector2.zero;
                safeArea.AddComponent<SafeAreaHandler>();

                CreatePauseMenuContent(safeArea.transform);

                go.SetActive(false);

                // Save as prefab
                EnsureDirectoriesExist();
                SaveAsPrefab(go, CANVASES_PATH, "PauseMenu_Canvas");
            }

            Undo.RegisterCreatedObjectUndo(go, "Create Pause Menu");
            Debug.Log("[UISetupWizard] Created Pause Menu Canvas");
            return go;
        }

        private static void CreatePauseMenuContent(Transform parent)
        {
            // Add controller to parent's canvas
            var canvas = parent.GetComponentInParent<Canvas>();
            PauseMenuController pauseController = null;
            if (canvas != null)
            {
                pauseController = canvas.gameObject.GetComponent<PauseMenuController>();
                if (pauseController == null)
                    pauseController = canvas.gameObject.AddComponent<PauseMenuController>();
            }

            // Dark overlay background
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(parent, false);
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0.051f, 0.051f, 0.051f, 0.85f);
            overlayImage.raycastTarget = true;
            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            // === MAIN PAUSE PANEL ===
            var mainPanel = new GameObject("MainPausePanel");
            mainPanel.transform.SetParent(parent, false);
            var mainPanelImage = mainPanel.AddComponent<Image>();
            mainPanelImage.color = new Color(0.102f, 0.102f, 0.102f, 0.95f);
            var mainPanelRect = mainPanel.GetComponent<RectTransform>();
            mainPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            mainPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            mainPanelRect.pivot = new Vector2(0.5f, 0.5f);
            mainPanelRect.sizeDelta = new Vector2(400, 400);

            // Title
            var title = new GameObject("Title");
            title.transform.SetParent(mainPanel.transform, false);
            var titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = "PAUSED";
            titleText.fontSize = 48;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.812f, 0.710f, 0.231f, 1f);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -20);
            titleRect.sizeDelta = new Vector2(0, 60);

            var resumeBtn = CreateMenuButton(mainPanel.transform, "ResumeButton", "Resume", new Vector2(0, 50));
            var optionsBtn = CreateMenuButton(mainPanel.transform, "OptionsButton", "Options", new Vector2(0, -20));
            var quitBtn = CreateMenuButton(mainPanel.transform, "QuitButton", "Quit", new Vector2(0, -90));

            // Ensure main panel starts active
            mainPanel.SetActive(true);

            // === OPTIONS PANEL ===
            var optionsPanel = CreateOptionsPanel(parent);
            // Ensure options panel starts INACTIVE
            optionsPanel.SetActive(false);

            // Wire up PauseMenuController
            if (pauseController != null)
            {
                var so = new SerializedObject(pauseController);
                so.FindProperty("mainPausePanel").objectReferenceValue = mainPanel;
                so.FindProperty("optionsPanel").objectReferenceValue = optionsPanel;
                so.FindProperty("resumeButton").objectReferenceValue = resumeBtn.GetComponent<Button>();
                so.FindProperty("optionsButton").objectReferenceValue = optionsBtn.GetComponent<Button>();
                so.FindProperty("quitButton").objectReferenceValue = quitBtn.GetComponent<Button>();
                so.FindProperty("mainMenuFirstSelected").objectReferenceValue = resumeBtn;

                // Find back button in options
                var backBtn = optionsPanel.transform.Find("Header/BackButton");
                if (backBtn != null)
                {
                    so.FindProperty("optionsBackButton").objectReferenceValue = backBtn.GetComponent<Button>();
                    so.FindProperty("optionsFirstSelected").objectReferenceValue = backBtn.gameObject;
                }

                so.ApplyModifiedProperties();
            }
        }

        private static GameObject CreateOptionsPanel(Transform parent)
        {
            var optionsPanel = new GameObject("OptionsPanel");
            optionsPanel.transform.SetParent(parent, false);
            var optionsPanelImage = optionsPanel.AddComponent<Image>();
            optionsPanelImage.color = new Color(0.102f, 0.102f, 0.102f, 0.95f);
            var optionsPanelRect = optionsPanel.GetComponent<RectTransform>();
            optionsPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            optionsPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            optionsPanelRect.pivot = new Vector2(0.5f, 0.5f);
            optionsPanelRect.sizeDelta = new Vector2(700, 500);

            // Add controller
            var optionsController = optionsPanel.AddComponent<OptionsMenuController>();

            // Header with back button and title
            var header = new GameObject("Header");
            header.transform.SetParent(optionsPanel.transform, false);
            var headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 60);

            // Back button
            var backBtn = CreateMenuButton(header.transform, "BackButton", "< Back", Vector2.zero);
            var backBtnRect = backBtn.GetComponent<RectTransform>();
            backBtnRect.anchorMin = new Vector2(0, 0.5f);
            backBtnRect.anchorMax = new Vector2(0, 0.5f);
            backBtnRect.pivot = new Vector2(0, 0.5f);
            backBtnRect.anchoredPosition = new Vector2(20, 0);
            backBtnRect.sizeDelta = new Vector2(120, 40);

            // Options title
            var optionsTitle = new GameObject("Title");
            optionsTitle.transform.SetParent(header.transform, false);
            var optionsTitleText = optionsTitle.AddComponent<TextMeshProUGUI>();
            optionsTitleText.text = "OPTIONS";
            optionsTitleText.fontSize = 36;
            optionsTitleText.alignment = TextAlignmentOptions.Center;
            optionsTitleText.color = new Color(0.812f, 0.710f, 0.231f, 1f);
            optionsTitleText.raycastTarget = false; // Don't block clicks to back button
            var optionsTitleRect = optionsTitle.GetComponent<RectTransform>();
            optionsTitleRect.anchorMin = Vector2.zero;
            optionsTitleRect.anchorMax = Vector2.one;
            optionsTitleRect.offsetMin = Vector2.zero;
            optionsTitleRect.offsetMax = Vector2.zero;

            // Tab bar
            var tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(optionsPanel.transform, false);
            var tabBarRect = tabBar.AddComponent<RectTransform>();
            tabBarRect.anchorMin = new Vector2(0, 1);
            tabBarRect.anchorMax = new Vector2(1, 1);
            tabBarRect.pivot = new Vector2(0.5f, 1);
            tabBarRect.anchoredPosition = new Vector2(0, -70);
            tabBarRect.sizeDelta = new Vector2(0, 50);

            var displayTab = CreateTabButton(tabBar.transform, "DisplayTab", "Display", 0);
            var audioTab = CreateTabButton(tabBar.transform, "AudioTab", "Audio", 1);
            var controlsTab = CreateTabButton(tabBar.transform, "ControlsTab", "Controls", 2);

            // Content area
            var contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(optionsPanel.transform, false);
            var contentRect = contentArea.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(20, 20);
            contentRect.offsetMax = new Vector2(-20, -130);

            // Create tab panels
            var displayPanel = CreateDisplaySettingsContent(contentArea.transform);
            var audioPanel = CreateAudioSettingsContent(contentArea.transform);
            var controlsPanel = CreateControlsSettingsContent(contentArea.transform);

            audioPanel.SetActive(false);
            controlsPanel.SetActive(false);

            // Wire up OptionsMenuController
            var so = new SerializedObject(optionsController);
            so.FindProperty("displayTabButton").objectReferenceValue = displayTab.GetComponent<Button>();
            so.FindProperty("audioTabButton").objectReferenceValue = audioTab.GetComponent<Button>();
            so.FindProperty("controlsTabButton").objectReferenceValue = controlsTab.GetComponent<Button>();
            so.FindProperty("displayPanel").objectReferenceValue = displayPanel;
            so.FindProperty("audioPanel").objectReferenceValue = audioPanel;
            so.FindProperty("controlsPanel").objectReferenceValue = controlsPanel;

            // Wire display settings
            var resDropdown = displayPanel.transform.Find("ResolutionRow/Dropdown");
            var modeDropdown = displayPanel.transform.Find("WindowModeRow/Dropdown");
            var aspectDropdown = displayPanel.transform.Find("AspectRatioRow/Dropdown");
            var applyBtn = displayPanel.transform.Find("ApplyButton");

            if (resDropdown != null) so.FindProperty("resolutionDropdown").objectReferenceValue = resDropdown.GetComponent<TMP_Dropdown>();
            if (modeDropdown != null) so.FindProperty("windowModeDropdown").objectReferenceValue = modeDropdown.GetComponent<TMP_Dropdown>();
            if (aspectDropdown != null) so.FindProperty("aspectRatioDropdown").objectReferenceValue = aspectDropdown.GetComponent<TMP_Dropdown>();
            if (applyBtn != null) so.FindProperty("applyDisplayButton").objectReferenceValue = applyBtn.GetComponent<Button>();

            // Wire audio settings
            var masterSlider = audioPanel.transform.Find("MasterRow/Slider");
            var musicSlider = audioPanel.transform.Find("MusicRow/Slider");
            var sfxSlider = audioPanel.transform.Find("SFXRow/Slider");
            var masterText = audioPanel.transform.Find("MasterRow/ValueText");
            var musicText = audioPanel.transform.Find("MusicRow/ValueText");
            var sfxText = audioPanel.transform.Find("SFXRow/ValueText");

            if (masterSlider != null) so.FindProperty("masterVolumeSlider").objectReferenceValue = masterSlider.GetComponent<Slider>();
            if (musicSlider != null) so.FindProperty("musicVolumeSlider").objectReferenceValue = musicSlider.GetComponent<Slider>();
            if (sfxSlider != null) so.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxSlider.GetComponent<Slider>();
            if (masterText != null) so.FindProperty("masterVolumeText").objectReferenceValue = masterText.GetComponent<TMP_Text>();
            if (musicText != null) so.FindProperty("musicVolumeText").objectReferenceValue = musicText.GetComponent<TMP_Text>();
            if (sfxText != null) so.FindProperty("sfxVolumeText").objectReferenceValue = sfxText.GetComponent<TMP_Text>();

            so.ApplyModifiedProperties();

            return optionsPanel;
        }

        private static GameObject CreateTabButton(Transform parent, string name, string label, int index)
        {
            var tab = new GameObject(name);
            tab.transform.SetParent(parent, false);

            var tabImage = tab.AddComponent<Image>();
            tabImage.color = index == 0 ? new Color(0.545f, 0f, 0f, 1f) : new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var tabButton = tab.AddComponent<Button>();
            var colors = tabButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.812f, 0.710f, 0.231f, 1f);
            tabButton.colors = colors;

            var tabRect = tab.GetComponent<RectTransform>();
            float tabWidth = 150f;
            float spacing = 10f;
            float startX = -(tabWidth + spacing);
            tabRect.anchorMin = new Vector2(0.5f, 0.5f);
            tabRect.anchorMax = new Vector2(0.5f, 0.5f);
            tabRect.pivot = new Vector2(0.5f, 0.5f);
            tabRect.anchoredPosition = new Vector2(startX + (tabWidth + spacing) * index, 0);
            tabRect.sizeDelta = new Vector2(tabWidth, 40);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(tab.transform, false);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = index == 0 ? new Color(0.961f, 0.961f, 0.863f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return tab;
        }

        private static GameObject CreateDisplaySettingsContent(Transform parent)
        {
            var panel = new GameObject("DisplayPanel");
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            CreateSettingsRow(panel.transform, "AspectRatioRow", "Aspect Ratio", 0, true);
            CreateSettingsRow(panel.transform, "ResolutionRow", "Resolution", 1, true);
            CreateSettingsRow(panel.transform, "WindowModeRow", "Window Mode", 2, true);

            // Apply button
            var applyBtn = CreateMenuButton(panel.transform, "ApplyButton", "Apply", Vector2.zero);
            var applyRect = applyBtn.GetComponent<RectTransform>();
            applyRect.anchorMin = new Vector2(0.5f, 0);
            applyRect.anchorMax = new Vector2(0.5f, 0);
            applyRect.pivot = new Vector2(0.5f, 0);
            applyRect.anchoredPosition = new Vector2(0, 20);
            applyRect.sizeDelta = new Vector2(200, 45);

            return panel;
        }

        private static GameObject CreateAudioSettingsContent(Transform parent)
        {
            var panel = new GameObject("AudioPanel");
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            CreateSettingsRow(panel.transform, "MasterRow", "Master Volume", 0, false);
            CreateSettingsRow(panel.transform, "MusicRow", "Music", 1, false);
            CreateSettingsRow(panel.transform, "SFXRow", "Sound Effects", 2, false);

            return panel;
        }

        private static GameObject CreateControlsSettingsContent(Transform parent)
        {
            var panel = new GameObject("ControlsPanel");
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Placeholder text
            var placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(panel.transform, false);
            var placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Control remapping coming soon...";
            placeholderText.fontSize = 24;
            placeholderText.alignment = TextAlignmentOptions.Center;
            placeholderText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            var placeholderRect = placeholder.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

            return panel;
        }

        private static void CreateSettingsRow(Transform parent, string name, string label, int rowIndex, bool isDropdown)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.pivot = new Vector2(0.5f, 1);
            rowRect.anchoredPosition = new Vector2(0, -20 - (rowIndex * 70));
            rowRect.sizeDelta = new Vector2(0, 60);

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(row.transform, false);
            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 24;
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.color = new Color(0.961f, 0.961f, 0.863f, 1f);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(0.4f, 1);
            labelRect.offsetMin = new Vector2(10, 0);
            labelRect.offsetMax = Vector2.zero;

            if (isDropdown)
            {
                // Dropdown
                var dropdownGO = new GameObject("Dropdown");
                dropdownGO.transform.SetParent(row.transform, false);

                var dropdownImage = dropdownGO.AddComponent<Image>();
                dropdownImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

                var dropdown = dropdownGO.AddComponent<TMP_Dropdown>();

                var dropdownRect = dropdownGO.GetComponent<RectTransform>();
                dropdownRect.anchorMin = new Vector2(0.45f, 0.1f);
                dropdownRect.anchorMax = new Vector2(1f, 0.9f);
                dropdownRect.offsetMin = Vector2.zero;
                dropdownRect.offsetMax = new Vector2(-10, 0);

                // Dropdown label
                var ddLabel = new GameObject("Label");
                ddLabel.transform.SetParent(dropdownGO.transform, false);
                var ddLabelText = ddLabel.AddComponent<TextMeshProUGUI>();
                ddLabelText.text = "Select...";
                ddLabelText.fontSize = 20;
                ddLabelText.alignment = TextAlignmentOptions.Left;
                ddLabelText.color = new Color(0.961f, 0.961f, 0.863f, 1f);
                var ddLabelRect = ddLabel.GetComponent<RectTransform>();
                ddLabelRect.anchorMin = Vector2.zero;
                ddLabelRect.anchorMax = Vector2.one;
                ddLabelRect.offsetMin = new Vector2(10, 0);
                ddLabelRect.offsetMax = new Vector2(-30, 0);

                dropdown.captionText = ddLabelText;

                // Template (basic)
                var template = new GameObject("Template");
                template.transform.SetParent(dropdownGO.transform, false);
                var templateImage = template.AddComponent<Image>();
                templateImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                var templateRect = template.GetComponent<RectTransform>();
                templateRect.anchorMin = new Vector2(0, 0);
                templateRect.anchorMax = new Vector2(1, 0);
                templateRect.pivot = new Vector2(0.5f, 1);
                templateRect.anchoredPosition = Vector2.zero;
                templateRect.sizeDelta = new Vector2(0, 150);
                template.SetActive(false);

                var viewport = new GameObject("Viewport");
                viewport.transform.SetParent(template.transform, false);
                viewport.AddComponent<RectMask2D>();
                var viewportRect = viewport.GetComponent<RectTransform>(); // RectMask2D auto-added it
                viewportRect.anchorMin = Vector2.zero;
                viewportRect.anchorMax = Vector2.one;
                viewportRect.offsetMin = Vector2.zero;
                viewportRect.offsetMax = Vector2.zero;

                var content = new GameObject("Content");
                content.transform.SetParent(viewport.transform, false);
                var contentRect = content.AddComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(0.5f, 1);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = new Vector2(0, 30);

                var item = new GameObject("Item");
                item.transform.SetParent(content.transform, false);
                var itemRect = item.AddComponent<RectTransform>();
                var itemToggle = item.AddComponent<Toggle>();
                itemRect.anchorMin = new Vector2(0, 0.5f);
                itemRect.anchorMax = new Vector2(1, 0.5f);
                itemRect.pivot = new Vector2(0.5f, 0.5f);
                itemRect.sizeDelta = new Vector2(0, 30);

                var itemLabel = new GameObject("Item Label");
                itemLabel.transform.SetParent(item.transform, false);
                var itemLabelText = itemLabel.AddComponent<TextMeshProUGUI>();
                itemLabelText.fontSize = 18;
                itemLabelText.alignment = TextAlignmentOptions.Left;
                itemLabelText.color = new Color(0.961f, 0.961f, 0.863f, 1f);
                var itemLabelRect = itemLabel.GetComponent<RectTransform>();
                itemLabelRect.anchorMin = Vector2.zero;
                itemLabelRect.anchorMax = Vector2.one;
                itemLabelRect.offsetMin = new Vector2(10, 0);
                itemLabelRect.offsetMax = Vector2.zero;

                dropdown.template = templateRect;
                dropdown.itemText = itemLabelText;
            }
            else
            {
                // Slider
                var sliderGO = new GameObject("Slider");
                sliderGO.transform.SetParent(row.transform, false);

                var slider = sliderGO.AddComponent<Slider>();
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.value = 1f;

                var sliderRect = sliderGO.GetComponent<RectTransform>();
                sliderRect.anchorMin = new Vector2(0.45f, 0.3f);
                sliderRect.anchorMax = new Vector2(0.85f, 0.7f);
                sliderRect.offsetMin = Vector2.zero;
                sliderRect.offsetMax = Vector2.zero;

                // Background
                var bg = new GameObject("Background");
                bg.transform.SetParent(sliderGO.transform, false);
                var bgImage = bg.AddComponent<Image>();
                bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                var bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                // Fill area
                var fillArea = new GameObject("Fill Area");
                fillArea.transform.SetParent(sliderGO.transform, false);
                var fillAreaRect = fillArea.AddComponent<RectTransform>();
                fillAreaRect.anchorMin = Vector2.zero;
                fillAreaRect.anchorMax = Vector2.one;
                fillAreaRect.offsetMin = Vector2.zero;
                fillAreaRect.offsetMax = Vector2.zero;

                var fill = new GameObject("Fill");
                fill.transform.SetParent(fillArea.transform, false);
                var fillImage = fill.AddComponent<Image>();
                fillImage.color = new Color(0.545f, 0f, 0f, 1f);
                var fillRect = fill.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;

                slider.fillRect = fillRect;

                // Handle area
                var handleArea = new GameObject("Handle Slide Area");
                handleArea.transform.SetParent(sliderGO.transform, false);
                var handleAreaRect = handleArea.AddComponent<RectTransform>();
                handleAreaRect.anchorMin = Vector2.zero;
                handleAreaRect.anchorMax = Vector2.one;
                handleAreaRect.offsetMin = Vector2.zero;
                handleAreaRect.offsetMax = Vector2.zero;

                var handle = new GameObject("Handle");
                handle.transform.SetParent(handleArea.transform, false);
                var handleImage = handle.AddComponent<Image>();
                handleImage.color = new Color(0.812f, 0.710f, 0.231f, 1f);
                var handleRect = handle.GetComponent<RectTransform>();
                handleRect.sizeDelta = new Vector2(20, 0);

                slider.handleRect = handleRect;
                slider.targetGraphic = handleImage;

                // Value text display
                var valueText = new GameObject("ValueText");
                valueText.transform.SetParent(row.transform, false);
                var valueTmp = valueText.AddComponent<TextMeshProUGUI>();
                valueTmp.text = "100%";
                valueTmp.fontSize = 20;
                valueTmp.alignment = TextAlignmentOptions.Right;
                valueTmp.color = new Color(0.961f, 0.961f, 0.863f, 1f);
                var valueRect = valueText.GetComponent<RectTransform>();
                valueRect.anchorMin = new Vector2(0.87f, 0);
                valueRect.anchorMax = new Vector2(1f, 1);
                valueRect.offsetMin = Vector2.zero;
                valueRect.offsetMax = new Vector2(-10, 0);
            }
        }

        private static GameObject CreateMenuButton(Transform parent, string name, string label, Vector2 position)
        {
            var buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);

            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.545f, 0f, 0f, 0.8f);

            var button = buttonGO.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.545f, 0f, 0f, 0.8f);
            colors.highlightedColor = new Color(0.812f, 0.710f, 0.231f, 1f);
            colors.pressedColor = new Color(0.4f, 0f, 0f, 1f);
            colors.selectedColor = new Color(0.812f, 0.710f, 0.231f, 1f);
            button.colors = colors;

            var buttonRect = buttonGO.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = position;
            buttonRect.sizeDelta = new Vector2(300, 50);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.961f, 0.961f, 0.863f, 1f);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return buttonGO;
        }

        private static GameObject CreateDisplaySettings()
        {
            var existing = Object.FindAnyObjectByType<DisplaySettings>();
            if (existing != null)
            {
                Debug.LogWarning("[UISetupWizard] DisplaySettings already exists in scene.");
                return existing.gameObject;
            }

            // Try to instantiate from prefab first
            string prefabPath = $"{SYSTEMS_PATH}/DisplaySettings.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            GameObject go;
            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = "DisplaySettings";
            }
            else
            {
                go = new GameObject("DisplaySettings");
                go.AddComponent<DisplaySettings>();

                // Save as prefab
                EnsureDirectoriesExist();
                SaveAsPrefab(go, SYSTEMS_PATH, "DisplaySettings");
            }

            Undo.RegisterCreatedObjectUndo(go, "Create DisplaySettings");
            Debug.Log("[UISetupWizard] Created DisplaySettings");
            return go;
        }

        private static GameObject CreateSkillTreeCanvas()
        {
            // Check for existing (including inactive)
            GameObject existing = FindSceneObject("SkillTree_Canvas");
            if (existing != null)
            {
                Debug.LogWarning("[UISetupWizard] SkillTree_Canvas already exists. Skipping.");
                return existing;
            }

            // Try to instantiate from prefab first
            string prefabPath = $"{CANVASES_PATH}/SkillTree_Canvas.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            GameObject go;
            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = "SkillTree_Canvas";
            }
            else
            {
                // Create new
                go = new GameObject("SkillTree_Canvas");

                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 150; // Between HUD and Pause

                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                go.AddComponent<GraphicRaycaster>();
                var canvasGroup = go.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0f; // Start hidden
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                // Add SkillTreeController
                var controller = go.AddComponent<SkillTreeController>();

                // Add SafeArea container for content
                var safeArea = new GameObject("SafeArea");
                safeArea.transform.SetParent(go.transform, false);
                var safeAreaRect = safeArea.AddComponent<RectTransform>();
                safeAreaRect.anchorMin = Vector2.zero;
                safeAreaRect.anchorMax = Vector2.one;
                safeAreaRect.offsetMin = Vector2.zero;
                safeAreaRect.offsetMax = Vector2.zero;
                safeArea.AddComponent<SafeAreaHandler>();

                CreateSkillTreeContent(safeArea.transform, controller);

                // Save as prefab
                EnsureDirectoriesExist();
                SaveAsPrefab(go, CANVASES_PATH, "SkillTree_Canvas");
            }

            Undo.RegisterCreatedObjectUndo(go, "Create Skill Tree Canvas");
            Debug.Log("[UISetupWizard] Created Skill Tree Canvas");
            return go;
        }

        private static GameObject CreateSkillManager()
        {
            // Check if SkillManager already exists
            var existing = Object.FindAnyObjectByType<SkillManager>();
            if (existing != null)
            {
                Debug.LogWarning("[UISetupWizard] SkillManager already exists in scene.");
                return existing.gameObject;
            }

            var go = new GameObject("SkillManager");
            var skillManager = go.AddComponent<SkillManager>();

            // Try to find and assign the Beginner job as default
            string beginnerJobPath = "Assets/_Project/ScriptableObjects/Skills/Jobs/Beginner.asset";
            var beginnerJob = AssetDatabase.LoadAssetAtPath<JobClassData>(beginnerJobPath);

            if (beginnerJob != null)
            {
                var so = new SerializedObject(skillManager);
                var defaultJobProp = so.FindProperty("defaultJob");
                if (defaultJobProp != null)
                {
                    defaultJobProp.objectReferenceValue = beginnerJob;
                    so.ApplyModifiedProperties();
                    Debug.Log("[UISetupWizard] Assigned Beginner job as default job");
                }
            }
            else
            {
                Debug.LogWarning("[UISetupWizard] Could not find Beginner.asset. Assign default job manually.");
            }

            Undo.RegisterCreatedObjectUndo(go, "Create SkillManager");
            Debug.Log("[UISetupWizard] Created SkillManager");
            return go;
        }

        private static void CreateSkillTreeContent(Transform parent, SkillTreeController controller)
        {
            // Dark overlay background
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(parent, false);
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0.051f, 0.051f, 0.051f, 0.9f);
            overlayImage.raycastTarget = true;
            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            // Main panel
            var mainPanel = new GameObject("SkillTreePanel");
            mainPanel.transform.SetParent(parent, false);
            var panelImage = mainPanel.AddComponent<Image>();
            panelImage.color = new Color(0.102f, 0.102f, 0.102f, 0.98f);
            var skillTreePanel = mainPanel.AddComponent<SkillTreePanel>();

            var mainPanelRect = mainPanel.GetComponent<RectTransform>();
            mainPanelRect.anchorMin = new Vector2(0.1f, 0.1f);
            mainPanelRect.anchorMax = new Vector2(0.9f, 0.9f);
            mainPanelRect.offsetMin = Vector2.zero;
            mainPanelRect.offsetMax = Vector2.zero;

            // Header
            var header = new GameObject("Header");
            header.transform.SetParent(mainPanel.transform, false);
            var headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 80);

            var headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.08f, 0.08f, 0.08f, 1f);

            // Title
            var title = new GameObject("Title");
            title.transform.SetParent(header.transform, false);
            var titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = "SKILL TREE";
            titleText.fontSize = 36;
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.color = new Color(0.812f, 0.710f, 0.231f, 1f);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.offsetMin = new Vector2(20, 0);
            titleRect.offsetMax = Vector2.zero;

            // SP Display
            var spDisplay = new GameObject("SPDisplay");
            spDisplay.transform.SetParent(header.transform, false);
            var spText = spDisplay.AddComponent<TextMeshProUGUI>();
            spText.text = "SP: 0";
            spText.fontSize = 28;
            spText.alignment = TextAlignmentOptions.Right;
            spText.color = new Color(0f, 0.808f, 0.820f, 1f);
            var spRect = spDisplay.GetComponent<RectTransform>();
            spRect.anchorMin = new Vector2(0.5f, 0);
            spRect.anchorMax = new Vector2(1, 1);
            spRect.offsetMin = Vector2.zero;
            spRect.offsetMax = new Vector2(-80, 0);

            // Close button
            var closeBtn = CreateMenuButton(header.transform, "CloseButton", "X", Vector2.zero);
            var closeBtnRect = closeBtn.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1, 0.5f);
            closeBtnRect.anchorMax = new Vector2(1, 0.5f);
            closeBtnRect.pivot = new Vector2(1, 0.5f);
            closeBtnRect.anchoredPosition = new Vector2(-10, 0);
            closeBtnRect.sizeDelta = new Vector2(50, 50);

            // Scroll area for skill nodes
            var scrollArea = new GameObject("ScrollArea");
            scrollArea.transform.SetParent(mainPanel.transform, false);
            var scrollRect = scrollArea.AddComponent<ScrollRect>();
            var scrollAreaRect = scrollArea.GetComponent<RectTransform>();
            scrollAreaRect.anchorMin = new Vector2(0, 0);
            scrollAreaRect.anchorMax = new Vector2(0.7f, 1);
            scrollAreaRect.offsetMin = new Vector2(10, 10);
            scrollAreaRect.offsetMax = new Vector2(0, -90);

            // Viewport
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollArea.transform, false);
            viewport.AddComponent<RectMask2D>();
            var viewportRect = viewport.GetComponent<RectTransform>(); // RectMask2D auto-added it
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            // Content for skill nodes
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 800);

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = true;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Nodes container
            var nodesContainer = new GameObject("NodesContainer");
            nodesContainer.transform.SetParent(content.transform, false);
            var nodesRect = nodesContainer.AddComponent<RectTransform>();
            nodesRect.anchorMin = Vector2.zero;
            nodesRect.anchorMax = Vector2.one;
            nodesRect.offsetMin = Vector2.zero;
            nodesRect.offsetMax = Vector2.zero;

            // Connections container
            var connectionsContainer = new GameObject("ConnectionsContainer");
            connectionsContainer.transform.SetParent(content.transform, false);
            var connRect = connectionsContainer.AddComponent<RectTransform>();
            connRect.anchorMin = Vector2.zero;
            connRect.anchorMax = Vector2.one;
            connRect.offsetMin = Vector2.zero;
            connRect.offsetMax = Vector2.zero;
            connectionsContainer.transform.SetAsFirstSibling(); // Behind nodes

            // Skill info panel (right side)
            var infoPanel = new GameObject("SkillInfoPanel");
            infoPanel.transform.SetParent(mainPanel.transform, false);
            var infoPanelImage = infoPanel.AddComponent<Image>();
            infoPanelImage.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            var infoPanelRect = infoPanel.GetComponent<RectTransform>();
            infoPanelRect.anchorMin = new Vector2(0.7f, 0);
            infoPanelRect.anchorMax = new Vector2(1, 1);
            infoPanelRect.offsetMin = new Vector2(10, 10);
            infoPanelRect.offsetMax = new Vector2(-10, -90);

            // Skill name
            var skillName = new GameObject("SkillName");
            skillName.transform.SetParent(infoPanel.transform, false);
            var skillNameText = skillName.AddComponent<TextMeshProUGUI>();
            skillNameText.text = "Select a Skill";
            skillNameText.fontSize = 28;
            skillNameText.alignment = TextAlignmentOptions.TopLeft;
            skillNameText.color = new Color(0.812f, 0.710f, 0.231f, 1f);
            var skillNameRect = skillName.GetComponent<RectTransform>();
            skillNameRect.anchorMin = new Vector2(0, 1);
            skillNameRect.anchorMax = new Vector2(1, 1);
            skillNameRect.pivot = new Vector2(0.5f, 1);
            skillNameRect.anchoredPosition = new Vector2(0, -10);
            skillNameRect.sizeDelta = new Vector2(-20, 40);

            // Skill description
            var skillDesc = new GameObject("SkillDescription");
            skillDesc.transform.SetParent(infoPanel.transform, false);
            var skillDescText = skillDesc.AddComponent<TextMeshProUGUI>();
            skillDescText.text = "Click on a skill node to view its details.";
            skillDescText.fontSize = 18;
            skillDescText.alignment = TextAlignmentOptions.TopLeft;
            skillDescText.color = new Color(0.831f, 0.769f, 0.659f, 1f);
            var skillDescRect = skillDesc.GetComponent<RectTransform>();
            skillDescRect.anchorMin = new Vector2(0, 0.4f);
            skillDescRect.anchorMax = new Vector2(1, 1);
            skillDescRect.offsetMin = new Vector2(10, 0);
            skillDescRect.offsetMax = new Vector2(-10, -60);

            // Learn button
            var learnBtn = CreateMenuButton(infoPanel.transform, "LearnButton", "Learn", Vector2.zero);
            var learnBtnRect = learnBtn.GetComponent<RectTransform>();
            learnBtnRect.anchorMin = new Vector2(0.5f, 0);
            learnBtnRect.anchorMax = new Vector2(0.5f, 0);
            learnBtnRect.pivot = new Vector2(0.5f, 0);
            learnBtnRect.anchoredPosition = new Vector2(0, 20);
            learnBtnRect.sizeDelta = new Vector2(200, 50);

            infoPanel.SetActive(false); // Hidden until skill selected

            // Placeholder text for empty tree
            var placeholder = new GameObject("PlaceholderText");
            placeholder.transform.SetParent(content.transform, false);
            var placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "No skill tree loaded.\nAssign a JobClassData with a SkillTree to the SkillManager.";
            placeholderText.fontSize = 24;
            placeholderText.alignment = TextAlignmentOptions.Center;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            var placeholderRect = placeholder.GetComponent<RectTransform>();
            placeholderRect.anchorMin = new Vector2(0.5f, 0.5f);
            placeholderRect.anchorMax = new Vector2(0.5f, 0.5f);
            placeholderRect.pivot = new Vector2(0.5f, 0.5f);
            placeholderRect.sizeDelta = new Vector2(600, 200);

            // Wire up SkillTreeController
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                var canvasProp = so.FindProperty("skillTreeCanvas");
                var canvasGroupProp = so.FindProperty("skillTreeCanvasGroup");
                var panelProp = so.FindProperty("skillTreePanel");
                var closeBtnProp = so.FindProperty("closeButton");

                if (canvasProp != null) canvasProp.objectReferenceValue = controller.GetComponent<Canvas>();
                if (canvasGroupProp != null) canvasGroupProp.objectReferenceValue = controller.GetComponent<CanvasGroup>();
                if (panelProp != null) panelProp.objectReferenceValue = skillTreePanel;
                if (closeBtnProp != null) closeBtnProp.objectReferenceValue = closeBtn.GetComponent<Button>();
                so.ApplyModifiedProperties();
            }

            // Wire up SkillTreePanel
            if (skillTreePanel != null)
            {
                // Create or load skill node prefab
                var skillNodePrefab = CreateOrLoadSkillNodePrefab();

                var panelSO = new SerializedObject(skillTreePanel);
                SetPropertyIfExists(panelSO, "scrollRect", scrollRect);
                SetPropertyIfExists(panelSO, "contentContainer", contentRect);
                SetPropertyIfExists(panelSO, "nodesContainer", nodesRect);
                SetPropertyIfExists(panelSO, "connectionsContainer", connRect);
                SetPropertyIfExists(panelSO, "spDisplayText", spText);
                SetPropertyIfExists(panelSO, "skillInfoPanel", infoPanel);
                SetPropertyIfExists(panelSO, "skillNameText", skillNameText);
                SetPropertyIfExists(panelSO, "skillDescriptionText", skillDescText);
                SetPropertyIfExists(panelSO, "learnButton", learnBtn.GetComponent<Button>());
                SetPropertyIfExists(panelSO, "learnButtonText", learnBtn.GetComponentInChildren<TMP_Text>());
                SetPropertyIfExists(panelSO, "skillNodePrefab", skillNodePrefab);
                panelSO.ApplyModifiedProperties();
            }
        }

        private static GameObject CreateOrLoadSkillNodePrefab()
        {
            string prefabPath = $"{COMPONENTS_PATH}/SkillNode.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab != null)
            {
                return prefab;
            }

            // Create new prefab
            var nodeGO = new GameObject("SkillNode");

            // Add RectTransform
            var rect = nodeGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80, 80);

            // Background
            var bg = new GameObject("Background");
            bg.transform.SetParent(nodeGO.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Frame
            var frame = new GameObject("Frame");
            frame.transform.SetParent(nodeGO.transform, false);
            var frameImage = frame.AddComponent<Image>();
            frameImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            var frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = new Vector2(-3, -3);
            frameRect.offsetMax = new Vector2(3, 3);
            frame.transform.SetAsFirstSibling(); // Behind background

            // Icon
            var icon = new GameObject("Icon");
            icon.transform.SetParent(nodeGO.transform, false);
            var iconImage = icon.AddComponent<Image>();
            iconImage.color = Color.white;
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            // Lock overlay
            var lockOverlay = new GameObject("LockOverlay");
            lockOverlay.transform.SetParent(nodeGO.transform, false);
            var lockImage = lockOverlay.AddComponent<Image>();
            lockImage.color = new Color(0f, 0f, 0f, 0.7f);
            var lockRect = lockOverlay.GetComponent<RectTransform>();
            lockRect.anchorMin = Vector2.zero;
            lockRect.anchorMax = Vector2.one;
            lockRect.offsetMin = Vector2.zero;
            lockRect.offsetMax = Vector2.zero;

            // Lock icon - simple X mark that works with any font
            var lockIcon = new GameObject("LockIcon");
            lockIcon.transform.SetParent(lockOverlay.transform, false);
            var lockText = lockIcon.AddComponent<TextMeshProUGUI>();
            lockText.text = "X";
            lockText.fontSize = 32;
            lockText.fontStyle = TMPro.FontStyles.Bold;
            lockText.alignment = TextAlignmentOptions.Center;
            lockText.color = new Color(0.6f, 0.2f, 0.2f, 0.8f);
            var lockIconRect = lockIcon.GetComponent<RectTransform>();
            lockIconRect.anchorMin = Vector2.zero;
            lockIconRect.anchorMax = Vector2.one;
            lockIconRect.offsetMin = Vector2.zero;
            lockIconRect.offsetMax = Vector2.zero;

            // Level text
            var levelGO = new GameObject("LevelText");
            levelGO.transform.SetParent(nodeGO.transform, false);
            var levelText = levelGO.AddComponent<TextMeshProUGUI>();
            levelText.text = "0/10";
            levelText.fontSize = 14;
            levelText.alignment = TextAlignmentOptions.Bottom;
            levelText.color = Color.white;
            var levelRect = levelGO.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0, 0);
            levelRect.anchorMax = new Vector2(1, 0.3f);
            levelRect.offsetMin = new Vector2(2, 2);
            levelRect.offsetMax = new Vector2(-2, 0);

            // SP Cost badge
            var spBadge = new GameObject("SPCostBadge");
            spBadge.transform.SetParent(nodeGO.transform, false);
            var spBadgeImage = spBadge.AddComponent<Image>();
            spBadgeImage.color = new Color(0.8f, 0.6f, 0.1f, 1f);
            var spBadgeRect = spBadge.GetComponent<RectTransform>();
            spBadgeRect.anchorMin = new Vector2(1, 1);
            spBadgeRect.anchorMax = new Vector2(1, 1);
            spBadgeRect.pivot = new Vector2(1, 1);
            spBadgeRect.anchoredPosition = new Vector2(5, 5);
            spBadgeRect.sizeDelta = new Vector2(24, 20);

            var spCostGO = new GameObject("SPCostText");
            spCostGO.transform.SetParent(spBadge.transform, false);
            var spCostText = spCostGO.AddComponent<TextMeshProUGUI>();
            spCostText.text = "1";
            spCostText.fontSize = 12;
            spCostText.alignment = TextAlignmentOptions.Center;
            spCostText.color = Color.white;
            var spCostRect = spCostGO.GetComponent<RectTransform>();
            spCostRect.anchorMin = Vector2.zero;
            spCostRect.anchorMax = Vector2.one;
            spCostRect.offsetMin = Vector2.zero;
            spCostRect.offsetMax = Vector2.zero;

            // Add SkillNodeUI component
            var nodeUI = nodeGO.AddComponent<SkillNodeUI>();

            // Wire up references via SerializedObject
            var nodeSO = new SerializedObject(nodeUI);
            SetPropertyIfExists(nodeSO, "iconImage", iconImage);
            SetPropertyIfExists(nodeSO, "frameImage", frameImage);
            SetPropertyIfExists(nodeSO, "backgroundImage", bgImage);
            SetPropertyIfExists(nodeSO, "lockOverlay", lockImage);
            SetPropertyIfExists(nodeSO, "levelText", levelText);
            SetPropertyIfExists(nodeSO, "spCostBadge", spBadge);
            SetPropertyIfExists(nodeSO, "spCostText", spCostText);
            nodeSO.ApplyModifiedProperties();

            // Save as prefab
            EnsureDirectoriesExist();
            string fullPath = $"{COMPONENTS_PATH}/SkillNode.prefab";
            prefab = PrefabUtility.SaveAsPrefabAsset(nodeGO, fullPath);
            Object.DestroyImmediate(nodeGO);

            Debug.Log("[UISetupWizard] Created SkillNode prefab");
            return prefab;
        }

        private static void AddAdaptiveScalerToSelection()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogWarning("[UISetupWizard] No GameObject selected.");
                return;
            }

            var canvas = Selection.activeGameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[UISetupWizard] Selected object is not a Canvas.");
                return;
            }

            // Remove existing CanvasScaler settings (AdaptiveCanvasScaler will handle it)
            var adaptiveScaler = Selection.activeGameObject.GetComponent<AdaptiveCanvasScaler>();
            if (adaptiveScaler == null)
            {
                adaptiveScaler = Selection.activeGameObject.AddComponent<AdaptiveCanvasScaler>();
                Undo.RegisterCreatedObjectUndo(adaptiveScaler, "Add AdaptiveCanvasScaler");
                Debug.Log("[UISetupWizard] Added AdaptiveCanvasScaler to " + Selection.activeGameObject.name);
            }
            else
            {
                Debug.LogWarning("[UISetupWizard] AdaptiveCanvasScaler already exists on " + Selection.activeGameObject.name);
            }
        }

        private static void AddSafeAreaHandlerToSelection()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogWarning("[UISetupWizard] No GameObject selected.");
                return;
            }

            var rectTransform = Selection.activeGameObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogWarning("[UISetupWizard] Selected object does not have a RectTransform.");
                return;
            }

            var safeAreaHandler = Selection.activeGameObject.GetComponent<SafeAreaHandler>();
            if (safeAreaHandler == null)
            {
                safeAreaHandler = Selection.activeGameObject.AddComponent<SafeAreaHandler>();
                Undo.RegisterCreatedObjectUndo(safeAreaHandler, "Add SafeAreaHandler");
                Debug.Log("[UISetupWizard] Added SafeAreaHandler to " + Selection.activeGameObject.name);
            }
            else
            {
                Debug.LogWarning("[UISetupWizard] SafeAreaHandler already exists on " + Selection.activeGameObject.name);
            }
        }

        #endregion

        #region Main Menu Creation

        // Track objects for cleanup on failure
        private static System.Collections.Generic.List<GameObject> createdObjects = new System.Collections.Generic.List<GameObject>();

        private static void CreateMainMenuScene()
        {
            // Check if scene already exists
            string scenePath = "Assets/Scenes/MainMenu.unity";
            if (File.Exists(scenePath))
            {
                if (!EditorUtility.DisplayDialog("Scene Exists",
                    "MainMenu.unity already exists. Do you want to overwrite it?",
                    "Overwrite", "Cancel"))
                {
                    return;
                }
            }

            // Clear tracking list
            createdObjects.Clear();

            UnityEngine.SceneManagement.Scene newScene = default;

            try
            {
                // Create new scene
                newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                    UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                    UnityEditor.SceneManagement.NewSceneMode.Single);

                // Setup camera
                var mainCamera = Object.FindAnyObjectByType<Camera>();
                if (mainCamera != null)
                {
                    mainCamera.orthographic = true;
                    mainCamera.orthographicSize = 5;
                    mainCamera.backgroundColor = new Color(0.051f, 0.051f, 0.051f, 1f);
                }

                // Create EventSystem with Input System
                var eventSystemGO = new GameObject("EventSystem");
                createdObjects.Add(eventSystemGO);
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

                // Create SceneLoader
                var sceneLoaderGO = new GameObject("SceneLoader");
                createdObjects.Add(sceneLoaderGO);
                sceneLoaderGO.AddComponent<SceneLoader>();

                // Create Main Menu Canvas
                var menuCanvas = CreateMainMenuCanvasInternal();
                if (menuCanvas != null)
                {
                    createdObjects.Add(menuCanvas);
                }

                // Ensure Scenes folder exists
                if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                {
                    AssetDatabase.CreateFolder("Assets", "Scenes");
                }

                // Save scene
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, scenePath);

                Debug.Log("[UISetupWizard] Created MainMenu scene at " + scenePath);

                // Prompt to add to build settings
                if (EditorUtility.DisplayDialog("Add to Build Settings?",
                    "Would you like to add MainMenu to Build Settings as scene index 0?",
                    "Yes", "No"))
                {
                    AddSceneToBuildSettings(scenePath, 0);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UISetupWizard] Failed to create MainMenu scene: {e.Message}\n{e.StackTrace}");

                // Cleanup created objects
                CleanupCreatedObjects();

                EditorUtility.DisplayDialog("Error",
                    $"Failed to create MainMenu scene:\n\n{e.Message}\n\nCheck console for details.",
                    "OK");
            }
            finally
            {
                createdObjects.Clear();
            }
        }

        private static void CleanupCreatedObjects()
        {
            foreach (var obj in createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            createdObjects.Clear();
            Debug.Log("[UISetupWizard] Cleaned up partially created objects");
        }

        private static void AddSceneToBuildSettings(string scenePath, int desiredIndex)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // Check if scene already in build settings
            int existingIndex = scenes.FindIndex(s => s.path == scenePath);
            if (existingIndex >= 0)
            {
                // Move to desired position if needed
                if (existingIndex != desiredIndex && desiredIndex < scenes.Count)
                {
                    var scene = scenes[existingIndex];
                    scenes.RemoveAt(existingIndex);
                    scenes.Insert(desiredIndex, scene);
                    EditorBuildSettings.scenes = scenes.ToArray();
                    Debug.Log($"[UISetupWizard] Moved {scenePath} to build index {desiredIndex}");
                }
                else
                {
                    Debug.Log($"[UISetupWizard] {scenePath} already at index {existingIndex}");
                }
            }
            else
            {
                // Add at desired position
                var newScene = new EditorBuildSettingsScene(scenePath, true);
                scenes.Insert(System.Math.Min(desiredIndex, scenes.Count), newScene);
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log($"[UISetupWizard] Added {scenePath} to build settings at index {desiredIndex}");
            }
        }

        private static GameObject CreateMainMenuCanvas()
        {
            createdObjects.Clear();

            try
            {
                var result = CreateMainMenuCanvasInternal();
                return result;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UISetupWizard] Failed to create MainMenu canvas: {e.Message}\n{e.StackTrace}");

                CleanupCreatedObjects();

                EditorUtility.DisplayDialog("Error",
                    $"Failed to create MainMenu canvas:\n\n{e.Message}\n\nCheck console for details.",
                    "OK");

                return null;
            }
            finally
            {
                createdObjects.Clear();
            }
        }

        private static GameObject CreateMainMenuCanvasInternal()
        {
            // Check for existing
            GameObject existing = FindSceneObject("MainMenu_Canvas");
            if (existing != null)
            {
                Debug.LogWarning("[UISetupWizard] MainMenu_Canvas already exists. Skipping.");
                return existing;
            }

            // Try to instantiate from prefab first
            string prefabPath = $"{CANVASES_PATH}/MainMenu_Canvas.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            GameObject go;
            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = "MainMenu_Canvas";
            }
            else
            {
                // Create new
                go = new GameObject("MainMenu_Canvas");
                createdObjects.Add(go);

                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                go.AddComponent<GraphicRaycaster>();
                go.AddComponent<CanvasGroup>();

                // Add MainMenuController
                var controller = go.AddComponent<MainMenuController>();

                // Add SafeArea container for content
                var safeArea = new GameObject("SafeArea");
                safeArea.transform.SetParent(go.transform, false);
                var safeAreaRect = safeArea.AddComponent<RectTransform>();
                safeAreaRect.anchorMin = Vector2.zero;
                safeAreaRect.anchorMax = Vector2.one;
                safeAreaRect.offsetMin = Vector2.zero;
                safeAreaRect.offsetMax = Vector2.zero;
                safeArea.AddComponent<SafeAreaHandler>();

                CreateMainMenuContent(safeArea.transform, controller);

                // Save as prefab
                EnsureDirectoriesExist();
                SaveAsPrefab(go, CANVASES_PATH, "MainMenu_Canvas");
            }

            Undo.RegisterCreatedObjectUndo(go, "Create Main Menu Canvas");
            Debug.Log("[UISetupWizard] Created Main Menu Canvas");
            return go;
        }

        private static void CreateMainMenuContent(Transform parent, MainMenuController controller)
        {
            // Background overlay
            var overlay = new GameObject("Background");
            overlay.transform.SetParent(parent, false);
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0.051f, 0.051f, 0.051f, 1f);
            overlayImage.raycastTarget = true;
            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            // Title Group
            var titleGroup = new GameObject("TitleGroup");
            titleGroup.transform.SetParent(parent, false);
            var titleRect = titleGroup.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -80);
            titleRect.sizeDelta = new Vector2(0, 120);

            var titleText = new GameObject("TitleText");
            titleText.transform.SetParent(titleGroup.transform, false);
            var titleTMP = titleText.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "ProjectNameHere";
            titleTMP.fontSize = 72;
            titleTMP.fontStyle = TMPro.FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = new Color(0.812f, 0.710f, 0.231f, 1f); // Aged gold
            titleTMP.overflowMode = TextOverflowModes.Overflow;
            titleTMP.textWrappingMode = TextWrappingModes.NoWrap;
            var titleTextRect = titleText.GetComponent<RectTransform>();
            titleTextRect.anchorMin = Vector2.zero;
            titleTextRect.anchorMax = Vector2.one;
            titleTextRect.offsetMin = Vector2.zero;
            titleTextRect.offsetMax = Vector2.zero;

            // === MAIN MENU PANEL ===
            var mainMenuPanel = new GameObject("MainMenuPanel");
            mainMenuPanel.transform.SetParent(parent, false);
            var mainMenuRect = mainMenuPanel.AddComponent<RectTransform>();
            mainMenuRect.anchorMin = new Vector2(0.5f, 0.5f);
            mainMenuRect.anchorMax = new Vector2(0.5f, 0.5f);
            mainMenuRect.pivot = new Vector2(0.5f, 0.5f);
            mainMenuRect.anchoredPosition = Vector2.zero;
            mainMenuRect.sizeDelta = new Vector2(400, 250);

            var startBtn = CreateMenuButton(mainMenuPanel.transform, "StartGameButton", "Start Game", new Vector2(0, 60));
            var optionsBtn = CreateMenuButton(mainMenuPanel.transform, "OptionsButton", "Options", new Vector2(0, 0));
            var quitBtn = CreateMenuButton(mainMenuPanel.transform, "QuitButton", "Quit", new Vector2(0, -60));

            // === SAVE SELECTION PANEL ===
            var savePanel = CreateSaveSelectionPanel(parent);
            savePanel.SetActive(false);

            // === OPTIONS PANEL ===
            var optionsPanel = CreateOptionsPanel(parent);
            optionsPanel.SetActive(false);

            // === OVERWRITE CONFIRM PANEL ===
            var overwritePanel = CreateOverwriteConfirmPanel(parent);
            overwritePanel.SetActive(false);

            // === DELETE CONFIRM PANEL ===
            var deletePanel = CreateDeleteConfirmPanel(parent);
            deletePanel.SetActive(false);

            // Wire up MainMenuController
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                SetPropertyIfExists(so, "mainMenuPanel", mainMenuPanel);
                SetPropertyIfExists(so, "saveSelectionPanel", savePanel);
                SetPropertyIfExists(so, "optionsPanel", optionsPanel);
                SetPropertyIfExists(so, "overwriteConfirmPanel", overwritePanel);
                SetPropertyIfExists(so, "deleteConfirmPanel", deletePanel);
                SetPropertyIfExists(so, "startGameButton", startBtn.GetComponent<Button>());
                SetPropertyIfExists(so, "optionsButton", optionsBtn.GetComponent<Button>());
                SetPropertyIfExists(so, "quitButton", quitBtn.GetComponent<Button>());
                SetPropertyIfExists(so, "mainMenuFirstSelected", startBtn);

                // Wire save selection elements
                var saveSlotContainer = savePanel.transform.Find("SaveSlotContainer");
                var newGameBtn = savePanel.transform.Find("ButtonRow/NewGameButton");
                var backBtn = savePanel.transform.Find("ButtonRow/BackButton");

                if (saveSlotContainer != null)
                    SetPropertyIfExists(so, "saveSlotContainer", saveSlotContainer);
                if (newGameBtn != null)
                    SetPropertyIfExists(so, "newGameButton", newGameBtn.GetComponent<Button>());
                if (backBtn != null)
                    SetPropertyIfExists(so, "backFromSaveButton", backBtn.GetComponent<Button>());

                // Wire overwrite elements
                var overwriteWarning = overwritePanel.transform.Find("WarningText");
                var overwriteInfo = overwritePanel.transform.Find("SlotInfoText");
                var confirmOverwrite = overwritePanel.transform.Find("ButtonRow/ConfirmButton");
                var cancelOverwrite = overwritePanel.transform.Find("ButtonRow/CancelButton");

                if (overwriteWarning != null)
                    SetPropertyIfExists(so, "overwriteWarningText", overwriteWarning.GetComponent<TMP_Text>());
                if (overwriteInfo != null)
                    SetPropertyIfExists(so, "overwriteSlotInfoText", overwriteInfo.GetComponent<TMP_Text>());
                if (confirmOverwrite != null)
                    SetPropertyIfExists(so, "confirmOverwriteButton", confirmOverwrite.GetComponent<Button>());
                if (cancelOverwrite != null)
                    SetPropertyIfExists(so, "cancelOverwriteButton", cancelOverwrite.GetComponent<Button>());

                // Wire delete elements
                var deleteWarning = deletePanel.transform.Find("WarningText");
                var deleteInfo = deletePanel.transform.Find("SlotInfoText");
                var confirmDelete = deletePanel.transform.Find("ButtonRow/ConfirmButton");
                var cancelDelete = deletePanel.transform.Find("ButtonRow/CancelButton");

                if (deleteWarning != null)
                    SetPropertyIfExists(so, "deleteWarningText", deleteWarning.GetComponent<TMP_Text>());
                if (deleteInfo != null)
                    SetPropertyIfExists(so, "deleteSlotInfoText", deleteInfo.GetComponent<TMP_Text>());
                if (confirmDelete != null)
                    SetPropertyIfExists(so, "confirmDeleteButton", confirmDelete.GetComponent<Button>());
                if (cancelDelete != null)
                    SetPropertyIfExists(so, "cancelDeleteButton", cancelDelete.GetComponent<Button>());

                // Wire options back button
                var optionsBackBtn = optionsPanel.transform.Find("Header/BackButton");
                if (optionsBackBtn != null)
                    SetPropertyIfExists(so, "optionsBackButton", optionsBackBtn.GetComponent<Button>());

                so.ApplyModifiedProperties();
            }
        }

        private static GameObject CreateSaveSelectionPanel(Transform parent)
        {
            var panel = new GameObject("SaveSelectionPanel");
            panel.transform.SetParent(parent, false);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.102f, 0.102f, 0.102f, 0.95f);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(600, 500);

            // Header
            var header = new GameObject("Header");
            header.transform.SetParent(panel.transform, false);
            var headerText = header.AddComponent<TextMeshProUGUI>();
            headerText.text = "Select Save Slot";
            headerText.fontSize = 36;
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.color = new Color(0.812f, 0.710f, 0.231f, 1f);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = new Vector2(0, -20);
            headerRect.sizeDelta = new Vector2(0, 50);

            // Save Slot Container
            var container = new GameObject("SaveSlotContainer");
            container.transform.SetParent(panel.transform, false);
            var containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.offsetMin = new Vector2(20, 80);
            containerRect.offsetMax = new Vector2(-20, -80);

            var verticalLayout = container.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 10;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = true;
            verticalLayout.padding = new RectOffset(10, 10, 10, 10);

            // Create 5 save slots
            for (int i = 0; i < 5; i++)
            {
                CreateSaveSlotUI(container.transform, i);
            }

            // Button row
            var buttonRow = new GameObject("ButtonRow");
            buttonRow.transform.SetParent(panel.transform, false);
            var buttonRowRect = buttonRow.AddComponent<RectTransform>();
            buttonRowRect.anchorMin = new Vector2(0, 0);
            buttonRowRect.anchorMax = new Vector2(1, 0);
            buttonRowRect.pivot = new Vector2(0.5f, 0);
            buttonRowRect.anchoredPosition = new Vector2(0, 20);
            buttonRowRect.sizeDelta = new Vector2(0, 50);

            var horizontalLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.spacing = 20;
            horizontalLayout.childAlignment = TextAnchor.MiddleCenter;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = false;

            var newGameBtn = CreateMenuButton(buttonRow.transform, "NewGameButton", "New Game", Vector2.zero);
            var newGameLayout = newGameBtn.AddComponent<LayoutElement>();
            newGameLayout.preferredWidth = 180;
            newGameLayout.preferredHeight = 45;

            var backBtn = CreateMenuButton(buttonRow.transform, "BackButton", "Back", Vector2.zero);
            var backLayout = backBtn.AddComponent<LayoutElement>();
            backLayout.preferredWidth = 180;
            backLayout.preferredHeight = 45;

            return panel;
        }

        private static void CreateSaveSlotUI(Transform parent, int slotIndex)
        {
            var slotGO = new GameObject($"SaveSlot_{slotIndex + 1}");
            slotGO.transform.SetParent(parent, false);

            var slotUI = slotGO.AddComponent<SaveSlotUI>();

            var layoutElement = slotGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 70;

            var bgImage = slotGO.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            var button = slotGO.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.812f, 0.710f, 0.231f, 1f);
            colors.pressedColor = new Color(0.6f, 0.5f, 0.2f, 1f);
            colors.selectedColor = new Color(0.812f, 0.710f, 0.231f, 1f);
            button.colors = colors;

            // Slot number
            var slotNumber = new GameObject("SlotNumber");
            slotNumber.transform.SetParent(slotGO.transform, false);
            var slotNumberText = slotNumber.AddComponent<TextMeshProUGUI>();
            slotNumberText.text = $"Slot {slotIndex + 1}";
            slotNumberText.fontSize = 22;
            slotNumberText.alignment = TextAlignmentOptions.Left;
            slotNumberText.color = new Color(0.812f, 0.710f, 0.231f, 1f);
            var slotNumberRect = slotNumber.GetComponent<RectTransform>();
            slotNumberRect.anchorMin = new Vector2(0, 0.5f);
            slotNumberRect.anchorMax = new Vector2(0.2f, 0.5f);
            slotNumberRect.pivot = new Vector2(0, 0.5f);
            slotNumberRect.anchoredPosition = new Vector2(15, 0);
            slotNumberRect.sizeDelta = new Vector2(0, 30);

            // Empty state group
            var emptyState = new GameObject("EmptyState");
            emptyState.transform.SetParent(slotGO.transform, false);
            var emptyRect = emptyState.AddComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0.2f, 0);
            emptyRect.anchorMax = new Vector2(1, 1);
            emptyRect.offsetMin = Vector2.zero;
            emptyRect.offsetMax = Vector2.zero;

            var statusText = new GameObject("StatusText");
            statusText.transform.SetParent(emptyState.transform, false);
            var statusTMP = statusText.AddComponent<TextMeshProUGUI>();
            statusTMP.text = "Empty Slot";
            statusTMP.fontSize = 20;
            statusTMP.alignment = TextAlignmentOptions.Left;
            statusTMP.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            var statusRect = statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = Vector2.one;
            statusRect.offsetMin = new Vector2(10, 0);
            statusRect.offsetMax = Vector2.zero;

            // Filled state group
            var filledState = new GameObject("FilledState");
            filledState.transform.SetParent(slotGO.transform, false);
            var filledRect = filledState.AddComponent<RectTransform>();
            filledRect.anchorMin = new Vector2(0.2f, 0);
            filledRect.anchorMax = new Vector2(0.9f, 1);
            filledRect.offsetMin = Vector2.zero;
            filledRect.offsetMax = Vector2.zero;
            filledState.SetActive(false);

            // Character name text (top left of filled area)
            var charNameText = new GameObject("CharacterNameText");
            charNameText.transform.SetParent(filledState.transform, false);
            var charNameTMP = charNameText.AddComponent<TextMeshProUGUI>();
            charNameTMP.text = "Hero";
            charNameTMP.fontSize = 20;
            charNameTMP.fontStyle = FontStyles.Bold;
            charNameTMP.alignment = TextAlignmentOptions.Left;
            charNameTMP.color = new Color(0.961f, 0.961f, 0.863f, 1f);
            var charNameRect = charNameText.GetComponent<RectTransform>();
            charNameRect.anchorMin = new Vector2(0, 0.5f);
            charNameRect.anchorMax = new Vector2(0.35f, 1);
            charNameRect.offsetMin = new Vector2(10, 2);
            charNameRect.offsetMax = Vector2.zero;

            // Level text (next to character name)
            var levelText = new GameObject("LevelText");
            levelText.transform.SetParent(filledState.transform, false);
            var levelTMP = levelText.AddComponent<TextMeshProUGUI>();
            levelTMP.text = "Lv. 1";
            levelTMP.fontSize = 18;
            levelTMP.alignment = TextAlignmentOptions.Left;
            levelTMP.color = new Color(0.812f, 0.710f, 0.231f, 1f);
            var levelRect = levelText.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.35f, 0.5f);
            levelRect.anchorMax = new Vector2(0.55f, 1);
            levelRect.offsetMin = new Vector2(5, 2);
            levelRect.offsetMax = Vector2.zero;

            // Wave text (top right)
            var waveText = new GameObject("WaveText");
            waveText.transform.SetParent(filledState.transform, false);
            var waveTMP = waveText.AddComponent<TextMeshProUGUI>();
            waveTMP.text = "Wave 1";
            waveTMP.fontSize = 16;
            waveTMP.alignment = TextAlignmentOptions.Right;
            waveTMP.color = new Color(0.545f, 0.545f, 0.7f, 1f);
            var waveRect = waveText.GetComponent<RectTransform>();
            waveRect.anchorMin = new Vector2(0.7f, 0.5f);
            waveRect.anchorMax = new Vector2(1, 1);
            waveRect.offsetMin = new Vector2(0, 2);
            waveRect.offsetMax = new Vector2(-5, 0);

            // Play time text (bottom left)
            var playTimeText = new GameObject("PlayTimeText");
            playTimeText.transform.SetParent(filledState.transform, false);
            var playTimeTMP = playTimeText.AddComponent<TextMeshProUGUI>();
            playTimeTMP.text = "0h 0m";
            playTimeTMP.fontSize = 14;
            playTimeTMP.alignment = TextAlignmentOptions.Left;
            playTimeTMP.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            var playTimeRect = playTimeText.GetComponent<RectTransform>();
            playTimeRect.anchorMin = new Vector2(0, 0);
            playTimeRect.anchorMax = new Vector2(0.35f, 0.5f);
            playTimeRect.offsetMin = new Vector2(10, 0);
            playTimeRect.offsetMax = new Vector2(0, -2);

            // Date text (bottom right)
            var dateText = new GameObject("DateText");
            dateText.transform.SetParent(filledState.transform, false);
            var dateTMP = dateText.AddComponent<TextMeshProUGUI>();
            dateTMP.text = "Jan 01, 2024";
            dateTMP.fontSize = 14;
            dateTMP.alignment = TextAlignmentOptions.Right;
            dateTMP.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            var dateRect = dateText.GetComponent<RectTransform>();
            dateRect.anchorMin = new Vector2(0.5f, 0);
            dateRect.anchorMax = new Vector2(1, 0.5f);
            dateRect.offsetMin = new Vector2(0, 0);
            dateRect.offsetMax = new Vector2(-5, -2);

            // Delete button
            var deleteBtn = CreateMenuButton(slotGO.transform, "DeleteButton", "X", Vector2.zero);
            var deleteBtnRect = deleteBtn.GetComponent<RectTransform>();
            deleteBtnRect.anchorMin = new Vector2(1, 0.5f);
            deleteBtnRect.anchorMax = new Vector2(1, 0.5f);
            deleteBtnRect.pivot = new Vector2(1, 0.5f);
            deleteBtnRect.anchoredPosition = new Vector2(-10, 0);
            deleteBtnRect.sizeDelta = new Vector2(40, 40);
            deleteBtn.SetActive(false);

            // Wire up SaveSlotUI
            var so = new SerializedObject(slotUI);
            SetPropertyIfExists(so, "slotButton", button);
            SetPropertyIfExists(so, "slotNumberText", slotNumberText);
            SetPropertyIfExists(so, "statusText", statusTMP);
            SetPropertyIfExists(so, "emptyStateGroup", emptyState);
            SetPropertyIfExists(so, "filledStateGroup", filledState);
            SetPropertyIfExists(so, "characterNameText", charNameTMP);
            SetPropertyIfExists(so, "levelText", levelTMP);
            SetPropertyIfExists(so, "waveText", waveTMP);
            SetPropertyIfExists(so, "playTimeText", playTimeTMP);
            SetPropertyIfExists(so, "dateText", dateTMP);
            SetPropertyIfExists(so, "deleteButton", deleteBtn.GetComponent<Button>());
            SetPropertyIfExists(so, "backgroundImage", bgImage);
            so.ApplyModifiedProperties();

            // Initialize with slot index
            slotUI.Initialize(slotIndex);
        }

        private static GameObject CreateOverwriteConfirmPanel(Transform parent)
        {
            var panel = new GameObject("OverwriteConfirmPanel");
            panel.transform.SetParent(parent, false);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.98f);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(500, 250);

            // Warning text
            var warningText = new GameObject("WarningText");
            warningText.transform.SetParent(panel.transform, false);
            var warningTMP = warningText.AddComponent<TextMeshProUGUI>();
            warningTMP.text = "This will overwrite your existing save!";
            warningTMP.fontSize = 28;
            warningTMP.alignment = TextAlignmentOptions.Center;
            warningTMP.color = new Color(0.9f, 0.3f, 0.3f, 1f);
            var warningRect = warningText.GetComponent<RectTransform>();
            warningRect.anchorMin = new Vector2(0, 1);
            warningRect.anchorMax = new Vector2(1, 1);
            warningRect.pivot = new Vector2(0.5f, 1);
            warningRect.anchoredPosition = new Vector2(0, -30);
            warningRect.sizeDelta = new Vector2(-40, 40);

            // Slot info text
            var slotInfoText = new GameObject("SlotInfoText");
            slotInfoText.transform.SetParent(panel.transform, false);
            var slotInfoTMP = slotInfoText.AddComponent<TextMeshProUGUI>();
            slotInfoTMP.text = "Slot 1: Level 15 - 2h 34m playtime";
            slotInfoTMP.fontSize = 22;
            slotInfoTMP.alignment = TextAlignmentOptions.Center;
            slotInfoTMP.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            var slotInfoRect = slotInfoText.GetComponent<RectTransform>();
            slotInfoRect.anchorMin = new Vector2(0, 0.4f);
            slotInfoRect.anchorMax = new Vector2(1, 0.6f);
            slotInfoRect.offsetMin = new Vector2(20, 0);
            slotInfoRect.offsetMax = new Vector2(-20, 0);

            // Button row
            var buttonRow = new GameObject("ButtonRow");
            buttonRow.transform.SetParent(panel.transform, false);
            var buttonRowRect = buttonRow.AddComponent<RectTransform>();
            buttonRowRect.anchorMin = new Vector2(0, 0);
            buttonRowRect.anchorMax = new Vector2(1, 0);
            buttonRowRect.pivot = new Vector2(0.5f, 0);
            buttonRowRect.anchoredPosition = new Vector2(0, 30);
            buttonRowRect.sizeDelta = new Vector2(0, 50);

            var horizontalLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.spacing = 30;
            horizontalLayout.childAlignment = TextAnchor.MiddleCenter;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = false;

            var confirmBtn = CreateMenuButton(buttonRow.transform, "ConfirmButton", "Overwrite", Vector2.zero);
            var confirmLayout = confirmBtn.AddComponent<LayoutElement>();
            confirmLayout.preferredWidth = 160;
            confirmLayout.preferredHeight = 45;

            var cancelBtn = CreateMenuButton(buttonRow.transform, "CancelButton", "Cancel", Vector2.zero);
            var cancelLayout = cancelBtn.AddComponent<LayoutElement>();
            cancelLayout.preferredWidth = 160;
            cancelLayout.preferredHeight = 45;

            return panel;
        }

        private static GameObject CreateDeleteConfirmPanel(Transform parent)
        {
            var panel = new GameObject("DeleteConfirmPanel");
            panel.transform.SetParent(parent, false);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.98f);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(500, 250);

            // Warning text
            var warningText = new GameObject("WarningText");
            warningText.transform.SetParent(panel.transform, false);
            var warningTMP = warningText.AddComponent<TextMeshProUGUI>();
            warningTMP.text = "Delete this save permanently?";
            warningTMP.fontSize = 28;
            warningTMP.alignment = TextAlignmentOptions.Center;
            warningTMP.color = new Color(0.9f, 0.3f, 0.3f, 1f);
            var warningRect = warningText.GetComponent<RectTransform>();
            warningRect.anchorMin = new Vector2(0, 1);
            warningRect.anchorMax = new Vector2(1, 1);
            warningRect.pivot = new Vector2(0.5f, 1);
            warningRect.anchoredPosition = new Vector2(0, -30);
            warningRect.sizeDelta = new Vector2(-40, 40);

            // Slot info text
            var slotInfoText = new GameObject("SlotInfoText");
            slotInfoText.transform.SetParent(panel.transform, false);
            var slotInfoTMP = slotInfoText.AddComponent<TextMeshProUGUI>();
            slotInfoTMP.text = "Slot 1: Level 15 - 2h 34m playtime";
            slotInfoTMP.fontSize = 22;
            slotInfoTMP.alignment = TextAlignmentOptions.Center;
            slotInfoTMP.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            var slotInfoRect = slotInfoText.GetComponent<RectTransform>();
            slotInfoRect.anchorMin = new Vector2(0, 0.4f);
            slotInfoRect.anchorMax = new Vector2(1, 0.6f);
            slotInfoRect.offsetMin = new Vector2(20, 0);
            slotInfoRect.offsetMax = new Vector2(-20, 0);

            // Button row
            var buttonRow = new GameObject("ButtonRow");
            buttonRow.transform.SetParent(panel.transform, false);
            var buttonRowRect = buttonRow.AddComponent<RectTransform>();
            buttonRowRect.anchorMin = new Vector2(0, 0);
            buttonRowRect.anchorMax = new Vector2(1, 0);
            buttonRowRect.pivot = new Vector2(0.5f, 0);
            buttonRowRect.anchoredPosition = new Vector2(0, 30);
            buttonRowRect.sizeDelta = new Vector2(0, 50);

            var horizontalLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.spacing = 30;
            horizontalLayout.childAlignment = TextAnchor.MiddleCenter;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = false;

            var confirmBtn = CreateMenuButton(buttonRow.transform, "ConfirmButton", "Delete", Vector2.zero);
            var confirmLayout = confirmBtn.AddComponent<LayoutElement>();
            confirmLayout.preferredWidth = 160;
            confirmLayout.preferredHeight = 45;

            var cancelBtn = CreateMenuButton(buttonRow.transform, "CancelButton", "Cancel", Vector2.zero);
            var cancelLayout = cancelBtn.AddComponent<LayoutElement>();
            cancelLayout.preferredWidth = 160;
            cancelLayout.preferredHeight = 45;

            return panel;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Safely gets or adds a RectTransform to a GameObject.
        /// </summary>
        private static RectTransform GetOrAddRectTransform(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = go.AddComponent<RectTransform>();
            }
            if (rect == null)
            {
                throw new System.InvalidOperationException($"Failed to get or add RectTransform to {go.name}");
            }
            return rect;
        }

        /// <summary>
        /// Configures a RectTransform with common anchor/offset settings.
        /// </summary>
        private static void ConfigureRectTransform(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            if (rect == null)
            {
                throw new System.ArgumentNullException(nameof(rect), "RectTransform is null");
            }
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetPropertyIfExists(SerializedObject so, string propName, UnityEngine.Object value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static GameObject FindSceneObject(string name)
        {
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.scene.name != null && obj.name == name)
                {
                    return obj;
                }
            }
            return null;
        }

        private static void CleanupDuplicates()
        {
            int removed = 0;

            string[] uiObjectNames = new string[]
            {
                "HUD_Canvas",
                "PauseMenu_Canvas",
                "SkillTree_Canvas",
                "UIManager",
                "HealthGroup"
            };

            foreach (string name in uiObjectNames)
            {
                var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                bool foundFirst = false;

                foreach (var obj in allObjects)
                {
                    if (obj.scene.name == null) continue;

                    if (obj.name == name)
                    {
                        if (!foundFirst)
                        {
                            foundFirst = true;
                        }
                        else
                        {
                            Undo.DestroyObjectImmediate(obj);
                            removed++;
                            Debug.Log($"[UISetupWizard] Removed duplicate: {name}");
                        }
                    }
                }
            }

            // Cleanup duplicate components
            var uiManagers = Object.FindObjectsByType<UIManager>(FindObjectsSortMode.None);
            if (uiManagers.Length > 1)
            {
                for (int i = 1; i < uiManagers.Length; i++)
                {
                    Undo.DestroyObjectImmediate(uiManagers[i].gameObject);
                    removed++;
                }
            }

            var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (eventSystems.Length > 1)
            {
                for (int i = 1; i < eventSystems.Length; i++)
                {
                    Undo.DestroyObjectImmediate(eventSystems[i].gameObject);
                    removed++;
                }
            }

            Debug.Log(removed > 0
                ? $"[UISetupWizard] Cleanup complete. Removed {removed} duplicate objects."
                : "[UISetupWizard] No duplicates found.");
        }

        #endregion
    }
}
#endif
