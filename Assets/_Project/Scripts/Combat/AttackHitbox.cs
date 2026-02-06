using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger-based damage dealing component for melee attacks.
/// Spawned by CombatController during attack active frames.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class AttackHitbox : MonoBehaviour
{
    private AttackData attackData;
    private CombatController owner;
    private BoxCollider2D hitboxCollider;
    private HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0f, 0f, 0.5f);

    /// <summary>
    /// Initialize the hitbox with attack data.
    /// </summary>
    public void Initialize(AttackData attack, CombatController combatController)
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
        int attackLayer = LayerMask.NameToLayer("PlayerAttack");
        if (attackLayer != -1)
        {
            gameObject.layer = attackLayer;
        }
        else
        {
            Debug.LogWarning("AttackHitbox: 'PlayerAttack' layer not found. Create it in Project Settings > Tags and Layers.");
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

        // Skip self
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
    }

    private void ApplyDamage(Collider2D target)
    {
        // Try to find HealthSystem on target
        HealthSystem healthSystem = target.GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            healthSystem = target.GetComponentInParent<HealthSystem>();
        }

        if (healthSystem != null)
        {
            healthSystem.TakeDamage(attackData.baseDamage);
        }

        // Also try IDamageable interface for flexibility
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

        // Calculate knockback direction
        Vector2 knockDir = attackData.knockbackDirection.normalized;

        // Flip horizontal knockback based on attack direction
        if (owner != null)
        {
            float facingDir = Mathf.Sign(owner.transform.localScale.x);

            switch (attackData.direction)
            {
                case AttackDirection.Forward:
                    knockDir.x *= facingDir;
                    break;
                case AttackDirection.Up:
                    knockDir = new Vector2(knockDir.x * facingDir, Mathf.Abs(knockDir.y));
                    break;
                case AttackDirection.Down:
                    knockDir = new Vector2(knockDir.x * facingDir, -Mathf.Abs(knockDir.y));
                    break;
            }
        }

        targetRb.AddForce(knockDir * attackData.knockbackForce, ForceMode2D.Impulse);
    }

    private void SpawnImpactVFX(Collider2D target)
    {
        if (attackData.impactVFXPrefab == null)
            return;

        Vector3 impactPoint = target.ClosestPoint(transform.position);
        Instantiate(attackData.impactVFXPrefab, impactPoint, Quaternion.identity);
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

/// <summary>
/// Interface for objects that can receive damage.
/// Implement this for custom damage handling beyond HealthSystem.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage, AttackData attackData = null);
}
