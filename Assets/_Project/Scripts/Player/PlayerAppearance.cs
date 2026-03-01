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
    /// Uses data-driven tint categories instead of per-slot switch logic.
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

        // Apply all parts via loop
        var allSlots = (BodyPartSlot[])System.Enum.GetValues(typeof(BodyPartSlot));
        foreach (var slot in allSlots)
        {
            var part = config.GetPart(slot);
            layeredSprite.SetPart(slot, part);

            // Apply tint based on the part's tint category
            if (part != null && part.supportsTinting && part.tintCategory != TintCategory.None)
            {
                var tint = config.GetTintForCategory(part.tintCategory);
                layeredSprite.SetTint(slot, tint);
            }
        }
    }

    /// <summary>
    /// Sets a single body part on the layered system and keeps CurrentConfig in sync.
    /// If the new part has an exclusiveGroup, hides other parts in the same group.
    /// </summary>
    public void SetPart(BodyPartSlot slot, BodyPartData part)
    {
        if (layeredSprite != null)
            layeredSprite.SetPart(slot, part);

        if (currentConfig != null)
        {
            EnsureConfigCloned();
            currentConfig.SetPart(slot, part);

            // Handle exclusivity: if this part has an exclusiveGroup,
            // clear other slots in the same group
            if (part != null && !string.IsNullOrEmpty(part.exclusiveGroup))
            {
                var allSlots = (BodyPartSlot[])System.Enum.GetValues(typeof(BodyPartSlot));
                foreach (var otherSlot in allSlots)
                {
                    if (otherSlot == slot) continue;
                    var otherPart = currentConfig.GetPart(otherSlot);
                    if (otherPart != null && otherPart.exclusiveGroup == part.exclusiveGroup)
                    {
                        currentConfig.SetPart(otherSlot, null);
                        if (layeredSprite != null)
                            layeredSprite.SetPart(otherSlot, null);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Sets the tint for a slot and keeps CurrentConfig in sync.
    /// </summary>
    public void SetTint(BodyPartSlot slot, Color tint)
    {
        if (layeredSprite != null)
            layeredSprite.SetTint(slot, tint);
    }

    /// <summary>
    /// Clones currentConfig on first mutation so we don't modify the original asset.
    /// </summary>
    private void EnsureConfigCloned()
    {
        if (configIsClone || currentConfig == null) return;
        currentConfig = currentConfig.Clone();
        configIsClone = true;
    }
}
