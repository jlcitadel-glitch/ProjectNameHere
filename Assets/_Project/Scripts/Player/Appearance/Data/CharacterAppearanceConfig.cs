using UnityEngine;

/// <summary>
/// ScriptableObject defining a complete character appearance — one BodyPartData per slot
/// plus tint colors. Used as the default appearance for a job class and as the
/// base configuration during character creation.
/// </summary>
[CreateAssetMenu(fileName = "NewAppearance", menuName = "Game/Character/Appearance Config")]
public class CharacterAppearanceConfig : ScriptableObject
{
    [Header("Identity")]
    public string configId;
    public string displayName;

    [Header("Body Parts")]
    public BodyPartData body;
    public BodyPartData head;
    public BodyPartData hair;
    public BodyPartData torso;
    public BodyPartData legs;
    public BodyPartData weaponBehind;
    public BodyPartData weaponFront;

    [Header("Tints")]
    public Color skinTint = Color.white;
    public Color hairTint = Color.white;
    public Color armorPrimaryTint = Color.white;
    public Color armorSecondaryTint = Color.white;

    /// <summary>
    /// Returns the BodyPartData for a given slot, or null.
    /// </summary>
    public BodyPartData GetPart(BodyPartSlot slot)
    {
        return slot switch
        {
            BodyPartSlot.Body => body,
            BodyPartSlot.Head => head,
            BodyPartSlot.Hair => hair,
            BodyPartSlot.Torso => torso,
            BodyPartSlot.Legs => legs,
            BodyPartSlot.WeaponBehind => weaponBehind,
            BodyPartSlot.WeaponFront => weaponFront,
            _ => null
        };
    }
}
