using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Scans existing LPC sprite folders on disk and creates BodyPartData ScriptableObject assets.
/// Handles both old-format folders (flat, LPC animation names like slash/thrust/walk)
/// and new-format folders (nested with _male/_female suffix, mapped names like attack1/attack2/run).
///
/// Usage: Unity Editor > Tools > LPC Import > 3 Build Assets from Local Files
/// </summary>
public static class LPCLocalAssetBuilder
{
    private const string SPRITE_ROOT = "Assets/_Project/Art/Sprites/Player/LPC";
    private const string DATA_ROOT = "Assets/_Project/ScriptableObjects/Character";
    private const string FRAME_MAP_PATH = DATA_ROOT + "/LPCSideViewFrameMap.asset";
    private const string REGISTRY_PATH = "Assets/_Project/Resources/BodyPartRegistry.asset";

    private const int FRAME_SIZE = 64;
    private const int OVERSIZE_FRAME = 192;
    private const int SIDE_VIEW_ROW = 3; // Right-facing row in LPC spritesheets

    // LPC animation name fallbacks: when the mapped name isn't found, try these alternatives
    private static readonly Dictionary<string, string[]> LPC_ANIM_FALLBACKS = new Dictionary<string, string[]>
    {
        { "attack1", new[] { "slash" } },
        { "attack2", new[] { "thrust" } },
        { "attack3", new[] { "spellcast", "shoot" } },
        { "run", new[] { "walk" } },
    };

    // Prefix-to-slot mapping for old-format flat folders (hair_afro, legs_armour, etc.)
    private static readonly (string prefix, BodyPartSlot slot)[] PREFIX_SLOT_MAP = new[]
    {
        ("hair_", BodyPartSlot.Hair),
        ("legs_", BodyPartSlot.Legs),
        ("torso_", BodyPartSlot.Torso),
        ("weapon_", BodyPartSlot.WeaponFront),
        ("body_", BodyPartSlot.Body),
        ("head_", BodyPartSlot.Head),
    };

    // Directory name to slot mapping for new-format nested folders
    private static readonly Dictionary<string, BodyPartSlot> DIR_SLOT_MAP =
        new Dictionary<string, BodyPartSlot>(StringComparer.OrdinalIgnoreCase)
    {
        { "Accessories", BodyPartSlot.Accessories },
        { "Beard", BodyPartSlot.Beard },
        { "body", BodyPartSlot.Body },
        { "Cape", BodyPartSlot.Cape },
        { "Feet", BodyPartSlot.Feet },
        { "Gloves", BodyPartSlot.Gloves },
        { "hair", BodyPartSlot.Hair },
        { "head", BodyPartSlot.Head },
        { "Legs", BodyPartSlot.Legs },
        { "Shadow", BodyPartSlot.Shadow },
        { "Torso", BodyPartSlot.Torso },
        { "WeaponBehind", BodyPartSlot.WeaponBehind },
        { "WeaponFront", BodyPartSlot.WeaponFront },
    };

    // Prefix-based slot overrides for items inside the Head/ directory.
    // The LPC library puts hats, hair, eyes, accessories, etc. all under Head/.
    // We remap them to the correct BodyPartSlot based on folder name prefix.
    private static readonly (string prefix, BodyPartSlot slot)[] HEAD_PREFIX_REMAP = new[]
    {
        ("hat_", BodyPartSlot.Hat),
        ("hairextr_", BodyPartSlot.Hair),
        ("hairextl_", BodyPartSlot.Hair),
        ("hairtie_", BodyPartSlot.Hair),
        ("hair_", BodyPartSlot.Hair),
        ("ponytail_", BodyPartSlot.Hair),
        ("updo_", BodyPartSlot.Hair),
        ("eyes_", BodyPartSlot.Eyes),
        ("eyebrows_", BodyPartSlot.Eyes),
        ("eye_", BodyPartSlot.Eyes),
        ("expression_", BodyPartSlot.Eyes),
        ("facial_", BodyPartSlot.Eyes),
        ("accessory_", BodyPartSlot.Accessories),
        ("charm_", BodyPartSlot.Accessories),
        ("earring_", BodyPartSlot.Accessories),
        ("earrings_", BodyPartSlot.Accessories),
        ("necklace_", BodyPartSlot.Accessories),
        ("visor_", BodyPartSlot.Hat),
        ("headcover_", BodyPartSlot.Hat),
        ("bandana_", BodyPartSlot.Hat),
        // Remaining stay as Head: head_, ears_, nose_, wrinkes_, furry_, fins_, horns_, neck_
    };

