using UnityEngine;

/// <summary>
/// Generic impact burst VFX. Spawns particles on Awake, then self-destructs.
/// Assign to DamageSkillEffect.hitEffectPrefab or AttackData.impactVFXPrefab.
/// </summary>
public class SkillHitVFX : MonoBehaviour
{
    [Header("Impact Settings")]
    [SerializeField] private Color impactColor = new Color(1f, 0.9f, 0.4f, 1f);
    [SerializeField] private Color fadeColor = new Color(1f, 0.5f, 0.1f, 0f);
    [SerializeField] private int particleCount = 12;
    [SerializeField] private float burstSpeed = 3f;
    [SerializeField] private float lifetime = 0.25f;
    [SerializeField] private float startSize = 0.06f;

    private void Awake()
    {
        SpawnBurst();
    }

    private void SpawnBurst()
    {
        ParticleSystem ps = gameObject.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = lifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(burstSpeed * 0.5f, burstSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(startSize * 0.5f, startSize);
        main.startColor = impactColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        main.playOnAwake = true;
        main.maxParticles = particleCount;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, particleCount)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        // Color over lifetime: impact -> fade
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(impactColor, 0f),
                new GradientColorKey(fadeColor, 1f)
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

        gameObject.AddComponent<SelfDestructVFX>();
    }
}
