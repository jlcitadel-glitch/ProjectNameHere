using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject registry of all available BodyPartData assets.
/// Used for resolving part IDs during save/load and for populating
/// the character creation UI with available options per slot.
/// </summary>
[CreateAssetMenu(fileName = "BodyPartRegistry", menuName = "Game/Character/Body Part Registry")]
public class BodyPartRegistry : ScriptableObject
{
    [Tooltip("All available body part assets in the game")]
    public BodyPartData[] allParts;

    private Dictionary<string, BodyPartData> idLookup;

    /// <summary>
    /// Resolves a part ID to its BodyPartData asset. Returns null if not found.
    /// </summary>
    public BodyPartData GetPartById(string partId)
    {
        if (string.IsNullOrEmpty(partId)) return null;

        EnsureLookup();
        return idLookup.TryGetValue(partId, out var part) ? part : null;
    }

    /// <summary>
    /// Returns all parts that belong to the given slot.
    /// </summary>
    public BodyPartData[] GetPartsForSlot(BodyPartSlot slot)
    {
        var result = new List<BodyPartData>();
        if (allParts == null) return result.ToArray();

        foreach (var part in allParts)
        {
            if (part != null && part.slot == slot)
                result.Add(part);
        }
        return result.ToArray();
    }

    /// <summary>
    /// Returns parts for a slot filtered by body type.
    /// Includes parts tagged "universal" plus those matching the given body type.
    /// </summary>
    public BodyPartData[] GetPartsForSlot(BodyPartSlot slot, string bodyType)
    {
        var result = new List<BodyPartData>();
        if (allParts == null) return result.ToArray();

        foreach (var part in allParts)
        {
            if (part != null && part.slot == slot)
            {
                if (string.IsNullOrEmpty(part.bodyTypeTag) ||
                    part.bodyTypeTag == "universal" ||
                    part.bodyTypeTag == bodyType)
                {
                    result.Add(part);
                }
            }
        }
        return result.ToArray();
    }

    private void EnsureLookup()
    {
        if (idLookup != null) return;

        idLookup = new Dictionary<string, BodyPartData>();
        if (allParts == null) return;

        foreach (var part in allParts)
        {
            if (part != null && !string.IsNullOrEmpty(part.partId))
            {
                if (!idLookup.ContainsKey(part.partId))
                    idLookup[part.partId] = part;
                else
                    Debug.LogWarning($"[BodyPartRegistry] Duplicate partId: {part.partId}");
            }
        }
    }

    private void OnEnable()
    {
        // Force rebuild on load/reimport
        idLookup = null;
    }
}
