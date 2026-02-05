using UnityEngine;

/// <summary>
/// ScriptableObject defining an individual skill's properties.
/// Contains identity, requirements, stats, scaling, and effects.
/// </summary>
[CreateAssetMenu(fileName = "NewSkill", menuName = "Skills/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique identifier for this skill")]
    public string skillId;

    [Tooltip("Display name")]
    public string skillName;

    [Tooltip("Skill description (supports rich text)")]
    [TextArea(3, 6)]
    public string description;

    [Tooltip("Skill icon for UI")]
    public Sprite icon;

    [Header("Classification")]
    [Tooltip("Behavior type of this skill")]
    public SkillType skillType = SkillType.Active;

    [Tooltip("Damage/element type")]
    public DamageType damageType = DamageType.Physical;

    [Tooltip("Job ID required to learn this skill")]
    public string requiredJobId;

    [Header("Requirements")]
    [Tooltip("Minimum player level to learn")]
    public int requiredPlayerLevel = 1;

    [Tooltip("Maximum skill level (1-20 typical)")]
    [Range(1, 30)]
    public int maxSkillLevel = 20;

    [Tooltip("Skills that must be learned first")]
    public SkillData[] prerequisiteSkills;

    [Tooltip("Required levels for each prerequisite skill")]
    public int[] prerequisiteLevels;

    [Tooltip("SP cost to learn/upgrade this skill")]
    public int spCost = 1;

    [Header("Base Stats (Level 1)")]
    [Tooltip("Base damage at level 1")]
    public float baseDamage;

    [Tooltip("Base mana cost at level 1")]
    public float baseManaCost = 10f;

    [Tooltip("Base cooldown in seconds")]
    public float baseCooldown = 5f;

    [Tooltip("Base duration for buffs/DoT")]
    public float baseDuration;

    [Header("Scaling Per Level")]
    [Tooltip("Damage increase per level")]
    public float damagePerLevel = 5f;

    [Tooltip("Mana cost increase per level")]
    public float manaCostPerLevel = 2f;

    [Tooltip("Cooldown reduction per level (in seconds)")]
    public float cooldownReductionPerLevel = 0.1f;

    [Tooltip("Duration increase per level")]
    public float durationPerLevel;

    [Tooltip("Minimum cooldown (won't go below this)")]
    public float minimumCooldown = 0.5f;

    [Header("Skill Tree Position")]
    [Tooltip("Position in the skill tree UI")]
    public Vector2 nodePosition;

    [Tooltip("Tier within the skill tree (0 = root)")]
    public int tier;

    [Header("Effects")]
    [Tooltip("Effects applied when skill is used")]
    public SkillEffectData[] effects;

    [Tooltip("Prefab instantiated when skill is cast")]
    public GameObject skillPrefab;

    [Tooltip("Sound played when skill is cast")]
    public AudioClip castSound;

    [Header("Animation")]
    [Tooltip("Animation trigger name")]
    public string animationTrigger;

    [Tooltip("Cast time before skill activates (0 for instant)")]
    public float castTime;

    /// <summary>
    /// Calculates damage at the specified skill level.
    /// </summary>
    public float GetDamage(int level)
    {
        level = Mathf.Clamp(level, 1, maxSkillLevel);
        return baseDamage + (damagePerLevel * (level - 1));
    }

    /// <summary>
    /// Calculates mana cost at the specified skill level.
    /// </summary>
    public float GetManaCost(int level)
    {
        level = Mathf.Clamp(level, 1, maxSkillLevel);
        return baseManaCost + (manaCostPerLevel * (level - 1));
    }

    /// <summary>
    /// Calculates cooldown at the specified skill level.
    /// </summary>
    public float GetCooldown(int level)
    {
        level = Mathf.Clamp(level, 1, maxSkillLevel);
        float cooldown = baseCooldown - (cooldownReductionPerLevel * (level - 1));
        return Mathf.Max(cooldown, minimumCooldown);
    }

    /// <summary>
    /// Calculates duration at the specified skill level.
    /// </summary>
    public float GetDuration(int level)
    {
        level = Mathf.Clamp(level, 1, maxSkillLevel);
        return baseDuration + (durationPerLevel * (level - 1));
    }

    /// <summary>
    /// Gets a formatted description with current level stats.
    /// </summary>
    public string GetFormattedDescription(int level)
    {
        string desc = description;

        desc = desc.Replace("{damage}", GetDamage(level).ToString("F0"));
        desc = desc.Replace("{manaCost}", GetManaCost(level).ToString("F0"));
        desc = desc.Replace("{cooldown}", GetCooldown(level).ToString("F1"));
        desc = desc.Replace("{duration}", GetDuration(level).ToString("F1"));
        desc = desc.Replace("{level}", level.ToString());
        desc = desc.Replace("{maxLevel}", maxSkillLevel.ToString());

        return desc;
    }

    /// <summary>
    /// Gets the description showing next level improvements.
    /// </summary>
    public string GetNextLevelDescription(int currentLevel)
    {
        if (currentLevel >= maxSkillLevel)
            return "MAX LEVEL";

        int nextLevel = currentLevel + 1;
        string improvements = "";

        if (damagePerLevel > 0)
            improvements += $"Damage: {GetDamage(currentLevel):F0} -> {GetDamage(nextLevel):F0}\n";

        if (manaCostPerLevel != 0)
            improvements += $"Mana Cost: {GetManaCost(currentLevel):F0} -> {GetManaCost(nextLevel):F0}\n";

        if (cooldownReductionPerLevel > 0)
            improvements += $"Cooldown: {GetCooldown(currentLevel):F1}s -> {GetCooldown(nextLevel):F1}s\n";

        if (durationPerLevel > 0)
            improvements += $"Duration: {GetDuration(currentLevel):F1}s -> {GetDuration(nextLevel):F1}s";

        return improvements.TrimEnd('\n');
    }
}
