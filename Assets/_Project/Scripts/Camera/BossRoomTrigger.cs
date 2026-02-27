using System;
using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    [Header("Boss Room Settings")]
    [SerializeField] private Transform roomCenter;
    [SerializeField] private bool lockOnEnter = true;
    [SerializeField] private bool unlockOnBossDefeat = true;

    [Header("Optional Boss Reference")]
    [SerializeField] private GameObject bossObject;

    private AdvancedCameraController cameraController;
    private bool isLocked = false;
    private bool bossWasAssigned;
    private BossController bossController;

    void Awake()
    {
        if (!roomCenter)
            roomCenter = transform;
    }

    void Start()
    {
        if (Camera.main != null)
            cameraController = Camera.main.GetComponent<AdvancedCameraController>();
        else
            Debug.LogWarning($"[BossRoomTrigger] {gameObject.name}: No MainCamera found");

        bossWasAssigned = bossObject != null;

        if (bossObject != null)
        {
            bossController = bossObject.GetComponent<BossController>();
            if (bossController != null)
                bossController.OnBossDefeated += OnBossDefeated;
        }
    }

    void OnDestroy()
    {
        if (bossController != null)
            bossController.OnBossDefeated -= OnBossDefeated;
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
        // Fallback polling for bosses without BossController component
        if (bossController != null) return;

        if (unlockOnBossDefeat && isLocked && bossWasAssigned
            && (bossObject == null || !bossObject.activeInHierarchy))
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

    /// <summary>
    /// Called via BossController event or manually as a fallback API.
    /// </summary>
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