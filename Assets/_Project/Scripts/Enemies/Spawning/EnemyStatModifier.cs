using System.Reflection;
using UnityEngine;

/// <summary>
/// Added to spawned enemies to apply wave-based stat scaling.
/// Clones the EnemyData ScriptableObject so scaling doesn't affect the original asset,
/// then overwrites health, damage, speed, and XP values based on wave number.
/// Runs in Awake() so modifications take effect before EnemyController.Start().
/// Self-destructs after applying.
/// </summary>
public class EnemyStatModifier : MonoBehaviour
{
    private int wave;
    private WaveConfig config;

    private static readonly FieldInfo enemyDataField = typeof(EnemyController)
        .GetField("enemyData", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Call immediately after Instantiate, before the first frame.
    /// </summary>
    public void Initialize(int waveNumber, WaveConfig waveConfig)
    {
        wave = waveNumber;
        config = waveConfig;
        ApplyScaling();
    }

    private void ApplyScaling()
    {
        EnemyController controller = GetComponent<EnemyController>();
        if (controller == null || controller.Data == null || config == null)
        {
            Destroy(this);
            return;
        }

        // Clone the EnemyData so we don't mutate the shared ScriptableObject asset
        EnemyData clonedData = Instantiate(controller.Data);
        clonedData.name = controller.Data.name + "_Wave" + wave;

        // Scale stats
        clonedData.maxHealth = WaveScaler.ScaleStat(clonedData.maxHealth, wave, config.healthScalePerWave);
        clonedData.contactDamage = WaveScaler.ScaleStat(clonedData.contactDamage, wave, config.damageScalePerWave);
        clonedData.moveSpeed = WaveScaler.ScaleStat(clonedData.moveSpeed, wave, config.speedScalePerWave);
        clonedData.chaseSpeed = WaveScaler.ScaleStat(clonedData.chaseSpeed, wave, config.speedScalePerWave);
        clonedData.experienceValue = Mathf.RoundToInt(
            WaveScaler.ScaleStat(clonedData.experienceValue, wave, config.healthScalePerWave));

        // Assign the cloned data to EnemyController via reflection
        // (enemyData is a private serialized field)
        if (enemyDataField != null)
        {
            enemyDataField.SetValue(controller, clonedData);
        }
        else
        {
            Debug.LogWarning("[EnemyStatModifier] Could not find enemyData field on EnemyController.");
        }

        // Done — remove this component to keep the hierarchy clean
        Destroy(this);
    }
}
