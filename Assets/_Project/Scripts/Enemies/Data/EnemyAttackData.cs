using UnityEngine;

/// <summary>
/// ScriptableObject defining a single enemy attack.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyAttack", menuName = "Enemies/Enemy Attack Data")]
public class EnemyAttackData : ScriptableObject
{
    [Header("Identity")]
    public string attackName = "Attack";

    [Header("Damage")]
    public float baseDamage = 10f;
    public float knockbackForce = 5f;
    public Vector2 knockbackDirection = new Vector2(1f, 0.5f);

    [Header("Timing")]
    [Tooltip("Time before attack becomes active (telegraph)")]
    public float windUpDuration = 0.2f;
    [Tooltip("Duration the hitbox is active")]
    public float activeDuration = 0.15f;
    [Tooltip("Time after attack before enemy can act again")]
    public float recoveryDuration = 0.3f;

    [Header("Hitbox")]
    public Vector2 hitboxSize = new Vector2(1f, 1f);
    public Vector2 hitboxOffset = new Vector2(1f, 0f);
    public LayerMask targetLayers;

    [Header("Projectile")]
    public bool isProjectile = false;
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float projectileLifetime = 5f;

    [Header("Range")]
    [Tooltip("Minimum range for this attack to be selected")]
    public float minRange = 0f;
    [Tooltip("Maximum range for this attack to be selected")]
    public float maxRange = 2f;

    [Header("Animation")]
    public string animationTrigger = "Attack";

    [Header("Audio/VFX")]
    public AudioClip attackSound;
    public GameObject windUpVFX;
    public GameObject attackVFX;
    public GameObject impactVFX;
}
