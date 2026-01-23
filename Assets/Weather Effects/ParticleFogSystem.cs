using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleFogSystem : MonoBehaviour
{
    [Header("Fog Behavior")]
    [SerializeField] private Vector2 windDirection = new Vector2(1f, 0.2f);
    [SerializeField] private float windStrength = 2f;
    [SerializeField] private float turbulence = 1f;

    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();

        // Configure particle system for rolling fog
        var main = ps.main;
        main.startSpeed = 0.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 20f);
        main.startColor = new Color(0.7f, 0.75f, 0.8f, 0.3f);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 3f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30f, 20f, 0.1f);

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(1f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-30f, 30f);

        // Fix the renderer - this was missing!
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        // Create a soft circular texture for fog
        Texture2D fogTexture = CreateFogTexture();
        renderer.material.mainTexture = fogTexture;
    }

    Texture2D CreateFogTexture()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(distance / radius);
                alpha = Mathf.Pow(alpha, 2f); // Softer falloff

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;

        return texture;
    }

    void LateUpdate()
    {
        if (ps == null) return;

        int count = ps.particleCount;
        if (particles == null || particles.Length < count)
        {
            particles = new ParticleSystem.Particle[ps.main.maxParticles];
        }

        count = ps.GetParticles(particles);

        // Apply turbulent wind to each particle
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = particles[i].position;

            // Perlin noise for turbulence
            float noiseX = Mathf.PerlinNoise(pos.x * 0.1f + Time.time * 0.3f, pos.y * 0.1f) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(pos.x * 0.1f, pos.y * 0.1f + Time.time * 0.3f) * 2f - 1f;

            Vector3 turbulenceForce = new Vector3(noiseX, noiseY, 0) * turbulence;
            Vector3 windForce = new Vector3(windDirection.x, windDirection.y, 0) * windStrength;

            Vector3 totalVelocity = particles[i].velocity + (turbulenceForce + windForce) * Time.deltaTime;
            particles[i].velocity = totalVelocity;
        }

        ps.SetParticles(particles, count);
    }
}