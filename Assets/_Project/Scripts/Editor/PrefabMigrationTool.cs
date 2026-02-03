using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Editor tool for migrating scene GameObjects to prefabs.
/// Run via Tools > Prefab Migration menu.
/// </summary>
public class PrefabMigrationTool : EditorWindow
{
    private Vector2 scrollPosition;
    private bool showPhase1 = true;
    private bool showPhase2 = true;
    private bool showPhase3 = true;
    private bool showPhase4 = true;
    private bool showPhase5 = true;

    [MenuItem("Tools/Prefab Migration/Open Migration Tool")]
    public static void ShowWindow()
    {
        GetWindow<PrefabMigrationTool>("Prefab Migration");
    }

    [MenuItem("Tools/Prefab Migration/Phase 1 - Core Systems")]
    public static void MigratePhase1()
    {
        if (!EditorUtility.DisplayDialog("Phase 1: Core Systems",
            "This will create prefabs for:\n- Player (with GroundCheck child)\n- MainCamera (God Cam)\n\nProceed?", "Yes", "Cancel"))
            return;

        int created = 0;
        created += CreatePrefabFromSceneObject("Player", "Assets/_Project/Prefabs/Player/Player.prefab");
        created += CreatePrefabFromSceneObject("God Cam", "Assets/_Project/Prefabs/Camera/MainCamera.prefab");

        EditorUtility.DisplayDialog("Phase 1 Complete", $"Created {created} prefab(s).", "OK");
    }

    [MenuItem("Tools/Prefab Migration/Phase 2 - PowerUps")]
    public static void MigratePhase2()
    {
        if (!EditorUtility.DisplayDialog("Phase 2: PowerUps",
            "This will create prefabs for:\n- DashPowerUp\n- DoubleJumpPowerUp\n\nProceed?", "Yes", "Cancel"))
            return;

        int created = 0;
        created += CreatePrefabFromSceneObject("Dash Power Unlock", "Assets/_Project/Prefabs/Abilities/DashPowerUp.prefab");
        created += CreatePrefabFromSceneObject("Double Jump Power Unlock", "Assets/_Project/Prefabs/Abilities/DoubleJumpPowerUp.prefab");

        EditorUtility.DisplayDialog("Phase 2 Complete", $"Created {created} prefab(s).", "OK");
    }

    [MenuItem("Tools/Prefab Migration/Phase 3 - Environment and VFX")]
    public static void MigratePhase3()
    {
        if (!EditorUtility.DisplayDialog("Phase 3: Environment & VFX",
            "This will create prefabs for:\n- Parallax layers (Foreground, Midground, Background Close/Far)\n- Fog\n- RainZone\n\nProceed?", "Yes", "Cancel"))
            return;

        int created = 0;

        // Parallax layers
        created += CreatePrefabFromSceneObject("Foreground", "Assets/_Project/Prefabs/Effects/Parallax/ParallaxLayer_Foreground.prefab");
        created += CreatePrefabFromSceneObject("Foreground Close", "Assets/_Project/Prefabs/Effects/Parallax/ParallaxLayer_ForegroundClose.prefab");
        created += CreatePrefabFromSceneObject("Midground", "Assets/_Project/Prefabs/Effects/Parallax/ParallaxLayer_Midground.prefab");
        created += CreatePrefabFromSceneObject("Midground Close", "Assets/_Project/Prefabs/Effects/Parallax/ParallaxLayer_MidgroundClose.prefab");
        created += CreatePrefabFromSceneObject("Background Close", "Assets/_Project/Prefabs/Effects/Parallax/ParallaxLayer_BackgroundClose.prefab");
        created += CreatePrefabFromSceneObject("Background Far", "Assets/_Project/Prefabs/Effects/Parallax/ParallaxLayer_BackgroundFar.prefab");

        // Weather/Atmosphere
        created += CreatePrefabFromSceneObject("Fog", "Assets/_Project/Prefabs/Effects/Weather/Fog.prefab");
        created += CreatePrefabFromSceneObject("RainZone", "Assets/_Project/Prefabs/Effects/Weather/RainZone.prefab");

        EditorUtility.DisplayDialog("Phase 3 Complete", $"Created {created} prefab(s).", "OK");
    }

