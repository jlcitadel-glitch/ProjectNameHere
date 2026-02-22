using UnityEngine;

/// <summary>
/// ScriptableObject defining a single body part option (e.g., one hair style, one armor piece).
/// The frames array must follow the same indexing convention as all other parts
/// so that frame N of body matches frame N of hair matches frame N of armor.
/// </summary>
[CreateAssetMenu(fileName = "NewBodyPart", menuName = "Game/Character/Body Part")]
public class BodyPartData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique ID for save/load resolution")]
    public string partId;

    [Tooltip("Display name shown in character creation")]
    public string displayName;

    [Tooltip("Which slot this part occupies")]
    public BodyPartSlot slot;

    [Header("Sprites")]
    [Tooltip("All animation frames in order. Indexing must match the AnimationStateFrameMap layout.")]
    public Sprite[] frames;

    [Header("Rendering")]
    [Tooltip("Additional sort order offset within the slot (for fine-tuning layering)")]
    public int sortOrderOffset;

    [Header("Tinting")]
    [Tooltip("Whether this part supports color tinting (e.g., hair color, skin tone)")]
    public bool supportsTinting;
    public Color defaultTint = Color.white;

    [Header("Preview")]
    [Tooltip("Single sprite for UI preview (typically the first idle frame)")]
    public Sprite previewSprite;
}
