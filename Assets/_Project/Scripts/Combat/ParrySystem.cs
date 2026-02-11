using System;
using UnityEngine;

/// <summary>
/// Result of a parry attempt, consumed by hitbox/projectile to apply effects.
/// </summary>
public struct ParryResult
{
    public bool wasParried;
    public float counterDamage;
    public float stunDuration;
    public bool reflectProjectile;
    public bool grantInvulnerability;
    public float invulnerabilityDuration;
}

/// <summary>
/// Core parry mechanic. Handles all three class variants (Fighter/Mage/Rogue)
/// based on the current job's ParryData. Attached to the Player alongside CombatController.
/// </summary>
public class ParrySystem : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    // State
    private bool isInParryWindow;
    private float parryWindowTimer;
    private float cooldownTimer;

    // Cached references
    private HealthSystem healthSystem;
    private CombatController combatController;
    private AudioSource audioSource;

    // Events
    public event Action<ParryType> OnParrySuccess;
    public event Action OnParryFailed;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        combatController = GetComponent<CombatController>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        // Tick cooldown
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // Tick parry window
        if (isInParryWindow)
        {
            parryWindowTimer -= Time.deltaTime;
            if (parryWindowTimer <= 0f)
            {
                EndParryWindow();
            }
        }
    }

    /// <summary>
    /// Called by input system when parry button is pressed.
    /// </summary>
    public void StartParry()
    {
        ParryData data = GetCurrentParryData();
        if (data == null)
            return;

        // Can't parry during cooldown
        if (cooldownTimer > 0f)
            return;

        // Can't parry while attacking
        if (combatController != null && combatController.CurrentState != CombatState.Idle)
            return;

        // Enter parry window
        isInParryWindow = true;
        parryWindowTimer = data.parryWindowDuration;

        if (debugLogging)
        {
            Debug.Log($"[ParrySystem] Parry started ({data.parryType}), window: {data.parryWindowDuration}s");
        }
    }

    /// <summary>
    /// Called by EnemyAttackHitbox or EnemyProjectile to check if this attack is parried.
    /// </summary>
    public ParryResult TryParry(float damage, Transform attacker, EnemyAttackData attackData)
    {
        ParryResult result = default;

        if (!isInParryWindow)
        {
            OnParryFailed?.Invoke();
            return result;
        }

        if (attackData != null && !attackData.isParryable)
            return result;

        ParryData data = GetCurrentParryData();
        if (data == null)
            return result;

        // Parry succeeds
        result.wasParried = true;
        result.counterDamage = damage * data.counterDamageMultiplier;
        result.stunDuration = data.enemyStunDuration;
        result.reflectProjectile = data.canReflectProjectiles;
        result.grantInvulnerability = data.grantsInvulnerability;
        result.invulnerabilityDuration = data.invulnerabilityDuration;

        // Apply class-specific effects
        switch (data.parryType)
        {
            case ParryType.ClassicParry:
                HandleClassicParry(data);
                break;
            case ParryType.SpellMirror:
                HandleSpellMirror(data);
                break;
            case ParryType.ShadowStep:
                HandleShadowStep(data, attacker);
                break;
        }

        // Start cooldown and end window
        cooldownTimer = data.cooldown;
        EndParryWindow();

        // Play parry SFX
        if (data.parrySound != null && audioSource != null)
        {
            SFXManager.PlayOneShot(audioSource, data.parrySound);
        }

        // Spawn parry VFX
        if (data.parryVFXPrefab != null)
        {
            Instantiate(data.parryVFXPrefab, transform.position, Quaternion.identity);
        }

        OnParrySuccess?.Invoke(data.parryType);

        if (debugLogging)
        {
            Debug.Log($"[ParrySystem] Parry SUCCESS ({data.parryType}), counter: {result.counterDamage}, stun: {result.stunDuration}s");
        }

        return result;
    }

    private void HandleClassicParry(ParryData data)
    {
        // Fighter: shield flash animation trigger could go here
    }

    private void HandleSpellMirror(ParryData data)
    {
        // Mage: arcane ward visual could go here
    }

    private void HandleShadowStep(ParryData data, Transform attacker)
    {
        // Grant i-frames
        if (data.grantsInvulnerability && healthSystem != null)
        {
            healthSystem.GrantInvulnerability(data.invulnerabilityDuration);
        }

        // Reposition behind attacker
        if (attacker != null && data.shadowStepDistance > 0f)
        {
            float attackerFacing = attacker.localScale.x >= 0 ? 1f : -1f;
            // Step behind the attacker (opposite of where they face)
            Vector3 targetPos = attacker.position + Vector3.right * (-attackerFacing * data.shadowStepDistance);
            transform.position = targetPos;

            // Face toward the attacker after repositioning
            float dirToAttacker = attacker.position.x - transform.position.x;
            if (dirToAttacker != 0f)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * Mathf.Sign(dirToAttacker);
                transform.localScale = scale;
            }
        }
    }

    private void EndParryWindow()
    {
        isInParryWindow = false;
        parryWindowTimer = 0f;
    }

    /// <summary>
    /// Gets the ParryData from the current job class, or null if none.
    /// </summary>
    private ParryData GetCurrentParryData()
    {
        if (SkillManager.Instance == null)
            return null;

        JobClassData currentJob = SkillManager.Instance.CurrentJob;
        if (currentJob == null)
            return null;

        return currentJob.parryData;
    }

    /// <summary>
    /// Whether the parry window is currently active.
    /// </summary>
    public bool IsParrying => isInParryWindow;
}
