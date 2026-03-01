using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor validation tool for LPC body part assets.
/// Checks: frame count consistency, duplicate partIds, registry completeness,
/// tintCategory correctness, and body type coverage.
///
/// Usage: Unity Editor > Tools > LPC Asset Validator
/// </summary>
public class LpcAssetValidator : EditorWindow
{
    private Vector2 scrollPos;
    private List<string> errors = new List<string>();
    private List<string> warnings = new List<string>();
    private List<string> info = new List<string>();
    private bool hasRun;

    [MenuItem("Tools/LPC Asset Validator")]
    public static void ShowWindow()
    {
        GetWindow<LpcAssetValidator>("LPC Asset Validator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("LPC Asset Validator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("Run Validation", GUILayout.Height(30)))
            RunValidation();

        if (!hasRun) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Errors: {errors.Count}  |  Warnings: {warnings.Count}  |  Info: {info.Count}");

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (errors.Count > 0)
        {
            EditorGUILayout.LabelField("ERRORS", EditorStyles.boldLabel);
            GUI.color = Color.red;
            foreach (var e in errors)
                EditorGUILayout.HelpBox(e, MessageType.Error);
            GUI.color = Color.white;
        }

        if (warnings.Count > 0)
        {
            EditorGUILayout.LabelField("WARNINGS", EditorStyles.boldLabel);
            foreach (var w in warnings)
                EditorGUILayout.HelpBox(w, MessageType.Warning);
        }

        if (info.Count > 0)
        {
            EditorGUILayout.LabelField("INFO", EditorStyles.boldLabel);
            foreach (var i in info)
                EditorGUILayout.HelpBox(i, MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
    }

    private void RunValidation()
    {
        errors.Clear();
        warnings.Clear();
        info.Clear();
        hasRun = true;

        // Load all BodyPartData assets
        var guids = AssetDatabase.FindAssets("t:BodyPartData");
        var allParts = new List<BodyPartData>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var part = AssetDatabase.LoadAssetAtPath<BodyPartData>(path);
            if (part != null) allParts.Add(part);
        }

        info.Add($"Found {allParts.Count} BodyPartData assets.");

        // Load frame map
        var frameMap = AssetDatabase.LoadAssetAtPath<AnimationStateFrameMap>(
            "Assets/_Project/ScriptableObjects/Character/LPCSideViewFrameMap.asset");
        int expectedFrameCount = 0;
        if (frameMap != null && frameMap.entries != null)
        {
            foreach (var entry in frameMap.entries)
                expectedFrameCount += entry.frameCount;
            info.Add($"Frame map expects {expectedFrameCount} total frames ({frameMap.entries.Length} animations).");
        }

        // Check 1: Duplicate partIds
        CheckDuplicatePartIds(allParts);

        // Check 2: Frame count consistency
        CheckFrameCounts(allParts, expectedFrameCount);

        // Check 3: Missing fields
        CheckMissingFields(allParts);

        // Check 4: Registry completeness
        CheckRegistryCompleteness(allParts);

        // Check 5: TintCategory consistency
        CheckTintCategories(allParts);

        // Check 6: Slot distribution
        ReportSlotDistribution(allParts);

        // Check 7: Body type coverage
        ReportBodyTypeCoverage(allParts);

        Repaint();
    }

    private void CheckDuplicatePartIds(List<BodyPartData> parts)
    {
        var idCounts = new Dictionary<string, List<string>>();
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part.partId)) continue;
            var path = AssetDatabase.GetAssetPath(part);
            if (!idCounts.ContainsKey(part.partId))
                idCounts[part.partId] = new List<string>();
            idCounts[part.partId].Add(path);
        }

        foreach (var kvp in idCounts)
        {
            if (kvp.Value.Count > 1)
                errors.Add($"Duplicate partId '{kvp.Key}' in: {string.Join(", ", kvp.Value)}");
        }

        int emptyIds = parts.Count(p => string.IsNullOrEmpty(p.partId));
        if (emptyIds > 0)
            warnings.Add($"{emptyIds} parts have empty partId.");
    }

    private void CheckFrameCounts(List<BodyPartData> parts, int expected)
    {
        if (expected == 0) return;

        int mismatch = 0;
        foreach (var part in parts)
        {
            if (part.frames == null || part.frames.Length == 0) continue;

            // Frame count should match or be less (trailing nulls stripped)
            if (part.frames.Length > expected)
            {
                warnings.Add($"[{part.partId}] Has {part.frames.Length} frames (expected max {expected}).");
                mismatch++;
            }
        }

        int noFrames = parts.Count(p => p.frames == null || p.frames.Length == 0);
        if (noFrames > 0)
            warnings.Add($"{noFrames} parts have no frames.");

        if (mismatch == 0)
            info.Add("All frame counts within expected range.");
    }

    private void CheckMissingFields(List<BodyPartData> parts)
    {
        int missingDisplay = parts.Count(p => string.IsNullOrEmpty(p.displayName));
        int missingPreview = parts.Count(p => p.previewSprite == null);

        if (missingDisplay > 0)
            warnings.Add($"{missingDisplay} parts have no displayName.");
        if (missingPreview > 0)
            warnings.Add($"{missingPreview} parts have no previewSprite.");
    }

    private void CheckRegistryCompleteness(List<BodyPartData> allParts)
    {
        var registry = Resources.Load<BodyPartRegistry>("BodyPartRegistry");
        if (registry == null)
        {
            errors.Add("BodyPartRegistry not found in Resources/. Save/load will fail.");
            return;
        }

        if (registry.allParts == null)
        {
            errors.Add("BodyPartRegistry.allParts is null.");
            return;
        }

        int registeredCount = registry.allParts.Length;
        int totalCount = allParts.Count;

        if (registeredCount < totalCount)
            warnings.Add($"Registry has {registeredCount} parts but {totalCount} exist. Run the importer to rebuild.");
        else
            info.Add($"Registry has {registeredCount} parts (matches {totalCount} assets).");

        // Check for null entries in registry
        int nullEntries = registry.allParts.Count(p => p == null);
        if (nullEntries > 0)
            errors.Add($"Registry has {nullEntries} null entries. Rebuild needed.");
    }

    private void CheckTintCategories(List<BodyPartData> parts)
    {
        foreach (var part in parts)
        {
            if (part.supportsTinting && part.tintCategory == TintCategory.None)
                warnings.Add($"[{part.partId}] supportsTinting=true but tintCategory=None.");

            if (!part.supportsTinting && part.tintCategory != TintCategory.None)
                warnings.Add($"[{part.partId}] supportsTinting=false but tintCategory={part.tintCategory}.");
        }
    }

    private void ReportSlotDistribution(List<BodyPartData> parts)
    {
        var slotCounts = new Dictionary<BodyPartSlot, int>();
        foreach (var part in parts)
        {
            if (!slotCounts.ContainsKey(part.slot))
                slotCounts[part.slot] = 0;
            slotCounts[part.slot]++;
        }

        var report = slotCounts
            .OrderBy(kvp => (int)kvp.Key)
            .Select(kvp => $"{kvp.Key}: {kvp.Value}");
        info.Add("Slot distribution: " + string.Join(", ", report));
    }

    private void ReportBodyTypeCoverage(List<BodyPartData> parts)
    {
        var bodyTypeCounts = new Dictionary<string, int>();
        foreach (var part in parts)
        {
            string bt = string.IsNullOrEmpty(part.bodyTypeTag) ? "unset" : part.bodyTypeTag;
            if (!bodyTypeCounts.ContainsKey(bt))
                bodyTypeCounts[bt] = 0;
            bodyTypeCounts[bt]++;
        }

        var report = bodyTypeCounts
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp => $"{kvp.Key}: {kvp.Value}");
        info.Add("Body type coverage: " + string.Join(", ", report));
    }
}
