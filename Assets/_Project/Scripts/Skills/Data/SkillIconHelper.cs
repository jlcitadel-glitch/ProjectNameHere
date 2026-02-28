using UnityEngine;

/// <summary>
/// Static utility for resolving skill icons and computing element-based tints.
/// Resolution order: direct skill.icon > database lookup by skill.iconId > null.
/// Tint order: manual iconTintOverride (if alpha > 0) > auto from DamageType > Color.white.
/// </summary>
public static class SkillIconHelper
{
    /// <summary>
    /// Resolves the best available icon sprite for a skill.
    /// Returns skill.icon if assigned, otherwise looks up skill.iconId in the database.
    /// </summary>
    public static Sprite ResolveIcon(SkillData skill)
    {
        if (skill == null) return null;

        // Direct sprite takes priority (backward compatible)
        if (skill.icon != null) return skill.icon;

        // Fall back to database lookup
        if (!string.IsNullOrEmpty(skill.iconId))
        {
            var db = SkillIconDatabase.Instance;
            if (db != null)
            {
                var entry = db.GetIconById(skill.iconId);
                if (entry != null) return entry.sprite;
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves the base tint color for a skill icon.
    /// Manual override (alpha > 0) takes priority, then auto-tint from DamageType,
    /// then Color.white as fallback.
    /// </summary>
    public static Color ResolveTint(SkillData skill)
    {
        if (skill == null) return Color.white;

        // Manual override — Color.clear (alpha=0) means "use auto"
        if (skill.iconTintOverride.a > 0f)
            return skill.iconTintOverride;

        // Auto-tint from DamageType if enabled
        if (skill.useAutoTint)
        {
            var (primary, _) = SkillVFXFactory.GetColors(skill.damageType);
            return primary;
        }

        return Color.white;
    }

    /// <summary>
    /// Multiplies a base tint with a state color (e.g. locked, cooldown, no-mana).
    /// </summary>
    public static Color ComposeTint(Color baseTint, Color stateColor)
    {
        return baseTint * stateColor;
    }
}
