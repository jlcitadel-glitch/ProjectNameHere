#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to wire character visual prefab references onto JobClassData assets.
/// Run via Tools > Setup Class Visuals after importing Miniature Army 2D assets.
/// </summary>
public static class ClassVisualSetup
{
    private const string PeasantPrefabPath = "Assets/Miniature Army 2D V.1/Prefab/Peasant.prefab";
    private const string PriestPrefabPath = "Assets/Miniature Army 2D V.1/Prefab/Priest.prefab";
    private const string ThiefPrefabPath = "Assets/Miniature Army 2D V.1/Prefab/Thief.prefab";

    private const string WarriorAssetPath = "Assets/_Project/ScriptableObjects/Skills/Jobs/Warrior.asset";
    private const string MageAssetPath = "Assets/_Project/ScriptableObjects/Skills/Jobs/Mage.asset";
    private const string RogueAssetPath = "Assets/_Project/ScriptableObjects/Skills/Jobs/Rogue.asset";

    [MenuItem("Tools/Setup Class Visuals")]
    public static void SetupClassVisuals()
    {
        int wired = 0;

        wired += WirePrefab(WarriorAssetPath, PeasantPrefabPath, "Warrior", "Peasant");
        wired += WirePrefab(MageAssetPath, PriestPrefabPath, "Mage", "Priest");
        wired += WirePrefab(RogueAssetPath, ThiefPrefabPath, "Rogue", "Thief");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ClassVisualSetup] Done. Wired {wired}/3 class visual prefabs.");
    }

    private static int WirePrefab(string jobAssetPath, string prefabPath, string className, string prefabName)
    {
        var jobData = AssetDatabase.LoadAssetAtPath<JobClassData>(jobAssetPath);
        if (jobData == null)
        {
            Debug.LogWarning($"[ClassVisualSetup] {className} JobClassData not found at {jobAssetPath}");
            return 0;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[ClassVisualSetup] {prefabName} prefab not found at {prefabPath}");
            return 0;
        }

        jobData.characterVisualPrefab = prefab;
        EditorUtility.SetDirty(jobData);

        Debug.Log($"[ClassVisualSetup] {className} -> {prefabName} prefab wired.");
        return 1;
    }
}
#endif
