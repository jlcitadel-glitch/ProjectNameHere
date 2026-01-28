using UnityEngine;

/// <summary>
/// Trigger-based zone that activates/deactivates precipitation when the player enters.
/// Can optionally spawn its own PrecipitationController or reference an existing one.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PrecipitationZone : MonoBehaviour
{
    [Header("Precipitation Source")]
    [Tooltip("Reference an existing controller, or leave null to create one")]
    [SerializeField] private PrecipitationController precipitationController;

    [Tooltip("Preset to use if creating a new controller")]
    [SerializeField] private PrecipitationPreset preset;

    [Header("Activation")]
    [Tooltip("Tag to detect (usually Player)")]
    [SerializeField] private string triggerTag = "Player";

    [Tooltip("Active when player is inside the zone")]
    [SerializeField] private bool activeWhileInside = true;

    [Tooltip("Start precipitation immediately when scene loads (if player starts inside)")]
    [SerializeField] private bool checkOnStart = true;

    [Header("Transitions")]
    [Tooltip("Smooth transition when entering/exiting")]
    [SerializeField] private bool useTransitions = true;

    [Tooltip("Transition duration in seconds")]
    [SerializeField] private float transitionDuration = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private Collider2D zoneCollider;
    private bool isPlayerInside;
    private bool isInitialized;

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

        // If no controller assigned, create one
        if (precipitationController == null && preset != null)
        {
            CreatePrecipitationController();
        }

        if (precipitationController != null)
        {
            // Apply zone's transition duration to controller
            precipitationController.TransitionDuration = transitionDuration;

            // Start disabled, will enable when player enters
            precipitationController.Disable(immediate: true);
        }

        isInitialized = true;
    }

    private void CreatePrecipitationController()
    {
        // Create child GameObject with ParticleSystem
        GameObject precipObj = new GameObject($"Precipitation_{preset.displayName}");
        precipObj.transform.SetParent(transform);
        precipObj.transform.localPosition = Vector3.zero;

        // Add ParticleSystem first (required by controller)
        ParticleSystem ps = precipObj.AddComponent<ParticleSystem>();

        // Stop the default particle system behavior
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Add and configure controller
        precipitationController = precipObj.AddComponent<PrecipitationController>();

        // Apply preset immediately so particle system is configured
        precipitationController.ApplyPreset(preset, immediate: true);

        if (showDebugLogs)
        {
            Debug.Log($"[PrecipitationZone] Created controller for {preset.displayName}");
        }
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
        if (precipitationController == null) return;

        if (showDebugLogs)
        {
            Debug.Log($"[PrecipitationZone] Player entered {gameObject.name}");
        }

        if (activeWhileInside)
        {
            // Apply preset if we have one
            if (preset != null)
            {
                precipitationController.ApplyPreset(preset, immediate: !useTransitions);
            }

            precipitationController.Enable(immediate: !useTransitions);
        }
        else
        {
            precipitationController.Disable(immediate: !useTransitions);
        }
    }

    private void OnPlayerExited()
    {
        if (precipitationController == null) return;

        if (showDebugLogs)
        {
            Debug.Log($"[PrecipitationZone] Player exited {gameObject.name}");
        }

        if (activeWhileInside)
        {
            precipitationController.Disable(immediate: !useTransitions);
        }
        else
        {
            precipitationController.Enable(immediate: !useTransitions);
        }
    }

    /// <summary>
    /// Manually activate precipitation in this zone.
    /// </summary>
    public void Activate(bool immediate = false)
    {
        if (precipitationController == null) return;

        precipitationController.Enable(immediate);
    }

    /// <summary>
    /// Manually deactivate precipitation in this zone.
    /// </summary>
    public void Deactivate(bool immediate = false)
    {
        if (precipitationController == null) return;

        precipitationController.Disable(immediate);
    }

    /// <summary>
    /// Switch to a different preset at runtime.
    /// </summary>
    public void SwitchPreset(PrecipitationPreset newPreset)
    {
        if (precipitationController == null || newPreset == null) return;

        preset = newPreset;
        precipitationController.TransitionToPreset(newPreset);
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
