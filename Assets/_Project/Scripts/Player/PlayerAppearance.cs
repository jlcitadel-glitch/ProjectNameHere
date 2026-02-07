using UnityEngine;

/// <summary>
/// Applies visual appearance from JobClassData to the player.
/// Reads the current job from SkillManager and sets the animator controller and default sprite.
/// Add this component to the Player prefab.
/// </summary>
public class PlayerAppearance : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private RuntimeAnimatorController originalAnimator;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (animator != null)
            originalAnimator = animator.runtimeAnimatorController;
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
    /// Falls back to the original animator if the job has no custom one or if the custom one is broken.
    /// </summary>
    public void ApplyAppearance(JobClassData jobData)
    {
        if (jobData == null)
            return;

        if (animator != null)
        {
            if (jobData.characterAnimator != null)
            {
                animator.runtimeAnimatorController = jobData.characterAnimator;

                // Safety: if the controller didn't actually apply, revert
                if (animator.runtimeAnimatorController == null)
                {
                    Debug.LogWarning($"[PlayerAppearance] Controller from {jobData.jobName} failed to apply, reverting.");
                    animator.runtimeAnimatorController = originalAnimator;
                }
            }
            // No custom animator - keep original (don't re-assign to avoid resetting state)
        }

        // Apply default sprite only if no animator is driving the sprite
        if (spriteRenderer != null && jobData.defaultSprite != null)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                spriteRenderer.sprite = jobData.defaultSprite;
            }
        }

        Debug.Log($"[PlayerAppearance] Applied: {jobData.jobName}, Controller: {animator?.runtimeAnimatorController?.name ?? "null"}");
    }
}
