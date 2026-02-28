using UnityEngine;

/// <summary>
/// A single entry in the skill icon database.
/// Maps an icon ID to a sprite and optional metadata for searching.
/// </summary>
[System.Serializable]
public class SkillIconEntry
{
    [Tooltip("Unique identifier (e.g. lorc/fire-bolt)")]
    public string iconId;

    [Tooltip("Human-readable display name")]
    public string displayName;

    [Tooltip("The icon sprite (white-on-transparent for tinting)")]
    public Sprite sprite;

    [Tooltip("Searchable tags (e.g. fire, projectile, magic)")]
    public string[] tags;
}
