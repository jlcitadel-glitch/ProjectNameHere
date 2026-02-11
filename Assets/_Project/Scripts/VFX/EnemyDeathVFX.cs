using UnityEngine;

/// <summary>
/// Spawns a radial particle burst on Awake, then self-destructs.
/// Assign to EnemyData.deathVFX as a prefab. Tint via Inspector.
/// </summary>
public class EnemyDeathVFX : MonoBehaviour
{
    [Header("Burst Settings")]
    [SerializeField] private Color burstColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color fadeColor = new Color(0.4f, 0.1f, 0.1f, 0f);
    [SerializeField] private int particleCount = 20;
    [SerializeField] private float burstSpeed = 3.5f;
    [SerializeField] private float lifetime = 0.4f;
    [SerializeField] private float startSize = 0.08f;

    private void Awake()
    {
        SpawnBurst();
    }

    private void SpawnBurst()
    {
        ParticleSystem ps = gameObject.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = lifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(burstSpeed * 0.6f, burstSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(startSize * 0.5f, startSize);
        main.startColor = burstColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        main.playOnAwake = true;
        main.maxParticles = particleCount;
        main.gravityModifier = 0.5f;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, particleCount)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;

        // Color over lifetime: burst color -> fade color (transparent)
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(burstColor, 0f),
                new GradientColorKey(fadeColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Size over lifetime: shrink to 0
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

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
            mat.SetFloat("_Surface", 1f);
            psRenderer.material = mat;
        }

        // Auto-destroy after particles finish
        gameObject.AddComponent<SelfDestructVFX>();
    }
}
