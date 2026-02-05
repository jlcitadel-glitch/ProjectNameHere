using System;
using UnityEngine;

/// <summary>
/// RuneScape-style leveling system with logarithmic XP scaling.
/// XP required increases exponentially toward max level 99.
/// Leveling up increases base health and mana by fixed intervals.
/// </summary>
public class LevelSystem : MonoBehaviour
{
    [Header("Level Settings")]
    [SerializeField] private int maxLevel = 99;

    [Header("Stat Scaling")]
    [SerializeField] private float baseHealth = 100f;
    [SerializeField] private float healthPerLevel = 5f;
    [SerializeField] private float baseMana = 50f;
    [SerializeField] private float manaPerLevel = 3f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    private int currentLevel = 1;
    private int currentXP = 0;
    private int[] xpTable;

    private HealthSystem healthSystem;
    private ManaSystem manaSystem;

    public int CurrentLevel => currentLevel;
    public int CurrentXP => currentXP;
    public int MaxLevel => maxLevel;
    public int XPForCurrentLevel => GetXPForLevel(currentLevel);
    public int XPForNextLevel => currentLevel < maxLevel ? GetXPForLevel(currentLevel + 1) : GetXPForLevel(maxLevel);
    public int XPToNextLevel => currentLevel < maxLevel ? XPForNextLevel - currentXP : 0;
    public bool IsMaxLevel => currentLevel >= maxLevel;

    /// <summary>
    /// Returns progress toward next level as 0-1 float.
    /// </summary>
    public float LevelProgress
    {
        get
        {
            if (currentLevel >= maxLevel)
                return 1f;

            int currentLevelXP = GetXPForLevel(currentLevel);
            int nextLevelXP = GetXPForLevel(currentLevel + 1);
            int xpIntoLevel = currentXP - currentLevelXP;
            int xpRequired = nextLevelXP - currentLevelXP;

            return xpRequired > 0 ? (float)xpIntoLevel / xpRequired : 1f;
        }
    }

    /// <summary>
    /// Fired when XP is gained. Provides (amount gained, new total XP).
    /// </summary>
    public event Action<int, int> OnXPGained;

    /// <summary>
    /// Fired when level increases. Provides new level.
    /// </summary>
    public event Action<int> OnLevelUp;

    /// <summary>
    /// Fired when XP changes. Provides (current XP, XP required for next level).
    /// </summary>
    public event Action<int, int> OnXPChanged;

    private void Awake()
    {
        BuildXPTable();
        healthSystem = GetComponent<HealthSystem>();
        manaSystem = GetComponent<ManaSystem>();
    }

    private void Start()
    {
        ApplyStatScaling(refill: false);
        OnXPChanged?.Invoke(currentXP, XPForNextLevel);
    }

    /// <summary>
    /// Adds XP and processes any resulting level-ups.
    /// </summary>
    public void AddXP(int amount)
    {
        if (amount <= 0)
            return;

        currentXP += amount;

        if (debugLogging)
        {
            Debug.Log($"[LevelSystem] Gained {amount} XP. Total: {currentXP}");
        }

        OnXPGained?.Invoke(amount, currentXP);

        while (currentLevel < maxLevel && currentXP >= GetXPForLevel(currentLevel + 1))
        {
            currentLevel++;

            if (debugLogging)
            {
                Debug.Log($"[LevelSystem] Level up! Now level {currentLevel}");
            }

            ApplyStatScaling(refill: true);
            OnLevelUp?.Invoke(currentLevel);
        }

        OnXPChanged?.Invoke(currentXP, XPForNextLevel);
    }

    /// <summary>
    /// Sets level directly (for debugging/cheats). Also sets XP to minimum for that level.
    /// </summary>
    public void SetLevel(int level)
    {
        level = Mathf.Clamp(level, 1, maxLevel);

        if (level == currentLevel)
            return;

        int oldLevel = currentLevel;
        currentLevel = level;
        currentXP = GetXPForLevel(level);

        if (debugLogging)
        {
            Debug.Log($"[LevelSystem] Level set from {oldLevel} to {currentLevel}");
        }

        ApplyStatScaling(refill: true);

        if (level > oldLevel)
        {
            OnLevelUp?.Invoke(currentLevel);
        }

        OnXPChanged?.Invoke(currentXP, XPForNextLevel);
    }

    /// <summary>
    /// Returns total XP required to reach the specified level.
    /// </summary>
    public int GetXPForLevel(int level)
    {
        if (level <= 1)
            return 0;
        if (level > maxLevel)
            level = maxLevel;

        return xpTable[level];
    }

    /// <summary>
    /// Returns the level that corresponds to the given total XP.
    /// </summary>
    public int GetLevelFromXP(int xp)
    {
        if (xp <= 0)
            return 1;

        for (int level = maxLevel; level >= 1; level--)
        {
            if (xp >= xpTable[level])
                return level;
        }

        return 1;
    }

    /// <summary>
    /// Returns the max health for a given level.
    /// </summary>
    public float GetMaxHealthForLevel(int level)
    {
        return baseHealth + (level - 1) * healthPerLevel;
    }

    /// <summary>
    /// Returns the max mana for a given level.
    /// </summary>
    public float GetMaxManaForLevel(int level)
    {
        return baseMana + (level - 1) * manaPerLevel;
    }

    /// <summary>
    /// Pre-calculates the XP table using the RuneScape formula.
    /// </summary>
    private void BuildXPTable()
    {
        xpTable = new int[maxLevel + 1];
        xpTable[1] = 0;

        int totalXP = 0;
        for (int level = 1; level < maxLevel; level++)
        {
            // RuneScape formula: floor((level + 300 * 2^(level/7)) / 4)
            double xpForLevel = Math.Floor((level + 300.0 * Math.Pow(2, level / 7.0)) / 4.0);
            totalXP += (int)xpForLevel;
            xpTable[level + 1] = totalXP;
        }

        if (debugLogging)
        {
            Debug.Log($"[LevelSystem] XP Table built. Level 99 requires {xpTable[99]:N0} XP");
        }
    }

    /// <summary>
    /// Applies stat scaling based on current level.
    /// </summary>
    private void ApplyStatScaling(bool refill)
    {
        float newMaxHealth = GetMaxHealthForLevel(currentLevel);
        float newMaxMana = GetMaxManaForLevel(currentLevel);

        if (healthSystem != null)
        {
            healthSystem.SetMaxHealth(newMaxHealth, refill);

            if (debugLogging)
            {
                Debug.Log($"[LevelSystem] Max Health set to {newMaxHealth}");
            }
        }

        if (manaSystem != null)
        {
            manaSystem.SetMaxMana(newMaxMana, refill);

            if (debugLogging)
            {
                Debug.Log($"[LevelSystem] Max Mana set to {newMaxMana}");
            }
        }
    }
}
