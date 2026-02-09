using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies visual appearance from JobClassData to the player.
/// Reads the current job from SkillManager and sets the animator controller and default sprite.
/// Supports both single-sprite (HeroKnight) and skeletal (Miniature Army) visual systems.
/// Add this component to the Player prefab.
/// </summary>
public class PlayerAppearance : MonoBehaviour
{
    // Animation clips reference "Body", "Body/Head", etc. — name must match exactly.
    private const string SkeletalBodyName = "Body";

    private static readonly string[] KnownColors = { "red", "blue", "green", "orange", "yellow", "white", "black" };

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private RuntimeAnimatorController originalAnimator;

    // Original physics values for restoration when switching back to fallback
    private Vector2 originalColliderOffset;
    private Vector2 originalColliderSize;
    private Vector3 originalGroundCheckPos;
    private bool hasOriginalPhysics;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (animator != null)
            originalAnimator = animator.runtimeAnimatorController;

        StoreOriginalPhysics();
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
        CleanupSkeletalVisual();

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
        Transform bodyTransform = instance.transform.Find(SkeletalBodyName);
        if (bodyTransform == null)
        {
            // Try finding any first child as fallback
            if (instance.transform.childCount > 0)
                bodyTransform = instance.transform.GetChild(0);
        }

        if (bodyTransform != null)
        {
            // Reparent the Body (and its full subtree) under the Player root.
            // Keep name as "Body" so animation paths ("Body/Head", "Body/Back arm", etc.) resolve.
            bodyTransform.SetParent(transform, false);
            bodyTransform.localPosition = Vector3.zero;
            bodyTransform.localRotation = Quaternion.identity;
            bodyTransform.localScale = Vector3.one;
        }

        // Destroy the instantiated shell (we only needed the Body subtree)
        Destroy(instance);

        // Swap sprites to class-specific color variant
        if (bodyTransform != null)
        {
            string targetColor = GetClassColor(jobData);
            SwapSpriteColors(bodyTransform, targetColor);
        }

        // Hide the root SpriteRenderer so HeroKnight sprite doesn't show
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        // Adjust collider and ground check to fit the skeletal character
        if (bodyTransform != null)
            AdjustPhysicsForSkeletal(bodyTransform);

        // Apply the override controller and rebind so the Animator discovers
        // the new child hierarchy ("Body", "Body/Head", etc.) for skeletal clips.
        if (animator != null && jobData.characterAnimator != null)
        {
            animator.runtimeAnimatorController = jobData.characterAnimator;

            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"[PlayerAppearance] Controller from {jobData.jobName} failed to apply, reverting.");
                animator.runtimeAnimatorController = originalAnimator;
            }
            else
            {
                // Rebind is critical: override clips target child transforms ("Body/Head")
                // instead of root SpriteRenderer like the original HeroKnight clips.
                // Without Rebind(), the Animator uses stale bindings and curves silently fail.
                animator.Rebind();
            }
        }
    }

    private void ApplyFallbackVisual(JobClassData jobData)
    {
        // Re-enable root SpriteRenderer for single-sprite mode
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        // Restore original collider/ground check for HeroKnight
        RestoreOriginalPhysics();

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

    private void CleanupSkeletalVisual()
    {
        // Destroy any existing skeletal visual child named "Body"
        var existing = transform.Find(SkeletalBodyName);
        if (existing != null)
            Destroy(existing.gameObject);
    }

    #region Sprite Color Swapping

    /// <summary>
    /// Returns the target sprite color for a given class.
    /// </summary>
    public static string GetClassColor(JobClassData jobData)
    {
        if (jobData == null || string.IsNullOrEmpty(jobData.jobId))
            return "red";

        switch (jobData.jobId.ToLower())
        {
            case "warrior": return "red";
            case "mage": return "blue";
            case "rogue": return "green";
            default: return "red";
        }
    }

    /// <summary>
    /// Swaps all color-variant sprites in the hierarchy to the target color.
    /// Sprites are identified by a "-{color}" suffix (e.g., "peasant-body-red" → "peasant-body-blue").
    /// </summary>
    public static void SwapSpriteColors(Transform root, string targetColor)
    {
        // Build lookup from all loaded sprites
        var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        var spriteLookup = new Dictionary<string, Sprite>();
        foreach (var s in allSprites)
        {
            if (s != null && !string.IsNullOrEmpty(s.name))
                spriteLookup[s.name] = s;
        }

        var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer.sprite == null) continue;
            string spriteName = renderer.sprite.name;

            foreach (var color in KnownColors)
            {
                string suffix = "-" + color;
                if (spriteName.EndsWith(suffix))
                {
                    if (color == targetColor) break; // Already the right color

                    string baseName = spriteName.Substring(0, spriteName.Length - suffix.Length);
                    string newName = baseName + "-" + targetColor;
                    if (spriteLookup.TryGetValue(newName, out var newSprite))
                        renderer.sprite = newSprite;
                    break;
                }
            }
        }
    }

    #endregion

    #region Physics Adjustment

    private void StoreOriginalPhysics()
    {
        var capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null)
        {
            originalColliderOffset = capsule.offset;
            originalColliderSize = capsule.size;
        }

        var groundCheck = transform.Find("GroundCheck");
        if (groundCheck != null)
            originalGroundCheckPos = groundCheck.localPosition;

        hasOriginalPhysics = true;
    }

    private void RestoreOriginalPhysics()
    {
        if (!hasOriginalPhysics) return;

        var capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null)
        {
            capsule.offset = originalColliderOffset;
            capsule.size = originalColliderSize;
        }

        var groundCheck = transform.Find("GroundCheck");
        if (groundCheck != null)
            groundCheck.localPosition = originalGroundCheckPos;
    }

    /// <summary>
    /// Dynamically adjusts the CapsuleCollider2D and GroundCheck position
    /// to match the skeletal character's rendered bounds.
    /// </summary>
    private void AdjustPhysicsForSkeletal(Transform bodyRoot)
    {
        var renderers = bodyRoot.GetComponentsInChildren<SpriteRenderer>(true);
        Bounds bounds = default;
        bool initialized = false;

        foreach (var r in renderers)
        {
            if (r.sprite == null) continue;
            if (!initialized)
            {
                bounds = r.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        if (!initialized) return;

        // Calculate positions relative to Player transform
        float localMinY = bounds.min.y - transform.position.y;
        float localMaxY = bounds.max.y - transform.position.y;
        float localCenterY = (localMinY + localMaxY) / 2f;
        float height = localMaxY - localMinY;
        float width = bounds.size.x;

        // Adjust CapsuleCollider2D to wrap the skeletal character
        var capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null)
        {
            capsule.offset = new Vector2(0f, localCenterY);
            capsule.size = new Vector2(Mathf.Max(width * 0.5f, 0.3f), Mathf.Max(height, 0.5f));
        }

        // Move GroundCheck to the character's feet
        var groundCheck = transform.Find("GroundCheck");
        if (groundCheck != null)
            groundCheck.localPosition = new Vector3(0f, localMinY, 0f);
    }

    #endregion
}
