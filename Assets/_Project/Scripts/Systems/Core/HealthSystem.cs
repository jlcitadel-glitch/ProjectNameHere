using System;
using UnityEngine;

/// <summary>
/// Core health system component for the Player.
/// Handles health pool, damage, healing, and death.
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float invulnerabilityDuration = 0.5f;

    private float currentHealth;
    private float invulnerabilityTimer;
    private Animator animator;
    private bool animationTriggersEnabled = true;
    private static readonly int AnimHurt = Animator.StringToHash("Hurt");
    private static readonly int AnimDie = Animator.StringToHash("Die");

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool IsFull => currentHealth >= maxHealth;
    public bool IsDead => currentHealth <= 0f;
    public bool IsInvulnerable => invulnerabilityTimer > 0f;

    /// <summary>
    /// Fired when health changes. Provides current and max values.
    /// </summary>
    public event Action<float, float> OnHealthChanged;

    /// <summary>
    /// Fired when health reaches maximum.
    /// </summary>
    public event Action OnHealthFull;

    /// <summary>
    /// Fired when health is depleted.
    /// </summary>
    public event Action OnDeath;

    /// <summary>
    /// Fired when damage is taken. Provides damage amount.
    /// </summary>
    public event Action<float> OnDamageTaken;

    /// <summary>
    /// Fired when healed. Provides heal amount.
    /// </summary>
    public event Action<float> OnHealed;

    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Applies damage to the health system. Returns actual damage dealt.
    /// </summary>
    public float TakeDamage(float amount, bool ignoreInvulnerability = false)
    {
        if (amount <= 0f || IsDead)
            return 0f;

        if (IsInvulnerable && !ignoreInvulnerability)
            return 0f;

        float actualDamage = Mathf.Min(amount, currentHealth);
        currentHealth -= actualDamage;

        invulnerabilityTimer = invulnerabilityDuration;

        // Trigger hurt animation (disabled for enemies — EnemyController handles it)
        if (animationTriggersEnabled && animator != null && currentHealth > 0f)
        {
            animator.SetTrigger(AnimHurt);
        }

        OnDamageTaken?.Invoke(actualDamage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            // Trigger death animation (disabled for enemies — EnemyController handles it)
            if (animationTriggersEnabled && animator != null)
            {
                animator.SetTrigger(AnimDie);
            }
            OnDeath?.Invoke();
        }

        return actualDamage;
    }

    /// <summary>
    /// Heals the health system. Returns actual amount healed.
    /// </summary>
    public float Heal(float amount)
    {
        if (amount <= 0f || IsDead)
            return 0f;

        float actualHeal = Mathf.Min(amount, maxHealth - currentHealth);

        if (actualHeal <= 0f)
            return 0f;

        bool wasFull = IsFull;
        currentHealth += actualHeal;

        OnHealed?.Invoke(actualHeal);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (!wasFull && IsFull)
        {
            OnHealthFull?.Invoke();
        }

        return actualHeal;
    }

    /// <summary>
    /// Instantly refills health to maximum.
    /// </summary>
    public void RefillHealth()
    {
        bool wasFull = IsFull;
        currentHealth = maxHealth;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (!wasFull)
        {
            OnHealthFull?.Invoke();
        }
    }

    /// <summary>
    /// Revives from death with specified health amount.
    /// </summary>
    public void Revive(float healthAmount = -1f)
    {
        if (!IsDead)
            return;

        currentHealth = healthAmount > 0f ? Mathf.Min(healthAmount, maxHealth) : maxHealth;
        invulnerabilityTimer = invulnerabilityDuration;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Sets maximum health (useful for upgrades).
    /// </summary>
    public void SetMaxHealth(float newMax, bool refill = false)
    {
        maxHealth = Mathf.Max(1f, newMax);

        if (refill)
        {
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Grants temporary invulnerability.
    /// </summary>
    public void GrantInvulnerability(float duration)
    {
        invulnerabilityTimer = Mathf.Max(invulnerabilityTimer, duration);
    }

    /// <summary>
    /// Sets the invulnerability duration after taking damage.
    /// </summary>
    public void SetInvulnerabilityDuration(float duration)
    {
        invulnerabilityDuration = Mathf.Max(0f, duration);
    }

    /// <summary>
    /// Disables HealthSystem's built-in animation triggers (Hurt/Die).
    /// Call this when another component (e.g. EnemyController) handles animations.
    /// </summary>
    public void DisableAnimationTriggers()
    {
        animationTriggersEnabled = false;
    }
}
