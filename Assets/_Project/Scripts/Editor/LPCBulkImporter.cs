using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

/// <summary>
/// JSON-driven bulk importer for the Universal LPC Spritesheet library.
/// Replaces the hardcoded LPCSetupWizard with a data-driven pipeline:
///   1. Downloads sheet_definitions/*.json from GitHub
///   2. Filters by safe license (CC0, OGA-BY, CC-BY — skips GPL, CC-BY-SA)
///   3. Downloads spritesheet PNGs for male + female body types
///   4. Slices spritesheets and creates BodyPartData assets
///   5. Rebuilds BodyPartRegistry
///
/// Usage: Unity Editor > Tools > LPC Bulk Importer
/// </summary>
public class LPCBulkImporter : EditorWindow
{
    // ===== Constants =====

    private const string GITHUB_API_TREE =
        "https://api.github.com/repos/LiberatedPixelCup/Universal-LPC-Spritesheet-Character-Generator/git/trees/master?recursive=1";
    private const string GITHUB_RAW_BASE =
        "https://raw.githubusercontent.com/LiberatedPixelCup/Universal-LPC-Spritesheet-Character-Generator/refs/heads/master/";

    private const string SPRITE_ROOT = "Assets/_Project/Art/Sprites/Player/LPC";
    private const string DATA_ROOT = "Assets/_Project/ScriptableObjects/Character";
    private const string FRAME_MAP_PATH = DATA_ROOT + "/LPCSideViewFrameMap.asset";
    private const string CREDITS_PATH = "Assets/_Project/Art/Sprites/Player/LPC_Import_Credits.txt";

    private const int FRAME_SIZE = 64;
    private const int OVERSIZE_FRAME = 192;
    private const int PIXELS_PER_UNIT = 32;
    private const int SIDE_VIEW_ROW = 3; // Right-facing row

    private static readonly string[] SAFE_LICENSES = { "CC0", "OGA-BY", "CC-BY" };
    private static readonly string[] UNSAFE_LICENSES = { "GPL", "CC-BY-SA" };
    private static readonly string[] WANTED_BODY_TYPES = { "male", "female" };

    // ===== Data Models =====

    [Serializable]
    private class LpcSheetDef
    {
        public string name;
        public int priority;
        public Dictionary<string, object> layer_1;
        public Dictionary<string, object> layer_2;
        public string[] variants;
        public string[] animations;
        public LpcCredit[] credits;
        public string[] path;
        public string type_name;
        public bool match_body_color;

        // Parsed from layer_1 body type paths
        [NonSerialized] public Dictionary<string, string> bodyTypePaths;
        // zPos from layer_1
        [NonSerialized] public int zPos;
        // Behind layer path and zPos (from layer_2 if present)
        [NonSerialized] public Dictionary<string, string> behindBodyTypePaths;
        [NonSerialized] public int behindZPos;
        // Source JSON path for debugging
        [NonSerialized] public string sourceJsonPath;
    }

    [Serializable]
    private class LpcCredit
    {
        public string file;
        public string notes;
        public string[] authors;
        public string[] licenses;
        public string[] urls;
    }

    private class ImportItem
    {
        public LpcSheetDef def;
        public string bodyType;
        public BodyPartSlot slot;
        public string layerPath; // path in spritesheets/ dir
        public string partId;
        public string displayName;
        public bool isBehindLayer;
        public bool isTintable;
        public TintCategory tintCategory;
    }

    // ===== Editor State =====

    private Vector2 scrollPos;
    private string statusMessage = "Ready. Click 'Discover' to scan the LPC library.";
    private float progress;
    private bool isRunning;

    private List<LpcSheetDef> allDefinitions = new List<LpcSheetDef>();
    private List<LpcSheetDef> safeDefinitions = new List<LpcSheetDef>();
    private List<LpcSheetDef> skippedDefinitions = new List<LpcSheetDef>();
    private List<ImportItem> importQueue = new List<ImportItem>();
    private List<string> creditLines = new List<string>();

    private bool showSkipped;
    private bool showImportQueue;
    private string skinVariant = "light";

    [MenuItem("Tools/LPC Bulk Importer Window")]
    public static void ShowWindow()
    {
        GetWindow<LPCBulkImporter>("LPC Bulk Importer");
    }

    /// <summary>
    /// Public entry point for discovery step. Called by LPCBulkImporterRunner.
    /// </summary>
    public void RunDiscoverStep()
    {
        DiscoverAndFilter();

        Debug.Log($"[LPC Bulk] Discovery complete: {allDefinitions.Count} total, " +
            $"{safeDefinitions.Count} safe, {skippedDefinitions.Count} skipped, " +
            $"{importQueue.Count} import items");

        foreach (var def in skippedDefinitions)
            Debug.Log($"[LPC Bulk] SKIPPED: {def.name} ({def.sourceJsonPath})");

        var slotCounts = new Dictionary<BodyPartSlot, int>();
        foreach (var item in importQueue)
        {
            if (!slotCounts.ContainsKey(item.slot))
                slotCounts[item.slot] = 0;
            slotCounts[item.slot]++;
        }
        foreach (var kvp in slotCounts.OrderBy(k => (int)k.Key))
            Debug.Log($"[LPC Bulk] Queue: {kvp.Key} = {kvp.Value} items");
    }

    /// <summary>
    /// Public entry point for full import. Called by LPCBulkImporterRunner.
    /// </summary>
    public void RunAllSteps()
    {
        RunAll();
        Debug.Log($"[LPC Bulk] Import complete: {importQueue.Count} items processed, " +
            $"{creditLines.Count} credit entries");
    }

