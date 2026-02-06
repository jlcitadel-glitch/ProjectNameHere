using UnityEngine;

public class AdvancedCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform target;

    [Header("Follow Settings")]
    [SerializeField] float smoothSpeed = 0.2f;
    [SerializeField] Vector3 offset = new Vector3(0, 0, -10f);

    [Header("Look Ahead")]
    [SerializeField] float lookAheadDistance = 2f;
    [SerializeField] float lookAheadSmooth = 5f;

    [Header("Fall Look Down")]
    [SerializeField] float fallLookDownDistance = 3f;
    [SerializeField] float fallLookDownSpeed = 2f;
    [SerializeField] float minFallVelocity = -5f; // How fast player must be falling

    [Header("Camera Bounds (Level Edges)")]
    [SerializeField] bool useBounds = true;
    [Tooltip("Left edge of level (ground sprites)")]
    [SerializeField] float minX = -100f;
    [Tooltip("Right edge of level (ground sprites)")]
    [SerializeField] float maxX = 100f;
    [Tooltip("Bottom edge of level (ground sprites)")]
    [SerializeField] float minY = -100f;
    [Tooltip("Top edge of level (ground sprites)")]
    [SerializeField] float maxY = 100f;

    [Header("Room Lock (Boss Fights)")]
    [SerializeField] bool isLockedToRoom = false;
    [SerializeField] Vector3 lockedRoomCenter;
    [SerializeField] float roomLockSpeed = 3f;

    private Vector3 currentVelocity;
    private float currentLookAhead;
    private float currentFallOffset;
    private Rigidbody2D targetRb;
    private Camera cam;

    // Shake state
    private float shakeTimer;
    private float shakeDuration;
    private float shakeMagnitude;

    void Awake()
    {
        cam = GetComponent<Camera>();

        if (target)
        {
            targetRb = target.GetComponent<Rigidbody2D>();
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        Vector3 desiredPosition;

        // If locked to room (boss fight), smoothly move to room center
        if (isLockedToRoom)
        {
            desiredPosition = lockedRoomCenter;
            desiredPosition.z = offset.z;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, roomLockSpeed * Time.deltaTime);

            // Apply shake even during room lock
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                float decay = Mathf.Clamp01(shakeTimer / shakeDuration);
                transform.position += new Vector3(
                    Random.Range(-1f, 1f) * shakeMagnitude * decay,
                    Random.Range(-1f, 1f) * shakeMagnitude * decay,
                    0f
                );
            }
            return;
        }

        // Normal follow behavior
        desiredPosition = CalculateDesiredPosition();

        // Apply bounds
        if (useBounds)
        {
            desiredPosition = ApplyBounds(desiredPosition);
        }

        // Smooth movement
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothSpeed);

        // Apply shake offset
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float decay = Mathf.Clamp01(shakeTimer / shakeDuration);
            Vector3 shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * shakeMagnitude * decay,
                Random.Range(-1f, 1f) * shakeMagnitude * decay,
                0f
            );
            transform.position += shakeOffset;
        }
    }

    Vector3 CalculateDesiredPosition()
    {
        Vector3 basePosition = target.position + offset;

        // Look ahead based on player facing direction
        float targetLookAhead = target.localScale.x * lookAheadDistance;
        currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, lookAheadSmooth * Time.deltaTime);
        basePosition.x += currentLookAhead;

        // Look down when falling
        if (targetRb && targetRb.linearVelocity.y < minFallVelocity)
        {
            float fallSpeed = Mathf.Abs(targetRb.linearVelocity.y);
            float targetFallOffset = Mathf.Lerp(0, fallLookDownDistance, fallSpeed / 20f);
            currentFallOffset = Mathf.Lerp(currentFallOffset, targetFallOffset, fallLookDownSpeed * Time.deltaTime);
        }
        else
        {
            currentFallOffset = Mathf.Lerp(currentFallOffset, 0, fallLookDownSpeed * Time.deltaTime);
        }

        basePosition.y -= currentFallOffset;

        return basePosition;
    }

    Vector3 ApplyBounds(Vector3 position)
    {
        // Calculate camera's half-extents based on orthographic size
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        // Clamp so the camera view never exceeds level bounds
        float clampedMinX = minX + halfWidth;
        float clampedMaxX = maxX - halfWidth;
        float clampedMinY = minY + halfHeight;
        float clampedMaxY = maxY - halfHeight;

        // Handle case where level is smaller than camera view
        if (clampedMinX > clampedMaxX)
        {
            position.x = (minX + maxX) / 2f; // Center camera on level
        }
        else
        {
            position.x = Mathf.Clamp(position.x, clampedMinX, clampedMaxX);
        }

        if (clampedMinY > clampedMaxY)
        {
            position.y = (minY + maxY) / 2f; // Center camera on level
        }
        else
        {
            position.y = Mathf.Clamp(position.y, clampedMinY, clampedMaxY);
        }

        return position;
    }

    // Call this to lock camera to a room (boss fight)
    public void LockToRoom(Vector3 roomCenter)
    {
        isLockedToRoom = true;
        lockedRoomCenter = roomCenter;
        lockedRoomCenter.z = offset.z;
    }

    // Call this to unlock camera
    public void UnlockCamera()
    {
        isLockedToRoom = false;
    }

    /// <summary>
    /// Triggers a camera shake effect.
    /// </summary>
    public void Shake(float magnitude, float duration)
    {
        if (duration <= 0f || magnitude <= 0f)
            return;

        // Only override if new shake is stronger than remaining shake
        if (shakeTimer > 0f && shakeMagnitude * (shakeTimer / shakeDuration) > magnitude)
            return;

        shakeMagnitude = magnitude;
        shakeDuration = duration;
        shakeTimer = duration;
    }

    // Update bounds dynamically (for progression-based area reveals)
    public void SetBounds(float newMinX, float newMaxX, float newMinY, float newMaxY)
    {
        minX = newMinX;
        maxX = newMaxX;
        minY = newMinY;
        maxY = newMaxY;
    }

    // Visualize bounds in editor
    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;

        // Draw level bounds (yellow) - the actual edges of your level
        Gizmos.color = Color.yellow;
        Vector3 bottomLeft = new Vector3(minX, minY, 0);
        Vector3 bottomRight = new Vector3(maxX, minY, 0);
        Vector3 topLeft = new Vector3(minX, maxY, 0);
        Vector3 topRight = new Vector3(maxX, maxY, 0);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        // Draw camera movement bounds (cyan) - where camera center can move
        if (cam == null) cam = GetComponent<Camera>();
        if (cam != null)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            Gizmos.color = Color.cyan;
            Vector3 camBL = new Vector3(minX + halfWidth, minY + halfHeight, 0);
            Vector3 camBR = new Vector3(maxX - halfWidth, minY + halfHeight, 0);
            Vector3 camTL = new Vector3(minX + halfWidth, maxY - halfHeight, 0);
            Vector3 camTR = new Vector3(maxX - halfWidth, maxY - halfHeight, 0);

            Gizmos.DrawLine(camBL, camBR);
            Gizmos.DrawLine(camBR, camTR);
            Gizmos.DrawLine(camTR, camTL);
            Gizmos.DrawLine(camTL, camBL);
        }

        // Draw locked room if active
        if (isLockedToRoom)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lockedRoomCenter, 1f);
        }
    }
}