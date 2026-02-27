using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax Settings")]
    [SerializeField] private float parallaxEffectX = 0.5f;
    [SerializeField] private float parallaxEffectY = 0.5f;

    [Header("Auto-Calculate from Z Depth")]
    [SerializeField] private bool autoCalculateFromZ = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private Transform cameraTransform;
    private Vector3 lastCameraPosition;

    void Start()
    {
        cameraTransform = Camera.main != null ? Camera.main.transform : FindFirstObjectByType<Camera>()?.transform;

        if (cameraTransform == null)
        {
            Debug.LogError($"ParallaxLayer on {gameObject.name}: No camera found! Make sure your camera has the 'MainCamera' tag.");
            enabled = false;
            return;
        }

        lastCameraPosition = cameraTransform.position;

        if (autoCalculateFromZ)
        {
            CalculateParallaxFromDepth();
        }

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: Parallax X={parallaxEffectX}, Y={parallaxEffectY}, Z Depth={transform.position.z}");
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;

        Vector3 parallaxMovement = new Vector3(
            deltaMovement.x * parallaxEffectX,
            deltaMovement.y * parallaxEffectY,
            0
        );

        transform.position += parallaxMovement;

        if (showDebugInfo && parallaxMovement.magnitude > 0.01f)
        {
            Debug.Log($"{gameObject.name} moved: {parallaxMovement}");
        }

        lastCameraPosition = cameraTransform.position;
    }

    void CalculateParallaxFromDepth()
    {
        float depth = transform.position.z;

        if (depth < 0)
        {
            // Foreground: moves very slightly faster than camera
            parallaxEffectX = 1.0f + (Mathf.Abs(depth) * 0.02f);
            parallaxEffectY = 1.0f + (Mathf.Abs(depth) * 0.02f);
        }
        else if (depth > 0)
        {
            // Background: far layers move almost with camera (barely scroll in view)
            // Asymptotic curve — never saturates at 0, spread stays visible at any depth
            parallaxEffectX = 1f / (1f + depth * 0.05f);
            parallaxEffectY = 1f / (1f + depth * 0.05f);
        }
        else
        {
            // Ground layer: moves exactly with camera
            parallaxEffectX = 1.0f;
            parallaxEffectY = 1.0f;
        }
    }
}