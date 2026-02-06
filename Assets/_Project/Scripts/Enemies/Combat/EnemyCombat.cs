using System;
using UnityEngine;

/// <summary>
/// Handles enemy attack execution, hitbox spawning, and attack selection.
/// </summary>
public class EnemyCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackOrigin;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;
    [SerializeField] private bool showHitboxGizmos = true;

    private EnemyController controller;
    private EnemyData enemyData;
    private Animator animator;

    private EnemyAttackData currentAttack;
    private Transform currentTarget;
    private float attackTimer;
    private AttackPhase attackPhase = AttackPhase.None;
    private GameObject activeHitbox;

    private enum AttackPhase
    {
        None,
        WindUp,
        Active,
        Recovery
    }

    public event Action OnAttackComplete;
    public event Action<EnemyAttackData> OnAttackStarted;
    public event Action<EnemyAttackData, Collider2D> OnAttackHit;

    public bool IsAttacking => attackPhase != AttackPhase.None;

    private void Awake()
    {
        controller = GetComponent<EnemyController>();
        animator = GetComponent<Animator>();

        // Auto-create attack origin if not assigned
        if (attackOrigin == null)
        {
            attackOrigin = transform.Find("AttackOrigin");
            if (attackOrigin == null)
            {
                GameObject go = new GameObject("AttackOrigin");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                attackOrigin = go.transform;
            }
        }
    }

    private void Start()
    {
        if (controller != null)
        {
            enemyData = controller.Data;
        }
    }

    private void Update()
    {
        if (attackPhase == AttackPhase.None)
            return;

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            AdvanceAttackPhase();
        }
    }

    /// <summary>
    /// Start an attack against the specified target.
    /// </summary>
    public void StartAttack(Transform target)
    {
        if (enemyData == null || enemyData.attacks == null || enemyData.attacks.Length == 0)
        {
            if (debugLogging)
                Debug.LogWarning($"[EnemyCombat] {gameObject.name}: No attacks configured!");

            OnAttackComplete?.Invoke();
            return;
        }

        currentTarget = target;

        // Select appropriate attack based on range
        currentAttack = SelectAttack(target);

        if (currentAttack == null)
        {
            if (debugLogging)
                Debug.LogWarning($"[EnemyCombat] {gameObject.name}: No valid attack for range!");

            OnAttackComplete?.Invoke();
            return;
        }

        if (debugLogging)
            Debug.Log($"[EnemyCombat] {gameObject.name}: Starting attack '{currentAttack.attackName}'");

        // Face the target
        controller?.FaceTarget();

        // Start wind-up phase
        attackPhase = AttackPhase.WindUp;
        attackTimer = currentAttack.windUpDuration;

        // Spawn wind-up VFX
        if (currentAttack.windUpVFX != null)
        {
            Instantiate(currentAttack.windUpVFX, attackOrigin.position, Quaternion.identity, transform);
        }

        // Play attack animation
        if (animator != null && !string.IsNullOrEmpty(currentAttack.animationTrigger))
        {
            animator.SetTrigger(currentAttack.animationTrigger);
        }

        OnAttackStarted?.Invoke(currentAttack);

        // Skip wind-up if duration is 0
        if (attackTimer <= 0f)
        {
            AdvanceAttackPhase();
        }
    }

    /// <summary>
    /// Cancel the current attack.
    /// </summary>
    public void CancelAttack()
    {
        if (attackPhase == AttackPhase.None)
            return;

        if (debugLogging)
            Debug.Log($"[EnemyCombat] {gameObject.name}: Attack cancelled");

        DestroyActiveHitbox();
        attackPhase = AttackPhase.None;
        currentAttack = null;
        currentTarget = null;
    }

    private void AdvanceAttackPhase()
    {
        switch (attackPhase)
        {
            case AttackPhase.WindUp:
                EnterActivePhase();
                break;

            case AttackPhase.Active:
                EnterRecoveryPhase();
                break;

            case AttackPhase.Recovery:
                CompleteAttack();
                break;
        }
    }

    private void EnterActivePhase()
    {
        attackPhase = AttackPhase.Active;
        attackTimer = currentAttack.activeDuration;

        // Play attack sound
        if (currentAttack.attackSound != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(currentAttack.attackSound);
            }
        }

        // Spawn hitbox or projectile
        if (currentAttack.isProjectile)
        {
            SpawnProjectile();
        }
        else
        {
            SpawnHitbox();
        }

        // Spawn attack VFX
        if (currentAttack.attackVFX != null)
        {
            Instantiate(currentAttack.attackVFX, attackOrigin.position, Quaternion.identity);
        }
    }

    private void EnterRecoveryPhase()
    {
        attackPhase = AttackPhase.Recovery;
        attackTimer = currentAttack.recoveryDuration;

        DestroyActiveHitbox();
    }

    private void CompleteAttack()
    {
        if (debugLogging)
            Debug.Log($"[EnemyCombat] {gameObject.name}: Attack complete");

        attackPhase = AttackPhase.None;
        currentAttack = null;
        currentTarget = null;

        OnAttackComplete?.Invoke();
    }

    private EnemyAttackData SelectAttack(Transform target)
    {
        if (target == null)
            return enemyData.attacks[0];

        float distance = Vector2.Distance(transform.position, target.position);

        // Find attack that matches the current range
        foreach (EnemyAttackData attack in enemyData.attacks)
        {
            if (distance >= attack.minRange && distance <= attack.maxRange)
            {
                return attack;
            }
        }

        // Default to first attack if no range match
        return enemyData.attacks[0];
    }

    private void SpawnHitbox()
    {
        Vector2 offset = CalculateHitboxOffset();
        Vector3 spawnPos = attackOrigin.position + (Vector3)offset;

        if (debugLogging)
            Debug.Log($"[EnemyCombat] Spawning hitbox at {spawnPos}, size: {currentAttack.hitboxSize}");

        // Create hitbox GameObject
        GameObject hitboxObj = new GameObject("EnemyAttackHitbox");
        hitboxObj.transform.position = spawnPos;
        hitboxObj.transform.SetParent(transform);

        // Setup hitbox component
        EnemyAttackHitbox hitbox = hitboxObj.AddComponent<EnemyAttackHitbox>();
        hitbox.Initialize(currentAttack, this);

        activeHitbox = hitboxObj;
    }

    private void SpawnProjectile()
    {
        if (currentAttack.projectilePrefab == null)
        {
            Debug.LogWarning($"[EnemyCombat] {gameObject.name}: Projectile prefab not assigned!");
            return;
        }

        Vector2 offset = CalculateHitboxOffset();
        Vector3 spawnPos = attackOrigin.position + (Vector3)offset;

        // Calculate direction to target
        Vector2 direction;
        if (currentTarget != null)
        {
            direction = ((Vector2)currentTarget.position - (Vector2)spawnPos).normalized;
        }
        else
        {
            direction = new Vector2(controller.FacingDirection, 0f);
        }

        Quaternion rotation = Quaternion.FromToRotation(Vector2.right, direction);
        GameObject projectileObj = Instantiate(currentAttack.projectilePrefab, spawnPos, rotation);

        // Setup projectile component
        EnemyProjectile projectile = projectileObj.GetComponent<EnemyProjectile>();
        if (projectile == null)
        {
            projectile = projectileObj.AddComponent<EnemyProjectile>();
        }

        projectile.Initialize(currentAttack, direction);
    }

    private Vector2 CalculateHitboxOffset()
    {
        Vector2 offset = currentAttack.hitboxOffset;
        float facingDir = controller != null ? controller.FacingDirection : 1f;

        // Flip offset based on facing direction
        offset.x *= facingDir;

        return offset;
    }

    private void DestroyActiveHitbox()
    {
        if (activeHitbox != null)
        {
            Destroy(activeHitbox);
            activeHitbox = null;
        }
    }

    /// <summary>
    /// Called by EnemyAttackHitbox when it hits a target.
    /// </summary>
    public void ReportHit(EnemyAttackData attack, Collider2D target)
    {
        OnAttackHit?.Invoke(attack, target);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showHitboxGizmos)
            return;

        if (enemyData == null || enemyData.attacks == null || enemyData.attacks.Length == 0)
            return;

        // Draw attack hitbox preview for first attack
        EnemyAttackData previewAttack = currentAttack ?? enemyData.attacks[0];
        if (previewAttack == null)
            return;

        Transform origin = attackOrigin != null ? attackOrigin : transform;
        Vector2 offset = previewAttack.hitboxOffset;
        float facingDir = transform.localScale.x >= 0 ? 1f : -1f;
        offset.x *= facingDir;

        Vector3 center = origin.position + (Vector3)offset;

        Gizmos.color = IsAttacking ? Color.red : new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireCube(center, previewAttack.hitboxSize);
    }
}
