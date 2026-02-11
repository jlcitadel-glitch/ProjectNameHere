using UnityEngine;

/// <summary>
/// Emits a particle trail while the player is dashing.
/// Place on the Player GameObject alongside DashAbility.
/// </summary>
[RequireComponent(typeof(DashAbility))]
public class DashTrailVFX : MonoBehaviour
{
    [Header("Trail Settings")]
    [SerializeField] private Color trailColor = new Color(0.6f, 0.85f, 1f, 0.8f);
    [SerializeField] private Color fadeColor = new Color(0.3f, 0.5f, 0.8f, 0f);
    [SerializeField] private float emissionRate = 40f;
    [SerializeField] private float particleLifetime = 0.3f;
    [SerializeField] private float particleSize = 0.12f;
    [SerializeField] private int maxParticles = 30;

    private DashAbility dashAbility;
    private ParticleSystem trailParticles;
    private ParticleSystem.EmissionModule emissionModule;

    private void Awake()
    {
        dashAbility = GetComponent<DashAbility>();
        CreateTrailParticles();
    }

    private void Update()
    {
        bool dashing = dashAbility != null && dashAbility.IsDashing();
        emissionModule.rateOverTime = dashing ? emissionRate : 0f;
    }

    private void CreateTrailParticles()
    {
        GameObject particleObj = new GameObject("DashTrail");
        particleObj.transform.SetParent(transform, false);
        particleObj.transform.localPosition = Vector3.zero;

        trailParticles = particleObj.AddComponent<ParticleSystem>();

        var main = trailParticles.main;
        main.startLifetime = particleLifetime;
        main.startSpeed = 0.1f;
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.5f, particleSize);
        main.startColor = trailColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;
        main.playOnAwake = true;
        main.maxParticles = maxParticles;

        emissionModule = trailParticles.emission;
        emissionModule.rateOverTime = 0f;

        var shape = trailParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

        // Color over lifetime: trail color -> transparent
        var colorOverLifetime = trailParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(trailColor, 0f),
                new GradientColorKey(fadeColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Size over lifetime: shrink
        var sizeOverLifetime = trailParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        // Renderer setup
        var psRenderer = trailParticles.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.sortingLayerName = "Foreground";
        psRenderer.sortingOrder = 8;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.SetFloat("_Surface", 1f);
            psRenderer.material = mat;
        }
    }
}
