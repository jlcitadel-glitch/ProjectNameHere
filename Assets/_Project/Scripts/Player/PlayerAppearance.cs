using UnityEngine;

/// <summary>
/// Applies visual appearance from JobClassData to the player.
/// Reads the current job from SkillManager and sets the animator controller and default sprite.
/// Supports both single-sprite (HeroKnight) and skeletal (Miniature Army) visual systems.
/// Add this component to the Player prefab.
/// </summary>
public class PlayerAppearance : MonoBehaviour
{
    private const string ClassVisualName = "ClassVisual";

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
    /// If the job has a characterVisualPrefab, instantiates the skeletal hierarchy.
    /// Otherwise falls back to the original single-sprite animator behavior.
    /// </summary>
    public void ApplyAppearance(JobClassData jobData)
    {
        if (jobData == null)
            return;

        // Clean up any previous skeletal visual
        CleanupClassVisual();

        if (jobData.characterVisualPrefab != null)
        {
            ApplySkeletalVisual(jobData);
        }
        else
        {
            ApplyFallbackVisual(jobData);
        }

        Debug.Log($"[PlayerAppearance] Applied: {jobData.jobName}, Controller: {animator?.runtimeAnimatorController?.name ?? "null"}");
    }

    private void ApplySkeletalVisual(JobClassData jobData)
    {
        // Instantiate the prefab
        var instance = Instantiate(jobData.characterVisualPrefab);

        // Find the "Body" child in the instantiated prefab
        Transform bodyTransform = instance.transform.Find("Body");
        if (bodyTransform == null)
        {
            // Try finding any first child as fallback
            if (instance.transform.childCount > 0)
                bodyTransform = instance.transform.GetChild(0);
        }

        if (bodyTransform != null)
        {
            // Reparent the Body (and its full subtree) under the Player root
            bodyTransform.SetParent(transform, false);
            bodyTransform.localPosition = Vector3.zero;
            bodyTransform.localRotation = Quaternion.identity;
            bodyTransform.localScale = Vector3.one;
            bodyTransform.gameObject.name = ClassVisualName;
        }

        // Destroy the instantiated shell (we only needed the Body subtree)
        Destroy(instance);

        // Hide the root SpriteRenderer so HeroKnight sprite doesn't show
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        // Apply the override controller
        if (animator != null && jobData.characterAnimator != null)
        {
            animator.runtimeAnimatorController = jobData.characterAnimator;

            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"[PlayerAppearance] Controller from {jobData.jobName} failed to apply, reverting.");
                animator.runtimeAnimatorController = originalAnimator;
            }
        }
    }

    private void ApplyFallbackVisual(JobClassData jobData)
    {
        // Re-enable root SpriteRenderer for single-sprite mode
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

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

        // Apply default sprite only if no animator is driving the sprite
        if (spriteRenderer != null && jobData.defaultSprite != null)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                spriteRenderer.sprite = jobData.defaultSprite;
            }
        }
    }

    private void CleanupClassVisual()
    {
        // Destroy any existing skeletal visual child
        var existing = transform.Find(ClassVisualName);
        if (existing != null)
            Destroy(existing.gameObject);
    }
}
