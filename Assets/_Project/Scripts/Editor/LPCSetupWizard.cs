using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Rendering;

/// <summary>
/// Editor wizard that automates the full LPC character system setup:
/// 1. Downloads LPC spritesheet PNGs from GitHub
/// 2. Configures import settings (Point filter, 64 PPU, Multiple sprite mode)
/// 3. Slices spritesheets into individual frames
/// 4. Creates BodyPartData ScriptableObjects
/// 5. Creates AnimationStateFrameMap
/// 6. Creates CharacterAppearanceConfig
/// 7. Updates the Player prefab with layered sprite components
///
/// Usage: Unity Editor > Tools > LPC Character Setup Wizard
/// </summary>
public class LPCSetupWizard : EditorWindow
{
    // GitHub raw URL base for the LPC repo
    private const string GITHUB_RAW_BASE =
        "https://raw.githubusercontent.com/LiberatedPixelCup/Universal-LPC-Spritesheet-Character-Generator/refs/heads/master/spritesheets/";

    // Asset paths
    private const string SPRITE_ROOT = "Assets/_Project/Art/Sprites/Player/LPC";
    private const string DATA_ROOT = "Assets/_Project/ScriptableObjects/Character";
    private const string FRAME_MAP_PATH = DATA_ROOT + "/LPCSideViewFrameMap.asset";

    // LPC frame size
    private const int FRAME_SIZE = 64;

    // Pixels-per-unit for world scale (project uses 32 PPU after scale normalization)
    private const int PIXELS_PER_UNIT = 32;

    // For side-scrollers, we use row index 3 (right-facing) from directional animations.
    // Left-facing is handled by flipping transform.localScale.x at runtime.
    private const int SIDE_VIEW_ROW = 3; // Right-facing row

    // Animation definitions: animation name -> (row count per direction, frames per row, is directional)
    private static readonly AnimDef[] ANIMATIONS = new AnimDef[]
    {
        new AnimDef("idle",      4, 0, true),   // frame count detected from image width
        new AnimDef("walk",      4, 0, true),
        new AnimDef("run",       4, 0, true),
        new AnimDef("jump",      4, 0, true),
        new AnimDef("slash",     4, 0, true),
        new AnimDef("thrust",    4, 0, true),
        new AnimDef("spellcast", 4, 0, true),
        new AnimDef("shoot",     4, 0, true),
        new AnimDef("hurt",      1, 0, false),  // single row, not directional
    };

    // Hair styles to download (each becomes a separate BodyPartData with slot=Hair)
    // Removed: "short", "mohawk", "messy" (don't exist in LPC repo)
    // "ponytail", "braid", "wavy", "xlong" use fg/bg layers — handled by fg/ fallback
    private static readonly string[] HAIR_STYLES = new string[]
    {
        "long", "bob", "pixie", "ponytail",
        "afro", "cornrows", "dreadlocks_long", "dreadlocks_short",
        "natural", "braid", "curly_short", "curly_long",
        "bangs", "wavy", "buzzcut", "shorthawk", "xlong",
        "messy1", "loose", "plain", "jewfro"
    };

    // Equipment sprites to download (each becomes a BodyPartData for visual equipment)
    private static readonly PartDef[] EQUIPMENT_PARTS = new PartDef[]
    {
        // Warrior
        new PartDef("torso_chainmail",    BodyPartSlot.Torso,       "torso/chainmail/male",              "gray"),
        new PartDef("legs_armour",        BodyPartSlot.Legs,        "legs/armour/plate/male",            "steel"),
        new PartDef("weapon_longsword",   BodyPartSlot.WeaponFront, "weapon/sword/longsword",            "longsword"),
        // Mage
        new PartDef("torso_longsleeve",   BodyPartSlot.Torso,       "torso/clothes/longsleeve/longsleeve/male", "white"),
        new PartDef("legs_pants_teal",    BodyPartSlot.Legs,        "legs/pants/male",                   "teal"),
        new PartDef("weapon_staff",       BodyPartSlot.WeaponFront, "weapon/magic/simple/foreground",    "simple"),
        // Rogue
        new PartDef("torso_leather",      BodyPartSlot.Torso,       "torso/armour/leather/male",         "leather"),
        new PartDef("legs_boots",         BodyPartSlot.Legs,        "feet/boots/basic/male",             "brown"),
        new PartDef("weapon_dagger",      BodyPartSlot.WeaponFront, "weapon/sword/dagger",               "dagger"),
        // Shared default pants
        new PartDef("legs_pants",         BodyPartSlot.Legs,        "legs/pants/male",                   "brown"),
    };

    // Body parts to download (folder path relative to spritesheets/)
    private static readonly PartDef[] BODY_PARTS = BuildDefaultParts();

