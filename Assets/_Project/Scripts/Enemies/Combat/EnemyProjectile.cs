using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy projectile that travels in a direction and deals damage on contact.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    private EnemyAttackData attackData;
    private Vector2 direction;
    private Rigidbody2D rb;
    private HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();
    private float lifetime;

    public void Initialize(EnemyAttackData attack, Vector2 moveDirection)
    {
        attackData = attack;
        direction = moveDirection.normalized;

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = direction * attack.projectileSpeed;

        lifetime = attack.projectileLifetime;

        // Set layer
        gameObject.layer = LayerMask.NameToLayer("EnemyAttack");
        if (gameObject.layer == -1)
        {
            Debug.LogWarning("EnemyProjectile: 'EnemyAttack' layer not found.");
            gameObject.layer = 0;
        }

        // Ensure collider is trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (debugLogging)
        {
            Debug.Log($"[EnemyProjectile] Initialized: speed={attack.projectileSpeed}, direction={direction}");
        }
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            DestroyProjectile();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Skip if already hit this target
        if (hitTargets.Contains(other))
            return;

        // Skip triggers
        if (other.isTrigger)
            return;

        // Check if we hit a wall/ground
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            SpawnImpactVFX(other);
            DestroyProjectile();
            return;
        }

        // Check if target is on valid layer
        if (attackData.targetLayers != 0)
        {
            int otherLayer = 1 << other.gameObject.layer;
            if ((attackData.targetLayers & otherLayer) == 0)
                return;
        }

        // Only hit player
        if (!other.CompareTag("Player"))
            return;

        // Mark as hit
        hitTargets.Add(other);

        // Apply damage
        ApplyDamage(other);

        // Apply knockback
        ApplyKnockback(other);

        // Spawn impact VFX
        SpawnImpactVFX(other);

        // Destroy projectile after hit
        DestroyProjectile();
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

            if (debugLogging)
            {
                Debug.Log($"[EnemyProjectile] Dealt {attackData.baseDamage} damage to {target.name}");
            }
        }
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
        knockDir.y = Mathf.Max(knockDir.y, 0.2f); // Add slight upward component

        targetRb.AddForce(knockDir.normalized * attackData.knockbackForce, ForceMode2D.Impulse);
    }

    private void SpawnImpactVFX(Collider2D target)
    {
        if (attackData.impactVFX == null)
            return;

        Vector3 impactPoint = target.ClosestPoint(transform.position);
        Instantiate(attackData.impactVFX, impactPoint, Quaternion.identity);
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}
