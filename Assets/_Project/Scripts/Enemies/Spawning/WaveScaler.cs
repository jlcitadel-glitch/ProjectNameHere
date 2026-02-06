using UnityEngine;

/// <summary>
/// Static utility for computing wave-scaled enemy stats.
/// Pure math — no MonoBehaviour dependency.
/// </summary>
public static class WaveScaler
{
    /// <summary>
    /// Scales a base stat by wave number using linear growth.
    /// Wave 1 returns baseStat unchanged.
    /// </summary>
    public static float ScaleStat(float baseStat, int wave, float scalePerWave)
    {
        return baseStat * (1f + (wave - 1) * scalePerWave);
    }

    /// <summary>
    /// Returns how many enemies should spawn in this wave, capped by maxAlive.
    /// </summary>
    public static int GetEnemyCount(int wave, int baseCount, int perWaveIncrease, int maxAlive)
    {
        return Mathf.Min(baseCount + (wave - 1) * perWaveIncrease, maxAlive);
    }
}
