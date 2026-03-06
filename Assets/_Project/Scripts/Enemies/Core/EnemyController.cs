using System;
using UnityEngine;
using ProjectName.UI;

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

    [Header("Audio Fallback")]
    [Tooltip("Played when enemy is hit and EnemyData.hurtSound is not assigned")]
    [SerializeField] private AudioClip fallbackHurtSound;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    // Components
    private HealthSystem healthSystem;
    private BaseEnemyMovement movement;
    private EnemyCombat combat;
    private EnemySensors sensors;
    private BossController bossController;
    private EnemyAppearance appearance;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    // State
    private EnemyState currentState = EnemyState.Idle;
    private EnemyState previousState;
    private float stateTimer;
    private float stunTimer;
    private Transform currentTarget;
    private bool isDead;
    private AttackData lastReceivedAttackData;

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
    public void SetData(EnemyData data) => enemyData = data;
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

            if (GetComponent<EnemyHitFlash>() == null)
            {
                gameObject.AddComponent<EnemyHitFlash>();
            }

            // Initialize layered appearance for humanoid enemies
            if (enemyData.appearanceConfig != null)
            {
                appearance = GetComponent<EnemyAppearance>();
                if (appearance == null)
                    appearance = gameObject.AddComponent<EnemyAppearance>();
                appearance.Initialize(enemyData.appearanceConfig, enemyData.appearanceFrameMap);

                // Wire layered flash to EnemyHitFlash
                var hitFlash = GetComponent<EnemyHitFlash>();
                if (hitFlash != null && appearance.LayeredSprite != null)
                    hitFlash.SetLayeredSprite(appearance.LayeredSprite);
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

        // Disable root motion so Animator doesn't override physics position.
        // Without this, animation clips with Transform curves can teleport the
        // enemy to distant positions, causing the "random teleport" bug.
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        // Cache player layer mask for contact damage detection
        playerLayerMask = LayerMask.GetMask("Player");
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
            case EnemyType.Hopping:
                Debug.LogWarning($"[EnemyController] {gameObject.name}: Missing HoppingMovement, adding automatically.");
                return gameObject.AddComponent<HoppingMovement>();
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

    private void FixedUpdate()
    {
        if (isDead)
            return;

        CheckContactDamage();
    }

    private void Update()
    {
        if (isDead)
            return;

        UpdateStateMachine();
        UpdateAnimator();
    }

    private void LateUpdate()
    {
        // The Animator runs in Update and can write Transform.position from
        // animation curves, overriding the Rigidbody2D's physics-driven position.
        // LateUpdate runs after the Animator, so we re-sync the transform to
        // where physics says the enemy actually is.
        if (rb != null && !isDead)
        {
            transform.position = new Vector3(rb.position.x, rb.position.y, transform.position.z);
        }
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
            // Stationary enemies return to Idle (they don't patrol)
            SetState(enemyData.enemyType == EnemyType.Stationary ? EnemyState.Idle : EnemyState.Patrol);
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);

        // Check if we lost the target
        if (distanceToTarget > enemyData.loseAggroRange)
        {
            currentTarget = null;
            SetState(enemyData.enemyType == EnemyType.Stationary ? EnemyState.Idle : EnemyState.Patrol);
            return;
        }

        // Check if in attack range
        if (combat != null && distanceToTarget <= enemyData.attackRange)
        {
            SetState(EnemyState.Attack);
            return;
        }

        // Stationary enemies (towers) can't move closer — attack at detection range
        // if they have combat configured. This allows ranged attacks to fire even when
        // the player is beyond the nominal attackRange.
        if (enemyData.enemyType == EnemyType.Stationary && combat != null)
        {
            FaceTarget();
            SetState(EnemyState.Attack);
            return;
        }

        // Chase the target (null-safe: stationary enemies have no movement)
        if (movement != null)
        {
            movement.ChaseTarget(currentTarget);
        }
        else
        {
            // No movement component — face target and wait
            FaceTarget();
        }
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

        // Play hurt sound with fallback chain:
        // 1. EnemyData.hurtSound  (designer-assigned per enemy type)
        // 2. fallbackHurtSound    (designer-assigned on prefab)
        // 3. AttackData.hitSound  (from the weapon that dealt the blow)
        // 4. AttackData.attackSound (swing sound — last resort so hits are never silent)
        AudioClip hurtClip = enemyData.hurtSound;
        if (hurtClip == null)
            hurtClip = fallbackHurtSound;
        if (hurtClip == null && lastReceivedAttackData != null)
            hurtClip = lastReceivedAttackData.hitSound ?? lastReceivedAttackData.attackSound;
        PlaySound(hurtClip);
        lastReceivedAttackData = null;

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

        // Spawn death hazard (e.g., noxious cloud)
        if (enemyData.deathHazardPrefab != null)
        {
            Instantiate(enemyData.deathHazardPrefab, transform.position, Quaternion.identity);
        }

        // Split into smaller enemies
        if (enemyData.deathSpawnPrefab != null && enemyData.deathSpawnCount > 0)
        {
            SpawnSplitEnemies();
        }

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
        // Cache the AttackData so HandleDamageTaken can use its hitSound
        // as a fallback when enemyData.hurtSound is not assigned.
        lastReceivedAttackData = attackData;

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

    private void SpawnSplitEnemies()
    {
        // Cache scene references once for all spawns
        WaveManager waveManager = FindAnyObjectByType<WaveManager>();
        EnemySpawnManager spawnManager = FindAnyObjectByType<EnemySpawnManager>();

        for (int i = 0; i < enemyData.deathSpawnCount; i++)
        {
            // Alternate left/right spread
            float offset = (i % 2 == 0 ? -1f : 1f) * enemyData.deathSpawnSpread * ((i / 2) + 1);
            Vector3 spawnPos = transform.position + new Vector3(offset, 0.5f, 0f);

            GameObject spawnedObj = Instantiate(enemyData.deathSpawnPrefab, spawnPos, Quaternion.identity);

            // Apply wave scaling if a WaveManager exists
            if (waveManager != null && waveManager.CurrentWave > 1 && waveManager.Config != null)
            {
                EnemyStatModifier modifier = spawnedObj.AddComponent<EnemyStatModifier>();
                modifier.Initialize(waveManager.CurrentWave, waveManager.Config);
            }

            // Register with spawn manager for wave tracking
            EnemyController spawnedController = spawnedObj.GetComponent<EnemyController>();
            if (spawnedController != null && spawnManager != null)
            {
                spawnManager.RegisterExternalEnemy(spawnedController);
            }
        }

        if (debugLogging)
        {
            Debug.Log($"[EnemyController] {gameObject.name}: Split into {enemyData.deathSpawnCount} {enemyData.deathSpawnPrefab.name}(s)");
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
    /// Stuns the enemy for the given duration. Called by ParrySystem on successful parry.
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (isDead)
            return;

        stunTimer = duration;
        SetState(EnemyState.Stunned);
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

    [Header("Contact Damage")]
    [SerializeField] private float contactCheckRadius = 0.6f;
    private float contactDamageCooldown;
    private const float ContactDamageInterval = 0.5f;
    private int playerLayerMask;

    /// <summary>
    /// Overlap-based contact damage. Player and Enemy layers don't physically collide
    /// (Physics2D layer matrix), so we detect proximity via OverlapCircle instead of
    /// OnCollisionStay2D. This prevents enemies and player from pushing each other
    /// while still dealing contact damage.
    /// </summary>
    private void CheckContactDamage()
    {
        if (isDead || enemyData == null || enemyData.contactDamage <= 0f)
            return;

        if (contactDamageCooldown > 0f)
        {
            contactDamageCooldown -= Time.fixedDeltaTime;
            return;
        }

        // Check for player overlap using the Player layer
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, contactCheckRadius, playerLayerMask);

        foreach (Collider2D hit in hits)
        {
            if (hit.isTrigger)
                continue;

            if (!hit.CompareTag("Player"))
                continue;

            HealthSystem playerHealth = hit.GetComponent<HealthSystem>();
            if (playerHealth != null && !playerHealth.IsInvulnerable)
            {
                playerHealth.TakeDamage(enemyData.contactDamage);
                contactDamageCooldown = ContactDamageInterval;

                // Spawn damage number for contact damage
                var spawner = DamageNumberSpawner.GetOrCreate();
                if (spawner != null)
                {
                    Vector3 spawnPos = hit.bounds.center + Vector3.up * hit.bounds.extents.y;
                    spawner.SpawnDamage(spawnPos, enemyData.contactDamage, DamageNumberType.Normal, false);
                }

                // Apply knockback to player
                Rigidbody2D playerRb = hit.GetComponent<Rigidbody2D>();
                if (playerRb != null && enemyData.contactKnockbackForce > 0f)
                {
                    Vector2 knockDir = (hit.transform.position - transform.position).normalized;
                    knockDir.y = 0.3f; // Add slight upward component
                    knockDir.Normalize();
                    playerRb.AddForce(knockDir * enemyData.contactKnockbackForce, ForceMode2D.Impulse);
                }
            }
            break; // Only damage one player per check
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
