using UnityEngine;

/// <summary>
/// Hop-based enemy movement for slime-type enemies.
/// Moves by rhythmic arcing hops instead of sliding along the ground.
/// Grounded pauses between hops, with a snappy fall via gravity multiplier.
/// </summary>
public class HoppingMovement : BaseEnemyMovement
{
    private bool isHopping;
    private float hopTimer;
    private bool isChasing;
    private Transform chaseTarget;
    private float baseGravityScale;

    protected override void Start()
    {
        base.Start();

        baseGravityScale = rb.gravityScale;

        // Start facing a random direction
        patrolDirection = Random.value > 0.5f ? 1 : -1;
        controller?.FaceDirection(patrolDirection);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // Apply fall gravity multiplier for snappy descent
        if (isHopping && rb.linearVelocity.y < 0f)
        {
            float fallMult = enemyData != null ? enemyData.hopFallGravityMultiplier : 3f;
            rb.gravityScale = baseGravityScale * fallMult;
        }
        else
        {
            rb.gravityScale = baseGravityScale;
        }

        // Detect landing
        if (isHopping && IsGrounded && rb.linearVelocity.y <= 0f)
        {
            Land();
        }
    }

    public override void Patrol()
    {
        if (!isPatrolling)
            return;

        // While airborne during a hop, maintain horizontal velocity
        if (isHopping)
            return;

        // Count down hop cooldown while grounded
        hopTimer -= Time.deltaTime;
        if (hopTimer > 0f)
        {
            // Sitting still between hops — zero horizontal velocity
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // Check for walls/ledges before hopping
        if (IsAtWall || IsAtLedge)
        {
            Flip();
        }

        // Hop
        ExecuteHop(patrolDirection, false);
    }

    public override void ChaseTarget(Transform target)
    {
        if (target == null)
            return;

        chaseTarget = target;
        isChasing = true;

        // While airborne during a hop, maintain horizontal velocity
        if (isHopping)
            return;

        // Count down hop cooldown while grounded
        hopTimer -= Time.deltaTime;
        if (hopTimer > 0f)
        {
            // Sitting still between hops — zero horizontal velocity, but face target
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            controller?.FaceTarget();
            return;
        }

        float directionToTarget = Mathf.Sign(target.position.x - transform.position.x);

        // Check if blocked by wall/ledge in the chase direction
        if (IsAtWall || IsAtLedge)
        {
            float facingDir = Mathf.Sign(transform.localScale.x);
            if (Mathf.Sign(directionToTarget) == facingDir)
            {
                // Blocked — stop but face target
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                controller?.FaceTarget();
                return;
            }
        }

        // Hop toward target
        ExecuteHop(directionToTarget, true);
    }

    private void ExecuteHop(float direction, bool chasing)
    {
        if (!IsGrounded)
            return;

        isHopping = true;

        float hopForce = enemyData != null ? enemyData.hopForce : 8f;
        float hopSpeed = enemyData != null ? enemyData.hopHorizontalSpeed : 3f;

        // Apply boss speed multiplier if applicable
        float bossSpeedMult = bossController != null ? bossController.GetSpeedMultiplier() : 1f;

        // Use chase speed for horizontal component if chasing
        if (chasing && enemyData != null)
        {
            hopSpeed = enemyData.chaseSpeed;
        }

        rb.linearVelocity = new Vector2(direction * hopSpeed * bossSpeedMult, hopForce);
        controller?.FaceDirection(direction);
    }

    private void Land()
    {
        isHopping = false;

        // Zero horizontal velocity on landing
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Reset gravity to base
        rb.gravityScale = baseGravityScale;

        // Set cooldown based on whether we're chasing or patrolling
        if (isChasing && chaseTarget != null)
        {
            hopTimer = enemyData != null ? enemyData.hopChaseCooldown : 0.4f;
        }
        else
        {
            hopTimer = enemyData != null ? enemyData.hopCooldown : 0.8f;
        }
    }

    public override void Stop()
    {
        base.Stop();
        isChasing = false;
        chaseTarget = null;
        hopTimer = 0f;

        // Don't clear isHopping — let the enemy land naturally if mid-air
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
