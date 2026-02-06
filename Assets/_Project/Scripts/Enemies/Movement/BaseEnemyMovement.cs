using UnityEngine;

/// <summary>
/// Abstract base class for enemy movement behaviors.
/// Provides ground/wall/ledge detection and common movement utilities.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class BaseEnemyMovement : MonoBehaviour
{
    [Header("Ground Detection")]
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected float groundCheckRadius = 0.2f;
    [SerializeField] protected LayerMask groundLayer;

    [Header("Wall Detection")]
    [SerializeField] protected Transform wallCheck;
    [SerializeField] protected float wallCheckDistance = 0.5f;

    [Header("Ledge Detection")]
    [SerializeField] protected Transform ledgeCheck;
    [SerializeField] protected float ledgeCheckDistance = 0.5f;

    protected Rigidbody2D rb;
    protected EnemyController controller;
    protected BossController bossController;
    protected EnemyData enemyData;

    protected bool isPatrolling;
    protected int patrolDirection = 1;

    public bool IsGrounded { get; protected set; }
    public bool IsAtWall { get; protected set; }
    public bool IsAtLedge { get; protected set; }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<EnemyController>();
        bossController = GetComponent<BossController>();

        // Ensure dynamic body type for physics-based movement
        if (rb != null && rb.bodyType != RigidbodyType2D.Dynamic)
        {
            Debug.LogWarning($"[BaseEnemyMovement] {gameObject.name}: Rigidbody2D was {rb.bodyType}, set to Dynamic.");
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        // Ensure gravity is enabled for ground enemies only
        // Flying enemies intentionally have gravityScale = 0
        if (rb != null && rb.gravityScale <= 0f && !(this is FlyingMovement))
        {
            Debug.LogWarning($"[BaseEnemyMovement] {gameObject.name}: gravityScale was {rb.gravityScale}, set to 1.");
            rb.gravityScale = 1f;
        }

        // Clear position-freeze constraints that prevent falling or horizontal movement
        if (rb != null)
        {
            var freezePos = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY;
            if ((rb.constraints & freezePos) != 0)
            {
                Debug.LogWarning($"[BaseEnemyMovement] {gameObject.name}: Rigidbody2D had frozen position axes, cleared for movement.");
                rb.constraints &= ~freezePos;
            }
        }

        // Auto-detect ground layer if not assigned
        if (groundLayer == 0)
        {
            TryAutoDetectGroundLayer();
        }

        // Auto-create check points if not assigned
        SetupCheckPoints();
    }

    protected virtual void Start()
    {
        if (controller != null)
        {
            enemyData = controller.Data;
        }

        // If groundLayer wasn't set on the component (e.g. added via AddComponent at runtime),
        // read it from the EnemyData ScriptableObject — which IS configured in the Inspector.
        if (groundLayer == 0 && enemyData != null && enemyData.groundLayer != 0)
        {
            groundLayer = enemyData.groundLayer;
        }
    }

    protected virtual void FixedUpdate()
    {
        UpdateDetection();
    }

    private void TryAutoDetectGroundLayer()
    {
        string[] layerNames = { "Ground", "ground", "Terrain", "Platform", "Environment" };
        foreach (string layerName in layerNames)
        {
            int index = LayerMask.NameToLayer(layerName);
            if (index >= 0)
            {
                groundLayer = 1 << index;
                Debug.LogWarning($"[BaseEnemyMovement] {gameObject.name}: groundLayer was empty, auto-detected '{layerName}' layer.");
                return;
            }
        }
        Debug.LogError($"[BaseEnemyMovement] {gameObject.name}: groundLayer not assigned and no common ground layer found! Enemy movement will use velocity-based fallback.");
    }

    private void SetupCheckPoints()
    {
        // Ground check
        if (groundCheck == null)
        {
            groundCheck = transform.Find("GroundCheck");
            if (groundCheck == null)
            {
                GameObject go = new GameObject("GroundCheck");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(0f, -0.5f, 0f);
                groundCheck = go.transform;
            }
        }

        // Wall check
        if (wallCheck == null)
        {
            wallCheck = transform.Find("WallCheck");
            if (wallCheck == null)
            {
                GameObject go = new GameObject("WallCheck");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(0.5f, 0f, 0f);
                wallCheck = go.transform;
            }
        }

        // Ledge check
        if (ledgeCheck == null)
        {
            ledgeCheck = transform.Find("LedgeCheck");
            if (ledgeCheck == null)
            {
                GameObject go = new GameObject("LedgeCheck");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(0.5f, -0.5f, 0f);
                ledgeCheck = go.transform;
            }
        }
    }

    protected virtual void UpdateDetection()
    {
        CheckGround();
        CheckWall();
        CheckLedge();
    }

    protected virtual void CheckGround()
    {
        if (groundCheck == null)
        {
            IsGrounded = false;
            return;
        }

        // Primary: layer-based overlap check
        if (groundLayer != 0)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            );

            foreach (Collider2D col in colliders)
            {
                if (col.gameObject != gameObject && !col.isTrigger)
                {
                    IsGrounded = true;
                    return;
                }
            }
        }

        // Secondary: layerless overlap — catches ground on any layer when
        // groundLayer is misconfigured or auto-detection failed
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(
                groundCheck.position,
                groundCheckRadius
            );

            foreach (Collider2D col in colliders)
            {
                if (col.gameObject != gameObject && !col.isTrigger)
                {
                    IsGrounded = true;
                    return;
                }
            }
        }

        // Last resort: velocity-based detection — if barely moving vertically
        // with gravity active, the enemy is resting on a surface
        IsGrounded = rb != null && rb.gravityScale > 0f && Mathf.Abs(rb.linearVelocity.y) < 0.5f;
    }

    protected virtual void CheckWall()
    {
        if (wallCheck == null || groundLayer == 0)
        {
            // Can't detect walls without a ground layer — skip to avoid
            // false positives (e.g. detecting other enemies as walls).
            // Physics collisions still prevent walking through walls.
            IsAtWall = false;
            return;
        }

        Vector2 direction = new Vector2(transform.localScale.x, 0f).normalized;

        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, direction, wallCheckDistance, groundLayer);

        IsAtWall = hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger;
    }

    protected virtual void CheckLedge()
    {
        if (ledgeCheck == null || groundLayer == 0)
        {
            // Can't detect ledges without a ground layer — assume no ledge
            // to avoid false positives that block all movement
            IsAtLedge = false;
            return;
        }

        RaycastHit2D hit = Physics2D.Raycast(
            ledgeCheck.position,
            Vector2.down,
            ledgeCheckDistance,
            groundLayer
        );

        // At ledge if there's NO ground ahead
        IsAtLedge = hit.collider == null;
    }

    /// <summary>
    /// Start patrolling behavior.
    /// </summary>
    public virtual void StartPatrol()
    {
        isPatrolling = true;
    }

    /// <summary>
    /// Execute patrol movement. Called each frame during patrol state.
    /// </summary>
    public abstract void Patrol();

    /// <summary>
    /// Chase the specified target. Called each frame during chase state.
    /// </summary>
    public abstract void ChaseTarget(Transform target);

    /// <summary>
    /// Stop all movement.
    /// </summary>
    public virtual void Stop()
    {
        isPatrolling = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    /// <summary>
    /// Flip the enemy to face the opposite direction.
    /// </summary>
    public virtual void Flip()
    {
        patrolDirection *= -1;
        controller?.FaceDirection(patrolDirection);
    }

    /// <summary>
    /// Move in the specified direction at the given speed.
    /// </summary>
    protected virtual void Move(float direction, float speed)
    {
        float bossSpeedMult = bossController != null ? bossController.GetSpeedMultiplier() : 1f;
        rb.linearVelocity = new Vector2(direction * speed * bossSpeedMult, rb.linearVelocity.y);
        controller?.FaceDirection(direction);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // Ground check
        if (groundCheck != null)
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Wall check
        if (wallCheck != null)
        {
            Gizmos.color = IsAtWall ? Color.red : Color.green;
            Vector3 direction = new Vector3(transform.localScale.x, 0f, 0f).normalized;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + direction * wallCheckDistance);
        }

        // Ledge check
        if (ledgeCheck != null)
        {
            Gizmos.color = IsAtLedge ? Color.red : Color.green;
            Gizmos.DrawLine(ledgeCheck.position, ledgeCheck.position + Vector3.down * ledgeCheckDistance);
        }
    }
}
