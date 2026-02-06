using UnityEngine;

/// <summary>
/// Ground-based enemy movement that patrols back and forth,
/// turning at walls and ledges.
/// </summary>
public class GroundPatrolMovement : BaseEnemyMovement
{
    [Header("Patrol Settings")]
    [SerializeField] private float idleWaitTime = 1f;
    [SerializeField] private bool turnAtLedges = true;
    [SerializeField] private bool turnAtWalls = true;

    private float idleTimer;
    private bool isWaiting;

    protected override void Start()
    {
        base.Start();

        // Start facing a random direction
        patrolDirection = Random.value > 0.5f ? 1 : -1;
        controller?.FaceDirection(patrolDirection);
    }

    public override void Patrol()
    {
        if (!isPatrolling)
            return;

        // Only skip patrol if clearly airborne (significant downward velocity).
        // This prevents patrol from being blocked by imperfect ground detection
        // while still stopping mid-air movement when actually falling.
        if (!IsGrounded && rb != null && rb.linearVelocity.y < -1f)
            return;

        // Handle idle waiting at turn points
        if (isWaiting)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                isWaiting = false;
            }
            return;
        }

        // Check for obstacles
        bool shouldTurn = false;

        if (turnAtWalls && IsAtWall)
        {
            shouldTurn = true;
        }

        if (turnAtLedges && IsAtLedge)
        {
            shouldTurn = true;
        }

        if (shouldTurn)
        {
            Flip();
            isWaiting = true;
            idleTimer = idleWaitTime;
            // Zero velocity without resetting isPatrolling — patrol resumes after the wait
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // Move in patrol direction
        float speed = enemyData != null ? enemyData.moveSpeed : 3f;
        Move(patrolDirection, speed);
    }

    public override void ChaseTarget(Transform target)
    {
        if (target == null)
            return;

        // Determine direction to target
        float directionToTarget = Mathf.Sign(target.position.x - transform.position.x);

        // Only check wall/ledge obstacles when grounded (detection requires ground layer)
        if (IsGrounded)
        {
            bool blocked = false;

            // Check wall in direction of target
            if (turnAtWalls && IsAtWall)
            {
                float facingDir = Mathf.Sign(transform.localScale.x);
                if (Mathf.Sign(directionToTarget) == facingDir)
                {
                    blocked = true;
                }
            }

            // Check ledge in direction of target
            if (turnAtLedges && IsAtLedge)
            {
                float facingDir = Mathf.Sign(transform.localScale.x);
                if (Mathf.Sign(directionToTarget) == facingDir)
                {
                    blocked = true;
                }
            }

            if (blocked)
            {
                // Stop at edge, but still face target
                Stop();
                controller?.FaceTarget();
                return;
            }
        }

        // Chase the target
        float speed = enemyData != null ? enemyData.chaseSpeed : 5f;
        Move(directionToTarget, speed);
    }

    public override void Stop()
    {
        base.Stop();
        isWaiting = false;
    }

    public override void Flip()
    {
        base.Flip();

        // Update ledge check position to match new direction
        if (ledgeCheck != null)
        {
            Vector3 pos = ledgeCheck.localPosition;
            pos.x = Mathf.Abs(pos.x) * patrolDirection;
            ledgeCheck.localPosition = pos;
        }

        // Update wall check position
        if (wallCheck != null)
        {
            Vector3 pos = wallCheck.localPosition;
            pos.x = Mathf.Abs(pos.x) * patrolDirection;
            wallCheck.localPosition = pos;
        }
    }
}
