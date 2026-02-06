using System;
using UnityEngine;

/// <summary>
/// Core enemy controller handling state machine, health integration, and death.
/// Coordinates between movement, combat, and sensor components.
/// </summary>
[RequireComponent(typeof(HealthSystem))]
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Configuration")]
    [SerializeField] private EnemyData enemyData;

    [Header("Component References")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    [Header("Experience Orbs")]
    [SerializeField] private GameObject experienceOrbPrefab;
    [SerializeField] private int orbCount = 3;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    // Components
    private HealthSystem healthSystem;
    private BaseEnemyMovement movement;
    private EnemyCombat combat;
    private EnemySensors sensors;
    private BossController bossController;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    // State
    private EnemyState currentState = EnemyState.Idle;
    private EnemyState previousState;
    private float stateTimer;
    private float stunTimer;
    private Transform currentTarget;
    private bool isDead;

    // Animation hashes
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int AnimHurt = Animator.StringToHash("Hurt");
    private static readonly int AnimDie = Animator.StringToHash("Die");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");

    // Cached parameter existence checks
    private bool hasSpeedParam;
    private bool hasIsGroundedParam;

    // Properties
    public EnemyData Data => enemyData;
    public EnemyState CurrentState => currentState;
    public Transform CurrentTarget => currentTarget;
    public bool IsDead => isDead;
    public bool IsStunned => currentState == EnemyState.Stunned;
    public float FacingDirection => transform.localScale.x >= 0 ? 1f : -1f;

    // Events
    public event Action<EnemyState, EnemyState> OnStateChanged;
    public event Action OnEnemyDeath;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        movement = GetComponent<BaseEnemyMovement>();
        combat = GetComponent<EnemyCombat>();
        sensors = GetComponent<EnemySensors>();
        bossController = GetComponent<BossController>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Auto-add missing components based on EnemyData configuration
        if (enemyData != null)
        {
            if (movement == null)
                movement = AddMovementComponent(enemyData.enemyType);

            if (sensors == null)
            {
                Debug.LogWarning($"[EnemyController] {gameObject.name}: Missing EnemySensors, adding automatically.");
                sensors = gameObject.AddComponent<EnemySensors>();
            }

            if (combat == null && enemyData.attacks != null && enemyData.attacks.Length > 0)
            {
                Debug.LogWarning($"[EnemyController] {gameObject.name}: Missing EnemyCombat, adding automatically.");
                combat = gameObject.AddComponent<EnemyCombat>();
            }
        }

        // Safety net: ensure Rigidbody2D is Dynamic so physics (gravity, collisions) work.
        // BaseEnemyMovement also enforces this, but Stationary enemies have no movement component.
        if (rb != null && rb.bodyType != RigidbodyType2D.Dynamic)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        if (animator == null)
            animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private BaseEnemyMovement AddMovementComponent(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.GroundPatrol:
                Debug.LogWarning($"[EnemyController] {gameObject.name}: Missing GroundPatrolMovement, adding automatically.");
                return gameObject.AddComponent<GroundPatrolMovement>();
            case EnemyType.Flying:
                Debug.LogWarning($"[EnemyController] {gameObject.name}: Missing FlyingMovement, adding automatically.");
                return gameObject.AddComponent<FlyingMovement>();
            case EnemyType.Stationary:
            default:
                return null;
        }
    }

    private void Start()
    {
        if (enemyData == null)
        {
            Debug.LogError($"[EnemyController] {gameObject.name}: EnemyData not assigned!");
            enabled = false;
            return;
        }

        InitializeFromData();
        SubscribeToEvents();

        // Spawn VFX and sound
        if (enemyData.spawnVFX != null)
        {
            Instantiate(enemyData.spawnVFX, transform.position, Quaternion.identity);
        }
        PlaySound(enemyData.spawnSound);

        // Force-enter Idle state. Can't use SetState() here because
        // currentState defaults to Idle, so SetState would skip due to
        // the duplicate-state check. We need EnterState to fire so
        // stateTimer is set and movement.Stop() is called.
        currentState = EnemyState.Idle;
        EnterState(EnemyState.Idle);
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeFromData()
    {
        // Configure health system
        healthSystem.SetMaxHealth(enemyData.maxHealth, true);
        healthSystem.SetInvulnerabilityDuration(enemyData.invulnerabilityDuration);

        // Disable HealthSystem's own animation triggers — EnemyController handles those
        healthSystem.DisableAnimationTriggers();

        // Cache which animator parameters exist to avoid errors
        CacheAnimatorParameters();
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.nameHash == AnimSpeed)
                hasSpeedParam = true;
            else if (param.nameHash == AnimIsGrounded)
                hasIsGroundedParam = true;
        }
    }

    private void SubscribeToEvents()
    {
        healthSystem.OnDamageTaken += HandleDamageTaken;
        healthSystem.OnDeath += HandleDeath;

        if (sensors != null)
        {
            sensors.OnTargetDetected += HandleTargetDetected;
            sensors.OnTargetLost += HandleTargetLost;
        }

        if (combat != null)
        {
            combat.OnAttackComplete += HandleAttackComplete;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDamageTaken -= HandleDamageTaken;
            healthSystem.OnDeath -= HandleDeath;
        }

        if (sensors != null)
        {
            sensors.OnTargetDetected -= HandleTargetDetected;
            sensors.OnTargetLost -= HandleTargetLost;
        }

        if (combat != null)
        {
            combat.OnAttackComplete -= HandleAttackComplete;
        }
    }

    private void Update()
    {
        if (isDead)
            return;

        UpdateStateMachine();
        UpdateAnimator();
    }

    private void UpdateStateMachine()
    {
        // Handle stun timer
        if (currentState == EnemyState.Stunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                // Return to appropriate state after stun
                if (currentTarget != null)
                    SetState(EnemyState.Chase);
                else
                    SetState(EnemyState.Patrol);
            }
            return;
        }

        // State-specific updates
        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Alert:
                UpdateAlert();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.Cooldown:
                UpdateCooldown();
                break;
        }
    }

    private void UpdateIdle()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            // Transition to patrol after idle period
            if (enemyData.enemyType != EnemyType.Stationary)
            {
                SetState(EnemyState.Patrol);
            }
            else
            {
                // Stationary enemies stay idle, reset timer
                stateTimer = UnityEngine.Random.Range(1f, 3f);
            }
        }
    }

    private void UpdatePatrol()
    {
        movement?.Patrol();
    }

    private void UpdateAlert()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            if (currentTarget != null)
                SetState(EnemyState.Chase);
            else
                SetState(EnemyState.Patrol);
        }
    }

    private void UpdateChase()
    {
        if (currentTarget == null)
        {
            SetState(EnemyState.Patrol);
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);

        // Check if we lost the target
        if (distanceToTarget > enemyData.loseAggroRange)
        {
            currentTarget = null;
            SetState(EnemyState.Patrol);
            return;
        }

        // Check if in attack range
        if (combat != null && distanceToTarget <= enemyData.attackRange)
        {
            SetState(EnemyState.Attack);
            return;
        }

        // Chase the target
        movement?.ChaseTarget(currentTarget);
    }

    private void UpdateAttack()
    {
        // Attack state is managed by EnemyCombat component
        // We wait for OnAttackComplete callback
    }

    private void UpdateCooldown()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            // After cooldown, decide next action
            if (currentTarget != null)
            {
                float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
                if (distanceToTarget <= enemyData.attackRange)
                    SetState(EnemyState.Attack);
                else
                    SetState(EnemyState.Chase);
            }
            else
            {
                SetState(EnemyState.Patrol);
            }
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        // Update movement speed
        if (hasSpeedParam)
        {
            float speed = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
            animator.SetFloat(AnimSpeed, speed);
        }

        // Update grounded state if movement component provides it
        if (hasIsGroundedParam && movement != null)
        {
            animator.SetBool(AnimIsGrounded, movement.IsGrounded);
        }
    }

    public void SetState(EnemyState newState)
    {
        if (currentState == newState && newState != EnemyState.Attack)
            return;

        if (isDead)
            return;

        previousState = currentState;
        currentState = newState;

        if (debugLogging)
        {
            Debug.Log($"[EnemyController] {gameObject.name}: {previousState} -> {newState}");
        }

        // Exit previous state
        ExitState(previousState);

        // Enter new state
        EnterState(newState);

        OnStateChanged?.Invoke(previousState, newState);
    }

    private void ExitState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Attack:
                combat?.CancelAttack();
                break;
        }
    }

    private void EnterState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Idle:
                movement?.Stop();
                stateTimer = UnityEngine.Random.Range(1f, 3f);
                PlaySound(enemyData.idleSound);
                break;

            case EnemyState.Patrol:
                movement?.StartPatrol();
                break;

            case EnemyState.Alert:
                movement?.Stop();
                stateTimer = 0.5f; // Brief alert pause
                PlaySound(enemyData.alertSound);
                break;

            case EnemyState.Chase:
                // Chase state is updated in UpdateChase
                break;

            case EnemyState.Attack:
                movement?.Stop();
                if (combat != null && currentTarget != null)
                {
                    combat.StartAttack(currentTarget);
                }
                PlaySound(enemyData.attackSound);
                if (animator != null)
                {
                    animator.SetTrigger(AnimAttack);
                }
                break;

            case EnemyState.Cooldown:
                movement?.Stop();
                float cooldownMult = bossController != null ? bossController.GetCooldownMultiplier() : 1f;
                stateTimer = enemyData.attackCooldown * cooldownMult;
                break;

            case EnemyState.Stunned:
                movement?.Stop();
                combat?.CancelAttack();
                if (animator != null)
                {
                    animator.SetTrigger(AnimHurt);
                }
                break;

            case EnemyState.Dead:
                movement?.Stop();
                combat?.CancelAttack();
                if (animator != null)
                {
                    animator.SetTrigger(AnimDie);
                }
                break;
        }
    }

    #region Event Handlers

    private void HandleDamageTaken(float damage)
    {
        if (isDead)
            return;

        if (debugLogging)
        {
            Debug.Log($"[EnemyController] {gameObject.name}: Took {damage} damage");
        }

        // Spawn hurt VFX
        if (enemyData.hurtVFX != null)
        {
            Instantiate(enemyData.hurtVFX, transform.position, Quaternion.identity);
        }

        PlaySound(enemyData.hurtSound);

        // Enter stunned state
        stunTimer = enemyData.stunDuration;
        SetState(EnemyState.Stunned);
    }

    private void HandleDeath()
    {
        if (isDead)
            return;

        isDead = true;

        if (debugLogging)
        {
            Debug.Log($"[EnemyController] {gameObject.name}: Died!");
        }

        SetState(EnemyState.Dead);

        // Spawn death VFX
        if (enemyData.deathVFX != null)
        {
            Instantiate(enemyData.deathVFX, transform.position, Quaternion.identity);
        }

        PlaySound(enemyData.deathSound);

        // Award XP to player
        AwardXP();

        // Drop loot
        DropLoot();

        // Fire death event
        OnEnemyDeath?.Invoke();

        // Destroy after a delay (for death animation)
        Destroy(gameObject, 1f);
    }

    private void HandleTargetDetected(Transform target)
    {
        if (isDead || currentState == EnemyState.Stunned)
            return;

        currentTarget = target;

        if (currentState == EnemyState.Idle || currentState == EnemyState.Patrol)
        {
            SetState(EnemyState.Alert);
        }
    }

    private void HandleTargetLost()
    {
        // Target lost is handled by distance check in UpdateChase
        // This callback is for when target is completely lost from sensors
    }

    private void HandleAttackComplete()
    {
        if (isDead)
            return;

        SetState(EnemyState.Cooldown);
    }

    #endregion

    #region IDamageable Implementation

    public void TakeDamage(float damage, AttackData attackData = null)
    {
        // Deal full damage through HealthSystem (knockback resistance does NOT reduce damage)
        if (healthSystem != null)
        {
            healthSystem.TakeDamage(damage);
        }

        // Note: knockback is handled by AttackHitbox.ApplyKnockback.
        // EnemyController does not apply additional knockback to avoid doubling.
    }

    #endregion

    #region Helpers

    private void AwardXP()
    {
        if (enemyData.experienceValue <= 0)
            return;

        // Spawn XP orbs if prefab assigned
        if (experienceOrbPrefab != null)
        {
            int count = Mathf.Clamp(orbCount, 1, enemyData.experienceValue);
            int xpPerOrb = enemyData.experienceValue / count;
            int remainder = enemyData.experienceValue - (xpPerOrb * count);

            for (int i = 0; i < count; i++)
            {
                GameObject orbObj = Instantiate(experienceOrbPrefab, transform.position, Quaternion.identity);
                ExperienceOrb orb = orbObj.GetComponent<ExperienceOrb>();
                if (orb != null)
                {
                    int thisOrbXP = xpPerOrb + (i == 0 ? remainder : 0);
                    orb.Initialize(thisOrbXP);
                }
            }

            if (debugLogging)
            {
                Debug.Log($"[EnemyController] Spawned {count} XP orbs totaling {enemyData.experienceValue} XP");
            }
            return;
        }

        // Fallback: direct XP award
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            LevelSystem levelSystem = player.GetComponent<LevelSystem>();
            if (levelSystem != null)
            {
                levelSystem.AddXP(enemyData.experienceValue);

                if (debugLogging)
                {
                    Debug.Log($"[EnemyController] Awarded {enemyData.experienceValue} XP to player");
                }
            }
        }
    }

    private void DropLoot()
    {
        if (enemyData.dropPrefabs == null || enemyData.dropPrefabs.Length == 0)
            return;

        if (UnityEngine.Random.value > enemyData.dropChance)
            return;

        foreach (GameObject drop in enemyData.dropPrefabs)
        {
            if (drop != null)
            {
                Vector3 dropPos = transform.position + new Vector3(
                    UnityEngine.Random.Range(-0.5f, 0.5f),
                    0.5f,
                    0f
                );
                Instantiate(drop, dropPos, Quaternion.identity);
            }
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        SFXManager.PlayOneShot(audioSource, clip);
    }

    /// <summary>
    /// Flips the enemy to face the specified direction.
    /// </summary>
    public void FaceDirection(float direction)
    {
        if (direction == 0f)
            return;

        float scaleX = Mathf.Sign(direction);
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * scaleX;
        transform.localScale = scale;
    }

    /// <summary>
    /// Makes the enemy face the current target.
    /// </summary>
    public void FaceTarget()
    {
        if (currentTarget == null)
            return;

        float direction = currentTarget.position.x - transform.position.x;
        FaceDirection(direction);
    }

    #endregion

    #region Contact Damage

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead || enemyData.contactDamage <= 0f)
            return;

        // Check if we hit the player
        if (collision.gameObject.CompareTag("Player"))
        {
            HealthSystem playerHealth = collision.gameObject.GetComponent<HealthSystem>();
            if (playerHealth != null && !playerHealth.IsInvulnerable)
            {
                playerHealth.TakeDamage(enemyData.contactDamage);

                // Apply knockback to player
                Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
                if (playerRb != null && enemyData.contactKnockbackForce > 0f)
                {
                    Vector2 knockDir = (collision.transform.position - transform.position).normalized;
                    knockDir.y = 0.3f; // Add slight upward component
                    knockDir.Normalize();
                    playerRb.AddForce(knockDir * enemyData.contactKnockbackForce, ForceMode2D.Impulse);
                }
            }
        }
    }

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (enemyData == null)
            return;

        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enemyData.detectionRange);

        // Draw lose aggro range
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, enemyData.loseAggroRange);

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyData.attackRange);
    }

    #endregion
}
