using UnityEngine;

/// <summary>
/// Abstract base class for skill effect prefabs.
/// Implement ISkillEffectHandler to receive skill data on spawn.
/// </summary>
public abstract class BaseSkillEffect : MonoBehaviour, ISkillEffectHandler
{
    [Header("Base Settings")]
    [Tooltip("Automatically destroy after duration")]
    [SerializeField] protected bool autoDestroy = true;

    [Tooltip("Default lifetime if skill has no duration")]
    [SerializeField] protected float defaultLifetime = 2f;

    [Header("Audio")]
    [SerializeField] protected AudioSource audioSource;

    [Header("Visual")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected ParticleSystem particles;

    // Runtime data
    protected SkillInstance skillInstance;
    protected GameObject caster;
    protected float damage;
    protected float duration;
    protected DamageType damageType;
    protected int skillLevel;

    protected bool isInitialized;
    protected float spawnTime;

    /// <summary>
    /// Called when the skill effect is spawned.
    /// </summary>
    public virtual void Initialize(SkillInstance skill, GameObject casterObject)
    {
        skillInstance = skill;
        caster = casterObject;

        if (skill != null)
        {
            damage = skill.GetDamage();
            duration = skill.GetDuration();
            damageType = skill.skillData?.damageType ?? DamageType.Physical;
            skillLevel = skill.currentLevel;
        }

        spawnTime = Time.time;
        isInitialized = true;

        OnInitialized();

        if (autoDestroy)
        {
            float lifetime = duration > 0 ? duration : defaultLifetime;
            Destroy(gameObject, lifetime);
        }
    }

    /// <summary>
    /// Override to handle post-initialization logic.
    /// </summary>
    protected virtual void OnInitialized() { }

    /// <summary>
    /// Gets the time since this effect was spawned.
    /// </summary>
    protected float GetElapsedTime()
    {
        return Time.time - spawnTime;
    }

    /// <summary>
    /// Gets the remaining duration (0 if no duration set).
    /// </summary>
    protected float GetRemainingTime()
    {
        if (duration <= 0) return 0;
        return Mathf.Max(0, duration - GetElapsedTime());
    }

    /// <summary>
    /// Plays a sound effect.
    /// </summary>
    protected void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, volume);
        }
    }

    /// <summary>
    /// Gets the effect data for a specific effect ID from the skill.
    /// </summary>
    protected SkillEffectData GetEffectData(string effectId)
    {
        if (skillInstance?.skillData?.effects == null) return null;

        foreach (var effect in skillInstance.skillData.effects)
        {
            if (effect != null && effect.effectId == effectId)
                return effect;
        }
        return null;
    }

    /// <summary>
    /// Gets the first effect data of a specific type.
    /// </summary>
    protected SkillEffectData GetEffectDataByType(SkillEffectData.EffectType type)
    {
        if (skillInstance?.skillData?.effects == null) return null;

        foreach (var effect in skillInstance.skillData.effects)
        {
            if (effect != null && effect.effectType == type)
                return effect;
        }
        return null;
    }
}
