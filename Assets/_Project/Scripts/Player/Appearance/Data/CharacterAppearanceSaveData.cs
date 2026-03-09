using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable data for saving/loading a character's appearance.
/// V2: Uses a dictionary of slot->partId for arbitrary slot support.
/// Legacy named fields are kept for backward compat with v6 saves.
/// </summary>
[Serializable]
public class CharacterAppearanceSaveData
{
    // ----- V2 fields (v7+) -----
    public string bodyType = "male";
    public Dictionary<string, string> slotPartIds;

    // ----- Legacy fields (v6 compat) -----
    public string bodyId;
    public string headId;
    public string hairId;
    public string torsoId;
    public string legsId;
    public string weaponBehindId;
    public string weaponFrontId;

    public string skinTintHex;
    public string hairTintHex;
    public string armorPrimaryTintHex;
    public string armorSecondaryTintHex;
    public string eyeTintHex;

    /// <summary>
    /// Creates save data from a CharacterAppearanceConfig.
    /// Populates both the new dictionary and legacy fields for backward compat.
    /// </summary>
    public static CharacterAppearanceSaveData FromConfig(CharacterAppearanceConfig config)
    {
        if (config == null) return null;

        var data = new CharacterAppearanceSaveData
        {
            bodyType = config.bodyType ?? "male",
            slotPartIds = new Dictionary<string, string>(),
            skinTintHex = ColorUtility.ToHtmlStringRGBA(config.skinTint),
            hairTintHex = ColorUtility.ToHtmlStringRGBA(config.hairTint),
            armorPrimaryTintHex = ColorUtility.ToHtmlStringRGBA(config.armorPrimaryTint),
            armorSecondaryTintHex = ColorUtility.ToHtmlStringRGBA(config.armorSecondaryTint)
        };

        // Populate dictionary for all slots
        var allSlots = (BodyPartSlot[])Enum.GetValues(typeof(BodyPartSlot));
        foreach (var slot in allSlots)
        {
            var part = config.GetPart(slot);
            if (part != null && !string.IsNullOrEmpty(part.partId))
                data.slotPartIds[slot.ToString()] = part.partId;
        }

        // Also populate legacy fields for backward compat
        data.bodyId = GetPartId(config, BodyPartSlot.Body);
        data.headId = GetPartId(config, BodyPartSlot.Head);
        data.hairId = GetPartId(config, BodyPartSlot.Hair);
        data.torsoId = GetPartId(config, BodyPartSlot.Torso);
        data.legsId = GetPartId(config, BodyPartSlot.Legs);
        data.weaponBehindId = GetPartId(config, BodyPartSlot.WeaponBehind);
        data.weaponFrontId = GetPartId(config, BodyPartSlot.WeaponFront);

        return data;
    }

    private static string GetPartId(CharacterAppearanceConfig config, BodyPartSlot slot)
    {
        var part = config.GetPart(slot);
        return part != null ? part.partId : "";
    }

    /// <summary>
    /// Resolves this save data into a runtime CharacterAppearanceConfig.
    /// Reads the new dictionary first; falls back to legacy fields for v6 saves.
    /// Returns null if registry is unavailable.
    /// </summary>
    public CharacterAppearanceConfig ToConfig(BodyPartRegistry registry)
    {
        if (registry == null) return null;

        var config = ScriptableObject.CreateInstance<CharacterAppearanceConfig>();
        config.bodyType = bodyType ?? "male";

        // Prefer new dictionary, fall back to legacy fields
        if (slotPartIds != null && slotPartIds.Count > 0)
        {
            foreach (var kvp in slotPartIds)
            {
                if (Enum.TryParse<BodyPartSlot>(kvp.Key, out var slot))
                {
                    var part = registry.GetPartById(kvp.Value);
                    if (part != null)
                        config.SetPart(slot, part);
                    else if (!string.IsNullOrEmpty(kvp.Value))
                        Debug.LogWarning($"[AppearanceSave] Unresolved partId '{kvp.Value}' for slot {kvp.Key}");
                }
            }
        }
        else
        {
            // Legacy v6 fallback
            ResolveAndSet(config, registry, BodyPartSlot.Body, bodyId);
            ResolveAndSet(config, registry, BodyPartSlot.Head, headId);
            ResolveAndSet(config, registry, BodyPartSlot.Hair, hairId);
            ResolveAndSet(config, registry, BodyPartSlot.Torso, torsoId);
            ResolveAndSet(config, registry, BodyPartSlot.Legs, legsId);
            ResolveAndSet(config, registry, BodyPartSlot.WeaponBehind, weaponBehindId);
            ResolveAndSet(config, registry, BodyPartSlot.WeaponFront, weaponFrontId);
        }

        if (ColorUtility.TryParseHtmlString("#" + skinTintHex, out var skin))
            config.skinTint = skin;
        if (ColorUtility.TryParseHtmlString("#" + hairTintHex, out var hairColor))
            config.hairTint = hairColor;
        if (ColorUtility.TryParseHtmlString("#" + armorPrimaryTintHex, out var primary))
            config.armorPrimaryTint = primary;
        if (ColorUtility.TryParseHtmlString("#" + armorSecondaryTintHex, out var secondary))
            config.armorSecondaryTint = secondary;

        return config;
    }

    private static void ResolveAndSet(CharacterAppearanceConfig config, BodyPartRegistry registry,
        BodyPartSlot slot, string partId)
    {
        if (string.IsNullOrEmpty(partId)) return;
        var part = registry.GetPartById(partId);
        if (part != null)
            config.SetPart(slot, part);
        else
            Debug.LogWarning($"[AppearanceSave] Unresolved legacy partId '{partId}' for slot {slot}");
    }

    /// <summary>
    /// Returns true if this save data has any non-empty part IDs.
    /// </summary>
    public bool HasAnyParts()
    {
        if (slotPartIds != null && slotPartIds.Count > 0)
            return true;

        return !string.IsNullOrEmpty(bodyId) ||
               !string.IsNullOrEmpty(headId) ||
               !string.IsNullOrEmpty(hairId) ||
               !string.IsNullOrEmpty(torsoId) ||
               !string.IsNullOrEmpty(legsId);
    }

    /// <summary>
    /// Migrates a v6 save data to v7 format by populating the slotPartIds dictionary
    /// from legacy named fields. Called by SaveManager during version migration.
    /// </summary>
    public void MigrateFromV6()
    {
        if (slotPartIds == null)
            slotPartIds = new Dictionary<string, string>();

        MigrateField(BodyPartSlot.Body, bodyId);
        MigrateField(BodyPartSlot.Head, headId);
        MigrateField(BodyPartSlot.Hair, hairId);
        MigrateField(BodyPartSlot.Torso, torsoId);
        MigrateField(BodyPartSlot.Legs, legsId);
        MigrateField(BodyPartSlot.WeaponBehind, weaponBehindId);
        MigrateField(BodyPartSlot.WeaponFront, weaponFrontId);

        if (string.IsNullOrEmpty(bodyType))
            bodyType = "male";
    }

    private void MigrateField(BodyPartSlot slot, string partId)
    {
        if (!string.IsNullOrEmpty(partId) && !slotPartIds.ContainsKey(slot.ToString()))
            slotPartIds[slot.ToString()] = partId;
    }
}
