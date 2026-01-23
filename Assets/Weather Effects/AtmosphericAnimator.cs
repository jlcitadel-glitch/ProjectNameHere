using UnityEngine;

public class AtmosphericAnimator : MonoBehaviour
{
    [Header("Drift Settings")]
    [SerializeField] private bool enableDrift = true;
    [SerializeField] private float driftSpeed = 0.5f;
    [SerializeField] private float driftAmount = 1.0f; // Reduced default

    [Header("Pulse/Fade Settings")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = 0.3f;
    [SerializeField] private float minOpacity = 0.4f; // Higher min
    [SerializeField] private float maxOpacity = 0.7f;

    [Header("Rotation Settings")]
    [SerializeField] private bool enableRotation = false;
    [SerializeField] private float rotationSpeed = 2f;

    private Vector3 startPosition;
    private SpriteRenderer spriteRenderer;
    private float driftOffsetX;
    private float driftOffsetY;
    private float pulseOffset;

    void Start()
    {
        startPosition = transform.localPosition;
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Random starting offsets so multiple effects don't sync
        driftOffsetX = Random.Range(0f, 100f);
        driftOffsetY = Random.Range(0f, 100f);
        pulseOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Drift movement using Perlin noise for smooth random motion
        if (enableDrift)
        {
            float noiseX = (Mathf.PerlinNoise(Time.time * driftSpeed + driftOffsetX, 0) - 0.5f) * 2f;
            float noiseY = (Mathf.PerlinNoise(0, Time.time * driftSpeed + driftOffsetY) - 0.5f) * 2f;

            Vector3 drift = new Vector3(noiseX * driftAmount, noiseY * driftAmount, 0);
            transform.localPosition = startPosition + drift;
        }

        // Opacity pulse for atmospheric breathing effect
        if (enablePulse && spriteRenderer != null)
        {
            float pulse = Mathf.PerlinNoise(Time.time * pulseSpeed + pulseOffset, 0);
            float opacity = Mathf.Lerp(minOpacity, maxOpacity, pulse);

            Color color = spriteRenderer.color;
            color.a = opacity;
            spriteRenderer.color = color;
        }

        // Slow rotation
        if (enableRotation)
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
    }
}