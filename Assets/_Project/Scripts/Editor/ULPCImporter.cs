using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor tool that imports ULPC spritesheet PNGs into sliced sprites,
/// then creates AnimationStateFrameMap, BodyPartData, and Animator Controller assets.
/// Extracts only the right-facing row (bottom row) from each spritesheet.
/// </summary>
public static class ULPCImporter
{
    // ── Animation definitions (order determines frame layout) ──────────────
    struct AnimDef
    {
        public string folder;      // subfolder name under Body/Male or Head/HumanMale
        public string stateName;   // Animator state name
        public int frameCount;     // frames per direction (width / 64)
        public float frameRate;
        public bool loop;
        public int rows;           // 4 for most, 1 for hurt/climb

        public AnimDef(string folder, string stateName, int frameCount, float frameRate, bool loop, int rows = 4)
        {
            this.folder = folder;
            this.stateName = stateName;
            this.frameCount = frameCount;
            this.frameRate = frameRate;
            this.loop = loop;
            this.rows = rows;
        }
    }

    static readonly AnimDef[] Animations = new AnimDef[]
    {
        new AnimDef("spellcast",    "Spellcast",  7,  10f, false),
        new AnimDef("thrust",       "Thrust",     8,  12f, false),
        new AnimDef("walk",         "Walk",       9,   9f, true),
        new AnimDef("slash",        "Slash",      6,  12f, false),
        new AnimDef("shoot",        "Shoot",     13,  12f, false),
        new AnimDef("hurt",         "Hurt",       6,   8f, false, 1),
        new AnimDef("climb",        "Climb",      6,   8f, true,  1),
        new AnimDef("idle",         "Idle",       2,   4f, true),
        new AnimDef("jump",         "Jump",       5,  10f, false),
        new AnimDef("sit",          "Sit",        3,   6f, false),
        new AnimDef("emote",        "Emote",      3,   6f, false),
        new AnimDef("run",          "Run",        8,  10f, true),
        new AnimDef("combat_idle",  "CombatIdle", 2,   4f, true),
        new AnimDef("backslash",    "Backslash", 13,  12f, false),
        new AnimDef("halfslash",    "Halfslash",  6,  12f, false),
    };

    static readonly string[] SkinTones = { "light", "amber", "olive", "taupe", "bronze", "brown", "black" };

    const string UlpcRoot        = "Assets/_Project/Art/Sprites/Player/ULPC";
    const string FrameMapPath    = "Assets/_Project/ScriptableObjects/Character/ULPCFrameMap.asset";
    const string BodyPartsDir    = "Assets/_Project/ScriptableObjects/Character/BodyParts";
    const string AnimCtrlPath    = "Assets/_Project/Art/Animations/Player/ULPC_Player.controller";
    const string AnimClipsDir    = "Assets/_Project/Art/Animations/Player/ULPC_Clips";
    const string AppearancePath  = "Assets/_Project/ScriptableObjects/Character/Appearances/ULPC_Default.asset";

    // ── Step 1: Configure texture import settings ─────────────────────────

    [MenuItem("Tools/ULPC/1 - Configure Texture Import Settings")]
    static void Step1_ConfigureTextures()
    {
        int count = 0;

        // Body and Head (keyed by skin tone)
        foreach (string sub in new[] { "Body/Male", "Head/HumanMale" })
            foreach (var anim in Animations)
                foreach (string skin in SkinTones)
                    count += ConfigureTexture($"{UlpcRoot}/{sub}/{anim.folder}/{skin}.png", skin, anim);

        // Angry face (skin-tone-matched head overlay, missing hurt/climb)
        foreach (var anim in Animations)
            foreach (string skin in SkinTones)
                count += ConfigureTexture($"{UlpcRoot}/Head/AngryMale/{anim.folder}/{skin}.png", skin, anim);

        AssetDatabase.Refresh();
        Debug.Log($"[ULPCImporter] Configured {count} textures with sprite slicing.");
    }

