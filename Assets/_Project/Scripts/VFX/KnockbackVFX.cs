using UnityEngine;

/// <summary>
/// Directional impact burst for knockback events.
/// Use KnockbackVFX.Spawn(position, direction) from any knockback source.
/// </summary>
public class KnockbackVFX : MonoBehaviour
{
    private const int ParticleCount = 8;
    private const float BurstSpeed = 4f;
    private const float Lifetime = 0.2f;
    private const float StartSize = 0.05f;

    private static readonly Color BurstColor = new Color(1f, 1f, 0.8f, 1f);
    private static readonly Color FadeColor = new Color(1f, 0.7f, 0.3f, 0f);

    /// <summary>
    /// Spawns a directional knockback burst at the given position.
    /// </summary>
    public static void Spawn(Vector3 position, Vector2 direction)
    {
        GameObject obj = new GameObject("KnockbackBurst");
        obj.transform.position = position;

        // Rotate to face knockback direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        obj.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = Lifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(BurstSpeed * 0.6f, BurstSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(StartSize * 0.5f, StartSize);
        main.startColor = BurstColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        main.playOnAwake = true;
        main.maxParticles = ParticleCount;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, ParticleCount)
        });

        // Narrow cone in knockback direction
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.05f;

        // Color over lifetime: bright -> fade
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(BurstColor, 0f),
                new GradientColorKey(FadeColor, 1f)
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

        obj.AddComponent<SelfDestructVFX>();
    }
}
