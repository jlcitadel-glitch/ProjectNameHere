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
        EnsureRuntimeData();
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

    /// <summary>
    /// Creates runtime fallback data when serialized fields are null (AddComponent scenario).
    /// Only runs when allJobData and allSkillData are both null/empty.
    /// First tries to find existing ScriptableObject assets before creating duplicates.
    /// </summary>
    private void EnsureRuntimeData()
    {
        bool hasJobs = allJobData != null && allJobData.Length > 0;
        bool hasSkills = allSkillData != null && allSkillData.Length > 0;

        if (hasJobs || hasSkills)
            return;

        // Try to find existing assets before creating runtime fallbacks.
        // This handles the case where SystemsBootstrap creates SkillManager via
        // AddComponent (no serialized refs) but real assets exist in the project.
        var foundJobs = Resources.FindObjectsOfTypeAll<JobClassData>();
        var foundSkills = Resources.FindObjectsOfTypeAll<SkillData>();

        if (foundJobs.Length > 0 && foundSkills.Length > 0)
        {
            allJobData = foundJobs;
            allSkillData = foundSkills;
            BuildLookupCaches();

            if (defaultJob == null)
            {
                foreach (var job in allJobData)
                {
                    if (job.tier == JobTier.Beginner)
                    {
                        defaultJob = job;
                        break;
                    }
                }
            }

            if (logEvents)
                Debug.Log($"[SkillManager] Found existing assets: {allJobData.Length} jobs, {allSkillData.Length} skills.");
            return;
        }

        if (logEvents)
            Debug.Log("[SkillManager] No serialized data found, creating runtime fallback data.");

        // --- Create Skills (3 per class = 9 total) ---

        // Warrior skills
        var cleave = CreateSkillData("warrior_cleave", "Cleave", "Sweeping physical attack that hits nearby enemies for {damage} damage.",
            SkillType.Active, DamageType.Physical, "warrior", tier: 0, position: new Vector2(0, 0),
            baseDamage: 25f, baseManaCost: 8f, baseCooldown: 3f);

        var ironWill = CreateSkillData("warrior_iron_will", "Iron Will", "Hardens your resolve, reducing damage taken for {duration}s.",
            SkillType.Buff, DamageType.Physical, "warrior", tier: 1, position: new Vector2(-1, 1),
            baseDamage: 0f, baseManaCost: 15f, baseCooldown: 12f, baseDuration: 8f, durationPerLevel: 1f,
            prereqs: new SkillData[] { cleave }, prereqLevels: new int[] { 1 });

        var warCry = CreateSkillData("warrior_war_cry", "War Cry", "Unleash a battle cry that boosts attack power for {duration}s.",
            SkillType.Buff, DamageType.Physical, "warrior", tier: 1, position: new Vector2(1, 1),
            baseDamage: 0f, baseManaCost: 20f, baseCooldown: 15f, baseDuration: 10f, durationPerLevel: 1f,
            prereqs: new SkillData[] { cleave }, prereqLevels: new int[] { 3 });

        // Mage skills
        var fireball = CreateSkillData("mage_fireball", "Fireball", "Hurls a ball of fire that deals {damage} fire damage.",
            SkillType.Active, DamageType.Fire, "mage", tier: 0, position: new Vector2(0, 0),
            baseDamage: 30f, baseManaCost: 12f, baseCooldown: 4f);

        var frostNova = CreateSkillData("mage_frost_nova", "Frost Nova", "Sends out a wave of frost dealing {damage} ice damage to nearby enemies.",
            SkillType.Active, DamageType.Ice, "mage", tier: 1, position: new Vector2(-1, 1),
            baseDamage: 20f, baseManaCost: 18f, baseCooldown: 8f,
            prereqs: new SkillData[] { fireball }, prereqLevels: new int[] { 1 });

        var arcaneShield = CreateSkillData("mage_arcane_shield", "Arcane Shield", "Conjures a magic barrier that absorbs damage for {duration}s.",
            SkillType.Buff, DamageType.Magic, "mage", tier: 1, position: new Vector2(1, 1),
            baseDamage: 0f, baseManaCost: 25f, baseCooldown: 18f, baseDuration: 6f, durationPerLevel: 0.5f,
            prereqs: new SkillData[] { fireball }, prereqLevels: new int[] { 3 });

        // Rogue skills
        var quickStrike = CreateSkillData("rogue_quick_strike", "Quick Strike", "A lightning-fast melee attack dealing {damage} damage.",
            SkillType.Active, DamageType.Physical, "rogue", tier: 0, position: new Vector2(0, 0),
            baseDamage: 18f, baseManaCost: 5f, baseCooldown: 1.5f);

        var shadowStep = CreateSkillData("rogue_shadow_step", "Shadow Step", "Vanish into shadow, dodging all attacks for {duration}s.",
            SkillType.Buff, DamageType.Dark, "rogue", tier: 1, position: new Vector2(-1, 1),
            baseDamage: 0f, baseManaCost: 15f, baseCooldown: 10f, baseDuration: 2f, durationPerLevel: 0.3f,
            prereqs: new SkillData[] { quickStrike }, prereqLevels: new int[] { 1 });

        var poisonBlade = CreateSkillData("rogue_poison_blade", "Poison Blade", "Coats your weapon in poison, dealing {damage} poison damage over time.",
            SkillType.Buff, DamageType.Poison, "rogue", tier: 1, position: new Vector2(1, 1),
            baseDamage: 12f, baseManaCost: 10f, baseCooldown: 8f, baseDuration: 5f, durationPerLevel: 0.5f,
            prereqs: new SkillData[] { quickStrike }, prereqLevels: new int[] { 3 });

        // --- Create Skill Trees ---
        var warriorTree = CreateSkillTree("warrior_tree", "Warrior Skills", cleave, ironWill, warCry);
        var mageTree = CreateSkillTree("mage_tree", "Mage Skills", fireball, frostNova, arcaneShield);
        var rogueTree = CreateSkillTree("rogue_tree", "Rogue Skills", quickStrike, shadowStep, poisonBlade);

        // --- Create Jobs ---
        var warrior = CreateJobData("warrior", "Warrior", "A stalwart fighter wielding brute strength.",
            JobTier.First, new Color(0.55f, 0f, 0f), // deep crimson
            strPerLevel: 3, intPerLevel: 1, agiPerLevel: 1, spPerLevel: 3,
            warriorTree, new SkillData[] { cleave, ironWill, warCry });

        var mage = CreateJobData("mage", "Mage", "A scholar of the arcane arts.",
            JobTier.First, new Color(0.1f, 0.1f, 0.44f), // midnight blue
            strPerLevel: 1, intPerLevel: 3, agiPerLevel: 1, spPerLevel: 3,
            mageTree, new SkillData[] { fireball, frostNova, arcaneShield });

        var rogue = CreateJobData("rogue", "Rogue", "A swift shadow striking from the dark.",
            JobTier.First, new Color(0f, 0.3f, 0f), // dark green
            strPerLevel: 1, intPerLevel: 1, agiPerLevel: 3, spPerLevel: 3,
            rogueTree, new SkillData[] { quickStrike, shadowStep, poisonBlade });

        var beginner = CreateJobData("beginner", "Beginner", "A novice adventurer ready to choose a path.",
            JobTier.Beginner, Color.white,
            strPerLevel: 1, intPerLevel: 1, agiPerLevel: 1, spPerLevel: 3,
            null, new SkillData[0]);
        beginner.childJobs = new JobClassData[] { warrior, mage, rogue };
        warrior.parentJob = beginner;
        mage.parentJob = beginner;
        rogue.parentJob = beginner;

        // --- Assign to serialized fields ---
        allSkillData = new SkillData[] { cleave, ironWill, warCry, fireball, frostNova, arcaneShield, quickStrike, shadowStep, poisonBlade };
        allJobData = new JobClassData[] { beginner, warrior, mage, rogue };
        defaultJob = beginner;

        // Rebuild caches with new data
        BuildLookupCaches();

        if (logEvents)
            Debug.Log($"[SkillManager] Runtime data created: {allJobData.Length} jobs, {allSkillData.Length} skills.");
    }

    private SkillData CreateSkillData(string id, string skillName, string description,
        SkillType skillType, DamageType damageType, string requiredJobId,
        int tier, Vector2 position, float baseDamage, float baseManaCost, float baseCooldown,
        float baseDuration = 0f, float durationPerLevel = 0f,
        SkillData[] prereqs = null, int[] prereqLevels = null)
    {
        var skill = ScriptableObject.CreateInstance<SkillData>();
        skill.skillId = id;
        skill.skillName = skillName;
        skill.description = description;
        skill.skillType = skillType;
        skill.damageType = damageType;
        skill.requiredJobId = requiredJobId;
        skill.requiredPlayerLevel = 1;
        skill.maxSkillLevel = 10;
        skill.spCost = 1;
        skill.baseDamage = baseDamage;
        skill.baseManaCost = baseManaCost;
        skill.baseCooldown = baseCooldown;
        skill.baseDuration = baseDuration;
        skill.damagePerLevel = baseDamage > 0 ? 5f : 0f;
        skill.manaCostPerLevel = 2f;
        skill.cooldownReductionPerLevel = 0.1f;
        skill.durationPerLevel = durationPerLevel;
        skill.tier = tier;
        skill.nodePosition = position;
        skill.prerequisiteSkills = prereqs;
        skill.prerequisiteLevels = prereqLevels;
        skill.name = skillName;
        return skill;
    }

    private SkillTreeData CreateSkillTree(string id, string treeName,
        SkillData rootSkill, SkillData leftBranch, SkillData rightBranch)
    {
        var tree = ScriptableObject.CreateInstance<SkillTreeData>();
        tree.treeId = id;
        tree.treeName = treeName;
        tree.name = treeName;

        // V-shape layout: root at top center, two branches below
        tree.nodes = new SkillTreeData.SkillNode[]
        {
            new SkillTreeData.SkillNode
            {
                skill = rootSkill,
                position = new Vector2(0, 0),
                row = 0,
                column = 1,
                childNodeIndices = new int[] { 1, 2 }
            },
            new SkillTreeData.SkillNode
            {
                skill = leftBranch,
                position = new Vector2(-1, 1),
                row = 1,
                column = 0,
                childNodeIndices = new int[0]
            },
            new SkillTreeData.SkillNode
            {
                skill = rightBranch,
                position = new Vector2(1, 1),
                row = 1,
                column = 2,
                childNodeIndices = new int[0]
            }
        };

        // Connections: root -> left, root -> right
        tree.connections = new SkillTreeData.NodeConnection[]
        {
            new SkillTreeData.NodeConnection { fromNodeIndex = 0, toNodeIndex = 1 },
            new SkillTreeData.NodeConnection { fromNodeIndex = 0, toNodeIndex = 2 }
        };

        return tree;
    }

    private JobClassData CreateJobData(string id, string jobName, string description,
        JobTier tier, Color color, int strPerLevel, int intPerLevel, int agiPerLevel,
        int spPerLevel, SkillTreeData skillTree, SkillData[] availableSkills)
    {
        var job = ScriptableObject.CreateInstance<JobClassData>();
        job.jobId = id;
        job.jobName = jobName;
        job.description = description;
        job.tier = tier;
        job.jobColor = color;
        job.strPerLevel = strPerLevel;
        job.intPerLevel = intPerLevel;
        job.agiPerLevel = agiPerLevel;
        job.spPerLevel = spPerLevel;
        job.skillTree = skillTree;
        job.availableSkills = availableSkills;
        job.requiredLevel = tier == JobTier.Beginner ? 1 : 10;
        job.bonusSPOnAdvancement = tier == JobTier.Beginner ? 0 : 5;
        job.attackModifier = 1f;
        job.magicModifier = 1f;
        job.defenseModifier = 1f;
        job.characterAnimator = null;
        job.idlePreviewFrames = null;
        job.defaultSprite = null;
        job.name = jobName;
        return job;
    }

    /// <summary>
    /// Sets the starting job directly (bypasses advancement checks).
    /// Used for character creation class selection.
    /// </summary>
    public void SetStartingJob(string jobId)
    {
        if (string.IsNullOrEmpty(jobId))
            return;

        var job = GetJobData(jobId);
        if (job == null)
        {
            if (logEvents)
                Debug.LogWarning($"[SkillManager] SetStartingJob: job '{jobId}' not found.");
            return;
        }

        var previousJob = currentJob;
        currentJob = job;

        if (!jobHistory.Contains(job))
            jobHistory.Add(job);

        if (logEvents)
            Debug.Log($"[SkillManager] Starting job set: {previousJob?.jobName ?? "None"} -> {job.jobName}");

        OnJobChanged?.Invoke(previousJob, job);
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
