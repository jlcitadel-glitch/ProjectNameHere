using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerScript : MonoBehaviour
{
    [Header("Player Component References")]
    [SerializeField] Rigidbody2D rb;

    [Header("Movement")]
    [SerializeField] float speed = 10f;

    [Header("Jump")]
    [SerializeField] float jumpingPower = 18f;
    [SerializeField] float jumpCutMultiplier = 0.3f;

    [Header("Jump Forgiveness")]
    [SerializeField] float coyoteTime = 0.15f;
    [SerializeField] float jumpBufferTime = 0.1f;

    [Header("Gravity")]
    [SerializeField] float baseGravityScale = 3f;
    [SerializeField] float fallGravityMultiplier = 5f;
    [SerializeField] float maxFallSpeed = -30f;

    [Header("Grounding")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.2f;

    private float horizontal;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool wasGrounded;

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
    }

    private void Update()
    {
        bool grounded = IsGrounded();

        // Only reset coyote time when transitioning from air to ground
        if (grounded && !wasGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }

        // Only decrement coyote time when in the air
        if (!grounded)
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        wasGrounded = grounded;

        jumpBufferCounter = Mathf.Max(0, jumpBufferCounter - Time.deltaTime);

        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            PerformJump();
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
        }
    }

    private void FixedUpdate()
    {
        // Horizontal movement
        rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);

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
            horizontal = context.ReadValue<Vector2>().x;
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
}