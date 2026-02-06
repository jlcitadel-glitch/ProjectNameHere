using System;
using UnityEngine;

/// <summary>
/// Player detection component for enemies.
/// Supports radius, cone, and line-of-sight detection types.
/// </summary>
public class EnemySensors : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private string targetTag = "Player";

    [Header("Sensor Origin")]
    [SerializeField] private Transform sensorOrigin;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;
    [SerializeField] private bool showGizmos = true;

    private EnemyController controller;
    private EnemyData enemyData;
    private Transform currentTarget;
    private bool hasTarget;

    public Transform CurrentTarget => currentTarget;
    public bool HasTarget => hasTarget;

    public event Action<Transform> OnTargetDetected;
    public event Action OnTargetLost;

    private void Awake()
    {
        controller = GetComponent<EnemyController>();

        if (sensorOrigin == null)
        {
            sensorOrigin = transform;
        }
    }

    private void Start()
    {
        if (controller != null)
        {
            enemyData = controller.Data;
        }

        // Set default target layer if not set
        if (targetLayers == 0)
        {
            targetLayers = LayerMask.GetMask("Player");
        }

        // Set default obstacle layer if not set
        if (obstacleLayers == 0)
        {
            obstacleLayers = LayerMask.GetMask("Ground");
        }
    }

    private void Update()
    {
        if (enemyData == null)
            return;

        // Skip detection if dead or stunned
        if (controller != null && (controller.IsDead || controller.IsStunned))
            return;

        CheckForTargets();
    }

    private void CheckForTargets()
    {
        Transform detectedTarget = null;

        switch (enemyData.detectionType)
        {
            case DetectionType.Radius:
                detectedTarget = DetectByRadius();
                break;

            case DetectionType.Cone:
                detectedTarget = DetectByCone();
                break;

            case DetectionType.LineOfSight:
                detectedTarget = DetectByLineOfSight();
                break;
        }

        // Handle target state changes
        if (detectedTarget != null && !hasTarget)
        {
            // New target detected
            currentTarget = detectedTarget;
            hasTarget = true;

            if (debugLogging)
            {
                Debug.Log($"[EnemySensors] {gameObject.name}: Target detected - {detectedTarget.name}");
            }

            OnTargetDetected?.Invoke(currentTarget);
        }
        else if (detectedTarget == null && hasTarget)
        {
            // Lost target
            currentTarget = null;
            hasTarget = false;

            if (debugLogging)
            {
                Debug.Log($"[EnemySensors] {gameObject.name}: Target lost");
            }

            OnTargetLost?.Invoke();
        }
        else if (detectedTarget != null)
        {
            // Update current target reference
            currentTarget = detectedTarget;
        }
    }

    private Transform DetectByRadius()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            sensorOrigin.position,
            enemyData.detectionRange,
            targetLayers
        );

        foreach (Collider2D col in colliders)
        {
            if (col.isTrigger)
                continue;

            if (IsValidTarget(col.transform))
            {
                return col.transform;
            }
        }

        return null;
    }

    private Transform DetectByCone()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            sensorOrigin.position,
            enemyData.detectionRange,
            targetLayers
        );

        float facingDir = controller != null ? controller.FacingDirection : 1f;
        Vector2 forward = new Vector2(facingDir, 0f);

        foreach (Collider2D col in colliders)
        {
            if (col.isTrigger)
                continue;

            if (!IsValidTarget(col.transform))
                continue;

            // Check if target is within cone angle
            Vector2 dirToTarget = ((Vector2)col.transform.position - (Vector2)sensorOrigin.position).normalized;
            float angle = Vector2.Angle(forward, dirToTarget);

            if (angle <= enemyData.detectionAngle / 2f)
            {
                return col.transform;
            }
        }

        return null;
    }

    private Transform DetectByLineOfSight()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            sensorOrigin.position,
            enemyData.detectionRange,
            targetLayers
        );

        foreach (Collider2D col in colliders)
        {
            if (col.isTrigger)
                continue;

            if (!IsValidTarget(col.transform))
                continue;

            // Check line of sight
            Vector2 dirToTarget = (Vector2)col.transform.position - (Vector2)sensorOrigin.position;
            float distance = dirToTarget.magnitude;

            RaycastHit2D hit = Physics2D.Raycast(
                sensorOrigin.position,
                dirToTarget.normalized,
                distance,
                obstacleLayers
            );

            // If raycast didn't hit anything, we have line of sight
            if (hit.collider == null)
            {
                return col.transform;
            }
        }

        return null;
    }

    private bool IsValidTarget(Transform target)
    {
        if (target == null)
            return false;

        // Check tag if specified
        if (!string.IsNullOrEmpty(targetTag))
        {
            if (!target.CompareTag(targetTag))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Manually set the current target.
    /// </summary>
    public void SetTarget(Transform target)
    {
        if (target == null)
        {
            ClearTarget();
            return;
        }

        currentTarget = target;
        hasTarget = true;
        OnTargetDetected?.Invoke(currentTarget);
    }

    /// <summary>
    /// Clear the current target.
    /// </summary>
    public void ClearTarget()
    {
        if (hasTarget)
        {
            currentTarget = null;
            hasTarget = false;
            OnTargetLost?.Invoke();
        }
    }

    /// <summary>
    /// Check if a specific position is within detection range.
    /// </summary>
    public bool IsInDetectionRange(Vector2 position)
    {
        float distance = Vector2.Distance(sensorOrigin.position, position);
        return distance <= enemyData.detectionRange;
    }

    /// <summary>
    /// Check if a specific position has line of sight.
    /// </summary>
    public bool HasLineOfSight(Vector2 position)
    {
        Vector2 direction = position - (Vector2)sensorOrigin.position;
        float distance = direction.magnitude;

        RaycastHit2D hit = Physics2D.Raycast(
            sensorOrigin.position,
            direction.normalized,
            distance,
            obstacleLayers
        );

        return hit.collider == null;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        Transform origin = sensorOrigin != null ? sensorOrigin : transform;

        // Get detection range from enemy data or use default
        float range = 6f;
        DetectionType type = DetectionType.Radius;
        float angle = 60f;

        if (enemyData != null)
        {
            range = enemyData.detectionRange;
            type = enemyData.detectionType;
            angle = enemyData.detectionAngle;
        }

        Gizmos.color = hasTarget ? Color.red : Color.yellow;

        switch (type)
        {
            case DetectionType.Radius:
                Gizmos.DrawWireSphere(origin.position, range);
                break;

            case DetectionType.Cone:
                DrawConeGizmo(origin.position, range, angle);
                break;

            case DetectionType.LineOfSight:
                Gizmos.DrawWireSphere(origin.position, range);
                // Draw line to current target if we have one
                if (hasTarget && currentTarget != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(origin.position, currentTarget.position);
                }
                break;
        }
    }

    private void DrawConeGizmo(Vector3 position, float range, float angle)
    {
        float facingDir = transform.localScale.x >= 0 ? 1f : -1f;
        Vector3 forward = new Vector3(facingDir, 0f, 0f);

        float halfAngle = angle / 2f;

        // Draw cone edges
        Vector3 leftEdge = Quaternion.Euler(0f, 0f, halfAngle) * forward * range;
        Vector3 rightEdge = Quaternion.Euler(0f, 0f, -halfAngle) * forward * range;

        Gizmos.DrawLine(position, position + leftEdge);
        Gizmos.DrawLine(position, position + rightEdge);

        // Draw arc
        int segments = 20;
        float angleStep = angle / segments;
        Vector3 previousPoint = position + leftEdge;

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = halfAngle - (angleStep * i);
            Vector3 point = position + Quaternion.Euler(0f, 0f, currentAngle) * forward * range;
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }
    }
}