    private static PartDef[] BuildDefaultParts()
    {
        var parts = new List<PartDef>
        {
            new PartDef("body",  BodyPartSlot.Body,  "body/bodies/male",       "light"),
            new PartDef("head",  BodyPartSlot.Head,  "head/heads/human/male",  "light"),
        };
        foreach (var style in HAIR_STYLES)
            parts.Add(new PartDef($"hair_{style}", BodyPartSlot.Hair, $"hair/{style}/adult", null));
        return parts.ToArray();
    }

    private string skinColor = "light";
    private string bodyType = "male";
    private bool downloadComplete;
    private string statusMessage = "Ready";
    private Vector2 scrollPos;

    private struct AnimDef
    {
        public string name;
        public int rowCount;
        public int frameCount; // 0 = auto-detect from image width
        public bool isDirectional;

        public AnimDef(string name, int rowCount, int frameCount, bool isDirectional)
        {
            this.name = name;
            this.rowCount = rowCount;
            this.frameCount = frameCount;
            this.isDirectional = isDirectional;
        }
    }

    private struct PartDef
    {
        public string name;
        public BodyPartSlot slot;
        public string repoPath;
        public string colorVariant; // null = no color variant, use base file

        public PartDef(string name, BodyPartSlot slot, string repoPath, string colorVariant)
        {
            this.name = name;
            this.slot = slot;
            this.repoPath = repoPath;
            this.colorVariant = colorVariant;
        }
    }

