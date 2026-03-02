using UnityEngine;
using UnityEditor;

/// <summary>
/// Static menu items to run LPCBulkImporter steps from the menu bar.
/// Separate from the EditorWindow class to ensure menu registration.
/// </summary>
public static class LPCBulkImporterRunner
{
    [MenuItem("Tools/LPC Import/1 Discover and Filter")]
    public static void RunDiscoverOnly()
    {
        var importer = ScriptableObject.CreateInstance<LPCBulkImporter>();
        try
        {
            importer.RunDiscoverStep();
        }
        finally
        {
            Object.DestroyImmediate(importer);
        }
    }

    [MenuItem("Tools/LPC Import/2 Run Full Import")]
    public static void RunFullImport()
    {
        var importer = ScriptableObject.CreateInstance<LPCBulkImporter>();
        try
        {
            importer.RunAllSteps();
        }
        finally
        {
            Object.DestroyImmediate(importer);
        }
    }

    [MenuItem("Tools/LPC Import/3 Build Assets from Local Files")]
    public static void RunLocalBuild()
    {
        LPCLocalAssetBuilder.Run();
    }
}
