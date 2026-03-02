/// <summary>
/// Enemy state machine states.
/// </summary>
public enum EnemyState
{
    Idle,
    Patrol,
    Alert,
    Chase,
    Attack,
    Cooldown,
    Stunned,
    Dead
}

/// <summary>
/// Enemy movement/behavior type.
/// </summary>
public enum EnemyType
{
    GroundPatrol,
    Flying,
    Stationary,
    Hopping
}

/// <summary>
/// Player detection method.
/// </summary>
public enum DetectionType
{
    Radius,
    Cone,
    LineOfSight
}

/// <summary>
/// Combat role for encounter template slot matching.
/// </summary>
public enum CombatRole
{
    DPS,
    Tank,
    Support,
    Controller,
    Artillery
}

/// <summary>
/// How quickly this enemy threatens the player after spawning.
/// </summary>
public enum ThreatClock
{
    Immediate,
    ShortFuse,
    Delayed,
    Ambient
}
