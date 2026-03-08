using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool that scans all BodyPartData assets and builds a BodyPartRegistry
/// ScriptableObject in Resources/ so it can be loaded at runtime.
/// Run from Tools > ULPC > Build Body Part Registry.
/// </summary>
public static class BodyPartRegistryBuilder
{
    const string OutputPath = "Assets/_Project/Resources/BodyPartRegistry.asset";

    [MenuItem("Tools/ULPC/Build Body Part Registry")]
    public static void Build()
    {
        // Find all BodyPartData assets in the project
        var guids = AssetDatabase.FindAssets("t:BodyPartData");
        var parts = new List<BodyPartData>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var part = AssetDatabase.LoadAssetAtPath<BodyPartData>(path);
            if (part != null)
                parts.Add(part);
        }

        // Load or create the registry
        var registry = AssetDatabase.LoadAssetAtPath<BodyPartRegistry>(OutputPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<BodyPartRegistry>();
            AssetDatabase.CreateAsset(registry, OutputPath);
        }

        registry.allParts = parts.ToArray();
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();

        Debug.Log($"[BodyPartRegistryBuilder] Built registry with {parts.Count} parts at {OutputPath}");
    }
}
