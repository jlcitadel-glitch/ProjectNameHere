using UnityEngine;

/// <summary>
/// Determines which color channel a body part uses for tinting.
/// Allows data-driven tint assignment instead of per-slot switch statements.
/// </summary>
public enum TintCategory
{
    None,
    Skin,
    Hair,
    ArmorPrimary,
    ArmorSecondary
}

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

    [Tooltip("Body type this part is compatible with (e.g., male, female, universal)")]
    public string bodyTypeTag = "universal";

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

    [Tooltip("Which tint color category this part uses when applied via config")]
    public TintCategory tintCategory = TintCategory.None;

    [Header("Exclusivity")]
    [Tooltip("Parts with the same exclusiveGroup auto-hide each other (e.g., Hat hides Hair)")]
    public string exclusiveGroup;

    [Header("Preview")]
    [Tooltip("Single sprite for UI preview (typically the first idle frame)")]
    public Sprite previewSprite;
}
