using UnityEngine;
using UnityEditor;

/// <summary>
/// One-shot editor tool to wire ULPC assets to the Player prefab and JobClassData assets.
/// Run from Tools > ULPC > 5 - Wire Up Player.
/// Safe to delete after running.
/// </summary>
public static class ULPCWireUp
{
    const string PlayerPrefabPath    = "Assets/_Project/Prefabs/Player/Player.prefab";
    const string FrameMapPath        = "Assets/_Project/ScriptableObjects/Character/ULPCFrameMap.asset";
    const string AnimControllerPath  = "Assets/_Project/Art/Animations/Player/ULPC_Player.controller";
    const string AppearancePath      = "Assets/_Project/ScriptableObjects/Character/Appearances/ULPC_Default.asset";
    const string AngryFacePath        = "Assets/_Project/ScriptableObjects/Character/BodyParts/Eyes/face_angry_male_light.asset";

    static readonly string[] JobPaths = {
        "Assets/_Project/Resources/Jobs/Beginner.asset",
        "Assets/_Project/Resources/Jobs/Mage.asset",
        "Assets/_Project/Resources/Jobs/Rogue.asset",
        "Assets/_Project/Resources/Jobs/Warrior.asset",
    };

    [MenuItem("Tools/ULPC/5 - Wire Up Player")]
    static void WireUp()
    {
        int changes = 0;

        // ── Load assets ───────────────────────────────────────────────────
        var frameMap = AssetDatabase.LoadAssetAtPath<AnimationStateFrameMap>(FrameMapPath);
        var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimControllerPath);
        var appearance = AssetDatabase.LoadAssetAtPath<CharacterAppearanceConfig>(AppearancePath);

        var angryFace = AssetDatabase.LoadAssetAtPath<BodyPartData>(AngryFacePath);

        if (frameMap == null) { Debug.LogError($"[ULPCWireUp] Frame map not found: {FrameMapPath}"); return; }
        if (animController == null) { Debug.LogError($"[ULPCWireUp] Animator controller not found: {AnimControllerPath}"); return; }
        if (appearance == null) { Debug.LogError($"[ULPCWireUp] Appearance config not found: {AppearancePath}"); return; }
        if (angryFace == null) { Debug.LogWarning($"[ULPCWireUp] Angry face not found: {AngryFacePath} — run Steps 1+2 first"); }

        // ── Wire Player prefab ────────────────────────────────────────────
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null) { Debug.LogError($"[ULPCWireUp] Player prefab not found: {PlayerPrefabPath}"); return; }

        // Open prefab for editing
        var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

        // Animator → set controller
        var animator = prefabRoot.GetComponent<Animator>();
        if (animator != null)
        {
            animator.runtimeAnimatorController = animController;
            changes++;
            Debug.Log("[ULPCWireUp] Set Animator controller to ULPC_Player");
        }
        else
        {
            Debug.LogWarning("[ULPCWireUp] No Animator component on Player prefab root");
        }

        // AnimationFrameDriver → set frame map
        var frameDriver = prefabRoot.GetComponent<AnimationFrameDriver>();
        if (frameDriver != null)
        {
            // frameMap is private with [SerializeField], use SerializedObject
            var so = new SerializedObject(frameDriver);
            var prop = so.FindProperty("frameMap");
            if (prop != null)
            {
                prop.objectReferenceValue = frameMap;
                so.ApplyModifiedPropertiesWithoutUndo();
                changes++;
                Debug.Log("[ULPCWireUp] Set AnimationFrameDriver.frameMap to ULPCFrameMap");
            }
            else
            {
                Debug.LogWarning("[ULPCWireUp] Could not find 'frameMap' property on AnimationFrameDriver");
            }
        }
        else
        {
            Debug.LogWarning("[ULPCWireUp] No AnimationFrameDriver on Player prefab — add the component first");
        }

        // HealthSystem → ensure it exists (required by DamageFlashReaction and PlayerControllerScript)
        var healthSystem = prefabRoot.GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            healthSystem = prefabRoot.AddComponent<HealthSystem>();
            changes++;
            Debug.Log("[ULPCWireUp] Added HealthSystem to Player prefab");
        }

        // DamageFlashReaction → add component and assign angry face overlay
        var flashReaction = prefabRoot.GetComponent<DamageFlashReaction>();
        if (flashReaction == null)
            flashReaction = prefabRoot.AddComponent<DamageFlashReaction>();
        if (flashReaction != null && angryFace != null)
        {
            var so = new SerializedObject(flashReaction);
            var prop = so.FindProperty("angryFace");
            if (prop != null)
            {
                prop.objectReferenceValue = angryFace;
                so.ApplyModifiedPropertiesWithoutUndo();
                changes++;
                Debug.Log("[ULPCWireUp] Set DamageFlashReaction.angryFace to face_angry_male_light");
            }
        }

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        // ── Wire JobClassData assets ──────────────────────────────────────
        foreach (string jobPath in JobPaths)
        {
            var job = AssetDatabase.LoadAssetAtPath<JobClassData>(jobPath);
            if (job == null)
            {
                Debug.LogWarning($"[ULPCWireUp] Job not found: {jobPath}");
                continue;
            }

            var so = new SerializedObject(job);

            var appearanceProp = so.FindProperty("defaultAppearance");
            if (appearanceProp != null)
            {
                appearanceProp.objectReferenceValue = appearance;
                changes++;
            }

            var animProp = so.FindProperty("characterAnimator");
            if (animProp != null)
            {
                animProp.objectReferenceValue = animController;
                changes++;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(job);
            Debug.Log($"[ULPCWireUp] Wired {job.name}: appearance → ULPC_Default, animator → ULPC_Player");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ULPCWireUp] Done. {changes} fields updated.");
    }
}