    // Prefix-based slot overrides for items inside the body/ directory.
    // LPC puts wings, tails, wheelchairs under body/ but they belong elsewhere.
    private static readonly (string prefix, BodyPartSlot slot)[] BODY_PREFIX_REMAP = new[]
    {
        ("wings_", BodyPartSlot.Cape),
        ("tail_", BodyPartSlot.Accessories),
        ("wheelchair_", BodyPartSlot.Accessories),
        // body_ stays as Body
    };

    // Prefix-based slot overrides for items inside the Gloves/ directory.
    // LPC puts shoulders, rings, etc. under Gloves/ but they belong elsewhere.
    private static readonly (string prefix, BodyPartSlot slot)[] GLOVES_PREFIX_REMAP = new[]
    {
        ("shoulders_", BodyPartSlot.Shoulders),
        ("bauldron_", BodyPartSlot.Shoulders),
        ("ring_", BodyPartSlot.Accessories),
        // gloves_, bracers_, wrists_, arms_ stay as Gloves
    };

    /// <summary>
    /// Main entry point. Scans LPC sprite folders, creates BodyPartData assets,
    /// deletes legacy flat assets, and rebuilds the registry.
    /// </summary>
    public static void Run()
    {
        var frameMap = AssetDatabase.LoadAssetAtPath<AnimationStateFrameMap>(FRAME_MAP_PATH);
        if (frameMap == null)
        {
            Debug.LogError($"[LPC Local] Frame map not found at {FRAME_MAP_PATH}");
            return;
        }

        string fullRoot = Path.GetFullPath(SPRITE_ROOT).Replace('\\', '/');
        if (!Directory.Exists(fullRoot))
        {
            Debug.LogError($"[LPC Local] Sprite root not found: {SPRITE_ROOT}");
            return;
        }

        // Clean up all existing BodyPartData assets for a fresh rebuild
        int cleaned = CleanAllSlotAssets();
        if (cleaned > 0)
            Debug.Log($"[LPC Local] Cleaned {cleaned} existing assets for fresh rebuild");

        // Discover all leaf folders (directories containing PNGs to process)
        var leafFolders = DiscoverLeafFolders(fullRoot);
        Debug.Log($"[LPC Local] Found {leafFolders.Count} leaf folders to process");

        int created = 0, updated = 0, errors = 0;

        // NOTE: Do NOT use StartAssetEditing/StopAssetEditing here.
        // SliceSpritesheet calls SaveAndReimport() and then immediately loads sprites,
        // which requires the reimport to complete synchronously.

        for (int i = 0; i < leafFolders.Count; i++)
        {
            var leaf = leafFolders[i];

            if (i % 20 == 0)
            {
                EditorUtility.DisplayProgressBar("Building BodyPartData Assets",
                    $"Processing {leaf.folderName}... ({i}/{leafFolders.Count})",
                    (float)i / leafFolders.Count);
            }

            try
            {
                bool isNew;
                var asset = CreateBodyPartFromFolder(leaf, frameMap, out isNew);
                if (asset != null)
                {
                    if (isNew) created++;
                    else updated++;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LPC Local] Error processing {leaf.folderName}: {e.Message}\n{e.StackTrace}");
                errors++;
            }
        }

        // Delete old legacy flat assets
        int deleted = DeleteLegacyAssets();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Rebuild registry
        RebuildRegistry();

        EditorUtility.ClearProgressBar();
        Debug.Log($"[LPC Local] Complete! Created: {created}, Updated: {updated}, Deleted: {deleted}, Errors: {errors}");
    }

    // ===== Discovery =====

    private struct LeafFolder
    {
        public string fullPath;       // Full filesystem path
        public string assetPath;      // Unity asset path (Assets/_Project/...)
        public string folderName;     // Name of this leaf folder
        public BodyPartSlot slot;
        public bool isOldFormat;      // True if PNGs use LPC animation names
    }

