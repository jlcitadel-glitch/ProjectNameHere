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

        // Auto-create check points if not assigned
        SetupCheckPoints();
    }

    protected virtual void Start()
    {
        if (controller != null)
        {
            enemyData = controller.Data;
        }
    }

    protected virtual void FixedUpdate()
    {
        UpdateDetection();
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

        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        IsGrounded = false;
        foreach (Collider2D col in colliders)
        {
            if (col.gameObject != gameObject && !col.isTrigger)
            {
                IsGrounded = true;
                break;
            }
        }
    }

    protected virtual void CheckWall()
    {
        if (wallCheck == null)
        {
            IsAtWall = false;
            return;
        }

        Vector2 direction = new Vector2(transform.localScale.x, 0f).normalized;
        RaycastHit2D hit = Physics2D.Raycast(
            wallCheck.position,
            direction,
            wallCheckDistance,
            groundLayer
        );

        IsAtWall = hit.collider != null && !hit.collider.isTrigger;
    }

    protected virtual void CheckLedge()
    {
        if (ledgeCheck == null)
        {
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
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
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
