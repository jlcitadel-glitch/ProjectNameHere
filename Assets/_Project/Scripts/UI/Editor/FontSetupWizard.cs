#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;

namespace ProjectName.UI.Editor
{
    /// <summary>
    /// Editor wizard to set up Cinzel font for the project.
    /// </summary>
    public class FontSetupWizard : EditorWindow
    {
        private const string FONTS_PATH = "Assets/_Project/Fonts";
        private const string FONT_NAME = "Cinzel";

        private TMP_FontAsset cinzelFontAsset;
        private Font cinzelFont;

        [MenuItem("Tools/ProjectName/Font Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow<FontSetupWizard>("Font Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Cinzel Font Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Step 1: Download instructions
            EditorGUILayout.HelpBox(
                "Step 1: Download Cinzel font from Google Fonts\n\n" +
                "1. Click the button below to open Google Fonts\n" +
                "2. Click 'Download family' (top right)\n" +
                "3. Extract the ZIP file\n" +
                "4. Copy the .ttf files to: Assets/_Project/Fonts/",
                MessageType.Info);

            if (GUILayout.Button("Open Google Fonts - Cinzel", GUILayout.Height(30)))
            {
                Application.OpenURL("https://fonts.google.com/specimen/Cinzel");
            }

            EditorGUILayout.Space();

            // Check if font files exist
            bool fontExists = CheckFontExists();

            if (fontExists)
            {
                EditorGUILayout.HelpBox("Cinzel font file found!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Cinzel font not found in Assets/_Project/Fonts/\n" +
                    "Please download and copy the .ttf file first.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space();

            // Step 2: Create TMP Font Asset
            EditorGUILayout.LabelField("Step 2: Create TMP Font Asset", EditorStyles.boldLabel);

            GUI.enabled = fontExists;
            if (GUILayout.Button("Create Cinzel TMP Font Asset", GUILayout.Height(30)))
            {
                CreateTMPFontAsset();
            }
            GUI.enabled = true;

            EditorGUILayout.Space();

            // Step 3: Set as default
            EditorGUILayout.LabelField("Step 3: Set as Default Font", EditorStyles.boldLabel);

            cinzelFontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField(
                "Cinzel TMP Font",
                cinzelFontAsset,
                typeof(TMP_FontAsset),
                false);

            // Try to find existing font asset
            if (cinzelFontAsset == null)
            {
                cinzelFontAsset = FindCinzelFontAsset();
            }

            GUI.enabled = cinzelFontAsset != null;
            if (GUILayout.Button("Set as Default TMP Font", GUILayout.Height(30)))
            {
                SetAsDefaultFont();
            }
            GUI.enabled = true;

            EditorGUILayout.Space();

            // Step 4: Update existing UI
            EditorGUILayout.LabelField("Step 4: Update Existing UI", EditorStyles.boldLabel);

            GUI.enabled = cinzelFontAsset != null;
            if (GUILayout.Button("Update All TMP Text in Scene", GUILayout.Height(30)))
            {
                UpdateAllTextInScene();
            }

            if (GUILayout.Button("Update All TMP Text in Prefabs", GUILayout.Height(30)))
            {
                UpdateAllTextInPrefabs();
            }
            GUI.enabled = true;
        }

        private bool CheckFontExists()
        {
            if (!Directory.Exists(FONTS_PATH))
            {
                Directory.CreateDirectory(FONTS_PATH);
                AssetDatabase.Refresh();
            }

            string[] ttfFiles = Directory.GetFiles(FONTS_PATH, "*.ttf", SearchOption.AllDirectories);
            string[] otfFiles = Directory.GetFiles(FONTS_PATH, "*.otf", SearchOption.AllDirectories);

            foreach (var file in ttfFiles)
            {
                if (file.ToLower().Contains("cinzel"))
                    return true;
            }
            foreach (var file in otfFiles)
            {
                if (file.ToLower().Contains("cinzel"))
                    return true;
            }

            return false;
        }

        private Font FindCinzelTTF()
        {
            string[] guids = AssetDatabase.FindAssets("Cinzel t:Font", new[] { FONTS_PATH });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<Font>(path);
            }
            return null;
        }

        private TMP_FontAsset FindCinzelFontAsset()
        {
            string[] guids = AssetDatabase.FindAssets("Cinzel t:TMP_FontAsset", new[] { FONTS_PATH });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
            return null;
        }

        private void CreateTMPFontAsset()
        {
            Font font = FindCinzelTTF();
            if (font == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "Could not find Cinzel font file in Assets/_Project/Fonts/",
                    "OK");
                return;
            }

            // Use TMP's font asset creator
            string fontPath = AssetDatabase.GetAssetPath(font);
            string fontAssetPath = Path.ChangeExtension(fontPath, null) + " SDF.asset";

            // Check if already exists
            if (File.Exists(fontAssetPath))
            {
                if (!EditorUtility.DisplayDialog("Font Asset Exists",
                    "Cinzel SDF font asset already exists. Recreate it?",
                    "Yes", "No"))
                {
                    cinzelFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
                    return;
                }
            }

            // Create font asset using TMP's built-in method
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);

            if (fontAsset != null)
            {
                AssetDatabase.CreateAsset(fontAsset, fontAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                cinzelFontAsset = fontAsset;

                Debug.Log($"[FontSetup] Created TMP Font Asset: {fontAssetPath}");
                EditorUtility.DisplayDialog("Success",
                    "Cinzel TMP Font Asset created!\n\n" +
                    "Note: For best quality, use Window > TextMeshPro > Font Asset Creator\n" +
                    "to generate with custom settings (Atlas Resolution, Padding, etc.)",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error",
                    "Failed to create TMP Font Asset. Try using:\n" +
                    "Window > TextMeshPro > Font Asset Creator",
                    "OK");
            }
        }

        private void SetAsDefaultFont()
        {
            if (cinzelFontAsset == null)
            {
                EditorUtility.DisplayDialog("Error", "No font asset selected.", "OK");
                return;
            }

            // Find TMP Settings
            string[] guids = AssetDatabase.FindAssets("TMP Settings t:TMP_Settings");
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Error",
                    "TMP Settings not found. Make sure TextMeshPro is properly installed.",
                    "OK");
                return;
            }

