/// <summary>
/// Represents the advancement tier of a job class.
/// Higher tiers unlock more powerful skills and abilities.
/// </summary>
public enum JobTier
{
    /// <summary>
    /// Starting class with basic skills. Selected at character creation.
    /// </summary>
    Beginner = 0,

    /// <summary>
    /// First job class (Warrior, Mage, Rogue). Selected at character creation.
    /// Skills unlock through levels 1-15, all maxed by level 20.
    /// </summary>
    First = 1,

    /// <summary>
    /// Second job advancement. Unlocks at level 20.
    /// Specialization within archetype or hybrid cross-class.
    /// </summary>
    Second = 2,

    /// <summary>
    /// Third job advancement. Unlocks at level 60.
    /// Deep specialization or advanced hybrid mastery.
    /// </summary>
    Third = 3,

    /// <summary>
    /// Fourth and final job advancement. Reserved for future use.
    /// </summary>
    Fourth = 4
}
