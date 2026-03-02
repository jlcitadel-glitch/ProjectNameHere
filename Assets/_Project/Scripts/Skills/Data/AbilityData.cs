using UnityEngine;

/// <summary>
/// ScriptableObject defining a non-skill ability (traversal, environmental, combat, etc.).
/// These are unlocked through various progression sources rather than the SP skill tree.
/// Examples: double jump, wall cling, grapple hook, soul absorption, realm shift.
/// </summary>
[CreateAssetMenu(fileName = "NewAbility", menuName = "Skills/Ability Data")]
public class AbilityData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique identifier for this ability")]
    public string abilityId;

    [Tooltip("Display name")]
    public string abilityName;

    [Tooltip("Ability description")]
    [TextArea(3, 6)]
    public string description;

    [Tooltip("Ability icon for UI")]
    public Sprite icon;

    [Header("Classification")]
    [Tooltip("What category this ability falls into")]
    public AbilityCategory category = AbilityCategory.Traversal;

    [Tooltip("How this ability is acquired")]
    public ProgressionSource source = ProgressionSource.BossReward;

    [Header("Upgrade Path")]
    [Tooltip("Whether this ability can be upgraded (e.g., double jump → triple jump)")]
    public bool isUpgradeable;

    [Tooltip("Current max tier (1 = base, 2+ = upgraded forms)")]
    [Range(1, 5)]
    public int maxTier = 1;

    [Tooltip("Display names for each tier (index 0 = tier 1)")]
    public string[] tierNames;

    [Tooltip("Descriptions for each tier")]
    [TextArea(2, 4)]
    public string[] tierDescriptions;

    [Header("Requirements")]
    [Tooltip("Abilities that must be unlocked first")]
    public AbilityData[] prerequisites;

    [Tooltip("Minimum player level to unlock")]
    public int requiredPlayerLevel = 1;

    [Tooltip("Required job archetype(s) — empty means any class can unlock")]
    public BaseArchetype[] requiredArchetypes;

    [Header("Acquisition")]
    [Tooltip("Boss that drops this ability (if source is BossReward)")]
    public string bossId;

    [Tooltip("Quest that rewards this ability (if source is QuestReward)")]
    public string questId;

    [Tooltip("Drop chance 0-1 (if source is SoulAbsorption, 1.0 for bosses)")]
    [Range(0f, 1f)]
    public float dropChance = 1f;

    [Header("Gameplay")]
    [Tooltip("Prefab spawned when ability is used (if applicable)")]
    public GameObject abilityPrefab;

    [Tooltip("Sound played when ability activates")]
    public AudioClip activateSound;

    [Tooltip("Animation trigger name")]
    public string animationTrigger;

    /// <summary>
    /// Gets the display name for a specific tier.
    /// </summary>
    public string GetTierName(int tier)
    {
        tier = Mathf.Clamp(tier, 1, maxTier);
        int index = tier - 1;
        if (tierNames != null && index < tierNames.Length && !string.IsNullOrEmpty(tierNames[index]))
            return tierNames[index];
        return tier > 1 ? $"{abilityName} {IntToRoman(tier)}" : abilityName;
    }

    /// <summary>
    /// Gets the description for a specific tier.
    /// </summary>
    public string GetTierDescription(int tier)
    {
        tier = Mathf.Clamp(tier, 1, maxTier);
        int index = tier - 1;
        if (tierDescriptions != null && index < tierDescriptions.Length)
            return tierDescriptions[index];
        return description;
    }

    private static string IntToRoman(int value)
    {
        return value switch
        {
            1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V",
            _ => value.ToString()
        };
    }
}