            string settingsPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath);

            if (settings != null)
            {
                SerializedObject so = new SerializedObject(settings);
                SerializedProperty defaultFontProp = so.FindProperty("m_defaultFontAsset");

                if (defaultFontProp != null)
                {
                    defaultFontProp.objectReferenceValue = cinzelFontAsset;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();

                    Debug.Log($"[FontSetup] Set {cinzelFontAsset.name} as default TMP font");
                    EditorUtility.DisplayDialog("Success",
                        $"Set {cinzelFontAsset.name} as the default TMP font.\n\n" +
                        "New TMP text will use this font by default.",
                        "OK");
                }
            }
        }

        private void UpdateAllTextInScene()
        {
            if (cinzelFontAsset == null)
            {
                EditorUtility.DisplayDialog("Error", "No font asset selected.", "OK");
                return;
            }

            TMP_Text[] allText = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            int count = 0;

            foreach (var text in allText)
            {
                Undo.RecordObject(text, "Change Font");
                text.font = cinzelFontAsset;
                EditorUtility.SetDirty(text);
                count++;
            }

            Debug.Log($"[FontSetup] Updated {count} TMP Text components in scene");
            EditorUtility.DisplayDialog("Success",
                $"Updated {count} TMP Text components in the current scene.",
                "OK");
        }

        private void UpdateAllTextInPrefabs()
        {
            if (cinzelFontAsset == null)
            {
                EditorUtility.DisplayDialog("Error", "No font asset selected.", "OK");
                return;
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs" });
            int prefabCount = 0;
            int textCount = 0;

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null) continue;

                TMP_Text[] texts = prefab.GetComponentsInChildren<TMP_Text>(true);
                if (texts.Length == 0) continue;

                // Load prefab for editing
                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

                TMP_Text[] prefabTexts = prefabRoot.GetComponentsInChildren<TMP_Text>(true);
                bool modified = false;

                foreach (var text in prefabTexts)
                {
                    if (text.font != cinzelFontAsset)
                    {
                        text.font = cinzelFontAsset;
                        textCount++;
                        modified = true;
                    }
                }

                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                    prefabCount++;
                }

                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[FontSetup] Updated {textCount} TMP Text components in {prefabCount} prefabs");
            EditorUtility.DisplayDialog("Success",
                $"Updated {textCount} TMP Text components in {prefabCount} prefabs.",
                "OK");
        }
    }
}
#endif
