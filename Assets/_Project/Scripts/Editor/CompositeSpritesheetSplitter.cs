using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor window that splits a composite spritesheet (with labeled rows)
/// into individual horizontal strip PNGs for MageSkillVFX consumption.
/// </summary>
public class CompositeSpritesheetSplitter : EditorWindow
{
    [System.Serializable]
    private class StripDefinition
    {
        public string outputName;
        public int yFromTop;
        public int height;
        public int frameCount;
        public bool enabled = true;
    }

    private Texture2D sourceTexture;
    private string outputDirectory = "Assets/_Project/Resources/VFX/MageSkills";
    private bool removeWhiteBackground = true;
    private float whiteThreshold = 0.95f;
    private Vector2 scrollPos;

    // Pre-populated strip definitions for the Gemini 1083x992 composite
    private List<StripDefinition> strips = new List<StripDefinition>
    {
        new StripDefinition { outputName = "arcane_slash_cast", yFromTop = 30,  height = 195, frameCount = 5, enabled = true },
        new StripDefinition { outputName = "charge_shot_cast",  yFromTop = 260, height = 195, frameCount = 5, enabled = true },
        new StripDefinition { outputName = "shield_cast_activate", yFromTop = 490, height = 210, frameCount = 6, enabled = true },
        new StripDefinition { outputName = "lightning_chain_cast", yFromTop = 740, height = 210, frameCount = 5, enabled = true },
    };

    [MenuItem("Tools/VFX/Split Composite Spritesheet")]
    public static void ShowWindow()
    {
        GetWindow<CompositeSpritesheetSplitter>("Split Composite Spritesheet");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Composite Spritesheet Splitter", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source Texture", sourceTexture, typeof(Texture2D), false);
        outputDirectory = EditorGUILayout.TextField("Output Directory", outputDirectory);
        removeWhiteBackground = EditorGUILayout.Toggle("Remove White Background", removeWhiteBackground);

        if (removeWhiteBackground)
        {
            whiteThreshold = EditorGUILayout.Slider("White Threshold", whiteThreshold, 0.8f, 1.0f);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Strip Definitions", EditorStyles.boldLabel);

        if (sourceTexture != null)
        {
            EditorGUILayout.HelpBox($"Source: {sourceTexture.width}x{sourceTexture.height}", MessageType.Info);
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        for (int i = 0; i < strips.Count; i++)
        {
            var strip = strips[i];
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            strip.enabled = EditorGUILayout.Toggle(strip.enabled, GUILayout.Width(20));
            strip.outputName = EditorGUILayout.TextField("Name", strip.outputName);
            EditorGUILayout.EndHorizontal();

            strip.yFromTop = EditorGUILayout.IntField("Y From Top", strip.yFromTop);
            strip.height = EditorGUILayout.IntField("Height", strip.height);
            strip.frameCount = EditorGUILayout.IntField("Frame Count", strip.frameCount);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Strip"))
        {
            strips.Add(new StripDefinition { outputName = "new_strip", yFromTop = 0, height = 100, frameCount = 5 });
        }
        if (strips.Count > 0 && GUILayout.Button("Remove Last"))
        {
            strips.RemoveAt(strips.Count - 1);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        GUI.enabled = sourceTexture != null;
        if (GUILayout.Button("Extract All", GUILayout.Height(30)))
        {
            ExtractAll();
        }
        GUI.enabled = true;
    }

    private void ExtractAll()
    {
        // Ensure source is readable
        string sourcePath = AssetDatabase.GetAssetPath(sourceTexture);
        var importer = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        bool wasReadable = true;

        if (importer != null && !importer.isReadable)
        {
            wasReadable = false;
            importer.isReadable = true;
            importer.SaveAndReimport();
            sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        }

        // Ensure output directory exists
        if (!AssetDatabase.IsValidFolder(outputDirectory))
        {
            string[] parts = outputDirectory.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        int extracted = 0;
        int texWidth = sourceTexture.width;
        int texHeight = sourceTexture.height;

        foreach (var strip in strips)
        {
            if (!strip.enabled) continue;

            // Unity textures have Y=0 at bottom, so flip Y
            int unityY = texHeight - strip.yFromTop - strip.height;

            if (unityY < 0 || unityY + strip.height > texHeight)
            {
                Debug.LogError($"[Splitter] Strip '{strip.outputName}' Y range out of bounds: unityY={unityY}, height={strip.height}, texHeight={texHeight}");
                continue;
            }

            // Extract the strip region
            Color[] pixels = sourceTexture.GetPixels(0, unityY, texWidth, strip.height);

            // Create output texture
            var output = new Texture2D(texWidth, strip.height, TextureFormat.RGBA32, false);
            output.SetPixels(pixels);

            // Remove white background if enabled
            if (removeWhiteBackground)
            {
                Color[] outputPixels = output.GetPixels();
                for (int p = 0; p < outputPixels.Length; p++)
                {
                    Color c = outputPixels[p];
                    if (c.r > whiteThreshold && c.g > whiteThreshold && c.b > whiteThreshold)
                    {
                        outputPixels[p] = Color.clear;
                    }
                }
                output.SetPixels(outputPixels);
            }

            output.Apply();

            // Encode and save
            byte[] pngData = output.EncodeToPNG();
            string outputPath = $"{outputDirectory}/{strip.outputName}.png";
            string absolutePath = Path.Combine(Application.dataPath, "..", outputPath).Replace('\\', '/');

            File.WriteAllBytes(absolutePath, pngData);
            Debug.Log($"[Splitter] Extracted: {outputPath} ({texWidth}x{strip.height}, {strip.frameCount} frames)");

            DestroyImmediate(output);
            extracted++;
        }

        AssetDatabase.Refresh();

        // Configure texture import settings for each extracted strip
        foreach (var strip in strips)
        {
            if (!strip.enabled) continue;

            string outputPath = $"{outputDirectory}/{strip.outputName}.png";
            var texImporter = AssetImporter.GetAtPath(outputPath) as TextureImporter;
            if (texImporter != null)
            {
                texImporter.textureType = TextureImporterType.Sprite;
                texImporter.spriteImportMode = SpriteImportMode.Single;
                texImporter.filterMode = FilterMode.Point;
                texImporter.isReadable = true;
                texImporter.textureCompression = TextureImporterCompression.Uncompressed;
                texImporter.SaveAndReimport();
            }
        }

        // Restore source readability if we changed it
        if (!wasReadable && importer != null)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        Debug.Log($"[Splitter] Done! Extracted {extracted} strips to {outputDirectory}");
        EditorUtility.DisplayDialog("Extraction Complete", $"Extracted {extracted} strips to:\n{outputDirectory}", "OK");
    }
}
