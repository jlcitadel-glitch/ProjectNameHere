using UnityEngine;

/// <summary>
/// Creates multi-depth parallax precipitation effect with back, mid, and front layers.
/// Add this component alongside PrecipitationController for enhanced visual depth.
/// </summary>
[RequireComponent(typeof(PrecipitationController))]
public class PrecipitationLayer : MonoBehaviour
{
    [System.Serializable]
    public class LayerSettings
    {
        [Tooltip("Enable this layer")]
        public bool enabled = true;

        [Tooltip("Size multiplier relative to base")]
        [Range(0.1f, 3f)]
        public float sizeMultiplier = 1f;

        [Tooltip("Speed multiplier relative to base (affects parallax feel)")]
        [Range(0.1f, 3f)]
        public float speedMultiplier = 1f;

        [Tooltip("Alpha/opacity multiplier")]
        [Range(0f, 1f)]
        public float alphaMultiplier = 1f;

        [Tooltip("Emission rate multiplier")]
        [Range(0f, 2f)]
        public float emissionMultiplier = 1f;

        [Tooltip("Z offset from base layer (negative = in front)")]
        public float zOffset = 0f;

        [Tooltip("Sorting order offset")]
        public int sortingOrderOffset = 0;
    }

    [Header("Layer Configuration")]
    [Tooltip("Back layer: distant, small, slow, faded")]
    [SerializeField] private LayerSettings backLayer = new LayerSettings
    {
        enabled = true,
        sizeMultiplier = 0.5f,
        speedMultiplier = 0.6f,
        alphaMultiplier = 0.4f,
        emissionMultiplier = 0.3f,
        zOffset = 2f,
        sortingOrderOffset = -2
    };

    [Tooltip("Mid layer: normal (uses base controller settings)")]
    [SerializeField] private LayerSettings midLayer = new LayerSettings
    {
        enabled = true,
        sizeMultiplier = 1f,
        speedMultiplier = 1f,
        alphaMultiplier = 1f,
        emissionMultiplier = 1f,
        zOffset = 0f,
        sortingOrderOffset = 0
    };

    [Tooltip("Front layer: close, large, fast, opaque")]
    [SerializeField] private LayerSettings frontLayer = new LayerSettings
    {
        enabled = true,
        sizeMultiplier = 1.5f,
        speedMultiplier = 1.4f,
        alphaMultiplier = 0.9f,
        emissionMultiplier = 0.2f,
        zOffset = -2f,
        sortingOrderOffset = 2
    };

    [Header("Runtime")]
    [SerializeField] private bool autoCreateLayers = true;

    // Layer particle systems
    private ParticleSystem backPS;
    private ParticleSystem frontPS;
    private PrecipitationController baseController;
    private PrecipitationPreset cachedPreset;

    private void Awake()
    {
        baseController = GetComponent<PrecipitationController>();
    }

    private void Start()
    {
        if (autoCreateLayers)
        {
            CreateLayers();
        }
    }

    private void OnEnable()
    {
        if (backPS != null) backPS.gameObject.SetActive(backLayer.enabled);
        if (frontPS != null) frontPS.gameObject.SetActive(frontLayer.enabled);
    }

    private void OnDisable()
    {
        if (backPS != null) backPS.gameObject.SetActive(false);
        if (frontPS != null) frontPS.gameObject.SetActive(false);
    }

    /// <summary>
    /// Creates the additional particle system layers.
    /// </summary>
    public void CreateLayers()
    {
        PrecipitationPreset preset = baseController.CurrentPreset;
        if (preset == null) return;

        cachedPreset = preset;

        // Create back layer
        if (backLayer.enabled)
        {
            backPS = CreateLayerParticleSystem("Back", backLayer, preset);
        }

        // Create front layer
        if (frontLayer.enabled)
        {
            frontPS = CreateLayerParticleSystem("Front", frontLayer, preset);
        }
    }

    private ParticleSystem CreateLayerParticleSystem(string layerName, LayerSettings settings, PrecipitationPreset preset)
    {
        // Create child GameObject
        GameObject layerObj = new GameObject($"Layer_{layerName}");
        layerObj.transform.SetParent(transform);
        layerObj.transform.localPosition = new Vector3(0f, 0f, settings.zOffset);

        // Add ParticleSystem
        ParticleSystem ps = layerObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Configure based on base controller with layer modifications
        ConfigureLayerParticleSystem(ps, settings, preset);

        return ps;
    }

