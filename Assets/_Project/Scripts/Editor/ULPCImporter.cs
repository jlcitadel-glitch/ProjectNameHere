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

    // ── Equipment piece definitions ─────────────────────────────────────────
    struct EquipDef
    {
        public string subFolder;   // path under Equipment/ in sprites folder
        public string variant;     // PNG filename (without .png)
        public string partId;      // BodyPartData.partId
        public string displayName; // BodyPartData.displayName
        public BodyPartSlot slot;  // rendering slot
        public string assetDir;    // subfolder under BodyParts/

        public EquipDef(string subFolder, string variant, string partId, string displayName, BodyPartSlot slot, string assetDir)
        {
            this.subFolder = subFolder;
            this.variant = variant;
            this.partId = partId;
            this.displayName = displayName;
            this.slot = slot;
            this.assetDir = assetDir;
        }
    }

    static readonly EquipDef[] EquipmentPieces = new EquipDef[]
    {
        new EquipDef("Equipment/Feet/BootsRevisedBrown", "brown", "feet_boots_revised_brown", "Brown Revised Boots", BodyPartSlot.Feet, "Feet"),
        new EquipDef("Equipment/Legs/PantsBlack",        "black", "legs_pants_black",          "Black Pants",         BodyPartSlot.Legs, "Legs"),
        new EquipDef("Equipment/Gloves/Steel",           "steel", "gloves_steel",              "Steel Gloves",        BodyPartSlot.Gloves, "Gloves"),
        new EquipDef("Equipment/Hat/ArmetSteel",         "steel", "hat_armet_steel",           "Steel Armet Helmet",  BodyPartSlot.Hat, "Hat"),
        new EquipDef("Equipment/Torso/PlateBlack",       "steel_black", "torso_plate_black",    "Steel Plate + Black Longsleeve", BodyPartSlot.Torso, "Torso"),
        // Mage equipment
        new EquipDef("Equipment/Hat/HoodPurple",          "purple",        "hat_hood_purple",       "Purple Hood",                    BodyPartSlot.Hat, "Hat"),
        new EquipDef("Equipment/Torso/RobePurple",        "purple_maroon", "torso_robe_purple",     "Purple Robe + Maroon Sash",      BodyPartSlot.Torso, "Torso"),
        new EquipDef("Equipment/Gloves/Brown",            "brown",         "gloves_brown",          "Brown Gloves",                   BodyPartSlot.Gloves, "Gloves"),
        new EquipDef("Equipment/Legs/PantaloonsNavy",     "navy",          "legs_pantaloons_navy",  "Navy Pantaloons",                BodyPartSlot.Legs, "Legs"),
        new EquipDef("Equipment/Feet/SlippersPurple",     "purple",        "feet_slippers_purple",  "Purple Slippers",                BodyPartSlot.Feet, "Feet"),
        // Rogue equipment
        new EquipDef("Equipment/Hat/BandanaCharcoal",     "charcoal",      "hat_bandana_charcoal",  "Charcoal Bandana",               BodyPartSlot.Hat, "Hat"),
        new EquipDef("Equipment/Torso/TunicCharcoal",     "charcoal",      "torso_tunic_charcoal",  "Charcoal Tunic + Sash",          BodyPartSlot.Torso, "Torso"),
        new EquipDef("Equipment/Gloves/Black",            "black",         "gloves_black",          "Black Gloves",                   BodyPartSlot.Gloves, "Gloves"),
        new EquipDef("Equipment/Legs/LeggingsCharcoal",   "charcoal",      "legs_leggings_charcoal","Charcoal Leggings",              BodyPartSlot.Legs, "Legs"),
        new EquipDef("Equipment/Feet/BootsFoldBrown",     "brown",         "feet_boots_fold_brown", "Brown Fold Boots",               BodyPartSlot.Feet, "Feet"),
    };

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

        // Equipment sprites
        foreach (var equip in EquipmentPieces)
            foreach (var anim in Animations)
                count += ConfigureTexture($"{UlpcRoot}/{equip.subFolder}/{anim.folder}/{equip.variant}.png", equip.variant, anim);

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

        // Equipment pieces
        foreach (var equip in EquipmentPieces)
        {
            EnsureDirectory($"{BodyPartsDir}/{equip.assetDir}");
            CreateBodyPartAsset(
                equip.partId, equip.displayName,
                equip.slot, "male", equip.subFolder, equip.variant, totalFrames,
                $"{BodyPartsDir}/{equip.assetDir}/{equip.partId}.asset");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Rebuild registry so it references the new asset GUIDs
        BodyPartRegistryBuilder.Build();

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

    // ── Step 6: Create EquipmentData assets and wire to Warrior ────────────

    struct EquipItemDef
    {
        public string equipmentId;
        public string displayName;
        public string description;
        public EquipmentSlotType slotType;
        public string visualPartId; // matches BodyPartData.partId from Step 2
        public int bonusSTR, bonusINT, bonusAGI;

        public EquipItemDef(string id, string name, string desc, EquipmentSlotType slot, string visualId, int str, int intel, int agi)
        {
            equipmentId = id; displayName = name; description = desc;
            slotType = slot; visualPartId = visualId;
            bonusSTR = str; bonusINT = intel; bonusAGI = agi;
        }
    }

    static readonly EquipItemDef[] WarriorEquipment = new EquipItemDef[]
    {
        new EquipItemDef("warrior_boots",  "Brown Boots",       "Sturdy revised boots.",              EquipmentSlotType.Feet,  "feet_boots_revised_brown", 1, 0, 0),
        new EquipItemDef("warrior_pants",  "Black Pants",       "Simple black trousers.",             EquipmentSlotType.Legs,  "legs_pants_black",         1, 0, 0),
        new EquipItemDef("warrior_gloves", "Steel Gauntlets",   "Heavy steel gauntlets.",             EquipmentSlotType.Hands, "gloves_steel",             1, 0, 0),
        new EquipItemDef("warrior_helm",   "Steel Armet",       "A full steel armet helmet.",         EquipmentSlotType.Head,  "hat_armet_steel",          2, 0, 0),
        new EquipItemDef("warrior_chest",  "Steel Plate Armor", "Steel plate over black longsleeve.", EquipmentSlotType.Armor, "torso_plate_black",        3, 0, 0),
    };

    static readonly EquipItemDef[] MageEquipment = new EquipItemDef[]
    {
        new EquipItemDef("mage_hood",      "Purple Hood",       "A mystic's hooded cowl.",            EquipmentSlotType.Head,  "hat_hood_purple",          0, 2, 0),
        new EquipItemDef("mage_robe",      "Purple Robe",       "Purple robe with maroon sash.",      EquipmentSlotType.Armor, "torso_robe_purple",        0, 3, 0),
        new EquipItemDef("mage_gloves",    "Leather Gloves",    "Worn leather gloves.",               EquipmentSlotType.Hands, "gloves_brown",             0, 1, 0),
        new EquipItemDef("mage_pants",     "Navy Pantaloons",   "Puffy scholar's pantaloons.",        EquipmentSlotType.Legs,  "legs_pantaloons_navy",     0, 1, 0),
        new EquipItemDef("mage_slippers",  "Purple Slippers",   "Soft mage's slippers.",              EquipmentSlotType.Feet,  "feet_slippers_purple",     0, 1, 0),
    };

    static readonly EquipItemDef[] RogueEquipment = new EquipItemDef[]
    {
        new EquipItemDef("rogue_bandana",  "Charcoal Bandana",  "A dark bandana for stealth.",         EquipmentSlotType.Head,  "hat_bandana_charcoal",     0, 0, 2),
        new EquipItemDef("rogue_tunic",    "Dark Tunic",        "Sleeveless charcoal tunic with sash.",EquipmentSlotType.Armor, "torso_tunic_charcoal",     1, 0, 2),
        new EquipItemDef("rogue_gloves",   "Black Gloves",      "Dark gloves for nimble fingers.",     EquipmentSlotType.Hands, "gloves_black",             0, 0, 1),
        new EquipItemDef("rogue_leggings", "Charcoal Leggings", "Tight-fitting dark leggings.",        EquipmentSlotType.Legs,  "legs_leggings_charcoal",   0, 0, 1),
        new EquipItemDef("rogue_boots",    "Leather Fold Boots","Soft brown leather boots.",           EquipmentSlotType.Feet,  "feet_boots_fold_brown",    0, 0, 1),
    };

    const string EquipmentDir = "Assets/_Project/Resources/Equipment";
    const string JobsDir = "Assets/_Project/Resources/Jobs";

    [MenuItem("Tools/ULPC/6 - Create Class Equipment")]
    static void Step6_CreateClassEquipment()
    {
        EnsureDirectory(EquipmentDir);

        CreateAndWireJobEquipment(WarriorEquipment, $"{JobsDir}/Warrior.asset", "Warrior");
        CreateAndWireJobEquipment(MageEquipment, $"{JobsDir}/Mage.asset", "Mage");
        CreateAndWireJobEquipment(RogueEquipment, $"{JobsDir}/Rogue.asset", "Rogue");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        // Rebuild registry so equipment BodyPartData refs are current
        BodyPartRegistryBuilder.Build();

        Debug.Log("[ULPCImporter] All class equipment setup complete.");
    }

    static void CreateAndWireJobEquipment(EquipItemDef[] items, string jobPath, string className)
    {
        var equipAssets = new List<EquipmentData>();

        foreach (var def in items)
        {
            var equip = ScriptableObject.CreateInstance<EquipmentData>();
            equip.equipmentId = def.equipmentId;
            equip.displayName = def.displayName;
            equip.description = def.description;
            equip.slotType = def.slotType;
            equip.bonusSTR = def.bonusSTR;
            equip.bonusINT = def.bonusINT;
            equip.bonusAGI = def.bonusAGI;

            equip.visualPart = FindBodyPartByPartId(def.visualPartId);
            if (equip.visualPart == null)
                Debug.LogWarning($"[ULPCImporter] Visual part '{def.visualPartId}' not found for {def.equipmentId} — run Step 2 first");

            string path = $"{EquipmentDir}/{def.equipmentId}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(equip, path);
            equipAssets.Add(equip);
            Debug.Log($"[ULPCImporter] Created equipment: {def.displayName} at {path}");
        }

        var job = AssetDatabase.LoadAssetAtPath<JobClassData>(jobPath);
        if (job != null)
        {
            var so = new SerializedObject(job);
            var starterProp = so.FindProperty("starterEquipment");
            if (starterProp != null)
            {
                starterProp.arraySize = equipAssets.Count;
                for (int i = 0; i < equipAssets.Count; i++)
                    starterProp.GetArrayElementAtIndex(i).objectReferenceValue = equipAssets[i];

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(job);
                Debug.Log($"[ULPCImporter] {className} starter gear: {equipAssets.Count} items (full replacement)");
            }
        }
        else
        {
            Debug.LogWarning($"[ULPCImporter] {className} job not found at {jobPath}");
        }
    }

    static BodyPartData FindBodyPartByPartId(string partId)
    {
        // Search all BodyPartData assets under BodyPartsDir
        string[] guids = AssetDatabase.FindAssets("t:BodyPartData", new[] { BodyPartsDir });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var part = AssetDatabase.LoadAssetAtPath<BodyPartData>(path);
            if (part != null && part.partId == partId)
                return part;
        }
        return null;
    }

    // ── Step 7: Configure Hair Textures ─────────────────────────────────
    // Hair sprites use the SAME per-animation PNG layout as Body/Head/Equipment:
    //   Hair/{style}/{anim}/{color}.png
    // Each PNG is a standard ULPC spritesheet (right-facing row at y:0).

    [MenuItem("Tools/ULPC/7 - Configure Hair Textures")]
    static void Step7_ConfigureHairTextures()
    {
        string hairRoot = $"{UlpcRoot}/Hair";
        if (!Directory.Exists(Path.GetFullPath(hairRoot)))
        {
            Debug.LogError($"[ULPCImporter] Hair root not found: {hairRoot}");
            return;
        }

        // Discover all styles (subdirectories of Hair/)
        var styleDirs = Directory.GetDirectories(Path.GetFullPath(hairRoot));
        int count = 0;
        int processed = 0;

        // Count total PNGs for progress bar
        int total = 0;
        foreach (string styleDir in styleDirs)
            foreach (var anim in Animations)
            {
                string animDir = Path.Combine(styleDir, anim.folder);
                if (Directory.Exists(animDir))
                    total += Directory.GetFiles(animDir, "*.png").Length;
            }

        try
        {
            foreach (string styleDir in styleDirs)
            {
                string styleName = Path.GetFileName(styleDir);

                foreach (var anim in Animations)
                {
                    string animDir = Path.Combine(styleDir, anim.folder);
                    if (!Directory.Exists(animDir)) continue;

                    foreach (string pngPath in Directory.GetFiles(animDir, "*.png"))
                    {
                        string color = Path.GetFileNameWithoutExtension(pngPath);
                        string assetPath = $"{hairRoot}/{styleName}/{anim.folder}/{color}.png";

                        processed++;
                        if (processed % 50 == 0)
                            EditorUtility.DisplayProgressBar("Configure Hair Textures",
                                $"{styleName}/{anim.folder}/{color} ({processed}/{total})",
                                (float)processed / Mathf.Max(total, 1));

                        count += ConfigureTexture(assetPath, color, anim);
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();
        Debug.Log($"[ULPCImporter] Configured {count} hair textures with sprite slicing.");
    }

    // ── Step 8: Create Hair BodyPart Assets ─────────────────────────────

    [MenuItem("Tools/ULPC/8 - Create Hair BodyPart Assets")]
    static void Step8_CreateHairBodyPartAssets()
    {
        string hairRoot = $"{UlpcRoot}/Hair";
        if (!Directory.Exists(Path.GetFullPath(hairRoot)))
        {
            Debug.LogError($"[ULPCImporter] Hair root not found: {hairRoot}");
            return;
        }

        EnsureDirectory($"{BodyPartsDir}/Hair");

        int totalFrames = 0;
        foreach (var anim in Animations)
            totalFrames += anim.frameCount;

        // Discover unique style+color combos by scanning walk/ directories
        // (walk is guaranteed to exist for every valid hair style)
        var styleDirs = Directory.GetDirectories(Path.GetFullPath(hairRoot));
        var combos = new List<(string style, string color)>();

        foreach (string styleDir in styleDirs)
        {
            string styleName = Path.GetFileName(styleDir);
            // Use walk/ as the canonical color list for this style
            string walkDir = Path.Combine(styleDir, "walk");
            if (!Directory.Exists(walkDir)) continue;

            foreach (string pngPath in Directory.GetFiles(walkDir, "*.png"))
            {
                string color = Path.GetFileNameWithoutExtension(pngPath);
                combos.Add((styleName, color));
            }
        }

        int count = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < combos.Count; i++)
            {
                var (style, color) = combos[i];

                if (i % 20 == 0)
                    EditorUtility.DisplayProgressBar("Create Hair BodyPart Assets",
                        $"{style}/{color} ({i}/{combos.Count})",
                        (float)i / combos.Count);

                CreateHairBodyPartAsset(style, color, hairRoot, totalFrames);
                count++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        BodyPartRegistryBuilder.Build();
        Debug.Log($"[ULPCImporter] Created {count} hair BodyPartData assets.");
    }

    static void CreateHairBodyPartAsset(string styleName, string color, string hairRoot, int totalFrames)
    {
        string partId = $"hair_{styleName}_male_{color}";
        string displayName = $"{CapFirst(styleName)} ({CapFirst(color.Replace('_', ' '))})";
        string outputPath = $"{BodyPartsDir}/Hair/{partId}.asset";
        string subFolder = $"Hair/{styleName}";

        // Reuse the same pattern as Body/Head/Equipment:
        // load per-animation PNGs from Hair/{style}/{anim}/{color}.png
        CreateBodyPartAsset(
            partId, displayName, BodyPartSlot.Hair, "male",
            subFolder, color, totalFrames, outputPath);
    }

    // ── Step 9: Create Civilian Clothing (preview-only, idle frames) ──────

    struct CivilianDef
    {
        public string subFolder;   // path under Equipment/ in sprites folder
        public string variant;     // PNG filename (without .png)
        public string partId;
        public string displayName;
        public BodyPartSlot slot;
        public string assetDir;    // subfolder under BodyParts/
        public bool supportsTinting;
        public Color defaultTint;

        public CivilianDef(string subFolder, string variant, string partId, string displayName,
            BodyPartSlot slot, string assetDir, bool tinting = false, Color? tint = null)
        {
            this.subFolder = subFolder;
            this.variant = variant;
            this.partId = partId;
            this.displayName = displayName;
            this.slot = slot;
            this.assetDir = assetDir;
            this.supportsTinting = tinting;
            this.defaultTint = tint ?? Color.white;
        }
    }

    static readonly CivilianDef[] CivilianPieces = new CivilianDef[]
    {
        new CivilianDef("Equipment/Torso/TshirtWhite", "white", "torso_tshirt_white", "White T-Shirt",
            BodyPartSlot.Torso, "Torso"),
        new CivilianDef("Equipment/Legs/ShortsNavy", "navy", "legs_shorts_navy", "Navy Shorts",
            BodyPartSlot.Legs, "Legs", true, new Color(0.15f, 0.15f, 0.4f, 1f)),
    };

    [MenuItem("Tools/ULPC/9 - Create Civilian Clothing")]
    static void Step9_CreateCivilianClothing()
    {
        // These assets only have idle frames (not the full 15-animation set).
        // They work for the character creation preview which uses previewSprite.
        var idleAnim = System.Array.Find(Animations, a => a.folder == "idle");

        foreach (var civ in CivilianPieces)
        {
            EnsureDirectory($"{BodyPartsDir}/{civ.assetDir}");

            // Configure texture import for the idle PNG
            string texPath = $"{UlpcRoot}/{civ.subFolder}/idle/{civ.variant}.png";
            ConfigureTexture(texPath, civ.variant, idleAnim);
            AssetDatabase.Refresh();

            // Load sliced sprites
            var sprites = LoadSpritesFromTexture(texPath, idleAnim.frameCount);
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogError($"[ULPCImporter] No sprites from {texPath} — ensure PNG exists");
                continue;
            }

            // Build BodyPartData with sparse frames (nulls for non-idle anims)
            var part = ScriptableObject.CreateInstance<BodyPartData>();
            part.partId = civ.partId;
            part.displayName = civ.displayName;
            part.slot = civ.slot;
            part.bodyTypeTag = "male";
            part.supportsTinting = civ.supportsTinting;
            part.defaultTint = civ.defaultTint;
            part.tintCategory = TintCategory.None;

            // Build frames array matching the full animation layout
            int totalFrames = 0;
            foreach (var a in Animations) totalFrames += a.frameCount;

            var frames = new Sprite[totalFrames];
            // Place idle sprites at the correct offset
            int idleStart = 0;
            foreach (var a in Animations)
            {
                if (a.folder == "idle") break;
                idleStart += a.frameCount;
            }
            for (int i = 0; i < sprites.Length && i < idleAnim.frameCount; i++)
                frames[idleStart + i] = sprites[i];

            part.frames = frames;
            part.previewSprite = sprites[0];

            string outputPath = $"{BodyPartsDir}/{civ.assetDir}/{civ.partId}.asset";
            AssetDatabase.DeleteAsset(outputPath);
            AssetDatabase.CreateAsset(part, outputPath);
            Debug.Log($"[ULPCImporter] Created civilian clothing: {civ.displayName} at {outputPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        BodyPartRegistryBuilder.Build();
        Debug.Log("[ULPCImporter] Civilian clothing setup complete.");
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
