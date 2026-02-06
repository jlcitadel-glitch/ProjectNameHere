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
    Stationary
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
