using UnityEngine;

/// <summary>
/// Controls a ParticleSystem based on a PrecipitationPreset.
/// Handles world-fixed spawning, wind integration, and runtime preset switching.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class PrecipitationController : MonoBehaviour
{
    [Header("Preset")]
    [SerializeField] private PrecipitationPreset preset;
    [SerializeField] private bool applyPresetOnStart = true;

    [Header("Material Override")]
    [Tooltip("Optional: Assign a particle material directly if auto-detection fails")]
    [SerializeField] private Material particleMaterial;

    [Header("Zone Bounds (World-Fixed)")]
    [Tooltip("Size of the precipitation zone in world units")]
    [SerializeField] private Vector2 zoneSize = new Vector2(50f, 30f);

    [Tooltip("Use transform position as zone center")]
    [SerializeField] private bool useTransformAsCenter = true;

    [Tooltip("Custom zone center (if not using transform)")]
    [SerializeField] private Vector2 customZoneCenter;

    [Header("State")]
    [SerializeField] private bool isActive = true;

    [Header("Transitions")]
    [SerializeField] private float transitionDuration = 1f;

    [Header("Debug")]
    [SerializeField] private bool showZoneBounds = true;
    [SerializeField] private Color boundsColor = new Color(0.3f, 0.7f, 1f, 0.5f);

    // Components
    private ParticleSystem ps;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.ShapeModule shapeModule;
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;
    private ParticleSystem.ColorOverLifetimeModule colorModule;
    private ParticleSystem.RotationOverLifetimeModule rotationModule;
    private ParticleSystem.CollisionModule collisionModule;
    private ParticleSystemRenderer psRenderer;

    // Runtime state
    private PrecipitationPreset currentPreset;
    private ParticleSystem.Particle[] particles;
    private float targetEmissionRate;
    private float currentEmissionRate;
    private bool isTransitioning;
    private Texture2D defaultTexture;

    // Public properties
    public PrecipitationPreset CurrentPreset => currentPreset;
    public bool IsActive => isActive;
    public Vector2 ZoneCenter => useTransformAsCenter ? (Vector2)transform.position : customZoneCenter;
    public float TransitionDuration
    {
        get => transitionDuration;
        set => transitionDuration = Mathf.Max(0.1f, value);
    }

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        psRenderer = ps.GetComponent<ParticleSystemRenderer>();

        // Cache modules
        mainModule = ps.main;
        emissionModule = ps.emission;
        shapeModule = ps.shape;
        velocityModule = ps.velocityOverLifetime;
        colorModule = ps.colorOverLifetime;
        rotationModule = ps.rotationOverLifetime;
        collisionModule = ps.collision;

        // Create default texture for particles without sprites
        CreateDefaultTexture();
    }

    private void Start()
    {
        if (applyPresetOnStart && preset != null)
        {
            ApplyPreset(preset);
        }
    }

    private void Update()
    {
        // Handle emission transitions
        if (isTransitioning && currentPreset != null)
        {
            float transitionSpeed = currentPreset.emissionRate / transitionDuration;
            currentEmissionRate = Mathf.MoveTowards(
                currentEmissionRate,
                targetEmissionRate,
                transitionSpeed * Time.deltaTime
            );

            emissionModule.rateOverTime = currentEmissionRate;

            if (Mathf.Approximately(currentEmissionRate, targetEmissionRate))
            {
                isTransitioning = false;
            }
        }
    }

    private void LateUpdate()
    {
        if (!isActive || currentPreset == null) return;

        ApplyWindToParticles();
    }

    /// <summary>
    /// Apply a new preset to this controller.
    /// </summary>
    public void ApplyPreset(PrecipitationPreset newPreset, bool immediate = true)
    {
        if (newPreset == null)
        {
            Debug.LogWarning($"[PrecipitationController] Cannot apply null preset on {gameObject.name}");
            return;
        }

        currentPreset = newPreset;
        preset = newPreset;

        ConfigureMainModule();
        ConfigureEmission(immediate);
        ConfigureShape();
        ConfigureVelocity();
        ConfigureColorOverLifetime();
        ConfigureRotation();
        ConfigureCollision();
        ConfigureRenderer();

        if (isActive && !ps.isPlaying)
        {
            ps.Play();
        }
    }

    private void ConfigureMainModule()
    {
        mainModule.startSpeed = 0f; // We control velocity manually
        mainModule.startSize = new ParticleSystem.MinMaxCurve(
            currentPreset.sizeRange.x,
            currentPreset.sizeRange.y
        );
        mainModule.startLifetime = new ParticleSystem.MinMaxCurve(
            currentPreset.lifetime - currentPreset.lifetimeVariation,
            currentPreset.lifetime + currentPreset.lifetimeVariation
        );
        mainModule.startColor = currentPreset.tint;
        mainModule.maxParticles = currentPreset.maxParticles;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        mainModule.gravityModifier = 0f; // We handle gravity via velocity

        if (currentPreset.enableRotation)
        {
            mainModule.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        }
    }

    private void ConfigureEmission(bool immediate)
    {
        targetEmissionRate = currentPreset.emissionRate;

        if (immediate)
        {
            currentEmissionRate = targetEmissionRate;
            emissionModule.rateOverTime = currentEmissionRate;
            isTransitioning = false;
        }
        else
        {
            isTransitioning = true;
        }
    }

    private void ConfigureShape()
    {
        shapeModule.enabled = true;
        shapeModule.shapeType = ParticleSystemShapeType.Box;

        // Spawn area positioned above zone
        Vector3 spawnScale = new Vector3(
            currentPreset.spawnAreaSize.x,
            currentPreset.spawnAreaSize.y,
            0.1f
        );
        shapeModule.scale = spawnScale;

        // Position shape at top of zone
        Vector3 shapePosition = new Vector3(
            currentPreset.spawnOffset.x,
            zoneSize.y / 2f + currentPreset.spawnOffset.y,
            currentPreset.zOffset
        );
        shapeModule.position = shapePosition;
    }

    private void ConfigureVelocity()
    {
        velocityModule.enabled = true;
        velocityModule.space = ParticleSystemSimulationSpace.World;

        // Base downward velocity
        float minFall = -(currentPreset.fallSpeed + currentPreset.fallSpeedVariation);
        float maxFall = -(currentPreset.fallSpeed - currentPreset.fallSpeedVariation);

        // All axes must use the same curve mode (TwoConstants)
        velocityModule.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocityModule.y = new ParticleSystem.MinMaxCurve(minFall, maxFall);
        velocityModule.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    private void ConfigureColorOverLifetime()
    {
        colorModule.enabled = currentPreset.fadeIn || currentPreset.fadeOut;

        if (colorModule.enabled)
        {
            colorModule.color = currentPreset.GetAlphaGradient();
        }
    }

    private void ConfigureRotation()
    {
        rotationModule.enabled = currentPreset.enableRotation;

        if (rotationModule.enabled)
        {
            rotationModule.z = new ParticleSystem.MinMaxCurve(
                currentPreset.rotationSpeedRange.x * Mathf.Deg2Rad,
                currentPreset.rotationSpeedRange.y * Mathf.Deg2Rad
            );
        }
    }

    private void ConfigureCollision()
    {
        collisionModule.enabled = currentPreset.enableCollision;

        if (collisionModule.enabled)
        {
            collisionModule.type = ParticleSystemCollisionType.World;
            collisionModule.mode = ParticleSystemCollisionMode.Collision2D;
            collisionModule.bounce = currentPreset.collisionBounce;
            collisionModule.lifetimeLoss = currentPreset.collisionLifetimeLoss;
            collisionModule.collidesWith = currentPreset.collisionLayers;
        }
    }

    private void ConfigureRenderer()
    {
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;

        Material mat;

        // Use override material if provided
        if (particleMaterial != null)
        {
            mat = new Material(particleMaterial);
        }
        else
        {
            // Use the existing particle material if valid
            mat = psRenderer.sharedMaterial;

            if (mat == null || !mat.shader.name.Contains("Particle"))
            {
                // Create a simple sprite material as fallback
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    Debug.LogError("[PrecipitationController] Could not find Sprites/Default shader!");
                    return;
                }
                mat = new Material(shader);
            }
            else
            {
                // Clone existing material
                mat = new Material(mat);
            }
        }

        // Apply tint color
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", currentPreset.tint);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", currentPreset.tint);
        if (mat.HasProperty("_TintColor"))
            mat.SetColor("_TintColor", currentPreset.tint);

        // Apply sprite or default texture
        if (currentPreset.particleSprite != null)
        {
            mat.mainTexture = currentPreset.particleSprite.texture;
        }
        else
        {
            mat.mainTexture = defaultTexture;
        }

        psRenderer.material = mat;

        // Sorting
        psRenderer.sortingLayerName = currentPreset.sortingLayerName;
        psRenderer.sortingOrder = currentPreset.orderInLayer;
    }

    private void ApplyWindToParticles()
    {
        int count = ps.particleCount;
        if (count == 0) return;

        if (particles == null || particles.Length < mainModule.maxParticles)
        {
            particles = new ParticleSystem.Particle[mainModule.maxParticles];
        }

        count = ps.GetParticles(particles);

        // Get wind values
        Vector2 windVector = Vector2.zero;
        float turbulenceScale = 0f;
        float turbulenceStrength = 0f;

        if (WindManager.Instance != null)
        {
            windVector = WindManager.Instance.CurrentWindVector * currentPreset.windInfluenceMultiplier;
            turbulenceScale = WindManager.Instance.TurbulenceScale;
            turbulenceStrength = WindManager.Instance.TurbulenceStrength * currentPreset.turbulenceInfluenceMultiplier;
        }

        float time = Time.time;
        float driftAmount = currentPreset.driftAmount;
        float driftFrequency = currentPreset.driftFrequency;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = particles[i].position;
            Vector3 velocity = particles[i].velocity;

            // Sinusoidal drift (unique per particle based on seed)
            float seed = particles[i].randomSeed * 0.0001f;
            float drift = Mathf.Sin((time + seed) * driftFrequency * Mathf.PI * 2f) * driftAmount;

            // Turbulence from wind manager
            Vector2 turbulence = Vector2.zero;
            if (turbulenceStrength > 0f)
            {
                float noiseX = Mathf.PerlinNoise(pos.x * turbulenceScale + time * 0.3f, pos.y * turbulenceScale) * 2f - 1f;
                float noiseY = Mathf.PerlinNoise(pos.x * turbulenceScale, pos.y * turbulenceScale + time * 0.3f) * 2f - 1f;
                turbulence = new Vector2(noiseX, noiseY) * turbulenceStrength;
            }

            // Apply forces
            float targetX = drift + windVector.x + turbulence.x;
            velocity.x = Mathf.Lerp(velocity.x, targetX, Time.deltaTime * 5f);

            // Wind can also affect vertical slightly
            velocity.y += (windVector.y + turbulence.y) * Time.deltaTime;

            particles[i].velocity = velocity;
        }

        ps.SetParticles(particles, count);
    }

    private void CreateDefaultTexture()
    {
        int size = 32;
        defaultTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(distance / radius);
                alpha = Mathf.Pow(alpha, 1.5f); // Soft falloff

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        defaultTexture.SetPixels(pixels);
        defaultTexture.Apply();
        defaultTexture.filterMode = FilterMode.Bilinear;
    }

    /// <summary>
    /// Enable precipitation with optional transition.
    /// </summary>
    public void Enable(bool immediate = false)
    {
        isActive = true;

        if (immediate)
        {
            emissionModule.rateOverTime = currentPreset != null ? currentPreset.emissionRate : 0f;
            currentEmissionRate = emissionModule.rateOverTime.constant;
        }
        else
        {
            targetEmissionRate = currentPreset != null ? currentPreset.emissionRate : 0f;
            isTransitioning = true;
        }

        if (!ps.isPlaying)
        {
            ps.Play();
        }
    }

    /// <summary>
    /// Disable precipitation with optional transition.
    /// </summary>
    public void Disable(bool immediate = false)
    {
        if (immediate)
        {
            isActive = false;
            ps.Stop();
            ps.Clear();
            currentEmissionRate = 0f;
        }
        else
        {
            targetEmissionRate = 0f;
            isTransitioning = true;

            // Actual disable happens when emission reaches 0
            // Particles already spawned will finish their lifetime
        }
    }

    /// <summary>
    /// Transition to a new preset smoothly.
    /// </summary>
    public void TransitionToPreset(PrecipitationPreset newPreset)
    {
        if (newPreset == null) return;

        ApplyPreset(newPreset, immediate: false);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showZoneBounds) return;

        Vector2 center = ZoneCenter;

        // Draw zone bounds
        Gizmos.color = boundsColor;
        Gizmos.DrawWireCube(
            new Vector3(center.x, center.y, 0f),
            new Vector3(zoneSize.x, zoneSize.y, 0.1f)
        );

        // Draw spawn area
        if (currentPreset != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Vector3 spawnCenter = new Vector3(
                center.x + currentPreset.spawnOffset.x,
                center.y + zoneSize.y / 2f + currentPreset.spawnOffset.y,
                currentPreset.zOffset
            );
            Gizmos.DrawWireCube(
                spawnCenter,
                new Vector3(currentPreset.spawnAreaSize.x, currentPreset.spawnAreaSize.y, 0.1f)
            );
        }
    }

    private void OnGUI()
    {
        if (!showZoneBounds || ps == null) return;

        GUILayout.BeginArea(new Rect(10, 120, 250, 120));
        GUILayout.Box("Precipitation Debug");
        GUILayout.Label($"Active: {isActive}");
        GUILayout.Label($"Playing: {ps.isPlaying}");
        GUILayout.Label($"Particle Count: {ps.particleCount}");
        GUILayout.Label($"Emission Rate: {currentEmissionRate:F1}");
        GUILayout.Label($"Preset: {(currentPreset != null ? currentPreset.displayName : "None")}");
        GUILayout.EndArea();
    }
}