    // ===== Editor GUI =====

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("LPC Full Library Importer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "JSON-driven bulk import from the Universal LPC Spritesheet Generator.\n" +
            "Filters by safe license (CC0, OGA-BY, CC-BY). Imports male + female body types.",
            MessageType.Info);

        EditorGUILayout.Space();
        skinVariant = EditorGUILayout.TextField("Skin Variant", skinVariant);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(statusMessage, MessageType.None);

        if (progress > 0 && progress < 1)
        {
            var rect = GUILayoutUtility.GetRect(18, 18, "TextField");
            EditorGUI.ProgressBar(rect, progress, $"{(int)(progress * 100)}%");
        }

        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(isRunning);

        if (GUILayout.Button("Step 1: Discover & Filter JSON Definitions", GUILayout.Height(28)))
            DiscoverAndFilter();

        EditorGUI.BeginDisabledGroup(importQueue.Count == 0);
        if (GUILayout.Button($"Step 2: Download PNGs ({importQueue.Count} items)", GUILayout.Height(28)))
            DownloadAllPNGs();

        if (GUILayout.Button("Step 3: Slice & Create Assets", GUILayout.Height(28)))
            SliceAndCreateAllAssets();
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Run All Steps", GUILayout.Height(36)))
            RunAll();
        GUI.backgroundColor = Color.white;

        EditorGUI.EndDisabledGroup();

