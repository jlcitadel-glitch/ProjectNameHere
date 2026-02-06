using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to create the ExperienceOrb prefab and assign it to all enemy prefabs.
/// Run via Tools > Experience Orbs > Create Prefab & Assign to Enemies.
/// </summary>
public static class ExperienceOrbSetup
{
    private const string PrefabPath = "Assets/_Project/Prefabs/Effects/ExperienceOrb.prefab";

    [MenuItem("Tools/Experience Orbs/Create Prefab && Assign to Enemies")]
    public static void CreateAndAssign()
    {
        GameObject prefab = CreateOrbPrefab();
        AssignToEnemies(prefab);
        Debug.Log("[ExperienceOrbSetup] Done! ExperienceOrb prefab created and assigned to all enemy prefabs.");
    }

    [MenuItem("Tools/Experience Orbs/Create Prefab Only")]
    public static void CreatePrefabOnly()
    {
        CreateOrbPrefab();
        Debug.Log("[ExperienceOrbSetup] ExperienceOrb prefab created at: " + PrefabPath);
    }

    private static GameObject CreateOrbPrefab()
    {
        // Check if prefab already exists
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null)
        {
            Debug.Log("[ExperienceOrbSetup] Prefab already exists, using existing.");
            return existing;
        }

        // Ensure directory exists
        string dir = System.IO.Path.GetDirectoryName(PrefabPath);
        if (!AssetDatabase.IsValidFolder(dir))
        {
            string parent = System.IO.Path.GetDirectoryName(dir);
            string folder = System.IO.Path.GetFileName(dir);
            AssetDatabase.CreateFolder(parent, folder);
        }

        // Create temporary GameObject
        GameObject orbObj = new GameObject("ExperienceOrb");
        orbObj.tag = "Untagged";
        orbObj.layer = 0; // Default layer

        // Add required components
        Rigidbody2D rb = orbObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        CircleCollider2D col = orbObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.15f;

        // ExperienceOrb script will auto-create visuals at runtime
        orbObj.AddComponent<ExperienceOrb>();

        // Save as prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(orbObj, PrefabPath);
        Object.DestroyImmediate(orbObj);

        AssetDatabase.Refresh();
        return prefab;
    }

    private static void AssignToEnemies(GameObject orbPrefab)
    {
        string[] enemyPrefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Enemies" });

        int assigned = 0;
        foreach (string guid in enemyPrefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (enemyPrefab == null) continue;

            EnemyController controller = enemyPrefab.GetComponent<EnemyController>();
            if (controller == null) continue;

            // Use SerializedObject to modify prefab fields
            SerializedObject so = new SerializedObject(controller);
            SerializedProperty orbProp = so.FindProperty("experienceOrbPrefab");

            if (orbProp != null && orbProp.objectReferenceValue == null)
            {
                orbProp.objectReferenceValue = orbPrefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(enemyPrefab);
                assigned++;
                Debug.Log($"[ExperienceOrbSetup] Assigned to: {enemyPrefab.name}");
            }
        }

        if (assigned > 0)
        {
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[ExperienceOrbSetup] Assigned orb prefab to {assigned} enemy prefab(s).");
    }
}
