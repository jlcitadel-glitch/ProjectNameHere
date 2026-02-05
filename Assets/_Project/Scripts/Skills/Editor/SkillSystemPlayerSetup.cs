#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using ProjectName.UI;

/// <summary>
/// Editor script to set up Player with skill components and create SkillTree UI Canvas.
/// </summary>
public class SkillSystemPlayerSetup : EditorWindow
{
    private const string PLAYER_PREFAB_PATH = "Assets/_Project/Prefabs/Player/Player.prefab";
    private const string INPUT_ACTIONS_PATH = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";
    private const string SKILL_TREE_CANVAS_PATH = "Assets/_Project/Prefabs/UI/Skills/SkillTreeCanvas.prefab";
    private const string SKILL_NODE_PREFAB_PATH = "Assets/_Project/Prefabs/UI/Skills/SkillNode.prefab";

    [MenuItem("Tools/ProjectName/Setup Skill System on Player")]
    public static void ShowWindow()
    {
        GetWindow<SkillSystemPlayerSetup>("Skill System Player Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Skill System Player Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This will:\n" +
            "1. Add PlayerSkillController to the Player prefab\n" +
            "2. Wire up skill input actions (1-6 keys)\n" +
            "3. Create SkillTree Canvas prefab\n" +
            "4. Create SkillNode prefab\n" +
            "5. Add SkillTree Canvas to the scene",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Setup Everything", GUILayout.Height(40)))
        {
            SetupPlayerPrefab();
            CreateSkillNodePrefab();
            CreateSkillTreeCanvasPrefab();
            AddSkillTreeCanvasToScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Complete", "Skill system setup on Player complete!", "OK");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Individual Steps", EditorStyles.boldLabel);

        if (GUILayout.Button("1. Setup Player Prefab Only"))
        {
            SetupPlayerPrefab();
        }

        if (GUILayout.Button("2. Create SkillNode Prefab"))
        {
            CreateSkillNodePrefab();
        }

        if (GUILayout.Button("3. Create SkillTree Canvas Prefab"))
        {
            CreateSkillTreeCanvasPrefab();
        }

        if (GUILayout.Button("4. Add SkillTree Canvas to Scene"))
        {
            AddSkillTreeCanvasToScene();
        }
    }

    private void SetupPlayerPrefab()
    {
        // Load player prefab
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH);
        if (playerPrefab == null)
        {
            // Try to find it
            string[] guids = AssetDatabase.FindAssets("t:Prefab Player");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Player.prefab"))
                {
                    playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    break;
                }
            }
        }

        if (playerPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Player prefab not found!", "OK");
            return;
        }

        // Open prefab for editing
        string prefabPath = AssetDatabase.GetAssetPath(playerPrefab);
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        // Add PlayerSkillController if not present
        var skillController = prefabRoot.GetComponent<PlayerSkillController>();
        if (skillController == null)
        {
            skillController = prefabRoot.AddComponent<PlayerSkillController>();
            Debug.Log("[SkillSystemSetup] Added PlayerSkillController to Player");
        }

