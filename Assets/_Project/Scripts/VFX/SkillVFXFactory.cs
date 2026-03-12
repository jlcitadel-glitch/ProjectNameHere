using UnityEngine;

/// <summary>
/// Static utility for spawning element-themed particle effects at runtime.
/// Each DamageType has a unique color palette. Follows the SkillHitVFX pattern
/// (ParticleSystem in code, URP Particles/Unlit shader, SelfDestructVFX cleanup).
/// </summary>
public static class SkillVFXFactory
{
    private static Material _particleMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Cleanup()
    {
        if (_particleMaterial != null)
        {
            Object.Destroy(_particleMaterial);
            _particleMaterial = null;
        }
    }

    private static Material ParticleMaterial
    {
        get
        {
            if (_particleMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null)
                    shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    _particleMaterial = new Material(shader);
                    _particleMaterial.SetFloat("_Surface", 1f); // Transparent
                }
            }
            return _particleMaterial;
        }
    }

    /// <summary>
    /// Spawns a melee sweep arc of particles in the facing direction.
    /// </summary>
    public static void SpawnMeleeSweep(Vector3 position, float facing, DamageType dmgType)
    {
        var (primary, accent) = GetColors(dmgType);

        var go = new GameObject("MeleeSweepVFX");
        go.transform.position = position;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = 2.5f;
        main.startSize = 0.08f;
        main.startColor = primary;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 20;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;
        shape.arc = 120f;
        shape.rotation = new Vector3(0, 0, facing > 0 ? -30f : 150f);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(primary, 0f), new GradientColorKey(accent, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        SetupRenderer(ps);

        go.AddComponent<SelfDestructVFX>();
        ps.Play();
    }

    /// <summary>
    /// Attaches a looping particle trail to a projectile GameObject.
    /// </summary>
    public static void AttachProjectileTrail(GameObject projectile, DamageType dmgType)
    {
        if (projectile == null) return;

        var (primary, accent) = GetColors(dmgType);

        var trailGo = new GameObject("ProjectileTrailVFX");
        trailGo.transform.SetParent(projectile.transform, false);
        trailGo.transform.localPosition = Vector3.zero;

        var ps = trailGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = true;
        main.startLifetime = 0.7f;
        main.startSpeed = 0.3f;
        main.startSize = 0.06f;
        main.startColor = primary;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 50;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 25f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(primary, 0f), new GradientColorKey(accent, 0.7f) },
            new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        SetupRenderer(ps);
    }

    /// <summary>
    /// Spawns a one-shot impact burst at the hit point.
    /// </summary>
    public static void SpawnImpactBurst(Vector3 position, DamageType dmgType)
    {
        var (primary, accent) = GetColors(dmgType);

        var go = new GameObject("ImpactBurstVFX");
        go.transform.position = position;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = 0.45f;
        main.startSpeed = 1.8f;
        main.startSize = 0.06f;
        main.startColor = primary;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 12;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(accent, 0f), new GradientColorKey(primary, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        SetupRenderer(ps);

        go.AddComponent<SelfDestructVFX>();
        ps.Play();
    }

    /// <summary>
    /// Spawns an expanding ring of particles for AoE effects.
    /// </summary>
    public static void SpawnAoECircle(Vector3 center, float radius, DamageType dmgType)
    {
        var (primary, accent) = GetColors(dmgType);

        var go = new GameObject("AoECircleVFX");
        go.transform.position = center;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = 1.0f;
        main.startSpeed = radius * 1.2f;
        main.startSize = 0.1f;
        main.startColor = primary;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 30;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(primary, 0f), new GradientColorKey(accent, 0.5f), new GradientColorKey(primary, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.5f));

        SetupRenderer(ps);

        go.AddComponent<SelfDestructVFX>();
        ps.Play();
    }

    /// <summary>
    /// Returns (primary, accent) color pair for a DamageType.
    /// </summary>
    public static (Color primary, Color accent) GetColors(DamageType dmgType)
    {
        return dmgType switch
        {
            DamageType.Physical  => (new Color(0.9f, 0.85f, 0.7f, 1f), Color.white),
            DamageType.Fire      => (new Color(1f, 0.4f, 0.1f, 1f), new Color(1f, 0.85f, 0.2f, 1f)),
            DamageType.Ice       => (new Color(0.5f, 0.8f, 1f, 1f), new Color(0.85f, 0.95f, 1f, 1f)),
            DamageType.Lightning => (new Color(1f, 0.95f, 0.4f, 1f), new Color(0.6f, 0.85f, 1f, 1f)),
            DamageType.Poison    => (new Color(0.3f, 0.8f, 0.2f, 1f), new Color(0.6f, 1f, 0.3f, 1f)),
            DamageType.Dark      => (new Color(0.4f, 0.1f, 0.5f, 1f), new Color(0.6f, 0.3f, 0.8f, 1f)),
            DamageType.Holy      => (new Color(1f, 0.95f, 0.7f, 1f), new Color(1f, 1f, 0.9f, 1f)),
            DamageType.Magic     => (new Color(0.6f, 0.3f, 1f, 1f), new Color(0.8f, 0.7f, 1f, 1f)),
            DamageType.True      => (Color.white, new Color(0.9f, 0.9f, 0.95f, 1f)),
            _                    => (Color.white, new Color(0.9f, 0.9f, 0.9f, 1f))
        };
    }

    private static void SetupRenderer(ParticleSystem ps)
    {
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = "Foreground";
        renderer.sortingOrder = 12;

        if (ParticleMaterial != null)
            renderer.sharedMaterial = ParticleMaterial;
    }
}
