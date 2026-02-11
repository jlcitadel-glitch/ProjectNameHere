using System;
using System.Collections.Generic;

/// <summary>
/// Represents a single highscore entry with character info and performance stats.
/// </summary>
[Serializable]
public class HighscoreEntry
{
    public string characterName;
    public string startingClass;
    public int maxWaveReached;
    public float playTimeSeconds;
    public string timestamp;

    public HighscoreEntry()
    {
        characterName = "Hero";
        startingClass = "";
        maxWaveReached = 0;
        playTimeSeconds = 0f;
        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
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
    /// Gets a formatted string for the date (e.g., "Jan 15, 2024").
    /// </summary>
    public string FormattedDate
    {
        get
        {
            if (string.IsNullOrEmpty(timestamp))
                return "";

            if (DateTime.TryParse(timestamp, out DateTime dt))
            {
                return dt.ToString("MMM dd, yyyy");
            }
            return timestamp;
        }
    }
}

/// <summary>
/// Wrapper class for JSON serialization of highscore entries via JsonUtility.
/// </summary>
[Serializable]
public class HighscoreData
{
    public List<HighscoreEntry> entries = new List<HighscoreEntry>();
}
