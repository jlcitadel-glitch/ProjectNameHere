using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject registry of all available skill icon assets.
/// Follows the BodyPartRegistry pattern: public array + lazy Dictionary lookup.
/// Place the asset in a Resources/ folder as "SkillIconDatabase".
/// </summary>
[CreateAssetMenu(fileName = "SkillIconDatabase", menuName = "Skills/Skill Icon Database")]
public class SkillIconDatabase : ScriptableObject
{
    [Tooltip("All available skill icon entries")]
    public SkillIconEntry[] allIcons;

    private Dictionary<string, SkillIconEntry> idLookup;

    private static SkillIconDatabase instance;

    /// <summary>
    /// Singleton accessor via Resources.Load.
    /// </summary>
    public static SkillIconDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<SkillIconDatabase>("SkillIconDatabase");
                if (instance == null)
                    Debug.LogWarning("[SkillIconDatabase] No SkillIconDatabase asset found in Resources/");
            }
            return instance;
        }
    }

    /// <summary>
    /// Resolves an icon ID to its entry. Returns null if not found.
    /// </summary>
    public SkillIconEntry GetIconById(string iconId)
    {
        if (string.IsNullOrEmpty(iconId)) return null;

        EnsureLookup();
        return idLookup.TryGetValue(iconId, out var entry) ? entry : null;
    }

    /// <summary>
    /// Returns all icons whose ID or tags contain the search term (case-insensitive).
    /// </summary>
    public List<SkillIconEntry> Search(string searchTerm)
    {
        var results = new List<SkillIconEntry>();
        if (allIcons == null || string.IsNullOrEmpty(searchTerm)) return results;

        string lower = searchTerm.ToLowerInvariant();
        foreach (var entry in allIcons)
        {
            if (entry == null) continue;

            if (entry.iconId != null && entry.iconId.ToLowerInvariant().Contains(lower))
            {
                results.Add(entry);
                continue;
            }

            if (entry.displayName != null && entry.displayName.ToLowerInvariant().Contains(lower))
            {
                results.Add(entry);
                continue;
            }

            if (entry.tags != null)
            {
                foreach (var tag in entry.tags)
                {
                    if (tag != null && tag.ToLowerInvariant().Contains(lower))
                    {
                        results.Add(entry);
                        break;
                    }
                }
            }
        }

        return results;
    }

    private void EnsureLookup()
    {
        if (idLookup != null) return;

        idLookup = new Dictionary<string, SkillIconEntry>();
        if (allIcons == null) return;

        foreach (var entry in allIcons)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.iconId))
            {
                if (!idLookup.ContainsKey(entry.iconId))
                    idLookup[entry.iconId] = entry;
                else
                    Debug.LogWarning($"[SkillIconDatabase] Duplicate iconId: {entry.iconId}");
            }
        }
    }

    private void OnEnable()
    {
        // Force rebuild on load/reimport
        idLookup = null;
    }
}
