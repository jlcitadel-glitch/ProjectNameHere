using System;
using UnityEngine;

/// <summary>
/// Boss-specific controller adding phase transitions and special attacks.
/// Works alongside EnemyController, EnemyCombat, and movement components.
/// </summary>
[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(HealthSystem))]
public class BossController : MonoBehaviour
{
    [Header("Boss Identity")]
    [SerializeField] private string bossName = "Boss";

    [Header("Phase Configuration")]
    [SerializeField] private float phase2HealthPercent = 0.5f;
    [SerializeField] private float enrageHealthPercent = 0.2f;

    [Header("Phase 2 Modifiers")]
    [SerializeField] private float phase2SpeedMultiplier = 1.3f;
    [SerializeField] private float phase2DamageMultiplier = 1.2f;
    [SerializeField] private float phase2CooldownMultiplier = 0.7f;

    [Header("Enrage Modifiers")]
    [SerializeField] private float enrageSpeedMultiplier = 1.5f;
    [SerializeField] private float enrageDamageMultiplier = 1.5f;
    [SerializeField] private float enrageCooldownMultiplier = 0.56f;

    [Header("VFX")]
    [SerializeField] private GameObject phaseChangeVFX;
    [SerializeField] private GameObject enrageVFX;
    [SerializeField] private GameObject enrageAuraPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip entranceSound;
    [SerializeField] private AudioClip phaseChangeSound;
    [SerializeField] private AudioClip enrageSound;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    public enum BossPhase { Phase1, Phase2, Enraged, Dead }

    private EnemyController enemyController;
    private HealthSystem healthSystem;
    private AudioSource audioSource;
    private BossPhase currentPhase = BossPhase.Phase1;
    private GameObject activeEnrageAura;

    public string BossName => bossName;
    public BossPhase CurrentPhase => currentPhase;
    public HealthSystem BossHealth => healthSystem;

    public event Action<BossPhase> OnPhaseChanged;
    public event Action OnBossDefeated;

    private void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        healthSystem = GetComponent<HealthSystem>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void Start()
    {
        healthSystem.OnHealthChanged += HandleHealthChanged;
        healthSystem.OnDeath += HandleDeath;

        // Enter boss fight mode
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnterBossFight();
        }

        // Play entrance sound
        if (entranceSound != null)
        {
            SFXManager.PlayOneShot(audioSource, entranceSound);
        }

        if (debugLogging)
        {
            Debug.Log($"[BossController] {bossName} fight started!");
        }
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= HandleHealthChanged;
            healthSystem.OnDeath -= HandleDeath;
        }

        // Clean up enrage aura
        if (activeEnrageAura != null)
        {
            Destroy(activeEnrageAura);
        }
    }

    private void HandleHealthChanged(float currentHP, float maxHP)
    {
        if (currentPhase == BossPhase.Dead)
            return;

        float healthPercent = currentHP / maxHP;

        // Check phase transitions (only advance, never go back)
        if (currentPhase == BossPhase.Phase1 && healthPercent <= phase2HealthPercent)
        {
            TransitionToPhase(BossPhase.Phase2);
        }
        else if (currentPhase == BossPhase.Phase2 && healthPercent <= enrageHealthPercent)
        {
            TransitionToPhase(BossPhase.Enraged);
        }
    }

    private void TransitionToPhase(BossPhase newPhase)
    {
        if (newPhase == currentPhase)
            return;

        BossPhase previousPhase = currentPhase;
        currentPhase = newPhase;

        if (debugLogging)
        {
            Debug.Log($"[BossController] {bossName}: {previousPhase} -> {newPhase}");
        }

        switch (newPhase)
        {
            case BossPhase.Phase2:
                EnterPhase2();
                break;
            case BossPhase.Enraged:
                EnterEnraged();
                break;
        }

        OnPhaseChanged?.Invoke(newPhase);
    }

    private void EnterPhase2()
    {
        // Play phase change VFX
        if (phaseChangeVFX != null)
        {
            Instantiate(phaseChangeVFX, transform.position, Quaternion.identity);
        }

        // Play phase change sound
        if (phaseChangeSound != null)
        {
            SFXManager.PlayOneShot(audioSource, phaseChangeSound);
        }

        // Brief invulnerability during transition
        healthSystem.GrantInvulnerability(0.5f);

        if (debugLogging)
        {
            Debug.Log($"[BossController] {bossName} entered Phase 2! Speed x{phase2SpeedMultiplier}, Damage x{phase2DamageMultiplier}");
        }
    }

    private void EnterEnraged()
    {
        // Play enrage VFX
        if (enrageVFX != null)
        {
            Instantiate(enrageVFX, transform.position, Quaternion.identity);
        }

        // Spawn persistent enrage aura
        if (enrageAuraPrefab != null)
        {
            activeEnrageAura = Instantiate(enrageAuraPrefab, transform);
            activeEnrageAura.transform.localPosition = Vector3.zero;
        }

        // Play enrage sound
        if (enrageSound != null)
        {
            SFXManager.PlayOneShot(audioSource, enrageSound);
        }

        // Brief invulnerability during transition
        healthSystem.GrantInvulnerability(0.5f);

        if (debugLogging)
        {
            Debug.Log($"[BossController] {bossName} ENRAGED! Speed x{enrageSpeedMultiplier}, Damage x{enrageDamageMultiplier}");
        }
    }

    private void HandleDeath()
    {
        currentPhase = BossPhase.Dead;

        if (debugLogging)
        {
            Debug.Log($"[BossController] {bossName} defeated!");
        }

        // Clean up enrage aura
        if (activeEnrageAura != null)
        {
            Destroy(activeEnrageAura);
            activeEnrageAura = null;
        }

        // Exit boss fight mode
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ExitBossFight();
        }

        OnBossDefeated?.Invoke();
    }

    /// <summary>
    /// Returns the speed multiplier for the current phase.
    /// Movement components can query this to adjust speed.
    /// </summary>
    public float GetSpeedMultiplier()
    {
        return currentPhase switch
        {
            BossPhase.Phase2 => phase2SpeedMultiplier,
            BossPhase.Enraged => enrageSpeedMultiplier,
            _ => 1f
        };
    }

    /// <summary>
    /// Returns the damage multiplier for the current phase.
    /// Combat components can query this to adjust damage.
    /// </summary>
    public float GetDamageMultiplier()
    {
        return currentPhase switch
        {
            BossPhase.Phase2 => phase2DamageMultiplier,
            BossPhase.Enraged => enrageDamageMultiplier,
            _ => 1f
        };
    }

    /// <summary>
    /// Returns the cooldown multiplier for the current phase.
    /// Lower = faster attacks.
    /// </summary>
    public float GetCooldownMultiplier()
    {
        return currentPhase switch
        {
            BossPhase.Phase2 => phase2CooldownMultiplier,
            BossPhase.Enraged => enrageCooldownMultiplier,
            _ => 1f
        };
    }
}
