using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    [Header("Boss Room Settings")]
    [SerializeField] Transform roomCenter;
    [SerializeField] bool lockOnEnter = true;
    [SerializeField] bool unlockOnBossDefeat = true;

    [Header("Optional Boss Reference")]
    [SerializeField] GameObject bossObject;

    private AdvancedCameraController cameraController;
    private bool isLocked = false;

    void Awake()
    {
        cameraController = Camera.main.GetComponent<AdvancedCameraController>();

        // If no room center specified, use this object's position
        if (!roomCenter)
        {
            roomCenter = transform;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && lockOnEnter && !isLocked)
        {
            LockCamera();
        }
    }

    void Update()
    {
        // Check if boss is defeated
        if (unlockOnBossDefeat && isLocked && bossObject == null)
        {
            UnlockCamera();
        }
    }

    public void LockCamera()
    {
        if (cameraController)
        {
            cameraController.LockToRoom(roomCenter.position);
            isLocked = true;
        }
    }

    public void UnlockCamera()
    {
        if (cameraController)
        {
            cameraController.UnlockCamera();
            isLocked = false;
        }
    }

    // Call this when boss is defeated
    public void OnBossDefeated()
    {
        UnlockCamera();
    }

    void OnDrawGizmosSelected()
    {
        if (!roomCenter) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(roomCenter.position, 2f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, roomCenter.position);
    }
}