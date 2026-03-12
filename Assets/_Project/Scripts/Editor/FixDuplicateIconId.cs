using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot editor utility to fix skill icon issues.
/// Run via menu: Tools > Fix Skill Icons, then delete this script.
/// </summary>
public static class FixDuplicateIconId
{
    [MenuItem("Tools/Fix Skill Icons")]
    public static void Fix()
    {
        int fixCount = 0;

        // Fix 1: ArcaneWard duplicate — lorc/magic-shield → lorc/shield-echoes
        fixCount += FixSkillIcon("ArcaneWard", "lorc/magic-shield", "lorc/shield-echoes");

        // Fix 2: ChargeShot invalid — lorc/magic-bolt doesn't exist → lorc/plasma-bolt
        fixCount += FixSkillIcon("ChargeShot", "lorc/magic-bolt", "lorc/plasma-bolt");

        if (fixCount > 0)
            AssetDatabase.SaveAssets();

        Debug.Log($"[FixSkillIcons] Done. {fixCount} icon(s) fixed.");
    }

    private static int FixSkillIcon(string assetName, string oldIconId, string newIconId)
    {
        string[] guids = AssetDatabase.FindAssets($"{assetName} t:SkillData");
        if (guids.Length == 0)
        {
            Debug.LogWarning($"[FixSkillIcons] {assetName} not found.");
            return 0;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
        if (skill == null)
        {
            Debug.LogWarning($"[FixSkillIcons] Failed to load {assetName}.");
            return 0;
        }

        if (skill.iconId == oldIconId)
        {
            skill.iconId = newIconId;
            EditorUtility.SetDirty(skill);
            Debug.Log($"[FixSkillIcons] {assetName} iconId changed from '{oldIconId}' to '{newIconId}'.");
            return 1;
        }

        Debug.Log($"[FixSkillIcons] {assetName} iconId is '{skill.iconId}', no change needed.");
        return 0;
    }
}