        // Stats
        if (allDefinitions.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Total definitions: {allDefinitions.Count}  |  " +
                $"Safe: {safeDefinitions.Count}  |  Skipped (license): {skippedDefinitions.Count}  |  " +
                $"Import queue: {importQueue.Count}");
        }

        // Collapsible sections
        if (skippedDefinitions.Count > 0)
        {
            showSkipped = EditorGUILayout.Foldout(showSkipped, $"Skipped ({skippedDefinitions.Count})");
            if (showSkipped)
            {
                EditorGUI.indentLevel++;
                foreach (var def in skippedDefinitions)
                    EditorGUILayout.LabelField($"  {def.name} ({def.sourceJsonPath})");
                EditorGUI.indentLevel--;
            }
        }

        if (importQueue.Count > 0)
        {
            showImportQueue = EditorGUILayout.Foldout(showImportQueue, $"Import Queue ({importQueue.Count})");
            if (showImportQueue)
            {
                EditorGUI.indentLevel++;
                foreach (var item in importQueue.Take(50))
                    EditorGUILayout.LabelField($"  [{item.slot}] {item.displayName} ({item.bodyType})");
                if (importQueue.Count > 50)
                    EditorGUILayout.LabelField($"  ... and {importQueue.Count - 50} more");
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.EndScrollView();
    }

    // ===== Step 1: Discover & Filter =====

    private void DiscoverAndFilter()
    {
        isRunning = true;
        statusMessage = "Discovering JSON definitions from GitHub...";
        Repaint();

        try
        {
            allDefinitions.Clear();
            safeDefinitions.Clear();
            skippedDefinitions.Clear();
            importQueue.Clear();
            creditLines.Clear();

            // Get repo tree to find all JSON files in sheet_definitions/
            var jsonPaths = DiscoverJsonPaths();
            statusMessage = $"Found {jsonPaths.Count} JSON definitions. Downloading and filtering...";
            Repaint();

            // Download and parse each JSON
            int processed = 0;
            foreach (var jsonPath in jsonPaths)
            {
                processed++;
                if (processed % 10 == 0)
                {
                    progress = (float)processed / jsonPaths.Count * 0.5f;
                    statusMessage = $"Parsing {processed}/{jsonPaths.Count}: {jsonPath}";
                    Repaint();
                }

                var def = DownloadAndParseJson(jsonPath);
                if (def == null) continue;

                allDefinitions.Add(def);
            }

            // Filter by license and build import queue
            foreach (var def in allDefinitions)
            {
                bool anySafe = false;
                foreach (var bt in WANTED_BODY_TYPES)
                {
                    if (IsLicenseSafeForBodyType(def, bt))
                    {
                        anySafe = true;
                        break;
                    }
                }

                if (anySafe)
                {
                    safeDefinitions.Add(def);
                    BuildImportItems(def);
                }
                else
                {
                    skippedDefinitions.Add(def);
                    Debug.Log($"[LPC Bulk] Skipped (license): {def.name} — {def.sourceJsonPath}");
                }
            }

            statusMessage = $"Discovery complete. {safeDefinitions.Count} safe definitions, " +
                $"{importQueue.Count} import items. {skippedDefinitions.Count} skipped.";
        }
        catch (Exception e)
        {
            statusMessage = $"Error during discovery: {e.Message}";
            Debug.LogError($"[LPC Bulk] Discovery error: {e}");
        }
        finally
        {
            isRunning = false;
            progress = 0;
            Repaint();
        }
    }

    private List<string> DiscoverJsonPaths()
    {
        var paths = new List<string>();
        string json;

        using (var client = new WebClient())
        {
            client.Headers.Add("User-Agent", "Unity-LPC-Importer");
            json = client.DownloadString(GITHUB_API_TREE);
        }

        // Simple JSON parsing for the tree API response
        // Looking for paths matching sheet_definitions/**/*.json (not meta_*.json)
        int searchStart = 0;
        while (true)
        {
            int pathIdx = json.IndexOf("\"path\":", searchStart);
            if (pathIdx < 0) break;

            int valueStart = json.IndexOf('"', pathIdx + 7) + 1;
            int valueEnd = json.IndexOf('"', valueStart);
            string path = json.Substring(valueStart, valueEnd - valueStart);
            searchStart = valueEnd + 1;

            if (path.StartsWith("sheet_definitions/") &&
                path.EndsWith(".json") &&
                !Path.GetFileName(path).StartsWith("meta_"))
            {
                paths.Add(path);
            }
        }

        return paths;
    }

    private LpcSheetDef DownloadAndParseJson(string repoPath)
    {
        try
        {
            string url = GITHUB_RAW_BASE + repoPath;
            string json;

            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", "Unity-LPC-Importer");
                json = client.DownloadString(url);
            }

            var def = JsonUtility.FromJson<LpcSheetDef>(json);
            if (def == null || string.IsNullOrEmpty(def.name)) return null;

            def.sourceJsonPath = repoPath;

            // Parse layer_1 manually since JsonUtility can't handle dynamic keys
            ParseLayerPaths(json, "layer_1", out def.bodyTypePaths, out def.zPos);
            ParseLayerPaths(json, "layer_2", out def.behindBodyTypePaths, out def.behindZPos);

            // Parse credits manually since JsonUtility struggles with nested arrays
            def.credits = ParseCredits(json);

            // Parse variants
            def.variants = ParseStringArray(json, "variants");

            // Parse animations
            def.animations = ParseStringArray(json, "animations");

            // Parse path
            def.path = ParseStringArray(json, "path");

            return def;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LPC Bulk] Failed to parse {repoPath}: {e.Message}");
            return null;
        }
    }

    // ===== JSON Parsing Helpers =====
    // JsonUtility can't handle Dictionary or dynamic keys, so we parse manually.

    private void ParseLayerPaths(string json, string layerKey, out Dictionary<string, string> paths, out int zPos)
    {
        paths = new Dictionary<string, string>();
        zPos = 0;

        string searchKey = $"\"{layerKey}\"";
        int layerIdx = json.IndexOf(searchKey);
        if (layerIdx < 0) return;

        int braceStart = json.IndexOf('{', layerIdx);
        if (braceStart < 0) return;
        int braceEnd = FindMatchingBrace(json, braceStart);
        if (braceEnd < 0) return;

        string layerJson = json.Substring(braceStart, braceEnd - braceStart + 1);

        // Extract zPos
        int zPosIdx = layerJson.IndexOf("\"zPos\"");
        if (zPosIdx >= 0)
        {
            int colonIdx = layerJson.IndexOf(':', zPosIdx);
            int commaIdx = layerJson.IndexOf(',', colonIdx);
            int braceEndInner = layerJson.IndexOf('}', colonIdx);
            int end = commaIdx > 0 ? Math.Min(commaIdx, braceEndInner) : braceEndInner;
            string zPosStr = layerJson.Substring(colonIdx + 1, end - colonIdx - 1).Trim();
            int.TryParse(zPosStr, out zPos);
        }

        // Extract body type paths
        foreach (var bt in new[] { "male", "muscular", "female", "pregnant", "teen", "child" })
        {
            string btKey = $"\"{bt}\"";
            int btIdx = layerJson.IndexOf(btKey);
            if (btIdx < 0) continue;

            int valStart = layerJson.IndexOf('"', btIdx + btKey.Length) + 1;
            int valEnd = layerJson.IndexOf('"', valStart);
            if (valStart > 0 && valEnd > valStart)
                paths[bt] = layerJson.Substring(valStart, valEnd - valStart);
        }
    }

    private LpcCredit[] ParseCredits(string json)
    {
        var credits = new List<LpcCredit>();

        int creditsIdx = json.IndexOf("\"credits\"");
        if (creditsIdx < 0) return credits.ToArray();

        int arrayStart = json.IndexOf('[', creditsIdx);
        if (arrayStart < 0) return credits.ToArray();
        int arrayEnd = FindMatchingBracket(json, arrayStart);
        if (arrayEnd < 0) return credits.ToArray();

        string creditsJson = json.Substring(arrayStart, arrayEnd - arrayStart + 1);

        // Find each credit object
        int searchPos = 0;
        while (true)
        {
            int objStart = creditsJson.IndexOf('{', searchPos);
            if (objStart < 0) break;
            int objEnd = FindMatchingBrace(creditsJson, objStart);
            if (objEnd < 0) break;

            string creditJson = creditsJson.Substring(objStart, objEnd - objStart + 1);
            searchPos = objEnd + 1;

            var credit = new LpcCredit
            {
                file = ExtractJsonString(creditJson, "file"),
                notes = ExtractJsonString(creditJson, "notes"),
                licenses = ParseStringArrayInline(creditJson, "licenses"),
                authors = ParseStringArrayInline(creditJson, "authors"),
                urls = ParseStringArrayInline(creditJson, "urls")
            };

            credits.Add(credit);
        }

        return credits.ToArray();
    }

    private string[] ParseStringArray(string json, string key)
    {
        var result = new List<string>();
        string searchKey = $"\"{key}\"";
        int keyIdx = json.IndexOf(searchKey);
        if (keyIdx < 0) return result.ToArray();

        int arrayStart = json.IndexOf('[', keyIdx);
        if (arrayStart < 0) return result.ToArray();
        int arrayEnd = FindMatchingBracket(json, arrayStart);
        if (arrayEnd < 0) return result.ToArray();

        string arrayJson = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
        return ExtractStringsFromArray(arrayJson);
    }

    private string[] ParseStringArrayInline(string json, string key)
    {
        return ParseStringArray(json, key);
    }

    private string ExtractJsonString(string json, string key)
    {
        string searchKey = $"\"{key}\"";
        int keyIdx = json.IndexOf(searchKey);
        if (keyIdx < 0) return "";

        int colonIdx = json.IndexOf(':', keyIdx + searchKey.Length);
        if (colonIdx < 0) return "";

        // Skip whitespace after colon
        int valStart = colonIdx + 1;
        while (valStart < json.Length && json[valStart] == ' ') valStart++;

        if (valStart < json.Length && json[valStart] == '"')
        {
            int strStart = valStart + 1;
            int strEnd = json.IndexOf('"', strStart);
            return strEnd > strStart ? json.Substring(strStart, strEnd - strStart) : "";
        }

        return "";
    }

    private string[] ExtractStringsFromArray(string arrayContent)
    {
        var result = new List<string>();
        int pos = 0;
        while (pos < arrayContent.Length)
        {
            int strStart = arrayContent.IndexOf('"', pos);
            if (strStart < 0) break;
            int strEnd = arrayContent.IndexOf('"', strStart + 1);
            if (strEnd < 0) break;
            result.Add(arrayContent.Substring(strStart + 1, strEnd - strStart - 1));
            pos = strEnd + 1;
        }
        return result.ToArray();
    }

    private int FindMatchingBrace(string json, int openPos)
    {
        int depth = 0;
        bool inString = false;
        for (int i = openPos; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '"' && (i == 0 || json[i - 1] != '\\')) inString = !inString;
            if (inString) continue;
            if (c == '{') depth++;
            if (c == '}') { depth--; if (depth == 0) return i; }
        }
        return -1;
    }

    private int FindMatchingBracket(string json, int openPos)
    {
        int depth = 0;
        bool inString = false;
        for (int i = openPos; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '"' && (i == 0 || json[i - 1] != '\\')) inString = !inString;
            if (inString) continue;
            if (c == '[') depth++;
            if (c == ']') { depth--; if (depth == 0) return i; }
        }
        return -1;
    }

    // ===== License Filtering =====

    private bool IsLicenseSafeForBodyType(LpcSheetDef def, string bodyType)
    {
        if (def.credits == null || def.credits.Length == 0) return false;

        // Check if this body type has a path in layer_1
        if (def.bodyTypePaths == null || !def.bodyTypePaths.ContainsKey(bodyType))
            return false;

        string layerPath = def.bodyTypePaths[bodyType];

        // Find credit entries relevant to this body type's path
        var relevantCredits = new List<LpcCredit>();
        foreach (var credit in def.credits)
        {
            if (string.IsNullOrEmpty(credit.file)) continue;

            // Match if the layer path starts with the credit's file path or vice versa
            string normalizedFile = credit.file.TrimEnd('/');
            string normalizedPath = layerPath.TrimEnd('/');

            if (normalizedPath.StartsWith(normalizedFile) ||
                normalizedFile.StartsWith(normalizedPath) ||
                normalizedPath.Contains(normalizedFile))
            {
                relevantCredits.Add(credit);
            }
        }

        // If no specific match found, use all credits (conservative)
        if (relevantCredits.Count == 0)
            relevantCredits.AddRange(def.credits);

        // Every relevant credit must have at least one safe license
        foreach (var credit in relevantCredits)
        {
            if (!HasAnySafeLicense(credit))
                return false;
        }

        return true;
    }

    private bool HasAnySafeLicense(LpcCredit credit)
    {
        if (credit.licenses == null || credit.licenses.Length == 0) return false;

        foreach (var license in credit.licenses)
        {
            // Check for safe licenses: CC0, OGA-BY (any version), CC-BY (non-SA)
            string upper = license.ToUpper();

            if (upper.Contains("CC0")) return true;
            if (upper.Contains("OGA-BY")) return true;
            if (upper.Contains("CC-BY") && !upper.Contains("-SA")) return true;
        }

        return false;
    }

    // ===== Import Queue Building =====

    private void BuildImportItems(LpcSheetDef def)
    {
        foreach (var bodyType in WANTED_BODY_TYPES)
        {
            if (!IsLicenseSafeForBodyType(def, bodyType)) continue;
            if (def.bodyTypePaths == null || !def.bodyTypePaths.ContainsKey(bodyType)) continue;

            string layerPath = def.bodyTypePaths[bodyType];
            BodyPartSlot slot = ResolveSlot(def, false);
            TintCategory tintCat = ResolveTintCategory(def, slot);
            bool isTintable = tintCat != TintCategory.None;

            string partId = BuildPartId(def, bodyType, false);

            importQueue.Add(new ImportItem
            {
                def = def,
                bodyType = bodyType,
                slot = slot,
                layerPath = layerPath,
                partId = partId,
                displayName = def.name,
                isBehindLayer = false,
                isTintable = isTintable,
                tintCategory = tintCat
            });

            // Collect credits
            CollectCredits(def, bodyType);

            // Check for behind layer (layer_2 without custom_animation)
            if (def.behindBodyTypePaths != null && def.behindBodyTypePaths.ContainsKey(bodyType) && def.behindZPos >= 0)
            {
                string behindPath = def.behindBodyTypePaths[bodyType];
                BodyPartSlot behindSlot = ResolveSlot(def, true);

                importQueue.Add(new ImportItem
                {
                    def = def,
                    bodyType = bodyType,
                    slot = behindSlot,
                    layerPath = behindPath,
                    partId = BuildPartId(def, bodyType, true),
                    displayName = def.name + " (Behind)",
                    isBehindLayer = true,
                    isTintable = isTintable,
                    tintCategory = tintCat
                });
            }
        }
    }

    private BodyPartSlot ResolveSlot(LpcSheetDef def, bool isBehindLayer)
    {
        if (def.path == null || def.path.Length == 0)
            return BodyPartSlot.Accessories;

        string category = def.path[0].ToLower();

        // Behind layer → WeaponBehind
        if (isBehindLayer) return BodyPartSlot.WeaponBehind;

        // Special cases by type_name
        string typeName = (def.type_name ?? "").ToLower();
        if (typeName == "shadow") return BodyPartSlot.Shadow;
        if (typeName == "beard" || typeName == "mustache") return BodyPartSlot.Beard;

        // Map by top-level category
        return category switch
        {
            "body" => BodyPartSlot.Body,
            "head" => BodyPartSlot.Head,
            "hair" => def.path.Length > 1 && def.path[1] == "beards" ? BodyPartSlot.Beard : BodyPartSlot.Hair,
            "torso" => ResolveTorsoSlot(def),
            "legs" => BodyPartSlot.Legs,
            "feet" => BodyPartSlot.Feet,
            "weapons" => def.path.Length > 1 && def.path[1] == "shields" ? BodyPartSlot.Shield : BodyPartSlot.WeaponFront,
            "headwear" => BodyPartSlot.Hat,
            "arms" => BodyPartSlot.Gloves,
            "tools" => BodyPartSlot.Accessories,
            _ => BodyPartSlot.Accessories
        };
    }

    private BodyPartSlot ResolveTorsoSlot(LpcSheetDef def)
    {
        if (def.path == null || def.path.Length < 2) return BodyPartSlot.Torso;

        string sub = def.path[1].ToLower();
        if (sub == "cape" || sub == "cloak") return BodyPartSlot.Cape;
        if (sub == "shoulders" || sub == "pauldrons") return BodyPartSlot.Shoulders;

        return BodyPartSlot.Torso;
    }

    private TintCategory ResolveTintCategory(LpcSheetDef def, BodyPartSlot slot)
    {
        if (def.match_body_color) return TintCategory.Skin;

        return slot switch
        {
            BodyPartSlot.Body => TintCategory.Skin,
            BodyPartSlot.Head => TintCategory.Skin,
            BodyPartSlot.Hair => TintCategory.Hair,
            BodyPartSlot.Beard => TintCategory.Hair,
            BodyPartSlot.Eyes => TintCategory.None,
            _ => TintCategory.None
        };
    }

    private string BuildPartId(LpcSheetDef def, string bodyType, bool isBehind)
    {
        string baseName = def.type_name ?? def.name.ToLower().Replace(" ", "_");
        string namePart = def.name.ToLower().Replace(" ", "_");
        string behindSuffix = isBehind ? "_behind" : "";
        return $"{baseName}_{namePart}_{bodyType}{behindSuffix}";
    }

    private void CollectCredits(LpcSheetDef def, string bodyType)
    {
        if (def.credits == null) return;

        foreach (var credit in def.credits)
        {
            string authors = credit.authors != null ? string.Join(", ", credit.authors) : "Unknown";
            string licenses = credit.licenses != null ? string.Join(", ", credit.licenses) : "Unknown";
            string urls = credit.urls != null ? string.Join(" ; ", credit.urls) : "";

            string line = $"[{def.name}] ({bodyType}) Authors: {authors} | License: {licenses} | {urls}";
            if (!creditLines.Contains(line))
                creditLines.Add(line);
        }
    }

    // ===== Step 2: Download PNGs =====

    private void DownloadAllPNGs()
    {
        isRunning = true;
        int downloaded = 0;
        int skipped = 0;
        int failed = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < importQueue.Count; i++)
            {
                var item = importQueue[i];
                progress = (float)i / importQueue.Count;
                statusMessage = $"Downloading {i + 1}/{importQueue.Count}: {item.displayName} ({item.bodyType})";

                if (i % 5 == 0) Repaint();

                var result = DownloadItemPNGs(item);
                downloaded += result.downloaded;
                skipped += result.skipped;
                failed += result.failed;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        // Configure import settings on all PNGs
        ConfigureAllSpriteImports();

        statusMessage = $"Download complete. {downloaded} downloaded, {skipped} skipped (exist), {failed} failed.";
        isRunning = false;
        progress = 0;
        Repaint();
    }

    private (int downloaded, int skipped, int failed) DownloadItemPNGs(ImportItem item)
    {
        int downloaded = 0, skipped = 0, failed = 0;

        string partDir = GetPartDirectory(item);
        EnsureDirectory(partDir);

        // Determine which variant to download
        string variant = ResolveVariant(item);

        // Get the list of animations this item supports
        string[] anims = item.def.animations ?? GetDefaultAnimations();

        // Map to our animator state names
        foreach (var anim in GetFrameMapAnimations())
        {
            string lpcAnim = MapStateToLPCAnimation(anim, item.def.animations);
            if (lpcAnim == null) continue; // This animation not supported by this item

            string localPath = Path.Combine(partDir, $"{anim}.png");
            string fullPath = Path.GetFullPath(localPath);

            if (File.Exists(fullPath))
            {
                skipped++;
                continue;
            }

            // Build download URL
            string layerPath = item.layerPath.TrimEnd('/');
            bool success = false;

            // Try patterns: path/anim/variant.png → path/anim.png → path/fg/anim.png
            string[] urlPatterns;
            if (!string.IsNullOrEmpty(variant))
            {
                urlPatterns = new[]
                {
                    $"{GITHUB_RAW_BASE}spritesheets/{layerPath}/{lpcAnim}/{variant}.png",
                    $"{GITHUB_RAW_BASE}spritesheets/{layerPath}/{lpcAnim}.png",
                    $"{GITHUB_RAW_BASE}spritesheets/{layerPath}/fg/{lpcAnim}.png"
                };
            }
            else
            {
                urlPatterns = new[]
                {
                    $"{GITHUB_RAW_BASE}spritesheets/{layerPath}/{lpcAnim}.png",
                    $"{GITHUB_RAW_BASE}spritesheets/{layerPath}/fg/{lpcAnim}.png"
                };
            }

            foreach (var url in urlPatterns)
            {
                if (success) break;
                try
                {
                    using (var client = new WebClient())
                    {
                        client.Headers.Add("User-Agent", "Unity-LPC-Importer");
                        client.DownloadFile(url, fullPath);
                        downloaded++;
                        success = true;
                    }
                }
                catch (WebException) { }
            }

            if (!success) failed++;
        }

        // Copy walk.png as run.png fallback if run doesn't exist
        CopyWalkAsRunFallback(partDir);

        return (downloaded, skipped, failed);
    }

    private string ResolveVariant(ImportItem item)
    {
        // For body/head (skin color parts), use the configured skin variant
        if (item.slot == BodyPartSlot.Body || item.slot == BodyPartSlot.Head)
            return skinVariant;

        // For tintable parts (hair, beard), pick first variant — will desaturate later
        if (item.isTintable && item.def.variants != null && item.def.variants.Length > 0)
            return item.def.variants[0];

        // For equipment with variants, pick the first one
        if (item.def.variants != null && item.def.variants.Length > 0)
            return item.def.variants[0];

        return null;
    }

    private string GetPartDirectory(ImportItem item)
    {
        // Organize: LPC/{slot}/{partId}
        string slotDir = item.slot.ToString();
        return Path.Combine(SPRITE_ROOT, slotDir, item.partId);
    }

    private string[] GetFrameMapAnimations()
    {
        // Must match the order in the frame map
        return new[] { "idle", "run", "jump", "fall", "attack1", "attack2", "attack3",
                       "roll", "hurt", "death", "wallslide", "ledgegrab" };
    }

    private string[] GetDefaultAnimations()
    {
        return new[] { "idle", "walk", "run", "jump", "slash", "thrust", "spellcast", "shoot", "hurt" };
    }

    private string MapStateToLPCAnimation(string state, string[] availableAnims)
    {
        // Map our animator state to LPC animation name
        string lpcName = state.ToLower() switch
        {
            "idle" => "idle",
            "run" => BestMatch(new[] { "run", "walk" }, availableAnims),
            "jump" => BestMatch(new[] { "jump", "idle" }, availableAnims),
            "fall" => BestMatch(new[] { "jump", "idle" }, availableAnims),
            "attack1" => BestMatch(new[] { "slash", "1h_slash", "attack_slash" }, availableAnims),
            "attack2" => BestMatch(new[] { "thrust", "attack_thrust" }, availableAnims),
            "attack3" => BestMatch(new[] { "spellcast" }, availableAnims),
            "roll" => BestMatch(new[] { "run", "walk" }, availableAnims),
            "hurt" => "hurt",
            "death" => "hurt",
            "wallslide" => "idle",
            "ledgegrab" => "idle",
            _ => state.ToLower()
        };

        return lpcName;
    }

    private string BestMatch(string[] preferred, string[] available)
    {
        if (available == null) return preferred[0];
        foreach (var p in preferred)
        {
            foreach (var a in available)
            {
                if (a.ToLower() == p.ToLower()) return a;
            }
        }
        return preferred[0]; // Fall back to first preference
    }

    private void CopyWalkAsRunFallback(string partDir)
    {
        string fullDir = Path.GetFullPath(partDir);
        string walkSrc = Path.Combine(fullDir, "run.png");

        // If run doesn't exist but walk does in the downloaded files, copy
        if (!File.Exists(walkSrc))
        {
            // Check if there's a walk file to use
            string walkFile = Path.Combine(fullDir, "idle.png"); // last resort
            // walk.png might have been downloaded under a different name
        }
    }

    // ===== Step 3: Slice & Create Assets =====

    private void SliceAndCreateAllAssets()
    {
        isRunning = true;
        statusMessage = "Slicing spritesheets and creating assets...";
        Repaint();

        try
        {
            EnsureDirectory(DATA_ROOT + "/BodyParts");

            // Load or create frame map
            var frameMap = LoadOrCreateFrameMap();

            int created = 0;
            int updated = 0;

            AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = 0; i < importQueue.Count; i++)
                {
                    var item = importQueue[i];
                    progress = (float)i / importQueue.Count;

                    if (i % 10 == 0)
                    {
                        statusMessage = $"Creating assets {i + 1}/{importQueue.Count}: {item.displayName}";
                        Repaint();
                    }

                    bool isNew;
                    var bodyPart = CreateBodyPartAsset(item, frameMap, out isNew);
                    if (bodyPart != null)
                    {
                        if (isNew) created++;
                        else updated++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // Rebuild registry
            RebuildRegistry();

            // Write credits file
            WriteCreditsFile();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            statusMessage = $"Asset creation complete. {created} new, {updated} updated. Registry rebuilt.";
        }
        catch (Exception e)
        {
            statusMessage = $"Error creating assets: {e.Message}";
            Debug.LogError($"[LPC Bulk] Asset creation error: {e}");
        }
        finally
        {
            isRunning = false;
            progress = 0;
            Repaint();
        }
    }

    private BodyPartData CreateBodyPartAsset(ImportItem item, AnimationStateFrameMap frameMap, out bool isNew)
    {
        isNew = false;
        string partDir = GetPartDirectory(item);

        // Ensure the directory has PNGs
        string fullDir = Path.GetFullPath(partDir);
        if (!Directory.Exists(fullDir))
            return null;

        var pngs = Directory.GetFiles(fullDir, "*.png");
        if (pngs.Length == 0) return null;

        // Slice all animation sheets and build frames array
        List<Sprite> allFrames = new List<Sprite>();

        foreach (var entry in frameMap.entries)
        {
            string animName = entry.stateName.ToLower();
            string pngPath = $"{partDir}/{animName}.png";

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            if (texture == null)
            {
                // Try walk.png frame 0 as static fallback for missing animations
                string walkPng = $"{partDir}/idle.png";
                var walkTex = AssetDatabase.LoadAssetAtPath<Texture2D>(walkPng);
                if (walkTex != null)
                {
                    int frameSize = DetectFrameSize(walkTex);
                    int wCols = walkTex.width / frameSize;
                    int wRows = walkTex.height / frameSize;
                    int wRow = wRows >= 4 ? SIDE_VIEW_ROW : 0;
                    var wSprites = SliceSpritesheet(walkPng, walkTex, wCols, wRows, frameSize);
                    int f0Idx = wRow * wCols;
                    Sprite staticFrame = (f0Idx < wSprites.Length) ? wSprites[f0Idx] : null;
                    for (int i = 0; i < entry.frameCount; i++)
                        allFrames.Add(staticFrame);
                    continue;
                }

                for (int i = 0; i < entry.frameCount; i++)
                    allFrames.Add(null);
                continue;
            }

            int fs = DetectFrameSize(texture);
            int framesInSheet = texture.width / fs;
            int rowsInSheet = texture.height / fs;
            bool isDirectional = rowsInSheet >= 4;
            int targetRow = isDirectional ? SIDE_VIEW_ROW : 0;

            var slicedSprites = SliceSpritesheet(pngPath, texture, framesInSheet, rowsInSheet, fs);

            int framesToUse = Mathf.Min(framesInSheet, entry.frameCount);
            for (int f = 0; f < entry.frameCount; f++)
            {
                if (f < framesToUse)
                {
                    int spriteIndex = targetRow * framesInSheet + f;
                    allFrames.Add(spriteIndex < slicedSprites.Length ? slicedSprites[spriteIndex] : null);
                }
                else
                {
                    int lastIdx = targetRow * framesInSheet + (framesToUse - 1);
                    allFrames.Add(lastIdx >= 0 && lastIdx < slicedSprites.Length ? slicedSprites[lastIdx] : null);
                }
            }
        }

        // Remove trailing nulls
        while (allFrames.Count > 0 && allFrames[allFrames.Count - 1] == null)
            allFrames.RemoveAt(allFrames.Count - 1);

        if (allFrames.Count == 0) return null;

        // Create or update BodyPartData asset
        string slotDir = item.slot.ToString();
        string assetDir = $"{DATA_ROOT}/BodyParts/{slotDir}";
        EnsureDirectory(assetDir);
        string assetPath = $"{assetDir}/{item.partId}.asset";

        var bodyPart = AssetDatabase.LoadAssetAtPath<BodyPartData>(assetPath);
        if (bodyPart == null)
        {
            bodyPart = CreateInstance<BodyPartData>();
            AssetDatabase.CreateAsset(bodyPart, assetPath);
            isNew = true;
        }

        bodyPart.partId = item.partId;
        bodyPart.displayName = item.displayName;
        bodyPart.slot = item.slot;
        bodyPart.bodyTypeTag = item.bodyType;
        bodyPart.frames = allFrames.ToArray();
        bodyPart.sortOrderOffset = 0;
        bodyPart.supportsTinting = item.isTintable;
        bodyPart.tintCategory = item.tintCategory;
        bodyPart.defaultTint = Color.white;
        bodyPart.previewSprite = allFrames.Count > 0 ? allFrames[0] : null;

        // Better preview for weapons
        if ((item.slot == BodyPartSlot.WeaponFront || item.slot == BodyPartSlot.WeaponBehind) && allFrames.Count > 26)
        {
            Sprite walkStatic = allFrames[0];
            int[][] combatRanges = { new[] { 26, 33 }, new[] { 20, 25 }, new[] { 34, 40 } };
            foreach (var range in combatRanges)
            {
                bool found = false;
                for (int i = range[0]; i <= range[1] && i < allFrames.Count; i++)
                {
                    if (allFrames[i] != null && allFrames[i] != walkStatic)
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

    private int DetectFrameSize(Texture2D texture)
    {
        // Oversized weapons use 192px frames
        if (texture.width % OVERSIZE_FRAME == 0 && texture.height % OVERSIZE_FRAME == 0)
        {
            int oversizeCols = texture.width / OVERSIZE_FRAME;
            int oversizeRows = texture.height / OVERSIZE_FRAME;
            // Only use oversize if it produces a reasonable grid
            if (oversizeCols >= 1 && oversizeRows >= 1 && oversizeCols <= 13)
                return OVERSIZE_FRAME;
        }
        return FRAME_SIZE;
    }

    private Sprite[] SliceSpritesheet(string assetPath, Texture2D texture, int cols, int rows, int frameSize)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return new Sprite[0];

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

    // ===== Registry & Credits =====

    private void RebuildRegistry()
    {
        string resourcesPath = "Assets/_Project/Resources";
        string path = resourcesPath + "/BodyPartRegistry.asset";
        EnsureDirectory(resourcesPath);

        var registry = AssetDatabase.LoadAssetAtPath<BodyPartRegistry>(path);
        if (registry == null)
        {
            registry = CreateInstance<BodyPartRegistry>();
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

        registry.allParts = allParts.ToArray();
        EditorUtility.SetDirty(registry);
        Debug.Log($"[LPC Bulk] Registry rebuilt with {allParts.Count} parts.");
    }

    private void WriteCreditsFile()
    {
        if (creditLines.Count == 0) return;

        string fullPath = Path.GetFullPath(CREDITS_PATH);
        EnsureDirectory(Path.GetDirectoryName(CREDITS_PATH));

        var lines = new List<string>
        {
            "=== LPC Import Credits ===",
            $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"Total items: {creditLines.Count}",
            "",
            "All assets imported under their respective safe licenses (CC0, OGA-BY, CC-BY).",
            "Assets requiring GPL or CC-BY-SA were excluded from import.",
            ""
        };
        lines.AddRange(creditLines.OrderBy(l => l));

        File.WriteAllLines(fullPath, lines);
        Debug.Log($"[LPC Bulk] Credits written to {CREDITS_PATH} ({creditLines.Count} entries).");
    }

    // ===== Frame Map =====

    private AnimationStateFrameMap LoadOrCreateFrameMap()
    {
        var frameMap = AssetDatabase.LoadAssetAtPath<AnimationStateFrameMap>(FRAME_MAP_PATH);
        if (frameMap != null) return frameMap;

        frameMap = CreateInstance<AnimationStateFrameMap>();
        EnsureDirectory(Path.GetDirectoryName(FRAME_MAP_PATH));
        AssetDatabase.CreateAsset(frameMap, FRAME_MAP_PATH);

        int offset = 0;
        var entries = new List<AnimationStateFrameMap.StateFrameEntry>();

        AddFrameEntry(entries, "Idle", ref offset, 4, 8f, true);
        AddFrameEntry(entries, "Run", ref offset, 8, 10f, true);
        AddFrameEntry(entries, "Jump", ref offset, 6, 10f, false);
        AddFrameEntry(entries, "Fall", ref offset, 2, 8f, true);
        AddFrameEntry(entries, "Attack1", ref offset, 6, 12f, false);
        AddFrameEntry(entries, "Attack2", ref offset, 8, 12f, false);
        AddFrameEntry(entries, "Attack3", ref offset, 7, 10f, false);
        AddFrameEntry(entries, "Roll", ref offset, 8, 10f, false);
        AddFrameEntry(entries, "Hurt", ref offset, 6, 8f, false);
        AddFrameEntry(entries, "Death", ref offset, 6, 6f, false);
        AddFrameEntry(entries, "WallSlide", ref offset, 2, 6f, true);
        AddFrameEntry(entries, "LedgeGrab", ref offset, 2, 6f, false);

        frameMap.entries = entries.ToArray();
        frameMap.fallback = new AnimationStateFrameMap.StateFrameEntry
        {
            stateName = "Idle",
            startFrameIndex = 0,
            frameCount = 4,
            frameRate = 8f,
            loop = true
        };

        EditorUtility.SetDirty(frameMap);
        return frameMap;
    }

    private void AddFrameEntry(List<AnimationStateFrameMap.StateFrameEntry> entries,
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

    // ===== Run All =====

    private void RunAll()
    {
        DiscoverAndFilter();

        if (importQueue.Count == 0)
        {
            statusMessage = "No items to import after license filtering.";
            return;
        }

        DownloadAllPNGs();

        // Desaturate tintable sprites
        DesaturateTintableSprites();

        SliceAndCreateAllAssets();
    }

    private void DesaturateTintableSprites()
    {
        statusMessage = "Desaturating tintable sprites (hair, beard, eyes)...";
        Repaint();

        int processed = 0;
        foreach (var item in importQueue)
        {
            if (!item.isTintable) continue;
            if (item.tintCategory != TintCategory.Hair) continue; // Only desaturate hair-tinted parts

            string fullDir = Path.GetFullPath(GetPartDirectory(item));
            if (!Directory.Exists(fullDir)) continue;

            string[] pngs = Directory.GetFiles(fullDir, "*.png");
            foreach (string png in pngs)
            {
                byte[] bytes = File.ReadAllBytes(png);
                var tex = new Texture2D(2, 2);
                tex.LoadImage(bytes);

                var pixels = tex.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
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
        Debug.Log($"[LPC Bulk] Desaturated {processed} tintable sprites.");
    }

    // ===== Import Settings =====

    private void ConfigureAllSpriteImports()
    {
        statusMessage = "Configuring sprite import settings...";
        Repaint();

        string fullRoot = Path.GetFullPath(SPRITE_ROOT);
        if (!Directory.Exists(fullRoot)) return;

        string[] pngs = Directory.GetFiles(fullRoot, "*.png", SearchOption.AllDirectories);
        AssetDatabase.StartAssetEditing();

        try
        {
            foreach (string png in pngs)
            {
                string assetPath = "Assets" + png.Replace(Path.GetFullPath("Assets"), "").Replace("\\", "/");
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) continue;

                // Skip if already configured
                if (importer.spriteImportMode == SpriteImportMode.Multiple &&
                    importer.spritePixelsPerUnit == PIXELS_PER_UNIT)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = PIXELS_PER_UNIT;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }
    }

    // ===== Utility =====

    private void EnsureDirectory(string path)
    {
        string fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath))
            Directory.CreateDirectory(fullPath);
    }
}
