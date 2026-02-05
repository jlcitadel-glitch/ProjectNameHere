using UnityEngine;

/// <summary>
/// Defines the effect properties for a skill.
/// Used to configure damage, buffs, heals, and other effects.
/// </summary>
[CreateAssetMenu(fileName = "NewSkillEffect", menuName = "Skills/Skill Effect Data")]
public class SkillEffectData : ScriptableObject
{
    public enum EffectType
    {
        Damage,
        Heal,
        Buff,
        Debuff,
        Projectile,
        AreaOfEffect,
        Summon
    }

    [Header("Effect Identity")]
    [Tooltip("Unique identifier for this effect")]
    public string effectId;

    [Tooltip("Type of effect")]
    public EffectType effectType;

    [Header("Damage/Heal Settings")]
    [Tooltip("Base value at level 1")]
    public float baseValue;

    [Tooltip("Value increase per skill level")]
    public float valuePerLevel;

    [Tooltip("Damage type for resistance calculations")]
    public DamageType damageType = DamageType.Physical;

    [Header("Duration Settings")]
    [Tooltip("Effect duration in seconds (0 for instant)")]
    public float baseDuration;

    [Tooltip("Duration increase per level")]
    public float durationPerLevel;

    [Tooltip("Tick interval for DoT/HoT effects")]
    public float tickInterval = 1f;

    [Header("Stat Modifiers (for Buffs/Debuffs)")]
    [Tooltip("Attack power modifier (1.0 = no change, 1.5 = +50%)")]
    public float attackModifier = 1f;

    [Tooltip("Defense modifier")]
    public float defenseModifier = 1f;

    [Tooltip("Speed modifier")]
    public float speedModifier = 1f;

    [Tooltip("Critical chance bonus (additive)")]
    public float criticalChanceBonus;

    [Tooltip("Critical damage bonus (additive)")]
    public float criticalDamageBonus;

    [Header("Projectile Settings")]
    [Tooltip("Projectile speed")]
    public float projectileSpeed = 10f;

    [Tooltip("Number of projectiles")]
    public int projectileCount = 1;

    [Tooltip("Projectile spread angle")]
    public float spreadAngle;

    [Tooltip("Piercing (passes through enemies)")]
    public bool piercing;

    [Header("Area of Effect Settings")]
    [Tooltip("Effect radius")]
    public float radius = 3f;

    [Tooltip("Maximum targets affected (-1 for unlimited)")]
    public int maxTargets = -1;

    [Header("Visual/Audio")]
    [Tooltip("Effect prefab to spawn")]
    public GameObject effectPrefab;

    [Tooltip("Sound effect to play")]
    public AudioClip soundEffect;

    /// <summary>
    /// Gets the effect value at the specified skill level.
    /// </summary>
    public float GetValue(int skillLevel)
    {
        return baseValue + (valuePerLevel * (skillLevel - 1));
    }

    /// <summary>
    /// Gets the effect duration at the specified skill level.
    /// </summary>
    public float GetDuration(int skillLevel)
    {
        return baseDuration + (durationPerLevel * (skillLevel - 1));
    }
}
