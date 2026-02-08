using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger-based damage delivery component for enemy melee attacks.
/// Spawned by EnemyCombat during attack active frames.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class EnemyAttackHitbox : MonoBehaviour
{
    private EnemyAttackData attackData;
    private EnemyCombat owner;
    private BoxCollider2D hitboxCollider;
    private HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.5f, 0f, 0.5f);

    /// <summary>
    /// Initialize the hitbox with attack data.
    /// </summary>
    public void Initialize(EnemyAttackData attack, EnemyCombat combatController)
    {
        attackData = attack;
        owner = combatController;

        SetupCollider();
    }

    private void SetupCollider()
    {
        hitboxCollider = GetComponent<BoxCollider2D>();
        if (hitboxCollider == null)
        {
            hitboxCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        hitboxCollider.isTrigger = true;
        hitboxCollider.size = attackData.hitboxSize;

        // Set layer for collision filtering
        int attackLayer = LayerMask.NameToLayer("EnemyAttack");
        if (attackLayer >= 0)
        {
            gameObject.layer = attackLayer;
        }
        else
        {
            // Fall back to parent's layer so it still exists in physics
            gameObject.layer = owner != null ? owner.gameObject.layer : 0;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Skip if already hit this target
        if (hitTargets.Contains(other))
            return;

        // Check if target is on valid layer
        if (attackData.targetLayers != 0)
        {
            int otherLayer = 1 << other.gameObject.layer;
            if ((attackData.targetLayers & otherLayer) == 0)
                return;
        }

        // Skip triggers
        if (other.isTrigger)
            return;

        // Skip self and other enemies
        if (owner != null && other.transform.IsChildOf(owner.transform))
            return;

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

        // Report hit to owner
        owner?.ReportHit(attackData, other);
    }

    private void ApplyDamage(Collider2D target)
    {
        // Apply boss phase damage multiplier
        float damageMultiplier = owner != null ? owner.GetDamageMultiplier() : 1f;
        float finalDamage = attackData.baseDamage * damageMultiplier;

        // Try to find HealthSystem on target
        HealthSystem healthSystem = target.GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            healthSystem = target.GetComponentInParent<HealthSystem>();
        }

        if (healthSystem != null)
        {
            healthSystem.TakeDamage(finalDamage);
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

        // Calculate knockback direction
        Vector2 knockDir = attackData.knockbackDirection.normalized;

        // Flip horizontal knockback based on enemy facing direction
        if (owner != null)
        {
            float facingDir = owner.transform.localScale.x >= 0 ? 1f : -1f;
            knockDir.x *= facingDir;
        }
        else
        {
            // Knockback away from hitbox position
            float direction = Mathf.Sign(target.transform.position.x - transform.position.x);
            knockDir.x *= direction;
        }

        targetRb.AddForce(knockDir * attackData.knockbackForce, ForceMode2D.Impulse);
    }

    private void SpawnImpactVFX(Collider2D target)
    {
        if (attackData.impactVFX == null)
            return;

        Vector3 impactPoint = target.ClosestPoint(transform.position);
        Instantiate(attackData.impactVFX, impactPoint, Quaternion.identity);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        Gizmos.color = gizmoColor;

        Vector3 size = attackData != null ? (Vector3)attackData.hitboxSize : Vector3.one;
        Gizmos.DrawCube(transform.position, size);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, size);
    }
}