    [MenuItem("Tools/LPC Character Setup Wizard")]
    public static void ShowWindow()
    {
        GetWindow<LPCSetupWizard>("LPC Character Setup");
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("LPC Character System Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This wizard sets up the entire LPC layered character system:\n" +
            "1. Downloads spritesheet PNGs from the LPC GitHub repo\n" +
            "2. Slices them and extracts side-view frames\n" +
            "3. Creates BodyPartData + AnimationStateFrameMap assets\n" +
            "4. Updates the Player prefab with layered sprite components",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

        skinColor = EditorGUILayout.TextField("Skin Color", skinColor);
        bodyType = EditorGUILayout.TextField("Body Type", bodyType);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(statusMessage, MessageType.None);

        EditorGUILayout.Space();

        if (GUILayout.Button("Step 1: Download & Import LPC Sprites", GUILayout.Height(30)))
        {
            DownloadAndImportSprites();
        }

        if (GUILayout.Button("Step 1b: Download & Import Equipment Sprites", GUILayout.Height(30)))
        {
            DownloadEquipmentSprites();
        }

        if (GUILayout.Button("Step 1c: Desaturate Hair Sprites (for tinting)", GUILayout.Height(30)))
        {
            DesaturateHairSprites();
        }

        if (GUILayout.Button("Step 2: Slice Spritesheets & Create Assets", GUILayout.Height(30)))
        {
            SliceAndCreateAssets();
        }

        if (GUILayout.Button("Step 3: Setup Player Prefab", GUILayout.Height(30)))
        {
            SetupPlayerPrefab();
        }

        EditorGUILayout.Space();

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Run All Steps", GUILayout.Height(40)))
        {
            DownloadAndImportSprites();
            AssetDatabase.Refresh();
            EditorApplication.delayCall += () =>
            {
                DesaturateHairSprites();
                AssetDatabase.Refresh();
                EditorApplication.delayCall += () =>
                {
                    SliceAndCreateAssets();
                    EditorApplication.delayCall += () =>
                    {
                        SetupPlayerPrefab();
                        statusMessage = "All steps complete!";
                    };
                };
            };
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    // ===== STEP 1: Download sprites from GitHub =====

    private void DownloadAndImportSprites()
    {
        statusMessage = "Downloading sprites...";

        // Update BODY_PARTS with current skin color / body type
        var parts = new List<PartDef>
        {
            new PartDef("body",  BodyPartSlot.Body,  $"body/bodies/{bodyType}",       skinColor),
            new PartDef("head",  BodyPartSlot.Head,  $"head/heads/human/{bodyType}",  skinColor),
        };
        foreach (var style in HAIR_STYLES)
            parts.Add(new PartDef($"hair_{style}", BodyPartSlot.Hair, $"hair/{style}/adult", null));

        int downloaded = 0;

        foreach (var part in parts)
        {
            string partDir = Path.Combine(SPRITE_ROOT, part.name);
            EnsureDirectory(partDir);

            foreach (var anim in ANIMATIONS)
            {
                string url;
                if (part.colorVariant != null)
                {
                    // Color variant is in a subdirectory: body/bodies/male/idle/light.png
                    url = $"{GITHUB_RAW_BASE}{part.repoPath}/{anim.name}/{part.colorVariant}.png";
                }
                else
                {
                    // No color variant: hair/long/idle.png (might not have color dirs)
                    url = $"{GITHUB_RAW_BASE}{part.repoPath}/{anim.name}.png";
                }

                string localPath = Path.Combine(partDir, $"{anim.name}.png");
                string fullPath = Path.GetFullPath(localPath);

                if (File.Exists(fullPath))
                {
                    downloaded++;
                    continue;
                }

                statusMessage = $"Downloading {part.name}/{anim.name}...";

                try
                {
                    using (var client = new System.Net.WebClient())
                    {
                        client.DownloadFile(url, fullPath);
                        downloaded++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[LPC Wizard] Failed to download {url}: {e.Message}");
                    bool recovered = false;

                    // Fallback 1: Try base file without color subdir (for color variant parts)
                    if (!recovered && part.colorVariant != null)
                    {
                        try
                        {
                            string altUrl = $"{GITHUB_RAW_BASE}{part.repoPath}/{anim.name}.png";
                            using (var client = new System.Net.WebClient())
                            {
                                client.DownloadFile(altUrl, fullPath);
                                downloaded++;
                                recovered = true;
                            }
                        }
                        catch (System.Exception) { }
                    }

                    // Fallback 2: Try fg/ subdirectory (for hair styles like ponytail that use bg/fg layers)
                    if (!recovered && part.colorVariant == null)
                    {
                        try
                        {
                            string fgUrl = $"{GITHUB_RAW_BASE}{part.repoPath}/fg/{anim.name}.png";
                            using (var client = new System.Net.WebClient())
                            {
                                client.DownloadFile(fgUrl, fullPath);
                                downloaded++;
                                recovered = true;
                            }
                        }
                        catch (System.Exception) { }
                    }

                    if (!recovered)
                        Debug.LogWarning($"[LPC Wizard] All URL patterns failed for {part.name}/{anim.name}");
                }
            }
        }

        AssetDatabase.Refresh();

        // Configure import settings for all downloaded PNGs
        string[] pngs = Directory.GetFiles(Path.GetFullPath(SPRITE_ROOT), "*.png", SearchOption.AllDirectories);
        foreach (string png in pngs)
        {
            string assetPath = "Assets" + png.Replace(Path.GetFullPath("Assets"), "").Replace("\\", "/");
            ConfigureSpriteImport(assetPath);
        }

        AssetDatabase.Refresh();
        statusMessage = $"Downloaded {downloaded} sprites. Import settings configured.";
    }

    // Some LPC equipment uses different animation folder names than our standard names.
    // Key: our animation name, Values: alternative names to try in order.
    private static readonly Dictionary<string, string[]> EQUIP_ANIM_ALIASES = new Dictionary<string, string[]>
    {
        { "slash",  new[] { "slash", "attack_slash" } },
        { "thrust", new[] { "thrust", "attack_thrust" } },
    };

    private void DownloadEquipmentSprites()
    {
        statusMessage = "Downloading equipment sprites...";
        int downloaded = 0;

        foreach (var part in EQUIPMENT_PARTS)
        {
            string partDir = Path.Combine(SPRITE_ROOT, part.name);
            EnsureDirectory(partDir);

            foreach (var anim in ANIMATIONS)
            {
                string localPath = Path.Combine(partDir, $"{anim.name}.png");
                string fullPath = Path.GetFullPath(localPath);

                if (File.Exists(fullPath))
                {
                    downloaded++;
                    continue;
                }

                statusMessage = $"Downloading {part.name}/{anim.name}...";

                // Build list of animation name variants to try
                string[] animNames = EQUIP_ANIM_ALIASES.ContainsKey(anim.name)
                    ? EQUIP_ANIM_ALIASES[anim.name]
                    : new[] { anim.name };

                bool success = false;
                foreach (var animName in animNames)
                {
                    if (success) break;

                    // Pattern 1: {repoPath}/{animName}/{colorVariant}.png
                    try
                    {
                        string url = $"{GITHUB_RAW_BASE}{part.repoPath}/{animName}/{part.colorVariant}.png";
                        using (var client = new System.Net.WebClient())
                        {
                            client.DownloadFile(url, fullPath);
                            downloaded++;
                            success = true;
                        }
                    }
                    catch (System.Exception) { }

                    // Pattern 2: {repoPath}/{animName}.png (no color variant)
                    if (!success)
                    {
                        try
                        {
                            string url = $"{GITHUB_RAW_BASE}{part.repoPath}/{animName}.png";
                            using (var client = new System.Net.WebClient())
                            {
                                client.DownloadFile(url, fullPath);
                                downloaded++;
                                success = true;
                            }
                        }
                        catch (System.Exception) { }
                    }
                }

                if (!success)
                    Debug.Log($"[LPC Wizard] Equipment anim not available (expected): {part.name}/{anim.name}");
            }

            // Post-download: copy walk.png as fallback for missing idle/run/jump.
            // Most LPC equipment only provides walk + combat animations.
            // Walk frames are a valid substitute for idle/run/jump visuals.
            string walkSrc = Path.GetFullPath(Path.Combine(partDir, "walk.png"));
            if (File.Exists(walkSrc))
            {
                foreach (var fallback in new[] { "idle", "run", "jump" })
                {
                    string fallbackPath = Path.GetFullPath(Path.Combine(partDir, $"{fallback}.png"));
                    if (!File.Exists(fallbackPath))
                    {
                        File.Copy(walkSrc, fallbackPath);
                        Debug.Log($"[LPC Wizard] Using walk.png as {fallback}.png fallback for {part.name}");
                    }
                }
            }
        }

        AssetDatabase.Refresh();

        // Configure import settings for all downloaded PNGs
        string[] pngs = Directory.GetFiles(Path.GetFullPath(SPRITE_ROOT), "*.png", SearchOption.AllDirectories);
        foreach (string png in pngs)
        {
            string assetPath = "Assets" + png.Replace(Path.GetFullPath("Assets"), "").Replace("\\", "/");
            ConfigureSpriteImport(assetPath);
        }

        AssetDatabase.Refresh();
        statusMessage = $"Downloaded {downloaded} equipment sprites. Import settings configured.";
    }

    private void ConfigureSpriteImport(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PIXELS_PER_UNIT;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;

        // Enable read/write so we can inspect dimensions
        importer.isReadable = true;

        importer.SaveAndReimport();
    }

    // ===== STEP 2: Slice and create ScriptableObjects =====

    private void SliceAndCreateAssets()
    {
        statusMessage = "Slicing spritesheets...";

        EnsureDirectory(DATA_ROOT);
        EnsureDirectory(DATA_ROOT + "/BodyParts");
        EnsureDirectory(DATA_ROOT + "/Appearances");

        // Create the AnimationStateFrameMap first
        var frameMap = CreateFrameMap();

        // Track all created parts for the appearance config
        var createdParts = new Dictionary<BodyPartSlot, BodyPartData>();

        // Process each body part directory
        string[] partDirs = Directory.GetDirectories(Path.GetFullPath(SPRITE_ROOT));
        foreach (string partDirFull in partDirs)
        {
            string partName = Path.GetFileName(partDirFull);
            BodyPartSlot slot = GuessSlot(partName);

            var bodyPart = SliceBodyPart(partName, slot, frameMap);
            if (bodyPart != null)
            {
                createdParts[slot] = bodyPart;
            }
        }

        // Create a default appearance config
        if (createdParts.Count > 0)
        {
            CreateAppearanceConfig(createdParts);
        }

        // Create/update the BodyPartRegistry so save/load and character creation can resolve parts
        CreateBodyPartRegistry(createdParts);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        statusMessage = $"Created {createdParts.Count} body part assets + frame map + appearance config + registry.";
    }

    private BodyPartData SliceBodyPart(string partName, BodyPartSlot slot, AnimationStateFrameMap frameMap)
    {
        string partDir = Path.Combine(SPRITE_ROOT, partName);
        List<Sprite> allFrames = new List<Sprite>();

        // We need to match frame ordering to the AnimationStateFrameMap
        foreach (var entry in frameMap.entries)
        {
            string animName = entry.stateName.ToLower();
            // Map animator state names to LPC file names
            string lpcAnimName = MapStateToLPCFile(animName);
            string pngPath = $"{partDir}/{lpcAnimName}.png";

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            if (texture == null)
            {
                // Fill with nulls to maintain frame indexing
                for (int i = 0; i < entry.frameCount; i++)
                    allFrames.Add(null);
                continue;
            }

            // Slice this animation sheet
            int framesInSheet = texture.width / FRAME_SIZE;
            int rowsInSheet = texture.height / FRAME_SIZE;

            // Determine which row to use
            int targetRow;
            bool isDirectional = rowsInSheet >= 4;

            if (isDirectional)
            {
                targetRow = SIDE_VIEW_ROW; // Right-facing
            }
            else
            {
                targetRow = 0; // Single row (e.g., hurt)
            }

            // Ensure spritesheet is properly sliced
            var slicedSprites = SliceSpritesheet(pngPath, texture, framesInSheet, rowsInSheet);

            // Extract the target row's frames
            int framesToUse = Mathf.Min(framesInSheet, entry.frameCount);
            for (int f = 0; f < entry.frameCount; f++)
            {
                if (f < framesToUse)
                {
                    int spriteIndex = targetRow * framesInSheet + f;
                    if (spriteIndex < slicedSprites.Length && slicedSprites[spriteIndex] != null)
                    {
                        allFrames.Add(slicedSprites[spriteIndex]);
                    }
                    else
                    {
                        allFrames.Add(null);
                    }
                }
                else
                {
                    // Repeat last frame if sheet has fewer frames than expected
                    int lastIdx = targetRow * framesInSheet + (framesToUse - 1);
                    if (lastIdx >= 0 && lastIdx < slicedSprites.Length)
                        allFrames.Add(slicedSprites[lastIdx]);
                    else
                        allFrames.Add(null);
                }
            }
        }

        // Remove trailing nulls but keep internal ones
        while (allFrames.Count > 0 && allFrames[allFrames.Count - 1] == null)
            allFrames.RemoveAt(allFrames.Count - 1);

        if (allFrames.Count == 0)
        {
            Debug.LogWarning($"[LPC Wizard] No frames found for body part: {partName}");
            return null;
        }

        // Create the BodyPartData asset
        // Only Body/Head use skinColor suffix; hair, equipment, and weapons don't
        string assetSuffix = (slot == BodyPartSlot.Body || slot == BodyPartSlot.Head) ? $"_{skinColor}" : "";
        string assetPath = $"{DATA_ROOT}/BodyParts/{partName}{assetSuffix}.asset";
        var bodyPart = AssetDatabase.LoadAssetAtPath<BodyPartData>(assetPath);
        if (bodyPart == null)
        {
            bodyPart = CreateInstance<BodyPartData>();
            AssetDatabase.CreateAsset(bodyPart, assetPath);
        }

        bodyPart.partId = $"{partName}{assetSuffix}";
        // Strip slot prefix and title-case: "hair_dreadlocks_long" → "Dreadlocks Long"
        bodyPart.displayName = FormatDisplayName(partName, slot);
        bodyPart.slot = slot;
        bodyPart.frames = allFrames.ToArray();
        bodyPart.sortOrderOffset = 0;
        bodyPart.supportsTinting = (slot == BodyPartSlot.Body || slot == BodyPartSlot.Head || slot == BodyPartSlot.Hair);
        bodyPart.defaultTint = Color.white;
        // Weapon idle frames are tiny (just a handle); use a slash/attack frame instead.
        // Attack1 (slash) starts at frame index 20 in the standard frame map.
        if ((slot == BodyPartSlot.WeaponFront || slot == BodyPartSlot.WeaponBehind) && allFrames.Count > 23)
            bodyPart.previewSprite = allFrames[23] ?? allFrames[22] ?? allFrames[21] ?? allFrames[20] ?? allFrames[0];
        else
            bodyPart.previewSprite = allFrames.Count > 0 ? allFrames[0] : null;

        EditorUtility.SetDirty(bodyPart);
        Debug.Log($"[LPC Wizard] Created BodyPartData: {assetPath} ({allFrames.Count} frames)");
        return bodyPart;
    }

    private Sprite[] SliceSpritesheet(string assetPath, Texture2D texture, int cols, int rows)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return new Sprite[0];

        // Use ISpriteEditorDataProvider (replaces obsolete importer.spritesheet)
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        // Build sprite rects
        var spriteRects = new List<SpriteRect>();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                spriteRects.Add(new SpriteRect
                {
                    name = $"{Path.GetFileNameWithoutExtension(assetPath)}_{row}_{col}",
                    spriteID = GUID.Generate(),
                    rect = new Rect(
                        col * FRAME_SIZE,
                        (rows - 1 - row) * FRAME_SIZE, // Unity textures are bottom-up
                        FRAME_SIZE,
                        FRAME_SIZE),
                    alignment = SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f)
                });
            }
        }

        dataProvider.SetSpriteRects(spriteRects.ToArray());

        // Register name-to-GUID mappings (required in Unity 6)
        var nameFileIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        var nameFileIdPairs = spriteRects
            .Select(s => new SpriteNameFileIdPair(s.name, s.spriteID))
            .ToList();
        nameFileIdProvider.SetNameFileIdPairs(nameFileIdPairs);

        dataProvider.Apply();
        importer.SaveAndReimport();

        // Load all sprites from the asset
        var allObjects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        var sprites = allObjects.OfType<Sprite>().ToArray();

        // Sort by name to match our indexing (row_col)
        var sortedSprites = new Sprite[rows * cols];
        foreach (var sprite in sprites)
        {
            string spriteName = sprite.name;
            // Parse row_col from name
            string[] parts = spriteName.Split('_');
            if (parts.Length >= 2)
            {
                if (int.TryParse(parts[parts.Length - 2], out int r) &&
                    int.TryParse(parts[parts.Length - 1], out int c))
                {
                    int idx = r * cols + c;
                    if (idx >= 0 && idx < sortedSprites.Length)
                        sortedSprites[idx] = sprite;
                }
            }
        }

        return sortedSprites;
    }