    private void ConfigureLayerParticleSystem(ParticleSystem ps, LayerSettings settings, PrecipitationPreset preset)
    {
        var main = ps.main;
        var emission = ps.emission;
        var shape = ps.shape;
        var velocity = ps.velocityOverLifetime;
        var colorOverLifetime = ps.colorOverLifetime;
        var noise = ps.noise;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();

        // Main module
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(
            preset.sizeRange.x * settings.sizeMultiplier,
            preset.sizeRange.y * settings.sizeMultiplier
        );
        main.startLifetime = new ParticleSystem.MinMaxCurve(
            preset.lifetime - preset.lifetimeVariation,
            preset.lifetime + preset.lifetimeVariation
        );

        // Apply alpha multiplier to tint
        Color layerTint = preset.tint;
        layerTint.a *= settings.alphaMultiplier;
        main.startColor = layerTint;

        main.maxParticles = Mathf.RoundToInt(preset.GetEffectiveMaxParticles() * settings.emissionMultiplier);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        // Emission
        emission.rateOverTime = preset.GetEffectiveEmissionRate() * settings.emissionMultiplier;

        // Copy shape from base (will be updated by controller's bounds)
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30f, 1f, 0.1f); // Will be overridden

        // Velocity with speed multiplier
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        float minFall = -(preset.fallSpeed + preset.fallSpeedVariation) * settings.speedMultiplier;
        float maxFall = -(preset.fallSpeed - preset.fallSpeedVariation) * settings.speedMultiplier;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(minFall, maxFall);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // Noise module for drift
        if (preset.driftAmount > 0f)
        {
            noise.enabled = true;
            noise.separateAxes = true;
            noise.frequency = preset.driftFrequency;
            noise.scrollSpeed = 0.2f;
            noise.damping = true;
            noise.octaveCount = 2;
            noise.strengthX = new ParticleSystem.MinMaxCurve(preset.driftAmount * settings.speedMultiplier);
            noise.strengthY = new ParticleSystem.MinMaxCurve(preset.driftAmount * 0.3f * settings.speedMultiplier);
            noise.strengthZ = new ParticleSystem.MinMaxCurve(0f);
        }
        else
        {
            noise.enabled = false;
        }

        // Color over lifetime for fade
        if (preset.fadeIn || preset.fadeOut)
        {
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = preset.GetAlphaGradient();
        }

        // Renderer
        if (preset.type == PrecipitationType.Rain)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.02f * settings.speedMultiplier;
            renderer.lengthScale = 1.2f;
        }
        else
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        // Material
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.SetColor("_Color", layerTint);
            if (preset.particleSprite != null)
            {
                mat.mainTexture = preset.particleSprite.texture;
            }
            renderer.material = mat;
        }

        // Sorting
        renderer.sortingLayerName = preset.sortingLayerName;
        renderer.sortingOrder = preset.orderInLayer + settings.sortingOrderOffset;

        // Start playing
        ps.Play();
    }

    /// <summary>
    /// Updates layers when the base preset changes.
    /// </summary>
    public void RefreshLayers()
    {
        PrecipitationPreset preset = baseController.CurrentPreset;
        if (preset == null || preset == cachedPreset) return;

        // Destroy existing layers
        if (backPS != null)
        {
            Destroy(backPS.gameObject);
            backPS = null;
        }
        if (frontPS != null)
        {
            Destroy(frontPS.gameObject);
            frontPS = null;
        }

        // Recreate
        CreateLayers();
    }

    /// <summary>
    /// Enable or disable a specific layer at runtime.
    /// </summary>
    public void SetLayerEnabled(int layerIndex, bool enabled)
    {
        switch (layerIndex)
        {
            case 0: // Back
                backLayer.enabled = enabled;
                if (backPS != null) backPS.gameObject.SetActive(enabled);
                break;
            case 1: // Mid (base controller)
                midLayer.enabled = enabled;
                break;
            case 2: // Front
                frontLayer.enabled = enabled;
                if (frontPS != null) frontPS.gameObject.SetActive(enabled);
                break;
        }
    }

    private void LateUpdate()
    {
        // Sync layer positions with base controller shape position
        if (baseController.Bounds.mode == BoundsMode.FollowCamera)
        {
            SyncLayerPositions();
        }
    }

    private void SyncLayerPositions()
    {
        Vector3 basePos = transform.position;

        if (backPS != null)
        {
            backPS.transform.position = new Vector3(basePos.x, basePos.y, basePos.z + backLayer.zOffset);
        }

        if (frontPS != null)
        {
            frontPS.transform.position = new Vector3(basePos.x, basePos.y, basePos.z + frontLayer.zOffset);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize layer Z positions
        Vector3 pos = transform.position;

        if (backLayer.enabled)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireCube(pos + Vector3.forward * backLayer.zOffset, new Vector3(2f, 0.5f, 0.1f));
            UnityEditor.Handles.Label(pos + Vector3.forward * backLayer.zOffset + Vector3.up * 0.5f, "Back");
        }

        if (midLayer.enabled)
        {
            Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.3f);
            Gizmos.DrawWireCube(pos, new Vector3(2f, 0.5f, 0.1f));
            UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, "Mid");
        }

        if (frontLayer.enabled)
        {
            Gizmos.color = new Color(1f, 0.5f, 0.5f, 0.3f);
            Gizmos.DrawWireCube(pos + Vector3.forward * frontLayer.zOffset, new Vector3(2f, 0.5f, 0.1f));
            UnityEditor.Handles.Label(pos + Vector3.forward * frontLayer.zOffset + Vector3.up * 0.5f, "Front");
        }
    }
}