    [MenuItem("Tools/Prefab Migration/Phase 4 - Systems and Managers")]
    public static void MigratePhase4()
    {
        if (!EditorUtility.DisplayDialog("Phase 4: Systems & Managers",
            "This will create prefabs for:\n- WindManager\n- ParallaxManager\n\nProceed?", "Yes", "Cancel"))
            return;

        int created = 0;
        created += CreatePrefabFromSceneObject("WindManager", "Assets/_Project/Prefabs/Systems/WindManager.prefab");
        created += CreatePrefabFromSceneObject("ParallaxManager", "Assets/_Project/Prefabs/Systems/ParallaxManager.prefab");

        EditorUtility.DisplayDialog("Phase 4 Complete", $"Created {created} prefab(s).", "OK");
    }

    [MenuItem("Tools/Prefab Migration/Phase 5 - UI System")]
    public static void MigratePhase5()
    {
        if (!EditorUtility.DisplayDialog("Phase 5: UI System",
            "This will create prefabs for:\n\n" +
            "Systems:\n- UIManager\n\n" +
            "Canvases:\n- MainMenuCanvas\n- HUDCanvas\n- PauseCanvas\n- WorldCanvas\n\n" +
            "Menus:\n- TitleScreen\n- PauseMenu\n- OptionsPanel\n\n" +
            "Components:\n- GothicFrame\n- GothicButton\n- TabBar\n\n" +
            "Note: Only existing scene objects will be converted.\n\nProceed?", "Yes", "Cancel"))
            return;

        // Ensure UI folder structure exists
        EnsureUIFolderStructure();

        int created = 0;

        // UI Systems (singleton managers)
        created += CreatePrefabFromSceneObject("UIManager", "Assets/_Project/Prefabs/UI/Systems/UIManager.prefab");
        created += CreatePrefabFromSceneObject("FocusManager", "Assets/_Project/Prefabs/UI/Systems/FocusManager.prefab");

        // Canvases (separate by function per UI/UX Agent guidelines)
        created += CreatePrefabFromSceneObject("MainMenuCanvas", "Assets/_Project/Prefabs/UI/Canvases/MainMenuCanvas.prefab");
        created += CreatePrefabFromSceneObject("HUDCanvas", "Assets/_Project/Prefabs/UI/Canvases/HUDCanvas.prefab");
        created += CreatePrefabFromSceneObject("PauseCanvas", "Assets/_Project/Prefabs/UI/Canvases/PauseCanvas.prefab");
        created += CreatePrefabFromSceneObject("WorldCanvas", "Assets/_Project/Prefabs/UI/Canvases/WorldCanvas.prefab");

        // Alternative canvas names (some projects use different naming)
        created += CreatePrefabFromSceneObject("Main Menu Canvas", "Assets/_Project/Prefabs/UI/Canvases/MainMenuCanvas.prefab");
        created += CreatePrefabFromSceneObject("HUD Canvas", "Assets/_Project/Prefabs/UI/Canvases/HUDCanvas.prefab");
        created += CreatePrefabFromSceneObject("Pause Canvas", "Assets/_Project/Prefabs/UI/Canvases/PauseCanvas.prefab");
        created += CreatePrefabFromSceneObject("World Canvas", "Assets/_Project/Prefabs/UI/Canvases/WorldCanvas.prefab");

        // Menu screens (gothic-themed per UI/UX Agent)
        created += CreatePrefabFromSceneObject("TitleScreen", "Assets/_Project/Prefabs/UI/Menus/TitleScreen.prefab");
        created += CreatePrefabFromSceneObject("Title Screen", "Assets/_Project/Prefabs/UI/Menus/TitleScreen.prefab");
        created += CreatePrefabFromSceneObject("MainMenu", "Assets/_Project/Prefabs/UI/Menus/MainMenu.prefab");
        created += CreatePrefabFromSceneObject("Main Menu", "Assets/_Project/Prefabs/UI/Menus/MainMenu.prefab");
        created += CreatePrefabFromSceneObject("PauseMenu", "Assets/_Project/Prefabs/UI/Menus/PauseMenu.prefab");
        created += CreatePrefabFromSceneObject("Pause Menu", "Assets/_Project/Prefabs/UI/Menus/PauseMenu.prefab");
        created += CreatePrefabFromSceneObject("OptionsPanel", "Assets/_Project/Prefabs/UI/Menus/OptionsPanel.prefab");
        created += CreatePrefabFromSceneObject("Options Panel", "Assets/_Project/Prefabs/UI/Menus/OptionsPanel.prefab");
        created += CreatePrefabFromSceneObject("SettingsMenu", "Assets/_Project/Prefabs/UI/Menus/SettingsMenu.prefab");
        created += CreatePrefabFromSceneObject("Settings Menu", "Assets/_Project/Prefabs/UI/Menus/SettingsMenu.prefab");
        created += CreatePrefabFromSceneObject("SaveSlotPanel", "Assets/_Project/Prefabs/UI/Menus/SaveSlotPanel.prefab");
        created += CreatePrefabFromSceneObject("Save Slot Panel", "Assets/_Project/Prefabs/UI/Menus/SaveSlotPanel.prefab");

        // Reusable UI Components
        created += CreatePrefabFromSceneObject("GothicFrame", "Assets/_Project/Prefabs/UI/Components/GothicFrame.prefab");
        created += CreatePrefabFromSceneObject("Gothic Frame", "Assets/_Project/Prefabs/UI/Components/GothicFrame.prefab");
        created += CreatePrefabFromSceneObject("GothicButton", "Assets/_Project/Prefabs/UI/Components/GothicButton.prefab");
        created += CreatePrefabFromSceneObject("Gothic Button", "Assets/_Project/Prefabs/UI/Components/GothicButton.prefab");
        created += CreatePrefabFromSceneObject("TabBar", "Assets/_Project/Prefabs/UI/Components/TabBar.prefab");
        created += CreatePrefabFromSceneObject("Tab Bar", "Assets/_Project/Prefabs/UI/Components/TabBar.prefab");
        created += CreatePrefabFromSceneObject("InventoryGrid", "Assets/_Project/Prefabs/UI/Components/InventoryGrid.prefab");
        created += CreatePrefabFromSceneObject("Inventory Grid", "Assets/_Project/Prefabs/UI/Components/InventoryGrid.prefab");

        // HUD elements
        created += CreatePrefabFromSceneObject("PlayerHUD", "Assets/_Project/Prefabs/UI/HUD/PlayerHUD.prefab");
        created += CreatePrefabFromSceneObject("Player HUD", "Assets/_Project/Prefabs/UI/HUD/PlayerHUD.prefab");
        created += CreatePrefabFromSceneObject("HealthBar", "Assets/_Project/Prefabs/UI/HUD/HealthBar.prefab");
        created += CreatePrefabFromSceneObject("Health Bar", "Assets/_Project/Prefabs/UI/HUD/HealthBar.prefab");
        created += CreatePrefabFromSceneObject("SoulMeter", "Assets/_Project/Prefabs/UI/HUD/SoulMeter.prefab");
        created += CreatePrefabFromSceneObject("Soul Meter", "Assets/_Project/Prefabs/UI/HUD/SoulMeter.prefab");
        created += CreatePrefabFromSceneObject("AbilityDisplay", "Assets/_Project/Prefabs/UI/HUD/AbilityDisplay.prefab");
        created += CreatePrefabFromSceneObject("Ability Display", "Assets/_Project/Prefabs/UI/HUD/AbilityDisplay.prefab");

        EditorUtility.DisplayDialog("Phase 5 Complete",
            $"Created {created} UI prefab(s).\n\n" +
            "Note: ScriptableObjects (UIStyleGuide, GothicFrameStyle, UISoundBank) " +
            "should remain as assets, not prefabs.", "OK");
    }

