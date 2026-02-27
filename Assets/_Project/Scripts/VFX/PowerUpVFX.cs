using UnityEngine;

/// <summary>
/// Adds idle visual presence (bobbing, glow, orbiting particles) and collection burst
/// to powerup pickups. Reads PowerUpType from sibling PowerUpPickup to select colors.
/// </summary>
public class PowerUpVFX : MonoBehaviour
{
    [Header("Bobbing")]
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobFrequency = 2f;

    [Header("Glow")]
    [SerializeField] private float glowScale = 2f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMin = 0.25f;
    [SerializeField] private float pulseMax = 0.6f;

    [Header("Ambient Particles")]
    [SerializeField] private int ambientMaxParticles = 12;
    [SerializeField] private float ambientEmissionRate = 4f;
    [SerializeField] private float orbitRadius = 0.4f;
    [SerializeField] private float orbitSpeed = 1.5f;

    [Header("Collection Burst")]
    [SerializeField] private int burstParticleCount = 15;
    [SerializeField] private float burstSpeed = 4f;
    [SerializeField] private float burstLifetime = 0.35f;

    [Header("Screen Flash")]
    [SerializeField] private float flashAlpha = 0.15f;
    [SerializeField] private float flashDuration = 0.15f;

    // Color palettes per powerup type
    private Color primaryColor;
    private Color accentColor;
    private Color glowColor;

    private SpriteRenderer glowRenderer;
    private ParticleSystem ambientParticles;
    private ParticleSystem.Particle[] particleBuffer;
    private Vector3 startPosition;
    private float perlinOffset;
    private Material glowMaterial;
    private Material ambientMaterial;
    private Texture2D glowTexture;

