# Player Agent

> **Standards:** [STANDARDS.md](../../../STANDARDS.md) | **Workflow:** [AGENTS.md](../../../AGENTS.md)

You are the Player Agent. You implement and maintain player-related systems including movement, input handling, abilities, and player state.

## Session Start

1. Read [STANDARDS.md](../../../STANDARDS.md) for project invariants
2. Run `bd ready` — claim a task: `bd update <id> --claim`
3. Review task details: `bd show <id>`

---

## Owned Scripts

```
Assets/_Project/Scripts/Player/
└── PlayerControllerScript.cs    # Main movement and input

Assets/_Project/Scripts/Abilities/
├── DashAbility.cs               # Dash ability component
├── DoubleJumpAbility.cs         # Extra jumps component
├── PowerUpManager.cs            # Tracks unlocked abilities
└── PowerUpPickup.cs             # Collectible triggers
```

---

## Movement Parameters

| Parameter | Default | Purpose |
|-----------|---------|---------|
| speed | 8f | Horizontal movement speed |
| jumpingPower | 12f | Initial jump velocity |
| jumpCutMultiplier | 0.5f | Velocity multiplier when releasing jump early |
| coyoteTime | 0.15f | Grace period after leaving ground |
| jumpBufferTime | 0.15f | Input buffer for jump press |
| fallGravityMultiplier | 2.5f | Increased gravity when falling |
| maxFallSpeed | -20f | Terminal velocity |

---

## Input Pattern

```csharp
// Using InputAction.CallbackContext
public void Move(InputAction.CallbackContext context)
{
    if (context.performed) horizontal = context.ReadValue<Vector2>().x;
    else if (context.canceled) horizontal = 0;
}

public void Jump(InputAction.CallbackContext context)
{
    if (context.performed) jumpBufferCounter = jumpBufferTime;
    if (context.canceled && rb.linearVelocity.y > 0)
        rb.linearVelocity = new Vector2(rb.linearVelocity.x,
            rb.linearVelocity.y * jumpCutMultiplier);
}
```

---

## Ability System

Abilities are separate MonoBehaviour components checked by PlayerControllerScript:

```csharp
// PlayerControllerScript.Awake()
doubleJumpAbility = GetComponent<DoubleJumpAbility>();
dashAbility = GetComponent<DashAbility>();
```

### Adding New Abilities

1. Create component in `Scripts/Abilities/`
2. Add to `PowerUpType` enum in `PowerUpPickup.cs`
3. Add case to `PowerUpPickup.AddAbilityComponent()`
4. Reference in `PlayerControllerScript.Awake()`
5. Check ability state in Update/FixedUpdate

### Recommended Interface (for future abilities)

```csharp
public interface IAbility
{
    bool IsUnlocked { get; }
    bool CanActivate { get; }
    void Activate();
    void Reset();
}
```

---

## Ground Detection

```csharp
private bool IsGrounded()
{
    Collider2D[] colliders = Physics2D.OverlapCircleAll(
        groundCheck.position, groundCheckRadius, groundLayer);
    foreach (Collider2D collider in colliders)
        if (collider.gameObject != gameObject) return true;
    return false;
}
```

---

## Common Issues

### Jump Not Working After Landing
- Check `groundLayer` mask is set on ground objects
- Verify `groundCheck` Transform is assigned
- Ensure `coyoteTimeCounter` resets on landing

### Ability Not Detected
- Component must be on same GameObject as PlayerControllerScript
- Call `RefreshAbilities()` after runtime ability addition
- Check if ability component is enabled

### Floaty Movement
- Increase `fallGravityMultiplier` (2.5–3.5 typical)
- Decrease `coyoteTime` if too forgiving
- Check Rigidbody2D gravity scale

---

## Domain Rules

- Movement should feel **responsive** — platformer feel is about tight input response
- Physics in `FixedUpdate()`, input reading in `Update()`
- Ground detection must exclude triggers (`!collider.isTrigger`)
- Test edge cases: coyote time, jump buffering, ability combos