    private static void EnsureUIFolderStructure()
    {
        string[] uiFolders = new string[]
        {
            "Assets/_Project/Prefabs/UI/Systems",
            "Assets/_Project/Prefabs/UI/Canvases",
            "Assets/_Project/Prefabs/UI/Menus",
            "Assets/_Project/Prefabs/UI/Components",
            "Assets/_Project/Prefabs/UI/HUD"
        };

        foreach (string folder in uiFolders)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Prefab Migration/Run All Phases")]
    public static void MigrateAll()
    {
        if (!EditorUtility.DisplayDialog("Run All Migration Phases",
            "This will create ALL prefabs from the current scene.\n\n" +
            "Phase 1: Player, Camera\n" +
            "Phase 2: PowerUps\n" +
            "Phase 3: Parallax, VFX\n" +
            "Phase 4: Managers\n" +
            "Phase 5: UI System\n\n" +
            "Proceed?", "Yes", "Cancel"))
            return;

        int totalCreated = 0;

        // Phase 1
        totalCreated += CreatePrefabFromSceneObject("Player", "Assets/_Project/Prefabs/Player/Player.prefab");
        totalCreated += CreatePrefabFromSceneObject("God Cam", "Assets/_Project/Prefabs/Camera/MainCamera.prefab");

        // Phase 2
        totalCreated += CreatePrefabFromSceneObject("Dash Power Unlock", "Assets/_Project/Prefabs/Abilities/DashPowerUp.prefab");
        totalCreated += CreatePrefabFromSceneObject("Double Jump Power Unlock", "Assets/_Project/Prefabs/Abilities/DoubleJumpPowerUp.prefab");

        // Phase 3
        totalCreated += CreatePrefabFromSceneObject("Foreground", "Assets/_Project/Prefabs/Effects/Parallax/ParallaxLayer_Foreground.prefab");
        totalCreated += CreatePrefabFromSceneObject("Foreground Close", "Assets/_Project/Prefabs/Effects/Parallax/ParallaxLayer_ForegroundClose.prefab");
        totalCreated += CreatePrefabFromSceneObject("Midground", "Assets/_Project/Prefabs/Effects/Parallax/ParallaxLayer_Midground.prefab");
        totalCreated += CreatePrefabFromSceneObject("Midground Close", "Assets/_Project/Prefabs/Effects/Parallax/ParallaxLayer_MidgroundClose.prefab");
        totalCreated += CreatePrefabFromSceneObject("Background Close", "Assets/_Project/Prefabs/Effects/Parallax/ParallaxLayer_BackgroundClose.prefab");
        totalCreated += CreatePrefabFromSceneObject("Background Far", "Assets/_Project/Prefabs/Effects/Parallax/ParallaxLayer_BackgroundFar.prefab");
        totalCreated += CreatePrefabFromSceneObject("Fog", "Assets/_Project/Prefabs/Effects/Weather/Fog.prefab");
        totalCreated += CreatePrefabFromSceneObject("RainZone", "Assets/_Project/Prefabs/Effects/Weather/RainZone.prefab");

        // Phase 4
        totalCreated += CreatePrefabFromSceneObject("WindManager", "Assets/_Project/Prefabs/Systems/WindManager.prefab");
        totalCreated += CreatePrefabFromSceneObject("ParallaxManager", "Assets/_Project/Prefabs/Systems/ParallaxManager.prefab");

        // Phase 5 - UI System
        EnsureUIFolderStructure();

        // UI Systems
        totalCreated += CreatePrefabFromSceneObject("UIManager", "Assets/_Project/Prefabs/UI/Systems/UIManager.prefab");
        totalCreated += CreatePrefabFromSceneObject("FocusManager", "Assets/_Project/Prefabs/UI/Systems/FocusManager.prefab");

        // Canvases
        totalCreated += CreatePrefabFromSceneObject("MainMenuCanvas", "Assets/_Project/Prefabs/UI/Canvases/MainMenuCanvas.prefab");
        totalCreated += CreatePrefabFromSceneObject("HUDCanvas", "Assets/_Project/Prefabs/UI/Canvases/HUDCanvas.prefab");
        totalCreated += CreatePrefabFromSceneObject("PauseCanvas", "Assets/_Project/Prefabs/UI/Canvases/PauseCanvas.prefab");
        totalCreated += CreatePrefabFromSceneObject("WorldCanvas", "Assets/_Project/Prefabs/UI/Canvases/WorldCanvas.prefab");

        // Menus
        totalCreated += CreatePrefabFromSceneObject("TitleScreen", "Assets/_Project/Prefabs/UI/Menus/TitleScreen.prefab");
        totalCreated += CreatePrefabFromSceneObject("MainMenu", "Assets/_Project/Prefabs/UI/Menus/MainMenu.prefab");
        totalCreated += CreatePrefabFromSceneObject("PauseMenu", "Assets/_Project/Prefabs/UI/Menus/PauseMenu.prefab");
        totalCreated += CreatePrefabFromSceneObject("OptionsPanel", "Assets/_Project/Prefabs/UI/Menus/OptionsPanel.prefab");

        // HUD
        totalCreated += CreatePrefabFromSceneObject("PlayerHUD", "Assets/_Project/Prefabs/UI/HUD/PlayerHUD.prefab");
        totalCreated += CreatePrefabFromSceneObject("HealthBar", "Assets/_Project/Prefabs/UI/HUD/HealthBar.prefab");

        EditorUtility.DisplayDialog("Migration Complete",
            $"Created {totalCreated} prefab(s).\n\nScene objects have been replaced with prefab instances.\nRemember to save the scene!", "OK");
    }

    /// <summary>
    /// Creates a prefab from a scene GameObject and replaces the scene object with the prefab instance.
    /// </summary>
    /// <param name="gameObjectName">Name of the GameObject in the scene</param>
    /// <param name="prefabPath">Path where the prefab should be saved</param>
    /// <returns>1 if successful, 0 if failed</returns>
    private static int CreatePrefabFromSceneObject(string gameObjectName, string prefabPath)
    {
        // Find the GameObject in the scene
        GameObject sceneObject = GameObject.Find(gameObjectName);
        if (sceneObject == null)
        {
            Debug.LogWarning($"PrefabMigration: Could not find '{gameObjectName}' in scene. Skipping.");
            return 0;
        }

        // Check if prefab already exists
        if (File.Exists(prefabPath))
        {
            Debug.LogWarning($"PrefabMigration: Prefab already exists at '{prefabPath}'. Skipping to avoid overwrite.");
            return 0;
        }

        // Ensure the directory exists
        string directory = Path.GetDirectoryName(prefabPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        // Store the transform info before creating prefab
        Transform parent = sceneObject.transform.parent;
        int siblingIndex = sceneObject.transform.GetSiblingIndex();
        Vector3 localPosition = sceneObject.transform.localPosition;
        Quaternion localRotation = sceneObject.transform.localRotation;
        Vector3 localScale = sceneObject.transform.localScale;

        // If the object is part of a prefab instance, we need to unpack it first
        // This handles cases where objects were previously prefabs or are nested in prefab instances
        if (PrefabUtility.IsPartOfAnyPrefab(sceneObject))
        {
            // Check if it's the root of a prefab instance
            if (PrefabUtility.IsAnyPrefabInstanceRoot(sceneObject))
            {
                // Unpack completely to make it a regular GameObject
                PrefabUtility.UnpackPrefabInstance(sceneObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                Debug.Log($"PrefabMigration: Unpacked existing prefab instance '{gameObjectName}'");
            }
            else if (PrefabUtility.IsPartOfPrefabInstance(sceneObject))
            {
                // It's a child of a prefab instance - we need to find the root and unpack
                GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(sceneObject);
                if (prefabRoot != null && prefabRoot != sceneObject)
                {
                    // The object is a child of another prefab - this is more complex
                    // We'll unpack the parent prefab first
                    PrefabUtility.UnpackPrefabInstance(prefabRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    Debug.Log($"PrefabMigration: Unpacked parent prefab to access '{gameObjectName}'");

                    // Re-find the object since references might have changed
                    sceneObject = GameObject.Find(gameObjectName);
                    if (sceneObject == null)
                    {
                        Debug.LogError($"PrefabMigration: Lost reference to '{gameObjectName}' after unpacking. Skipping.");
                        return 0;
                    }
                }
                else if (prefabRoot == sceneObject)
                {
                    // It is the root, unpack it
                    PrefabUtility.UnpackPrefabInstance(sceneObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    Debug.Log($"PrefabMigration: Unpacked existing prefab instance '{gameObjectName}'");
                }
            }
        }

        // Create the prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sceneObject, prefabPath, out bool success);

        if (!success || prefab == null)
        {
            Debug.LogError($"PrefabMigration: Failed to create prefab for '{gameObjectName}'");
            return 0;
        }

        // Delete the original scene object
        Object.DestroyImmediate(sceneObject);

        // Instantiate the prefab in its place
        GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        prefabInstance.transform.SetParent(parent);
        prefabInstance.transform.SetSiblingIndex(siblingIndex);
        prefabInstance.transform.localPosition = localPosition;
        prefabInstance.transform.localRotation = localRotation;
        prefabInstance.transform.localScale = localScale;

        // Mark scene as dirty so it can be saved
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"PrefabMigration: Created prefab '{prefabPath}' from '{gameObjectName}'");
        return 1;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Prefab Migration Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This tool converts scene GameObjects to prefabs for better collaboration.\n\n" +
            "Each phase can be run independently. After each phase:\n" +
            "1. Enter Play mode to verify functionality\n" +
            "2. Check prefab connections in Hierarchy (blue cube icon)\n" +
            "3. Save the scene (Ctrl+S)\n" +
            "4. Commit the prefab files and scene together",
            MessageType.Info);

        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Phase 1
        showPhase1 = EditorGUILayout.Foldout(showPhase1, "Phase 1: Core Systems", true);
        if (showPhase1)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Player", "Assets/_Project/Prefabs/Player/Player.prefab");
            EditorGUILayout.LabelField("MainCamera", "Assets/_Project/Prefabs/Camera/MainCamera.prefab");
            if (GUILayout.Button("Run Phase 1"))
                MigratePhase1();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Phase 2
        showPhase2 = EditorGUILayout.Foldout(showPhase2, "Phase 2: PowerUps", true);
        if (showPhase2)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("DashPowerUp", "Assets/_Project/Prefabs/Abilities/DashPowerUp.prefab");
            EditorGUILayout.LabelField("DoubleJumpPowerUp", "Assets/_Project/Prefabs/Abilities/DoubleJumpPowerUp.prefab");
            if (GUILayout.Button("Run Phase 2"))
                MigratePhase2();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Phase 3
        showPhase3 = EditorGUILayout.Foldout(showPhase3, "Phase 3: Environment & VFX", true);
        if (showPhase3)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Parallax Layers:");
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Foreground", "ParallaxLayer_Foreground.prefab");
            EditorGUILayout.LabelField("Foreground Close", "ParallaxLayer_ForegroundClose.prefab");
            EditorGUILayout.LabelField("Midground", "ParallaxLayer_Midground.prefab");
            EditorGUILayout.LabelField("Midground Close", "ParallaxLayer_MidgroundClose.prefab");
            EditorGUILayout.LabelField("Background Close", "ParallaxLayer_BackgroundClose.prefab");
            EditorGUILayout.LabelField("Background Far", "ParallaxLayer_BackgroundFar.prefab");
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField("Weather/Atmosphere:");
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Fog", "Fog.prefab");
            EditorGUILayout.LabelField("RainZone", "RainZone.prefab");
            EditorGUI.indentLevel--;
            if (GUILayout.Button("Run Phase 3"))
                MigratePhase3();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Phase 4
        showPhase4 = EditorGUILayout.Foldout(showPhase4, "Phase 4: Systems & Managers", true);
        if (showPhase4)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("WindManager", "Assets/_Project/Prefabs/Systems/WindManager.prefab");
            EditorGUILayout.LabelField("ParallaxManager", "Assets/_Project/Prefabs/Systems/ParallaxManager.prefab");
            if (GUILayout.Button("Run Phase 4"))
                MigratePhase4();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Phase 5 - UI System
        showPhase5 = EditorGUILayout.Foldout(showPhase5, "Phase 5: UI System (Gothic Theme)", true);
        if (showPhase5)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Systems:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("UIManager", "UI/Systems/UIManager.prefab");
            EditorGUILayout.LabelField("FocusManager", "UI/Systems/FocusManager.prefab");
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Canvases:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("MainMenuCanvas", "Screen Space Overlay (sort: 100)");
            EditorGUILayout.LabelField("HUDCanvas", "Screen Space Overlay (sort: 10)");
            EditorGUILayout.LabelField("PauseCanvas", "Screen Space Overlay (sort: 200)");
            EditorGUILayout.LabelField("WorldCanvas", "World Space (damage numbers)");
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Menus:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("TitleScreen", "New Game / Continue / Options");
            EditorGUILayout.LabelField("PauseMenu", "Hollow Knight-style tabs");
            EditorGUILayout.LabelField("OptionsPanel", "Audio / Video / Controls");
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Components:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("GothicFrame", "9-slice ornate border");
            EditorGUILayout.LabelField("GothicButton", "Styled button with effects");
            EditorGUILayout.LabelField("TabBar", "LB/RB navigation strip");
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("HUD:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("PlayerHUD", "Health, soul, currency");
            EditorGUILayout.LabelField("HealthBar", "SOTN-style health display");
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Note: ScriptableObjects (UIStyleGuide, GothicFrameStyle, UISoundBank) " +
                "are data assets, not prefabs. They stay in Assets/_Project/Settings/",
                MessageType.Info);

            if (GUILayout.Button("Run Phase 5"))
                MigratePhase5();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // Run All button
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Run All Phases", GUILayout.Height(30)))
            MigrateAll();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "After migration:\n" +
            "- Scene objects will be replaced with prefab instances\n" +
            "- Original functionality is preserved\n" +
            "- Edit prefabs in Prefab Mode (double-click prefab)\n" +
            "- Scene changes become minimal (just transforms)",
            MessageType.None);
    }
}
