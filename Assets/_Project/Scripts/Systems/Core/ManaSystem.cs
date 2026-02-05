using System;
using UnityEngine;

/// <summary>
/// Core mana system component for the Player.
/// Handles mana pool, spending, restoration, and passive regeneration.
/// </summary>
public class ManaSystem : MonoBehaviour
{
    [Header("Mana Settings")]
    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float regenRate = 5f;
    [SerializeField] private float regenDelay = 1f;

    private float currentMana;
    private float regenDelayTimer;

    public float CurrentMana => currentMana;
    public float MaxMana => maxMana;
    public float ManaPercent => maxMana > 0f ? currentMana / maxMana : 0f;
    public bool IsFull => currentMana >= maxMana;
    public bool IsEmpty => currentMana <= 0f;

    /// <summary>
    /// Fired when mana changes. Provides current and max values.
    /// </summary>
    public event Action<float, float> OnManaChanged;

    /// <summary>
    /// Fired when mana reaches maximum.
    /// </summary>
    public event Action OnManaFull;

    /// <summary>
    /// Fired when mana is depleted.
    /// </summary>
    public event Action OnManaEmpty;

    private void Awake()
    {
        currentMana = maxMana;
    }

    private void Start()
    {
        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    private void Update()
    {
        if (regenDelayTimer > 0f)
        {
            regenDelayTimer -= Time.deltaTime;
            return;
        }

        if (currentMana < maxMana)
        {
            float previousMana = currentMana;
            currentMana = Mathf.Min(currentMana + regenRate * Time.deltaTime, maxMana);

            if (currentMana != previousMana)
            {
                OnManaChanged?.Invoke(currentMana, maxMana);

                if (currentMana >= maxMana)
                {
                    OnManaFull?.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// Attempts to spend mana. Returns true if successful.
    /// </summary>
    public bool SpendMana(float amount)
    {
        if (amount <= 0f)
            return true;

        if (currentMana < amount)
            return false;

        currentMana -= amount;
        regenDelayTimer = regenDelay;

        OnManaChanged?.Invoke(currentMana, maxMana);

        if (currentMana <= 0f)
        {
            OnManaEmpty?.Invoke();
        }

        return true;
    }

    /// <summary>
    /// Restores mana by the specified amount.
    /// </summary>
    public void RestoreMana(float amount)
    {
        if (amount <= 0f)
            return;

        bool wasFull = IsFull;
        currentMana = Mathf.Min(currentMana + amount, maxMana);

        OnManaChanged?.Invoke(currentMana, maxMana);

        if (!wasFull && IsFull)
        {
            OnManaFull?.Invoke();
        }
    }

    /// <summary>
    /// Returns true if the player can afford the mana cost.
    /// </summary>
    public bool CanAfford(float cost)
    {
        return currentMana >= cost;
    }

    /// <summary>
    /// Instantly refills mana to maximum.
    /// </summary>
    public void RefillMana()
    {
        bool wasFull = IsFull;
        currentMana = maxMana;
        regenDelayTimer = 0f;

        OnManaChanged?.Invoke(currentMana, maxMana);

        if (!wasFull)
        {
            OnManaFull?.Invoke();
        }
    }

    /// <summary>
    /// Sets maximum mana (useful for upgrades).
    /// </summary>
    public void SetMaxMana(float newMax, bool refill = false)
    {
        maxMana = Mathf.Max(1f, newMax);

        if (refill)
        {
            currentMana = maxMana;
        }
        else
        {
            currentMana = Mathf.Min(currentMana, maxMana);
        }

        OnManaChanged?.Invoke(currentMana, maxMana);
    }
}
