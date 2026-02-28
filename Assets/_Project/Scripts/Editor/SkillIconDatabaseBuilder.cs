#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool that scans PNG icons and populates the SkillIconDatabase asset.
/// Also configures texture import settings for optimal icon display.
/// </summary>
public static class SkillIconDatabaseBuilder
{
    private const string IconsRoot = "Assets/_Project/Art/UI/Icons/Skills";
    private const string DatabasePath = "Assets/_Project/Resources/SkillIconDatabase.asset";

    [MenuItem("Tools/Skill Icons/Rebuild Database")]
    public static void RebuildDatabase()
    {
        // Ensure Resources folder exists
        string resourcesDir = Path.GetDirectoryName(DatabasePath);
        if (!Directory.Exists(resourcesDir))
        {
            Directory.CreateDirectory(resourcesDir);
            AssetDatabase.Refresh();
        }

        // Load or create database
        var database = AssetDatabase.LoadAssetAtPath<SkillIconDatabase>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<SkillIconDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        // Scan all PNGs under the icons root
        var entries = new List<SkillIconEntry>();
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { IconsRoot });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null) continue;

            // Configure import settings
            ConfigureTextureImport(assetPath);

            // Build icon ID from relative path: "lorc/fire-bolt"
            string relativePath = assetPath.Substring(IconsRoot.Length + 1); // +1 for "/"
            string iconId = Path.ChangeExtension(relativePath, null).Replace("\\", "/");
            string displayName = FormatDisplayName(Path.GetFileNameWithoutExtension(assetPath));

            var entry = new SkillIconEntry
            {
                iconId = iconId,
                displayName = displayName,
                sprite = sprite,
                tags = GenerateTags(iconId, displayName)
            };

            entries.Add(entry);
        }

        database.allIcons = entries.ToArray();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SkillIconDatabaseBuilder] Rebuilt database with {entries.Count} icons");
    }

    [MenuItem("Tools/Skill Icons/Configure Import Settings")]
    public static void ConfigureAllImportSettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { IconsRoot });
        int configured = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (ConfigureTextureImport(path))
                configured++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[SkillIconDatabaseBuilder] Configured {configured} texture import settings");
    }

    private static bool ConfigureTextureImport(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return false;

        bool changed = false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (importer.maxTextureSize != 128)
        {
            importer.maxTextureSize = 128;
            changed = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (importer.filterMode != FilterMode.Bilinear)
        {
            importer.filterMode = FilterMode.Bilinear;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }

        return changed;
    }

    private static string FormatDisplayName(string fileName)
    {
        // Convert "fire-bolt" to "Fire Bolt"
        var words = fileName.Split('-');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
        }
        return string.Join(" ", words);
    }

    private static string[] GenerateTags(string iconId, string displayName)
    {
        var tags = new List<string>();

        // Add author as a tag
        int slashIndex = iconId.IndexOf('/');
        if (slashIndex > 0)
        {
            tags.Add(iconId.Substring(0, slashIndex));
        }

        // Split display name words as tags
        foreach (string word in displayName.Split(' '))
        {
            string lower = word.ToLowerInvariant();
            if (lower.Length > 2 && !tags.Contains(lower))
            {
                tags.Add(lower);
            }
        }

        return tags.ToArray();
    }
}
#endif
