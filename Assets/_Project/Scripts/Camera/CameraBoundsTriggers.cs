using UnityEngine;

public class CameraBoundsTrigger : MonoBehaviour
{
    [Header("New Camera Bounds")]
    [SerializeField] private float minX = -50f;
    [SerializeField] private float maxX = 50f;
    [SerializeField] private float minY = -50f;
    [SerializeField] private float maxY = 50f;

    [Header("Settings")]
    [SerializeField] private bool requiresAbility = false;
    [SerializeField] private PowerUpType requiredAbility;

    private AdvancedCameraController cameraController;
    private Collider2D triggerCollider;
    private bool hasBeenActivated = false;

    void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        if (Camera.main != null)
            cameraController = Camera.main.GetComponent<AdvancedCameraController>();
        else
            Debug.LogWarning($"[CameraBoundsTrigger] {gameObject.name}: No MainCamera found");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasBeenActivated)
        {
            // Check if player has required ability
            if (requiresAbility)
            {
                PowerUpManager powerUpManager = other.GetComponent<PowerUpManager>();
                if (powerUpManager == null || !powerUpManager.HasPowerUp(requiredAbility))
                {
                    return; // Player doesn't have required ability
                }
            }

            // Update camera bounds
            if (cameraController)
            {
                cameraController.SetBounds(minX, maxX, minY, maxY);
                hasBeenActivated = true;
            }
        }
    }

    // Visualize the new bounds in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 bottomLeft = new Vector3(minX, minY, 0);
        Vector3 bottomRight = new Vector3(maxX, minY, 0);
        Vector3 topLeft = new Vector3(minX, maxY, 0);
        Vector3 topRight = new Vector3(maxX, maxY, 0);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        // Draw trigger area
        Gizmos.color = Color.green;
        Collider2D col = triggerCollider != null ? triggerCollider : GetComponent<Collider2D>();
        if (col != null)
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
    }
}