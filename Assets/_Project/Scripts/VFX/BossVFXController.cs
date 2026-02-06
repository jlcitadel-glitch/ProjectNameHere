using UnityEngine;

/// <summary>
/// Manages all boss fight VFX: entrance fog sweep, phase-change shockwave + screen flash,
/// and enrage persistent aura. Attach to the same GameObject as BossController.
/// Subscribes to BossController events and spawns effects programmatically.
/// </summary>
[RequireComponent(typeof(BossController))]
public class BossVFXController : MonoBehaviour
{
    [Header("Entrance")]
    [SerializeField] private float entranceShakeMagnitude = 0.2f;
    [SerializeField] private float entranceShakeDuration = 0.3f;
    [SerializeField] private int fogParticleCount = 50;
    [SerializeField] private float fogExpandSpeed = 4f;
    [SerializeField] private float fogLifetime = 1f;

    [Header("Phase Change")]
    [SerializeField] private Color phaseFlashColor = new Color(1f, 0f, 0f, 0.4f);
    [SerializeField] private float phaseFlashDuration = 0.2f;
    [SerializeField] private int shockwaveParticleCount = 40;
    [SerializeField] private float shockwaveSpeed = 6f;
    [SerializeField] private float shockwaveLifetime = 0.5f;

    [Header("Enrage Aura")]
    [SerializeField] private float auraEmissionRate = 20f;
    [SerializeField] private float auraParticleLifetime = 1f;
    [SerializeField] private float auraRadius = 1f;

    private BossController bossController;
    private AdvancedCameraController cameraController;
    private GameObject activeAura;

    private void Awake()
    {
        bossController = GetComponent<BossController>();
    }

    private void Start()
    {
        cameraController = FindAnyObjectByType<AdvancedCameraController>();

        bossController.OnPhaseChanged += HandlePhaseChanged;
        bossController.OnBossDefeated += HandleBossDefeated;

        // Play entrance VFX
        PlayEntranceVFX();
    }

    private void OnDestroy()
    {
        if (bossController != null)
        {
            bossController.OnPhaseChanged -= HandlePhaseChanged;
            bossController.OnBossDefeated -= HandleBossDefeated;
        }

        if (activeAura != null)
        {
            Destroy(activeAura);
        }
    }

    private void HandlePhaseChanged(BossController.BossPhase newPhase)
    {
        switch (newPhase)
        {
            case BossController.BossPhase.Phase2:
                PlayPhaseChangeVFX();
                break;
            case BossController.BossPhase.Enraged:
                PlayPhaseChangeVFX();
                SpawnEnrageAura();
                break;
        }
    }

    private void HandleBossDefeated()
    {
        if (activeAura != null)
        {
            Destroy(activeAura);
            activeAura = null;
        }
    }

    private void PlayEntranceVFX()
    {
        // Camera shake
        if (cameraController != null)
        {
            cameraController.Shake(entranceShakeMagnitude, entranceShakeDuration);
        }

        // Dark fog sweep
        SpawnFogSweep();
    }

    private void SpawnFogSweep()
    {
        GameObject fogObj = new GameObject("BossEntranceFog");
        fogObj.transform.position = transform.position;

        ParticleSystem ps = fogObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = fogLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(fogExpandSpeed * 0.5f, fogExpandSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startColor = new Color(0.15f, 0f, 0f, 0.6f); // Dark red
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        main.playOnAwake = true;
        main.maxParticles = fogParticleCount;
        main.gravityModifier = -0.2f; // Slight upward float

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, fogParticleCount)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;

        // Fade out
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.15f, 0f, 0f), 0f),
                new GradientColorKey(new Color(0.05f, 0f, 0f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.6f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Grow over lifetime
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.5f));

        SetupParticleRenderer(ps, 5);
        fogObj.AddComponent<SelfDestructVFX>();
    }

    private void PlayPhaseChangeVFX()
    {
        // Screen flash
        if (ScreenFlash.Instance != null)
        {
            ScreenFlash.Instance.Flash(phaseFlashColor, phaseFlashDuration);
        }

        // Camera shake
        if (cameraController != null)
        {
            cameraController.Shake(entranceShakeMagnitude * 1.5f, entranceShakeDuration);
        }

        // Shockwave ring
        SpawnShockwave();
    }

    private void SpawnShockwave()
    {
        GameObject waveObj = new GameObject("BossShockwave");
        waveObj.transform.position = transform.position;

        ParticleSystem ps = waveObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = shockwaveLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(shockwaveSpeed * 0.7f, shockwaveSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.startColor = new Color(0.8f, 0.1f, 0.1f, 0.8f); // Red
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        main.playOnAwake = true;
        main.maxParticles = shockwaveParticleCount;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, shockwaveParticleCount)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;
        shape.radiusThickness = 0f; // Emit from edge only (ring)

        // Red → Dark fade
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.8f, 0.1f, 0.1f), 0f),
                new GradientColorKey(new Color(0.2f, 0f, 0f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Shrink over lifetime
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

        SetupParticleRenderer(ps, 12);
        waveObj.AddComponent<SelfDestructVFX>();
    }

    private void SpawnEnrageAura()
    {
        if (activeAura != null)
            return;

        activeAura = new GameObject("EnrageAura");
        activeAura.transform.SetParent(transform);
        activeAura.transform.localPosition = Vector3.zero;

        ParticleSystem ps = activeAura.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = auraParticleLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = new Color(1f, 0.3f, 0f, 0.7f); // Orange-red
        main.simulationSpace = ParticleSystemSimulationSpace.Local; // Follows boss
        main.loop = true;
        main.playOnAwake = true;
        main.maxParticles = 40;

        var emission = ps.emission;
        emission.rateOverTime = auraEmissionRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = auraRadius;

        // Red → Orange → Transparent
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.1f, 0f), 0f),
                new GradientColorKey(new Color(1f, 0.5f, 0f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.7f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Shrink over lifetime
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        SetupParticleRenderer(ps, 8);
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
