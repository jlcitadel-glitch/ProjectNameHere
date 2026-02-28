using UnityEngine;
using ProjectName.UI;

/// <summary>
/// Self-contained hazard zone spawned on enemy death (e.g., Mushroom).
/// Creates a looping poison cloud VFX and damages the player on contact.
/// Auto-destroys after the configured duration.
/// </summary>
public class NoxiousCloud : MonoBehaviour
{
    [Header("Hazard Settings")]
    [SerializeField] private float radius = 1.5f;
    [SerializeField] private float duration = 5f;
    [SerializeField] private float damagePerTick = 4f;
    [SerializeField] private float tickInterval = 0.5f;

    [Header("VFX")]
    [SerializeField] private Color cloudColor = new Color(0.3f, 0.8f, 0.2f, 0.4f);

    private float lifetime;
    private float tickTimer;
    private CircleCollider2D triggerCollider;
    private ParticleSystem cloudVFX;

    private void Awake()
    {
        SetupCollider();
        SetupVFX();
        lifetime = duration;
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            // Stop emitting and destroy after particles fade
            if (cloudVFX != null)
            {
                var emission = cloudVFX.emission;
                emission.enabled = false;
            }
            Destroy(gameObject, 2f);
            enabled = false;
            return;
        }

        tickTimer -= Time.deltaTime;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (tickTimer > 0f)
            return;

        if (other.isTrigger)
            return;

        if (!other.CompareTag("Player"))
            return;

        HealthSystem playerHealth = other.GetComponent<HealthSystem>();
        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<HealthSystem>();

        if (playerHealth != null && !playerHealth.IsInvulnerable)
        {
            playerHealth.TakeDamage(damagePerTick);
            tickTimer = tickInterval;

            // Spawn damage number
            var spawner = DamageNumberSpawner.GetOrCreate();
            if (spawner != null)
            {
                Vector3 spawnPos = other.bounds.center + Vector3.up * other.bounds.extents.y;
                spawner.SpawnDamage(spawnPos, damagePerTick, DamageNumberType.Normal, false);
            }
        }
    }

    private void SetupCollider()
    {
        triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = radius;
    }

    private void SetupVFX()
    {
        // Create ParticleSystem for the cloud effect
        GameObject vfxObj = new GameObject("CloudVFX");
        vfxObj.transform.SetParent(transform);
        vfxObj.transform.localPosition = Vector3.zero;

        cloudVFX = vfxObj.AddComponent<ParticleSystem>();

        // Stop auto-play to configure before starting
        cloudVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = cloudVFX.main;
        main.startLifetime = 2f;
        main.startSpeed = 0.3f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startColor = cloudColor;
        main.loop = true;
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.05f; // Slight upward drift

        var emission = cloudVFX.emission;
        emission.rateOverTime = 12f;

        var shape = cloudVFX.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius * 0.8f;

        // Color over lifetime: fade from cloudColor to transparent
        var colorOverLifetime = cloudVFX.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(cloudColor.r, cloudColor.g, cloudColor.b), 0f),
                new GradientColorKey(new Color(cloudColor.r, cloudColor.g, cloudColor.b), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(cloudColor.a, 0.15f),
                new GradientAlphaKey(cloudColor.a * 0.8f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // Size over lifetime: breathing/pulsing effect
        var sizeOverLifetime = cloudVFX.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(0.7f, 0.9f);
        sizeCurve.AddKey(1f, 0.3f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Renderer settings
        ParticleSystemRenderer renderer = vfxObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 11; // Above enemies
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        renderer.material = new Material(shader);
        renderer.material.SetColor("_Color", cloudColor);

        cloudVFX.Play();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
