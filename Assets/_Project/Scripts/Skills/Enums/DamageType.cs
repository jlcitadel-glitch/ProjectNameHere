/// <summary>
/// Defines the elemental or physical type of damage.
/// Used for damage calculations and resistances.
/// </summary>
public enum DamageType
{
    /// <summary>
    /// Standard physical damage. Scales with strength.
    /// </summary>
    Physical,

    /// <summary>
    /// Pure magical damage. Scales with intelligence.
    /// </summary>
    Magic,

    /// <summary>
    /// Fire elemental damage. Burns enemies over time.
    /// </summary>
    Fire,

    /// <summary>
    /// Ice elemental damage. May slow or freeze enemies.
    /// </summary>
    Ice,

    /// <summary>
    /// Lightning elemental damage. May stun or chain to nearby enemies.
    /// </summary>
    Lightning,

    /// <summary>
    /// Poison damage. Deals damage over time.
    /// </summary>
    Poison,

    /// <summary>
    /// Dark/Shadow damage. May reduce healing or vision.
    /// </summary>
    Dark,

    /// <summary>
    /// Holy/Light damage. Effective against undead.
    /// </summary>
    Holy,

    /// <summary>
    /// True damage. Ignores all resistances and defenses.
    /// </summary>
    True
}
