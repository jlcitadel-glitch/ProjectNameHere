/// <summary>
/// Represents the advancement tier of a job class.
/// Higher tiers unlock more powerful skills.
/// </summary>
public enum JobTier
{
    /// <summary>
    /// Starting class with basic skills.
    /// </summary>
    Beginner = 0,

    /// <summary>
    /// First job advancement. Unlocks at level 10.
    /// </summary>
    First = 1,

    /// <summary>
    /// Second job advancement. Unlocks at level 30.
    /// </summary>
    Second = 2,

    /// <summary>
    /// Third job advancement. Unlocks at level 60.
    /// </summary>
    Third = 3,

    /// <summary>
    /// Fourth and final job advancement. Unlocks at level 100.
    /// </summary>
    Fourth = 4
}
