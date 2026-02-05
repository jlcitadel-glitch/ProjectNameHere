using System;

/// <summary>
/// Runtime instance of a learned skill.
/// Tracks the current level and provides access to scaled stats.
/// </summary>
[Serializable]
public class SkillInstance
{
    /// <summary>
    /// Reference to the skill data definition.
    /// </summary>
    public SkillData skillData;

    /// <summary>
    /// Current level of this skill (1 to maxSkillLevel).
    /// </summary>
    public int currentLevel;

    /// <summary>
    /// Total SP invested in this skill.
    /// </summary>
    public int totalSPInvested;

    /// <summary>
    /// Whether this skill is currently active (for toggle skills).
    /// </summary>
    public bool isActive;

    public string SkillId => skillData?.skillId ?? "";
    public string SkillName => skillData?.skillName ?? "Unknown";
    public SkillType SkillType => skillData?.skillType ?? SkillType.Active;
    public bool IsMaxLevel => currentLevel >= (skillData?.maxSkillLevel ?? 1);
    public int MaxLevel => skillData?.maxSkillLevel ?? 1;

    public SkillInstance() { }

    public SkillInstance(SkillData data, int level = 1)
    {
        skillData = data;
        currentLevel = Math.Max(1, Math.Min(level, data?.maxSkillLevel ?? 1));
        totalSPInvested = currentLevel * (data?.spCost ?? 1);
        isActive = false;
    }

    /// <summary>
    /// Gets the damage at current level.
    /// </summary>
    public float GetDamage()
    {
        return skillData?.GetDamage(currentLevel) ?? 0f;
    }

    /// <summary>
    /// Gets the mana cost at current level.
    /// </summary>
    public float GetManaCost()
    {
        return skillData?.GetManaCost(currentLevel) ?? 0f;
    }

    /// <summary>
    /// Gets the cooldown at current level.
    /// </summary>
    public float GetCooldown()
    {
        return skillData?.GetCooldown(currentLevel) ?? 0f;
    }

    /// <summary>
    /// Gets the duration at current level.
    /// </summary>
    public float GetDuration()
    {
        return skillData?.GetDuration(currentLevel) ?? 0f;
    }

    /// <summary>
    /// Attempts to level up the skill.
    /// Returns true if successful.
    /// </summary>
    public bool LevelUp()
    {
        if (IsMaxLevel || skillData == null)
            return false;

        currentLevel++;
        totalSPInvested += skillData.spCost;
        return true;
    }

    /// <summary>
    /// Gets the SP cost to level up (returns 0 if max level).
    /// </summary>
    public int GetLevelUpCost()
    {
        if (IsMaxLevel || skillData == null)
            return 0;

        return skillData.spCost;
    }

    /// <summary>
    /// Creates save data for this skill instance.
    /// </summary>
    public LearnedSkillData ToSaveData()
    {
        return new LearnedSkillData
        {
            skillId = SkillId,
            level = currentLevel,
            spInvested = totalSPInvested,
            isActive = isActive
        };
    }

    /// <summary>
    /// Restores from save data.
    /// </summary>
    public static SkillInstance FromSaveData(LearnedSkillData saveData, SkillData skillData)
    {
        if (skillData == null) return null;

        return new SkillInstance
        {
            skillData = skillData,
            currentLevel = saveData.level,
            totalSPInvested = saveData.spInvested,
            isActive = saveData.isActive
        };
    }
}

/// <summary>
/// Serializable data for saving learned skill state.
/// </summary>
[Serializable]
public class LearnedSkillData
{
    public string skillId;
    public int level;
    public int spInvested;
    public bool isActive;
}
