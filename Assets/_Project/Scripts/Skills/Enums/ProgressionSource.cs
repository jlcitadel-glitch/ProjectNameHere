/// <summary>
/// How an ability or power is acquired. Tracks the origin of each
/// progression unlock for save/load and UI display.
/// </summary>
public enum ProgressionSource
{
    /// <summary>SP-based skill tree learning.</summary>
    Skill = 0,

    /// <summary>Absorbed from defeated enemies (chance drop, boss guaranteed).</summary>
    SoulAbsorption = 1,

    /// <summary>Channeled from environment/enemy patterns mid-combat.</summary>
    GlyphAbsorption = 2,

    /// <summary>Passive ability slotted into limited charm/notch capacity.</summary>
    CharmNotch = 3,

    /// <summary>Leveled through weapon use, mastery unlocks unique ability.</summary>
    WeaponProficiency = 4,

    /// <summary>Guaranteed reward from defeating a boss.</summary>
    BossReward = 5,

    /// <summary>Reward from completing a quest.</summary>
    QuestReward = 6,

    /// <summary>Found through exploration (hidden rooms, breakable walls).</summary>
    Exploration = 7,

    /// <summary>Innate to the character, always available.</summary>
    Innate = 8,

    /// <summary>Granted by job advancement.</summary>
    JobAdvancement = 9
}