    private AnimationStateFrameMap CreateFrameMap()
    {
        var frameMap = AssetDatabase.LoadAssetAtPath<AnimationStateFrameMap>(FRAME_MAP_PATH);
        if (frameMap == null)
        {
            frameMap = CreateInstance<AnimationStateFrameMap>();
            EnsureDirectory(Path.GetDirectoryName(FRAME_MAP_PATH));
            AssetDatabase.CreateAsset(frameMap, FRAME_MAP_PATH);
        }

        // State names must match Player.controller Animator states exactly.
        // MapStateToLPCFile() maps these back to LPC spritesheet filenames.
        int offset = 0;
        var entries = new List<AnimationStateFrameMap.StateFrameEntry>();

        // Order must match how we build BodyPartData.frames[]
        AddEntry(entries, "Idle",      ref offset, 4,  8f,  true);
        AddEntry(entries, "Run",       ref offset, 8,  10f, true);
        AddEntry(entries, "Jump",      ref offset, 6,  10f, false);
        AddEntry(entries, "Fall",      ref offset, 2,  8f,  true);
        AddEntry(entries, "Attack1",   ref offset, 6,  12f, false); // LPC: slash
        AddEntry(entries, "Attack2",   ref offset, 8,  12f, false); // LPC: thrust
        AddEntry(entries, "Attack3",   ref offset, 7,  10f, false); // LPC: spellcast
        AddEntry(entries, "Roll",      ref offset, 8,  10f, false); // LPC: reuses run
        AddEntry(entries, "Hurt",      ref offset, 6,  8f,  false);
        AddEntry(entries, "Death",     ref offset, 6,  6f,  false); // LPC: reuses hurt
        AddEntry(entries, "WallSlide", ref offset, 2,  6f,  true);  // LPC: reuses idle
        AddEntry(entries, "LedgeGrab", ref offset, 2,  6f,  false); // LPC: reuses idle

        frameMap.entries = entries.ToArray();

        // Fallback to Idle
        frameMap.fallback = new AnimationStateFrameMap.StateFrameEntry
        {
            stateName = "Idle",
            startFrameIndex = 0,
            frameCount = 4,
            frameRate = 8f,
            loop = true
        };

        EditorUtility.SetDirty(frameMap);
        Debug.Log($"[LPC Wizard] Created AnimationStateFrameMap at {FRAME_MAP_PATH}");
        return frameMap;
    }

