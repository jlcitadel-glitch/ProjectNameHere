using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages game save/load functionality using PlayerPrefs with JSON serialization.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_KEY = "GameSave";
    private const int CURRENT_SAVE_VERSION = 2;

    public event Action OnSaveCompleted;
    public event Action OnLoadCompleted;
    public event Action OnSaveDeleted;

    [Serializable]
    public class SaveData
    {
        public int saveVersion = CURRENT_SAVE_VERSION;

        // Player state
        public float playerPositionX;
        public float playerPositionY;
        public int currentHealth;
        public int maxHealth;

        // Abilities (stored as strings for flexibility)
        public List<string> unlockedAbilities = new List<string>();

        // Collectibles
        public List<string> collectedItems = new List<string>();

        // Checkpoint
        public string lastCheckpointId = "";

        // Play time (in seconds)
        public float playTime;

        // Skill System
        public SkillSaveData skillData;

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
            Debug.LogWarning($"[SaveManager] Duplicate instance on {gameObject.name}, destroying.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sessionStartTime = Time.realtimeSinceStartup;
    }

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

        var player = GameObject.FindGameObjectWithTag("Player");
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

            // Health integration (when PlayerHealth component exists)
            // var health = player.GetComponent<PlayerHealth>();
            // if (health != null)
            // {
            //     data.currentHealth = health.CurrentHealth;
            //     data.maxHealth = health.MaxHealth;
            // }

            // Skill hotbar
            var skillController = player.GetComponent<PlayerSkillController>();
            if (skillController != null && data.skillData != null)
            {
                data.skillData.hotbarSkillIds = skillController.SaveHotbar();
            }
        }

        // Skill system save
        if (SkillManager.Instance != null)
        {
            data.skillData = SkillManager.Instance.CreateSaveData();
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
    /// Saves the current game state.
    /// </summary>
    public void Save()
    {
        currentSaveData = CreateSaveData();

        string json = JsonUtility.ToJson(currentSaveData, true);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();

        Debug.Log($"[SaveManager] Game saved. Play time: {FormatPlayTime(currentSaveData.playTime)}");
        OnSaveCompleted?.Invoke();
    }

    /// <summary>
    /// Saves to a specific checkpoint.
    /// </summary>
    public void SaveAtCheckpoint(string checkpointId)
    {
        currentSaveData = CreateSaveData();
        currentSaveData.lastCheckpointId = checkpointId;

        string json = JsonUtility.ToJson(currentSaveData, true);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();

        Debug.Log($"[SaveManager] Saved at checkpoint: {checkpointId}");
        OnSaveCompleted?.Invoke();
    }

    /// <summary>
    /// Checks if a save file exists.
    /// </summary>
    public bool HasSave()
    {
        return PlayerPrefs.HasKey(SAVE_KEY);
    }

    /// <summary>
    /// Loads save data from storage.
    /// </summary>
    public bool Load()
    {
        if (!HasSave())
        {
            Debug.Log("[SaveManager] No save data found.");
            return false;
        }

        string json = PlayerPrefs.GetString(SAVE_KEY);
        currentSaveData = JsonUtility.FromJson<SaveData>(json);

        if (currentSaveData.saveVersion < CURRENT_SAVE_VERSION)
        {
            MigrateSaveData(currentSaveData);
        }

        sessionStartTime = Time.realtimeSinceStartup;

        Debug.Log($"[SaveManager] Save loaded. Play time: {FormatPlayTime(currentSaveData.playTime)}");
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

        var player = GameObject.FindGameObjectWithTag("Player");
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

        // Health integration (when PlayerHealth component exists)
        // var health = player.GetComponent<PlayerHealth>();
        // if (health != null)
        // {
        //     health.SetHealth(currentSaveData.currentHealth, currentSaveData.maxHealth);
        // }

        // Apply skill system data
        if (SkillManager.Instance != null && currentSaveData.skillData != null)
        {
            SkillManager.Instance.ApplySaveData(currentSaveData.skillData);
        }

        // Apply skill hotbar
        var skillController = player.GetComponent<PlayerSkillController>();
        if (skillController != null && currentSaveData.skillData?.hotbarSkillIds != null)
        {
            skillController.LoadHotbar(currentSaveData.skillData.hotbarSkillIds);
        }

        Debug.Log("[SaveManager] Save data applied to game world.");
    }

    /// <summary>
    /// Deletes the save file.
    /// </summary>
    public void DeleteSave()
    {
        if (HasSave())
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            currentSaveData = null;
            sessionStartTime = Time.realtimeSinceStartup;
            Debug.Log("[SaveManager] Save data deleted.");
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
}
