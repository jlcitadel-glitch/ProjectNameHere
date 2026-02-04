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

            if (GUILayout.Button("Create Test Controller Only"))
            {
                CreateTestController();
            }

            if (GUILayout.Button("Create Display Settings Only"))
            {
                CreateDisplaySettings();
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

            // Create and wire up
            var uiManager = CreateUIManager();
            var hudCanvas = CreateHUDCanvas();
            var pauseCanvas = CreatePauseMenu();
            CreateTestController();

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
            SaveSceneObjectAsPrefab("UITestController", DEBUG_PATH);

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
            InstantiatePrefabIfNotInScene($"{DEBUG_PATH}/UITestController.prefab", "UITestController");

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
            // Health placeholder (top-left)
            var healthGroup = new GameObject("HealthGroup");
            healthGroup.transform.SetParent(parent, false);

            var healthRect = healthGroup.AddComponent<RectTransform>();
            healthRect.anchorMin = new Vector2(0, 1);
            healthRect.anchorMax = new Vector2(0, 1);
            healthRect.pivot = new Vector2(0, 1);
            healthRect.anchoredPosition = new Vector2(20, -20);
            healthRect.sizeDelta = new Vector2(300, 50);

            var healthText = new GameObject("HealthText");
            healthText.transform.SetParent(healthGroup.transform, false);
            var tmp = healthText.AddComponent<TextMeshProUGUI>();
            tmp.text = "♦♦♦♦♦ Health";
            tmp.fontSize = 24;
            tmp.color = new Color(0.545f, 0f, 0f, 1f); // Deep crimson
            var textRect = healthText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
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
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
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
                ddLabelText.alignment = TextAlignmentOptions.MidlineLeft;
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
                var viewportRect = viewport.AddComponent<RectTransform>();
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
                var itemToggle = item.AddComponent<Toggle>();
                var itemRect = item.AddComponent<RectTransform>();
                itemRect.anchorMin = new Vector2(0, 0.5f);
                itemRect.anchorMax = new Vector2(1, 0.5f);
                itemRect.pivot = new Vector2(0.5f, 0.5f);
                itemRect.sizeDelta = new Vector2(0, 30);

                var itemLabel = new GameObject("Item Label");
                itemLabel.transform.SetParent(item.transform, false);
                var itemLabelText = itemLabel.AddComponent<TextMeshProUGUI>();
                itemLabelText.fontSize = 18;
                itemLabelText.alignment = TextAlignmentOptions.MidlineLeft;
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
                valueTmp.alignment = TextAlignmentOptions.MidlineRight;
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

        private static GameObject CreateTestController()
        {
            var existing = Object.FindAnyObjectByType<UITestController>();
            if (existing != null)
            {
                Debug.LogWarning("[UISetupWizard] UITestController already exists in scene.");
                return existing.gameObject;
            }

            // Try to instantiate from prefab first
            string prefabPath = $"{DEBUG_PATH}/UITestController.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            GameObject go;
            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = "UITestController";
            }
            else
            {
                go = new GameObject("UITestController");
                go.AddComponent<UITestController>();

                // Save as prefab
                EnsureDirectoriesExist();
                SaveAsPrefab(go, DEBUG_PATH, "UITestController");
            }

            Undo.RegisterCreatedObjectUndo(go, "Create UITestController");
            Debug.Log("[UISetupWizard] Created UITestController");
            return go;
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

        #region Utilities

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
                "UITestController",
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

            var testControllers = Object.FindObjectsByType<UITestController>(FindObjectsSortMode.None);
            if (testControllers.Length > 1)
            {
                for (int i = 1; i < testControllers.Length; i++)
                {
                    Undo.DestroyObjectImmediate(testControllers[i].gameObject);
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
