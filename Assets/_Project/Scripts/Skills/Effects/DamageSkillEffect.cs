using System.Collections.Generic;
using UnityEngine;
using ProjectName.UI;

/// <summary>
/// Skill effect that deals damage to enemies in range.
/// Can be configured for instant, area, or lingering damage.
/// </summary>
public class DamageSkillEffect : BaseSkillEffect
{
    [Header("Damage Settings")]
    [Tooltip("Radius for area damage (0 for single target)")]
    [SerializeField] private float damageRadius = 2f;

    [Tooltip("Layer mask for valid targets")]
    [SerializeField] private LayerMask targetLayers;

    [Tooltip("Tags that can be damaged")]
    [SerializeField] private string[] targetTags = { "Enemy" };

    [Tooltip("Deal damage on spawn")]
    [SerializeField] private bool damageOnSpawn = true;

    [Tooltip("Deal damage on trigger enter")]
    [SerializeField] private bool damageOnTrigger = false;

    [Tooltip("Deal damage over time")]
    [SerializeField] private bool damageOverTime = false;

    [Tooltip("Interval between DoT ticks")]
    [SerializeField] private float dotInterval = 0.5f;

    [Header("Knockback")]
    [Tooltip("Apply knockback on damage")]
    [SerializeField] private bool applyKnockback = false;

    [Tooltip("Knockback force")]
    [SerializeField] private float knockbackForce = 5f;

    [Header("Visual Feedback")]
    [Tooltip("Flash color on hit")]
    [SerializeField] private Color hitFlashColor = Color.white;

    [Tooltip("Spawn hit effect prefab")]
    [SerializeField] private GameObject hitEffectPrefab;

    private HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();
    private float lastDotTime;

    protected override void OnInitialized()
    {
        if (damageOnSpawn)
        {
            DealAreaDamage();
        }

        lastDotTime = Time.time;
    }

    private void Update()
    {
        if (!isInitialized) return;

        if (damageOverTime)
        {
            if (Time.time >= lastDotTime + dotInterval)
            {
                lastDotTime = Time.time;
                DealAreaDamage();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized || !damageOnTrigger) return;

        if (hitTargets.Contains(other)) return;

        if (IsValidTarget(other))
        {
            DealDamageToTarget(other);
            hitTargets.Add(other);
        }
    }

    /// <summary>
    /// Deals damage to all valid targets in radius.
    /// </summary>
    public void DealAreaDamage()
    {
        if (damageRadius <= 0)
        {
            // Single target - raycast forward
            RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.right, 1f, targetLayers);
            if (hit.collider != null && IsValidTarget(hit.collider))
            {
                DealDamageToTarget(hit.collider);
            }
            return;
        }

        // Area damage
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, damageRadius, targetLayers);

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit.gameObject == caster) continue;

            if (IsValidTarget(hit))
            {
                DealDamageToTarget(hit);
            }
        }
    }

    private bool IsValidTarget(Collider2D target)
    {
        if (target == null) return false;
        if (target.isTrigger) return false;
        if (target.gameObject == caster) return false;

        // Check tags
        foreach (var tag in targetTags)
        {
            if (target.CompareTag(tag))
                return true;
        }

        return false;
    }

    private void DealDamageToTarget(Collider2D target)
    {
        // Try to find a health system on the target
        var targetHealth = target.GetComponent<HealthSystem>();

        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage);

            // Spawn floating damage number
            if (DamageNumberSpawner.Instance != null)
            {
                Vector3 spawnPos = target.bounds.center + Vector3.up * target.bounds.extents.y;
                DamageNumberSpawner.Instance.SpawnDamageWithType(spawnPos, damage, damageType, false);
            }

            // Spawn hit effect
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, target.transform.position, Quaternion.identity);
            }

            // Apply knockback
            if (applyKnockback)
            {
                ApplyKnockbackToTarget(target);
            }

            Debug.Log($"[DamageSkillEffect] Dealt {damage} {damageType} damage to {target.name}");
        }
    }

    private void ApplyKnockbackToTarget(Collider2D target)
    {
        var rb = target.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        Vector2 direction = (target.transform.position - transform.position).normalized;
        rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
    }

    private void OnDrawGizmosSelected()
    {
        if (damageRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, damageRadius);
        }
    }
}
