using UnityEngine;

/// <summary>
/// Applies visual appearance from JobClassData to the player.
/// Uses the LayeredSpriteController for modular body part rendering
/// and falls back to a direct SpriteRenderer if no layered system is present.
/// </summary>
public class PlayerAppearance : MonoBehaviour
{
    private LayeredSpriteController layeredSprite;
    private SpriteRenderer fallbackRenderer;
    private Animator animator;
    private RuntimeAnimatorController originalAnimator;
    private CharacterAppearanceConfig currentConfig;
    private bool configIsClone;

    /// <summary>
    /// The currently applied appearance config, or null if using fallback rendering.
    /// Reflects equipment changes made via SetPart().
    /// </summary>
    public CharacterAppearanceConfig CurrentConfig => currentConfig;

    private void Awake()
    {
        layeredSprite = GetComponent<LayeredSpriteController>();
        fallbackRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (animator != null)
            originalAnimator = animator.runtimeAnimatorController;

        // Ensure the fallback renderer is enabled at startup so the character
        // is visible even before a job appearance is applied. ApplyAppearance()
        // will disable it later if a layered appearance config is available.
        if (fallbackRenderer != null && layeredSprite != null)
            fallbackRenderer.enabled = true;
    }

    private void Start()
    {
        if (SkillManager.Instance != null)
        {
            if (SkillManager.Instance.CurrentJob != null)
                ApplyAppearance(SkillManager.Instance.CurrentJob);

            SkillManager.Instance.OnJobChanged += HandleJobChanged;
        }
    }

    private void OnDestroy()
    {
        if (SkillManager.Instance != null)
            SkillManager.Instance.OnJobChanged -= HandleJobChanged;
    }

    private void HandleJobChanged(JobClassData previousJob, JobClassData newJob)
    {
        if (newJob != null)
            ApplyAppearance(newJob);
    }

    /// <summary>
    /// Applies the visual appearance defined by the given job class data.
    /// Swaps the animator controller and updates layered body parts if a
    /// defaultAppearance config is set on the job.
    /// </summary>
    public void ApplyAppearance(JobClassData jobData)
    {
        if (jobData == null)
            return;

        // Swap animator controller
        if (animator != null)
        {
            if (jobData.characterAnimator != null)
            {
                animator.runtimeAnimatorController = jobData.characterAnimator;

                if (animator.runtimeAnimatorController == null)
                {
                    Debug.LogWarning($"[PlayerAppearance] Controller from {jobData.jobName} failed to apply, reverting.");
                    animator.runtimeAnimatorController = originalAnimator;
                }
            }
        }

        // Apply layered appearance if available
        if (layeredSprite != null && jobData.defaultAppearance != null)
        {
            ApplyConfig(jobData.defaultAppearance);

            // Disable root SpriteRenderer when layered system is active
            if (fallbackRenderer != null)
                fallbackRenderer.enabled = false;
        }
        else
        {
            // No layered appearance — ensure root SpriteRenderer is enabled
            // so the Animator can drive it as before
            if (fallbackRenderer != null)
                fallbackRenderer.enabled = true;

            // If no animator controller, set the sprite directly
            if (fallbackRenderer != null && jobData.defaultSprite != null)
            {
                if (animator == null || animator.runtimeAnimatorController == null)
                    fallbackRenderer.sprite = jobData.defaultSprite;
            }
        }

        Debug.Log($"[PlayerAppearance] Applied: {jobData.jobName}, Controller: {animator?.runtimeAnimatorController?.name ?? "null"}, Layered: {layeredSprite != null}");
    }

    /// <summary>
    /// Applies a CharacterAppearanceConfig to the layered sprite system.
    /// </summary>
    public void ApplyConfig(CharacterAppearanceConfig config)
    {
        if (layeredSprite == null || config == null)
            return;

        currentConfig = config;
        configIsClone = false;

        // Disable root SpriteRenderer when layered system is active
        if (fallbackRenderer != null)
            fallbackRenderer.enabled = false;

        layeredSprite.SetPart(BodyPartSlot.Body, config.body);
        layeredSprite.SetPart(BodyPartSlot.Head, config.head);
        layeredSprite.SetPart(BodyPartSlot.Hair, config.hair);
        layeredSprite.SetPart(BodyPartSlot.Torso, config.torso);
        layeredSprite.SetPart(BodyPartSlot.Legs, config.legs);
        layeredSprite.SetPart(BodyPartSlot.WeaponBehind, config.weaponBehind);
        layeredSprite.SetPart(BodyPartSlot.WeaponFront, config.weaponFront);

        if (config.body != null && config.body.supportsTinting)
            layeredSprite.SetTint(BodyPartSlot.Body, config.skinTint);
        if (config.head != null && config.head.supportsTinting)
            layeredSprite.SetTint(BodyPartSlot.Head, config.skinTint);
        if (config.hair != null && config.hair.supportsTinting)
            layeredSprite.SetTint(BodyPartSlot.Hair, config.hairTint);
        if (config.torso != null && config.torso.supportsTinting)
            layeredSprite.SetTint(BodyPartSlot.Torso, config.armorPrimaryTint);
        if (config.legs != null && config.legs.supportsTinting)
            layeredSprite.SetTint(BodyPartSlot.Legs, config.armorSecondaryTint);
    }

    /// <summary>
    /// Sets a single body part on the layered system and keeps CurrentConfig in sync.
    /// </summary>
    public void SetPart(BodyPartSlot slot, BodyPartData part)
    {
        if (layeredSprite != null)
            layeredSprite.SetPart(slot, part);

        if (currentConfig != null)
        {
            EnsureConfigCloned();
            switch (slot)
            {
                case BodyPartSlot.Body: currentConfig.body = part; break;
                case BodyPartSlot.Head: currentConfig.head = part; break;
                case BodyPartSlot.Hair: currentConfig.hair = part; break;
                case BodyPartSlot.Torso: currentConfig.torso = part; break;
                case BodyPartSlot.Legs: currentConfig.legs = part; break;
                case BodyPartSlot.WeaponBehind: currentConfig.weaponBehind = part; break;
                case BodyPartSlot.WeaponFront: currentConfig.weaponFront = part; break;
            }
        }
    }

    /// <summary>
    /// Clones currentConfig on first mutation so we don't modify the original asset.
    /// </summary>
    private void EnsureConfigCloned()
    {
        if (configIsClone || currentConfig == null) return;

        var clone = ScriptableObject.CreateInstance<CharacterAppearanceConfig>();
        clone.body = currentConfig.body;
        clone.head = currentConfig.head;
        clone.hair = currentConfig.hair;
        clone.torso = currentConfig.torso;
        clone.legs = currentConfig.legs;
        clone.weaponBehind = currentConfig.weaponBehind;
        clone.weaponFront = currentConfig.weaponFront;
        clone.skinTint = currentConfig.skinTint;
        clone.hairTint = currentConfig.hairTint;
        clone.armorPrimaryTint = currentConfig.armorPrimaryTint;
        clone.armorSecondaryTint = currentConfig.armorSecondaryTint;
        currentConfig = clone;
        configIsClone = true;
    }
}
