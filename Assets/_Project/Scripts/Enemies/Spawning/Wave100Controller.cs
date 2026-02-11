using System;
using System.Collections;
using UnityEngine;
using ProjectName.UI;

/// <summary>
/// Orchestrates the wave 100 milestone event: pre-boss cutscene, boss fight,
/// post-defeat cutscene, power absorption, credits roll, then resume gameplay.
/// Listens to WaveManager.OnSpecialWaveReached and intercepts the milestone wave.
/// Manages GameManager state directly to avoid state flicker between phases.
/// </summary>
public class Wave100Controller : MonoBehaviour
{
    [Header("Milestone Settings")]
    [SerializeField] private int milestoneWave = 100;

    [Header("Cutscene References")]
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private CutsceneData preBossCutscene;
    [SerializeField] private CutsceneData postDefeatCutscene;

    [Header("Wave References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private EnemySpawnManager spawnManager;

    [Header("Credits")]
    [SerializeField] private CreditsController creditsController;

    [Header("Boss")]
    [SerializeField] private GameObject milestoneBossPrefab;
    [SerializeField] private WaveConfig waveConfig;

    [Header("Power Absorption Buffs")]
    [SerializeField] private float hpMultiplier = 1.25f;
    [SerializeField] private float damageMultiplier = 1.20f;
    [SerializeField] private float speedMultiplier = 1.15f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    private bool sequenceActive;

    private void OnEnable()
    {
        if (waveManager != null)
        {
            waveManager.OnSpecialWaveReached += HandleSpecialWave;
        }
    }

    private void OnDisable()
    {
        if (waveManager != null)
        {
            waveManager.OnSpecialWaveReached -= HandleSpecialWave;
        }
    }

    private void HandleSpecialWave(int wave)
    {
        if (wave != milestoneWave)
            return;

        // Check if player has already seen the cutscene
        if (SaveManager.Instance != null && SaveManager.Instance.HasSaveData
            && SaveManager.Instance.CurrentSave.hasSeenWave100Cutscene)
        {
            if (debugLogging)
                Debug.Log($"[Wave100Controller] Wave {milestoneWave} cutscene already seen — skipping.");
            return;
        }

        if (sequenceActive)
            return;

        if (debugLogging)
            Debug.Log($"[Wave100Controller] Milestone wave {milestoneWave} reached — starting sequence.");

        // Pause the wave loop so we can run our sequence
        waveManager.PauseWaveProgression();
        StartCoroutine(MilestoneSequence());
    }

    private IEnumerator MilestoneSequence()
    {
        sequenceActive = true;

        // --- PRE-BOSS CUTSCENE ---
        // We manage GameManager state ourselves (manageGameState: false)
        // to avoid state flicker between phases.
        GameManager.Instance?.StartCutscene();

        if (preBossCutscene != null && cutsceneManager != null)
        {
            bool cutsceneDone = false;
            cutsceneManager.PlayCutscene(preBossCutscene, () => cutsceneDone = true, manageGameState: false);

            while (!cutsceneDone)
                yield return null;
        }

        if (debugLogging)
            Debug.Log("[Wave100Controller] Pre-boss cutscene complete — spawning boss.");

        // --- BOSS FIGHT ---
        GameManager.Instance?.EnterBossFight();

        // Spawn the milestone boss (use dedicated prefab or fall back to wave config boss)
        GameObject bossPrefab = milestoneBossPrefab != null ? milestoneBossPrefab : waveConfig?.bossPrefab;

        EnemyController bossController = null;
        if (bossPrefab != null && spawnManager != null)
        {
            bossController = spawnManager.SpawnEnemy(bossPrefab, milestoneWave, waveConfig);
        }
        else
        {
            Debug.LogWarning("[Wave100Controller] No boss prefab configured — skipping boss fight.");
        }

        // Wait for boss to be defeated
        if (bossController != null)
        {
            bool bossDefeated = false;
            bossController.OnEnemyDeath += () => bossDefeated = true;

            while (!bossDefeated)
                yield return null;
        }

        if (debugLogging)
            Debug.Log("[Wave100Controller] Boss defeated — playing post-defeat cutscene.");

        // --- POST-DEFEAT CUTSCENE ---
        GameManager.Instance?.StartCutscene();

        if (postDefeatCutscene != null && cutsceneManager != null)
        {
            bool cutsceneDone = false;
            cutsceneManager.PlayCutscene(postDefeatCutscene, () => cutsceneDone = true, manageGameState: false);

            while (!cutsceneDone)
                yield return null;
        }

        // --- POWER ABSORPTION ---
        ApplyPowerAbsorption();

        // --- CREDITS ROLL ---
        if (creditsController != null)
        {
            bool creditsDone = false;

            if (debugLogging)
                Debug.Log("[Wave100Controller] Rolling credits.");

            // Stay in Cutscene state for credits (already in Cutscene from post-defeat)
            creditsController.Show(() => creditsDone = true);

            while (!creditsDone)
                yield return null;

            creditsController.gameObject.SetActive(false);
        }

        // --- SAVE MILESTONE FLAG ---
        if (SaveManager.Instance != null)
        {
            if (SaveManager.Instance.CurrentSave != null)
            {
                SaveManager.Instance.CurrentSave.hasSeenWave100Cutscene = true;
            }
            SaveManager.Instance.Save();
        }

        // --- RESUME GAMEPLAY ---
        // Return to Playing state before resuming the wave loop
        GameManager.Instance?.EndCutscene();

        if (debugLogging)
            Debug.Log($"[Wave100Controller] Milestone sequence complete — advancing past wave {milestoneWave}.");

        sequenceActive = false;

        // Use CompleteMilestoneWave instead of ResumeWaveProgression to:
        // 1. Fire OnWaveCleared (so SurvivalArena saves progress)
        // 2. Advance currentWave (avoids double-boss-spawn since wave 5 is a boss wave)
        // 3. Start the rest phase for the next wave
        waveManager.CompleteMilestoneWave();
    }

    private void ApplyPowerAbsorption()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        // Apply HP boost via HealthSystem
        var healthSystem = player.GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            float newMax = healthSystem.MaxHealth * hpMultiplier;
            healthSystem.SetMaxHealth(newMax, refill: true);
        }

        // Show notification
        if (NotificationSystem.Instance != null)
        {
            NotificationSystem.Instance.ShowNotification(
                "Champion's Essence Absorbed! Stats permanently increased.",
                NotificationType.LevelUp
            );
        }

        if (debugLogging)
            Debug.Log($"[Wave100Controller] Power absorption applied — HP x{hpMultiplier}");
    }
}
