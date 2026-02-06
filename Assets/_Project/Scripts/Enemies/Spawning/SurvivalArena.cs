using UnityEngine;

/// <summary>
/// Top-level coordinator for the survival arena.
/// Placed on a trigger zone in the scene. When the player enters,
/// locks the camera and starts the wave loop. Integrates with SaveManager
/// to persist wave progress.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class SurvivalArena : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private EnemySpawnManager spawnManager;

    [Header("Arena Settings")]
    [Tooltip("If true, resumes from the player's saved wave on entry")]
    [SerializeField] private bool resumeFromSave = false;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    private bool isActive;
    private HealthSystem playerHealth;

    private void Start()
    {
        // Ensure the collider is a trigger
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        if (waveManager != null)
        {
            waveManager.OnWaveCleared += HandleWaveCleared;
        }
    }

    private void OnDestroy()
    {
        if (waveManager != null)
        {
            waveManager.OnWaveCleared -= HandleWaveCleared;
        }

        if (playerHealth != null)
        {
            playerHealth.OnDeath -= HandlePlayerDeath;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActive)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (debugLogging)
            Debug.Log("[SurvivalArena] Player entered arena");

        isActive = true;

        // Subscribe to player death
        playerHealth = other.GetComponent<HealthSystem>();
        if (playerHealth != null)
        {
            playerHealth.OnDeath += HandlePlayerDeath;
        }

        // Determine starting wave
        int startWave = 1;
        if (resumeFromSave && SaveManager.Instance != null && SaveManager.Instance.HasSaveData)
        {
            startWave = Mathf.Max(1, SaveManager.Instance.CurrentSave.currentWave);
        }

        // Start wave loop
        waveManager.StartWaves(startWave);
    }

    private void HandleWaveCleared(int wave)
    {
        // Persist progress
        if (SaveManager.Instance != null)
        {
            if (SaveManager.Instance.CurrentSave == null)
            {
                // Create save data if none exists
                SaveManager.Instance.Save();
            }

            SaveManager.Instance.CurrentSave.currentWave = wave + 1;

            if (wave > SaveManager.Instance.CurrentSave.maxWaveReached)
            {
                SaveManager.Instance.CurrentSave.maxWaveReached = wave;
            }

            SaveManager.Instance.Save();

            if (debugLogging)
                Debug.Log($"[SurvivalArena] Saved progress — next wave: {wave + 1}, max reached: {SaveManager.Instance.CurrentSave.maxWaveReached}");
        }
    }

    private void HandlePlayerDeath()
    {
        if (!isActive)
            return;

        if (debugLogging)
            Debug.Log("[SurvivalArena] Player died — stopping arena");

        Deactivate();
    }

    /// <summary>
    /// Stops the arena and cleans up enemies.
    /// </summary>
    public void Deactivate()
    {
        if (!isActive)
            return;

        isActive = false;

        waveManager.StopWaves();

        if (playerHealth != null)
        {
            playerHealth.OnDeath -= HandlePlayerDeath;
            playerHealth = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw arena bounds
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
            Vector3 center = transform.position + (Vector3)col.offset;
            Gizmos.DrawCube(center, col.size);

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center, col.size);
        }

    }
}
