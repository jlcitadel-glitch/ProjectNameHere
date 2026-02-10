#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ProjectName.UI.Editor
{
    /// <summary>
    /// Editor wizard that wires AnimatorOverrideControllers, idle preview frames,
    /// and default sprites into JobClassData ScriptableObjects.
    /// Run via Tools > ProjectName > Class Appearance Setup.
    /// </summary>
    public class ClassAppearanceSetup : EditorWindow
    {
        private const string OVERRIDES_PATH = "Assets/_Project/Art/Animations/Player/Overrides";
        private const string JOBS_PATH = "Assets/_Project/ScriptableObjects/Skills/Jobs";
        private const string HERO_KNIGHT_SPRITE_PATH = "Assets/ThirdParty/Hero Knight - Pixel Art/Sprites/HeroKnight.png";

        // Idle frames in the HeroKnight sheet (HeroKnight_0 through HeroKnight_7)
        private const int IDLE_FRAME_START = 0;
        private const int IDLE_FRAME_COUNT = 8;

        private JobClassData warriorData;
        private JobClassData mageData;
        private JobClassData rogueData;

        private AnimatorOverrideController warriorOverride;
        private AnimatorOverrideController mageOverride;
        private AnimatorOverrideController rogueOverride;

        private bool autoDetected;

        [MenuItem("Tools/ProjectName/Class Appearance Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<ClassAppearanceSetup>("Class Appearance Setup");
            window.minSize = new Vector2(450, 500);
        }

        private void OnEnable()
        {
            AutoDetectAssets();
        }

        private void AutoDetectAssets()
        {
            // Find JobClassData assets
            warriorData = AssetDatabase.LoadAssetAtPath<JobClassData>($"{JOBS_PATH}/Warrior.asset");
            mageData = AssetDatabase.LoadAssetAtPath<JobClassData>($"{JOBS_PATH}/Mage.asset");
            rogueData = AssetDatabase.LoadAssetAtPath<JobClassData>($"{JOBS_PATH}/Rogue.asset");

            // Find override controllers
            warriorOverride = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>($"{OVERRIDES_PATH}/Warrior.overrideController");
            mageOverride = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>($"{OVERRIDES_PATH}/Mage.overrideController");
            rogueOverride = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>($"{OVERRIDES_PATH}/Rogue.overrideController");

            autoDetected = true;
        }

        private void OnGUI()
        {
            GUILayout.Label("Class Appearance Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Assigns AnimatorOverrideControllers, idle preview frames, and default sprites to each JobClassData asset.",
                MessageType.Info);
            EditorGUILayout.Space(10);

            // Job Data references
            GUILayout.Label("Job Data Assets", EditorStyles.boldLabel);
            warriorData = (JobClassData)EditorGUILayout.ObjectField("Warrior", warriorData, typeof(JobClassData), false);
            mageData = (JobClassData)EditorGUILayout.ObjectField("Mage", mageData, typeof(JobClassData), false);
            rogueData = (JobClassData)EditorGUILayout.ObjectField("Rogue", rogueData, typeof(JobClassData), false);

            EditorGUILayout.Space(10);

            // Override controller references
            GUILayout.Label("Override Controllers", EditorStyles.boldLabel);
            warriorOverride = (AnimatorOverrideController)EditorGUILayout.ObjectField("Warrior", warriorOverride, typeof(AnimatorOverrideController), false);
            mageOverride = (AnimatorOverrideController)EditorGUILayout.ObjectField("Mage", mageOverride, typeof(AnimatorOverrideController), false);
            rogueOverride = (AnimatorOverrideController)EditorGUILayout.ObjectField("Rogue", rogueOverride, typeof(AnimatorOverrideController), false);

            EditorGUILayout.Space(10);

            // Status
            GUILayout.Label("Status", EditorStyles.boldLabel);
            DrawStatus("Warrior Data", warriorData != null);
            DrawStatus("Mage Data", mageData != null);
            DrawStatus("Rogue Data", rogueData != null);
            DrawStatus("Warrior Override", warriorOverride != null);
            DrawStatus("Mage Override", mageOverride != null);
            DrawStatus("Rogue Override", rogueOverride != null);

            bool hasHeroKnight = AssetDatabase.LoadMainAssetAtPath(HERO_KNIGHT_SPRITE_PATH) != null;
            DrawStatus("HeroKnight Sprite Sheet", hasHeroKnight);

            EditorGUILayout.Space(15);

            // Action buttons
            bool allRefsValid = warriorData != null && mageData != null && rogueData != null
                             && warriorOverride != null && mageOverride != null && rogueOverride != null;

            EditorGUI.BeginDisabledGroup(!allRefsValid);
            if (GUILayout.Button("Assign Override Controllers to Jobs", GUILayout.Height(30)))
            {
                AssignOverrideControllers();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!allRefsValid || !hasHeroKnight);
            if (GUILayout.Button("Assign HeroKnight Idle Frames + Default Sprite", GUILayout.Height(30)))
            {
                AssignIdleFramesAndDefaultSprite();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(!allRefsValid || !hasHeroKnight);
            if (GUILayout.Button("Do Everything (Full Setup)", GUILayout.Height(35)))
            {
                AssignOverrideControllers();
                AssignIdleFramesAndDefaultSprite();
                Debug.Log("[ClassAppearanceSetup] Full setup complete!");
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "After running setup, each class uses the same HeroKnight animations. " +
                "To differentiate classes:\n" +
                "1. Import per-class sprite sheets\n" +
                "2. Create animation clips from them\n" +
                "3. Open each Override Controller and drag in the new clips\n" +
                "4. Update idlePreviewFrames on each JobClassData with the class-specific idle frames",
                MessageType.None);
        }

        private void DrawStatus(string label, bool found)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(found ? "+" : "X", GUILayout.Width(15));
            GUILayout.Label(label);
            EditorGUILayout.EndHorizontal();
        }

        private void AssignOverrideControllers()
        {
            Undo.RecordObject(warriorData, "Assign Warrior Override Controller");
            warriorData.characterAnimator = warriorOverride;
            EditorUtility.SetDirty(warriorData);

            Undo.RecordObject(mageData, "Assign Mage Override Controller");
            mageData.characterAnimator = mageOverride;
            EditorUtility.SetDirty(mageData);

            Undo.RecordObject(rogueData, "Assign Rogue Override Controller");
            rogueData.characterAnimator = rogueOverride;
            EditorUtility.SetDirty(rogueData);

            AssetDatabase.SaveAssets();
            Debug.Log("[ClassAppearanceSetup] Override controllers assigned to all 3 job classes.");
        }

        private void AssignIdleFramesAndDefaultSprite()
        {
            // Load all sub-sprites from the HeroKnight sheet
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(HERO_KNIGHT_SPRITE_PATH);
            var spriteMap = new SortedDictionary<int, Sprite>();

            foreach (var asset in allAssets)
            {
                if (asset is Sprite sprite && sprite.name.StartsWith("HeroKnight_"))
                {
                    string numStr = sprite.name.Substring("HeroKnight_".Length);
                    if (int.TryParse(numStr, out int index))
                    {
                        spriteMap[index] = sprite;
                    }
                }
            }

            // Extract idle frames (HeroKnight_0 through HeroKnight_7)
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

            // Assign to all three classes
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
