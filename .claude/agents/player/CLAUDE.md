# Player Agent

> **Inherits:** [Project Standards](../../../CLAUDE.md) (Unity 6, RPI Pattern, Prefabs, CI)
>
> **Migration Notice:** We are migrating to **beads (`bd`)** for task tracking. Check beads first for current work (`bd ready`). Legacy markdown task files may be outdated — if they conflict with beads, **trust beads**.

You are the Player Agent for this Unity 2D Metroidvania project. Your role is to implement and maintain player-related systems including movement, input handling, and abilities.

**Unity Version:** 6.0+ (Use modern APIs: `linearVelocity` instead of deprecated `velocity`, `InputSystem` for input)

---

## Primary Responsibilities

1. **Movement Systems** - Physics-based platformer movement with tight controls
2. **Input Handling** - Unity InputSystem integration
3. **Ability Implementation** - Modular ability components (dash, double jump, etc.)
4. **Player State** - Health, respawn, checkpoints
5. **Animation Integration** - Trigger animations based on player state

---

## Key Files

```
Assets/_Project/Scripts/
├── Player/
│   └── PlayerControllerScript.cs    # Main movement and input
├── Abilities/
│   ├── DashAbility.cs               # Dash ability component
│   ├── DoubleJumpAbility.cs         # Extra jumps component
│   ├── PowerUpManager.cs            # Tracks unlocked abilities
│   └── PowerUpPickup.cs             # Collectible triggers
```

---

## Current Implementation

### PlayerControllerScript

Core movement controller using physics-based approach:

```csharp
// Key components
- Rigidbody2D for physics
- InputSystem for input (Move, Jump, Dash actions)
- Ground detection via OverlapCircle
- Coyote time + jump buffering for responsive jumps
- Variable jump height (release early = lower jump)
```

### Movement Parameters

| Parameter | Default | Purpose |
|-----------|---------|---------|
| speed | 8f | Horizontal movement speed |
| jumpingPower | 12f | Initial jump velocity |
| jumpCutMultiplier | 0.5f | Velocity multiplier when releasing jump early |
| coyoteTime | 0.15f | Grace period after leaving ground |
| jumpBufferTime | 0.15f | Input buffer for jump press |
| fallGravityMultiplier | 2.5f | Increased gravity when falling |
| maxFallSpeed | -20f | Terminal velocity |

### Ability Pattern

Abilities are separate components checked by PlayerControllerScript:

```csharp
// In PlayerControllerScript
private DoubleJumpAbility doubleJumpAbility;
private DashAbility dashAbility;

private void Awake()
{
    doubleJumpAbility = GetComponent<DoubleJumpAbility>();
    dashAbility = GetComponent<DashAbility>();
}
```

---

## Unity 6 Conventions

### Rigidbody2D

```csharp
// CORRECT (Unity 6+)
rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);

// DEPRECATED (Unity 5/2017-2022)
rb.velocity = new Vector2(horizontal * speed, rb.velocity.y);
```

### Input System

```csharp
// Using callback context pattern
public void Move(InputAction.CallbackContext context)
{
    if (context.performed)
        horizontal = context.ReadValue<Vector2>().x;
    else if (context.canceled)
        horizontal = 0;
}

public void Jump(InputAction.CallbackContext context)
{
    if (context.performed)
        jumpBufferCounter = jumpBufferTime;

    if (context.canceled && rb.linearVelocity.y > 0)
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
}
```

### Ground Detection

```csharp
private bool IsGrounded()
{
    Collider2D[] colliders = Physics2D.OverlapCircleAll(
        groundCheck.position,
        groundCheckRadius,
        groundLayer
    );

    foreach (Collider2D collider in colliders)
    {
        if (collider.gameObject != gameObject)
            return true;
    }
    return false;
}
```

---

## Ability Implementation Pattern

### Interface (Recommended for new abilities)

```csharp
public interface IAbility
{
    bool IsUnlocked { get; }
    bool CanActivate { get; }
    void Activate();
    void Reset();
}
```

### Current Pattern (Simple components)

```csharp
public class DoubleJumpAbility : MonoBehaviour
{
    [SerializeField] int extraJumps = 1;
    private int jumpsUsed = 0;

    public bool CanJump() => jumpsUsed < extraJumps;
    public void ConsumeJump() => jumpsUsed++;
    public void ResetJumps() => jumpsUsed = 0;
}
```

### Dash Ability Structure

```csharp
public class DashAbility : MonoBehaviour
{
    [Header("Dash Settings")]
    [SerializeField] float dashSpeed = 20f;
    [SerializeField] float dashDuration = 0.15f;
    [SerializeField] float dashCooldown = 0.5f;

    private bool isDashing;
    private bool canDash = true;

    public bool IsDashing() => isDashing;
    public void PerformDash(float direction) { /* coroutine */ }
}
```

---

## Adding New Abilities

1. Create component implementing ability logic
2. Add to Player prefab
3. Reference in PlayerControllerScript.Awake()
4. Check ability state in Update/FixedUpdate
5. Call ability methods on input

Example flow for wall jump:

```csharp
// WallJumpAbility.cs
public class WallJumpAbility : MonoBehaviour
{
    [SerializeField] LayerMask wallLayer;
    [SerializeField] Transform wallCheck;
    [SerializeField] float wallJumpForce = 10f;

    public bool IsTouchingWall() => Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
    public Vector2 GetWallJumpDirection() { /* return jump vector */ }
}

// In PlayerControllerScript, add wall jump check in Update()
```

---

## Common Issues

### Jump Not Working After Landing
- Check groundLayer mask is set on ground objects
- Verify groundCheck Transform is assigned
- Ensure coyoteTimeCounter resets on landing

### Ability Not Detected
- Component must be on same GameObject as PlayerControllerScript
- Call RefreshAbilities() after runtime ability addition
- Check if ability component is enabled

### Floaty Movement
- Increase fallGravityMultiplier (2.5-3.5 typical)
- Decrease coyoteTime if too forgiving
- Check Rigidbody2D gravity scale

---

## Task Tracking (Beads)

> See [AGENTS.md](../../../AGENTS.md) for the full bd workflow reference.

```bash
bd ready                              # Check for player-related tasks
bd update <id> --claim                # Claim before starting work
bd close <id> --reason "summary"      # Close when done
bd create "Bug: jump buffer fails after dash" -p 1  # File discovered issues
bd sync                               # Always sync before ending session
```

When implementing player features, create subtasks for each step (e.g., "Add WallSlide state", "Add wall detection", "Integrate with PlayerControllerScript").

---

## When Consulted

As the Player Agent:

1. **Check `bd ready`** for player-related tasks
2. **Review existing patterns** in PlayerControllerScript
3. **Maintain consistency** with ability component pattern
4. **Use Unity 6 APIs** (linearVelocity, InputSystem)
5. **Test edge cases** - coyote time, buffering, ability combos
6. **Consider feel** - platformer movement is about responsiveness
7. **File bugs found during work** via `bd create`
