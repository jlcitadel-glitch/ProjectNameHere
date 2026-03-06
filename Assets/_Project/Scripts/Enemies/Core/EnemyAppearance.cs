using UnityEngine;

/// <summary>
/// Adds layered LPC sprite rendering to humanoid enemies.
/// Reuses the same LayeredSpriteController / AnimationFrameDriver / BodyPartData
/// system as the player. Optional component — enemies without this use their
/// existing single-SpriteRenderer setup.
///
/// Requires: Animator on the same GameObject, EnemyData with appearanceConfig set.
/// Creates: LayeredSpriteController + AnimationFrameDriver as sibling components.
/// </summary>
public class EnemyAppearance : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private AnimationStateFrameMap frameMap;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "Ground";
    [SerializeField] private int baseSortingOrder = 10;

    private LayeredSpriteController layeredSprite;
    private AnimationFrameDriver frameDriver;
    private CharacterAppearanceConfig activeConfig;

    public LayeredSpriteController LayeredSprite => layeredSprite;

    /// <summary>
    /// Initializes the layered appearance system. Called by EnemyController
    /// when an EnemyData with appearanceConfig is detected.
    /// </summary>
    public void Initialize(CharacterAppearanceConfig config, AnimationStateFrameMap map)
    {
        if (config == null)
        {
            Debug.LogWarning($"[EnemyAppearance] {gameObject.name}: null config, skipping.");
            return;
        }

        if (map != null)
            frameMap = map;

        // Clone the config so runtime tint changes don't modify the asset
        activeConfig = config.Clone();

        EnsureComponents();
        ApplyConfig(activeConfig);
        DisableRootSprite();
    }

    private void EnsureComponents()
    {
        // AnimationFrameDriver requires Animator (already on enemy via EnemyController)
        frameDriver = GetComponent<AnimationFrameDriver>();
        if (frameDriver == null)
            frameDriver = gameObject.AddComponent<AnimationFrameDriver>();

        if (frameMap != null)
            frameDriver.SetFrameMap(frameMap);

        // LayeredSpriteController requires AnimationFrameDriver (just added above)
        layeredSprite = GetComponent<LayeredSpriteController>();
        if (layeredSprite == null)
            layeredSprite = gameObject.AddComponent<LayeredSpriteController>();
    }

    private void ApplyConfig(CharacterAppearanceConfig config)
    {
        if (layeredSprite == null || config == null)
            return;

        // Apply each body part from the config
        foreach (var kvp in config.GetAllParts())
        {
            layeredSprite.SetPart(kvp.Key, kvp.Value);

            // Apply tint if the part supports it
            if (kvp.Value != null && kvp.Value.supportsTinting)
            {
                Color tint = config.GetTintForCategory(kvp.Value.tintCategory);
                layeredSprite.SetTint(kvp.Key, tint);
            }
        }
    }

    /// <summary>
    /// Hides the root SpriteRenderer so the layered sprites are the only
    /// visible representation. The root SpriteRenderer is kept for
    /// collider/bounds compatibility but made invisible.
    /// </summary>
    private void DisableRootSprite()
    {
        var rootSR = GetComponent<SpriteRenderer>();
        if (rootSR != null)
        {
            rootSR.enabled = false;
        }
    }

    /// <summary>
    /// Sets a specific body part at runtime (e.g., equip different weapon).
    /// </summary>
    public void SetPart(BodyPartSlot slot, BodyPartData part)
    {
        layeredSprite?.SetPart(slot, part);
    }

    /// <summary>
    /// Sets a tint color for a specific slot.
    /// </summary>
    public void SetTint(BodyPartSlot slot, Color tint)
    {
        layeredSprite?.SetTint(slot, tint);
    }
}
