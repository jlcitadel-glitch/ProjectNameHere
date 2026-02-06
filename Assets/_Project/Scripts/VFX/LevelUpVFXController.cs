using UnityEngine;

/// <summary>
/// Spawns a level-up VFX at the player's position when LevelSystem.OnLevelUp fires.
/// Creates radial burst + vertical column particle systems programmatically.
/// Attach to the Player GameObject.
/// </summary>
public class LevelUpVFXController : MonoBehaviour
{
    [Header("Radial Burst")]
    [SerializeField] private int burstCount = 30;
    [SerializeField] private float burstSpeed = 4f;
    [SerializeField] private float burstLifetime = 0.5f;
    [SerializeField] private float burstRadius = 0.5f;

    [Header("Vertical Column")]
    [SerializeField] private float columnEmissionRate = 40f;
    [SerializeField] private float columnDuration = 1f;
    [SerializeField] private float columnSpeed = 3f;
    [SerializeField] private float columnLifetime = 0.8f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeMagnitude = 0.15f;
    [SerializeField] private float shakeDuration = 0.3f;

    private LevelSystem levelSystem;

    private void Start()
    {
        levelSystem = GetComponent<LevelSystem>();
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp += HandleLevelUp;
        }
    }

    private void OnDestroy()
    {
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp -= HandleLevelUp;
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        SpawnLevelUpVFX();

        // Camera shake
        AdvancedCameraController cam = FindAnyObjectByType<AdvancedCameraController>();
        if (cam != null)
        {
            cam.Shake(shakeMagnitude, shakeDuration);
        }
    }

    private void SpawnLevelUpVFX()
    {
        GameObject vfxRoot = new GameObject("LevelUpVFX");
        vfxRoot.transform.position = transform.position;

        CreateRadialBurst(vfxRoot.transform);
        CreateVerticalColumn(vfxRoot.transform);

        vfxRoot.AddComponent<SelfDestructVFX>();
    }

    private void CreateRadialBurst(Transform parent)
    {
        GameObject burstObj = new GameObject("RadialBurst");
        burstObj.transform.SetParent(parent, false);

        ParticleSystem ps = burstObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = burstLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(burstSpeed * 0.6f, burstSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        main.playOnAwake = true;
        main.maxParticles = burstCount;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, burstCount)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = burstRadius;
        shape.radiusThickness = 1f; // Emit from edge

        // Gold → White gradient
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.84f, 0f), 0f),   // Gold
                new GradientColorKey(Color.white, 1f)
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

        SetupParticleRenderer(ps, 15);
    }

    private void CreateVerticalColumn(Transform parent)
    {
        GameObject colObj = new GameObject("Column");
        colObj.transform.SetParent(parent, false);

        ParticleSystem ps = colObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = columnDuration;
        main.startLifetime = columnLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(columnSpeed * 0.5f, columnSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        main.playOnAwake = true;
        main.maxParticles = 60;

        var emission = ps.emission;
        emission.rateOverTime = columnEmissionRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 5f;
        shape.radius = 0.3f;
        // Cone emits upward by default (local Y+)
        shape.rotation = new Vector3(-90f, 0f, 0f); // Point upward in 2D

        // White → Gold → Transparent
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.84f, 0f), 0.5f), // Gold
                new GradientColorKey(new Color(1f, 0.84f, 0f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.5f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Size over lifetime: shrink
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        SetupParticleRenderer(ps, 14);
    }

    private void SetupParticleRenderer(ParticleSystem ps, int sortOrder)
    {
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = "Foreground";
        renderer.sortingOrder = sortOrder;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.SetFloat("_Surface", 1f); // Transparent
            renderer.material = mat;
        }
    }
}
