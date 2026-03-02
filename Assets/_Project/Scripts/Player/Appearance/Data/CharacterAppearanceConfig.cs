using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// ScriptableObject defining a complete character appearance — one BodyPartData per slot
/// plus tint colors. Used as the default appearance for a job class and as the
/// base configuration during character creation.
///
/// V2: Dictionary-based storage replaces per-field slots. Old fields kept with
/// [FormerlySerializedAs] for one migration cycle on existing assets.
/// </summary>
[CreateAssetMenu(fileName = "NewAppearance", menuName = "Game/Character/Appearance Config")]
public class CharacterAppearanceConfig : ScriptableObject
{
    [Header("Identity")]
    public string configId;
    public string displayName;
    public string bodyType = "male";

    [Header("Slot Parts (data-driven)")]
    [Tooltip("All equipped body parts keyed by slot. Replaces per-field storage.")]
    [SerializeField] private BodyPartEntry[] slotEntries = Array.Empty<BodyPartEntry>();

    [Header("Tints")]
    public Color skinTint = Color.white;
    public Color hairTint = Color.white;
    public Color armorPrimaryTint = Color.white;
    public Color armorSecondaryTint = Color.white;
    public Color eyeTint = new Color(0.45f, 0.30f, 0.15f, 1f);

    // ----- Legacy fields for migration from v1 assets -----
    [HideInInspector, FormerlySerializedAs("body")]     public BodyPartData _legacyBody;
    [HideInInspector, FormerlySerializedAs("head")]     public BodyPartData _legacyHead;
    [HideInInspector, FormerlySerializedAs("hair")]     public BodyPartData _legacyHair;
    [HideInInspector, FormerlySerializedAs("torso")]    public BodyPartData _legacyTorso;
    [HideInInspector, FormerlySerializedAs("legs")]     public BodyPartData _legacyLegs;
    [HideInInspector, FormerlySerializedAs("weaponBehind")] public BodyPartData _legacyWeaponBehind;
    [HideInInspector, FormerlySerializedAs("weaponFront")]  public BodyPartData _legacyWeaponFront;

    [Serializable]
    public struct BodyPartEntry
    {
        public BodyPartSlot slot;
        public BodyPartData part;
    }

    // Runtime dictionary built on demand from serialized array
    [NonSerialized] private Dictionary<BodyPartSlot, BodyPartData> slotDict;
    [NonSerialized] private bool dictBuilt;

    private void OnEnable()
    {
        dictBuilt = false;
        MigrateLegacyFields();
    }

    /// <summary>
    /// One-time migration: move legacy per-field data into slotEntries array.
    /// </summary>
    private void MigrateLegacyFields()
    {
        // Only migrate if slotEntries is empty and any legacy field is set
        if (slotEntries != null && slotEntries.Length > 0) return;

        bool hasLegacy = _legacyBody != null || _legacyHead != null || _legacyHair != null ||
                         _legacyTorso != null || _legacyLegs != null ||
                         _legacyWeaponBehind != null || _legacyWeaponFront != null;

        if (!hasLegacy) return;

        var entries = new List<BodyPartEntry>();
        if (_legacyBody != null) entries.Add(new BodyPartEntry { slot = BodyPartSlot.Body, part = _legacyBody });
        if (_legacyHead != null) entries.Add(new BodyPartEntry { slot = BodyPartSlot.Head, part = _legacyHead });
        if (_legacyHair != null) entries.Add(new BodyPartEntry { slot = BodyPartSlot.Hair, part = _legacyHair });
        if (_legacyTorso != null) entries.Add(new BodyPartEntry { slot = BodyPartSlot.Torso, part = _legacyTorso });
        if (_legacyLegs != null) entries.Add(new BodyPartEntry { slot = BodyPartSlot.Legs, part = _legacyLegs });
        if (_legacyWeaponBehind != null) entries.Add(new BodyPartEntry { slot = BodyPartSlot.WeaponBehind, part = _legacyWeaponBehind });
        if (_legacyWeaponFront != null) entries.Add(new BodyPartEntry { slot = BodyPartSlot.WeaponFront, part = _legacyWeaponFront });

        slotEntries = entries.ToArray();
        dictBuilt = false;
    }