    private void AddEntry(List<AnimationStateFrameMap.StateFrameEntry> entries,
        string stateName, ref int offset, int frameCount, float frameRate, bool loop)
    {
        entries.Add(new AnimationStateFrameMap.StateFrameEntry
        {
            stateName = stateName,
            startFrameIndex = offset,
            frameCount = frameCount,
            frameRate = frameRate,
            loop = loop
        });
        offset += frameCount;
    }

    private void CreateAppearanceConfig(Dictionary<BodyPartSlot, BodyPartData> parts)
    {
        string path = DATA_ROOT + "/Appearances/Default.asset";
        EnsureDirectory(DATA_ROOT + "/Appearances");

        var config = AssetDatabase.LoadAssetAtPath<CharacterAppearanceConfig>(path);
        if (config == null)
        {
            config = CreateInstance<CharacterAppearanceConfig>();
            AssetDatabase.CreateAsset(config, path);
        }

        config.configId = "default";
        config.displayName = "Default";

        parts.TryGetValue(BodyPartSlot.Body, out var body);
        parts.TryGetValue(BodyPartSlot.Head, out var head);
        parts.TryGetValue(BodyPartSlot.Hair, out var hair);
        parts.TryGetValue(BodyPartSlot.Torso, out var torso);
        parts.TryGetValue(BodyPartSlot.Legs, out var legs);
        parts.TryGetValue(BodyPartSlot.WeaponBehind, out var wpnBehind);
        parts.TryGetValue(BodyPartSlot.WeaponFront, out var wpnFront);

        config.body = body;
        config.head = head;
        config.hair = hair;
        config.torso = torso;
        config.legs = legs;
        config.weaponBehind = wpnBehind;
        config.weaponFront = wpnFront;
        config.skinTint = Color.white;
        config.hairTint = Color.white;

        EditorUtility.SetDirty(config);
        Debug.Log($"[LPC Wizard] Created CharacterAppearanceConfig at {path}");
    }

