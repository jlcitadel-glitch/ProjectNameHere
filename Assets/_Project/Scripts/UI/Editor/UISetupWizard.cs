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
            // Dark overlay background
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(parent, false);
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0.051f, 0.051f, 0.051f, 0.85f);
            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            // Center panel
            var panel = new GameObject("Panel");
            panel.transform.SetParent(parent, false);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.102f, 0.102f, 0.102f, 0.95f);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(400, 500);

            // Title
            var title = new GameObject("Title");
            title.transform.SetParent(panel.transform, false);
            var titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = "PAUSED";
            titleText.fontSize = 48;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.812f, 0.710f, 0.231f, 1f);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -30);
            titleRect.sizeDelta = new Vector2(0, 60);

            CreateMenuButton(panel.transform, "ResumeButton", "Resume", new Vector2(0, 40));
            CreateMenuButton(panel.transform, "OptionsButton", "Options", new Vector2(0, -30));
            CreateMenuButton(panel.transform, "QuitButton", "Quit to Title", new Vector2(0, -100));
        }

        private static void CreateMenuButton(Transform parent, string name, string label, Vector2 position)
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

            if (name == "ResumeButton")
            {
                button.onClick.AddListener(() => UIManager.Instance?.Resume());
            }
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
