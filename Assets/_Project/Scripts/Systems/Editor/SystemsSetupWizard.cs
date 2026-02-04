using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility for setting up core game systems (GameManager, SaveManager).
/// </summary>
public class SystemsSetupWizard : EditorWindow
{
    [MenuItem("Tools/ProjectName/Systems Setup Wizard")]
    public static void ShowWindow()
    {
        GetWindow<SystemsSetupWizard>("Systems Setup");
    }

    [MenuItem("GameObject/ProjectName/Create Managers", false, 10)]
    public static void CreateManagersGameObject()
    {
        CreateManagers();
    }

    private void OnGUI()
    {
        GUILayout.Label("Systems Setup Wizard", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This wizard helps you set up the core game systems:\n" +
            "- GameManager: Controls game state (Playing, Paused, etc.)\n" +
            "- SaveManager: Handles save/load functionality",
            MessageType.Info);

        EditorGUILayout.Space();

        // Check current state
        var gameManager = FindAnyObjectByType<GameManager>();
        var saveManager = FindAnyObjectByType<SaveManager>();

        EditorGUILayout.LabelField("Current Status:", EditorStyles.boldLabel);

        DrawStatusRow("GameManager", gameManager != null);
        DrawStatusRow("SaveManager", saveManager != null);

        EditorGUILayout.Space();

        if (gameManager == null || saveManager == null)
        {
            if (GUILayout.Button("Create Managers GameObject", GUILayout.Height(30)))
            {
                CreateManagers();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Managers Prefab", GUILayout.Height(25)))
            {
                CreateManagersPrefab();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("All managers are set up!", MessageType.Info);

            if (GUILayout.Button("Select Managers in Scene"))
            {
                if (gameManager != null)
                {
                    Selection.activeGameObject = gameManager.gameObject;
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Actions:", EditorStyles.boldLabel);

        if (GUILayout.Button("Add GameManager Only"))
        {
            AddGameManager();
        }

        if (GUILayout.Button("Add SaveManager Only"))
        {
            AddSaveManager();
        }
    }

    private void DrawStatusRow(string label, bool exists)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(120));

        var prevColor = GUI.color;
        GUI.color = exists ? Color.green : Color.yellow;
        EditorGUILayout.LabelField(exists ? "Found" : "Missing");
        GUI.color = prevColor;

        EditorGUILayout.EndHorizontal();
    }

    private static void CreateManagers()
    {
        // Check if managers already exist
        var existingGameManager = FindAnyObjectByType<GameManager>();
        var existingSaveManager = FindAnyObjectByType<SaveManager>();

        GameObject managersGO = null;

        // Try to find existing Managers object
        if (existingGameManager != null)
        {
            managersGO = existingGameManager.gameObject;
        }
        else if (existingSaveManager != null)
        {
            managersGO = existingSaveManager.gameObject;
        }

        // Create new if needed
        if (managersGO == null)
        {
            managersGO = new GameObject("Managers");
            Undo.RegisterCreatedObjectUndo(managersGO, "Create Managers");
        }

        // Add components
        if (managersGO.GetComponent<GameManager>() == null)
        {
            Undo.AddComponent<GameManager>(managersGO);
            Debug.Log("[SystemsSetup] Added GameManager component");
        }

        if (managersGO.GetComponent<SaveManager>() == null)
        {
            Undo.AddComponent<SaveManager>(managersGO);
            Debug.Log("[SystemsSetup] Added SaveManager component");
        }

        Selection.activeGameObject = managersGO;
        Debug.Log("[SystemsSetup] Managers GameObject created/updated successfully!");
    }

    private static void CreateManagersPrefab()
    {
        // Create temporary GameObject
        var managersGO = new GameObject("Managers");
        managersGO.AddComponent<GameManager>();
        managersGO.AddComponent<SaveManager>();

        // Ensure directory exists
        string prefabDir = "Assets/_Project/Prefabs/Systems";
        if (!AssetDatabase.IsValidFolder(prefabDir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
            }
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Systems");
        }

        // Save as prefab
        string prefabPath = $"{prefabDir}/Managers.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(managersGO, prefabPath);

        // Destroy temporary object
        DestroyImmediate(managersGO);

        // Select the prefab
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log($"[SystemsSetup] Created Managers prefab at: {prefabPath}");
    }

    private static void AddGameManager()
    {
        var existing = FindAnyObjectByType<GameManager>();
        if (existing != null)
        {
            Debug.LogWarning("[SystemsSetup] GameManager already exists in scene!");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        var go = new GameObject("GameManager");
        Undo.RegisterCreatedObjectUndo(go, "Create GameManager");
        Undo.AddComponent<GameManager>(go);
        Selection.activeGameObject = go;
        Debug.Log("[SystemsSetup] Created GameManager");
    }

    private static void AddSaveManager()
    {
        var existing = FindAnyObjectByType<SaveManager>();
        if (existing != null)
        {
            Debug.LogWarning("[SystemsSetup] SaveManager already exists in scene!");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        var go = new GameObject("SaveManager");
        Undo.RegisterCreatedObjectUndo(go, "Create SaveManager");
        Undo.AddComponent<SaveManager>(go);
        Selection.activeGameObject = go;
        Debug.Log("[SystemsSetup] Created SaveManager");
    }
}
