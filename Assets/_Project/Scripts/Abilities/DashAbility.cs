using UnityEngine;

public class DashAbility : MonoBehaviour
{
    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    private Rigidbody2D rb;

    private bool isDashing = false;
    private float dashTimeRemaining;
    private float dashCooldownRemaining;
    private float dashDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            Debug.LogError($"[{gameObject.name}] DashAbility: Missing Rigidbody2D");
    }

    private void Update()
    {
        dashCooldownRemaining -= Time.deltaTime;

        if (isDashing)
        {
            dashTimeRemaining -= Time.deltaTime;
            if (dashTimeRemaining <= 0)
            {
                isDashing = false;
                rb.gravityScale = 1f; // Restore gravity after dash
            }
        }
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            // Lock velocity during dash
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0);
        }
    }

    public void PerformDash(float direction)
    {
        if (dashCooldownRemaining <= 0 && !isDashing && direction != 0)
        {
            isDashing = true;
            dashTimeRemaining = dashDuration;
            dashCooldownRemaining = dashCooldown;
            dashDirection = Mathf.Sign(direction);

            // Disable gravity during dash
            rb.gravityScale = 0f;

            // TODO: ULPC has no roll/dash animation — add VFX or custom anim later
        }
    }

    public bool IsDashing()
    {
        return isDashing;
    }

    public bool CanDash()
    {
        return dashCooldownRemaining <= 0 && !isDashing;
    }
}