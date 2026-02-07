using System;
using UnityEngine;

/// <summary>
/// Manages player stats (STR/INT/AGI) with base values from class auto-growth
/// and manual allocation from stat points earned on level-up.
/// </summary>
public class StatSystem : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private int statPointsPerLevel = 5;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    // Base stats (from class auto-growth each level)
    private int baseStrength = 1;
    private int baseIntelligence = 1;
    private int baseAgility = 1;

    // Manually allocated stats
    private int allocatedStrength;
    private int allocatedIntelligence;
    private int allocatedAgility;

    // Available points for manual allocation
    private int availableStatPoints;

    // Component references
    private LevelSystem levelSystem;

    // Total stats
    public int Strength => baseStrength + allocatedStrength;
    public int Intelligence => baseIntelligence + allocatedIntelligence;
    public int Agility => baseAgility + allocatedAgility;
    public int AvailableStatPoints => availableStatPoints;

    // Derived stats
    public float BonusMaxHP => Strength * 5f;
    public float MeleeDamageMultiplier => 1f + (Strength * 0.02f);
    public float BonusMaxMana => Intelligence * 3f;
    public float SkillDamageMultiplier => 1f + (Intelligence * 0.02f);
    public float SpeedMultiplier => 1f + (Agility * 0.01f);
    public float CritChance => Mathf.Min(0.5f, Agility * 0.005f);

    // Events
    public event Action OnStatsChanged;
    public event Action<int> OnStatPointsChanged;

    private void Awake()
    {
        levelSystem = GetComponent<LevelSystem>();
    }

    private void Start()
    {
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp += HandleLevelUp;
        }
    }

    private void OnDestroy()
    {
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp -= HandleLevelUp;
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        // Award manual stat points
        availableStatPoints += statPointsPerLevel;

        // Apply class auto-growth
        ApplyAutoGrowth();

        if (debugLogging)
        {
            Debug.Log($"[StatSystem] Level {newLevel}: +{statPointsPerLevel} stat points, " +
                $"STR={Strength} INT={Intelligence} AGI={Agility}");
        }

        OnStatPointsChanged?.Invoke(availableStatPoints);
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// Applies auto-growth stats from the current job class.
    /// </summary>
    private void ApplyAutoGrowth()
    {
        if (SkillManager.Instance == null || SkillManager.Instance.CurrentJob == null)
        {
            // Default growth when no class assigned
            baseStrength += 1;
            baseIntelligence += 1;
            baseAgility += 1;
            return;
        }

        var job = SkillManager.Instance.CurrentJob;
        baseStrength += job.strPerLevel;
        baseIntelligence += job.intPerLevel;
        baseAgility += job.agiPerLevel;
    }

    /// <summary>
    /// Allocates one stat point to the specified stat.
    /// </summary>
    public bool AllocateStat(string statName)
    {
        if (availableStatPoints <= 0)
            return false;

        switch (statName.ToLower())
        {
            case "str":
            case "strength":
                allocatedStrength++;
                break;
            case "int":
            case "intelligence":
                allocatedIntelligence++;
                break;
            case "agi":
            case "agility":
                allocatedAgility++;
                break;
            default:
                return false;
        }

        availableStatPoints--;

        if (debugLogging)
        {
            Debug.Log($"[StatSystem] Allocated 1 point to {statName}. Remaining: {availableStatPoints}");
        }

        OnStatPointsChanged?.Invoke(availableStatPoints);
        OnStatsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Refunds all manually allocated stat points.
    /// </summary>
    public void ResetAllocations()
    {
        int refunded = allocatedStrength + allocatedIntelligence + allocatedAgility;
        availableStatPoints += refunded;
        allocatedStrength = 0;
        allocatedIntelligence = 0;
        allocatedAgility = 0;

        if (debugLogging)
        {
            Debug.Log($"[StatSystem] Reset allocations. Refunded {refunded} points.");
        }

        OnStatPointsChanged?.Invoke(availableStatPoints);
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// Initializes base stats for a given class (used at character creation).
    /// </summary>
    public void InitializeForClass(JobClassData jobClass)
    {
        if (jobClass == null)
            return;

        baseStrength = jobClass.strPerLevel;
        baseIntelligence = jobClass.intPerLevel;
        baseAgility = jobClass.agiPerLevel;

        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// Creates serializable save data for this system.
    /// </summary>
    public StatSaveData CreateSaveData()
    {
        return new StatSaveData
        {
            baseStrength = baseStrength,
            baseIntelligence = baseIntelligence,
            baseAgility = baseAgility,
            allocatedStrength = allocatedStrength,
            allocatedIntelligence = allocatedIntelligence,
            allocatedAgility = allocatedAgility,
            availableStatPoints = availableStatPoints
        };
    }

    /// <summary>
    /// Restores state from save data.
    /// </summary>
    public void ApplySaveData(StatSaveData data)
    {
        if (data == null)
            return;

        baseStrength = data.baseStrength;
        baseIntelligence = data.baseIntelligence;
        baseAgility = data.baseAgility;
        allocatedStrength = data.allocatedStrength;
        allocatedIntelligence = data.allocatedIntelligence;
        allocatedAgility = data.allocatedAgility;
        availableStatPoints = data.availableStatPoints;

        OnStatPointsChanged?.Invoke(availableStatPoints);
        OnStatsChanged?.Invoke();
    }
}

/// <summary>
/// Serializable save data for the stat system.
/// </summary>
[Serializable]
public class StatSaveData
{
    public int baseStrength;
    public int baseIntelligence;
    public int baseAgility;
    public int allocatedStrength;
    public int allocatedIntelligence;
    public int allocatedAgility;
    public int availableStatPoints;
}
