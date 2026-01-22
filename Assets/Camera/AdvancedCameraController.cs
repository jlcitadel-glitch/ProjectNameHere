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

    [Header("Camera Bounds")]
    [SerializeField] bool useBounds = true;
    [SerializeField] float minY = -100f; // Don't go below this Y position
    [SerializeField] float maxY = 100f;
    [SerializeField] float minX = -100f;
    [SerializeField] float maxX = 100f;

    [Header("Room Lock (Boss Fights)")]
    [SerializeField] bool isLockedToRoom = false;
    [SerializeField] Vector3 lockedRoomCenter;
    [SerializeField] float roomLockSpeed = 3f;

    private Vector3 currentVelocity;
    private float currentLookAhead;
    private float currentFallOffset;
    private Rigidbody2D targetRb;
    private Camera cam;

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
        // Clamp camera position to bounds
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
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

        Gizmos.color = Color.yellow;

        // Draw boundary box
        Vector3 bottomLeft = new Vector3(minX, minY, 0);
        Vector3 bottomRight = new Vector3(maxX, minY, 0);
        Vector3 topLeft = new Vector3(minX, maxY, 0);
        Vector3 topRight = new Vector3(maxX, maxY, 0);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        // Draw locked room if active
        if (isLockedToRoom)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lockedRoomCenter, 1f);
        }
    }
}