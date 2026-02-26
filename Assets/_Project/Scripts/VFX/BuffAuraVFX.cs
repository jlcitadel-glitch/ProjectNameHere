using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attached to the Player. Listens to ActiveBuffTracker events and
/// creates/destroys looping particle auras for each active buff.
/// </summary>
public class BuffAuraVFX : MonoBehaviour
{
    private ActiveBuffTracker buffTracker;
    private Dictionary<string, ParticleSystem> activeAuras = new Dictionary<string, ParticleSystem>();

    private static Material _particleMaterial;
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
                    _particleMaterial.SetFloat("_Surface", 1f);
                }
            }
            return _particleMaterial;
        }
    }

    private void Awake()
    {
        buffTracker = GetComponent<ActiveBuffTracker>();
    }

    private void OnEnable()
    {
        if (buffTracker != null)
        {
            buffTracker.OnBuffApplied += HandleBuffApplied;
            buffTracker.OnBuffExpired += HandleBuffExpired;
        }
    }

    private void OnDisable()
    {
        if (buffTracker != null)
        {
            buffTracker.OnBuffApplied -= HandleBuffApplied;
            buffTracker.OnBuffExpired -= HandleBuffExpired;
        }

        // Clean up all active auras
        foreach (var kvp in activeAuras)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        activeAuras.Clear();
    }

    private void HandleBuffApplied(string buffId)
    {
        // If aura already exists for this buff, remove the old one first
        if (activeAuras.TryGetValue(buffId, out var existing))
        {
            if (existing != null)
                Destroy(existing.gameObject);
            activeAuras.Remove(buffId);
        }

        var config = GetAuraConfig(buffId);
        if (config == null) return;

        var ps = CreateAuraParticleSystem(buffId, config.Value);
        activeAuras[buffId] = ps;
    }

    private void HandleBuffExpired(string buffId)
    {
        if (activeAuras.TryGetValue(buffId, out var ps))
        {
            if (ps != null)
            {
                // Stop emitting, let existing particles fade
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(ps.gameObject, 1f);
            }
            activeAuras.Remove(buffId);
        }
    }

    private ParticleSystem CreateAuraParticleSystem(string buffId, AuraConfig config)
    {
        var go = new GameObject($"BuffAura_{buffId}");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = config.offset;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = true;
        main.startLifetime = config.particleLifetime;
        main.startSpeed = config.particleSpeed;
        main.startSize = config.particleSize;
        main.startColor = config.primary;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;
        main.gravityModifier = config.gravity;

        var emission = ps.emission;
        emission.rateOverTime = config.emissionRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = config.shapeType;
        shape.radius = config.shapeRadius;
        if (config.shapeType == ParticleSystemShapeType.Circle)
            shape.arc = config.shapeArc;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(config.primary, 0f), new GradientColorKey(config.accent, 1f) },
            new[] { new GradientAlphaKey(config.startAlpha, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
            AnimationCurve.Linear(0f, config.sizeStart, 1f, config.sizeEnd));

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = "Foreground";
        renderer.sortingOrder = 8;

        if (ParticleMaterial != null)
            renderer.material = ParticleMaterial;

        return ps;
    }

    private struct AuraConfig
    {
        public Color primary;
        public Color accent;
        public float emissionRate;
        public float particleLifetime;
        public float particleSpeed;
        public float particleSize;
        public float gravity;
        public ParticleSystemShapeType shapeType;
        public float shapeRadius;
        public float shapeArc;
        public Vector3 offset;
        public float startAlpha;
        public float sizeStart;
        public float sizeEnd;
    }

    private static AuraConfig? GetAuraConfig(string buffId)
    {
        return buffId switch
        {
            "guard" => new AuraConfig
            {
                primary = new Color(0.5f, 0.7f, 1f, 1f),
                accent = new Color(0.8f, 0.9f, 1f, 1f),
                emissionRate = 10f,
                particleLifetime = 0.8f,
                particleSpeed = 0.3f,
                particleSize = 0.08f,
                gravity = 0f,
                shapeType = ParticleSystemShapeType.Circle,
                shapeRadius = 0.6f,
                shapeArc = 360f,
                offset = new Vector3(0f, -0.3f, 0f),
                startAlpha = 0.6f,
                sizeStart = 1f,
                sizeEnd = 0.3f
            },
            "berserk" => new AuraConfig
            {
                primary = new Color(1f, 0.3f, 0.1f, 1f),
                accent = new Color(1f, 0.7f, 0.2f, 1f),
                emissionRate = 12f,
                particleLifetime = 0.5f,
                particleSpeed = 1.5f,
                particleSize = 0.06f,
                gravity = -0.3f,
                shapeType = ParticleSystemShapeType.Circle,
                shapeRadius = 0.3f,
                shapeArc = 360f,
                offset = new Vector3(0f, -0.2f, 0f),
                startAlpha = 0.8f,
                sizeStart = 1f,
                sizeEnd = 0f
            },
            "war_cry" => new AuraConfig
            {
                primary = new Color(0.9f, 0.8f, 0.3f, 1f),
                accent = new Color(1f, 0.95f, 0.6f, 1f),
                emissionRate = 8f,
                particleLifetime = 0.6f,
                particleSpeed = 2f,
                particleSize = 0.1f,
                gravity = 0f,
                shapeType = ParticleSystemShapeType.Circle,
                shapeRadius = 0.5f,
                shapeArc = 360f,
                offset = Vector3.zero,
                startAlpha = 0.5f,
                sizeStart = 0.5f,
                sizeEnd = 1.5f
            },
            "magic_shield" => new AuraConfig
            {
                primary = new Color(0.3f, 0.8f, 1f, 1f),
                accent = new Color(0.6f, 0.95f, 1f, 1f),
                emissionRate = 15f,
                particleLifetime = 1f,
                particleSpeed = 0.2f,
                particleSize = 0.05f,
                gravity = 0f,
                shapeType = ParticleSystemShapeType.Circle,
                shapeRadius = 0.7f,
                shapeArc = 360f,
                offset = Vector3.zero,
                startAlpha = 0.4f,
                sizeStart = 1f,
                sizeEnd = 0.5f
            },
            "evasion" => new AuraConfig
            {
                primary = new Color(0.4f, 0.1f, 0.5f, 1f),
                accent = new Color(0.6f, 0.3f, 0.8f, 1f),
                emissionRate = 8f,
                particleLifetime = 0.4f,
                particleSpeed = 0.8f,
                particleSize = 0.04f,
                gravity = 0f,
                shapeType = ParticleSystemShapeType.Circle,
                shapeRadius = 0.4f,
                shapeArc = 360f,
                offset = new Vector3(0f, 0.3f, 0f),
                startAlpha = 0.6f,
                sizeStart = 1f,
                sizeEnd = 0f
            },
            _ => null
        };
    }
}