    private void CreateBodyPartRegistry(Dictionary<BodyPartSlot, BodyPartData> parts)
    {
        // Place in Resources so it can be loaded at runtime via Resources.Load
        string resourcesPath = "Assets/_Project/Resources";
        string path = resourcesPath + "/BodyPartRegistry.asset";

        // Also check old location and migrate if found
        string oldPath = DATA_ROOT + "/BodyPartRegistry.asset";
        var registry = AssetDatabase.LoadAssetAtPath<BodyPartRegistry>(path);
        if (registry == null)
        {
            registry = AssetDatabase.LoadAssetAtPath<BodyPartRegistry>(oldPath);
            if (registry != null)
            {
                AssetDatabase.MoveAsset(oldPath, path);
            }
            else
            {
                registry = CreateInstance<BodyPartRegistry>();
                EnsureDirectory(resourcesPath);
                AssetDatabase.CreateAsset(registry, path);
            }
        }

        // Collect all BodyPartData assets in the project
        var allPartGuids = AssetDatabase.FindAssets("t:BodyPartData");
        var allParts = new List<BodyPartData>();
        foreach (var guid in allPartGuids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var part = AssetDatabase.LoadAssetAtPath<BodyPartData>(assetPath);
            if (part != null)
                allParts.Add(part);
        }

        registry.allParts = allParts.ToArray();
        EditorUtility.SetDirty(registry);
        Debug.Log($"[LPC Wizard] Created BodyPartRegistry at {path} ({allParts.Count} parts)");
    }

