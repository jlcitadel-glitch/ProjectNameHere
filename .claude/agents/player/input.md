# Input

> Back to [CLAUDE.md](CLAUDE.md)

## InputAction.CallbackContext Pattern

All player input uses Unity's Input System with `InputAction.CallbackContext` callbacks. These are wired via the Player Input component in the Inspector (or via code with `playerInput.actions["ActionName"].performed += ...`).

Each callback handles two phases:
- **`performed`** — the input was activated (button pressed, stick moved past threshold)
- **`canceled`** — the input was released (button released, stick returned to dead zone)

```csharp
public void Move(InputAction.CallbackContext context)
{
    if (context.performed)
        horizontal = context.ReadValue<Vector2>().x;
    else if (context.canceled)
        horizontal = 0;
}
```

**Key rule:** Read input values in these callbacks (which fire in `Update` timing), then apply physics using those values in `FixedUpdate`. Never call `context.ReadValue` inside `FixedUpdate` — the context may be stale or invalid.

---

## Jump Input with Buffering

Jump uses both `performed` (press) and `canceled` (release) for variable jump height:

```csharp
public void Jump(InputAction.CallbackContext context)
{
    if (context.performed)
        jumpBufferCounter = jumpBufferTime;  // Start the buffer window

    if (context.canceled && rb.linearVelocity.y > 0)
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            rb.linearVelocity.y * jumpCutMultiplier);  // Cut jump short
}
```

The buffer counter is decremented each frame in `Update`. The actual jump executes when both conditions are met: `jumpBufferCounter > 0` AND `coyoteTimeCounter > 0`.

---

## Coyote Time Implementation

Coyote time allows the player to jump for a brief window after walking off a ledge:

```csharp
// In Update()
if (IsGrounded())
    coyoteTimeCounter = coyoteTime;    // Reset window while grounded
else
    coyoteTimeCounter -= Time.deltaTime; // Count down while airborne

// Jump executes when both buffers overlap
if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
{
    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
    jumpBufferCounter = 0f;   // Consume the buffer
    coyoteTimeCounter = 0f;   // Consume coyote time
}
```

**Why both timers exist:** Jump buffering forgives early presses (before landing). Coyote time forgives late presses (after leaving ground). Together they create a forgiving jump system where the player almost never misses a jump they intended.

---

## Jump Buffer + Coyote Time Interaction

These two systems overlap intentionally:

```
Timeline:
  ... grounded ... | ledge | ... airborne ...
                         |<-- coyoteTime -->|
  |<-- jumpBufferTime -->|
       ^press here               ^or here — both result in a jump
```

**Edge case:** If the player presses jump during coyote time while also having a dash available, the coyote jump should not consume the double jump counter. The jump is still considered a "grounded" jump for ability tracking purposes.

---

## Input Remapping Considerations

The project uses Unity's Input System 1.17.0 which supports runtime rebinding:

- Input actions are defined in an `.inputactions` asset (not hardcoded).
- Rebinding UI should use `InputActionRebindingExtensions.PerformInteractiveRebinding()`.
- Rebindings persist via `InputActionAsset.SaveBindingOverridesAsJson()` / `LoadBindingOverridesFromJson()`.
- Save/load of rebindings should integrate with `SaveManager` (hand off to systems agent).

---

## Common Issues

### Input Dropped on High Refresh Rate Monitors
**Root cause:** Reading input in `FixedUpdate` instead of `Update`. At 144+ FPS, multiple `Update` frames occur between `FixedUpdate` calls, so input events between physics ticks are lost.
**Fix:** Always read input in callbacks or `Update`. Store the value and consume it in `FixedUpdate`.

### Jump Feels Unresponsive
**Root cause:** Jump buffer time too short or not implemented.
**Fix:** Ensure `jumpBufferTime` is at least 0.1s (0.15s recommended). Verify the buffer counter decrements in `Update` (not `FixedUpdate`, which would drain it too slowly on high-FPS systems).

### Variable Jump Height Not Working
**Root cause:** Jump cancel logic not triggering, or `jumpCutMultiplier` set to 1.0.
**Fix:** Confirm the `canceled` branch in the Jump callback fires (log it). Check that `jumpCutMultiplier` is less than 1.0 (0.5 is typical). Verify the velocity check (`rb.linearVelocity.y > 0`) so the cut only applies while rising.
