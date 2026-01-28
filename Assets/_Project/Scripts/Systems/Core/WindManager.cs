using UnityEngine;

/// <summary>
/// Global wind system that provides wind values for other systems to read.
/// Precipitation, foliage, particles, etc. can all reference this for consistent wind behavior.
/// </summary>
public class WindManager : MonoBehaviour
{
    public static WindManager Instance { get; private set; }

    [Header("Base Wind")]
    [SerializeField] private Vector2 windDirection = Vector2.right;
    [SerializeField] private float baseStrength = 1f;

    [Header("Gusts")]
    [SerializeField] private bool enableGusts = true;
    [SerializeField] private float gustStrength = 2f;
    [SerializeField] private float gustFrequency = 0.3f;
    [SerializeField] private float gustDuration = 1.5f;

    [Header("Turbulence")]
    [SerializeField] private bool enableTurbulence = true;
    [SerializeField] private float turbulenceStrength = 0.5f;
    [SerializeField] private float turbulenceScale = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // Public properties for other systems to read
    public Vector2 WindDirection => windDirection.normalized;
    public float BaseStrength => baseStrength;
    public float CurrentStrength => baseStrength + currentGustValue;
    public Vector2 CurrentWindVector => WindDirection * CurrentStrength;
    public float TurbulenceStrength => enableTurbulence ? turbulenceStrength : 0f;
    public float TurbulenceScale => turbulenceScale;

    private float currentGustValue;
    private float gustTimer;
    private float gustCooldown;
    private bool isGusting;
    private float gustStartTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[WindManager] Duplicate instance on {gameObject.name}, destroying.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (enableGusts)
        {
            UpdateGusts();
        }
    }

    private void UpdateGusts()
    {
        if (isGusting)
        {
            // Currently in a gust - interpolate strength
            float gustProgress = (Time.time - gustStartTime) / gustDuration;

            if (gustProgress >= 1f)
            {
                // Gust ended
                isGusting = false;
                currentGustValue = 0f;
                gustCooldown = Random.Range(1f / gustFrequency * 0.5f, 1f / gustFrequency * 1.5f);
            }
            else
            {
                // Smooth gust curve: ramp up, hold, ramp down
                float curve = Mathf.Sin(gustProgress * Mathf.PI);
                currentGustValue = gustStrength * curve;
            }
        }
        else
        {
            // Waiting for next gust
            gustCooldown -= Time.deltaTime;

            if (gustCooldown <= 0f)
            {
                // Start new gust
                isGusting = true;
                gustStartTime = Time.time;
            }
        }
    }

    /// <summary>
    /// Get turbulence offset for a given world position.
    /// Use this for per-particle or per-object turbulence variation.
    /// </summary>
    public Vector2 GetTurbulenceAt(Vector2 position)
    {
        if (!enableTurbulence) return Vector2.zero;

        float noiseX = Mathf.PerlinNoise(
            position.x * turbulenceScale + Time.time * 0.3f,
            position.y * turbulenceScale
        ) * 2f - 1f;

        float noiseY = Mathf.PerlinNoise(
            position.x * turbulenceScale,
            position.y * turbulenceScale + Time.time * 0.3f
        ) * 2f - 1f;

        return new Vector2(noiseX, noiseY) * turbulenceStrength;
    }

    /// <summary>
    /// Set wind direction at runtime (e.g., for zone-specific wind).
    /// </summary>
    public void SetWindDirection(Vector2 direction)
    {
        windDirection = direction.normalized;
    }

    /// <summary>
    /// Set base wind strength at runtime.
    /// </summary>
    public void SetBaseStrength(float strength)
    {
        baseStrength = Mathf.Max(0f, strength);
    }

    /// <summary>
    /// Trigger an immediate gust.
    /// </summary>
    public void TriggerGust(float strength = -1f)
    {
        isGusting = true;
        gustStartTime = Time.time;
        if (strength > 0f)
        {
            currentGustValue = strength;
        }
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 200, 100));
        GUILayout.Label($"Wind Dir: {windDirection}");
        GUILayout.Label($"Base: {baseStrength:F2}");
        GUILayout.Label($"Gust: {currentGustValue:F2}");
        GUILayout.Label($"Total: {CurrentStrength:F2}");
        GUILayout.EndArea();
    }
}
