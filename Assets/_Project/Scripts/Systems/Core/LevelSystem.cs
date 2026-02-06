using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Infinite leveling system with RuneScape-inspired exponential XP scaling.
/// XP required increases exponentially with no practical cap.
/// Stats scale forever - high level players are genuinely stronger.
/// </summary>
public class LevelSystem : MonoBehaviour
{
    [Header("Stat Scaling")]
    [SerializeField] private float baseHealth = 100f;
    [SerializeField] private float healthPerLevel = 5f;
    [SerializeField] private float baseMana = 50f;
    [SerializeField] private float manaPerLevel = 3f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    private int currentLevel = 1;
    private long currentXP = 0;
    private Dictionary<int, long> xpCache = new Dictionary<int, long>();

    private HealthSystem healthSystem;
    private ManaSystem manaSystem;

    public int CurrentLevel => currentLevel;
    public long CurrentXP => currentXP;
    public long XPForCurrentLevel => GetXPForLevel(currentLevel);
    public long XPForNextLevel => GetXPForLevel(currentLevel + 1);
    public long XPToNextLevel => XPForNextLevel - currentXP;

    /// <summary>
    /// Returns progress toward next level as 0-1 float.
    /// </summary>
    public float LevelProgress
    {
        get
        {
            long currentLevelXP = GetXPForLevel(currentLevel);
            long nextLevelXP = GetXPForLevel(currentLevel + 1);
            long xpIntoLevel = currentXP - currentLevelXP;
            long xpRequired = nextLevelXP - currentLevelXP;

            return xpRequired > 0 ? (float)xpIntoLevel / xpRequired : 0f;
        }
    }

    /// <summary>
    /// Fired when XP is gained. Provides (amount gained, new total XP).
    /// </summary>
    public event Action<long, long> OnXPGained;

    /// <summary>
    /// Fired when level increases. Provides new level.
    /// </summary>
    public event Action<int> OnLevelUp;

    /// <summary>
    /// Fired when XP changes. Provides (current XP, XP required for next level).
    /// </summary>
    public event Action<long, long> OnXPChanged;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        manaSystem = GetComponent<ManaSystem>();
    }

    private void Start()
    {
        ApplyStatScaling(refill: false);
        OnXPChanged?.Invoke(currentXP, XPForNextLevel);

        // Wire level-up to skill manager SP awards
        OnLevelUp += HandleLevelUpForSkills;

        if (debugLogging)
        {
            LogMilestones();
        }
    }

    private void OnDestroy()
    {
        OnLevelUp -= HandleLevelUpForSkills;
    }

    private void HandleLevelUpForSkills(int newLevel)
    {
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.SetPlayerLevel(newLevel);
        }
    }

    /// <summary>
    /// Adds XP and processes any resulting level-ups.
    /// </summary>
    public void AddXP(long amount)
    {
        if (amount <= 0)
            return;

        currentXP += amount;

        if (debugLogging)
        {
            Debug.Log($"[LevelSystem] Gained {amount:N0} XP. Total: {currentXP:N0}");
        }

        OnXPGained?.Invoke(amount, currentXP);

        while (currentXP >= GetXPForLevel(currentLevel + 1))
        {
            currentLevel++;

            if (debugLogging)
            {
                Debug.Log($"[LevelSystem] Level up! Now level {currentLevel}. Next level at {GetXPForLevel(currentLevel + 1):N0} XP");
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
        if (level < 1)
            level = 1;

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
    /// Uses RuneScape formula extended infinitely with caching for performance.
    /// </summary>
    public long GetXPForLevel(int level)
    {
        if (level <= 1)
            return 0;

        if (xpCache.TryGetValue(level, out long cachedXP))
            return cachedXP;

        // Calculate from the highest cached level or from scratch
        int startLevel = 1;
        long totalXP = 0;

        // Find highest cached level below target
        for (int i = level - 1; i >= 1; i--)
        {
            if (xpCache.TryGetValue(i, out long foundXP))
            {
                startLevel = i;
                totalXP = foundXP;
                break;
            }
        }

        // Calculate from startLevel to target level
        for (int lvl = startLevel; lvl < level; lvl++)
        {
            // RuneScape formula: floor((level + 300 * 2^(level/7)) / 4)
            double xpForLevel = Math.Floor((lvl + 300.0 * Math.Pow(2, lvl / 7.0)) / 4.0);
            totalXP += (long)xpForLevel;

            // Cache intermediate values for future lookups
            if (!xpCache.ContainsKey(lvl + 1))
            {
                xpCache[lvl + 1] = totalXP;
            }
        }

        return totalXP;
    }

    /// <summary>
    /// Returns the level that corresponds to the given total XP.
    /// Uses binary search for efficiency with large XP values.
    /// </summary>
    public int GetLevelFromXP(long xp)
    {
        if (xp <= 0)
            return 1;

        // Binary search to find the level
        int low = 1;
        int high = 1000; // Start with reasonable upper bound

        // Expand upper bound if needed
        while (GetXPForLevel(high) <= xp)
        {
            high *= 2;
        }

        // Binary search
        while (low < high)
        {
            int mid = (low + high + 1) / 2;
            if (GetXPForLevel(mid) <= xp)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low;
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
    /// Formats XP value with thousand separators for display.
    /// </summary>
    public static string FormatXP(long xp)
    {
        return xp.ToString("N0");
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
                Debug.Log($"[LevelSystem] Max Health set to {newMaxHealth:N0}");
            }
        }

        if (manaSystem != null)
        {
            manaSystem.SetMaxMana(newMaxMana, refill);

            if (debugLogging)
            {
                Debug.Log($"[LevelSystem] Max Mana set to {newMaxMana:N0}");
            }
        }
    }

    /// <summary>
    /// Logs XP milestones for debugging and design reference.
    /// </summary>
    private void LogMilestones()
    {
        Debug.Log("[LevelSystem] === XP Milestones ===");
        int[] milestones = { 10, 25, 50, 75, 99, 100, 126, 150, 200, 250, 300, 500 };
        foreach (int level in milestones)
        {
            long xp = GetXPForLevel(level);
            float hp = GetMaxHealthForLevel(level);
            float mp = GetMaxManaForLevel(level);
            Debug.Log($"Level {level}: {xp:N0} XP | {hp:N0} HP | {mp:N0} MP");
        }
        Debug.Log("[LevelSystem] =====================");
    }
}
