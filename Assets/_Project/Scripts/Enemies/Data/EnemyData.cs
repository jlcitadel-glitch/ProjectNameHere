using UnityEngine;

/// <summary>
/// ScriptableObject containing all configuration data for an enemy type.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "Enemy";
    public EnemyType enemyType = EnemyType.GroundPatrol;

    [Header("Health")]
    public float maxHealth = 30f;
    public float invulnerabilityDuration = 0.1f;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    [Tooltip("Layer used for ground/wall/ledge detection. If unset, auto-detects or uses layerless fallback.")]
    public LayerMask groundLayer;

    [Header("Contact Damage")]
    [Tooltip("Damage dealt when player touches the enemy. Set to 0 for no contact damage.")]
    public float contactDamage = 10f;
    public float contactKnockbackForce = 5f;

    [Header("Detection")]
    public DetectionType detectionType = DetectionType.Radius;
    [Tooltip("Range at which enemy detects the player")]
    public float detectionRange = 6f;
    [Tooltip("Angle for cone detection (ignored for radius detection)")]
    [Range(0f, 180f)]
    public float detectionAngle = 60f;
    [Tooltip("Range at which enemy loses aggro on player")]
    public float loseAggroRange = 10f;

    [Header("Combat")]
    public EnemyAttackData[] attacks;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    [Header("Stun/Knockback")]
    [Tooltip("0 = full knockback, 1 = immune to knockback")]
    [Range(0f, 1f)]
    public float knockbackResistance = 0f;
    public float stunDuration = 0.5f;

    [Header("Rewards")]
    public int experienceValue = 10;
    public GameObject[] dropPrefabs;
    [Range(0f, 1f)]
    public float dropChance = 0.5f;

    [Header("Hopping (Hopping type only)")]
    [Tooltip("Vertical force applied on each hop")]
    public float hopForce = 8f;
    [Tooltip("Horizontal speed during each hop")]
    public float hopHorizontalSpeed = 3f;
    [Tooltip("Pause between hops during patrol")]
    public float hopCooldown = 0.8f;
    [Tooltip("Pause between hops during chase (shorter = more aggressive)")]
    public float hopChaseCooldown = 0.4f;
    [Tooltip("Gravity multiplier while falling for snappy descent")]
    public float hopFallGravityMultiplier = 3f;

    [Header("Audio")]
    public AudioClip spawnSound;
    public AudioClip idleSound;
    public AudioClip alertSound;
    public AudioClip attackSound;
    public AudioClip hurtSound;
    public AudioClip deathSound;

    [Header("VFX")]
    public GameObject spawnVFX;
    public GameObject deathVFX;
    public GameObject hurtVFX;
}
