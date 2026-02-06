using UnityEngine;

/// <summary>
/// ScriptableObject defining properties for a single attack.
/// </summary>
[CreateAssetMenu(fileName = "NewAttack", menuName = "Combat/Attack Data")]
public class AttackData : ScriptableObject
{
    [Header("Identity")]
    public string attackName;
    public AttackDirection direction;
    public WeaponType weaponType;

    [Header("Damage")]
    public float baseDamage = 10f;
    public float knockbackForce = 5f;
    public Vector2 knockbackDirection = new Vector2(1f, 0.5f);

    [Header("Timing")]
    [Tooltip("Delay before attack becomes active")]
    public float windUpDuration = 0f;
    [Tooltip("How long the hitbox is active")]
    public float activeDuration = 0.15f;
    [Tooltip("Recovery time after attack before next action")]
    public float recoveryDuration = 0.2f;

    [Header("Hitbox")]
    public Vector2 hitboxSize = new Vector2(1.5f, 1f);
    public Vector2 hitboxOffset = new Vector2(1f, 0f);
    public LayerMask targetLayers;

    [Header("Resource")]
    public float manaCost = 0f;

    [Header("Combo")]
    [Tooltip("Time window to chain into next attack")]
    public float comboWindowDuration = 0.3f;
    [Tooltip("Next attack in combo chain (optional)")]
    public AttackData comboNextAttack;

    [Header("Projectile (Ranged/Magic)")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 15f;

    [Header("VFX")]
    public GameObject hitboxVFXPrefab;
    public GameObject impactVFXPrefab;
    public string animationTrigger;

    [Header("Audio")]
    [Tooltip("Sound played when attack starts")]
    public AudioClip attackSound;
    [Tooltip("Sound played when attack hits a target")]
    public AudioClip hitSound;

    [Header("Aerial")]
    public bool canUseInAir = true;
    [Tooltip("Bounce upward when hitting enemy with down attack")]
    public bool pogoOnDownHit = false;
    public float pogoForce = 10f;

    [Header("Movement")]
    [Tooltip("Can player move during this attack")]
    public bool allowMovement = false;
    [Tooltip("Movement speed multiplier during attack")]
    public float movementMultiplier = 0.5f;

    /// <summary>
    /// Total duration of the attack from start to finish.
    /// </summary>
    public float TotalDuration => windUpDuration + activeDuration + recoveryDuration;
}