    static int ConfigureTexture(string assetPath, string variant, AnimDef anim)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return 0; // texture not present (e.g. climb missing for eyes)

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 64;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        // Build sprite rects for right-facing row only (bottom row = y:0)
        var rects = new List<SpriteMetaData>();
        for (int i = 0; i < anim.frameCount; i++)
        {
            var meta = new SpriteMetaData();
            meta.name = $"{variant}_{anim.folder}_{i}";
            meta.rect = new Rect(i * 64, 0, 64, 64);
            meta.alignment = (int)SpriteAlignment.BottomCenter;
            meta.pivot = new Vector2(0.5f, 0f);
            rects.Add(meta);
        }
#pragma warning disable CS0618
        importer.spritesheet = rects.ToArray();
#pragma warning restore CS0618

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        return 1;
    }

    // ── Step 2: Create frame map and BodyPartData assets ──────────────────

    [MenuItem("Tools/ULPC/2 - Create Frame Map and BodyPart Assets")]
    static void Step2_CreateAssets()
    {
        EnsureDirectory("Assets/_Project/ScriptableObjects/Character");
        EnsureDirectory($"{BodyPartsDir}/Body");
        EnsureDirectory($"{BodyPartsDir}/Head");

        // ── AnimationStateFrameMap ────────────────────────────────────────
        var frameMap = ScriptableObject.CreateInstance<AnimationStateFrameMap>();
        var entries = new List<AnimationStateFrameMap.StateFrameEntry>();
        int startIndex = 0;

        foreach (var anim in Animations)
        {
            entries.Add(new AnimationStateFrameMap.StateFrameEntry
            {
                stateName = anim.stateName,
                startFrameIndex = startIndex,
                frameCount = anim.frameCount,
                frameRate = anim.frameRate,
                loop = anim.loop
            });
            startIndex += anim.frameCount;
        }

        // Death reuses Hurt frames but holds on the last frame (face-down)
        var hurtEntry = entries.Find(e => e.stateName == "Hurt");
        entries.Add(new AnimationStateFrameMap.StateFrameEntry
        {
            stateName = "Death",
            startFrameIndex = hurtEntry.startFrameIndex,
            frameCount = hurtEntry.frameCount,
            frameRate = hurtEntry.frameRate,
            loop = false
        });

        frameMap.entries = entries.ToArray();
        frameMap.fallback = new AnimationStateFrameMap.StateFrameEntry
        {
            stateName = "Idle",
            startFrameIndex = entries.Find(e => e.stateName == "Idle").startFrameIndex,
            frameCount = 2,
            frameRate = 4f,
            loop = true
        };

        // Delete existing asset first (CreateAsset fails if one already exists)
        AssetDatabase.DeleteAsset(FrameMapPath);
        AssetDatabase.CreateAsset(frameMap, FrameMapPath);
        Debug.Log($"[ULPCImporter] Frame map: {entries.Count} entries, {startIndex} total frames.");

        // ── BodyPartData assets ───────────────────────────────────────────
        int totalFrames = startIndex;

        foreach (string skin in SkinTones)
        {
            CreateBodyPartAsset(
                $"body_male_{skin}", $"Body ({CapFirst(skin)})",
                BodyPartSlot.Body, "male", "Body/Male", skin, totalFrames,
                $"{BodyPartsDir}/Body/body_male_{skin}.asset");

            CreateBodyPartAsset(
                $"head_human_male_{skin}", $"Head ({CapFirst(skin)})",
                BodyPartSlot.Head, "male", "Head/HumanMale", skin, totalFrames,
                $"{BodyPartsDir}/Head/head_human_male_{skin}.asset");
        }

        // Angry face — overlay on Eyes slot, renders above Head
        // (hurt/climb missing from ULPC faces → null frames for those anims)
        EnsureDirectory($"{BodyPartsDir}/Eyes");
        foreach (string skin in SkinTones)
        {
            CreateBodyPartAsset(
                $"face_angry_male_{skin}", $"Angry Face ({CapFirst(skin)})",
                BodyPartSlot.Eyes, "male", "Head/AngryMale", skin, totalFrames,
                $"{BodyPartsDir}/Eyes/face_angry_male_{skin}.asset");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ULPCImporter] All BodyPartData assets created.");
    }

    // ── Step 3: Create Animator Controller ─────────────────────────────────

    [MenuItem("Tools/ULPC/3 - Create Animator Controller")]
    static void Step3_CreateAnimatorController()
    {
        EnsureDirectory(AnimClipsDir);

        // Delete existing controller first (safe for re-runs)
        AssetDatabase.DeleteAsset(AnimCtrlPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(AnimCtrlPath);
        var rootStateMachine = controller.layers[0].stateMachine;

        // ── Parameters ────────────────────────────────────────────────────
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VelocityY", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsWallSliding", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsLedgeGrabbing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("InCombat", AnimatorControllerParameterType.Bool);
        // Triggers for one-shot animations
        controller.AddParameter("Slash", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Backslash", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Halfslash", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Thrust", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Spellcast", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Sit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Emote", AnimatorControllerParameterType.Trigger);
        // Legacy trigger: HealthSystem fires "Die" — route to Hurt state
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        // ── Create dummy clips and states ─────────────────────────────────
        // AnimationFrameDriver drives sprites via frame map, so clips are
        // empty single-frame placeholders. Their duration controls normalizedTime.
        var states = new Dictionary<string, AnimatorState>();

        foreach (var anim in Animations)
        {
            var clip = new AnimationClip { name = $"ULPC_{anim.stateName}" };

            // Set clip length: frameCount / frameRate seconds
            float clipLength = anim.frameCount / anim.frameRate;

            // Add a dummy curve so Unity respects the clip length.
            // Uses a harmless float property ("_DummyFrame") instead of m_IsActive
            // to avoid any risk of deactivating the GameObject.
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(clipLength, 1f));
            clip.SetCurve("", typeof(Animator), "DummyFrame", curve);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = anim.loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            string clipPath = $"{AnimClipsDir}/ULPC_{anim.stateName}.anim";
            AssetDatabase.DeleteAsset(clipPath);
            AssetDatabase.CreateAsset(clip, clipPath);

            var state = rootStateMachine.AddState(anim.stateName);
            state.motion = clip;
            state.writeDefaultValues = true;
            states[anim.stateName] = state;
        }

        // ── Set default state ─────────────────────────────────────────────
        rootStateMachine.defaultState = states["Idle"];

        // ── Transitions ───────────────────────────────────────────────────

        // -- Idle → Run (Speed > 0.1)
        var t = states["Idle"].AddTransition(states["Run"]);
        t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        // -- Run → Idle (Speed < 0.1)
        t = states["Run"].AddTransition(states["Idle"]);
        t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // -- Idle → CombatIdle (InCombat = true)
        t = states["Idle"].AddTransition(states["CombatIdle"]);
        t.hasExitTime = false; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0, "InCombat");

        // -- CombatIdle → Idle (InCombat = false)
        t = states["CombatIdle"].AddTransition(states["Idle"]);
        t.hasExitTime = false; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "InCombat");

        // -- Idle/Run → Jump (not grounded, VelocityY > 0.1)
        foreach (string src in new[] { "Idle", "Run", "CombatIdle" })
        {
            t = states[src].AddTransition(states["Jump"]);
            t.hasExitTime = false; t.duration = 0.05f;
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VelocityY");
        }

        // -- Jump → Idle (grounded)
        t = states["Jump"].AddTransition(states["Idle"]);
        t.hasExitTime = false; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

        // -- Climb (wall slide)
        foreach (string src in new[] { "Idle", "Run", "Jump" })
        {
            t = states[src].AddTransition(states["Climb"]);
            t.hasExitTime = false; t.duration = 0.05f;
            t.AddCondition(AnimatorConditionMode.If, 0, "IsWallSliding");
        }
        t = states["Climb"].AddTransition(states["Idle"]);
        t.hasExitTime = false; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWallSliding");

        // -- AnyState → attack/action triggers (return to Idle on exit time)
        string[] triggerAnims = { "Slash", "Backslash", "Halfslash", "Thrust", "Shoot", "Spellcast", "Hurt", "Sit", "Emote" };
        foreach (string trigName in triggerAnims)
        {
            t = rootStateMachine.AddAnyStateTransition(states[trigName]);
            t.hasExitTime = false; t.duration = 0f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0, trigName);

            // Return to Idle after clip finishes
            t = states[trigName].AddTransition(states["Idle"]);
            t.hasExitTime = true; t.exitTime = 0.95f; t.duration = 0.05f;
        }

        // -- Death state: plays Hurt anim and holds on last frame (face-down)
        {
            var hurtClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimClipsDir}/ULPC_Hurt.anim");
            var deathState = rootStateMachine.AddState("Death");
            deathState.motion = hurtClip; // reuse Hurt clip
            deathState.writeDefaultValues = true;
            // No exit transition — stays in Death forever
            states["Death"] = deathState;

            t = rootStateMachine.AddAnyStateTransition(deathState);
            t.hasExitTime = false; t.duration = 0f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0, "Die");
        }

        // -- Walk state: available but not auto-transitioned
        // Walk can be reached via script: animator.Play("Walk")

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ULPCImporter] Created Animator Controller at {AnimCtrlPath} with {states.Count} states.");
    }

    // ── Step 4: Create default CharacterAppearanceConfig ──────────────────

    [MenuItem("Tools/ULPC/4 - Create Default Appearance Config")]
    static void Step4_CreateAppearanceConfig()
    {
        EnsureDirectory("Assets/_Project/ScriptableObjects/Character/Appearances");

        var config = ScriptableObject.CreateInstance<CharacterAppearanceConfig>();
        config.configId = "ulpc_default";
        config.displayName = "ULPC Default (Light)";
        config.bodyType = "male";

        // Load body and head for "light" skin tone
        var body = AssetDatabase.LoadAssetAtPath<BodyPartData>($"{BodyPartsDir}/Body/body_male_light.asset");
        var head = AssetDatabase.LoadAssetAtPath<BodyPartData>($"{BodyPartsDir}/Head/head_human_male_light.asset");

        if (body == null || head == null)
        {
            Debug.LogError("[ULPCImporter] Run Step 2 first — body/head assets not found.");
            return;
        }

        config.body = body;
        config.head = head;

        AssetDatabase.DeleteAsset(AppearancePath);
        AssetDatabase.CreateAsset(config, AppearancePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[ULPCImporter] Created default appearance config at {AppearancePath}");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    static void CreateBodyPartAsset(
        string partId, string displayName, BodyPartSlot slot,
        string bodyType, string subFolder, string skin,
        int totalFrames, string outputPath)
    {
        var part = ScriptableObject.CreateInstance<BodyPartData>();
        part.partId = partId;
        part.displayName = displayName;
        part.slot = slot;
        part.bodyTypeTag = bodyType;
        part.supportsTinting = false; // skin tone baked into sprites
        part.defaultTint = Color.white;
        part.tintCategory = TintCategory.None;

        // Collect sprites from all animation sheets in frame-map order
        var frames = new List<Sprite>();

        foreach (var anim in Animations)
        {
            string texPath = $"{UlpcRoot}/{subFolder}/{anim.folder}/{skin}.png";
            var sprites = LoadSpritesFromTexture(texPath, anim.frameCount);

            if (sprites == null || sprites.Length != anim.frameCount)
            {
                // Missing PNGs are expected for angry heads (no hurt/climb in ULPC faces)
                if (sprites == null || sprites.Length == 0)
                    Debug.LogWarning($"[ULPCImporter] No sprites from {texPath} — filling {anim.frameCount} null frames");
                else
                    Debug.LogError($"[ULPCImporter] Expected {anim.frameCount} sprites from {texPath}, got {sprites.Length}");
                for (int i = 0; i < anim.frameCount; i++)
                    frames.Add(sprites != null && i < sprites.Length ? sprites[i] : null);
            }
            else
            {
                frames.AddRange(sprites);
            }
        }

        part.frames = frames.ToArray();

        // Preview = first idle frame
        int idleStart = 0;
        foreach (var a in Animations)
        {
            if (a.folder == "idle") break;
            idleStart += a.frameCount;
        }
        if (idleStart < frames.Count && frames[idleStart] != null)
            part.previewSprite = frames[idleStart];

        // Delete existing asset first (CreateAsset fails if one already exists)
        AssetDatabase.DeleteAsset(outputPath);
        AssetDatabase.CreateAsset(part, outputPath);
        Debug.Log($"[ULPCImporter] Created {outputPath} ({frames.Count} frames)");
    }

    static Sprite[] LoadSpritesFromTexture(string assetPath, int expectedCount)
    {
        var allObjects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        var sprites = new List<Sprite>();

        foreach (var obj in allObjects)
        {
            if (obj is Sprite sprite)
                sprites.Add(sprite);
        }

        // Sort by frame index extracted from name (format: "skin_anim_N")
        sprites.Sort((a, b) => ExtractFrameIndex(a.name).CompareTo(ExtractFrameIndex(b.name)));

        return sprites.ToArray();
    }

    static int ExtractFrameIndex(string spriteName)
    {
        int lastUnderscore = spriteName.LastIndexOf('_');
        if (lastUnderscore >= 0 && int.TryParse(spriteName.Substring(lastUnderscore + 1), out int idx))
            return idx;
        return 0;
    }

    static string CapFirst(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s.Substring(1);
    }

    static void EnsureDirectory(string assetPath)
    {
        string fullPath = Path.GetFullPath(assetPath);
        if (!Directory.Exists(fullPath))
            Directory.CreateDirectory(fullPath);
    }
}
