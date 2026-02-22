using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Core component for the modular layered character system.
/// Creates and manages child SpriteRenderers (one per BodyPartSlot).
/// Listens to AnimationFrameDriver for frame changes and updates all layers
/// to show the correct sprite for the current animation frame.
/// </summary>
[RequireComponent(typeof(AnimationFrameDriver))]
public class LayeredSpriteController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int baseSortingOrder = 0;

    [Header("Debug")]
    [SerializeField] private bool logFrameChanges;

    private AnimationFrameDriver frameDriver;
    private Dictionary<BodyPartSlot, LayerEntry> layers;
    private Transform layerParent;

    private class LayerEntry
    {
        public GameObject gameObject;
        public SpriteRenderer renderer;
        public BodyPartData currentPart;
        public Color baseTint = Color.white;
    }

    private void Awake()
    {
        frameDriver = GetComponent<AnimationFrameDriver>();
        InitializeLayers();
    }

    private void OnEnable()
    {
        if (frameDriver != null)
            frameDriver.OnFrameChanged += HandleFrameChanged;
    }

    private void OnDisable()
    {
        if (frameDriver != null)
            frameDriver.OnFrameChanged -= HandleFrameChanged;
    }

    private void InitializeLayers()
    {
        layers = new Dictionary<BodyPartSlot, LayerEntry>();

        // Ensure SortingGroup exists on root for proper draw ordering
        if (GetComponent<SortingGroup>() == null)
            gameObject.AddComponent<SortingGroup>();

        // Find or create the SpriteLayers parent
        layerParent = transform.Find("SpriteLayers");
        if (layerParent == null)
        {
            var parentObj = new GameObject("SpriteLayers");
            parentObj.transform.SetParent(transform, false);
            layerParent = parentObj.transform;
        }

        // Create a child SpriteRenderer for each slot
        var slots = (BodyPartSlot[])System.Enum.GetValues(typeof(BodyPartSlot));
        foreach (var slot in slots)
        {
            CreateLayerForSlot(slot);
        }
    }

    private void CreateLayerForSlot(BodyPartSlot slot)
    {
        // Check if a child already exists from the prefab
        string childName = slot.ToString();
        Transform existing = layerParent.Find(childName);

        GameObject layerObj;
        SpriteRenderer sr;

        if (existing != null)
        {
            layerObj = existing.gameObject;
            sr = layerObj.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = layerObj.AddComponent<SpriteRenderer>();
        }
        else
        {
            layerObj = new GameObject(childName);
            layerObj.transform.SetParent(layerParent, false);
            sr = layerObj.AddComponent<SpriteRenderer>();
        }

        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = baseSortingOrder + (int)slot;

        var entry = new LayerEntry
        {
            gameObject = layerObj,
            renderer = sr,
            currentPart = null,
            baseTint = Color.white
        };

        layers[slot] = entry;

        // Start invisible until a part is assigned
        layerObj.SetActive(false);
    }

    private void HandleFrameChanged(int globalFrameIndex)
    {
        if (logFrameChanges)
            Debug.Log($"[LayeredSprite] Frame: {globalFrameIndex}, State: {frameDriver.CurrentStateName}");

        foreach (var kvp in layers)
        {
            var entry = kvp.Value;
            if (entry.currentPart == null || entry.currentPart.frames == null)
                continue;

            if (globalFrameIndex >= 0 && globalFrameIndex < entry.currentPart.frames.Length)
            {
                var sprite = entry.currentPart.frames[globalFrameIndex];
                if (sprite != null)
                {
                    entry.renderer.sprite = sprite;
                }
            }
        }
    }

    /// <summary>
    /// Sets the body part for a given slot.
    /// Pass null to clear the slot (hides the layer).
    /// </summary>
    public void SetPart(BodyPartSlot slot, BodyPartData part)
    {
        if (!layers.TryGetValue(slot, out var entry))
            return;

        entry.currentPart = part;

        if (part == null)
        {
            entry.gameObject.SetActive(false);
            return;
        }

        entry.gameObject.SetActive(true);
        entry.renderer.sortingOrder = baseSortingOrder + (int)slot + part.sortOrderOffset;

        // Apply default tint
        entry.baseTint = part.supportsTinting ? part.defaultTint : Color.white;
        entry.renderer.color = entry.baseTint;

        // Show preview sprite or first frame immediately
        if (part.frames != null && part.frames.Length > 0)
        {
            entry.renderer.sprite = part.frames[0];
        }
        else if (part.previewSprite != null)
        {
            entry.renderer.sprite = part.previewSprite;
        }
    }

    /// <summary>
    /// Sets the tint color for a specific slot.
    /// </summary>
    public void SetTint(BodyPartSlot slot, Color tint)
    {
        if (!layers.TryGetValue(slot, out var entry))
            return;

        entry.baseTint = tint;
        entry.renderer.color = tint;
    }

    /// <summary>
    /// Returns the BodyPartData currently assigned to a slot, or null.
    /// </summary>
    public BodyPartData GetPart(BodyPartSlot slot)
    {
        return layers.TryGetValue(slot, out var entry) ? entry.currentPart : null;
    }

    /// <summary>
    /// Flashes all visible layers to the given color (for damage feedback).
    /// Call RestoreAllTints() to revert.
    /// </summary>
    public void FlashAll(Color flashColor)
    {
        foreach (var entry in layers.Values)
        {
            if (entry.gameObject.activeSelf)
                entry.renderer.color = flashColor;
        }
    }

    /// <summary>
    /// Restores all layers to their base tint colors.
    /// </summary>
    public void RestoreAllTints()
    {
        foreach (var entry in layers.Values)
        {
            if (entry.gameObject.activeSelf)
                entry.renderer.color = entry.baseTint;
        }
    }

    /// <summary>
    /// Returns all SpriteRenderers managed by this controller.
    /// Useful for external systems that need to iterate over player sprites.
    /// </summary>
    public SpriteRenderer[] GetAllRenderers()
    {
        var renderers = new List<SpriteRenderer>();
        foreach (var entry in layers.Values)
        {
            if (entry.gameObject.activeSelf)
                renderers.Add(entry.renderer);
        }
        return renderers.ToArray();
    }

    /// <summary>
    /// Returns the primary (Body) SpriteRenderer for backwards compatibility
    /// with systems that expect a single SpriteRenderer.
    /// </summary>
    public SpriteRenderer GetPrimaryRenderer()
    {
        return layers.TryGetValue(BodyPartSlot.Body, out var entry) ? entry.renderer : null;
    }
}
