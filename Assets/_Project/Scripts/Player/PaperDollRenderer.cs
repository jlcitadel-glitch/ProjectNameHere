using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Paper-doll rendering system. Instantiates Peasant skeletal hierarchy
/// and manages body part slots for sprite swapping and class color tinting.
/// Replaces PlayerAppearance.cs.
/// </summary>
public class PaperDollRenderer : MonoBehaviour
{
    public enum BodyPartSlot
    {
        Head,
        Body,
        BackArm,
        FrontArm,
        BackLeg,
        FrontLeg,
        WeaponBack,
        WeaponFront
    }

    [Header("Base Character")]
    [Tooltip("Peasant prefab from Miniature Army 2D")]
    [SerializeField] private GameObject basePrefab;

    private const string SkeletalBodyName = "Body";

    private static readonly string[] KnownColors = { "red", "blue", "green", "orange", "yellow", "white", "black" };

    // Maps slot enum to the child transform name in the Peasant hierarchy
    private static readonly Dictionary<BodyPartSlot, string> SlotToChildName = new Dictionary<BodyPartSlot, string>
    {
        { BodyPartSlot.Head, "Head" },
        { BodyPartSlot.Body, null }, // Body is the root of the hierarchy
        { BodyPartSlot.BackArm, "Back arm" },
        { BodyPartSlot.FrontArm, "Front arm" },
        { BodyPartSlot.BackLeg, "Back leg" },
        { BodyPartSlot.FrontLeg, "Front leg" },
        { BodyPartSlot.WeaponBack, "bow" },
        { BodyPartSlot.WeaponFront, "arrow" }
    };

    private SpriteRenderer rootSpriteRenderer;
    private Animator animator;
    private Transform bodyRoot;
    private string currentColor = "red";

    // Slot -> SpriteRenderer mapping
    private readonly Dictionary<BodyPartSlot, SpriteRenderer> slotRenderers =
        new Dictionary<BodyPartSlot, SpriteRenderer>();

    // Slot -> original sprite (before equipment override)
    private readonly Dictionary<BodyPartSlot, Sprite> baseSprites =
        new Dictionary<BodyPartSlot, Sprite>();

    // Original physics values (reserved for future fallback restoration)
    private Vector2 originalColliderOffset;
    private Vector2 originalColliderSize;
    private Vector3 originalGroundCheckPos;

    private void Awake()
    {
        rootSpriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        StoreOriginalPhysics();
        InitializeBody();
    }