    private void Awake()
    {
        // Read color from sibling PowerUpPickup
        PowerUpPickup pickup = GetComponent<PowerUpPickup>();
        PowerUpType type = PowerUpType.DoubleJump;
        if (pickup != null)
        {
            type = pickup.Type;
        }

        SetColorsForType(type);

        // Create glow child
        CreateGlowChild();

        // Create ambient orbiting particles
        CreateAmbientParticles();

        perlinOffset = Random.Range(0f, 100f);
    }

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        UpdateBobbing();
        UpdateGlowPulse();
        UpdateOrbitingParticles();
    }

    /// <summary>
    /// Spawns a detached collection burst and screen flash. Call before destroying the pickup.
    /// </summary>
    public void SpawnCollectionVFX()
    {
        SpawnCollectionBurst();

        if (ScreenFlash.Instance != null)
        {
            ScreenFlash.Instance.Flash(
                new Color(primaryColor.r, primaryColor.g, primaryColor.b, flashAlpha),
                flashDuration);
        }
    }

    private void SetColorsForType(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.DoubleJump:
                // Soul Blue + Spectral Cyan accent
                primaryColor = new Color(0.255f, 0.412f, 0.882f, 1f);
                accentColor = new Color(0f, 0.808f, 0.82f, 1f);
                glowColor = new Color(0.15f, 0.35f, 0.85f, 0.5f);
                break;
            case PowerUpType.Dash:
                // Aged Gold + Deep Crimson accent
                primaryColor = new Color(0.812f, 0.710f, 0.231f, 1f);
                accentColor = new Color(0.698f, 0.133f, 0.133f, 1f);
                glowColor = new Color(0.8f, 0.65f, 0.15f, 0.5f);
                break;
            default:
                primaryColor = new Color(0f, 0.808f, 0.82f, 1f);
                accentColor = Color.white;
                glowColor = new Color(0f, 0.6f, 0.65f, 0.5f);
                break;
        }
    }

    private void UpdateBobbing()
    {
        float yOffset = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = new Vector3(startPosition.x, startPosition.y + yOffset, startPosition.z);
    }

    private void UpdateGlowPulse()
    {
        if (glowRenderer == null)
            return;

        // Perlin-driven pulse for organic feel (matching ExperienceOrb pattern)
        float noise = Mathf.PerlinNoise(Time.time * pulseSpeed, perlinOffset);
        float alpha = Mathf.Lerp(pulseMin, pulseMax, noise);

        Color c = glowRenderer.color;
        c.a = alpha;
        glowRenderer.color = c;
    }

    private void UpdateOrbitingParticles()
    {
        if (ambientParticles == null)
            return;

        int count = ambientParticles.GetParticles(particleBuffer);
        for (int i = 0; i < count; i++)
        {
            // Apply gentle orbital motion via velocity
            float age = particleBuffer[i].startLifetime - particleBuffer[i].remainingLifetime;
            float angle = (age * orbitSpeed + i * 1.2f) * Mathf.PI * 2f;
            Vector3 orbitTarget = transform.position + new Vector3(
                Mathf.Cos(angle) * orbitRadius,
                Mathf.Sin(angle) * orbitRadius * 0.6f,
                0f);

            Vector3 toTarget = orbitTarget - particleBuffer[i].position;
            particleBuffer[i].velocity = toTarget * 3f;
        }
        ambientParticles.SetParticles(particleBuffer, count);
    }

    private void CreateGlowChild()
    {
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(transform, false);
        glowObj.transform.localPosition = Vector3.zero;
        glowObj.transform.localScale = Vector3.one * glowScale;

        glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = CreateSoftCircleSprite(32, 128f, 2f);
        glowRenderer.color = glowColor;
        glowRenderer.sortingLayerName = "Foreground";
        glowRenderer.sortingOrder = 9;

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            glowMaterial = new Material(shader);
            glowRenderer.material = glowMaterial;
        }
    }

    private void CreateAmbientParticles()
    {
        GameObject particleObj = new GameObject("AmbientParticles");
        particleObj.transform.SetParent(transform, false);
        particleObj.transform.localPosition = Vector3.zero;

        ambientParticles = particleObj.AddComponent<ParticleSystem>();
        var main = ambientParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        main.startSpeed = 0.2f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.startColor = primaryColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;
        main.playOnAwake = true;
        main.maxParticles = ambientMaxParticles;

        var emission = ambientParticles.emission;
        emission.rateOverTime = ambientEmissionRate;

        var shape = ambientParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = orbitRadius * 0.5f;

        // Color over lifetime: primary -> accent, fade out
        var colorOverLifetime = ambientParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(primaryColor, 0f),
                new GradientColorKey(accentColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Size over lifetime: shrink
        var sizeOverLifetime = ambientParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Renderer setup
        var psRenderer = ambientParticles.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.sortingLayerName = "Foreground";
        psRenderer.sortingOrder = 10;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            ambientMaterial = new Material(shader);
            ambientMaterial.SetFloat("_Surface", 1f); // Transparent
            psRenderer.material = ambientMaterial;
        }

        // Cache particle buffer
        particleBuffer = new ParticleSystem.Particle[ambientMaxParticles];
    }

    private void SpawnCollectionBurst()
    {
        GameObject burstObj = new GameObject("PowerUpCollectBurst");
        burstObj.transform.position = transform.position;

        ParticleSystem ps = burstObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = burstLifetime;
        main.startSpeed = burstSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.startColor = primaryColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        main.playOnAwake = true;
        main.maxParticles = burstParticleCount;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, burstParticleCount)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

        // Color over lifetime: primary -> accent, fade out
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(primaryColor, 0f),
                new GradientColorKey(accentColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Renderer setup
        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.sortingLayerName = "Foreground";
        psRenderer.sortingOrder = 11;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.SetFloat("_Surface", 1f); // Transparent
            psRenderer.material = mat;
        }

        // Auto-destroy after particles finish
        burstObj.AddComponent<SelfDestructVFX>();
    }

    private void OnDestroy()
    {
        if (glowMaterial != null) Destroy(glowMaterial);
        if (ambientMaterial != null) Destroy(ambientMaterial);
        if (glowTexture != null) Destroy(glowTexture);
    }

    private Sprite CreateSoftCircleSprite(int size, float pixelsPerUnit = 128f, float falloffPower = 3f)
    {
        glowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Texture2D tex = glowTexture;
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(dist / radius);
                alpha = Mathf.Pow(alpha, falloffPower);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }
}
