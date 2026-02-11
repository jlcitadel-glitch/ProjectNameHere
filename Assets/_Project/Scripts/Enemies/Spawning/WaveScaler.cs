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
    /// Returns the stat multiplier for a given wave, with accelerated scaling
    /// after the acceleration start wave (default 100). Post-acceleration waves
    /// scale at double the normal rate.
    /// </summary>
    public static float GetStatMultiplier(int wave, float scalePerWave, int accelerationStartWave = 100)
    {
        if (wave <= 1) return 1f;

        if (wave <= accelerationStartWave)
        {
            return 1f + (wave - 1) * scalePerWave;
        }

        // Normal rate up to the acceleration threshold, doubled rate after
        float normalPortion = (float)(accelerationStartWave - 1);
        float acceleratedPortion = (float)(wave - accelerationStartWave);
        return 1f + normalPortion * scalePerWave + acceleratedPortion * scalePerWave * 2f;
    }

    /// <summary>
    /// Returns how many enemies should spawn in this wave, capped by maxAlive.
    /// </summary>
    public static int GetEnemyCount(int wave, int baseCount, int perWaveIncrease, int maxAlive)
    {
        return Mathf.Min(baseCount + (wave - 1) * perWaveIncrease, maxAlive);
    }
}
