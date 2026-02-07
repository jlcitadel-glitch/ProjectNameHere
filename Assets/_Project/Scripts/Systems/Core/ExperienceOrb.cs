using UnityEngine;

/// <summary>
/// XP orb that scatters from enemies on death, then attracts toward the player.
/// States: Scattering → Idle → Attracting → Collected.
/// Manages glow pulsing, particle trail, and collection burst VFX.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class ExperienceOrb : MonoBehaviour
{
    [Header("Scatter")]
    [SerializeField] private float scatterDuration = 0.5f;
    [SerializeField] private float scatterForce = 5f;
    [SerializeField] private float scatterUpwardBias = 3f;

    [Header("Idle")]
    [SerializeField] private float idleGravity = 0.3f;

    [Header("Attraction")]
    [SerializeField] private float attractionRadius = 3f;
    [SerializeField] private float attractionAcceleration = 20f;
    [SerializeField] private float maxAttractionSpeed = 15f;
    [SerializeField] private float collectRadius = 0.3f;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 30f;

    [Header("Glow")]
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseMin = 0.3f;
    [SerializeField] private float pulseMax = 0.8f;
    [SerializeField] private float attractBrightness = 1f;
    [SerializeField] private float attractScaleBoost = 1.3f;

    [Header("Trail")]
    [SerializeField] private float trailEmissionScatter = 15f;
    [SerializeField] private float trailEmissionIdle = 5f;
    [SerializeField] private float trailEmissionAttract = 30f;

    [Header("Collection Burst")]
    [SerializeField] private int burstParticleCount = 10;
    [SerializeField] private float burstSpeed = 3f;
    [SerializeField] private float burstLifetime = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip dropSound;
    [SerializeField] private AudioClip collectSound;

    private enum OrbState { Scattering, Idle, Attracting }

    private OrbState currentState = OrbState.Scattering;
    private Rigidbody2D rb;
    private int xpValue;
    private float stateTimer;
    private float lifetimeTimer;
    private Transform playerTransform;
    private LevelSystem playerLevelSystem;
    private float currentSpeed;

    // Visual references (found in children)
    private SpriteRenderer glowRenderer;
    private ParticleSystem trailParticles;
    private ParticleSystem.EmissionModule trailEmission;
    private Vector3 baseScale;

    /// <summary>
    /// Sets the XP value this orb awards on collection.
    /// </summary>
    public void Initialize(int xp)
    {
        xpValue = xp;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Ensure the orb has a core sprite
        SpriteRenderer coreRenderer = GetComponent<SpriteRenderer>();
        if (coreRenderer == null)
        {
            coreRenderer = gameObject.AddComponent<SpriteRenderer>();
            coreRenderer.sprite = CreateSoftCircleSprite(16);
            coreRenderer.color = new Color(0.2f, 0.5f, 1f, 1f); // Bright blue
            coreRenderer.sortingLayerName = "Foreground";
            coreRenderer.sortingOrder = 10;
        }

        baseScale = transform.localScale;

        // Find or create glow child
        Transform glowChild = transform.Find("Glow");
        if (glowChild == null)
        {
            glowChild = CreateGlowChild();
        }
        glowRenderer = glowChild.GetComponent<SpriteRenderer>();

        // Find or create trail particle system
        trailParticles = GetComponentInChildren<ParticleSystem>();
        if (trailParticles == null)
        {
            trailParticles = CreateTrailParticleSystem();
        }
        trailEmission = trailParticles.emission;

        // Setup collider
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            col.radius = 0.15f;
        }
    }

    private void Start()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerLevelSystem = player.GetComponent<LevelSystem>();
        }

        // Scatter in random direction with upward bias
        Vector2 scatterDir = Random.insideUnitCircle.normalized;
        scatterDir.y = Mathf.Abs(scatterDir.y) + scatterUpwardBias;
        scatterDir.Normalize();
        rb.AddForce(scatterDir * scatterForce, ForceMode2D.Impulse);

        stateTimer = scatterDuration;
        lifetimeTimer = lifetime;

        // Play drop sound
        SFXManager.PlayAtPoint(dropSound, transform.position);

        // Set initial trail emission
        ApplyVisualState();
    }

    private void Update()
    {
        // Lifetime countdown
        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        switch (currentState)
        {
            case OrbState.Scattering:
                UpdateScattering();
                break;
            case OrbState.Idle:
                UpdateIdle();
                break;
            case OrbState.Attracting:
                UpdateAttracting();
                break;
        }

        UpdateGlow();
    }

    private void UpdateScattering()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            currentState = OrbState.Idle;
            rb.gravityScale = idleGravity;
            rb.linearDamping = 2f;
            ApplyVisualState();
        }
    }

    private void UpdateIdle()
    {
        // Re-acquire player if lost (death/respawn)
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerLevelSystem = player.GetComponent<LevelSystem>();
            }
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        if (distance <= attractionRadius)
        {
            currentState = OrbState.Attracting;
            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
            currentSpeed = 0f;
            ApplyVisualState();
        }
    }

    private void UpdateAttracting()
    {
        if (playerTransform == null)
        {
            currentState = OrbState.Idle;
            rb.gravityScale = idleGravity;
            ApplyVisualState();
            return;
        }

        // Accelerate toward player
        Vector2 direction = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        currentSpeed = Mathf.Min(currentSpeed + attractionAcceleration * Time.deltaTime, maxAttractionSpeed);
        rb.linearVelocity = direction * currentSpeed;

        // Check for collection
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        if (distance <= collectRadius)
        {
            Collect();
        }
    }

    private void UpdateGlow()
    {
        if (glowRenderer == null)
            return;

        float baseAlpha = Mathf.Lerp(pulseMin, pulseMax,
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);

        // Brighten during attraction
        if (currentState == OrbState.Attracting)
        {
            baseAlpha = Mathf.Lerp(baseAlpha, attractBrightness,
                (Mathf.Sin(Time.time * pulseSpeed * 2f) + 1f) * 0.5f);
        }

        Color c = glowRenderer.color;
        c.a = baseAlpha;
        glowRenderer.color = c;
    }

    private void ApplyVisualState()
    {
        // Trail emission rate
        if (trailParticles != null)
        {
            float rate = currentState switch
            {
                OrbState.Scattering => trailEmissionScatter,
                OrbState.Idle => trailEmissionIdle,
                OrbState.Attracting => trailEmissionAttract,
                _ => trailEmissionIdle
            };
            trailEmission.rateOverTime = rate;
        }

        // Scale boost during attraction
        if (currentState == OrbState.Attracting)
        {
            transform.localScale = baseScale * attractScaleBoost;
        }
        else
        {
            transform.localScale = baseScale;
        }
    }

    private void Collect()
    {
        // Award XP
        if (playerLevelSystem != null)
        {
            playerLevelSystem.AddXP(xpValue);
        }

        // Play collect sound
        if (collectSound != null)
        {
            SFXManager.PlayAtPoint(collectSound, transform.position);
        }

        // Spawn collection burst
        SpawnCollectionBurst();

        Destroy(gameObject);
    }

    private void SpawnCollectionBurst()
    {
        GameObject burstObj = new GameObject("OrbCollectBurst");
        burstObj.transform.position = transform.position;

        ParticleSystem ps = burstObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = burstLifetime;
        main.startSpeed = burstSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startColor = new Color(0.3f, 0.6f, 1f, 1f); // Blue
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
        shape.radius = 0.1f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.3f, 0.6f, 1f), 0f),
                new GradientColorKey(new Color(0.5f, 0.8f, 1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Set URP-compatible material
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = "Foreground";
        renderer.sortingOrder = 11;

        // Try URP particle shader, fall back to Sprites/Default
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.SetFloat("_Surface", 1f); // Transparent
            renderer.material = mat;
        }

        // Auto-destroy
        SelfDestructVFX selfDestruct = burstObj.AddComponent<SelfDestructVFX>();
    }

    private Transform CreateGlowChild()
    {
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(transform, false);
        glowObj.transform.localPosition = Vector3.zero;
        glowObj.transform.localScale = Vector3.one * 2.5f; // Larger than core

        SpriteRenderer sr = glowObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSoftCircleSprite(32);
        sr.color = new Color(0.1f, 0.4f, 1f, 0.6f); // Blue glow
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 9;

        // Use additive material if available
        Shader addShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (addShader == null)
            addShader = Shader.Find("Sprites/Default");
        if (addShader != null)
        {
            sr.material = new Material(addShader);
        }

        return glowObj.transform;
    }

    private ParticleSystem CreateTrailParticleSystem()
    {
        GameObject trailObj = new GameObject("Trail");
        trailObj.transform.SetParent(transform, false);
        trailObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = trailObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        main.startColor = new Color(0.2f, 0.5f, 1f, 0.8f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;
        main.playOnAwake = true;
        main.maxParticles = 50;

        var emission = ps.emission;
        emission.rateOverTime = trailEmissionScatter;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        // Fade out over lifetime
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.2f, 0.5f, 1f), 0f),
                new GradientColorKey(new Color(0.4f, 0.8f, 1f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Size over lifetime: shrink
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Renderer setup
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = "Foreground";
        renderer.sortingOrder = 8;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.SetFloat("_Surface", 1f);
            renderer.material = mat;
        }

        return ps;
    }

    private Sprite CreateSoftCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
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
                alpha = Mathf.Pow(alpha, 1.5f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