        // Wire up input action references
        var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);
        if (inputActions != null)
        {
            var so = new SerializedObject(skillController);

            // Find the Player action map
            var playerMap = inputActions.FindActionMap("Player");
            if (playerMap != null)
            {
                SetInputActionReference(so, "skill1Action", inputActions, "Player/Skill1");
                SetInputActionReference(so, "skill2Action", inputActions, "Player/Skill2");
                SetInputActionReference(so, "skill3Action", inputActions, "Player/Skill3");
                SetInputActionReference(so, "skill4Action", inputActions, "Player/Skill4");
                SetInputActionReference(so, "skill5Action", inputActions, "Player/Skill5");
                SetInputActionReference(so, "skill6Action", inputActions, "Player/Skill6");
            }

            so.ApplyModifiedProperties();
            Debug.Log("[SkillSystemSetup] Wired up skill input actions");
        }

        // Wire up ManaSystem and HealthSystem references
        var manaSystem = prefabRoot.GetComponent<ManaSystem>();
        var healthSystem = prefabRoot.GetComponent<HealthSystem>();

        var controllerSO = new SerializedObject(skillController);

        var manaProp = controllerSO.FindProperty("manaSystem");
        if (manaProp != null && manaSystem != null)
        {
            manaProp.objectReferenceValue = manaSystem;
        }

        var healthProp = controllerSO.FindProperty("healthSystem");
        if (healthProp != null && healthSystem != null)
        {
            healthProp.objectReferenceValue = healthSystem;
        }

        controllerSO.ApplyModifiedProperties();

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Debug.Log("[SkillSystemSetup] Player prefab setup complete");
    }

    private void SetInputActionReference(SerializedObject so, string propertyName, InputActionAsset asset, string actionPath)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null) return;

        // Find or create InputActionReference
        var action = asset.FindAction(actionPath);
        if (action == null) return;

        // Create a new InputActionReference asset
        string refPath = $"Assets/_Project/Settings/Input/References/{propertyName}.asset";

        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Settings/Input/References"))
        {
            AssetDatabase.CreateFolder("Assets/_Project/Settings/Input", "References");
        }

        var actionRef = AssetDatabase.LoadAssetAtPath<InputActionReference>(refPath);
        if (actionRef == null)
        {
            actionRef = ScriptableObject.CreateInstance<InputActionReference>();
            actionRef.Set(action);
            AssetDatabase.CreateAsset(actionRef, refPath);
        }
        else
        {
            actionRef.Set(action);
            EditorUtility.SetDirty(actionRef);
        }

        prop.objectReferenceValue = actionRef;
    }

    private void CreateSkillNodePrefab()
    {
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/UI/Skills"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/UI"))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "UI");
            }
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs/UI", "Skills");
        }

        // Check if prefab already exists
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(SKILL_NODE_PREFAB_PATH);
        if (existing != null)
        {
            Debug.Log("[SkillSystemSetup] SkillNode prefab already exists");
            return;
        }

        // Create skill node GameObject
        var nodeGO = new GameObject("SkillNode");

        // Add RectTransform
        var rectTransform = nodeGO.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(80, 80);

        // Add CanvasGroup for fading
        nodeGO.AddComponent<CanvasGroup>();

        // Background image
        var bgImage = nodeGO.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        // Frame (child)
        var frameGO = new GameObject("Frame");
        frameGO.transform.SetParent(nodeGO.transform, false);
        var frameRect = frameGO.AddComponent<RectTransform>();
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;
        var frameImage = frameGO.AddComponent<Image>();
        frameImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        frameImage.raycastTarget = false;

        // Icon (child)
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(nodeGO.transform, false);
        var iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        var iconImage = iconGO.AddComponent<Image>();
        iconImage.raycastTarget = false;

        // Lock overlay (child)
        var lockGO = new GameObject("LockOverlay");
        lockGO.transform.SetParent(nodeGO.transform, false);
        var lockRect = lockGO.AddComponent<RectTransform>();
        lockRect.anchorMin = Vector2.zero;
        lockRect.anchorMax = Vector2.one;
        lockRect.offsetMin = Vector2.zero;
        lockRect.offsetMax = Vector2.zero;
        var lockImage = lockGO.AddComponent<Image>();
        lockImage.color = new Color(0, 0, 0, 0.7f);
        lockImage.raycastTarget = false;

        // Level text (child)
        var levelGO = new GameObject("LevelText");
        levelGO.transform.SetParent(nodeGO.transform, false);
        var levelRect = levelGO.AddComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0, 0);
        levelRect.anchorMax = new Vector2(1, 0.25f);
        levelRect.offsetMin = Vector2.zero;
        levelRect.offsetMax = Vector2.zero;
        var levelText = levelGO.AddComponent<TextMeshProUGUI>();
        levelText.text = "0/20";
        levelText.fontSize = 12;
        levelText.alignment = TextAlignmentOptions.Center;
        levelText.raycastTarget = false;

        // SP Cost badge (child)
        var spBadgeGO = new GameObject("SPCostBadge");
        spBadgeGO.transform.SetParent(nodeGO.transform, false);
        var spBadgeRect = spBadgeGO.AddComponent<RectTransform>();
        spBadgeRect.anchorMin = new Vector2(1, 1);
        spBadgeRect.anchorMax = new Vector2(1, 1);
        spBadgeRect.pivot = new Vector2(1, 1);
        spBadgeRect.sizeDelta = new Vector2(24, 24);
        spBadgeRect.anchoredPosition = new Vector2(5, 5);
        var spBadgeImage = spBadgeGO.AddComponent<Image>();
        spBadgeImage.color = new Color(0.8f, 0.7f, 0.2f, 1f);
        spBadgeImage.raycastTarget = false;

        // SP Cost text (child of badge)
        var spTextGO = new GameObject("SPText");
        spTextGO.transform.SetParent(spBadgeGO.transform, false);
        var spTextRect = spTextGO.AddComponent<RectTransform>();
        spTextRect.anchorMin = Vector2.zero;
        spTextRect.anchorMax = Vector2.one;
        spTextRect.offsetMin = Vector2.zero;
        spTextRect.offsetMax = Vector2.zero;
        var spText = spTextGO.AddComponent<TextMeshProUGUI>();
        spText.text = "1";
        spText.fontSize = 14;
        spText.alignment = TextAlignmentOptions.Center;
        spText.color = Color.black;
        spText.raycastTarget = false;

        // Add SkillNodeUI component and wire references
        var nodeUI = nodeGO.AddComponent<SkillNodeUI>();
        var nodeUISO = new SerializedObject(nodeUI);

        nodeUISO.FindProperty("iconImage").objectReferenceValue = iconImage;
        nodeUISO.FindProperty("frameImage").objectReferenceValue = frameImage;
        nodeUISO.FindProperty("backgroundImage").objectReferenceValue = bgImage;
        nodeUISO.FindProperty("lockOverlay").objectReferenceValue = lockImage;
        nodeUISO.FindProperty("levelText").objectReferenceValue = levelText;
        nodeUISO.FindProperty("spCostBadge").objectReferenceValue = spBadgeGO;
        nodeUISO.FindProperty("spCostText").objectReferenceValue = spText;

        nodeUISO.ApplyModifiedProperties();

        // Add Button component for interaction
        var button = nodeGO.AddComponent<Button>();
        button.targetGraphic = bgImage;

        // Save as prefab
        PrefabUtility.SaveAsPrefabAsset(nodeGO, SKILL_NODE_PREFAB_PATH);
        DestroyImmediate(nodeGO);

        Debug.Log("[SkillSystemSetup] Created SkillNode prefab");
    }

    private void CreateSkillTreeCanvasPrefab()
    {
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/UI/Skills"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/UI"))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "UI");
            }
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs/UI", "Skills");
        }

        // Check if prefab already exists
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(SKILL_TREE_CANVAS_PATH);
        if (existing != null)
        {
            Debug.Log("[SkillSystemSetup] SkillTree Canvas prefab already exists");
            return;
        }

        // Create canvas
        var canvasGO = new GameObject("SkillTree_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        var canvasGroup = canvasGO.AddComponent<CanvasGroup>();

        // Add SkillTreeController
        var controller = canvasGO.AddComponent<SkillTreeController>();

        // Wire up input action for opening skill tree
        var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);
        if (inputActions != null)
        {
            var controllerSO = new SerializedObject(controller);

            // Create InputActionReference for OpenSkillTree
            string refPath = "Assets/_Project/Settings/Input/References/openSkillTreeAction.asset";
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Settings/Input/References"))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Settings/Input", "References");
            }

            var action = inputActions.FindAction("Player/OpenSkillTree");
            if (action != null)
            {
                var actionRef = AssetDatabase.LoadAssetAtPath<InputActionReference>(refPath);
                if (actionRef == null)
                {
                    actionRef = ScriptableObject.CreateInstance<InputActionReference>();
                    actionRef.Set(action);
                    AssetDatabase.CreateAsset(actionRef, refPath);
                }
                else
                {
                    actionRef.Set(action);
                    EditorUtility.SetDirty(actionRef);
                }

                var openActionProp = controllerSO.FindProperty("openSkillTreeAction");
                if (openActionProp != null)
                {
                    openActionProp.objectReferenceValue = actionRef;
                }
            }

            controllerSO.ApplyModifiedProperties();
        }

        // Background panel
        var bgPanel = new GameObject("Background");
        bgPanel.transform.SetParent(canvasGO.transform, false);
        var bgRect = bgPanel.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

        // Header panel
        var headerPanel = new GameObject("Header");
        headerPanel.transform.SetParent(canvasGO.transform, false);
        var headerRect = headerPanel.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.sizeDelta = new Vector2(0, 80);
        headerRect.anchoredPosition = Vector2.zero;
        var headerImage = headerPanel.AddComponent<Image>();
        headerImage.color = new Color(0.1f, 0.1f, 0.15f, 1f);

        // Job title text
        var jobTitleGO = new GameObject("JobTitle");
        jobTitleGO.transform.SetParent(headerPanel.transform, false);
        var jobTitleRect = jobTitleGO.AddComponent<RectTransform>();
        jobTitleRect.anchorMin = new Vector2(0, 0);
        jobTitleRect.anchorMax = new Vector2(0.5f, 1);
        jobTitleRect.offsetMin = new Vector2(20, 10);
        jobTitleRect.offsetMax = new Vector2(0, -10);
        var jobTitleText = jobTitleGO.AddComponent<TextMeshProUGUI>();
        jobTitleText.text = "Beginner";
        jobTitleText.fontSize = 32;
        jobTitleText.alignment = TextAlignmentOptions.Left;

        // SP display
        var spDisplayGO = new GameObject("SPDisplay");
        spDisplayGO.transform.SetParent(headerPanel.transform, false);
        var spDisplayRect = spDisplayGO.AddComponent<RectTransform>();
        spDisplayRect.anchorMin = new Vector2(0.7f, 0);
        spDisplayRect.anchorMax = new Vector2(1, 1);
        spDisplayRect.offsetMin = new Vector2(0, 10);
        spDisplayRect.offsetMax = new Vector2(-20, -10);
        var spDisplayText = spDisplayGO.AddComponent<TextMeshProUGUI>();
        spDisplayText.text = "SP: 0";
        spDisplayText.fontSize = 24;
        spDisplayText.alignment = TextAlignmentOptions.Right;

        // Tree view scroll rect
        var scrollViewGO = new GameObject("TreeView");
        scrollViewGO.transform.SetParent(canvasGO.transform, false);
        var scrollRect = scrollViewGO.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(0.7f, 1);
        scrollRect.offsetMin = new Vector2(20, 20);
        scrollRect.offsetMax = new Vector2(-10, -100);
        var scrollView = scrollViewGO.AddComponent<ScrollRect>();
        scrollViewGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.3f);
        scrollViewGO.AddComponent<Mask>().showMaskGraphic = true;

        // Content container
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollViewGO.transform, false);
        var contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(800, 600);
        scrollView.content = contentRect;

        // Connections container
        var connectionsGO = new GameObject("Connections");
        connectionsGO.transform.SetParent(contentGO.transform, false);
        var connectionsRect = connectionsGO.AddComponent<RectTransform>();
        connectionsRect.anchorMin = Vector2.zero;
        connectionsRect.anchorMax = Vector2.one;
        connectionsRect.offsetMin = Vector2.zero;
        connectionsRect.offsetMax = Vector2.zero;

        // Nodes container
        var nodesGO = new GameObject("Nodes");
        nodesGO.transform.SetParent(contentGO.transform, false);
        var nodesRect = nodesGO.AddComponent<RectTransform>();
        nodesRect.anchorMin = Vector2.zero;
        nodesRect.anchorMax = Vector2.one;
        nodesRect.offsetMin = Vector2.zero;
        nodesRect.offsetMax = Vector2.zero;

        // Skill info panel
        var infoPanelGO = new GameObject("SkillInfoPanel");
        infoPanelGO.transform.SetParent(canvasGO.transform, false);
        var infoPanelRect = infoPanelGO.AddComponent<RectTransform>();
        infoPanelRect.anchorMin = new Vector2(0.7f, 0);
        infoPanelRect.anchorMax = new Vector2(1, 1);
        infoPanelRect.offsetMin = new Vector2(10, 20);
        infoPanelRect.offsetMax = new Vector2(-20, -100);
        var infoPanelImage = infoPanelGO.AddComponent<Image>();
        infoPanelImage.color = new Color(0.1f, 0.1f, 0.12f, 1f);
        var infoPanelLayout = infoPanelGO.AddComponent<VerticalLayoutGroup>();
        infoPanelLayout.padding = new RectOffset(15, 15, 15, 15);
        infoPanelLayout.spacing = 10;
        infoPanelLayout.childControlWidth = true;
        infoPanelLayout.childControlHeight = false;

        // Skill name in info panel
        var skillNameGO = new GameObject("SkillName");
        skillNameGO.transform.SetParent(infoPanelGO.transform, false);
        var skillNameLE = skillNameGO.AddComponent<LayoutElement>();
        skillNameLE.preferredHeight = 40;
        var skillNameText = skillNameGO.AddComponent<TextMeshProUGUI>();
        skillNameText.text = "Select a Skill";
        skillNameText.fontSize = 28;
        skillNameText.alignment = TextAlignmentOptions.Center;

        // Skill description
        var skillDescGO = new GameObject("SkillDescription");
        skillDescGO.transform.SetParent(infoPanelGO.transform, false);
        var skillDescLE = skillDescGO.AddComponent<LayoutElement>();
        skillDescLE.preferredHeight = 100;
        skillDescLE.flexibleHeight = 1;
        var skillDescText = skillDescGO.AddComponent<TextMeshProUGUI>();
        skillDescText.text = "Click on a skill node to see its details.";
        skillDescText.fontSize = 16;
        skillDescText.alignment = TextAlignmentOptions.TopLeft;

        // Learn button
        var learnBtnGO = new GameObject("LearnButton");
        learnBtnGO.transform.SetParent(infoPanelGO.transform, false);
        var learnBtnLE = learnBtnGO.AddComponent<LayoutElement>();
        learnBtnLE.preferredHeight = 50;
        var learnBtnImage = learnBtnGO.AddComponent<Image>();
        learnBtnImage.color = new Color(0.2f, 0.5f, 0.2f, 1f);
        var learnBtn = learnBtnGO.AddComponent<Button>();
        learnBtn.targetGraphic = learnBtnImage;
        learnBtn.interactable = false;

        var learnBtnTextGO = new GameObject("Text");
        learnBtnTextGO.transform.SetParent(learnBtnGO.transform, false);
        var learnBtnTextRect = learnBtnTextGO.AddComponent<RectTransform>();
        learnBtnTextRect.anchorMin = Vector2.zero;
        learnBtnTextRect.anchorMax = Vector2.one;
        learnBtnTextRect.offsetMin = Vector2.zero;
        learnBtnTextRect.offsetMax = Vector2.zero;
        var learnBtnText = learnBtnTextGO.AddComponent<TextMeshProUGUI>();
        learnBtnText.text = "Learn (SP: 1)";
        learnBtnText.fontSize = 20;
        learnBtnText.alignment = TextAlignmentOptions.Center;
        learnBtnText.color = Color.white;

        // Close button
        var closeBtnGO = new GameObject("CloseButton");
        closeBtnGO.transform.SetParent(canvasGO.transform, false);
        var closeBtnRect = closeBtnGO.AddComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1, 1);
        closeBtnRect.anchorMax = new Vector2(1, 1);
        closeBtnRect.pivot = new Vector2(1, 1);
        closeBtnRect.sizeDelta = new Vector2(40, 40);
        closeBtnRect.anchoredPosition = new Vector2(-10, -10);
        var closeBtnImage = closeBtnGO.AddComponent<Image>();
        closeBtnImage.color = new Color(0.6f, 0.2f, 0.2f, 1f);
        var closeBtn = closeBtnGO.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnImage;

        var closeBtnTextGO = new GameObject("Text");
        closeBtnTextGO.transform.SetParent(closeBtnGO.transform, false);
        var closeBtnTextRect = closeBtnTextGO.AddComponent<RectTransform>();
        closeBtnTextRect.anchorMin = Vector2.zero;
        closeBtnTextRect.anchorMax = Vector2.one;
        closeBtnTextRect.offsetMin = Vector2.zero;
        closeBtnTextRect.offsetMax = Vector2.zero;
        var closeBtnText = closeBtnTextGO.AddComponent<TextMeshProUGUI>();
        closeBtnText.text = "X";
        closeBtnText.fontSize = 24;
        closeBtnText.alignment = TextAlignmentOptions.Center;
        closeBtnText.color = Color.white;

        // Add SkillTreePanel and wire references
        var skillTreePanel = canvasGO.AddComponent<SkillTreePanel>();
        var panelSO = new SerializedObject(skillTreePanel);

        panelSO.FindProperty("scrollRect").objectReferenceValue = scrollView;
        panelSO.FindProperty("contentContainer").objectReferenceValue = contentRect;
        panelSO.FindProperty("nodesContainer").objectReferenceValue = nodesRect;
        panelSO.FindProperty("connectionsContainer").objectReferenceValue = connectionsRect;
        panelSO.FindProperty("jobTitleText").objectReferenceValue = jobTitleText;
        panelSO.FindProperty("spDisplayText").objectReferenceValue = spDisplayText;
        panelSO.FindProperty("skillInfoPanel").objectReferenceValue = infoPanelGO;
        panelSO.FindProperty("skillNameText").objectReferenceValue = skillNameText;
        panelSO.FindProperty("skillDescriptionText").objectReferenceValue = skillDescText;
        panelSO.FindProperty("learnButton").objectReferenceValue = learnBtn;
        panelSO.FindProperty("learnButtonText").objectReferenceValue = learnBtnText;

        // Load and assign skill node prefab
        var skillNodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SKILL_NODE_PREFAB_PATH);
        if (skillNodePrefab != null)
        {
            panelSO.FindProperty("skillNodePrefab").objectReferenceValue = skillNodePrefab;
        }

        panelSO.ApplyModifiedProperties();

        // Wire controller references
        var controllerSO2 = new SerializedObject(controller);
        controllerSO2.FindProperty("skillTreeCanvas").objectReferenceValue = canvas;
        controllerSO2.FindProperty("skillTreeCanvasGroup").objectReferenceValue = canvasGroup;
        controllerSO2.FindProperty("skillTreePanel").objectReferenceValue = skillTreePanel;
        controllerSO2.FindProperty("closeButton").objectReferenceValue = closeBtn;
        controllerSO2.ApplyModifiedProperties();

        // Save as prefab
        PrefabUtility.SaveAsPrefabAsset(canvasGO, SKILL_TREE_CANVAS_PATH);
        DestroyImmediate(canvasGO);

        Debug.Log("[SkillSystemSetup] Created SkillTree Canvas prefab");
    }

    private void AddSkillTreeCanvasToScene()
    {
        // Check if already in scene
        var existingController = FindAnyObjectByType<SkillTreeController>();
        if (existingController != null)
        {
            Debug.Log("[SkillSystemSetup] SkillTree Canvas already in scene");
            return;
        }

        // Load prefab
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SKILL_TREE_CANVAS_PATH);
        if (prefab == null)
        {
            Debug.LogError("[SkillSystemSetup] SkillTree Canvas prefab not found. Create it first.");
            return;
        }

        // Instantiate in scene
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        instance.name = "SkillTree_Canvas";

        // Start hidden
        instance.SetActive(false);

        Undo.RegisterCreatedObjectUndo(instance, "Add SkillTree Canvas");
        Selection.activeGameObject = instance;

        Debug.Log("[SkillSystemSetup] Added SkillTree Canvas to scene");
    }
}
#endif