    // ===== STEP 3: Setup Player Prefab =====

    private void SetupPlayerPrefab()
    {
        statusMessage = "Setting up Player prefab...";

        string prefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            statusMessage = "ERROR: Player.prefab not found at " + prefabPath;
            return;
        }

        // Open prefab for editing
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        var prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

        try
        {
            // Add SortingGroup if missing
            if (prefabRoot.GetComponent<SortingGroup>() == null)
            {
                prefabRoot.AddComponent<SortingGroup>();
                Debug.Log("[LPC Wizard] Added SortingGroup to Player");
            }

            // Add AnimationFrameDriver if missing
            var frameDriver = prefabRoot.GetComponent<AnimationFrameDriver>();
            if (frameDriver == null)
            {
                frameDriver = prefabRoot.AddComponent<AnimationFrameDriver>();
                Debug.Log("[LPC Wizard] Added AnimationFrameDriver to Player");
            }

            // Set the frame map on the driver
            var frameMap = AssetDatabase.LoadAssetAtPath<AnimationStateFrameMap>(FRAME_MAP_PATH);
            if (frameMap != null)
            {
                var so = new SerializedObject(frameDriver);
                var prop = so.FindProperty("frameMap");
                if (prop != null)
                {
                    prop.objectReferenceValue = frameMap;
                    so.ApplyModifiedProperties();
                }
            }

            // Add LayeredSpriteController if missing
            var layeredSprite = prefabRoot.GetComponent<LayeredSpriteController>();
            if (layeredSprite == null)
            {
                layeredSprite = prefabRoot.AddComponent<LayeredSpriteController>();
                Debug.Log("[LPC Wizard] Added LayeredSpriteController to Player");
            }

            // Add PlayerAppearance if missing (bridges SkillManager job system to layered visuals)
            if (prefabRoot.GetComponent<PlayerAppearance>() == null)
            {
                prefabRoot.AddComponent<PlayerAppearance>();
                Debug.Log("[LPC Wizard] Added PlayerAppearance to Player");
            }

            // Disable the root SpriteRenderer (don't remove it to avoid breaking other refs)
            var rootSR = prefabRoot.GetComponent<SpriteRenderer>();
            if (rootSR != null)
            {
                rootSR.enabled = false;
                Debug.Log("[LPC Wizard] Disabled root SpriteRenderer (layered system takes over)");
            }

            // Save the prefab
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            statusMessage = "Player prefab setup complete!";
            Debug.Log("[LPC Wizard] Player prefab updated successfully");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        // Wire the default appearance config to JobClassData assets
        WireAppearanceToJobs();

        // Wire BodyPartRegistry to SaveManager
        WireRegistryToSaveManager();
    }

    private void WireAppearanceToJobs()
    {
        string configPath = DATA_ROOT + "/Appearances/Default.asset";
        var config = AssetDatabase.LoadAssetAtPath<CharacterAppearanceConfig>(configPath);
        if (config == null) return;

        // Find all JobClassData assets and set defaultAppearance if null
        string[] guids = AssetDatabase.FindAssets("t:JobClassData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var jobData = AssetDatabase.LoadAssetAtPath<JobClassData>(path);
            if (jobData != null && jobData.defaultAppearance == null)
            {
                jobData.defaultAppearance = config;
                EditorUtility.SetDirty(jobData);
                Debug.Log($"[LPC Wizard] Wired default appearance to {jobData.jobName}");
            }
        }

        AssetDatabase.SaveAssets();
    }

