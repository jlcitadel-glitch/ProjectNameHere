using UnityEngine;

/// <summary>
/// ScriptableObject grouping attacks into a weapon.
/// </summary>
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string weaponName;
    public WeaponType weaponType;
    public Sprite weaponIcon;

    [Header("Attacks")]
    [Tooltip("Attack when pressing forward or neutral")]
    public AttackData forwardAttack;
    [Tooltip("Attack when holding up")]
    public AttackData upAttack;
    [Tooltip("Attack when holding down")]
    public AttackData downAttack;

    [Header("Aerial Overrides (Optional)")]
    [Tooltip("Different forward attack while airborne")]
    public AttackData aerialForwardAttack;
    [Tooltip("Different up attack while airborne")]
    public AttackData aerialUpAttack;
    [Tooltip("Different down attack while airborne")]
    public AttackData aerialDownAttack;

    /// <summary>
    /// Gets the appropriate attack based on direction and grounded state.
    /// </summary>
    public AttackData GetAttack(AttackDirection dir, bool grounded)
    {
        AttackData attack = null;

        // Check for aerial overrides first
        if (!grounded)
        {
            attack = dir switch
            {
                AttackDirection.Forward => aerialForwardAttack,
                AttackDirection.Up => aerialUpAttack,
                AttackDirection.Down => aerialDownAttack,
                _ => null
            };
        }

        // Fall back to standard attacks
        if (attack == null)
        {
            attack = dir switch
            {
                AttackDirection.Forward => forwardAttack,
                AttackDirection.Up => upAttack,
                AttackDirection.Down => downAttack,
                _ => forwardAttack
            };
        }

        return attack;
    }

    /// <summary>
    /// Returns true if this weapon has any attack defined.
    /// </summary>
    public bool HasAnyAttack()
    {
        return forwardAttack != null || upAttack != null || downAttack != null;
    }
}
