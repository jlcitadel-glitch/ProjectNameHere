using UnityEngine;

/// <summary>
/// ScriptableObject defining a charm (passive ability slotted into limited notch capacity).
/// Powerful charms cost more slots. Boss kills can expand total capacity.
/// Inspired by Hollow Knight's charm/notch system.
/// </summary>
[CreateAssetMenu(fileName = "NewCharm", menuName = "Skills/Charm Data")]
public class CharmData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique identifier for this charm")]
    public string charmId;

    [Tooltip("Display name")]
    public string charmName;

    [Tooltip("Charm description")]
    [TextArea(3, 6)]
    public string description;

    [Tooltip("Charm icon for UI")]
    public Sprite icon;

    [Header("Slotting")]
    [Tooltip("Number of notch slots this charm requires to equip")]
    [Range(1, 5)]
    public int notchCost = 1;

    [Tooltip("Charms that cannot be equipped alongside this one")]
    public CharmData[] incompatibleCharms;

    [Header("Effects")]
    [Tooltip("Stat modifiers while equipped")]
    public CharmStatModifier[] statModifiers;

    [Tooltip("Abilities granted while equipped")]
    public AbilityData[] grantedAbilities;

    [Header("Acquisition")]
    [Tooltip("How this charm is obtained")]
    public ProgressionSource source = ProgressionSource.Exploration;

    [Tooltip("Required player level to equip")]
    public int requiredLevel = 1;

    [Tooltip("Required archetype(s) — empty means any class")]
    public BaseArchetype[] requiredArchetypes;

    [Header("Synergy")]
    [Tooltip("Charms that create a synergy bonus when equipped together")]
    public CharmData[] synergyCharms;

    [Tooltip("Description of the synergy effect")]
    public string synergyDescription;
}

/// <summary>
/// A single stat modification applied by a charm while equipped.
/// </summary>
[System.Serializable]
public class CharmStatModifier
{
    [Tooltip("Which stat to modify")]
    public CharmStat stat;

    [Tooltip("Flat bonus added to the stat")]
    public float flatBonus;

    [Tooltip("Percentage modifier (1.0 = no change, 1.2 = +20%)")]
    public float percentModifier = 1f;
}

/// <summary>
/// Stats that charms can modify.
/// </summary>
public enum CharmStat
{
    MaxHP,
    MaxMP,
    Attack,
    Magic,
    Defense,
    MoveSpeed,
    CritChance,
    CritDamage,
    DodgeChance,
    HPRegen,
    MPRegen,
    SoulDropRate,
    CurrencyBonus,
    XPBonus
}