    private static List<LeafFolder> DiscoverLeafFolders(string rootFullPath)
    {
        var result = new List<LeafFolder>();
        string projectRoot = Path.GetFullPath("Assets/..").Replace('\\', '/');
        if (!projectRoot.EndsWith("/")) projectRoot += "/";

        var topDirs = Directory.GetDirectories(rootFullPath);

        foreach (var topDir in topDirs)
        {
            string dirName = Path.GetFileName(topDir);

            // Skip .meta files captured as directories (shouldn't happen, but be safe)
            if (dirName.EndsWith(".meta")) continue;

            var subDirs = Directory.GetDirectories(topDir);
            var pngs = Directory.GetFiles(topDir, "*.png");

            if (pngs.Length > 0 && subDirs.Length == 0)
            {
                // Old format: PNGs directly in this top-level folder
                if (TryGetSlotFromPrefix(dirName, out var slot))
                {
                    result.Add(new LeafFolder
                    {
                        fullPath = topDir.Replace('\\', '/'),
                        assetPath = ToAssetPath(topDir, projectRoot),
                        folderName = dirName,
                        slot = slot,
                        isOldFormat = true,
                    });
                }
            }
            else if (subDirs.Length > 0)
            {
                // New format: subdirectories contain PNGs
                if (DIR_SLOT_MAP.TryGetValue(dirName, out var slot))
                {
                    // Select the appropriate prefix remap table for this directory
                    (string prefix, BodyPartSlot slot)[] prefixRemap = null;
                    if (dirName.Equals("head", StringComparison.OrdinalIgnoreCase)
                        || dirName.Equals("Head", StringComparison.OrdinalIgnoreCase))
                        prefixRemap = HEAD_PREFIX_REMAP;
                    else if (dirName.Equals("body", StringComparison.OrdinalIgnoreCase))
                        prefixRemap = BODY_PREFIX_REMAP;
                    else if (dirName.Equals("Gloves", StringComparison.OrdinalIgnoreCase))
                        prefixRemap = GLOVES_PREFIX_REMAP;

                    foreach (var subDir in subDirs)
                    {
                        string subName = Path.GetFileName(subDir);
                        if (subName.EndsWith(".meta")) continue;

                        if (Directory.GetFiles(subDir, "*.png").Length > 0)
                        {
                            // Remap by prefix if this directory has a remap table
                            BodyPartSlot leafSlot = slot;
                            if (prefixRemap != null)
                                leafSlot = RemapByPrefix(subName, prefixRemap, slot);

                            result.Add(new LeafFolder
                            {
                                fullPath = subDir.Replace('\\', '/'),
                                assetPath = ToAssetPath(subDir, projectRoot),
                                folderName = subName,
                                slot = leafSlot,
                                isOldFormat = false,
                            });
                        }
                    }
                }
            }
        }

        return result;
    }

    private static bool TryGetSlotFromPrefix(string dirName, out BodyPartSlot slot)
    {
        foreach (var (prefix, s) in PREFIX_SLOT_MAP)
        {
            if (dirName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                slot = s;
                return true;
            }
        }
        slot = default;
        return false;
    }

