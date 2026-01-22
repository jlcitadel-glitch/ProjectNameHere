using UnityEngine;

public class CameraBoundsTrigger : MonoBehaviour
{
    [Header("New Camera Bounds")]
    [SerializeField] float minX = -50f;
    [SerializeField] float maxX = 50f;
    [SerializeField] float minY = -50f;
    [SerializeField] float maxY = 50f;

    [Header("Settings")]
    [SerializeField] bool requiresAbility = false;
    [SerializeField] PowerUpType requiredAbility;

    private AdvancedCameraController cameraController;
    private bool hasBeenActivated = false;

    void Awake()
    {
        cameraController = Camera.main.GetComponent<AdvancedCameraController>();
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
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider2D>().bounds.size);
    }
}