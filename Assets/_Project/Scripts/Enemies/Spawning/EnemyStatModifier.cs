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

        // Scale stats using accelerated multiplier (doubles rate after wave 100)
        float healthMult = WaveScaler.GetStatMultiplier(wave, config.healthScalePerWave);
        float damageMult = WaveScaler.GetStatMultiplier(wave, config.damageScalePerWave);
        float speedMult = WaveScaler.GetStatMultiplier(wave, config.speedScalePerWave);

        clonedData.maxHealth *= healthMult;
        clonedData.contactDamage *= damageMult;
        clonedData.moveSpeed *= speedMult;
        clonedData.chaseSpeed *= speedMult;
        clonedData.experienceValue = Mathf.RoundToInt(clonedData.experienceValue * healthMult);

        // Assign the cloned data to EnemyController
        controller.SetData(clonedData);

        // Done — remove this component to keep the hierarchy clean
        Destroy(this);
    }
}