    private static BodyPartSlot RemapByPrefix(string folderName, (string prefix, BodyPartSlot slot)[] remapTable, BodyPartSlot defaultSlot)
    {
        foreach (var (prefix, slot) in remapTable)
        {
            if (folderName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return slot;
        }
        return defaultSlot;
    }

    private static string ToAssetPath(string fullPath, string projectRoot)
    {
        string normalized = fullPath.Replace('\\', '/');
        if (normalized.StartsWith(projectRoot))
            return normalized.Substring(projectRoot.Length);

        // Fallback: find "Assets/" in the path
        int idx = normalized.IndexOf("Assets/", StringComparison.Ordinal);
        return idx >= 0 ? normalized.Substring(idx) : normalized;
    }

    // ===== Asset Creation =====

    private static BodyPartData CreateBodyPartFromFolder(LeafFolder leaf, AnimationStateFrameMap frameMap, out bool isNew)
    {
        isNew = false;

        string partId = leaf.folderName;

        // Determine body type from suffix
        string bodyType = "universal";
        if (partId.EndsWith("_male"))
            bodyType = "male";
        else if (partId.EndsWith("_female"))
            bodyType = "female";

        // Build display name
        string displayName = BuildDisplayName(partId, leaf.slot);

        // Tint category from slot
        TintCategory tintCategory = GetTintCategory(leaf.slot);
        bool supportsTinting = tintCategory != TintCategory.None;

        // Exclusive group
        string exclusiveGroup = "";
        if (leaf.slot == BodyPartSlot.Hair || leaf.slot == BodyPartSlot.Hat)
            exclusiveGroup = "head_cover";

        // Build frames array from PNGs
        var allFrames = BuildFramesArray(leaf, frameMap);

        // Remove trailing nulls
        while (allFrames.Count > 0 && allFrames[allFrames.Count - 1] == null)
            allFrames.RemoveAt(allFrames.Count - 1);

        if (allFrames.Count == 0) return null;

        // Create or update BodyPartData asset
        string slotDir = leaf.slot.ToString();
        string assetDir = $"{DATA_ROOT}/BodyParts/{slotDir}";
        EnsureDirectory(assetDir);
        string assetPath = $"{assetDir}/{partId}.asset";

        var bodyPart = AssetDatabase.LoadAssetAtPath<BodyPartData>(assetPath);
        if (bodyPart == null)
        {
            bodyPart = ScriptableObject.CreateInstance<BodyPartData>();
            AssetDatabase.CreateAsset(bodyPart, assetPath);
            isNew = true;
        }

        bodyPart.partId = partId;
        bodyPart.displayName = displayName;
        bodyPart.slot = leaf.slot;
        bodyPart.bodyTypeTag = bodyType;
        bodyPart.frames = allFrames.ToArray();
        bodyPart.sortOrderOffset = 0;
        bodyPart.supportsTinting = supportsTinting;
        bodyPart.tintCategory = tintCategory;
        bodyPart.defaultTint = Color.white;
        bodyPart.exclusiveGroup = exclusiveGroup;
        bodyPart.previewSprite = allFrames.Count > 0 ? allFrames[0] : null;

        // Better preview for weapons: find a combat frame instead of idle
        if ((leaf.slot == BodyPartSlot.WeaponFront || leaf.slot == BodyPartSlot.WeaponBehind) && allFrames.Count > 26)
        {
            Sprite idleSprite = allFrames[0];
            int[][] combatRanges = { new[] { 26, 33 }, new[] { 20, 25 }, new[] { 34, 40 } };
            foreach (var range in combatRanges)
            {
                bool found = false;
                for (int i = range[0]; i <= range[1] && i < allFrames.Count; i++)
                {
                    if (allFrames[i] != null && allFrames[i] != idleSprite)
                    {
                        bodyPart.previewSprite = allFrames[i];
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }
        }

        EditorUtility.SetDirty(bodyPart);
        return bodyPart;
    }

    private static List<Sprite> BuildFramesArray(LeafFolder leaf, AnimationStateFrameMap frameMap)
    {
        var allFrames = new List<Sprite>();

        foreach (var entry in frameMap.entries)
        {
            string animName = entry.stateName.ToLower();

            // Try the mapped name first
            string pngPath = $"{leaf.assetPath}/{animName}.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);

            // If not found and this is an old-format folder, try LPC name fallbacks
            if (texture == null && LPC_ANIM_FALLBACKS.TryGetValue(animName, out var fallbacks))
            {
                foreach (var fallback in fallbacks)
                {
                    pngPath = $"{leaf.assetPath}/{fallback}.png";
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
                    if (texture != null) break;
                }
            }

            if (texture == null)
            {
                // Use idle.png frame 0 as static fallback for missing animations
                string idlePng = $"{leaf.assetPath}/idle.png";
                var idleTex = AssetDatabase.LoadAssetAtPath<Texture2D>(idlePng);
                if (idleTex != null)
                {
                    int fs = DetectFrameSize(idleTex);
                    int cols = idleTex.width / fs;
                    int rows = idleTex.height / fs;
                    int targetRow = rows >= 4 ? SIDE_VIEW_ROW : 0;
                    var idleSprites = SliceSpritesheet(idlePng, idleTex, cols, rows, fs);
                    int f0Idx = targetRow * cols;
                    Sprite staticFrame = (f0Idx < idleSprites.Length) ? idleSprites[f0Idx] : null;
                    for (int i = 0; i < entry.frameCount; i++)
                        allFrames.Add(staticFrame);
                    continue;
                }

                // No idle either - add nulls
                for (int i = 0; i < entry.frameCount; i++)
                    allFrames.Add(null);
                continue;
            }

            // Slice the found spritesheet
            int frameSize = DetectFrameSize(texture);
            int framesInSheet = texture.width / frameSize;
            int rowsInSheet = texture.height / frameSize;
            bool isDirectional = rowsInSheet >= 4;
            int sideViewRow = isDirectional ? SIDE_VIEW_ROW : 0;

            var slicedSprites = SliceSpritesheet(pngPath, texture, framesInSheet, rowsInSheet, frameSize);

            int framesToUse = Mathf.Min(framesInSheet, entry.frameCount);
            for (int f = 0; f < entry.frameCount; f++)
            {
                if (f < framesToUse)
                {
                    int spriteIndex = sideViewRow * framesInSheet + f;
                    allFrames.Add(spriteIndex < slicedSprites.Length ? slicedSprites[spriteIndex] : null);
                }
                else
                {
                    // Repeat last frame for padding
                    int lastIdx = sideViewRow * framesInSheet + (framesToUse - 1);
                    allFrames.Add(lastIdx >= 0 && lastIdx < slicedSprites.Length ? slicedSprites[lastIdx] : null);
                }
            }
        }

        return allFrames;
    }

    // ===== Sprite Slicing =====

    private static int DetectFrameSize(Texture2D texture)
    {
        // Oversized weapons use 192px frames
        if (texture.width % OVERSIZE_FRAME == 0 && texture.height % OVERSIZE_FRAME == 0)
        {
            int oversizeCols = texture.width / OVERSIZE_FRAME;
            int oversizeRows = texture.height / OVERSIZE_FRAME;
            if (oversizeCols >= 1 && oversizeRows >= 1 && oversizeCols <= 13)
                return OVERSIZE_FRAME;
        }
        return FRAME_SIZE;
    }

    private static Sprite[] SliceSpritesheet(string assetPath, Texture2D texture, int cols, int rows, int frameSize)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return new Sprite[0];

        // Configure texture import settings for sprite sheets
        if (importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = true;
        }

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

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
                        col * frameSize,
                        (rows - 1 - row) * frameSize,
                        frameSize,
                        frameSize),
                    alignment = SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f)
                });
            }
        }

        dataProvider.SetSpriteRects(spriteRects.ToArray());

        var nameFileIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        var nameFileIdPairs = spriteRects
            .Select(s => new SpriteNameFileIdPair(s.name, s.spriteID))
            .ToList();
        nameFileIdProvider.SetNameFileIdPairs(nameFileIdPairs);

        dataProvider.Apply();
        importer.SaveAndReimport();

        var allObjects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        var sprites = allObjects.OfType<Sprite>().ToArray();

        var sortedSprites = new Sprite[rows * cols];
        foreach (var sprite in sprites)
        {
            string[] parts = sprite.name.Split('_');
            if (parts.Length >= 2 &&
                int.TryParse(parts[parts.Length - 2], out int r) &&
                int.TryParse(parts[parts.Length - 1], out int c))
            {
                int idx = r * cols + c;
                if (idx >= 0 && idx < sortedSprites.Length)
                    sortedSprites[idx] = sprite;
            }
        }

        return sortedSprites;
    }

    // ===== Cleanup & Registry =====

    private static int CleanAllSlotAssets()
    {
        string bodyPartsRoot = $"{DATA_ROOT}/BodyParts";
        string fullRoot = Path.GetFullPath(bodyPartsRoot).Replace('\\', '/');
        if (!Directory.Exists(fullRoot)) return 0;

        var allAssets = Directory.GetFiles(fullRoot, "*.asset", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".meta"))
            .ToArray();

        int deleted = 0;
        foreach (var file in allAssets)
        {
            string assetPath = ToAssetPath(file, Path.GetFullPath("Assets/..").Replace('\\', '/') + "/");
            if (AssetDatabase.DeleteAsset(assetPath))
                deleted++;
        }

        if (deleted > 0)
            AssetDatabase.Refresh();

        return deleted;
    }

    private static int DeleteLegacyAssets()
    {
        // Legacy assets are flat in BodyParts/ root (not in slot subfolders)
        string legacyDir = $"{DATA_ROOT}/BodyParts";
        string fullLegacyDir = Path.GetFullPath(legacyDir).Replace('\\', '/');

        if (!Directory.Exists(fullLegacyDir))
            return 0;

        var legacyAssets = Directory.GetFiles(fullLegacyDir, "*.asset")
            .Where(f => !f.EndsWith(".meta"))
            .ToArray();

        int deleted = 0;
        foreach (var file in legacyAssets)
        {
            string assetPath = ToAssetPath(file, Path.GetFullPath("Assets/..").Replace('\\', '/') + "/");
            if (AssetDatabase.DeleteAsset(assetPath))
            {
                deleted++;
            }
            else
            {
                Debug.LogWarning($"[LPC Local] Failed to delete legacy asset: {assetPath}");
            }
        }

        if (deleted > 0)
            Debug.Log($"[LPC Local] Deleted {deleted} legacy flat assets from {legacyDir}");

        return deleted;
    }

    private static void RebuildRegistry()
    {
        string resourcesPath = "Assets/_Project/Resources";
        string path = REGISTRY_PATH;
        EnsureDirectory(resourcesPath);

        var registry = AssetDatabase.LoadAssetAtPath<BodyPartRegistry>(path);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<BodyPartRegistry>();
            AssetDatabase.CreateAsset(registry, path);
        }

        var allPartGuids = AssetDatabase.FindAssets("t:BodyPartData");
        var allParts = new List<BodyPartData>();
        foreach (var guid in allPartGuids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var part = AssetDatabase.LoadAssetAtPath<BodyPartData>(assetPath);
            if (part != null)
                allParts.Add(part);
        }

        // Sort by slot order then by partId for deterministic ordering
        allParts.Sort((a, b) =>
        {
            int slotCmp = ((int)a.slot).CompareTo((int)b.slot);
            return slotCmp != 0 ? slotCmp : string.Compare(a.partId, b.partId, StringComparison.Ordinal);
        });

        registry.allParts = allParts.ToArray();
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
        Debug.Log($"[LPC Local] Registry rebuilt with {allParts.Count} parts.");
    }

    // ===== Helpers =====

    private static TintCategory GetTintCategory(BodyPartSlot slot)
    {
        switch (slot)
        {
            case BodyPartSlot.Body:
            case BodyPartSlot.Head:
            case BodyPartSlot.Eyes:
                return TintCategory.Skin;
            case BodyPartSlot.Hair:
            case BodyPartSlot.Beard:
                return TintCategory.Hair;
            default:
                return TintCategory.None;
        }
    }

    private static string BuildDisplayName(string partId, BodyPartSlot slot)
    {
        // Strip slot prefix and body type suffix to make a readable name
        string name = partId;

        // Remove common prefixes that repeat the slot name
        string slotLower = slot.ToString().ToLower();
        if (name.StartsWith(slotLower + "_", StringComparison.OrdinalIgnoreCase))
            name = name.Substring(slotLower.Length + 1);

        // Remove body type suffixes
        if (name.EndsWith("_male")) name = name.Substring(0, name.Length - 5);
        else if (name.EndsWith("_female")) name = name.Substring(0, name.Length - 7);

        // Remove duplicate category prefixes (e.g., "beard_medium_beard" → "medium_beard")
        // This handles patterns from the LPC naming convention
        string[] parts = name.Split('_');
        if (parts.Length >= 3 && parts[0].Equals(parts[parts.Length - 1], StringComparison.OrdinalIgnoreCase))
        {
            name = string.Join("_", parts, 1, parts.Length - 1);
        }

        // Convert underscores to spaces and title-case
        name = name.Replace('_', ' ');
        if (name.Length > 0)
            name = char.ToUpper(name[0]) + name.Substring(1);

        return name;
    }

    private static void EnsureDirectory(string assetDir)
    {
        string fullPath = Path.GetFullPath(assetDir);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            AssetDatabase.Refresh();
        }
    }
}
