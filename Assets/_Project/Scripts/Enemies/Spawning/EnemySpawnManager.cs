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

    [Header("Spawn Distribution")]
    [Tooltip("Minimum distance between spawned enemies to prevent overlap")]
    [SerializeField] private float minSpawnSeparation = 2.0f;
    [Tooltip("Random horizontal offset applied when all spawn points are occupied")]
    [SerializeField] private float fallbackSpawnOffset = 2.5f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    private readonly List<EnemyController> aliveEnemies = new List<EnemyController>();
    private readonly List<int> recentlyUsedSpawnPoints = new List<int>();
    private readonly List<Vector3> recentSpawnPositions = new List<Vector3>();

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

        // Pick a spawn point that isn't too close to existing enemies
        Transform point = PickSpawnPoint();
        Vector3 spawnPos;
        if (point == null)
        {
            // All spawn points occupied — pick a random one and add an offset
            // so the new enemy doesn't land exactly on top of an existing one.
            point = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            float offsetX = (UnityEngine.Random.value > 0.5f ? 1f : -1f) * fallbackSpawnOffset;
            spawnPos = point.position + new Vector3(offsetX, 0f, 0f);

            if (debugLogging)
            {
                Debug.LogWarning($"[EnemySpawnManager] No valid spawn point available — using fallback with offset ({offsetX:F1}, 0).");
            }
        }
        else
        {
            spawnPos = point.position;
        }

        // For non-flying enemies, raycast down to find the actual ground surface
        // so they don't spawn floating in mid-air. This applies to GroundPatrol,
        // Hopping, and Stationary (tower) types.
        EnemyController prefabController = prefab.GetComponent<EnemyController>();
        if (prefabController != null && prefabController.Data != null
            && prefabController.Data.enemyType != EnemyType.Flying)
        {
            LayerMask groundMask = prefabController.Data.groundLayer;

            // If no ground layer configured, try common layer names
            if (groundMask == 0)
            {
                string[] layerNames = { "Ground", "ground", "Terrain", "Platform", "Environment" };
                foreach (string name in layerNames)
                {
                    int idx = LayerMask.NameToLayer(name);
                    if (idx >= 0)
                    {
                        groundMask = 1 << idx;
                        break;
                    }
                }
            }

            if (groundMask != 0)
            {
                RaycastHit2D hit = Physics2D.Raycast(spawnPos, Vector2.down, 50f, groundMask);
                if (hit.collider != null)
                {
                    // Place at ground surface plus a small offset so the collider doesn't clip
                    spawnPos = new Vector3(spawnPos.x, hit.point.y + 0.5f, spawnPos.z);
                }
            }
        }

        // Record the final spawn position for overlap prevention
        recentSpawnPositions.Add(spawnPos);

        // Instantiate
        GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);

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
    /// Registers an externally-created enemy (e.g., from split-on-death) for wave tracking.
    /// The wave won't end until all registered enemies are dead.
    /// </summary>
    public void RegisterExternalEnemy(EnemyController controller)
    {
        if (controller == null || aliveEnemies.Contains(controller))
            return;

        aliveEnemies.Add(controller);
        controller.OnEnemyDeath += () => HandleEnemyDeath(controller);
        OnEnemyCountChanged?.Invoke(aliveEnemies.Count);
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

    /// <summary>
    /// Resets recently-used tracking. Call between waves so all points become available again.
    /// </summary>
    public void ResetSpawnPoints()
    {
        recentlyUsedSpawnPoints.Clear();
        recentSpawnPositions.Clear();
    }

    private Transform PickSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        // Reset tracking when all points have been used
        if (recentlyUsedSpawnPoints.Count >= spawnPoints.Length)
            recentlyUsedSpawnPoints.Clear();

        // Build list of candidate indices (not recently used)
        List<int> candidates = new List<int>();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (!recentlyUsedSpawnPoints.Contains(i))
                candidates.Add(i);
        }

        // Shuffle candidates and pick the first one far enough from existing enemies
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        foreach (int idx in candidates)
        {
            Vector3 pos = spawnPoints[idx].position;
            bool tooClose = false;

            // Check distance to alive enemies
            foreach (EnemyController enemy in aliveEnemies)
            {
                if (enemy != null && Vector2.Distance(pos, enemy.transform.position) < minSpawnSeparation)
                {
                    tooClose = true;
                    break;
                }
            }

            // Also check recent spawn positions (catches rapid successive spawns
            // before the enemy has moved away from its spawn point)
            if (!tooClose)
            {
                foreach (Vector3 recentPos in recentSpawnPositions)
                {
                    if (Vector2.Distance(pos, recentPos) < minSpawnSeparation)
                    {
                        tooClose = true;
                        break;
                    }
                }
            }

            if (!tooClose)
            {
                recentlyUsedSpawnPoints.Add(idx);
                recentSpawnPositions.Add(pos);
                return spawnPoints[idx];
            }
        }

        return null; // All points too close to existing enemies
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
