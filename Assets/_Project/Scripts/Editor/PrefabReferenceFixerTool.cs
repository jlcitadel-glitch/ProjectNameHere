using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Utility to fix cross-prefab references after migration.
/// Some components (like AdvancedCameraController) reference other scene objects.
/// After prefab migration, these references need to be reconnected.
/// </summary>
public class PrefabReferenceFixerTool : EditorWindow
{
    [MenuItem("Tools/Prefab Migration/Fix Cross-Prefab References")]
    public static void FixReferences()
    {
        int fixedCount = 0;

        // Fix Camera -> Player reference
        GameObject camera = GameObject.Find("MainCamera");
        if (camera == null) camera = GameObject.Find("God Cam");

        GameObject player = GameObject.Find("Player");

        if (camera != null && player != null)
        {
            var cameraController = camera.GetComponent<AdvancedCameraController>();
            if (cameraController != null)
            {
                SerializedObject so = new SerializedObject(cameraController);
                SerializedProperty targetProp = so.FindProperty("target");
                if (targetProp != null)
                {
                    targetProp.objectReferenceValue = player.transform;
                    so.ApplyModifiedProperties();
                    fixedCount++;
                    Debug.Log("Fixed: AdvancedCameraController.target -> Player.transform");
                }
            }
        }

        // Fix ParallaxManager -> Layer references
        GameObject parallaxManager = GameObject.Find("ParallaxManager");
        if (parallaxManager != null)
        {
            var manager = parallaxManager.GetComponent<ParallaxBackgroundManager>();
            if (manager != null)
            {
                // The ParallaxBackgroundManager should auto-find layers by name or tag
                // This is a manual fix if needed
                Debug.Log("ParallaxManager found. Check layers array manually if needed.");
            }
        }

        // Fix PlayerControllerScript -> GroundCheck reference
        if (player != null)
        {
            var playerController = player.GetComponent<PlayerControllerScript>();
            if (playerController != null)
            {
                Transform groundCheck = player.transform.Find("GroundCheck");
                if (groundCheck != null)
                {
                    SerializedObject so = new SerializedObject(playerController);
                    SerializedProperty groundCheckProp = so.FindProperty("groundCheck");
                    if (groundCheckProp != null)
                    {
                        groundCheckProp.objectReferenceValue = groundCheck;
                        so.ApplyModifiedProperties();
                        fixedCount++;
                        Debug.Log("Fixed: PlayerControllerScript.groundCheck -> GroundCheck");
                    }
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Reference Fixer",
            $"Fixed {fixedCount} reference(s).\n\nSave the scene to persist changes.",
            "OK");
    }

    [MenuItem("Tools/Prefab Migration/Verify Prefab Connections")]
    public static void VerifyConnections()
    {
        string report = "Prefab Connection Report:\n\n";
        int connected = 0;
        int disconnected = 0;

        string[] expectedPrefabs = new string[]
        {
            "Player",
            "MainCamera",
            "Dash Power Unlock",
            "Double Jump Power Unlock",
            "Foreground",
            "Foreground Close",
            "Midground",
            "Midground Close",
            "Background Close",
            "Background Far",
            "Fog",
            "RainZone",
            "WindManager",
            "ParallaxManager"
        };

        // Also check for renamed prefab instances
        string[] altNames = new string[]
        {
            "God Cam",
            "DashPowerUp",
            "DoubleJumpPowerUp"
        };

        foreach (string name in expectedPrefabs)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                bool isPrefab = PrefabUtility.IsPartOfPrefabInstance(obj);
                if (isPrefab)
                {
                    report += $"[OK] {name} - Connected to prefab\n";
                    connected++;
                }
                else
                {
                    report += $"[!!] {name} - NOT a prefab instance\n";
                    disconnected++;
                }
            }
        }

        foreach (string name in altNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                bool isPrefab = PrefabUtility.IsPartOfPrefabInstance(obj);
                if (isPrefab)
                {
                    report += $"[OK] {name} - Connected to prefab\n";
                    connected++;
                }
                else
                {
                    report += $"[..] {name} - Scene object (not yet migrated)\n";
                }
            }
        }

        report += $"\n---\nConnected: {connected}\nNot migrated: {disconnected}\n";

        Debug.Log(report);
        EditorUtility.DisplayDialog("Prefab Verification", report, "OK");
    }
}
