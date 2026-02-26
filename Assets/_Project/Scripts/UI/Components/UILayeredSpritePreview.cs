using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Canvas-based layered character preview. Stacks Image components
/// to show the character's appearance in menus and character creation.
/// </summary>
public class UILayeredSpritePreview : MonoBehaviour
{
    private Dictionary<BodyPartSlot, Image> layerImages;
    private Dictionary<BodyPartSlot, BodyPartData> currentParts;
    private RectTransform rectTransform;
    private int currentFrameIndex;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        InitializeLayers();
    }

    private void InitializeLayers()
    {
        // Guard: if already initialized, destroy existing children to avoid duplicates
        if (layerImages != null && layerImages.Count > 0)
        {
            foreach (var kvp in layerImages)
            {
                if (kvp.Value != null && kvp.Value.gameObject != null)
                    Destroy(kvp.Value.gameObject);
            }
        }

        layerImages = new Dictionary<BodyPartSlot, Image>();
        currentParts = new Dictionary<BodyPartSlot, BodyPartData>();

        var slots = (BodyPartSlot[])System.Enum.GetValues(typeof(BodyPartSlot));
        foreach (var slot in slots)
        {
            var layerObj = new GameObject(slot.ToString(), typeof(RectTransform));
            layerObj.transform.SetParent(transform, false);

            // Stretch to fill parent
            var rt = layerObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = layerObj.AddComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.enabled = false;

            layerImages[slot] = img;
        }
    }

    /// <summary>
    /// Sets the body part for a given slot. Pass null to clear.
    /// </summary>
    public void SetPart(BodyPartSlot slot, BodyPartData part)
    {
        if (!layerImages.TryGetValue(slot, out var img))
            return;

        currentParts[slot] = part;

        if (part == null || part.frames == null || part.frames.Length == 0)
        {
            img.enabled = false;
            return;
        }

        img.enabled = true;
        img.color = part.supportsTinting ? part.defaultTint : Color.white;

        // Weapon layers use frames[0] (neutral stance) to align with the idle body.
        // previewSprite on weapons is a combat frame meant for standalone icons,
        // which misaligns with the idle body pose in layered previews.
        bool isWeapon = slot == BodyPartSlot.WeaponFront || slot == BodyPartSlot.WeaponBehind;
        if (isWeapon && part.frames.Length > 0 && part.frames[0] != null)
            img.sprite = part.frames[0];
        else if (part.previewSprite != null)
            img.sprite = part.previewSprite;
        else if (part.frames.Length > 0 && part.frames[0] != null)
            img.sprite = part.frames[0];
    }

    /// <summary>
    /// Sets the tint color for a slot.
    /// </summary>
    public void SetTint(BodyPartSlot slot, Color tint)
    {
        if (layerImages.TryGetValue(slot, out var img))
            img.color = tint;
    }

    /// <summary>
    /// Sets a specific animation frame on all active layers.
    /// </summary>
    public void SetFrame(int frameIndex)
    {
        currentFrameIndex = frameIndex;

        foreach (var kvp in currentParts)
        {
            var part = kvp.Value;
            if (part == null || part.frames == null) continue;

            if (layerImages.TryGetValue(kvp.Key, out var img))
            {
                if (frameIndex >= 0 && frameIndex < part.frames.Length && part.frames[frameIndex] != null)
                    img.sprite = part.frames[frameIndex];
            }
        }
    }

    /// <summary>
    /// Applies a full CharacterAppearanceConfig to the preview.
    /// </summary>
    public void ApplyConfig(CharacterAppearanceConfig config)
    {
        if (config == null) return;

        SetPart(BodyPartSlot.Body, config.body);
        SetPart(BodyPartSlot.Head, config.head);
        SetPart(BodyPartSlot.Hair, config.hair);
        SetPart(BodyPartSlot.Torso, config.torso);
        SetPart(BodyPartSlot.Legs, config.legs);
        SetPart(BodyPartSlot.WeaponBehind, config.weaponBehind);
        SetPart(BodyPartSlot.WeaponFront, config.weaponFront);

        if (config.body != null && config.body.supportsTinting)
            SetTint(BodyPartSlot.Body, config.skinTint);
        if (config.head != null && config.head.supportsTinting)
            SetTint(BodyPartSlot.Head, config.skinTint);
        if (config.hair != null && config.hair.supportsTinting)
            SetTint(BodyPartSlot.Hair, config.hairTint);
        if (config.torso != null && config.torso.supportsTinting)
            SetTint(BodyPartSlot.Torso, config.armorPrimaryTint);
        if (config.legs != null && config.legs.supportsTinting)
            SetTint(BodyPartSlot.Legs, config.armorSecondaryTint);
    }

    /// <summary>
    /// Clears all layers.
    /// </summary>
    public void Clear()
    {
        if (layerImages == null) return;
        foreach (var slot in layerImages.Keys)
            SetPart(slot, null);
    }
}
