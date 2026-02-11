using UnityEngine;
using UnityEditor;

/// <summary>
/// Debug utility to skip to a specific wave during Play mode.
/// Use Tools > Debug > Skip To Boss Wave (F5) to jump to wave 5.
/// </summary>
public static class DebugWaveSkip
{
    [MenuItem("Tools/Debug/Skip To Boss Wave _F5")]
    public static void SkipToBossWave()
    {
        SkipToWave(5);
    }

    [MenuItem("Tools/Debug/Skip To Wave 10 _F6")]
    public static void SkipToWave10()
    {
        SkipToWave(10);
    }

    [MenuItem("Tools/Debug/Skip To Boss Wave _F5", validate = true)]
    [MenuItem("Tools/Debug/Skip To Wave 10 _F6", validate = true)]
    public static bool ValidateSkip()
    {
        return Application.isPlaying;
    }

    private static void SkipToWave(int targetWave)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[DebugWaveSkip] Only works in Play mode.");
            return;
        }

        WaveManager waveManager = Object.FindAnyObjectByType<WaveManager>();
        if (waveManager == null)
        {
            Debug.LogError("[DebugWaveSkip] No WaveManager found in scene.");
            return;
        }

        EnemySpawnManager spawnManager = Object.FindAnyObjectByType<EnemySpawnManager>();
        if (spawnManager == null)
        {
            Debug.LogError("[DebugWaveSkip] No EnemySpawnManager found in scene.");
            return;
        }

        int previousWave = waveManager.CurrentWave;

        // Stop current wave loop and destroy all enemies
        waveManager.StopWaves();
        spawnManager.DestroyAllEnemies();

        // Restart from the target wave
        waveManager.StartWaves(targetWave);

        Debug.Log($"[DebugWaveSkip] Skipped from wave {previousWave} to wave {targetWave}");
    }
}
