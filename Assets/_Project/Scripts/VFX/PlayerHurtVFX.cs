using UnityEngine;

/// <summary>
/// Visual feedback when the player takes damage: sprite flash, hit particles, and screen flash.
/// Place on the Player GameObject.
/// </summary>
[RequireComponent(typeof(HealthSystem))]
public class PlayerHurtVFX : MonoBehaviour
{
    [Header("Sprite Flash")]
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color flashColor = Color.white;

    [Header("Hit Particles")]
    [SerializeField] private Color hitColor = new Color(0.9f, 0.15f, 0.15f, 1f);
    [SerializeField] private int hitParticleCount = 10;
    [SerializeField] private float hitParticleSpeed = 2.5f;
    [SerializeField] private float hitParticleLifetime = 0.3f;
    [SerializeField] private float hitParticleSize = 0.06f;

    [Header("Screen Flash")]
    [SerializeField] private float screenFlashAlpha = 0.12f;
    [SerializeField] private float screenFlashDuration = 0.1f;

    private HealthSystem healthSystem;
    private LayeredSpriteController layeredSprite;
    private SpriteRenderer fallbackRenderer;
    private Color originalColor;
    private float flashTimer;
    private bool isFlashing;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        layeredSprite = GetComponent<LayeredSpriteController>();
        fallbackRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDamageTaken += HandleDamageTaken;
        }
    }

    private void OnDisable()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDamageTaken -= HandleDamageTaken;
        }

        if (isFlashing)
        {
            if (UseLayeredFlash())
                layeredSprite.RestoreAllTints();
            else if (fallbackRenderer != null)
                fallbackRenderer.color = originalColor;
            isFlashing = false;
        }
    }

    private void Update()
    {
        if (!isFlashing)
            return;

        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f)
        {
            if (UseLayeredFlash())
                layeredSprite.RestoreAllTints();
            else if (fallbackRenderer != null)
                fallbackRenderer.color = originalColor;
            isFlashing = false;
        }
    }

    private void HandleDamageTaken(float damage)
    {
        FlashSprite();
        SpawnHitParticles();
        TriggerScreenFlash();
    }

    private bool UseLayeredFlash()
    {
        // Use layered flash only when the layered system is actively rendering
        // (fallback renderer disabled means layered appearance is applied)
        return layeredSprite != null && fallbackRenderer != null && !fallbackRenderer.enabled;
    }

    private void FlashSprite()
    {
        if (UseLayeredFlash())
        {
            layeredSprite.FlashAll(flashColor);
            flashTimer = flashDuration;
            isFlashing = true;
            return;
        }

        if (fallbackRenderer == null)
            return;

        if (!isFlashing)
        {
            originalColor = fallbackRenderer.color;
        }

        fallbackRenderer.color = flashColor;
        flashTimer = flashDuration;
        isFlashing = true;
    }

    private void SpawnHitParticles()
    {
        GameObject burstObj = new GameObject("PlayerHitBurst");
        burstObj.transform.position = transform.position;

        ParticleSystem ps = burstObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = hitParticleLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(hitParticleSpeed * 0.5f, hitParticleSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(hitParticleSize * 0.5f, hitParticleSize);
        main.startColor = hitColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        main.playOnAwake = true;
        main.maxParticles = hitParticleCount;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, hitParticleCount)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        // Color over lifetime: red -> transparent
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(hitColor, 0f),
                new GradientColorKey(hitColor, 0.5f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Size over lifetime: shrink
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Renderer setup
        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.sortingLayerName = "Foreground";
        psRenderer.sortingOrder = 12;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.SetFloat("_Surface", 1f);
            psRenderer.material = mat;
        }

        burstObj.AddComponent<SelfDestructVFX>();
    }

    private void TriggerScreenFlash()
    {
        if (ScreenFlash.Instance != null)
        {
            ScreenFlash.Instance.Flash(
                new Color(hitColor.r, hitColor.g, hitColor.b, screenFlashAlpha),
                screenFlashDuration);
        }
    }
}
