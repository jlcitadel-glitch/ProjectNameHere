using UnityEngine;
using System;

/// <summary>
/// Type of precipitation, affects rendering and default behaviors.
/// </summary>
public enum PrecipitationType
{
    Rain,
    Snow,
    Ash,
    Spores,
    Pollen,
    Dust,
    Embers,
    Custom
}

/// <summary>
/// ScriptableObject containing all configuration for a precipitation type.
/// Create presets via Assets > Create > Game > Precipitation Preset.
/// </summary>
[CreateAssetMenu(fileName = "NewPrecipitationPreset", menuName = "Game/Precipitation Preset")]
public class PrecipitationPreset : ScriptableObject
{
    // ==================== ESSENTIAL SETTINGS ====================
    // These are always visible in the inspector

    [Header("Essential")]
    [Tooltip("Type of precipitation (affects rendering mode)")]
    public PrecipitationType type = PrecipitationType.Rain;

    [Tooltip("Display name for debugging and UI")]
    public string displayName = "New Precipitation";

    [Tooltip("Overall intensity (0-1). Controls emission rate and particle count.")]
    [Range(0f, 1f)]
    public float intensity = 0.5f;

    [Tooltip("Sprite/texture for particles. If null, uses default circle.")]
    public Sprite particleSprite;

    [Tooltip("Color tint applied to particles")]
    public Color tint = Color.white;

    // ==================== ADVANCED SETTINGS ====================
    // These are collapsed by default in the inspector

    [Header("Advanced - Size")]
    [Tooltip("Minimum and maximum particle size")]
    public Vector2 sizeRange = new Vector2(0.05f, 0.15f);

    [Tooltip("How much size varies between min and max (0 = all same size)")]
    [Range(0f, 1f)]
    public float sizeVariation = 0.3f;

    [Header("Advanced - Emission")]
    [Tooltip("Base particles emitted per second (scaled by intensity)")]
    public float emissionRate = 50f;

    [Tooltip("How long each particle lives (seconds)")]
    public float lifetime = 3f;

    [Tooltip("Variation in lifetime")]
    public float lifetimeVariation = 0.5f;

    [Tooltip("Base maximum particles alive at once (scaled by intensity)")]
    public int maxParticles = 500;

    [Header("Spawn Area (Legacy)")]
    [Obsolete("Use PrecipitationController.bounds instead")]
    [Tooltip("Deprecated: Size of the spawn area - use controller bounds")]
    public Vector2 spawnAreaSize = new Vector2(25f, 2f);

    [Obsolete("Use PrecipitationController.bounds instead")]
    [Tooltip("Deprecated: Offset from zone center - use controller bounds")]
    public Vector2 spawnOffset = new Vector2(0f, 2f);

    [Header("Advanced - Movement")]
    [Tooltip("Base downward fall speed")]
    public float fallSpeed = 5f;

    [Tooltip("Random variation in fall speed")]
    public float fallSpeedVariation = 1f;

    [Tooltip("Horizontal drift oscillation amplitude")]
    public float driftAmount = 0.5f;

    [Tooltip("How fast the drift oscillates")]
    public float driftFrequency = 1f;

    [Tooltip("Multiplier for global wind effect (0 = ignores wind, 2 = very affected)")]
    [Range(0f, 3f)]
    public float windInfluenceMultiplier = 1f;

    [Tooltip("Multiplier for wind turbulence effect")]
    [Range(0f, 2f)]
    public float turbulenceInfluenceMultiplier = 1f;

    [Header("Advanced - Rotation")]
    [Tooltip("Enable particle rotation over lifetime")]
    public bool enableRotation = false;

    [Tooltip("Rotation speed range (degrees per second)")]
    public Vector2 rotationSpeedRange = new Vector2(-45f, 45f);

    [Header("Advanced - Fade")]
    [Tooltip("Fade in at start of lifetime")]
    public bool fadeIn = false;

    [Tooltip("Percentage of lifetime where fade-in completes")]
    [Range(0f, 0.5f)]
    public float fadeInPoint = 0.1f;

    [Tooltip("Fade out at end of lifetime")]
    public bool fadeOut = true;

    [Tooltip("Percentage of lifetime where fade-out begins")]
    [Range(0.5f, 1f)]
    public float fadeOutPoint = 0.8f;

    [Header("Advanced - Rendering")]
    [Tooltip("Sorting layer for particles")]
    public string sortingLayerName = "Default";

    [Tooltip("Order within sorting layer")]
    public int orderInLayer = 10;

    [Tooltip("Z position offset (negative = in front of player at Z=0)")]
    public float zOffset = -1f;

    // ==================== EXPERT SETTINGS ====================
    // These are hidden by default, only shown in debug mode

    [Header("Expert - Collision")]
    [Tooltip("Enable collision with world geometry")]
    public bool enableCollision = false;

    [Tooltip("Layers particles collide with")]
    public LayerMask collisionLayers;

    [Tooltip("Bounce factor on collision (0 = stop, 1 = full bounce)")]
    [Range(0f, 1f)]
    public float collisionBounce = 0f;

    [Tooltip("Lifetime multiplier after collision (0 = die immediately)")]
    [Range(0f, 1f)]
    public float collisionLifetimeLoss = 0.8f;

    // ==================== INTENSITY CURVES ====================
    // Define how intensity maps to emission and particles

    /// <summary>
    /// Minimum emission rate at intensity 0.
    /// </summary>
    private const float MIN_EMISSION_MULTIPLIER = 0.1f;

    /// <summary>
    /// Maximum emission rate at intensity 1.
    /// </summary>
    private const float MAX_EMISSION_MULTIPLIER = 2.0f;

    /// <summary>
    /// Minimum particle count multiplier at intensity 0.
    /// </summary>
    private const float MIN_PARTICLES_MULTIPLIER = 0.2f;

    /// <summary>
    /// Maximum particle count multiplier at intensity 1.
    /// </summary>
    private const float MAX_PARTICLES_MULTIPLIER = 2.0f;

    /// <summary>
    /// Gets the effective emission rate based on intensity.
    /// </summary>
    public float GetEffectiveEmissionRate()
    {
        float multiplier = Mathf.Lerp(MIN_EMISSION_MULTIPLIER, MAX_EMISSION_MULTIPLIER, intensity);
        return emissionRate * multiplier;
    }

    /// <summary>
    /// Gets the effective max particles based on intensity.
    /// </summary>
    public int GetEffectiveMaxParticles()
    {
        float multiplier = Mathf.Lerp(MIN_PARTICLES_MULTIPLIER, MAX_PARTICLES_MULTIPLIER, intensity);
        return Mathf.RoundToInt(maxParticles * multiplier);
    }

    /// <summary>
    /// Creates the alpha gradient for particle color over lifetime.
    /// </summary>
    public Gradient GetAlphaGradient()
    {
        Gradient gradient = new Gradient();

        GradientColorKey[] colorKeys = new GradientColorKey[]
        {
            new GradientColorKey(Color.white, 0f),
            new GradientColorKey(Color.white, 1f)
        };

        GradientAlphaKey[] alphaKeys;

        if (fadeIn && fadeOut)
        {
            alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, fadeInPoint),
                new GradientAlphaKey(1f, fadeOutPoint),
                new GradientAlphaKey(0f, 1f)
            };
        }
        else if (fadeIn)
        {
            alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, fadeInPoint),
                new GradientAlphaKey(1f, 1f)
            };
        }
        else if (fadeOut)
        {
            alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, fadeOutPoint),
                new GradientAlphaKey(0f, 1f)
            };
        }
        else
        {
            alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
        }

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }
}
