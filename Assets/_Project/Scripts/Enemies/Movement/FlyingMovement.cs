using UnityEngine;

/// <summary>
/// Flying enemy movement that hovers and chases in any direction.
/// Uses smooth movement for a floating effect.
/// </summary>
public class FlyingMovement : BaseEnemyMovement
{
    [Header("Flying Settings")]
    [SerializeField] private float hoverHeight = 2f;
    [SerializeField] private float hoverAmplitude = 0.5f;
    [SerializeField] private float hoverFrequency = 1f;
    [SerializeField] private float smoothTime = 0.3f;

    [Header("Patrol Bounds")]
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private bool usePatrolBounds = true;

    private Vector2 patrolCenter;
    private Vector2 patrolTarget;
    private Vector2 velocity;
    private float hoverOffset;
    private float patrolTimer;

    protected override void Start()
    {
        base.Start();

        // Store initial position as patrol center
        patrolCenter = transform.position;

        // Disable gravity for flying enemies
        if (rb != null)
        {
            rb.gravityScale = 0f;
        }

        // Set initial patrol target
        ChooseNewPatrolTarget();
    }

    protected override void FixedUpdate()
    {
        // Flying enemies don't need ground detection
        IsGrounded = false;
        IsAtWall = false;
        IsAtLedge = false;

        // Update hover offset
        hoverOffset = Mathf.Sin(Time.time * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;
    }

    public override void Patrol()
    {
        if (!isPatrolling)
            return;

        patrolTimer -= Time.deltaTime;

        // Choose new patrol target periodically
        if (patrolTimer <= 0f || Vector2.Distance(transform.position, patrolTarget) < 0.5f)
        {
            ChooseNewPatrolTarget();
        }

        // Move toward patrol target with hover
        Vector2 targetPos = patrolTarget + Vector2.up * hoverOffset;
        MoveToward(targetPos, enemyData != null ? enemyData.moveSpeed : 3f);
    }

    public override void ChaseTarget(Transform target)
    {
        if (target == null)
            return;

        // Calculate target position with some offset
        Vector2 targetPos = target.position;

        // Add slight hover offset during chase
        targetPos.y += hoverOffset * 0.5f;

        // Move toward target
        float speed = enemyData != null ? enemyData.chaseSpeed : 5f;
        MoveToward(targetPos, speed);

        // Face target
        controller?.FaceTarget();
    }

    public override void Stop()
    {
        isPatrolling = false;
        velocity = Vector2.zero;
        // Maintain current position with hover
        rb.linearVelocity = Vector2.zero;
    }

    private void MoveToward(Vector2 targetPos, float speed)
    {
        // Calculate direction
        Vector2 direction = (targetPos - (Vector2)transform.position).normalized;

        // Smooth damp toward target velocity
        Vector2 targetVelocity = direction * speed;
        Vector2 newVelocity = Vector2.SmoothDamp(
            rb.linearVelocity,
            targetVelocity,
            ref velocity,
            smoothTime
        );

        rb.linearVelocity = newVelocity;

        // Face movement direction (only horizontal)
        if (Mathf.Abs(newVelocity.x) > 0.1f)
        {
            controller?.FaceDirection(newVelocity.x);
        }
    }

    private void ChooseNewPatrolTarget()
    {
        if (usePatrolBounds)
        {
            // Choose random point within patrol radius
            Vector2 randomOffset = Random.insideUnitCircle * patrolRadius;
            patrolTarget = patrolCenter + randomOffset;
        }
        else
        {
            // Just wander in a direction
            patrolTarget = (Vector2)transform.position + Random.insideUnitCircle * 3f;
        }

        // Ensure target stays within hover height range
        float minY = patrolCenter.y + hoverHeight - hoverAmplitude;
        float maxY = patrolCenter.y + hoverHeight + hoverAmplitude;
        patrolTarget.y = Mathf.Clamp(patrolTarget.y, minY, maxY);

        patrolTimer = Random.Range(2f, 4f);
    }

    /// <summary>
    /// Sets the center point for patrol bounds.
    /// </summary>
    public void SetPatrolCenter(Vector2 center)
    {
        patrolCenter = center;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Draw patrol bounds
        if (usePatrolBounds)
        {
            Gizmos.color = Color.cyan;
            Vector3 center = Application.isPlaying ? (Vector3)patrolCenter : transform.position;
            Gizmos.DrawWireSphere(center, patrolRadius);
        }

        // Draw current patrol target
        if (Application.isPlaying && isPatrolling)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(patrolTarget, 0.3f);
            Gizmos.DrawLine(transform.position, patrolTarget);
        }
    }
}
