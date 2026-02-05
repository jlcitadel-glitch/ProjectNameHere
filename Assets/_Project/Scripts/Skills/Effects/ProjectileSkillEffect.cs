using UnityEngine;

/// <summary>
/// Skill effect that travels as a projectile.
/// Deals damage on collision with valid targets.
/// </summary>
public class ProjectileSkillEffect : BaseSkillEffect
{
    [Header("Projectile Settings")]
    [Tooltip("Projectile speed")]
    [SerializeField] private float speed = 15f;

    [Tooltip("Maximum travel distance before despawning")]
    [SerializeField] private float maxDistance = 20f;

    [Tooltip("Piercing (passes through targets)")]
    [SerializeField] private bool piercing = false;

    [Tooltip("Maximum targets to pierce (-1 for unlimited)")]
    [SerializeField] private int maxPierceTargets = -1;

    [Tooltip("Homing towards targets")]
    [SerializeField] private bool homing = false;

    [Tooltip("Homing turn speed")]
    [SerializeField] private float homingTurnSpeed = 180f;

    [Tooltip("Homing detection range")]
    [SerializeField] private float homingRange = 10f;

    [Header("Collision")]
    [Tooltip("Layer mask for valid targets")]
    [SerializeField] private LayerMask targetLayers;

    [Tooltip("Layer mask for obstacles")]
    [SerializeField] private LayerMask obstacleLayer;

    [Tooltip("Tags that can be damaged")]
    [SerializeField] private string[] targetTags = { "Enemy" };

    [Header("Knockback")]
    [Tooltip("Apply knockback on hit")]
    [SerializeField] private bool applyKnockback = true;

    [Tooltip("Knockback force")]
    [SerializeField] private float knockbackForce = 3f;

    [Header("Impact")]
    [Tooltip("Prefab spawned on impact")]
    [SerializeField] private GameObject impactPrefab;

    [Tooltip("Sound played on impact")]
    [SerializeField] private AudioClip impactSound;

    // Runtime
    private Vector3 startPosition;
    private Vector2 direction;
    private int pierceCount;
    private Transform homingTarget;
    private Rigidbody2D rb;

    protected override void OnInitialized()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();

        // Get direction from caster facing
        if (caster != null)
        {
            var sr = caster.GetComponent<SpriteRenderer>();
            direction = sr != null && sr.flipX ? Vector2.left : Vector2.right;
        }
        else
        {
            direction = transform.right;
        }

        // Override speed from effect data if available
        var effectData = GetEffectDataByType(SkillEffectData.EffectType.Projectile);
        if (effectData != null)
        {
            speed = effectData.projectileSpeed;
            piercing = effectData.piercing;
        }

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Check max distance
        float distanceTraveled = Vector3.Distance(startPosition, transform.position);
        if (distanceTraveled >= maxDistance)
        {
            DestroyProjectile(false);
            return;
        }

        // Handle homing
        if (homing)
        {
            UpdateHoming();
        }

        // Move if no rigidbody
        if (rb == null)
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }
    }

    private void UpdateHoming()
    {
        // Find target if we don't have one
        if (homingTarget == null)
        {
            FindHomingTarget();
        }

        if (homingTarget == null) return;

        // Calculate turn towards target
        Vector2 toTarget = (homingTarget.position - transform.position).normalized;
        float angleDiff = Vector2.SignedAngle(direction, toTarget);
        float maxTurn = homingTurnSpeed * Time.deltaTime;

        float turn = Mathf.Clamp(angleDiff, -maxTurn, maxTurn);
        direction = Quaternion.Euler(0, 0, turn) * direction;

        // Update velocity
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        // Update rotation to match direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void FindHomingTarget()
    {
        Collider2D[] potentialTargets = Physics2D.OverlapCircleAll(transform.position, homingRange, targetLayers);

        float closestDist = float.MaxValue;
        Transform closest = null;

        foreach (var target in potentialTargets)
        {
            if (!IsValidTarget(target)) continue;

            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = target.transform;
            }
        }

        homingTarget = closest;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized) return;

        // Check obstacle collision
        if ((obstacleLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            DestroyProjectile(true);
            return;
        }

        // Check valid target
        if (!IsValidTarget(other)) return;

        DealDamageToTarget(other);

        // Handle piercing
        if (piercing)
        {
            pierceCount++;
            if (maxPierceTargets > 0 && pierceCount >= maxPierceTargets)
            {
                DestroyProjectile(true);
            }
        }
        else
        {
            DestroyProjectile(true);
        }
    }

    private bool IsValidTarget(Collider2D target)
    {
        if (target == null) return false;
        if (target.isTrigger) return false;
        if (target.gameObject == caster) return false;

        foreach (var tag in targetTags)
        {
            if (target.CompareTag(tag))
                return true;
        }

        return false;
    }

    private void DealDamageToTarget(Collider2D target)
    {
        var healthSystem = target.GetComponent<HealthSystem>();

        if (healthSystem != null)
        {
            healthSystem.TakeDamage(damage);

            // Apply knockback
            if (applyKnockback)
            {
                var rb = target.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
                }
            }

            Debug.Log($"[ProjectileSkillEffect] Hit {target.name} for {damage} damage");
        }
    }

    private void DestroyProjectile(bool showImpact)
    {
        if (showImpact)
        {
            // Spawn impact effect
            if (impactPrefab != null)
            {
                Instantiate(impactPrefab, transform.position, Quaternion.identity);
            }

            // Play impact sound
            if (impactSound != null)
            {
                AudioSource.PlayClipAtPoint(impactSound, transform.position);
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, direction * 2f);

        if (homing)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, homingRange);
        }
    }
}
