using UnityEngine;

/// <summary>
/// Flashes the enemy sprite white when damage is taken.
/// Subscribes to HealthSystem.OnDamageTaken.
/// </summary>
public class EnemyHitFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color flashColor = Color.white;

    private SpriteRenderer spriteRenderer;
    private HealthSystem healthSystem;
    private Color originalColor;
    private float flashTimer;
    private bool isFlashing;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        healthSystem = GetComponent<HealthSystem>();
    }

    private void OnEnable()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDamageTaken += HandleDamageTaken;
        }
    }

    private void OnDisable()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDamageTaken -= HandleDamageTaken;
        }

        // Restore original color if disabled mid-flash
        if (isFlashing && spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            isFlashing = false;
        }
    }

    private void Update()
    {
        if (!isFlashing)
            return;

        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f)
        {
            spriteRenderer.color = originalColor;
            isFlashing = false;
        }
    }

    private void HandleDamageTaken(float damage)
    {
        if (spriteRenderer == null)
            return;

        if (!isFlashing)
        {
            originalColor = spriteRenderer.color;
        }

        spriteRenderer.color = flashColor;
        flashTimer = flashDuration;
        isFlashing = true;
    }
}
