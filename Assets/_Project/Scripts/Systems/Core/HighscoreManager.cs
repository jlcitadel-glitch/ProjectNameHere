using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages local highscore persistence via PlayerPrefs, separate from save slots.
/// Stores up to 10 entries sorted by wave descending, then play time ascending.
/// </summary>
public class HighscoreManager : MonoBehaviour
{
    public static HighscoreManager Instance { get; private set; }

    private const string HIGHSCORES_KEY = "Highscores";
    private const int MAX_ENTRIES = 10;

    private HighscoreData data;

    public event Action OnScoresChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadScores();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Records a new score. Adds to the list, sorts, trims to top 10, and persists.
    /// Duplicate entries are harmless — only the best 10 survive.
    /// </summary>
    public void RecordScore(string characterName, string startingClass, int maxWave, float playTimeSeconds)
    {
        if (maxWave <= 0)
            return;

        var entry = new HighscoreEntry
        {
            characterName = !string.IsNullOrEmpty(characterName) ? characterName : "Hero",
            startingClass = startingClass ?? "",
            maxWaveReached = maxWave,
            playTimeSeconds = playTimeSeconds,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        data.entries.Add(entry);
        SortAndTrim();
        SaveScores();

        Debug.Log($"[HighscoreManager] Recorded: {entry.characterName} ({entry.startingClass}) - Wave {entry.maxWaveReached}");
        OnScoresChanged?.Invoke();
    }

    /// <summary>
    /// Returns the top scores list (up to 10 entries).
    /// </summary>
    public List<HighscoreEntry> GetTopScores()
    {
        return data.entries;
    }

    /// <summary>
    /// Clears all highscore data. Intended for debug use.
    /// </summary>
    public void ClearAllScores()
    {
        data.entries.Clear();
        SaveScores();
        Debug.Log("[HighscoreManager] All scores cleared");
        OnScoresChanged?.Invoke();
    }

    private void SortAndTrim()
    {
        // Sort by wave descending, then by play time ascending (faster = better)
        data.entries.Sort((a, b) =>
        {
            int waveCompare = b.maxWaveReached.CompareTo(a.maxWaveReached);
            if (waveCompare != 0) return waveCompare;
            return a.playTimeSeconds.CompareTo(b.playTimeSeconds);
        });

        // Trim to max entries
        if (data.entries.Count > MAX_ENTRIES)
        {
            data.entries.RemoveRange(MAX_ENTRIES, data.entries.Count - MAX_ENTRIES);
        }
    }

    private void LoadScores()
    {
        if (PlayerPrefs.HasKey(HIGHSCORES_KEY))
        {
            try
            {
                string json = PlayerPrefs.GetString(HIGHSCORES_KEY);
                data = JsonUtility.FromJson<HighscoreData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HighscoreManager] Failed to load scores: {e.Message}");
                data = new HighscoreData();
            }
        }

        if (data == null)
        {
            data = new HighscoreData();
        }
    }

    private void SaveScores()
    {
        string json = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString(HIGHSCORES_KEY, json);
        PlayerPrefs.Save();
    }
}
