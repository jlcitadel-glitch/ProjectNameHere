using UnityEngine;

/// <summary>
/// Defines how the precipitation spawn bounds are determined.
/// </summary>
public enum BoundsMode
{
    /// <summary>Spawn area follows the main camera viewport.</summary>
    FollowCamera,
    /// <summary>Fixed world-space bounds (use transform position and size).</summary>
    WorldFixed,
    /// <summary>Use an attached Collider2D to define bounds.</summary>
    UseCollider
}

/// <summary>
/// Unified configuration for precipitation spawn bounds.
/// </summary>
[System.Serializable]
public class PrecipitationBounds
{
    [Tooltip("How spawn bounds are determined")]
    public BoundsMode mode = BoundsMode.FollowCamera;

    [Tooltip("Size of fixed bounds (only used in WorldFixed mode)")]
    public Vector2 size = new Vector2(30f, 20f);

    [Tooltip("How far above the visible/bounds area to spawn particles")]
    public float spawnHeightAbove = 3f;

    [Tooltip("Extra horizontal padding for particles drifting with wind")]
    public float horizontalPadding = 5f;

    [Tooltip("Optional collider reference (only used in UseCollider mode)")]
    public Collider2D boundsCollider;
}

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

    [Header("Bounds")]
    [SerializeField] private PrecipitationBounds bounds = new PrecipitationBounds();

    [Header("GPU Motion")]
    [Tooltip("Use GPU noise module for drift (better performance). Disable for CPU fallback.")]
    [SerializeField] private bool useGPUMotion = true;

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
    private ParticleSystem.NoiseModule noiseModule;
    private ParticleSystemRenderer psRenderer;

    // Camera reference for FollowCamera mode
    private Camera mainCamera;
    private Vector3 lastCameraPosition;

    // Runtime state
    private PrecipitationPreset currentPreset;
    private ParticleSystem.Particle[] particles;
    private float targetEmissionRate;
    private float currentEmissionRate;
    private bool isTransitioning;
    private Texture2D defaultTexture;
    private float windUpdateTimer;
    private const float WIND_UPDATE_INTERVAL = 0.1f; // Update wind curves every 100ms

    // Public properties
    public PrecipitationPreset CurrentPreset => currentPreset;
    public bool IsActive => isActive;
    public PrecipitationBounds Bounds => bounds;
    public bool UseGPUMotion
    {
        get => useGPUMotion;
        set
        {
            useGPUMotion = value;
            if (currentPreset != null)
            {
                ConfigureNoiseModule();
            }
        }
    }
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
        noiseModule = ps.noise;

        // Cache camera for FollowCamera mode
        mainCamera = Camera.main;

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
            float emissionRate = currentPreset.GetEffectiveEmissionRate();
            float transitionSpeed = emissionRate / transitionDuration;
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

        // Update shape position for camera-following mode
        if (bounds.mode == BoundsMode.FollowCamera && mainCamera != null)
        {
            UpdateCameraFollowBounds();
        }

        // Periodic wind update for GPU motion
        if (useGPUMotion && WindManager.Instance != null)
        {
            windUpdateTimer += Time.deltaTime;
            if (windUpdateTimer >= WIND_UPDATE_INTERVAL)
            {
                windUpdateTimer = 0f;
                UpdateWindVelocityCurves();
            }
        }
    }

    private void LateUpdate()
    {
        if (!isActive || currentPreset == null) return;

        // Only use CPU particle iteration if GPU motion is disabled
        if (!useGPUMotion)
        {
            ApplyWindToParticles();
        }
    }

    private void UpdateCameraFollowBounds()
    {
        if (mainCamera == null) return;

        Vector3 camPos = mainCamera.transform.position;

        // Only update if camera moved significantly
        if (Vector3.SqrMagnitude(camPos - lastCameraPosition) < 0.01f) return;

        lastCameraPosition = camPos;

        // Calculate viewport bounds
        float camHeight = mainCamera.orthographicSize * 2f;
        float camWidth = camHeight * mainCamera.aspect;

        // Position spawn area above camera viewport
        Vector3 shapePosition = new Vector3(
            camPos.x,
            camPos.y + mainCamera.orthographicSize + bounds.spawnHeightAbove,
            currentPreset != null ? currentPreset.zOffset : -1f
        );

        // Update shape position (world space)
        transform.position = new Vector3(shapePosition.x, shapePosition.y, transform.position.z);

        // Update shape scale to match viewport + padding
        shapeModule.scale = new Vector3(
            camWidth + bounds.horizontalPadding * 2f,
            1f, // Thin spawn line
            0.1f
        );
    }

    private void UpdateWindVelocityCurves()
    {
        if (WindManager.Instance == null || currentPreset == null) return;

        Vector2 windVector = WindManager.Instance.CurrentWindVector * currentPreset.windInfluenceMultiplier;

        // Update velocity module with current wind
        float minFall = -(currentPreset.fallSpeed + currentPreset.fallSpeedVariation);
        float maxFall = -(currentPreset.fallSpeed - currentPreset.fallSpeedVariation);

        velocityModule.x = new ParticleSystem.MinMaxCurve(windVector.x * 0.8f, windVector.x * 1.2f);
        velocityModule.y = new ParticleSystem.MinMaxCurve(minFall + windVector.y, maxFall + windVector.y);
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
        ConfigureNoiseModule();
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
        mainModule.maxParticles = currentPreset.GetEffectiveMaxParticles();
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        mainModule.gravityModifier = 0f; // We handle gravity via velocity

        if (currentPreset.enableRotation)
        {
            mainModule.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        }
    }

    private void ConfigureEmission(bool immediate)
    {
        targetEmissionRate = currentPreset.GetEffectiveEmissionRate();

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

        switch (bounds.mode)
        {
            case BoundsMode.FollowCamera:
                ConfigureShapeForCamera();
                break;

            case BoundsMode.WorldFixed:
                ConfigureShapeForWorldFixed();
                break;

            case BoundsMode.UseCollider:
                ConfigureShapeForCollider();
                break;
        }
    }

    private void ConfigureShapeForCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[PrecipitationController] No main camera found for FollowCamera mode");
                return;
            }
        }

        // Calculate viewport size
        float camHeight = mainCamera.orthographicSize * 2f;
        float camWidth = camHeight * mainCamera.aspect;

        // Spawn area spans viewport width + padding, thin height
        Vector3 spawnScale = new Vector3(
            camWidth + bounds.horizontalPadding * 2f,
            1f,
            0.1f
        );
        shapeModule.scale = spawnScale;

        // Position is handled in UpdateCameraFollowBounds
        shapeModule.position = Vector3.zero;

        // Force initial camera update
        lastCameraPosition = Vector3.positiveInfinity;
        UpdateCameraFollowBounds();
    }

    private void ConfigureShapeForWorldFixed()
    {
        // Spawn area spans the fixed bounds width, thin spawn line at top
        Vector3 spawnScale = new Vector3(
            bounds.size.x + bounds.horizontalPadding * 2f,
            1f,
            0.1f
        );
        shapeModule.scale = spawnScale;

        // Position shape at top of fixed bounds
        Vector3 shapePosition = new Vector3(
            0f,
            bounds.size.y / 2f + bounds.spawnHeightAbove,
            currentPreset.zOffset
        );
        shapeModule.position = shapePosition;
    }

    private void ConfigureShapeForCollider()
    {
        Collider2D col = bounds.boundsCollider;
        if (col == null)
        {
            col = GetComponent<Collider2D>();
        }

        if (col == null)
        {
            Debug.LogWarning("[PrecipitationController] UseCollider mode requires a Collider2D");
            ConfigureShapeForWorldFixed(); // Fallback
            return;
        }

        Bounds b = col.bounds;

        // Spawn area spans collider width
        Vector3 spawnScale = new Vector3(
            b.size.x + bounds.horizontalPadding * 2f,
            1f,
            0.1f
        );
        shapeModule.scale = spawnScale;

        // Position shape at top of collider bounds
        Vector3 shapePosition = new Vector3(
            b.center.x - transform.position.x,
            b.max.y - transform.position.y + bounds.spawnHeightAbove,
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

        // Initial wind influence
        Vector2 windVector = Vector2.zero;
        if (WindManager.Instance != null)
        {
            windVector = WindManager.Instance.CurrentWindVector * currentPreset.windInfluenceMultiplier;
        }

        // All axes must use the same curve mode (TwoConstants)
        velocityModule.x = new ParticleSystem.MinMaxCurve(windVector.x * 0.8f, windVector.x * 1.2f);
        velocityModule.y = new ParticleSystem.MinMaxCurve(minFall + windVector.y, maxFall + windVector.y);
        velocityModule.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    private void ConfigureNoiseModule()
    {
        noiseModule.enabled = useGPUMotion && currentPreset.driftAmount > 0f;

        if (!noiseModule.enabled) return;

        // GPU-accelerated drift using noise module
        noiseModule.separateAxes = true;
        noiseModule.frequency = currentPreset.driftFrequency;
        noiseModule.scrollSpeed = 0.2f;
        noiseModule.damping = true;
        noiseModule.octaveCount = 2;

        // Horizontal drift (X axis) - full drift amount
        noiseModule.strengthX = new ParticleSystem.MinMaxCurve(currentPreset.driftAmount);

        // Vertical drift (Y axis) - reduced for natural look
        noiseModule.strengthY = new ParticleSystem.MinMaxCurve(currentPreset.driftAmount * 0.3f);

        // No Z drift for 2D
        noiseModule.strengthZ = new ParticleSystem.MinMaxCurve(0f);

        // Add turbulence influence
        if (WindManager.Instance != null)
        {
            float turbulence = WindManager.Instance.TurbulenceStrength * currentPreset.turbulenceInfluenceMultiplier;
            noiseModule.strengthX = new ParticleSystem.MinMaxCurve(currentPreset.driftAmount + turbulence);
            noiseModule.strengthY = new ParticleSystem.MinMaxCurve((currentPreset.driftAmount + turbulence) * 0.3f);
        }
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
        // Configure render mode based on precipitation type
        // Rain uses stretched billboards for motion blur effect
        if (currentPreset.type == PrecipitationType.Rain)
        {
            psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            psRenderer.velocityScale = 0.02f; // Stretch based on velocity
            psRenderer.lengthScale = 1.2f;    // Base stretch length
            psRenderer.cameraVelocityScale = 0f; // Don't stretch based on camera movement
        }
        else
        {
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

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
            emissionModule.rateOverTime = currentPreset != null ? currentPreset.GetEffectiveEmissionRate() : 0f;
            currentEmissionRate = emissionModule.rateOverTime.constant;
        }
        else
        {
            targetEmissionRate = currentPreset != null ? currentPreset.GetEffectiveEmissionRate() : 0f;
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

        Vector3 center;
        Vector3 size;

        switch (bounds.mode)
        {
            case BoundsMode.FollowCamera:
                Camera cam = Camera.main;
                if (cam == null) return;
                center = cam.transform.position;
                float camHeight = cam.orthographicSize * 2f;
                float camWidth = camHeight * cam.aspect;
                size = new Vector3(camWidth + bounds.horizontalPadding * 2f, camHeight, 0.1f);
                break;

            case BoundsMode.WorldFixed:
                center = transform.position;
                size = new Vector3(bounds.size.x, bounds.size.y, 0.1f);
                break;

            case BoundsMode.UseCollider:
                Collider2D col = bounds.boundsCollider ?? GetComponent<Collider2D>();
                if (col == null) return;
                center = col.bounds.center;
                size = col.bounds.size;
                break;

            default:
                return;
        }

        // Draw zone bounds
        Gizmos.color = boundsColor;
        Gizmos.DrawWireCube(center, size);

        // Draw spawn area
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Vector3 spawnCenter = new Vector3(
            center.x,
            center.y + size.y / 2f + bounds.spawnHeightAbove,
            currentPreset != null ? currentPreset.zOffset : -1f
        );
        Vector3 spawnSize = new Vector3(size.x, 1f, 0.1f);
        Gizmos.DrawWireCube(spawnCenter, spawnSize);
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