    private void Start()
    {
        if (SkillManager.Instance != null)
        {
            if (SkillManager.Instance.CurrentJob != null)
                ApplyClassColor(SkillManager.Instance.CurrentJob);

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
            ApplyClassColor(newJob);
    }

    /// <summary>
    /// Instantiates the base Peasant body hierarchy and caches slot renderers.
    /// </summary>
    private void InitializeBody()
    {
        // Clean up any existing body
        var existing = transform.Find(SkeletalBodyName);
        if (existing != null)
            Destroy(existing.gameObject);

        if (basePrefab == null)
        {
            Debug.LogWarning("[PaperDollRenderer] No basePrefab assigned. Skeletal body will not be created.");
            return;
        }

        // Instantiate the prefab
        var instance = Instantiate(basePrefab);

        // Find the "Body" child
        Transform bodyTransform = instance.transform.Find(SkeletalBodyName);
        if (bodyTransform == null && instance.transform.childCount > 0)
            bodyTransform = instance.transform.GetChild(0);

        if (bodyTransform != null)
        {
            bodyTransform.SetParent(transform, false);
            bodyTransform.localPosition = Vector3.zero;
            bodyTransform.localRotation = Quaternion.identity;
            bodyTransform.localScale = Vector3.one;
            bodyRoot = bodyTransform;
        }

        // Destroy the instantiated shell
        Destroy(instance);

        // Hide root SpriteRenderer (HeroKnight sprite)
        if (rootSpriteRenderer != null)
            rootSpriteRenderer.enabled = false;

        // Cache slot renderers
        CacheSlotRenderers();

        // Adjust physics to fit skeletal body
        if (bodyRoot != null)
            AdjustPhysicsForSkeletal(bodyRoot);

        // Rebind animator so it discovers the new child hierarchy
        if (animator != null)
            animator.Rebind();

        Debug.Log("[PaperDollRenderer] Body initialized with " + slotRenderers.Count + " slots.");
    }

    /// <summary>
    /// Finds and caches SpriteRenderers for each body part slot.
    /// </summary>
    private void CacheSlotRenderers()
    {
        slotRenderers.Clear();
        baseSprites.Clear();

        if (bodyRoot == null) return;

        foreach (var kvp in SlotToChildName)
        {
            SpriteRenderer renderer = null;

            if (kvp.Value == null)
            {
                // Body slot is the root itself
                renderer = bodyRoot.GetComponent<SpriteRenderer>();
            }
            else
            {
                // Search children recursively
                var child = FindChildRecursive(bodyRoot, kvp.Value);
                if (child != null)
                    renderer = child.GetComponent<SpriteRenderer>();
            }

            if (renderer != null)
            {
                slotRenderers[kvp.Key] = renderer;
                baseSprites[kvp.Key] = renderer.sprite;
            }
        }
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        // Direct child first
        var direct = parent.Find(name);
        if (direct != null) return direct;

        // Recursive search
        for (int i = 0; i < parent.childCount; i++)
        {
            var result = FindChildRecursive(parent.GetChild(i), name);
            if (result != null) return result;
        }

        return null;
    }

    #region Public API

    /// <summary>
    /// Sets a specific sprite on a body part slot (for equipment).
    /// </summary>
    public void SetSlotSprite(BodyPartSlot slot, Sprite sprite)
    {
        if (slotRenderers.TryGetValue(slot, out var renderer))
            renderer.sprite = sprite;
    }

    /// <summary>
    /// Reverts a slot to its base sprite (unequip).
    /// </summary>
    public void ClearSlot(BodyPartSlot slot)
    {
        if (slotRenderers.TryGetValue(slot, out var renderer) &&
            baseSprites.TryGetValue(slot, out var baseSprite))
            renderer.sprite = baseSprite;
    }

    /// <summary>
    /// Applies a color variant to all body part sprites.
    /// Uses the Miniature Army naming convention: {character}-{part}-{color}.
    /// </summary>
    public void SetClassColor(string colorName)
    {
        if (string.IsNullOrEmpty(colorName) || bodyRoot == null)
            return;

        currentColor = colorName;
        SwapSpriteColors(bodyRoot, colorName);

        // Update base sprites cache after color swap
        foreach (var kvp in slotRenderers)
            baseSprites[kvp.Key] = kvp.Value.sprite;
    }

    /// <summary>
    /// Gets the SpriteRenderer for a specific body part slot.
    /// </summary>
    public SpriteRenderer GetSlotRenderer(BodyPartSlot slot)
    {
        slotRenderers.TryGetValue(slot, out var renderer);
        return renderer;
    }

    /// <summary>
    /// Returns the current class color name.
    /// </summary>
    public string CurrentColor => currentColor;

    #endregion

    #region Class Color

    private void ApplyClassColor(JobClassData jobData)
    {
        string color = GetClassColor(jobData);
        SetClassColor(color);
        Debug.Log($"[PaperDollRenderer] Applied color '{color}' for {jobData.jobName}");
    }

    /// <summary>
    /// Returns the target sprite color for a given class.
    /// Checks classColor field first, then falls back to jobId mapping.
    /// </summary>
    public static string GetClassColor(JobClassData jobData)
    {
        if (jobData == null)
            return "red";

        // Use data-driven classColor if set
        if (!string.IsNullOrEmpty(jobData.classColor))
            return jobData.classColor;

        // Fallback to jobId mapping
        if (string.IsNullOrEmpty(jobData.jobId))
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
    /// Sprites are identified by a "-{color}" suffix (e.g., "peasant-body-red" -> "peasant-body-blue").
    /// </summary>
    public static void SwapSpriteColors(Transform root, string targetColor)
    {
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
                    if (color == targetColor) break;

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
    }

    private void AdjustPhysicsForSkeletal(Transform bodyTransform)
    {
        var renderers = bodyTransform.GetComponentsInChildren<SpriteRenderer>(true);
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

        float localMinY = bounds.min.y - transform.position.y;
        float localMaxY = bounds.max.y - transform.position.y;
        float localCenterY = (localMinY + localMaxY) / 2f;
        float height = localMaxY - localMinY;
        float width = bounds.size.x;

        var capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null)
        {
            capsule.offset = new Vector2(0f, localCenterY);
            capsule.size = new Vector2(Mathf.Max(width * 0.5f, 0.3f), Mathf.Max(height, 0.5f));
        }

        var groundCheckTransform = transform.Find("GroundCheck");
        if (groundCheckTransform != null)
            groundCheckTransform.localPosition = new Vector3(0f, localMinY, 0f);
    }

    #endregion
}
