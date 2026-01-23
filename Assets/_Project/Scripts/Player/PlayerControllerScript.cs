using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerScript : MonoBehaviour
{
    [Header("Player Component References")]
    [SerializeField] Rigidbody2D rb;

    [Header("Movement")]
    [SerializeField] float speed = 8f;

    [Header("Jump")]
    [SerializeField] float jumpingPower = 12f;
    [SerializeField] float jumpCutMultiplier = 0.5f;

    [Header("Jump Forgiveness")]
    [SerializeField] float coyoteTime = 0.15f;
    [SerializeField] float jumpBufferTime = 0.15f;

    [Header("Gravity")]
    [SerializeField] float baseGravityScale = 1f;
    [SerializeField] float fallGravityMultiplier = 2.5f;
    [SerializeField] float maxFallSpeed = -20f;

    [Header("Grounding")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.2f;

    private float horizontal;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool wasGrounded;

    // Ability components
    private DoubleJumpAbility doubleJumpAbility;
    private DashAbility dashAbility;

    // Double-tap dash detection
    private float lastTapTime;
    private float lastTapDirection;
    private float doubleTapWindow = 0.3f; // Time window for double-tap

    private void Awake()
    {
        if (!rb)
        {
            rb = GetComponent<Rigidbody2D>();
            if (!rb)
            {
                Debug.LogError("PlayerControllerScript: Rigidbody2D component not found!");
            }
        }

        if (!groundCheck)
        {
            Debug.LogError("PlayerControllerScript: groundCheck Transform is not assigned!");
        }

        // Get ability components if they exist
        doubleJumpAbility = GetComponent<DoubleJumpAbility>();
        dashAbility = GetComponent<DashAbility>();
    }

    private void Update()
    {
        // Refresh ability references each frame (only checks if null)
        if (doubleJumpAbility == null)
            doubleJumpAbility = GetComponent<DoubleJumpAbility>();
        if (dashAbility == null)
            dashAbility = GetComponent<DashAbility>();

        bool grounded = IsGrounded();

        // Only reset coyote time when transitioning from air to ground
        if (grounded && !wasGrounded)
        {
            coyoteTimeCounter = coyoteTime;

            // Reset double jump when landing
            if (doubleJumpAbility != null)
            {
                doubleJumpAbility.ResetJumps();
            }
        }

        // Only decrement coyote time when in the air
        if (!grounded)
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        wasGrounded = grounded;

        jumpBufferCounter = Mathf.Max(0, jumpBufferCounter - Time.deltaTime);

        // First jump (grounded or coyote time)
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            PerformJump();
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
        }
        // Double jump (if ability exists and in air)
        else if (doubleJumpAbility != null && jumpBufferCounter > 0 && doubleJumpAbility.CanJump())
        {
            PerformJump();
            doubleJumpAbility.ConsumeJump();
            jumpBufferCounter = 0;
        }
    }

    private void FixedUpdate()
    {
        // Skip normal movement if dashing
        if (dashAbility != null && dashAbility.IsDashing())
        {
            return;
        }

        // Horizontal movement
        rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);

        // Flip character based on movement direction
        if (horizontal > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (horizontal < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        // Faster falling
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = fallGravityMultiplier;
        }
        else
        {
            rb.gravityScale = baseGravityScale;
        }

        // Clamp fall speed
        if (rb.linearVelocity.y < maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
        }
    }

    #region PLAYER_CONTROLS
    public void Move(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            float newInput = context.ReadValue<Vector2>().x;

            // Detect double-tap for dash
            if (dashAbility != null && newInput != 0)
            {
                float currentTime = Time.time;

                // Check if this is a double-tap in the same direction
                if (currentTime - lastTapTime < doubleTapWindow &&
                    Mathf.Sign(newInput) == Mathf.Sign(lastTapDirection))
                {
                    // Double-tap detected! Perform dash
                    dashAbility.PerformDash(newInput);
                    lastTapTime = 0; // Reset to prevent triple-tap
                }
                else
                {
                    // First tap, record it
                    lastTapTime = currentTime;
                    lastTapDirection = newInput;
                }
            }

            horizontal = newInput;
        }
        else if (context.canceled)
        {
            horizontal = 0;
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        // Jump button pressed
        if (context.performed)
        {
            jumpBufferCounter = jumpBufferTime;
        }

        // Jump button released early -> cut jump
        if (context.canceled && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * jumpCutMultiplier
            );
        }
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (context.performed && dashAbility != null)
        {
            // Dash in the direction the player is facing
            float dashDirection = transform.localScale.x;

            // If player is inputting a direction, use that instead
            if (horizontal != 0)
            {
                dashDirection = horizontal;
            }

            dashAbility.PerformDash(dashDirection);
        }
    }
    #endregion

    private void PerformJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
    }

    private bool IsGrounded()
    {
        if (!groundCheck)
        {
            return false;
        }

        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        // Check if any collider found is NOT our own
        foreach (Collider2D collider in colliders)
        {
            if (collider.gameObject != gameObject)
            {
                return true;
            }
        }

        return false;
    }

    // Call this when a new ability is added
    public void RefreshAbilities()
    {
        doubleJumpAbility = GetComponent<DoubleJumpAbility>();
        dashAbility = GetComponent<DashAbility>();
    }
}