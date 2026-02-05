/// <summary>
/// Combat system enumerations.
/// </summary>

public enum AttackDirection
{
    Forward,
    Up,
    Down
}

public enum WeaponType
{
    Melee,
    Ranged,
    Magic
}

public enum CombatState
{
    Idle,
    WindUp,
    Attacking,
    Recovery
}