    private void WireRegistryToSaveManager()
    {
        string registryPath = DATA_ROOT + "/BodyPartRegistry.asset";
        var registry = AssetDatabase.LoadAssetAtPath<BodyPartRegistry>(registryPath);
        if (registry == null) return;

        // Find the SaveManager in the scene or on a prefab
        var saveManager = UnityEngine.Object.FindAnyObjectByType<SaveManager>();
        if (saveManager != null)
        {
            var so = new SerializedObject(saveManager);
            var prop = so.FindProperty("bodyPartRegistry");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = registry;
                so.ApplyModifiedProperties();
                Debug.Log("[LPC Wizard] Wired BodyPartRegistry to SaveManager");
            }
        }
    }

    // ===== Step 1c: Desaturate Hair Sprites =====

    /// <summary>
    /// Converts all hair sprite textures to grayscale so that Image.color
    /// tinting produces the correct hue. Without this, the pre-colored
    /// sprites multiply with the tint and produce wrong colors.
    /// </summary>
    private void DesaturateHairSprites()
    {
        statusMessage = "Desaturating hair sprites for tinting...";

        string spriteRoot = Path.GetFullPath(SPRITE_ROOT);
        var hairDirs = Directory.GetDirectories(spriteRoot)
            .Where(d => Path.GetFileName(d).StartsWith("hair"))
            .ToList();

        // Also include the base "hair" directory if it exists
        string baseHairDir = Path.Combine(spriteRoot, "hair");
        if (Directory.Exists(baseHairDir) && !hairDirs.Contains(baseHairDir))
            hairDirs.Add(baseHairDir);

        int processed = 0;
        foreach (string dir in hairDirs)
        {
            string[] pngs = Directory.GetFiles(dir, "*.png");
            foreach (string png in pngs)
            {
                byte[] bytes = File.ReadAllBytes(png);
                var tex = new Texture2D(2, 2);
                tex.LoadImage(bytes);

                var pixels = tex.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    // Luminance-weighted grayscale, preserving alpha
                    byte gray = (byte)(pixels[i].r * 0.299f + pixels[i].g * 0.587f + pixels[i].b * 0.114f);
                    pixels[i] = new Color32(gray, gray, gray, pixels[i].a);
                }
                tex.SetPixels32(pixels);

                byte[] pngBytes = tex.EncodeToPNG();
                File.WriteAllBytes(png, pngBytes);
                DestroyImmediate(tex);
                processed++;
            }
        }

        AssetDatabase.Refresh();
        statusMessage = $"Desaturated {processed} hair sprites across {hairDirs.Count} directories.";
        Debug.Log($"[LPC Wizard] Desaturated {processed} hair sprites for tinting.");
    }

    // ===== Helpers =====

    private string MapStateToLPCFile(string stateName)
    {
        // Maps Player.controller Animator state names to LPC spritesheet filenames
        return stateName.ToLower() switch
        {
            "idle" => "idle",
            "run" => "run",
            "jump" => "jump",
            "fall" => "jump",           // fall reuses jump sheet
            "attack1" => "slash",       // melee attack
            "attack2" => "thrust",      // spear/stab attack
            "attack3" => "spellcast",   // magic attack
            "roll" => "run",            // roll reuses run sheet
            "hurt" => "hurt",
            "death" => "hurt",          // death reuses hurt sheet
            "wallslide" => "idle",      // wall slide reuses idle sheet
            "ledgegrab" => "idle",      // ledge grab reuses idle sheet
            _ => stateName.ToLower()
        };
    }

    private string FormatDisplayName(string partName, BodyPartSlot slot)
    {
        // Strip known slot prefixes: "hair_long" → "long", "torso_chainmail" → "chainmail"
        string raw = partName;
        string[] prefixes = { "hair_", "torso_", "legs_", "feet_", "weapon_" };
        foreach (var prefix in prefixes)
        {
            if (raw.StartsWith(prefix))
            {
                raw = raw.Substring(prefix.Length);
                break;
            }
        }

        // Replace underscores with spaces and title-case each word
        var words = raw.Split('_');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
        }
        return string.Join(" ", words);
    }

    private BodyPartSlot GuessSlot(string dirName)
    {
        string lower = dirName.ToLower();
        if (lower.StartsWith("hair")) return BodyPartSlot.Hair;
        if (lower.StartsWith("torso")) return BodyPartSlot.Torso;
        if (lower.StartsWith("legs") || lower.StartsWith("feet")) return BodyPartSlot.Legs;
        if (lower.StartsWith("weapon")) return BodyPartSlot.WeaponFront;
        return lower switch
        {
            "body" => BodyPartSlot.Body,
            "head" => BodyPartSlot.Head,
            _ => BodyPartSlot.Body
        };
    }

    private void EnsureDirectory(string path)
    {
        string fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }
    }
}
