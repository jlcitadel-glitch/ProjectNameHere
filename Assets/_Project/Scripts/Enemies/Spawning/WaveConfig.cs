using System;
using UnityEngine;

/// <summary>
/// ScriptableObject defining wave composition, enemy pool, and scaling rules
/// for the endless survival arena.
/// </summary>
[CreateAssetMenu(fileName = "NewWaveConfig", menuName = "Enemies/Wave Config")]
public class WaveConfig : ScriptableObject
{
    [Serializable]
    public class EnemySpawnEntry
    {
        public GameObject prefab;
        [Tooltip("Relative spawn frequency. 1.0 = normal")]
        public float spawnWeight = 1f;
        [Tooltip("First wave this enemy can appear in")]
        public int minWaveToAppear = 1;
    }

    [Header("Enemy Pool")]
    public EnemySpawnEntry[] enemyPool;

    [Header("Wave Sizing")]
    [Tooltip("Number of enemies in wave 1")]
    public int baseEnemyCount = 3;
    [Tooltip("Additional enemies per wave after wave 1")]
    public int enemiesPerWaveIncrease = 2;
    [Tooltip("Maximum enemies alive at once")]
    public int maxEnemiesAlive = 15;

    [Header("Stat Scaling Per Wave")]
    [Tooltip("Fractional HP increase per wave (0.15 = 15%)")]
    public float healthScalePerWave = 0.15f;
    [Tooltip("Fractional damage increase per wave (0.10 = 10%)")]
    public float damageScalePerWave = 0.10f;
    [Tooltip("Fractional speed increase per wave (0.05 = 5%)")]
    public float speedScalePerWave = 0.05f;

    [Header("Timing")]
    [Tooltip("Seconds between waves")]
    public float restDuration = 3f;
    [Tooltip("Seconds between individual enemy spawns within a wave")]
    public float spawnInterval = 0.5f;

    [Header("Boss Waves")]
    [Tooltip("Spawn a boss every N waves. 0 = no boss waves.")]
    public int bossWaveInterval = 5;
    [Tooltip("Boss prefab to spawn on boss waves")]
    public GameObject bossPrefab;
}
