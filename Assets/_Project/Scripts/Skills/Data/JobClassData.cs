using UnityEngine;

/// <summary>
/// ScriptableObject defining a job class and its advancement path.
/// Contains identity, requirements, available skills, and SP allocation rules.
/// </summary>
[CreateAssetMenu(fileName = "NewJobClass", menuName = "Skills/Job Class Data")]
public class JobClassData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique identifier for this job")]
    public string jobId;

    [Tooltip("Display name")]
    public string jobName;

    [Tooltip("Job description")]
    [TextArea(3, 6)]
    public string description;

    [Tooltip("Job class icon")]
    public Sprite jobIcon;

    [Tooltip("Theme color for UI")]
    public Color jobColor = Color.white;

    [Header("Classification")]
    [Tooltip("Advancement tier of this job")]
    public JobTier tier = JobTier.Beginner;

    [Tooltip("Parent job (null for Beginner)")]
    public JobClassData parentJob;

    [Tooltip("Available advancement options from this job")]
    public JobClassData[] childJobs;

    [Header("Advancement Requirements")]
    [Tooltip("Player level required to advance to this job")]
    public int requiredLevel = 1;

    [Tooltip("Skills from previous job that must be learned")]
    public SkillData[] requiredSkills;

    [Tooltip("Required levels for each prerequisite skill")]
    public int[] requiredSkillLevels;

    [Tooltip("Quest or item requirement (optional)")]
    public string advancementQuestId;

    [Header("Skills")]
    [Tooltip("Skill tree for this job")]
    public SkillTreeData skillTree;

    [Tooltip("All skills available to this job")]
    public SkillData[] availableSkills;

    [Header("SP Allocation")]
    [Tooltip("SP gained per player level")]
    public int spPerLevel = 3;

    [Tooltip("Bonus SP awarded on job advancement")]
    public int bonusSPOnAdvancement = 5;

    [Tooltip("Maximum SP that can be spent in this job's tree")]
    public int maxSPInTree = -1; // -1 = unlimited

    [Header("Stats")]
    [Tooltip("Base HP bonus for this job")]
    public int baseHPBonus;

    [Tooltip("Base MP bonus for this job")]
    public int baseMPBonus;

    [Tooltip("Attack power modifier (1.0 = base)")]
    public float attackModifier = 1f;

    [Tooltip("Magic power modifier (1.0 = base)")]
    public float magicModifier = 1f;

    [Tooltip("Defense modifier (1.0 = base)")]
    public float defenseModifier = 1f;

    /// <summary>
    /// Checks if a player meets the requirements to advance to this job.
    /// </summary>
    public bool CanAdvance(int playerLevel, System.Func<SkillData, int> getSkillLevel)
    {
        // Check level requirement
        if (playerLevel < requiredLevel)
            return false;

        // Check skill requirements
        if (requiredSkills != null && requiredSkillLevels != null)
        {
            for (int i = 0; i < requiredSkills.Length; i++)
            {
                if (requiredSkills[i] == null) continue;

                int requiredLevel = i < requiredSkillLevels.Length ? requiredSkillLevels[i] : 1;
                int currentLevel = getSkillLevel?.Invoke(requiredSkills[i]) ?? 0;

                if (currentLevel < requiredLevel)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the full job path from Beginner to this job.
    /// </summary>
    public JobClassData[] GetJobPath()
    {
        var path = new System.Collections.Generic.List<JobClassData>();
        var current = this;

        while (current != null)
        {
            path.Insert(0, current);
            current = current.parentJob;
        }

        return path.ToArray();
    }

    /// <summary>
    /// Checks if this job is an ancestor of another job.
    /// </summary>
    public bool IsAncestorOf(JobClassData other)
    {
        var current = other;
        while (current != null)
        {
            if (current.parentJob == this)
                return true;
            current = current.parentJob;
        }
        return false;
    }

    /// <summary>
    /// Gets the tier level as an integer for comparison.
    /// </summary>
    public int GetTierLevel()
    {
        return (int)tier;
    }

    /// <summary>
    /// Gets a formatted requirement string for UI display.
    /// </summary>
    public string GetRequirementsSummary()
    {
        var requirements = new System.Text.StringBuilder();

        requirements.AppendLine($"Level {requiredLevel} Required");

        if (requiredSkills != null && requiredSkills.Length > 0)
        {
            requirements.AppendLine("Required Skills:");
            for (int i = 0; i < requiredSkills.Length; i++)
            {
                if (requiredSkills[i] == null) continue;
                int level = i < requiredSkillLevels.Length ? requiredSkillLevels[i] : 1;
                requirements.AppendLine($"  - {requiredSkills[i].skillName} Lv.{level}");
            }
        }

        if (!string.IsNullOrEmpty(advancementQuestId))
        {
            requirements.AppendLine("Advancement Quest Required");
        }

        return requirements.ToString().TrimEnd();
    }
}
