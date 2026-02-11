using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages active temporary buffs on the player.
/// Attached to the Player alongside SkillExecutor.
/// </summary>
public class ActiveBuffTracker : MonoBehaviour
{
    /// <summary>
    /// Represents a single active buff with its remaining duration and modifiers.
    /// </summary>
    public struct ActiveBuff
    {
        public string skillId;
        public float remainingDuration;
        public float attackMultiplier;
        public float defenseMultiplier;
        public float critChanceBonus;
        public float speedMultiplier;
        public bool invulnerable;
        public float manaRegenBonus;
    }

    private readonly Dictionary<string, ActiveBuff> activeBuffs = new Dictionary<string, ActiveBuff>();
    private HealthSystem healthSystem;

    // Aggregate modifiers — recalculated each frame
    public float TotalAttackMultiplier { get; private set; } = 1f;
    public float TotalDefenseMultiplier { get; private set; } = 1f;
    public float TotalCritChanceBonus { get; private set; }
    public float TotalSpeedMultiplier { get; private set; } = 1f;
    public bool IsInvulnerable { get; private set; }

    public event Action<string> OnBuffApplied;
    public event Action<string> OnBuffExpired;

    public int ActiveBuffCount => activeBuffs.Count;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
    }

    private void Update()
    {
        if (activeBuffs.Count == 0) return;

        // Tick durations and collect expired buffs
        List<string> expired = null;
        var keys = new List<string>(activeBuffs.Keys);

        foreach (var key in keys)
        {
            var buff = activeBuffs[key];
            buff.remainingDuration -= Time.deltaTime;

            if (buff.remainingDuration <= 0f)
            {
                expired ??= new List<string>();
                expired.Add(key);
            }
            else
            {
                activeBuffs[key] = buff;
            }
        }

        // Remove expired
        if (expired != null)
        {
            foreach (var id in expired)
            {
                activeBuffs.Remove(id);
                OnBuffExpired?.Invoke(id);
            }
        }

        RecalculateAggregates();
    }

    /// <summary>
    /// Adds or refreshes a buff. If the same skillId is already active, it replaces it (no stacking).
    /// </summary>
    public void AddBuff(ActiveBuff buff)
    {
        activeBuffs[buff.skillId] = buff;
        RecalculateAggregates();
        OnBuffApplied?.Invoke(buff.skillId);
    }

    /// <summary>
    /// Removes a buff early (before it expires naturally).
    /// </summary>
    public void RemoveBuff(string skillId)
    {
        if (activeBuffs.Remove(skillId))
        {
            RecalculateAggregates();
            OnBuffExpired?.Invoke(skillId);
        }
    }

    /// <summary>
    /// Returns true if the specified buff is currently active.
    /// </summary>
    public bool HasBuff(string skillId)
    {
        return activeBuffs.ContainsKey(skillId);
    }

    /// <summary>
    /// Gets the remaining duration for a specific buff. Returns 0 if not active.
    /// </summary>
    public float GetRemainingDuration(string skillId)
    {
        return activeBuffs.TryGetValue(skillId, out var buff) ? buff.remainingDuration : 0f;
    }

    private void RecalculateAggregates()
    {
        float attack = 1f;
        float defense = 1f;
        float critBonus = 0f;
        float speed = 1f;
        bool invuln = false;

        foreach (var buff in activeBuffs.Values)
        {
            attack *= buff.attackMultiplier;
            defense *= buff.defenseMultiplier;
            critBonus += buff.critChanceBonus;
            speed *= buff.speedMultiplier;
            invuln |= buff.invulnerable;
        }

        TotalAttackMultiplier = attack;
        TotalDefenseMultiplier = defense;
        TotalCritChanceBonus = critBonus;
        TotalSpeedMultiplier = speed;
        IsInvulnerable = invuln;

        // Apply defense and invulnerability to HealthSystem
        if (healthSystem == null)
            healthSystem = GetComponent<HealthSystem>();

        if (healthSystem != null)
        {
            healthSystem.SetDefenseMultiplier(defense);
            if (invuln)
            {
                healthSystem.GrantInvulnerability(Time.deltaTime + 0.1f);
            }
        }
    }

    /// <summary>
    /// Creates an ActiveBuff for the specified skill ID with the correct config.
    /// Returns null skillId if the skill is not a known buff.
    /// </summary>
    public static ActiveBuff CreateBuffForSkill(string skillId, float duration)
    {
        var buff = new ActiveBuff
        {
            skillId = skillId,
            remainingDuration = duration,
            attackMultiplier = 1f,
            defenseMultiplier = 1f,
            critChanceBonus = 0f,
            speedMultiplier = 1f,
            invulnerable = false,
            manaRegenBonus = 0f
        };

        switch (skillId)
        {
            case "guard":
                buff.defenseMultiplier = 1.3f;
                break;
            case "berserk":
                buff.attackMultiplier = 1.5f;
                buff.defenseMultiplier = 0.8f; // Risky: take more damage
                break;
            case "war_cry":
                buff.attackMultiplier = 1.2f;
                break;
            case "magic_shield":
                buff.defenseMultiplier = 2.0f;
                break;
            case "evasion":
                buff.invulnerable = true;
                break;
        }

        return buff;
    }
}
