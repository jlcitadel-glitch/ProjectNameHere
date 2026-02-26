using UnityEngine;
using ProjectName.UI;

/// <summary>
/// Lightweight runtime projectile for skill-based attacks.
/// Created at runtime by SkillExecutor — no prefab required.
/// </summary>
public class SkillProjectile : MonoBehaviour
{
    private float damage;
    private DamageType damageType;
    private bool isCrit;
    private float speed;
    private Vector2 direction;
    private GameObject caster;
    private LayerMask targetLayers;

    // Optional: slow effect for ice_bolt
    private float slowPercent;
    private float slowDuration;

    private Rigidbody2D rb;
    private bool initialized;

    /// <summary>
    /// Configures the projectile. Must be called immediately after creation.
    /// </summary>
    public void Initialize(float damage, DamageType damageType, bool isCrit,
        float speed, float lifetime, Vector2 direction, GameObject caster,
        LayerMask targetLayers, float slowPercent = 0f, float slowDuration = 0f)
    {
        this.damage = damage;
        this.damageType = damageType;
        this.isCrit = isCrit;
        this.speed = speed;
        this.direction = direction.normalized;
        this.caster = caster;
        this.targetLayers = targetLayers;
        this.slowPercent = slowPercent;
        this.slowDuration = slowDuration;

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = this.direction * speed;
        }

        // Tint sprite by damage type
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var (primary, _) = SkillVFXFactory.GetColors(damageType);
            sr.color = primary;
        }

        Destroy(gameObject, lifetime);
        initialized = true;
    }

    private void FixedUpdate()
    {
        if (!initialized || rb == null) return;
        // Maintain velocity (in case something alters it)
        rb.linearVelocity = direction * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!initialized) return;

        // Skip caster
        if (caster != null && other.transform.IsChildOf(caster.transform))
            return;

        // Skip other triggers
        if (other.isTrigger) return;

        // Check layer mask
        if (targetLayers != 0)
        {
            int otherLayer = 1 << other.gameObject.layer;
            if ((targetLayers & otherLayer) == 0)
            {
                // Allow IDamageable targets regardless of layer
                IDamageable fallback = other.GetComponent<IDamageable>()
                    ?? other.GetComponentInParent<IDamageable>();
                if (fallback == null)
                {
                    // Hit a wall or obstacle — impact + destroy
                    SkillVFXFactory.SpawnImpactBurst(transform.position, damageType);
                    Destroy(gameObject);
                    return;
                }
            }
        }

        // Apply damage + impact VFX
        ApplyDamage(other);
        SkillVFXFactory.SpawnImpactBurst(other.bounds.center, damageType);
        Destroy(gameObject);
    }

    private void ApplyDamage(Collider2D target)
    {
        // Try IDamageable first
        IDamageable damageable = target.GetComponent<IDamageable>()
            ?? target.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
        else
        {
            // Fallback to HealthSystem
            HealthSystem hs = target.GetComponent<HealthSystem>()
                ?? target.GetComponentInParent<HealthSystem>();
            if (hs != null)
            {
                hs.TakeDamage(damage);
            }
        }

        // Spawn damage number
        SpawnDamageNumber(target);

        // Apply slow if configured (ice_bolt)
        if (slowPercent > 0f && slowDuration > 0f)
        {
            ApplySlow(target);
        }
    }

    private void SpawnDamageNumber(Collider2D target)
    {
        var spawner = DamageNumberSpawner.GetOrCreate();
        if (spawner == null) return;

        Vector3 spawnPos = target.bounds.center + Vector3.up * target.bounds.extents.y;
        spawner.SpawnDamageWithType(spawnPos, damage, damageType, isCrit);
    }

    private void ApplySlow(Collider2D target)
    {
        // Apply movement slow via Rigidbody2D velocity reduction
        // Enemies with custom AI will need their own slow handler in the future
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>()
            ?? target.GetComponentInParent<Rigidbody2D>();
        if (targetRb == null) return;

        float originalGravity = targetRb.gravityScale;
        float slowFactor = 1f - slowPercent;

        // Reduce current velocity
        targetRb.linearVelocity *= slowFactor;

        // Spawn "Slow" text indicator
        var spawner = DamageNumberSpawner.GetOrCreate();
        if (spawner != null)
        {
            Vector3 pos = target.bounds.center + Vector3.up * target.bounds.extents.y;
            spawner.SpawnText(pos, "Slow", new Color(0.5f, 0.8f, 1f, 1f));
        }
    }

    /// <summary>
    /// Creates a runtime projectile GameObject with all required components.
    /// </summary>
    public static SkillProjectile Create(Vector3 position, Vector2 direction)
    {
        var go = new GameObject("SkillProjectile");
        go.transform.position = position;

        // Rotate to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.5f, 0.3f);

        // Set to PlayerAttack layer if available
        int layer = LayerMask.NameToLayer("PlayerAttack");
        if (layer != -1)
            go.layer = layer;

        var projectile = go.AddComponent<SkillProjectile>();

        // Add a simple sprite renderer for visibility
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = Color.white;
        sr.sortingOrder = 10;

        return projectile;
    }
}
