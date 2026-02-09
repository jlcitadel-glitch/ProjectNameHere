#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ProjectName.UI.Editor
{
    /// <summary>
    /// Editor wizard that assigns idle preview frames, default sprites,
    /// and class colors to JobClassData ScriptableObjects.
    /// Run via Tools > ProjectName > Class Appearance Setup.
    /// </summary>
    public class ClassAppearanceSetup : EditorWindow
    {
        private const string JOBS_PATH = "Assets/_Project/ScriptableObjects/Skills/Jobs";
        private const string HERO_KNIGHT_SPRITE_PATH = "Assets/ThirdParty/Hero Knight - Pixel Art/Sprites/HeroKnight.png";

        private const int IDLE_FRAME_START = 0;
        private const int IDLE_FRAME_COUNT = 8;

        private JobClassData warriorData;
        private JobClassData mageData;
        private JobClassData rogueData;

        [MenuItem("Tools/ProjectName/Class Appearance Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<ClassAppearanceSetup>("Class Appearance Setup");
            window.minSize = new Vector2(450, 400);
        }

        private void OnEnable()
        {
            AutoDetectAssets();
        }

        private void AutoDetectAssets()
        {
            warriorData = AssetDatabase.LoadAssetAtPath<JobClassData>($"{JOBS_PATH}/Warrior.asset");
            mageData = AssetDatabase.LoadAssetAtPath<JobClassData>($"{JOBS_PATH}/Mage.asset");
            rogueData = AssetDatabase.LoadAssetAtPath<JobClassData>($"{JOBS_PATH}/Rogue.asset");
        }

        private void OnGUI()
        {
            GUILayout.Label("Class Appearance Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Assigns idle preview frames, default sprites, and class colors to JobClassData assets.\n" +
                "The paper-doll system uses a shared Peasant skeleton with color tinting per class.",
                MessageType.Info);
            EditorGUILayout.Space(10);

            GUILayout.Label("Job Data Assets", EditorStyles.boldLabel);
            warriorData = (JobClassData)EditorGUILayout.ObjectField("Warrior", warriorData, typeof(JobClassData), false);
            mageData = (JobClassData)EditorGUILayout.ObjectField("Mage", mageData, typeof(JobClassData), false);
            rogueData = (JobClassData)EditorGUILayout.ObjectField("Rogue", rogueData, typeof(JobClassData), false);

            EditorGUILayout.Space(10);

            GUILayout.Label("Status", EditorStyles.boldLabel);
            DrawStatus("Warrior Data", warriorData != null);
            DrawStatus("Mage Data", mageData != null);
            DrawStatus("Rogue Data", rogueData != null);

            bool hasHeroKnight = AssetDatabase.LoadMainAssetAtPath(HERO_KNIGHT_SPRITE_PATH) != null;
            DrawStatus("HeroKnight Sprite Sheet", hasHeroKnight);

            EditorGUILayout.Space(15);

            bool allRefsValid = warriorData != null && mageData != null && rogueData != null;

            EditorGUI.BeginDisabledGroup(!allRefsValid);
            if (GUILayout.Button("Assign Class Colors", GUILayout.Height(30)))
            {
                AssignClassColors();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!allRefsValid || !hasHeroKnight);
            if (GUILayout.Button("Assign Preview Frames + Default Sprite", GUILayout.Height(30)))
            {
                AssignIdleFramesAndDefaultSprite();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(!allRefsValid);
            if (GUILayout.Button("Full Setup (Colors + Previews)", GUILayout.Height(35)))
            {
                AssignClassColors();
                if (hasHeroKnight)
                    AssignIdleFramesAndDefaultSprite();
                Debug.Log("[ClassAppearanceSetup] Full setup complete!");
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "The paper-doll system uses one shared Peasant skeleton for all classes.\n" +
                "Class differences are handled by color tinting (classColor field).\n" +
                "Equipment sprites can be swapped via PaperDollRenderer.SetSlotSprite().",
                MessageType.None);
        }

        private void DrawStatus(string label, bool found)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(found ? "+" : "X", GUILayout.Width(15));
            GUILayout.Label(label);
            EditorGUILayout.EndHorizontal();
        }

        private void AssignClassColors()
        {
            SetClassColor(warriorData, "red", "Warrior");
            SetClassColor(mageData, "blue", "Mage");
            SetClassColor(rogueData, "green", "Rogue");

            AssetDatabase.SaveAssets();
            Debug.Log("[ClassAppearanceSetup] Class colors assigned to all 3 job classes.");
        }

        private void SetClassColor(JobClassData jobData, string color, string className)
        {
            Undo.RecordObject(jobData, $"Assign {className} class color");
            jobData.classColor = color;
            EditorUtility.SetDirty(jobData);
        }

        private void AssignIdleFramesAndDefaultSprite()
        {
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(HERO_KNIGHT_SPRITE_PATH);
            var spriteMap = new SortedDictionary<int, Sprite>();

            foreach (var asset in allAssets)
            {
                if (asset is Sprite sprite && sprite.name.StartsWith("HeroKnight_"))
                {
                    string numStr = sprite.name.Substring("HeroKnight_".Length);
                    if (int.TryParse(numStr, out int index))
                        spriteMap[index] = sprite;
                }
            }

            var idleFrames = new List<Sprite>();
            for (int i = IDLE_FRAME_START; i < IDLE_FRAME_START + IDLE_FRAME_COUNT; i++)
            {
                if (spriteMap.TryGetValue(i, out var sprite))
                    idleFrames.Add(sprite);
            }

            if (idleFrames.Count == 0)
            {
                Debug.LogError("[ClassAppearanceSetup] Could not find HeroKnight idle sprites!");
                return;
            }

            Sprite defaultSprite = idleFrames[0];
            Sprite[] idleArray = idleFrames.ToArray();

            AssignVisuals(warriorData, idleArray, defaultSprite, 8f);
            AssignVisuals(mageData, idleArray, defaultSprite, 8f);
            AssignVisuals(rogueData, idleArray, defaultSprite, 8f);

            AssetDatabase.SaveAssets();
            Debug.Log($"[ClassAppearanceSetup] Assigned {idleFrames.Count} idle frames and default sprite to all 3 job classes.");
        }

        private void AssignVisuals(JobClassData jobData, Sprite[] idleFrames, Sprite defaultSprite, float frameRate)
        {
            Undo.RecordObject(jobData, $"Assign visuals to {jobData.jobName}");
            jobData.idlePreviewFrames = idleFrames;
            jobData.defaultSprite = defaultSprite;
            jobData.idlePreviewFrameRate = frameRate;
            EditorUtility.SetDirty(jobData);
        }
    }
}
#endif
