using UnityEngine;

/// <summary>
/// Skill effect that heals the caster or allies.
/// </summary>
public class HealSkillEffect : BaseSkillEffect
{
    [Header("Heal Settings")]
    [Tooltip("Heal the caster")]
    [SerializeField] private bool healSelf = true;

    [Tooltip("Heal allies in range")]
    [SerializeField] private bool healAllies = false;

    [Tooltip("Radius for ally healing")]
    [SerializeField] private float healRadius = 5f;

    [Tooltip("Layer mask for allies")]
    [SerializeField] private LayerMask allyLayers;

    [Tooltip("Heal over time instead of instant")]
    [SerializeField] private bool healOverTime = false;

    [Tooltip("Interval between HoT ticks")]
    [SerializeField] private float hotInterval = 0.5f;

    [Header("Visual")]
    [Tooltip("Spawn visual on healed targets")]
    [SerializeField] private GameObject healVisualPrefab;

    [Tooltip("Color tint for heal visual")]
    [SerializeField] private Color healColor = new Color(0f, 1f, 0.5f, 1f);

    // Runtime
    private float healAmount;
    private float lastHotTime;
    private int hotTicks;
    private int totalHotTicks;

    protected override void OnInitialized()
    {
        // Get heal amount from effect data or skill damage
        var healData = GetEffectDataByType(SkillEffectData.EffectType.Heal);
        if (healData != null)
        {
            healAmount = healData.GetValue(skillLevel);
        }
        else
        {
            healAmount = damage; // Use damage value as heal amount
        }

        if (healOverTime && duration > 0)
        {
            totalHotTicks = Mathf.CeilToInt(duration / hotInterval);
            healAmount = healAmount / totalHotTicks; // Divide total heal across ticks
            lastHotTime = Time.time;
            hotTicks = 0;
        }
        else
        {
            // Instant heal
            PerformHeal();
        }
    }

    private void Update()
    {
        if (!isInitialized || !healOverTime) return;

        if (hotTicks >= totalHotTicks) return;

        if (Time.time >= lastHotTime + hotInterval)
        {
            lastHotTime = Time.time;
            hotTicks++;
            PerformHeal();
        }
    }

    private void PerformHeal()
    {
        if (healSelf && caster != null)
        {
            HealTarget(caster);
        }

        if (healAllies)
        {
            HealAlliesInRange();
        }
    }

    private void HealTarget(GameObject target)
    {
        var healthSystem = target.GetComponent<HealthSystem>();
        if (healthSystem == null) return;

        float actualHeal = healthSystem.Heal(healAmount);

        if (actualHeal > 0)
        {
            SpawnHealVisual(target.transform.position);
            Debug.Log($"[HealSkillEffect] Healed {target.name} for {actualHeal}");
        }
    }

    private void HealAlliesInRange()
    {
        if (healRadius <= 0) return;

        Collider2D[] allies = Physics2D.OverlapCircleAll(transform.position, healRadius, allyLayers);

        foreach (var ally in allies)
        {
            if (ally != null)
            {
                HealTarget(ally.gameObject);
            }
        }
    }

    private void SpawnHealVisual(Vector3 position)
    {
        if (healVisualPrefab == null) return;

        var visual = Instantiate(healVisualPrefab, position, Quaternion.identity);

        // Apply heal color if there's a sprite renderer
        var sr = visual.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = healColor;
        }

        // Apply color to particles if present
        var ps = visual.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = healColor;
        }

        // Auto-destroy after 1 second
        Destroy(visual, 1f);
    }

    private void OnDrawGizmosSelected()
    {
        if (healAllies && healRadius > 0)
        {
            Gizmos.color = healColor;
            Gizmos.DrawWireSphere(transform.position, healRadius);
        }
    }
}
