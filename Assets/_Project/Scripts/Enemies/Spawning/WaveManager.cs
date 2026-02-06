using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core state machine driving the endless survival wave loop.
/// States: Idle -> Rest -> Spawning -> Active -> (Rest -> Spawning -> Active -> ...)
/// </summary>
public class WaveManager : MonoBehaviour
{
    public enum WaveState
    {
        Idle,
        Rest,
        Spawning,
        Active
    }

    [Header("References")]
    [SerializeField] private WaveConfig waveConfig;
    [SerializeField] private EnemySpawnManager spawnManager;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    private WaveState currentState = WaveState.Idle;
    private int currentWave;
    private int totalEnemiesThisWave;
    private int spawnedThisWave;
    private Coroutine activeCoroutine;

    public WaveState CurrentState => currentState;
    public int CurrentWave => currentWave;
    public float RestTimer { get; private set; }

    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCleared;
    public event Action<int, int> OnEnemyCountChanged; // alive, total
    public event Action<float> OnRestTimerUpdated; // seconds remaining

    /// <summary>
    /// Begin the wave loop, optionally starting from a specific wave number.
    /// </summary>
    public void StartWaves(int startingWave = 1)
    {
        if (currentState != WaveState.Idle)
            return;

        currentWave = Mathf.Max(1, startingWave);

        spawnManager.OnAllEnemiesDead += HandleAllEnemiesDead;
        spawnManager.OnEnemyCountChanged += HandleEnemyCountChanged;

        if (debugLogging)
            Debug.Log($"[WaveManager] Starting waves from wave {currentWave}");

        TransitionToRest();
    }

    /// <summary>
    /// Stops the wave loop and cleans up all enemies.
    /// </summary>
    public void StopWaves()
    {
        if (currentState == WaveState.Idle)
            return;

        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        spawnManager.OnAllEnemiesDead -= HandleAllEnemiesDead;
        spawnManager.OnEnemyCountChanged -= HandleEnemyCountChanged;

        spawnManager.DestroyAllEnemies();
        currentState = WaveState.Idle;

        if (debugLogging)
            Debug.Log("[WaveManager] Waves stopped");
    }

    private void OnDestroy()
    {
        if (spawnManager != null)
        {
            spawnManager.OnAllEnemiesDead -= HandleAllEnemiesDead;
            spawnManager.OnEnemyCountChanged -= HandleEnemyCountChanged;
        }
    }

    private void TransitionToRest()
    {
        currentState = WaveState.Rest;
        activeCoroutine = StartCoroutine(RestCoroutine());
    }

    private IEnumerator RestCoroutine()
    {
        float timer = waveConfig.restDuration;

        if (debugLogging)
            Debug.Log($"[WaveManager] Rest phase — Wave {currentWave} incoming in {timer}s");

        while (timer > 0f)
        {
            RestTimer = timer;
            OnRestTimerUpdated?.Invoke(timer);
            yield return null;
            timer -= Time.deltaTime;
        }

        RestTimer = 0f;
        OnRestTimerUpdated?.Invoke(0f);
        TransitionToSpawning();
    }

    private void TransitionToSpawning()
    {
        currentState = WaveState.Spawning;

        totalEnemiesThisWave = WaveScaler.GetEnemyCount(
            currentWave,
            waveConfig.baseEnemyCount,
            waveConfig.enemiesPerWaveIncrease,
            waveConfig.maxEnemiesAlive);
        spawnedThisWave = 0;

        if (debugLogging)
            Debug.Log($"[WaveManager] Wave {currentWave} — spawning {totalEnemiesThisWave} enemies");

        OnWaveStarted?.Invoke(currentWave);
        activeCoroutine = StartCoroutine(SpawnCoroutine());
    }

    private IEnumerator SpawnCoroutine()
    {
        List<WaveConfig.EnemySpawnEntry> eligible = GetEligibleEnemies();

        if (eligible.Count == 0)
        {
            Debug.LogWarning("[WaveManager] No eligible enemies for this wave!");
            yield break;
        }

        float totalWeight = 0f;
        foreach (var entry in eligible)
            totalWeight += entry.spawnWeight;

        while (spawnedThisWave < totalEnemiesThisWave)
        {
            // Wait if at max alive limit
            while (spawnManager.AliveCount >= waveConfig.maxEnemiesAlive)
            {
                yield return null;
            }

            // Weighted random selection
            GameObject prefab = PickWeightedRandom(eligible, totalWeight);
            spawnManager.SpawnEnemy(prefab, currentWave, waveConfig);
            spawnedThisWave++;

            if (spawnedThisWave < totalEnemiesThisWave)
            {
                yield return new WaitForSeconds(waveConfig.spawnInterval);
            }
        }

        // All enemies spawned — transition to Active
        currentState = WaveState.Active;
        activeCoroutine = null;

        if (debugLogging)
            Debug.Log($"[WaveManager] All {totalEnemiesThisWave} enemies spawned, entering Active state");
    }

    private List<WaveConfig.EnemySpawnEntry> GetEligibleEnemies()
    {
        var eligible = new List<WaveConfig.EnemySpawnEntry>();
        if (waveConfig.enemyPool == null)
            return eligible;

        foreach (var entry in waveConfig.enemyPool)
        {
            if (entry.prefab != null && currentWave >= entry.minWaveToAppear)
            {
                eligible.Add(entry);
            }
        }
        return eligible;
    }

    private GameObject PickWeightedRandom(List<WaveConfig.EnemySpawnEntry> pool, float totalWeight)
    {
        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in pool)
        {
            cumulative += entry.spawnWeight;
            if (roll <= cumulative)
                return entry.prefab;
        }

        // Fallback (shouldn't happen)
        return pool[pool.Count - 1].prefab;
    }

    private void HandleAllEnemiesDead()
    {
        if (currentState != WaveState.Active)
            return;

        if (debugLogging)
            Debug.Log($"[WaveManager] Wave {currentWave} cleared!");

        OnWaveCleared?.Invoke(currentWave);
        currentWave++;
        TransitionToRest();
    }

    private void HandleEnemyCountChanged(int aliveCount)
    {
        OnEnemyCountChanged?.Invoke(aliveCount, totalEnemiesThisWave);
    }
}
