using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles enemy instantiation, spawn point selection, and alive-enemy tracking.
/// </summary>
public class EnemySpawnManager : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("Empty GameObjects marking where enemies can spawn")]
    [SerializeField] private Transform[] spawnPoints;

    private readonly List<EnemyController> aliveEnemies = new List<EnemyController>();

    public int AliveCount => aliveEnemies.Count;

    public event Action OnAllEnemiesDead;
    public event Action<int> OnEnemyCountChanged;

    /// <summary>
    /// Spawns an enemy from the given prefab, applies wave scaling, and begins tracking it.
    /// Returns the spawned EnemyController.
    /// </summary>
    public EnemyController SpawnEnemy(GameObject prefab, int wave, WaveConfig config)
    {
        if (prefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[EnemySpawnManager] Missing prefab or spawn points.");
            return null;
        }

        // Pick a random spawn point
        Transform point = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];

        // Instantiate
        GameObject enemyObj = Instantiate(prefab, point.position, Quaternion.identity);

        // Apply wave scaling before Start() fires
        EnemyStatModifier modifier = enemyObj.AddComponent<EnemyStatModifier>();
        modifier.Initialize(wave, config);

        // Track the enemy
        EnemyController controller = enemyObj.GetComponent<EnemyController>();
        if (controller != null)
        {
            aliveEnemies.Add(controller);
            controller.OnEnemyDeath += () => HandleEnemyDeath(controller);
            OnEnemyCountChanged?.Invoke(aliveEnemies.Count);
        }

        return controller;
    }

    /// <summary>
    /// Destroys all currently alive enemies immediately.
    /// </summary>
    public void DestroyAllEnemies()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] != null && aliveEnemies[i].gameObject != null)
            {
                Destroy(aliveEnemies[i].gameObject);
            }
        }
        aliveEnemies.Clear();
        OnEnemyCountChanged?.Invoke(0);
    }

    private void HandleEnemyDeath(EnemyController enemy)
    {
        aliveEnemies.Remove(enemy);
        OnEnemyCountChanged?.Invoke(aliveEnemies.Count);

        if (aliveEnemies.Count == 0)
        {
            OnAllEnemiesDead?.Invoke();
        }
    }
}
