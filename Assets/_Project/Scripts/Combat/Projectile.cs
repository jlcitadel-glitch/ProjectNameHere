using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projectile component for ranged and magic attacks.
/// Spawned by CombatController and travels in a direction.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private int maxPierceCount = 0;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private AttackData attackData;
    private CombatController owner;
    private Vector2 direction;
    private Rigidbody2D rb;
    private BoxCollider2D projectileCollider;
    private HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();
    private int pierceCount = 0;
    private float lifetimeTimer;

    /// <summary>
    /// Initialize the projectile with attack data and direction.
    /// </summary>
    public void Initialize(AttackData attack, Vector2 dir, CombatController combatController)
    {
        attackData = attack;
        direction = dir.normalized;
        owner = combatController;

        SetupComponents();
        lifetimeTimer = lifetime;
    }

    private void SetupComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.gravityScale = 0f;
        rb.linearVelocity = direction * attackData.projectileSpeed;

        projectileCollider = GetComponent<BoxCollider2D>();
        if (projectileCollider == null)
        {
            projectileCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        projectileCollider.isTrigger = true;
        projectileCollider.size = attackData.hitboxSize;

        // Set layer
        gameObject.layer = LayerMask.NameToLayer("PlayerAttack");
        if (gameObject.layer == -1)
        {
            gameObject.layer = 0;
        }
    }

    private void Update()
    {
        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            DestroyProjectile();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Skip if already hit this target
        if (hitTargets.Contains(other))
            return;

        // Check layer mask
        if (attackData.targetLayers != 0)
        {
            int otherLayer = 1 << other.gameObject.layer;
            if ((attackData.targetLayers & otherLayer) == 0)
            {
                // Check if it's a wall/obstacle (non-enemy solid)
                if (!other.isTrigger)
                {
                    DestroyProjectile();
                }
                return;
            }
        }

        // Skip triggers
        if (other.isTrigger)
            return;

        // Skip owner
        if (owner != null && other.transform.IsChildOf(owner.transform))
            return;

        // Mark as hit
        hitTargets.Add(other);

        // Apply damage
        ApplyDamage(other);

        // Apply knockback
        ApplyKnockback(other);

        // Spawn impact VFX
        SpawnImpactVFX(other);

        // Report hit to owner
        owner?.ReportHit(attackData, other);

        // Handle pierce or destroy
        if (destroyOnHit)
        {
            if (maxPierceCount > 0 && pierceCount < maxPierceCount)
            {
                pierceCount++;
            }
            else
            {
                DestroyProjectile();
            }
        }
    }

    private void ApplyDamage(Collider2D target)
    {
        HealthSystem healthSystem = target.GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            healthSystem = target.GetComponentInParent<HealthSystem>();
        }

        if (healthSystem != null)
        {
            healthSystem.TakeDamage(attackData.baseDamage);
        }

        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable == null)
        {
            damageable = target.GetComponentInParent<IDamageable>();
        }

        damageable?.TakeDamage(attackData.baseDamage, attackData);
    }

    private void ApplyKnockback(Collider2D target)
    {
        if (attackData.knockbackForce <= 0f)
            return;

        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb == null)
        {
            targetRb = target.GetComponentInParent<Rigidbody2D>();
        }

        if (targetRb == null)
            return;

        // Knockback in projectile direction
        Vector2 knockDir = direction;
        knockDir.y = Mathf.Max(knockDir.y, 0.3f); // Add some upward force

        targetRb.AddForce(knockDir.normalized * attackData.knockbackForce, ForceMode2D.Impulse);
    }

    private void SpawnImpactVFX(Collider2D target)
    {
        if (attackData.impactVFXPrefab == null)
            return;

        Vector3 impactPoint = target.ClosestPoint(transform.position);
        Instantiate(attackData.impactVFXPrefab, impactPoint, Quaternion.identity);
    }

    private void DestroyProjectile()
    {
        // Could spawn destruction VFX here
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        Gizmos.color = Color.cyan;
        Vector3 size = attackData != null ? (Vector3)attackData.hitboxSize : Vector3.one * 0.5f;
        Gizmos.DrawWireCube(transform.position, size);

        // Draw direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, direction * 2f);
    }
}
