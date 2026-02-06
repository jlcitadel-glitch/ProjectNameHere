using UnityEngine;

/// <summary>
/// Defines how the zone interacts with precipitation controllers.
/// </summary>
public enum ZoneControlMode
{
    /// <summary>Use a global camera-following controller (shared across zones).</summary>
    UseGlobalController,
    /// <summary>Use a local controller specific to this zone.</summary>
    UseLocalController
}

/// <summary>
/// Trigger-based zone that activates/deactivates precipitation when the player enters.
/// Simplified design: references existing controllers rather than creating them dynamically.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PrecipitationZone : MonoBehaviour
{
    [Header("Control Mode")]
    [Tooltip("How this zone controls precipitation")]
    [SerializeField] private ZoneControlMode controlMode = ZoneControlMode.UseLocalController;

    [Header("Controller Reference")]
    [Tooltip("The precipitation controller to activate/deactivate")]
    [SerializeField] private PrecipitationController precipitationController;

    [Tooltip("Preset to apply when entering (optional, uses controller's preset if null)")]
    [SerializeField] private PrecipitationPreset presetOverride;

    [Header("Activation")]
    [Tooltip("Tag to detect (usually Player)")]
    [SerializeField] private string triggerTag = "Player";

    [Tooltip("Active when player is inside the zone")]
    [SerializeField] private bool activeWhileInside = true;

    [Tooltip("Check if player starts inside zone on scene load")]
    [SerializeField] private bool checkOnStart = true;

    [Header("Transitions")]
    [Tooltip("Smooth transition when entering/exiting")]
    [SerializeField] private bool useTransitions = true;

    [Tooltip("Transition duration in seconds")]
    [SerializeField] private float transitionDuration = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // Global controller singleton reference
    private static PrecipitationController globalController;

    private Collider2D zoneCollider;
    private bool isPlayerInside;
    private bool isInitialized;

    /// <summary>
    /// Gets or sets the global precipitation controller (shared by zones using UseGlobalController mode).
    /// </summary>
    public static PrecipitationController GlobalController
    {
        get => globalController;
        set => globalController = value;
    }

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();

        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }
    }

    private void Start()
    {
        Initialize();

        if (checkOnStart)
        {
            CheckInitialState();
        }
    }

    private void Initialize()
    {
        if (isInitialized) return;

        // Resolve which controller to use based on mode
        PrecipitationController controller = GetActiveController();

        if (controller != null)
        {
            // Apply zone's transition duration to controller
            controller.TransitionDuration = transitionDuration;

            // Start disabled, will enable when player enters
            controller.Disable(immediate: true);
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning($"[PrecipitationZone] No controller found for {gameObject.name}. " +
                "Assign a controller reference or set up a global controller.");
        }

        isInitialized = true;
    }

    /// <summary>
    /// Gets the active controller based on current control mode.
    /// </summary>
    private PrecipitationController GetActiveController()
    {
        switch (controlMode)
        {
            case ZoneControlMode.UseGlobalController:
                return globalController ?? precipitationController;

            case ZoneControlMode.UseLocalController:
            default:
                return precipitationController;
        }
    }

    /// <summary>
    /// Registers a controller as the global controller.
    /// Call this from a camera-following controller on Awake.
    /// </summary>
    public static void RegisterGlobalController(PrecipitationController controller)
    {
        globalController = controller;
    }

    private void CheckInitialState()
    {
        // Check if player is already inside zone at start
        GameObject player = GameObject.FindGameObjectWithTag(triggerTag);

        if (player != null && zoneCollider != null)
        {
            Collider2D playerCollider = player.GetComponent<Collider2D>();

            if (playerCollider != null && zoneCollider.bounds.Intersects(playerCollider.bounds))
            {
                isPlayerInside = true;
                OnPlayerEntered();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(triggerTag)) return;
        if (isPlayerInside) return;

        isPlayerInside = true;
        OnPlayerEntered();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(triggerTag)) return;
        if (!isPlayerInside) return;

        isPlayerInside = false;
        OnPlayerExited();
    }

    private void OnPlayerEntered()
    {
        PrecipitationController controller = GetActiveController();
        if (controller == null) return;

        if (showDebugLogs)
        {
            Debug.Log($"[PrecipitationZone] Player entered {gameObject.name}");
        }

        if (activeWhileInside)
        {
            // Apply preset override if we have one
            if (presetOverride != null)
            {
                controller.ApplyPreset(presetOverride, immediate: !useTransitions);
            }

            controller.Enable(immediate: !useTransitions);
        }
        else
        {
            controller.Disable(immediate: !useTransitions);
        }
    }

    private void OnPlayerExited()
    {
        PrecipitationController controller = GetActiveController();
        if (controller == null) return;

        if (showDebugLogs)
        {
            Debug.Log($"[PrecipitationZone] Player exited {gameObject.name}");
        }

        if (activeWhileInside)
        {
            controller.Disable(immediate: !useTransitions);
        }
        else
        {
            controller.Enable(immediate: !useTransitions);
        }
    }

    /// <summary>
    /// Manually activate precipitation in this zone.
    /// </summary>
    public void Activate(bool immediate = false)
    {
        PrecipitationController controller = GetActiveController();
        if (controller == null) return;

        controller.Enable(immediate);
    }

    /// <summary>
    /// Manually deactivate precipitation in this zone.
    /// </summary>
    public void Deactivate(bool immediate = false)
    {
        PrecipitationController controller = GetActiveController();
        if (controller == null) return;

        controller.Disable(immediate);
    }

    /// <summary>
    /// Switch to a different preset at runtime.
    /// </summary>
    public void SwitchPreset(PrecipitationPreset newPreset)
    {
        PrecipitationController controller = GetActiveController();
        if (controller == null || newPreset == null) return;

        presetOverride = newPreset;
        controller.TransitionToPreset(newPreset);
    }

    /// <summary>
    /// Sets the control mode at runtime.
    /// </summary>
    public void SetControlMode(ZoneControlMode mode)
    {
        controlMode = mode;
    }

    /// <summary>
    /// Assigns a local controller reference.
    /// </summary>
    public void SetLocalController(PrecipitationController controller)
    {
        precipitationController = controller;
    }

    private void OnDrawGizmos()
    {
        // Draw zone bounds
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = isPlayerInside
            ? new Color(0.3f, 1f, 0.3f, 0.3f)
            : new Color(0.3f, 0.7f, 1f, 0.2f);

        if (col is BoxCollider2D box)
        {
            Vector3 center = transform.position + (Vector3)box.offset;
            Vector3 size = box.size;
            Gizmos.DrawCube(center, size);
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 1f);
            Gizmos.DrawWireCube(center, size);
        }
        else if (col is CircleCollider2D circle)
        {
            Vector3 center = transform.position + (Vector3)circle.offset;
            Gizmos.DrawSphere(center, circle.radius);
        }
    }
}
