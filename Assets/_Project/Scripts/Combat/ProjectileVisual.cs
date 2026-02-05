using UnityEngine;

/// <summary>
/// Simple visual component for projectiles.
/// Rotates to face movement direction and can add trail effects.
/// </summary>
public class ProjectileVisual : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private bool rotateToFaceDirection = true;
    [SerializeField] private float rotationOffset = 0f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Auto-find components if not assigned
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (trailRenderer == null)
            trailRenderer = GetComponentInChildren<TrailRenderer>();
    }

    private void Update()
    {
        if (rotateToFaceDirection && rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
        }
    }

    /// <summary>
    /// Sets the projectile color (for different weapon types).
    /// </summary>
    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = color;

        if (trailRenderer != null)
        {
            trailRenderer.startColor = color;
            trailRenderer.endColor = new Color(color.r, color.g, color.b, 0f);
        }
    }
}
