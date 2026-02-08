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

    [Header("Audio")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] [Range(0f, 2f)] private float jumpVolume = 1f;
    [SerializeField] private AudioClip landSound;
    [SerializeField] [Range(0f, 2f)] private float landVolume = 1f;
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] [Range(0f, 2f)] private float footstepVolume = 1f;
    [SerializeField] private float footstepInterval = 0.3f;

    // Audio
    private AudioSource audioSource;
    private float footstepTimer;

    // Animator
    private Animator animator;
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int AnimVelocityY = Animator.StringToHash("VelocityY");

    private float horizontal;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool wasGrounded;

    // Health
    private HealthSystem healthSystem;
    private bool IsDead => healthSystem != null && healthSystem.IsDead;

    // Ability components
    private DoubleJumpAbility doubleJumpAbility;
    private DashAbility dashAbility;
    private CombatController combatController;
    private StatSystem statSystem;

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

        // Auto-find GroundCheck child if not assigned
        if (!groundCheck)
        {
            groundCheck = transform.Find("GroundCheck");
            if (!groundCheck)
            {
                Debug.LogError("PlayerControllerScript: groundCheck Transform is not assigned and GroundCheck child not found!");
            }
        }

        // Get health system for death checks
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.OnDeath += HandlePlayerDeath;
        }

        // Get ability components if they exist
        doubleJumpAbility = GetComponent<DoubleJumpAbility>();
        dashAbility = GetComponent<DashAbility>();
        combatController = GetComponent<CombatController>();
        statSystem = GetComponent<StatSystem>();
        if (statSystem == null)
        {
            statSystem = gameObject.AddComponent<StatSystem>();
        }

        // Get animator
        animator = GetComponent<Animator>();

        // Get or create AudioSource for player sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDeath -= HandlePlayerDeath;
        }
    }

    private void HandlePlayerDeath()
    {
        horizontal = 0f;
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }
    }

    private void Update()
    {
        if (IsDead)
            return;

        // Refresh ability references each frame (only checks if null)
        if (doubleJumpAbility == null)
            doubleJumpAbility = GetComponent<DoubleJumpAbility>();
        if (dashAbility == null)
            dashAbility = GetComponent<DashAbility>();
        if (combatController == null)
            combatController = GetComponent<CombatController>();
        if (statSystem == null)
            statSystem = GetComponent<StatSystem>();

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

            // Play landing sound
            audioSource.PlayOneShot(landSound, SFXManager.GetVolume() * landVolume);
        }

        // Only decrement coyote time when in the air
        if (!grounded)
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        wasGrounded = grounded;

        // Footstep sounds while moving on ground
        if (grounded && Mathf.Abs(horizontal) > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                audioSource.PlayOneShot(footstepSound, SFXManager.GetVolume() * footstepVolume);
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }

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

        // Update animator parameters
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        animator.SetFloat(AnimSpeed, Mathf.Abs(horizontal));
        animator.SetBool(AnimIsGrounded, IsGrounded());
        animator.SetFloat(AnimVelocityY, rb.linearVelocity.y);
    }

    private void FixedUpdate()
    {
        if (IsDead)
            return;

        // Skip normal movement if dashing
        if (dashAbility != null && dashAbility.IsDashing())
        {
            return;
        }

        // Skip or modify movement if attacking
        if (combatController != null && !combatController.CanMove())
        {
            // Still apply gravity and clamp fall speed
            ApplyGravityAndFallSpeed();
            return;
        }

        // Horizontal movement (apply combat and stat multipliers)
        float moveMultiplier = combatController != null ? combatController.GetMovementMultiplier() : 1f;
        float agiMultiplier = statSystem != null ? statSystem.SpeedMultiplier : 1f;
        rb.linearVelocity = new Vector2(horizontal * speed * moveMultiplier * agiMultiplier, rb.linearVelocity.y);

        // Flip character based on movement direction
        if (horizontal > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (horizontal < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        ApplyGravityAndFallSpeed();
    }

    private void ApplyGravityAndFallSpeed()
    {
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
        if (IsDead) { horizontal = 0f; return; }

        // Forward move input to combat controller for direction detection
        combatController?.OnMove(context);

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
        if (IsDead) return;

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
        if (IsDead) return;

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

    public void Attack(InputAction.CallbackContext context)
    {
        if (IsDead) return;
        combatController?.OnAttack(context);
    }

    public void SwitchWeapon(InputAction.CallbackContext context)
    {
        if (IsDead) return;
        combatController?.OnWeaponSwitch(context);
    }
    #endregion

    private void PerformJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
        audioSource.PlayOneShot(jumpSound, SFXManager.GetVolume() * jumpVolume);
    }

    private bool IsGrounded()
    {
        if (!groundCheck)
        {
            // Try to find it again at runtime
            groundCheck = transform.Find("GroundCheck");
            if (!groundCheck)
            {
                Debug.LogWarning("PlayerControllerScript: GroundCheck still not found!");
                return false;
            }
        }

        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        // Check if any collider found is NOT our own and is NOT a trigger
        foreach (Collider2D collider in colliders)
        {
            if (collider.gameObject != gameObject && !collider.isTrigger)
            {
                return true;
            }
        }

        return false;
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = IsGrounded() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    // Call this when a new ability is added
    public void RefreshAbilities()
    {
        doubleJumpAbility = GetComponent<DoubleJumpAbility>();
        dashAbility = GetComponent<DashAbility>();
        combatController = GetComponent<CombatController>();
        statSystem = GetComponent<StatSystem>();
    }

    // Expose grounded state for other systems
    public bool GetIsGrounded()
    {
        return IsGrounded();
    }

    // Expose animator for other systems
    public Animator GetAnimator()
    {
        return animator;
    }
}