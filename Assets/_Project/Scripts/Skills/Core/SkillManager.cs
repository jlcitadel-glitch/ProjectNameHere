using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager for the skill system.
/// Tracks learned skills, SP, job class, and handles skill learning/upgrading.
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Default job for new players")]
    [SerializeField] private JobClassData defaultJob;

    [Tooltip("All available skill data assets")]
    [SerializeField] private SkillData[] allSkillData;

    [Tooltip("All available job data assets")]
    [SerializeField] private JobClassData[] allJobData;

    [Header("Debug")]
    [SerializeField] private bool logEvents = true;

    // Runtime state
    private JobClassData currentJob;
    private List<JobClassData> jobHistory = new List<JobClassData>();
    private Dictionary<string, SkillInstance> learnedSkills = new Dictionary<string, SkillInstance>();
    private int availableSP;
    private int totalSPEarned;
    private int playerLevel = 1;

    // Lookup caches
    private Dictionary<string, SkillData> skillDataLookup = new Dictionary<string, SkillData>();
    private Dictionary<string, JobClassData> jobDataLookup = new Dictionary<string, JobClassData>();

    // Properties
    public JobClassData CurrentJob => currentJob;
    public IReadOnlyList<JobClassData> JobHistory => jobHistory;
    public int AvailableSP => availableSP;
    public int TotalSPEarned => totalSPEarned;
    public int PlayerLevel => playerLevel;
    public int LearnedSkillCount => learnedSkills.Count;

    // Events
    public event Action<JobClassData, JobClassData> OnJobChanged;
    public event Action<SkillInstance> OnSkillLearned;
    public event Action<SkillInstance, int, int> OnSkillLevelChanged;
    public event Action<int, int> OnSPChanged;
    public event Action<int, int> OnPlayerLevelChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[SkillManager] Duplicate instance on {gameObject.name}, destroying.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildLookupCaches();
        InitializeDefaultState();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void BuildLookupCaches()
    {
        skillDataLookup.Clear();
        if (allSkillData != null)
        {
            foreach (var skill in allSkillData)
            {
                if (skill != null && !string.IsNullOrEmpty(skill.skillId))
                {
                    skillDataLookup[skill.skillId] = skill;
                }
            }
        }

        jobDataLookup.Clear();
        if (allJobData != null)
        {
            foreach (var job in allJobData)
            {
                if (job != null && !string.IsNullOrEmpty(job.jobId))
                {
                    jobDataLookup[job.jobId] = job;
                }
            }
        }
    }

    private void InitializeDefaultState()
    {
        if (defaultJob != null)
        {
            currentJob = defaultJob;
            jobHistory.Add(defaultJob);

            if (logEvents)
                Debug.Log($"[SkillManager] Initialized with default job: {defaultJob.jobName}");
        }
    }

    /// <summary>
    /// Sets the player level and awards SP.
    /// </summary>
    public void SetPlayerLevel(int level)
    {
        if (level <= playerLevel) return;

        int previousLevel = playerLevel;
        int spGain = 0;

        // Calculate SP gain for each level
        for (int i = previousLevel + 1; i <= level; i++)
        {
            spGain += currentJob?.spPerLevel ?? 3;
        }

        playerLevel = level;
        AddSP(spGain);

        if (logEvents)
            Debug.Log($"[SkillManager] Level up: {previousLevel} -> {level}, gained {spGain} SP");

        OnPlayerLevelChanged?.Invoke(previousLevel, level);
    }

    /// <summary>
    /// Adds SP to the available pool.
    /// </summary>
    public void AddSP(int amount)
    {
        if (amount <= 0) return;

        int previousSP = availableSP;
        availableSP += amount;
        totalSPEarned += amount;

        OnSPChanged?.Invoke(previousSP, availableSP);
    }

    /// <summary>
    /// Checks if a skill can be learned.
    /// </summary>
    public bool CanLearnSkill(SkillData skill)
    {
        if (skill == null) return false;

        // Already learned?
        if (learnedSkills.ContainsKey(skill.skillId))
            return false;

        // Check job requirement
        if (!string.IsNullOrEmpty(skill.requiredJobId))
        {
            bool hasJob = false;
            foreach (var job in jobHistory)
            {
                if (job.jobId == skill.requiredJobId)
                {
                    hasJob = true;
                    break;
                }
            }
            if (!hasJob) return false;
        }

        // Check player level
        if (playerLevel < skill.requiredPlayerLevel)
            return false;

        // Check SP
        if (availableSP < skill.spCost)
            return false;

        // Check prerequisites
        if (skill.prerequisiteSkills != null)
        {
            for (int i = 0; i < skill.prerequisiteSkills.Length; i++)
            {
                var prereq = skill.prerequisiteSkills[i];
                if (prereq == null) continue;

                if (!learnedSkills.TryGetValue(prereq.skillId, out var instance))
                    return false;

                int requiredLevel = i < skill.prerequisiteLevels.Length ? skill.prerequisiteLevels[i] : 1;
                if (instance.currentLevel < requiredLevel)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Learns a new skill.
    /// </summary>
    public bool LearnSkill(SkillData skill)
    {
        if (!CanLearnSkill(skill))
            return false;

        var instance = new SkillInstance(skill, 1);
        learnedSkills[skill.skillId] = instance;
        availableSP -= skill.spCost;

        if (logEvents)
            Debug.Log($"[SkillManager] Learned skill: {skill.skillName}");

        OnSkillLearned?.Invoke(instance);
        OnSPChanged?.Invoke(availableSP + skill.spCost, availableSP);

        return true;
    }

    /// <summary>
    /// Checks if a skill can be upgraded.
    /// </summary>
    public bool CanUpgradeSkill(string skillId)
    {
        if (!learnedSkills.TryGetValue(skillId, out var instance))
            return false;

        if (instance.IsMaxLevel)
            return false;

        if (availableSP < instance.GetLevelUpCost())
            return false;

        return true;
    }

    /// <summary>
    /// Upgrades a learned skill by one level.
    /// </summary>
    public bool UpgradeSkill(string skillId)
    {
        if (!CanUpgradeSkill(skillId))
            return false;

        var instance = learnedSkills[skillId];
        int previousLevel = instance.currentLevel;
        int cost = instance.GetLevelUpCost();

        if (!instance.LevelUp())
            return false;

        availableSP -= cost;

        if (logEvents)
            Debug.Log($"[SkillManager] Upgraded {instance.SkillName}: Lv.{previousLevel} -> Lv.{instance.currentLevel}");

        OnSkillLevelChanged?.Invoke(instance, previousLevel, instance.currentLevel);
        OnSPChanged?.Invoke(availableSP + cost, availableSP);

        return true;
    }

    /// <summary>
    /// Gets a learned skill instance by ID.
    /// </summary>
    public SkillInstance GetLearnedSkill(string skillId)
    {
        learnedSkills.TryGetValue(skillId, out var instance);
        return instance;
    }

    /// <summary>
    /// Gets the level of a learned skill (0 if not learned).
    /// </summary>
    public int GetSkillLevel(string skillId)
    {
        if (learnedSkills.TryGetValue(skillId, out var instance))
            return instance.currentLevel;
        return 0;
    }

    /// <summary>
    /// Gets the level of a learned skill (0 if not learned).
    /// </summary>
    public int GetSkillLevel(SkillData skill)
    {
        if (skill == null) return 0;
        return GetSkillLevel(skill.skillId);
    }

    /// <summary>
    /// Gets all learned skills.
    /// </summary>
    public SkillInstance[] GetAllLearnedSkills()
    {
        var skills = new SkillInstance[learnedSkills.Count];
        learnedSkills.Values.CopyTo(skills, 0);
        return skills;
    }

    /// <summary>
    /// Gets all learned skills of a specific type.
    /// </summary>
    public List<SkillInstance> GetSkillsByType(SkillType type)
    {
        var result = new List<SkillInstance>();
        foreach (var instance in learnedSkills.Values)
        {
            if (instance.SkillType == type)
                result.Add(instance);
        }
        return result;
    }

    /// <summary>
    /// Checks if a skill is learned.
    /// </summary>
    public bool HasSkill(string skillId)
    {
        return learnedSkills.ContainsKey(skillId);
    }

    /// <summary>
    /// Checks if player can advance to a specific job.
    /// </summary>
    public bool CanAdvanceJob(JobClassData targetJob)
    {
        if (targetJob == null) return false;

        // Must be a valid next job from current
        if (currentJob != null && currentJob.childJobs != null)
        {
            bool isValidAdvancement = false;
            foreach (var child in currentJob.childJobs)
            {
                if (child == targetJob)
                {
                    isValidAdvancement = true;
                    break;
                }
            }
            if (!isValidAdvancement) return false;
        }

        // Check requirements
        return targetJob.CanAdvance(playerLevel, GetSkillLevel);
    }

    /// <summary>
    /// Advances to a new job class.
    /// </summary>
    public bool AdvanceJob(JobClassData targetJob)
    {
        if (!CanAdvanceJob(targetJob))
            return false;

        var previousJob = currentJob;
        currentJob = targetJob;
        jobHistory.Add(targetJob);

        // Award bonus SP
        if (targetJob.bonusSPOnAdvancement > 0)
        {
            AddSP(targetJob.bonusSPOnAdvancement);
        }

        if (logEvents)
            Debug.Log($"[SkillManager] Job advancement: {previousJob?.jobName ?? "None"} -> {targetJob.jobName}");

        OnJobChanged?.Invoke(previousJob, targetJob);

        return true;
    }

    /// <summary>
    /// Gets available job advancements.
    /// </summary>
    public JobClassData[] GetAvailableAdvancements()
    {
        if (currentJob?.childJobs == null)
            return new JobClassData[0];

        var available = new List<JobClassData>();
        foreach (var child in currentJob.childJobs)
        {
            if (CanAdvanceJob(child))
                available.Add(child);
        }
        return available.ToArray();
    }

    /// <summary>
    /// Gets a skill data by ID.
    /// </summary>
    public SkillData GetSkillData(string skillId)
    {
        skillDataLookup.TryGetValue(skillId, out var data);
        return data;
    }

    /// <summary>
    /// Gets a job data by ID.
    /// </summary>
    public JobClassData GetJobData(string jobId)
    {
        jobDataLookup.TryGetValue(jobId, out var data);
        return data;
    }

    /// <summary>
    /// Creates save data for the skill system.
    /// </summary>
    public SkillSaveData CreateSaveData()
    {
        var saveData = new SkillSaveData
        {
            currentJobId = currentJob?.jobId ?? "",
            availableSP = availableSP,
            totalSPEarned = totalSPEarned,
            playerLevel = playerLevel,
            jobHistoryIds = new List<string>(),
            learnedSkills = new List<LearnedSkillData>()
        };

        foreach (var job in jobHistory)
        {
            saveData.jobHistoryIds.Add(job.jobId);
        }

        foreach (var instance in learnedSkills.Values)
        {
            saveData.learnedSkills.Add(instance.ToSaveData());
        }

        return saveData;
    }

    /// <summary>
    /// Applies save data to restore skill system state.
    /// </summary>
    public void ApplySaveData(SkillSaveData saveData)
    {
        if (saveData == null) return;

        // Restore job history
        jobHistory.Clear();
        if (saveData.jobHistoryIds != null)
        {
            foreach (var jobId in saveData.jobHistoryIds)
            {
                var job = GetJobData(jobId);
                if (job != null)
                    jobHistory.Add(job);
            }
        }

        // Restore current job
        currentJob = GetJobData(saveData.currentJobId);
        if (currentJob == null && defaultJob != null)
        {
            currentJob = defaultJob;
            if (!jobHistory.Contains(currentJob))
                jobHistory.Add(currentJob);
        }

        // Restore SP
        availableSP = saveData.availableSP;
        totalSPEarned = saveData.totalSPEarned;
        playerLevel = saveData.playerLevel;

        // Restore learned skills
        learnedSkills.Clear();
        if (saveData.learnedSkills != null)
        {
            foreach (var skillSave in saveData.learnedSkills)
            {
                var skillData = GetSkillData(skillSave.skillId);
                if (skillData != null)
                {
                    var instance = SkillInstance.FromSaveData(skillSave, skillData);
                    if (instance != null)
                        learnedSkills[skillSave.skillId] = instance;
                }
            }
        }

        if (logEvents)
            Debug.Log($"[SkillManager] Loaded save data: Level {playerLevel}, {learnedSkills.Count} skills, {availableSP} SP");
    }

    /// <summary>
    /// Resets all skill progression (for new game).
    /// </summary>
    public void ResetProgression()
    {
        learnedSkills.Clear();
        jobHistory.Clear();
        availableSP = 0;
        totalSPEarned = 0;
        playerLevel = 1;

        if (defaultJob != null)
        {
            currentJob = defaultJob;
            jobHistory.Add(defaultJob);
        }

        if (logEvents)
            Debug.Log("[SkillManager] Progression reset");
    }

#if UNITY_EDITOR
    [ContextMenu("Add 10 SP (Debug)")]
    private void DebugAddSP()
    {
        AddSP(10);
    }

    [ContextMenu("Level Up (Debug)")]
    private void DebugLevelUp()
    {
        SetPlayerLevel(playerLevel + 1);
    }
#endif
}

/// <summary>
/// Serializable save data for the skill system.
/// </summary>
[Serializable]
public class SkillSaveData
{
    public string currentJobId;
    public List<string> jobHistoryIds;
    public int availableSP;
    public int totalSPEarned;
    public int playerLevel;
    public List<LearnedSkillData> learnedSkills;
    public string[] hotbarSkillIds;
}
