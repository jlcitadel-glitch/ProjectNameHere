using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages game save/load functionality using PlayerPrefs with JSON serialization.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_KEY_PREFIX = "GameSave_Slot_";
    private const string LEGACY_SAVE_KEY = "GameSave";
    private const int CURRENT_SAVE_VERSION = 6;
    private const int MAX_SAVE_SLOTS = 5;

    [Header("Settings")]
    [SerializeField] private int activeSlotIndex = 0;

    [Header("Character System")]
    [Tooltip("Registry for resolving body part IDs during save/load")]
    [SerializeField] private BodyPartRegistry bodyPartRegistry;

    private BodyPartRegistry GetBodyPartRegistry()
    {
        if (bodyPartRegistry == null)
            bodyPartRegistry = Resources.Load<BodyPartRegistry>("BodyPartRegistry");
        return bodyPartRegistry;
    }

    public int ActiveSlotIndex => activeSlotIndex;

    public event Action OnSaveCompleted;
    public event Action OnLoadCompleted;
    public event Action OnSaveDeleted;
    public event Action<int> OnSlotChanged;

    [Serializable]
    public class SaveData
    {
        public int saveVersion = CURRENT_SAVE_VERSION;

        // Player state
        public float playerPositionX;
        public float playerPositionY;
        public int currentHealth;
        public int maxHealth;

        // Character info
        public string characterName = "Hero";

        // Abilities (stored as strings for flexibility)
        public List<string> unlockedAbilities = new List<string>();

        // Collectibles
        public List<string> collectedItems = new List<string>();

        // Checkpoint
        public string lastCheckpointId = "";

        // Wave/Progress state
        public int currentWave;
        public int maxWaveReached;

        // Play time (in seconds)
        public float playTime;

        // Skill System
        public SkillSaveData skillData;

        // Character creation
        public string startingClass = "";
        public int appearanceIndex;

        // Stat system
        public StatSaveData statData;

        // Death tracking
        public int deathCount;

        // Wave 100 milestone
        public bool hasSeenWave100Cutscene;

        // Character appearance (v6)
        public CharacterAppearanceSaveData appearanceData;

        // Equipment (v7)
        public string[] equippedItemIds;

        // Metadata
        public string saveTimestamp;

        public SaveData()
        {
            saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    private SaveData currentSaveData;
    private float sessionStartTime;

    public SaveData CurrentSave => currentSaveData;
    public bool HasSaveData => currentSaveData != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Expected on scene reload — DontDestroyOnLoad instance already exists
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sessionStartTime = Time.realtimeSinceStartup;
        MigrateLegacySave();
    }

    /// <summary>
    /// Migrates old single-slot save to new multi-slot system (slot 0).
    /// </summary>
    private void MigrateLegacySave()
    {
        // Check if legacy save exists and new slot 0 doesn't
        if (PlayerPrefs.HasKey(LEGACY_SAVE_KEY) && !PlayerPrefs.HasKey(GetSlotKey(0)))
        {
            string legacyJson = PlayerPrefs.GetString(LEGACY_SAVE_KEY);
            PlayerPrefs.SetString(GetSlotKey(0), legacyJson);
            PlayerPrefs.DeleteKey(LEGACY_SAVE_KEY);
            PlayerPrefs.Save();
            Debug.Log("[SaveManager] Migrated legacy save to slot 0");
        }
    }

    /// <summary>
    /// Gets the PlayerPrefs key for a specific slot.
    /// </summary>
    private string GetSlotKey(int slotIndex)
    {
        return SAVE_KEY_PREFIX + slotIndex;
    }

    private GameObject FindPlayer() => GameObject.FindGameObjectWithTag("Player");

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Creates a new SaveData object populated with current game state.
    /// </summary>
    public SaveData CreateSaveData()
    {
        var data = new SaveData();

        var player = FindPlayer();
        if (player != null)
        {
            data.playerPositionX = player.transform.position.x;
            data.playerPositionY = player.transform.position.y;

            var powerUpManager = player.GetComponent<PowerUpManager>();
            if (powerUpManager != null)
            {
                var abilities = powerUpManager.GetAllUnlockedPowerUps();
                foreach (var ability in abilities)
                {
                    data.unlockedAbilities.Add(ability.ToString());
                }
            }

            // Save current health
            var healthSystem = player.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                data.currentHealth = Mathf.RoundToInt(healthSystem.CurrentHealth);
                data.maxHealth = Mathf.RoundToInt(healthSystem.MaxHealth);
            }

        }

        // Skill system save
        if (SkillManager.Instance != null)
        {
            data.skillData = SkillManager.Instance.CreateSaveData();
        }

        // Skill hotbar (must be after skillData is created)
        if (player != null && data.skillData != null)
        {
            var skillController = player.GetComponent<PlayerSkillController>();
            if (skillController != null)
            {
                data.skillData.hotbarSkillIds = skillController.SaveHotbar();
            }
        }

        // Stat system save
        if (player != null)
        {
            var statSystem = player.GetComponent<StatSystem>();
            if (statSystem != null)
            {
                data.statData = statSystem.CreateSaveData();
            }
        }

        // Carry over character creation data from current session
        if (currentSaveData != null)
        {
            data.characterName = currentSaveData.characterName;
            data.startingClass = currentSaveData.startingClass;
            data.appearanceIndex = currentSaveData.appearanceIndex;
            data.appearanceData = currentSaveData.appearanceData;
        }

        // Capture current appearance from player (overrides carried-over data if layered system is active)
        if (player != null)
        {
            var playerAppearance = player.GetComponent<PlayerAppearance>();
            if (playerAppearance != null && playerAppearance.CurrentConfig != null)
            {
                data.appearanceData = CharacterAppearanceSaveData.FromConfig(playerAppearance.CurrentConfig);
            }
        }

        // Equipment save
        if (player != null)
        {
            var equipmentManager = player.GetComponent<EquipmentManager>();
            if (equipmentManager != null)
                data.equippedItemIds = equipmentManager.GetEquippedIds();
        }

        // Carry over death count
        data.deathCount = currentSaveData?.deathCount ?? 0;

        // Wave progress
        var waveManager = UnityEngine.Object.FindAnyObjectByType<WaveManager>();
        if (waveManager != null)
        {
            data.currentWave = waveManager.CurrentWave;
            data.maxWaveReached = Mathf.Max(
                waveManager.CurrentWave,
                currentSaveData?.maxWaveReached ?? 0
            );
        }
        else if (currentSaveData != null)
        {
            data.currentWave = currentSaveData.currentWave;
            data.maxWaveReached = currentSaveData.maxWaveReached;
        }

        // Calculate play time
        if (currentSaveData != null)
        {
            data.playTime = currentSaveData.playTime + (Time.realtimeSinceStartup - sessionStartTime);
        }
        else
        {
            data.playTime = Time.realtimeSinceStartup - sessionStartTime;
        }

        data.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        return data;
    }

    /// <summary>
    /// Sets the active save slot for subsequent save/load operations.
    /// </summary>
    public void SetActiveSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SAVE_SLOTS)
        {
            Debug.LogWarning($"[SaveManager] Invalid slot index: {slotIndex}. Must be 0-{MAX_SAVE_SLOTS - 1}");
            return;
        }

        if (activeSlotIndex != slotIndex)
        {
            activeSlotIndex = slotIndex;
            OnSlotChanged?.Invoke(activeSlotIndex);
            Debug.Log($"[SaveManager] Active slot changed to: {slotIndex}");
        }
    }

    /// <summary>
    /// Checks if a specific slot has save data.
    /// </summary>
    public bool HasSaveInSlot(int slotIndex)
    {
        return PlayerPrefs.HasKey(GetSlotKey(slotIndex));
    }

    /// <summary>
    /// Gets metadata for a specific save slot without loading full data.
    /// </summary>
    public SaveSlotInfo GetSlotInfo(int slotIndex)
    {
        var info = new SaveSlotInfo { slotIndex = slotIndex };

        string key = GetSlotKey(slotIndex);
        if (!PlayerPrefs.HasKey(key))
        {
            info.isEmpty = true;
            return info;
        }

        try
        {
            string json = PlayerPrefs.GetString(key);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            info.isEmpty = false;
            info.characterName = !string.IsNullOrEmpty(data.characterName) ? data.characterName : "Hero";
            info.playerLevel = data.skillData?.playerLevel ?? 1;
            info.playTimeSeconds = data.playTime;
            info.lastSavedTimestamp = data.saveTimestamp;
            info.checkpointName = data.lastCheckpointId ?? "";
            info.currentWave = data.currentWave;
            info.maxWaveReached = data.maxWaveReached;
            info.startingClass = data.startingClass ?? "";
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Failed to parse slot {slotIndex}: {e.Message}");
            info.isEmpty = true;
        }

        return info;
    }

    /// <summary>
    /// Gets metadata for all save slots.
    /// </summary>
    public SaveSlotInfo[] GetAllSlotInfo()
    {
        var slots = new SaveSlotInfo[MAX_SAVE_SLOTS];
        for (int i = 0; i < MAX_SAVE_SLOTS; i++)
        {
            slots[i] = GetSlotInfo(i);
        }
        return slots;
    }

    /// <summary>
    /// Creates a new game in the specified slot.
    /// </summary>
    public void CreateNewGame(int slotIndex)
    {
        CreateNewGame(slotIndex, "Hero", "", 0);
    }

    /// <summary>
    /// Creates a new game in the specified slot with character creation data.
    /// </summary>
    public void CreateNewGame(int slotIndex, string characterName, string startingClass, int appearanceIndex)
    {
        CreateNewGame(slotIndex, characterName, startingClass, appearanceIndex, null);
    }

    /// <summary>
    /// Creates a new game with character creation data including custom appearance.
    /// </summary>
    public void CreateNewGame(int slotIndex, string characterName, string startingClass, int appearanceIndex, CharacterAppearanceSaveData appearance)
    {
        SetActiveSlot(slotIndex);

        // Clear any existing save data for this slot
        string key = GetSlotKey(slotIndex);
        if (PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.DeleteKey(key);
        }

        // Initialize save data with character creation info
        currentSaveData = new SaveData();
        currentSaveData.characterName = !string.IsNullOrEmpty(characterName) ? characterName : "Hero";
        currentSaveData.startingClass = startingClass ?? "";
        currentSaveData.appearanceIndex = appearanceIndex;
        currentSaveData.appearanceData = appearance;
        currentSaveData.currentWave = 1;
        sessionStartTime = Time.realtimeSinceStartup;

        // Persist immediately so save slot shows character info
        string json = JsonUtility.ToJson(currentSaveData, true);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();

        Debug.Log($"[SaveManager] New game created in slot {slotIndex} - {currentSaveData.characterName} ({currentSaveData.startingClass})");
    }

    /// <summary>
    /// Deletes save data from a specific slot.
    /// </summary>
    public void DeleteSlot(int slotIndex)
    {
        string key = GetSlotKey(slotIndex);
        if (PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Debug.Log($"[SaveManager] Deleted save in slot {slotIndex}");
            OnSaveDeleted?.Invoke();
        }
    }

    /// <summary>
    /// Saves the current game state to the active slot.
    /// </summary>
    public void Save()
    {
        currentSaveData = CreateSaveData();

        string json = JsonUtility.ToJson(currentSaveData, true);
        PlayerPrefs.SetString(GetSlotKey(activeSlotIndex), json);
        PlayerPrefs.Save();

        // Record highscore on every save
        if (HighscoreManager.Instance != null && currentSaveData.maxWaveReached > 0)
        {
            HighscoreManager.Instance.RecordScore(
                currentSaveData.characterName,
                currentSaveData.startingClass,
                currentSaveData.maxWaveReached,
                currentSaveData.playTime
            );
        }

        Debug.Log($"[SaveManager] Game saved to slot {activeSlotIndex}. Play time: {FormatPlayTime(currentSaveData.playTime)}");
        OnSaveCompleted?.Invoke();
    }

    /// <summary>
    /// Saves to a specific checkpoint in the active slot.
    /// </summary>
    public void SaveAtCheckpoint(string checkpointId)
    {
        currentSaveData = CreateSaveData();
        currentSaveData.lastCheckpointId = checkpointId;

        string json = JsonUtility.ToJson(currentSaveData, true);
        PlayerPrefs.SetString(GetSlotKey(activeSlotIndex), json);
        PlayerPrefs.Save();

        Debug.Log($"[SaveManager] Saved at checkpoint: {checkpointId} in slot {activeSlotIndex}");
        OnSaveCompleted?.Invoke();
    }

    /// <summary>
    /// Checks if the active slot has save data.
    /// </summary>
    public bool HasSave()
    {
        return HasSaveInSlot(activeSlotIndex);
    }

    /// <summary>
    /// Loads save data from the active slot.
    /// </summary>
    public bool Load()
    {
        if (!HasSave())
        {
            Debug.Log($"[SaveManager] No save data found in slot {activeSlotIndex}.");
            return false;
        }

        string json = PlayerPrefs.GetString(GetSlotKey(activeSlotIndex));
        currentSaveData = JsonUtility.FromJson<SaveData>(json);

        if (currentSaveData.saveVersion < CURRENT_SAVE_VERSION)
        {
            MigrateSaveData(currentSaveData);
        }

        sessionStartTime = Time.realtimeSinceStartup;

        Debug.Log($"[SaveManager] Save loaded from slot {activeSlotIndex}. Play time: {FormatPlayTime(currentSaveData.playTime)}");
        OnLoadCompleted?.Invoke();
        return true;
    }

    /// <summary>
    /// Applies loaded save data to the game world.
    /// Call this after scene is loaded and player exists.
    /// </summary>
    public void ApplyLoadedData()
    {
        if (currentSaveData == null)
        {
            Debug.LogWarning("[SaveManager] No save data to apply.");
            return;
        }

        var player = FindPlayer();
        if (player == null)
        {
            Debug.LogWarning("[SaveManager] Player not found. Cannot apply save data.");
            return;
        }

        // Apply position
        player.transform.position = new Vector3(
            currentSaveData.playerPositionX,
            currentSaveData.playerPositionY,
            player.transform.position.z
        );

        // Apply abilities
        var powerUpManager = player.GetComponent<PowerUpManager>();
        if (powerUpManager == null)
        {
            powerUpManager = player.AddComponent<PowerUpManager>();
        }

        var playerController = player.GetComponent<PlayerControllerScript>();

        foreach (string abilityName in currentSaveData.unlockedAbilities)
        {
            if (Enum.TryParse<PowerUpType>(abilityName, out PowerUpType powerUpType))
            {
                powerUpManager.UnlockPowerUp(powerUpType);

                switch (powerUpType)
                {
                    case PowerUpType.DoubleJump:
                        if (player.GetComponent<DoubleJumpAbility>() == null)
                        {
                            player.AddComponent<DoubleJumpAbility>();
                        }
                        break;
                    case PowerUpType.Dash:
                        if (player.GetComponent<DashAbility>() == null)
                        {
                            player.AddComponent<DashAbility>();
                        }
                        break;
                }
            }
        }

        if (playerController != null)
        {
            playerController.RefreshAbilities();
        }

        // Restore health — refill after stat scaling has set the correct maxHealth
        var healthSystem = player.GetComponent<HealthSystem>();
        if (healthSystem != null)
            healthSystem.RefillHealth();

        // Apply skill system data
        if (SkillManager.Instance != null && currentSaveData.skillData != null)
        {
            SkillManager.Instance.ApplySaveData(currentSaveData.skillData);
        }
        // Set initial job from character creation (for new games without skill save data)
        else if (SkillManager.Instance != null && !string.IsNullOrEmpty(currentSaveData.startingClass))
        {
            SkillManager.Instance.SetStartingJob(currentSaveData.startingClass.ToLower());
        }

        // Apply skill hotbar
        var skillController = player.GetComponent<PlayerSkillController>();
        if (skillController != null && currentSaveData.skillData?.hotbarSkillIds != null)
        {
            skillController.LoadHotbar(currentSaveData.skillData.hotbarSkillIds);
        }

        // Apply stat system data
        var statSystem = player.GetComponent<StatSystem>();
        if (statSystem != null && currentSaveData.statData != null)
        {
            statSystem.ApplySaveData(currentSaveData.statData);
        }

        // Apply player appearance — saved custom appearance takes priority over job default
        var playerAppearance = player.GetComponent<PlayerAppearance>();
        if (playerAppearance != null)
        {
            if (currentSaveData.appearanceData != null && currentSaveData.appearanceData.HasAnyParts() && GetBodyPartRegistry() != null)
            {
                var config = currentSaveData.appearanceData.ToConfig(GetBodyPartRegistry());
                if (config != null)
                    playerAppearance.ApplyConfig(config);
            }
            else if (SkillManager.Instance?.CurrentJob != null)
            {
                playerAppearance.ApplyAppearance(SkillManager.Instance.CurrentJob);
            }
        }

        // Apply equipment
        var equipmentManager = player.GetComponent<EquipmentManager>();
        if (equipmentManager == null)
            equipmentManager = player.AddComponent<EquipmentManager>();
        if (currentSaveData.equippedItemIds != null && currentSaveData.equippedItemIds.Length > 0)
            equipmentManager.LoadFromIds(currentSaveData.equippedItemIds);

        Debug.Log("[SaveManager] Save data applied to game world.");
    }

    /// <summary>
    /// Applies only starting class and appearance for new games.
    /// Does NOT modify player position or abilities (preserves scene spawn point).
    /// </summary>
    public void ApplyNewGameData()
    {
        if (currentSaveData == null)
        {
            Debug.LogWarning("[SaveManager] No save data for new game.");
            return;
        }

        var player = FindPlayer();
        if (player == null)
        {
            Debug.LogWarning("[SaveManager] Player not found. Cannot apply new game data.");
            return;
        }

        // Set starting job from character creation
        if (SkillManager.Instance != null && !string.IsNullOrEmpty(currentSaveData.startingClass))
        {
            SkillManager.Instance.SetStartingJob(currentSaveData.startingClass.ToLower());
        }

        // Apply stat system defaults
        var statSystem = player.GetComponent<StatSystem>();
        if (statSystem != null && currentSaveData.statData != null)
        {
            statSystem.ApplySaveData(currentSaveData.statData);
        }

        // Apply player appearance — saved custom appearance takes priority over job default
        var playerAppearance = player.GetComponent<PlayerAppearance>();
        if (playerAppearance != null)
        {
            if (currentSaveData.appearanceData != null && currentSaveData.appearanceData.HasAnyParts() && GetBodyPartRegistry() != null)
            {
                var config = currentSaveData.appearanceData.ToConfig(GetBodyPartRegistry());
                if (config != null)
                    playerAppearance.ApplyConfig(config);
            }
            else if (SkillManager.Instance?.CurrentJob != null)
            {
                playerAppearance.ApplyAppearance(SkillManager.Instance.CurrentJob);
            }
        }

        // Equip starter gear for the chosen class
        var equipmentManager = player.GetComponent<EquipmentManager>();
        if (equipmentManager == null)
            equipmentManager = player.AddComponent<EquipmentManager>();
        if (SkillManager.Instance?.CurrentJob != null)
            equipmentManager.EquipStarterGear(SkillManager.Instance.CurrentJob);

        // New games always start at full health
        var healthSystem = player.GetComponent<HealthSystem>();
        if (healthSystem != null)
            healthSystem.RefillHealth();

        Debug.Log($"[SaveManager] New game data applied - Class: {currentSaveData.startingClass}, Name: {currentSaveData.characterName}");
    }

    /// <summary>
    /// Deletes save data from the active slot.
    /// </summary>
    public void DeleteSave()
    {
        if (HasSave())
        {
            PlayerPrefs.DeleteKey(GetSlotKey(activeSlotIndex));
            PlayerPrefs.Save();
            currentSaveData = null;
            sessionStartTime = Time.realtimeSinceStartup;
            Debug.Log($"[SaveManager] Save data deleted from slot {activeSlotIndex}.");
            OnSaveDeleted?.Invoke();
        }
    }

    private void MigrateSaveData(SaveData data)
    {
        // Migration from version 1 to 2: Add skill system data
        if (data.saveVersion < 2)
        {
            data.skillData = new SkillSaveData
            {
                currentJobId = "",
                jobHistoryIds = new List<string>(),
                availableSP = 0,
                totalSPEarned = 0,
                playerLevel = 1,
                learnedSkills = new List<LearnedSkillData>(),
                hotbarSkillIds = new string[0]
            };
        }

        // Migration from version 2 to 3: Add character creation and stat system data
        if (data.saveVersion < 3)
        {
            data.startingClass = "";
            data.appearanceIndex = 0;
            data.statData = null;
        }

        // Migration from version 3 to 4: Add death counter
        if (data.saveVersion < 4)
        {
            data.deathCount = 0;
        }

        // Migration from version 4 to 5: Add wave 100 milestone flag
        if (data.saveVersion < 5)
        {
            data.hasSeenWave100Cutscene = false;
        }

        // Migration from version 5 to 6: Add character appearance data
        if (data.saveVersion < 6)
        {
            data.appearanceData = null; // null = use job default appearance
        }

        data.saveVersion = CURRENT_SAVE_VERSION;
        Debug.Log($"[SaveManager] Migrated save data to version {CURRENT_SAVE_VERSION}");
    }

    /// <summary>
    /// Formats play time as a readable string.
    /// </summary>
    public static string FormatPlayTime(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);
        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours}h {time.Minutes}m";
        }
        return $"{time.Minutes}m {time.Seconds}s";
    }

    /// <summary>
    /// Gets the last checkpoint ID from current save.
    /// </summary>
    public string GetLastCheckpointId()
    {
        return currentSaveData?.lastCheckpointId ?? "";
    }

    /// <summary>
    /// Gets total play time including current session.
    /// </summary>
    public float GetTotalPlayTime()
    {
        float baseTime = currentSaveData?.playTime ?? 0f;
        return baseTime + (Time.realtimeSinceStartup - sessionStartTime);
    }

    /// <summary>
    /// Adds an item to the collected items list.
    /// </summary>
    public void AddCollectedItem(string itemId)
    {
        if (currentSaveData == null)
        {
            currentSaveData = new SaveData();
        }

        if (!currentSaveData.collectedItems.Contains(itemId))
        {
            currentSaveData.collectedItems.Add(itemId);
        }
    }

    /// <summary>
    /// Checks if an item has been collected.
    /// </summary>
    public bool HasCollectedItem(string itemId)
    {
        return currentSaveData?.collectedItems.Contains(itemId) ?? false;
    }

    /// <summary>
    /// Increments the death counter and persists immediately.
    /// </summary>
    public void RecordDeath()
    {
        if (currentSaveData == null)
        {
            currentSaveData = new SaveData();
        }

        currentSaveData.deathCount++;

        string json = JsonUtility.ToJson(currentSaveData, true);
        PlayerPrefs.SetString(GetSlotKey(activeSlotIndex), json);
        PlayerPrefs.Save();

        Debug.Log($"[SaveManager] Death recorded. Total deaths: {currentSaveData.deathCount}");
    }

    /// <summary>
    /// Gets the current death count.
    /// </summary>
    public int GetDeathCount()
    {
        return currentSaveData?.deathCount ?? 0;
    }
}
