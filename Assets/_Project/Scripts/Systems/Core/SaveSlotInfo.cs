using System;

/// <summary>
/// Lightweight data class containing metadata about a save slot.
/// Used for displaying save slot information without loading full game state.
/// </summary>
[Serializable]
public class SaveSlotInfo
{
    public int slotIndex;
    public bool isEmpty;
    public string characterName;
    public int playerLevel;
    public float playTimeSeconds;
    public string lastSavedTimestamp;
    public string checkpointName;
    public int currentWave;
    public int maxWaveReached;

    public SaveSlotInfo()
    {
        slotIndex = 0;
        isEmpty = true;
        characterName = "Hero";
        playerLevel = 1;
        playTimeSeconds = 0f;
        lastSavedTimestamp = "";
        checkpointName = "";
        currentWave = 0;
        maxWaveReached = 0;
    }

    /// <summary>
    /// Gets a formatted string for play time (e.g., "2h 34m" or "45m 12s").
    /// </summary>
    public string FormattedPlayTime
    {
        get
        {
            TimeSpan time = TimeSpan.FromSeconds(playTimeSeconds);
            if (time.TotalHours >= 1)
            {
                return $"{(int)time.TotalHours}h {time.Minutes}m";
            }
            return $"{time.Minutes}m {time.Seconds}s";
        }
    }

    /// <summary>
    /// Gets a formatted string for the last saved date (e.g., "Jan 15, 2024").
    /// </summary>
    public string FormattedDate
    {
        get
        {
            if (string.IsNullOrEmpty(lastSavedTimestamp))
                return "";

            // Parse the timestamp (format: "yyyy-MM-dd HH:mm:ss")
            if (DateTime.TryParse(lastSavedTimestamp, out DateTime dt))
            {
                return dt.ToString("MMM dd, yyyy");
            }
            return lastSavedTimestamp;
        }
    }

    /// <summary>
    /// Gets a formatted string for the wave state (e.g., "Wave 5").
    /// </summary>
    public string FormattedWave
    {
        get
        {
            if (currentWave <= 0 && maxWaveReached <= 0)
                return "";

            if (maxWaveReached > 0)
                return $"Wave {maxWaveReached}";

            return $"Wave {currentWave}";
        }
    }
}
