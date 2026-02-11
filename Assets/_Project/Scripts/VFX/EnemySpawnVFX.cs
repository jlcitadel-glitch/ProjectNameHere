using UnityEngine;

/// <summary>
/// Materialization burst VFX for enemy spawns. Plays on Awake, then self-destructs.
/// Assign to EnemyData.spawnVFX as a prefab.
/// </summary>
public class EnemySpawnVFX : MonoBehaviour
{
    [Header("Spawn Burst Settings")]
    [SerializeField] private Color burstColor = new Color(0.6f, 0.4f, 0.8f, 1f);
    [SerializeField] private Color fadeColor = new Color(0.3f, 0.2f, 0.5f, 0f);
    [SerializeField] private int particleCount = 16;
    [SerializeField] private float burstSpeed = 2f;
    [SerializeField] private float lifetime = 0.5f;
    [SerializeField] private float startSize = 0.07f;

    private void Awake()
    {
        SpawnBurst();
    }

    private void SpawnBurst()
    {
        ParticleSystem ps = gameObject.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = lifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(burstSpeed * 0.4f, burstSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(startSize * 0.5f, startSize);
        main.startColor = burstColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        main.playOnAwake = true;
        main.maxParticles = particleCount;
        main.gravityModifier = -0.3f; // Slight upward float

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, particleCount)
        });

        // Upward-biased cone for materialization feel
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 40f;
        shape.radius = 0.3f;
        shape.rotation = new Vector3(-90f, 0f, 0f); // Point upward

        // Color over lifetime: burst -> fade
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
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Size over lifetime: grow slightly then shrink
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 0f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Renderer setup
        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.sortingLayerName = "Foreground";
        psRenderer.sortingOrder = 10;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.SetFloat("_Surface", 1f);
            psRenderer.material = mat;
        }

        gameObject.AddComponent<SelfDestructVFX>();
    }
}
