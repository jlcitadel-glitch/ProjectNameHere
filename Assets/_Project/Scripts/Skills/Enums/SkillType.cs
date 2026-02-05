/// <summary>
/// Defines the behavior type of a skill.
/// </summary>
public enum SkillType
{
    /// <summary>
    /// Active skill that must be manually triggered.
    /// Consumes mana and has a cooldown.
    /// </summary>
    Active,

    /// <summary>
    /// Passive skill that provides permanent stat bonuses.
    /// Always active once learned, no mana cost or cooldown.
    /// </summary>
    Passive,

    /// <summary>
    /// Toggle skill that can be turned on/off.
    /// May drain mana while active.
    /// </summary>
    Toggle,

    /// <summary>
    /// Buff skill that provides temporary stat bonuses.
    /// Has a duration and cooldown.
    /// </summary>
    Buff
}
