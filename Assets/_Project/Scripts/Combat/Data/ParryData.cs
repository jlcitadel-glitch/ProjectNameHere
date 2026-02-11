using UnityEngine;

/// <summary>
/// ScriptableObject defining parry behavior per job class.
/// </summary>
[CreateAssetMenu(fileName = "NewParryData", menuName = "Combat/Parry Data")]
public class ParryData : ScriptableObject
{
    [Header("Type")]
    [Tooltip("Which parry variant this class uses")]
    public ParryType parryType = ParryType.ClassicParry;

    [Header("Timing")]
    [Tooltip("Duration of the active parry window after pressing the button")]
    public float parryWindowDuration = 0.15f;

    [Tooltip("Cooldown between parries")]
    public float cooldown = 1.0f;

    [Header("Counter Effects")]
    [Tooltip("Damage multiplier reflected back to the attacker (x attack damage)")]
    public float counterDamageMultiplier = 0.5f;

    [Tooltip("Duration the attacker is stunned on successful parry")]
    public float enemyStunDuration = 1.0f;

    [Header("Invulnerability")]
    [Tooltip("Whether this parry grants i-frames")]
    public bool grantsInvulnerability = false;

    [Tooltip("Duration of i-frames when granted")]
    public float invulnerabilityDuration = 0f;

    [Header("Projectile")]
    [Tooltip("Whether this parry can reflect projectiles back at enemies")]
    public bool canReflectProjectiles = false;

    [Header("Shadow Step")]
    [Tooltip("Distance to teleport behind the attacker (Rogue only)")]
    public float shadowStepDistance = 0f;

    [Header("Audio/VFX")]
    [Tooltip("Sound played on successful parry")]
    public AudioClip parrySound;

    [Tooltip("VFX spawned on successful parry")]
    public GameObject parryVFXPrefab;
}
