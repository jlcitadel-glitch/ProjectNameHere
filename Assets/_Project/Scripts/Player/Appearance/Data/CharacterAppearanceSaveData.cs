using System;
using UnityEngine;

/// <summary>
/// Serializable data for saving/loading a character's appearance.
/// Stores body part IDs (resolved via BodyPartRegistry) and color hex strings.
/// </summary>
[Serializable]
public class CharacterAppearanceSaveData
{
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

    /// <summary>
    /// Creates save data from a CharacterAppearanceConfig.
    /// </summary>
    public static CharacterAppearanceSaveData FromConfig(CharacterAppearanceConfig config)
    {
        if (config == null) return null;

        return new CharacterAppearanceSaveData
        {
            bodyId = config.body != null ? config.body.partId : "",
            headId = config.head != null ? config.head.partId : "",
            hairId = config.hair != null ? config.hair.partId : "",
            torsoId = config.torso != null ? config.torso.partId : "",
            legsId = config.legs != null ? config.legs.partId : "",
            weaponBehindId = config.weaponBehind != null ? config.weaponBehind.partId : "",
            weaponFrontId = config.weaponFront != null ? config.weaponFront.partId : "",
            skinTintHex = ColorUtility.ToHtmlStringRGBA(config.skinTint),
            hairTintHex = ColorUtility.ToHtmlStringRGBA(config.hairTint),
            armorPrimaryTintHex = ColorUtility.ToHtmlStringRGBA(config.armorPrimaryTint),
            armorSecondaryTintHex = ColorUtility.ToHtmlStringRGBA(config.armorSecondaryTint)
        };
    }

    /// <summary>
    /// Resolves this save data into a runtime CharacterAppearanceConfig.
    /// Returns null if registry is unavailable.
    /// </summary>
    public CharacterAppearanceConfig ToConfig(BodyPartRegistry registry)
    {
        if (registry == null) return null;

        var config = ScriptableObject.CreateInstance<CharacterAppearanceConfig>();
        config.body = registry.GetPartById(bodyId);
        config.head = registry.GetPartById(headId);
        config.hair = registry.GetPartById(hairId);
        config.torso = registry.GetPartById(torsoId);
        config.legs = registry.GetPartById(legsId);
        config.weaponBehind = registry.GetPartById(weaponBehindId);
        config.weaponFront = registry.GetPartById(weaponFrontId);

        if (ColorUtility.TryParseHtmlString("#" + skinTintHex, out var skin))
            config.skinTint = skin;
        if (ColorUtility.TryParseHtmlString("#" + hairTintHex, out var hair))
            config.hairTint = hair;
        if (ColorUtility.TryParseHtmlString("#" + armorPrimaryTintHex, out var primary))
            config.armorPrimaryTint = primary;
        if (ColorUtility.TryParseHtmlString("#" + armorSecondaryTintHex, out var secondary))
            config.armorSecondaryTint = secondary;

        return config;
    }

    /// <summary>
    /// Returns true if this save data has any non-empty part IDs.
    /// </summary>
    public bool HasAnyParts()
    {
        return !string.IsNullOrEmpty(bodyId) ||
               !string.IsNullOrEmpty(headId) ||
               !string.IsNullOrEmpty(hairId) ||
               !string.IsNullOrEmpty(torsoId) ||
               !string.IsNullOrEmpty(legsId);
    }
}