    private void EnsureDict()
    {
        if (dictBuilt) return;
        slotDict = new Dictionary<BodyPartSlot, BodyPartData>();
        if (slotEntries != null)
        {
            foreach (var entry in slotEntries)
            {
                if (entry.part != null)
                    slotDict[entry.slot] = entry.part;
            }
        }
        dictBuilt = true;
    }

    /// <summary>
    /// Returns the BodyPartData for a given slot, or null.
    /// </summary>
    public BodyPartData GetPart(BodyPartSlot slot)
    {
        EnsureDict();
        return slotDict.TryGetValue(slot, out var part) ? part : null;
    }

    /// <summary>
    /// Sets the part for a given slot. Pass null to clear.
    /// </summary>
    public void SetPart(BodyPartSlot slot, BodyPartData part)
    {
        EnsureDict();
        if (part != null)
            slotDict[slot] = part;
        else
            slotDict.Remove(slot);
        SyncToArray();
    }

    /// <summary>
    /// Returns the tint color for a given tint category.
    /// </summary>
    public Color GetTintForCategory(TintCategory category)
    {
        return category switch
        {
            TintCategory.Skin => skinTint,
            TintCategory.Hair => hairTint,
            TintCategory.ArmorPrimary => armorPrimaryTint,
            TintCategory.ArmorSecondary => armorSecondaryTint,
            TintCategory.Eyes => eyeTint,
            _ => Color.white
        };
    }

    /// <summary>
    /// Returns all slots that have a part assigned.
    /// </summary>
    public IEnumerable<KeyValuePair<BodyPartSlot, BodyPartData>> GetAllParts()
    {
        EnsureDict();
        return slotDict;
    }

    /// <summary>
    /// Creates a deep clone of this config as a runtime instance (not saved to disk).
    /// </summary>
    public CharacterAppearanceConfig Clone()
    {
        var clone = CreateInstance<CharacterAppearanceConfig>();
        clone.configId = configId;
        clone.displayName = displayName;
        clone.bodyType = bodyType;
        clone.skinTint = skinTint;
        clone.hairTint = hairTint;
        clone.armorPrimaryTint = armorPrimaryTint;
        clone.armorSecondaryTint = armorSecondaryTint;
        clone.eyeTint = eyeTint;

        if (slotEntries != null)
        {
            clone.slotEntries = new BodyPartEntry[slotEntries.Length];
            Array.Copy(slotEntries, clone.slotEntries, slotEntries.Length);
        }
        clone.dictBuilt = false;
        return clone;
    }

    /// <summary>
    /// Writes the runtime dictionary back to the serialized array.
    /// </summary>
    private void SyncToArray()
    {
        if (slotDict == null) return;
        var entries = new List<BodyPartEntry>();
        foreach (var kvp in slotDict)
        {
            entries.Add(new BodyPartEntry { slot = kvp.Key, part = kvp.Value });
        }
        slotEntries = entries.ToArray();
    }

    // ----- Backward compatibility accessors -----
    // These properties let existing code (LPCSetupWizard, tests) that assigns
    // config.body = ... continue to compile during the migration period.

    public BodyPartData body { get => GetPart(BodyPartSlot.Body); set => SetPart(BodyPartSlot.Body, value); }
    public BodyPartData head { get => GetPart(BodyPartSlot.Head); set => SetPart(BodyPartSlot.Head, value); }
    public BodyPartData hair { get => GetPart(BodyPartSlot.Hair); set => SetPart(BodyPartSlot.Hair, value); }
    public BodyPartData torso { get => GetPart(BodyPartSlot.Torso); set => SetPart(BodyPartSlot.Torso, value); }
    public BodyPartData legs { get => GetPart(BodyPartSlot.Legs); set => SetPart(BodyPartSlot.Legs, value); }
    public BodyPartData weaponBehind { get => GetPart(BodyPartSlot.WeaponBehind); set => SetPart(BodyPartSlot.WeaponBehind, value); }
    public BodyPartData weaponFront { get => GetPart(BodyPartSlot.WeaponFront); set => SetPart(BodyPartSlot.WeaponFront, value); }
}
